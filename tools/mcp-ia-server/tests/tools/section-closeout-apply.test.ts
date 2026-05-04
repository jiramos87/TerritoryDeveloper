/**
 * section_closeout_apply E2E test — TECH-5071.
 *
 * Drives `applySectionCloseout` happy + negative paths against the
 * synthetic 2-section topology authored by TECH-5070
 * (`tests/fixtures/section-closeout.fixture.ts`).
 *
 * Happy path:
 *   - Seed fixture, open section claim + 2 stage claims, raw-UPDATE both
 *     section stages to status='done', call `applySectionCloseout`.
 *   - Assert all 7 result fields (`applied=true`, `stages_total=2`,
 *     `stages_done=2`, `change_log_entry_id != null`,
 *     `section_claim_released=true`, `cascaded_stage_releases=2`).
 *   - Assert `ia_master_plan_change_log` row body parses to JSON with
 *     `section_id='A'` and `stages.length === 2`.
 *   - Assert `runArchDriftScan({plan_id, scope:'intra-plan',
 *     section_id:'A'})` returns zero `affected_stages`.
 *
 * Negative path:
 *   - Reseed fixture, open claims, flip only 1 of 2 stages to done, call
 *     `applySectionCloseout`. Assert `applied=false`,
 *     `error='stages_not_done'`, no change_log row written, claims still
 *     open.
 *
 * Sandbox slug `__test_section_closeout_apply__` — disjoint from
 * TECH-5070's `__test_section_closeout_e2e__` so parallel `node --test`
 * workers don't collide.
 *
 * Stage status flips use raw `pool.query` UPDATE on `ia_stages.status`
 * because no mutation helper for direct stage flip exists and
 * `applySectionCloseout` reads `ia_stages.status`, not `ia_tasks.status`
 * (per `section-closeout-apply.ts` L73-78).
 */

import assert from "node:assert/strict";
import { after, before, describe, it } from "node:test";
import { getIaDatabasePool } from "../../src/ia-db/pool.js";
import { applySectionClaim } from "../../src/tools/section-claim.js";
import { applyStageClaim } from "../../src/tools/stage-claim.js";
import { applySectionCloseout } from "../../src/tools/section-closeout-apply.js";
import { runArchDriftScan } from "../../src/tools/arch.js";
import {
  seedSectionCloseoutFixture,
  teardownSectionCloseoutFixture,
} from "../fixtures/section-closeout.fixture.js";

const pool = getIaDatabasePool();
const skip = pool === null ? { skip: "DB pool unavailable" } : {};

const SLUG = "__test_section_closeout_apply__";
const SECTION_ID = "A";
const STAGE_A1 = "section.A.1";
const STAGE_A2 = "section.A.2";

interface ChangeLogRow {
  kind: string;
  body: string;
}

async function flipStageDone(slug: string, stageId: string): Promise<void> {
  if (!pool) return;
  await pool.query(
    `UPDATE ia_stages
        SET status = 'done'::stage_status, updated_at = now()
      WHERE slug = $1 AND stage_id = $2`,
    [slug, stageId],
  );
}

async function readChangeLog(
  slug: string,
  entryId: number,
): Promise<ChangeLogRow | null> {
  if (!pool) return null;
  const res = await pool.query<ChangeLogRow>(
    `SELECT kind, body
       FROM ia_master_plan_change_log
      WHERE slug = $1 AND entry_id = $2`,
    [slug, entryId],
  );
  return res.rows[0] ?? null;
}

async function countChangeLogRows(slug: string): Promise<number> {
  if (!pool) return 0;
  const res = await pool.query<{ n: string }>(
    `SELECT COUNT(*)::text AS n
       FROM ia_master_plan_change_log
      WHERE slug = $1 AND kind = 'section_done'`,
    [slug],
  );
  return Number(res.rows[0]?.n ?? "0");
}

async function countOpenSectionClaims(slug: string): Promise<number> {
  if (!pool) return 0;
  const res = await pool.query<{ n: string }>(
    `SELECT COUNT(*)::text AS n
       FROM ia_section_claims
      WHERE slug = $1 AND released_at IS NULL`,
    [slug],
  );
  return Number(res.rows[0]?.n ?? "0");
}

