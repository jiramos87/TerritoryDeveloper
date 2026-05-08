/**
 * MCP tool registrations: unity_bridge_command, unity_bridge_get, unity_compile.
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { getIaDatabasePool } from "../../ia-db/pool.js";
import { runWithToolTiming } from "../../instrumentation.js";
import { wrapTool, dbUnconfiguredError } from "../../envelope.js";
import { unityBridgeTimeoutMsSchema } from "./constants.js";
import { unityBridgeCommandInputSchema } from "./input-schema.js";
import type { UnityBridgeCommandInput } from "./input-schema.js";
import { unityBridgeGetInputSchema } from "./get-schema.js";
import type { UnityBridgeGetInput } from "./get-schema.js";
import { jsonResult } from "./envelope.js";
import { runUnityBridgeCommand, runUnityBridgeGet } from "./run.js";

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
        "IDE agent bridge: enqueue a Unity Editor job in Postgres agent_bridge_job (pending). Kinds: export_agent_context (agent context JSON + optional Postgres registry; optional seed_cell \"x,y\" for Moore neighborhood center), get_console_logs (buffered Console lines in response.log_lines), capture_screenshot (Play Mode PNG under tools/reports/bridge-screenshots/; include_ui for Game view + Overlay UI), enter_play_mode (EditorApplication.EnterPlaymode; completes when GridManager.isInitialized; response.ready, play_mode_state, grid_width/height), exit_play_mode (ExitPlaymode; completes when back in Edit Mode), get_play_mode_status (immediate response: play_mode_state edit_mode|play_mode_loading|play_mode_ready), debug_context_bundle (single job: Moore export + optional screenshot + console + anomaly scan; response.bundle; requires seed_cell; Play Mode + GridManager ready), get_compilation_status (synchronous compile snapshot: response.compilation_status with compiling, compilation_failed, last_error_excerpt, recent_error_messages), economy_balance_snapshot (reads population, happiness, money, tax rates, R/C/I demand in response.economy_snapshot), prefab_manifest (lists scene MonoBehaviours + missing script references in response.prefab_manifest), sorting_order_debug (requires seed_cell; returns SpriteRenderers at cell with sorting_layer/sorting_order in response.sorting_order_debug), catalog_preview (params: catalog_entry_id, include_screenshot; loads draft entity in CatalogPreview.unity via PreviewCatalog component; returns screenshot_path + resolved + entry_id). Mutation kinds (Edit Mode only — TECH-412): attach_component, remove_component, assign_serialized_field, create_gameobject, delete_gameobject, find_gameobject, set_transform, set_gameobject_active, set_gameobject_parent, save_scene, open_scene, new_scene, instantiate_prefab, apply_prefab_overrides, create_scriptable_object, modify_scriptable_object, refresh_asset_database, move_asset, delete_asset, execute_menu_item. Each mutation kind returns response.mutation_result (JSON string with kind-specific fields). Safety: Edit Mode only; each kind validates target existence before mutation; MarkSceneDirty called after scene mutations; AssetDatabase.SaveAssets+Refresh called after asset mutations. Requires DATABASE_URL / config/postgres-dev.json, migration 0008, Unity on REPO_ROOT. Polls until completed, failed, or timeout_ms (default 30000, max 120000). On timeout, run `npm run unity:ensure-editor` then retry with timeout_ms 60000. Removes pending row on MCP timeout.",
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
