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
/// Editor exports → interchange JSON (cell/chunk subset + dev world snapshot) via
/// <see cref="EditorPostgresExportRegistrar.TryPersistReport"/> (Postgres-only).
/// Uses <see cref="GridManager.GetCell"/> + <see cref="TerrainManager.GetHeightMap"/> — no direct grid array access.
/// </summary>
public static class InterchangeJsonReportsMenu
{
    const string MenuRoot = "Territory Developer/Reports/";
    const int DefaultChunkW = 8;
    const int DefaultChunkH = 8;

    internal const int DefaultChunkWidth = DefaultChunkW;
    internal const int DefaultChunkHeight = DefaultChunkH;

    [MenuItem(MenuRoot + "Export CityCell Chunk (Interchange)", priority = 20)]
    public static void ExportCellChunkInterchange()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Interchange export", "Enter Play Mode with an initialized grid.", "OK");
            return;
        }

        AgentBridgeCellChunkOutcome outcome = ExportCellChunkForAgentBridge(0, 0, DefaultChunkW, DefaultChunkH);
        if (outcome.Success)
            Debug.Log("[Interchange] CityCell chunk stored in Postgres (editor_export_terrain_cell_chunk).");
        else
            Debug.LogError($"[Interchange] Export CityCell Chunk failed: {outcome.ErrorMessage}");
    }

    /// <summary>
    /// Parameterized cell chunk export for IDE agent bridge. Builds JSON via
    /// <see cref="BuildCellChunkInterchangeJsonString"/> and persists to Postgres.
    /// </summary>
    public static AgentBridgeCellChunkOutcome ExportCellChunkForAgentBridge(
        int originX = 0,
        int originY = 0,
        int chunkWidth = DefaultChunkW,
        int chunkHeight = DefaultChunkH)
    {
        try
        {
            if (chunkWidth <= 0) chunkWidth = DefaultChunkW;
            if (chunkHeight <= 0) chunkHeight = DefaultChunkH;

            string stamp = UtcTimestampForFilename();
            string baseName = $"cell-chunk-interchange-{stamp}";
            string json = BuildCellChunkInterchangeJsonString(originX, originY, chunkWidth, chunkHeight);
            bool dbOk = EditorPostgresExportRegistrar.TryPersistReport(
                EditorPostgresExportRegistrar.KindTerrainCellChunk,
                json,
                false,
                baseName,
                out _);
            if (!dbOk)
                return AgentBridgeCellChunkOutcome.Fail("Postgres persist failed for cell chunk export. Configure DATABASE_URL and migrations.");

            return AgentBridgeCellChunkOutcome.Ok();
        }
        catch (Exception ex)
        {
            return AgentBridgeCellChunkOutcome.Fail(ex.Message);
        }
    }

    [MenuItem(MenuRoot + "Export World Snapshot (Dev Interchange)", priority = 21)]
    public static void ExportWorldSnapshotDev()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Interchange export", "Enter Play Mode with an initialized grid.", "OK");
            return;
        }

        try
        {
            string stamp = UtcTimestampForFilename();
            string baseName = $"world-snapshot-dev-{stamp}";
            string json = BuildWorldSnapshotDevJsonString(includeHeightRaster: true);
            bool dbOk = EditorPostgresExportRegistrar.TryPersistReport(
                EditorPostgresExportRegistrar.KindWorldSnapshotDev,
                json,
                false,
                baseName,
                out _);
            if (dbOk)
                Debug.Log("[Interchange] World snapshot stored in Postgres (editor_export_world_snapshot_dev).");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Interchange] Export World Snapshot failed: {ex.Message}");
        }
    }

    internal static string BuildCellChunkInterchangeJsonString(int x0, int y0, int w, int h)
    {
        GridManager grid = UnityEngine.Object.FindObjectOfType<GridManager>();
        TerrainManager terrain = grid != null ? grid.terrainManager : UnityEngine.Object.FindObjectOfType<TerrainManager>();
        WaterManager waterManager = UnityEngine.Object.FindObjectOfType<WaterManager>();
        if (grid == null || !grid.isInitialized)
            throw new InvalidOperationException("GridManager missing or not initialized.");
        HeightMap hm = terrain != null ? terrain.GetHeightMap() : null;
        WaterMap wm = waterManager != null ? waterManager.GetWaterMap() : null;

        x0 = Mathf.Clamp(x0, 0, grid.width - 1);
        y0 = Mathf.Clamp(y0, 0, grid.height - 1);
        w = Mathf.Clamp(w, 1, grid.width - x0);
        h = Mathf.Clamp(h, 1, grid.height - y0);

        var cells = new List<CellChunkCellDto>();
        for (int y = y0; y < y0 + h; y++)
        {
            for (int x = x0; x < x0 + w; x++)
            {
                int heightMapValue = hm != null && hm.IsValidPosition(x, y) ? hm.GetHeight(x, y) : int.MinValue;
                CityCell cell = grid.GetCell(x, y);
                int cellHeight = cell != null ? cell.height : int.MinValue;
                if (cell != null && hm != null && hm.IsValidPosition(x, y) && heightMapValue != cellHeight)
                {
                    Debug.LogWarning($"[Interchange] HeightMap vs CityCell.height mismatch at ({x},{y}): map={heightMapValue} cell={cellHeight}. Export uses HeightMap as authority.");
                }

                int exportHeight = hm != null && hm.IsValidPosition(x, y) ? heightMapValue : cellHeight;
                int authoritativeBodyId = wm != null && wm.IsValidPosition(x, y) ? wm.GetWaterBodyId(x, y) : 0;
                bool waterMapIsWater = wm != null && wm.IsValidPosition(x, y) && wm.IsWater(x, y);
                cells.Add(new CellChunkCellDto
                {
                    x = x,
                    y = y,
                    height = exportHeight,
                    prefabName = cell != null ? cell.prefabName ?? "" : "",
                    waterBodyType = cell != null ? cell.waterBodyType.ToString() : "",
                    waterBodyId = cell != null ? cell.waterBodyId : 0,
                    waterMapBodyId = authoritativeBodyId,
                    waterMapIsWater = waterMapIsWater
                });
            }
        }

        // Body inventory (TECH-debug 2026-05-15): per-body SurfaceHeight + Classification
        // explicit, no longer inferable only from cell-level rows. Lets agents distinguish
        // sea (surface=0) from elevated lake/cascade (surface≥1) without scanning cells.
        var bodies = new List<WaterBodyEntryDto>();
        if (wm != null)
        {
            foreach (var kv in wm.GetBodies())
            {
                WaterBody b = kv.Value;
                if (b == null) continue;
                bodies.Add(new WaterBodyEntryDto
                {
                    bodyId = b.Id,
                    classification = b.Classification.ToString(),
                    surfaceHeight = b.SurfaceHeight,
                    cellCount = b.CellCount
                });
            }
            bodies.Sort((a, c) => a.bodyId.CompareTo(c.bodyId));
        }

        var root = new CellChunkInterchangeRootDto
        {
            artifact = "terrain_cell_chunk",
            schema_version = 2,
            origin_x = x0,
            origin_y = y0,
            width = w,
            height = h,
            cells = cells.ToArray(),
            bodies = bodies.ToArray(),
            notes = "v2: + bodies[] (SurfaceHeight, Classification, cellCount), + cells[].waterMapBodyId / waterMapIsWater (authoritative WaterMap, distinct from CityCell-cached waterBodyId). height is HeightMap when available, else CityCell.height."
        };

        return JsonUtility.ToJson(root, true);
    }

    static string BuildWorldSnapshotDevJsonString(bool includeHeightRaster)
    {
        GridManager grid = UnityEngine.Object.FindObjectOfType<GridManager>();
        WaterManager waterManager = UnityEngine.Object.FindObjectOfType<WaterManager>();
        TerrainManager terrain = grid != null ? grid.terrainManager : UnityEngine.Object.FindObjectOfType<TerrainManager>();
        if (grid == null || !grid.isInitialized)
            throw new InvalidOperationException("GridManager missing or not initialized.");

        WaterMap wm = waterManager != null ? waterManager.GetWaterMap() : null;
        HeightMap hm = terrain != null ? terrain.GetHeightMap() : null;

        var histogram = new List<WaterBodyCountEntryDto>();
        if (wm != null)
        {
            var counts = new Dictionary<int, int>();
            for (int y = 0; y < wm.Height; y++)
            {
                for (int x = 0; x < wm.Width; x++)
                {
                    int id = wm.GetWaterBodyId(x, y);
                    counts.TryGetValue(id, out int c);
                    counts[id] = c + 1;
                }
            }

            foreach (var kv in counts.OrderBy(k => k.Key))
            {
                histogram.Add(new WaterBodyCountEntryDto { body_id = kv.Key, cell_count = kv.Value });
            }
        }

        int[] heightsFlat = Array.Empty<int>();
        if (includeHeightRaster && hm != null)
        {
            heightsFlat = new int[grid.width * grid.height];
            int i = 0;
            for (int y = 0; y < grid.height; y++)
            {
                for (int x = 0; x < grid.width; x++)
                {
                    heightsFlat[i++] = hm.IsValidPosition(x, y) ? hm.GetHeight(x, y) : -1;
                }
            }
        }

        var root = new WorldSnapshotDevRootDto
        {
            artifact = "world_snapshot_dev",
            schema_version = 1,
            map_width = grid.width,
            map_height = grid.height,
            water_map_width = wm != null ? wm.Width : 0,
            water_map_height = wm != null ? wm.Height : 0,
            water_body_histogram = histogram.ToArray(),
            heights_row_major_width = includeHeightRaster && hm != null ? grid.width : 0,
            heights_row_major_height = includeHeightRaster && hm != null ? grid.height : 0,
            heights_row_major = heightsFlat,
            notes = "Diagnostics only — not Save data or Load pipeline input (TECH-41 G1)."
        };

        return JsonUtility.ToJson(root, true);
    }

    static string UtcTimestampForFilename()
    {
        return DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
    }
}

