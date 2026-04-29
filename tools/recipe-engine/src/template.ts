/**
 * Minimal `${path}` template resolver for recipe argument trees.
 *
 * Resolves dotted paths against a context bag. Supports:
 *   - "${foo}"            → ctx.foo (typed passthrough when whole string is one ref)
 *   - "${foo.bar.baz}"    → ctx.foo.bar.baz
 *   - "prefix-${x}-suffix" → string interpolation (coerced via String())
 *
 * No expression evaluation. Conditionals (`when`, `flow.cond`) evaluate via
 * coerceTruthy() on the resolved value.
 */

const REF_RE = /\$\{([a-zA-Z_][a-zA-Z0-9_.[\]"-]*)\}/g;

export function resolvePath(ctx: Record<string, unknown>, dotted: string): unknown {
  const segs = dotted.split(".");
  let cur: unknown = ctx;
  for (const seg of segs) {
    if (cur == null) return undefined;
    if (typeof cur !== "object") return undefined;
    cur = (cur as Record<string, unknown>)[seg];
  }
  return cur;
}

export function resolveString(ctx: Record<string, unknown>, raw: string): unknown {
  const wholeMatch = raw.match(/^\$\{([^}]+)\}$/);
  if (wholeMatch) {
    return resolvePath(ctx, wholeMatch[1]);
  }
  return raw.replace(REF_RE, (_, p1) => {
    const v = resolvePath(ctx, p1);
    return v === undefined || v === null ? "" : String(v);
  });
}

export function resolveTree(ctx: Record<string, unknown>, value: unknown): unknown {
  if (typeof value === "string") return resolveString(ctx, value);
  if (Array.isArray(value)) return value.map((v) => resolveTree(ctx, v));
  if (value && typeof value === "object") {
    const out: Record<string, unknown> = {};
    for (const [k, v] of Object.entries(value as Record<string, unknown>)) {
      out[k] = resolveTree(ctx, v);
    }
    return out;
  }
  return value;
}

export function coerceTruthy(v: unknown): boolean {
  if (v === undefined || v === null) return false;
  if (typeof v === "boolean") return v;
  if (typeof v === "number") return v !== 0;
  if (typeof v === "string") {
    const t = v.trim().toLowerCase();
    if (t === "" || t === "false" || t === "0" || t === "null" || t === "undefined") return false;
    return true;
  }
  if (Array.isArray(v)) return v.length > 0;
  if (typeof v === "object") return Object.keys(v).length > 0;
  return Boolean(v);
}
