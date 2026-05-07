/**
 * claim_heartbeat + claims_sweep test (TECH-4829).
 *
 * Asserts V2 row-only heartbeat + sweep semantics (mig 0052):
 *
 *   1. `applyHeartbeat({slug, stage_id})` refreshes both `last_heartbeat`
 *      columns: target stage row in `ia_stage_claims` + parent section row
 *      in `ia_section_claims` (looked up via `ia_stages.section_id`). Returns
 *      counts `{section_claims_refreshed: 1, stage_claims_refreshed: 1}`.
 *
 *   2. `applySweep()` reads `carcass_config.claim_heartbeat_timeout_minutes`
 *      (default 10) and releases rows whose `last_heartbeat` is older than
 *      that interval — cascading across both tables.
 *
 *   3. Bonus: `applyHeartbeat({slug})` with no stage_id and no section_id
 *      throws `missing_target`.
 *
 * Time-warp via direct UPDATE setting `last_heartbeat = now() - interval ...`
 * — deterministic, no setTimeout / wall-clock waits. Sandbox slug
 * `__test_claim_heartbeat__` — disjoint per parallel `node --test` worker.
 */

import assert from "node:assert/strict";
import { after, before, describe, it } from "node:test";
import { getIaDatabasePool } from "../../src/ia-db/pool.js";
import { applySectionClaim } from "../../src/tools/section-claim.js";
import { applyStageClaim } from "../../src/tools/stage-claim.js";
import {
  applyHeartbeat,
  applySweep,
} from "../../src/tools/claim-heartbeat.js";
import {
  seedCarcassHealthFixture,
  teardownCarcassHealthFixture,
} from "../fixtures/parallel-carcass-health-mv.fixture.js";
import { withFixtureLock } from "../fixtures/serialize-fixture.js";

const pool = getIaDatabasePool();
const skip = pool === null ? { skip: "DB pool unavailable" } : {};

const SLUG = "__test_claim_heartbeat__";
const SECTION_ID = "A";
const STAGE_ID = "section.A.1";

interface HeartbeatRow {
  last_heartbeat: Date;
  released_at: Date | null;
}

async function readSectionRow(
  slug: string,
  sectionId: string,
): Promise<HeartbeatRow | null> {
  if (!pool) return null;
  const res = await pool.query<HeartbeatRow>(
    `SELECT last_heartbeat, released_at
       FROM ia_section_claims
      WHERE slug = $1 AND section_id = $2`,
    [slug, sectionId],
  );
  return res.rows[0] ?? null;
}

async function readStageRow(
  slug: string,
  stageId: string,
): Promise<HeartbeatRow | null> {
  if (!pool) return null;
  const res = await pool.query<HeartbeatRow>(
    `SELECT last_heartbeat, released_at
       FROM ia_stage_claims
      WHERE slug = $1 AND stage_id = $2`,
    [slug, stageId],
  );
  return res.rows[0] ?? null;
}

async function freezeRowsToPast(
  slug: string,
  sectionId: string,
  stageId: string,
  intervalText: string,
): Promise<void> {
  if (!pool) return;
  await pool.query(
    `UPDATE ia_section_claims
        SET last_heartbeat = now() - $3::interval
      WHERE slug = $1 AND section_id = $2 AND released_at IS NULL`,
    [slug, sectionId, intervalText],
  );
  await pool.query(
    `UPDATE ia_stage_claims
        SET last_heartbeat = now() - $3::interval
      WHERE slug = $1 AND stage_id = $2 AND released_at IS NULL`,
    [slug, stageId, intervalText],
  );
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

describe("claim_heartbeat + claims_sweep (TECH-4829)", skip, () => {
  before(async () => {
    if (!pool) return;
    await teardownCarcassHealthFixture(SLUG);
    await seedCarcassHealthFixture(SLUG);
  });

  after(async () => {
    if (!pool) return;
    await teardownCarcassHealthFixture(SLUG);
  });

  it("refreshes both tables — stage heartbeat updates section + stage rows", async () => {
    if (!pool) return;
    await resetClaims();

    await applySectionClaim({ slug: SLUG, section_id: SECTION_ID });
    await applyStageClaim({ slug: SLUG, stage_id: STAGE_ID });

    // Freeze both rows 5 minutes into the past so post-heartbeat last_heartbeat
    // is decisively newer (≥ baseline_minus_5min + slack).
    await freezeRowsToPast(SLUG, SECTION_ID, STAGE_ID, "5 minutes");

    const result = await applyHeartbeat({ slug: SLUG, stage_id: STAGE_ID });
    assert.equal(result.section_claims_refreshed, 1);
    assert.equal(result.stage_claims_refreshed, 1);
    assert.equal(result.section_id, SECTION_ID);
    assert.equal(result.stage_id, STAGE_ID);

    const sec = await readSectionRow(SLUG, SECTION_ID);
    const stg = await readStageRow(SLUG, STAGE_ID);
    assert.ok(sec !== null && stg !== null, "rows must exist post-heartbeat");

    const fourMinAgo = new Date(Date.now() - 4 * 60 * 1000);
    assert.ok(
      sec.last_heartbeat.getTime() > fourMinAgo.getTime(),
      `section last_heartbeat ${sec.last_heartbeat.toISOString()} should be newer than 4min ago`,
    );
    assert.ok(
      stg.last_heartbeat.getTime() > fourMinAgo.getTime(),
      `stage last_heartbeat ${stg.last_heartbeat.toISOString()} should be newer than 4min ago`,
    );
  });

  it("sweep releases stale rows past timeout — cascades both tables", async () => {
    if (!pool) return;
    // Lock spans backdate → applySweep → assertions because applySweep is
    // global (not slug-scoped). Without serialization, a parallel worker's
    // applySweep can release this slug's rows before our applySweep runs,
    // leaving result.{section,stage}_claims_released = 0.
    await withFixtureLock(pool, async () => {
      await resetClaims();

      await applySectionClaim({ slug: SLUG, section_id: SECTION_ID });
      await applyStageClaim({ slug: SLUG, stage_id: STAGE_ID });

      // Breach default 10-min timeout by a wide margin (60 min). Default
      // applies if carcass_config row absent (src fallback).
      await freezeRowsToPast(SLUG, SECTION_ID, STAGE_ID, "1 hour");

      const result = await applySweep();
      assert.ok(
        result.section_claims_released >= 1,
        `expected ≥1 section release, got ${result.section_claims_released}`,
      );
      assert.ok(
        result.stage_claims_released >= 1,
        `expected ≥1 stage release, got ${result.stage_claims_released}`,
      );

      const sec = await readSectionRow(SLUG, SECTION_ID);
      const stg = await readStageRow(SLUG, STAGE_ID);
      assert.ok(sec !== null && stg !== null);
      assert.ok(
        sec.released_at !== null,
        "section row must be released after sweep",
      );
      assert.ok(
        stg.released_at !== null,
        "stage row must be released after sweep",
      );
    });
  });

  it("missing target — heartbeat without stage_id or section_id throws", async () => {
    if (!pool) return;

    await assert.rejects(
      () => applyHeartbeat({ slug: SLUG }),
      (err: { code?: string }) => err.code === "missing_target",
      "expected missing_target when neither stage_id nor section_id provided",
    );
  });
});
