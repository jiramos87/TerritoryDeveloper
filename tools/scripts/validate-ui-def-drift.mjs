/**
 * validate-ui-def-drift.mjs
 * Pure-data drift-gate: panel_detail DB rows vs Assets/UI/Snapshots/panels.json.
 * Exit 0 = match. Exit 1 = drift listed by slug or fatal error.
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

async function main() {
  // ── 1. Snapshot ──────────────────────────────────────────────────────────
  if (!existsSync(SNAPSHOT_PATH)) {
    process.stderr.write('fatal: snapshots/panels.json missing\n');
    process.exit(1);
  }

  let snapshotItems;
  try {
    const raw = readFileSync(SNAPSHOT_PATH, 'utf8');
    const parsed = JSON.parse(raw);
    snapshotItems = parsed.items ?? [];
  } catch (err) {
    process.stderr.write(`fatal: panels.json parse error — ${err.message}\n`);
    process.exit(1);
  }

  // Build slug→rect_json map from snapshot (fields.rect_json is a JSON string)
  /** @type {Map<string, unknown>} */
  const snapshotMap = new Map();
  for (const item of snapshotItems) {
    const slug = item.slug;
    const rawRect = item.fields?.rect_json;
    if (slug && rawRect !== undefined) {
      let parsed = rawRect;
      if (typeof rawRect === 'string') {
        try { parsed = JSON.parse(rawRect); } catch { parsed = rawRect; }
      }
      snapshotMap.set(slug, parsed);
    }
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

  let rows;
  try {
    const result = await pool.query(`
      SELECT ce.slug, pd.rect_json
      FROM panel_detail pd
      JOIN catalog_entity ce ON ce.id = pd.entity_id
      WHERE ce.kind = 'panel'
    `);
    rows = result.rows;
  } catch (err) {
    process.stderr.write(`fatal: database unreachable — ${err.message}\n`);
    await pool.end().catch(() => {});
    process.exit(1);
  }

  await pool.end().catch(() => {});

  // ── 3. Diff ──────────────────────────────────────────────────────────────
  const drifts = [];

  for (const { slug, rect_json: dbRect } of rows) {
    if (!snapshotMap.has(slug)) continue; // panel not in snapshot — skip

    const snapRect = snapshotMap.get(slug);

    // Normalise both sides to plain objects
    const dbObj = typeof dbRect === 'string' ? JSON.parse(dbRect) : (dbRect ?? {});
    const snapObj = typeof snapRect === 'string' ? JSON.parse(snapRect) : (snapRect ?? {});

    // Deep equality check field-by-field
    const allKeys = new Set([...Object.keys(dbObj), ...Object.keys(snapObj)]);
    for (const field of allKeys) {
      const dbVal = JSON.stringify(dbObj[field] ?? null);
      const snapVal = JSON.stringify(snapObj[field] ?? null);
      if (dbVal !== snapVal) {
        drifts.push(`drift: ${slug} field=${field}`);
        break; // one line per slug
      }
    }
  }

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
