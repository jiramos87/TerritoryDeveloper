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
 * Also writes tokens.json and components.json snapshots.
 *
 * Published-row gate: catalog_entity.kind='panel' AND
 * current_published_version_id IS NOT NULL AND retired_at IS NULL.
 * Children ordered by panel_child.order_idx ASC.
 *
 * Sprite ref shape: button_detail.sprite_icon_entity_id →
 * sprite_detail.assets_path (Unity-relative path string).
 *
 * Exit codes:
 *   0  wrote panels.json + tokens.json + components.json
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
const OUT_TOKENS_REL = 'Assets/UI/Snapshots/tokens.json';
const OUT_COMPONENTS_REL = 'Assets/UI/Snapshots/components.json';
const SCHEMA_VERSION = 4;

const require = createRequire(import.meta.url);
const pgRequire = createRequire(join(REPO_ROOT, 'tools/postgres-ia/package.json'));
const pg = pgRequire('pg');

/** Pull all component catalog_entity rows + component_detail. */
const COMPONENTS_QUERY = `
  SELECT
    ce.id           AS entity_id,
    ce.slug         AS slug,
    ce.display_name AS display_name,
    cd.role         AS role,
    cd.default_props_json AS default_props_json,
    cd.variants_json      AS variants_json
  FROM catalog_entity ce
  JOIN component_detail cd ON cd.entity_id = ce.id
  WHERE ce.kind = 'component'
    AND ce.retired_at IS NULL
  ORDER BY ce.slug ASC
`;

/** Pull all token catalog_entity rows + token_detail. */
const TOKENS_QUERY = `
  SELECT
    ce.id        AS entity_id,
    ce.slug      AS slug,
    ce.display_name AS display_name,
    td.token_kind   AS token_kind,
    td.value_json   AS value_json
  FROM catalog_entity ce
  JOIN token_detail td ON td.entity_id = ce.id
  WHERE ce.kind = 'token'
    AND ce.retired_at IS NULL
  ORDER BY ce.slug ASC
`;

/** Pull published panels + their ordered children + per-child sprite ref. */
const PANELS_QUERY = `
  SELECT
    pe.id                  AS panel_entity_id,
    pe.slug                AS panel_slug,
    pe.display_name        AS panel_display_name,
    pd.layout_template     AS panel_layout_template,
    pd.layout              AS panel_layout,
    pd.gap_px              AS panel_gap_px,
    pd.padding_json        AS panel_padding_json,
    pd.params_json         AS panel_params_json,
    pd.rect_json           AS panel_rect_json
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
    sd.assets_path               AS sprite_ref,
    pc.layout_json               AS layout_json,
    pc.instance_slug             AS instance_slug
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
        layout_json: k.layout_json == null
          ? null
          : typeof k.layout_json === 'string'
            ? k.layout_json
            : JSON.stringify(k.layout_json),
        instance_slug: k.instance_slug ?? null,
      }));
      totalChildren += children.length;
      items.push({
        slug: p.panel_slug,
        fields: {
          display_name: p.panel_display_name ?? '',
          layout_template: p.panel_layout_template ?? p.panel_layout ?? 'vstack',
          layout: p.panel_layout ?? '',
          gap_px: p.panel_gap_px,
          padding_json: typeof p.panel_padding_json === 'string'
            ? p.panel_padding_json
            : JSON.stringify(p.panel_padding_json ?? {}),
          params_json: typeof p.panel_params_json === 'string'
            ? p.panel_params_json
            : JSON.stringify(p.panel_params_json ?? {}),
          rect_json: typeof p.panel_rect_json === 'string'
            ? p.panel_rect_json
            : JSON.stringify(p.panel_rect_json ?? {}),
        },
        // Legacy shape kept for backwards compat readers.
        panel: {
          slug: p.panel_slug,
          layout_template: p.panel_layout_template ?? p.panel_layout ?? 'vstack',
          layout: p.panel_layout ?? '',
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

    // ── Tokens snapshot ──────────────────────────────────────────────────────
    const tokenRows = (await client.query(TOKENS_QUERY)).rows;
    const tokenItems = tokenRows.map((r) => ({
      slug: r.slug,
      display_name: r.display_name,
      token_kind: r.token_kind,
      value_json: typeof r.value_json === 'string' ? r.value_json : JSON.stringify(r.value_json ?? {}),
    }));

    const tokensSnapshot = {
      snapshot_id: new Date().toISOString(),
      kind: 'tokens',
      schema_version: 1,
      items: tokenItems,
    };

    const tokensOutAbs = join(REPO_ROOT, OUT_TOKENS_REL);
    writeFileSync(tokensOutAbs, JSON.stringify(tokensSnapshot, null, 2) + '\n', 'utf8');
    console.log(`wrote tokens.json (${tokenItems.length} tokens)`);

    // ── Components snapshot ──────────────────────────────────────────────────
    const componentRows = (await client.query(COMPONENTS_QUERY)).rows;
    const componentItems = componentRows.map((r) => ({
      slug: r.slug,
      display_name: r.display_name,
      role: r.role,
      default_props_json: typeof r.default_props_json === 'string'
        ? r.default_props_json
        : JSON.stringify(r.default_props_json ?? {}),
      variants_json: typeof r.variants_json === 'string'
        ? r.variants_json
        : JSON.stringify(r.variants_json ?? []),
    }));

    const componentsSnapshot = {
      snapshot_id: new Date().toISOString(),
      kind: 'components',
      schema_version: 1,
      items: componentItems,
    };

    const componentsOutAbs = join(REPO_ROOT, OUT_COMPONENTS_REL);
    writeFileSync(componentsOutAbs, JSON.stringify(componentsSnapshot, null, 2) + '\n', 'utf8');
    console.log(`wrote components.json (${componentItems.length} components)`);
  } finally {
    await client.end();
  }
}

main().catch((e) => {
  console.error('snapshot-export-game-ui:', e?.stack || e);
  process.exit(1);
});
