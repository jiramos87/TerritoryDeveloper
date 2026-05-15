/**
 * _ui-toolkit-shared.ts — shared Zod schemas + per-kind validators + idempotency contract.
 *
 * Used by Stage 2 mutation tools:
 *   - ui_toolkit_panel_node_upsert (T2.1)
 *   - ui_toolkit_panel_node_remove (T2.2)
 *   - ui_toolkit_uss_rule_upsert   (T2.3)
 *
 * Idempotency contract: re-serialize-then-byte-compare-before-write.
 * Natural key per operation:
 *   - node upsert: (slug, parent_path, name)
 *   - node remove: (slug, node_path)  — no-op when absent
 *   - uss rule:    (slug, selector)
 */

import { z } from "zod";

// ---------------------------------------------------------------------------
// Allow-list
// ---------------------------------------------------------------------------

/** Callers permitted to invoke mutation tools. */
export const MUTATION_ALLOW_LIST = ["spec-implementer", "plan-author"] as const;

export type AllowedCaller = (typeof MUTATION_ALLOW_LIST)[number];

/**
 * Assert caller is allow-listed.
 * Throws structured error when not authorized.
 */
export function assertCallerAuthorized(caller: string): void {
  if (!(MUTATION_ALLOW_LIST as readonly string[]).includes(caller)) {
    throw {
      code: "unauthorized" as const,
      message: `Caller '${caller}' is not in allow-list. Permitted: ${MUTATION_ALLOW_LIST.join(", ")}.`,
    };
  }
}

// ---------------------------------------------------------------------------
// 9 UXML element kinds (matches panel-schema.yaml panel_kind.ui-toolkit-overlay)
// ---------------------------------------------------------------------------

export const UXML_ELEMENT_KINDS = [
  "button",
  "label",
  "slider",
  "toggle",
  "dropdown",
  "text-field",
  "integer-field",
  "scroll-view",
  "visual-element",
] as const;

export type UxmlElementKind = (typeof UXML_ELEMENT_KINDS)[number];

/** Map kind → Unity UXML tag (ui:XXX) */
export const KIND_TO_UXML_TAG: Record<UxmlElementKind, string> = {
  button: "ui:Button",
  label: "ui:Label",
  slider: "ui:Slider",
  toggle: "ui:Toggle",
  dropdown: "ui:DropdownField",
  "text-field": "ui:TextField",
  "integer-field": "ui:IntegerField",
  "scroll-view": "ui:ScrollView",
  "visual-element": "ui:VisualElement",
};

// ---------------------------------------------------------------------------
// Per-kind param schemas
// ---------------------------------------------------------------------------

const buttonParamsSchema = z.object({
  action_id: z.string().min(1, "button requires action_id"),
});

const labelParamsSchema = z.object({
  text: z.string().min(1, "label requires text"),
});

const sliderParamsSchema = z.object({
  "low-value": z.union([z.string(), z.number()]),
  "high-value": z.union([z.string(), z.number()]),
});

const toggleParamsSchema = z.object({
  label: z.string().min(1, "toggle requires label"),
});

const dropdownParamsSchema = z.object({
  choices: z.array(z.string()).min(1, "dropdown requires at least one choice"),
});

const textFieldParamsSchema = z.object({
  label: z.string().min(1, "text-field requires label"),
});

const integerFieldParamsSchema = z.object({
  label: z.string().min(1, "integer-field requires label"),
});

const scrollViewParamsSchema = z.object({}).passthrough();

const visualElementParamsSchema = z.object({}).passthrough();

/** Validate per-kind params. Returns parsed params or throws ZodError. */
export function validatePanelKind(kind: UxmlElementKind, params: Record<string, unknown>): Record<string, unknown> {
  switch (kind) {
    case "button":
      return buttonParamsSchema.parse(params);
    case "label":
      return labelParamsSchema.parse(params);
    case "slider":
      return sliderParamsSchema.parse(params);
    case "toggle":
      return toggleParamsSchema.parse(params);
    case "dropdown":
      return dropdownParamsSchema.parse(params);
    case "text-field":
      return textFieldParamsSchema.parse(params);
    case "integer-field":
      return integerFieldParamsSchema.parse(params);
    case "scroll-view":
      return scrollViewParamsSchema.parse(params);
    case "visual-element":
      return visualElementParamsSchema.parse(params);
  }
}

// ---------------------------------------------------------------------------
// Idempotency contract: byte-compare before write
// ---------------------------------------------------------------------------

/**
 * Compare new content against existing content.
 *
 * Returns true when content is byte-for-byte identical → write is a no-op.
 * Caller skips write + returns { ok:true, idempotent:true }.
 */
export function isIdempotentWrite(existing: string, proposed: string): boolean {
  return existing === proposed;
}

// ---------------------------------------------------------------------------
// Position type for USS rule ordering
// ---------------------------------------------------------------------------

export type UssRulePosition =
  | "prepend"
  | "append"
  | `before:${string}`
  | `after:${string}`;

/** Parse a position string into structured form. */
export function parseUssPosition(pos: string): { kind: "prepend" | "append" | "before" | "after"; ref?: string } {
  if (pos === "prepend") return { kind: "prepend" };
  if (pos === "append") return { kind: "append" };
  if (pos.startsWith("before:")) return { kind: "before", ref: pos.slice(7) };
  if (pos.startsWith("after:")) return { kind: "after", ref: pos.slice(6) };
  return { kind: "append" }; // default fallback
}

// ---------------------------------------------------------------------------
// USS serializer — round-trip write preserving literal hex
// ---------------------------------------------------------------------------

/**
 * Serialize a flat list of UssRule objects back to .uss text.
 * Preserves literal hex values verbatim (no normalisation).
 */
export interface SerializableUssRule {
  selector: string;
  props: Record<string, string>;
}

export function serializeUssRules(rules: SerializableUssRule[]): string {
  const blocks: string[] = [];
  for (const rule of rules) {
    const propLines = Object.entries(rule.props)
      .map(([k, v]) => `    ${k}: ${v};`)
      .join("\n");
    blocks.push(`${rule.selector} {\n${propLines}\n}`);
  }
  return blocks.join("\n\n") + (blocks.length > 0 ? "\n" : "");
}
