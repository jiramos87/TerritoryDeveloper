/**
 * canonicalStringify — RFC-8785-style stable JSON.
 *
 * Produces a deterministic UTF-8 string for any JSON-compatible value:
 *   - Object keys sorted lexicographically (UTF-16 code-unit order).
 *   - Arrays preserve insertion order.
 *   - `undefined` / function values inside objects are dropped (matches `JSON.stringify`).
 *   - `NaN` / `+/-Infinity` rejected (RFC-8785 forbids; throw `RangeError`).
 *   - Numbers normalized via `JSON.stringify(n)` (e.g. `1.0` → `"1"`, `1e10` → `"10000000000"`).
 *   - Strings escaped per JSON spec.
 *
 * Used by `web/lib/snapshot/export.ts` so re-running the export over an unchanged
 * DB produces byte-identical files (golden hash assertion).
 *
 * @see ia/projects/asset-pipeline/stage-13.1 — TECH-2673 §Plan Digest
 */

export type JsonValue =
  | null
  | boolean
  | number
  | string
  | JsonValue[]
  | { [key: string]: JsonValue };

export function canonicalStringify(value: unknown): string {
  return serialize(value);
}

function serialize(value: unknown): string {
  if (value === null) return "null";
  if (typeof value === "boolean") return value ? "true" : "false";
  if (typeof value === "number") {
    if (!Number.isFinite(value)) {
      throw new RangeError(
        `canonicalStringify: non-finite number rejected (${value})`,
      );
    }
    // JSON.stringify normalizes integers + floats per spec; covers `1.0` → `1`.
    return JSON.stringify(value);
  }
  if (typeof value === "string") {
    return JSON.stringify(value);
  }
  if (Array.isArray(value)) {
    const parts: string[] = [];
    for (const item of value) {
      // Mirror JSON.stringify: undefined / function inside arrays → null.
      if (item === undefined || typeof item === "function") {
        parts.push("null");
      } else {
        parts.push(serialize(item));
      }
    }
    return `[${parts.join(",")}]`;
  }
  if (typeof value === "object") {
    const obj = value as Record<string, unknown>;
    const keys = Object.keys(obj).sort();
    const parts: string[] = [];
    for (const key of keys) {
      const child = obj[key];
      // Drop undefined / function — matches JSON.stringify object behavior.
      if (child === undefined || typeof child === "function") continue;
      parts.push(`${JSON.stringify(key)}:${serialize(child)}`);
    }
    return `{${parts.join(",")}}`;
  }
  // bigint / symbol / undefined at top level fall through.
  throw new TypeError(
    `canonicalStringify: unsupported value type (${typeof value})`,
  );
}
