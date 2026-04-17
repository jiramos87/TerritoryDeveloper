// Lazy singleton — avoids build-time connection + repeat init.
import { neon, type NeonQueryFunction } from '@neondatabase/serverless';

let _sql: NeonQueryFunction<false, false> | null = null;

export function getSql(): NeonQueryFunction<false, false> {
  if (_sql) return _sql;
  const url = process.env.DATABASE_URL;
  if (!url) throw new Error('DATABASE_URL not set — required for DB access.');
  _sql = neon(url);
  return _sql;
}

// Re-export as `sql` for tagged-template ergonomics at call sites.
// Stage 5.2 drizzle adapter wraps this: drizzle(getSql(), { schema }).
export const sql = new Proxy({} as NeonQueryFunction<false, false>, {
  get: (_t, prop) => Reflect.get(getSql() as object, prop),
  apply: (_t, _thisArg, args) => (getSql() as unknown as (...a: unknown[]) => unknown)(...args),
});
