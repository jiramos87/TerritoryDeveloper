#!/usr/bin/env node
/**
 * recovery-panels-patch — One-shot panels.json mutation for Phase A3 of the
 * UI Toolkit Parity Recovery plan. Bumps schema_version 4→5, sets
 * fields.theme:"cream" on plan-scope panels, adds minimal rows for
 * time-controls / glossary-panel / building-info / alerts-panel so the
 * theme-patch step can target their hand-authored UXMLs.
 *
 * Idempotent: re-running this is a no-op when state already matches.
 *
 * Why theme lives on fields.theme (not panel.theme): the bake handler
 * (`UiBakeHandler.BakePipeline.BakeFromPanelSnapshot`) reads `item.fields.*`
 * through JsonUtility, and unknown fields are tolerated. Sticking the theme
 * here keeps the source-of-truth on the snapshot row.
 */

import * as fs from 'node:fs';
import * as path from 'node:path';

const REPO_ROOT = path.resolve(path.dirname(new URL(import.meta.url).pathname), '..', '..');
const PANELS_JSON = path.join(REPO_ROOT, 'Assets', 'UI', 'Snapshots', 'panels.json');

const CREAM_SCOPE = new Set([
  'main-menu', 'hud-bar', 'toolbar', 'time-controls', 'tool-subtype-picker',
  'stats-panel', 'budget-panel', 'glossary-panel', 'info-panel',
  'building-info', 'alerts-panel',
]);

// Minimal panel-row factory for slugs not yet in panels.json. children=[] —
// the corresponding Assets/UI/Generated/{slug}.uxml is the live render
// source until the bake handler emits UXML from this row.
function minimalRow(slug, displayName, layoutTemplate, paramsJson, rectJson) {
  return {
    slug,
    fields: {
      display_name: displayName,
      layout_template: layoutTemplate,
      layout: layoutTemplate.startsWith('h') ? 'hstack' : 'vstack',
      gap_px: 8,
      padding_json: '{"top":8,"left":8,"right":8,"bottom":8,"border_width":2,"corner_radius":8,"border_color_token":"color-border-accent"}',
      params_json: paramsJson,
      rect_json: rectJson,
      theme: 'cream',
    },
    panel: {
      slug,
      layout_template: layoutTemplate,
      layout: layoutTemplate.startsWith('h') ? 'hstack' : 'vstack',
      gap_px: 8,
      padding_json: '{"top":8,"left":8,"right":8,"bottom":8,"border_width":2,"corner_radius":8,"border_color_token":"color-border-accent"}',
      params_json: paramsJson,
    },
    children: [],
  };
}

const MINIMAL_ROWS = [
  minimalRow(
    'time-controls', 'Time Controls', 'hstack',
    '{"width":192,"height":48,"anchor":"top-right"}',
    '{"pivot":[1,1],"anchor_max":[1,1],"anchor_min":[1,1],"size_delta":[192,48],"anchored_position":[-8,-64]}'
  ),
  minimalRow(
    'glossary-panel', 'Glossary', 'vstack',
    '{"width":640,"height":900}',
    '{"pivot":[0.5,0.5],"anchor_max":[0.5,0.5],"anchor_min":[0.5,0.5],"size_delta":[640,900],"anchored_position":[0,0]}'
  ),
  minimalRow(
    'building-info', 'Building Info', 'vstack',
    '{"width":420,"height":560}',
    '{"pivot":[1,0.5],"anchor_max":[1,0.5],"anchor_min":[1,0.5],"size_delta":[420,560],"anchored_position":[-16,0]}'
  ),
  minimalRow(
    'alerts-panel', 'Alerts', 'vstack',
    '{"width":420,"height":320,"anchor":"bottom-right"}',
    '{"pivot":[1,0],"anchor_max":[1,0],"anchor_min":[1,0],"size_delta":[420,320],"anchored_position":[-16,16]}'
  ),
];

function main() {
  const raw = fs.readFileSync(PANELS_JSON, 'utf8');
  const doc = JSON.parse(raw);

  // Schema bump (T10).
  if ((doc.schema_version ?? 0) < 5) doc.schema_version = 5;

  const haveBySlug = new Map((doc.items ?? []).map((it) => [it.slug, it]));

  // Set theme on existing plan-scope rows.
  let mutated = 0;
  for (const slug of CREAM_SCOPE) {
    const item = haveBySlug.get(slug);
    if (!item) continue;
    item.fields = item.fields ?? {};
    if (item.fields.theme !== 'cream') {
      item.fields.theme = 'cream';
      mutated++;
    }
    item.panel = item.panel ?? {};
    if (item.panel.theme !== 'cream') {
      item.panel.theme = 'cream';
      mutated++;
    }
  }

  // Append missing rows.
  for (const row of MINIMAL_ROWS) {
    if (haveBySlug.has(row.slug)) continue;
    doc.items.push(row);
    haveBySlug.set(row.slug, row);
    mutated++;
  }

  // Stable sort by slug to keep diffs tight.
  doc.items.sort((a, b) => String(a.slug ?? '').localeCompare(String(b.slug ?? '')));

  fs.writeFileSync(PANELS_JSON, JSON.stringify(doc, null, 2) + '\n');
  console.log(JSON.stringify({ ok: true, mutated, schema_version: doc.schema_version, item_count: doc.items.length }, null, 2));
}

main();
