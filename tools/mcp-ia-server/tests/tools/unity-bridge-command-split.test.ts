/**
 * unity-bridge-command-split — verifies module folder barrel preserves all public exports.
 * Red-Stage Proof anchor: unit-test:tools/mcp-ia-server/tests/unity-bridge-command-split.test.ts::*
 */

import assert from "node:assert/strict";
import { describe, it } from "node:test";

// ── Import from barrel (unity-bridge-command.ts) — unchanged surface ─────────
import {
  BRIDGE_OUTPUT_PREVIEW_MAX,
  UNITY_BRIDGE_TIMEOUT_MS_MAX,
  unityBridgeTimeoutMsSchema,
  unityBridgeCommandInputSchema,
  unityBridgeGetInputSchema,
  unityCompileInputSchema,
  resolveExportSugarTimeoutMs,
  EXPORT_SUGAR_DEFAULT_TIMEOUT_MS,
  runUnityBridgeCommand,
  runUnityBridgeGet,
  enqueueUnityBridgeJob,
  pollUnityBridgeJobUntilTerminal,
  registerUnityBridgeCommand,
  buildRequestEnvelope,
  jsonResult,
  sleepMs,
  selectBridgeRow,
} from "../../src/tools/unity-bridge-command.js";

// ── Import directly from sub-modules — split surface intact ──────────────────
import { BRIDGE_OUTPUT_PREVIEW_MAX as CONST_PREVIEW } from "../../src/tools/unity-bridge-command/constants.js";
import { unityBridgeCommandInputSchema as SCHEMA_INPUT } from "../../src/tools/unity-bridge-command/input-schema.js";
import { unityBridgeGetInputSchema as SCHEMA_GET } from "../../src/tools/unity-bridge-command/get-schema.js";
import { buildRequestEnvelope as BUILD_ENV, jsonResult as JSON_RESULT } from "../../src/tools/unity-bridge-command/envelope.js";
import { runUnityBridgeCommand as RUN_CMD, EXPORT_SUGAR_DEFAULT_TIMEOUT_MS as SUGAR_DEFAULT } from "../../src/tools/unity-bridge-command/run.js";
import { unityCompileInputSchema as COMPILE_SCHEMA, registerUnityBridgeCommand as REGISTER_CMD } from "../../src/tools/unity-bridge-command/register.js";

