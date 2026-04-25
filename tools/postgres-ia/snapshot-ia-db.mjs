#!/usr/bin/env node
/**
 * Daily DB snapshot generator (Step 11 — IA dev DB-primary refactor).
 *
 * Produces two artifacts under `ia/state/`:
 *
 *   - `db-snapshot-metadata.sql`  — plain SQL (committable / diffable). Schema
 *      for all `ia_*` tables + small-table data + ia_tasks metadata WITHOUT the
 *      `body` column (substituted with NULL on restore). Diff this file to track
 *      schema drift + status flips + stage / task graph changes.
 *
 *   - `db-snapshot-bodies.dump`   — pg_dump custom-format binary (committable
 *      but compressed; gitignored if it grows past LFS budget — see §Voids in
 *      Step 11). Carries the heavyweight body columns: `ia_tasks.body` +
 *      `ia_task_spec_history.body`.
 *
 * Usage:
 *
 *   node tools/postgres-ia/snapshot-ia-db.mjs
 *
 * Restore (smoke):
 *
 *   node tools/postgres-ia/restore-snapshot-smoke.mjs
 *
 * @see `docs/ia-dev-db-refactor-implementation.md` Step 11 + design F5/F6/E10.
 */

import { execFileSync, spawnSync } from "node:child_process";
import { mkdirSync, writeFileSync, statSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath, URL } from "node:url";
import { resolveDatabaseUrl } from "./resolve-database-url.mjs";

const REPO_ROOT = join(dirname(fileURLToPath(import.meta.url)), "..", "..");
const STATE_DIR = join(REPO_ROOT, "ia", "state");
const META_PATH = join(STATE_DIR, "db-snapshot-metadata.sql");
const BODIES_PATH = join(STATE_DIR, "db-snapshot-bodies.dump");

