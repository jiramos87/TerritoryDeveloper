/**
 * MCP tool: ui_toolkit_host_inspect — Host C# AST scan tool.
 *
 * Input: host_class (C# class name).
 * Output: serialized_fields, q_lookups grouped by element kind,
 *   click_bindings, find_object_of_type_chain, modal_slug,
 *   blip_bindings, runtime_ve_constructions.
 *
 * Powers "where does this button click go" reverse-lookups.
 * Outside IUIToolkitPanelBackend per DEC-A28 I4 — Host C# is human-canonical, read-only.
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
import { scanHostClass } from "../ia-db/csharp-host-parser.js";
import { resolveRepoRoot } from "../config.js";

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

const inputSchema = z.object({
  host_class: z
    .string()
    .describe(
      "C# Host class name to scan (e.g. 'BudgetPanelHost'). Scanned under Assets/Scripts/**/*.cs.",
    ),
  path: z
    .string()
    .optional()
    .describe(
      "Repo-relative directory to scan (default: Assets/Scripts/). Passed through to file search.",
    ),
});

type Input = z.infer<typeof inputSchema>;

export function registerUiToolkitHostInspect(server: McpServer): void {
  server.registerTool(
    "ui_toolkit_host_inspect",
    {
      description:
        "Scan a UI Toolkit Host C# class and return structured AST summary: serialized_fields, Q<T> lookups grouped by element kind, RegisterCallback<ClickEvent> bindings, FindObjectOfType chain, modal_slug (OpenModal/ShowPanel), EventBus subscriptions, and runtime VisualElement constructions. Powers reverse-lookup ('where does this button go'). Read-only; outside IUIToolkitPanelBackend boundary. Returns empty shape (file:null) when class not found rather than throwing.",
      inputSchema,
    },
    async (args) =>
      runWithToolTiming("ui_toolkit_host_inspect", async () => {
        const envelope = await wrapTool(async (input: Input) => {
          const hostClass = input.host_class.trim();
          if (!hostClass) {
            throw { code: "invalid_input" as const, message: "host_class is required." };
          }

          const repoRoot = resolveRepoRoot();
          const summary = scanHostClass(hostClass, repoRoot);
          return summary;
        })(inputSchema.parse(args ?? {}));
        return jsonResult(envelope);
      }),
  );
}
