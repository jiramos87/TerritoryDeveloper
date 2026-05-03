/**
 * arch.ts MCP tools (Stage 1.3 of architecture-coherence-system) — unit tests.
 *
 * Drives `runArch*` handlers with a stub `pg.Pool` that records query calls
 * and returns canned rows. No real DB / git required.
 */

import test, { after, before, describe, it } from "node:test";
import assert from "node:assert/strict";
import type { Pool } from "pg";
import {
  runArchDecisionGet,
  runArchDecisionList,
  runArchSurfaceResolve,
  runArchDriftScan,
  runArchChangelogSince,
  runArchSurfaceWrite,
} from "../../src/tools/arch.js";
import { getIaDatabasePool } from "../../src/ia-db/pool.js";

interface Recorded {
  sql: string;
  params: unknown[];
}

function makeStubPool(scripts: Array<{ rows: unknown[] }>): {
  pool: Pool;
  calls: Recorded[];
} {
  const calls: Recorded[] = [];
  let i = 0;
  const pool = {
    async query(sql: string, params: unknown[] = []) {
      calls.push({ sql, params });
      const next = scripts[i++];
      if (!next) return { rows: [] };
      return next;
    },
  } as unknown as Pool;
  return { pool, calls };
}

// ---------------------------------------------------------------------------
// arch_decision_get
// ---------------------------------------------------------------------------

test("arch_decision_get: happy path returns row + maps surface_slug", async () => {
  const { pool } = makeStubPool([
    {
      rows: [
        {
          slug: "DEC-A12",
          title: "plan-arch-link-stage-level",
          status: "active",
          rationale: "Stage-level arch_surfaces[].",
          alternatives: "task-level link; JSONB column; tag string array",
          superseded_by: null,
          surface_slug: "decisions/all",
          created_at: "2026-04-27T10:00:00Z",
        },
      ],
    },
  ]);
  const out = await runArchDecisionGet(pool, { slug: "DEC-A12" });
  assert.equal(out.slug, "DEC-A12");
  assert.equal(out.surface_slug, "decisions/all");
  assert.equal(out.status, "active");
});

test("arch_decision_get: missing slug → invalid_input", async () => {
  const { pool } = makeStubPool([]);
  await assert.rejects(
    () => runArchDecisionGet(pool, { slug: "" }),
    (e: unknown) =>
      e !== null && typeof e === "object" && (e as { code?: string }).code === "invalid_input",
  );
});

test("arch_decision_get: unknown slug → decision_not_found", async () => {
  const { pool } = makeStubPool([{ rows: [] }]);
  await assert.rejects(
    () => runArchDecisionGet(pool, { slug: "DEC-A999" }),
    (e: unknown) =>
      e !== null && typeof e === "object" && (e as { code?: string }).code === "decision_not_found",
  );
});

// ---------------------------------------------------------------------------
// arch_decision_list
// ---------------------------------------------------------------------------

test("arch_decision_list: no filters → params empty, returns all", async () => {
  const { pool, calls } = makeStubPool([
    {
      rows: [
        { slug: "DEC-A1", title: "x", status: "active", rationale: "r", alternatives: null, superseded_by: null, surface_slug: null, created_at: "t" },
        { slug: "DEC-A2", title: "y", status: "active", rationale: "r", alternatives: null, superseded_by: null, surface_slug: "interchange/agent-ia", created_at: "t" },
      ],
    },
  ]);
  const out = await runArchDecisionList(pool, {});
  assert.equal(out.decisions.length, 2);
  assert.deepEqual(calls[0].params, []);
});

test("arch_decision_list: status filter narrows", async () => {
  const { pool, calls } = makeStubPool([{ rows: [] }]);
  const out = await runArchDecisionList(pool, { status: "active" });
  assert.deepEqual(out, { decisions: [] });
  assert.deepEqual(calls[0].params, ["active"]);
  assert.match(calls[0].sql, /d\.status = \$1/);
});

test("arch_decision_list: surface_slug filter joins arch_surfaces.slug", async () => {
  const { pool, calls } = makeStubPool([{ rows: [] }]);
  await runArchDecisionList(pool, { surface_slug: "interchange/agent-ia" });
  assert.deepEqual(calls[0].params, ["interchange/agent-ia"]);
  assert.match(calls[0].sql, /s\.slug = \$1/);
});

test("arch_decision_list: empty result → {decisions: []}", async () => {
  const { pool } = makeStubPool([{ rows: [] }]);
  const out = await runArchDecisionList(pool, {});
  assert.deepEqual(out, { decisions: [] });
});

