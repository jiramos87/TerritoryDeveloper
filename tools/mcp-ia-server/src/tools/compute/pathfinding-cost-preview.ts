/**
 * MCP tool: pathfinding_cost_preview — v1 Manhattan approximation (glossary **Computational MCP tools (TECH-39)**; not geo §10).
 */

import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import {
  pathfindingCostPreviewInputSchema,
  pathfindingCostPreviewInputShape,
  pathfindingCostPreviewManhattanV1,
} from "territory-compute-lib";
import { ZodError } from "zod";
import { runWithToolTiming } from "../../instrumentation.js";
import { checkGridCellBounds } from "./withComputeLimits.js";
import { jsonToolResult } from "./jsonToolResult.js";

export function registerPathfindingCostPreview(server: McpServer): void {
  server.registerTool(
    "pathfinding_cost_preview",
    {
      description:
        "v1 preview: Manhattan steps × unit_cost_per_step between two cells. " +
        "Explicit approximation only — not isometric-geography-system §10 A* terrain costs, wet runs, or road legality. " +
        "For authoritative pathfinding behavior, use spec_section (geo §10) or Unity tools when available (BACKLOG TECH-65 for v2 parity).",
      inputSchema: pathfindingCostPreviewInputShape,
    },
    async (args: unknown) =>
      runWithToolTiming("pathfinding_cost_preview", async () => {
        try {
          const input = pathfindingCostPreviewInputSchema.parse(
            args === undefined || args === null ? {} : args,
          );
          for (const c of [input.from_cell, input.to_cell]) {
            const bad = checkGridCellBounds(c.x, c.y, input.map_width, input.map_height);
            if (bad) return jsonToolResult(bad);
          }
          const preview = pathfindingCostPreviewManhattanV1(
            input.from_cell.x,
            input.from_cell.y,
            input.to_cell.x,
            input.to_cell.y,
            input.unit_cost_per_step ?? 1,
          );
          return jsonToolResult({
            ok: true as const,
            data: preview,
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
