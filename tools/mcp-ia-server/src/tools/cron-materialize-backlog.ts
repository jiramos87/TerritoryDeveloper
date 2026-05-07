/**
 * MCP tool: cron_materialize_backlog_enqueue
 *
 * Fire-and-forget enqueue of one materialize-backlog run into cron_materialize_backlog_jobs.
 * Cron supervisor drains it by shelling to bash tools/scripts/materialize-backlog.sh.
 * P95 < 100 ms (single INSERT + commit).
 * Returns {job_id, status:'queued'}.
 *
 * TECH-18098 / async-cron-jobs Stage 3.0.2
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

export function registerCronMaterializeBacklogEnqueue(server: McpServer): void {
  server.registerTool(
    "cron_materialize_backlog_enqueue",
    {
      description:
        "Fire-and-forget: enqueue one materialize-backlog run into cron_materialize_backlog_jobs. Cron supervisor drains it by running bash tools/scripts/materialize-backlog.sh (cadence: */2 * * * *). Returns {job_id, status:'queued'} in <100ms.",
      inputSchema: {
        triggered_by: z
          .string()
          .optional()
          .describe("Caller tag (e.g. 'project-new-apply', 'closeout-tail')."),
        idempotency_key: z
          .string()
          .optional()
          .describe("Dedup key (optional). Duplicate key → no-op enqueue."),
      },
    },
    async (args) =>
      runWithToolTiming("cron_materialize_backlog_enqueue", async () => {
        const envelope = await wrapTool(
          async (
            input:
              | {
                  triggered_by?: string;
                  idempotency_key?: string;
                }
              | undefined,
          ) => {
            const pool = getIaDatabasePool();
            if (!pool) {
              throw new IaDbUnavailableError();
            }
            const idempotency_key = input?.idempotency_key ?? null;
            const res = await pool.query<{ job_id: string }>(
              `INSERT INTO cron_materialize_backlog_jobs
                 (triggered_by, idempotency_key)
               VALUES ($1, $2)
               ON CONFLICT (idempotency_key) WHERE idempotency_key IS NOT NULL DO NOTHING
               RETURNING job_id::text`,
              [input?.triggered_by ?? null, idempotency_key],
            );
            if (res.rowCount === 0) {
              return { job_id: null, status: "queued", deduped: true };
            }
            return { job_id: res.rows[0]!.job_id, status: "queued" };
          },
        )(
          args as
            | {
                triggered_by?: string;
                idempotency_key?: string;
              }
            | undefined,
        );
        return jsonResult(envelope);
      }),
  );
}
