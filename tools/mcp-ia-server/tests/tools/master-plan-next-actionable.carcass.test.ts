/**
 * Carcass-gate test for `master_plan_next_actionable` (TECH-4623).
 *
 * Two-phase assert proving §6.2 carcass-gate behavior on V2 row-only
 * claim model (post mig 0052):
 *
 *   Phase 1 (carcass not done) — only carcass-role stages emitted.
 *   Phase-flip — UPDATE ia_stages.status='done' WHERE carcass_role='carcass';
 *                REFRESH MATERIALIZED VIEW ia_master_plan_health.
 *   Phase 2 (carcass done) — section-role stages emitted.
 *
 * Calls the pure cores (`computeNextActionable` + `applyCarcassFilters`)
 * directly per §Pending Decisions — sidesteps MCP envelope to keep the
 * carcass filter as the unit under test.
 *
 * V2 row-only model: claims map is empty (no session_id column post 0052)
 * — fixture seeds zero claim rows so the section-claim path is no-op.
 */

import assert from "node:assert/strict";
import { after, before, describe, it } from "node:test";
import { getIaDatabasePool } from "../../src/ia-db/pool.js";
import { getStageDependsOnGraph } from "../../src/ia-db/queries.js";
import {
  applyCarcassFilters,
  computeNextActionable,
  type NextActionableEntry,
} from "../../src/tools/master-plan-next-actionable.js";
import {
  seedCarcassHealthFixture,
  teardownCarcassHealthFixture,
} from "../fixtures/parallel-carcass-health-mv.fixture.js";

const pool = getIaDatabasePool();
const skip = pool === null ? { skip: "DB pool unavailable" } : {};

// Per-file slug — `node --test` runs files in parallel workers. Shared
// slug + teardown DELETE cascades cause `ia_stages_slug_fkey` violations
// mid-seed. Each file owns a disjoint sandbox slug.
const CARCASS_FIXTURE_SLUG = "__test_carcass_next_actionable__";

interface StageMetaRow {
  stage_id: string;
  carcass_role: string | null;
  section_id: string | null;
  status: "pending" | "in_progress" | "done";
}

async function readMeta(slug: string): Promise<StageMetaRow[]> {
  if (!pool) return [];
  const res = await pool.query<StageMetaRow>(
    `SELECT stage_id, carcass_role, section_id, status
       FROM ia_stages
      WHERE slug = $1`,
    [slug],
  );
  return res.rows;
}

// V2 row-only: claims keyed by row only — no session_id column.
// Empty claim maps simulate "no claims held"; behaviorally equivalent to
// the V2 readClaims path when all rows have released_at NULL absent.
const EMPTY_CLAIMS = {
  section_session: new Map<string, string>(),
  stage_session: new Map<string, string>(),
};

async function nextActionableForSlug(
  slug: string,
): Promise<NextActionableEntry[]> {
  const graph = await getStageDependsOnGraph(slug);
  const raw = computeNextActionable(graph, new Map());
  const meta = await readMeta(slug);
  return applyCarcassFilters(raw, meta, EMPTY_CLAIMS, undefined);
}

describe("master_plan_next_actionable — carcass gate (TECH-4623)", skip, () => {
  before(async () => {
    if (!pool) return;
    await teardownCarcassHealthFixture(CARCASS_FIXTURE_SLUG);
    await seedCarcassHealthFixture(CARCASS_FIXTURE_SLUG);
    await pool.query(`REFRESH MATERIALIZED VIEW ia_master_plan_health`);
  });

  after(async () => {
    if (!pool) return;
    await teardownCarcassHealthFixture(CARCASS_FIXTURE_SLUG);
  });

  it("phase 1 — carcass not done: returns ONLY carcass-role stages", async () => {
    const entries = await nextActionableForSlug(CARCASS_FIXTURE_SLUG);
    const meta = await readMeta(CARCASS_FIXTURE_SLUG);
    const roleByStageId = new Map(meta.map((m) => [m.stage_id, m.carcass_role]));

    assert.ok(entries.length >= 1, "expected ≥1 carcass entry pre-flip");
    for (const e of entries) {
      const role = roleByStageId.get(e.stage_id);
      assert.equal(
        role,
        "carcass",
        `expected carcass-only pre-flip, got stage ${e.stage_id} role=${role}`,
      );
    }
    assert.ok(
      !entries.some((e) => e.stage_id.startsWith("section.")),
      "section.* stages must NOT appear pre-carcass-done",
    );
  });

  it("phase 2 — carcass done: returns ≥1 section-role stage", async () => {
    if (!pool) return;
    await pool.query(
      `UPDATE ia_stages SET status = 'done'
        WHERE slug = $1 AND carcass_role = 'carcass'`,
      [CARCASS_FIXTURE_SLUG],
    );
    await pool.query(`REFRESH MATERIALIZED VIEW ia_master_plan_health`);

    const entries = await nextActionableForSlug(CARCASS_FIXTURE_SLUG);
    assert.ok(
      entries.some((e) => e.stage_id.startsWith("section.")),
      "expected ≥1 section.* stage post-carcass-done — gate did not lift",
    );
  });
});
