/**
 * Integer grid metrics — parity with C# GridDistanceMath (not geo §10 pathfinding costs).
 */

export type GridDistanceMode = "chebyshev" | "manhattan";

export function gridDistanceBetweenCells(
  ax: number,
  ay: number,
  bx: number,
  by: number,
  mode: GridDistanceMode,
): number {
  const dx = ax > bx ? ax - bx : bx - ax;
  const dy = ay > by ? ay - by : by - ay;
  if (mode === "manhattan") return dx + dy;
  return dx > dy ? dx : dy;
}
