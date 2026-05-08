/**
 * Core runtime functions: enqueue, poll, runUnityBridgeCommand, runUnityBridgeGet.
 */

import { randomUUID } from "node:crypto";
import type { Pool } from "pg";
import { getIaDatabasePool } from "../../ia-db/pool.js";
import { BRIDGE_OUTPUT_PREVIEW_MAX, UNITY_BRIDGE_TIMEOUT_MS_MAX } from "./constants.js";
import { buildRequestEnvelope, selectBridgeRow, sleepMs } from "./envelope.js";
import type { UnityBridgeCommandInput } from "./input-schema.js";
import type { UnityBridgeGetInput } from "./get-schema.js";
import type { UnityBridgeResponsePayload } from "./response-types.js";

/** Default wait for export sugar tools when `timeout_ms` omitted and `BRIDGE_TIMEOUT_MS` unset (agent-led verification policy initial). */
export const EXPORT_SUGAR_DEFAULT_TIMEOUT_MS = 40_000;

/**
 * Resolve poll deadline for MCP sugar tools: explicit `timeout_ms`, else `BRIDGE_TIMEOUT_MS` env (same knob as CLI bridge scripts), else {@link EXPORT_SUGAR_DEFAULT_TIMEOUT_MS}.
 */
export function resolveExportSugarTimeoutMs(explicitMs?: number): number {
  if (explicitMs !== undefined && Number.isFinite(explicitMs)) {
    return Math.min(UNITY_BRIDGE_TIMEOUT_MS_MAX, Math.max(1000, explicitMs));
  }
  const envRaw = process.env.BRIDGE_TIMEOUT_MS;
  if (envRaw !== undefined && envRaw !== "") {
    const n = Number(envRaw);
    if (Number.isFinite(n) && n >= 1000) {
      return Math.min(UNITY_BRIDGE_TIMEOUT_MS_MAX, n);
    }
  }
  return EXPORT_SUGAR_DEFAULT_TIMEOUT_MS;
}

export type UnityBridgeCommandRunOptions = {
  /** Test hook: override pool resolution. */
  pool?: Pool | null;
};

/**
 * Insert a pending agent_bridge_job row (shared with {@link runUnityBridgeCommand} and sugar tools).
 */
export async function enqueueUnityBridgeJob(
  input: UnityBridgeCommandInput,
  pool: Pool,
): Promise<
  | { ok: true; command_id: string }
  | { ok: false; error: "db_error"; message: string; command_id: string }
> {
  const commandId = randomUUID();
  const envelope = buildRequestEnvelope(commandId, input);
  try {
    await pool.query(
      `INSERT INTO agent_bridge_job (command_id, kind, status, request, agent_id)
       VALUES ($1::uuid, $2, $3, $4::jsonb, $5)`,
      [commandId, input.kind, "pending", JSON.stringify(envelope), input.agent_id ?? "anonymous"],
    );
    return { ok: true, command_id: commandId };
  } catch (e) {
    return {
      ok: false,
      error: "db_error",
      message: e instanceof Error ? e.message : String(e),
      command_id: commandId,
    };
  }
}

/**
 * Poll `unity_bridge_get` until the job is completed/failed or `timeoutMs` elapses (TECH-572).
 * Uses the same MCP read path as agents that poll by `command_id`.
 */
export async function pollUnityBridgeJobUntilTerminal(
  commandId: string,
  timeoutMs: number,
  pool: Pool,
): Promise<
  | { ok: true; response: UnityBridgeResponsePayload; command_id: string }
  | {
      ok: false;
      error: string;
      message: string;
      command_id?: string;
      last_output_preview?: string;
    }
