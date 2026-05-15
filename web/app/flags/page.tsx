/**
 * Feature flags dashboard — read-only panel.
 * Reads ia_feature_flags via Postgres pool; shows slug, enabled, default_value, owner, stage_id.
 *
 * TECH-36138 / vibe-coding-safety stage-5-0
 */
import Link from "next/link";
import { getSql } from "@/lib/db/client";

export const dynamic = "force-dynamic";

interface FlagRow {
  slug: string;
  enabled: boolean;
  default_value: boolean;
  owner: string | null;
  stage_id: number | null;
  created_at: string;
}

async function loadFlags(): Promise<FlagRow[]> {
  const sql = getSql();
  const rows = await sql<FlagRow[]>`
    SELECT slug, enabled, default_value, owner, stage_id,
           to_char(created_at, 'YYYY-MM-DD HH24:MI') AS created_at
    FROM ia_feature_flags
    ORDER BY slug
  `;
  return rows;
}

export default async function FlagsPage() {
  let flags: FlagRow[] = [];
  let err: string | null = null;
  try {
    flags = await loadFlags();
  } catch (e) {
    err = e instanceof Error ? e.message : "Failed to load flags";
  }

  return (
    <main className="mx-auto max-w-4xl space-y-6 px-4 py-8 font-mono text-sm">
      <header>
        <div className="mb-2 flex gap-2 text-[11px] uppercase tracking-wide text-neutral-500">
          <Link href="/" className="hover:text-neutral-200">Territory</Link>
          <span>/</span>
          <span className="text-neutral-300">Feature flags</span>
        </div>
        <h1 className="text-xl font-bold text-neutral-100">Feature flags</h1>
        <p className="mt-1 text-neutral-500 text-xs">
          Read-only. Toggle via <code>feature_flags_snapshot_write</code> MCP tool + bridge <code>flag_flip</code>.
        </p>
      </header>

      {err && (
        <div className="rounded border border-red-800 bg-red-950 px-4 py-3 text-red-300">
          {err}
        </div>
      )}

      {!err && flags.length === 0 && (
        <p className="text-neutral-500">No flags in ia_feature_flags. Run the migration + insert seed rows.</p>
      )}

      {flags.length > 0 && (
        <table className="w-full border-collapse text-xs">
          <thead>
            <tr className="border-b border-neutral-700 text-left text-neutral-400">
              <th className="py-2 pr-4">Slug</th>
              <th className="py-2 pr-4">Enabled</th>
              <th className="py-2 pr-4">Default</th>
              <th className="py-2 pr-4">Owner</th>
              <th className="py-2 pr-4">Stage ID</th>
              <th className="py-2">Created</th>
            </tr>
          </thead>
          <tbody>
            {flags.map((f) => (
              <tr key={f.slug} className="border-b border-neutral-800 hover:bg-neutral-900">
                <td className="py-2 pr-4 text-neutral-200 font-semibold">{f.slug}</td>
                <td className="py-2 pr-4">
                  <span className={f.enabled ? "text-green-400" : "text-neutral-500"}>
                    {f.enabled ? "on" : "off"}
                  </span>
                </td>
                <td className="py-2 pr-4 text-neutral-400">{f.default_value ? "on" : "off"}</td>
                <td className="py-2 pr-4 text-neutral-400">{f.owner ?? "—"}</td>
                <td className="py-2 pr-4 text-neutral-400">{f.stage_id ?? "—"}</td>
                <td className="py-2 text-neutral-500">{f.created_at}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </main>
  );
}
