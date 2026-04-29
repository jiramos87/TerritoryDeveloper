/**
 * section_closeout E2E test — TECH-5070.
 *
 * Drives a sandbox 1-carcass + 2-section topology end-to-end through the
 * Stage 1.3 happy path: open section + stage claims, walk both section
 * stages pending → implemented → verified → done via
 * `mutateTaskStatusFlip` (per task) + `mutateStageCloseoutApply` (per
 * stage), then assert terminal DB state.
 *
 * Specifically asserts the V2 row-only release-timing rule from
 * `section-closeout-apply.ts` L137-147: stage-level closeout
 * (`mutateStageCloseoutApply`) does NOT release stage claims; releases
 * cascade only when section-level closeout (`applySectionCloseout`)
 * fires. TECH-5071 covers that cascade in its own sandbox.
 *
 * Sandbox slug `__test_section_closeout_e2e__` — disjoint per parallel
 * `node --test` worker. Fixture authored under
 * `tests/fixtures/section-closeout.fixture.ts` for reuse by TECH-5071.
 */

import assert from "node:assert/strict";
import { after, before, describe, it } from "node:test";
import { getIaDatabasePool } from "../../src/ia-db/pool.js";
import { applySectionClaim } from "../../src/tools/section-claim.js";
import { applyStageClaim } from "../../src/tools/stage-claim.js";
import {
  mutateStageCloseoutApply,
  mutateTaskStatusFlip,
} from "../../src/ia-db/mutations.js";
import {
  seedSectionCloseoutFixture,
  teardownSectionCloseoutFixture,
} from "../fixtures/section-closeout.fixture.js";

const pool = getIaDatabasePool();
const skip = pool === null ? { skip: "DB pool unavailable" } : {};

const SLUG = "__test_section_closeout_e2e__";
const SECTION_ID = "A";
const STAGE_A1 = "section.A.1";
const STAGE_A2 = "section.A.2";

interface ClaimRow {
  released_at: Date | null;
}

interface StageStatusRow {
  status: string;
}

async function readSectionRow(slug: string): Promise<ClaimRow | null> {
  if (!pool) return null;
  const res = await pool.query<ClaimRow>(
    `SELECT released_at
       FROM ia_section_claims
      WHERE slug = $1 AND section_id = $2`,
    [slug, SECTION_ID],
  );
  return res.rows[0] ?? null;
}

async function readStageRow(
  slug: string,
  stageId: string,
): Promise<ClaimRow | null> {
  if (!pool) return null;
  const res = await pool.query<ClaimRow>(
    `SELECT released_at
       FROM ia_stage_claims
      WHERE slug = $1 AND stage_id = $2`,
    [slug, stageId],
  );
  return res.rows[0] ?? null;
}

async function readStageStatus(
  slug: string,
  stageId: string,
): Promise<string | null> {
  if (!pool) return null;
  const res = await pool.query<StageStatusRow>(
    `SELECT status::text AS status
       FROM ia_stages
      WHERE slug = $1 AND stage_id = $2`,
    [slug, stageId],
  );
  return res.rows[0]?.status ?? null;
}

describe("section_closeout E2E (TECH-5070)", skip, () => {
  let taskA1: string;
  let taskA2: string;

  before(async () => {
    if (!pool) return;
    await teardownSectionCloseoutFixture(SLUG);
    const handle = await seedSectionCloseoutFixture(SLUG);
    [taskA1, taskA2] = handle.task_ids;
  });

  after(async () => {
    if (!pool) return;
    await teardownSectionCloseoutFixture(SLUG);
  });

  it("drive 2 section stages to done — section + stage claims stay open post-stage-closeout", async () => {
    if (!pool) return;

    // Open section claim, then per-stage claims (stage_claim asserts
    // section claim exists when stage carries section_id).
    await applySectionClaim({ slug: SLUG, section_id: SECTION_ID });
    await applyStageClaim({ slug: SLUG, stage_id: STAGE_A1 });
    await applyStageClaim({ slug: SLUG, stage_id: STAGE_A2 });

    // Walk each section stage's task pending → implemented → verified →
    // done, then close the stage. Order matters: stage closeout asserts
    // all tasks under (slug, stage_id) are done or archived.
    for (const [stageId, taskId] of [
      [STAGE_A1, taskA1],
      [STAGE_A2, taskA2],
    ] as const) {
      await mutateTaskStatusFlip(taskId, "implemented");
      await mutateTaskStatusFlip(taskId, "verified");
      await mutateTaskStatusFlip(taskId, "done");
      const closeout = await mutateStageCloseoutApply(SLUG, stageId);
      assert.equal(closeout.stage_status, "done");
      assert.equal(closeout.archived_task_count, 1);
    }

    // Both section stages must be terminal.
    assert.equal(await readStageStatus(SLUG, STAGE_A1), "done");
    assert.equal(await readStageStatus(SLUG, STAGE_A2), "done");

    // Section claim must still be open — release fires only on
    // section_closeout_apply, not stage closeout.
    const sec = await readSectionRow(SLUG);
    assert.ok(sec !== null, "section claim row must exist");
    assert.equal(
      sec.released_at,
      null,
      "section claim must stay open after stage closeouts",
    );

    // Stage claims must still be open for the same reason — cascade
    // release fires on section closeout, not stage closeout.
    const stg1 = await readStageRow(SLUG, STAGE_A1);
    const stg2 = await readStageRow(SLUG, STAGE_A2);
    assert.ok(stg1 !== null && stg2 !== null, "stage claim rows must exist");
    assert.equal(
      stg1.released_at,
      null,
      "stage A1 claim must stay open after stage closeout",
    );
    assert.equal(
      stg2.released_at,
      null,
      "stage A2 claim must stay open after stage closeout",
    );
  });
});
