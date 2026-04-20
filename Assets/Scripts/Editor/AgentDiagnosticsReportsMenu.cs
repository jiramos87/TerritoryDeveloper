using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Territory.Core;
using Territory.Terrain;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor-only agent diagnostics. Bounded grid snapshot JSON + sorting debug markdown; persisted via
/// <see cref="EditorPostgresExportRegistrar.TryPersistReport"/> (Postgres-only). Uses <see cref="GridManager.GetCell"/> only — no direct grid array access.
/// </summary>
public static class AgentDiagnosticsReportsMenu
{
    const string SchemaVersion = "1";
    const int MaxContextCells = 16;
    const int MaxCellChildNames = 24;
    const int MaxSortingDebugCells = 25;
    const int MaxMonoBehaviourTypeNames = 20;
    const int MaxHierarchyPathChars = 500;
    const int MaxSpriteRendererSamples = 12;
    const string MenuRoot = "Territory Developer/Reports/";

    [MenuItem(MenuRoot + "Export Agent Context", priority = 10)]
    public static void ExportAgentContext()
    {
        try
        {
            string stamp = UtcTimestampForFilename();
            string baseName = $"agent-context-{stamp}";
            string json = BuildAgentContextJsonString(null, null);
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
    /// Parameterized sorting debug export for IDE agent bridge. Builds Markdown via
    /// <see cref="BuildSortingDebugMarkdownString(int?, int?)"/> and persists to Postgres.
    /// </summary>
    public static AgentBridgeSortingDebugOutcome ExportSortingDebugForAgentBridge(
        int? overrideSeedX = null,
        int? overrideSeedY = null)
    {
        try
        {
            string stamp = UtcTimestampForFilename();
            string baseName = $"sorting-debug-{stamp}";
            string md = BuildSortingDebugMarkdownString(overrideSeedX, overrideSeedY);
            bool dbOk = EditorPostgresExportRegistrar.TryPersistReport(
                EditorPostgresExportRegistrar.KindSortingDebug,
                md,
                true,
                baseName,
                out _);
            if (!dbOk)
                return AgentBridgeSortingDebugOutcome.Fail("Postgres persist failed for sorting debug export. Configure DATABASE_URL and migrations.");

            return AgentBridgeSortingDebugOutcome.Ok();
        }
        catch (Exception ex)
        {
            return AgentBridgeSortingDebugOutcome.Fail(ex.Message);
        }
    }

    /// <summary>
    /// Same persistence as <see cref="ExportAgentContext"/>, no Finder or extra logging side effects.
    /// Used by Postgres-backed IDE agent bridge; menus keep calling <see cref="ExportAgentContext"/>.
    /// </summary>
    /// <param name="overrideSeedX">When both overrides set + in grid bounds → Moore sample centered here instead of selection / (0,0).</param>
    public static AgentBridgeAgentContextOutcome ExportAgentContextForAgentBridge(
        int? overrideSeedX = null,
        int? overrideSeedY = null,
        bool writeBridgeArtifactFile = false)
    {
        try
        {
            string stamp = UtcTimestampForFilename();
            string baseName = $"agent-context-{stamp}";
            string json = BuildAgentContextJsonString(overrideSeedX, overrideSeedY);
            bool dbOk = EditorPostgresExportRegistrar.TryPersistReport(
                EditorPostgresExportRegistrar.KindAgentContext,
                json,
                false,
                baseName,
                out _);
            if (!dbOk)
            {
                return AgentBridgeAgentContextOutcome.Fail(
                    "Agent context export failed: Postgres did not accept the report. Configure DATABASE_URL and migrations; see docs/postgres-ia-dev-setup.md.");
            }

            if (!writeBridgeArtifactFile)
                return AgentBridgeAgentContextOutcome.OkPostgresOnly();

            EnsureReportsDirectory();
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string bridgeName = $"agent-context-bridge-{stamp}.json";
            string absBridge = Path.Combine(projectRoot, "tools", "reports", bridgeName);
            File.WriteAllText(absBridge, json, Encoding.UTF8);
            string repoRel = "tools/reports/" + bridgeName;
            return AgentBridgeAgentContextOutcome.OkPostgresWithDiskArtifact(repoRel);
        }
        catch (Exception ex)
        {
            return AgentBridgeAgentContextOutcome.Fail(ex.Message);
        }
    }

    static string BuildAgentContextJsonString(int? overrideSeedX = null, int? overrideSeedY = null)
    {
        var report = new AgentContextReportDto
        {
            schema_version = SchemaVersion,
            exported_at_utc = DateTime.UtcNow.ToString("o"),
            unity_version = Application.unityVersion,
            play_mode = EditorApplication.isPlaying ? "play" : "edit",
            active_scene_path = Truncate(ScenePathUtil.GetActiveScenePath(), MaxHierarchyPathChars),
            active_scene_name = Truncate(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, 200),
            notes = BuildContextNotes(),
            selection = BuildSelectionDto(),
            grid = BuildGridSampleDto(overrideSeedX, overrideSeedY)
        };

        return JsonUtility.ToJson(report, true);
    }

    static string BuildSortingDebugMarkdownString() => BuildSortingDebugMarkdownString(null, null);

    /// <summary>
    /// Bridge-callable overload: explicit seed overrides Selection-based resolve when both non-null.
    /// </summary>
    internal static string BuildSortingDebugMarkdownString(int? overrideSeedX, int? overrideSeedY)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Sorting debug export");
        sb.AppendLine();
        sb.AppendLine("Reference: `isometric-geography-system.md` — Sorting order / formula. Values below come from live `TerrainManager` APIs (`CalculateTerrainSortingOrder`, etc.), not a second implementation.");
        sb.AppendLine();
        sb.AppendLine($"- **UTC:** {DateTime.UtcNow:o}");
        sb.AppendLine($"- **Unity:** {Application.unityVersion}");
        sb.AppendLine($"- **Mode:** {(EditorApplication.isPlaying ? "Play Mode" : "Edit Mode")}");
        sb.AppendLine();

        if (!EditorApplication.isPlaying)
        {
            sb.AppendLine("## Not available in Edit Mode");
            sb.AppendLine();
            sb.AppendLine("Enter **Play Mode** with an initialized **grid** (`GridManager.InitializeGrid` has run) to emit per-cell **Sorting order** breakdowns.");
            return sb.ToString();
        }

        GridManager grid = UnityEngine.Object.FindObjectOfType<GridManager>();
        if (grid == null || !grid.isInitialized)
        {
            sb.AppendLine("## Grid not ready");
            sb.AppendLine();
            sb.AppendLine("No initialized **GridManager** in Play Mode — start a **New Game** or load a scene after **geography initialization**.");
            return sb.ToString();
        }

        TerrainManager terrain = grid.terrainManager;
        if (terrain == null)
        {
            sb.AppendLine("## TerrainManager missing");
            sb.AppendLine();
            sb.AppendLine("**GridManager.terrainManager** is null; cannot compute terrain sorting orders.");
            return sb.ToString();
        }

        int seedX, seedY;
        if (overrideSeedX.HasValue && overrideSeedY.HasValue &&
            overrideSeedX.Value >= 0 && overrideSeedY.Value >= 0 &&
            overrideSeedX.Value < grid.width && overrideSeedY.Value < grid.height)
        {
            seedX = overrideSeedX.Value;
            seedY = overrideSeedY.Value;
        }
        else if (!TryResolveSeedCell(grid, out seedX, out seedY))
        {
            sb.AppendLine("## Seed cell");
            sb.AppendLine();
            sb.AppendLine("Could not map selection to a **CityCell**; using grid origin `(0,0)` if in bounds.");
        }

        List<Vector2Int> cells = CollectMooreExpansion(seedX, seedY, grid.width, grid.height, MaxSortingDebugCells);
        sb.AppendLine("## Formula constants (TerrainManager)");
        sb.AppendLine();
        sb.AppendLine($"- `TERRAIN_BASE_ORDER` = {TerrainManager.TERRAIN_BASE_ORDER}");
        sb.AppendLine($"- `DEPTH_MULTIPLIER` = {TerrainManager.DEPTH_MULTIPLIER}");
        sb.AppendLine($"- `HEIGHT_MULTIPLIER` = {TerrainManager.HEIGHT_MULTIPLIER}");
        sb.AppendLine($"- `GridSortingOrderService.ROAD_SORTING_OFFSET` = {GridSortingOrderService.ROAD_SORTING_OFFSET}");
        sb.AppendLine();

        foreach (Vector2Int p in cells)
        {
            AppendCellSortingSection(sb, grid, terrain, p.x, p.y);
        }

        return sb.ToString();
    }

    static void AppendCellSortingSection(StringBuilder sb, GridManager grid, TerrainManager terrain, int x, int y)
    {
        CityCell cell = grid.GetCell(x, y);
        sb.AppendLine($"## CityCell ({x}, {y})");
        sb.AppendLine();
        if (cell == null)
        {
            sb.AppendLine("- (no **CityCell** at this coordinate)");
            sb.AppendLine();
            return;
        }

        int hMap = -1;
        HeightMap hm = terrain.GetHeightMap();
        if (hm != null && x >= 0 && y >= 0 && x < grid.width && y < grid.height)
            hMap = hm.GetHeight(x, y);

        int hSample = hMap >= 0 ? hMap : cell.height;
        int isoDepth = x + y;
        int depthOrder = -isoDepth * TerrainManager.DEPTH_MULTIPLIER;
        int heightOrder = hSample * TerrainManager.HEIGHT_MULTIPLIER;
        int terrainBase = TerrainManager.TERRAIN_BASE_ORDER + depthOrder + heightOrder;

        sb.AppendLine($"- **CityCell.height:** {cell.height}");
        if (hMap >= 0)
            sb.AppendLine($"- **HeightMap** height at cell: {hMap}");
        sb.AppendLine($"- **Terrain base** (`CalculateTerrainSortingOrder`): {terrain.CalculateTerrainSortingOrder(x, y, hSample)}");
        sb.AppendLine($"  - breakdown: depth `(x+y)={isoDepth}` → depthOrder `{depthOrder}`, heightOrder `{heightOrder}` (h={hSample}), matches `TERRAIN_BASE_ORDER + depthOrder + heightOrder` = `{terrainBase}`");
        sb.AppendLine($"- **Slope** (`CalculateSlopeSortingOrder`): {terrain.CalculateSlopeSortingOrder(x, y, hSample)}");
        sb.AppendLine($"- **Building** (`CalculateBuildingSortingOrder`): {terrain.CalculateBuildingSortingOrder(x, y, hSample)}");
        sb.AppendLine($"- **Road** offset (+5) via `CalculateSortingOrder`/`ObjectType.Road`: {terrain.CalculateSortingOrder(x, y, TerrainManager.ObjectType.Road)}");
        sb.AppendLine($"- **Utility** offset (+8): {terrain.CalculateSortingOrder(x, y, TerrainManager.ObjectType.Utility)}");
        sb.AppendLine($"- **Zone type:** {cell.zoneType}");
        sb.AppendLine($"- **Interstate flag:** {cell.isInterstate}");
        sb.AppendLine();

        sb.AppendLine("### SpriteRenderer `sortingOrder` (capped)");
        sb.AppendLine();
        int count = 0;
        var srs = cell.gameObject.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer sr in srs)
        {
            if (sr == null || count >= MaxSpriteRendererSamples)
                break;
            string spritePath = Truncate(GetHierarchyPath(sr.transform), 200);
            sb.AppendLine($"- `{spritePath}` → sortingOrder **{sr.sortingOrder}**");
            count++;
        }
        if (count == 0)
            sb.AppendLine("- (none)");
        sb.AppendLine();
    }

