/**
 * Bespoke JSON Schema validator (TECH-1673).
 *
 * Covers the draft-07 subset used by `archetype_version.params_schema`:
 *   - type (string|number|integer|boolean|array|object) + nullable arrays
 *   - enum
 *   - minimum / maximum / multipleOf
 *   - required (object only)
 *   - items (array)
 *   - properties (object, recursive)
 *
 * Ajv intentionally omitted — dependency cost > value for this surface.
 */

import type { JsonSchemaNode, ValidationError, ValidationResult } from "./types";

function joinPath(parent: string, key: string | number): string {
  if (parent === "") return String(key);
  return `${parent}.${key}`;
}

function typeOfValue(value: unknown): string {
  if (value === null) return "null";
  if (Array.isArray(value)) return "array";
  return typeof value;
}

function matchesType(value: unknown, t: JsonSchemaNode["type"]): boolean {
  if (t === undefined) return true;
  const types = Array.isArray(t) ? t : [t];
  const actual = typeOfValue(value);
  for (const expected of types) {
    if (expected === "integer") {
      if (actual === "number" && Number.isInteger(value as number)) return true;
    } else if (expected === actual) {
      return true;
    }
  }
  return false;
}

function validateNode(
  schema: JsonSchemaNode,
  value: unknown,
  path: string,
  errors: ValidationError[],
): void {
  // Type check (skip when value undefined and not required — caller decides).
  if (value === undefined) return;

  if (!matchesType(value, schema.type)) {
    errors.push({ path, message: `expected ${String(schema.type)}, got ${typeOfValue(value)}` });
    return;
  }

  if (schema.enum && !schema.enum.includes(value as string | number)) {
    errors.push({ path, message: `must be one of ${schema.enum.join(", ")}` });
  }

  if (typeof value === "number") {
    if (schema.minimum !== undefined && value < schema.minimum) {
      errors.push({ path, message: `must be ≥ ${schema.minimum}` });
    }
    if (schema.maximum !== undefined && value > schema.maximum) {
      errors.push({ path, message: `must be ≤ ${schema.maximum}` });
    }
    if (schema.multipleOf !== undefined) {
      const rem = Math.abs(value / schema.multipleOf - Math.round(value / schema.multipleOf));
      if (rem > 1e-9) {
        errors.push({ path, message: `must be a multiple of ${schema.multipleOf}` });
      }
    }
  }

  if (schema.type === "array" && Array.isArray(value) && schema.items) {
    for (let i = 0; i < value.length; i++) {
      validateNode(schema.items, value[i], joinPath(path, i), errors);
    }
  }

  if (schema.type === "object" && schema.properties && value !== null && typeof value === "object") {
    const obj = value as Record<string, unknown>;
    if (schema.required) {
      for (const req of schema.required) {
        if (obj[req] === undefined || obj[req] === null || obj[req] === "") {
          errors.push({ path: joinPath(path, req), message: "is required" });
        }
      }
    }
    for (const [propKey, propSchema] of Object.entries(schema.properties)) {
      if (obj[propKey] !== undefined) {
        validateNode(propSchema, obj[propKey], joinPath(path, propKey), errors);
      }
    }
  }
}

export function validate(schema: JsonSchemaNode, value: unknown): ValidationResult {
  const errors: ValidationError[] = [];
  // Top-level required check uses schema's own required + properties (object root).
  if (schema.type === "object" && schema.required && (value === null || typeof value !== "object")) {
    for (const req of schema.required) {
      errors.push({ path: req, message: "is required" });
    }
    return { valid: false, errors };
  }
  validateNode(schema, value, "", errors);
  return { valid: errors.length === 0, errors };
}

/**
 * Build the default value tree from schema-level `default` keywords. Walks
 * `properties` + `items` recursively. Used by Reset-to-defaults.
 */
export function defaultValueOf(schema: JsonSchemaNode): unknown {
  if (schema.default !== undefined) return JSON.parse(JSON.stringify(schema.default));
  if (schema.type === "object" && schema.properties) {
    const out: Record<string, unknown> = {};
    for (const [k, v] of Object.entries(schema.properties)) {
      const dv = defaultValueOf(v);
      if (dv !== undefined) out[k] = dv;
    }
    return out;
  }
  if (schema.type === "array") return [];
  return undefined;
}
