/**
 * MCP tool: ui_bake_history_query — read recent rows from ia_ui_bake_history
 * joined with ia_bake_diffs (Layer 6 auditability, TECH-28379).
 *
 * Lets agents trace bake drift across runs without scraping the web dashboard.
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { getIaDatabasePool } from "../ia-db/pool.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool, dbUnconfiguredError } from "../envelope.js";

// ── Exported query function (used by MCP tool + test harness) ────────────────

export interface BakeHistoryRow {
  id: number;
  panel_slug: string;
  baked_at: string;
  bake_handler_version: string;
  diff_summary: Record<string, unknown>;
  commit_sha: string;
  diffs: BakeDiffRow[];
}

export interface BakeDiffRow {
  id: number;
  change_kind: string;
  child_kind: string;
  slug: string;
  before: unknown;
  after: unknown;
}

/**
 * Query recent bake history rows for a given panel, optionally limited.
 * Accepts a raw connection string (for test harness) or uses the pool.
 */
export async function uiBakeHistoryQuery(
  dbUrl: string,
  panelSlug: string,
  limit: number,
): Promise<BakeHistoryRow[]> {
  // Lazy import so tree-shaking works in environments without pg.
  const pg = await import("pg");
  const pool = new pg.default.Pool({ connectionString: dbUrl, max: 2 });
  try {
    const historyResult = await pool.query<{
      id: number;
      panel_slug: string;
      baked_at: Date;
      bake_handler_version: string;
      diff_summary: Record<string, unknown>;
      commit_sha: string;
    }>(
      `SELECT id, panel_slug, baked_at, bake_handler_version, diff_summary, commit_sha
       FROM ia_ui_bake_history
       WHERE panel_slug = $1
       ORDER BY baked_at DESC
       LIMIT $2`,
      [panelSlug, limit],
    );

    const historyIds = historyResult.rows.map((r) => r.id);
    let diffsByHistoryId = new Map<number, BakeDiffRow[]>();

    if (historyIds.length > 0) {
      const diffResult = await pool.query<{
        id: number;
        history_id: number;
        change_kind: string;
        child_kind: string;
        slug: string;
        before: unknown;
        after: unknown;
      }>(
        `SELECT id, history_id, change_kind, child_kind, slug, before, after
         FROM ia_bake_diffs
         WHERE history_id = ANY($1::bigint[])
         ORDER BY history_id, id`,
        [historyIds],
      );
      for (const diff of diffResult.rows) {
        const arr = diffsByHistoryId.get(diff.history_id) ?? [];
        arr.push({
          id: diff.id,
          change_kind: diff.change_kind,
          child_kind: diff.child_kind,
          slug: diff.slug,
          before: diff.before,
          after: diff.after,
        });
        diffsByHistoryId.set(diff.history_id, arr);
      }
    }

    return historyResult.rows.map((row) => ({
      id: row.id,
      panel_slug: row.panel_slug,
      baked_at: row.baked_at instanceof Date
        ? row.baked_at.toISOString()
        : String(row.baked_at),
      bake_handler_version: row.bake_handler_version,
      diff_summary: row.diff_summary ?? {},
      commit_sha: row.commit_sha ?? "",
      diffs: diffsByHistoryId.get(row.id) ?? [],
    }));
  } finally {
    await pool.end().catch(() => {});
  }
}

// ── MCP registration ──────────────────────────────────────────────────────────

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

export function registerUiBakeHistoryQuery(server: McpServer): void {
  server.registerTool(
    "ui_bake_history_query",
    {
      description:
        "Returns recent bake-history rows from **ia_ui_bake_history** joined with **ia_bake_diffs** " +
        "(Layer 6 auditability — TECH-28379). " +
        "Lets agents trace bake drift across runs without scraping the web dashboard. " +
        "Requires **DATABASE_URL** or **config/postgres-dev.json** + migration 0151.",
      inputSchema: {
        panel_slug: z
          .string()
          .min(1)
          .describe("Panel slug to query (e.g. 'settings', 'budget-panel')."),
        limit: z
          .coerce.number()
          .int()
          .min(1)
          .max(200)
          .optional()
          .describe("Max rows to return (newest first by baked_at). Default 10."),
      },
    },
    async (args) =>
      runWithToolTiming("ui_bake_history_query", async () => {
        const envelope = await wrapTool(
          async (input: { panel_slug: string; limit?: number }) => {
            const pool = getIaDatabasePool();
            if (!pool) throw dbUnconfiguredError();

            const limit = input.limit ?? 10;
            const panelSlug = input.panel_slug.trim();

            try {
              const historyResult = await pool.query<{
                id: number;
                panel_slug: string;
                baked_at: Date;
                bake_handler_version: string;
                diff_summary: Record<string, unknown>;
                commit_sha: string;
              }>(
                `SELECT id, panel_slug, baked_at, bake_handler_version, diff_summary, commit_sha
                 FROM ia_ui_bake_history
                 WHERE panel_slug = $1
                 ORDER BY baked_at DESC
                 LIMIT $2`,
                [panelSlug, limit],
              );

              const historyIds = historyResult.rows.map((r) => r.id);
              const diffsByHistoryId = new Map<number, BakeDiffRow[]>();

              if (historyIds.length > 0) {
                const diffResult = await pool.query<{
                  id: number;
                  history_id: number;
                  change_kind: string;
                  child_kind: string;
                  slug: string;
                  before: unknown;
                  after: unknown;
                }>(
                  `SELECT id, history_id, change_kind, child_kind, slug, before, after
                   FROM ia_bake_diffs
                   WHERE history_id = ANY($1::bigint[])
                   ORDER BY history_id, id`,
                  [historyIds],
                );
                for (const diff of diffResult.rows) {
                  const arr = diffsByHistoryId.get(diff.history_id) ?? [];
                  arr.push({
                    id: diff.id,
                    change_kind: diff.change_kind,
                    child_kind: diff.child_kind,
                    slug: diff.slug,
                    before: diff.before,
                    after: diff.after,
                  });
                  diffsByHistoryId.set(diff.history_id, arr);
                }
              }

              const rows: BakeHistoryRow[] = historyResult.rows.map((row) => ({
                id: row.id,
                panel_slug: row.panel_slug,
                baked_at: row.baked_at instanceof Date
                  ? row.baked_at.toISOString()
                  : String(row.baked_at),
                bake_handler_version: row.bake_handler_version,
                diff_summary: row.diff_summary ?? {},
                commit_sha: row.commit_sha ?? "",
                diffs: diffsByHistoryId.get(row.id) ?? [],
              }));

              return { row_count: rows.length, rows };
            } catch (e) {
              const msg = e instanceof Error ? e.message : String(e);
              const code =
                e && typeof e === "object" && "code" in e
                  ? String((e as { code?: string }).code)
                  : "";
              if (code === "42P01") {
                throw {
                  code: "db_error" as const,
                  message:
                    "ia_ui_bake_history not found. Run `npm run db:migrate` (migration 0151).",
                  hint: "Run `npm run db:migrate`",
                };
              }
              throw { code: "db_error" as const, message: msg };
            }
          },
        )(args as { panel_slug: string; limit?: number });
        return jsonResult(envelope);
      }),
  );
}
