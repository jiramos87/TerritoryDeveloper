/**
 * Pure migration-hint validator (TECH-2461 / Stage 11.1).
 *
 * Checks `migration_hint_json` against a `SchemaDiff`:
 * - Every `removed` field MUST have a rule (`drop` or `rename`).
 * - `rename.to` MUST reference an `added` field of the same type.
 * - `default.value` MUST type-match the target `added` field.
 * - `drop.slug` MUST reference a `removed` field.
 *
 * @see ia/projects/asset-pipeline/stage-11.1 — TECH-2461 §Plan Digest
 */
import type { JsonSchemaNode } from "@/lib/json-schema-form/types";
import type { SchemaDiff } from "@/lib/archetype/diff-schemas";

export type MigrationHint = {
  rename?: ReadonlyArray<{ from: string; to: string }>;
  default?: ReadonlyArray<{ slug: string; value: unknown }>;
  drop?: ReadonlyArray<{ slug: string }>;
};

export type HintError = { path: string; message: string };

export type HintValidationResult =
  | { ok: true }
  | { ok: false; errors: HintError[] };

function nodeOf(schema: JsonSchemaNode, slug: string): JsonSchemaNode | null {
  return schema.properties?.[slug] ?? null;
}

function fieldType(node: JsonSchemaNode | null): string {
  if (!node) return "unknown";
  if (Array.isArray(node.type)) return node.type[0] ?? "unknown";
  return (node.type as string | undefined) ?? "unknown";
}

function matchesType(t: string, v: unknown): boolean {
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

export function validateMigrationHint(
  diff: SchemaDiff,
  hint: MigrationHint,
  newSchema: JsonSchemaNode,
): HintValidationResult {
  const errors: HintError[] = [];
  const removedSet = new Set(diff.removed.map((r) => r.slug));
  const addedByName = new Map(diff.added.map((a) => [a.slug, a]));

  // Rule coverage: every removed field needs a drop or a rename.from.
  const renameFroms = new Set((hint.rename ?? []).map((r) => r.from));
  const dropSlugs = new Set((hint.drop ?? []).map((d) => d.slug));
  for (const r of diff.removed) {
    if (!renameFroms.has(r.slug) && !dropSlugs.has(r.slug)) {
      errors.push({
        path: `removed.${r.slug}`,
        message: `removed field ${r.slug} needs a drop or rename rule`,
      });
    }
  }

  // Rename target validity.
  for (const rule of hint.rename ?? []) {
    if (!removedSet.has(rule.from)) {
      errors.push({
        path: `rename.${rule.from}`,
        message: `rename.from ${rule.from} is not a removed field`,
      });
      continue;
    }
    const target = addedByName.get(rule.to);
    if (target == null) {
      errors.push({
        path: `rename.${rule.from}`,
        message: `rename.to ${rule.to} is not an added field`,
      });
      continue;
    }
    const removedRow = diff.removed.find((x) => x.slug === rule.from)!;
    if (target.type !== removedRow.type) {
      errors.push({
        path: `rename.${rule.from}`,
        message: `rename.to ${rule.to} type ${target.type} does not match ${removedRow.type}`,
      });
    }
  }

  // Drop validity.
  for (const rule of hint.drop ?? []) {
    if (!removedSet.has(rule.slug)) {
      errors.push({
        path: `drop.${rule.slug}`,
        message: `drop.slug ${rule.slug} is not a removed field`,
      });
    }
  }

  // Default value type-match.
  for (const rule of hint.default ?? []) {
    if (!addedByName.has(rule.slug)) {
      errors.push({
        path: `default.${rule.slug}`,
        message: `default.slug ${rule.slug} is not an added field`,
      });
      continue;
    }
    const node = nodeOf(newSchema, rule.slug);
    const t = fieldType(node);
    if (!matchesType(t, rule.value)) {
      errors.push({
        path: `default.${rule.slug}`,
        message: `default value does not match type ${t}`,
      });
    }
  }

  return errors.length === 0 ? { ok: true } : { ok: false, errors };
}
