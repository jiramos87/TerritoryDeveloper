/**
 * Release registry — hand-maintained, pure data layer.
 *
 * Canonical source of truth for `children[]`:
 *   docs/full-game-mvp-rollout-tracker.md
 *
 * DRIFT WARNING: `children[]` is manually synced with the rollout tracker.
 * When rows advance in the rollout tracker, update this file to match.
 * The registry is intentionally static (MVP, single release) — automated
 * discovery was rejected as premature (locked decision 2026-04-17).
 *
 * Consumers: `getReleasePlans` (`web/lib/releases/resolve.ts`), Stage 7.2 RSC pages.
 * Non-goals: multi-release support, automated sync, filter shaping.
 */

export interface Release {
  /** URL slug, e.g. 'full-game-mvp' */
  id: string;
  /** Human-facing label, e.g. 'Full-Game MVP' */
  label: string;
  /** Basename of the umbrella orchestrator, e.g. 'full-game-mvp-master-plan.md' */
  umbrellaMasterPlan: string;
  /** Basenames of child master plans; MAY include umbrella for self-inclusion in tree */
  children: string[];
}

/**
 * All known releases. Single entry for MVP — YAGNI until second release tracker exists.
 *
 * Sync checklist (manual):
 *   1. Open docs/full-game-mvp-rollout-tracker.md
 *   2. Verify `children` basenames match authored rows in the tracker
 *   3. utilities / landmarks / distribution intentionally absent (tracker rows 8, 9, 11 — authored later)
 */
export const releases: Release[] = [
  {
    id: 'full-game-mvp',
    label: 'Full-Game MVP',
    umbrellaMasterPlan: 'full-game-mvp-master-plan.md',
    children: [
      'multi-scale-master-plan.md',
      'city-sim-depth-master-plan.md',
      'zone-s-economy-master-plan.md',
      'sprite-gen-master-plan.md',
      'ui-polish-master-plan.md',
      'blip-master-plan.md',
      'music-player-master-plan.md',
      'citystats-overhaul-master-plan.md',
      'web-platform-master-plan.md',
      // utilities / landmarks / distribution absent — authored later per rollout tracker rows 8, 9, 11
    ],
  },
];

/**
 * Look up a release by id.
 *
 * @param id - URL slug to look up (e.g. 'full-game-mvp')
 * @returns Matching `Release`, or `null` if not found.
 *          Caller maps `null` to `notFound()` — this function does not throw.
 */
export function resolveRelease(id: string): Release | null {
  return releases.find((r) => r.id === id) ?? null;
}
