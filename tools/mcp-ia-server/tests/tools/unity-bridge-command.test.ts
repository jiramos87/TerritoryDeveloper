/**
 * unity_bridge_command / unity_bridge_get — Postgres job queue (mock pool).
 */

import assert from "node:assert/strict";
import { describe, it } from "node:test";
import type { Pool, QueryResult } from "pg";
import {
  runUnityBridgeCommand,
  runUnityBridgeGet,
  unityBridgeCommandInputSchema,
  type UnityBridgeResponsePayload,
} from "../../src/tools/unity-bridge-command.js";

function mockPool(handlers: {
  insert?: () => QueryResult;
  selectSequence?: Array<BridgeSelectRow | null>;
  delete?: () => QueryResult;
}): Pool {
  let selectIdx = 0;
  return {
    query: async (sql: string) => {
      if (sql.includes("INSERT INTO agent_bridge_job")) {
        return handlers.insert?.() ?? { rows: [], rowCount: 1 };
      }
      if (sql.includes("DELETE FROM agent_bridge_job")) {
        return handlers.delete?.() ?? { rows: [], rowCount: 1 };
      }
      if (sql.includes("SELECT status, response, error, kind")) {
        const row = handlers.selectSequence?.[selectIdx++] ?? null;
        if (row === undefined) {
          throw new Error("selectSequence exhausted");
        }
        return {
          rows: row ? [row] : [],
          rowCount: row ? 1 : 0,
        };
      }
      throw new Error(`Unexpected SQL in mock: ${sql.slice(0, 80)}`);
    },
  } as unknown as Pool;
}

type BridgeSelectRow = {
  status: string;
  response: UnityBridgeResponsePayload | null;
  error: string | null;
  kind: string;
};

describe("runUnityBridgeCommand", () => {
  it("returns db_unconfigured when pool is null", async () => {
    const r = await runUnityBridgeCommand(
      { kind: "export_agent_context", timeout_ms: 1000 },
      { pool: null },
    );
    assert.equal(r.ok, false);
    if (!r.ok) assert.equal(r.error, "db_unconfigured");
  });

  it("returns response when job completes", async () => {
    const sampleResponse: UnityBridgeResponsePayload = {
      schema_version: 1,
      artifact: "unity_agent_bridge_response",
      command_id: "will-be-overwritten",
      ok: true,
      completed_at_utc: new Date().toISOString(),
      storage: "postgres",
      artifact_paths: [],
      postgres_only: true,
      error: null,
    };
    const pool = mockPool({
      selectSequence: [
        {
          status: "completed",
          response: sampleResponse,
          error: null,
          kind: "export_agent_context",
        },
      ],
    });
    const r = await runUnityBridgeCommand(
      { kind: "export_agent_context", timeout_ms: 5000 },
      { pool },
    );
    assert.equal(r.ok, true);
    if (r.ok) {
      assert.equal(r.response.ok, true);
      assert.equal(r.response.storage, "postgres");
      assert.ok(r.command_id);
    }
  });

  it("defaults timeout when timeout_ms omitted (programmatic caller)", async () => {
    const sampleResponse: UnityBridgeResponsePayload = {
      schema_version: 1,
      artifact: "unity_agent_bridge_response",
      command_id: "x",
      ok: true,
      completed_at_utc: new Date().toISOString(),
      storage: "postgres",
      artifact_paths: [],
      postgres_only: true,
      error: null,
    };
    const pool = mockPool({
      selectSequence: [
        {
          status: "completed",
          response: sampleResponse,
          error: null,
          kind: "export_agent_context",
        },
      ],
    });
    const r = await runUnityBridgeCommand(
      { kind: "export_agent_context" } as Parameters<typeof runUnityBridgeCommand>[0],
      { pool },
    );
    assert.equal(r.ok, true);
  });

  it("times out and deletes pending row", async () => {
    let deleteCalled = false;
    const pool = mockPool({
      selectSequence: Array.from({ length: 40 }, () => ({
        status: "pending",
        response: null,
        error: null,
        kind: "export_agent_context",
      })),
      delete: () => {
        deleteCalled = true;
        return { rows: [], rowCount: 1 };
      },
    });
    const r = await runUnityBridgeCommand(
      { kind: "export_agent_context", timeout_ms: 800 },
      { pool },
    );
    assert.equal(r.ok, false);
    if (!r.ok) assert.equal(r.error, "timeout");
    assert.equal(deleteCalled, true);
  });
});

describe("unityBridgeCommandInputSchema", () => {
  it("rejects max_lines above 2000 for get_console_logs", () => {
    const r = unityBridgeCommandInputSchema.safeParse({
      kind: "get_console_logs",
      max_lines: 99999,
      timeout_ms: 5000,
    });
    assert.equal(r.success, false);
  });

  it("accepts get_console_logs with defaults", () => {
    const r = unityBridgeCommandInputSchema.safeParse({
      kind: "get_console_logs",
      timeout_ms: 5000,
    });
    assert.equal(r.success, true);
    if (r.success) {
      assert.equal(r.data.severity_filter, "all");
      assert.equal(r.data.max_lines, 200);
    }
  });

  it("accepts capture_screenshot with include_ui true", () => {
    const r = unityBridgeCommandInputSchema.safeParse({
      kind: "capture_screenshot",
      timeout_ms: 5000,
      include_ui: true,
    });
    assert.equal(r.success, true);
    if (r.success) assert.equal(r.data.include_ui, true);
  });

  it("defaults include_ui to false for capture_screenshot", () => {
    const r = unityBridgeCommandInputSchema.safeParse({
      kind: "capture_screenshot",
      timeout_ms: 5000,
    });
    assert.equal(r.success, true);
    if (r.success) assert.equal(r.data.include_ui, false);
  });

  it("rejects timeout_ms above 30000", () => {
    const r = unityBridgeCommandInputSchema.safeParse({
      kind: "export_agent_context",
      timeout_ms: 35000,
    });
    assert.equal(r.success, false);
  });

  it("defaults timeout_ms to 30000", () => {
    const r = unityBridgeCommandInputSchema.safeParse({});
    assert.equal(r.success, true);
    if (r.success) assert.equal(r.data.timeout_ms, 30_000);
  });
});

describe("runUnityBridgeGet", () => {
  it("returns db_unconfigured when pool is null", async () => {
    const r = await runUnityBridgeGet(
      { command_id: "550e8400-e29b-41d4-a716-446655440000", wait_ms: 0 },
      { pool: null },
    );
    assert.equal(r.ok, false);
    if (!r.ok) assert.equal(r.error, "db_unconfigured");
  });

  it("returns not_found for missing command_id", async () => {
    const pool = mockPool({
      selectSequence: [null],
    });
    const r = await runUnityBridgeGet(
      { command_id: "550e8400-e29b-41d4-a716-446655440000", wait_ms: 0 },
      { pool },
    );
    assert.equal(r.ok, false);
    if (!r.ok) assert.equal(r.error, "not_found");
  });
});
