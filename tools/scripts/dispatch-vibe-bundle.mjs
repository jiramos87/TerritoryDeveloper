// One-shot: dispatch /tmp/vibe-bundle.json via SELECT master_plan_bundle_apply($1::jsonb).
// Mirrors the MCP tool body — same Postgres tx, atomic.

import { readFileSync } from "node:fs";
import pg from "pg";

const bundle = JSON.parse(readFileSync("/tmp/vibe-bundle.json", "utf8"));
const url = process.env.DATABASE_URL ?? "postgresql://postgres:postgres@localhost:5434/territory_ia_dev";
const pool = new pg.Pool({ connectionString: url });

try {
  const t0 = Date.now();
  const res = await pool.query("SELECT master_plan_bundle_apply($1::jsonb) AS result", [
    JSON.stringify(bundle),
  ]);
  const dt = Date.now() - t0;
  const result = res.rows[0]?.result;
  console.log(JSON.stringify({ ok: true, elapsed_ms: dt, result }, null, 2));
} catch (e) {
  console.error(JSON.stringify({ ok: false, code: e.code, message: e.message }, null, 2));
  process.exitCode = 1;
} finally {
  await pool.end();
}
