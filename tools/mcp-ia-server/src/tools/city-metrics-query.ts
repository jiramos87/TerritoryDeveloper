/**
 * MCP tool: city_metrics_query — read recent rows from city_metrics_history (Postgres).
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { getIaDatabasePool } from "../ia-db/pool.js";
import { runWithToolTiming } from "../instrumentation.js";

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

export function registerCityMetricsQuery(server: McpServer): void {
  server.registerTool(
    "city_metrics_query",
    {
      description:
        "Returns recent rows from Postgres table **city_metrics_history** (per-tick city metric snapshots written from Unity **MetricsRecorder** when **DATABASE_URL** resolves). Optional filter by **scenario id** (test mode). Requires **DATABASE_URL** or **config/postgres-dev.json** when not in CI.",
      inputSchema: {
        scenario_id: z
          .string()
          .optional()
          .describe(
            "When set, only rows with this **scenario id** (from test mode session). Omit for all scenarios.",
          ),
        last_n_rows: z
          .coerce.number()
          .int()
          .min(1)
          .max(500)
          .optional()
          .describe("Max rows to return (newest first by `id`). Default 50."),
      },
    },
    async (args) =>
      runWithToolTiming("city_metrics_query", async () => {
        const pool = getIaDatabasePool();
        if (!pool) {
          return jsonResult({
            error: "db_unconfigured",
            message:
              "No database URL: set DATABASE_URL, add config/postgres-dev.json, or see docs/postgres-ia-dev-setup.md.",
          });
        }

        const a = args as {
          scenario_id?: string;
          last_n_rows?: number;
        };

        const limit = a.last_n_rows ?? 50;
        const scenario = a.scenario_id?.trim();

        try {
          let rows: Record<string, unknown>[];
          if (scenario) {
            const r = await pool.query(
              `SELECT id, recorded_at, simulation_tick_index, game_date, population, money, happiness,
                      demand_r, demand_c, demand_i, employment_rate, forest_coverage, scenario_id, metadata
               FROM city_metrics_history
               WHERE scenario_id = $1
               ORDER BY id DESC
               LIMIT $2`,
              [scenario, limit],
            );
            rows = r.rows as Record<string, unknown>[];
          } else {
            const r = await pool.query(
              `SELECT id, recorded_at, simulation_tick_index, game_date, population, money, happiness,
                      demand_r, demand_c, demand_i, employment_rate, forest_coverage, scenario_id, metadata
               FROM city_metrics_history
               ORDER BY id DESC
               LIMIT $1`,
              [limit],
            );
            rows = r.rows as Record<string, unknown>[];
          }

          return jsonResult({
            ok: true,
            row_count: rows.length,
            rows,
          });
        } catch (e) {
          const msg = e instanceof Error ? e.message : String(e);
          const code =
            e && typeof e === "object" && "code" in e
              ? String((e as { code?: string }).code)
              : "";
          if (code === "42P01") {
            return jsonResult({
              error: "table_missing",
              message:
                "city_metrics_history not found. Run `npm run db:migrate` from the repository root.",
            });
          }
          return jsonResult({
            error: "query_failed",
            message: msg,
          });
        }
      }),
  );
}
