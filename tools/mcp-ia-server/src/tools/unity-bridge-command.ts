/**
 * MCP tools: unity_bridge_command, unity_bridge_get, unity_compile — Postgres-backed IDE agent bridge (agent_bridge_job).
 */

import { randomUUID } from "node:crypto";
import { z } from "zod";
import type { Pool } from "pg";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { getIaDatabasePool } from "../ia-db/pool.js";
import { runWithToolTiming } from "../instrumentation.js";

/** Upper bound for `timeout_ms` on `unity_bridge_command` / `unity_compile`. Agents use 40s initial + escalation protocol (see docs/agent-led-verification-policy.md). */
export const UNITY_BRIDGE_TIMEOUT_MS_MAX = 120_000;

/** Exported for `unity_compile` and unit tests. */
export const unityBridgeTimeoutMsSchema = z
  .number()
  .int()
  .min(1000)
  .max(UNITY_BRIDGE_TIMEOUT_MS_MAX)
  .default(30_000)
  .describe(
    "Max time to wait for Unity to dequeue, run the command, and complete the job row (requires Postgres + Unity on REPO_ROOT). Capped at 120s; default 30s. Agents: use 40s initial, then escalation protocol (npm run unity:ensure-editor + retry 60s). Deferred ScreenCapture completes within ~15s on the Unity side when healthy.",
  );

const unityBridgeCommandInputShape = {
  kind: z
    .enum([
      "export_agent_context",
      "get_console_logs",
      "capture_screenshot",
      "enter_play_mode",
      "exit_play_mode",
      "get_play_mode_status",
      "debug_context_bundle",
      "get_compilation_status",
      "economy_balance_snapshot",
      "prefab_manifest",
      "sorting_order_debug",
    ])
    .default("export_agent_context")
    .describe(
      "Bridge command kind: export_agent_context (Reports → Export Agent Context); get_console_logs (buffered Unity Console); capture_screenshot (Play Mode PNG under tools/reports/bridge-screenshots/); enter_play_mode (Editor enters Play Mode, waits for GridManager.isInitialized); exit_play_mode (Editor exits Play Mode); get_play_mode_status (immediate edit_mode / play_mode_loading / play_mode_ready + optional grid dimensions); debug_context_bundle (one round-trip: Moore export + optional Game-view screenshot + console + anomaly scan; requires seed_cell, Play Mode + initialized GridManager); get_compilation_status (synchronous: EditorApplication.isCompiling, EditorUtility.scriptCompilationFailed, recent Console error lines in response.compilation_status); economy_balance_snapshot (reads population, happiness, money, tax rates, R/C/I demand from EconomyManager/CityStats/DemandManager); prefab_manifest (lists scene MonoBehaviours and detects missing script references); sorting_order_debug (requires seed_cell \"x,y\": returns all SpriteRenderers on a cell with sorting layer/order).",
    ),
  timeout_ms: unityBridgeTimeoutMsSchema,
  since_utc: z
    .string()
    .optional()
    .describe(
      "get_console_logs only: ISO-8601 UTC lower bound; omit for entire buffer since domain reload.",
    ),
  severity_filter: z
    .enum(["all", "log", "warning", "error"])
    .default("all")
    .describe("get_console_logs only: filter by Unity log type."),
  tag_filter: z
    .string()
    .optional()
    .describe(
      "get_console_logs only: case-insensitive substring match on message or stack.",
    ),
  max_lines: z
    .number()
    .int()
    .min(1)
    .max(2000)
    .default(200)
    .describe("get_console_logs only: max lines returned (newest matching first)."),
  camera: z
    .string()
    .optional()
    .describe(
      "capture_screenshot only: GameObject name of a Camera; omit for full game view capture.",
    ),
  filename_stem: z
    .string()
    .optional()
    .describe(
      "capture_screenshot only: sanitized stem; default screenshot-{utc} if omitted.",
    ),
  include_ui: z
    .boolean()
    .default(false)
    .describe(
      "capture_screenshot only: when true, use ScreenCapture of the Game view (includes Screen Space - Overlay UI). When false (default), prefer Camera render (world / Camera-mode UI only). Ignores camera when true.",
    ),
  seed_cell: z
    .string()
    .optional()
    .describe(
      'export_agent_context: Moore neighborhood center as "x,y" (e.g. "3,0"); omit to use selected Cell or (0,0). debug_context_bundle: required "x,y" seed for export + scan.',
    ),
  include_screenshot: z
    .boolean()
    .default(true)
    .describe(
      "debug_context_bundle only: when false, skip Game view PNG (bundle.screenshot.skipped true). Default true.",
    ),
  include_console: z
    .boolean()
    .default(true)
    .describe(
      "debug_context_bundle only: when false, skip console snapshot (bundle.console.skipped true). Default true.",
    ),
  include_anomaly_scan: z
    .boolean()
    .default(true)
    .describe(
      "debug_context_bundle only: when false, skip Moore neighborhood anomaly rules (bundle.anomaly_scan_skipped true). Default true.",
    ),
};

