/**
 * MCP tools: unity_bridge_command, unity_bridge_get, unity_compile — Postgres-backed IDE agent bridge (agent_bridge_job).
 */

import { randomUUID } from "node:crypto";
import { z } from "zod";
import type { Pool } from "pg";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { getIaDatabasePool } from "../ia-db/pool.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool, dbUnconfiguredError } from "../envelope.js";

/** Max chars for last_output_preview in bridge_timeout details. */
export const BRIDGE_OUTPUT_PREVIEW_MAX = 512;

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
      // ── OBSERVATION — do not modify ───────────────────────────────────────
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
      "export_cell_chunk",
      "export_sorting_debug",
      // ── MUTATION (Edit Mode only) — TECH-412 ─────────────────────────────
      // Component lifecycle
      "attach_component",
      "remove_component",
      "assign_serialized_field",
      // GameObject lifecycle
      "create_gameobject",
      "delete_gameobject",
      "find_gameobject",
      "set_transform",
      "set_gameobject_active",
      "set_gameobject_parent",
      // Scene lifecycle
      "save_scene",
      "open_scene",
      "new_scene",
      // Prefab lifecycle
      "instantiate_prefab",
      "apply_prefab_overrides",
      // Asset lifecycle
      "create_scriptable_object",
      "modify_scriptable_object",
      "refresh_asset_database",
      "move_asset",
      "delete_asset",
      // Catch-all
      "execute_menu_item",
      // ── Game UI bake (Stage 2) ───────────────────────────────────────────
      "bake_ui_from_ir",
    ])
    .default("export_agent_context")
    .describe(
      "Bridge command kind: export_agent_context (Reports → Export Agent Context); get_console_logs (buffered Unity Console); capture_screenshot (Play Mode PNG under tools/reports/bridge-screenshots/); enter_play_mode (Editor enters Play Mode, waits for GridManager.isInitialized); exit_play_mode (Editor exits Play Mode); get_play_mode_status (immediate edit_mode / play_mode_loading / play_mode_ready + optional grid dimensions); debug_context_bundle (one round-trip: Moore export + optional Game-view screenshot + console + anomaly scan; requires seed_cell, Play Mode + initialized GridManager); get_compilation_status (synchronous: EditorApplication.isCompiling, EditorUtility.scriptCompilationFailed, recent Console error lines in response.compilation_status); economy_balance_snapshot (reads population, happiness, money, tax rates, R/C/I demand from EconomyManager/CityStats/DemandManager); prefab_manifest (lists scene MonoBehaviours and detects missing script references); sorting_order_debug (requires seed_cell \"x,y\": returns all SpriteRenderers on a cell with sorting layer/order). Mutation kinds (Edit Mode only — TECH-412): attach_component (params: target_path, component_type_name), remove_component (params: target_path, component_type_name), assign_serialized_field (params: target_path, component_type_name, field_name, value_kind∈object_ref|asset_ref|int|float|bool|string|vector3, value, value_object_path?), create_gameobject (params: name, parent_path?, position?), delete_gameobject (params: target_path), find_gameobject (params: target_path), set_transform (params: target_path, position?, rotation?, scale? as 'x,y,z'), set_gameobject_active (params: target_path, active), set_gameobject_parent (params: target_path, new_parent_path?, world_position_stays), save_scene (params: scene_path?), open_scene (params: scene_path, mode∈single|additive), new_scene (params: setup_mode∈default_game_objects|empty_scene, mode∈single|additive), instantiate_prefab (params: prefab_path, parent_path?, position?), apply_prefab_overrides (params: target_path), create_scriptable_object (params: type_name, asset_path), modify_scriptable_object (params: asset_path, field_writes[]{field_name, value_kind, value, value_object_path?}), refresh_asset_database (no params), move_asset (params: asset_path, new_path), delete_asset (params: asset_path), execute_menu_item (params: menu_path).",
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
      'export_agent_context / export_sorting_debug: Moore neighborhood center as "x,y" (e.g. "3,0"); omit to use selected Cell or (0,0). debug_context_bundle: required "x,y" seed for export + scan.',
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
  // ── export_cell_chunk params ─────────────────────────────────────────────
  origin_x: z
    .number()
    .int()
    .min(0)
    .default(0)
    .describe("export_cell_chunk: origin X (defaults to 0)."),
  origin_y: z
    .number()
    .int()
    .min(0)
    .default(0)
    .describe("export_cell_chunk: origin Y (defaults to 0)."),
  chunk_width: z
    .number()
    .int()
    .min(1)
    .max(128)
    .default(8)
    .describe("export_cell_chunk: chunk width (defaults to 8)."),
  chunk_height: z
    .number()
    .int()
    .min(1)
    .max(128)
    .default(8)
    .describe("export_cell_chunk: chunk height (defaults to 8)."),
  agent_id: z
    .string()
    .optional()
    .describe(
      "Caller identity for audit — use issue id (e.g. 'TECH-121') or session tag. Stored in agent_bridge_job.agent_id. Default 'anonymous'. For Play Mode commands, pair with unity_bridge_lease to prevent concurrent session conflicts.",
    ),
  // ── Mutation params (TECH-412) — Edit Mode only ───────────────────────────
  // Shared across component / GO lifecycle
  target_path: z
    .string()
    .optional()
    .describe(
      "Mutation kinds: scene-root-relative path to the target GameObject (e.g. 'Managers/EconomyManager'). Used by: attach_component, remove_component, assign_serialized_field, delete_gameobject, find_gameobject, set_transform, set_gameobject_active, set_gameobject_parent, apply_prefab_overrides.",
    ),
  component_type_name: z
    .string()
    .optional()
    .describe(
      "attach_component / remove_component / assign_serialized_field: short class name (e.g. 'TreasuryFloorClampService'). Resolved across all loaded assemblies; ambiguity → structured error type_ambiguous with candidate list.",
    ),
  // assign_serialized_field
  field_name: z
    .string()
    .optional()
    .describe("assign_serialized_field / modify_scriptable_object: serialized field name on the component or asset."),
  value_kind: z
    .enum(["object_ref", "asset_ref", "int", "float", "bool", "string", "vector3"])
    .optional()
    .describe(
      "assign_serialized_field: tagged-union discriminant. object_ref = scene GO path in value_object_path; asset_ref = asset path in value_object_path; primitives = string value; vector3 = 'x,y,z' string.",
    ),
  value: z
    .string()
    .optional()
    .describe("assign_serialized_field: primitive value as string (int, float, bool, string, vector3 as 'x,y,z')."),
  value_object_path: z
    .string()
    .optional()
    .describe(
      "assign_serialized_field: for object_ref — scene-root-relative GO path; for asset_ref — asset database path (e.g. 'Assets/Prefabs/Foo.prefab').",
    ),
  // create_gameobject
  go_name: z
    .string()
    .optional()
    .describe("create_gameobject: name for the new GameObject."),
  parent_path: z
    .string()
    .optional()
    .describe("create_gameobject / instantiate_prefab: scene path of the parent GO; omit for scene root."),
  position: z
    .string()
    .optional()
    .describe("create_gameobject / instantiate_prefab / set_transform: position as 'x,y,z'."),
  // set_transform
  rotation: z
    .string()
    .optional()
    .describe("set_transform: Euler rotation as 'x,y,z'."),
  scale: z
    .string()
    .optional()
    .describe("set_transform: local scale as 'x,y,z'."),
  // set_gameobject_active
  active: z
    .boolean()
    .optional()
    .describe("set_gameobject_active: true to activate, false to deactivate."),
  // set_gameobject_parent
  new_parent_path: z
    .string()
    .optional()
    .describe("set_gameobject_parent: scene path of the new parent; omit to reparent to scene root."),
  world_position_stays: z
    .boolean()
    .optional()
    .describe("set_gameobject_parent: when true, world position is preserved during reparent."),
  // Scene lifecycle
  scene_path: z
    .string()
    .optional()
    .describe("save_scene / open_scene: scene asset path (e.g. 'Assets/Scenes/MainScene.unity'). Omit = active scene."),
  scene_mode: z
    .enum(["single", "additive"])
    .optional()
    .describe("open_scene / new_scene: 'single' (default) replaces current; 'additive' adds alongside."),
  setup_mode: z
    .enum(["default_game_objects", "empty_scene"])
    .optional()
    .describe("new_scene: 'default_game_objects' (camera + directional light) or 'empty_scene'."),
  // Prefab lifecycle
  prefab_path: z
    .string()
    .optional()
    .describe("instantiate_prefab: asset database path to the prefab (e.g. 'Assets/Prefabs/Foo.prefab')."),
  // Asset lifecycle
  type_name: z
    .string()
    .optional()
    .describe("create_scriptable_object: short class name of the ScriptableObject subtype."),
  asset_path: z
    .string()
    .optional()
    .describe(
      "create_scriptable_object / modify_scriptable_object / move_asset / delete_asset: asset database path (e.g. 'Assets/Data/Config.asset').",
    ),
  new_path: z
    .string()
    .optional()
    .describe("move_asset: destination asset path."),
  field_writes: z
    .array(
      z.object({
        field_name: z.string(),
        value_kind: z.enum(["object_ref", "asset_ref", "int", "float", "bool", "string", "vector3"]),
        value: z.string().optional(),
        value_object_path: z.string().optional(),
      }),
    )
    .optional()
    .describe("modify_scriptable_object: array of field writes to apply to the asset."),
  // Catch-all
  menu_path: z
    .string()
    .optional()
    .describe("execute_menu_item: Unity Editor menu path (e.g. 'Assets/Refresh'). Unresolved path → menu_not_found."),
  // ── Game UI bake (Stage 2) ─────────────────────────────────────────────
  ir_path: z
    .string()
    .optional()
    .describe("bake_ui_from_ir: repo-relative path to IR JSON from transcribe:cd-game-ui."),
  out_dir: z
    .string()
    .optional()
    .describe("bake_ui_from_ir: repo-relative output dir for placeholder prefabs (default 'Assets/UI/Prefabs/Generated')."),
  theme_so: z
    .string()
    .optional()
    .describe("bake_ui_from_ir: repo-relative path to UiTheme ScriptableObject asset (default 'Assets/UI/Theme/DefaultUiTheme.asset')."),
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
    agent_id: input.agent_id ?? "anonymous",
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
  if (input.kind === "export_cell_chunk") {
    return {
      ...base,
      params: {
        origin_x: input.origin_x ?? 0,
        origin_y: input.origin_y ?? 0,
        chunk_width: input.chunk_width ?? 8,
        chunk_height: input.chunk_height ?? 8,
      },
    };
  }
  if (input.kind === "export_sorting_debug") {
    const trimmed = input.seed_cell?.trim();
    const params: Record<string, unknown> =
      trimmed && trimmed.length > 0 ? { seed_cell: trimmed } : {};
    return { ...base, params };
  }
  // ── Mutation kinds (TECH-412) ─────────────────────────────────────────────
  if (input.kind === "attach_component") {
    return { ...base, params: { target_path: input.target_path ?? "", component_type_name: input.component_type_name ?? "" } };
  }
  if (input.kind === "remove_component") {
    return { ...base, params: { target_path: input.target_path ?? "", component_type_name: input.component_type_name ?? "" } };
  }
  if (input.kind === "assign_serialized_field") {
    return {
      ...base,
      params: {
        target_path: input.target_path ?? "",
        component_type_name: input.component_type_name ?? "",
        field_name: input.field_name ?? "",
        value_kind: input.value_kind ?? "",
        value: input.value ?? "",
        value_object_path: input.value_object_path ?? null,
      },
    };
  }
  if (input.kind === "create_gameobject") {
    return { ...base, params: { name: input.go_name ?? "", parent_path: input.parent_path ?? null, position: input.position ?? null } };
  }
  if (input.kind === "delete_gameobject") {
    return { ...base, params: { target_path: input.target_path ?? "" } };
  }
  if (input.kind === "find_gameobject") {
    return { ...base, params: { target_path: input.target_path ?? "" } };
  }
  if (input.kind === "set_transform") {
    return {
      ...base,
      params: {
        target_path: input.target_path ?? "",
        position: input.position ?? null,
        rotation: input.rotation ?? null,
        scale: input.scale ?? null,
      },
    };
  }
  if (input.kind === "set_gameobject_active") {
    return { ...base, params: { target_path: input.target_path ?? "", active: input.active ?? true } };
  }
  if (input.kind === "set_gameobject_parent") {
    return {
      ...base,
      params: {
        target_path: input.target_path ?? "",
        new_parent_path: input.new_parent_path ?? null,
        world_position_stays: input.world_position_stays ?? false,
      },
    };
  }
  if (input.kind === "save_scene") {
    return { ...base, params: { scene_path: input.scene_path ?? null } };
  }
  if (input.kind === "open_scene") {
    return { ...base, params: { scene_path: input.scene_path ?? "", mode: input.scene_mode ?? "single" } };
  }
  if (input.kind === "new_scene") {
    return { ...base, params: { setup_mode: input.setup_mode ?? "default_game_objects", mode: input.scene_mode ?? "single" } };
  }
  if (input.kind === "instantiate_prefab") {
    return { ...base, params: { prefab_path: input.prefab_path ?? "", parent_path: input.parent_path ?? null, position: input.position ?? null } };
  }
  if (input.kind === "apply_prefab_overrides") {
    return { ...base, params: { target_path: input.target_path ?? "" } };
  }
  if (input.kind === "create_scriptable_object") {
    return { ...base, params: { type_name: input.type_name ?? "", asset_path: input.asset_path ?? "" } };
  }
  if (input.kind === "modify_scriptable_object") {
    return { ...base, params: { asset_path: input.asset_path ?? "", field_writes: input.field_writes ?? [] } };
  }
  if (input.kind === "refresh_asset_database") {
    return { ...base, params: {} };
  }
  if (input.kind === "move_asset") {
    return { ...base, params: { asset_path: input.asset_path ?? "", new_path: input.new_path ?? "" } };
  }
  if (input.kind === "delete_asset") {
    return { ...base, params: { asset_path: input.asset_path ?? "" } };
  }
  if (input.kind === "execute_menu_item") {
    return { ...base, params: { menu_path: input.menu_path ?? "" } };
  }
  if (input.kind === "bake_ui_from_ir") {
    return {
      ...base,
      params: {
        ir_path: input.ir_path ?? "",
        out_dir: input.out_dir ?? "",
        theme_so: input.theme_so ?? "",
      },
    };
  }
  return { ...base, params: {} };
}

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

