/**
 * Stage 6.0 — stats-panel test suite.
 * Red state at T6.0.1 creation; green on stage close.
 * Node --test runner.
 */
import { test } from 'node:test';
import assert from 'node:assert/strict';
import pg from 'pg';

const { Client } = pg;

// Allowed panel_child kinds for stats-panel (T6.0.1 spec).
const ALLOWED_KINDS = new Set([
  'themed-label',
  'themed-button',
  'tab-strip',
  'range-tabs',
  'chart',
  'stacked-bar-row',
  'service-row',
]);

async function dbClient() {
  const client = new Client({ connectionString: process.env.DATABASE_URL });
  await client.connect();
  return client;
}

// ── T6.0.1 — DB seed migration ────────────────────────────────────────────

test('stats-panel catalog_entity exists with kind=panel', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(
      `SELECT slug, kind FROM catalog_entity WHERE slug = 'stats-panel'`
    );
    assert.equal(rows.length, 1, 'stats-panel row missing from catalog_entity');
    assert.equal(rows[0].kind, 'panel');
  } finally {
    await client.end();
  }
});

test('stats-panel panel_detail has layout_template=modal-card', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(
      `SELECT layout_template FROM panel_detail WHERE panel_slug = 'stats-panel'`
    );
    assert.equal(rows.length, 1, 'panel_detail row missing');
    assert.equal(rows[0].layout_template, 'modal-card');
  } finally {
    await client.end();
  }
});

test('stats-panel has exactly 21 panel_child rows', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(
      `SELECT count(*)::int AS cnt FROM panel_child WHERE panel_slug = 'stats-panel'`
    );
    assert.equal(rows[0].cnt, 21, `Expected 21 children, got ${rows[0].cnt}`);
  } finally {
    await client.end();
  }
});

test('every stats-panel child has params_json.kind in allowed kinds', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(
      `SELECT slug, params_json FROM panel_child WHERE panel_slug = 'stats-panel'`
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

// ── T6.0.2 — 5 new archetypes round-trip ──────────────────────────────────

test('5 stats archetype rows present in catalog_archetype', async () => {
  const client = await dbClient();
  try {
    const ARCHETYPE_KINDS = ['tab-strip', 'chart', 'range-tabs', 'stacked-bar-row', 'service-row'];
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
