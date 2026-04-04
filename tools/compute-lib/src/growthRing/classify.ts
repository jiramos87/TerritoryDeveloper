/**
 * Urban growth ring classification — parity with C# UrbanGrowthRingMath (simulation-system §Rings).
 */

import type { GrowthRingClassifyInput } from "../schemas/growthRing.js";

export type UrbanRingName = "Inner" | "Mid" | "Outer" | "Rural";

const MinUrbanRadius = 20;
const RadiusScale = 1.8;

const DefaultBoundaryFractions: readonly [number, number, number] = [0.7, 1.0, 1.8];

/**
 * Effective urban radius from building cell count (same formula as C# UrbanGrowthRingMath.ComputeUrbanRadiusFromCellCount).
 */
export function computeUrbanRadiusFromCellCount(urbanCellCount: number): number {
  const r = RadiusScale * Math.sqrt(urbanCellCount / Math.PI);
  return Math.max(MinUrbanRadius, r);
}

/**
 * Normalized distance bands vs urban radius (C# ClassifyRingFromDistance).
 */
export function classifyRingFromDistance(
  distanceToPole: number,
  urbanRadius: number,
  boundaryFractions: readonly [number, number, number] = DefaultBoundaryFractions,
): UrbanRingName {
  if (urbanRadius <= 0) return "Rural";
  if (distanceToPole <= urbanRadius * boundaryFractions[0]) return "Inner";
  if (distanceToPole <= urbanRadius * boundaryFractions[1]) return "Mid";
  if (distanceToPole <= urbanRadius * boundaryFractions[2]) return "Outer";
  return "Rural";
}

export type GrowthRingClassifyResult = {
  ring: UrbanRingName;
  urban_radius: number;
  /** Minimum Euclidean distance to any centroid; null when centroids array is empty (fallback ring). */
  distance_to_pole: number | null;
};

/**
 * Multipolar: minimum distance to any pole, equal weight (C# ClassifyRingMultipolar).
 */
export function classifyGrowthRing(input: GrowthRingClassifyInput): GrowthRingClassifyResult {
  const boundaryFractions = input.ring_boundary_fractions ?? DefaultBoundaryFractions;
  const urbanRadius =
    input.urban_radius !== undefined
      ? input.urban_radius
      : computeUrbanRadiusFromCellCount(input.urban_cell_count!);

  const fallback: UrbanRingName = input.fallback_ring ?? "Mid";

  if (input.centroids.length === 0) {
    return { ring: fallback, urban_radius: urbanRadius, distance_to_pole: null };
  }

  let best = Number.POSITIVE_INFINITY;
  for (const c of input.centroids) {
    const dx = input.cell.x - c.x;
    const dy = input.cell.y - c.y;
    const d = Math.hypot(dx, dy);
    if (d < best) best = d;
  }

  const ring = classifyRingFromDistance(best, urbanRadius, boundaryFractions);
  return { ring, urban_radius: urbanRadius, distance_to_pole: best };
}
