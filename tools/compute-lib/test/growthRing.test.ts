import assert from "node:assert/strict";
import { describe, it } from "node:test";
import {
  classifyGrowthRing,
  classifyRingFromDistance,
  computeUrbanRadiusFromCellCount,
} from "../src/growthRing/classify.js";

describe("UrbanGrowthRingMath parity (ComputeLibParityTests vectors)", () => {
  it("radius and single-centroid bands match C#", () => {
    const r = computeUrbanRadiusFromCellCount(500);
    assert.ok(r > 0);

    const inner = classifyGrowthRing({
      cell: { x: 0, y: 0 },
      centroids: [{ x: 0, y: 0 }],
      urban_radius: r,
    });
    assert.equal(inner.ring, "Inner");

    assert.equal(classifyRingFromDistance(r * 0.85, r), "Mid");
    assert.equal(classifyRingFromDistance(r * 1.4, r), "Outer");
    assert.equal(classifyRingFromDistance(r * 2, r), "Rural");
  });

  it("multipolar minimum distance matches C# poles", () => {
    const r = computeUrbanRadiusFromCellCount(500);
    const poles = [
      { x: 0, y: 0 },
      { x: 20, y: 0 },
    ];
    const a = classifyGrowthRing({
      cell: { x: 0, y: 0 },
      centroids: poles,
      urban_radius: r,
    });
    const b = classifyGrowthRing({
      cell: { x: 20, y: 0 },
      centroids: poles,
      urban_radius: r,
    });
    assert.equal(a.ring, "Inner");
    assert.equal(b.ring, "Inner");
  });
});
