/**
 * Synthetic fixture for section-closeout E2E + apply tests.
 *
 * Seeds one sandbox master plan with topology required by both
 * TECH-5070 (e2e drive-stages-to-done) and TECH-5071 (apply happy +
 * negative paths):
 *
 *   - 1 carcass stage `carcass.1` (status='done', carcass_role='carcass',
 *     section_id=null) — represents the carcass row that has already
 *     completed before the section opens.
 *   - 2 section stages `section.A.1`, `section.A.2` (status='pending',
 *     carcass_role='section', section_id='A') — drive these to done in
 *     test bodies.
 *   - 1 task per section stage (1 TECH-prefix task each) seeded
 *     `status='pending'` so callers may walk pending → implemented →
 *     verified → done via `mutateTaskStatusFlip` (TECH-5070 path).
 *     TECH-5071 ignores tasks and flips `ia_stages.status` directly.
 *
 * Per-file slug isolation: `seed`/`teardown` accept `slug` arg. Callers
 * pass disjoint slugs (`__test_section_closeout_e2e__` vs
 * `__test_section_closeout_apply__`) because `node --test` schedules
 * `*.test.ts` files in parallel workers — shared slugs race teardowns
 * through cascade DELETE + cause `ia_stages_slug_fkey` violations.
 *
 * Idempotent: teardown cascades via FK ON DELETE CASCADE on
 * `ia_master_plans` → `ia_stages` → `ia_tasks` + `ia_section_claims` +
 * `ia_stage_claims` (mig 0049 + 0052). Re-runs leave zero rows under
 * the chosen slug.
 *
 * DB-less CI: getIaDatabasePool() === null short-circuits both helpers.
 */

import { getIaDatabasePool } from "../../src/ia-db/pool.js";
import { mutateTaskInsert } from "../../src/ia-db/mutations.js";

export interface SectionCloseoutFixtureHandle {
  slug: string;
  section_id: string;
  carcass_stage_id: string;
  section_stage_ids: string[];
  /** Task ids returned in the same order as `section_stage_ids`. */
  task_ids: string[];
}

export async function seedSectionCloseoutFixture(
  slug: string,
): Promise<SectionCloseoutFixtureHandle> {
  const handle: SectionCloseoutFixtureHandle = {
    slug,
    section_id: "A",
    carcass_stage_id: "carcass.1",
    section_stage_ids: ["section.A.1", "section.A.2"],
    task_ids: [],
  };

  const pool = getIaDatabasePool();
  if (pool === null) return handle;

  await pool.query(
    `INSERT INTO ia_master_plans (slug, title)
       VALUES ($1, 'sandbox section-closeout plan')
     ON CONFLICT (slug) DO NOTHING`,
    [slug],
  );

  // Carcass stage — done, no section_id.
  await pool.query(
    `INSERT INTO ia_stages
       (slug, stage_id, title, status, carcass_role, section_id)
     VALUES ($1, $2, 'sandbox carcass stage', 'done', 'carcass', NULL)
     ON CONFLICT (slug, stage_id) DO UPDATE
       SET carcass_role = EXCLUDED.carcass_role,
           section_id   = EXCLUDED.section_id,
           status       = EXCLUDED.status`,
    [slug, handle.carcass_stage_id],
  );

  // Section stages — pending, section_id='A'.
  for (const stageId of handle.section_stage_ids) {
    await pool.query(
      `INSERT INTO ia_stages
         (slug, stage_id, title, status, carcass_role, section_id)
       VALUES ($1, $2, $3, 'pending', 'section', 'A')
       ON CONFLICT (slug, stage_id) DO UPDATE
         SET carcass_role = EXCLUDED.carcass_role,
             section_id   = EXCLUDED.section_id,
             status       = EXCLUDED.status`,
      [slug, stageId, `sandbox section stage ${stageId}`],
    );
  }

  // 1 task per section stage. Use mutateTaskInsert to allocate monotonic
  // TECH ids from per-prefix sequence; bind each task to its stage.
  for (const stageId of handle.section_stage_ids) {
    const ins = await mutateTaskInsert({
      prefix: "TECH",
      slug,
      stage_id: stageId,
      title: `sandbox task for ${stageId}`,
      status: "pending",
    });
    handle.task_ids.push(ins.task_id);
  }

  return handle;
}

export async function teardownSectionCloseoutFixture(
  slug: string,
): Promise<void> {
  const pool = getIaDatabasePool();
  if (pool === null) return;

  // Order matters: `ia_tasks_stage_fk` is ON DELETE RESTRICT (not
  // CASCADE), so deleting `ia_master_plans` first blocks on
  // `ia_stages` → `ia_tasks`. Delete tasks explicitly, then plan;
  // remaining cascades reach ia_stages + stage_carcass_signals +
  // ia_section_claims + ia_stage_claims via ON DELETE CASCADE.
  await pool.query(
    `DELETE FROM ia_tasks WHERE slug = $1`,
    [slug],
  );
  await pool.query(
    `DELETE FROM ia_master_plans WHERE slug = $1`,
    [slug],
  );
}
