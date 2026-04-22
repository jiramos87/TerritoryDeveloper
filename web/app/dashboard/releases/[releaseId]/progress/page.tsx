// Stage 27 T27.5 — CD ScreenDetail port
// /dashboard/releases/:releaseId/rollout — reserved; URL 404s by default; no filesystem stub (B1)

import Link from 'next/link';
import { notFound } from 'next/navigation';
import { resolveRelease } from '@/lib/releases';
import { getReleasePlans } from '@/lib/releases/resolve';
import { deriveDefaultExpandedStageId } from '@/lib/releases/default-expand';
import { buildPlanTree } from '@/lib/plan-tree';
import { loadAllPlans } from '@/lib/plan-loader';
import { computePlanMetrics } from '@/lib/plan-parser';
import { PlanTree } from '@/components/PlanTree';
import { Rack } from '@/components/console';
import { Heading } from '@/components/type/Heading';

export default async function ProgressPage({
  params,
}: {
  params: Promise<{ releaseId: string }>;
}) {
  const { releaseId } = await params;
  const release = resolveRelease(releaseId);
  if (!release) notFound();

  const allPlans = await loadAllPlans();
  const plans = getReleasePlans(release, allPlans);

  return (
    <main className="mx-auto max-w-4xl space-y-8 px-4 py-8">
      <div className="mb-2 flex flex-wrap gap-2 font-mono text-[11px] uppercase tracking-wide text-[var(--ds-text-meta)]">
        <Link href="/" className="text-[var(--ds-text-meta)]">
          Territory
        </Link>
        <span className="opacity-50">{'//'}</span>
        <Link href="/dashboard" className="text-[var(--ds-text-meta)]">
          Dashboard
        </Link>
        <span className="opacity-50">{'//'}</span>
        <Link href="/dashboard/releases" className="text-[var(--ds-text-meta)]">
          Releases
        </Link>
        <span className="opacity-50">{'//'}</span>
        <span className="text-[var(--ds-raw-blue)]">{releaseId}</span>
      </div>

      <div className="mb-4 flex flex-wrap items-start justify-between gap-4">
        <div>
          <div className="mb-1 font-mono text-[10px] uppercase tracking-[0.2em] text-[var(--ds-raw-amber)]">
            {'// Release detail'}
          </div>
          <Heading level="h1" className="text-[26px] font-semibold">
            {release.id} — {release.label}
          </Heading>
          <p className="mt-1 font-mono text-sm text-[var(--ds-text-meta)]">
            Orchestrator children: {release.children.length} plans. Stage 7.2 loader pipeline.
          </p>
        </div>
      </div>

      {plans.map((plan) => {
        const metrics = computePlanMetrics(plan);
        const tree = buildPlanTree(plan);
        const defaultId = deriveDefaultExpandedStageId(plan, metrics);
        return (
          <Rack key={plan.filename} className="space-y-4" label={plan.title}>
            <Heading level="h2" className="text-lg font-semibold">
              {plan.title}
            </Heading>
            <PlanTree
              nodes={tree}
              initialExpanded={new Set(defaultId ? [defaultId] : [])}
            />
          </Rack>
        );
      })}
    </main>
  );
}
