/**
 * TECH-39 pathfinding_cost_preview v1 — Manhattan step count × scalar only (approximation; not geo §10 A* costs).
 */

export type PathfindingCostPreviewV1Result = {
  steps: number;
  total_cost: number;
  unit_cost_per_step: number;
  approximation: true;
  note: string;
};

export function pathfindingCostPreviewManhattanV1(
  fromX: number,
  fromY: number,
  toX: number,
  toY: number,
  unitCostPerStep = 1,
): PathfindingCostPreviewV1Result {
  const steps = Math.abs(fromX - toX) + Math.abs(fromY - toY);
  return {
    steps,
    total_cost: steps * unitCostPerStep,
    unit_cost_per_step: unitCostPerStep,
    approximation: true,
    note:
      "v1 preview: Manhattan steps × unit_cost_per_step only. Not committed road legality or isometric-geography-system §10 edge costs; use Unity / spec_section for authoritative pathfinding.",
  };
}
