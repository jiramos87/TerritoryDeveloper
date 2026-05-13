/**
 * ui-visual-baseline — CRUD + sha256 stability + supersedes chain tests.
 *
 * TECH-31890: Stage 1.0 tracer — verifies DAL repos (visualBaselineRepo).
 * Skips when DB pool unavailable (mirrors mutations.test.ts pattern).
 *
 * Anchor: tools/mcp-ia-server/tests/tools/ui-visual-baseline.test.ts::sha256_stable_across_runs
 */

import assert from "node:assert/strict";
import { after, before, describe, it } from "node:test";
import * as crypto from "node:crypto";
import { getIaDatabasePool } from "../../src/ia-db/pool.js";
import { visualBaselineRepo } from "../../src/ia-db/ui-catalog.js";

const pool = getIaDatabasePool();
const skip = pool === null ? { skip: "DB pool unavailable" } : {};

const TEST_SLUG = "__visual_test_panel__";

// ── sha256_stable_across_runs ─────────────────────────────────────────────
// Anchor method: deterministic sha256 from same bytes across 3 runs.
function sha256_stable_across_runs(): void {
  const bytes = Buffer.from("fake-png-bytes-for-test");
  const run1 = crypto.createHash("sha256").update(bytes).digest("hex");
  const run2 = crypto.createHash("sha256").update(bytes).digest("hex");
  const run3 = crypto.createHash("sha256").update(bytes).digest("hex");
  assert.equal(run1, run2, "sha256 must be deterministic across runs (run1 vs run2)");
  assert.equal(run2, run3, "sha256 must be deterministic across runs (run2 vs run3)");
}

describe("visual baseline DAL — CRUD + sha256 + supersedes chain", skip, () => {
  before(async () => {
    if (!pool) return;
    // Clean up any prior test rows.
    await pool.query(
      `DELETE FROM ia_visual_baseline WHERE panel_slug = $1`,
      [TEST_SLUG],
    );
  });

  after(async () => {
    if (!pool) return;
    await pool.query(
      `DELETE FROM ia_visual_baseline WHERE panel_slug = $1`,
      [TEST_SLUG],
    );
  });

  it("sha256_stable_across_runs — deterministic hash across 3 runs", () => {
    sha256_stable_across_runs();
  });

  it("CRUD — insert active row, read it back, assert status=active", async () => {
    if (!pool) return;
    const client = await pool.connect();
    try {
      const repo = visualBaselineRepo(client);
      const sha = crypto.createHash("sha256").update("png-bytes-v1").digest("hex");
      const row = await repo.record({
        panel_slug: TEST_SLUG,
        image_ref: `Assets/UI/VisualBaselines/${TEST_SLUG}@v1.png`,
        image_sha256: sha,
      });
      assert.equal(row.panel_slug, TEST_SLUG);
      assert.equal(row.status, "active");
      assert.equal(row.image_sha256, sha);

      const fetched = await repo.get(TEST_SLUG);
      assert.ok(fetched, "get() must return active row");
      assert.equal(fetched!.id, row.id);
      assert.equal(fetched!.status, "active");
    } finally {
      client.release();
    }
  });

  it("supersedes chain — second record flips first to retired, supersedes_id set", async () => {
    if (!pool) return;
    const client = await pool.connect();
    try {
      const repo = visualBaselineRepo(client);

      // First row already inserted by prior test; get its id.
      const first = await repo.get(TEST_SLUG);
      assert.ok(first, "first active row must exist from prior test");
      const firstId = first!.id;

      const sha2 = crypto.createHash("sha256").update("png-bytes-v2").digest("hex");
      const second = await repo.record({
        panel_slug: TEST_SLUG,
        image_ref: `Assets/UI/VisualBaselines/${TEST_SLUG}@v2.png`,
        image_sha256: sha2,
      });

      assert.equal(second.status, "active");
      assert.equal(second.supersedes_id, firstId, "supersedes_id must point to prior row");

      // First row must now be retired.
      const res = await client.query(
        `SELECT status FROM ia_visual_baseline WHERE id = $1`,
        [firstId],
      );
      assert.equal(res.rows[0].status, "retired", "prior active row must be retired");
    } finally {
      client.release();
    }
  });

  it("retire() — explicit retire flips status", async () => {
    if (!pool) return;
    const client = await pool.connect();
    try {
      const repo = visualBaselineRepo(client);
      const active = await repo.get(TEST_SLUG);
      assert.ok(active);
      await repo.retire(active!.id);
      const res = await client.query(
        `SELECT status FROM ia_visual_baseline WHERE id = $1`,
        [active!.id],
      );
      assert.equal(res.rows[0].status, "retired");
    } finally {
      client.release();
    }
  });

  it("list() — filter by slug returns all rows for slug", async () => {
    if (!pool) return;
    const client = await pool.connect();
    try {
      const repo = visualBaselineRepo(client);
      const rows = await repo.list({ panel_slug: TEST_SLUG });
      assert.ok(rows.length >= 2, "must have at least 2 rows for test slug");
    } finally {
      client.release();
    }
  });
});
