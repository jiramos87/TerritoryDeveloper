using System;

// Stage 6.1 extract — core bridge DTOs extracted from AgentBridgeCommandRunner.cs stem.
// Moved to keep stem ≤200 LOC. These are file-level (no namespace) to match legacy shape.

[Serializable]
public class AgentBridgeRequestEnvelopeDto
{
    public int schema_version;
    public string artifact;
    public string command_id;
    public string kind;
    public string requested_at_utc;
    public AgentBridgeParamsPayloadDto bridge_params;
}

[Serializable]
public class AgentBridgeParamsPayloadDto
{
    public string since_utc;
    public string severity_filter;
    public string tag_filter;
    public int max_lines;
    public string camera;
    public string filename_stem;
    public bool include_ui;
    /// <summary><c>export_agent_context</c> / <c>export_sorting_debug</c>: optional <c>"cellX,cellY"</c> Moore neighborhood seed (e.g. <c>3,0</c>).</summary>
    public string seed_cell;
    public int origin_x;
    public int origin_y;
    public int chunk_width;
    public int chunk_height;
    public bool include_screenshot;
    public bool include_console;
    public bool include_anomaly_scan;
    public string catalog_entry_id;
    public string panel_id;
}

[Serializable]
public class AgentBridgeLogLineDto
{
    public string timestamp_utc;
    public string severity;
    public string message;
    public string stack;
}

[Serializable]
public class AgentBridgeCompilationStatusDto
{
    public bool compiling;
    public bool compilation_failed;
    public string last_error_excerpt;
    public AgentBridgeLogLineDto[] recent_error_messages;
}

[Serializable]
public class AgentBridgeResponseFileDto
{
    public int schema_version;
    public string artifact;
    public string command_id;
    public bool ok;
    public string completed_at_utc;
    public string storage;
    public string[] artifact_paths;
    public bool postgres_only;
    public string error;
    public AgentBridgeLogLineDto[] log_lines;
    public string play_mode_state;
    public bool ready;
    public bool already_playing;
    public bool already_stopped;
    public bool has_grid_dimensions;
    public int grid_width;
    public int grid_height;
    public AgentBridgeBundleDto bundle;
    public AgentBridgeCompilationStatusDto compilation_status;
    public AgentBridgeEconomySnapshotDto economy_snapshot;
    public AgentBridgePrefabManifestDto prefab_manifest;
    public AgentBridgeSortingOrderDebugDto sorting_order_debug;
    public string catalog_preview_result;
    public AgentBridgePrefabInspectDto prefab_inspect_result;
    public AgentBridgePanelStateDto panel_state_result;
    public AgentBridgeActionLogResultDto action_log_result;
    public AgentBridgeUiTreeWalkDto ui_tree_walk_result;
    public AgentBridgeConformanceResultDto claude_design_conformance_result;
    public string result_json;
    public string mutation_result;

    public static AgentBridgeResponseFileDto CreateOk(string commandId, string storage)
    {
        return new AgentBridgeResponseFileDto
        {
            schema_version   = 1,
            artifact         = "unity_agent_bridge_response",
            command_id       = commandId,
            ok               = true,
            completed_at_utc = DateTime.UtcNow.ToString("o"),
            storage          = storage,
            postgres_only    = false,
            error            = string.Empty,
            artifact_paths   = Array.Empty<string>(),
            log_lines        = Array.Empty<AgentBridgeLogLineDto>(),
            play_mode_state  = string.Empty,
            ready            = false,
            already_playing  = false,
            already_stopped  = false,
            has_grid_dimensions = false,
            grid_width       = 0,
            grid_height      = 0,
        };
    }
}

[Serializable]
public class AgentBridgeBundleDto
{
    public AgentBridgeBundleCellExportDto cell_export;
    public AgentBridgeBundleScreenshotDto screenshot;
    public AgentBridgeBundleConsoleDto console;
    public AgentBridgeAnomalyRecordDto[] anomalies;
    public int anomaly_count;
    public bool anomaly_scan_skipped;
}

[Serializable]
public class AgentBridgeEconomySnapshotDto
{
    public int population;
    public float happiness;
    public float pollution;
    public int money;
    public int residential_tax;
    public int commercial_tax;
    public int industrial_tax;
    public int residential_building_count;
    public int commercial_building_count;
    public int industrial_building_count;
    public float residential_demand;
    public float commercial_demand;
    public float industrial_demand;
}

[Serializable]
public class AgentBridgePrefabManifestDto
{
    public int total_monobehaviours;
    public int missing_script_count;
    public AgentBridgePrefabManifestEntryDto[] entries;
}

[Serializable]
public class AgentBridgePrefabManifestEntryDto
{
    public string game_object_path;
    public string component_type;
    public bool is_missing_script;
}

[Serializable]
public class AgentBridgeSortingOrderDebugDto
{
    public int cell_x;
    public int cell_y;
    public int cell_height;
    public int renderer_count;
    public AgentBridgeSortingRendererDto[] renderers;
}

[Serializable]
public class AgentBridgeSortingRendererDto
{
    public string name;
    public string sorting_layer;
    public int sorting_order;
    public string sprite_name;
    public bool enabled;
}

[Serializable]
public class AgentBridgeBundleCellExportDto
{
    public string artifact_path;
    public bool ok;
}

[Serializable]
public class AgentBridgeBundleScreenshotDto
{
    public string artifact_path;
    public bool ok;
    public bool skipped;
}

[Serializable]
public class AgentBridgeBundleConsoleDto
{
    public AgentBridgeLogLineDto[] log_lines;
    public int line_count;
    public bool skipped;
}

[Serializable]
public class AgentBridgeAnomalyRecordDto
{
    public string rule;
    public int cell_x;
    public int cell_y;
    public string severity;
    public string message;
}
