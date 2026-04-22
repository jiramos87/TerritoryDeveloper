#!/usr/bin/env node
/**
 * Compares `db/migrations/0011` / `0012` `CREATE TABLE` column names to presence in
 * `web/types/api/*.ts` (snake_case property names on DTO rows). Fails on missing field or missing file.
 * SQL is authoritative — run after editing either side.
 */
import { readFileSync, existsSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";

const __dirname = dirname(fileURLToPath(import.meta.url));
const root = join(__dirname, "../..");

function read(p) {
  return readFileSync(join(root, p), "utf8");
}

/** Extract data column names from a CREATE TABLE ( … ) block. */
function extractTableColumns(sql, tableName) {
  const startMarker = `CREATE TABLE IF NOT EXISTS ${tableName}`;
  const i = sql.indexOf(startMarker);
  if (i < 0) {
    throw new Error(`Table block not found: ${tableName}`);
  }
  const from = i + startMarker.length;
  const sub = sql.slice(from);
  const open = sub.indexOf("(");
  if (open < 0) return [];
  let depth = 0;
  let j = open;
  for (; j < sub.length; j++) {
    const c = sub[j];
    if (c === "(") depth += 1;
    if (c === ")") {
      depth -= 1;
      if (depth === 0) {
        j += 1;
        break;
      }
    }
  }
  const block = sub.slice(open + 1, j - 1);
  const lines = block.split("\n");
  const cols = [];
  for (const line of lines) {
    const t = line.trim();
    if (!t || t.startsWith("--")) continue;
    const u = t.toUpperCase();
    if (u.startsWith("CONSTRAINT ")) continue;
    if (u.startsWith("PRIMARY KEY")) continue;
    if (u.startsWith("UNIQUE ")) continue;
    if (u.startsWith("FOREIGN KEY")) continue;
    if (u.startsWith("REFERENCES ")) continue;
    if (u.startsWith("CHECK ")) continue;
    const m = /^([a-z_][a-z0-9_]*)\s/i.exec(t);
    if (m) cols.push(m[1]);
  }
  return cols;
}

const expectations = [
  { table: "catalog_asset", file: "web/types/api/catalog-asset.ts" },
  { table: "catalog_sprite", file: "web/types/api/catalog-sprite.ts" },
  { table: "catalog_asset_sprite", file: "web/types/api/catalog-asset-sprite.ts" },
  { table: "catalog_economy", file: "web/types/api/catalog-economy.ts" },
  { table: "catalog_spawn_pool", file: "web/types/api/catalog-pool.ts" },
  { table: "catalog_pool_member", file: "web/types/api/catalog-pool.ts" },
];

const sql11 = read("db/migrations/0011_catalog_core.sql");
const sql12 = read("db/migrations/0012_catalog_spawn_pools.sql");

let failed = false;
for (const { table, file } of expectations) {
  const sql = ["catalog_spawn_pool", "catalog_pool_member"].includes(table) ? sql12 : sql11;
  const cols = extractTableColumns(sql, table);
  const pathF = join(root, file);
  if (!existsSync(pathF)) {
    console.error(`catalog-dto-migration-check: missing file ${file}`);
    failed = true;
    continue;
  }
  const ts = read(file);
  for (const col of cols) {
    const re = new RegExp(`\\b${col}\\b`);
    if (!re.test(ts)) {
      console.error(
        `catalog-dto-migration-check: ${table}.${col} not found in ${file} (align DTO to SQL)`
      );
      failed = true;
    }
  }
}

if (failed) {
  process.exit(1);
}
console.log("catalog-dto-migration-check: OK (0011/0012 columns present in web/types/api DTOs)");
