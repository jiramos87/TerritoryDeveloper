/**
 * arch_surfaces_backfill MCP handler — unit tests (TECH-2978 / Stage 1).
 *
 * Drives `runArchSurfacesBackfill` against a stub `pg.Pool` that records query
 * calls + returns canned rows. No real DB / git required.
 *
 * Covers §Test Blueprint:
 *   - Confident single-match upsert (idempotent ON CONFLICT DO NOTHING).
 *   - Idempotent re-run inserts zero new rows.
 *   - Ambiguous candidates surface in polling (kind = "ambiguous").
 *   - None-eligible stages surface in polling (kind = "none-eligible").
 *   - Already-linked stage skipped silently.
 *   - Invariant guard: arch_surfaces row count unchanged.
 *   - Empty arch_surfaces → invalid_input.
 *   - Unknown plan_slug → invalid_input.
 *   - dry_run skips INSERT but still counts confident links.
 */

import test from "node:test";
import assert from "node:assert/strict";
import type { Pool } from "pg";
import { runArchSurfacesBackfill } from "../../src/tools/arch-surfaces.js";

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
// Empty arch_surfaces → invalid_input
// ---------------------------------------------------------------------------

test("arch_surfaces_backfill: empty arch_surfaces → invalid_input", async () => {
  const { pool } = makeStubPool([{ rows: [] }]);
  await assert.rejects(
    () => runArchSurfacesBackfill(pool, {}),
    (e: unknown) =>
      e !== null &&
      typeof e === "object" &&
      (e as { code?: string }).code === "invalid_input" &&
      ((e as { message?: string }).message ?? "").includes("no rows in arch_surfaces"),
  );
});

// ---------------------------------------------------------------------------
// Unknown plan_slug → invalid_input
// ---------------------------------------------------------------------------

test("arch_surfaces_backfill: unknown plan_slug → invalid_input", async () => {
  const { pool } = makeStubPool([
    { rows: [{ slug: "surface/a", kind: "spec", spec_path: "ia/specs/a.md", spec_section: null }] },
    { rows: [], rowCount: 0 },
  ]);
  await assert.rejects(
    () => runArchSurfacesBackfill(pool, { plan_slug: "ghost-plan" }),
    (e: unknown) =>
      e !== null &&
      typeof e === "object" &&
      (e as { code?: string }).code === "invalid_input" &&
      ((e as { message?: string }).message ?? "").includes("no master plan found for slug=ghost-plan"),
  );
});

// ---------------------------------------------------------------------------
// Confident single-match — INSERT path
// ---------------------------------------------------------------------------

test("arch_surfaces_backfill: confident single-match upserts + counts confident_links", async () => {
  const { pool, calls } = makeStubPool([
    // 1) load arch_surfaces inventory
    {
      rows: [
        { slug: "surface/a", kind: "spec", spec_path: "ia/specs/a.md", spec_section: null },
        { slug: "surface/b", kind: "spec", spec_path: "ia/specs/b.md", spec_section: null },
      ],
    },
    // 2) load plans
    { rows: [{ slug: "plan-x" }] },
    // 3) load stages for plan-x — body mentions surface/a only
    {
      rows: [
        {
          stage_id: "1",
          body: "Stage 1 work touches ia/specs/a.md only.",
          objective: "",
          exit_criteria: "",
        },
      ],
    },
    // 4) load existing stage_arch_surfaces for (plan-x, 1)
    { rows: [] },
    // 5) INSERT confident match
    { rows: [], rowCount: 1 },
    // 6) post invariant count guard
    { rows: [{ n: 2 }] },
  ]);

  const result = await runArchSurfacesBackfill(pool, {});

  assert.equal(result.dry_run, false);
  assert.equal(result.plan_scope, null);
  assert.equal(result.stages_walked, 1);
  assert.equal(result.confident_links, 1);
  assert.equal(result.ambiguous_count, 0);
  assert.equal(result.none_eligible_count, 0);
  assert.equal(result.polling.length, 0);

  const insertCall = calls.find((c) => c.sql.includes("INSERT INTO stage_arch_surfaces"));
  assert.ok(insertCall, "expected INSERT into stage_arch_surfaces");
  assert.deepEqual(insertCall!.params, ["plan-x", "1", "surface/a"]);
});

// ---------------------------------------------------------------------------
// dry_run path — skips INSERT but counts
// ---------------------------------------------------------------------------

test("arch_surfaces_backfill: dry_run skips INSERT but counts confident_links", async () => {
  const { pool, calls } = makeStubPool([
    {
      rows: [{ slug: "surface/a", kind: "spec", spec_path: "ia/specs/a.md", spec_section: null }],
    },
    { rows: [{ slug: "plan-x" }] },
    {
      rows: [
        { stage_id: "1", body: "touches ia/specs/a.md", objective: "", exit_criteria: "" },
      ],
    },
    { rows: [] },
    // NOTE: no INSERT row in scripts — dry_run must NOT issue INSERT
    { rows: [{ n: 1 }] },
  ]);

  const result = await runArchSurfacesBackfill(pool, { dry_run: true });
  assert.equal(result.dry_run, true);
  assert.equal(result.confident_links, 1);

  const insertCall = calls.find((c) => c.sql.includes("INSERT INTO stage_arch_surfaces"));
  assert.equal(insertCall, undefined, "dry_run must not INSERT");
});

// ---------------------------------------------------------------------------
// Already-linked stage — silent skip
// ---------------------------------------------------------------------------

