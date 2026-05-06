/**
 * Unit test — claim + flip round-trip on cron_audit_log_jobs fixture rows.
 *
 * Requires live DB (DATABASE_URL / postgres-dev.json).
 * Run: npx tsx --test tools/cron-server/lib/__tests__/claim-flip.test.ts
 */

import { strict as assert } from "node:assert";
import { describe, it, before, after } from "node:test";
import { getCronDbPool, claimBatch, markDone, markFailed } from "../index.js";

const TABLE = "cron_audit_log_jobs";

describe("claim-flip round-trip", () => {
  let jobId1: string;
  let jobId2: string;

  before(async () => {
    const pool = getCronDbPool();
    // Insert 2 fixture rows
    const r1 = await pool.query<{ job_id: string }>(
      `INSERT INTO ${TABLE} (slug, version, audit_kind, body)
       VALUES ('claim-flip-test', 1, 'test', '{}'::jsonb)
       RETURNING job_id`,
    );
    const r2 = await pool.query<{ job_id: string }>(
      `INSERT INTO ${TABLE} (slug, version, audit_kind, body)
       VALUES ('claim-flip-test', 1, 'test', '{}'::jsonb)
       RETURNING job_id`,
    );
    jobId1 = r1.rows[0]!.job_id;
    jobId2 = r2.rows[0]!.job_id;
  });

  it("claimBatch returns 2 rows and flips status to running", async () => {
    const rows = await claimBatch(TABLE, 10);
    const ids = rows.map((r) => r["job_id"]);
    assert.ok(ids.includes(jobId1), "should include jobId1");
    assert.ok(ids.includes(jobId2), "should include jobId2");
    for (const r of rows) {
      assert.equal(r["status"], "running");
    }
  });

  it("markDone flips first row to done", async () => {
    await markDone(TABLE, jobId1);
    const pool = getCronDbPool();
    const res = await pool.query<{ status: string }>(
      `SELECT status FROM ${TABLE} WHERE job_id = $1`,
      [jobId1],
    );
    assert.equal(res.rows[0]!.status, "done");
  });

  it("markFailed flips second row to failed", async () => {
    await markFailed(TABLE, jobId2, "fixture-error");
    const pool = getCronDbPool();
    const res = await pool.query<{ status: string; error: string }>(
      `SELECT status, error FROM ${TABLE} WHERE job_id = $1`,
      [jobId2],
    );
    assert.equal(res.rows[0]!.status, "failed");
    assert.equal(res.rows[0]!.error, "fixture-error");
  });

  after(async () => {
    // Cleanup fixture rows
    const pool = getCronDbPool();
    await pool.query(
      `DELETE FROM ${TABLE} WHERE job_id IN ($1, $2)`,
      [jobId1, jobId2],
    );
    await pool.end();
  });
});
