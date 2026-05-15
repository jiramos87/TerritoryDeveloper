// bridge-client.mjs — vitest-friendly wrapper around agent_bridge_job (Postgres mig 0008).
//
// Used by stage test files under tests/{slug}/stage{N}.M-{slug}.test.mjs to drive
// Unity Editor operations without opening a CLI. Enqueues an agent_bridge_job row,
// polls until status terminal, returns parsed response.
//
// Requires:
//   - DATABASE_URL env var (loaded automatically via tools/scripts/load-repo-env.inc.sh
//     when this module is imported from a node --test runner inside the repo)
//   - Live Unity Editor on REPO_ROOT with AgentBridgeCommandRunner picking up jobs
//   - pg npm package (resolvable from root via tools/mcp-ia-server/node_modules/pg)
//
// Kinds supported (all existing in AgentBridgeCommandRunner):
//   enter_play_mode, exit_play_mode, get_play_mode_status,
//   get_compilation_status, get_console_logs, capture_screenshot,
//   debug_context_bundle, ui_tree_walk, prefab_inspect, prefab_manifest,
//   read_panel_state, dispatch_action, get_action_log,
//   export_cell_chunk, export_sorting_debug, sorting_order_debug,
//   claude_design_check, claude_design_conformance, validate_panel_blueprint,
//   economy_balance_snapshot, export_agent_context.
//
// Convenience helpers wrap common patterns:
//   findGameObject(name)        → calls ui_tree_walk + scans hierarchy, then
//                                 falls back to debug_context_bundle scene scan
//   getComponent(goId, type)    → calls debug_context_bundle + walks bundle JSON
//   activeScenePath()           → calls get_play_mode_status (returns scene name)
//
// Escape hatch:
//   command(kind, params)       → enqueue any bridge kind not yet wrapped here
//
// Failure modes:
//   - DATABASE_URL unset            → throws on first method call
//   - pg unresolvable               → throws on import
//   - bridge job times out (60s)    → throws BridgeTimeoutError
//   - bridge kind not in runner     → returned response.error captured + thrown

import { createRequire } from 'node:module';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';

const __dirname = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = resolve(__dirname, '../../..');

