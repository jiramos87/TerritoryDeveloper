/**
 * snapshot-export-game-ui.test.mjs
 *
 * TECH-17990 / game-ui-catalog-bake Stage 9.10 T1.
 *
 * Asserts the emitted panels.json shape includes layout_json.zone per child
 * for hud-bar panel entries. Uses the committed snapshot file as fixture
 * (exporter runs against live DB; CI uses committed output).
 */

import assert from 'node:assert';
import { readFileSync } from 'node:fs';
import { join, resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { test } from 'node:test';

const __dirname = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = resolve(__dirname, '..', '..', '..');
const PANELS_JSON_PATH = join(REPO_ROOT, 'Assets', 'UI', 'Snapshots', 'panels.json');

function loadPanels() {
  const raw = readFileSync(PANELS_JSON_PATH, 'utf-8');
  return JSON.parse(raw);
}

test('panels.json schema_version is 3', () => {
  const snapshot = loadPanels();
  assert.strictEqual(snapshot.schema_version, 3, 'expected schema_version 3 after layout_json.zone extension');
});

test('panels.json items[] is non-empty array', () => {
  const snapshot = loadPanels();
  assert.ok(Array.isArray(snapshot.items), 'items must be array');
  assert.ok(snapshot.items.length > 0, 'items must have at least one entry');
});

test('test_zone_per_child_present — every hud-bar child has layout_json.zone', () => {
  const snapshot = loadPanels();
  const hudBar = snapshot.items.find(
    (i) => i.slug === 'hud_bar' || i?.panel?.slug === 'hud_bar',
  );
  assert.ok(hudBar, 'hud_bar panel must exist in panels.json');

  const children = hudBar.children;
  assert.ok(Array.isArray(children), 'hud_bar children must be array');
  assert.ok(children.length > 0, 'hud_bar must have at least one child');

  const validZones = new Set(['left', 'center', 'right']);
  for (const child of children) {
    assert.ok(
      child.layout_json !== null && child.layout_json !== undefined,
      `child ord=${child.ord} missing layout_json`,
    );
    assert.ok(
      typeof child.layout_json.zone === 'string',
      `child ord=${child.ord} layout_json.zone must be string`,
    );
    assert.ok(
      validZones.has(child.layout_json.zone),
      `child ord=${child.ord} layout_json.zone="${child.layout_json.zone}" must be left|center|right`,
    );
  }
});

test('hud-bar children have at least one left, center, and right zone', () => {
  const snapshot = loadPanels();
  const hudBar = snapshot.items.find(
    (i) => i.slug === 'hud_bar' || i?.panel?.slug === 'hud_bar',
  );
  assert.ok(hudBar, 'hud_bar panel must exist');

  const zones = new Set(hudBar.children.map((c) => c?.layout_json?.zone));
  assert.ok(zones.has('left'), 'must have at least one "left" zone child');
  assert.ok(zones.has('center'), 'must have at least one "center" zone child');
  assert.ok(zones.has('right'), 'must have at least one "right" zone child');
});

test('hud-bar item has slug and fields.layout_template', () => {
  const snapshot = loadPanels();
  const hudBar = snapshot.items.find((i) => i.slug === 'hud_bar');
  assert.ok(hudBar, 'top-level slug field must exist on hud_bar item');
  assert.ok(hudBar.fields, 'fields block must exist');
  assert.ok(
    typeof hudBar.fields.layout_template === 'string' && hudBar.fields.layout_template.length > 0,
    'fields.layout_template must be non-empty string',
  );
});
