/**
 * Pure schema-shape validator for archetype `params_json` editor (TECH-2460 / Stage 11.1).
 *
 * Editor surfaces a row-per-field UX over a `JsonSchemaNode.properties` object.
 * `validateSchemaShape` enforces editor-level invariants that the JSON Schema
 * spec itself does not (slug uniqueness when projected to editor rows, enum
 * non-empty, default-vs-type pairing per row).
 *
 * Lives in `lib/` per `web-backend-logic` rule — components only render the
 * `{ok, errors}` result, never re-implement the rules.
 *
 * @see ia/projects/asset-pipeline/stage-11.1 — TECH-2460 §Plan Digest
 */
import type { JsonSchemaNode } from "@/lib/json-schema-form/types";

export type SchemaShapeError = {
  /** Dotted path the editor uses internally; not a JSON Pointer. */
  path: string;
  message: string;
};

export type SchemaShapeResult =
  | { ok: true }
  | { ok: false; errors: SchemaShapeError[] };

/** Slug regex used by editor field rows: lowercase alnum + underscores, must start with letter. */
export const FIELD_SLUG_RE = /^[a-z][a-z0-9_]{0,63}$/;

/**
 * Walks `schema.properties` row-by-row and reports editor-level violations.
 * Caller passes the editor's working `JsonSchemaNode`; we never mutate.
 */
export function validateSchemaShape(schema: JsonSchemaNode): SchemaShapeResult {
  const errors: SchemaShapeError[] = [];

  if (typeof schema !== "object" || schema == null) {
    return { ok: false, errors: [{ path: "$", message: "schema must be object" }] };
  }
  const props = schema.properties ?? {};
  if (typeof props !== "object" || Array.isArray(props)) {
    return {
      ok: false,
      errors: [{ path: "properties", message: "properties must be object" }],
    };
  }

  const seenSlugs = new Set<string>();
  for (const [slug, node] of Object.entries(props)) {
    if (!FIELD_SLUG_RE.test(slug)) {
      errors.push({
        path: `properties.${slug}`,
        message: `slug must match ${FIELD_SLUG_RE}`,
      });
    }
    if (seenSlugs.has(slug)) {
      errors.push({
        path: `properties.${slug}`,
        message: "duplicate field slug",
      });
    }
    seenSlugs.add(slug);
    errors.push(...validateNodeShape(slug, node));
  }
  return errors.length === 0 ? { ok: true } : { ok: false, errors };
}

function validateNodeShape(slug: string, node: JsonSchemaNode): SchemaShapeError[] {
  const out: SchemaShapeError[] = [];
  const path = `properties.${slug}`;
  if (typeof node !== "object" || node == null) {
    out.push({ path, message: "field node must be object" });
    return out;
  }
  if (node.type === undefined) {
    out.push({ path: `${path}.type`, message: "type required" });
    return out;
  }
  const t = Array.isArray(node.type) ? node.type[0] : node.type;
  // Enum-only rule: if `enum` array present it must be non-empty.
  if (node.enum !== undefined) {
    if (!Array.isArray(node.enum) || node.enum.length === 0) {
      out.push({ path: `${path}.enum`, message: "enum must be non-empty array" });
    }
  }
  // default-vs-type pairing.
  if (node.default !== undefined) {
    if (!matchesType(t, node.default)) {
      out.push({
        path: `${path}.default`,
        message: `default value does not match type ${String(t)}`,
      });
    }
    if (
      node.enum !== undefined &&
      Array.isArray(node.enum) &&
      node.enum.length > 0 &&
      !(node.enum as ReadonlyArray<unknown>).includes(node.default as never)
    ) {
      out.push({
        path: `${path}.default`,
        message: "default not in enum",
      });
    }
  }
  // numeric bound sanity.
  if (
    typeof node.minimum === "number" &&
    typeof node.maximum === "number" &&
    node.minimum > node.maximum
  ) {
    out.push({
      path: `${path}.minimum`,
      message: "minimum greater than maximum",
    });
  }
  return out;
}

function matchesType(t: unknown, v: unknown): boolean {
  switch (t) {
    case "string":
      return typeof v === "string";
    case "boolean":
      return typeof v === "boolean";
    case "integer":
      return typeof v === "number" && Number.isInteger(v);
    case "number":
      return typeof v === "number";
    case "array":
      return Array.isArray(v);
    case "object":
      return typeof v === "object" && v !== null && !Array.isArray(v);
    default:
      return false;
  }
}