// Lazy-load .env so test runners that don't source the shell env still pick up DATABASE_URL.
function ensureDatabaseUrl() {
  if (process.env.DATABASE_URL) return;
  try {
    const envText = readFileSync(`${REPO_ROOT}/.env`, 'utf8');
    for (const line of envText.split('\n')) {
      const m = line.match(/^DATABASE_URL=(.+)$/);
      if (m) {
        process.env.DATABASE_URL = m[1].trim().replace(/^['"]|['"]$/g, '');
        return;
      }
    }
  } catch {}
  if (!process.env.DATABASE_URL) {
    throw new Error('bridge-client: DATABASE_URL unset and .env unreadable');
  }
}

// Resolve pg from either root or mcp-ia-server's nested node_modules.
const require = createRequire(import.meta.url);
let pgModule;
try {
  pgModule = require('pg');
} catch {
  pgModule = require(`${REPO_ROOT}/tools/mcp-ia-server/node_modules/pg`);
}
const { Pool } = pgModule;

export class BridgeTimeoutError extends Error {
  constructor(kind, jobId, elapsedMs) {
    super(`bridge timeout: kind=${kind} job=${jobId} elapsed=${elapsedMs}ms`);
    this.kind = kind;
    this.jobId = jobId;
    this.elapsedMs = elapsedMs;
  }
}

export class BridgeError extends Error {
  constructor(kind, jobId, errorPayload) {
    super(`bridge error: kind=${kind} job=${jobId} error=${JSON.stringify(errorPayload)}`);
    this.kind = kind;
    this.jobId = jobId;
    this.errorPayload = errorPayload;
  }
}

export class BridgeClient {
  constructor(opts = {}) {
    this.timeoutMs = opts.timeoutMs ?? 60000;
    this.pollIntervalMs = opts.pollIntervalMs ?? 250;
    this._pool = null;
  }

  _getPool() {
    if (this._pool) return this._pool;
    ensureDatabaseUrl();
    this._pool = new Pool({ connectionString: process.env.DATABASE_URL, max: 2 });
    return this._pool;
  }

  async close() {
    if (this._pool) {
      await this._pool.end();
      this._pool = null;
    }
  }

  async command(kind, params = {}) {
    const pool = this._getPool();
    const requestJson = JSON.stringify({ params });
    const insert = await pool.query(
      `INSERT INTO agent_bridge_job (command_id, kind, status, request)
       VALUES (gen_random_uuid(), $1, 'pending', $2::jsonb)
       RETURNING command_id`,
      [kind, requestJson]
    );
    const jobId = insert.rows[0].command_id;
    const start = Date.now();
    while (Date.now() - start < this.timeoutMs) {
      const r = await pool.query(
        `SELECT status, response FROM agent_bridge_job WHERE command_id = $1`,
        [jobId]
      );
      if (r.rows.length === 0) {
        await sleep(this.pollIntervalMs);
        continue;
      }
      const { status, response } = r.rows[0];
      if (status === 'completed') {
        return response;
      }
      if (status === 'failed') {
        throw new BridgeError(kind, jobId, response);
      }
      await sleep(this.pollIntervalMs);
    }
    throw new BridgeTimeoutError(kind, jobId, Date.now() - start);
  }

  // Convenience wrappers — passthrough to .command() for clarity in tests.
  enterPlayMode() { return this.command('enter_play_mode'); }
  exitPlayMode() { return this.command('exit_play_mode'); }
  getPlayModeStatus() { return this.command('get_play_mode_status'); }
  getCompilationStatus(opts = {}) { return this.command('get_compilation_status', opts); }
  getConsoleLogs(opts = {}) { return this.command('get_console_logs', opts); }
  captureScreenshot(opts = {}) { return this.command('capture_screenshot', opts); }
  debugContextBundle(opts = {}) { return this.command('debug_context_bundle', opts); }
  uiTreeWalk(opts = {}) { return this.command('ui_tree_walk', opts); }
  prefabInspect(opts = {}) { return this.command('prefab_inspect', opts); }
  prefabManifest(opts = {}) { return this.command('prefab_manifest', opts); }
  readPanelState(opts = {}) { return this.command('read_panel_state', opts); }
  dispatchAction(opts = {}) { return this.command('dispatch_action', opts); }

  // Higher-level helpers — built on inspection kinds.

  // Return the currently active scene path (e.g. "Assets/Scenes/CityScene.unity")
  // by reading get_play_mode_status which exposes scene_name + scene_path.
  async activeScenePath() {
    const status = await this.getPlayModeStatus();
    return status?.scene_path ?? status?.scene_name ?? null;
  }

  // Assert the active scene matches `scenePath` (no scene-load mutation kind exists yet —
  // tests must pre-arrange the Editor with the target scene open).
  async openScene(scenePath) {
    const active = await this.activeScenePath();
    if (!active) {
      throw new Error(`bridge-client.openScene: no active scene reported by get_play_mode_status`);
    }
    if (active !== scenePath && !active.endsWith(scenePath)) {
      throw new Error(
        `bridge-client.openScene: active scene "${active}" does not match expected "${scenePath}"`
      );
    }
    return { scene_path: active };
  }

  // Walk a debug_context_bundle response and return matching GameObject(s) by name.
  // Returns null if not found.
  async findGameObject(name, opts = {}) {
    const bundle = await this.debugContextBundle(opts);
    return walkBundleForName(bundle, name);
  }

  // Walk a debug_context_bundle and find a component matching `componentType` on
  // the named GameObject. Returns the component object (with fields) or null.
  async getComponent(gameObjectName, componentType, opts = {}) {
    const bundle = await this.debugContextBundle(opts);
    const go = walkBundleForName(bundle, gameObjectName);
    if (!go) return null;
    const comps = go.components ?? [];
    return comps.find((c) => c.type === componentType || c.type?.endsWith('.' + componentType)) ?? null;
  }
}

function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

function walkBundleForName(bundle, name) {
  if (!bundle) return null;
  // debug_context_bundle returns { bundle: { scene_state: { gameObjects: [...] } } }
  const scene = bundle?.bundle?.scene_state ?? bundle?.scene_state ?? bundle;
  const queue = [];
  if (Array.isArray(scene?.gameObjects)) queue.push(...scene.gameObjects);
  if (Array.isArray(scene?.children)) queue.push(...scene.children);
  while (queue.length) {
    const node = queue.shift();
    if (node?.name === name) return node;
    if (Array.isArray(node?.children)) queue.push(...node.children);
  }
  return null;
}

export default BridgeClient;
