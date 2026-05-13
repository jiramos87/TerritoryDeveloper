#!/usr/bin/env npx tsx
/**
 * unity-bake-ui — driver for Game UI Catalog Bake pipeline.
 *
 * Stage 9.10: Pre-bake hook refreshes panels.json from DB before issuing
 * bridge mutation. Bake fails hard if exporter exits non-zero.
 *
 * Invokes the Unity Editor bridge mutation kind `bake_ui_from_ir`
 * (registered in {@link ../mcp-ia-server/src/tools/unity-bridge-command.ts}).
 * The bridge handler {@link ../../Assets/Scripts/Editor/Bridge/UiBakeHandler.cs}
 * populates the UiTheme SO dictionaries + writes placeholder prefabs per panel.
 *
 * Reuses the same enqueue + poll pipeline as MCP `unity_bridge_command` —
 * see {@link ../mcp-ia-server/scripts/run-unity-bridge-once.ts} for the
 * canonical pattern.
 *
 * Usage from repository root:
 *   npm run unity:bake-ui
 *   npm run unity:bake-ui -- --capture-baselines --panels pause-menu
 *   PANELS_PATH=custom/panels.json npm run unity:bake-ui
 *
 * Env:
 *   PANELS_PATH    Repo-relative path to panels snapshot JSON
 *                  (default Assets/UI/Snapshots/panels.json).
 *   IR_PATH        Deprecated alias for PANELS_PATH. Logs warning on use.
 *   THEME_SO       Repo-relative path to UiTheme asset
 *                  (default Assets/UI/Theme/DefaultUiTheme.asset).
 *   OUT_DIR        Repo-relative dir for placeholder prefabs
 *                  (default Assets/UI/Prefabs/Generated).
 *   BRIDGE_TIMEOUT_MS  Override bridge timeout (default 30000, max 120000).
 *   DATABASE_URL   Optional override for Postgres connection.
 *   SKIP_SNAPSHOT_EXPORT  When "1", skip pre-bake exporter step (CI fixture mode).
 *
 * Flags (TECH-31891 — visual regression):
 *   --capture-baselines   After bake, emit candidate PNGs to Library/UiBaselines/_candidate/.
 *   --panels {csv}        Comma-separated panel slugs to capture (requires --capture-baselines).
 *                         Omitting captures all baked panels.
 *
 * Exit codes:
 *   0  on bake success
 *   1  on any bridge / snapshot / connection error
 */

import { spawnSync } from 'node:child_process';
import * as path from 'node:path';
import * as process from 'node:process';

import { resolveRepoRoot } from '../mcp-ia-server/src/config.js';
import { loadRepoDotenvIfNotCi } from '../mcp-ia-server/src/ia-db/repo-dotenv.js';
import {
  runUnityBridgeCommand,
  UNITY_BRIDGE_TIMEOUT_MS_MAX,
} from '../mcp-ia-server/src/tools/unity-bridge-command.js';

// ── Argv helpers (TECH-31891) ──────────────────────────────────────────────

/** Parse --capture-baselines + --panels {csv} from process.argv. */
function parseVisualRegressionFlags(): { captureBaselines: boolean; panelsCsv: string } {
  const argv = process.argv.slice(2);
  const captureBaselines = argv.includes('--capture-baselines');
  let panelsCsv = '';
  const panelsIdx = argv.findIndex((a) => a === '--panels');
  if (panelsIdx !== -1 && argv[panelsIdx + 1]) {
    panelsCsv = argv[panelsIdx + 1]!.trim();
  }
  return { captureBaselines, panelsCsv };
}

const PANELS_PATH_DEFAULT = 'Assets/UI/Snapshots/panels.json';
const THEME_SO_DEFAULT = 'Assets/UI/Theme/DefaultUiTheme.asset';
const OUT_DIR_DEFAULT = 'Assets/UI/Prefabs/Generated';

/** Resolve PANELS_PATH from env, honouring deprecated IR_PATH alias. */
function resolvePanelsPath(): string {
  if (process.env.PANELS_PATH) {
    return process.env.PANELS_PATH.trim();
  }
  if (process.env.IR_PATH) {
    console.error(
      '[unity-bake-ui] IR_PATH deprecated → use PANELS_PATH',
    );
    return process.env.IR_PATH.trim();
  }
  return PANELS_PATH_DEFAULT;
}

/** Run snapshot exporter before bake. Throws with structured error on failure. */
function runSnapshotExporter(repoRoot: string): void {
  const exporterScript = path.join(repoRoot, 'tools/scripts/snapshot-export-game-ui.mjs');
  const result = spawnSync('node', [exporterScript], {
    encoding: 'utf8',
    cwd: repoRoot,
    env: process.env,
  });
  if (result.stdout) process.stdout.write(result.stdout);
  if (result.stderr) process.stderr.write(result.stderr);
  if (result.status !== 0) {
    const stderr = result.stderr ?? '';
    const msg = stderr.slice(0, 400) || `exporter exited ${result.status}`;
    throw Object.assign(new Error(`bake.snapshot_export_failed: ${msg}`), {
      code: 'snapshot_export_failed',
    });
  }
}

async function main(): Promise<number> {
  const repo = resolveRepoRoot();
  loadRepoDotenvIfNotCi(repo);

  const panels_path = resolvePanelsPath();
  const theme_so = (process.env.THEME_SO ?? THEME_SO_DEFAULT).trim();
  const out_dir = (process.env.OUT_DIR ?? OUT_DIR_DEFAULT).trim();

  const timeout_ms = Math.min(
    UNITY_BRIDGE_TIMEOUT_MS_MAX,
    Math.max(1000, Number(process.env.BRIDGE_TIMEOUT_MS ?? 30_000) || 30_000),
  );

  const { captureBaselines, panelsCsv } = parseVisualRegressionFlags();

  console.error(
    `unity-bake-ui: REPO_ROOT=${repo} panels_path=${panels_path} theme_so=${theme_so} out_dir=${out_dir} timeout_ms=${timeout_ms} captureBaselines=${captureBaselines} panelsCsv=${panelsCsv || '(all)'}`,
  );

  // Pre-bake hook: refresh panels.json from DB unless fixture mode.
  if (process.env.SKIP_SNAPSHOT_EXPORT !== '1') {
    try {
      runSnapshotExporter(repo);
    } catch (err: unknown) {
      const e = err as Error & { code?: string };
      console.error(`unity-bake-ui: pre-bake snapshot export failed — ${e.message}`);
      return 1;
    }
  }

  const r = await runUnityBridgeCommand({
    kind: 'bake_ui_from_ir',
    timeout_ms,
    // Bridge still receives ir_path key for backwards-compat with C# handler
    // until Stage 9.10 T3 swaps C# input source.
    ir_path: panels_path,
    out_dir,
    theme_so,
    agent_id: 'unity-bake-ui',
    // Visual regression flags (TECH-31891). Passed through BakeArgs JSON.
    ...(captureBaselines
      ? { capture_baselines: true, capture_panels_csv: panelsCsv }
      : {}),
  });

  console.log(JSON.stringify(r, null, 2));
  if (!r.ok) {
    console.error(`unity-bake-ui: bridge failed — error=${r.error} message=${r.message}`);
    return 1;
  }
  if (r.response.error) {
    console.error(`unity-bake-ui: handler returned error — ${r.response.error}`);
    return 1;
  }
  return 0;
}

main().then(
  (code) => process.exit(code),
  (err) => {
    console.error('unity-bake-ui: unexpected error', err);
    process.exit(1);
  },
);
