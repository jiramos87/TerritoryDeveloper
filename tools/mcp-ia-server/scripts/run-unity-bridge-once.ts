/**
 * One-shot IDE agent bridge call (same logic as MCP unity_bridge_command).
 * Run from repository root or any subdirectory; optional REPO_ROOT.
 *
 * Usage: npx tsx tools/mcp-ia-server/scripts/run-unity-bridge-once.ts
 * Env: BRIDGE_TIMEOUT_MS (default 30000, max 120000), DATABASE_URL (optional override).
 */

import { resolveRepoRoot } from "../src/config.js";
import { loadRepoDotenvIfNotCi } from "../src/ia-db/repo-dotenv.js";
import { runUnityBridgeCommand, UNITY_BRIDGE_TIMEOUT_MS_MAX } from "../src/tools/unity-bridge-command.js";

loadRepoDotenvIfNotCi(resolveRepoRoot());

const timeout = Math.min(
  UNITY_BRIDGE_TIMEOUT_MS_MAX,
  Math.max(1000, Number(process.env.BRIDGE_TIMEOUT_MS ?? 30_000) || 30_000),
);

const r = await runUnityBridgeCommand({
  kind: "export_agent_context",
  timeout_ms: timeout,
});
console.log(JSON.stringify(r, null, 2));
process.exit(typeof r === "object" && r !== null && "ok" in r && r.ok ? 0 : 1);
