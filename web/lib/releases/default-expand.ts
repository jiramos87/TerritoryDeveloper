import type { PlanData, PlanMetrics } from '../plan-loader-types';

/**
 * Returns the id of the first step where task counts indicate incomplete work,
 * or `null` when all steps are done or when no steps exist.
 *
 * Ground-truth rule: task counts (`metrics.stepCounts`) are the source of
 * truth. Step-header Status prose (e.g. `step.status`) is intentionally
 * ignored — it drifts and may be stale.
 *
 * Missing-entry rule: if `metrics.stepCounts[step.id]` is absent the step is
 * treated as 0/0 and skipped (not a match).
 *
 * Blocked-unreachable note: `'blocked'` is part of the HierarchyStatus union
 * but no step emits it at MVP. The predicate has no special handling for it;
 * a blocked step is simply evaluated by its task counts like any other step.
 */
export function deriveDefaultExpandedStepId(
  plan: PlanData,
  metrics: PlanMetrics,
): string | null {
  for (const s of plan.steps) {
    const c = metrics.stepCounts[s.id];
    if (c && c.done < c.total) return s.id;
  }
  return null;
}