// ---------------------------------------------------------------------------
// arch_surface_resolve
// ---------------------------------------------------------------------------

test("arch_surface_resolve: happy path stage_id+slug", async () => {
  const { pool, calls } = makeStubPool([
    {
      rows: [
        { slug: "interchange/agent-ia", kind: "contract", spec_path: "x.md", spec_section: "S" },
      ],
    },
  ]);
  const out = await runArchSurfaceResolve(pool, {
    slug: "architecture-coherence-system",
    stage_id: "1.3",
  });
  assert.equal(out.surfaces.length, 1);
  assert.equal(out.surfaces[0].slug, "interchange/agent-ia");
  assert.deepEqual(calls[0].params, ["1.3", "architecture-coherence-system"]);
});

test("arch_surface_resolve: happy path task_id resolves to stage", async () => {
  const { pool, calls } = makeStubPool([
    { rows: [{ slug: "architecture-coherence-system", stage_id: "1.3" }] },
    {
      rows: [
        { slug: "decisions/all", kind: "decision", spec_path: "x.md", spec_section: null },
      ],
    },
  ]);
  const out = await runArchSurfaceResolve(pool, { task_id: "TECH-2444" });
  assert.equal(out.surfaces.length, 1);
  // First call resolves task → stage; second pulls surfaces.
  assert.deepEqual(calls[0].params, ["TECH-2444"]);
  assert.deepEqual(calls[1].params, ["1.3", "architecture-coherence-system"]);
});

test("arch_surface_resolve: both stage_id + task_id → invalid_input", async () => {
  const { pool } = makeStubPool([]);
  await assert.rejects(
    () => runArchSurfaceResolve(pool, { stage_id: "1.3", task_id: "TECH-2444" }),
    (e: unknown) =>
      e !== null && typeof e === "object" && (e as { code?: string }).code === "invalid_input",
  );
});

test("arch_surface_resolve: neither set → invalid_input", async () => {
  const { pool } = makeStubPool([]);
  await assert.rejects(
    () => runArchSurfaceResolve(pool, {}),
    (e: unknown) =>
      e !== null && typeof e === "object" && (e as { code?: string }).code === "invalid_input",
  );
});

test("arch_surface_resolve: unknown task → task_not_found", async () => {
  const { pool } = makeStubPool([{ rows: [] }]);
  await assert.rejects(
    () => runArchSurfaceResolve(pool, { task_id: "TECH-9999" }),
    (e: unknown) =>
      e !== null && typeof e === "object" && (e as { code?: string }).code === "task_not_found",
  );
});

test("arch_surface_resolve: empty link table → {surfaces: []}", async () => {
  const { pool } = makeStubPool([{ rows: [] }]);
  const out = await runArchSurfaceResolve(pool, {
    slug: "architecture-coherence-system",
    stage_id: "9.9",
  });
  assert.deepEqual(out, { surfaces: [] });
});

// ---------------------------------------------------------------------------
// arch_drift_scan
// ---------------------------------------------------------------------------

test("arch_drift_scan: detects drift after pending flip ts", async () => {
  // Stage row: one stage in plan, with last_pending_flip_ts.
  const flipTs = new Date("2026-04-01T00:00:00Z");
  const planCreatedAt = new Date("2026-03-01T00:00:00Z");
  const { pool, calls } = makeStubPool([
    {
      rows: [
        {
          slug: "architecture-coherence-system",
          stage_id: "1.3",
          plan_created_at: planCreatedAt,
          last_pending_flip_ts: flipTs,
        },
      ],
    },
    {
      rows: [
        {
          surface_slug: "interchange/agent-ia",
          decision_slug: "DEC-A18",
          kind: "decide",
          ts: "2026-04-15T10:00:00Z",
        },
      ],
    },
  ]);
  const out = await runArchDriftScan(pool, { plan_id: "architecture-coherence-system" });
  assert.equal(out.affected_stages.length, 1);
  assert.equal(out.affected_stages[0].stage_id, "1.3");
  assert.equal(out.affected_stages[0].drifted_surfaces.length, 1);
  assert.equal(out.affected_stages[0].drifted_surfaces[0].slug, "interchange/agent-ia");
  // Question shape for `decide` kind references new decision slug.
  assert.match(out.affected_stages[0].suggested_questions[0], /DEC-A18.*re-plan\?/);
  // Drift query uses flipTs as the cutoff (3rd param).
  assert.equal(calls[1].params[2], flipTs);
});

