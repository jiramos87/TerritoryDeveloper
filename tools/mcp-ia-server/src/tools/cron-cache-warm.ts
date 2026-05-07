/**
 * MCP tool: cron_cache_warm_enqueue
 *
 * Fire-and-forget enqueue of one cache-warm job into cron_cache_warm_jobs.
 * Cron supervisor drains it by pre-populating ia_mcp_context_cache for the
 * given (cache_kind, cache_key) pair (cadence: every 5 min).
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

export function registerCronCacheWarmEnqueue(server: McpServer): void {
  server.registerTool(
    "cron_cache_warm_enqueue",
    {
      description:
        "Fire-and-forget: enqueue one cache-warm job into cron_cache_warm_jobs. Cron supervisor drains it by pre-populating ia_mcp_context_cache for the given (cache_kind, cache_key) pair (cadence: */5 * * * * — every 5 min). cache_kind: e.g. 'db_read_batch'. cache_key: for db_read_batch, format is 'db_read_batch:<raw_sql>' — the handler normalizes, hashes, and writes the cache entry. slug: plan_id namespace for ia_mcp_context_cache (default: 'global'). Returns {job_id, status:'queued'} in <100ms.",
      inputSchema: {
        cache_kind: z
          .string()
          .min(1)
          .describe("Cache kind (e.g. 'db_read_batch')."),
        cache_key: z
          .string()
          .min(1)
          .describe("Cache key. For db_read_batch: 'db_read_batch:<raw_sql>' — handler normalizes + hashes."),
        slug: z
          .string()
          .optional()
          .describe("Master-plan slug / plan_id namespace for ia_mcp_context_cache (optional; default: 'global')."),
        idempotency_key: z
          .string()
          .optional()
          .describe("Dedup key (optional). Duplicate key → no-op enqueue."),
      },
    },
    async (args) =>
      runWithToolTiming("cron_cache_warm_enqueue", async () => {
        const envelope = await wrapTool(
          async (
            input:
              | {
                  cache_kind?: string;
                  cache_key?: string;
                  slug?: string;
                  idempotency_key?: string;
                }
              | undefined,
          ) => {
            const pool = getIaDatabasePool();
            if (!pool) {
              throw new IaDbUnavailableError();
            }
            const cache_kind = (input?.cache_kind ?? "").trim();
            const cache_key = (input?.cache_key ?? "").trim();
            if (!cache_kind) throw { code: "invalid_input", message: "cache_kind is required." };
            if (!cache_key) throw { code: "invalid_input", message: "cache_key is required." };
            const slug = input?.slug ?? null;
            const idempotency_key = input?.idempotency_key ?? null;
            const res = await pool.query<{ job_id: string }>(
              `INSERT INTO cron_cache_warm_jobs
                 (cache_kind, cache_key, slug, idempotency_key)
               VALUES ($1, $2, $3, $4)
               ON CONFLICT (idempotency_key) WHERE idempotency_key IS NOT NULL DO NOTHING
               RETURNING job_id::text`,
              [cache_kind, cache_key, slug, idempotency_key],
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
                cache_key?: string;
                slug?: string;
                idempotency_key?: string;
              }
            | undefined,
        );
        return jsonResult(envelope);
      }),
  );
}
