import test from "node:test";
import assert from "node:assert/strict";
import { inferDomainHintsFromPath } from "../../src/tools/router-for-task.js";
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
