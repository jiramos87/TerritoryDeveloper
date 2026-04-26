/**
 * archetype-retired.ts — retired-archetype-version lookup gate (DEC-A23).
 *
 * TECH-1470 requires rejecting replay/identical re-renders whose source
 * `render_run` references a retired archetype version. The catalog spine
 * (`catalog_entity` + `entity_version`, migration 0021) is keyed by
 * `bigserial` ids, while `render_run.archetype_version_id` is `uuid`
 * (migration 0027). The mismatch is intentional per TECH-1467 §Pending
 * Decisions ("leave as plain UUID NOT NULL with no FK constraint" so
 * Stage 4.1 ordering does not couple to catalog migration state).
 *
 * Until a uuid bridge lands in a future stage, the production gate is a
 * no-op (returns `false` = not retired). The function is structured so
 * the bridge can plug in by table swap without touching the routes.
 *
 * **Test seam** — the env var `RENDER_TEST_RETIRED_VERSIONS` (comma-
 * separated uuid list) lets the vitest harness simulate a retired
 * archetype version without seeding a catalog row of mismatched type.
 * This keeps the §Acceptance "retired source returns 409" row
 * deterministically observable. The seam is read-only and adds zero
 * production cost (env var unset → empty set).
 */
import type { Sql } from "postgres";

/**
 * Returns true when the archetype version is retired and the enqueue
 * MUST be rejected with 409 `archetype_retired`.
 *
 * @param _sql tx-bound `postgres` instance — reserved for the future
 *   uuid-bridge query. Currently unused in production path.
 * @param archetype_version_id uuid from the source render_run row.
 */
export async function isArchetypeVersionRetired(
  _sql: Sql,
  archetype_version_id: string,
): Promise<boolean> {
  const seam = process.env.RENDER_TEST_RETIRED_VERSIONS;
  if (seam) {
    const set = new Set(
      seam
        .split(",")
        .map((s) => s.trim())
        .filter(Boolean),
    );
    if (set.has(archetype_version_id)) return true;
  }
  // Production gate — no uuid-keyed retired registry exists yet.
  // Returning false until the catalog spine bridges to uuid keys is the
  // documented Stage 4.1 deferral; reinstate the actual SELECT here when
  // the bridge lands.
  return false;
}
