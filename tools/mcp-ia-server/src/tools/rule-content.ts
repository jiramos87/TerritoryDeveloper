/**
 * MCP tool: rule_content — Markdown body of a `.mdc` rule (frontmatter stripped).
 */

import fs from "node:fs";
import matter from "gray-matter";
import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import type { SpecRegistryEntry } from "../parser/types.js";
import { findRuleEntry } from "../config.js";
import { formatRuleGlobs } from "./list-rules.js";
import { runWithToolTiming } from "../instrumentation.js";

const DEFAULT_MAX_CHARS = 3000;

const inputShape = {
  rule: z
    .string()
    .describe(
      "Key or filename (e.g. 'invariants', 'coding-conventions', 'roads').",
    ),
  max_chars: z
    .number()
    .optional()
    .describe("Maximum characters to return. Default: 3000."),
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

function truncateContent(
  content: string,
  maxChars: number,
): { text: string; truncated: boolean; totalChars: number } {
  const totalChars = content.length;
  if (content.length <= maxChars) {
    return { text: content, truncated: false, totalChars };
  }
  const slice = content.slice(0, maxChars);
  const lastNl = slice.lastIndexOf("\n");
  const cut = lastNl > 0 ? slice.slice(0, lastNl) : slice;
  return { text: cut, truncated: true, totalChars };
}

/**
 * Register the rule_content tool.
 */
export function registerRuleContent(
  server: McpServer,
  registry: SpecRegistryEntry[],
): void {
  server.registerTool(
    "rule_content",
    {
      description:
        "Retrieve the full Markdown body of a Cursor rule (.mdc), without YAML frontmatter.",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("rule_content", async () => {
        const ruleKey = args?.rule ?? "";
        const maxChars = args?.max_chars ?? DEFAULT_MAX_CHARS;

        const entry = findRuleEntry(registry, ruleKey);
        if (!entry) {
          const available_rules = registry
            .filter((e) => e.category === "rule")
            .map((e) => ({ key: e.key, description: e.description }))
            .sort((a, b) => a.key.localeCompare(b.key));
          return jsonResult({
            error: "unknown_rule",
            message: `No rule found for '${ruleKey}'. Use list_rules to see available rules.`,
            available_rules,
          });
        }

        const raw = fs.readFileSync(entry.filePath, "utf8");
        const { data, content } = matter(raw);
        const d = data as Record<string, unknown>;
        const description =
          typeof d.description === "string" ? d.description : entry.description;
        const alwaysApply =
          typeof d.alwaysApply === "boolean" ? d.alwaysApply : false;
        const globs = formatRuleGlobs(d.globs);
        const body = content.trimStart();
        const { text, truncated, totalChars } = truncateContent(body, maxChars);

        return jsonResult({
          key: entry.key,
          fileName: entry.fileName,
          description,
          alwaysApply,
          globs,
          content: text,
          truncated,
          totalChars,
        });
      }),
  );
}
