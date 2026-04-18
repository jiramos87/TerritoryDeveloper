import type { Release } from '../releases';
import type { PlanData } from '../plan-loader-types';

/**
 * Filter `allPlans` to only those listed in `release.children`.
 *
 * - Order is preserved by `release.children` (registry is canonical display order).
 * - Children not found in `allPlans` are silently dropped (no throw, no warning).
 * - Both sides normalised to include the `.md` suffix before comparing.
 *
 * Pure function — no I/O, no side effects.
 */
export function getReleasePlans(release: Release, allPlans: PlanData[]): PlanData[] {
  const normalize = (name: string): string =>
    name.endsWith('.md') ? name : `${name}.md`;

  const plansByBasename = new Map<string, PlanData>(
    allPlans.map((p) => [normalize(p.filename), p]),
  );

  return release.children
    .map((child) => plansByBasename.get(normalize(child)))
    .filter((p): p is PlanData => p !== undefined);
}
