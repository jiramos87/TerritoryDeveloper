/**
 * MCP tool: list_specs — IA document registry with optional category filter.
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import fs from "node:fs";
import type { SpecRegistryEntry } from "../parser/types.js";
import { splitLines } from "../parser/markdown-parser.js";
import { relativePathForEntry, resolveRepoRoot } from "../config.js";

const listSpecsInputShape = {
  category: z
    .enum(["spec", "rule", "root-doc", "all"])
    .optional()
    .describe("Filter by category. Defaults to 'all'."),
};

function jsonResult(payload: unknown) {
  return {
    content: [
      {
        type: "text" as const,
        text: JSON.stringify(payload, null, 2),
      },
    ],
  };
}

/**
 * Register the list_specs tool on the given server.
 */
export function registerListSpecs(
  server: McpServer,
  registry: SpecRegistryEntry[],
): void {
  const repoRoot = resolveRepoRoot();

  server.registerTool(
    "list_specs",
    {
      description:
        "List all Information Architecture documents (specs, rules, root docs) available for querying.",
      inputSchema: listSpecsInputShape,
    },
    async (args) => {
      const category = args?.category ?? "all";

      const filtered =
        category === "all"
          ? registry
          : registry.filter((e) => e.category === category);

      const rows = filtered.map((e) => {
        const raw = fs.readFileSync(e.filePath, "utf8");
        const lineCount = splitLines(raw).length;
        return {
          key: e.key,
          fileName: e.fileName,
          relativePath: relativePathForEntry(repoRoot, e),
          description: e.description,
          category: e.category,
          lineCount,
        };
      });

      return jsonResult(rows);
    },
  );
}
