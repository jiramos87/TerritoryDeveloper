import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { gridDistanceBetweenCells } from "../src/gridDistance/distance.js";

describe("GridDistanceMath parity", () => {
  it("Chebyshev and Manhattan match C#", () => {
    assert.equal(gridDistanceBetweenCells(0, 0, 1, 1, "chebyshev"), 1);
    assert.equal(gridDistanceBetweenCells(0, 0, 1, 1, "manhattan"), 2);
    assert.equal(gridDistanceBetweenCells(0, 0, -5, 2, "chebyshev"), 5);
  });
});
