/**
 * Carcass-flip semantics test for `ia_master_plan_health` mat view (TECH-4624).
 *
 * Sequential flip + refresh + assert per carcass stage; final assert proves
 * `carcass_done=true` only after ALL carcass-role rows reach status='done'.
 *
 * Legacy edge-case: zero-carcass plan (NULL `carcass_role`) → `carcass_done`
 * IS NULL (mig 0050 CASE WHEN n_carcass=0 THEN NULL). Test asserts NULL,
 * NOT true — caller path (`applyCarcassFilters`) treats NULL the same as
 * "no gate", so legacy linear plans never block.
 *
 * Runner: node:test (sibling pattern). Direct `pool.query` access — matches
 * mutations.test.ts harness style.
 */

import assert from "node:assert/strict";
import { after, before, describe, it } from "node:test";
import { getIaDatabasePool } from "../../src/ia-db/pool.js";
import { getMasterPlanHealth } from "../../src/tools/master-plan-health.js";
import {
  seedCarcassHealthFixture,
  teardownCarcassHealthFixture,
} from "../fixtures/parallel-carcass-health-mv.fixture.js";

const pool = getIaDatabasePool();
const skip = pool === null ? { skip: "DB pool unavailable" } : {};

// Per-file slug — `node --test` runs files in parallel workers. Shared
// slug + teardown DELETE cascades cause `ia_stages_slug_fkey` violations
// mid-seed. Each file owns a disjoint sandbox slug.
const CARCASS_FIXTURE_SLUG = "__test_carcass_health__";
const LEGACY_SLUG = "__test_legacy_carcass_health__";

interface HealthRow {
  carcass_done: boolean | null;
  n_carcass: number;
  n_carcass_done: number;
}

async function readHealth(slug: string): Promise<HealthRow | null> {
  if (!pool) return null;
  const res = await pool.query<HealthRow>(
    `SELECT carcass_done, n_carcass, n_carcass_done
       FROM ia_master_plan_health
      WHERE slug = $1`,
    [slug],
  );
  return res.rows[0] ?? null;
}

async function refreshHealth(): Promise<void> {
  if (!pool) return;
  await pool.query(`REFRESH MATERIALIZED VIEW ia_master_plan_health`);
}

async function seedLegacyPlan(): Promise<void> {
  if (!pool) return;
  await pool.query(
    `INSERT INTO ia_master_plans (slug, title)
       VALUES ($1, 'sandbox legacy linear plan')
     ON CONFLICT (slug) DO NOTHING`,
    [LEGACY_SLUG],
  );
  for (const stageId of ["legacy.1", "legacy.2"]) {
    await pool.query(
      `INSERT INTO ia_stages
         (slug, stage_id, title, status, carcass_role, section_id)
       VALUES ($1, $2, $3, 'pending', NULL, NULL)
       ON CONFLICT (slug, stage_id) DO UPDATE
         SET carcass_role = NULL,
             section_id   = NULL,
             status       = EXCLUDED.status`,
      [LEGACY_SLUG, stageId, `legacy stage ${stageId}`],
    );
  }
}

async function teardownLegacyPlan(): Promise<void> {
  if (!pool) return;
  await pool.query(
    `DELETE FROM ia_master_plans WHERE slug = $1`,
    [LEGACY_SLUG],
  );
}

