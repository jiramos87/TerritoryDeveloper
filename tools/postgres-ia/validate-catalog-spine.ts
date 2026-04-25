#!/usr/bin/env tsx
/**
 * validate-catalog-spine.ts
 *
 * Stage 1 gate (asset-pipeline-stage-0-1-impl.md). Asserts spine schema
 * invariants after 0021_catalog_spine.sql + 0022_catalog_detail_link.sql.
 *
 * Read-only. Never mutates DB state.
 *
 * Invariants (8):
 *   1. Every `catalog_entity` row has matching `*_detail` row for its kind.
 *   2. `entity_version.entity_id` valid; version_number monotonic per entity (no gaps from 1).
 *   3. `current_published_version_id` (when set) points at a published version
 *      of the same entity.
 *   4. Slug regex satisfied for every `catalog_entity` row (defensive — also
 *      enforced by CHECK).
 *   5. No `(kind, slug)` collisions (defensive — also enforced by UNIQUE).
 *   6. `asset_detail.*_sprite_entity_id` references entities of kind='sprite'.
 *   7. `pool_member.pool_entity_id` references kind='pool';
 *      `pool_member.asset_entity_id` references kind='asset'.
 *   8. `catalog_touch_updated_at` trigger present on `catalog_entity` and
 *      `entity_version`.
 *
 * Inputs: DATABASE_URL or config/postgres-dev.json (resolveDatabaseUrl).
 *
 * Exit codes:
 *   0 — all 8 invariants pass.
 *   1 — DB unreachable, required spine table missing, or any invariant fails.
 */

import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { Client } from 'pg';
import { resolveDatabaseUrl } from './resolve-database-url.mjs';

const __dirname = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = resolve(__dirname, '../..');

const SLUG_REGEX = /^[a-z][a-z0-9_]{2,63}$/;

const KIND_TO_DETAIL: Record<string, string> = {
  sprite: 'sprite_detail',
  asset: 'asset_detail',
  pool: 'pool_detail',
  // 'button' / 'panel' / 'token' / 'archetype' / 'audio' detail tables land
  // in later stages — ignore until a row of that kind exists.
};

interface Failure {
  invariant: number;
  message: string;
  detail?: unknown;
}

async function tableExists(client: Client, name: string): Promise<boolean> {
  const { rows } = await client.query(
    `SELECT 1 FROM information_schema.tables
      WHERE table_schema = 'public' AND table_name = $1`,
    [name],
  );
  return rows.length > 0;
}

async function preflight(client: Client): Promise<void> {
  const required = [
    'catalog_entity',
    'entity_version',
    'asset_detail',
    'sprite_detail',
    'economy_detail',
    'pool_detail',
    'pool_member',
  ];
  const missing: string[] = [];
  for (const t of required) {
    if (!(await tableExists(client, t))) missing.push(t);
  }
  if (missing.length > 0) {
    console.error(
      `[validate:catalog-spine] missing spine tables — run db:migrate first. missing=${missing.join(', ')}`,
    );
    process.exit(1);
  }
}

async function checkInvariant1(client: Client, fails: Failure[]): Promise<void> {
  // Every catalog_entity row has matching *_detail row appropriate for its kind.
  for (const [kind, detailTable] of Object.entries(KIND_TO_DETAIL)) {
    const { rows } = await client.query(
      `SELECT ce.id, ce.kind, ce.slug
         FROM catalog_entity ce
    LEFT JOIN ${detailTable} d ON d.entity_id = ce.id
        WHERE ce.kind = $1 AND d.entity_id IS NULL
        ORDER BY ce.id
        LIMIT 50`,
      [kind],
    );
    if (rows.length > 0) {
      fails.push({
        invariant: 1,
        message: `entities of kind=${kind} missing ${detailTable} row`,
        detail: { sample_count: rows.length, sample: rows.slice(0, 5) },
      });
    }
  }

  // Reverse: detail rows must correspond to an entity of the right kind.
  for (const [kind, detailTable] of Object.entries(KIND_TO_DETAIL)) {
    const { rows } = await client.query(
      `SELECT d.entity_id, ce.kind
         FROM ${detailTable} d
    LEFT JOIN catalog_entity ce ON ce.id = d.entity_id
        WHERE ce.id IS NULL OR ce.kind <> $1
        ORDER BY d.entity_id
        LIMIT 50`,
      [kind],
    );
    if (rows.length > 0) {
      fails.push({
        invariant: 1,
        message: `${detailTable} rows missing or pointing at wrong-kind entity`,
        detail: { sample_count: rows.length, sample: rows.slice(0, 5) },
      });
    }
  }
}

