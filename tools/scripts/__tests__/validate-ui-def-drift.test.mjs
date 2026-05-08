/**
 * validate-ui-def-drift.test.mjs
 * Unit tests for validate-ui-def-drift.mjs.
 * Tests: HudBar_GreenBaseline_ExitsZero, DriftScan_FlagsRectJsonMismatch,
 *        SnapshotMissing_ExitsOneFatal, ExtraFieldInDb_DetectedAsDrift,
 *        TokenDriftAndComponentDriftDetected
 */

import assert from 'node:assert';
import { test } from 'node:test';
import { spawnSync } from 'node:child_process';
import { createRequire } from 'node:module';
import { existsSync, renameSync, readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join, resolve } from 'node:path';

const require = createRequire(import.meta.url);
const __dirname = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = resolve(__dirname, '..', '..', '..');
const SCRIPT_PATH = join(REPO_ROOT, 'tools', 'scripts', 'validate-ui-def-drift.mjs');
const SNAPSHOT_PATH = join(REPO_ROOT, 'Assets', 'UI', 'Snapshots', 'panels.json');
const TOKENS_SNAPSHOT_PATH = join(REPO_ROOT, 'Assets', 'UI', 'Snapshots', 'tokens.json');
const COMPONENTS_SNAPSHOT_PATH = join(REPO_ROOT, 'Assets', 'UI', 'Snapshots', 'components.json');

/** Resolve DATABASE_URL for live-DB tests. */
async function getDbUrl() {
  const { resolveDatabaseUrl } = await import('../../postgres-ia/resolve-database-url.mjs');
  return resolveDatabaseUrl(REPO_ROOT);
}

/** Spawn the validator and return { status, stdout, stderr }. */
function runValidator(env = {}) {
  const result = spawnSync(process.execPath, [SCRIPT_PATH], {
    encoding: 'utf8',
    env: { ...process.env, ...env },
    timeout: 15_000,
  });
  return {
    status: result.status,
    stdout: result.stdout ?? '',
    stderr: result.stderr ?? '',
  };
}

// ─── Test 1 ──────────────────────────────────────────────────────────────────

test('HudBar_GreenBaseline_ExitsZero', async () => {
  const dbUrl = await getDbUrl();
  if (!dbUrl) {
    // CI without DB — validator exits 0 with info line; acceptable
    const r = runValidator();
    assert.strictEqual(r.status, 0, `expected exit 0, got ${r.status}\nstdout: ${r.stdout}\nstderr: ${r.stderr}`);
    return;
  }

  const r = runValidator();
  assert.strictEqual(
    r.status,
    0,
    `expected exit 0 on green baseline\nstdout: ${r.stdout}\nstderr: ${r.stderr}`
  );
});

// ─── Test 2 ──────────────────────────────────────────────────────────────────

test('DriftScan_FlagsRectJsonMismatch', async () => {
  const dbUrl = await getDbUrl();
  if (!dbUrl) {
    // Skip live-DB mutation test in CI
    return;
  }

  const pg = require('pg');
  const Pool = pg.Pool ?? pg.default?.Pool;
  const pool = new Pool({ connectionString: dbUrl });

  // Mutate: set rect_json h equivalent — update size_delta to inject drift
  // We patch by overwriting rect_json with a modified version
  let originalRect;
  try {
    const { rows } = await pool.query(`
      SELECT pd.rect_json
      FROM panel_detail pd
      JOIN catalog_entity ce ON ce.id = pd.entity_id
      WHERE ce.slug = 'hud-bar' AND ce.kind = 'panel'
    `);
    if (!rows.length) {
      await pool.end();
      // hud-bar not seeded — skip
      return;
    }
    originalRect = rows[0].rect_json;

    // Inject drift: change size_delta[1] from 144 to 88
    const mutated = typeof originalRect === 'string'
      ? JSON.parse(originalRect)
      : { ...originalRect };
    if (Array.isArray(mutated.size_delta)) {
      mutated.size_delta = [mutated.size_delta[0], 88];
    }

    await pool.query(`
      UPDATE panel_detail pd
      SET rect_json = $1::jsonb
      FROM catalog_entity ce
      WHERE ce.id = pd.entity_id AND ce.slug = 'hud-bar' AND ce.kind = 'panel'
    `, [JSON.stringify(mutated)]);

    const r = runValidator();
    assert.strictEqual(r.status, 1, `expected exit 1 on drift\nstdout: ${r.stdout}\nstderr: ${r.stderr}`);
    assert.ok(
      r.stdout.includes('hud-bar'),
      `expected stdout to contain 'hud-bar'\nstdout: ${r.stdout}`
    );
  } finally {
    // Revert
    if (originalRect !== undefined) {
      await pool.query(`
        UPDATE panel_detail pd
        SET rect_json = $1::jsonb
        FROM catalog_entity ce
        WHERE ce.id = pd.entity_id AND ce.slug = 'hud-bar' AND ce.kind = 'panel'
      `, [typeof originalRect === 'string' ? originalRect : JSON.stringify(originalRect)]);
    }
    await pool.end().catch(() => {});
  }
});

