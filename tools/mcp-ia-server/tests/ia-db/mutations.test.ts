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
  mutateMasterPlanChangeLogAppend,
  mutateStageCloseoutApply,
  mutateTaskDepRegister,
  mutateTaskInsert,
  mutateTaskRawMarkdownWrite,
  mutateTaskSpecSectionWrite,
  mutateTaskStatusFlip,
} from "../../src/ia-db/mutations.js";
import {
  queryStageCloseoutDiagnose,
  queryTaskState,
} from "../../src/ia-db/queries.js";

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
    // Closeout audit rows use session ids `closeout-<slug>-<stage_id>`.
    await pool.query(
      `DELETE FROM ia_ship_stage_journal WHERE slug = $1`,
      [SANDBOX_SLUG],
    );
    await pool.query(
      `DELETE FROM ia_master_plan_change_log WHERE slug = $1`,
      [SANDBOX_SLUG],
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

  it("task_raw_markdown_write persists body + idempotent re-write", async () => {
    const ins = await mutateTaskInsert({
      prefix: "TECH",
      slug: SANDBOX_SLUG,
      stage_id: SANDBOX_STAGE,
      title: "raw_markdown write target",
    });
    const row1 =
      `- [ ] **TECH-${ins.task_id.replace("TECH-", "")}** ${"smoke row block"}\n` +
      `  - Files: foo.ts\n  - Notes: bar\n`;
    const r1 = await mutateTaskRawMarkdownWrite(ins.task_id, row1);
    assert.equal(r1.task_id, ins.task_id);
    assert.equal(r1.bytes_written, Buffer.byteLength(row1, "utf8"));
    const q1 = await pool!.query<{ raw_markdown: string }>(
      `SELECT raw_markdown FROM ia_tasks WHERE task_id = $1`,
      [ins.task_id],
    );
    assert.equal(q1.rows[0]!.raw_markdown, row1);
    // Idempotent overwrite with new body.
    const row2 = row1 + "  - Extra: baz\n";
    const r2 = await mutateTaskRawMarkdownWrite(ins.task_id, row2);
    assert.equal(r2.bytes_written, Buffer.byteLength(row2, "utf8"));
    const q2 = await pool!.query<{ raw_markdown: string }>(
      `SELECT raw_markdown FROM ia_tasks WHERE task_id = $1`,
      [ins.task_id],
    );
    assert.equal(q2.rows[0]!.raw_markdown, row2);
    // Empty string normalised to "" (not null).
    const r3 = await mutateTaskRawMarkdownWrite(ins.task_id, "");
    assert.equal(r3.bytes_written, 0);
    const q3 = await pool!.query<{ raw_markdown: string | null }>(
      `SELECT raw_markdown FROM ia_tasks WHERE task_id = $1`,
      [ins.task_id],
    );
    assert.equal(q3.rows[0]!.raw_markdown, "");
  });

  it("task_raw_markdown_write rejects unknown task_id", async () => {
    await assert.rejects(
      () => mutateTaskRawMarkdownWrite("TECH-999999999", "x"),
      /task not found/,
    );
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

  it("master_plan_change_log_append dedups on (slug, stage_id, kind, commit_sha) UNIQUE", async () => {
    const sha = `dedup-test-${Date.now().toString(36)}`;
    const r1 = await mutateMasterPlanChangeLogAppend(
      SANDBOX_SLUG,
      "smoke-dedup",
      "first append",
      { commit_sha: sha, stage_id: SANDBOX_STAGE },
    );
    assert.equal(r1.deduped, false);
    assert.ok(r1.entry_id! > 0);
    const r2 = await mutateMasterPlanChangeLogAppend(
      SANDBOX_SLUG,
      "smoke-dedup",
      "second append (same key)",
      { commit_sha: sha, stage_id: SANDBOX_STAGE },
    );
    assert.equal(r2.deduped, true);
    assert.equal(r2.entry_id, null);
    // Different stage_id → distinct row.
    const r3 = await mutateMasterPlanChangeLogAppend(
      SANDBOX_SLUG,
      "smoke-dedup",
      "different stage",
      { commit_sha: sha, stage_id: "t2" },
    );
    assert.equal(r3.deduped, false);
    assert.ok(r3.entry_id! > 0);
    // NULL stage_id is distinct from any non-null stage_id.
    const r4 = await mutateMasterPlanChangeLogAppend(
      SANDBOX_SLUG,
      "smoke-dedup",
      "null stage",
      { commit_sha: sha },
    );
    assert.equal(r4.deduped, false);
    assert.ok(r4.entry_id! > 0);
    // Cleanup test fixture stage `t2` (sandbox `t1` cleanup is in after()).
    // No-op — change_log cleanup happens in after() too.
  });

  it("stage_closeout_apply happy path → 4-step audit trail readable by diagnose", async () => {
    // Dedicated stage so the closeout flip does not collide with other tests.
    const stage = "closeout-ok";
    await pool!.query(
      `INSERT INTO ia_stages (slug, stage_id, title, status)
         VALUES ($1, $2, 'closeout ok stage', 'in_progress')
       ON CONFLICT (slug, stage_id) DO NOTHING`,
      [SANDBOX_SLUG, stage],
    );
    // One done task so archive_done_tasks has work to do (count > 0).
    const t = await mutateTaskInsert({
      prefix: "TECH",
      slug: SANDBOX_SLUG,
      stage_id: stage,
      title: "closeout target task",
    });
    // Walk pending → implemented → verified → done.
    await mutateTaskStatusFlip(t.task_id, "implemented");
    await mutateTaskStatusFlip(t.task_id, "verified");
    await mutateTaskStatusFlip(t.task_id, "done");

    const r = await mutateStageCloseoutApply(SANDBOX_SLUG, stage);
    assert.equal(r.stage_status, "done");
    assert.equal(r.archived_task_count, 1);

    const trail = await queryStageCloseoutDiagnose(SANDBOX_SLUG, stage);
    assert.equal(trail.length, 4);
    assert.deepEqual(
      trail.map((s) => s.step_name),
      ["stage_lock", "non_terminal_check", "archive_done_tasks", "stage_status_done"],
    );
    assert.ok(trail.every((s) => s.ok === true));
    assert.ok(trail.every((s) => s.error === null));
    // ts ordered ASC.
    for (let i = 1; i < trail.length; i++) {
      assert.ok(
        new Date(trail[i]!.ts).getTime() >= new Date(trail[i - 1]!.ts).getTime(),
      );
    }
  });

  it("stage_closeout_apply non_terminal_check failure → audit shows failed step", async () => {
    const stage = "closeout-fail";
    await pool!.query(
      `INSERT INTO ia_stages (slug, stage_id, title, status)
         VALUES ($1, $2, 'closeout fail stage', 'in_progress')
       ON CONFLICT (slug, stage_id) DO NOTHING`,
      [SANDBOX_SLUG, stage],
    );
    // Pending task → guard rejects closeout at non_terminal_check.
    await mutateTaskInsert({
      prefix: "TECH",
      slug: SANDBOX_SLUG,
      stage_id: stage,
      title: "blocking pending task",
    });

    await assert.rejects(
      () => mutateStageCloseoutApply(SANDBOX_SLUG, stage),
      /non-terminal tasks/,
    );

    // State unchanged → stage still in_progress.
    const sr = await pool!.query<{ status: string }>(
      `SELECT status::text AS status FROM ia_stages WHERE slug = $1 AND stage_id = $2`,
      [SANDBOX_SLUG, stage],
    );
    assert.equal(sr.rows[0]!.status, "in_progress");

    // Audit trail shows stage_lock OK, then non_terminal_check failed.
    const trail = await queryStageCloseoutDiagnose(SANDBOX_SLUG, stage);
    assert.ok(trail.length >= 2);
    assert.equal(trail[0]!.step_name, "stage_lock");
    assert.equal(trail[0]!.ok, true);
    const failRow = trail.find((s) => s.ok === false);
    assert.ok(failRow, "expected at least one failure row");
    assert.equal(failRow!.step_name, "non_terminal_check");
    assert.ok(failRow!.error && /non-terminal tasks/.test(failRow!.error));
  });

  it("stage_closeout_diagnose returns empty array for legacy stage with no audit rows", async () => {
    const stage = "closeout-legacy";
    await pool!.query(
      `INSERT INTO ia_stages (slug, stage_id, title, status)
         VALUES ($1, $2, 'legacy stage', 'in_progress')
       ON CONFLICT (slug, stage_id) DO NOTHING`,
      [SANDBOX_SLUG, stage],
    );
    const trail = await queryStageCloseoutDiagnose(SANDBOX_SLUG, stage);
    assert.deepEqual(trail, []);
  });

  it("task_dep_register clean linear chain A→B→C → edges_added=2 then idempotent", async () => {
    const a = await mutateTaskInsert({
      prefix: "TECH",
      slug: SANDBOX_SLUG,
      stage_id: SANDBOX_STAGE,
      title: "dep chain A",
    });
    const b = await mutateTaskInsert({
      prefix: "TECH",
      slug: SANDBOX_SLUG,
      stage_id: SANDBOX_STAGE,
      title: "dep chain B",
    });
    const c = await mutateTaskInsert({
      prefix: "TECH",
      slug: SANDBOX_SLUG,
      stage_id: SANDBOX_STAGE,
      title: "dep chain C",
    });
    // A → B and B → C inserted in two calls; total edges_added = 2.
    const r1 = await mutateTaskDepRegister(a.task_id, [b.task_id]);
    assert.equal(r1.ok, true);
    if (r1.ok) assert.equal(r1.edges_added, 1);
    const r2 = await mutateTaskDepRegister(b.task_id, [c.task_id]);
    assert.equal(r2.ok, true);
    if (r2.ok) assert.equal(r2.edges_added, 1);

    // Idempotent re-register: same edge yields edges_added=0.
    const r3 = await mutateTaskDepRegister(a.task_id, [b.task_id]);
    assert.equal(r3.ok, true);
    if (r3.ok) assert.equal(r3.edges_added, 0);

    // Edge set persists.
    const edges = await pool!.query<{ task_id: string; depends_on_id: string }>(
      `SELECT task_id, depends_on_id
         FROM ia_task_deps
        WHERE kind = 'depends_on'
          AND task_id IN ($1, $2, $3)
        ORDER BY task_id, depends_on_id`,
      [a.task_id, b.task_id, c.task_id],
    );
    assert.equal(edges.rowCount, 2);
  });

  it("task_dep_register cycle-inducing edge → cycle_detected, rollback preserves edge set", async () => {
    const a = await mutateTaskInsert({
      prefix: "TECH",
      slug: SANDBOX_SLUG,
      stage_id: SANDBOX_STAGE,
      title: "cycle A",
    });
    const b = await mutateTaskInsert({
      prefix: "TECH",
      slug: SANDBOX_SLUG,
      stage_id: SANDBOX_STAGE,
      title: "cycle B",
    });
    const c = await mutateTaskInsert({
      prefix: "TECH",
      slug: SANDBOX_SLUG,
      stage_id: SANDBOX_STAGE,
      title: "cycle C",
    });
    // Build linear chain A → B → C.
    await mutateTaskDepRegister(a.task_id, [b.task_id]);
    await mutateTaskDepRegister(b.task_id, [c.task_id]);

    // Snapshot edge set pre-attempt.
    const pre = await pool!.query<{ task_id: string; depends_on_id: string }>(
      `SELECT task_id, depends_on_id
         FROM ia_task_deps
        WHERE kind = 'depends_on'
          AND task_id IN ($1, $2, $3)
        ORDER BY task_id, depends_on_id`,
      [a.task_id, b.task_id, c.task_id],
    );
    const preSet = pre.rows.map((r) => `${r.task_id}->${r.depends_on_id}`).sort();

    // C → A closes the cycle. Should be rejected.
    const r = await mutateTaskDepRegister(c.task_id, [a.task_id]);
    assert.equal(r.ok, false);
    if (!r.ok) {
      assert.equal(r.error.code, "cycle_detected");
      assert.deepEqual(
        r.error.scc_members.slice().sort(),
        [a.task_id, b.task_id, c.task_id].sort(),
      );
    }

    // Rollback verified: edge set unchanged from pre-attempt.
    const post = await pool!.query<{ task_id: string; depends_on_id: string }>(
      `SELECT task_id, depends_on_id
         FROM ia_task_deps
        WHERE kind = 'depends_on'
          AND task_id IN ($1, $2, $3)
        ORDER BY task_id, depends_on_id`,
      [a.task_id, b.task_id, c.task_id],
    );
    const postSet = post.rows.map((r) => `${r.task_id}->${r.depends_on_id}`).sort();
    assert.deepEqual(postSet, preSet);
  });

  it("task_dep_register self-reference rejected pre-Tarjan with scc_members=[task_id]", async () => {
    const a = await mutateTaskInsert({
      prefix: "TECH",
      slug: SANDBOX_SLUG,
      stage_id: SANDBOX_STAGE,
      title: "self-ref target",
    });
    const r = await mutateTaskDepRegister(a.task_id, [a.task_id]);
    assert.equal(r.ok, false);
    if (!r.ok) {
      assert.equal(r.error.code, "cycle_detected");
      assert.deepEqual(r.error.scc_members, [a.task_id]);
    }
    // No edge inserted.
    const rows = await pool!.query<{ n: string }>(
      `SELECT COUNT(*)::text AS n FROM ia_task_deps WHERE task_id = $1`,
      [a.task_id],
    );
    assert.equal(rows.rows[0]!.n, "0");
  });

  it("task_dep_register rejects unknown dep target", async () => {
    const a = await mutateTaskInsert({
      prefix: "TECH",
      slug: SANDBOX_SLUG,
      stage_id: SANDBOX_STAGE,
      title: "unknown-dep source",
    });
    await assert.rejects(
      () => mutateTaskDepRegister(a.task_id, ["TECH-999999998"]),
      /unknown dep targets/,
    );
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