/** Exported for unit tests (Zod validation of MCP arguments). */
export const unityBridgeCommandInputSchema = z
  .object(unityBridgeCommandInputShape)
  .superRefine((data, ctx) => {
    if (data.kind === "debug_context_bundle") {
      const s = data.seed_cell?.trim();
      if (!s) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: 'seed_cell is required for debug_context_bundle (e.g. "62,0").',
          path: ["seed_cell"],
        });
      }
    }
    if (data.kind === "sorting_order_debug") {
      const s = data.seed_cell?.trim();
      if (!s) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: 'seed_cell is required for sorting_order_debug (e.g. "3,0").',
          path: ["seed_cell"],
        });
      }
    }
  });

export type UnityBridgeCommandInput = z.infer<typeof unityBridgeCommandInputSchema>;

export type UnityBridgeLogLine = {
  timestamp_utc: string;
  severity: string;
  message: string;
  stack: string;
};

export type UnityBridgeResponsePayload = {
  schema_version: number;
  artifact: string;
  command_id: string;
  ok: boolean;
  completed_at_utc: string;
  storage: string;
  artifact_paths: string[];
  postgres_only: boolean;
  error: string | null;
  log_lines?: UnityBridgeLogLine[];
  /** Populated for enter_play_mode / exit_play_mode / get_play_mode_status (Unity AgentBridgeCommandRunner). */
  play_mode_state?: string;
  ready?: boolean;
  already_playing?: boolean;
  already_stopped?: boolean;
  has_grid_dimensions?: boolean;
  grid_width?: number;
  grid_height?: number;
  /** Populated for debug_context_bundle (Unity AgentBridgeCommandRunner). */
  bundle?: {
    cell_export: { artifact_path: string; ok: boolean };
    screenshot: { artifact_path: string; ok: boolean; skipped: boolean };
    console: {
      log_lines: UnityBridgeLogLine[];
      line_count: number;
      skipped: boolean;
    };
    anomalies: Array<{
      rule: string;
      cell_x: number;
      cell_y: number;
      severity: string;
      message: string;
    }>;
    anomaly_count: number;
    anomaly_scan_skipped: boolean;
  };
  /** Populated for get_compilation_status (Unity AgentBridgeCommandRunner). */
  compilation_status?: {
    compiling: boolean;
    compilation_failed: boolean;
    last_error_excerpt: string;
    recent_error_messages: UnityBridgeLogLine[];
  };
};

const getInputShape = {
  command_id: z.string().uuid().describe("Bridge job id returned by unity_bridge_command or dequeue."),
  wait_ms: z
    .number()
    .int()
    .min(0)
    .max(10_000)
    .default(0)
    .describe(
      "Optional blocking wait: poll every ~150ms until status is completed or failed, or wait_ms elapses (0 = single read).",
    ),
};

/** Exported for tests and IA tooling that mirror MCP `unity_bridge_get` inputSchema. */
export const unityBridgeGetInputSchema = z.object(getInputShape);

export type UnityBridgeGetInput = z.infer<typeof unityBridgeGetInputSchema>;

function sleepMs(ms: number): Promise<void> {
  return new Promise((r) => setTimeout(r, ms));
}

function jsonResult(payload: unknown) {
  return {
    content: [
      {
        type: "text" as const,
        text: JSON.stringify(payload, null, 2),
      },
    ],
  };
}

type BridgeRow = {
  status: string;
  response: UnityBridgeResponsePayload | null;
  error: string | null;
  kind: string;
};

async function selectBridgeRow(
  pool: Pool,
  commandId: string,
): Promise<BridgeRow | null> {
  const { rows } = await pool.query<{
    status: string;
    response: UnityBridgeResponsePayload | null;
    error: string | null;
    kind: string;
  }>(
    `SELECT status, response, error, kind FROM agent_bridge_job WHERE command_id = $1::uuid`,
    [commandId],
  );
  return rows[0] ?? null;
}