const IA_TABLES = [
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

const BODY_HEAVY = new Set(["ia_tasks", "ia_task_spec_history"]);

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

function pgDump(args, dbConn) {
  const env = { ...process.env, PGPASSWORD: dbConn.password };
  const baseArgs = ["-h", dbConn.host, "-p", dbConn.port, "-U", dbConn.user, "-d", dbConn.database];
  const r = spawnSync("pg_dump", [...baseArgs, ...args], { env, encoding: "buffer" });
  if (r.status !== 0) {
    throw new Error(`pg_dump failed (status ${r.status}): ${r.stderr.toString()}`);
  }
  return r.stdout;
}

function psqlQuery(sql, dbConn) {
  const env = { ...process.env, PGPASSWORD: dbConn.password };
  const r = spawnSync(
    "psql",
    [
      "-h", dbConn.host,
      "-p", dbConn.port,
      "-U", dbConn.user,
      "-d", dbConn.database,
      "-At",
      "-F", "\t",
      "-c", sql,
    ],
    { env, encoding: "utf8" },
  );
  if (r.status !== 0) throw new Error(`psql failed: ${r.stderr}`);
  return r.stdout;
}

function dumpExtensionPrelude() {
  // pg_dump --schema-only --no-acl emits CREATE EXTENSION lines that require
  // superuser; throwaway smoke DBs are blank. Emit IF NOT EXISTS form so the
  // metadata SQL is self-applicable on a fresh DB. Mirrors
  // `db/migrations/0015_ia_tasks_core.sql`.
  return [
    "-- Extensions (mirrors db/migrations/0015_ia_tasks_core.sql)",
    "CREATE EXTENSION IF NOT EXISTS pg_trgm;",
    "",
  ].join("\n") + "\n";
}

function dumpEnumPrelude(dbConn) {
  // pg_dump -t filters to tables and skips dependent enum types. Emit them
  // verbatim from `pg_type` so the metadata SQL is self-applicable on a fresh
  // DB. Mirrors `db/migrations/0015_ia_tasks_core.sql`.
  const tsv = psqlQuery(
    `SELECT t.typname,
            string_agg(quote_literal(e.enumlabel), ', ' ORDER BY e.enumsortorder)
       FROM pg_type t
       JOIN pg_enum e ON e.enumtypid = t.oid
       JOIN pg_namespace n ON n.oid = t.typnamespace
      WHERE n.nspname = 'public'
        AND t.typname IN ('task_prefix', 'task_status', 'stage_status', 'stage_verdict', 'ia_task_dep_kind')
      GROUP BY t.typname
      ORDER BY t.typname;`,
    dbConn,
  );
  const lines = ["-- Enum types (recreated from pg_type catalog)"];
  for (const row of tsv.split("\n")) {
    if (!row) continue;
    const [name, labels] = row.split("\t");
    lines.push(
      `DO $$ BEGIN`,
      `  IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = '${name}') THEN`,
      `    CREATE TYPE ${name} AS ENUM (${labels});`,
      `  END IF;`,
      `END $$;`,
    );
  }
  return lines.join("\n") + "\n";
}

function dumpMetadataSql(dbConn) {
  const tableArgs = IA_TABLES.flatMap((t) => ["-t", `public.${t}`]);

  const extensionPrelude = dumpExtensionPrelude();
  const enumPrelude = dumpEnumPrelude(dbConn);

  const schemaSql = pgDump(
    ["--schema-only", "--no-owner", "--no-acl", ...tableArgs],
    dbConn,
  ).toString("utf8");

  const dataTables = IA_TABLES.filter((t) => !BODY_HEAVY.has(t));
  const dataArgs = dataTables.flatMap((t) => ["-t", `public.${t}`]);
  const dataSql = pgDump(
    ["--data-only", "--no-owner", "--no-acl", "--inserts", ...dataArgs],
    dbConn,
  ).toString("utf8");

  // ia_tasks metadata (no body) — emit as INSERTs so plain diff stays useful.
  const taskRowsTsv = psqlQuery(
    `SELECT task_id, prefix::text, slug, stage_id, title, status::text, priority, type, notes,
            extract(epoch FROM created_at)::bigint, extract(epoch FROM updated_at)::bigint,
            CASE WHEN completed_at IS NULL THEN '' ELSE extract(epoch FROM completed_at)::bigint::text END,
            CASE WHEN archived_at IS NULL THEN '' ELSE extract(epoch FROM archived_at)::bigint::text END
       FROM ia_tasks ORDER BY task_id;`,
    dbConn,
  );

  // ia_tasks metadata as SQL COMMENTs — diffable (e.g. status flips visible in
  // git log) without conflicting with the binary dump's COPY restore. The
  // restore script applies metadata.sql + then pg_restore bodies dump; the
  // bodies dump is the ground truth for ia_tasks data (incl. metadata cols).
  const taskRows = [];
  for (const line of taskRowsTsv.split("\n")) {
    if (!line) continue;
    const cols = line.split("\t");
    const [task_id, prefix, slug, stage_id, title, status, priority, type, notes,
      created, updated, completed, archived] = cols;
    const fmt = (s) => (s == null || s === "" ? "" : String(s).replace(/\n/g, " "));
    taskRows.push(
      `--   ${task_id} | ${fmt(prefix)} | ${fmt(slug)} | ${fmt(stage_id)} | ${fmt(status)} | ${fmt(priority)} | ${fmt(type)} | created=${fmt(created)} updated=${fmt(updated)} completed=${fmt(completed)} archived=${fmt(archived)} | ${fmt(title)}`,
    );
  }

  const header = `--
-- IA dev DB metadata snapshot (Step 11 — refactor)
-- Generated: ${new Date().toISOString()}
-- Schema-only + data for ${dataTables.length} small tables + ia_tasks metadata
-- (body column restored from db-snapshot-bodies.dump).
--
SET client_min_messages TO WARNING;
SET search_path TO public;

`;

  const taskBlock = `\n--\n-- ia_tasks (metadata only — body + full row data restored from db-snapshot-bodies.dump)\n-- task_id | prefix | slug | stage_id | status | priority | type | timestamps | title\n--\n${taskRows.join("\n")}\n`;

  // Wrap data inserts so FK constraints from ia_task_deps -> ia_tasks don't
  // trip during apply (ia_tasks rows arrive later via pg_restore of the bodies
  // binary). `session_replication_role = replica` is the standard pg_dump
  // trick to defer FK + trigger checks during bulk load.
  const dataBlock = `\n--\n-- Data load (FK checks disabled — ia_tasks restored from db-snapshot-bodies.dump after this file)\n--\nSET session_replication_role = replica;\n${dataSql}\nSET session_replication_role = origin;\n`;

  return header + extensionPrelude + enumPrelude + "\n" + schemaSql + dataBlock + taskBlock;
}

function dumpBodiesBinary(dbConn) {
  return pgDump(
    [
      "--data-only",
      "--no-owner",
      "--no-acl",
      "-Fc",
      "-Z", "9",
      "-t", "public.ia_tasks",
      "-t", "public.ia_task_spec_history",
    ],
    dbConn,
  );
}

function main() {
  const dbUri = resolveDatabaseUrl(REPO_ROOT);
  if (!dbUri) {
    console.error("DATABASE_URL unresolved — refusing to snapshot.");
    process.exit(1);
  }
  const conn = parseDbUrl(dbUri);

  mkdirSync(STATE_DIR, { recursive: true });

  console.error(`[snapshot-ia-db] writing metadata SQL to ${META_PATH}`);
  const meta = dumpMetadataSql(conn);
  writeFileSync(META_PATH, meta, "utf8");
  console.error(`[snapshot-ia-db]   ${statSync(META_PATH).size} bytes`);

  console.error(`[snapshot-ia-db] writing bodies dump to ${BODIES_PATH}`);
  const bodies = dumpBodiesBinary(conn);
  writeFileSync(BODIES_PATH, bodies);
  console.error(`[snapshot-ia-db]   ${statSync(BODIES_PATH).size} bytes`);

  // Verify history table has rows (audit signal — Step 11 acceptance).
  const histCount = psqlQuery(`SELECT count(*) FROM ia_task_spec_history;`, conn).trim();
  console.error(`[snapshot-ia-db] ia_task_spec_history row count = ${histCount}`);

  console.error(`[snapshot-ia-db] OK`);
}

main();