    static string BuildContextNotes()
    {
        return Truncate(
            "Sample rule: seed from **CityCell** on selected **GameObject** (or ancestor), else (0,0). " +
            "Then **Moore neighborhood** expansion (Chebyshev rings) up to max cells. " +
            "Limits: max_context_cells=" + MaxContextCells +
            ", max_sorting_debug_cells=" + MaxSortingDebugCells +
            ", max_mono_types=" + MaxMonoBehaviourTypeNames + ".",
            MaxHierarchyPathChars);
    }

    static SelectionDto BuildSelectionDto()
    {
        var dto = new SelectionDto();
        GameObject go = Selection.activeGameObject;
        if (go == null)
            return dto;

        dto.hierarchy_path = Truncate(GetHierarchyPath(go.transform), MaxHierarchyPathChars);
        var types = new HashSet<string>();
        foreach (MonoBehaviour mb in go.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb == null)
                continue;
            types.Add(mb.GetType().FullName ?? mb.GetType().Name);
            if (types.Count >= MaxMonoBehaviourTypeNames)
                break;
        }
        dto.mono_behaviour_types = types.OrderBy(s => s).ToArray();
        return dto;
    }

    static GridSampleDto BuildGridSampleDto(int? overrideSeedX = null, int? overrideSeedY = null)
    {
        var dto = new GridSampleDto();
        GridManager grid = UnityEngine.Object.FindObjectOfType<GridManager>();
        if (grid == null)
        {
            dto.grid_manager_found = false;
            dto.notes = "No **GridManager** in loaded scenes.";
            return dto;
        }

        dto.grid_manager_found = true;
        dto.grid_initialized = grid.isInitialized;
        dto.grid_width = grid.width;
        dto.grid_height = grid.height;

        if (!grid.isInitialized)
        {
            dto.notes = "**GridManager** present but **not initialized** (expected in Edit Mode before **Play** or before **New Game**).";
            dto.cells = Array.Empty<CellSampleDto>();
            return dto;
        }

        int seedX;
        int seedY;
        if (overrideSeedX.HasValue && overrideSeedY.HasValue)
        {
            int ox = overrideSeedX.Value;
            int oy = overrideSeedY.Value;
            if (ox >= 0 && oy >= 0 && ox < grid.width && oy < grid.height)
            {
                seedX = ox;
                seedY = oy;
            }
            else
            {
                TryResolveSeedCell(grid, out seedX, out seedY);
                dto.notes =
                    $"**seed_cell** override ({ox},{oy}) out of bounds for grid {grid.width}×{grid.height}; fell back to selection or (0,0). ";
            }
        }
        else
        {
            TryResolveSeedCell(grid, out seedX, out seedY);
        }

        dto.seed_cell_x = seedX;
        dto.seed_cell_y = seedY;

        List<Vector2Int> coords = CollectMooreExpansion(seedX, seedY, grid.width, grid.height, MaxContextCells);
        var samples = new List<CellSampleDto>();
        TerrainManager terrain = grid.terrainManager;
        WaterManager water = grid.waterManager;
        HeightMap heightMap = terrain != null ? terrain.GetHeightMap() : null;
        WaterMap waterMap = water != null ? water.GetWaterMap() : null;

        foreach (Vector2Int p in coords)
        {
            CityCell cell = grid.GetCell(p.x, p.y);
            if (cell == null)
                continue;

            var c = new CellSampleDto
            {
                x = p.x,
                y = p.y,
                cell_height = cell.height,
                height_map_height = -1,
                water_map_body_id = 0,
                cell_water_body_id = cell.waterBodyId,
                water_body_type = cell.waterBodyType.ToString(),
                zone_type = cell.zoneType.ToString(),
                is_interstate = cell.isInterstate,
                water_map_reports_open_water = false,
                water_map_classification = ""
            };

            if (heightMap != null && p.x >= 0 && p.y >= 0 && p.x < grid.width && p.y < grid.height)
                c.height_map_height = heightMap.GetHeight(p.x, p.y);

            if (waterMap != null && waterMap.IsValidPosition(p.x, p.y))
            {
                c.water_map_body_id = waterMap.GetWaterBodyId(p.x, p.y);
                c.water_map_reports_open_water = waterMap.IsWater(p.x, p.y);
                if (c.water_map_reports_open_water)
                    c.water_map_classification = waterMap.GetBodyClassificationAt(p.x, p.y).ToString();
            }

            c.cell_child_names = CollectCellChildNames(cell);

            samples.Add(c);
        }

        dto.cells = samples.ToArray();
        return dto;
    }

    static bool TryResolveSeedCell(GridManager grid, out int sx, out int sy)
    {
        sx = 0;
        sy = 0;
        GameObject go = Selection.activeGameObject;
        if (go == null)
            return false;

        Transform t = go.transform;
        while (t != null)
        {
            var cell = t.GetComponent<CityCell>();
            if (cell != null && cell.x >= 0 && cell.y >= 0 && cell.x < grid.width && cell.y < grid.height)
            {
                sx = cell.x;
                sy = cell.y;
                return true;
            }
            t = t.parent;
        }
        return false;
    }

    /// <summary>
    /// Chebyshev-ring expansion: center → distance 1, 2, … until maxCells or grid exhausted.
    /// </summary>
    static List<Vector2Int> CollectMooreExpansion(int seedX, int seedY, int w, int h, int maxCells)
    {
        var result = new List<Vector2Int>();
        var seen = new HashSet<Vector2Int>();

        void TryAdd(int x, int y)
        {
            if (x < 0 || y < 0 || x >= w || y >= h)
                return;
            var v = new Vector2Int(x, y);
            if (!seen.Add(v))
                return;
            result.Add(v);
        }

        TryAdd(seedX, seedY);
        int maxRing = Math.Max(w, h) + 2;
        for (int ring = 1; result.Count < maxCells && ring <= maxRing; ring++)
        {
            for (int dx = -ring; dx <= ring && result.Count < maxCells; dx++)
            {
                for (int dy = -ring; dy <= ring && result.Count < maxCells; dy++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != ring)
                        continue;
                    TryAdd(seedX + dx, seedY + dy);
                }
            }
        }

        return result;
    }

    static string[] CollectCellChildNames(CityCell cell)
    {
        if (cell == null || cell.gameObject == null)
            return Array.Empty<string>();

        Transform root = cell.gameObject.transform;
        int n = root.childCount;
        if (n <= 0)
            return Array.Empty<string>();

        int cap = Math.Min(n, MaxCellChildNames);
        var names = new string[cap];
        for (int i = 0; i < cap; i++)
        {
            Transform ch = root.GetChild(i);
            names[i] = ch != null ? Truncate(ch.name, 120) : "";
        }

        return names;
    }

    static void EnsureReportsDirectory()
    {
        string dir = GetReportsDirectory();
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    static string GetReportsDirectory()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        return Path.Combine(projectRoot, "tools", "reports");
    }

    static string UtcTimestampForFilename()
        => DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");

    static string Truncate(string s, int maxLen)
    {
        if (string.IsNullOrEmpty(s))
            return s ?? "";
        if (s.Length <= maxLen)
            return s;
        return s.Substring(0, maxLen) + "...";
    }

    static string GetHierarchyPath(Transform t)
    {
        var sb = new StringBuilder();
        while (t != null)
        {
            if (sb.Length > 0)
                sb.Insert(0, "/");
            sb.Insert(0, t.name);
            t = t.parent;
        }
        return sb.ToString();
    }

    static class ScenePathUtil
    {
        public static string GetActiveScenePath()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            return string.IsNullOrEmpty(scene.path) ? scene.name : scene.path;
        }
    }
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