export type UnityBridgeCommandRunOptions = {
  /** Test hook: override pool resolution. */
  pool?: Pool | null;
};

function buildRequestEnvelope(
  commandId: string,
  input: UnityBridgeCommandInput,
): Record<string, unknown> {
  const base = {
    schema_version: 1,
    artifact: "unity_agent_bridge_command",
    command_id: commandId,
    requested_at_utc: new Date().toISOString(),
    kind: input.kind,
  };
  if (input.kind === "export_agent_context") {
    const trimmed = input.seed_cell?.trim();
    const params =
      trimmed && trimmed.length > 0 ? { seed_cell: trimmed } : {};
    return { ...base, params };
  }
  if (input.kind === "get_console_logs") {
    return {
      ...base,
      params: {
        since_utc: input.since_utc ?? null,
        severity_filter: input.severity_filter,
        tag_filter: input.tag_filter ?? null,
        max_lines: input.max_lines,
      },
    };
  }
  if (input.kind === "capture_screenshot") {
    return {
      ...base,
      params: {
        camera: input.camera ?? null,
        filename_stem: input.filename_stem ?? null,
        include_ui: input.include_ui,
      },
    };
  }
  if (input.kind === "debug_context_bundle") {
    const trimmed = input.seed_cell?.trim() ?? "";
    return {
      ...base,
      params: {
        seed_cell: trimmed,
        include_screenshot: input.include_screenshot,
        include_console: input.include_console,
        include_anomaly_scan: input.include_anomaly_scan,
        filename_stem: input.filename_stem ?? null,
        since_utc: input.since_utc ?? null,
        severity_filter: input.severity_filter,
        tag_filter: input.tag_filter ?? null,
        max_lines: input.max_lines,
      },
    };
  }
  if (input.kind === "get_compilation_status") {
    return { ...base, params: {} };
  }
  if (input.kind === "economy_balance_snapshot") {
    return { ...base, params: {} };
  }
  if (input.kind === "prefab_manifest") {
    return { ...base, params: {} };
  }
  if (input.kind === "sorting_order_debug") {
    const trimmed = input.seed_cell?.trim();
    return { ...base, params: { seed_cell: trimmed ?? "" } };
  }
  return { ...base, params: {} };
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

  const commandId = randomUUID();
  const envelope = buildRequestEnvelope(commandId, input);

  const timeoutMs = Math.min(
    UNITY_BRIDGE_TIMEOUT_MS_MAX,
    Math.max(1000, input.timeout_ms ?? 30_000),
  );

  try {
    await pool.query(
      `INSERT INTO agent_bridge_job (command_id, kind, status, request)
       VALUES ($1::uuid, $2, $3, $4::jsonb)`,
      [commandId, input.kind, "pending", JSON.stringify(envelope)],
    );
  } catch (e) {
    return {
      ok: false,
      error: "db_error",
      message: e instanceof Error ? e.message : String(e),
      command_id: commandId,
    };
  }

  const deadline = Date.now() + timeoutMs;
  const pollMs = 150;

  try {
    while (Date.now() < deadline) {
      const row = await selectBridgeRow(pool, commandId);
      if (!row) {
        return {
          ok: false,
          error: "job_missing",
          message: "Bridge job row disappeared after insert.",
          command_id: commandId,
        };
      }
      if (row.status === "completed") {
        if (!row.response || typeof row.response !== "object") {
          return {
            ok: false,
            error: "invalid_response",
            message: "Completed job has empty or invalid response JSON.",
            command_id: commandId,
          };
        }
        const resp = { ...row.response, command_id: commandId };
        return { ok: true, response: resp as UnityBridgeResponsePayload, command_id: commandId };
      }
      if (row.status === "failed") {
        return {
          ok: false,
          error: "unity_failed",
          message: row.error ?? "Unity marked the bridge job as failed.",
          command_id: commandId,
        };
      }
      await sleepMs(pollMs);
    }

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

/**
 * Register unity_bridge_command and unity_bridge_get.
 */
const unityCompileInputShape = {
  timeout_ms: unityBridgeTimeoutMsSchema,
};

/** Exported for tests and IA tooling that mirror MCP `unity_compile` inputSchema. */
export const unityCompileInputSchema = z.object(unityCompileInputShape);

/**
 * Register unity_bridge_command, unity_bridge_get, and unity_compile (alias for get_compilation_status).
 */
export function registerUnityBridgeCommand(server: McpServer): void {
  server.registerTool(
    "unity_bridge_command",
    {
      description:
        "IDE agent bridge: enqueue a Unity Editor job in Postgres agent_bridge_job (pending). Kinds: export_agent_context (agent context JSON + optional Postgres registry; optional seed_cell \"x,y\" for Moore neighborhood center), get_console_logs (buffered Console lines in response.log_lines), capture_screenshot (Play Mode PNG under tools/reports/bridge-screenshots/; include_ui for Game view + Overlay UI), enter_play_mode (EditorApplication.EnterPlaymode; completes when GridManager.isInitialized; response.ready, play_mode_state, grid_width/height), exit_play_mode (ExitPlaymode; completes when back in Edit Mode), get_play_mode_status (immediate response: play_mode_state edit_mode|play_mode_loading|play_mode_ready), debug_context_bundle (single job: Moore export + optional screenshot + console + anomaly scan; response.bundle; requires seed_cell; Play Mode + GridManager ready), get_compilation_status (synchronous compile snapshot: response.compilation_status with compiling, compilation_failed, last_error_excerpt, recent_error_messages), economy_balance_snapshot (reads population, happiness, money, tax rates, R/C/I demand in response.economy_snapshot), prefab_manifest (lists scene MonoBehaviours + missing script references in response.prefab_manifest), sorting_order_debug (requires seed_cell; returns SpriteRenderers at cell with sorting_layer/sorting_order in response.sorting_order_debug). Requires DATABASE_URL / config/postgres-dev.json, migration 0008, Unity on REPO_ROOT. Polls until completed, failed, or timeout_ms (default 30000, max 120000). On timeout, run `npm run unity:ensure-editor` then retry with timeout_ms 60000. Removes pending row on MCP timeout.",
      // Full Zod object (not a raw shape) so @modelcontextprotocol/sdk JSON Schema matches
      // unityBridgeCommandInputSchema.safeParse in the handler (timeout_ms max = UNITY_BRIDGE_TIMEOUT_MS_MAX).
      inputSchema: unityBridgeCommandInputSchema,
    },
    async (args) =>
      runWithToolTiming("unity_bridge_command", async () => {
        const parsed = unityBridgeCommandInputSchema.safeParse(args ?? {});
        if (!parsed.success) {
          return jsonResult({
            error: "invalid_input",
            message: parsed.error.flatten().fieldErrors,
          });
        }

        const result = await runUnityBridgeCommand(parsed.data);
        if (!result.ok) {
          return jsonResult(result);
        }
        return jsonResult(result.response);
      }),
  );

  server.registerTool(
    "unity_bridge_get",
    {
      description:
        "IDE agent bridge: read agent_bridge_job by command_id (from unity_bridge_command). Default: single SELECT. With wait_ms > 0, blocks until completed/failed or wait_ms elapses. Returns status, kind, response JSON, and error text.",
      inputSchema: unityBridgeGetInputSchema,
    },
    async (args) =>
      runWithToolTiming("unity_bridge_get", async () => {
        const parsed = unityBridgeGetInputSchema.safeParse(args ?? {});
        if (!parsed.success) {
          return jsonResult({
            error: "invalid_input",
            message: parsed.error.flatten().fieldErrors,
          });
        }

        const result = await runUnityBridgeGet(parsed.data);
        if (!result.ok) {
          return jsonResult(result);
        }
        return jsonResult(result);
      }),
  );

  server.registerTool(
    "unity_compile",
    {
      description:
        "IDE agent bridge shortcut: same enqueue/complete path as unity_bridge_command with kind get_compilation_status. Returns response.compilation_status (compiling, compilation_failed, last_error_excerpt, recent_error_messages from buffered Console errors). Use when the Editor is open on REPO_ROOT; prefer npm run unity:compile-check (batchmode) only when no Editor holds the project lock. Requires DATABASE_URL, migration 0008, timeout_ms default 30000, max 120000.",
      inputSchema: unityCompileInputSchema,
    },
    async (args) =>
      runWithToolTiming("unity_compile", async () => {
        const parsed = unityCompileInputSchema.safeParse(args ?? {});
        if (!parsed.success) {
          return jsonResult({
            error: "invalid_input",
            message: parsed.error.flatten().fieldErrors,
          });
        }

        const result = await runUnityBridgeCommand({
          kind: "get_compilation_status",
          timeout_ms: parsed.data.timeout_ms,
        });
        if (!result.ok) {
          return jsonResult(result);
        }
        return jsonResult(result.response);
      }),
  );
}
