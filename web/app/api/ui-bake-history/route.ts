/**
 * GET /api/ui-bake-history?panel_slug=<slug>&limit=<n>
 *
 * Returns recent bake history rows for a panel joined with diff rows.
 * Fronts ia_ui_bake_history + ia_bake_diffs (Layer 6, TECH-28380).
 *
 * Response: { row_count: number, rows: BakeHistoryRow[] }
 * BakeHistoryRow: { id, panel_slug, baked_at, bake_handler_version,
 *                   diff_summary, commit_sha, diffs: BakeDiffRow[] }
 */

import { NextResponse, type NextRequest } from "next/server";
import { sql } from "@/lib/db/client";
import {
  iaJsonError,
  isDbConfigError,
  postgresErrorResponse,
} from "@/lib/ia/api-errors";

export const dynamic = "force-dynamic";

export type BakeDiffRow = {
  id: number;
  change_kind: string;
  child_kind: string;
  slug: string;
  before: unknown;
  after: unknown;
};

export type BakeHistoryRow = {
  id: number;
  panel_slug: string;
  baked_at: string;
  bake_handler_version: string;
  diff_summary: Record<string, unknown>;
  commit_sha: string;
  diffs: BakeDiffRow[];
};

export type BakeHistoryResponse = {
  row_count: number;
  rows: BakeHistoryRow[];
};

const DEFAULT_LIMIT = 10;
const MAX_LIMIT = 200;

export async function GET(req: NextRequest) {
  const { searchParams } = req.nextUrl;
  const panelSlug = (searchParams.get("panel_slug") ?? "").trim();
  const rawLimit = parseInt(searchParams.get("limit") ?? String(DEFAULT_LIMIT), 10);
  const limit = isNaN(rawLimit) ? DEFAULT_LIMIT : Math.min(Math.max(1, rawLimit), MAX_LIMIT);

  if (!panelSlug) {
    return iaJsonError(400, "bad_request", "Missing panel_slug query param");
  }

  try {
    const historyRows = await sql<
      {
        id: string;
        panel_slug: string;
        baked_at: Date;
        bake_handler_version: string;
        diff_summary: Record<string, unknown>;
        commit_sha: string;
      }[]
    >`
      SELECT id::text, panel_slug, baked_at, bake_handler_version, diff_summary, commit_sha
      FROM ia_ui_bake_history
      WHERE panel_slug = ${panelSlug}
      ORDER BY baked_at DESC
      LIMIT ${limit}
    `;

    const historyIds = historyRows.map((r) => r.id);
    const diffsByHistoryId = new Map<string, BakeDiffRow[]>();

    if (historyIds.length > 0) {
      const diffRows = await sql<
        {
          id: string;
          history_id: string;
          change_kind: string;
          child_kind: string;
          slug: string;
          before: unknown;
          after: unknown;
        }[]
      >`
        SELECT id::text, history_id::text, change_kind, child_kind, slug, before, after
        FROM ia_bake_diffs
        WHERE history_id = ANY(${historyIds}::bigint[])
        ORDER BY history_id, id
      `;
      for (const diff of diffRows) {
        const arr = diffsByHistoryId.get(diff.history_id) ?? [];
        arr.push({
          id: Number(diff.id),
          change_kind: diff.change_kind,
          child_kind: diff.child_kind,
          slug: diff.slug,
          before: diff.before,
          after: diff.after,
        });
        diffsByHistoryId.set(diff.history_id, arr);
      }
    }

    const rows: BakeHistoryRow[] = historyRows.map((row) => ({
      id: Number(row.id),
      panel_slug: row.panel_slug,
      baked_at:
        row.baked_at instanceof Date
          ? row.baked_at.toISOString()
          : String(row.baked_at),
      bake_handler_version: row.bake_handler_version,
      diff_summary: row.diff_summary ?? {},
      commit_sha: row.commit_sha ?? "",
      diffs: diffsByHistoryId.get(row.id) ?? [],
    }));

    const body: BakeHistoryResponse = { row_count: rows.length, rows };
    return NextResponse.json(body, { status: 200 });
  } catch (e) {
    if (isDbConfigError(e)) {
      return iaJsonError(500, "internal", "Database not configured");
    }
    return postgresErrorResponse(e, "ui-bake-history");
  }
}
