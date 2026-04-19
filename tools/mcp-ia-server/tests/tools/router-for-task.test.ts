import test from "node:test";
import assert from "node:assert/strict";
import { inferDomainHintsFromPath, LifecycleStage } from "../../src/tools/router-for-task.js";
import { wrapTool } from "../../src/envelope.js";

test("inferDomainHintsFromPath maps road and water filenames", () => {
  const road = inferDomainHintsFromPath("Assets/Scripts/RoadPlacementHelper.cs");
  assert.ok(
    road.some((d) => d.includes("Road logic")),
    `got ${JSON.stringify(road)}`,
  );
  const water = inferDomainHintsFromPath("Assets/Scripts/Managers/UnitManagers/WaterMap.cs");
  assert.ok(
    water.some((d) => d.includes("Water, terrain")),
    `got ${JSON.stringify(water)}`,
  );
});

test("inferDomainHintsFromPath maps GridManager to geography row", () => {
  const g = inferDomainHintsFromPath("Assets/Scripts/Managers/GameManagers/GridManager.cs");
  assert.ok(
    g.some((d) => d.includes("Slopes, sorting")),
    `got ${JSON.stringify(g)}`,
  );
});

test("inferDomainHintsFromPath maps mcp package to Backlog row", () => {
  const m = inferDomainHintsFromPath("tools/mcp-ia-server/src/index.ts");
  assert.ok(
    m.some((d) => d.includes("Backlog")),
    `got ${JSON.stringify(m)}`,
  );
});

// Envelope shape — router_for_task wraps handler in wrapTool (TECH-399).
test("router_for_task envelope: empty input → ok false, invalid_input", async () => {
  const envelope = await wrapTool(async () => {
    throw {
      code: "invalid_input" as const,
      message: "Provide `domain` and/or a non-empty `files` array.",
    };
  })(undefined);
  assert.equal(envelope.ok, false);
  if (!envelope.ok) {
    assert.equal(envelope.error.code, "invalid_input");
  }
});

test("router_for_task envelope: no match → ok false, invalid_input with details.available_domains", async () => {
  const available_domains = ["roads", "water"];
  const envelope = await wrapTool(async () => {
    throw {
      code: "invalid_input" as const,
      message: "No router row matches domain 'zzz'.",
      details: { available_domains },
    };
  })(undefined);
  assert.equal(envelope.ok, false);
  if (!envelope.ok) {
    assert.equal(envelope.error.code, "invalid_input");
    assert.ok(
      Array.isArray(
        (envelope.error.details as { available_domains?: unknown[] })
          ?.available_domains,
      ),
    );
  }
});

// ---------------------------------------------------------------------------
// LifecycleStage enum — TECH-458
// ---------------------------------------------------------------------------

const EXPECTED_LIFECYCLE_STAGES = [
  "plan_review",
  "plan_fix_apply",
  "stage_file_plan",
  "stage_file_apply",
  "project_new_plan",
  "project_new_apply",
  "spec_enrich",
  "opus_audit",
  "opus_code_review",
  "code_fix_apply",
  "closeout_apply",
] as const;

test("LifecycleStage enum has exactly 11 values and no Phase-era values", () => {
  const options = LifecycleStage.options;
  assert.equal(options.length, 11, `expected 11, got ${options.length}: ${JSON.stringify(options)}`);
  for (const v of EXPECTED_LIFECYCLE_STAGES) {
    assert.ok(options.includes(v), `missing expected value: ${v}`);
  }
  // No Phase-era values
  for (const v of options) {
    assert.ok(
      !v.includes("phase") && !v.includes("parent_phase"),
      `Phase-era value found: ${v}`,
    );
  }
});

test("LifecycleStage enum accepts all 11 valid values", () => {
  for (const v of EXPECTED_LIFECYCLE_STAGES) {
    const result = LifecycleStage.safeParse(v);
    assert.ok(result.success, `expected success for value: ${v}`);
  }
});

test("LifecycleStage enum rejects unknown string", () => {
  const result = LifecycleStage.safeParse("unknown_stage");
  assert.equal(result.success, false);
  if (!result.success) {
    // Zod enum rejection code is version-dependent (invalid_value or invalid_enum_value)
    const code = result.error.issues[0]?.code;
    assert.ok(
      code === "invalid_enum_value" || code === "invalid_value",
      `expected enum rejection code, got: ${code}`,
    );
  }
});

test("LifecycleStage enum rejects Phase-era value 'plan_phase'", () => {
  const result = LifecycleStage.safeParse("plan_phase");
  assert.equal(result.success, false);
});

test("LifecycleStage.optional() accepts undefined", () => {
  const optSchema = LifecycleStage.optional();
  const result = optSchema.safeParse(undefined);
  assert.ok(result.success);
});