test("arch_drift_scan: missing flip ts → fallback to plan created_at", async () => {
  const planCreatedAt = new Date("2026-03-01T00:00:00Z");
  const { pool, calls } = makeStubPool([
    {
      rows: [
        {
          slug: "architecture-coherence-system",
          stage_id: "1.3",
          plan_created_at: planCreatedAt,
          last_pending_flip_ts: null,
        },
      ],
    },
    { rows: [] },
  ]);
  await runArchDriftScan(pool, { plan_id: "architecture-coherence-system" });
  // Cutoff falls back to plan_created_at.
  assert.equal(calls[1].params[2], planCreatedAt);
});

test("arch_drift_scan: zero drift → empty affected_stages", async () => {
  const { pool } = makeStubPool([
    {
      rows: [
        {
          slug: "architecture-coherence-system",
          stage_id: "1.3",
          plan_created_at: new Date(),
          last_pending_flip_ts: null,
        },
      ],
    },
    { rows: [] },
  ]);
  const out = await runArchDriftScan(pool, { plan_id: "architecture-coherence-system" });
  assert.deepEqual(out, { affected_stages: [] });
});

test("arch_drift_scan: omitted plan_id scans every open plan", async () => {
  const { pool, calls } = makeStubPool([
    { rows: [] },
  ]);
  await runArchDriftScan(pool, {});
  // No params bound when plan_id omitted; SQL filters by open plans only.
  // Open = at least one stage with status != 'done' (orchestrators are
  // permanent — ia_master_plans has no status column).
  assert.deepEqual(calls[0].params, []);
  assert.match(calls[0].sql, /s\.status <> 'done'/);
});

test("arch_drift_scan: question shape per kind (supersede → pivot, edit → re-validate)", async () => {
  const flipTs = new Date("2026-04-01T00:00:00Z");
  const { pool } = makeStubPool([
    {
      rows: [
        {
          slug: "architecture-coherence-system",
          stage_id: "1.3",
          plan_created_at: new Date(),
          last_pending_flip_ts: flipTs,
        },
      ],
    },
    {
      rows: [
        { surface_slug: "x", decision_slug: null, kind: "supersede", ts: "2026-04-15T10:00:00Z" },
        { surface_slug: "y", decision_slug: null, kind: "edit", ts: "2026-04-16T10:00:00Z" },
      ],
    },
  ]);
  const out = await runArchDriftScan(pool, { plan_id: "architecture-coherence-system" });
  const qs = out.affected_stages[0].suggested_questions;
  assert.match(qs[0], /retired surface x.*pivot\?/);
  assert.match(qs[1], /schema changed.*re-validate\?/);
});

// ---------------------------------------------------------------------------
// arch_changelog_since
// ---------------------------------------------------------------------------

test("arch_changelog_since: since_ts returns ordered rows", async () => {
  const { pool, calls } = makeStubPool([
    {
      rows: [
        { id: 1, kind: "edit", surface_slug: "s/a", decision_slug: null, commit_sha: null, body: null, created_at: "2026-04-02T00:00:00Z" },
        { id: 2, kind: "decide", surface_slug: null, decision_slug: "DEC-A18", commit_sha: "abc", body: "x", created_at: "2026-04-03T00:00:00Z" },
      ],
    },
  ]);
  const out = await runArchChangelogSince(pool, { since_ts: "2026-04-01T00:00:00Z" });
  assert.equal(out.entries.length, 2);
  assert.equal(out.entries[0].id, 1);
  assert.equal(out.entries[1].decision_slug, "DEC-A18");
  assert.deepEqual(calls[0].params, ["2026-04-01T00:00:00Z"]);
  assert.match(calls[0].sql, /ORDER BY created_at ASC/);
});

test("arch_changelog_since: since_commit invokes resolver", async () => {
  const { pool, calls } = makeStubPool([{ rows: [] }]);
  const stub = (sha: string) => {
    assert.equal(sha, "abc1234");
    return "2026-04-10T00:00:00Z";
  };
  await runArchChangelogSince(pool, { since_commit: "abc1234" }, stub);
  assert.deepEqual(calls[0].params, ["2026-04-10T00:00:00Z"]);
});

test("arch_changelog_since: both inputs → invalid_input", async () => {
  const { pool } = makeStubPool([]);
  await assert.rejects(
    () => runArchChangelogSince(pool, { since_ts: "x", since_commit: "y" }),
    (e: unknown) =>
      e !== null && typeof e === "object" && (e as { code?: string }).code === "invalid_input",
  );
});

