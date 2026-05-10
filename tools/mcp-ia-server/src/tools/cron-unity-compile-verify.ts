/**
 * MCP tool: cron_unity_compile_verify_enqueue
 *
 * Fire-and-forget enqueue of one unity-compile-verify row into
 * cron_unity_compile_verify_jobs. Cron handler polls
 * unity_bridge_command(kind="get_compilation_status") and writes verdict
 * back to job row + ia_stage_verifications.
 *
 * Replaces synchronous 60s live-Editor compile poll in ship-cycle Phase 8.
 *
 * Lifecycle skills refactor — Phase 2 / weak-spot #9.
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
import { getIaDatabasePool } from "../ia-db/pool.js";
import { IaDbUnavailableError } from "../ia-db/queries.js";

function jsonResult(payload: unknown) {
  return {
    content: [
      {
        type: "text" as const,
        text: JSON.stringify(payload, null, 2),
      },
    ],
  };
}

interface UnityCompileVerifyInput {
  slug?: string;
  stage_id?: string;
  commit_sha?: string;
  bridge_lease_id?: string;
  idempotency_key?: string;
}

export function registerCronUnityCompileVerifyEnqueue(server: McpServer): void {
  server.registerTool(
    "cron_unity_compile_verify_enqueue",
    {
      description:
        "Fire-and-forget: enqueue one unity-compile-verify row into cron_unity_compile_verify_jobs. Cron drainer polls unity_bridge_command get_compilation_status; writes verdict to job row + ia_stage_verifications. Returns {job_id, status:'queued'} in <100ms. Replaces sync 60s compile poll in ship-cycle Phase 8.",
      inputSchema: {
        slug: z.string().describe("Master-plan slug."),
        stage_id: z.string().describe("Stage id (canonical N.M form preferred)."),
        commit_sha: z.string().optional().describe("Stage commit sha (optional)."),
        bridge_lease_id: z
          .string()
          .optional()
          .describe("Pre-acquired bridge lease (optional). Drainer takes its own lease if absent."),
        idempotency_key: z
          .string()
          .optional()
          .describe("Dedup key (optional). Duplicate key → no-op enqueue."),
      },
    },
    async (args) =>
      runWithToolTiming("cron_unity_compile_verify_enqueue", async () => {
        const envelope = await wrapTool(async (input: UnityCompileVerifyInput | undefined) => {
          const slug = (input?.slug ?? "").trim();
          const stage_id = (input?.stage_id ?? "").trim();
          if (!slug || !stage_id) {
            throw {
              code: "invalid_input",
              message: "slug and stage_id are required.",
            };
          }
          const pool = getIaDatabasePool();
          if (!pool) {
            throw new IaDbUnavailableError();
          }
          const idempotency_key = input?.idempotency_key ?? null;
          const res = await pool.query<{ job_id: string }>(
            `INSERT INTO cron_unity_compile_verify_jobs
               (slug, stage_id, commit_sha, bridge_lease_id, idempotency_key)
             VALUES ($1, $2, $3, $4, $5)
             ON CONFLICT (idempotency_key) WHERE idempotency_key IS NOT NULL DO NOTHING
             RETURNING job_id::text`,
            [
              slug,
              stage_id,
              input?.commit_sha ?? null,
              input?.bridge_lease_id ?? null,
              idempotency_key,
            ],
          );
          if (res.rowCount === 0) {
            return { job_id: null, status: "queued", deduped: true };
          }
          return { job_id: res.rows[0]!.job_id, status: "queued" };
        })(args as UnityCompileVerifyInput | undefined);
        return jsonResult(envelope);
      }),
  );
}
