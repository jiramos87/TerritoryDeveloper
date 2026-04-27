/**
 * Pure migration runner (TECH-2462 / Stage 11.1).
 *
 * Applies a `MigrationHint` to an entity's `params_json` payload to produce
 * the upgraded params for the target archetype version. Side-effect free —
 * caller persists the new `entity_version` row.
 *
 * Rules (all non-fatal — emit warnings instead of throwing):
 * - `rename`: copy `params[from]` into `params[to]`, drop `params[from]`. If
 *   `params[from]` is missing, treat as drop (warning).
 * - `default`: set `params[slug]` only when slug not already present
 *   (warning when override skipped).
 * - `drop`: delete `params[slug]`. Warn when slug not present in source.
 *
 * @see ia/projects/asset-pipeline/stage-11.1 — TECH-2462 §Plan Digest
 */
import type { MigrationHint } from "@/lib/archetype/migration-hint-validator";

export type MigrationWarning = { path: string; message: string };

export type MigrationResult = {
  params: Record<string, unknown>;
  warnings: MigrationWarning[];
};

export function applyMigration(
  source: Record<string, unknown>,
  hint: MigrationHint,
): MigrationResult {
  const params: Record<string, unknown> = { ...source };
  const warnings: MigrationWarning[] = [];

  // Renames first — value moves from old slug to new.
  for (const rule of hint.rename ?? []) {
    if (!(rule.from in params)) {
      warnings.push({
        path: `rename.${rule.from}`,
        message: `source missing slug ${rule.from} — treated as drop`,
      });
      continue;
    }
    params[rule.to] = params[rule.from];
    delete params[rule.from];
  }

  // Drops — explicit removals.
  for (const rule of hint.drop ?? []) {
    if (!(rule.slug in params)) {
      warnings.push({
        path: `drop.${rule.slug}`,
        message: `slug ${rule.slug} not present in source — no-op`,
      });
      continue;
    }
    delete params[rule.slug];
  }

  // Defaults last — fill only when absent.
  for (const rule of hint.default ?? []) {
    if (rule.slug in params) {
      warnings.push({
        path: `default.${rule.slug}`,
        message: `slug ${rule.slug} already present — default skipped`,
      });
      continue;
    }
    params[rule.slug] = rule.value;
  }

  return { params, warnings };
}
