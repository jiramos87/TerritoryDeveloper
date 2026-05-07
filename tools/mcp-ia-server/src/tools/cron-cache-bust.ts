/**
 * MCP tool: cron_cache_bust_enqueue
 *
 * Fire-and-forget enqueue of one cache-bust invalidation into cron_cache_bust_jobs.
 * Cron supervisor drains it by DELETEing ia_mcp_context_cache rows matching
 * the given (cache_kind, cache_key_pattern) pair (cadence: * * * * * — every minute).
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

export function registerCronCacheBustEnqueue(server: McpServer): void {
  server.registerTool(
    "cron_cache_bust_enqueue",
    {
      description:
        "Fire-and-forget: enqueue one cache-bust invalidation into cron_cache_bust_jobs. Cron supervisor drains it by DELETEing ia_mcp_context_cache rows matching key LIKE cache_key_pattern (cadence: * * * * * — every minute). cache_kind: e.g. 'db_read_batch' (informational for logging; the pattern should include the kind prefix, e.g. 'db_read_batch:%'). cache_key_pattern: SQL LIKE pattern — % wildcard OK (e.g. 'db_read_batch:%' busts all db_read_batch entries). Returns {job_id, status:'queued'} in <100ms.",
      inputSchema: {
        cache_kind: z
          .string()
          .min(1)
          .describe("Cache kind for logging (e.g. 'db_read_batch'). Include the kind as prefix in cache_key_pattern."),
        cache_key_pattern: z
          .string()
          .min(1)
          .describe("SQL LIKE pattern for keys to invalidate (e.g. 'db_read_batch:%' = all db_read_batch entries)."),
        idempotency_key: z
          .string()
          .optional()
          .describe("Dedup key (optional). Duplicate key → no-op enqueue."),
      },
    },
    async (args) =>
      runWithToolTiming("cron_cache_bust_enqueue", async () => {
        const envelope = await wrapTool(
          async (
            input:
              | {
                  cache_kind?: string;
                  cache_key_pattern?: string;
                  idempotency_key?: string;
                }
              | undefined,
          ) => {
            const pool = getIaDatabasePool();
            if (!pool) {
              throw new IaDbUnavailableError();
            }
            const cache_kind = (input?.cache_kind ?? "").trim();
            const cache_key_pattern = (input?.cache_key_pattern ?? "").trim();
            if (!cache_kind) throw { code: "invalid_input", message: "cache_kind is required." };
            if (!cache_key_pattern) throw { code: "invalid_input", message: "cache_key_pattern is required." };
            const idempotency_key = input?.idempotency_key ?? null;
            const res = await pool.query<{ job_id: string }>(
              `INSERT INTO cron_cache_bust_jobs
                 (cache_kind, cache_key_pattern, idempotency_key)
               VALUES ($1, $2, $3)
               ON CONFLICT (idempotency_key) WHERE idempotency_key IS NOT NULL DO NOTHING
               RETURNING job_id::text`,
              [cache_kind, cache_key_pattern, idempotency_key],
            );
            if (res.rowCount === 0) {
              return { job_id: null, status: "queued", deduped: true };
            }
            return { job_id: res.rows[0]!.job_id, status: "queued" };
          },
        )(
          args as
            | {
                cache_kind?: string;
                cache_key_pattern?: string;
                idempotency_key?: string;
              }
            | undefined,
        );
        return jsonResult(envelope);
      }),
  );
}
