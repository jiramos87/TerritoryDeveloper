// Stage 27 T27.4 — CD ScreenReleases port
import Link from 'next/link';
import { Breadcrumb } from '@/components/Breadcrumb';
import { releases, resolveRelease } from '@/lib/releases';
import { Button } from '@/components/Button';
import { Rack } from '@/components/console';
import { Heading } from '@/components/type/Heading';
import { BadgeChip } from '@/components/BadgeChip';
import { StatBar } from '@/components/StatBar';
import { toBadgeStatus } from '@/app/dashboard/_status';

/**
 * Releases page — RSC. Registry read via `releases` / `resolveRelease` (Stage 7.2).
 * Schema: each row = one `Release`; child route `/dashboard/releases/[releaseId]/progress` unchanged.
 */
export default async function ReleasesPage({
  searchParams,
}: {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}) {
  const sp = await searchParams;
  const qRaw = sp.q;
  const q = (typeof qRaw === 'string' ? qRaw : Array.isArray(qRaw) ? qRaw[0] : '').trim();
  const lower = q.toLowerCase();

  const rows = releases.filter(
    (r) =>
      !q ||
      r.id.toLowerCase().includes(lower) ||
      r.label.toLowerCase().includes(lower),
  );

  return (
    <main className="mx-auto max-w-5xl space-y-6 px-4 py-8">
      <Breadcrumb
        crumbs={[
          { label: 'Home', href: '/' },
          { label: 'Dashboard', href: '/dashboard' },
          { label: 'Releases' },
        ]}
      />

      <div className="mb-2 flex flex-wrap items-start justify-between gap-4 border-b border-[var(--ds-border-subtle)] pb-4">
        <div>
          <div className="mb-1 font-mono text-[10px] uppercase tracking-[0.2em] text-[var(--ds-raw-amber)]">
            {'// Releases'}
          </div>
          <Heading level="h1" className="text-[26px] font-semibold">
            Release ledger
          </Heading>
          <p className="mt-1 font-mono text-sm text-[var(--ds-text-meta)]">
            Signed tags, drafts, and hotfix candidates.
          </p>
        </div>
        <div>
          <Button variant="primary" size="md" className="pointer-events-none opacity-60" disabled>
            New release
          </Button>
        </div>
      </div>

      <form method="get" className="mb-3 flex flex-wrap items-center gap-2">
        <input
          name="q"
          defaultValue={q}
          className="min-w-[200px] flex-1 rounded border border-black bg-[#050505] px-2.5 py-2 font-mono text-sm text-[var(--ds-text-primary)] shadow-[inset_0_2px_3px_rgba(0,0,0,0.8)] placeholder:text-[var(--ds-text-meta)]"
          placeholder="search: tag / title"
          aria-label="Filter releases"
        />
        <Button type="submit" variant="secondary" size="md">
          Search
        </Button>
        {q ? (
          <Button type="button" variant="ghost" size="md" href="/dashboard/releases">
            Clear
          </Button>
        ) : null}
      </form>

      <Rack>
        {rows.length === 0 ? (
          <div className="flex flex-col items-center gap-3 px-6 py-10 text-center">
            <p className="font-mono text-sm uppercase tracking-wide text-[var(--ds-text-primary)]">
              No releases match
            </p>
            <p className="max-w-md text-sm text-[var(--ds-text-meta)]">
              Try clearing the search or widening the query.
            </p>
            {q ? (
              <Button variant="secondary" size="md" href="/dashboard/releases">
                Clear search
              </Button>
            ) : null}
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[640px] border-collapse text-left text-sm">
              <thead>
                <tr className="border-b border-[var(--ds-border-subtle)] font-mono text-[10px] uppercase tracking-wider text-[var(--ds-text-meta)]">
                  <th className="px-3 py-2">Tag</th>
                  <th className="px-3 py-2">Title</th>
                  <th className="px-3 py-2">Status</th>
                  <th className="hidden min-w-[160px] px-3 py-2 md:table-cell">Progress</th>
                  <th className="hidden px-3 py-2 md:table-cell">Owner</th>
                  <th className="px-3 py-2">Date</th>
                </tr>
              </thead>
              <tbody>
                {rows.map((r) => {
                  const resolved = resolveRelease(r.id);
                  const hasChild = Boolean(resolved);
                  return (
                    <tr
                      key={r.id}
                      className="border-b border-[rgba(255,255,255,0.04)] transition-colors hover:bg-[rgba(74,123,200,0.05)]"
                    >
                      <td className="px-3 py-3 font-mono">
                        <span className="text-[var(--ds-raw-amber)]">{r.id}</span>
                      </td>
                      <td className="px-3 py-3 text-[var(--ds-text-primary)]">
                        {hasChild ? (
                          <Link
                            className="font-medium text-[var(--ds-text-primary)] underline-offset-2 hover:underline"
                            href={`/dashboard/releases/${r.id}/progress`}
                          >
                            {r.label}
                          </Link>
                        ) : (
                          r.label
                        )}
                      </td>
                      <td className="px-3 py-3">
                        <BadgeChip status={toBadgeStatus('In Progress — active')} />
                      </td>
                      <td className="hidden min-w-[160px] px-3 py-3 md:table-cell">
                        <StatBar
                          label="Plans"
                          value={Math.min(8, r.children.length)}
                          max={Math.max(9, r.children.length)}
                        />
                      </td>
                      <td className="hidden px-3 py-3 font-mono text-xs text-[var(--ds-text-meta)] md:table-cell">
                        team
                      </td>
                      <td className="px-3 py-3 font-mono text-[11px] text-[var(--ds-text-meta)]">
                        2026-04-18
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </Rack>
    </main>
  );
}
