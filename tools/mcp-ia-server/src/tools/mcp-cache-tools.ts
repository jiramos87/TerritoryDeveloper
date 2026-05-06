/**
 * MCP tools: mcp_cache_get / mcp_cache_set — shared context cache (TECH-15902).
 *
 * Cache `router_for_task` + `glossary_lookup` + `invariants_summary` results
 * per plan_id in ia_mcp_context_cache. Content-hash gating (no TTL).
 * Source-content-hash ensures stale entries are detected on read.
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
import { getIaDatabasePool } from "../ia-db/pool.js";

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

// ---------------------------------------------------------------------------
// mcp_cache_get
// ---------------------------------------------------------------------------

export function registerMcpCacheGet(server: McpServer): void {
  server.registerTool(
    "mcp_cache_get",
    {
      description:
        "Retrieve a cached MCP context entry (router_for_task / glossary_lookup / invariants_summary) by plan_id + key. Returns null payload when miss or stale (content_hash mismatch).",
      inputSchema: {
        plan_id: z.string().min(1).describe("Master-plan slug or session id."),
        key: z.string().min(1).describe("Cache key, e.g. router_for_task:{domain}."),
        content_hash: z
          .string()
          .optional()
          .describe("SHA-256 of current source doc. When provided, miss returned if hash differs."),
      },
    },
    async (args) =>
      runWithToolTiming("mcp_cache_get", async () => {
        const envelope = await wrapTool(
          async (input: { plan_id: string; key: string; content_hash?: string }) => {
            const pool = getIaDatabasePool();
            if (!pool) {
              return { hit: false, payload: null, reason: "db_unavailable" };
            }

            const res = await pool.query<{
              payload: unknown;
              content_hash: string;
            }>(
              `SELECT payload, content_hash
                 FROM ia_mcp_context_cache
                WHERE plan_id = $1 AND key = $2
                LIMIT 1`,
              [input.plan_id, input.key],
            );

            if (res.rowCount === 0) {
              return { hit: false, payload: null, reason: "miss" };
            }

            const row = res.rows[0]!;
            if (input.content_hash && row.content_hash !== input.content_hash) {
              return { hit: false, payload: null, reason: "stale", cached_hash: row.content_hash };
            }

            return { hit: true, payload: row.payload, content_hash: row.content_hash };
          },
        )({
          plan_id: (args as { plan_id?: string }).plan_id ?? "",
          key: (args as { key?: string }).key ?? "",
          content_hash: (args as { content_hash?: string }).content_hash,
        });
        return jsonResult(envelope);
      }),
  );
}

// ---------------------------------------------------------------------------
// mcp_cache_set
// ---------------------------------------------------------------------------

export function registerMcpCacheSet(server: McpServer): void {
  server.registerTool(
    "mcp_cache_set",
    {
      description:
        "Upsert a cached MCP context entry by plan_id + key. content_hash is the SHA-256 of the source document — used for stale detection (no TTL).",
      inputSchema: {
        plan_id: z.string().min(1).describe("Master-plan slug or session id."),
        key: z.string().min(1).describe("Cache key, e.g. glossary_lookup:{term}."),
        payload: z.record(z.string(), z.unknown()).describe("MCP tool response payload to cache."),
        content_hash: z
          .string()
          .min(1)
          .describe("SHA-256 hex of the source document at cache write time."),
      },
    },
    async (args) =>
      runWithToolTiming("mcp_cache_set", async () => {
        const envelope = await wrapTool(
          async (input: {
            plan_id: string;
            key: string;
            payload: Record<string, unknown>;
            content_hash: string;
          }) => {
            const pool = getIaDatabasePool();
            if (!pool) {
              return { ok: false, reason: "db_unavailable" };
            }

            await pool.query(
              `INSERT INTO ia_mcp_context_cache (plan_id, key, payload, content_hash, updated_at)
               VALUES ($1, $2, $3, $4, now())
               ON CONFLICT (plan_id, key)
               DO UPDATE SET payload = EXCLUDED.payload,
                             content_hash = EXCLUDED.content_hash,
                             updated_at = now()`,
              [input.plan_id, input.key, JSON.stringify(input.payload), input.content_hash],
            );

            return { ok: true, plan_id: input.plan_id, key: input.key };
          },
        )({
          plan_id: (args as { plan_id?: string }).plan_id ?? "",
          key: (args as { key?: string }).key ?? "",
          payload: (args as { payload?: Record<string, unknown> }).payload ?? {},
          content_hash: (args as { content_hash?: string }).content_hash ?? "",
        });
        return jsonResult(envelope);
      }),
  );
}
