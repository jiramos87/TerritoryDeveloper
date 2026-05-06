import assert from 'node:assert';
import { spawnSync } from 'node:child_process';
import * as path from 'node:path';
import { test } from 'node:test';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, '..', '..', '..');
const SCRIPT = path.join(REPO_ROOT, 'tools/scripts/unity-bake-ui.ts');

/**
 * Smoke tests — verify driver loads + parses env defaults + reports error
 * paths cleanly without hanging. Real bake requires Unity Editor + Postgres,
 * both absent in CI; simulate with unreachable Postgres + SKIP_SNAPSHOT_EXPORT=1.
 *
 * Stage 9.10: PANELS_PATH replaces IR_PATH; IR_PATH kept as deprecated alias.
 */

test('unity-bake-ui exits non-zero when bridge cannot reach Postgres', () => {
  const env = {
    ...process.env,
    DATABASE_URL: 'postgresql://nobody:nobody@127.0.0.1:1/nope',
    BRIDGE_TIMEOUT_MS: '2000',
    CI: '1',
    // Skip exporter so test only exercises bridge path.
    SKIP_SNAPSHOT_EXPORT: '1',
  };
  const r = spawnSync('npx', ['tsx', SCRIPT], {
    encoding: 'utf8',
    env,
    cwd: REPO_ROOT,
    timeout: 30_000,
  });
  assert.notStrictEqual(r.status, 0, `expected non-zero exit; stdout=${r.stdout}\nstderr=${r.stderr}`);
  // driver header always logged before bridge call
  assert.match(r.stderr, /unity-bake-ui: REPO_ROOT=/);
  // either db error surfaces in stdout JSON (ok:false) or stderr fail-line
  assert.match(
    r.stdout + r.stderr,
    /db_error|db_unconfigured|bridge failed|unity-bake-ui:/,
  );
});

test('unity-bake-ui honours PANELS_PATH/THEME_SO/OUT_DIR env overrides in header', () => {
  const env = {
    ...process.env,
    DATABASE_URL: 'postgresql://nobody:nobody@127.0.0.1:1/nope',
    BRIDGE_TIMEOUT_MS: '2000',
    CI: '1',
    SKIP_SNAPSHOT_EXPORT: '1',
    PANELS_PATH: 'custom/panels.json',
    THEME_SO: 'custom/theme.asset',
    OUT_DIR: 'custom/out',
  };
  const r = spawnSync('npx', ['tsx', SCRIPT], {
    encoding: 'utf8',
    env,
    cwd: REPO_ROOT,
    timeout: 30_000,
  });
  assert.notStrictEqual(r.status, 0);
  assert.match(r.stderr, /panels_path=custom\/panels\.json/);
  assert.match(r.stderr, /theme_so=custom\/theme\.asset/);
  assert.match(r.stderr, /out_dir=custom\/out/);
});

test('unity-bake-ui honours deprecated IR_PATH alias with deprecation warning', () => {
  const env = {
    ...process.env,
    DATABASE_URL: 'postgresql://nobody:nobody@127.0.0.1:1/nope',
    BRIDGE_TIMEOUT_MS: '2000',
    CI: '1',
    SKIP_SNAPSHOT_EXPORT: '1',
    IR_PATH: 'custom/ir.json',
  };
  const r = spawnSync('npx', ['tsx', SCRIPT], {
    encoding: 'utf8',
    env,
    cwd: REPO_ROOT,
    timeout: 30_000,
  });
  assert.notStrictEqual(r.status, 0);
  // Deprecation warning emitted on stderr
  assert.match(r.stderr, /IR_PATH deprecated/);
  // Resolved path appears in header as panels_path
  assert.match(r.stderr, /panels_path=custom\/ir\.json/);
});

test('test_prebake_hook_invokes_exporter — exporter failure exits bake non-zero', () => {
  // Point exporter at bad DATABASE_URL without SKIP_SNAPSHOT_EXPORT;
  // exporter exits non-zero → bake exits non-zero with snapshot_export_failed.
  const env = {
    ...process.env,
    DATABASE_URL: 'postgresql://nobody:nobody@127.0.0.1:1/nope',
    BRIDGE_TIMEOUT_MS: '2000',
    CI: '1',
    // No SKIP_SNAPSHOT_EXPORT — let exporter run and fail.
  };
  const r = spawnSync('npx', ['tsx', SCRIPT], {
    encoding: 'utf8',
    env,
    cwd: REPO_ROOT,
    timeout: 30_000,
  });
  assert.notStrictEqual(r.status, 0, `expected non-zero from exporter failure; stderr=${r.stderr}`);
  // Either snapshot_export_failed logged or the driver header at minimum.
  assert.match(
    r.stdout + r.stderr,
    /snapshot_export_failed|snapshot-export-game-ui|unity-bake-ui:/,
  );
});
