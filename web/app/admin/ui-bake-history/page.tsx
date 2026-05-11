/**
 * /admin/ui-bake-history — Layer 6 bake audit dashboard (TECH-28380).
 *
 * Server component. Reads bake history direct from DB.
 * Shows last N bakes per panel + per-bake diff rows.
 *
 * Usage: /admin/ui-bake-history?panel_slug=<slug>&limit=<n>
 */

import Link from "next/link";
import { sql } from "@/lib/db/client";
import type { BakeHistoryRow } from "@/app/api/ui-bake-history/route";

export const dynamic = "force-dynamic";

type PageProps = {
  searchParams: Promise<{ panel_slug?: string; limit?: string }>;
};

async function loadPanelSlugs(): Promise<string[]> {
  try {
    const rows = await sql<{ panel_slug: string }[]>`
      SELECT DISTINCT panel_slug
      FROM ia_ui_bake_history
      ORDER BY panel_slug
    `;
    return rows.map((r) => r.panel_slug);
  } catch {
    return [];
  }
}

async function loadHistory(
  panelSlug: string,
  limit: number,
): Promise<BakeHistoryRow[]> {
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
    const diffsByHistoryId = new Map<string, { id: number; change_kind: string; child_kind: string; slug: string }[]>();

    if (historyIds.length > 0) {
      const diffRows = await sql<
        {
          id: string;
          history_id: string;
          change_kind: string;
          child_kind: string;
          slug: string;
        }[]
      >`
        SELECT id::text, history_id::text, change_kind, child_kind, slug
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
        });
        diffsByHistoryId.set(diff.history_id, arr);
      }
    }

    return historyRows.map((row) => ({
      id: Number(row.id),
      panel_slug: row.panel_slug,
      baked_at:
        row.baked_at instanceof Date
          ? row.baked_at.toISOString()
          : String(row.baked_at),
      bake_handler_version: row.bake_handler_version,
      diff_summary: row.diff_summary ?? {},
      commit_sha: row.commit_sha ?? "",
      diffs: (diffsByHistoryId.get(row.id) ?? []).map((d) => ({
        ...d,
        before: null,
        after: null,
      })),
    }));
  } catch {
    return [];
  }
}

export default async function UiBakeHistoryPage({ searchParams }: PageProps) {
  const params = await searchParams;
  const panelSlug = (params.panel_slug ?? "").trim();
  const limit = Math.min(
    Math.max(1, parseInt(params.limit ?? "20", 10) || 20),
    200,
  );

  const [panelSlugs, history] = await Promise.all([
    loadPanelSlugs(),
    panelSlug ? loadHistory(panelSlug, limit) : Promise.resolve([] as BakeHistoryRow[]),
  ]);

  return (
    <main className="mx-auto max-w-5xl space-y-8 px-4 py-8 font-mono text-sm">
      <header>
        <div className="mb-2 flex flex-wrap gap-2 text-[11px] uppercase tracking-wide text-neutral-500">
          <Link href="/" className="hover:text-neutral-200">Territory</Link>
          <span>/</span>
          <Link href="/ia" className="hover:text-neutral-200">IA</Link>
          <span>/</span>
          <span className="text-neutral-300">UI Bake History</span>
        </div>
        <h1 className="text-xl font-bold text-neutral-100">UI Bake History</h1>
        <p className="mt-1 text-neutral-400">
          Layer 6 auditability — every bake run per panel with diff summary.
        </p>
      </header>

      {/* Panel picker */}
      <section className="space-y-2">
        <h2 className="text-xs font-semibold uppercase tracking-wide text-neutral-500">
          Panels ({panelSlugs.length})
        </h2>
        {panelSlugs.length === 0 ? (
          <p className="text-neutral-500">
            No bake history yet. Run a panel bake to populate.
          </p>
        ) : (
          <div className="flex flex-wrap gap-2">
            {panelSlugs.map((slug) => (
              <Link
                key={slug}
                href={`/admin/ui-bake-history?panel_slug=${encodeURIComponent(slug)}&limit=${limit}`}
                className={`rounded border px-3 py-1 text-xs transition-colors ${
                  slug === panelSlug
                    ? "border-sky-500 bg-sky-900/30 text-sky-300"
                    : "border-neutral-700 text-neutral-400 hover:border-neutral-500 hover:text-neutral-200"
                }`}
              >
                {slug}
              </Link>
            ))}
          </div>
        )}
      </section>

      {/* History table */}
      {panelSlug && (
        <section className="space-y-4">
          <h2 className="text-xs font-semibold uppercase tracking-wide text-neutral-500">
            {panelSlug} — last {limit} bakes
          </h2>
          {history.length === 0 ? (
            <p className="text-neutral-500">No history rows for this panel.</p>
          ) : (
            <div className="overflow-x-auto rounded border border-neutral-800">
              <table className="w-full text-left text-xs">
                <thead className="bg-neutral-900 text-neutral-500">
                  <tr>
                    <th className="px-3 py-2">ID</th>
                    <th className="px-3 py-2">Baked At</th>
                    <th className="px-3 py-2">Handler Version</th>
                    <th className="px-3 py-2">Commit</th>
                    <th className="px-3 py-2">Diffs</th>
                    <th className="px-3 py-2">Diff Summary</th>
                  </tr>
                </thead>
                <tbody>
                  {history.map((row, idx) => (
                    <tr
                      key={row.id}
                      className={
                        idx % 2 === 0
                          ? "bg-neutral-950"
                          : "bg-neutral-900/50"
                      }
                    >
                      <td className="px-3 py-2 text-neutral-500">{row.id}</td>
                      <td className="px-3 py-2 text-neutral-300">
                        {new Date(row.baked_at).toLocaleString()}
                      </td>
                      <td className="px-3 py-2 text-sky-400">
                        {row.bake_handler_version}
                      </td>
                      <td className="px-3 py-2 font-mono text-neutral-400">
                        {row.commit_sha || "—"}
                      </td>
                      <td className="px-3 py-2 text-neutral-300">
                        {row.diffs.length > 0 ? (
                          <span className="text-amber-400">
                            {row.diffs.length} change{row.diffs.length !== 1 ? "s" : ""}
                          </span>
                        ) : (
                          <span className="text-neutral-600">—</span>
                        )}
                      </td>
                      <td className="px-3 py-2">
                        <code className="rounded bg-neutral-800 px-1 py-0.5 text-[10px] text-neutral-400">
                          {JSON.stringify(row.diff_summary)}
                        </code>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {/* Drift trend sparkline — per-bake diff count over time */}
          {history.length > 1 && (
            <section className="space-y-1">
              <h3 className="text-xs text-neutral-500">Diff count over time (oldest → newest)</h3>
              <div className="flex items-end gap-1 rounded border border-neutral-800 bg-neutral-950 px-3 py-3" style={{ minHeight: "48px" }}>
                {[...history].reverse().map((row) => {
                  const count = row.diffs.length;
                  const maxCount = Math.max(...history.map((r) => r.diffs.length), 1);
                  const pct = Math.round((count / maxCount) * 100);
                  return (
                    <div
                      key={row.id}
                      className="flex flex-col items-center justify-end"
                      style={{ height: "32px" }}
                      title={`${new Date(row.baked_at).toLocaleDateString()} — ${count} diffs`}
                    >
                      <div
                        style={{ height: `${Math.max(2, pct)}%` }}
                        className="w-2 rounded-sm bg-sky-600 transition-all"
                      />
                    </div>
                  );
                })}
              </div>
            </section>
          )}
        </section>
      )}
    </main>
  );
}
