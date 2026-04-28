#!/usr/bin/env npx tsx
/**
 * unity-bake-ui — driver for Game UI Design System Stage 2 bake.
 *
 * Reads IR JSON produced by `transcribe:cd-game-ui` (Stage 1) and invokes the Unity Editor
 * bridge mutation kind `bake_ui_from_ir` (registered in
 * {@link ../mcp-ia-server/src/tools/unity-bridge-command.ts}). The bridge handler
 * {@link ../../Assets/Scripts/Editor/Bridge/UiBakeHandler.cs} populates the UiTheme SO
 * dictionaries + writes empty placeholder prefabs per panel/interactive.
 *
 * Reuses the same enqueue + poll pipeline as MCP `unity_bridge_command` —
 * see {@link ../mcp-ia-server/scripts/run-unity-bridge-once.ts} for the canonical pattern.
 *
 * Usage from repository root:
 *   npm run unity:bake-ui
 *   IR_PATH=foo/bar/ir.json npm run unity:bake-ui
 *
 * Env:
 *   IR_PATH        Repo-relative path to IR JSON (default web/design-refs/step-1-game-ui/ir.json).
 *   THEME_SO       Repo-relative path to UiTheme asset (default Assets/UI/Theme/DefaultUiTheme.asset).
 *   OUT_DIR        Repo-relative dir for placeholder prefabs (default Assets/UI/Prefabs/Generated).
 *   BRIDGE_TIMEOUT_MS  Override bridge timeout (default 30000, max 120000).
 *   DATABASE_URL   Optional override for Postgres connection.
 *
 * Exit codes:
 *   0  on bake success
 *   1  on any bridge / IR / connection error
 */

import { resolveRepoRoot } from '../mcp-ia-server/src/config.js';
import { loadRepoDotenvIfNotCi } from '../mcp-ia-server/src/ia-db/repo-dotenv.js';
import {
  runUnityBridgeCommand,
  UNITY_BRIDGE_TIMEOUT_MS_MAX,
} from '../mcp-ia-server/src/tools/unity-bridge-command.js';

const IR_PATH_DEFAULT = 'web/design-refs/step-1-game-ui/ir.json';
const THEME_SO_DEFAULT = 'Assets/UI/Theme/DefaultUiTheme.asset';
const OUT_DIR_DEFAULT = 'Assets/UI/Prefabs/Generated';

async function main(): Promise<number> {
  const repo = resolveRepoRoot();
  loadRepoDotenvIfNotCi(repo);

  const ir_path = (process.env.IR_PATH ?? IR_PATH_DEFAULT).trim();
  const theme_so = (process.env.THEME_SO ?? THEME_SO_DEFAULT).trim();
  const out_dir = (process.env.OUT_DIR ?? OUT_DIR_DEFAULT).trim();

  const timeout_ms = Math.min(
    UNITY_BRIDGE_TIMEOUT_MS_MAX,
    Math.max(1000, Number(process.env.BRIDGE_TIMEOUT_MS ?? 30_000) || 30_000),
  );

  console.error(
    `unity-bake-ui: REPO_ROOT=${repo} ir_path=${ir_path} theme_so=${theme_so} out_dir=${out_dir} timeout_ms=${timeout_ms}`,
  );

  const r = await runUnityBridgeCommand({
    kind: 'bake_ui_from_ir',
    timeout_ms,
    ir_path,
    out_dir,
    theme_so,
    agent_id: 'unity-bake-ui',
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