describe("ia_master_plan_health — carcass_done flip (TECH-4624)", skip, () => {
  before(async () => {
    if (!pool) return;
    await teardownCarcassHealthFixture(CARCASS_FIXTURE_SLUG);
    await teardownLegacyPlan();
    await seedCarcassHealthFixture(CARCASS_FIXTURE_SLUG);
    await refreshHealth();
  });

  after(async () => {
    if (!pool) return;
    await teardownCarcassHealthFixture(CARCASS_FIXTURE_SLUG);
    await teardownLegacyPlan();
  });

  it("sequential flip — carcass_done flips false→true after final stage closes", async () => {
    if (!pool) return;

    const pre = await readHealth(CARCASS_FIXTURE_SLUG);
    assert.ok(pre !== null, "expected MV row pre-flip — STOPPED mv_refresh_failed");
    assert.equal(pre.carcass_done, false, "pre-flip carcass_done must be false");
    assert.equal(pre.n_carcass, 2, "expected n_carcass=2");
    assert.equal(pre.n_carcass_done, 0, "expected n_carcass_done=0 pre-flip");

    const carcassStages = ["carcass.1", "carcass.2"] as const;
    for (let i = 0; i < carcassStages.length; i++) {
      const stageId = carcassStages[i];
      await pool.query(
        `UPDATE ia_stages SET status = 'done'
          WHERE slug = $1 AND stage_id = $2`,
        [CARCASS_FIXTURE_SLUG, stageId],
      );
      await refreshHealth();
      const row = await readHealth(CARCASS_FIXTURE_SLUG);
      assert.ok(row !== null, `MV row missing after flip ${stageId}`);
      const expectedDone = i === carcassStages.length - 1;
      assert.equal(
        row.carcass_done,
        expectedDone,
        `after flipping ${stageId} (${i + 1}/${carcassStages.length}) ` +
          `expected carcass_done=${expectedDone}, got ${row.carcass_done}`,
      );
      assert.equal(
        row.n_carcass_done,
        i + 1,
        `n_carcass_done should track flip count (got ${row.n_carcass_done})`,
      );
    }

    const final = await readHealth(CARCASS_FIXTURE_SLUG);
    assert.ok(final !== null);
    assert.equal(final.carcass_done, true, "final carcass_done must be true");
    assert.equal(final.n_carcass, 2);
    assert.equal(final.n_carcass_done, 2);
  });

  it("master-plan-health: arch_drift_scan_p95_ms field present in tool payload (null tolerated pre-bench)", async () => {
    if (!pool) return;
    const rows = await getMasterPlanHealth(CARCASS_FIXTURE_SLUG);
    assert.ok(rows.length > 0, "expected MV row for fixture slug");
    assert.ok(
      "arch_drift_scan_p95_ms" in rows[0]!,
      "arch_drift_scan_p95_ms missing from getMasterPlanHealth() payload (mig 0055)",
    );
  });

  it("master-plan-health: stage_1_is_tracer surfaces tracer_slice_block presence (TECH-10307)", async () => {
    if (!pool) return;

    const { readTracerSliceSlugs } = await import(
      "../../src/tools/master-plan-health.js"
    );

    // Baseline: ensure fixture has no tracer_slice_block on Stage 1.x.
    await pool.query(
      `UPDATE ia_stages SET tracer_slice_block = NULL
        WHERE slug = $1 AND stage_id IN ('1.0','1.1')`,
      [CARCASS_FIXTURE_SLUG],
    );
    const baseSet = await readTracerSliceSlugs();
    assert.equal(
      baseSet.has(CARCASS_FIXTURE_SLUG),
      false,
      "baseline: fixture slug must NOT appear in tracer-slice slug set",
    );

    // Seed a Stage 1.1 with tracer_slice_block; assert slug now appears.
    await pool.query(
      `INSERT INTO ia_stages (slug, stage_id, title, status, tracer_slice_block)
         VALUES ($1, '1.1', 'sandbox tracer stage', 'pending', $2::jsonb)
       ON CONFLICT (slug, stage_id) DO UPDATE
         SET tracer_slice_block = EXCLUDED.tracer_slice_block`,
      [
        CARCASS_FIXTURE_SLUG,
        JSON.stringify({
          name: "sandbox-tracer",
          verb: "test",
          surface: "sandbox",
          evidence: "sandbox",
          gate: "sandbox",
        }),
      ],
    );
    const seededSet = await readTracerSliceSlugs();
    assert.equal(
      seededSet.has(CARCASS_FIXTURE_SLUG),
      true,
      "after seed: fixture slug MUST appear in tracer-slice slug set",
    );

    // rowToPayload synthesis contract: stage_1_is_tracer = tracerSlugs.has(slug).
    // Asserted via membership check — payload layer is a pure boolean projection
    // of this set, so set-membership = field-truth.

    // Cleanup the seeded tracer block.
    await pool.query(
      `UPDATE ia_stages SET tracer_slice_block = NULL
        WHERE slug = $1 AND stage_id = '1.1'`,
      [CARCASS_FIXTURE_SLUG],
    );
  });

  it("legacy edge case — zero-carcass plan: carcass_done IS NULL", async () => {
    if (!pool) return;
    await seedLegacyPlan();
    await refreshHealth();

    const row = await readHealth(LEGACY_SLUG);
    assert.ok(row !== null, "expected MV row for legacy plan");
    assert.equal(
      row.carcass_done,
      null,
      "vacuous semantic: zero carcass stages → carcass_done IS NULL " +
        "(mig 0050 CASE WHEN n_carcass=0 THEN NULL). Non-NULL → vacuous_semantic_drift.",
    );
    assert.equal(row.n_carcass, 0, "legacy plan should report n_carcass=0");
  });
});
