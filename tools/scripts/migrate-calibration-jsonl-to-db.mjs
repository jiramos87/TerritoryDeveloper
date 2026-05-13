#!/usr/bin/env node
/**
 * migrate-calibration-jsonl-to-db.mjs
 *
 * Migrates legacy ui-calibration JSONL verdict rows from ia/state/ into the DB
 * table ia_ui_calibration_verdict (idempotent, UNIQUE source_file+line_idx).
 *
 * ui-visual-regression Stage 3.0 — TECH-31896
 *
 * Usage:
 *   node tools/scripts/migrate-calibration-jsonl-to-db.mjs --dry-run
 *   node tools/scripts/migrate-calibration-jsonl-to-db.mjs --apply
 *
 * Flags:
 *   --dry-run   Print JSON diff to stdout; no DB mutations (default when no flag given).
 *   --apply     Commit inserts in a single transaction.
 *
 * Source files (repo-relative):
 *   ia/state/ui-calibration-verdicts.jsonl
 *   ia/state/ui-calibration-corpus.jsonl
 *
 * Target table: ia_ui_calibration_verdict (created by migration 0157)
 */

import { readFileSync, existsSync } from "node:fs";
import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";
import { createRequire } from "node:module";
import { createHash } from "node:crypto";

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(__dirname, "../../");

// ── Load .env ───────────────────────────────────────────────────────────────

const dotenvPath = resolve(repoRoot, ".env");
if (existsSync(dotenvPath)) {
  const raw = readFileSync(dotenvPath, "utf8");
  for (const line of raw.split("\n")) {
    const t = line.trim();
    if (!t || t.startsWith("#")) continue;
    const eq = t.indexOf("=");
    if (eq < 0) continue;
    const key = t.slice(0, eq).trim();
    const val = t.slice(eq + 1).trim().replace(/^['"]|['"]$/g, "");
    if (!process.env[key]) process.env[key] = val;
  }
}

// ── Parse args ──────────────────────────────────────────────────────────────

const args = process.argv.slice(2);
const applyMode = args.includes("--apply");
const dryRun = args.includes("--dry-run") || !applyMode;

// ── Source JSONL files ───────────────────────────────────────────────────────

const SOURCES = [
  "ia/state/ui-calibration-verdicts.jsonl",
  "ia/state/ui-calibration-corpus.jsonl",
];

function readJsonlRows(relPath) {
  const absPath = resolve(repoRoot, relPath);
  if (!existsSync(absPath)) {
    return [];
  }
  const raw = readFileSync(absPath, "utf8");
  const rows = [];
  for (const [lineIdx, line] of raw.split("\n").entries()) {
    const trimmed = line.trim();
    if (!trimmed) continue;
    try {
      const payload = JSON.parse(trimmed);
      rows.push({ source_file: relPath, line_idx: lineIdx, payload });
    } catch {
      // skip malformed lines
    }
  }
  return rows;
}

// ── Normalize rows ───────────────────────────────────────────────────────────

function normalizedChecksum(rows) {
  const canonical = rows
    .map((r) => JSON.stringify({ source_file: r.source_file, line_idx: r.line_idx, payload: r.payload }))
    .sort()
    .join("\n");
  return createHash("sha256").update(canonical).digest("hex");
}

// ── Collect all rows ─────────────────────────────────────────────────────────

const allRows = [];
for (const src of SOURCES) {
  allRows.push(...readJsonlRows(src));
}

const totalRows = allRows.length;
const checksum = normalizedChecksum(allRows);

if (dryRun) {
  const diff = {
    mode: "dry-run",
    sources: SOURCES,
    total_rows: totalRows,
    checksum_sha256: checksum,
    rows: allRows.map((r) => ({
      source_file: r.source_file,
      line_idx: r.line_idx,
      panel_slug: r.payload?.panel_slug ?? null,
      payload_keys: Object.keys(r.payload),
    })),
  };
  console.log(JSON.stringify(diff, null, 2));
  process.exit(0);
}

// ── Apply mode: insert into DB ───────────────────────────────────────────────

const dbUrl = process.env.DATABASE_URL;
if (!dbUrl) {
  console.error("[migrate-calibration-jsonl-to-db] DATABASE_URL not set.");
  process.exit(1);
}

const req = createRequire(import.meta.url);
let pg;
try {
  pg = req("pg");
} catch {
  console.error("[migrate-calibration-jsonl-to-db] pg module not available.");
  process.exit(1);
}

const pool = new pg.Pool({ connectionString: dbUrl, max: 2 });
const client = await pool.connect();

let insertedCount = 0;
let skippedCount = 0;

try {
  await client.query("BEGIN");

  for (const row of allRows) {
    const { source_file, line_idx, payload } = row;
    const panelSlug = payload?.panel_slug ?? null;
    const result = await client.query(
      `INSERT INTO ia_ui_calibration_verdict (source_file, line_idx, panel_slug, payload)
       VALUES ($1, $2, $3, $4)
       ON CONFLICT (source_file, line_idx) DO NOTHING`,
      [source_file, line_idx, panelSlug, JSON.stringify(payload)],
    );
    if (result.rowCount > 0) {
      insertedCount++;
    } else {
      skippedCount++;
    }
  }

  await client.query("COMMIT");

  // Verify parity
  const countRes = await client.query(
    `SELECT COUNT(*)::int AS cnt FROM ia_ui_calibration_verdict
     WHERE source_file = ANY($1::text[])`,
    [SOURCES],
  );
  const dbCount = countRes.rows[0].cnt;

  const result = {
    mode: "apply",
    sources: SOURCES,
    total_rows: totalRows,
    inserted: insertedCount,
    skipped_duplicate: skippedCount,
    db_count_post_apply: dbCount,
    parity: dbCount >= totalRows,
    checksum_sha256: checksum,
  };
  console.log(JSON.stringify(result, null, 2));
} catch (e) {
  await client.query("ROLLBACK");
  console.error("[migrate-calibration-jsonl-to-db] Transaction failed:", e.message);
  process.exit(1);
} finally {
  client.release();
  await pool.end();
}
