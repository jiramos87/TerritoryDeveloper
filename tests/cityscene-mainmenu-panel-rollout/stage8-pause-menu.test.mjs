/**
 * Stage 8.0 — pause-menu test suite.
 * Red state at T8.0.1 creation; green on stage close.
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

// ── T8.0.1 — DB seed migration ────────────────────────────────────────────────

test('pause-menu catalog_entity exists with kind=panel', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(
      `SELECT slug, kind FROM catalog_entity WHERE slug = 'pause-menu'`
    );
    assert.equal(rows.length, 1, 'pause-menu row missing from catalog_entity');
    assert.equal(rows[0].kind, 'panel');
  } finally {
    await client.end();
  }
});

test('pause-menu panel_detail has layout_template=modal-card and modal=true', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(`
      SELECT pd.layout_template, pd.modal
      FROM panel_detail pd
      JOIN catalog_entity ce ON ce.id = pd.entity_id
      WHERE ce.slug = 'pause-menu' AND ce.kind = 'panel'
    `);
    assert.equal(rows.length, 1, 'panel_detail row missing');
    assert.equal(rows[0].layout_template, 'modal-card', 'layout_template mismatch');
    assert.equal(rows[0].modal, true, 'modal flag should be true');
  } finally {
    await client.end();
  }
});

test('pause-menu has exactly 7 panel_child rows', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(`
      SELECT count(*)::int AS cnt
      FROM panel_child pc
      JOIN catalog_entity ce ON ce.id = pc.panel_entity_id
      WHERE ce.slug = 'pause-menu'
    `);
    assert.equal(rows[0].cnt, 7, `Expected 7 children, got ${rows[0].cnt}`);
  } finally {
    await client.end();
  }
});

test('pause-menu has exactly 1 title label', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(`
      SELECT count(*)::int AS cnt
      FROM panel_child pc
      JOIN catalog_entity ce ON ce.id = pc.panel_entity_id
      WHERE ce.slug = 'pause-menu' AND pc.child_kind = 'label'
    `);
    assert.equal(rows[0].cnt, 1, `Expected 1 label, got ${rows[0].cnt}`);
  } finally {
    await client.end();
  }
});

test('pause-menu has exactly 4 buttons and 2 confirm-buttons', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(`
      SELECT pc.child_kind, count(*)::int AS cnt
      FROM panel_child pc
      JOIN catalog_entity ce ON ce.id = pc.panel_entity_id
      WHERE ce.slug = 'pause-menu'
      GROUP BY pc.child_kind
      ORDER BY pc.child_kind
    `);
    const byKind = Object.fromEntries(rows.map(r => [r.child_kind, r.cnt]));
    assert.equal(byKind['button'] ?? 0, 4, `Expected 4 buttons, got ${byKind['button'] ?? 0}`);
    assert.equal(byKind['confirm-button'] ?? 0, 2, `Expected 2 confirm-buttons, got ${byKind['confirm-button'] ?? 0}`);
  } finally {
    await client.end();
  }
});

test('every pause-menu button child has params_json.action set', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(`
      SELECT pc.instance_slug, pc.params_json
      FROM panel_child pc
      JOIN catalog_entity ce ON ce.id = pc.panel_entity_id
      WHERE ce.slug = 'pause-menu'
        AND pc.child_kind IN ('button', 'confirm-button')
    `);
    for (const row of rows) {
      const pj = typeof row.params_json === 'string'
        ? JSON.parse(row.params_json)
        : row.params_json;
      assert.ok(pj.action, `${row.instance_slug} missing params_json.action`);
    }
  } finally {
    await client.end();
  }
});

// ── T8.0.2 — modal-card layout_template constraint ────────────────────────────

test('panel_detail layout_template CHECK constraint includes modal-card', async () => {
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
        AND cc.check_clause LIKE '%modal-card%'
    `);
    assert.equal(rows.length >= 1, true, 'modal-card not found in panel_detail layout_template CHECK');
  } finally {
    await client.end();
  }
});

// ── T8.0.1 instance_slug validation ──────────────────────────────────────────

test('pause-menu-resume-button exists in panel_child', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(
      `SELECT instance_slug FROM panel_child WHERE instance_slug = 'pause-menu-resume-button'`
    );
    assert.equal(rows.length, 1, 'pause-menu-resume-button missing');
  } finally {
    await client.end();
  }
});

test('pause-menu-main-menu-button and pause-menu-quit-button are confirm-buttons', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(`
      SELECT instance_slug, child_kind
      FROM panel_child
      WHERE instance_slug IN ('pause-menu-main-menu-button', 'pause-menu-quit-button')
    `);
    assert.equal(rows.length, 2, 'Expected 2 confirm-button rows');
    for (const row of rows) {
      assert.equal(row.child_kind, 'confirm-button', `${row.instance_slug} should be confirm-button`);
    }
  } finally {
    await client.end();
  }
});
