/**
 * MCP tool: ui_toolkit_panel_node_upsert — UXML tree mutation tool.
 *
 * Writes/updates a VisualElement node in a panel's UXML file via DiskBackend.
 * Idempotent on natural key (slug, parent_path, name): re-run same args = no-op.
 * Seeds USS stub when --seed-uss=true.
 * Allow-list gated: spec-implementer | plan-author.
 *
 * Implemented in: TECH-34919 (ui-toolkit-authoring-mcp-slices Stage 2).
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
import { createBackend } from "../ia-db/ui-toolkit-backend.js";
import {
  UXML_ELEMENT_KINDS,
  KIND_TO_UXML_TAG,
  validatePanelKind,
  assertCallerAuthorized,
  type UxmlElementKind,
} from "./_ui-toolkit-shared.js";

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

const inputSchema = z.object({
  slug: z.string().min(1).describe("Panel slug. Matches .uxml filename stem."),
  parent_path: z
    .string()
    .describe(
      "XPath-style path to the parent element (e.g. 'root/content-area'). Use 'root' or empty string for top-level insertion.",
    ),
  kind: z
    .enum(UXML_ELEMENT_KINDS)
    .describe("UXML element kind. One of: " + UXML_ELEMENT_KINDS.join(", ")),
  name: z.string().min(1).describe("Element name attribute. Part of natural key with (slug, parent_path)."),
  classes: z.array(z.string()).optional().default([]).describe("CSS class list applied via class attribute."),
  params: z
    .record(z.string(), z.unknown())
    .optional()
    .default({})
    .describe("Per-kind validated params (e.g. action_id for button, text for label)."),
  ord: z
    .number()
    .int()
    .optional()
    .describe("Insertion order hint (0-based). Informational for now — DiskBackend appends after parent open-tag."),
  inline_style: z
    .record(z.string(), z.string())
    .optional()
    .describe("Optional inline style properties to set as style attribute on the element."),
  seed_uss: z
    .boolean()
    .optional()
    .default(false)
    .describe("When true, seeds a USS stub for each class in classes[] into the panel's generated .uss file."),
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

export function registerUiToolkitPanelNodeUpsert(server: McpServer): void {
  server.registerTool(
    "ui_toolkit_panel_node_upsert",
    {
      description:
        "Upsert a UXML VisualElement node into a panel's .uxml file. Idempotent on (slug, parent_path, name) — re-run with same args is a no-op. Per-kind params validated (button→action_id, slider→low-value/high-value, dropdown→choices, etc.). Optionally seeds USS stub for each class. Allow-list gated: spec-implementer | plan-author.",
      inputSchema,
    },
    async (args) =>
      runWithToolTiming("ui_toolkit_panel_node_upsert", async () => {
        const envelope = await wrapTool(async (input: Input) => {
          // Allow-list check
          assertCallerAuthorized(input.caller);

          // Per-kind param validation
          const kind = input.kind as UxmlElementKind;
          const validatedParams = validatePanelKind(kind, input.params as Record<string, unknown>);

          const tag = KIND_TO_UXML_TAG[kind];

          // Build attrs from inline_style + validated params that map to UXML attrs
          const attrs: Record<string, string> = {};

          // Common UXML attribute mappings from params
          if ("text" in validatedParams && typeof validatedParams["text"] === "string") {
            attrs["text"] = validatedParams["text"];
          }
          if ("label" in validatedParams && typeof validatedParams["label"] === "string") {
            attrs["label"] = validatedParams["label"];
          }
          if ("low-value" in validatedParams) {
            attrs["low-value"] = String(validatedParams["low-value"]);
          }
          if ("high-value" in validatedParams) {
            attrs["high-value"] = String(validatedParams["high-value"]);
          }
          if ("action_id" in validatedParams && typeof validatedParams["action_id"] === "string") {
            // action_id is stored as a custom attribute on the element
            attrs["action_id"] = validatedParams["action_id"];
          }
          if ("choices" in validatedParams && Array.isArray(validatedParams["choices"])) {
            // DropdownField choices serialized as semicolon-separated string for Unity
            attrs["choices"] = (validatedParams["choices"] as string[]).join(";");
          }

          if (input.inline_style && Object.keys(input.inline_style).length > 0) {
            const styleStr = Object.entries(input.inline_style)
              .map(([k, v]) => `${k}: ${v}`)
              .join("; ");
            attrs["style"] = styleStr;
          }

          const backend = createBackend({ kind: input.backend });
          const result = await backend.upsertNode(
            input.slug,
            input.parent_path,
            {
              tag,
              name: input.name,
              classes: input.classes,
              attrs,
            },
            input.ord,
          );

          if (!result.ok) {
            throw { code: "upsert_failed" as const, message: result.error };
          }

          // Seed USS stub if requested
          const seeded_classes: string[] = [];
          if (input.seed_uss && input.classes && input.classes.length > 0) {
            for (const cls of input.classes) {
              const selector = `.${cls}`;
              const ussResult = await backend.upsertUssRule(input.slug, selector, {}, "append");
              if (ussResult.ok && !ussResult.idempotent) {
                seeded_classes.push(selector);
              }
            }
          }

          return {
            ok: true,
            idempotent: result.idempotent ?? false,
            slug: input.slug,
            name: input.name,
            kind,
            tag,
            parent_path: input.parent_path,
            seeded_uss_stubs: seeded_classes,
            validated_params: validatedParams,
          };
        })(inputSchema.parse(args ?? {}));
        return jsonResult(envelope);
      }),
  );
}
