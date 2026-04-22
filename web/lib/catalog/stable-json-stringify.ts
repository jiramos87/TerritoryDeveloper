/**
 * Deterministic `JSON.stringify` for snapshot files: sort object keys recursively, keep array order, `JSON.stringify` with 2-space indent, trailing newline.
 * `DATABASE_URL` and other env secrets are not included in the snapshot.
 */
function sortKeysDeep(v: unknown): unknown {
  if (v === null || typeof v !== "object") return v;
  if (Array.isArray(v)) return v.map(sortKeysDeep);
  const o = v as Record<string, unknown>;
  const out: Record<string, unknown> = {};
  for (const k of Object.keys(o).sort()) {
    out[k] = sortKeysDeep(o[k]);
  }
  return out;
}

export function stableJsonStringify(value: unknown): string {
  return `${JSON.stringify(sortKeysDeep(value), null, 2)}\n`;
}
