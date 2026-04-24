/**
 * Mutation tests for ia_* tables (Step 4 of ia-dev-db-refactor).
 *
 * Skip when no IA DB pool is available (mirrors resolve-database-url
 * behaviour). When a dev pool is up, exercises:
 *
 *   - task_insert round-trip (insert → queryTaskState matches)
 *   - sequence monotonic across 5 parallel inserts (unique ids)
 *   - task_status_flip race: two parallel calls on same row serialise
 *   - task_spec_section_write appends → body updates + history row
 *   - journal_append → row visible via pool query
 *   - fix_plan_write + fix_plan_consume lifecycle
 *
 * Uses sandbox slug `__test_sandbox__` with a dedicated throwaway stage to
 * keep the real master-plan + ia_tasks tables untouched. Cleanup in
 * `after()` removes everything created under the sandbox slug.
 */

import assert from "node:assert/strict";
import { after, before, describe, it } from "node:test";
import { getIaDatabasePool } from "../../src/ia-db/pool.js";
import {
  mutateFixPlanConsume,
  mutateFixPlanWrite,
  mutateJournalAppend,
  mutateTaskInsert,
  mutateTaskSpecSectionWrite,
  mutateTaskStatusFlip,
} from "../../src/ia-db/mutations.js";
import { queryTaskState } from "../../src/ia-db/queries.js";

const SANDBOX_SLUG = "__test_sandbox__";
const SANDBOX_STAGE = "t1";
const SANDBOX_SESSION = "test-session-step4";

const pool = getIaDatabasePool();
const skip = pool === null ? { skip: "DB pool unavailable" } : {};

