import type { PlanChartDatum } from '@/components/PlanChart';
import type { PlanData } from '@/lib/plan-loader-types';
import { toPlanSlug } from '@/lib/dashboard/plan-slug';

/**
 * Aggregate release-scope chart data: one entry per master plan.
 *
 * Bar stacks count stages by status (done / in-progress / pending) within each plan.
 * Ring slice = one master plan, colored by overall plan status — equal weight per plan
 * regardless of stage count, matching equal-slice semantics of `PlanRingChart`.
 *
 * Plans with zero stages render as `skeleton` (dimmed pending bar/slice).
 */
export function buildReleaseChartData(plans: PlanData[]): PlanChartDatum[] {
  return plans.map((plan) => {
    let done = 0;
    let inProgress = 0;
    let pending = 0;
    for (const stage of plan.stages) {
      const s = stage.status;
      if (s === 'Final') done++;
      else if (s === 'In Progress' || s === 'In Review') inProgress++;
      else pending++;
    }
    const skeleton = plan.stages.length === 0;
    return {
      label: plan.title,
      done,
      inProgress,
      pending,
      slug: toPlanSlug(plan.title),
      status: plan.overallStatus,
      ...(skeleton ? { skeleton: true } : {}),
    };
  });
}
