/**
 * MCP tool: list_rules — all Cursor `.mdc` rules with frontmatter metadata.
 */

import fs from "node:fs";
import matter from "gray-matter";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import type { SpecRegistryEntry } from "../parser/types.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";

/**
 * Normalize `globs` from YAML (string, array, or absent) for JSON output.
 */
export function formatRuleGlobs(value: unknown): string | null {
  if (value === undefined || value === null) return null;
  if (typeof value === "string") return value;
  if (Array.isArray(value)) return value.map(String).join(", ");
  return String(value);
}

/**
 * Register the list_rules tool (no inputs).
 */
export function registerListRules(
  server: McpServer,
  registry: SpecRegistryEntry[],
): void {
  server.registerTool(
    "list_rules",
    {
      description:
        "List all Cursor rule files (.mdc) with descriptions and frontmatter metadata.",
    },
    async () =>
      runWithToolTiming("list_rules", async () => {
        const envelope = await wrapTool(async () => {
          const rules = registry.filter((e) => e.category === "rule");
          const rows = rules.map((e) => {
            const raw = fs.readFileSync(e.filePath, "utf8");
            const { data } = matter(raw);
            const d = data as Record<string, unknown>;
            const alwaysApply =
              typeof d.alwaysApply === "boolean" ? d.alwaysApply : false;
            const globs = formatRuleGlobs(d.globs);
            return {
              key: e.key,
              fileName: e.fileName,
              description: e.description,
              alwaysApply,
              globs,
            };
          });
          return { rules: rows };
        })(undefined);
        return {
          content: [
            {
              type: "text" as const,
              text: JSON.stringify(envelope, null, 2),
            },
          ],
        };
      }),
  );
}