describe("unity-bridge-command split barrel", () => {
  it("constants re-exported correctly from barrel", () => {
    assert.equal(typeof BRIDGE_OUTPUT_PREVIEW_MAX, "number");
    assert.equal(BRIDGE_OUTPUT_PREVIEW_MAX, 512);
    assert.equal(UNITY_BRIDGE_TIMEOUT_MS_MAX, 120_000);
    // sub-module matches barrel
    assert.equal(CONST_PREVIEW, BRIDGE_OUTPUT_PREVIEW_MAX);
  });

  it("EXPORT_SUGAR_DEFAULT_TIMEOUT_MS re-exported correctly", () => {
    assert.equal(EXPORT_SUGAR_DEFAULT_TIMEOUT_MS, 40_000);
    assert.equal(SUGAR_DEFAULT, EXPORT_SUGAR_DEFAULT_TIMEOUT_MS);
  });

  it("resolveExportSugarTimeoutMs returns default when no args", () => {
    const saved = process.env.BRIDGE_TIMEOUT_MS;
    delete process.env.BRIDGE_TIMEOUT_MS;
    const ms = resolveExportSugarTimeoutMs();
    assert.equal(ms, EXPORT_SUGAR_DEFAULT_TIMEOUT_MS);
    if (saved !== undefined) process.env.BRIDGE_TIMEOUT_MS = saved;
  });

  it("resolveExportSugarTimeoutMs respects explicit arg", () => {
    assert.equal(resolveExportSugarTimeoutMs(5000), 5000);
  });

  it("resolveExportSugarTimeoutMs clamps to UNITY_BRIDGE_TIMEOUT_MS_MAX", () => {
    assert.equal(resolveExportSugarTimeoutMs(999_999), UNITY_BRIDGE_TIMEOUT_MS_MAX);
  });

  it("unityBridgeCommandInputSchema parses default kind", () => {
    const result = unityBridgeCommandInputSchema.safeParse({});
    assert.ok(result.success);
    assert.equal(result.data.kind, "export_agent_context");
    // sub-module schema identical
    const result2 = SCHEMA_INPUT.safeParse({});
    assert.ok(result2.success);
    assert.equal(result2.data.kind, "export_agent_context");
  });

  it("unityBridgeCommandInputSchema rejects debug_context_bundle without seed_cell", () => {
    const result = unityBridgeCommandInputSchema.safeParse({ kind: "debug_context_bundle" });
    assert.ok(!result.success);
    const msgs = result.error.issues.map((i) => i.message);
    assert.ok(msgs.some((m) => m.includes("seed_cell")));
  });

  it("unityBridgeCommandInputSchema rejects claude_design_conformance with both prefab and scene", () => {
    const result = unityBridgeCommandInputSchema.safeParse({
      kind: "claude_design_conformance",
      prefab_path: "Assets/Foo.prefab",
      scene_root_path: "MyRoot",
      ir_path: "web/ir.json",
      theme_so: "Assets/Theme.asset",
    });
    assert.ok(!result.success);
  });

  it("unityBridgeGetInputSchema parses valid input", () => {
    const uuid = "550e8400-e29b-41d4-a716-446655440000";
    const result = unityBridgeGetInputSchema.safeParse({ command_id: uuid });
    assert.ok(result.success);
    assert.equal(result.data.command_id, uuid);
    // sub-module
    const r2 = SCHEMA_GET.safeParse({ command_id: uuid });
    assert.ok(r2.success);
  });

  it("unityCompileInputSchema parses empty args (defaults)", () => {
    const result = unityCompileInputSchema.safeParse({});
    assert.ok(result.success);
    assert.equal(result.data.timeout_ms, 30_000);
    // sub-module
    const r2 = COMPILE_SCHEMA.safeParse({});
    assert.ok(r2.success);
  });

  it("unityBridgeTimeoutMsSchema clamps correctly", () => {
    const parsed = unityBridgeTimeoutMsSchema.parse(60_000);
    assert.equal(parsed, 60_000);
    const tooHigh = unityBridgeTimeoutMsSchema.safeParse(200_000);
    assert.ok(!tooHigh.success);
    const tooLow = unityBridgeTimeoutMsSchema.safeParse(500);
    assert.ok(!tooLow.success);
  });

  it("buildRequestEnvelope returns base shape for unknown kind fallthrough", () => {
    const parsed = unityBridgeCommandInputSchema.parse({ kind: "refresh_asset_database" });
    const env = buildRequestEnvelope("test-id", parsed);
    assert.equal(env.kind, "refresh_asset_database");
    assert.equal(env.schema_version, 1);
    assert.equal(env.artifact, "unity_agent_bridge_command");
    assert.equal(env.command_id, "test-id");
    assert.equal(env.agent_id, "anonymous");
    // sub-module returns same structural fields (timestamp may differ by ms)
    const env2 = BUILD_ENV("test-id", parsed);
    assert.equal(env2.kind, "refresh_asset_database");
    assert.equal(env2.schema_version, 1);
    assert.equal(env2.artifact, "unity_agent_bridge_command");
    assert.deepEqual(env2.params, {});
  });

  it("jsonResult wraps payload as text content", () => {
    const result = jsonResult({ ok: true });
    assert.equal(result.content[0].type, "text");
    assert.ok(result.content[0].text.includes('"ok": true'));
    // sub-module
    const r2 = JSON_RESULT({ ok: true });
    assert.deepEqual(r2, result);
  });

  it("runUnityBridgeCommand returns db_unconfigured when pool is null", async () => {
    const result = await runUnityBridgeCommand(
      unityBridgeCommandInputSchema.parse({ kind: "get_compilation_status" }),
      { pool: null },
    );
    assert.ok(!result.ok);
    assert.equal(result.error, "db_unconfigured");
    // sub-module
    const r2 = await RUN_CMD(
      unityBridgeCommandInputSchema.parse({ kind: "get_compilation_status" }),
      { pool: null },
    );
    assert.ok(!r2.ok);
    assert.equal(r2.error, "db_unconfigured");
  });

  it("runUnityBridgeGet returns db_unconfigured when pool is null", async () => {
    const result = await runUnityBridgeGet(
      unityBridgeGetInputSchema.parse({ command_id: "550e8400-e29b-41d4-a716-446655440001" }),
      { pool: null },
    );
    assert.ok(!result.ok);
    assert.equal(result.error, "db_unconfigured");
  });

  it("registerUnityBridgeCommand is a function", () => {
    assert.equal(typeof registerUnityBridgeCommand, "function");
    assert.equal(typeof REGISTER_CMD, "function");
    assert.equal(registerUnityBridgeCommand, REGISTER_CMD);
  });

  it("enqueueUnityBridgeJob and pollUnityBridgeJobUntilTerminal are functions", () => {
    assert.equal(typeof enqueueUnityBridgeJob, "function");
    assert.equal(typeof pollUnityBridgeJobUntilTerminal, "function");
  });

  it("selectBridgeRow and sleepMs exported from barrel", () => {
    assert.equal(typeof selectBridgeRow, "function");
    assert.equal(typeof sleepMs, "function");
  });

  it("buildRequestEnvelope builds correct params for get_console_logs", () => {
    const parsed = unityBridgeCommandInputSchema.parse({
      kind: "get_console_logs",
      since_utc: "2026-01-01T00:00:00Z",
      severity_filter: "error",
      tag_filter: "Grid",
      max_lines: 50,
    });
    const env = buildRequestEnvelope("cid", parsed);
    const params = env.params as Record<string, unknown>;
    assert.equal(params.since_utc, "2026-01-01T00:00:00Z");
    assert.equal(params.severity_filter, "error");
    assert.equal(params.tag_filter, "Grid");
    assert.equal(params.max_lines, 50);
  });

  it("buildRequestEnvelope builds correct params for export_cell_chunk", () => {
    const parsed = unityBridgeCommandInputSchema.parse({
      kind: "export_cell_chunk",
      origin_x: 4,
      origin_y: 8,
      chunk_width: 16,
      chunk_height: 16,
    });
    const env = buildRequestEnvelope("cid", parsed);
    const params = env.params as Record<string, unknown>;
    assert.equal(params.origin_x, 4);
    assert.equal(params.origin_y, 8);
    assert.equal(params.chunk_width, 16);
    assert.equal(params.chunk_height, 16);
  });

  it("buildRequestEnvelope builds correct params for set_panel_visible", () => {
    const parsed = unityBridgeCommandInputSchema.parse({
      kind: "set_panel_visible",
      slug: "hud-bar",
      active: false,
    });
    const env = buildRequestEnvelope("cid", parsed);
    const params = env.params as Record<string, unknown>;
    assert.equal(params.slug, "hud-bar");
    assert.equal(params.active, false);
  });
});
