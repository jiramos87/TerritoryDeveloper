/**
 * Pure typed diff between two `entity_version.params_json` payloads
 * (TECH-3300 / Stage 14.3).
 *
 * `diffVersions(kind, from, to)` walks the union of top-level keys, classifies
 * each as `added` / `removed` / `changed`, attaches a render hint via
 * `hintFor(kind, field)`, and returns a sorted `KindDiff`. No DB / no fetch /
 * no React imports — foundation for the API route (T14.3.2) and renderer
 * components (T14.3.3 / T14.3.4 / T14.3.5).
 *
 * Equality: shallow strict-equality for primitives, structural deep-equal for
 * objects/arrays via stable JSON stringify. Adequate for the simple shape of
 * `params_json` per kind-schema; deep-eq libs add weight without payoff.
 *
 * Output ordering: `added` / `removed` / `changed[].field` alpha-sorted for
 * stable golden fixtures and reviewer-friendly diffs.
 *
 * @see ia/projects/asset-pipeline/stage-14.3 — TECH-3300 §Plan Digest
 * @see web/lib/diff/kind-schemas.ts — `KindDiff`, `FieldHint`, `hintFor`
 */
import type { CatalogKind } from "@/lib/refs/types";

import { hintFor, type KindDiff, type KindDiffChange } from "./kind-schemas";

const isPlainObject = (v: unknown): v is Record<string, unknown> =>
  typeof v === "object" && v !== null && !Array.isArray(v);

/**
 * Structural equality for JSON-compatible values. Returns `true` for two
 * primitives that are `===`, two arrays whose elements pairwise satisfy
 * `deepEq`, and two plain objects whose key sets match and whose values
 * pairwise satisfy `deepEq`. Handles `null` / `undefined`.
 */
function deepEq(a: unknown, b: unknown): boolean {
  if (a === b) return true;
  if (a == null || b == null) return a === b;
  if (Array.isArray(a) && Array.isArray(b)) {
    if (a.length !== b.length) return false;
    for (let i = 0; i < a.length; i++) {
      if (!deepEq(a[i], b[i])) return false;
    }
    return true;
  }
  if (isPlainObject(a) && isPlainObject(b)) {
    const aKeys = Object.keys(a);
    const bKeys = Object.keys(b);
    if (aKeys.length !== bKeys.length) return false;
    for (const k of aKeys) {
      if (!Object.prototype.hasOwnProperty.call(b, k)) return false;
      if (!deepEq(a[k], b[k])) return false;
    }
    return true;
  }
  return false;
}

/**
 * Compute the typed diff between two `params_json` payloads for a given kind.
 *
 * - Keys in `to` only → `added`.
 * - Keys in `from` only → `removed`.
 * - Keys in both with non-equal values → `changed` with `hint` resolved via
 *   `hintFor(kind, field)`; structurally equal values omitted.
 *
 * Lists alpha-sorted for stable output.
 */
export function diffVersions(
  kind: CatalogKind,
  from: Record<string, unknown>,
  to: Record<string, unknown>,
): KindDiff {
  const fromKeys = new Set(Object.keys(from));
  const toKeys = new Set(Object.keys(to));
  const added: string[] = [];
  const removed: string[] = [];
  const changed: KindDiffChange[] = [];

  for (const k of toKeys) {
    if (!fromKeys.has(k)) {
      added.push(k);
    }
  }
  for (const k of fromKeys) {
    if (!toKeys.has(k)) {
      removed.push(k);
    }
  }
  for (const k of fromKeys) {
    if (!toKeys.has(k)) continue;
    const a = from[k];
    const b = to[k];
    if (!deepEq(a, b)) {
      changed.push({
        field: k,
        before: a,
        after: b,
        hint: hintFor(kind, k),
      });
    }
  }

  added.sort();
  removed.sort();
  changed.sort((x, y) => (x.field < y.field ? -1 : x.field > y.field ? 1 : 0));

  return { added, removed, changed };
}
