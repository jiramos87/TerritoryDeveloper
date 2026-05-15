// Stage 3.0 — Wave B (EARS rubric rule 11 + /spec-freeze gate) — incremental TDD red→green.
//
// Note: handoff red_stage_proof points at tests/vibe-coding-safety/stage2-spec-freeze.test.mjs.
// File name retained verbatim from handoff so red_test_anchor matches at validator time.
//
// Stage anchor: visibility-delta-test:tests/vibe-coding-safety/stage2-spec-freeze.test.mjs::ShipPlanRefusesNonFrozen
//
// Tasks:
//   3.0.1  Migration — ia_master_plans add ears_grandfathered column + backfill
//   3.0.2  Migration — ia_master_plan_specs table
//   3.0.3  Register MCP tool master_plan_spec_freeze
//   3.0.4  Author /spec-freeze skill
//   3.0.5  Rubric rule 11 in plan-digest-contract.md
//   3.0.6  Inject rubric rule 11 into /stage-authoring Phase 4 prompt
//   3.0.7  Extend validate:plan-digest-coverage to enforce EARS
//   3.0.8  Gate /ship-plan on frozen spec
//   3.0.9  Stage test — spec-freeze + ship-plan gate + EARS rubric (this file)
//   3.0.10 Regenerate skill catalog + IA indexes

import { describe, it } from "node:test";
import assert from "node:assert/strict";

// TODO(task 3.0.9): wire pg client + spawnSync runners once 3.0.1..3.0.8 land.
// Skeleton kept as red placeholders so file is RED until last task closes the loop.

describe("Stage 3.0 — spec-freeze + ship-plan gate + EARS rubric", () => {
  it("specFreezeInsertsRowAndEmitsArchChangelog [task 3.0.9]", () => {
    assert.fail("TODO 3.0.9 — pending master_plan_spec_freeze MCP + arch_changelog assertion");
  });

  it("shipPlanRefusesNonFrozenSpec [task 3.0.9]", () => {
    assert.fail("TODO 3.0.9 — pending ship-plan Phase A gate (task 3.0.8)");
  });

  it("validatePlanDigestCoverageRejectsNonEarsAcceptance [task 3.0.9]", () => {
    assert.fail("TODO 3.0.9 — pending EARS rubric enforcement (task 3.0.7)");
  });

  it("earsGrandfatheredBypassesEarsCheck [task 3.0.9]", () => {
    assert.fail("TODO 3.0.9 — pending ears_grandfathered column behaviour (task 3.0.1)");
  });
});
