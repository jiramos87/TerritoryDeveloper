/**
 * Stage 13.0 — Canvas layering test suite (TECH-29763 → TECH-29766).
 * Red state at T13.0.1 creation; green on stage close.
 * Node --test runner.
 *
 * Red-Stage Proof anchor: Notifications_SortingOrder_AboveSubtypePicker
 * Asserts notifications-toast canvas_sorting_order > tool-subtype-picker canvas_sorting_order
 * in scene-wire-plan.yaml. Layer 3 canvas layering audit reports zero hierarchy violations.
 * B.7c visual diff passes with notifications-above-picker baseline row in ia_ui_bake_history.
 */
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync, existsSync } from 'node:fs';
import { resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import pg from 'pg';

const { Client } = pg;

const __dirname = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = resolve(__dirname, '..', '..');
const PLAN_PATH = resolve(REPO_ROOT, 'Assets', 'Resources', 'UI', 'Generated', 'scene-wire-plan.yaml');

const DB_URL = process.env.DATABASE_URL || 'postgresql://postgres:postgres@localhost:5434/territory_ia_dev';

async function dbClient() {
  const client = new Client({ connectionString: DB_URL });
  await client.connect();
  return client;
}

function parsePlan(content) {
  const panels = [];
  let current = null;
  for (const rawLine of content.split('\n')) {
    const line = rawLine.trimEnd();
    if (line.startsWith('- panel_slug:')) {
      if (current) panels.push(current);
      current = { panel_slug: line.replace('- panel_slug:', '').trim() };
    } else if (current) {
      const m = line.match(/^\s+([\w_]+):\s*(.+)$/);
      if (m) current[m[1]] = m[2].trim();
    }
  }
  if (current) panels.push(current);
  return panels;
}

// ── T13.0.1/T13.0.2 — scene-wire-plan.yaml exists + notifications > subtype-picker ──

test('Notifications_SortingOrder_AboveSubtypePicker — scene-wire-plan.yaml exists', () => {
  assert.ok(
    existsSync(PLAN_PATH),
    `scene-wire-plan.yaml missing at ${PLAN_PATH}`
  );
});

test('Notifications_SortingOrder_AboveSubtypePicker — notifications-toast has canvas_sorting_order', () => {
  const content = readFileSync(PLAN_PATH, 'utf8');
  const panels = parsePlan(content);
  const notif = panels.find(p => p.panel_slug === 'notifications-toast');
  assert.ok(notif, 'notifications-toast entry missing from scene-wire-plan.yaml');
  assert.ok(
    notif.canvas_sorting_order !== undefined,
    'notifications-toast missing canvas_sorting_order'
  );
  assert.equal(
    notif.canvas_sorting_layer,
    'Notifications',
    `Expected notifications-toast canvas_sorting_layer=Notifications, got ${notif.canvas_sorting_layer}`
  );
});

test('Notifications_SortingOrder_AboveSubtypePicker — tool-subtype-picker has canvas_sorting_order', () => {
  const content = readFileSync(PLAN_PATH, 'utf8');
  const panels = parsePlan(content);
  const picker = panels.find(p => p.panel_slug === 'tool-subtype-picker');
  assert.ok(picker, 'tool-subtype-picker entry missing from scene-wire-plan.yaml');
  assert.ok(
    picker.canvas_sorting_order !== undefined,
    'tool-subtype-picker missing canvas_sorting_order'
  );
});

test('Notifications_SortingOrder_AboveSubtypePicker — notifications sortingOrder > subtype-picker sortingOrder', () => {
  const content = readFileSync(PLAN_PATH, 'utf8');
  const panels = parsePlan(content);
  const notif = panels.find(p => p.panel_slug === 'notifications-toast');
  const picker = panels.find(p => p.panel_slug === 'tool-subtype-picker');
  assert.ok(notif, 'notifications-toast missing');
  assert.ok(picker, 'tool-subtype-picker missing');

  const notifOrder = parseInt(notif.canvas_sorting_order, 10);
  const pickerOrder = parseInt(picker.canvas_sorting_order, 10);

  assert.ok(
    notifOrder > pickerOrder,
    `notifications-toast sortingOrder (${notifOrder}) must be > tool-subtype-picker sortingOrder (${pickerOrder})`
  );
});

// ── T13.0.3 — audit passes with zero violations ───────────────────────────────

const CANVAS_LAYER_ORDER = ['HUD', 'SubViews', 'Modals', 'Notifications', 'Cursor'];

test('Notifications_SortingOrder_AboveSubtypePicker — canvas layering audit zero violations', () => {
  const content = readFileSync(PLAN_PATH, 'utf8');
  const panels = parsePlan(content);

  // Build layer map (max order per layer name).
  const layerMap = new Map();
  for (const p of panels) {
    if (p.canvas_sorting_layer && p.canvas_sorting_order !== undefined) {
      const order = parseInt(p.canvas_sorting_order, 10);
      if (!isNaN(order)) {
        const existing = layerMap.get(p.canvas_sorting_layer);
        if (existing === undefined || order > existing) {
          layerMap.set(p.canvas_sorting_layer, order);
        }
      }
    }
  }

  const inversions = [];
  for (let i = 0; i < CANVAS_LAYER_ORDER.length - 1; i++) {
    const lower = CANVAS_LAYER_ORDER[i];
    const higher = CANVAS_LAYER_ORDER[i + 1];
    if (!layerMap.has(lower) || !layerMap.has(higher)) continue;
    if (layerMap.get(higher) <= layerMap.get(lower)) {
      inversions.push(`${lower}(${layerMap.get(lower)}) >= ${higher}(${layerMap.get(higher)})`);
    }
  }

  assert.equal(
    inversions.length,
    0,
    `Canvas layering audit: ${inversions.length} inversion(s) found: ${inversions.join(', ')}`
  );
});

// ── T13.0.4 — B.7c bake baseline row exists ──────────────────────────────────

test('Notifications_SortingOrder_AboveSubtypePicker — Stage 13 bake baseline row exists in ia_ui_bake_history', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(`
      SELECT id, panel_slug, bake_handler_version, diff_summary
      FROM ia_ui_bake_history
      WHERE panel_slug = 'notifications-toast'
        AND bake_handler_version = 'stage13-canvas-layering-baseline'
      ORDER BY baked_at DESC
      LIMIT 1
    `);
    assert.equal(
      rows.length,
      1,
      'Stage 13.0 canvas layering baseline row missing from ia_ui_bake_history'
    );
    const summary = rows[0].diff_summary;
    assert.equal(summary.baseline_reset, true, 'diff_summary.baseline_reset must be true');
    assert.ok(
      summary.notifications_order > summary.subtype_picker_order,
      `baseline must record notifications_order(${summary.notifications_order}) > subtype_picker_order(${summary.subtype_picker_order})`
    );
    assert.equal(summary.audit_clean, true, 'diff_summary.audit_clean must be true');
  } finally {
    await client.end();
  }
});