> {
  const deadline = Date.now() + timeoutMs;
  let lastSnapshot: {
    status: string;
    response: UnityBridgeResponsePayload | null;
    error: string | null;
  } | null = null;

  try {
    while (Date.now() < deadline) {
      const remaining = deadline - Date.now();
      if (remaining <= 0) break;
      const waitSlice = Math.min(10_000, Math.max(1, remaining));
      const get = await runUnityBridgeGet({ command_id: commandId, wait_ms: waitSlice }, { pool });
      if (!get.ok) {
        if (get.error === "not_found") {
          return {
            ok: false,
            error: "job_missing",
            message: "Bridge job row disappeared after enqueue.",
            command_id: commandId,
          };
        }
        return {
          ok: false,
          error: get.error,
          message: get.message,
          command_id: commandId,
        };
      }
      lastSnapshot = {
        status: get.status,
        response: get.response,
        error: get.error,
      };
      if (get.status === "completed") {
        if (!get.response || typeof get.response !== "object") {
          return {
            ok: false,
            error: "invalid_response",
            message: "Completed job has empty or invalid response JSON.",
            command_id: commandId,
          };
        }
        const resp = { ...get.response, command_id: commandId };
        return { ok: true, response: resp as UnityBridgeResponsePayload, command_id: commandId };
      }
      if (get.status === "failed") {
        return {
          ok: false,
          error: "unity_failed",
          message: get.error ?? "Unity marked the bridge job as failed.",
          command_id: commandId,
        };
      }
    }

    const rawPreview = lastSnapshot
      ? (lastSnapshot.error ??
        (lastSnapshot.response ? JSON.stringify(lastSnapshot.response) : ""))
      : "";
    const last_output_preview = rawPreview.slice(0, BRIDGE_OUTPUT_PREVIEW_MAX);

    try {
      await pool.query(
        `DELETE FROM agent_bridge_job WHERE command_id = $1::uuid AND status = 'pending'`,
        [commandId],
      );
    } catch {
      // non-fatal
    }

    return {
      ok: false,
      error: "timeout",
      message:
        "Unity did not complete the bridge job within timeout_ms. Run `npm run unity:ensure-editor` to launch Unity if not running. Ensure Postgres migration 0008 is applied, DATABASE_URL matches Unity, and the Editor is open (AgentBridgeCommandRunner polls via agent-bridge-dequeue.mjs). Pending rows are removed on MCP timeout; if Unity was dequeueing, check for stuck processing rows.",
      command_id: commandId,
      last_output_preview,
    };
  } catch (e) {
    return {
      ok: false,
      error: "db_error",
      message: e instanceof Error ? e.message : String(e),
      command_id: commandId,
    };
  }
}

/**
 * Core logic for MCP **`unity_bridge_command`** (also used by CLI helpers so they do not duplicate
 * the Postgres queue contract): {@link ../../scripts/bridge-playmode-smoke.ts},
 * {@link ../../scripts/run-unity-bridge-once.ts}.
 *
 * Exported for unit tests via {@link UnityBridgeCommandRunOptions.pool}.
 */
export async function runUnityBridgeCommand(
  input: UnityBridgeCommandInput,
  options?: UnityBridgeCommandRunOptions,
): Promise<
  | { ok: true; response: UnityBridgeResponsePayload; command_id: string }
  | {
      ok: false;
      error: string;
      message: string;
      command_id?: string;
      last_output_preview?: string;
    }
> {
  const pool = options?.pool !== undefined ? options.pool : getIaDatabasePool();
  if (!pool) {
    return {
      ok: false,
      error: "db_unconfigured",
      message:
        "No database URL: set DATABASE_URL, add config/postgres-dev.json, or see docs/postgres-ia-dev-setup.md.",
    };
  }

  const timeoutMs = Math.min(
    UNITY_BRIDGE_TIMEOUT_MS_MAX,
    Math.max(1000, input.timeout_ms ?? 30_000),
  );

  const enq = await enqueueUnityBridgeJob(input, pool);
  if (!enq.ok) {
    return {
      ok: false,
      error: enq.error,
      message: enq.message,
      command_id: enq.command_id,
    };
  }

  return pollUnityBridgeJobUntilTerminal(enq.command_id, timeoutMs, pool);
}

/**
 * Read bridge job status (optional short wait).
 */
export async function runUnityBridgeGet(
  input: UnityBridgeGetInput,
  options?: UnityBridgeCommandRunOptions,
): Promise<
  | {
      ok: true;
      command_id: string;
      status: string;
      kind: string;
      response: UnityBridgeResponsePayload | null;
      error: string | null;
    }
  | { ok: false; error: string; message: string }
> {
  const pool = options?.pool !== undefined ? options.pool : getIaDatabasePool();
  if (!pool) {
    return {
      ok: false,
      error: "db_unconfigured",
      message:
        "No database URL: set DATABASE_URL, add config/postgres-dev.json, or see docs/postgres-ia-dev-setup.md.",
    };
  }

  const pollMs = 150;

  try {
    if (input.wait_ms <= 0) {
      const row = await selectBridgeRow(pool, input.command_id);
      if (!row) {
        return {
          ok: false,
          error: "not_found",
          message: `No agent_bridge_job for command_id ${input.command_id}.`,
        };
      }
      return {
        ok: true,
        command_id: input.command_id,
        status: row.status,
        kind: row.kind,
        response: row.response,
        error: row.error,
      };
    }

    const deadline = Date.now() + input.wait_ms;
    while (true) {
      const row = await selectBridgeRow(pool, input.command_id);
      if (!row) {
        return {
          ok: false,
          error: "not_found",
          message: `No agent_bridge_job for command_id ${input.command_id}.`,
        };
      }
      if (row.status === "completed" || row.status === "failed" || Date.now() >= deadline) {
        return {
          ok: true,
          command_id: input.command_id,
          status: row.status,
          kind: row.kind,
          response: row.response,
          error: row.error,
        };
      }
      await sleepMs(pollMs);
    }
  } catch (e) {
    return {
      ok: false,
      error: "db_error",
      message: e instanceof Error ? e.message : String(e),
    };
  }
}