// ─── Test 3 ──────────────────────────────────────────────────────────────────

test('SnapshotMissing_ExitsOneFatal', async () => {
  if (!existsSync(SNAPSHOT_PATH)) {
    // Already missing — just run
    const r = runValidator();
    assert.strictEqual(r.status, 1, `expected exit 1 when snapshot missing`);
    assert.ok(
      r.stderr.includes('snapshots/panels.json missing'),
      `expected fatal message\nstderr: ${r.stderr}`
    );
    return;
  }

  const hiddenPath = SNAPSHOT_PATH + '.bak_drift_test';
  try {
    renameSync(SNAPSHOT_PATH, hiddenPath);
    const r = runValidator();
    assert.strictEqual(r.status, 1, `expected exit 1 when snapshot missing\nstdout: ${r.stdout}\nstderr: ${r.stderr}`);
    assert.ok(
      r.stderr.includes('snapshots/panels.json missing'),
      `expected fatal message in stderr\nstderr: ${r.stderr}`
    );
  } finally {
    if (existsSync(hiddenPath)) renameSync(hiddenPath, SNAPSHOT_PATH);
  }
});

// ─── Test 4 ──────────────────────────────────────────────────────────────────

test('ExtraFieldInDb_DetectedAsDrift', async () => {
  const dbUrl = await getDbUrl();
  if (!dbUrl) {
    return;
  }

  const pg = require('pg');
  const Pool = pg.Pool ?? pg.default?.Pool;
  const pool = new Pool({ connectionString: dbUrl });

  let originalRect;
  try {
    const { rows } = await pool.query(`
      SELECT pd.rect_json
      FROM panel_detail pd
      JOIN catalog_entity ce ON ce.id = pd.entity_id
      WHERE ce.slug = 'hud-bar' AND ce.kind = 'panel'
    `);
    if (!rows.length) {
      await pool.end();
      return;
    }
    originalRect = rows[0].rect_json;

    // Inject extra field not present in snapshot
    const mutated = typeof originalRect === 'string'
      ? JSON.parse(originalRect)
      : { ...originalRect };
    mutated.__extra_test_field__ = 'drift-injected';

    await pool.query(`
      UPDATE panel_detail pd
      SET rect_json = $1::jsonb
      FROM catalog_entity ce
      WHERE ce.id = pd.entity_id AND ce.slug = 'hud-bar' AND ce.kind = 'panel'
    `, [JSON.stringify(mutated)]);

    const r = runValidator();
    assert.strictEqual(r.status, 1, `expected exit 1 on extra field drift\nstdout: ${r.stdout}\nstderr: ${r.stderr}`);
  } finally {
    if (originalRect !== undefined) {
      await pool.query(`
        UPDATE panel_detail pd
        SET rect_json = $1::jsonb
        FROM catalog_entity ce
        WHERE ce.id = pd.entity_id AND ce.slug = 'hud-bar' AND ce.kind = 'panel'
      `, [typeof originalRect === 'string' ? originalRect : JSON.stringify(originalRect)]);
    }
    await pool.end().catch(() => {});
  }
});

