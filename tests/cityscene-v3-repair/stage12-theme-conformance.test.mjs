/**
 * Stage 12.0 — Theme conformance test suite (TECH-29759 → TECH-29762).
 * Red state at T12.0.1 creation; green on stage close.
 * Node --test runner.
 *
 * Red-Stage Proof anchor: PauseMenu_TokensResolve_MainMenuAligned
 * Asserts pause-menu params_json bg_color_token resolves to catalog_entity token,
 * title child size_token resolves to catalog_entity token,
 * and bake history baseline row exists. B.7c visual diff = DB Layer 1 gate passes.
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

// ── T12.0.2 — pause-menu bg_color_token resolves in catalog_entity ────────────

test('PauseMenu_TokensResolve_MainMenuAligned — bg_color_token token-color-bg-menu resolves in catalog_entity', async () => {
  const client = await dbClient();
  try {
    // Extract bg_color_token value from panel_detail params_json
    const { rows: pdRows } = await client.query(`
      SELECT pd.params_json->>'bg_color_token' AS bg_token
      FROM panel_detail pd
      JOIN catalog_entity ce ON ce.id = pd.entity_id
      WHERE ce.slug = 'pause-menu'
    `);
    assert.equal(pdRows.length, 1, 'pause-menu panel_detail row missing');
    const tokenRef = pdRows[0].bg_token;
    assert.ok(tokenRef, 'bg_color_token must be set in pause-menu params_json');
    assert.equal(tokenRef, 'token-color-bg-menu', `Expected token-color-bg-menu, got: ${tokenRef}`);

    // Strip token- prefix to get slug
    const tokenSlug = tokenRef.replace(/^token-/, '');
    const { rows: tokenRows } = await client.query(
      `SELECT slug FROM catalog_entity WHERE kind = 'token' AND slug = $1 AND retired_at IS NULL`,
      [tokenSlug]
    );
    assert.equal(
      tokenRows.length,
      1,
      `token '${tokenSlug}' not found in catalog_entity (kind=token) — Layer 1 gate fail`
    );
  } finally {
    await client.end();
  }
});

// ── T12.0.2 — pause-menu title child size_token resolves in catalog_entity ───

test('PauseMenu_TokensResolve_MainMenuAligned — title size_token token-size-text-modal-title resolves in catalog_entity', async () => {
  const client = await dbClient();
  try {
    const { rows: childRows } = await client.query(`
      SELECT pc.params_json->>'size_token' AS size_token
      FROM panel_child pc
      JOIN catalog_entity ce ON ce.id = pc.panel_entity_id
      WHERE ce.slug = 'pause-menu'
        AND pc.slot_name = 'title'
        AND pc.child_kind = 'label'
    `);
    assert.equal(childRows.length, 1, 'pause-menu title label child missing');
    const tokenRef = childRows[0].size_token;
    assert.equal(
      tokenRef,
      'token-size-text-modal-title',
      `Expected token-size-text-modal-title, got: ${tokenRef}`
    );

    // Resolve in catalog_entity
    const tokenSlug = tokenRef.replace(/^token-/, '');
    const { rows: tokenRows } = await client.query(
      `SELECT slug, td.token_kind, td.value_json
       FROM catalog_entity ce
       JOIN token_detail td ON td.entity_id = ce.id
       WHERE ce.kind = 'token' AND ce.slug = $1 AND ce.retired_at IS NULL`,
      [tokenSlug]
    );
    assert.equal(
      tokenRows.length,
      1,
      `token '${tokenSlug}' not found in catalog_entity — Layer 1 gate fail`
    );
    assert.equal(tokenRows[0].token_kind, 'type-scale', `Expected type-scale token, got: ${tokenRows[0].token_kind}`);
  } finally {
    await client.end();
  }
});

// ── T12.0.2 — Layer 1 gate: all token-* refs in pause-menu params_json resolve ─

test('PauseMenu_TokensResolve_MainMenuAligned — Layer 1 gate: all token-* refs resolve', async () => {
  const client = await dbClient();
  try {
    // Collect all token-* refs from panel_detail + panel_child params_json
    const { rows: pdRows } = await client.query(`
      SELECT pd.params_json
      FROM panel_detail pd
      JOIN catalog_entity ce ON ce.id = pd.entity_id
      WHERE ce.slug = 'pause-menu'
    `);
    const { rows: childRows } = await client.query(`
      SELECT pc.params_json
      FROM panel_child pc
      JOIN catalog_entity ce ON ce.id = pc.panel_entity_id
      WHERE ce.slug = 'pause-menu'
    `);

    const TOKEN_REF_RE = /token-[a-z0-9_-]+/g;
    function extractTokenRefs(obj) {
      const str = JSON.stringify(obj);
      return [...(str.match(TOKEN_REF_RE) ?? [])];
    }

    const allRefs = new Set([
      ...pdRows.flatMap(r => extractTokenRefs(r.params_json)),
      ...childRows.flatMap(r => extractTokenRefs(r.params_json)),
    ]);

    const dangling = [];
    for (const tokenId of allRefs) {
      const tokenSlug = tokenId.replace(/^token-/, '');
      const { rows } = await client.query(
        `SELECT slug FROM catalog_entity WHERE kind = 'token' AND (slug = $1 OR slug = $2) AND retired_at IS NULL LIMIT 1`,
        [tokenId, tokenSlug]
      );
      if (rows.length === 0) {
        dangling.push(tokenId);
      }
    }

    assert.equal(
      dangling.length,
      0,
      `Dangling token refs in pause-menu: ${dangling.join(', ')}`
    );
  } finally {
    await client.end();
  }
});

// ── T12.0.2 — pause-menu params_json has bg_color_token matching main-menu ───

test('PauseMenu_TokensResolve_MainMenuAligned — pause-menu bg_color_token matches main-menu bg_color_token', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(`
      SELECT ce.slug, pd.params_json->>'bg_color_token' AS bg_token
      FROM panel_detail pd
      JOIN catalog_entity ce ON ce.id = pd.entity_id
      WHERE ce.slug IN ('pause-menu', 'main-menu')
      ORDER BY ce.slug
    `);
    const bySlug = Object.fromEntries(rows.map(r => [r.slug, r.bg_token]));
    assert.ok(bySlug['main-menu'], 'main-menu bg_color_token must be set');
    assert.equal(
      bySlug['pause-menu'],
      bySlug['main-menu'],
      `pause-menu bg_color_token (${bySlug['pause-menu']}) must match main-menu (${bySlug['main-menu']})`
    );
  } finally {
    await client.end();
  }
});

// ── T12.0.3 — bake history baseline row exists (B.7c visual diff reset) ──────

test('PauseMenu_TokensResolve_MainMenuAligned — Stage 12 bake baseline row exists in ia_ui_bake_history', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(`
      SELECT id, panel_slug, bake_handler_version, diff_summary
      FROM ia_ui_bake_history
      WHERE panel_slug = 'pause-menu'
        AND bake_handler_version = 'stage12-token-align-baseline'
      ORDER BY baked_at DESC
      LIMIT 1
    `);
    assert.equal(
      rows.length,
      1,
      'Stage 12.0 bake baseline row for pause-menu missing from ia_ui_bake_history'
    );
    const summary = rows[0].diff_summary;
    assert.equal(summary.baseline_reset, true, 'diff_summary.baseline_reset must be true');
  } finally {
    await client.end();
  }
});

// ── T12.0.3 — ia_bake_diffs has expected change rows for stage 12 ────────────

test('PauseMenu_TokensResolve_MainMenuAligned — ia_bake_diffs has 3 change rows for stage 12 baseline', async () => {
  const client = await dbClient();
  try {
    const { rows: histRows } = await client.query(`
      SELECT id FROM ia_ui_bake_history
      WHERE panel_slug = 'pause-menu'
        AND bake_handler_version = 'stage12-token-align-baseline'
      ORDER BY baked_at DESC LIMIT 1
    `);
    assert.equal(histRows.length, 1, 'Bake history row missing');
    const histId = histRows[0].id;

    const { rows: diffRows } = await client.query(
      `SELECT change_kind, child_kind, slug FROM ia_bake_diffs WHERE history_id = $1 ORDER BY id`,
      [histId]
    );
    assert.equal(diffRows.length, 3, `Expected 3 bake diff rows, got ${diffRows.length}`);

    const kinds = new Set(diffRows.map(r => r.change_kind));
    assert.ok(kinds.has('added'),    'Expected at least one "added" diff row');
    assert.ok(kinds.has('modified'), 'Expected at least one "modified" diff row');
  } finally {
    await client.end();
  }
});

// ── T12.0.1 — size-text-modal-title token entity seeded correctly ─────────────

test('PauseMenu_TokensResolve_MainMenuAligned — size-text-modal-title token has type-scale kind and correct pt value', async () => {
  const client = await dbClient();
  try {
    const { rows } = await client.query(`
      SELECT ce.slug, td.token_kind, td.value_json
      FROM catalog_entity ce
      JOIN token_detail td ON td.entity_id = ce.id
      WHERE ce.slug = 'size-text-modal-title' AND ce.kind = 'token'
    `);
    assert.equal(rows.length, 1, 'size-text-modal-title token missing');
    assert.equal(rows[0].token_kind, 'type-scale');
    const val = rows[0].value_json;
    assert.equal(val.pt, 24, `Expected pt=24, got ${val.pt}`);
    assert.equal(val.weight, 'bold', `Expected weight=bold, got ${val.weight}`);
  } finally {
    await client.end();
  }
});
