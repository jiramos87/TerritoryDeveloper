/**
 * validate-ui-def-drift.mjs
 * Pure-data drift-gate: DB rows vs Assets/UI/Snapshots per kind.
 *   kind=panel    → panel_detail.rect_json vs panels.json items[].fields.rect_json
 *   kind=token    → token_detail.value_json vs tokens.json items[].value_json
 *   kind=component → component_detail.default_props_json vs components.json items[].default_props_json
 * Exit 0 = match. Exit 1 = drift listed by slug/kind or fatal error.
 * No Unity Editor boot required.
 */

import { createRequire } from 'node:module';
import { existsSync, readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join, resolve } from 'node:path';

const require = createRequire(import.meta.url);
const __dirname = dirname(fileURLToPath(import.meta.url));

const REPO_ROOT = resolve(__dirname, '..', '..');
const SNAPSHOT_PATH = join(REPO_ROOT, 'Assets', 'UI', 'Snapshots', 'panels.json');
const TOKENS_SNAPSHOT_PATH = join(REPO_ROOT, 'Assets', 'UI', 'Snapshots', 'tokens.json');
const COMPONENTS_SNAPSHOT_PATH = join(REPO_ROOT, 'Assets', 'UI', 'Snapshots', 'components.json');

/** Parse a snapshot file and return items array. */
function loadSnapshot(filePath, kindLabel) {
  if (!existsSync(filePath)) {
    process.stderr.write(`fatal: snapshots/${kindLabel}.json missing\n`);
    process.exit(1);
  }
  try {
    const raw = readFileSync(filePath, 'utf8');
    const parsed = JSON.parse(raw);
    return parsed.items ?? [];
  } catch (err) {
    process.stderr.write(`fatal: ${kindLabel}.json parse error — ${err.message}\n`);
    process.exit(1);
  }
}

/** Deep-equal two JSON-serialisable values. */
function jsonEqual(a, b) {
  return JSON.stringify(a) === JSON.stringify(b);
}

/** Normalise a value to a plain object/primitive from string or object. */
function normalise(v) {
  if (v == null) return {};
  if (typeof v === 'string') {
    try { return JSON.parse(v); } catch { return v; }
  }
  return v;
}

async function main() {
  // ── 1. Load snapshots ────────────────────────────────────────────────────
  const panelItems = loadSnapshot(SNAPSHOT_PATH, 'panels');
  const tokenItems = loadSnapshot(TOKENS_SNAPSHOT_PATH, 'tokens');
  const componentItems = loadSnapshot(COMPONENTS_SNAPSHOT_PATH, 'components');

  // Build slug→value maps
  /** @type {Map<string, unknown>} */
  const panelSnapMap = new Map();
  for (const item of panelItems) {
    const slug = item.slug;
    const rawRect = item.fields?.rect_json;
    if (slug && rawRect !== undefined) {
      panelSnapMap.set(slug, normalise(rawRect));
    }
  }

  /** @type {Map<string, unknown>} */
  const tokenSnapMap = new Map();
  for (const item of tokenItems) {
    if (item.slug) tokenSnapMap.set(item.slug, normalise(item.value_json));
  }

  /** @type {Map<string, unknown>} */
  const componentSnapMap = new Map();
  for (const item of componentItems) {
    if (item.slug) componentSnapMap.set(item.slug, normalise(item.default_props_json));
  }

  // ── 2. DB ────────────────────────────────────────────────────────────────
  const { resolveDatabaseUrl } = await import('../postgres-ia/resolve-database-url.mjs');
  const dbUrl = resolveDatabaseUrl(REPO_ROOT);
  if (!dbUrl) {
    // CI without DB — skip gracefully (exit 0)
    process.stdout.write('info: DATABASE_URL not set; skipping ui-drift check in CI\n');
    process.exit(0);
  }

  let pool;
  try {
    const pg = require('pg');
    const Pool = pg.Pool ?? pg.default?.Pool;
    pool = new Pool({ connectionString: dbUrl });
  } catch (err) {
    process.stderr.write(`fatal: database unreachable — ${err.message}\n`);
    process.exit(1);
  }

  let panelRows, tokenRows, componentRows;
  try {
    const [pr, tr, cr] = await Promise.all([
      pool.query(`
        SELECT ce.slug, pd.rect_json
        FROM panel_detail pd
        JOIN catalog_entity ce ON ce.id = pd.entity_id
        WHERE ce.kind = 'panel'
      `),
      pool.query(`
        SELECT ce.slug, td.value_json
        FROM token_detail td
        JOIN catalog_entity ce ON ce.id = td.entity_id
        WHERE ce.kind = 'token'
      `),
      pool.query(`
        SELECT ce.slug, cd.default_props_json
        FROM component_detail cd
        JOIN catalog_entity ce ON ce.id = cd.entity_id
        WHERE ce.kind = 'component'
      `),
    ]);
    panelRows = pr.rows;
    tokenRows = tr.rows;
    componentRows = cr.rows;
  } catch (err) {
    process.stderr.write(`fatal: database unreachable — ${err.message}\n`);
    await pool.end().catch(() => {});
    process.exit(1);
  }

  await pool.end().catch(() => {});

  // ── 3. Diff — panels ─────────────────────────────────────────────────────
  const drifts = [];

  for (const { slug, rect_json: dbRect } of panelRows) {
    if (!panelSnapMap.has(slug)) continue;
    const dbObj = normalise(dbRect);
    const snapObj = panelSnapMap.get(slug);
    const allKeys = new Set([...Object.keys(dbObj), ...Object.keys(snapObj)]);
    for (const field of allKeys) {
      const dbVal = JSON.stringify(dbObj[field] ?? null);
      const snapVal = JSON.stringify(snapObj[field] ?? null);
      if (dbVal !== snapVal) {
        drifts.push(`drift: kind=panel slug=${slug} field=${field}`);
        break;
      }
    }
  }

  // ── 4. Diff — tokens ─────────────────────────────────────────────────────

  for (const { slug, value_json: dbValueJson } of tokenRows) {
    if (!tokenSnapMap.has(slug)) continue;
    const dbObj = normalise(dbValueJson);
    const snapObj = tokenSnapMap.get(slug);
    if (!jsonEqual(dbObj, snapObj)) {
      drifts.push(`drift: kind=token slug=${slug} field=value_json`);
    }
  }

  // ── 5. Diff — components ─────────────────────────────────────────────────

  for (const { slug, default_props_json: dbPropsJson } of componentRows) {
    if (!componentSnapMap.has(slug)) continue;
    const dbObj = normalise(dbPropsJson);
    const snapObj = componentSnapMap.get(slug);
    if (!jsonEqual(dbObj, snapObj)) {
      drifts.push(`drift: kind=component slug=${slug} field=default_props_json`);
    }
  }

  // ── 6. Report ─────────────────────────────────────────────────────────────

  if (drifts.length > 0) {
    for (const line of drifts) process.stdout.write(line + '\n');
    process.exit(1);
  }

  process.exit(0);
}

main().catch(err => {
  process.stderr.write(`fatal: database unreachable — ${err.message}\n`);
  process.exit(1);
});
