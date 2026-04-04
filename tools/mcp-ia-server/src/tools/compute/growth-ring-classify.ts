/**
 * MCP tool: growth_ring_classify — urban growth rings (simulation-system §Rings; parity UrbanGrowthRingMath).
 */

import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import {
  classifyGrowthRing,
  growthRingClassifyInputSchema,
  growthRingClassifyInputShape,
} from "territory-compute-lib";
import { ZodError } from "zod";
import { runWithToolTiming } from "../../instrumentation.js";
import { jsonToolResult } from "./jsonToolResult.js";

export function registerGrowthRingClassify(server: McpServer): void {
  server.registerTool(
    "growth_ring_classify",
    {
      description:
        "Classify a logical position into an urban growth ring (glossary: Urban growth rings, Urban centroid) " +
        "using Euclidean distance to one or more centroids vs effective urban radius — simulation-system §Rings. " +
        "Matches C# UrbanGrowthRingMath (multipolar = minimum distance to poles). " +
        "Provide urban_cell_count (radius from built area) or urban_radius directly.",
      inputSchema: growthRingClassifyInputShape,
    },
    async (args: unknown) =>
      runWithToolTiming("growth_ring_classify", async () => {
        try {
          const input = growthRingClassifyInputSchema.parse(
            args === undefined || args === null ? {} : args,
          );
          const data = classifyGrowthRing(input);
          return jsonToolResult({
            ok: true as const,
            data: {
              ring: data.ring,
              urban_radius: data.urban_radius,
              distance_to_pole: data.distance_to_pole,
            },
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
