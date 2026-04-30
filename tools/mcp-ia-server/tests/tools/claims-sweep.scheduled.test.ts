/**
 * claims-sweep.scheduled — scheduler-tick integration test (TECH-5253).
 *
 * Asserts the T2.3.1 `runSweepTick()` runner releases backdated claim rows
 * + emits one `master_plan_change_log` row per affected slug:
 *
 *   1. Seed sandbox master plan + section + stage rows under disjoint slug
 *      `__test_claims_sweep_scheduled__`.
 *   2. Insert two claim rows (`ia_section_claims` + `ia_stage_claims`) and
 *      backdate `last_heartbeat` 11 minutes — past the default 10-min
 *      timeout from `carcass_config.claim_heartbeat_timeout_minutes`.
 *   3. Invoke `runSweepTick()` directly (no cron wait).
 *   4. Assert both rows have `released_at IS NOT NULL`.
 *   5. Assert `master_plan_change_log` carries exactly one row scoped to
 *      sandbox slug with `kind='claim_swept'` + `metadata.released_count`
 *      ≥ 2 + presence of both `metadata.section_released[]` +
 *      `metadata.stage_released[]` arrays.
 *
 * Sandbox isolation: per-file slug `__test_claims_sweep_scheduled__`,
 * disjoint from sibling claim-heartbeat fixture (`__test_claim_heartbeat__`)
 * + carcass-health fixture. `node --test` runs files in parallel workers.
 *
 * Stage 2.3 / TECH-5253 — parallel-carcass §6.2 / D4.
 */

import assert from "node:assert/strict";
import { after, before, describe, it } from "node:test";
import { getIaDatabasePool } from "../../src/ia-db/pool.js";
// @ts-expect-error — .mjs import, no type defs
import { runSweepTick } from "../../../scripts/claims-sweep-tick.mjs";

const pool = getIaDatabasePool();
const skip = pool === null ? { skip: "DB pool unavailable" } : {};

const SLUG = "__test_claims_sweep_scheduled__";
const SECTION_ID = "S";
const STAGE_ID = "section.S.1";

interface ClaimRow {
  released_at: Date | null;
}

interface ChangeLogRow {
  kind: string;
  body: string;
  actor: string | null;
}

async function seedSandbox(): Promise<void> {
  if (!pool) return;
  await pool.query(
    `INSERT INTO ia_master_plans (slug, title)
       VALUES ($1, 'sandbox claims-sweep scheduled plan')
     ON CONFLICT (slug) DO NOTHING`,
    [SLUG],
  );
  // Carcass stage first — DB-level trigger blocks section-only plans
  // ("section stages but zero carcass stages"). Carcass stages have
  // section_id NULL by D19.
  await pool.query(
    `INSERT INTO ia_stages
       (slug, stage_id, title, status, carcass_role, section_id)
     VALUES ($1, 'carcass.1', 'sandbox carcass stage', 'pending', 'carcass', NULL)
     ON CONFLICT (slug, stage_id) DO UPDATE
       SET section_id = EXCLUDED.section_id,
           carcass_role = EXCLUDED.carcass_role`,
    [SLUG],
  );
  await pool.query(
    `INSERT INTO ia_stages
       (slug, stage_id, title, status, carcass_role, section_id)
     VALUES ($1, $2, 'sandbox section stage', 'pending', 'section', $3)
     ON CONFLICT (slug, stage_id) DO UPDATE
       SET section_id = EXCLUDED.section_id,
           carcass_role = EXCLUDED.carcass_role`,
    [SLUG, STAGE_ID, SECTION_ID],
  );
}

async function teardownSandbox(): Promise<void> {
  if (!pool) return;
  // Order: change_log first (no FK back to plan but row is plan-scoped),
  // then claims (FK on slug + (section_id|stage_id)), then plan (cascades
  // ia_stages). Keeps teardown deterministic across parallel workers.
  await pool.query(
    `DELETE FROM ia_master_plan_change_log WHERE slug = $1`,
    [SLUG],
  );
  await pool.query(
    `DELETE FROM ia_stage_claims WHERE slug = $1`,
    [SLUG],
  );
  await pool.query(
    `DELETE FROM ia_section_claims WHERE slug = $1`,
    [SLUG],
  );
  await pool.query(
    `DELETE FROM ia_master_plans WHERE slug = $1`,
    [SLUG],
  );
}

