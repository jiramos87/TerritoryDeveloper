import type { PlanData, PlanMetrics } from '../plan-loader-types';

/**
 * Returns the id of the first stage where task counts indicate incomplete work,
 * or `null` when all stages are done or when no stages exist.
 *
 * Ground-truth rule: task counts (`metrics.stageCounts`) are the source of
 * truth. Stage-header Status prose (e.g. `stage.status`) is intentionally
 * ignored — it drifts and may be stale.
 *
 * Missing-entry rule: if `metrics.stageCounts[stage.id]` is absent the stage is
 * treated as 0/0 and skipped (not a match).
 */
export function deriveDefaultExpandedStageId(
  plan: PlanData,
  metrics: PlanMetrics,
): string | null {
  for (const s of plan.stages) {
    const c = metrics.stageCounts[s.id];
    if (c && c.done < c.total) return s.id;
  }
  return null;
}