describe("ia-db mutations (Step 4)", skip, () => {
  before(async () => {
    if (!pool) return;
    await pool.query(
      `INSERT INTO ia_master_plans (slug, title)
         VALUES ($1, 'sandbox test plan')
       ON CONFLICT (slug) DO NOTHING`,
      [SANDBOX_SLUG],
    );
    await pool.query(
      `INSERT INTO ia_stages (slug, stage_id, title, status)
         VALUES ($1, $2, 'sandbox stage', 'in_progress')
       ON CONFLICT (slug, stage_id) DO NOTHING`,
      [SANDBOX_SLUG, SANDBOX_STAGE],
    );
  });

  after(async () => {
    if (!pool) return;
    // FK cascade order: deps → commits → spec_history → fix tuples → tasks
    // → verifications → stages → journal → master_plans.
    await pool.query(
      `DELETE FROM ia_task_deps
        WHERE task_id IN (
          SELECT task_id FROM ia_tasks WHERE slug = $1
        )`,
      [SANDBOX_SLUG],
    );
    await pool.query(
      `DELETE FROM ia_task_commits
        WHERE task_id IN (SELECT task_id FROM ia_tasks WHERE slug = $1)`,
      [SANDBOX_SLUG],
    );
    await pool.query(
      `DELETE FROM ia_task_spec_history
        WHERE task_id IN (SELECT task_id FROM ia_tasks WHERE slug = $1)`,
      [SANDBOX_SLUG],
    );
    await pool.query(
      `DELETE FROM ia_fix_plan_tuples
        WHERE task_id IN (SELECT task_id FROM ia_tasks WHERE slug = $1)`,
      [SANDBOX_SLUG],
    );
    await pool.query(`DELETE FROM ia_tasks WHERE slug = $1`, [SANDBOX_SLUG]);
    await pool.query(
      `DELETE FROM ia_stage_verifications WHERE slug = $1`,
      [SANDBOX_SLUG],
    );
    await pool.query(`DELETE FROM ia_stages WHERE slug = $1`, [SANDBOX_SLUG]);
    await pool.query(
      `DELETE FROM ia_ship_stage_journal WHERE session_id = $1`,
      [SANDBOX_SESSION],
    );
    await pool.query(`DELETE FROM ia_master_plans WHERE slug = $1`, [
      SANDBOX_SLUG,
    ]);
  });

  it("task_insert round-trip: insert → queryTaskState reads same fields", async () => {
    const r = await mutateTaskInsert({
      prefix: "TECH",
      slug: SANDBOX_SLUG,
      stage_id: SANDBOX_STAGE,
      title: "sandbox round-trip task",
      body: "# sandbox body\n\nhello\n",
      priority: "high",
      notes: "smoke",
    });
    assert.ok(r.task_id.startsWith("TECH-"));
    const row = await queryTaskState(r.task_id);
    assert.ok(row);
    assert.equal(row!.title, "sandbox round-trip task");
    assert.equal(row!.slug, SANDBOX_SLUG);
    assert.equal(row!.stage_id, SANDBOX_STAGE);
    assert.equal(row!.priority, "high");
    assert.equal(row!.status, "pending");
  });

  it("task_insert parallel x5 → 5 unique monotonic ids", async () => {
    const results = await Promise.all(
      Array.from({ length: 5 }).map((_, i) =>
        mutateTaskInsert({
          prefix: "TECH",
          slug: SANDBOX_SLUG,
          stage_id: SANDBOX_STAGE,
          title: `parallel-insert-${i}`,
        }),
      ),
    );
    const ids = new Set(results.map((r) => r.task_id));
    assert.equal(ids.size, 5, "duplicate ids from parallel inserts");
    const nums = results
      .map((r) => parseInt(r.task_id.replace("TECH-", ""), 10))
      .sort((a, b) => a - b);
    for (let i = 1; i < nums.length; i++) {
      assert.ok(
        nums[i]! > nums[i - 1]!,
        `ids not monotonic: ${nums.join(", ")}`,
      );
    }
  });

  it("task_status_flip parallel on same row → row-level lock serialises", async () => {
    const ins = await mutateTaskInsert({
      prefix: "TECH",
      slug: SANDBOX_SLUG,
      stage_id: SANDBOX_STAGE,
      title: "concurrent flip target",
    });
    const id = ins.task_id;
    const [a, b] = await Promise.all([
      mutateTaskStatusFlip(id, "implemented"),
      mutateTaskStatusFlip(id, "verified"),
    ]);
    // Both completed without error; final state is one of the two.
    const row = await queryTaskState(id);
    assert.ok(row);
    assert.ok(
      row!.status === "implemented" || row!.status === "verified",
      `unexpected final status: ${row!.status}`,
    );
    assert.equal(a.task_id, id);
    assert.equal(b.task_id, id);
  });

  it("task_spec_section_write replaces existing section + writes history row", async () => {
    const ins = await mutateTaskInsert({
      prefix: "TECH",
      slug: SANDBOX_SLUG,
      stage_id: SANDBOX_STAGE,
      title: "section write target",
      body: "# Top\n\nintro\n\n## Plan Digest\n\nold plan\n\n## Notes\n\nn\n",
    });
    const res = await mutateTaskSpecSectionWrite(
      ins.task_id,
      "Plan Digest",
      "## Plan Digest\n\nnew plan v2\n",
      { actor: "test", change_reason: "round-trip" },
    );
    assert.ok(res.history_id > 0);
    const row = await queryTaskState(ins.task_id);
    assert.ok(row);
    const bodyRes = await pool!.query<{ body: string }>(
      `SELECT body FROM ia_tasks WHERE task_id = $1`,
      [ins.task_id],
    );
    const newBody = bodyRes.rows[0]!.body;
    assert.ok(newBody.includes("new plan v2"));
    assert.ok(!newBody.includes("old plan"));
    assert.ok(newBody.includes("## Notes"));
    const hist = await pool!.query<{ body: string }>(
      `SELECT body FROM ia_task_spec_history WHERE task_id = $1`,
      [ins.task_id],
    );
    assert.ok(hist.rowCount! >= 1);
    assert.ok(hist.rows[0]!.body.includes("old plan"));
  });

  it("task_spec_section_write appends when section missing", async () => {
    const ins = await mutateTaskInsert({
      prefix: "TECH",
      slug: SANDBOX_SLUG,
      stage_id: SANDBOX_STAGE,
      title: "section append target",
      body: "# Top\n\nintro\n",
    });
    await mutateTaskSpecSectionWrite(
      ins.task_id,
      "Plan Digest",
      "## Plan Digest\n\nfresh\n",
    );
    const bodyRes = await pool!.query<{ body: string }>(
      `SELECT body FROM ia_tasks WHERE task_id = $1`,
      [ins.task_id],
    );
    assert.ok(bodyRes.rows[0]!.body.includes("## Plan Digest"));
    assert.ok(bodyRes.rows[0]!.body.includes("fresh"));
  });

  it("journal_append round-trip: row visible via pool query", async () => {
    const r = await mutateJournalAppend({
      session_id: SANDBOX_SESSION,
      phase: "pass_a.implement",
      payload_kind: "tool_call",
      payload: { tool: "task_insert", ok: true, n: 3 },
    });
    assert.ok(r.id > 0);
    const rows = await pool!.query<{
      payload_kind: string;
      payload: Record<string, unknown>;
    }>(
      `SELECT payload_kind, payload
         FROM ia_ship_stage_journal
        WHERE id = $1`,
      [r.id],
    );
    assert.equal(rows.rowCount, 1);
    assert.equal(rows.rows[0]!.payload_kind, "tool_call");
    assert.equal((rows.rows[0]!.payload as { ok: boolean }).ok, true);
  });

  it("fix_plan_write + fix_plan_consume lifecycle", async () => {
    const ins = await mutateTaskInsert({
      prefix: "TECH",
      slug: SANDBOX_SLUG,
      stage_id: SANDBOX_STAGE,
      title: "fix plan target",
    });
    const w = await mutateFixPlanWrite(ins.task_id, 0, [
      { op: "replace", anchor: "foo", payload: "bar" },
      { op: "insert", anchor: "baz", payload: "qux" },
    ]);
    assert.equal(w.written, 2);
    const before = await pool!.query<{ n: string }>(
      `SELECT COUNT(*)::text AS n
         FROM ia_fix_plan_tuples
        WHERE task_id = $1 AND round = 0 AND applied_at IS NULL`,
      [ins.task_id],
    );
    assert.equal(before.rows[0]!.n, "2");
    const c = await mutateFixPlanConsume(ins.task_id, 0);
    assert.equal(c.consumed, 2);
    const after = await pool!.query<{ n: string }>(
      `SELECT COUNT(*)::text AS n
         FROM ia_fix_plan_tuples
        WHERE task_id = $1 AND round = 0 AND applied_at IS NULL`,
      [ins.task_id],
    );
    assert.equal(after.rows[0]!.n, "0");
    // Re-consume idempotent: 0.
    const again = await mutateFixPlanConsume(ins.task_id, 0);
    assert.equal(again.consumed, 0);
  });
});
