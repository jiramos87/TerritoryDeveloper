#!/usr/bin/env tsx
/**
 * validate-trgm-indexes.ts — TECH-4180 / Stage 15.1.
 *
 * Asserts that the three pg_trgm GIN indexes on `catalog_entity` exist in the
 * target DB (0051_pg_trgm_search.sql applied). Skips cleanly when
 * DATABASE_URL is unset (CI without DB).
 *
 * Exit codes:
 *   0  all three indexes present (or DATABASE_URL unset → skip).
 *   1  one or more indexes missing.
 *   2  internal error (DB connection failure).
 */

import { config as dotenvConfig } from "dotenv";
import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = resolve(__dirname, "..", "..");

dotenvConfig({ path: resolve(REPO_ROOT, ".env") });
dotenvConfig({ path: resolve(REPO_ROOT, ".env.local"), override: true });

const DATABASE_URL = process.env["DATABASE_URL"];
if (!DATABASE_URL) {
  console.log("validate-trgm-indexes: DATABASE_URL unset — skipping.");
  process.exit(0);
}

const REQUIRED_INDEXES = [
  "catalog_entity_name_trgm_idx",
  "catalog_entity_slug_trgm_idx",
  "catalog_entity_tags_trgm_idx",
];

async function main(): Promise<void> {
  const postgres = (await import("postgres")).default;
  const sql = postgres(DATABASE_URL!, { max: 1 });

  try {
    const rows = await sql<{ indexname: string }[]>`
      SELECT indexname
      FROM pg_indexes
      WHERE tablename = 'catalog_entity'
        AND indexname = ANY(${REQUIRED_INDEXES}::text[])
    `;
    const found = new Set(rows.map((r) => r.indexname));
    const missing = REQUIRED_INDEXES.filter((n) => !found.has(n));
    if (missing.length > 0) {
      console.error(`validate-trgm-indexes: MISSING indexes: ${missing.join(", ")}`);
      console.error("  Run: npm run db:migrate (applies 0051_pg_trgm_search.sql)");
      process.exit(1);
    }
    console.log(`validate-trgm-indexes: OK — ${REQUIRED_INDEXES.length} GIN indexes present.`);
    process.exit(0);
  } catch (err) {
    console.error("validate-trgm-indexes: DB error:", err);
    process.exit(2);
  } finally {
    await sql.end();
  }
}

await main();
