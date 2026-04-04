/**
 * MCP tool: grid_distance — Chebyshev / Manhattan on integer cells (not geo §10 costs).
 */

import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import {
  gridDistanceBetweenCells,
  gridDistanceInputSchema,
  gridDistanceInputShape,
} from "territory-compute-lib";
import { ZodError } from "zod";
import { runWithToolTiming } from "../../instrumentation.js";
import { checkGridCellBounds } from "./withComputeLimits.js";
import { jsonToolResult } from "./jsonToolResult.js";

export function registerGridDistance(server: McpServer): void {
  server.registerTool(
    "grid_distance",
    {
      description:
        "Chebyshev or Manhattan distance between two integer grid cells (glossary: Chebyshev distance). " +
        "Does not define pathfinding edge costs — see isometric-geography-system §10 for the in-game cost model.",
      inputSchema: gridDistanceInputShape,
    },
    async (args: unknown) =>
      runWithToolTiming("grid_distance", async () => {
        try {
          const input = gridDistanceInputSchema.parse(
            args === undefined || args === null ? {} : args,
          );
          for (const [x, y] of [
            [input.ax, input.ay],
            [input.bx, input.by],
          ] as const) {
            const bad = checkGridCellBounds(x, y, input.map_width, input.map_height);
            if (bad) return jsonToolResult(bad);
          }
          const distance = gridDistanceBetweenCells(
            input.ax,
            input.ay,
            input.bx,
            input.by,
            input.mode,
          );
          return jsonToolResult({
            ok: true as const,
            data: { distance, mode: input.mode },
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
            error: { code: "VALIDATION_ERROR" as const, message: msg },
          });
        }
      }),
  );
}
