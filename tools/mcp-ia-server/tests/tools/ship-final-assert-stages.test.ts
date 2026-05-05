/**
 * ship-final assert-stages-done.sh — red-stage proof test (TECH-12643).
 *
 * Drives `tools/scripts/recipe-engine/ship-final/assert-stages-done.sh`
 * through happy + negative paths against a synthetic 2-stage sandbox plan.
 * Validates Phase 3 of `ship-final` recipe: the gate that refuses to close
 * a master-plan version while any stage status is non-`done` (including
 * the new `partial` enum value from migration 0069).
 *
 * Test matrix:
 *   - Negative: 1 stage `done` + 1 stage `partial` → exit 1, stderr
 *     contains `stages_not_done`, no stdout success line.
 *   - Happy: flip the partial stage to `done` → exit 0, stdout matches
 *     `stages_done=2 stages_total=2`.
 *
 * Sandbox slug `__test_ship_final_assert_stages__` — disjoint from other
 * fixture slugs so parallel `node --test` workers don't collide via
 * cascade DELETE.
 *
 * Stage status flips use raw `pool.query` UPDATE on `ia_stages.status`
 * because `assert-stages-done.sh` reads stage status directly from psql
 * and no helper exists for direct stage flip outside `stage_closeout_apply`.
 */

import test from "node:test";
import assert from "node:assert/strict";
import { spawn } from "node:child_process";
import { fileURLToPath } from "node:url";
import { join, resolve } from "node:path";
import { getIaDatabasePool } from "../../src/ia-db/pool.js";

const THIS_DIR = fileURLToPath(new URL(".", import.meta.url));
const REPO_ROOT = resolve(THIS_DIR, "../../../../");
const SCRIPT_PATH = join(
  REPO_ROOT,
  "tools/scripts/recipe-engine/ship-final/assert-stages-done.sh",
);

const SLUG = "__test_ship_final_assert_stages__";
const STAGE_DONE = "stage.A.1";
const STAGE_OPEN = "stage.A.2";

const pool = getIaDatabasePool();
const skip = pool === null ? { skip: "DB pool unavailable" } : {};

async function seedFixture(): Promise<void> {
  if (!pool) return;
  await teardownFixture();
  await pool.query(
    `INSERT INTO ia_master_plans (slug, title)
       VALUES ($1, 'sandbox ship-final assert-stages plan')
     ON CONFLICT (slug) DO NOTHING`,
    [SLUG],
  );
  await pool.query(
    `INSERT INTO ia_stages (slug, stage_id, title, status)
       VALUES ($1, $2, 'sandbox done stage', 'done')
     ON CONFLICT (slug, stage_id) DO UPDATE
       SET status = EXCLUDED.status`,
    [SLUG, STAGE_DONE],
  );
  await pool.query(
    `INSERT INTO ia_stages (slug, stage_id, title, status)
       VALUES ($1, $2, 'sandbox partial stage', 'partial')
     ON CONFLICT (slug, stage_id) DO UPDATE
       SET status = EXCLUDED.status`,
    [SLUG, STAGE_OPEN],
  );
}

async function teardownFixture(): Promise<void> {
  if (!pool) return;
  await pool.query(`DELETE FROM ia_tasks WHERE slug = $1`, [SLUG]);
  await pool.query(`DELETE FROM ia_master_plans WHERE slug = $1`, [SLUG]);
}

async function flipStageDone(stageId: string): Promise<void> {
  if (!pool) return;
  await pool.query(
    `UPDATE ia_stages
        SET status = 'done'::stage_status, updated_at = now()
      WHERE slug = $1 AND stage_id = $2`,
    [SLUG, stageId],
  );
}

function runScript(): Promise<{
  stdout: string;
  stderr: string;
  exitCode: number;
}> {
  return new Promise((resolveRun) => {
    const child = spawn("bash", [SCRIPT_PATH, "--slug", SLUG], {
      cwd: REPO_ROOT,
      env: { ...process.env },
    });
    let stdout = "";
    let stderr = "";
    child.stdout.on("data", (c: Buffer) => (stdout += c.toString()));
    child.stderr.on("data", (c: Buffer) => (stderr += c.toString()));
    child.on("close", (code) =>
      resolveRun({ stdout, stderr, exitCode: code ?? 1 }),
    );
    child.on("error", (err) =>
      resolveRun({ stdout, stderr: stderr + err.message, exitCode: 1 }),
    );
  });
}

test("ship-final assert-stages-done: rejects partial stage (red proof)", skip, async () => {
  if (!pool) return;
  try {
    await seedFixture();
    const { stdout, stderr, exitCode } = await runScript();
    assert.equal(exitCode, 1, "must exit 1 when partial stage present");
    assert.match(
      stderr,
      /stages_not_done=/,
      "stderr must surface stages_not_done diagnostic",
    );
    assert.match(
      stderr,
      new RegExp(`${STAGE_OPEN}:partial`),
      "stages_not_done must enumerate the offending stage_id:status",
    );
    assert.doesNotMatch(
      stdout,
      /stages_done=/,
      "stdout must NOT emit success line on negative path",
    );
  } finally {
    await teardownFixture();
  }
});

test("ship-final assert-stages-done: passes when all done (green)", skip, async () => {
  if (!pool) return;
  try {
    await seedFixture();
    await flipStageDone(STAGE_OPEN);

    const { stdout, exitCode } = await runScript();
    assert.equal(exitCode, 0, "must exit 0 when all stages done");
    assert.match(
      stdout,
      /stages_done=2 stages_total=2/,
      "stdout must report 2-of-2 done",
    );
  } finally {
    await teardownFixture();
  }
});

test("ship-final assert-stages-done: errors on missing slug arg", async () => {
  // Non-DB test — bash arg parsing only.
  const child = spawn("bash", [SCRIPT_PATH], {
    cwd: REPO_ROOT,
    env: { ...process.env },
  });
  let stderr = "";
  await new Promise<void>((done) => {
    child.stderr.on("data", (c: Buffer) => (stderr += c.toString()));
    child.on("close", () => done());
    child.on("error", () => done());
  });
  assert.match(stderr, /missing --slug/);
});

test("ship-final assert-stages-done: errors on slug with zero stages", skip, async () => {
  if (!pool) return;
  try {
    // Seed plan WITHOUT any stages → version_close requires ≥1 stage.
    await teardownFixture();
    await pool.query(
      `INSERT INTO ia_master_plans (slug, title)
         VALUES ($1, 'empty sandbox')
       ON CONFLICT (slug) DO NOTHING`,
      [SLUG],
    );
    const { stderr, exitCode } = await runScript();
    assert.equal(exitCode, 1);
    assert.match(stderr, /no stages for slug=/);
  } finally {
    await teardownFixture();
  }
});
