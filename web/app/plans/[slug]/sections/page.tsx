/**
 * RSC /plans/[slug]/sections — read-only sections-cluster dashboard.
 *
 * Renders one card per `section_id` for a master plan: member stages with
 * status, owned `arch_surfaces.slug` chips, claim pill (held / free / stale)
 * derived from `last_heartbeat` + `claim_heartbeat_timeout_minutes`, drift
 * warnings list. Surfaces contention before claiming so ops + agents see
 * sections in flight.
 *
 * Stage 2.2 / TECH-5243 + TECH-5245 of `parallel-carcass-rollout`. Badge
 * presentation lives in `<StaleClaimBadge>` (T2.2.2).
 */

import Link from "next/link";
import { notFound } from "next/navigation";
import { headers } from "next/headers";
import {
  getPlanSectionsBundle,
  type PlanSectionsBundle,
  type SectionView,
} from "@/lib/ia/sections-data";
import { BadgeChip, type Status } from "@/components/BadgeChip";
import { StaleClaimBadge } from "@/components/StaleClaimBadge";

export const dynamic = "force-dynamic";

function dbStageStatusToBadge(s: string): Status {
  if (s === "done") return "done";
  if (s === "in_progress") return "in-progress";
  return "pending";
}

async function fetchBundleViaBff(
  slug: string,
): Promise<PlanSectionsBundle | null> {
  // Prefer direct query — same DB, skips HTTP roundtrip. BFF route still
  // exists for external clients (E2E test, future agents).
  return getPlanSectionsBundle(slug);
}

export default async function SectionsPage({
  params,
}: {
  params: Promise<{ slug: string }>;
}) {
  const { slug } = await params;
  // touch headers() so the page is treated as dynamic at request time even
  // when an upstream cache layer would otherwise opt-in to static export.
  await headers();

  const bundle = await fetchBundleViaBff(slug);
  if (!bundle) notFound();

  const { sections, carcass_stages, warnings, claim_heartbeat_timeout_minutes } =
    bundle;

  return (
    <main className="mx-auto max-w-6xl space-y-6 px-4 py-8">
      {/* Breadcrumb */}
      <div className="mb-2 flex flex-wrap gap-2 font-mono text-[11px] uppercase tracking-wide text-[var(--ds-text-meta)]">
        <Link href="/" className="text-[var(--ds-text-meta)]">
          Territory
        </Link>
        <span className="opacity-50">{"//"}</span>
        <Link
          href={`/plans/${slug}`}
          className="text-[var(--ds-text-meta)]"
        >
          {slug}
        </Link>
        <span className="opacity-50">{"//"}</span>
        <span className="text-[var(--ds-raw-blue)]">sections</span>
      </div>

      <header className="space-y-2">
        <h1 className="text-[26px] font-semibold text-[var(--ds-text-primary)]">
          Sections
        </h1>
        <p className="text-sm text-[var(--ds-text-muted)]">
          Section-cluster contention map for <code>{slug}</code>. Each card is
          one parallel-carcass section; the pill shows whether an agent
          currently holds the claim.
        </p>
        {warnings.length > 0 && (
          <ul className="rounded border border-[var(--ds-raw-amber)] bg-[var(--ds-bg-panel)] p-3 text-sm text-[var(--ds-raw-amber)]">
            {warnings.map((w) => (
              <li key={w} className="font-mono text-xs">
                {w}
              </li>
            ))}
          </ul>
        )}
        <div className="flex flex-wrap items-center gap-2 text-xs text-[var(--ds-text-meta)]">
          <span className="font-mono uppercase tracking-wide">
            Carcass stages
          </span>
          {carcass_stages.length === 0 ? (
            <span>—</span>
          ) : (
            carcass_stages.map((c) => (
              <span
                key={c.stage_id}
                className="inline-flex items-center gap-1 rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] px-2 py-0.5 font-mono"
              >
                <span className="text-[var(--ds-text-muted)]">{c.stage_id}</span>
                <BadgeChip status={dbStageStatusToBadge(c.status)} />
              </span>
            ))
          )}
        </div>
      </header>

      {sections.length === 0 ? (
        <div className="rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] p-6 text-sm text-[var(--ds-text-muted)]">
          No sections defined for this plan yet.
        </div>
      ) : (
        <ul
          className="grid gap-4"
          style={{
            gridTemplateColumns:
              "repeat(auto-fit, minmax(min(20rem, 100%), 1fr))",
          }}
        >
          {sections.map((s) => (
            <SectionCard
              key={s.section_id}
              section={s}
              timeoutMinutes={claim_heartbeat_timeout_minutes}
            />
          ))}
        </ul>
      )}
    </main>
  );
}

// ---------------------------------------------------------------------------
// SectionCard
// ---------------------------------------------------------------------------

function SectionCard({
  section,
  timeoutMinutes,
}: {
  section: SectionView;
  timeoutMinutes: number;
}) {
  const lastHeartbeat = section.claim
    ? new Date(section.claim.last_heartbeat)
    : null;

  return (
    <li
      data-testid="section-card"
      data-section-id={section.section_id}
      className="space-y-3 rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] p-4"
    >
      <div className="flex flex-wrap items-center gap-2">
        <h2 className="font-mono text-[15px] font-semibold text-[var(--ds-text-primary)]">
          Section {section.section_id}
        </h2>
        <StaleClaimBadge
          lastHeartbeat={lastHeartbeat}
          timeoutMinutes={timeoutMinutes}
        />
      </div>

      <div>
        <div className="mb-1 font-mono text-[10px] uppercase tracking-[0.2em] text-[var(--ds-text-meta)]">
          Member stages ({section.stages.length})
        </div>
        <ul className="space-y-1">
          {section.stages.map((st) => (
            <li
              key={st.stage_id}
              className="flex flex-wrap items-center gap-2 rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-canvas)] px-2 py-1 font-mono text-xs"
            >
              <span className="text-[var(--ds-text-primary)]">
                Stage {st.stage_id}
              </span>
              <BadgeChip status={dbStageStatusToBadge(st.status)} />
              {st.carcass_role && (
                <span className="text-[var(--ds-text-meta)]">
                  {st.carcass_role}
                </span>
              )}
            </li>
          ))}
        </ul>
      </div>

      <div>
        <div className="mb-1 font-mono text-[10px] uppercase tracking-[0.2em] text-[var(--ds-text-meta)]">
          Owned arch surfaces ({section.owned_surfaces.length})
        </div>
        {section.owned_surfaces.length === 0 ? (
          <span className="text-xs text-[var(--ds-text-muted)]">
            No surfaces linked.
          </span>
        ) : (
          <div className="flex flex-wrap gap-2">
            {section.owned_surfaces.map((surf) => (
              <span
                key={surf}
                className="inline-flex items-center rounded-full border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-canvas)] px-2 py-0.5 font-mono text-xs text-[var(--ds-raw-blue)]"
              >
                {surf}
              </span>
            ))}
          </div>
        )}
      </div>

      {section.warnings.length > 0 && (
        <div>
          <div className="mb-1 font-mono text-[10px] uppercase tracking-[0.2em] text-[var(--ds-raw-amber)]">
            Drift warnings ({section.warnings.length})
          </div>
          <ul className="space-y-1">
            {section.warnings.map((w) => (
              <li
                key={w}
                className="rounded border border-[var(--ds-raw-amber)] bg-[var(--ds-bg-canvas)] px-2 py-1 font-mono text-xs text-[var(--ds-raw-amber)]"
              >
                {w}
              </li>
            ))}
          </ul>
        </div>
      )}
    </li>
  );
}
