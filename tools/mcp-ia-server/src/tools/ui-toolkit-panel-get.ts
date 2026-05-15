/**
 * MCP tool: ui_toolkit_panel_get — composite read returning UXML tree + USS + Host C# summary.
 *
 * Single round-trip replaces 4-5 sequential Read/grep passes.
 * Delegates to IUIToolkitPanelBackend.getPanel(slug).
 * Host AST scan delegates to csharp-host-parser.ts (T1.2).
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
import { createBackend } from "../ia-db/ui-toolkit-backend.js";
import { scanHostClass } from "../ia-db/csharp-host-parser.js";
import { resolveRepoRoot } from "../config.js";

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

const inputSchema = z.object({
  slug: z
    .string()
    .describe("Panel slug (e.g. 'budget-panel'). Matches .uxml filename stem."),
  host_class: z
    .string()
    .optional()
    .describe(
      "Optional Host C# class name (e.g. 'BudgetPanelHost'). When provided, ui_toolkit_host_inspect is called inline and included in the result.",
    ),
  backend: z
    .enum(["disk", "db"])
    .optional()
    .describe("Override backend (disk|db). Default: UI_TOOLKIT_BACKEND env or 'disk'."),
});

type Input = z.infer<typeof inputSchema>;

export function registerUiToolkitPanelGet(server: McpServer): void {
  server.registerTool(
    "ui_toolkit_panel_get",
    {
      description:
        "Composite read for a UI Toolkit panel: UXML content + parsed VisualElement tree + per-panel USS rules + optional Host C# structural summary + golden manifest entry. Single round-trip replaces 4-5 sequential file reads + grep passes. Returns `exists:false` (no throw) when UXML not found. Requires the panel .uxml file to exist under Assets/UI/ subtree.",
      inputSchema,
    },
    async (args) =>
      runWithToolTiming("ui_toolkit_panel_get", async () => {
        const envelope = await wrapTool(async (input: Input) => {
          const slug = input.slug.trim();
          if (!slug) {
            throw { code: "invalid_input" as const, message: "slug is required." };
          }

          const backend = createBackend({ kind: input.backend });
          const panel = await backend.getPanel(slug);

          // Inline host inspect when host_class provided
          let host_summary: unknown = null;
          if (input.host_class) {
            const repoRoot = resolveRepoRoot();
            host_summary = scanHostClass(input.host_class, repoRoot);
          }

          return {
            slug: panel.slug,
            exists: panel.exists,
            uxml_path: panel.uxml_path,
            uxml_content: panel.uxml_content,
            uxml_tree: panel.uxml_tree,
            uss_rules: panel.uss_rules,
            uss_paths: panel.uss_paths,
            scene_uidoc: panel.scene_uidoc,
            golden_manifest: panel.golden_manifest,
            host_summary,
          };
        })(inputSchema.parse(args ?? {}));
        return jsonResult(envelope);
      }),
  );
}
