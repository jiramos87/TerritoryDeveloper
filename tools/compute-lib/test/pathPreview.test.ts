import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { pathfindingCostPreviewManhattanV1 } from "../src/pathPreview/manhattanV1.js";

describe("pathfindingCostPreviewManhattanV1", () => {
  it("counts Manhattan steps and scales by unit cost", () => {
    const p = pathfindingCostPreviewManhattanV1(0, 0, 3, 4, 2);
    assert.equal(p.steps, 7);
    assert.equal(p.total_cost, 14);
    assert.equal(p.approximation, true);
    assert.ok(p.note.includes("§10"));
  });
});
