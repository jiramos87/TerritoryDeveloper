/**
 * MCP tool: cron_regen_indexes_enqueue
 *
 * Fire-and-forget enqueue of one regen-indexes run into cron_regen_indexes_jobs.
 * Cron supervisor drains it by running npm run generate:ia-indexes.
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

export function registerCronRegenIndexesEnqueue(server: McpServer): void {
  server.registerTool(
    "cron_regen_indexes_enqueue",
    {
      description:
        "Fire-and-forget: enqueue one regen-indexes run into cron_regen_indexes_jobs. Cron supervisor drains it by running npm run generate:ia-indexes (cadence: */5 * * * *). scope: 'all' | 'glossary' | 'specs' — defaults to 'all'. Returns {job_id, status:'queued'} in <100ms.",
      inputSchema: {
        scope: z
          .enum(["all", "glossary", "specs"])
          .optional()
          .describe("Index scope: 'all' (default), 'glossary', or 'specs'."),
        idempotency_key: z
          .string()
          .optional()
          .describe("Dedup key (optional). Duplicate key → no-op enqueue."),
      },
    },
    async (args) =>
      runWithToolTiming("cron_regen_indexes_enqueue", async () => {
        const envelope = await wrapTool(
          async (
            input:
              | {
                  scope?: "all" | "glossary" | "specs";
                  idempotency_key?: string;
                }
              | undefined,
          ) => {
            const pool = getIaDatabasePool();
            if (!pool) {
              throw new IaDbUnavailableError();
            }
            const scope = input?.scope ?? "all";
            const idempotency_key = input?.idempotency_key ?? null;
            const res = await pool.query<{ job_id: string }>(
              `INSERT INTO cron_regen_indexes_jobs
                 (scope, idempotency_key)
               VALUES ($1, $2)
               ON CONFLICT (idempotency_key) WHERE idempotency_key IS NOT NULL DO NOTHING
               RETURNING job_id::text`,
              [scope, idempotency_key],
            );
            if (res.rowCount === 0) {
              return { job_id: null, status: "queued", deduped: true };
            }
            return { job_id: res.rows[0]!.job_id, status: "queued" };
          },
        )(
          args as
            | {
                scope?: "all" | "glossary" | "specs";
                idempotency_key?: string;
              }
            | undefined,
        );
        return jsonResult(envelope);
      }),
  );
}