async function seedBackdatedClaims(): Promise<void> {
  if (!pool) return;
  // Insert active rows then backdate last_heartbeat. Default insert clamps
  // last_heartbeat to now() — explicit UPDATE is the cheapest path.
  await pool.query(
    `INSERT INTO ia_section_claims (slug, section_id)
       VALUES ($1, $2)
     ON CONFLICT (slug, section_id) DO UPDATE
       SET released_at = NULL`,
    [SLUG, SECTION_ID],
  );
  await pool.query(
    `INSERT INTO ia_stage_claims (slug, stage_id)
       VALUES ($1, $2)
     ON CONFLICT (slug, stage_id) DO UPDATE
       SET released_at = NULL`,
    [SLUG, STAGE_ID],
  );
  await pool.query(
    `UPDATE ia_section_claims
        SET last_heartbeat = now() - interval '11 minutes',
            released_at    = NULL
      WHERE slug = $1 AND section_id = $2`,
    [SLUG, SECTION_ID],
  );
  await pool.query(
    `UPDATE ia_stage_claims
        SET last_heartbeat = now() - interval '11 minutes',
            released_at    = NULL
      WHERE slug = $1 AND stage_id = $2`,
    [SLUG, STAGE_ID],
  );
}

async function readSectionRow(): Promise<ClaimRow | null> {
  if (!pool) return null;
  const res = await pool.query<ClaimRow>(
    `SELECT released_at
       FROM ia_section_claims
      WHERE slug = $1 AND section_id = $2`,
    [SLUG, SECTION_ID],
  );
  return res.rows[0] ?? null;
}

async function readStageRow(): Promise<ClaimRow | null> {
  if (!pool) return null;
  const res = await pool.query<ClaimRow>(
    `SELECT released_at
       FROM ia_stage_claims
      WHERE slug = $1 AND stage_id = $2`,
    [SLUG, STAGE_ID],
  );
  return res.rows[0] ?? null;
}

async function readChangeLogRows(): Promise<ChangeLogRow[]> {
  if (!pool) return [];
  const res = await pool.query<ChangeLogRow>(
    `SELECT kind, body, actor
       FROM ia_master_plan_change_log
      WHERE slug = $1
        AND kind = 'claim_swept'`,
    [SLUG],
  );
  return res.rows;
}

describe("claims-sweep.scheduled (TECH-5253)", skip, () => {
  before(async () => {
    if (!pool) return;
    await teardownSandbox();
    await seedSandbox();
  });

  after(async () => {
    if (!pool) return;
    await teardownSandbox();
  });

  it("tick releases backdated rows + emits one change_log row per slug", async () => {
    if (!pool) return;
    await seedBackdatedClaims();

    const result = await runSweepTick();

    assert.ok(
      result.released_count_total >= 2,
      `expected >= 2 total releases, got ${result.released_count_total}`,
    );
    assert.ok(
      result.affected_slugs.includes(SLUG),
      `expected sandbox slug ${SLUG} in affected_slugs, got ${JSON.stringify(result.affected_slugs)}`,
    );

    const sec = await readSectionRow();
    const stg = await readStageRow();
    assert.ok(sec !== null && stg !== null, "claim rows must exist post-sweep");
    assert.ok(
      sec.released_at !== null,
      "section claim row must be released after sweep tick",
    );
    assert.ok(
      stg.released_at !== null,
      "stage claim row must be released after sweep tick",
    );

    const rows = await readChangeLogRows();
    // Exactly one row per affected slug — sandbox slug owns one row.
    assert.equal(
      rows.length,
      1,
      `expected exactly one claim_swept change_log row for sandbox slug, got ${rows.length}`,
    );
    const parsed = JSON.parse(rows[0]!.body) as {
      released_count: number;
      section_released: string[];
      stage_released: string[];
      timeout_minutes: number;
    };
    assert.ok(
      parsed.released_count >= 2,
      `metadata.released_count expected >= 2, got ${parsed.released_count}`,
    );
    assert.ok(
      Array.isArray(parsed.section_released) &&
        parsed.section_released.length >= 1,
      `metadata.section_released[] missing or empty: ${JSON.stringify(parsed.section_released)}`,
    );
    assert.ok(
      Array.isArray(parsed.stage_released) &&
        parsed.stage_released.length >= 1,
      `metadata.stage_released[] missing or empty: ${JSON.stringify(parsed.stage_released)}`,
    );
    assert.equal(rows[0]!.kind, "claim_swept");
    assert.equal(rows[0]!.actor, "claims-sweep-tick");
  });
});
