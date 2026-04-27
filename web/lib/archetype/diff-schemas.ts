/**
 * Pure schema diff (TECH-2461 / Stage 11.1).
 *
 * Compares two archetype `params_json` (`JsonSchemaNode`) trees and reports
 * `added`, `removed`, and `renamed_candidates` field rows. Pure — no DB, no
 * network. Lives in `lib/` per `web-backend-logic` rule.
 *
 * Rename heuristic: same `type` + Levenshtein distance ≤ 3 against a removed
 * slug. Override always available in the editor; heuristic is hint-only.
 *
 * @see ia/projects/asset-pipeline/stage-11.1 — TECH-2461 §Plan Digest
 */
import type { JsonSchemaNode } from "@/lib/json-schema-form/types";

export type SchemaFieldRef = {
  slug: string;
  type: string;
};

export type RenameCandidate = {
  from: string;
  to: string;
  type: string;
  /** Levenshtein distance — 0 means identical slug (won't appear here, included for sort). */
  distance: number;
};

export type SchemaDiff = {
  added: SchemaFieldRef[];
  removed: SchemaFieldRef[];
  renamed_candidates: RenameCandidate[];
};

const RENAME_MAX_DISTANCE = 3;

/** Returns lexicographically stable slug sort. */
function sortBySlug<T extends { slug: string }>(rows: T[]): T[] {
  return rows.slice().sort((a, b) => a.slug.localeCompare(b.slug));
}

function fieldType(node: JsonSchemaNode): string {
  if (Array.isArray(node.type)) return node.type[0] ?? "unknown";
  return (node.type as string | undefined) ?? "unknown";
}

/** Standard iterative Levenshtein. O(m*n) — fine for slug-length strings. */
export function levenshtein(a: string, b: string): number {
  if (a === b) return 0;
  const m = a.length;
  const n = b.length;
  if (m === 0) return n;
  if (n === 0) return m;
  const prev = new Array<number>(n + 1);
  const curr = new Array<number>(n + 1);
  for (let j = 0; j <= n; j++) prev[j] = j;
  for (let i = 1; i <= m; i++) {
    curr[0] = i;
    for (let j = 1; j <= n; j++) {
      const cost = a.charCodeAt(i - 1) === b.charCodeAt(j - 1) ? 0 : 1;
      curr[j] = Math.min(
        prev[j]! + 1, // delete
        curr[j - 1]! + 1, // insert
        prev[j - 1]! + cost, // substitute
      );
    }
    for (let j = 0; j <= n; j++) prev[j] = curr[j]!;
  }
  return prev[n]!;
}

/**
 * Computes the diff between two archetype schema trees. Considers only the
 * top-level `properties` map (nested-property migrations are out of scope for
 * Stage 11.1 — DEC pending).
 */
export function diffSchemas(
  oldSchema: JsonSchemaNode,
  newSchema: JsonSchemaNode,
): SchemaDiff {
  const oldProps = oldSchema.properties ?? {};
  const newProps = newSchema.properties ?? {};

  const added: SchemaFieldRef[] = [];
  const removed: SchemaFieldRef[] = [];
  for (const [slug, node] of Object.entries(newProps)) {
    if (!(slug in oldProps)) added.push({ slug, type: fieldType(node) });
  }
  for (const [slug, node] of Object.entries(oldProps)) {
    if (!(slug in newProps)) removed.push({ slug, type: fieldType(node) });
  }

  const renamed_candidates: RenameCandidate[] = [];
  for (const r of removed) {
    let best: RenameCandidate | null = null;
    for (const a of added) {
      if (a.type !== r.type) continue;
      const d = levenshtein(r.slug, a.slug);
      if (d > RENAME_MAX_DISTANCE) continue;
      if (best == null || d < best.distance) {
        best = { from: r.slug, to: a.slug, type: r.type, distance: d };
      }
    }
    if (best != null) renamed_candidates.push(best);
  }

  return {
    added: sortBySlug(added),
    removed: sortBySlug(removed),
    renamed_candidates: renamed_candidates
      .slice()
      .sort((a, b) => a.from.localeCompare(b.from)),
  };
}
