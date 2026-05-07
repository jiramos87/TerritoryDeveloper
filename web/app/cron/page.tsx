/**
 * /cron — operator dashboard for cron queue health.
 *
 * Queries cron_jobs_all view: queue depth per kind × status,
 * plus recent failures (last 24 h). Server component — force-dynamic.
 */
import Link from "next/link";
import { sql } from "@/lib/db/client";

export const dynamic = "force-dynamic";

interface DepthRow {
  kind: string;
  status: string;
  count: string;
}

interface FailureRow {
  kind: string;
  error: string | null;
  enqueued_at: Date;
  finished_at: Date | null;
}

interface KindSummary {
  kind: string;
  queued: number;
  running: number;
  done: number;
  failed: number;
  total: number;
}

async function getDepths(): Promise<KindSummary[]> {
  const rows = await sql<DepthRow[]>`
    SELECT kind, status, COUNT(*)::text AS count
    FROM cron_jobs_all
    GROUP BY kind, status
    ORDER BY kind, status
  `;

  const map = new Map<string, KindSummary>();
  for (const r of rows) {
    if (!map.has(r.kind)) {
      map.set(r.kind, { kind: r.kind, queued: 0, running: 0, done: 0, failed: 0, total: 0 });
    }
    const s = map.get(r.kind)!;
    const n = Number(r.count);
    if (r.status === "queued") s.queued += n;
    else if (r.status === "running") s.running += n;
    else if (r.status === "done") s.done += n;
    else if (r.status === "failed") s.failed += n;
    s.total += n;
  }

  return Array.from(map.values()).sort((a, b) => a.kind.localeCompare(b.kind));
}

async function getRecentFailures(): Promise<FailureRow[]> {
  return sql<FailureRow[]>`
    SELECT kind, error, enqueued_at, finished_at
    FROM cron_jobs_all
    WHERE status = 'failed'
      AND enqueued_at > now() - interval '24 hours'
    ORDER BY enqueued_at DESC
    LIMIT 100
  `;
}

function StatusBadge({ count, label, variant }: { count: number; label: string; variant: "neutral" | "warn" | "ok" | "danger" }) {
  const colors: Record<string, string> = {
    neutral: "bg-neutral-800 text-neutral-300",
    warn: "bg-yellow-900 text-yellow-200",
    ok: "bg-green-900 text-green-200",
    danger: "bg-red-900 text-red-300",
  };
  if (count === 0) return null;
  return (
    <span className={`inline-flex items-center gap-1 rounded px-2 py-0.5 text-xs font-mono ${colors[variant]}`}>
      {count} {label}
    </span>
  );
}

