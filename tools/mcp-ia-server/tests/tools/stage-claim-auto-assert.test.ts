/**
 * Stage-claim parent-assert + sibling-reuse test (TECH-4828).
 *
 * Three behaviors of `applyStageClaim` under V2 row-only mutex (mig 0052):
 *
 *   1. Bare `applyStageClaim` (no prior section claim) on a stage whose
 *      `ia_stages.section_id` is non-null → throws `section_claim_required`.
 *      Src `stage-claim.ts` performs an explicit SELECT on
 *      `ia_section_claims` and refuses to insert when no open parent row
 *      exists. (Drifts from older spec text suggesting auto-insert; src
 *      is the truth.)
 *
 *   2. Sibling reuse: with parent `section_claim` open, two sibling
 *      stages (`section.A.1`, `section.A.2`, both in `section_id='A'`) can
 *      both `stage_claim` successfully under the same parent row → 1 open
 *      section row + 2 open stage rows.
 *
 *   3. Per-stage release symmetry: `applyStageRelease` on one stage does
 *      not cascade to parent or sibling — only the targeted stage row
 *      flips `released_at IS NOT NULL`.
 *
 * Sandbox slug `__test_stage_claim_auto_assert__`. Reuses fixture topology
 * (section.A.1 + section.A.2 stages with `section_id='A'`).
 */

import assert from "node:assert/strict";
import { after, before, describe, it } from "node:test";
import { getIaDatabasePool } from "../../src/ia-db/pool.js";
import {
  applySectionClaim,
  applySectionRelease,
} from "../../src/tools/section-claim.js";
import {
  applyStageClaim,
  applyStageRelease,
} from "../../src/tools/stage-claim.js";
import {
  seedCarcassHealthFixture,
  teardownCarcassHealthFixture,
} from "../fixtures/parallel-carcass-health-mv.fixture.js";

const pool = getIaDatabasePool();
const skip = pool === null ? { skip: "DB pool unavailable" } : {};

const SLUG = "__test_stage_claim_auto_assert__";
const SECTION_ID = "A";
const STAGE_1 = "section.A.1";
const STAGE_2 = "section.A.2";

async function countOpenSectionClaims(slug: string): Promise<number> {
  if (!pool) return 0;
  const res = await pool.query<{ count: string }>(
    `SELECT count(*)::text AS count
       FROM ia_section_claims
      WHERE slug = $1 AND released_at IS NULL`,
    [slug],
  );
  return Number(res.rows[0]?.count ?? "0");
}

async function countOpenStageClaims(slug: string): Promise<number> {
  if (!pool) return 0;
  const res = await pool.query<{ count: string }>(
    `SELECT count(*)::text AS count
       FROM ia_stage_claims
      WHERE slug = $1 AND released_at IS NULL`,
    [slug],
  );
  return Number(res.rows[0]?.count ?? "0");
}

async function isStageReleased(slug: string, stageId: string): Promise<boolean> {
  if (!pool) return false;
  const res = await pool.query<{ released_at: Date | null }>(
    `SELECT released_at
       FROM ia_stage_claims
      WHERE slug = $1 AND stage_id = $2`,
    [slug, stageId],
  );
  return res.rows[0]?.released_at !== null;
}

async function isSectionReleased(
  slug: string,
  sectionId: string,
): Promise<boolean> {
  if (!pool) return false;
  const res = await pool.query<{ released_at: Date | null }>(
    `SELECT released_at
       FROM ia_section_claims
      WHERE slug = $1 AND section_id = $2`,
    [slug, sectionId],
  );
  return res.rows[0]?.released_at !== null;
}

async function resetClaims(): Promise<void> {
  if (!pool) return;
  await pool.query(
    `DELETE FROM ia_stage_claims WHERE slug = $1`,
    [SLUG],
  );
  await pool.query(
    `DELETE FROM ia_section_claims WHERE slug = $1`,
    [SLUG],
  );
}

describe("stage_claim — parent-assert + sibling reuse (TECH-4828)", skip, () => {
  before(async () => {
    if (!pool) return;
    await teardownCarcassHealthFixture(SLUG);
    await seedCarcassHealthFixture(SLUG);
  });

  after(async () => {
    if (!pool) return;
    await teardownCarcassHealthFixture(SLUG);
  });

  it("parent gate — bare stage_claim throws section_claim_required", async () => {
    if (!pool) return;
    await resetClaims();

    await assert.rejects(
      () => applyStageClaim({ slug: SLUG, stage_id: STAGE_1 }),
      (err: { code?: string }) => err.code === "section_claim_required",
      "expected section_claim_required when no open parent claim exists",
    );

    assert.equal(await countOpenStageClaims(SLUG), 0);
    assert.equal(await countOpenSectionClaims(SLUG), 0);
  });

  it("sibling reuse — two stages in same section share one parent claim", async () => {
    if (!pool) return;
    await resetClaims();

    const sectionResult = await applySectionClaim({
      slug: SLUG,
      section_id: SECTION_ID,
    });
    assert.equal(sectionResult.status, "claimed");

    const stage1Result = await applyStageClaim({ slug: SLUG, stage_id: STAGE_1 });
    const stage2Result = await applyStageClaim({ slug: SLUG, stage_id: STAGE_2 });

    assert.equal(stage1Result.status, "claimed");
    assert.equal(stage2Result.status, "claimed");
    assert.equal(await countOpenSectionClaims(SLUG), 1);
    assert.equal(await countOpenStageClaims(SLUG), 2);
  });

  it("release symmetry — stage release leaves parent + sibling open", async () => {
    if (!pool) return;
    await resetClaims();

    await applySectionClaim({ slug: SLUG, section_id: SECTION_ID });
    await applyStageClaim({ slug: SLUG, stage_id: STAGE_1 });
    await applyStageClaim({ slug: SLUG, stage_id: STAGE_2 });

    const release = await applyStageRelease({ slug: SLUG, stage_id: STAGE_1 });
    assert.equal(release.released, true);

    assert.equal(await isStageReleased(SLUG, STAGE_1), true);
    assert.equal(await isStageReleased(SLUG, STAGE_2), false);
    assert.equal(await isSectionReleased(SLUG, SECTION_ID), false);

    // Cleanup so the after() teardown is symmetric.
    await applyStageRelease({ slug: SLUG, stage_id: STAGE_2 });
    await applySectionRelease({ slug: SLUG, section_id: SECTION_ID });
  });
});