async function countOpenStageClaims(slug: string): Promise<number> {
  if (!pool) return 0;
  const res = await pool.query<{ n: string }>(
    `SELECT COUNT(*)::text AS n
       FROM ia_stage_claims
      WHERE slug = $1 AND released_at IS NULL`,
    [slug],
  );
  return Number(res.rows[0]?.n ?? "0");
}

describe("section_closeout_apply E2E (TECH-5071)", skip, () => {
  before(async () => {
    if (!pool) return;
    await teardownSectionCloseoutFixture(SLUG);
  });

  after(async () => {
    if (!pool) return;
    await teardownSectionCloseoutFixture(SLUG);
  });

  it("happy path — all 7 result fields + change_log JSON + drift-scan empty", async () => {
    if (!pool) return;
    await teardownSectionCloseoutFixture(SLUG);
    await seedSectionCloseoutFixture(SLUG);

    await applySectionClaim({ slug: SLUG, section_id: SECTION_ID });
    await applyStageClaim({ slug: SLUG, stage_id: STAGE_A1 });
    await applyStageClaim({ slug: SLUG, stage_id: STAGE_A2 });

    await flipStageDone(SLUG, STAGE_A1);
    await flipStageDone(SLUG, STAGE_A2);

    const result = await applySectionCloseout({
      slug: SLUG,
      section_id: SECTION_ID,
      actor: "test",
      commit_sha: "deadbeef",
    });

    assert.equal(result.applied, true);
    assert.equal(result.stages_total, 2);
    assert.equal(result.stages_done, 2);
    assert.notEqual(result.change_log_entry_id, null);
    assert.equal(result.section_claim_released, true);
    assert.equal(result.cascaded_stage_releases, 2);
    assert.equal(result.error, undefined);

    const cl = await readChangeLog(SLUG, result.change_log_entry_id!);
    assert.ok(cl !== null, "change_log row must exist");
    assert.equal(cl.kind, "section_done");
    const body = JSON.parse(cl.body) as {
      section_id: string;
      stages: string[];
    };
    assert.equal(body.section_id, SECTION_ID);
    assert.equal(body.stages.length, 2);
    assert.deepEqual([...body.stages].sort(), [STAGE_A1, STAGE_A2]);

    const drift = await runArchDriftScan(pool, {
      plan_id: SLUG,
      scope: "intra-plan",
      section_id: SECTION_ID,
    });
    assert.equal(
      drift.affected_stages.length,
      0,
      "intra-plan drift scan must be empty after section closeout",
    );

    // Sanity: claims released post-apply.
    assert.equal(await countOpenSectionClaims(SLUG), 0);
    assert.equal(await countOpenStageClaims(SLUG), 0);
  });

  it("red coverage — 100% failed_as_expected → applied=true, coverage=100", async () => {
    if (!pool) return;
    await teardownSectionCloseoutFixture(SLUG);
    await seedSectionCloseoutFixture(SLUG);

    await flipStageDone(SLUG, STAGE_A1);
    await flipStageDone(SLUG, STAGE_A2);

    // Seed failed_as_expected proof for both stages.
    for (const stageId of [STAGE_A1, STAGE_A2]) {
      await pool.query(
        `INSERT INTO ia_red_stage_proofs
           (slug, stage_id, target_kind, anchor, proof_artifact_id, proof_status)
         VALUES ($1, $2, 'tracer_verb', $3, gen_random_uuid(), 'failed_as_expected')`,
        [SLUG, stageId, `test-anchor:${stageId}::SomeTest`],
      );
    }

    const result = await applySectionCloseout({ slug: SLUG, section_id: SECTION_ID });
    assert.equal(result.applied, true);
    assert.equal(result.red_stage_coverage_pct, 100);
    assert.equal(result.pending_count, 0);
    assert.equal(result.unexpected_pass_count, 0);
  });

  it("red coverage — partial with pending rows → applied=true, coverage<100, pending_count>0", async () => {
    if (!pool) return;
    await teardownSectionCloseoutFixture(SLUG);
    await seedSectionCloseoutFixture(SLUG);

    await flipStageDone(SLUG, STAGE_A1);
    await flipStageDone(SLUG, STAGE_A2);

    // A1 has failed_as_expected, A2 has no proof row (pending).
    await pool.query(
      `INSERT INTO ia_red_stage_proofs
         (slug, stage_id, target_kind, anchor, proof_artifact_id, proof_status)
       VALUES ($1, $2, 'tracer_verb', $3, gen_random_uuid(), 'failed_as_expected')`,
      [SLUG, STAGE_A1, `test-anchor:${STAGE_A1}::SomeTest`],
    );

    const result = await applySectionCloseout({ slug: SLUG, section_id: SECTION_ID });
    assert.equal(result.applied, true);
    assert.ok(result.red_stage_coverage_pct !== null && result.red_stage_coverage_pct < 100);
    assert.ok(result.pending_count > 0);
    assert.equal(result.unexpected_pass_count, 0);
  });

  it("red coverage — unexpected_pass blocks closeout → applied=false, error set, no change_log", async () => {
    if (!pool) return;
    await teardownSectionCloseoutFixture(SLUG);
    await seedSectionCloseoutFixture(SLUG);

    await flipStageDone(SLUG, STAGE_A1);
    await flipStageDone(SLUG, STAGE_A2);

    // A1 has unexpected_pass proof — should block closeout.
    await pool.query(
      `INSERT INTO ia_red_stage_proofs
         (slug, stage_id, target_kind, anchor, proof_artifact_id, proof_status)
       VALUES ($1, $2, 'tracer_verb', $3, gen_random_uuid(), 'unexpected_pass')`,
      [SLUG, STAGE_A1, `test-anchor:${STAGE_A1}::SomeTest`],
    );

    const before_log = await countChangeLogRows(SLUG);
    const result = await applySectionCloseout({ slug: SLUG, section_id: SECTION_ID });
    assert.equal(result.applied, false);
    assert.equal(result.error, "red_stage_unexpected_pass_blocks_closeout");
    assert.ok(result.unexpected_pass_count > 0);
    assert.equal(await countChangeLogRows(SLUG), before_log);
    assert.equal(result.change_log_entry_id, null);
  });

  it("red coverage — null for grandfathered section (zero proof rows) → applied=true, coverage=null", async () => {
    if (!pool) return;
    await teardownSectionCloseoutFixture(SLUG);
    await seedSectionCloseoutFixture(SLUG);

    await flipStageDone(SLUG, STAGE_A1);
    await flipStageDone(SLUG, STAGE_A2);

    // No proof rows inserted — grandfathered section.
    const result = await applySectionCloseout({ slug: SLUG, section_id: SECTION_ID });
    assert.equal(result.applied, true);
    assert.equal(result.red_stage_coverage_pct, null);
    assert.equal(result.pending_count, 0);
    assert.equal(result.unexpected_pass_count, 0);
  });

  it("negative path — only 1/2 stages done → applied=false, no change_log, claims open", async () => {
    if (!pool) return;
    await teardownSectionCloseoutFixture(SLUG);
    await seedSectionCloseoutFixture(SLUG);

    await applySectionClaim({ slug: SLUG, section_id: SECTION_ID });
    await applyStageClaim({ slug: SLUG, stage_id: STAGE_A1 });
    await applyStageClaim({ slug: SLUG, stage_id: STAGE_A2 });

    // Flip only A1; A2 stays pending.
    await flipStageDone(SLUG, STAGE_A1);

    const before_log = await countChangeLogRows(SLUG);
    const result = await applySectionCloseout({
      slug: SLUG,
      section_id: SECTION_ID,
    });

    assert.equal(result.applied, false);
    assert.equal(result.error, "stages_not_done");
    assert.equal(result.stages_total, 2);
    assert.equal(result.stages_done, 1);
    assert.equal(result.change_log_entry_id, null);
    assert.equal(result.section_claim_released, false);
    assert.equal(result.cascaded_stage_releases, 0);

    // Change log unchanged.
    assert.equal(await countChangeLogRows(SLUG), before_log);

    // Claims still open.
    assert.equal(await countOpenSectionClaims(SLUG), 1);
    assert.equal(await countOpenStageClaims(SLUG), 2);
  });
});