async function checkInvariant2(client: Client, fails: Failure[]): Promise<void> {
  // entity_id valid + version_number monotonic from 1 with no gaps.
  const orphan = await client.query(`
    SELECT ev.id, ev.entity_id
      FROM entity_version ev
 LEFT JOIN catalog_entity ce ON ce.id = ev.entity_id
     WHERE ce.id IS NULL
     LIMIT 50
  `);
  if (orphan.rows.length > 0) {
    fails.push({
      invariant: 2,
      message: 'entity_version rows reference missing catalog_entity',
      detail: { sample_count: orphan.rows.length, sample: orphan.rows.slice(0, 5) },
    });
  }

  const gaps = await client.query(`
    SELECT entity_id,
           array_agg(version_number ORDER BY version_number) AS versions,
           min(version_number) AS min_v,
           max(version_number) AS max_v,
           count(*)            AS n
      FROM entity_version
  GROUP BY entity_id
    HAVING min(version_number) <> 1
        OR max(version_number) <> count(*)
     LIMIT 50
  `);
  if (gaps.rows.length > 0) {
    fails.push({
      invariant: 2,
      message: 'entity_version.version_number not monotonic 1..N per entity',
      detail: { sample_count: gaps.rows.length, sample: gaps.rows.slice(0, 5) },
    });
  }
}

async function checkInvariant3(client: Client, fails: Failure[]): Promise<void> {
  // current_published_version_id (when set) points at a published version
  // of the SAME entity.
  const bad = await client.query(`
    SELECT ce.id AS entity_id,
           ce.current_published_version_id AS version_id,
           ev.entity_id AS version_entity_id,
           ev.status
      FROM catalog_entity ce
 LEFT JOIN entity_version ev ON ev.id = ce.current_published_version_id
     WHERE ce.current_published_version_id IS NOT NULL
       AND (ev.id IS NULL OR ev.status <> 'published' OR ev.entity_id <> ce.id)
     LIMIT 50
  `);
  if (bad.rows.length > 0) {
    fails.push({
      invariant: 3,
      message: 'current_published_version_id mispoints (missing / not published / wrong entity)',
      detail: { sample_count: bad.rows.length, sample: bad.rows.slice(0, 5) },
    });
  }
}

async function checkInvariant4(client: Client, fails: Failure[]): Promise<void> {
  const { rows } = await client.query(`
    SELECT id, kind, slug FROM catalog_entity
     ORDER BY id
  `);
  const offenders = rows.filter((r) => !SLUG_REGEX.test(String(r.slug)));
  if (offenders.length > 0) {
    fails.push({
      invariant: 4,
      message: `slug regex violated by ${offenders.length} row(s)`,
      detail: { sample: offenders.slice(0, 5) },
    });
  }
}

async function checkInvariant5(client: Client, fails: Failure[]): Promise<void> {
  const { rows } = await client.query(`
    SELECT kind, slug, count(*) AS n
      FROM catalog_entity
  GROUP BY kind, slug
    HAVING count(*) > 1
     LIMIT 50
  `);
  if (rows.length > 0) {
    fails.push({
      invariant: 5,
      message: '(kind, slug) duplicates found',
      detail: { sample_count: rows.length, sample: rows.slice(0, 5) },
    });
  }
}

