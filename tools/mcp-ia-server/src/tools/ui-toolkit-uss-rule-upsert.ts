/**
 * MCP tool: ui_toolkit_uss_rule_upsert — literal-hex preserving USS rule tool.
 *
 * Writes/updates a CSS rule in a panel's .uss file.
 * Preserves literal hex color values verbatim — no #5b7fa8 → rgb(...) conversion.
 * Idempotent on (slug, selector): re-run with same props = no-op.
 * Supports position: prepend | append | before:{selector} | after:{selector}.
 * Allow-list gated: spec-implementer | plan-author.
 *
 * Implemented in: TECH-34921 (ui-toolkit-authoring-mcp-slices Stage 2).
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
  slug: z.string().min(1).describe("Panel slug. Matches .uss filename stem."),
  selector: z
    .string()
    .min(1)
    .describe("CSS selector string (e.g. '.my-button', '#panel-root', '.row:hover')."),
  properties: z
    .record(z.string(), z.string())
    .describe(
      "CSS property key→value pairs. Literal hex values (e.g. #5b7fa8) preserved verbatim — no normalisation.",
    ),
  position: z
    .string()
    .optional()
    .default("append")
    .describe(
      "Insertion position: 'prepend' | 'append' | 'before:{other-selector}' | 'after:{other-selector}'. Default: append.",
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

export function registerUiToolkitUssRuleUpsert(server: McpServer): void {
  server.registerTool(
    "ui_toolkit_uss_rule_upsert",
    {
      description:
        "Upsert a CSS rule into a panel's .uss file. Idempotent on (slug, selector) — re-run with same properties is a no-op. Literal hex color values preserved verbatim (no rgb() conversion). Supports position: prepend | append | before:{selector} | after:{selector}. Allow-list gated: spec-implementer | plan-author.",
      inputSchema,
    },
    async (args) =>
      runWithToolTiming("ui_toolkit_uss_rule_upsert", async () => {
        const envelope = await wrapTool(async (input: Input) => {
          // Allow-list check
          assertCallerAuthorized(input.caller);

          if (!input.selector.trim()) {
            throw { code: "invalid_input" as const, message: "selector must be non-empty." };
          }

          const backend = createBackend({ kind: input.backend });
          const result = await backend.upsertUssRule(
            input.slug,
            input.selector,
            input.properties,
            input.position,
          );

          if (!result.ok) {
            throw { code: "upsert_uss_failed" as const, message: result.error };
          }

          return {
            ok: true,
            idempotent: result.idempotent ?? false,
            slug: input.slug,
            selector: input.selector,
            properties: input.properties,
            position: input.position,
            note: result.idempotent
              ? "No change — selector already exists with identical properties."
              : "USS rule written.",
          };
        })(inputSchema.parse(args ?? {}));
        return jsonResult(envelope);
      }),
  );
}
