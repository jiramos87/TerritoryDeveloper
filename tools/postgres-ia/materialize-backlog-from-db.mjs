#!/usr/bin/env node
/**
 * materialize-backlog-from-db.mjs
 *
 * Regenerate BACKLOG.md + BACKLOG-ARCHIVE.md from the IA Postgres DB
 * (ia_tasks.raw_markdown + section manifests). Step 5 of
 * docs/ia-dev-db-refactor-implementation.md — replaces the yaml-reading
 * materialize-backlog.mjs with a DB-sourced generator.
 *
 * Usage:
 *   node tools/scripts/materialize-backlog-from-db.mjs [--check]
 *
 * --check: diff generated vs on-disk (whitespace-normalized); exit 1 on diff.
 *
 * Section manifest format: see materialize-backlog.mjs header block.
 * Open-issue manifest drives BACKLOG.md, archive manifest drives
 * BACKLOG-ARCHIVE.md. Open issues = ia_tasks where status != 'archived';
 * closed issues = ia_tasks where status = 'archived'.
 */

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import pg from "pg";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "../..");
const CHECK_MODE = process.argv.includes("--check");

// ---------------------------------------------------------------------------
// DATABASE_URL resolution (mirrors tools/postgres-ia/resolve-database-url.mjs)
// ---------------------------------------------------------------------------

function resolveDatabaseUrl() {
  if (process.env.IA_DATABASE_URL) return process.env.IA_DATABASE_URL;
  if (process.env.DATABASE_URL) return process.env.DATABASE_URL;
  const devCfg = path.join(REPO_ROOT, "config/postgres-dev.json");
  if (fs.existsSync(devCfg)) {
    try {
      const parsed = JSON.parse(fs.readFileSync(devCfg, "utf8"));
      if (parsed.database_url) return parsed.database_url;
    } catch (e) {
      console.error(
        `[materialize-from-db] failed to parse ${devCfg}: ${(e && e.message) || e}`,
      );
    }
  }
  return null;
}

const databaseUrl = resolveDatabaseUrl();
if (!databaseUrl) {
  console.error(
    "[materialize-from-db] ERROR: no DATABASE_URL / IA_DATABASE_URL / config/postgres-dev.json — cannot query ia_tasks.",
  );
  process.exit(1);
}

// ---------------------------------------------------------------------------
// Load section manifests (filesystem = ordering rules, DB = row bodies)
// ---------------------------------------------------------------------------

function loadManifest(filePath) {
  if (!fs.existsSync(filePath)) return { preamble: [], sections: [] };
  return JSON.parse(fs.readFileSync(filePath, "utf8"));
}

const backlogManifest = loadManifest(
  path.join(REPO_ROOT, "ia/state/backlog-sections.json"),
);
const archiveManifest = loadManifest(
  path.join(REPO_ROOT, "ia/state/backlog-archive-sections.json"),
);

// ---------------------------------------------------------------------------
// Query ia_tasks — split open vs archived
// ---------------------------------------------------------------------------

const pool = new pg.Pool({ connectionString: databaseUrl });

async function loadIssueMaps() {
  const { rows } = await pool.query(
    `SELECT task_id, status, raw_markdown
       FROM ia_tasks`,
  );
  const openMap = new Map();
  const closedMap = new Map();
  for (const r of rows) {
    const rec = { id: r.task_id, raw_markdown: r.raw_markdown ?? "" };
    // Step 2 importer maps yaml status closed → 'archived'; everything
    // else stays 'pending'. Manifests were authored against the old
    // open/closed split so the mapping here stays symmetric.
    if (r.status === "archived") closedMap.set(r.task_id, rec);
    else openMap.set(r.task_id, rec);
  }
  return { openMap, closedMap };
}

// ---------------------------------------------------------------------------
// Reconstruct md content from interleaved manifest
// ---------------------------------------------------------------------------

function reconstruct(manifest, issueMap) {
  const lines = [];

  for (const l of manifest.preamble || []) lines.push(l);

  for (const section of manifest.sections || []) {
    lines.push(section.header);

    for (const item of section.items || []) {
      if (item.type === "prose") {
        for (const l of item.lines || []) lines.push(l);
      } else if (item.type === "issue") {
        const record = issueMap.get(item.id);
        if (!record) {
          console.error(
            `[materialize-from-db] WARNING: no DB row for ${item.id} — skipping`,
          );
          continue;
        }
        const rawMd = record.raw_markdown || "";
        const mdLines = rawMd.split("\n");
        while (mdLines.length > 0 && !mdLines[mdLines.length - 1].trim()) {
          mdLines.pop();
        }
        if (
          item.checklist_line &&
          mdLines.length > 0 &&
          mdLines[0] !== item.checklist_line
        ) {
          mdLines[0] = item.checklist_line;
        }
        for (const l of mdLines) lines.push(l);
        const trailingBlanks = item.trailing_blanks || 0;
        for (let b = 0; b < trailingBlanks; b++) lines.push("");
      }
    }
  }

  return lines.join("\n");
}

// ---------------------------------------------------------------------------
// Main
// ---------------------------------------------------------------------------

function normalizeWS(s) {
  return s
    .split("\n")
    .map((l) => l.trimEnd())
    .join("\n")
    .replace(/\n{3,}/g, "\n\n")
    .trim();
}

async function main() {
  const { openMap, closedMap } = await loadIssueMaps();

  const generatedBacklog = reconstruct(backlogManifest, openMap);
  const generatedArchive = reconstruct(archiveManifest, closedMap);

  if (CHECK_MODE) {
    const backlogPath = path.join(REPO_ROOT, "BACKLOG.md");
    const archivePath = path.join(REPO_ROOT, "BACKLOG-ARCHIVE.md");
    let ok = true;

    for (const [label, onDiskPath, generated] of [
      ["BACKLOG.md", backlogPath, generatedBacklog],
      ["BACKLOG-ARCHIVE.md", archivePath, generatedArchive],
    ]) {
      if (!fs.existsSync(onDiskPath)) continue;
      const current = fs.readFileSync(onDiskPath, "utf8");
      const normCurrent = normalizeWS(current);
      const normGenerated = normalizeWS(generated);
      if (normCurrent !== normGenerated) {
        console.error(
          `[materialize-from-db] DIFF: ${label} does not match generated output`,
        );
        const a = normCurrent.split("\n");
        const b = normGenerated.split("\n");
        for (let i = 0; i < Math.max(a.length, b.length); i++) {
          if (a[i] !== b[i]) {
            console.error(`  First diff at line ${i + 1}:`);
            console.error(`  DISK:  ${JSON.stringify(a[i])}`);
            console.error(`  GEN:   ${JSON.stringify(b[i])}`);
            break;
          }
        }
        ok = false;
      } else {
        console.error(`[materialize-from-db] OK: ${label} matches generated output`);
      }
    }

    await pool.end();
    if (!ok) process.exit(1);
  } else {
    fs.writeFileSync(
      path.join(REPO_ROOT, "BACKLOG.md"),
      generatedBacklog,
      "utf8",
    );
    fs.writeFileSync(
      path.join(REPO_ROOT, "BACKLOG-ARCHIVE.md"),
      generatedArchive,
      "utf8",
    );
    console.error(
      "[materialize-from-db] Written: BACKLOG.md + BACKLOG-ARCHIVE.md (DB-sourced)",
    );
    await pool.end();
  }
}

main().catch(async (e) => {
  console.error("[materialize-from-db] ERROR:", e && e.message ? e.message : e);
  try {
    await pool.end();
  } catch {
    // pool already closed
  }
  process.exit(1);
});
