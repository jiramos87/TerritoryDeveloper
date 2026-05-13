using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor-only agent diagnostics menu shell (Stage 6.3 Tier-D thin hub).
/// All build + persist logic lives in <see cref="DiagnosticsReportsService"/>
/// (Assets/Scripts/Editor/Bridge/Services/DiagnosticsReportsService.cs).
/// Uses <see cref="GridManager.GetCell"/> only — no direct grid array access.
/// </summary>
public static class AgentDiagnosticsReportsMenu
{
    const string MenuRoot = "Territory Developer/Reports/";

    [MenuItem(MenuRoot + "Export Agent Context", priority = 10)]
    public static void ExportAgentContext()
    {
        try
        {
            string json = DiagnosticsReportsService.BuildAgentContextJsonString(null, null);
            string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            string baseName = $"agent-context-{stamp}";
            bool dbOk = EditorPostgresExportRegistrar.TryPersistReport(
                EditorPostgresExportRegistrar.KindAgentContext,
                json,
                false,
                baseName,
                out _);
            if (dbOk)
                Debug.Log("[AgentDiagnostics] Agent context stored in Postgres (editor_export_agent_context).");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AgentDiagnostics] Export Agent Context failed: {ex.Message}");
        }
    }

    [MenuItem(MenuRoot + "Export Sorting Debug (Markdown)", priority = 11)]
    public static void ExportSortingDebug()
    {
        AgentBridgeSortingDebugOutcome outcome = ExportSortingDebugForAgentBridge();
        if (outcome.Success)
            Debug.Log("[AgentDiagnostics] Sorting debug stored in Postgres (editor_export_sorting_debug).");
        else
            Debug.LogError($"[AgentDiagnostics] Export Sorting Debug failed: {outcome.ErrorMessage}");
    }

    /// <summary>
    /// Parameterized sorting debug export for IDE agent bridge. Delegates to <see cref="DiagnosticsReportsService"/>.
    /// </summary>
    public static AgentBridgeSortingDebugOutcome ExportSortingDebugForAgentBridge(
        int? overrideSeedX = null,
        int? overrideSeedY = null)
        => DiagnosticsReportsService.ExportSortingDebugForBridge(overrideSeedX, overrideSeedY);

    /// <summary>
    /// Parameterized agent context export for IDE agent bridge. Delegates to <see cref="DiagnosticsReportsService"/>.
    /// </summary>
    public static AgentBridgeAgentContextOutcome ExportAgentContextForAgentBridge(
        int? overrideSeedX = null,
        int? overrideSeedY = null,
        bool writeBridgeArtifactFile = false)
        => DiagnosticsReportsService.ExportAgentContextForBridge(overrideSeedX, overrideSeedY, writeBridgeArtifactFile);
}

/// <summary>
/// Outcome of <see cref="AgentDiagnosticsReportsMenu.ExportAgentContextForAgentBridge"/> → IDE agent bridge.
/// </summary>
public readonly struct AgentBridgeAgentContextOutcome
{
    public bool Success { get; }
    public string ErrorMessage { get; }
    public bool PostgresOnly { get; }
    public string Storage { get; }
    public string ArtifactPathRepoRelative { get; }

    AgentBridgeAgentContextOutcome(bool success, string error, bool postgresOnly, string storage, string artifactRel)
    {
        Success = success;
        ErrorMessage = error ?? "";
        PostgresOnly = postgresOnly;
        Storage = storage ?? "none";
        ArtifactPathRepoRelative = artifactRel ?? "";
    }

    public static AgentBridgeAgentContextOutcome OkPostgresOnly() =>
        new(true, "", true, "postgres", "");

    /// <summary>
    /// Postgres row written + UTF-8 JSON copy under <c>tools/reports/</c> for IDE agents (bridge <c>artifact_paths</c>).
    /// </summary>
    public static AgentBridgeAgentContextOutcome OkPostgresWithDiskArtifact(string repoRelativePath) =>
        new(true, "", true, "postgres", repoRelativePath ?? "");

    public static AgentBridgeAgentContextOutcome Fail(string message) =>
        new(false, message ?? "Unknown error.", false, "none", "");
}

/// <summary>
/// Outcome of <see cref="AgentDiagnosticsReportsMenu.ExportSortingDebugForAgentBridge"/> → IDE agent bridge.
/// </summary>
public readonly struct AgentBridgeSortingDebugOutcome
{
    public bool Success { get; }
    public string ErrorMessage { get; }

    AgentBridgeSortingDebugOutcome(bool success, string error)
    {
        Success = success;
        ErrorMessage = error ?? "";
    }

    public static AgentBridgeSortingDebugOutcome Ok() => new(true, "");

    public static AgentBridgeSortingDebugOutcome Fail(string message) =>
        new(false, message ?? "Unknown error.");
}

[Serializable]
class AgentContextReportDto
{
    public string schema_version;
    public string exported_at_utc;
    public string unity_version;
    public string play_mode;
    public string active_scene_path;
    public string active_scene_name;
    public string notes;
    public SelectionDto selection = new SelectionDto();
    public GridSampleDto grid = new GridSampleDto();
}

[Serializable]
class SelectionDto
{
    public string hierarchy_path = "";
    public string[] mono_behaviour_types = Array.Empty<string>();
}

[Serializable]
class GridSampleDto
{
    public bool grid_manager_found;
    public bool grid_initialized;
    public int grid_width;
    public int grid_height;
    public int seed_cell_x;
    public int seed_cell_y;
    public string notes = "";
    public CellSampleDto[] cells = Array.Empty<CellSampleDto>();
}

[Serializable]
class CellSampleDto
{
    public int x;
    public int y;
    public int cell_height;
    public int height_map_height;
    public int water_map_body_id;
    public int cell_water_body_id;
    public string water_body_type;
    public string zone_type;
    public bool is_interstate;
    public bool water_map_reports_open_water;
    public string water_map_classification;
    public string[] cell_child_names = Array.Empty<string>();
}