export default async function CronDashboardPage() {
  let depths: KindSummary[] = [];
  let failures: FailureRow[] = [];
  let depthErr: string | null = null;
  let failureErr: string | null = null;

  try {
    depths = await getDepths();
  } catch (e) {
    depthErr = e instanceof Error ? e.message : "Failed to load queue depths";
  }

  try {
    failures = await getRecentFailures();
  } catch (e) {
    failureErr = e instanceof Error ? e.message : "Failed to load recent failures";
  }

  const totalFailed = depths.reduce((a, d) => a + d.failed, 0);
  const totalQueued = depths.reduce((a, d) => a + d.queued, 0);

  return (
    <main className="mx-auto max-w-5xl space-y-8 px-4 py-8 font-mono text-sm">
      <header className="space-y-1">
        <div className="text-[11px] uppercase tracking-wide text-neutral-500">
          <Link href="/" className="hover:text-neutral-200">Territory</Link>
          <span className="mx-2">/</span>
          <span className="text-neutral-300">Cron</span>
        </div>
        <h1 className="text-2xl font-semibold tracking-tight text-neutral-100">Cron Queue Dashboard</h1>
        <p className="text-neutral-400 text-xs">
          {depths.length} queue{depths.length !== 1 ? "s" : ""} monitored
          {totalQueued > 0 && <span className="ml-2 text-yellow-300">{totalQueued} queued</span>}
          {totalFailed > 0 && <span className="ml-2 text-red-400">{totalFailed} failed (all-time)</span>}
        </p>
      </header>

      {/* Queue depth table */}
      <section className="space-y-3">
        <h2 className="text-xs uppercase tracking-widest text-neutral-500">Queue Depths</h2>
        {depthErr ? (
          <div className="rounded border border-red-800 bg-red-950 px-4 py-3 text-red-300 text-xs">{depthErr}</div>
        ) : depths.length === 0 ? (
          <p className="text-neutral-500 text-xs">No jobs in any queue.</p>
        ) : (
          <div className="overflow-x-auto rounded border border-neutral-800">
            <table className="w-full text-xs">
              <thead className="border-b border-neutral-800 bg-neutral-900">
                <tr>
                  <th className="px-4 py-2 text-left text-neutral-400 font-normal">Kind</th>
                  <th className="px-4 py-2 text-right text-neutral-400 font-normal">Queued</th>
                  <th className="px-4 py-2 text-right text-neutral-400 font-normal">Running</th>
                  <th className="px-4 py-2 text-right text-neutral-400 font-normal">Done</th>
                  <th className="px-4 py-2 text-right text-neutral-400 font-normal">Failed</th>
                  <th className="px-4 py-2 text-right text-neutral-400 font-normal">Total</th>
                </tr>
              </thead>
              <tbody>
                {depths.map((d) => (
                  <tr key={d.kind} className="border-b border-neutral-800 last:border-0 hover:bg-neutral-900/50">
                    <td className="px-4 py-2 text-neutral-200">{d.kind}</td>
                    <td className="px-4 py-2 text-right">
                      {d.queued > 0 ? <span className="text-yellow-300">{d.queued}</span> : <span className="text-neutral-600">—</span>}
                    </td>
                    <td className="px-4 py-2 text-right">
                      {d.running > 0 ? <span className="text-blue-300">{d.running}</span> : <span className="text-neutral-600">—</span>}
                    </td>
                    <td className="px-4 py-2 text-right">
                      {d.done > 0 ? <span className="text-green-400">{d.done}</span> : <span className="text-neutral-600">—</span>}
                    </td>
                    <td className="px-4 py-2 text-right">
                      {d.failed > 0 ? <span className="text-red-400 font-semibold">{d.failed}</span> : <span className="text-neutral-600">—</span>}
                    </td>
                    <td className="px-4 py-2 text-right text-neutral-400">{d.total}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {/* Recent failures */}
      <section className="space-y-3">
        <h2 className="text-xs uppercase tracking-widest text-neutral-500">
          Recent Failures <span className="text-neutral-600">(last 24 h)</span>
        </h2>
        {failureErr ? (
          <div className="rounded border border-red-800 bg-red-950 px-4 py-3 text-red-300 text-xs">{failureErr}</div>
        ) : failures.length === 0 ? (
          <p className="text-neutral-500 text-xs">No failures in the last 24 hours.</p>
        ) : (
          <div className="overflow-x-auto rounded border border-neutral-800">
            <table className="w-full text-xs">
              <thead className="border-b border-neutral-800 bg-neutral-900">
                <tr>
                  <th className="px-4 py-2 text-left text-neutral-400 font-normal">Kind</th>
                  <th className="px-4 py-2 text-left text-neutral-400 font-normal">Error</th>
                  <th className="px-4 py-2 text-right text-neutral-400 font-normal">Enqueued</th>
                  <th className="px-4 py-2 text-right text-neutral-400 font-normal">Finished</th>
                </tr>
              </thead>
              <tbody>
                {failures.map((f, i) => (
                  <tr key={i} className="border-b border-neutral-800 last:border-0 hover:bg-neutral-900/50">
                    <td className="px-4 py-2 text-neutral-300">{f.kind}</td>
                    <td className="px-4 py-2 text-red-300 max-w-xs truncate" title={f.error ?? ""}>{f.error ?? "—"}</td>
                    <td className="px-4 py-2 text-right text-neutral-500">
                      {f.enqueued_at instanceof Date ? f.enqueued_at.toISOString().replace("T", " ").slice(0, 19) : String(f.enqueued_at)}
                    </td>
                    <td className="px-4 py-2 text-right text-neutral-500">
                      {f.finished_at instanceof Date ? f.finished_at.toISOString().replace("T", " ").slice(0, 19) : "—"}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </main>
  );
}
