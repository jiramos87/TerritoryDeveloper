// Lazy singleton — avoids build-time connection + repeat init.
import postgres, { type Sql } from 'postgres';

let _sql: Sql | null = null;

export function getSql(): Sql {
  if (_sql) return _sql;
  const url = process.env.DATABASE_URL;
  if (!url) throw new Error('DATABASE_URL not set — required for DB access.');
  _sql = postgres(url);
  return _sql;
}

// Re-export as `sql` for tagged-template ergonomics at call sites.
export const sql = new Proxy({} as Sql, {
  get: (_t, prop) => Reflect.get(getSql() as object, prop),
  apply: (_t, _thisArg, args) => (getSql() as unknown as (...a: unknown[]) => unknown)(...args),
});