test("arch_changelog_since: neither input → invalid_input", async () => {
  const { pool } = makeStubPool([]);
  await assert.rejects(
    () => runArchChangelogSince(pool, {}),
    (e: unknown) =>
      e !== null && typeof e === "object" && (e as { code?: string }).code === "invalid_input",
  );
});

test("arch_changelog_since: resolver throws git_resolution_failed", async () => {
  const { pool } = makeStubPool([]);
  const stub = (_sha: string): string => {
    throw { code: "git_resolution_failed" as const, message: "bad sha" };
  };
  await assert.rejects(
    () => runArchChangelogSince(pool, { since_commit: "deadbeef" }, stub),
    (e: unknown) =>
      e !== null && typeof e === "object" && (e as { code?: string }).code === "git_resolution_failed",
  );
});

test("arch_changelog_since: empty result → {entries: []}", async () => {
  const { pool } = makeStubPool([{ rows: [] }]);
  const out = await runArchChangelogSince(pool, { since_ts: "2026-04-01T00:00:00Z" });
  assert.deepEqual(out, { entries: [] });
});

// ---------------------------------------------------------------------------
// arch_drift_scan — intra-plan: cross-section (TECH-5246)
// ---------------------------------------------------------------------------

test("arch_drift_scan: intra-plan flags cross-section surface edit with both section_ids", async () => {
  const oldCutoff = new Date("2026-01-01T00:00:00Z");
  const futureCutoff = new Date("2026-12-31T00:00:00Z");
  const planCreatedAt = new Date("2026-01-01T00:00:00Z");

  const { pool } = makeStubPool([
    // stageSql: two stages in different sections
    {
      rows: [
        { slug: "pcr-xsec", stage_id: "1.1", section_id: "section-a", plan_created_at: planCreatedAt, last_pending_flip_ts: oldCutoff },
        { slug: "pcr-xsec", stage_id: "2.1", section_id: "section-b", plan_created_at: planCreatedAt, last_pending_flip_ts: futureCutoff },
      ],
    },
    // driftSql for stage 1.1: drift on shared-surf (decision_status absent → not filtered)
    { rows: [{ surface_slug: "shared-surf", decision_slug: null, kind: "edit", ts: "2026-04-10T00:00:00Z" }] },
    // intra-plan xs for stage 1.1: cross-section link to section-b
    { rows: [{ section_id: "section-b", stage_id: "2.1" }] },
    // driftSql for stage 2.1: future cutoff → no changelog entries after it
    { rows: [] },
  ]);

  const out = await runArchDriftScan(pool, { plan_id: "pcr-xsec", scope: "intra-plan" });

  assert.equal(out.affected_stages.length, 1);
  assert.equal(out.affected_stages[0].stage_id, "1.1");
  assert.equal(out.affected_stages[0].section_id, "section-a");
  assert.equal(out.affected_stages[0].drifted_surfaces.length, 1);
  const xlinks = out.affected_stages[0].drifted_surfaces[0].cross_section_links;
  assert.ok(Array.isArray(xlinks) && xlinks.length === 1, "expected cross_section_links with 1 entry");
  assert.equal(xlinks![0].section_id, "section-b");
});

// ---------------------------------------------------------------------------
// arch_drift_scan — edge cases (TECH-5247)
// ---------------------------------------------------------------------------

test("arch_drift_scan: intra-plan with empty stage_arch_surfaces returns zero drift events", async () => {
  const { pool } = makeStubPool([
    // stageSql: one stage with no linked surfaces (driftSql returns [] because no JOIN match)
    { rows: [{ slug: "pcr-empty", stage_id: "1.1", section_id: "sec-a", plan_created_at: new Date(), last_pending_flip_ts: null }] },
    { rows: [] }, // driftSql: zero rows
  ]);
  const out = await runArchDriftScan(pool, { plan_id: "pcr-empty", scope: "intra-plan" });
  assert.deepEqual(out, { affected_stages: [] });
});

test("arch_drift_scan: intra-plan skips drift for superseded arch_decisions row (DEC-A17)", async () => {
  const { pool } = makeStubPool([
    { rows: [{ slug: "pcr-sup", stage_id: "1.1", section_id: "sec-a", plan_created_at: new Date("2026-01-01T00:00:00Z"), last_pending_flip_ts: new Date("2026-01-01T00:00:00Z") }] },
    // driftSql: one decide-kind row whose decision is superseded
    { rows: [{ surface_slug: "sup-surf", decision_slug: "DEC-ASUP", kind: "decide", ts: "2026-04-10T00:00:00Z", decision_status: "superseded" }] },
    // xs query never reached (loop hits continue before intra-plan block)
  ]);
  const out = await runArchDriftScan(pool, { plan_id: "pcr-sup", scope: "intra-plan" });
  assert.deepEqual(out, { affected_stages: [] });
});

