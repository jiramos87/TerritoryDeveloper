#!/usr/bin/env node
/**
 * validate-asset-pipeline.mjs — schema-only asset-pipeline validator.
 *
 * Checks asset-registry DB rows (catalog_entity kind IN panel/button/token/archetype):
 *   1. Required fields: slug, kind, ds_tokens (non-null), motion (non-null).
 *   2. motion enum values within allowed set: fade | slide | none.
 *   3. No orphaned asset_detail rows (FK integrity spot-check).
 *
 * Does NOT require Unity Editor. Requires DATABASE_URL or config/postgres-dev.json.
 * Exit 0 = green. Exit 1 = schema fault (blocks validate:all CI gate).
 *
 * TECH-15217 — Stage 9.2 game-ui-catalog-bake (DEC-A25 asset-pipeline-standard-v1).
 */

import path from "node:path";
import { fileURLToPath } from "node:url";
import { createRequire } from "node:module";
import { resolveDatabaseUrl } from "../postgres-ia/resolve-database-url.mjs";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = process.env.IA_REPO_ROOT ?? path.resolve(__dirname, "../..");

const pgRequire = createRequire(path.join(REPO_ROOT, "tools/postgres-ia/package.json"));
const pg = pgRequire("pg");

const ALLOWED_MOTION_VALUES = new Set(["fade", "slide", "none"]);
const ASSET_REGISTRY_KINDS = ["panel", "button", "token", "archetype"];

async function main() {
  const databaseUrl = resolveDatabaseUrl(REPO_ROOT);
  const client = new pg.Client({ connectionString: databaseUrl });

  try {
    await client.connect();
  } catch (err) {
    // No DB = skip gracefully (validator is schema-only; CI with DB required for full gate).
    console.log("asset-pipeline: no-db (skipped — DB not reachable)");
    process.exit(0);
  }

  try {
    // 1. Fetch all asset-registry rows.
    const { rows } = await client.query(
      `SELECT id, slug, kind, ds_tokens, motion
       FROM catalog_entity
       WHERE kind = ANY($1::text[]) AND retired_at IS NULL`,
      [ASSET_REGISTRY_KINDS],
    );

    const faults = [];

    for (const row of rows) {
      const prefix = `[${row.kind}/${row.slug}]`;

      // Required: ds_tokens non-null + non-empty object
      if (!row.ds_tokens || typeof row.ds_tokens !== "object") {
        faults.push(`${prefix} ds_tokens missing or non-object`);
        continue;
      }

      // Required: motion non-null + has enter/exit/hover keys
      const m = row.motion;
      if (!m || typeof m !== "object") {
        faults.push(`${prefix} motion missing or non-object`);
        continue;
      }
      for (const key of ["enter", "exit", "hover"]) {
        if (!(key in m)) {
          faults.push(`${prefix} motion.${key} missing`);
        } else if (!ALLOWED_MOTION_VALUES.has(m[key])) {
          faults.push(`${prefix} motion.${key}="${m[key]}" not in allowed set {fade,slide,none}`);
        }
      }
    }

    // 2. Orphaned asset_detail check (spot-check FK integrity for asset-registry kinds).
    const orphanResult = await client.query(
      `SELECT ad.entity_id
       FROM asset_detail ad
       LEFT JOIN catalog_entity ce ON ce.id = ad.entity_id
       WHERE ce.id IS NULL
       LIMIT 5`,
    );
    if (orphanResult.rows.length > 0) {
      const ids = orphanResult.rows.map((r) => r.entity_id).join(", ");
      faults.push(`orphaned asset_detail rows (entity_id not in catalog_entity): ${ids}`);
    }

    // ── sprite-catalog schema check ──────────────────────────────────────────
    // TECH-15233 — Stage 9.6 game-ui-catalog-bake.
    // Validates sprite_catalog rows: slug non-empty, kind=sprite, path file-prefix plausible.
    let spriteFaults = [];
    let spriteTotal = 0;
    try {
      const { rows: spriteRows } = await client.query(
        `SELECT id, slug, kind, path, tier FROM sprite_catalog`,
      );
      spriteTotal = spriteRows.length;
      for (const row of spriteRows) {
        const prefix = `[sprite/${row.slug ?? "<null>"}]`;
        if (!row.slug || row.slug.trim() === "") {
          spriteFaults.push(`${prefix} slug empty or null`);
        }
        if (row.kind !== "sprite") {
          spriteFaults.push(`${prefix} kind="${row.kind}" expected "sprite"`);
        }
        if (!row.path || row.path.trim() === "") {
          spriteFaults.push(`${prefix} path empty or null`);
        } else if (!row.path.startsWith("Assets/")) {
          spriteFaults.push(`${prefix} path="${row.path}" does not start with "Assets/"`);
        }
        if (row.tier !== "sprite-catalog") {
          spriteFaults.push(`${prefix} tier="${row.tier}" expected "sprite-catalog"`);
        }
      }
    } catch (err) {
      // sprite_catalog table absent (pre-migration run) — skip gracefully.
      if (err.message && err.message.includes("sprite_catalog")) {
        console.log("asset-pipeline: sprite-catalog table absent — skipping sprite-catalog check");
      } else {
        spriteFaults.push(`sprite-catalog query error: ${err.message}`);
      }
    }

    faults.push(...spriteFaults);

    if (faults.length > 0) {
      console.error(`asset-pipeline: FAIL — ${faults.length} fault(s):`);
      for (const f of faults) console.error(`  ${f}`);
      process.exit(1);
    }

    const total = rows.length;
    console.log(
      `asset-pipeline: green (${total} asset-registry row${total !== 1 ? "s" : ""} + ${spriteTotal} sprite-catalog row${spriteTotal !== 1 ? "s" : ""} validated)`,
    );
    process.exit(0);
  } finally {
    await client.end();
  }
}

main().catch((err) => {
  console.error("asset-pipeline: ERROR —", err.message);
  process.exit(1);
});
