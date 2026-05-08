/**
 * Request envelope builder + low-level DB helpers for unity_bridge_command.
 */

import type { Pool } from "pg";
import type { UnityBridgeCommandInput } from "./input-schema.js";
import type { UnityBridgeResponsePayload } from "./response-types.js";

export function sleepMs(ms: number): Promise<void> {
  return new Promise((r) => setTimeout(r, ms));
}

export function jsonResult(payload: unknown) {
  return {
    content: [
      {
        type: "text" as const,
        text: JSON.stringify(payload, null, 2),
      },
    ],
  };
}

export type BridgeRow = {
  status: string;
  response: UnityBridgeResponsePayload | null;
  error: string | null;
  kind: string;
};

export async function selectBridgeRow(
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

export function buildRequestEnvelope(
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
  if (input.kind === "set_panel_visible") {
    return {
      ...base,
      params: {
        slug: input.slug ?? "",
        active: input.active ?? true,
      },
    };
  }
  if (input.kind === "catalog_preview") {
    return {
      ...base,
      params: {
        catalog_entry_id: input.catalog_entry_id ?? "",
        include_screenshot: input.include_screenshot ?? true,
      },
    };
  }
  if (input.kind === "prefab_inspect") {
    return { ...base, params: { prefab_path: input.prefab_path ?? "" } };
  }
  if (input.kind === "ui_tree_walk") {
    return {
      ...base,
      params: {
        root_path: input.root_path ?? "",
        active_only: input.active_only ?? true,
        include_serialized_fields: input.include_serialized_fields ?? false,
      },
    };
  }
  if (input.kind === "claude_design_conformance") {
    return {
      ...base,
      params: {
        ir_path: input.ir_path ?? "",
        theme_so: input.theme_so ?? "",
        prefab_path: input.prefab_path ?? "",
        scene_root_path: input.scene_root_path ?? "",
      },
    };
  }
  return { ...base, params: {} };
}
