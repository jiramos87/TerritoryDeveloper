/**
 * Integration test — TECH-526 / B1 server split semantics.
 *
 * Asserts `registerIaCoreTools` + `registerBridgeTools` buckets are disjoint and
 * cover the expected tool sets. Simulates MCP dispatch patterns without spawning
 * actual stdio servers by capturing `server.registerTool(name, ...)` calls on a
 * real `McpServer` instance.
 *
 * ACs:
 *   1. IA-core bucket (design-explore-style dispatch) EXCLUDES `unity_bridge_command`
 *      + the other 11 bridge + compute tool names.
 *   2. Bridge bucket (spec-implementer-style dispatch) INCLUDES `unity_bridge_command`
 *      + the 6 compute tools.
 *   3. Buckets are disjoint: intersection is empty.
 *   4. IA-core bucket has ≥22 registrations (exit criterion from master plan).
 */

import test from "node:test";
import assert from "node:assert/strict";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { buildRegistry } from "../src/config.js";
import {
  registerBridgeTools,
  registerIaCoreTools,
} from "../src/server-registrations.js";

// Capture registered tool names on a fresh McpServer without touching stdio.
function captureToolNames(register: (server: McpServer) => void): Set<string> {
  const server = new McpServer({
    name: "test-harness",
    version: "0.0.0",
    description: "Test-only — captures registerTool calls.",
  });
  const originalRegister = server.registerTool.bind(server);
  const names = new Set<string>();
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  (server as any).registerTool = (name: string, ...rest: unknown[]): unknown => {
    names.add(name);
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    return (originalRegister as any)(name, ...rest);
  };
  register(server);
  return names;
}

const BRIDGE_TOOL_NAMES = [
  "unity_bridge_command",
  "unity_bridge_lease",
  "unity_callers_of",
  "unity_subscribers_of",
  "findobjectoftype_scan",
  "city_metrics_query",
  "isometric_world_to_grid",
  "growth_ring_classify",
  "grid_distance",
  "pathfinding_cost_preview",
  "geography_init_params_validate",
  "desirability_top_cells",
];

test("AC1 — IA-core dispatch excludes all bridge + compute tools", () => {
  const registry = buildRegistry();
  const iaCoreNames = captureToolNames((server) =>
    registerIaCoreTools(server, registry),
  );
  for (const bridgeName of BRIDGE_TOOL_NAMES) {
    assert.ok(
      !iaCoreNames.has(bridgeName),
      `IA-core bucket must not register bridge tool ${bridgeName}`,
    );
  }
});

test("AC2 — Bridge dispatch includes all bridge + compute tools", () => {
  const bridgeNames = captureToolNames((server) => registerBridgeTools(server));
  for (const bridgeName of BRIDGE_TOOL_NAMES) {
    assert.ok(
      bridgeNames.has(bridgeName),
      `Bridge bucket must register ${bridgeName}`,
    );
  }
});

test("AC3 — Buckets are disjoint (no tool registered in both)", () => {
  const registry = buildRegistry();
  const iaCoreNames = captureToolNames((server) =>
    registerIaCoreTools(server, registry),
  );
  const bridgeNames = captureToolNames((server) => registerBridgeTools(server));
  const overlap = [...iaCoreNames].filter((n) => bridgeNames.has(n));
  assert.deepEqual(
    overlap,
    [],
    `Buckets must be disjoint; overlapping names: ${overlap.join(", ")}`,
  );
});

test("AC4 — IA-core bucket registers ≥22 tools (exit criterion)", () => {
  const registry = buildRegistry();
  const iaCoreNames = captureToolNames((server) =>
    registerIaCoreTools(server, registry),
  );
  assert.ok(
    iaCoreNames.size >= 22,
    `IA-core bucket must register ≥22 tools; found ${iaCoreNames.size}`,
  );
});
