/**
 * One-shot IDE agent bridge call (same logic as MCP unity_bridge_command).
 * Run from repository root or any subdirectory; optional REPO_ROOT.
 *
 * Usage: npx tsx tools/mcp-ia-server/scripts/run-unity-bridge-once.ts
 * Env: BRIDGE_TIMEOUT_MS (default 30000, max 30000), DATABASE_URL (optional override).
 */

import { runUnityBridgeCommand } from "../src/tools/unity-bridge-command.js";

const timeout = Math.min(
  30_000,
  Math.max(1000, Number(process.env.BRIDGE_TIMEOUT_MS ?? 30_000) || 30_000),
);

const r = await runUnityBridgeCommand({
  kind: "export_agent_context",
  timeout_ms: timeout,
});
console.log(JSON.stringify(r, null, 2));
process.exit(typeof r === "object" && r !== null && "ok" in r && r.ok ? 0 : 1);