async function checkInvariant6(client: Client, fails: Failure[]): Promise<void> {
  const slotCols = [
    'world_sprite_entity_id',
    'button_target_sprite_entity_id',
    'button_pressed_sprite_entity_id',
    'button_disabled_sprite_entity_id',
    'button_hover_sprite_entity_id',
  ];
  for (const col of slotCols) {
    const { rows } = await client.query(`
      SELECT ad.entity_id AS asset_entity_id,
             ad.${col}    AS sprite_entity_id,
             ce.kind       AS pointed_kind
        FROM asset_detail ad
   LEFT JOIN catalog_entity ce ON ce.id = ad.${col}
       WHERE ad.${col} IS NOT NULL
         AND (ce.id IS NULL OR ce.kind <> 'sprite')
       LIMIT 50
    `);
    if (rows.length > 0) {
      fails.push({
        invariant: 6,
        message: `asset_detail.${col} points at non-sprite entity`,
        detail: { sample_count: rows.length, sample: rows.slice(0, 5) },
      });
    }
  }
}

async function checkInvariant7(client: Client, fails: Failure[]): Promise<void> {
  const badPool = await client.query(`
    SELECT pm.pool_entity_id, ce.kind
      FROM pool_member pm
 LEFT JOIN catalog_entity ce ON ce.id = pm.pool_entity_id
     WHERE ce.id IS NULL OR ce.kind <> 'pool'
     LIMIT 50
  `);
  if (badPool.rows.length > 0) {
    fails.push({
      invariant: 7,
      message: 'pool_member.pool_entity_id points at non-pool entity',
      detail: { sample_count: badPool.rows.length, sample: badPool.rows.slice(0, 5) },
    });
  }

  const badAsset = await client.query(`
    SELECT pm.asset_entity_id, ce.kind
      FROM pool_member pm
 LEFT JOIN catalog_entity ce ON ce.id = pm.asset_entity_id
     WHERE ce.id IS NULL OR ce.kind <> 'asset'
     LIMIT 50
  `);
  if (badAsset.rows.length > 0) {
    fails.push({
      invariant: 7,
      message: 'pool_member.asset_entity_id points at non-asset entity',
      detail: { sample_count: badAsset.rows.length, sample: badAsset.rows.slice(0, 5) },
    });
  }
}

async function checkInvariant8(client: Client, fails: Failure[]): Promise<void> {
  const { rows } = await client.query(`
    SELECT event_object_table AS tbl, trigger_name
      FROM information_schema.triggers
     WHERE event_object_schema = 'public'
       AND event_object_table IN ('catalog_entity', 'entity_version')
       AND action_statement ILIKE '%catalog_touch_updated_at%'
  `);
  const tables = new Set(rows.map((r) => r.tbl));
  for (const expected of ['catalog_entity', 'entity_version']) {
    if (!tables.has(expected)) {
      fails.push({
        invariant: 8,
        message: `catalog_touch_updated_at trigger missing on ${expected}`,
      });
    }
  }
}

function redact(url: string): string {
  try {
    const u = new URL(url);
    if (u.password) u.password = '***';
    return u.toString();
  } catch {
    return url;
  }
}

async function main(): Promise<void> {
  const url = resolveDatabaseUrl(REPO_ROOT);
  if (!url) {
    console.error('[validate:catalog-spine] no DATABASE_URL or config/postgres-dev.json');
    process.exit(1);
  }
  const client = new Client({ connectionString: url });
  await client.connect();
  try {
    await preflight(client);
    const fails: Failure[] = [];
    await checkInvariant1(client, fails);
    await checkInvariant2(client, fails);
    await checkInvariant3(client, fails);
    await checkInvariant4(client, fails);
    await checkInvariant5(client, fails);
    await checkInvariant6(client, fails);
    await checkInvariant7(client, fails);
    await checkInvariant8(client, fails);

    if (fails.length > 0) {
      console.error(`[validate:catalog-spine] FAIL — ${fails.length} invariant violation(s) (db=${redact(url)}):`);
      for (const f of fails) {
        console.error(`  invariant ${f.invariant}: ${f.message}`);
        if (f.detail) console.error(`    detail: ${JSON.stringify(f.detail)}`);
      }
      process.exit(1);
    }

    console.log(`[validate:catalog-spine] OK — 8 invariants pass (db=${redact(url)})`);
  } finally {
    await client.end();
  }
}

main().catch((err) => {
  console.error('[validate:catalog-spine] error:', err);
  process.exit(1);
});
