import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, it } from "node:test";
import {
  gridToWorldPlanar,
  worldToGridPlanar,
} from "../src/isometric/worldToGrid.js";
import { isometricWorldToGridInputSchema } from "../src/schemas/isometric.js";

const __dirname = dirname(fileURLToPath(import.meta.url));

interface FixtureCase {
  world_x: number;
  world_y: number;
  tile_width: number;
  tile_height: number;
  origin_x?: number;
  origin_y?: number;
  cell_x: number;
  cell_y: number;
}

describe("isometricWorldToGridInputSchema", () => {
  it("rejects non-finite coordinates", () => {
    assert.throws(() =>
      isometricWorldToGridInputSchema.parse({
        world_x: Number.POSITIVE_INFINITY,
        world_y: 0,
        tile_width: 1,
        tile_height: 0.5,
      }),
    );
  });
});

describe("worldToGridPlanar", () => {
  it("matches golden fixture", () => {
    const raw = readFileSync(
      join(__dirname, "fixtures/world-to-grid.json"),
      "utf8",
    );
    const fixture = JSON.parse(raw) as { cases: FixtureCase[] };
    for (const c of fixture.cases) {
      const got = worldToGridPlanar({
        worldX: c.world_x,
        worldY: c.world_y,
        tileWidth: c.tile_width,
        tileHeight: c.tile_height,
        originX: c.origin_x,
        originY: c.origin_y,
      });
      assert.equal(got.cellX, c.cell_x, `cellX case ${JSON.stringify(c)}`);
      assert.equal(got.cellY, c.cell_y, `cellY case ${JSON.stringify(c)}`);
    }
  });

  it("round-trips grid → world → grid for integer cells", () => {
    const tileW = 1;
    const tileH = 0.5;
    for (const cell of [
      [0, 0],
      [3, 4],
      [-1, 2],
      [5, 1],
    ] as const) {
      const w = gridToWorldPlanar({
        cellX: cell[0],
        cellY: cell[1],
        tileWidth: tileW,
        tileHeight: tileH,
        heightLevel: 1,
      });
      const back = worldToGridPlanar({
        worldX: w.worldX,
        worldY: w.worldY,
        tileWidth: tileW,
        tileHeight: tileH,
      });
      assert.equal(back.cellX, cell[0]);
      assert.equal(back.cellY, cell[1]);
    }
  });
});
