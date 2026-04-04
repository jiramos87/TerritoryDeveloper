/**
 * MCP tool: desirability_top_cells — requires Unity batchmode grid export (BACKLOG TECH-66); honest NOT_AVAILABLE until implemented.
 */

import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { runWithToolTiming } from "../../instrumentation.js";
import { jsonToolResult } from "./jsonToolResult.js";

const desirabilityTopCellsShape = {
  k: z.number().int().min(1).max(64).optional(),
};

export function registerDesirabilityTopCells(server: McpServer): void {
  server.registerTool(
    "desirability_top_cells",
    {
      description:
        "Reserved for top-k cells by desirability scores (glossary: Desirability) from a live grid export. " +
        "Not implemented until BACKLOG TECH-66 (Unity batchmode export) — returns NOT_AVAILABLE with guidance.",
      inputSchema: desirabilityTopCellsShape,
    },
    async (args: unknown) =>
      runWithToolTiming("desirability_top_cells", async () => {
        z.object(desirabilityTopCellsShape).parse(args ?? {});
        return jsonToolResult({
          ok: false as const,
          error: {
            code: "NOT_AVAILABLE" as const,
            message:
              "desirability_top_cells requires Unity batchmode grid export (BACKLOG TECH-66; trace: glossary Computational MCP tools (TECH-39)). " +
              "Use spec_section and managers-reference Demand until the export ships.",
          },
        });
      }),
  );
}
