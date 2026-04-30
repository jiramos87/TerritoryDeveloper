/**
 * Synthetic fixture for parallel-carcass sections-visibility E2E.
 *
 * Seeds one sandbox master plan with section topology designed to exercise
 * the `/plans/[slug]/sections` render path:
 *   - 2 section stages: section.A.1 (section_id='A'), section.B.1
 *     (section_id='B'); both `carcass_role='section'`, status='pending'.
 *   - 1 carcass stage: carcass.1 (carcass_role='carcass', section_id NULL).
 *   - 1 ia_section_claims row: section_id='A', last_heartbeat=now() (held).
 *   - section_id='B' has no claim row → "free".
 *
 * Idempotent: teardown cascades via FK ON DELETE CASCADE on ia_stages +
 * ia_section_claims (mig 0049 + 0052). Re-runs leave zero rows under the
 * chosen slug.
 *
 * DB-less CI: getIaDatabasePool() === null short-circuits both helpers.
 *
 * Stage 2.2 / TECH-5249 of `parallel-carcass-rollout`. Spec at
 * `web/tests/plans-sections.spec.ts` consumes seed/teardown.
 */

import { getIaDatabasePool } from "../../src/ia-db/pool.js";

export const SECTIONS_VISIBILITY_FIXTURE_SLUG =
  "__test_parallel_sections_visibility__";

export interface SectionsVisibilityFixtureHandle {
  slug: string;
  carcassStageIds: string[];
  sectionStageIds: string[];
  heldSectionId: string;
  freeSectionId: string;
}

export async function seedSectionsVisibilityFixture(
  slug: string = SECTIONS_VISIBILITY_FIXTURE_SLUG,
): Promise<SectionsVisibilityFixtureHandle> {
  const handle: SectionsVisibilityFixtureHandle = {
    slug,
    carcassStageIds: ["carcass.1"],
    sectionStageIds: ["section.A.1", "section.B.1"],
    heldSectionId: "A",
    freeSectionId: "B",
  };

  const pool = getIaDatabasePool();
  if (pool === null) return handle;

  // Cascade-clean any prior run before re-seeding so heartbeats stay fresh.
  await pool.query(`DELETE FROM ia_master_plans WHERE slug = $1`, [
    handle.slug,
  ]);

  await pool.query(
    `INSERT INTO ia_master_plans (slug, title)
       VALUES ($1, 'sandbox sections visibility plan')`,
    [handle.slug],
  );

  // Carcass stage — carcass_role='carcass', section_id NULL (per D19).
  await pool.query(
    `INSERT INTO ia_stages
       (slug, stage_id, title, status, carcass_role, section_id)
     VALUES ($1, 'carcass.1', 'sandbox carcass stage',
             'done', 'carcass', NULL)`,
    [handle.slug],
  );

  // Section stages — carcass_role='section'; section_id 'A' / 'B'.
  await pool.query(
    `INSERT INTO ia_stages
       (slug, stage_id, title, status, carcass_role, section_id)
     VALUES ($1, 'section.A.1', 'sandbox section A stage',
             'pending', 'section', 'A')`,
    [handle.slug],
  );
  await pool.query(
    `INSERT INTO ia_stages
       (slug, stage_id, title, status, carcass_role, section_id)
     VALUES ($1, 'section.B.1', 'sandbox section B stage',
             'pending', 'section', 'B')`,
    [handle.slug],
  );

  // Active claim on section A — last_heartbeat fresh (now()).
  await pool.query(
    `INSERT INTO ia_section_claims
       (slug, section_id, claimed_at, last_heartbeat)
     VALUES ($1, $2, now(), now())`,
    [handle.slug, handle.heldSectionId],
  );

  return handle;
}

export async function teardownSectionsVisibilityFixture(
  slug: string = SECTIONS_VISIBILITY_FIXTURE_SLUG,
): Promise<void> {
  const pool = getIaDatabasePool();
  if (pool === null) return;
  await pool.query(`DELETE FROM ia_master_plans WHERE slug = $1`, [slug]);
}
