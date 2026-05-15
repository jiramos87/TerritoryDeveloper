/**
 * MCP tool: ui_toolkit_panel_node_remove — cascade delete + orphan USS drift report.
 *
 * Removes a UXML node (and its subtree) from a panel's .uxml file.
 * Scans the panel's .uss file for orphan class selectors after removal.
 * Does NOT auto-delete orphan USS rules — emits drift report to caller.
 * Idempotent on (slug, node_path): removing a non-existent path is a no-op.
 * Allow-list gated: spec-implementer | plan-author.
 *
 * Implemented in: TECH-34920 (ui-toolkit-authoring-mcp-slices Stage 2).
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
import { createBackend } from "../ia-db/ui-toolkit-backend.js";
import { assertCallerAuthorized } from "./_ui-toolkit-shared.js";

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

const inputSchema = z.object({
  slug: z.string().min(1).describe("Panel slug. Matches .uxml filename stem."),
  node_path: z
    .string()
    .min(1)
    .describe(
      "Path to the node to remove (e.g. 'root/content-area/close-button' or just 'close-button'). Last segment is the element name attribute.",
    ),
  caller: z
    .string()
    .optional()
    .default("spec-implementer")
    .describe("Caller identity for allow-list check. Must be 'spec-implementer' or 'plan-author'."),
  backend: z
    .enum(["disk", "db"])
    .optional()
    .describe("Override backend. Default: disk."),
});

type Input = z.infer<typeof inputSchema>;

export function registerUiToolkitPanelNodeRemove(server: McpServer): void {
  server.registerTool(
    "ui_toolkit_panel_node_remove",
    {
      description:
        "Remove a UXML node and its entire subtree from a panel's .uxml file. Idempotent on (slug, node_path) — removing a non-existent path is a clean no-op. Post-removal USS drift report: emits orphan_uss_rules[] (class selectors in the panel's .uss that now have no matching UXML elements). Orphan rules are NOT auto-deleted — caller decides whether to clean them. Allow-list gated: spec-implementer | plan-author.",
      inputSchema,
    },
    async (args) =>
      runWithToolTiming("ui_toolkit_panel_node_remove", async () => {
        const envelope = await wrapTool(async (input: Input) => {
          // Allow-list check
          assertCallerAuthorized(input.caller);

          const backend = createBackend({ kind: input.backend });
          const result = await backend.removeNode(input.slug, input.node_path);

          if (!result.ok) {
            throw { code: "remove_failed" as const, message: result.error };
          }

          return {
            ok: true,
            idempotent: result.idempotent ?? false,
            slug: input.slug,
            node_path: input.node_path,
            orphan_uss_rules: result.orphan_uss_rules ?? [],
            note: (result.orphan_uss_rules ?? []).length > 0
              ? "Orphan USS selectors found. Review and clean manually — auto-delete skipped to preserve hand-tuned overrides."
              : "No orphan USS selectors detected.",
          };
        })(inputSchema.parse(args ?? {}));
        return jsonResult(envelope);
      }),
  );
}
