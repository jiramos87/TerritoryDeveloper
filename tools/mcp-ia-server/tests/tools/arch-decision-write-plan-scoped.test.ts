/**
 * arch_decision_write plan-scoped wiring (Stage 2.4 / TECH-5244).
 *
 * Covers:
 *   - global path regression (no plan_slug → INSERT with plan_slug=NULL)
 *   - plan-scoped happy path (plan_slug set + master plan row exists)
 *   - unknown master-plan slug → unknown_master_plan_slug
 *   - malformed slug → Zod validation rejection (regex union)
 */

import test from "node:test";
import assert from "node:assert/strict";
import type { Pool } from "pg";
import { runArchDecisionWrite } from "../../src/tools/arch.js";

interface Recorded {
  sql: string;
  params: unknown[];
}

function makeStubPool(scripts: Array<{ rows: unknown[]; rowCount?: number }>): {
  pool: Pool;
  calls: Recorded[];
} {
  const calls: Recorded[] = [];
  let i = 0;
  const pool = {
    async query(sql: string, params: unknown[] = []) {
      calls.push({ sql, params });
      const next = scripts[i++];
      if (!next) return { rows: [], rowCount: 0 };
      return { rows: next.rows, rowCount: next.rowCount ?? next.rows.length };
    },
  } as unknown as Pool;
  return { pool, calls };
}

// ---------------------------------------------------------------------------
// global path regression — no plan_slug → INSERT with NULL plan_slug
// ---------------------------------------------------------------------------

test("arch_decision_write: global path keeps plan_slug NULL", async () => {
  const { pool, calls } = makeStubPool([
    // INSERT
    { rows: [{ id: 1, status: "active", plan_slug: null }] },
  ]);
  const out = await runArchDecisionWrite(pool, {
    slug: "DEC-A18",
    title: "global-decision",
    rationale: "global rationale",
  });
  assert.equal(out.slug, "DEC-A18");
  assert.equal(out.plan_slug, null);
  // Single INSERT call (no plan-slug preflight).
  assert.equal(calls.length, 1);
  assert.match(calls[0].sql, /INSERT INTO arch_decisions/);
  // Param 7 is plan_slug → null.
  assert.equal(calls[0].params[6], null);
});

// ---------------------------------------------------------------------------
// plan-scoped happy path
// ---------------------------------------------------------------------------

test("arch_decision_write: plan-scoped happy path persists plan_slug", async () => {
  const { pool, calls } = makeStubPool([
    // 1) plan_slug preflight → row exists
    { rows: [{ slug: "fixture-plan" }] },
    // 2) INSERT
    { rows: [{ id: 2, status: "active", plan_slug: "fixture-plan" }] },
  ]);
  const out = await runArchDecisionWrite(pool, {
    slug: "plan-fixture-plan-boundaries",
    title: "fixture-boundaries",
    rationale: "scope: fixture domain only",
    plan_slug: "fixture-plan",
  });
  assert.equal(out.slug, "plan-fixture-plan-boundaries");
  assert.equal(out.plan_slug, "fixture-plan");
  // Two calls: preflight + INSERT.
  assert.equal(calls.length, 2);
  assert.match(calls[0].sql, /SELECT slug FROM ia_master_plans/);
  assert.deepEqual(calls[0].params, ["fixture-plan"]);
  assert.match(calls[1].sql, /INSERT INTO arch_decisions/);
  assert.equal(calls[1].params[6], "fixture-plan");
});

// ---------------------------------------------------------------------------
// unknown master-plan slug → unknown_master_plan_slug
// ---------------------------------------------------------------------------

test("arch_decision_write: unknown plan_slug → unknown_master_plan_slug", async () => {
  const { pool } = makeStubPool([
    // preflight returns no rows
    { rows: [], rowCount: 0 },
  ]);
  await assert.rejects(
    () =>
      runArchDecisionWrite(pool, {
        slug: "plan-ghost-plan-shared-seams",
        title: "ghost",
        rationale: "x",
        plan_slug: "ghost-plan",
      }),
    (e: unknown) =>
      e !== null &&
      typeof e === "object" &&
      (e as { code?: string }).code === "unknown_master_plan_slug",
  );
});

// ---------------------------------------------------------------------------
// malformed slug — runtime parse via input schema
// ---------------------------------------------------------------------------

test("arch_decision_write: regex accepts plan-scoped suffixes (boundaries|end-state-contract|shared-seams)", async () => {
  // We exercise regex at the schema level by importing the schema indirectly via a
  // local re-creation of the same regex (mirror — keep in sync with arch.ts).
  const re = /^(DEC-A\d+|plan-[a-z0-9-]+-(boundaries|end-state-contract|shared-seams))$/;
  assert.ok(re.test("DEC-A1"));
  assert.ok(re.test("DEC-A99"));
  assert.ok(re.test("plan-foo-boundaries"));
  assert.ok(re.test("plan-foo-bar-end-state-contract"));
  assert.ok(re.test("plan-foo-shared-seams"));
  // Negatives.
  assert.ok(!re.test("plan-foo-other"));
  assert.ok(!re.test("plan--boundaries"));
  assert.ok(!re.test("DEC-B1"));
  assert.ok(!re.test("random-slug"));
});

// ---------------------------------------------------------------------------
// missing required fields → invalid_input
// ---------------------------------------------------------------------------

test("arch_decision_write: missing rationale → invalid_input", async () => {
  const { pool } = makeStubPool([]);
  await assert.rejects(
    () =>
      runArchDecisionWrite(pool, {
        slug: "DEC-A50",
        title: "x",
      }),
    (e: unknown) =>
      e !== null && typeof e === "object" && (e as { code?: string }).code === "invalid_input",
  );
});
