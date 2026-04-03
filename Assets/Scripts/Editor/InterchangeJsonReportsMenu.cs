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
/// Editor exports for TECH-41 interchange JSON under <c>tools/reports/</c> (gitignored): cell/chunk subset and dev world snapshot.
/// Uses <see cref="GridManager.GetCell"/> and <see cref="TerrainManager.GetHeightMap"/> — no direct grid array access.
/// </summary>
public static class InterchangeJsonReportsMenu
{
    const string MenuRoot = "Territory Developer/Reports/";
    const int DefaultChunkW = 8;
    const int DefaultChunkH = 8;

    [MenuItem(MenuRoot + "Export Cell Chunk (Interchange)", priority = 20)]
    public static void ExportCellChunkInterchange()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Interchange export", "Enter Play Mode with an initialized grid.", "OK");
            return;
        }

        try
        {
            string path = WriteCellChunkInterchangeJson(0, 0, DefaultChunkW, DefaultChunkH);
            EditorUtility.RevealInFinder(path);
            Debug.Log($"[Interchange] Wrote cell chunk: {path}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Interchange] Export Cell Chunk failed: {ex.Message}");
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
            string path = WriteWorldSnapshotDevJson(includeHeightRaster: true);
            EditorUtility.RevealInFinder(path);
            Debug.Log($"[Interchange] Wrote world snapshot: {path}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Interchange] Export World Snapshot failed: {ex.Message}");
        }
    }

    static string WriteCellChunkInterchangeJson(int x0, int y0, int w, int h)
    {
        EnsureReportsDirectory();
        string stamp = UtcTimestampForFilename();
        string filePath = Path.Combine(GetReportsDirectory(), $"cell-chunk-interchange-{stamp}.json");

        GridManager grid = UnityEngine.Object.FindObjectOfType<GridManager>();
        TerrainManager terrain = grid != null ? grid.terrainManager : UnityEngine.Object.FindObjectOfType<TerrainManager>();
        if (grid == null || !grid.isInitialized)
            throw new InvalidOperationException("GridManager missing or not initialized.");
        HeightMap hm = terrain != null ? terrain.GetHeightMap() : null;

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
                Cell cell = grid.GetCell(x, y);
                int cellHeight = cell != null ? cell.height : int.MinValue;
                if (cell != null && hm != null && hm.IsValidPosition(x, y) && heightMapValue != cellHeight)
                {
                    Debug.LogWarning($"[Interchange] HeightMap vs Cell.height mismatch at ({x},{y}): map={heightMapValue} cell={cellHeight}. Export uses HeightMap as authority.");
                }

                int exportHeight = hm != null && hm.IsValidPosition(x, y) ? heightMapValue : cellHeight;
                cells.Add(new CellChunkCellDto
                {
                    x = x,
                    y = y,
                    height = exportHeight,
                    prefabName = cell != null ? cell.prefabName ?? "" : "",
                    waterBodyType = cell != null ? cell.waterBodyType.ToString() : "",
                    waterBodyId = cell != null ? cell.waterBodyId : 0
                });
            }
        }

        var root = new CellChunkInterchangeRootDto
        {
            artifact = "terrain_cell_chunk",
            schema_version = 1,
            origin_x = x0,
            origin_y = y0,
            width = w,
            height = h,
            cells = cells.ToArray(),
            notes = "Subset fields for tooling (TECH-41 G2). height is HeightMap when available, else Cell.height."
        };

        string json = JsonUtility.ToJson(root, true);
        File.WriteAllText(filePath, json, Encoding.UTF8);
        return filePath;
    }

    static string WriteWorldSnapshotDevJson(bool includeHeightRaster)
    {
        EnsureReportsDirectory();
        string stamp = UtcTimestampForFilename();
        string filePath = Path.Combine(GetReportsDirectory(), $"world-snapshot-dev-{stamp}.json");

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

        string json = JsonUtility.ToJson(root, true);
        File.WriteAllText(filePath, json, Encoding.UTF8);
        return filePath;
    }

    static void EnsureReportsDirectory()
    {
        string dir = GetReportsDirectory();
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    static string GetReportsDirectory()
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        return Path.Combine(projectRoot, "tools", "reports");
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
