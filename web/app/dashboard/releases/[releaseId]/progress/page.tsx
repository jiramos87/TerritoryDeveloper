// /dashboard/releases/:releaseId/rollout — reserved; URL 404s by default; no filesystem stub (B1)

import { notFound } from 'next/navigation';
import { resolveRelease } from '@/lib/releases';
import { getReleasePlans } from '@/lib/releases/resolve';
import { deriveDefaultExpandedStepId } from '@/lib/releases/default-expand';
import { buildPlanTree } from '@/lib/plan-tree';
import { loadAllPlans } from '@/lib/plan-loader';
import { computePlanMetrics } from '@/lib/plan-parser';
import { PlanTree } from '@/components/PlanTree';
import { Breadcrumb } from '@/components/Breadcrumb';

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
    <main className="max-w-4xl mx-auto px-4 py-8">
      <Breadcrumb
        crumbs={[
          { href: '/dashboard', label: 'Dashboard' },
          { href: '/dashboard/releases', label: 'Releases' },
          { label: release.label },
          { label: 'Progress' },
        ]}
      />
      <h1 className="text-2xl font-bold mb-8">{release.label} — Progress</h1>
      {plans.map((plan) => {
        const metrics = computePlanMetrics(plan);
        const tree = buildPlanTree(plan, metrics);
        const defaultId = deriveDefaultExpandedStepId(plan, metrics);
        return (
          <section key={plan.filename} className="mb-10">
            <h2 className="text-lg font-semibold mb-4">{plan.title}</h2>
            <PlanTree
              nodes={tree}
              initialExpanded={new Set(defaultId ? [defaultId] : [])}
            />
          </section>
        );
      })}
    </main>
  );
}