test("arch_drift_scan: intra-plan idempotent re-scan returns identical events and writes no new rows", async () => {
  const cutoff = new Date("2026-01-01T00:00:00Z");
  const stageRow = { slug: "pcr-idemp", stage_id: "1.1", section_id: "sec-a", plan_created_at: cutoff, last_pending_flip_ts: cutoff };
  const driftRow = { surface_slug: "idem-surf", decision_slug: null, kind: "edit", ts: "2026-04-10T00:00:00Z" };
  const xsRow = { section_id: "sec-b", stage_id: "2.1" };

  const { pool, calls } = makeStubPool([
    // first call
    { rows: [stageRow] },
    { rows: [driftRow] },
    { rows: [xsRow] },
    // second call
    { rows: [stageRow] },
    { rows: [driftRow] },
    { rows: [xsRow] },
  ]);

  const input = { plan_id: "pcr-idemp", scope: "intra-plan" as const };
  const out1 = await runArchDriftScan(pool, input);
  const out2 = await runArchDriftScan(pool, input);

  assert.deepEqual(out1, out2);
  const insertCalls = calls.filter((c) => /insert/i.test(c.sql));
  assert.equal(insertCalls.length, 0, "arch_drift_scan must not write to arch_changelog");
});

// ---------------------------------------------------------------------------
// arch_surface_write (prototype-first-methodology Stage 1.1 / migration 0058)
// ---------------------------------------------------------------------------

test("arch_surface_write: missing slug → invalid_input", async () => {
  const { pool } = makeStubPool([]);
  await assert.rejects(
    () =>
      runArchSurfaceWrite(pool, {
        slug: "",
        kind: "rule",
        spec_path: "ia/rules/x.md",
      }),
    (e: unknown) =>
      e !== null && typeof e === "object" && (e as { code?: string }).code === "invalid_input",
  );
});

test("arch_surface_write: missing kind → invalid_input", async () => {
  const { pool } = makeStubPool([]);
  await assert.rejects(
    () =>
      runArchSurfaceWrite(pool, {
        slug: "rules/foo",
        kind: undefined,
        spec_path: "ia/rules/foo.md",
      }),
    (e: unknown) =>
      e !== null && typeof e === "object" && (e as { code?: string }).code === "invalid_input",
  );
});

test("arch_surface_write: new slug → INSERT, upserted=false", async () => {
  const { pool, calls } = makeStubPool([
    { rows: [] }, // pre-check: no existing row
    {
      rows: [
        {
          id: 42,
          slug: "rules/prototype-first-methodology",
          kind: "rule",
          spec_path: "ia/rules/prototype-first-methodology.md",
          spec_section: null,
        },
      ],
    },
  ]);
  const out = await runArchSurfaceWrite(pool, {
    slug: "rules/prototype-first-methodology",
    kind: "rule",
    spec_path: "ia/rules/prototype-first-methodology.md",
  });
  assert.equal(out.id, 42);
  assert.equal(out.slug, "rules/prototype-first-methodology");
  assert.equal(out.kind, "rule");
  assert.equal(out.spec_section, null);
  assert.equal(out.upserted, false);
  // Pre-check then insert.
  assert.match(calls[0].sql, /SELECT id FROM arch_surfaces WHERE slug = \$1/);
  assert.match(calls[1].sql, /INSERT INTO arch_surfaces/);
  assert.match(calls[1].sql, /ON CONFLICT \(slug\) DO UPDATE/);
});

test("arch_surface_write: existing slug → UPSERT, upserted=true", async () => {
  const { pool } = makeStubPool([
    { rows: [{ id: 7 }] }, // pre-check: existing row
    {
      rows: [
        {
          id: 7,
          slug: "decisions/all",
          kind: "decision",
          spec_path: "ia/specs/architecture/decisions.md",
          spec_section: "DEC-A22",
        },
      ],
    },
  ]);
  const out = await runArchSurfaceWrite(pool, {
    slug: "decisions/all",
    kind: "decision",
    spec_path: "ia/specs/architecture/decisions.md",
    spec_section: "DEC-A22",
  });
  assert.equal(out.id, 7);
  assert.equal(out.spec_section, "DEC-A22");
  assert.equal(out.upserted, true);
});
