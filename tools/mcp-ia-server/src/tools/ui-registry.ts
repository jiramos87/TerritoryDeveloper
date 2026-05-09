/**
 * MCP tools: action_registry_list + bind_registry_list — Wave A0 (TECH-27061).
 *
 * Read-only slices backed by action_registry_log + bind_registry_log tables
 * populated by UiActionRegistry / UiBindRegistry Awake hooks via Editor bridge.
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { getIaDatabasePool } from "../ia-db/pool.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool, dbUnconfiguredError } from "../envelope.js";

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

// ── action_registry_list ──────────────────────────────────────────────────

const actionListSchema = z.object({
  handler_bound: z
    .boolean()
    .optional()
    .describe("When true, return only handler-bound entries. Omit for all."),
});

export function registerActionRegistryList(server: McpServer): void {
  server.registerTool(
    "action_registry_list",
    {
      description:
        "Wave A0 — list all registered action ids from action_registry_log. " +
        "Returns array of { ref_id, handler_bound, last_updated_at }. " +
        "Populated by UiActionRegistry Awake hook via Editor bridge. Empty before first Play Mode run.",
      inputSchema: actionListSchema,
    },
    async (args) =>
      runWithToolTiming("action_registry_list", async () => {
        const envelope = await wrapTool(async (input: z.infer<typeof actionListSchema>) => {
          const pool = getIaDatabasePool();
          if (!pool) throw dbUnconfiguredError();

          const client = await pool.connect();
          try {
            let sql =
              "SELECT ref_id, handler_bound, last_updated_at FROM action_registry_log";
            const params: unknown[] = [];
            if (input.handler_bound !== undefined) {
              sql += " WHERE handler_bound = $1";
              params.push(input.handler_bound);
            }
            sql += " ORDER BY ref_id";

            const result = await client.query(sql, params);
            return {
              count: result.rows.length,
              actions: result.rows,
            };
          } catch (e) {
            const msg = e instanceof Error ? e.message : String(e);
            throw { code: "db_error" as const, message: msg };
          } finally {
            client.release();
          }
        })(actionListSchema.parse(args ?? {}));
        return jsonResult(envelope);
      }),
  );
}

// ── bind_registry_list ───────────────────────────────────────────────────

const bindListSchema = z.object({
  include_values: z
    .boolean()
    .optional()
    .describe("When true, include value_json in response. Default false."),
});

export function registerBindRegistryList(server: McpServer): void {
  server.registerTool(
    "bind_registry_list",
    {
      description:
        "Wave A0 — list all registered bind ids from bind_registry_log. " +
        "Returns array of { ref_id, handler_bound, subscriber_count, last_updated_at } " +
        "and optionally value_json when include_values=true. " +
        "Populated by UiBindRegistry Awake hook via Editor bridge. Empty before first Play Mode run.",
      inputSchema: bindListSchema,
    },
    async (args) =>
      runWithToolTiming("bind_registry_list", async () => {
        const envelope = await wrapTool(async (input: z.infer<typeof bindListSchema>) => {
          const pool = getIaDatabasePool();
          if (!pool) throw dbUnconfiguredError();

          const client = await pool.connect();
          try {
            const cols = input.include_values
              ? "ref_id, handler_bound, value_json, subscriber_count, last_updated_at"
              : "ref_id, handler_bound, subscriber_count, last_updated_at";
            const sql = `SELECT ${cols} FROM bind_registry_log ORDER BY ref_id`;

            const result = await client.query(sql);
            return {
              count: result.rows.length,
              binds: result.rows,
            };
          } catch (e) {
            const msg = e instanceof Error ? e.message : String(e);
            throw { code: "db_error" as const, message: msg };
          } finally {
            client.release();
          }
        })(bindListSchema.parse(args ?? {}));
        return jsonResult(envelope);
      }),
  );
}