/**
 * Register unity_bridge_command, unity_bridge_get, and unity_compile (alias for get_compilation_status).
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
        "IDE agent bridge: enqueue a Unity Editor job in Postgres agent_bridge_job (pending). Kinds: export_agent_context (agent context JSON + optional Postgres registry; optional seed_cell \"x,y\" for Moore neighborhood center), get_console_logs (buffered Console lines in response.log_lines), capture_screenshot (Play Mode PNG under tools/reports/bridge-screenshots/; include_ui for Game view + Overlay UI), enter_play_mode (EditorApplication.EnterPlaymode; completes when GridManager.isInitialized; response.ready, play_mode_state, grid_width/height), exit_play_mode (ExitPlaymode; completes when back in Edit Mode), get_play_mode_status (immediate response: play_mode_state edit_mode|play_mode_loading|play_mode_ready), debug_context_bundle (single job: Moore export + optional screenshot + console + anomaly scan; response.bundle; requires seed_cell; Play Mode + GridManager ready), get_compilation_status (synchronous compile snapshot: response.compilation_status with compiling, compilation_failed, last_error_excerpt, recent_error_messages), economy_balance_snapshot (reads population, happiness, money, tax rates, R/C/I demand in response.economy_snapshot), prefab_manifest (lists scene MonoBehaviours + missing script references in response.prefab_manifest), sorting_order_debug (requires seed_cell; returns SpriteRenderers at cell with sorting_layer/sorting_order in response.sorting_order_debug). Mutation kinds (Edit Mode only — TECH-412): attach_component, remove_component, assign_serialized_field, create_gameobject, delete_gameobject, find_gameobject, set_transform, set_gameobject_active, set_gameobject_parent, save_scene, open_scene, new_scene, instantiate_prefab, apply_prefab_overrides, create_scriptable_object, modify_scriptable_object, refresh_asset_database, move_asset, delete_asset, execute_menu_item. Each mutation kind returns response.mutation_result (JSON string with kind-specific fields). Safety: Edit Mode only; each kind validates target existence before mutation; MarkSceneDirty called after scene mutations; AssetDatabase.SaveAssets+Refresh called after asset mutations. Requires DATABASE_URL / config/postgres-dev.json, migration 0008, Unity on REPO_ROOT. Polls until completed, failed, or timeout_ms (default 30000, max 120000). On timeout, run `npm run unity:ensure-editor` then retry with timeout_ms 60000. Removes pending row on MCP timeout.",
      // Full Zod object (not a raw shape) so @modelcontextprotocol/sdk JSON Schema matches
      // unityBridgeCommandInputSchema.safeParse in the handler (timeout_ms max = UNITY_BRIDGE_TIMEOUT_MS_MAX).
      inputSchema: unityBridgeCommandInputSchema,
    },
    async (args) =>
      runWithToolTiming("unity_bridge_command", async () => {
        const envelope = await wrapTool(async (input: UnityBridgeCommandInput) => {
          const pool = getIaDatabasePool();
          if (!pool) throw dbUnconfiguredError();

          const result = await runUnityBridgeCommand(input, { pool });
          if (!result.ok) {
            if (result.error === "timeout") {
              throw {
                code: "bridge_timeout" as const,
                message: result.message,
                hint: "Run `npm run unity:ensure-editor` then retry with timeout_ms 60000.",
                details: {
                  command_id: result.command_id,
                  last_output_preview: result.last_output_preview ?? "",
                },
              };
            }
            throw { code: result.error as string, message: result.message, details: result.command_id ? { command_id: result.command_id } : undefined };
          }
          return result.response;
        })(unityBridgeCommandInputSchema.parse(args ?? {}));
        return jsonResult(envelope);
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
        const envelope = await wrapTool(async (input: UnityBridgeGetInput) => {
          const pool = getIaDatabasePool();
          if (!pool) throw dbUnconfiguredError();

          const result = await runUnityBridgeGet(input, { pool });
          if (!result.ok) {
            throw { code: result.error as string, message: result.message };
          }
          return result;
        })(unityBridgeGetInputSchema.parse(args ?? {}));
        return jsonResult(envelope);
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
        const envelope = await wrapTool(async (input: { timeout_ms?: number }) => {
          const pool = getIaDatabasePool();
          if (!pool) throw dbUnconfiguredError();

          const bridgeInput = unityBridgeCommandInputSchema.parse({
            kind: "get_compilation_status",
            timeout_ms: input.timeout_ms,
          });
          const result = await runUnityBridgeCommand(bridgeInput, { pool });
          if (!result.ok) {
            if (result.error === "timeout") {
              throw {
                code: "bridge_timeout" as const,
                message: result.message,
                hint: "Run `npm run unity:ensure-editor` then retry with timeout_ms 60000.",
                details: {
                  command_id: result.command_id,
                  last_output_preview: result.last_output_preview ?? "",
                },
              };
            }
            throw { code: result.error as string, message: result.message, details: result.command_id ? { command_id: result.command_id } : undefined };
          }
          return result.response;
        })(unityCompileInputSchema.parse(args ?? {}));
        return jsonResult(envelope);
      }),
  );
}