// ─── Test 5 ──────────────────────────────────────────────────────────────────

test('TokenDriftAndComponentDriftDetected', async () => {
  const dbUrl = await getDbUrl();
  if (!dbUrl) {
    // CI without DB — skip
    return;
  }

  if (!existsSync(TOKENS_SNAPSHOT_PATH) || !existsSync(COMPONENTS_SNAPSHOT_PATH)) {
    // Snapshots not yet emitted — skip
    return;
  }

  const pg = require('pg');
  const Pool = pg.Pool ?? pg.default?.Pool;
  const pool = new Pool({ connectionString: dbUrl });

  // ── Token drift ──────────────────────────────────────────────────────────
  let originalTokenValue;
  let tokenEntityId;
  try {
    const { rows } = await pool.query(`
      SELECT ce.id, td.value_json
      FROM token_detail td
      JOIN catalog_entity ce ON ce.id = td.entity_id
      WHERE ce.kind = 'token' AND ce.slug = 'color-bg-cream'
    `);
    if (!rows.length) {
      await pool.end();
      return;
    }
    tokenEntityId = rows[0].id;
    originalTokenValue = rows[0].value_json;

    // Inject drift: change the color value
    await pool.query(
      `UPDATE token_detail SET value_json = $1::jsonb WHERE entity_id = $2`,
      [JSON.stringify({ value: '#000000' }), tokenEntityId]
    );

    const r = runValidator();
    assert.strictEqual(r.status, 1, `expected exit 1 on token drift\nstdout: ${r.stdout}\nstderr: ${r.stderr}`);
    assert.ok(
      r.stdout.includes('color-bg-cream'),
      `expected stdout to contain 'color-bg-cream'\nstdout: ${r.stdout}`
    );
  } finally {
    if (originalTokenValue !== undefined && tokenEntityId !== undefined) {
      await pool.query(
        `UPDATE token_detail SET value_json = $1::jsonb WHERE entity_id = $2`,
        [typeof originalTokenValue === 'string' ? originalTokenValue : JSON.stringify(originalTokenValue), tokenEntityId]
      );
    }
    await pool.end().catch(() => {});
  }

  // ── Component drift ──────────────────────────────────────────────────────
  const pool2 = new Pool({ connectionString: dbUrl });
  let originalProps;
  let componentEntityId;
  try {
    const { rows } = await pool2.query(`
      SELECT ce.id, cd.default_props_json
      FROM component_detail cd
      JOIN catalog_entity ce ON ce.id = cd.entity_id
      WHERE ce.kind = 'component' AND ce.slug = 'icon-button'
    `);
    if (!rows.length) {
      await pool2.end();
      return;
    }
    componentEntityId = rows[0].id;
    originalProps = rows[0].default_props_json;

    // Inject drift: add extra field
    const mutated = typeof originalProps === 'string' ? JSON.parse(originalProps) : { ...originalProps };
    mutated.__drift_test__ = true;

    await pool2.query(
      `UPDATE component_detail SET default_props_json = $1::jsonb WHERE entity_id = $2`,
      [JSON.stringify(mutated), componentEntityId]
    );

    const r2 = runValidator();
    assert.strictEqual(r2.status, 1, `expected exit 1 on component drift\nstdout: ${r2.stdout}\nstderr: ${r2.stderr}`);
    assert.ok(
      r2.stdout.includes('icon-button'),
      `expected stdout to contain 'icon-button'\nstdout: ${r2.stdout}`
    );
  } finally {
    if (originalProps !== undefined && componentEntityId !== undefined) {
      await pool2.query(
        `UPDATE component_detail SET default_props_json = $1::jsonb WHERE entity_id = $2`,
        [typeof originalProps === 'string' ? originalProps : JSON.stringify(originalProps), componentEntityId]
      );
    }
    await pool2.end().catch(() => {});
  }
});
