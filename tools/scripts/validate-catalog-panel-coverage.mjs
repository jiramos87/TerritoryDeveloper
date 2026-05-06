#!/usr/bin/env node
/**
 * validate-catalog-panel-coverage.mjs — orphan-button detector.
 *
 * TECH-19062 / game-ui-catalog-bake Stage 9.12.
 *
 * Queries catalog_entity WHERE kind='button' AND retired_at IS NULL.
 * For each button, asserts ≥1 panel_child row references it via
 * params_json->>'button_ref' (canonical) or child_entity_id FK (fallback).
 *
 * Exit codes:
 *   0 = all buttons parented (clean)
 *   1 = orphan buttons found (hard fail)
 *   2 = config / DB error
 *
 * Wired into validate:all:readonly after validate:catalog-naming.
 */

import { readFileSync, existsSync } from "node:fs";
import { resolve, dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { createRequire } from "node:module";

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const REPO_ROOT = resolve(__dirname, "../..");

// ---------------------------------------------------------------------------
// DB helpers
// ---------------------------------------------------------------------------

function loadEnv() {
  const envPath = resolve(REPO_ROOT, ".env");
  if (existsSync(envPath)) {
    const lines = readFileSync(envPath, "utf8").split("\n");
    for (const line of lines) {
      const m = line.match(/^([A-Z_][A-Z0-9_]*)=(.*)$/);
      if (m && !process.env[m[1]]) {
        process.env[m[1]] = m[2].trim();
      }
    }
  }
}

/**
 * Returns list of orphan button slugs.
 * A button is orphaned when no panel_child row references it
 * (neither via params_json->>'button_ref' nor via child_entity_id FK).
 *
 * @param {string} databaseUrl
 * @returns {Promise<string[]>} orphan slugs
 */
export async function findOrphanButtons(databaseUrl) {
  const pgRequire = createRequire(join(REPO_ROOT, "tools/postgres-ia/package.json"));
  const pg = pgRequire("pg");
  const client = new pg.Client({ connectionString: databaseUrl });
  await client.connect();
  try {
    const res = await client.query(`
      SELECT ce.slug
      FROM catalog_entity ce
      WHERE ce.kind = 'button'
        AND ce.retired_at IS NULL
        AND NOT EXISTS (
          SELECT 1
          FROM panel_child pc
          WHERE
            pc.child_entity_id = ce.id
            OR pc.params_json->>'button_ref' = ce.slug
        )
      ORDER BY ce.slug
    `);
    return res.rows.map((r) => r.slug);
  } finally {
    await client.end();
  }
}

// ---------------------------------------------------------------------------
// Main
// ---------------------------------------------------------------------------

async function main() {
  loadEnv();
  const databaseUrl = process.env.DATABASE_URL;
  if (!databaseUrl) {
    console.error("[catalog-panel-coverage] ERROR: DATABASE_URL not set");
    process.exit(2);
  }

  let orphans;
  try {
    orphans = await findOrphanButtons(databaseUrl);
  } catch (err) {
    console.error(`[catalog-panel-coverage] DB query failed: ${err.message}`);
    process.exit(2);
  }

  if (orphans.length === 0) {
    console.log("[catalog-panel-coverage] All buttons have a parent panel. OK.");
    process.exit(0);
  }

  console.error(`[catalog-panel-coverage] FAIL: ${orphans.length} orphan button(s) found:\n`);
  for (const slug of orphans) {
    console.error(`  ✗ ${slug}`);
  }
  console.error(`
Remediation: for each orphan slug above, either:
  (a) Register a parent panel and add a panel_child row with params_json->>'button_ref'='{slug}', OR
  (b) Retire the button via UPDATE catalog_entity SET retired_at=NOW() WHERE kind='button' AND slug='{slug}'.
`);
  process.exit(1);
}

main().catch((err) => {
  console.error("[catalog-panel-coverage] unexpected error:", err);
  process.exit(2);
});
