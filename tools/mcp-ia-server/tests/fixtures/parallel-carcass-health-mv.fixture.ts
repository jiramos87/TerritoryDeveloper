/**
 * Synthetic fixture for parallel-carcass health mat-view tests.
 *
 * Seeds one sandbox master plan with mixed `carcass_role` topology:
 *   - 2 carcass stages  (carcass.1, carcass.2)
 *   - 2 section stages  (section.A.1, section.A.2; section_id='A')
 *   - 1 stage_carcass_signals row binding (slug, 'carcass.1',
 *     'dev_loop_affordance')
 *
 * Consumed by:
 *   - tests/tools/master-plan-next-actionable.carcass.test.ts (TECH-4623)
 *   - tests/tools/ia-master-plan-health.carcass.test.ts       (TECH-4624)
 *
 * Per-file slug isolation: `seed`/`teardown` accept `slug` arg. Default
 * remains `CARCASS_FIXTURE_SLUG` for backwards compat. Each test file MUST
 * pass a unique slug because `node --test` schedules `*.test.ts` files in
 * parallel workers — shared slugs race teardowns through cascade DELETE +
 * cause `ia_stages_slug_fkey` violations mid-seed.
 *
 * Idempotent: teardown cascades via FK ON DELETE CASCADE on ia_stages +
 * stage_carcass_signals + ia_section_claims (mig 0049 + 0052). Re-runs
 * leave zero rows under the chosen slug.
 *
 * DB-less CI: getIaDatabasePool() === null short-circuits both helpers.
 */

import { getIaDatabasePool } from "../../src/ia-db/pool.js";
import { withFixtureLock } from "./serialize-fixture.js";

export const CARCASS_FIXTURE_SLUG = "__test_carcass__";

export interface CarcassFixtureHandle {
  slug: string;
  carcassStageIds: string[];
  sectionStageIds: string[];
}

export async function seedCarcassHealthFixture(
  slug: string = CARCASS_FIXTURE_SLUG,
): Promise<CarcassFixtureHandle> {
  const handle: CarcassFixtureHandle = {
    slug,
    carcassStageIds: ["carcass.1", "carcass.2"],
    sectionStageIds: ["section.A.1", "section.A.2"],
  };

  const pool = getIaDatabasePool();
  if (pool === null) return handle;

  // Serialize parallel-worker fixture seeds via PG advisory lock — prevents
  // deadlocks + FK races between concurrent cascade DELETEs/INSERTs across
  // overlapping FK-bound tables (`ia_stages`, `stage_carcass_signals`,
  // `ia_section_claims`, `ia_stage_claims`).
  await withFixtureLock(pool, async () => {
    await pool.query(
      `INSERT INTO ia_master_plans (slug, title)
         VALUES ($1, 'sandbox carcass health plan')
       ON CONFLICT (slug) DO NOTHING`,
      [handle.slug],
    );

    // Carcass stages — section_id NULL by D19; carcass_role='carcass'.
    for (const stageId of handle.carcassStageIds) {
      await pool.query(
        `INSERT INTO ia_stages
           (slug, stage_id, title, status, carcass_role, section_id)
         VALUES ($1, $2, $3, 'pending', 'carcass', NULL)
         ON CONFLICT (slug, stage_id) DO UPDATE
           SET carcass_role = EXCLUDED.carcass_role,
               section_id   = EXCLUDED.section_id,
               status       = EXCLUDED.status`,
        [handle.slug, stageId, `carcass stage ${stageId}`],
      );
    }

    // Section stages — section_id='A'; carcass_role='section'.
    for (const stageId of handle.sectionStageIds) {
      await pool.query(
        `INSERT INTO ia_stages
           (slug, stage_id, title, status, carcass_role, section_id)
         VALUES ($1, $2, $3, 'pending', 'section', 'A')
         ON CONFLICT (slug, stage_id) DO UPDATE
           SET carcass_role = EXCLUDED.carcass_role,
               section_id   = EXCLUDED.section_id,
               status       = EXCLUDED.status`,
        [handle.slug, stageId, `section stage ${stageId}`],
      );
    }

    // Bind carcass.1 to dev_loop_affordance signal kind.
    await pool.query(
      `INSERT INTO stage_carcass_signals (slug, stage_id, signal_kind)
         VALUES ($1, 'carcass.1', 'dev_loop_affordance')
       ON CONFLICT (slug, stage_id, signal_kind) DO NOTHING`,
      [handle.slug],
    );
  });

  return handle;
}

export async function teardownCarcassHealthFixture(
  slug: string = CARCASS_FIXTURE_SLUG,
): Promise<void> {
  const pool = getIaDatabasePool();
  if (pool === null) return;

  // Cascade chain: ia_master_plans → ia_stages → stage_carcass_signals
  // + ia_section_claims (FK ON DELETE CASCADE per mig 0049 + 0052).
  // Wrapped in advisory lock — concurrent cascade DELETEs in parallel
  // workers race row/table locks across the FK chain → deadlock.
  await withFixtureLock(pool, async () => {
    await pool.query(
      `DELETE FROM ia_master_plans WHERE slug = $1`,
      [slug],
    );
  });
}
