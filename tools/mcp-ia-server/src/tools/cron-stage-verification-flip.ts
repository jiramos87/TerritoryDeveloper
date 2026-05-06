/**
 * MCP tool: cron_stage_verification_flip_enqueue
 *
 * Fire-and-forget enqueue of one stage-verification row into cron_stage_verification_flip_jobs.
 * Mirrors stage_verification_flip payload shape.
 * P95 < 100 ms (single INSERT + commit).
 * Returns {job_id, status:'queued'}.
 *
 * TECH-18094 / async-cron-jobs Stage 2.0.2
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

export function registerCronStageVerificationFlipEnqueue(server: McpServer): void {
  server.registerTool(
    "cron_stage_verification_flip_enqueue",
    {
      description:
        "Fire-and-forget: enqueue one stage-verification row into cron_stage_verification_flip_jobs. Cron supervisor drains it to ia_stage_verifications. Same payload shape as stage_verification_flip. Returns {job_id, status:'queued'} in <100ms.",
      inputSchema: {
        slug: z.string().describe("Master-plan slug."),
        stage_id: z.string().describe("Stage id."),
        verdict: z
          .enum(["pass", "fail", "partial"])
          .describe("Verdict: pass|fail|partial."),
        actor: z.string().optional().describe("Who recorded the verdict."),
        commit_sha: z.string().optional().describe("Stage commit sha."),
        notes: z.string().optional().describe("Short caveman note."),
        idempotency_key: z
          .string()
          .optional()
          .describe("Dedup key (optional)."),
      },
    },
    async (args) =>
      runWithToolTiming("cron_stage_verification_flip_enqueue", async () => {
        const envelope = await wrapTool(
          async (
            input:
              | {
                  slug?: string;
                  stage_id?: string;
                  verdict?: "pass" | "fail" | "partial";
                  actor?: string;
                  commit_sha?: string;
                  notes?: string;
                  idempotency_key?: string;
                }
              | undefined,
          ) => {
            const slug = (input?.slug ?? "").trim();
            const stage_id = (input?.stage_id ?? "").trim();
            const verdict = input?.verdict;
            if (!slug || !stage_id || !verdict) {
              throw {
                code: "invalid_input",
                message: "slug, stage_id, and verdict are required.",
              };
            }
            const pool = getIaDatabasePool();
            if (!pool) {
              throw new IaDbUnavailableError();
            }
            const idempotency_key = input?.idempotency_key ?? null;
            const res = await pool.query<{ job_id: string }>(
              `INSERT INTO cron_stage_verification_flip_jobs
                 (slug, stage_id, verdict, actor, commit_sha, notes, idempotency_key)
               VALUES ($1, $2, $3, $4, $5, $6, $7)
               ON CONFLICT (idempotency_key) WHERE idempotency_key IS NOT NULL DO NOTHING
               RETURNING job_id::text`,
              [
                slug,
                stage_id,
                verdict,
                input?.actor ?? null,
                input?.commit_sha ?? null,
                input?.notes ?? null,
                idempotency_key,
              ],
            );
            if (res.rowCount === 0) {
              return { job_id: null, status: "queued", deduped: true };
            }
            return { job_id: res.rows[0]!.job_id, status: "queued" };
          },
        )(
          args as
            | {
                slug?: string;
                stage_id?: string;
                verdict?: "pass" | "fail" | "partial";
                actor?: string;
                commit_sha?: string;
                notes?: string;
                idempotency_key?: string;
              }
            | undefined,
        );
        return jsonResult(envelope);
      }),
  );
}