[Serializable]
class CellChunkInterchangeRootDto
{
    public string artifact;
    public int schema_version;
    public int origin_x;
    public int origin_y;
    public int width;
    public int height;
    public CellChunkCellDto[] cells;
    public WaterBodyEntryDto[] bodies;
    public string notes;
}

[Serializable]
class CellChunkCellDto
{
    public int x;
    public int y;
    public int height;
    public string prefabName;
    public string waterBodyType;
    public int waterBodyId;
    public int waterMapBodyId;
    public bool waterMapIsWater;
}

[Serializable]
class WaterBodyEntryDto
{
    public int bodyId;
    public string classification;
    public int surfaceHeight;
    public int cellCount;
}

[Serializable]
class WorldSnapshotDevRootDto
{
    public string artifact;
    public int schema_version;
    public int map_width;
    public int map_height;
    public int water_map_width;
    public int water_map_height;
    public WaterBodyCountEntryDto[] water_body_histogram;
    public int heights_row_major_width;
    public int heights_row_major_height;
    public int[] heights_row_major;
    public string notes;
}

[Serializable]
class WaterBodyCountEntryDto
{
    public int body_id;
    public int cell_count;
}

/// <summary>
/// Outcome of <see cref="InterchangeJsonReportsMenu.ExportCellChunkForAgentBridge"/> → IDE agent bridge.
/// </summary>
public readonly struct AgentBridgeCellChunkOutcome
{
    public bool Success { get; }
    public string ErrorMessage { get; }

    AgentBridgeCellChunkOutcome(bool success, string error)
    {
        Success = success;
        ErrorMessage = error ?? "";
    }

    public static AgentBridgeCellChunkOutcome Ok() => new(true, "");

    public static AgentBridgeCellChunkOutcome Fail(string message) =>
        new(false, message ?? "Unknown error.");
}
