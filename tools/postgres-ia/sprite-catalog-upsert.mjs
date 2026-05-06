#!/usr/bin/env node
/**
 * sprite-catalog-upsert.mjs — upsert one row into sprite_catalog.
 *
 * Args: --slug <slug> --path <repo-relative-asset-path>
 * Idempotent: ON CONFLICT (path) DO NOTHING.
 * Exit 0 = inserted or already present. Exit 1 = error.
 *
 * TECH-15231 — Stage 9.6 game-ui-catalog-bake.
 */

import { resolveDatabaseUrl } from "./resolve-database-url.mjs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { createRequire } from "node:module";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = process.env.IA_REPO_ROOT ?? path.resolve(__dirname, "../..");

const pgRequire = createRequire(path.join(REPO_ROOT, "tools/postgres-ia/package.json"));
const pg = pgRequire("pg");

function parseArgs() {
  const args = process.argv.slice(2);
  const result = { slug: null, assetPath: null };
  for (let i = 0; i < args.length; i++) {
    if (args[i] === "--slug" && args[i + 1]) result.slug = args[++i];
    if (args[i] === "--path" && args[i + 1]) result.assetPath = args[++i];
  }
  return result;
}

async function main() {
  const { slug, assetPath } = parseArgs();
  if (!slug || !assetPath) {
    console.error("sprite-catalog-upsert: ERROR — missing --slug or --path");
    process.exit(1);
  }

  const databaseUrl = resolveDatabaseUrl(REPO_ROOT);
  if (!databaseUrl) {
    console.log("sprite-catalog-upsert: no-db (skipped — DB not reachable)");
    process.exit(0);
  }

  const client = new pg.Client({ connectionString: databaseUrl });
  try {
    await client.connect();
  } catch (err) {
    console.log(`sprite-catalog-upsert: no-db (connect failed — ${err.message})`);
    process.exit(0);
  }

  try {
    const res = await client.query(
      `INSERT INTO sprite_catalog (slug, kind, path, tier)
       VALUES ($1, 'sprite', $2, 'sprite-catalog')
       ON CONFLICT (path) DO NOTHING
       RETURNING id`,
      [slug, assetPath],
    );
    if (res.rowCount > 0) {
      console.log(`sprite-catalog-upsert: inserted id=${res.rows[0].id} slug=${slug} path=${assetPath}`);
    } else {
      console.log(`sprite-catalog-upsert: already-present slug=${slug} path=${assetPath}`);
    }
    process.exit(0);
  } catch (err) {
    console.error(`sprite-catalog-upsert: ERROR — ${err.message}`);
    process.exit(1);
  } finally {
    await client.end();
  }
}

main().catch((err) => {
  console.error("sprite-catalog-upsert: ERROR —", err.message);
  process.exit(1);
});
