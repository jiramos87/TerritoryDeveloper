/**
 * MCP tool: isometric_world_to_grid — planar world → logical cell (isometric-geography-system §1.3).
 */

import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import {
  isometricWorldToGridInputSchema,
  isometricWorldToGridInputShape,
  worldToGridPlanar,
} from "territory-compute-lib";
import { ZodError } from "zod";
import { runWithToolTiming } from "../../instrumentation.js";
import { jsonToolResult } from "./jsonToolResult.js";

/**
 * Register the isometric_world_to_grid tool (World ↔ Grid conversion — inverse §1.3).
 */
export function registerIsometricWorldToGrid(server: McpServer): void {
  server.registerTool(
    "isometric_world_to_grid",
    {
      description:
        "Convert an isometric world-space point (x, y) to logical grid cell indices (cell_x, cell_y) using " +
        "isometric-geography-system §1.3 inverse formulas (glossary: World ↔ Grid conversion). " +
        "Planar only — does not replicate Unity height-aware cell picking. " +
        "Optional origin_x/origin_y shift world coordinates before conversion.",
      inputSchema: isometricWorldToGridInputShape,
    },
    async (args: unknown) =>
      runWithToolTiming("isometric_world_to_grid", async () => {
        try {
          const input = isometricWorldToGridInputSchema.parse(
            args === undefined || args === null ? {} : args,
          );
          const { cellX, cellY } = worldToGridPlanar({
            worldX: input.world_x,
            worldY: input.world_y,
            tileWidth: input.tile_width,
            tileHeight: input.tile_height,
            originX: input.origin_x,
            originY: input.origin_y,
          });
          return jsonToolResult({
            ok: true as const,
            cell_x: cellX,
            cell_y: cellY,
          });
        } catch (e) {
          if (e instanceof ZodError) {
            return jsonToolResult({
              ok: false as const,
              error: {
                code: "VALIDATION_ERROR" as const,
                message: e.issues.map((i) => i.message).join("; ") || "Invalid input",
              },
            });
          }
          const msg = e instanceof Error ? e.message : String(e);
          return jsonToolResult({
            ok: false as const,
            error: {
              code: "VALIDATION_ERROR" as const,
              message: msg,
            },
          });
        }
      }),
  );
}
