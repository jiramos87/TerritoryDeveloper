#!/usr/bin/env node
/**
 * validate-cron-cadence-coverage.mjs
 *
 * Asserts every cron_%_jobs table in the DB has its cadence registered
 * in tools/cron-server/index.ts (KINDS array table entry or explicit cron.schedule).
 * Also asserts no orphan cadences (tables registered in index.ts that don't exist in DB).
 *
 * Exit 0 = full coverage. Exit 1 = drift detected.
 *
 * TECH-18109 / async-cron-jobs Stage 6.0.3
 */

import path from "node:path";
import { fileURLToPath } from "node:url";
import { createRequire } from "node:module";
import fs from "node:fs";
import { resolveDatabaseUrl } from "../postgres-ia/resolve-database-url.mjs";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = process.env.IA_REPO_ROOT ?? path.resolve(__dirname, "../..");

const CRON_INDEX = path.join(REPO_ROOT, "tools/cron-server/index.ts");

const pgRequire = createRequire(
  path.join(REPO_ROOT, "tools/postgres-ia/package.json"),
);
const pg = pgRequire("pg");

async function main() {
  const dbUrl = await resolveDatabaseUrl(REPO_ROOT);
  const client = new pg.Client({ connectionString: dbUrl });
  await client.connect();

  let tableNames;
  try {
    const res = await client.query(
      `SELECT table_name
       FROM information_schema.tables
       WHERE table_schema = 'public'
         AND table_name LIKE 'cron_%\_jobs' ESCAPE '\\'
       ORDER BY table_name`,
    );
    tableNames = res.rows.map((r) => r.table_name);
  } finally {
    await client.end();
  }

  if (tableNames.length === 0) {
    console.log("validate:cron-cadence-coverage: no cron_*_jobs tables found. ok.");
    process.exit(0);
  }

  // Parse index.ts for table names mentioned in KINDS array or cron.schedule blocks.
  const indexContent = fs.readFileSync(CRON_INDEX, "utf8");

  // Extract table strings from KINDS array: table: "cron_*_jobs"
  const tableMatches = indexContent.matchAll(/table:\s*["'](cron_[a-z_]+_jobs)["']/g);
  const registeredTables = new Set([...tableMatches].map((m) => m[1]));

  const missing = tableNames.filter((t) => !registeredTables.has(t));
  const orphan = [...registeredTables].filter((t) => !tableNames.includes(t));

  const ok = missing.length === 0 && orphan.length === 0;

  if (ok) {
    console.log(
      `validate:cron-cadence-coverage: ${tableNames.length} table(s) — full cadence coverage. ok.`,
    );
    process.exit(0);
  } else {
    console.error("validate:cron-cadence-coverage: DRIFT detected.");
    if (missing.length > 0) {
      console.error(`  tables_in_db_missing_cadence: ${JSON.stringify(missing)}`);
      console.error(
        "  Add a KINDS entry in tools/cron-server/index.ts for each missing table.",
      );
    }
    if (orphan.length > 0) {
      console.error(`  cadences_with_no_db_table: ${JSON.stringify(orphan)}`);
      console.error(
        "  Either create the missing DB table or remove the KINDS entry.",
      );
    }
    process.exit(1);
  }
}

main().catch((err) => {
  console.error("validate:cron-cadence-coverage: unexpected error:", err);
  process.exit(1);
});
