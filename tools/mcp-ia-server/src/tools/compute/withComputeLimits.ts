/**
 * Computational MCP limits (glossary **Computational MCP tools (TECH-39)** — dimension bounds before compute (no silent wrong grids).
 */

import { DEFAULT_MAX_GRID_DIMENSION } from "territory-compute-lib";

export type LimitError = {
  ok: false;
  error: { code: "LIMIT_EXCEEDED"; message: string };
};

export function checkGridCellBounds(
  x: number,
  y: number,
  mapWidth?: number,
  mapHeight?: number,
): LimitError | null {
  const w = mapWidth ?? DEFAULT_MAX_GRID_DIMENSION;
  const h = mapHeight ?? DEFAULT_MAX_GRID_DIMENSION;
  if (!Number.isInteger(x) || !Number.isInteger(y)) {
    return {
      ok: false,
      error: {
        code: "LIMIT_EXCEEDED",
        message: "Grid cell coordinates must be integers",
      },
    };
  }
  if (x < 0 || y < 0 || x >= w || y >= h) {
    return {
      ok: false,
      error: {
        code: "LIMIT_EXCEEDED",
        message: `Cell (${x},${y}) out of bounds for map ${w}×${h} (max dimension ${DEFAULT_MAX_GRID_DIMENSION})`,
      },
    };
  }
  return null;
}
