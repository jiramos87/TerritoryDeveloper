/**
 * Pure deep-diff utility for two JSON-shaped payloads. Consumed by
 * `<StaleEditModal>` (TECH-1616) to render the 3-way diff.
 *
 * The diff is computed pairwise (base → other) and reports each leaf path
 * touched. Callers compose two diffs (loaded vs current, loaded vs pending)
 * to surface the 3-way decision to the author.
 *
 * @see TECH-1616 — Optimistic concurrency middleware + 409 handler
 */

export type DiffStatus = "added" | "removed" | "changed";

export type DiffEntry = {
  /** Dot-and-bracket path, e.g. `meta.tags[2]`. */
  path: string;
  status: DiffStatus;
  /** Value in the base payload; `undefined` when status === "added". */
  base: unknown;
  /** Value in the other payload; `undefined` when status === "removed". */
  other: unknown;
};

export type DiffResult = ReadonlyArray<DiffEntry>;

const isObject = (value: unknown): value is Record<string, unknown> =>
  typeof value === "object" && value !== null && !Array.isArray(value);

const sameType = (a: unknown, b: unknown): boolean => {
  if (Array.isArray(a)) return Array.isArray(b);
  if (isObject(a)) return isObject(b);
  return typeof a === typeof b;
};

const joinPath = (parent: string, segment: string | number): string => {
  if (typeof segment === "number") return `${parent}[${segment}]`;
  if (parent === "") return segment;
  return `${parent}.${segment}`;
};

/** Strict structural equality for JSON values (no NaN handling — JSON has none). */
function jsonEqual(a: unknown, b: unknown): boolean {
  if (a === b) return true;
  if (!sameType(a, b)) return false;
  if (Array.isArray(a) && Array.isArray(b)) {
    if (a.length !== b.length) return false;
    for (let i = 0; i < a.length; i++) if (!jsonEqual(a[i], b[i])) return false;
    return true;
  }
  if (isObject(a) && isObject(b)) {
    const keys = new Set([...Object.keys(a), ...Object.keys(b)]);
    for (const k of keys) if (!jsonEqual(a[k], b[k])) return false;
    return true;
  }
  return false;
}

function walk(base: unknown, other: unknown, path: string, out: DiffEntry[]): void {
  if (jsonEqual(base, other)) return;

  // Both arrays: pair-walk indices, account for length diff.
  if (Array.isArray(base) && Array.isArray(other)) {
    const max = Math.max(base.length, other.length);
    for (let i = 0; i < max; i++) {
      const childPath = joinPath(path, i);
      if (i >= base.length) out.push({ path: childPath, status: "added", base: undefined, other: other[i] });
      else if (i >= other.length) out.push({ path: childPath, status: "removed", base: base[i], other: undefined });
      else walk(base[i], other[i], childPath, out);
    }
    return;
  }

  // Both objects: union keys.
  if (isObject(base) && isObject(other)) {
    const keys = new Set([...Object.keys(base), ...Object.keys(other)]);
    for (const k of keys) {
      const childPath = joinPath(path, k);
      const inBase = k in base;
      const inOther = k in other;
      if (!inBase) out.push({ path: childPath, status: "added", base: undefined, other: other[k] });
      else if (!inOther) out.push({ path: childPath, status: "removed", base: base[k], other: undefined });
      else walk(base[k], other[k], childPath, out);
    }
    return;
  }

  // Type mismatch or primitive change → leaf entry.
  out.push({ path: path === "" ? "$" : path, status: "changed", base, other });
}

export function deepDiff(base: unknown, other: unknown): DiffResult {
  const out: DiffEntry[] = [];
  walk(base, other, "", out);
  return out;
}
