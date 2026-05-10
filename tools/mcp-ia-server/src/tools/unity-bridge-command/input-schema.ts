/**
 * Zod input schema + type for unity_bridge_command.
 */

import { z } from "zod";
import { unityBridgeTimeoutMsSchema } from "./constants.js";

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
      "catalog_preview",
      "prefab_inspect",
      "ui_tree_walk",
      "claude_design_conformance",
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
      // ── Game UI runtime (Stage 12 Step 16.10) — Play Mode allowed ────────
      "set_panel_visible",
      // ── Scene mutation (Stage 9.14 / TECH-22667) ─────────────────────────
      "scene_replace_with_prefab",
      // ── Bake pipeline hardening (Stage 1 / bake-pipeline-hardening) ──────
      "validate_panel_blueprint",
    ])
    .default("export_agent_context")
    .describe(
      "Bridge command kind: export_agent_context (Reports → Export Agent Context); get_console_logs (buffered Unity Console); capture_screenshot (Play Mode PNG under tools/reports/bridge-screenshots/); enter_play_mode (Editor enters Play Mode, waits for GridManager.isInitialized); exit_play_mode (Editor exits Play Mode); get_play_mode_status (immediate edit_mode / play_mode_loading / play_mode_ready + optional grid dimensions); debug_context_bundle (one round-trip: Moore export + optional Game-view screenshot + console + anomaly scan; requires seed_cell, Play Mode + initialized GridManager); get_compilation_status (synchronous: EditorApplication.isCompiling, EditorUtility.scriptCompilationFailed, recent Console error lines in response.compilation_status); economy_balance_snapshot (reads population, happiness, money, tax rates, R/C/I demand from EconomyManager/CityStats/DemandManager); prefab_manifest (lists scene MonoBehaviours and detects missing script references); sorting_order_debug (requires seed_cell \"x,y\": returns all SpriteRenderers on a cell with sorting layer/order); catalog_preview (params: catalog_entry_id, include_screenshot — loads draft catalog entity in sandboxed CatalogPreview.unity scene via PreviewCatalog component; returns screenshot_path when include_screenshot true); validate_panel_blueprint (params: panel_id — reads catalog row + asserts required params_json keys against tools/blueprints/panel-schema.yaml; returns {ok, missing}; UiBakeHandler pre-flight calls before bake). Mutation kinds (Edit Mode only — TECH-412): attach_component, remove_component, assign_serialized_field, create_gameobject, delete_gameobject, find_gameobject, set_transform, set_gameobject_active, set_gameobject_parent, save_scene, open_scene, new_scene, instantiate_prefab, apply_prefab_overrides, create_scriptable_object, modify_scriptable_object, refresh_asset_database, move_asset, delete_asset, execute_menu_item. Each mutation kind returns response.mutation_result (JSON string with kind-specific fields). Safety: Edit Mode only; each kind validates target existence before mutation; MarkSceneDirty called after scene mutations; AssetDatabase.SaveAssets+Refresh called after asset mutations. Requires DATABASE_URL / config/postgres-dev.json, migration 0008, Unity on REPO_ROOT. Polls until completed, failed, or timeout_ms (default 30000, max 120000). On timeout, run `npm run unity:ensure-editor` then retry with timeout_ms 60000. Removes pending row on MCP timeout.",
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
      "debug_context_bundle / catalog_preview: when false, skip screenshot capture. Default true.",
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
  // ── catalog_preview params ───────────────────────────────────────────────
  catalog_entry_id: z
    .string()
    .optional()
    .describe(
      "catalog_preview: catalog entity id to preview (maps to catalog_entity.entity_id UUID).",
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
    .enum(["object_ref", "component_ref", "asset_ref", "int", "float", "bool", "string", "vector3"])
    .optional()
    .describe(
      "assign_serialized_field: tagged-union discriminant. object_ref = scene GO path in value_object_path; component_ref = scene GO path in value_object_path + short component type name in value; asset_ref = asset path in value_object_path; primitives = string value; vector3 = 'x,y,z' string.",
    ),
  value: z
    .string()
    .optional()
    .describe(
      "primitive value as string (int/float/bool/string; vector3 'x,y,z'); short component type name when value_kind=component_ref.",
    ),
  value_object_path: z
    .string()
    .optional()
    .describe(
      "assign_serialized_field: for object_ref / component_ref — scene-root-relative GO path; for asset_ref — asset database path (e.g. 'Assets/Prefabs/Foo.prefab').",
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
    .describe("save_scene / open_scene: scene asset path (e.g. 'Assets/Scenes/CityScene.unity'). Omit = active scene."),
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
    .describe("Prefab asset path for instantiate/inspect/conformance/replace ops (e.g. 'Assets/Prefabs/Foo.prefab')."),
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
        value_kind: z.enum(["object_ref", "component_ref", "asset_ref", "int", "float", "bool", "string", "vector3"]),
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
  // ── Game UI runtime (Stage 12 Step 16.10) ──────────────────────────────
  slug: z
    .string()
    .optional()
    .describe("set_panel_visible: ThemedPanel slug (matches GameObject root name baked from IR panel.slug)."),
  // ── Game UI bake (Stage 2) ─────────────────────────────────────────────
  ir_path: z
    .string()
    .optional()
    .describe("bake_ui_from_ir / claude_design_conformance: repo-relative path to IR JSON from transcribe:cd-game-ui."),
  out_dir: z
    .string()
    .optional()
    .describe("bake_ui_from_ir: repo-relative output dir for placeholder prefabs (default 'Assets/UI/Prefabs/Generated')."),
  theme_so: z
    .string()
    .optional()
    .describe("bake_ui_from_ir + claude_design_conformance: UiTheme SO asset path (default 'Assets/UI/Theme/DefaultUiTheme.asset')."),
  // ── ui_tree_walk (Stage 12 Step 14.2) ───────────────────────────────────
  root_path: z
    .string()
    .optional()
    .describe("ui_tree_walk: filter to one Canvas by scene path or short name. Omit to walk every Canvas in the active scene."),
  active_only: z
    .boolean()
    .default(true)
    .describe("ui_tree_walk: when true (default) skip Canvases whose GameObject is inactive in hierarchy."),
  include_serialized_fields: z
    .boolean()
    .default(false)
    .describe("ui_tree_walk: when true, include serialized field snapshot per component (same shape as prefab_inspect). Default false."),
  // ── claude_design_conformance (Stage 12 Step 14.3) ──────────────────────
  scene_root_path: z
    .string()
    .optional()
    .describe("claude_design_conformance: scene-mode root GameObject name (mutex with prefab_path; exactly one required)."),
  // ── scene_replace_with_prefab (Stage 9.14 / TECH-22667) ─────────────────
  target_object_name: z
    .string()
    .optional()
    .describe("scene_replace_with_prefab: name of the root GameObject to replace in the scene (e.g. 'hud-bar')."),
  // ── validate_panel_blueprint (bake-pipeline-hardening Stage 1) ───────────
  panel_id: z
    .string()
    .optional()
    .describe("validate_panel_blueprint: panel slug/id to validate against panel-schema.yaml. Returns {ok, missing} per child kind."),
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
    if (data.kind === "claude_design_conformance") {
      const hasPrefab = !!data.prefab_path?.trim();
      const hasScene = !!data.scene_root_path?.trim();
      if (hasPrefab === hasScene) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: 'claude_design_conformance requires exactly one of prefab_path or scene_root_path.',
          path: ["prefab_path"],
        });
      }
      if (!data.ir_path?.trim()) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: 'ir_path is required for claude_design_conformance (e.g. "web/design-refs/step-1-game-ui/ir.json").',
          path: ["ir_path"],
        });
      }
      if (!data.theme_so?.trim()) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: 'theme_so is required for claude_design_conformance (e.g. "Assets/UI/Theme/DefaultUiTheme.asset").',
          path: ["theme_so"],
        });
      }
    }
  });

export type UnityBridgeCommandInput = z.infer<typeof unityBridgeCommandInputSchema>;
