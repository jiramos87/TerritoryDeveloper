import assert from 'node:assert';
import { spawnSync } from 'node:child_process';
import * as path from 'node:path';
import { test } from 'node:test';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, '..', '..', '..');
const SCRIPT = path.join(REPO_ROOT, 'tools/scripts/unity-bake-ui.ts');

/**
 * Smoke test — verify driver loads + parses env defaults + reports an
 * error path cleanly without hanging. Real bake requires Unity Editor
 * + Postgres, both absent in CI; we simulate by pointing DATABASE_URL
 * at an unreachable host so the bridge enqueue fails fast.
 */
test('unity-bake-ui exits non-zero when bridge cannot reach Postgres', () => {
  const env = {
    ...process.env,
    // unreachable Postgres → enqueueUnityBridgeJob returns db_error → exit 1
    DATABASE_URL: 'postgresql://nobody:nobody@127.0.0.1:1/nope',
    BRIDGE_TIMEOUT_MS: '2000',
    // disable repo .env loader override (loadRepoDotenvIfNotCi early-returns when CI)
    CI: '1',
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

test('unity-bake-ui honours IR_PATH/THEME_SO/OUT_DIR env overrides in header', () => {
  const env = {
    ...process.env,
    DATABASE_URL: 'postgresql://nobody:nobody@127.0.0.1:1/nope',
    BRIDGE_TIMEOUT_MS: '2000',
    CI: '1',
    IR_PATH: 'custom/ir.json',
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
  assert.match(r.stderr, /ir_path=custom\/ir\.json/);
  assert.match(r.stderr, /theme_so=custom\/theme\.asset/);
  assert.match(r.stderr, /out_dir=custom\/out/);
});
