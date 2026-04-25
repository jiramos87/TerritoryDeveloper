#!/usr/bin/env node
/**
 * Restore-from-snapshot smoke (Step 11 — IA dev DB-primary refactor).
 *
 * Spins up a throwaway DB, applies `ia/state/db-snapshot-metadata.sql`, then
 * `pg_restore`s `ia/state/db-snapshot-bodies.dump`, then runs row-count checks
 * against the live source DB. Drops the throwaway DB at the end (best effort).
 *
 * Usage:
 *
 *   node tools/postgres-ia/restore-snapshot-smoke.mjs
 *
 * Env: same as snapshot-ia-db.mjs — resolves DB URI from .env.local /
 * config/postgres-dev.json.
 *
 * @see `docs/ia-dev-db-refactor-implementation.md` Step 11.
 */

import { spawnSync } from "node:child_process";
import { existsSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath, URL } from "node:url";
import { resolveDatabaseUrl } from "./resolve-database-url.mjs";

const REPO_ROOT = join(dirname(fileURLToPath(import.meta.url)), "..", "..");
const STATE_DIR = join(REPO_ROOT, "ia", "state");
const META_PATH = join(STATE_DIR, "db-snapshot-metadata.sql");
const BODIES_PATH = join(STATE_DIR, "db-snapshot-bodies.dump");

const TABLES = [
  "ia_master_plans",
  "ia_stages",
  "ia_tasks",
  "ia_task_deps",
  "ia_task_spec_history",
  "ia_task_commits",
  "ia_stage_verifications",
  "ia_master_plan_change_log",
  "ia_fix_plan_tuples",
  "ia_runtime_state",
  "ia_project_spec_journal",
  "ia_ship_stage_journal",
];

function parseDbUrl(uri) {
  const u = new URL(uri);
  return {
    host: u.hostname,
    port: u.port || "5432",
    user: decodeURIComponent(u.username),
    password: decodeURIComponent(u.password),
    database: u.pathname.replace(/^\//, ""),
  };
}

function runPsql(args, conn, opts = {}) {
  const env = { ...process.env, PGPASSWORD: conn.password };
  const r = spawnSync(
    "psql",
    ["-h", conn.host, "-p", conn.port, "-U", conn.user, ...args],
    { env, encoding: "utf8", ...opts },
  );
  if (r.status !== 0) throw new Error(`psql failed (${r.status}): ${r.stderr}`);
  return r.stdout;
}

function rowCount(table, conn) {
  const out = runPsql(
    ["-d", conn.database, "-At", "-c", `SELECT count(*) FROM ${table};`],
    conn,
  );
  return Number(out.trim());
}

function main() {
  const dbUri = resolveDatabaseUrl(REPO_ROOT);
  if (!dbUri) {
    console.error("DATABASE_URL unresolved.");
    process.exit(1);
  }
  if (!existsSync(META_PATH) || !existsSync(BODIES_PATH)) {
    console.error("Snapshot files missing — run snapshot-ia-db.mjs first.");
    process.exit(1);
  }

  const sourceConn = parseDbUrl(dbUri);
  const smokeDbName = `territory_ia_smoke_${process.pid}_${Date.now()}`;
  const adminConn = { ...sourceConn, database: "postgres" };
  const smokeConn = { ...sourceConn, database: smokeDbName };

  // Capture source row counts BEFORE creating the smoke DB.
  console.error(`[restore-smoke] capturing source row counts from ${sourceConn.database}`);
  const sourceCounts = Object.fromEntries(TABLES.map((t) => [t, rowCount(t, sourceConn)]));
  for (const [t, n] of Object.entries(sourceCounts)) {
    console.error(`[restore-smoke]   source ${t.padEnd(28)} = ${n}`);
  }

  let dbCreated = false;
  try {
    console.error(`[restore-smoke] creating throwaway DB ${smokeDbName}`);
    runPsql(
      ["-d", "postgres", "-c", `CREATE DATABASE "${smokeDbName}";`],
      adminConn,
    );
    dbCreated = true;

    console.error(`[restore-smoke] applying metadata SQL`);
    const env = { ...process.env, PGPASSWORD: smokeConn.password };
    const meta = spawnSync(
      "psql",
      [
        "-h", smokeConn.host,
        "-p", smokeConn.port,
        "-U", smokeConn.user,
        "-d", smokeConn.database,
        "-v", "ON_ERROR_STOP=1",
        "-f", META_PATH,
      ],
      { env, encoding: "utf8" },
    );
    if (meta.status !== 0) throw new Error(`psql metadata apply failed: ${meta.stderr}`);

    console.error(`[restore-smoke] truncating body-heavy tables before binary restore`);
    runPsql(
      [
        "-d", smokeConn.database,
        "-v", "ON_ERROR_STOP=1",
        "-c", "TRUNCATE ia_task_spec_history; DELETE FROM ia_tasks;",
      ],
      smokeConn,
    );

    console.error(`[restore-smoke] pg_restore bodies binary`);
    const restore = spawnSync(
      "pg_restore",
      [
        "-h", smokeConn.host,
        "-p", smokeConn.port,
        "-U", smokeConn.user,
        "-d", smokeConn.database,
        "--data-only",
        "--no-owner",
        "--single-transaction",
        BODIES_PATH,
      ],
      { env, encoding: "utf8" },
    );
    if (restore.status !== 0) throw new Error(`pg_restore failed: ${restore.stderr}`);

    console.error(`[restore-smoke] comparing row counts`);
    const errors = [];
    for (const t of TABLES) {
      const got = rowCount(t, smokeConn);
      const want = sourceCounts[t];
      const ok = got === want;
      console.error(`[restore-smoke]   ${ok ? "OK " : "FAIL"} ${t.padEnd(28)} source=${want} restored=${got}`);
      if (!ok) errors.push(`${t}: source=${want} restored=${got}`);
    }
    if (errors.length > 0) {
      throw new Error(`Row-count mismatch:\n  ${errors.join("\n  ")}`);
    }

    // Re-run an FTS query on restored body to confirm body_tsv regen.
    const fts = runPsql(
      [
        "-d", smokeConn.database,
        "-At",
        "-c",
        `SELECT count(*) FROM ia_tasks WHERE body_tsv @@ websearch_to_tsquery('english', 'Goal');`,
      ],
      smokeConn,
    );
    console.error(`[restore-smoke] FTS smoke (body_tsv match for 'Goal') = ${fts.trim()}`);

    console.error(`[restore-smoke] OK`);
  } finally {
    if (dbCreated) {
      try {
        runPsql(
          ["-d", "postgres", "-c", `DROP DATABASE IF EXISTS "${smokeDbName}";`],
          adminConn,
        );
        console.error(`[restore-smoke] dropped throwaway DB`);
      } catch (e) {
        console.error(`[restore-smoke] drop failed (manual cleanup may be needed): ${e}`);
      }
    }
  }
}

main();
