/**
 * RSC /plans/[slug] — plan landing page hosting carcass-status tile.
 *
 * Tile shows: carcass-done flag, section count, link to `/plans/[slug]/sections`.
 * Pulls from `getPlanSectionsBundle()` (server-side direct pg) — same payload
 * the sections dashboard uses, no extra HTTP roundtrip. When carcass not yet
 * decomposed (zero sections), tile renders an empty-state line instead of the
 * carcass indicator.
 *
 * Stage 2.2 / TECH-5245 of `parallel-carcass-rollout`.
 */

import Link from "next/link";
import { notFound } from "next/navigation";
import { headers } from "next/headers";
import { getPlanSectionsBundle } from "@/lib/ia/sections-data";
import { StaleClaimBadge } from "@/components/StaleClaimBadge";

export const dynamic = "force-dynamic";

export default async function PlanLandingPage({
  params,
}: {
  params: Promise<{ slug: string }>;
}) {
  const { slug } = await params;
  await headers();

  const bundle = await getPlanSectionsBundle(slug);
  if (!bundle) notFound();

  const {
    sections,
    carcass_stages,
    carcass_done,
    claim_heartbeat_timeout_minutes,
    red_stage_coverage,
  } = bundle;

  function redStageCoverageColor(coverage: number | null): string {
    if (coverage === null) return "var(--ds-text-muted)";
    if (coverage >= 100) return "var(--ds-bg-status-done)";
    if (coverage >= 50) return "var(--ds-bg-status-progress)";
    return "var(--ds-bg-status-blocked)";
  }

  const coverageDisplay =
    red_stage_coverage !== null ? `${Math.round(red_stage_coverage)}%` : "—";
  const coverageAriaLabel =
    red_stage_coverage !== null
      ? `Red-stage coverage ${Math.round(red_stage_coverage)} percent`
      : "Red-stage coverage unknown";

  const heldClaims = sections.filter((s) => s.claim).length;
  const freshestHeartbeat = sections
    .map((s) => (s.claim ? new Date(s.claim.last_heartbeat) : null))
    .filter((d): d is Date => d !== null)
    .sort((a, b) => b.getTime() - a.getTime())[0] ?? null;

  return (
    <main className="mx-auto max-w-6xl space-y-6 px-4 py-8">
      {/* Breadcrumb */}
      <div className="mb-2 flex flex-wrap gap-2 font-mono text-[11px] uppercase tracking-wide text-[var(--ds-text-meta)]">
        <Link href="/" className="text-[var(--ds-text-meta)]">
          Territory
        </Link>
        <span className="opacity-50">{"//"}</span>
        <span className="text-[var(--ds-raw-blue)]">{slug}</span>
      </div>

      <header className="space-y-2">
        <h1 className="text-[26px] font-semibold text-[var(--ds-text-primary)]">
          {slug}
        </h1>
        <p className="text-sm text-[var(--ds-text-muted)]">
          Plan landing surface. Carcass progress and section claims at a
          glance; deep dive available under <code>/plans/{slug}/sections</code>.
        </p>
      </header>

      {/* Carcass tile */}
      <section
        data-testid="carcass-tile"
        className="space-y-3 rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] p-5"
      >
        <div className="flex flex-wrap items-baseline justify-between gap-2">
          <h2 className="font-mono text-sm uppercase tracking-[0.2em] text-[var(--ds-text-meta)]">
            Carcass
          </h2>
          <span
            data-testid="carcass-done-flag"
            data-carcass-done={carcass_done ? "true" : "false"}
            className={
              carcass_done
                ? "font-mono text-xs text-[var(--ds-raw-blue)]"
                : "font-mono text-xs text-[var(--ds-text-muted)]"
            }
          >
            {carcass_done ? "complete" : "in flight"}
          </span>
        </div>

        <div className="grid gap-4 sm:grid-cols-4">
          <div>
            <div className="font-mono text-[10px] uppercase tracking-[0.2em] text-[var(--ds-text-meta)]">
              Carcass stages
            </div>
            <div
              data-testid="carcass-stage-count"
              className="text-2xl font-semibold text-[var(--ds-text-primary)]"
            >
              {carcass_stages.length}
            </div>
          </div>

          <div>
            <div className="font-mono text-[10px] uppercase tracking-[0.2em] text-[var(--ds-text-meta)]">
              Sections
            </div>
            <div
              data-testid="section-count"
              className="text-2xl font-semibold text-[var(--ds-text-primary)]"
            >
              {sections.length}
            </div>
          </div>

          <div>
            <div className="font-mono text-[10px] uppercase tracking-[0.2em] text-[var(--ds-text-meta)]">
              Red-stage coverage
            </div>
            <div
              data-testid="red-stage-coverage-badge"
              aria-label={coverageAriaLabel}
              className="text-2xl font-semibold"
              style={{ color: redStageCoverageColor(red_stage_coverage) }}
            >
              {coverageDisplay}
            </div>
          </div>

          <div>
            <div className="font-mono text-[10px] uppercase tracking-[0.2em] text-[var(--ds-text-meta)]">
              Active claims
            </div>
            <div className="flex items-center gap-2">
              <span
                data-testid="held-claim-count"
                className="text-2xl font-semibold text-[var(--ds-text-primary)]"
              >
                {heldClaims}
              </span>
              <StaleClaimBadge
                lastHeartbeat={freshestHeartbeat}
                timeoutMinutes={claim_heartbeat_timeout_minutes}
              />
            </div>
          </div>
        </div>

        {sections.length === 0 ? (
          <p className="text-xs text-[var(--ds-text-muted)]">
            No sections decomposed yet. Run section decomposition before
            spinning up parallel agents.
          </p>
        ) : (
          <Link
            data-testid="sections-link"
            href={`/plans/${slug}/sections`}
            className="inline-block font-mono text-xs uppercase tracking-wide text-[var(--ds-raw-blue)] underline"
          >
            View sections →
          </Link>
        )}
      </section>
    </main>
  );
}
