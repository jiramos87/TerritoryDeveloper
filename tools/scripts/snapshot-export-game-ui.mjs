#!/usr/bin/env node
/**
 * snapshot-export-game-ui.mjs
 *
 * TECH-11927 / game-ui-catalog-bake Stage 1.0 §Plan Digest.
 *
 * Reads published `panel` catalog rows joined with `panel_child` + button
 * `sprite_icon_entity_id` + `sprite_detail.assets_path`, and writes
 * `Assets/UI/Snapshots/panels.json` per `ia/specs/catalog-architecture.md §5.2`:
 *
 *   {
 *     snapshot_id:    string,        // ISO-8601 UTC timestamp
 *     kind:           "panels",
 *     schema_version: 1,
 *     items: [
 *       {
 *         panel:    { slug, layout, gap_px, padding_json },
 *         children: [{ ord, kind, params_json, sprite_ref }, ...]
 *       },
 *       ...
 *     ]
 *   }
 *
 * Published-row gate: catalog_entity.kind='panel' AND
 * current_published_version_id IS NOT NULL AND retired_at IS NULL.
 * Children ordered by panel_child.order_idx ASC.
 *
 * Sprite ref shape: button_detail.sprite_icon_entity_id →
 * sprite_detail.assets_path (Unity-relative path string).
 *
 * Exit codes:
 *   0  wrote panels.json
 *   1  DB error or write failure
 */

import { mkdirSync, writeFileSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { createRequire } from 'node:module';

import { resolveDatabaseUrl } from '../postgres-ia/resolve-database-url.mjs';

const __dirname = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = resolve(__dirname, '../..');
const OUT_REL = 'Assets/UI/Snapshots/panels.json';
const SCHEMA_VERSION = 1;

const require = createRequire(import.meta.url);
const pgRequire = createRequire(join(REPO_ROOT, 'tools/postgres-ia/package.json'));
const pg = pgRequire('pg');

/** Pull published panels + their ordered children + per-child sprite ref. */
const PANELS_QUERY = `
  SELECT
    pe.id              AS panel_entity_id,
    pe.slug            AS panel_slug,
    pd.layout          AS panel_layout,
    pd.gap_px          AS panel_gap_px,
    pd.padding_json    AS panel_padding_json,
    pd.params_json     AS panel_params_json
  FROM catalog_entity pe
  JOIN panel_detail pd ON pd.entity_id = pe.id
  WHERE pe.kind = 'panel'
    AND pe.current_published_version_id IS NOT NULL
    AND pe.retired_at IS NULL
  ORDER BY pe.slug ASC
`;

const CHILDREN_QUERY = `
  SELECT
    pc.order_idx                 AS ord,
    pc.child_kind                AS kind,
    pc.params_json               AS params_json,
    sd.assets_path               AS sprite_ref
  FROM panel_child pc
  LEFT JOIN button_detail bd ON bd.entity_id = pc.child_entity_id
  LEFT JOIN sprite_detail sd ON sd.entity_id = bd.sprite_icon_entity_id
  WHERE pc.panel_entity_id = $1
  ORDER BY pc.order_idx ASC
`;

async function main() {
  const databaseUrl = resolveDatabaseUrl(REPO_ROOT);
  if (!databaseUrl) {
    console.error('snapshot-export-game-ui: missing DATABASE_URL — see docs/postgres-ia-dev-setup.md');
    process.exit(1);
  }

  const client = new pg.Client({ connectionString: databaseUrl });
  await client.connect();

  let panels;
  try {
    panels = (await client.query(PANELS_QUERY)).rows;

    const items = [];
    let totalChildren = 0;
    for (const p of panels) {
      const { rows: kids } = await client.query(CHILDREN_QUERY, [p.panel_entity_id]);
      const children = kids.map((k) => ({
        ord: k.ord,
        kind: k.kind,
        params_json: typeof k.params_json === 'string' ? k.params_json : JSON.stringify(k.params_json ?? {}),
        sprite_ref: k.sprite_ref ?? '',
      }));
      totalChildren += children.length;
      items.push({
        panel: {
          slug: p.panel_slug,
          layout: p.panel_layout,
          gap_px: p.panel_gap_px,
          padding_json: typeof p.panel_padding_json === 'string'
            ? p.panel_padding_json
            : JSON.stringify(p.panel_padding_json ?? {}),
          params_json: typeof p.panel_params_json === 'string'
            ? p.panel_params_json
            : JSON.stringify(p.panel_params_json ?? {}),
        },
        children,
      });
    }

    const snapshot = {
      snapshot_id: new Date().toISOString(),
      kind: 'panels',
      schema_version: SCHEMA_VERSION,
      items,
    };

    const outAbs = join(REPO_ROOT, OUT_REL);
    mkdirSync(dirname(outAbs), { recursive: true });
    writeFileSync(outAbs, JSON.stringify(snapshot, null, 2) + '\n', 'utf8');

    console.log(`wrote panels.json (${items.length} panels, ${totalChildren} children)`);
  } finally {
    await client.end();
  }
}

main().catch((e) => {
  console.error('snapshot-export-game-ui:', e?.stack || e);
  process.exit(1);
});
