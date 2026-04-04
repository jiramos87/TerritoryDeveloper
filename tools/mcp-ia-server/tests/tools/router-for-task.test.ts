import test from "node:test";
import assert from "node:assert/strict";
import { inferDomainHintsFromPath } from "../../src/tools/router-for-task.js";

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
