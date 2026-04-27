/**
 * Schema-driven form types (TECH-1673).
 *
 * Subset of JSON Schema draft-07 sufficient for `archetype_version.params_schema`
 * + `ui_hints_json` per DEC-A45. Widget registry maps `(schema_type, hint.widget)`
 * → React component.
 */

export type JsonSchemaType =
  | "string"
  | "number"
  | "integer"
  | "boolean"
  | "array"
  | "object";

export type JsonSchemaNode = {
  type?: JsonSchemaType | JsonSchemaType[];
  title?: string;
  description?: string;
  enum?: ReadonlyArray<string | number>;
  default?: unknown;
  minimum?: number;
  maximum?: number;
  multipleOf?: number;
  format?: string;
  items?: JsonSchemaNode;
  properties?: Record<string, JsonSchemaNode>;
  required?: ReadonlyArray<string>;
  // Bespoke extension marker used in tests + UI hints.
  $widget?: WidgetKind;
};

export type WidgetKind =
  | "slider"
  | "knob"
  | "enum"
  | "bool"
  | "color"
  | "entity_ref"
  | "array"
  | "object";

export type FieldHint = {
  widget?: WidgetKind;
  step?: number;
  /** Used by `enum` widget to switch between `<select>` (default) and radio. */
  style?: "select" | "radio";
  /** Used by `entity_ref` widget — kind of catalog entity to look up. */
  kind?: string;
  /** Free-form bag for forward compat. */
  [k: string]: unknown;
};

export type UiHints = Record<string, FieldHint>;

export type ValidationError = {
  path: string;
  message: string;
};

export type ValidationResult = {
  valid: boolean;
  errors: ValidationError[];
};
