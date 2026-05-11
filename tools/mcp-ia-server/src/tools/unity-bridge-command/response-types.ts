/**
 * Response payload types for unity_bridge_command / unity_bridge_get.
 */

export type UnityBridgeLogLine = {
  timestamp_utc: string;
  severity: string;
  message: string;
  stack: string;
};

export type PrefabInspectField = {
  field_name: string;
  propertyType: string;
  value_str: string;
};

export type PrefabInspectComponent = {
  type_name: string;
  full_type_name?: string;
  is_missing_script: boolean;
  fields: PrefabInspectField[];
};

export type PrefabInspectRect = {
  anchor_min: string;
  anchor_max: string;
  pivot: string;
  anchored_position: string;
  size_delta: string;
  offset_min: string;
  offset_max: string;
  local_scale: string;
};

export type PrefabInspectNode = {
  name: string;
  relative_path: string;
  active_self: boolean;
  tag: string;
  layer: string;
  rect_transform?: PrefabInspectRect | null;
  components: PrefabInspectComponent[];
  children: PrefabInspectNode[];
};

export type UiTreeScreenRect = {
  x: number;
  y: number;
  width: number;
  height: number;
};

export type UiTreeNode = {
  name: string;
  relative_path: string;
  active_self: boolean;
  active_in_hierarchy: boolean;
  tag: string;
  layer: string;
  rect_transform: PrefabInspectRect | null;
  screen_rect: UiTreeScreenRect | null;
  components: PrefabInspectComponent[];
  children: UiTreeNode[];
};

export type UiTreeCanvas = {
  name: string;
  scene_path: string;
  render_mode: string;
  reference_resolution: string | null;
  reference_pixels_per_unit: number;
  sort_order: number;
  is_active_in_hierarchy: boolean;
  root: UiTreeNode;
};

export type ConformanceRow = {
  node_path: string;
  component: string;
  check_kind:
    | "palette_ramp"
    | "font_face"
    | "frame_style"
    | "panel_kind"
    | "caption"
    | "contrast_ratio"
    | "frame_sprite_bound"
    | "button_state_block";
  slug: string;
  expected: string;
  resolved: string;
  actual: string;
  severity: "info" | "warn" | "fail";
  pass: boolean;
  message: string;
};

export type ConformanceResult = {
  ir_path: string;
  theme_so: string;
  target_kind: "prefab" | "scene";
  target_path: string;
  row_count: number;
  fail_count: number;
  rows: ConformanceRow[];
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
  /** Populated for prefab_inspect (Stage 12 Step 14.1) — read-only prefab hierarchy + serialized field dump. */
  prefab_inspect_result?: {
    prefab_path: string;
    root_name: string;
    node_count: number;
    component_count: number;
    missing_script_count: number;
    root: PrefabInspectNode;
  };
  /** Populated for ui_tree_walk (Stage 12 Step 14.2) — read-only Canvas walk with screen-space rects. */
  ui_tree_walk_result?: {
    scene_name: string;
    scene_path: string;
    canvas_count: number;
    node_count: number;
    component_count: number;
    missing_script_count: number;
    canvases: UiTreeCanvas[];
  };
  /** Populated for claude_design_conformance (Stage 12 Step 14.3) — IR + UiTheme conformance rows. */
  claude_design_conformance_result?: ConformanceResult;
  /** Populated for read_panel_state (Stage 4 T4.0.1) — live panel runtime state. */
  panel_state_result?: {
    panel_slug: string;
    mounted: boolean;
    anchor_path: string;
    child_count: number;
    bind_count: number;
    action_count: number;
    controller_alive: boolean;
  };
  /** Populated for get_action_log (Stage 4 T4.0.3) — action-fire telemetry log tail. */
  action_log_result?: {
    log_path: string;
    entries: Array<{
      action_id: string;
      handler_class: string;
      ts: string;
      marker: string;
    }>;
  };
};
