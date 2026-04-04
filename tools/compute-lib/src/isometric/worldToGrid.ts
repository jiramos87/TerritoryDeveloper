import { roundLikeUnity } from "./round.js";

export interface WorldToGridPlanarParams {
  worldX: number;
  worldY: number;
  tileWidth: number;
  tileHeight: number;
  originX?: number;
  originY?: number;
}

/**
 * Planar inverse isometric mapping (world → logical cell), isometric-geography-system §1.3.
 * Does not implement height-aware picking (Unity-only).
 */
export function worldToGridPlanar(p: WorldToGridPlanarParams): {
  cellX: number;
  cellY: number;
} {
  const { worldX, worldY, tileWidth, tileHeight } = p;
  const originX = p.originX ?? 0;
  const originY = p.originY ?? 0;

  if (!Number.isFinite(worldX) || !Number.isFinite(worldY)) {
    throw new Error("worldToGridPlanar: world coordinates must be finite numbers");
  }
  if (!(tileWidth > 0) || !(tileHeight > 0)) {
    throw new Error("worldToGridPlanar: tileWidth and tileHeight must be positive");
  }

  const wx = worldX - originX;
  const wy = worldY - originY;

  const posX = wx / (tileWidth / 2);
  const posY = wy / (tileHeight / 2);

  return {
    cellX: roundLikeUnity((posY + posX) / 2),
    cellY: roundLikeUnity((posY - posX) / 2),
  };
}

export interface GridToWorldPlanarParams {
  cellX: number;
  cellY: number;
  tileWidth: number;
  tileHeight: number;
  /** Terrain height level (1 = base). Matches GridManager height offset convention. */
  heightLevel?: number;
  originX?: number;
  originY?: number;
}

/**
 * Forward mapping grid → world (§1.1), including optional per-cell height offset.
 */
export function gridToWorldPlanar(p: GridToWorldPlanarParams): {
  worldX: number;
  worldY: number;
} {
  const { cellX, cellY, tileWidth, tileHeight } = p;
  const h = p.heightLevel ?? 1;
  const originX = p.originX ?? 0;
  const originY = p.originY ?? 0;

  if (!Number.isInteger(cellX) || !Number.isInteger(cellY)) {
    throw new Error("gridToWorldPlanar: cell coordinates must be integers");
  }
  if (!(tileWidth > 0) || !(tileHeight > 0)) {
    throw new Error("gridToWorldPlanar: tileWidth and tileHeight must be positive");
  }
  if (!Number.isInteger(h) || h < 1) {
    throw new Error("gridToWorldPlanar: heightLevel must be an integer >= 1");
  }

  const heightOffset = (h - 1) * (tileHeight / 2);
  const worldX =
    (cellX - cellY) * (tileWidth / 2) + originX;
  const worldY =
    (cellX + cellY) * (tileHeight / 2) + heightOffset + originY;

  return { worldX, worldY };
}
