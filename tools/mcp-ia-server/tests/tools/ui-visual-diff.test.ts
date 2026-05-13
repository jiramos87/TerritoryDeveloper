/**
 * ui-visual-diff — verdict enum + region mask zeroing tests.
 *
 * TECH-31890: Stage 1.0 tracer — verifies visualDiffRepo.
 * Skips when DB pool unavailable.
 */

import assert from "node:assert/strict";
import { after, before, describe, it } from "node:test";
import * as crypto from "node:crypto";
import { getIaDatabasePool } from "../../src/ia-db/pool.js";
import { visualBaselineRepo, visualDiffRepo } from "../../src/ia-db/ui-catalog.js";

const pool = getIaDatabasePool();
const skip = pool === null ? { skip: "DB pool unavailable" } : {};

const DIFF_TEST_SLUG = "__visual_diff_test__";
const VALID_VERDICTS = new Set(["match", "regression", "new_baseline_needed"]);

describe("visual diff DAL — verdict enum + region mask", skip, () => {
  let baselineId: string;

  before(async () => {
    if (!pool) return;
    await pool.query(
      `DELETE FROM ia_visual_baseline WHERE panel_slug = $1`,
      [DIFF_TEST_SLUG],
    );
    const client = await pool.connect();
    try {
      const repo = visualBaselineRepo(client);
      const sha = crypto.createHash("sha256").update("diff-test-bytes").digest("hex");
      const row = await repo.record({
        panel_slug: DIFF_TEST_SLUG,
        image_ref: `Assets/UI/VisualBaselines/${DIFF_TEST_SLUG}@v1.png`,
        image_sha256: sha,
      });
      baselineId = row.id;
    } finally {
      client.release();
    }
  });

  after(async () => {
    if (!pool) return;
    // Diff rows cascade-deleted via FK.
    await pool.query(
      `DELETE FROM ia_visual_baseline WHERE panel_slug = $1`,
      [DIFF_TEST_SLUG],
    );
  });

  it("verdict enum — inserted verdict is strictly one of {match, regression, new_baseline_needed}", async () => {
    if (!pool) return;
    const client = await pool.connect();
    try {
      const diffRepo = visualDiffRepo(client);
      const hash = crypto.createHash("sha256").update("candidate-bytes").digest("hex");
      const row = await diffRepo.run({
        baseline_id: baselineId,
        candidate_hash: hash,
        diff_pct: 0.001,
        verdict: "regression",
      });
      assert.ok(
        VALID_VERDICTS.has(row.verdict),
        `verdict must be one of ${[...VALID_VERDICTS].join("|")} but got ${row.verdict}`,
      );
      assert.equal(row.verdict, "regression");
      assert.equal(row.baseline_id, baselineId);
    } finally {
      client.release();
    }
  });

  it("region_map zeroing — region_map stored + retrieved as jsonb", async () => {
    if (!pool) return;
    const client = await pool.connect();
    try {
      const diffRepo = visualDiffRepo(client);
      const hash = crypto.createHash("sha256").update("candidate-region").digest("hex");
      const regionMap = { exclude: [{ x: 0, y: 0, w: 100, h: 50 }] };
      const row = await diffRepo.run({
        baseline_id: baselineId,
        candidate_hash: hash,
        diff_pct: 0.0,
        verdict: "match",
        region_map: regionMap,
      });
      // Verify region_map stored as jsonb (DB returns parsed object).
      const res = await client.query(
        `SELECT region_map FROM ia_visual_diff WHERE id = $1`,
        [row.id],
      );
      const stored = res.rows[0].region_map;
      assert.ok(stored, "region_map must be stored");
      assert.deepEqual(stored, regionMap, "region_map must round-trip correctly");
    } finally {
      client.release();
    }
  });

  it("getLatest — returns most recent diff row for baseline", async () => {
    if (!pool) return;
    const client = await pool.connect();
    try {
      const diffRepo = visualDiffRepo(client);
      const latest = await diffRepo.getLatest(baselineId);
      assert.ok(latest, "getLatest must return a row");
      assert.equal(latest!.baseline_id, baselineId);
    } finally {
      client.release();
    }
  });
});
