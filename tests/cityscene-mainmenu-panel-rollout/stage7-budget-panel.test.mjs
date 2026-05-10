/**
 * Stage 7.0 — budget-panel test suite.
 * Red state at T7.0.1 creation; green on stage close.
 * Node --test runner.
 */
import { test } from 'node:test';
import assert from 'node:assert/strict';
import pg from 'pg';

const { Client } = pg;

// Allowed panel_child kinds for budget-panel (T7.0.1 spec).
const ALLOWED_KINDS = new Set([
  'themed-label',
  'themed-button',
  'section-header',
  'slider-row-numeric',
  'expense-row',
  'readout-block',
  'chart',
  'range-tabs',
]);

async function dbClient() {
  const client = new Client({ connectionString: process.env.DATABASE_URL });
  await client.connect();
  return client;
}

// ── T7.0.1 — DB seed migration ────────────────────────────────────────────────

test('budget-panel catalog_entity exists with kind=panel', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(
      `SELECT slug, kind FROM catalog_entity WHERE slug = 'budget-panel'`
    );
    assert.equal(rows.length, 1, 'budget-panel row missing from catalog_entity');
    assert.equal(rows[0].kind, 'panel');
  } finally {
    await client.end();
  }
});

test('budget-panel panel_detail has layout_template=modal-card', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(
      `SELECT layout_template FROM panel_detail WHERE panel_slug = 'budget-panel'`
    );
    assert.equal(rows.length, 1, 'panel_detail row missing');
    assert.equal(rows[0].layout_template, 'modal-card');
  } finally {
    await client.end();
  }
});

test('budget-panel has exactly 25 panel_child rows', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(
      `SELECT count(*)::int AS cnt FROM panel_child WHERE panel_slug = 'budget-panel'`
    );
    assert.equal(rows[0].cnt, 25, `Expected 25 children, got ${rows[0].cnt}`);
  } finally {
    await client.end();
  }
});

test('every budget-panel child has params_json.kind in allowed kinds', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(
      `SELECT slug, params_json FROM panel_child WHERE panel_slug = 'budget-panel'`
    );
    for (const row of rows) {
      const pj = typeof row.params_json === 'string'
        ? JSON.parse(row.params_json)
        : row.params_json;
      assert.ok(
        ALLOWED_KINDS.has(pj?.kind),
        `Child '${row.slug}' has unmapped kind '${pj?.kind}'`
      );
    }
  } finally {
    await client.end();
  }
});

test('chart + range-tabs archetypes published from Wave B2 (T6.0.2)', async () => {
  const client = await dbClient();
  try {
    const PREREQ_ARCHETYPES = ['chart', 'range-tabs'];
    const { rows } = await client.query(
      `SELECT kind FROM catalog_archetype WHERE kind = ANY($1)`,
      [PREREQ_ARCHETYPES]
    );
    const found = new Set(rows.map(r => r.kind));
    for (const k of PREREQ_ARCHETYPES) {
      assert.ok(found.has(k), `Pre-req archetype '${k}' missing — Wave B2 migration not applied`);
    }
  } finally {
    await client.end();
  }
});

// ── T7.0.2 — 3 new archetype rows round-trip ──────────────────────────────────

test('3 budget archetype rows present in catalog_archetype', async () => {
  const client = await dbClient();
  try {
    const ARCHETYPE_KINDS = ['slider-row-numeric', 'expense-row', 'readout-block'];
    const { rows } = await client.query(
      `SELECT kind FROM catalog_archetype WHERE kind = ANY($1)`,
      [ARCHETYPE_KINDS]
    );
    const found = new Set(rows.map(r => r.kind));
    for (const k of ARCHETYPE_KINDS) {
      assert.ok(found.has(k), `Archetype kind '${k}' missing from catalog_archetype`);
    }
  } finally {
    await client.end();
  }
});

test('slider-row-numeric archetype has numeric=true in params_json', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(
      `SELECT params_json FROM catalog_archetype WHERE kind = 'slider-row-numeric'`
    );
    assert.equal(rows.length, 1, 'slider-row-numeric archetype missing');
    const pj = typeof rows[0].params_json === 'string'
      ? JSON.parse(rows[0].params_json)
      : rows[0].params_json;
    assert.equal(pj.numeric, true, 'slider-row-numeric missing numeric=true in params_json');
  } finally {
    await client.end();
  }
});

test('expense-row archetype has icon field in params_json', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(
      `SELECT params_json FROM catalog_archetype WHERE kind = 'expense-row'`
    );
    assert.equal(rows.length, 1, 'expense-row archetype missing');
    const pj = typeof rows[0].params_json === 'string'
      ? JSON.parse(rows[0].params_json)
      : rows[0].params_json;
    assert.ok('icon' in pj, 'expense-row missing icon field in params_json');
    assert.ok('bindId' in pj, 'expense-row missing bindId field in params_json');
  } finally {
    await client.end();
  }
});

test('readout-block archetype has deltaColorRule in params_json', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(
      `SELECT params_json FROM catalog_archetype WHERE kind = 'readout-block'`
    );
    assert.equal(rows.length, 1, 'readout-block archetype missing');
    const pj = typeof rows[0].params_json === 'string'
      ? JSON.parse(rows[0].params_json)
      : rows[0].params_json;
    assert.ok('deltaColorRule' in pj, 'readout-block missing deltaColorRule in params_json');
    assert.ok('bindId' in pj, 'readout-block missing bindId in params_json');
  } finally {
    await client.end();
  }
});

// ── T7.0.4 — HUD trigger wire ─────────────────────────────────────────────────

test('hud-bar-budget-button child has action=budget.open in params_json', async () => {
  const client = await dbClient();
  try {
    // hud-bar-budget-button is the illuminated-button child in hud-bar panel_child row.
    // After migration 0141, its action should be 'budget.open'.
    const { rows } = await client.query(`
      SELECT pc.params_json
      FROM panel_child pc
      JOIN catalog_entity ce_child ON ce_child.id = pc.child_entity_id
      JOIN catalog_entity ce_panel ON ce_panel.id = pc.panel_entity_id
      WHERE ce_panel.slug = 'hud-bar'
        AND ce_child.slug = 'hud-bar-budget-button'
      LIMIT 1
    `);
    assert.equal(rows.length, 1, 'hud-bar-budget-button panel_child row not found');
    const pj = typeof rows[0].params_json === 'string'
      ? JSON.parse(rows[0].params_json)
      : rows[0].params_json;
    const action = pj?.action ?? pj?.actionId;
    assert.equal(action, 'budget.open', `Expected action=budget.open, got '${action}'`);
  } finally {
    await client.end();
  }
});
