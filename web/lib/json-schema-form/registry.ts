"use client";

import type { ComponentType } from "react";

import BoolWidget from "./widgets/Bool";
import ColorWidget from "./widgets/Color";
import EntityRefWidget from "./widgets/EntityRef";
import EnumWidget from "./widgets/EnumSelect";
import KnobWidget from "./widgets/Knob";
import SliderWidget from "./widgets/Slider";
import type { FieldHint, JsonSchemaNode, WidgetKind } from "./types";

/**
 * Widget contract — every widget receives the leaf schema node, optional
 * UI hint, current value, dotted path, and an `onChange(value)` callback.
 * Container widgets (`array`, `object`) additionally receive
 * `renderChild(childSchema, childHint, childPath, childValue)` so they can
 * recurse into the same registry without forking.
 */
export type WidgetProps = {
  schema: JsonSchemaNode;
  hint?: FieldHint;
  value: unknown;
  path: string;
  onChange: (next: unknown) => void;
  renderChild?: (args: {
    schema: JsonSchemaNode;
    hint?: FieldHint;
    value: unknown;
    path: string;
    onChange: (next: unknown) => void;
  }) => React.ReactNode;
};

export type WidgetRegistry = Record<WidgetKind, ComponentType<WidgetProps>>;

// Lazy-imported recursive widgets to avoid circular import at module init.
import ArrayField from "./widgets/ArrayField";
import ObjectField from "./widgets/ObjectField";

export const widgetRegistry: WidgetRegistry = {
  slider: SliderWidget,
  knob: KnobWidget,
  enum: EnumWidget,
  bool: BoolWidget,
  color: ColorWidget,
  entity_ref: EntityRefWidget,
  array: ArrayField,
  object: ObjectField,
};

/**
 * Resolve `(schema_type, hint.widget) → WidgetKind` per DEC-A45 mapping table.
 * Hint wins when present and matches a known kind; otherwise infer from schema.
 */
export function resolveWidgetKind(schema: JsonSchemaNode, hint?: FieldHint): WidgetKind {
  if (hint?.widget && hint.widget in widgetRegistry) return hint.widget;
  if (schema.$widget && schema.$widget in widgetRegistry) return schema.$widget;
  if (schema.enum && schema.enum.length > 0) return "enum";
  const t = Array.isArray(schema.type) ? schema.type[0] : schema.type;
  if (t === "boolean") return "bool";
  if (t === "number" || t === "integer") return "slider";
  if (t === "array") return "array";
  if (t === "object") return "object";
  if (t === "string") {
    if (schema.format === "color") return "color";
    return "enum"; // fallback — string without enum is rare; surfaces select-of-empty.
  }
  return "object";
}