test("arch_surfaces_backfill: already-linked stage skipped silently", async () => {
  const { pool, calls } = makeStubPool([
    {
      rows: [{ slug: "surface/a", kind: "spec", spec_path: "ia/specs/a.md", spec_section: null }],
    },
    { rows: [{ slug: "plan-x" }] },
    {
      rows: [{ stage_id: "1", body: "ia/specs/a.md mention", objective: "", exit_criteria: "" }],
    },
    // already linked — surface/a present in stage_arch_surfaces
    { rows: [{ surface_slug: "surface/a" }] },
    { rows: [{ n: 1 }] },
  ]);

  const result = await runArchSurfacesBackfill(pool, {});
  assert.equal(result.confident_links, 0);
  assert.equal(result.ambiguous_count, 0);
  assert.equal(result.none_eligible_count, 0);

  const insertCall = calls.find((c) => c.sql.includes("INSERT INTO stage_arch_surfaces"));
  assert.equal(insertCall, undefined);
});

// ---------------------------------------------------------------------------
// Ambiguous candidates — polling row, no INSERT
// ---------------------------------------------------------------------------

test("arch_surfaces_backfill: ambiguous candidates surfaced via polling[]", async () => {
  const { pool, calls } = makeStubPool([
    {
      rows: [
        { slug: "surface/a", kind: "spec", spec_path: "ia/specs/a.md", spec_section: null },
        { slug: "surface/b", kind: "spec", spec_path: "ia/specs/b.md", spec_section: null },
      ],
    },
    { rows: [{ slug: "plan-x" }] },
    {
      rows: [
        {
          stage_id: "1",
          body: "mentions ia/specs/a.md AND ia/specs/b.md both",
          objective: "",
          exit_criteria: "",
        },
      ],
    },
    { rows: [] },
    { rows: [{ n: 2 }] },
  ]);

  const result = await runArchSurfacesBackfill(pool, {});
  assert.equal(result.confident_links, 0);
  assert.equal(result.ambiguous_count, 1);
  assert.equal(result.polling.length, 1);
  assert.equal(result.polling[0]!.kind, "ambiguous");
  assert.deepEqual(result.polling[0]!.candidates.sort(), ["surface/a", "surface/b"]);

  const insertCall = calls.find((c) => c.sql.includes("INSERT INTO stage_arch_surfaces"));
  assert.equal(insertCall, undefined);
});

// ---------------------------------------------------------------------------
// None-eligible stage — polling row kind = "none-eligible"
// ---------------------------------------------------------------------------

test("arch_surfaces_backfill: none-eligible stage surfaced in polling[]", async () => {
  const { pool } = makeStubPool([
    {
      rows: [{ slug: "surface/a", kind: "spec", spec_path: "ia/specs/a.md", spec_section: null }],
    },
    { rows: [{ slug: "plan-x" }] },
    {
      rows: [
        { stage_id: "1", body: "no surface refs at all", objective: "", exit_criteria: "" },
      ],
    },
    { rows: [] },
    { rows: [{ n: 1 }] },
  ]);

  const result = await runArchSurfacesBackfill(pool, {});
  assert.equal(result.confident_links, 0);
  assert.equal(result.none_eligible_count, 1);
  assert.equal(result.polling.length, 1);
  assert.equal(result.polling[0]!.kind, "none-eligible");
  assert.deepEqual(result.polling[0]!.candidates, []);
});

// ---------------------------------------------------------------------------
// Invariant guard — arch_surfaces row count must NEVER change
// ---------------------------------------------------------------------------

test("arch_surfaces_backfill: row count drift trips invariant_violation", async () => {
  const { pool } = makeStubPool([
    {
      rows: [{ slug: "surface/a", kind: "spec", spec_path: "ia/specs/a.md", spec_section: null }],
    },
    { rows: [{ slug: "plan-x" }] },
    {
      rows: [{ stage_id: "1", body: "no match", objective: "", exit_criteria: "" }],
    },
    { rows: [] },
    // post-count returns 99 — drift
    { rows: [{ n: 99 }] },
  ]);

  await assert.rejects(
    () => runArchSurfacesBackfill(pool, {}),
    (e: unknown) =>
      e !== null &&
      typeof e === "object" &&
      (e as { code?: string }).code === "invariant_violation",
  );
});

// ---------------------------------------------------------------------------
// plan_slug filter — scopes to one plan
// ---------------------------------------------------------------------------

test("arch_surfaces_backfill: plan_slug scopes plan query + result.plan_scope set", async () => {
  const { pool, calls } = makeStubPool([
    {
      rows: [{ slug: "surface/a", kind: "spec", spec_path: "ia/specs/a.md", spec_section: null }],
    },
    { rows: [{ slug: "plan-x" }] },
    {
      rows: [
        { stage_id: "1", body: "ia/specs/a.md", objective: "", exit_criteria: "" },
      ],
    },
    { rows: [] },
    { rows: [], rowCount: 1 },
    { rows: [{ n: 1 }] },
  ]);

  const result = await runArchSurfacesBackfill(pool, { plan_slug: "plan-x" });
  assert.equal(result.plan_scope, "plan-x");
  assert.equal(result.confident_links, 1);

  const planQuery = calls.find((c) => c.sql.includes("FROM ia_master_plans WHERE slug"));
  assert.ok(planQuery, "expected scoped plan query");
  assert.deepEqual(planQuery!.params, ["plan-x"]);
});
