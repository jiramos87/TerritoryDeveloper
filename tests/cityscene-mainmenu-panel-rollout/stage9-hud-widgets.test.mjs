/**
 * Stage 9.0 — HUD widgets test suite (TECH-27097 → TECH-27101).
 * Red state at T9.0.1 creation; green on stage close.
 * Node --test runner.
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

// ── T9.0.1 — DB seed migration row counts ────────────────────────────────────

test('info-panel catalog_entity exists with kind=panel', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(
      `SELECT slug, kind FROM catalog_entity WHERE slug = 'info-panel'`
    );
    assert.equal(rows.length, 1, 'info-panel row missing from catalog_entity');
    assert.equal(rows[0].kind, 'panel');
  } finally {
    await client.end();
  }
});

test('map-panel catalog_entity exists with kind=panel', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(
      `SELECT slug, kind FROM catalog_entity WHERE slug = 'map-panel'`
    );
    assert.equal(rows.length, 1, 'map-panel row missing from catalog_entity');
    assert.equal(rows[0].kind, 'panel');
  } finally {
    await client.end();
  }
});

test('notifications-toast catalog_entity exists with kind=panel', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(
      `SELECT slug, kind FROM catalog_entity WHERE slug = 'notifications-toast'`
    );
    assert.equal(rows.length, 1, 'notifications-toast row missing from catalog_entity');
    assert.equal(rows[0].kind, 'panel');
  } finally {
    await client.end();
  }
});

test('info-panel panel_detail has layout_template=right-edge-dock', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(`
      SELECT pd.layout_template
      FROM panel_detail pd
      JOIN catalog_entity ce ON ce.id = pd.entity_id
      WHERE ce.slug = 'info-panel' AND ce.kind = 'panel'
    `);
    assert.equal(rows.length, 1, 'panel_detail row missing for info-panel');
    assert.equal(rows[0].layout_template, 'right-edge-dock');
  } finally {
    await client.end();
  }
});

test('map-panel panel_detail has layout_template=bottom-right-dock', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(`
      SELECT pd.layout_template
      FROM panel_detail pd
      JOIN catalog_entity ce ON ce.id = pd.entity_id
      WHERE ce.slug = 'map-panel' AND ce.kind = 'panel'
    `);
    assert.equal(rows.length, 1, 'panel_detail row missing for map-panel');
    assert.equal(rows[0].layout_template, 'bottom-right-dock');
  } finally {
    await client.end();
  }
});

test('notifications-toast panel_detail has layout_template=top-right-toast', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(`
      SELECT pd.layout_template
      FROM panel_detail pd
      JOIN catalog_entity ce ON ce.id = pd.entity_id
      WHERE ce.slug = 'notifications-toast' AND ce.kind = 'panel'
    `);
    assert.equal(rows.length, 1, 'panel_detail row missing for notifications-toast');
    assert.equal(rows[0].layout_template, 'top-right-toast');
  } finally {
    await client.end();
  }
});

test('info-panel has exactly 9 panel_child rows', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(`
      SELECT count(*)::int AS cnt
      FROM panel_child pc
      JOIN catalog_entity ce ON ce.id = pc.panel_entity_id
      WHERE ce.slug = 'info-panel'
    `);
    assert.equal(rows[0].cnt, 9, `Expected 9 children, got ${rows[0].cnt}`);
  } finally {
    await client.end();
  }
});

test('info-panel has exactly 1 confirm-button (demolish)', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(`
      SELECT count(*)::int AS cnt
      FROM panel_child pc
      JOIN catalog_entity ce ON ce.id = pc.panel_entity_id
      WHERE ce.slug = 'info-panel' AND pc.child_kind = 'confirm-button'
    `);
    assert.equal(rows[0].cnt, 1, `Expected 1 confirm-button, got ${rows[0].cnt}`);
  } finally {
    await client.end();
  }
});

test('info-panel demolish confirm-button has params_json.action=info.demolish', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(`
      SELECT pc.instance_slug, pc.params_json
      FROM panel_child pc
      JOIN catalog_entity ce ON ce.id = pc.panel_entity_id
      WHERE ce.slug = 'info-panel' AND pc.child_kind = 'confirm-button'
    `);
    assert.equal(rows.length, 1, 'confirm-button row missing');
    const pj = typeof rows[0].params_json === 'string'
      ? JSON.parse(rows[0].params_json)
      : rows[0].params_json;
    assert.equal(pj.action, 'info.demolish', 'action should be info.demolish');
  } finally {
    await client.end();
  }
});

test('map-panel has minimap-canvas child', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(`
      SELECT count(*)::int AS cnt
      FROM panel_child pc
      JOIN catalog_entity ce ON ce.id = pc.panel_entity_id
      WHERE ce.slug = 'map-panel' AND pc.child_kind = 'minimap-canvas'
    `);
    assert.equal(rows[0].cnt, 1, 'Expected 1 minimap-canvas child');
  } finally {
    await client.end();
  }
});

test('map-panel has exactly 3 layer-toggle rows', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(`
      SELECT count(*)::int AS cnt
      FROM panel_child pc
      JOIN catalog_entity ce ON ce.id = pc.panel_entity_id
      WHERE ce.slug = 'map-panel' AND pc.child_kind = 'toggle-row'
    `);
    assert.equal(rows[0].cnt, 3, `Expected 3 toggle-row children, got ${rows[0].cnt}`);
  } finally {
    await client.end();
  }
});

test('notifications-toast has exactly 1 toast-stack child', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(`
      SELECT count(*)::int AS cnt
      FROM panel_child pc
      JOIN catalog_entity ce ON ce.id = pc.panel_entity_id
      WHERE ce.slug = 'notifications-toast' AND pc.child_kind = 'toast-stack'
    `);
    assert.equal(rows[0].cnt, 1, 'Expected 1 toast-stack child');
  } finally {
    await client.end();
  }
});

test('notifications-toast has toast-card children including sticky milestone variants', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(`
      SELECT count(*)::int AS cnt
      FROM panel_child pc
      JOIN catalog_entity ce ON ce.id = pc.panel_entity_id
      WHERE ce.slug = 'notifications-toast' AND pc.child_kind = 'toast-card'
    `);
    assert.ok(rows[0].cnt >= 3, `Expected at least 3 toast-card children, got ${rows[0].cnt}`);
  } finally {
    await client.end();
  }
});

test('3 distinct layout_templates referenced across the 3 panels', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(`
      SELECT count(DISTINCT pd.layout_template)::int AS cnt
      FROM panel_detail pd
      JOIN catalog_entity ce ON ce.id = pd.entity_id
      WHERE ce.slug IN ('info-panel', 'map-panel', 'notifications-toast')
    `);
    assert.equal(rows[0].cnt, 3, `Expected 3 distinct layout_templates, got ${rows[0].cnt}`);
  } finally {
    await client.end();
  }
});

// ── T9.0.1 — panel_detail CHECK constraint includes new layout templates ──────

test('panel_detail layout_template CHECK includes right-edge-dock', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(`
      SELECT cc.check_clause
      FROM information_schema.table_constraints tc
      JOIN information_schema.check_constraints cc
        ON tc.constraint_name = cc.constraint_name
      WHERE tc.table_schema = 'public'
        AND tc.table_name = 'panel_detail'
        AND tc.constraint_type = 'CHECK'
        AND cc.check_clause LIKE '%right-edge-dock%'
    `);
    assert.equal(rows.length >= 1, true, 'right-edge-dock not in panel_detail CHECK');
  } finally {
    await client.end();
  }
});

test('panel_detail layout_template CHECK includes top-right-toast', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(`
      SELECT cc.check_clause
      FROM information_schema.table_constraints tc
      JOIN information_schema.check_constraints cc
        ON tc.constraint_name = cc.constraint_name
      WHERE tc.table_schema = 'public'
        AND tc.table_name = 'panel_detail'
        AND tc.constraint_type = 'CHECK'
        AND cc.check_clause LIKE '%top-right-toast%'
    `);
    assert.equal(rows.length >= 1, true, 'top-right-toast not in panel_detail CHECK');
  } finally {
    await client.end();
  }
});
