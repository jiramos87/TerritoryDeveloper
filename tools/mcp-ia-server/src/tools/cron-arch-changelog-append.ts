/**
 * MCP tool: cron_arch_changelog_append_enqueue
 *
 * Fire-and-forget enqueue of one arch-changelog row into cron_arch_changelog_append_jobs.
 * Mirrors arch_changelog_append payload shape.
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

export function registerCronArchChangelogAppendEnqueue(server: McpServer): void {
  server.registerTool(
    "cron_arch_changelog_append_enqueue",
    {
      description:
        "Fire-and-forget: enqueue one arch-changelog row into cron_arch_changelog_append_jobs. Cron supervisor drains it to arch_changelog. Same payload shape as arch_changelog_append. Returns {job_id, status:'queued'} in <100ms.",
      inputSchema: {
        decision_slug: z.string().describe("Decision slug (e.g. plan-{slug}-boundaries)."),
        kind: z
          .enum([
            "edit",
            "decide",
            "supersede",
            "spec_edit_commit",
            "design_explore_decision",
            "design_explore_persist_contract_v2",
          ])
          .describe("Audit row kind."),
        body: z.string().describe("Markdown body / audit note."),
        surface_path: z.string().optional().describe("Spec path (maps to spec_path in arch_changelog)."),
        commit_sha: z.string().optional().describe("Commit sha (optional). Enables dedup on (commit_sha, spec_path)."),
        plan_slug: z.string().optional().describe("Master-plan slug for per-plan attribution."),
        idempotency_key: z
          .string()
          .optional()
          .describe("Dedup key (optional)."),
      },
    },
    async (args) =>
      runWithToolTiming("cron_arch_changelog_append_enqueue", async () => {
        const envelope = await wrapTool(
          async (
            input:
              | {
                  decision_slug?: string;
                  kind?: string;
                  body?: string;
                  surface_path?: string;
                  commit_sha?: string;
                  plan_slug?: string;
                  idempotency_key?: string;
                }
              | undefined,
          ) => {
            const decision_slug = (input?.decision_slug ?? "").trim();
            const kind = (input?.kind ?? "").trim();
            const body = input?.body ?? "";
            if (!decision_slug || !kind || !body) {
              throw {
                code: "invalid_input",
                message: "decision_slug, kind, and body are required.",
              };
            }
            const pool = getIaDatabasePool();
            if (!pool) {
              throw new IaDbUnavailableError();
            }
            const idempotency_key = input?.idempotency_key ?? null;
            const res = await pool.query<{ job_id: string }>(
              `INSERT INTO cron_arch_changelog_append_jobs
                 (decision_slug, kind, body, surface_path, commit_sha, plan_slug, idempotency_key)
               VALUES ($1, $2, $3::jsonb, $4, $5, $6, $7)
               ON CONFLICT (idempotency_key) WHERE idempotency_key IS NOT NULL DO NOTHING
               RETURNING job_id::text`,
              [
                decision_slug,
                kind,
                JSON.stringify(body),
                input?.surface_path ?? null,
                input?.commit_sha ?? null,
                input?.plan_slug ?? null,
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
                decision_slug?: string;
                kind?: string;
                body?: string;
                surface_path?: string;
                commit_sha?: string;
                plan_slug?: string;
                idempotency_key?: string;
              }
            | undefined,
        );
        return jsonResult(envelope);
      }),
  );
}
