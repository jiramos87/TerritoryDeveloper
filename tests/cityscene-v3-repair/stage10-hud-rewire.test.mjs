/**
 * Stage 10.0 — HUD button rewire test suite (TECH-29751 → TECH-29754).
 * Red state at T10.0.1 creation; green on stage close.
 * Node --test runner.
 *
 * Red-Stage Proof anchor: HudButton_FiresDB_DeclaredTarget
 * Asserts every published HUD button's panel_child.params_json.action
 * matches button_detail.action_id (DB-canonical). Zero wrong-target
 * entries = drift-detector clean run.
 */
import { test } from 'node:test';
import assert from 'node:assert/strict';
import pg from 'pg';

const { Client } = pg;

const DB_URL = process.env.DATABASE_URL || 'postgresql://postgres:postgres@localhost:5434/territory_ia_dev';

async function dbClient() {
  const client = new Client({ connectionString: DB_URL });
  await client.connect();
  return client;
}

// ── T10.0.1 — corrected mapping artifact exists in DB ────────────────────────

test('HudButton_FiresDB_DeclaredTarget — panel_child action matches button_detail.action_id for all HUD buttons', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(`
      SELECT
        ce2.slug                         AS button_slug,
        bd.action_id                     AS db_action_id,
        pc.params_json->>'action'        AS child_action,
        bd.action_id = (pc.params_json->>'action') AS matches
      FROM panel_child pc
      JOIN catalog_entity ce  ON ce.id  = pc.panel_entity_id
      JOIN catalog_entity ce2 ON ce2.id = pc.child_entity_id
      LEFT JOIN button_detail bd ON bd.entity_id = ce2.id
      WHERE ce.slug = 'hud-bar'
        AND pc.params_json->>'kind' = 'illuminated-button'
        AND bd.action_id IS NOT NULL
      ORDER BY pc.order_idx
    `);

    assert.ok(rows.length > 0, 'Expected at least one HUD button row');

    const drifts = rows.filter(r => !r.matches);
    assert.equal(
      drifts.length,
      0,
      `wrong-target HUD buttons: ${drifts.map(d => `${d.button_slug} (child="${d.child_action}" db="${d.db_action_id}")`).join(', ')}`
    );
  } finally {
    await client.end();
  }
});

// ── T10.0.2 — hud-bar-budget-button uses DB-canonical action_id ──────────────

test('hud-bar-budget-button panel_child action equals action.budget-panel-toggle', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(`
      SELECT pc.params_json->>'action' AS child_action, bd.action_id AS db_action_id
      FROM panel_child pc
      JOIN catalog_entity ce  ON ce.id  = pc.panel_entity_id
      JOIN catalog_entity ce2 ON ce2.id = pc.child_entity_id
      JOIN button_detail bd ON bd.entity_id = ce2.id
      WHERE ce.slug = 'hud-bar' AND ce2.slug = 'hud-bar-budget-button'
    `);
    assert.equal(rows.length, 1, 'hud-bar-budget-button panel_child row missing');
    assert.equal(rows[0].child_action, 'action.budget-panel-toggle',
      `Expected action.budget-panel-toggle, got: ${rows[0].child_action}`);
    assert.equal(rows[0].db_action_id, 'action.budget-panel-toggle',
      `button_detail.action_id should be action.budget-panel-toggle`);
  } finally {
    await client.end();
  }
});

// ── T10.0.2 — all panel-toggle buttons match DB ──────────────────────────────

test('hud-bar-stats-button panel_child action equals action.stats-panel-toggle', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(`
      SELECT pc.params_json->>'action' AS child_action
      FROM panel_child pc
      JOIN catalog_entity ce  ON ce.id  = pc.panel_entity_id
      JOIN catalog_entity ce2 ON ce2.id = pc.child_entity_id
      WHERE ce.slug = 'hud-bar' AND ce2.slug = 'hud-bar-stats-button'
    `);
    assert.equal(rows.length, 1, 'hud-bar-stats-button panel_child row missing');
    assert.equal(rows[0].child_action, 'action.stats-panel-toggle',
      `Expected action.stats-panel-toggle, got: ${rows[0].child_action}`);
  } finally {
    await client.end();
  }
});

test('hud-bar-map-button panel_child action equals action.map-panel-toggle', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(`
      SELECT pc.params_json->>'action' AS child_action
      FROM panel_child pc
      JOIN catalog_entity ce  ON ce.id  = pc.panel_entity_id
      JOIN catalog_entity ce2 ON ce2.id = pc.child_entity_id
      WHERE ce.slug = 'hud-bar' AND ce2.slug = 'hud-bar-map-button'
    `);
    assert.equal(rows.length, 1, 'hud-bar-map-button panel_child row missing');
    assert.equal(rows[0].child_action, 'action.map-panel-toggle',
      `Expected action.map-panel-toggle, got: ${rows[0].child_action}`);
  } finally {
    await client.end();
  }
});

// ── T10.0.3 — zero wrong-target entries (drift-detector clean) ───────────────

test('zero wrong-target entries across all hud-bar illuminated-button children', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(`
      SELECT
        ce2.slug                         AS button_slug,
        bd.action_id                     AS db_action_id,
        pc.params_json->>'action'        AS child_action
      FROM panel_child pc
      JOIN catalog_entity ce  ON ce.id  = pc.panel_entity_id
      JOIN catalog_entity ce2 ON ce2.id = pc.child_entity_id
      LEFT JOIN button_detail bd ON bd.entity_id = ce2.id
      WHERE ce.slug = 'hud-bar'
        AND pc.params_json->>'kind' = 'illuminated-button'
        AND bd.action_id IS NOT NULL
        AND bd.action_id <> (pc.params_json->>'action')
    `);
    assert.equal(
      rows.length,
      0,
      `Expected 0 wrong-target entries, found ${rows.length}: ${JSON.stringify(rows)}`
    );
  } finally {
    await client.end();
  }
});

// ── T10.0.4 — published HUD button count sanity check ────────────────────────

test('hud-bar has at least 10 published illuminated-button children', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(`
      SELECT count(*)::int AS cnt
      FROM panel_child pc
      JOIN catalog_entity ce  ON ce.id  = pc.panel_entity_id
      WHERE ce.slug = 'hud-bar'
        AND pc.params_json->>'kind' = 'illuminated-button'
    `);
    assert.ok(rows[0].cnt >= 10,
      `Expected ≥10 illuminated-button children in hud-bar, got ${rows[0].cnt}`);
  } finally {
    await client.end();
  }
});
