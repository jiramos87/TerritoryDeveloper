/**
 * MCP tool: cron_drift_lint_enqueue
 *
 * Fire-and-forget enqueue of one drift-lint sweep into cron_drift_lint_jobs.
 * Cron supervisor drains it by running tools/scripts/drift-lint-sweep.mjs
 * (reads ia_tasks + ia_spec_anchors; cadence: every 10 min).
 * P95 < 100 ms (single INSERT + commit).
 * Returns {job_id, status:'queued'}.
 *
 * TECH-18105 / async-cron-jobs Stage 5.0.2
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

export function registerCronDriftLintEnqueue(server: McpServer): void {
  server.registerTool(
    "cron_drift_lint_enqueue",
    {
      description:
        "Fire-and-forget: enqueue one drift-lint sweep into cron_drift_lint_jobs. Cron supervisor drains it by running tools/scripts/drift-lint-sweep.mjs (reads ia_tasks + ia_spec_anchors; cadence: */10 * * * * — every 10 min). commit_sha: optional SHA for audit trail. slug: optional plan slug to scope the sweep (reserved; current sweep is full-scan). Returns {job_id, status:'queued'} in <100ms.",
      inputSchema: {
        commit_sha: z
          .string()
          .optional()
          .describe("Git commit SHA for audit trail (optional)."),
        slug: z
          .string()
          .optional()
          .describe("Master-plan slug to scope the sweep (optional; reserved for future per-plan scoping)."),
        idempotency_key: z
          .string()
          .optional()
          .describe("Dedup key (optional). Duplicate key → no-op enqueue."),
      },
    },
    async (args) =>
      runWithToolTiming("cron_drift_lint_enqueue", async () => {
        const envelope = await wrapTool(
          async (
            input:
              | {
                  commit_sha?: string;
                  slug?: string;
                  idempotency_key?: string;
                }
              | undefined,
          ) => {
            const pool = getIaDatabasePool();
            if (!pool) {
              throw new IaDbUnavailableError();
            }
            const commit_sha = input?.commit_sha ?? null;
            const slug = input?.slug ?? null;
            const idempotency_key = input?.idempotency_key ?? null;
            const res = await pool.query<{ job_id: string }>(
              `INSERT INTO cron_drift_lint_jobs
                 (commit_sha, slug, idempotency_key)
               VALUES ($1, $2, $3)
               ON CONFLICT (idempotency_key) WHERE idempotency_key IS NOT NULL DO NOTHING
               RETURNING job_id::text`,
              [commit_sha, slug, idempotency_key],
            );
            if (res.rowCount === 0) {
              return { job_id: null, status: "queued", deduped: true };
            }
            return { job_id: res.rows[0]!.job_id, status: "queued" };
          },
        )(
          args as
            | {
                commit_sha?: string;
                slug?: string;
                idempotency_key?: string;
              }
            | undefined,
        );
        return jsonResult(envelope);
      }),
  );
}
