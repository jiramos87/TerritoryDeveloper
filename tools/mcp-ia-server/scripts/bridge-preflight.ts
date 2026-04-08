/**
 * Bridge environment preflight — verifies Postgres connectivity and agent_bridge_job table.
 *
 * Exit codes (stable contract for agents):
 *   0  OK — Postgres reachable, agent_bridge_job table present
 *   1  No URL — CI without DATABASE_URL (local dev resolves `.env`, `config/postgres-dev.json`, or built-in default)
 *   2  Connection refused / timeout — URL resolved but Postgres unreachable
 *   3  Table missing — connected, but agent_bridge_job does not exist (migration 0008 not applied)
 *   4  Unexpected SQL error — connected, query failed for another reason
 *
 * Usage from repository root:
 *   npm run db:bridge-preflight
 *
 * URL resolution: reuses resolveIaDatabaseUrl (repo `.env` / `.env.local` when not CI, then DATABASE_URL,
 * then `config/postgres-dev.json`, then dev default URI). Unity may use EditorPrefs for its own client.
 * See docs/postgres-ia-dev-setup.md for alignment notes.
 */

import pg from "pg";
import { resolveIaDatabaseUrl } from "../src/ia-db/resolve-database-url.js";

async function main(): Promise<number> {
  // Step 1: Resolve URL
  const url = resolveIaDatabaseUrl();
  if (!url) {
    console.error(
      "bridge-preflight: exit 1 — no database URL (CI: set DATABASE_URL; local: use .env or config/postgres-dev.json).",
    );
    return 1;
  }

  // Mask password for log output
  let safeUrl: string;
  try {
    const u = new URL(url);
    if (u.password) u.password = "***";
    safeUrl = u.toString();
  } catch {
    safeUrl = "(unparseable URL)";
  }

  // Step 2: Connect
  const client = new pg.Client({ connectionString: url, connectionTimeoutMillis: 5_000 });
  try {
    await client.connect();
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : String(err);
    console.error(`bridge-preflight: exit 2 — connection failed to ${safeUrl}`);
    console.error(`  ${msg}`);
    return 2;
  }

  // Step 3: Check agent_bridge_job table
  try {
    const res = await client.query(
      `SELECT 1 FROM information_schema.tables
       WHERE table_schema = 'public' AND table_name = 'agent_bridge_job'`,
    );
    if (res.rowCount === 0) {
      console.error("bridge-preflight: exit 3 — table agent_bridge_job not found.");
      console.error("  Run: npm run db:migrate (migration 0008_agent_bridge_job.sql)");
      await client.end();
      return 3;
    }
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : String(err);
    console.error("bridge-preflight: exit 4 — unexpected SQL error.");
    console.error(`  ${msg}`);
    await client.end();
    return 4;
  }

  await client.end();
  console.error(`bridge-preflight: exit 0 — OK (${safeUrl})`);
  return 0;
}

main()
  .then((code) => process.exit(code))
  .catch((err) => {
    console.error("bridge-preflight: exit 4 — unexpected error.");
    console.error(err);
    process.exit(4);
  });
