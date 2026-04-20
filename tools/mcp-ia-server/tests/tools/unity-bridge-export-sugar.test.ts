/**
 * Bridge export sugar tools — Zod, timeout resolution, enqueue + poll helpers (mock pool only).
 */

import assert from "node:assert/strict";
import { describe, it } from "node:test";
import type { Pool, QueryResult } from "pg";
import {
  resolveExportSugarTimeoutMs,
  EXPORT_SUGAR_DEFAULT_TIMEOUT_MS,
  pollUnityBridgeJobUntilTerminal,
  enqueueUnityBridgeJob,
  unityBridgeCommandInputSchema,
  UNITY_BRIDGE_TIMEOUT_MS_MAX,
  type UnityBridgeResponsePayload,
} from "../../src/tools/unity-bridge-command.js";
import {
  unityExportCellChunkInputSchema,
  unityExportSortingDebugInputSchema,
} from "../../src/tools/unity-bridge-export-sugar.js";

describe("resolveExportSugarTimeoutMs", () => {
  it("uses explicit ms when provided", () => {
    assert.equal(resolveExportSugarTimeoutMs(25_000), 25_000);
  });

  it("clamps explicit ms to policy max", () => {
    assert.equal(resolveExportSugarTimeoutMs(999_000), UNITY_BRIDGE_TIMEOUT_MS_MAX);
  });

  it("defaults when env unset and explicit omitted", () => {
    const prev = process.env.BRIDGE_TIMEOUT_MS;
    delete process.env.BRIDGE_TIMEOUT_MS;
    try {
      assert.equal(resolveExportSugarTimeoutMs(undefined), EXPORT_SUGAR_DEFAULT_TIMEOUT_MS);
    } finally {
      if (prev === undefined) delete process.env.BRIDGE_TIMEOUT_MS;
      else process.env.BRIDGE_TIMEOUT_MS = prev;
    }
  });

  it("reads BRIDGE_TIMEOUT_MS when explicit omitted", () => {
    const prev = process.env.BRIDGE_TIMEOUT_MS;
    process.env.BRIDGE_TIMEOUT_MS = "5000";
    try {
      assert.equal(resolveExportSugarTimeoutMs(undefined), 5000);
    } finally {
      if (prev === undefined) delete process.env.BRIDGE_TIMEOUT_MS;
      else process.env.BRIDGE_TIMEOUT_MS = prev;
    }
  });
});

describe("unity export sugar Zod schemas", () => {
  it("rejects chunk_width below 1", () => {
    const r = unityExportCellChunkInputSchema.safeParse({
      origin_x: 0,
      origin_y: 0,
      chunk_width: 0,
      chunk_height: 8,
    });
    assert.equal(r.success, false);
  });

  it("accepts sorting debug with empty object (defaults)", () => {
    const r = unityExportSortingDebugInputSchema.safeParse({});
    assert.equal(r.success, true);
  });
});

type BridgeSelectRow = {
  status: string;
  response: UnityBridgeResponsePayload | null;
  error: string | null;
  kind: string;
};

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

describe("pollUnityBridgeJobUntilTerminal (sugar path)", () => {
  it("returns unity_failed when job fails", async () => {
    const sampleResponse: UnityBridgeResponsePayload = {
      schema_version: 1,
      artifact: "unity_agent_bridge_response",
      command_id: "x",
      ok: false,
      completed_at_utc: new Date().toISOString(),
      storage: "postgres",
      artifact_paths: [],
      postgres_only: true,
      error: "grid not ready",
    };
    const pool = mockPool({
      selectSequence: [
        {
          status: "failed",
          response: sampleResponse,
          error: "grid not ready",
          kind: "export_cell_chunk",
        },
      ],
    });
    const cmd = "00000000-0000-4000-8000-000000000001";
    const r = await pollUnityBridgeJobUntilTerminal(cmd, 5000, pool);
    assert.equal(r.ok, false);
    if (!r.ok) assert.equal(r.error, "unity_failed");
  });

  it("returns completed when first get sees completed", async () => {
    const sampleResponse: UnityBridgeResponsePayload = {
      schema_version: 1,
      artifact: "unity_agent_bridge_response",
      command_id: "x",
      ok: true,
      completed_at_utc: new Date().toISOString(),
      storage: "postgres",
      artifact_paths: ["/tmp/out.json"],
      postgres_only: true,
      error: null,
    };
    const pool = mockPool({
      selectSequence: [
        {
          status: "completed",
          response: sampleResponse,
          error: null,
          kind: "export_sorting_debug",
        },
      ],
    });
    const cmd = "00000000-0000-4000-8000-000000000002";
    const r = await pollUnityBridgeJobUntilTerminal(cmd, 5000, pool);
    assert.equal(r.ok, true);
    if (r.ok) assert.equal(r.response.artifact_paths?.length, 1);
  });

  it("times out when job stays pending", async () => {
    let deleteCalled = false;
    const pool = mockPool({
      selectSequence: Array.from({ length: 200 }, () => ({
        status: "pending",
        response: null,
        error: null,
        kind: "export_cell_chunk",
      })),
      delete: () => {
        deleteCalled = true;
        return { rows: [], rowCount: 1 };
      },
    });
    const r = await pollUnityBridgeJobUntilTerminal(
      "00000000-0000-4000-8000-000000000099",
      600,
      pool,
    );
    assert.equal(r.ok, false);
    if (!r.ok) assert.equal(r.error, "timeout");
    assert.equal(deleteCalled, true);
  });
});

describe("enqueueUnityBridgeJob + poll (export kinds)", () => {
  it("round-trips export_cell_chunk through enqueue and poll", async () => {
    const body: UnityBridgeResponsePayload = {
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
          response: body,
          error: null,
          kind: "export_cell_chunk",
        },
      ],
    });
    const input = unityBridgeCommandInputSchema.parse({
      kind: "export_cell_chunk",
      origin_x: 1,
      origin_y: 2,
      chunk_width: 4,
      chunk_height: 4,
      timeout_ms: 5000,
    });
    const enq = await enqueueUnityBridgeJob(input, pool);
    assert.equal(enq.ok, true);
    if (!enq.ok) return;
    const fin = await pollUnityBridgeJobUntilTerminal(enq.command_id, 5000, pool);
    assert.equal(fin.ok, true);
  });
});
