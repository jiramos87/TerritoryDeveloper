#!/usr/bin/env node
/**
 * validate-catalog-panel-coverage.mjs — orphan-button detector + duplicate-stub-reference rule.
 *
 * TECH-19062 / game-ui-catalog-bake Stage 9.12 — Rule 1: orphan-button.
 * TECH-19976 / game-ui-catalog-bake Stage 9.13 — Rule 2: duplicate-stub-reference.
 *
 * Rule 1 (orphan-button):
 *   Queries catalog_entity WHERE kind='button' AND retired_at IS NULL.
 *   For each button, asserts ≥1 panel_child row references it via
 *   params_json->>'button_ref' (canonical) or child_entity_id FK (fallback).
 *
 * Rule 2 (duplicate-stub-reference):
 *   For each panel with ≥3 children, groups panel_child rows by
 *   params_json->>'button_ref' (also label_ref, sprite_ref).
 *   Any group with count > 1 → FAIL.
 *   Stderr format: [duplicate-stub] panel={slug} ref={dup_slug} count={n} hint=register distinct slug per control
 *
 * Both rules run independently; aggregate exit 1 if either fails.
 *
 * Exit codes:
 *   0 = all rules pass (clean)
 *   1 = ≥1 rule failed (hard fail)
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
 * Rule 1: Returns list of orphan button slugs.
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

/**
 * Rule 2: Returns list of duplicate-stub violations.
 * For each panel with ≥3 children, finds any button_ref / label_ref / sprite_ref
 * that appears on more than 1 panel_child row.
 *
 * @param {string} databaseUrl
 * @returns {Promise<Array<{panelSlug: string, refSlug: string, count: number}>>}
 */
export async function findDuplicateStubReferences(databaseUrl) {
  const pgRequire = createRequire(join(REPO_ROOT, "tools/postgres-ia/package.json"));
  const pg = pgRequire("pg");
  const client = new pg.Client({ connectionString: databaseUrl });
  await client.connect();
  try {
    const res = await client.query(`
      WITH panel_child_refs AS (
        SELECT
          panel_ce.slug AS panel_slug,
          COALESCE(
            pc.params_json->>'button_ref',
            pc.params_json->>'label_ref',
            pc.params_json->>'sprite_ref'
          ) AS ref_slug,
          COUNT(*) OVER (PARTITION BY pc.panel_entity_id) AS total_children
        FROM panel_child pc
        JOIN catalog_entity panel_ce ON panel_ce.id = pc.panel_entity_id
        WHERE COALESCE(
          pc.params_json->>'button_ref',
          pc.params_json->>'label_ref',
          pc.params_json->>'sprite_ref'
        ) IS NOT NULL
      ),
      grouped AS (
        SELECT
          panel_slug,
          ref_slug,
          total_children,
          COUNT(*) AS dup_count
        FROM panel_child_refs
        WHERE total_children >= 3
        GROUP BY panel_slug, ref_slug, total_children
      )
      SELECT panel_slug, ref_slug, dup_count AS count
      FROM grouped
      WHERE dup_count > 1
      ORDER BY panel_slug, ref_slug
    `);
    return res.rows.map((r) => ({
      panelSlug: r.panel_slug,
      refSlug: r.ref_slug,
      count: parseInt(r.count, 10),
    }));
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
  let duplicates;
  try {
    [orphans, duplicates] = await Promise.all([
      findOrphanButtons(databaseUrl),
      findDuplicateStubReferences(databaseUrl),
    ]);
  } catch (err) {
    console.error(`[catalog-panel-coverage] DB query failed: ${err.message}`);
    process.exit(2);
  }

  let failed = false;

  // ── Rule 1: orphan buttons ──────────────────────────────────────────────────

  if (orphans.length > 0) {
    failed = true;
    console.error(`[catalog-panel-coverage] FAIL: ${orphans.length} orphan button(s) found:\n`);
    for (const slug of orphans) {
      console.error(`  ✗ ${slug}`);
    }
    console.error(`
Remediation: for each orphan slug above, either:
  (a) Register a parent panel and add a panel_child row with params_json->>'button_ref'='{slug}', OR
  (b) Retire the button via UPDATE catalog_entity SET retired_at=NOW() WHERE kind='button' AND slug='{slug}'.
`);
  }

  // ── Rule 2: duplicate-stub references ──────────────────────────────────────

  if (duplicates.length > 0) {
    failed = true;
    console.error(`[catalog-panel-coverage] FAIL: ${duplicates.length} duplicate-stub-reference violation(s) found:\n`);
    for (const { panelSlug, refSlug, count } of duplicates) {
      console.error(
        `[duplicate-stub] panel=${panelSlug} ref=${refSlug} count=${count} hint=register distinct slug per control`
      );
    }
    console.error(`
Remediation: for each panel + ref listed above:
  (a) Insert distinct catalog_entity rows per HUD control, OR
  (b) Run migration to replace stub panel_child rows with distinct button_ref/label_ref slugs.
`);
  }

  if (!failed) {
    console.log("[catalog-panel-coverage] All buttons have a parent panel. No duplicate-stub references. OK.");
    process.exit(0);
  }

  process.exit(1);
}

main().catch((err) => {
  console.error("[catalog-panel-coverage] unexpected error:", err);
  process.exit(2);
});
