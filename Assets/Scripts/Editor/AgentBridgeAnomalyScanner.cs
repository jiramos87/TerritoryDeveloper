using System;
using System.Collections.Generic;
using Territory.Core;
using Territory.Terrain;
using UnityEngine;

/// <summary>
/// Rule-based anomaly scan for <c>debug_context_bundle</c> Moore neighborhood exports (IDE agent bridge).
/// Operates only on 3×3 neighborhood around seed cell; does not walk full grid.
/// </summary>
public static class AgentBridgeAnomalyScanner
{
    /// <summary>
    /// Scan Moore neighborhood (up to 9 in-bounds cells) around <paramref name="centerX"/>, <paramref name="centerY"/>.
    /// </summary>
    public static List<AgentBridgeAnomalyRecordDto> ScanNeighborhood(int centerX, int centerY)
    {
        var results = new List<AgentBridgeAnomalyRecordDto>();
        GridManager grid = UnityEngine.Object.FindObjectOfType<GridManager>();
        if (grid == null || !grid.isInitialized)
            return results;

        TerrainManager terrain = grid.terrainManager != null
            ? grid.terrainManager
            : UnityEngine.Object.FindObjectOfType<TerrainManager>();
        if (terrain == null)
            return results;

        HeightMap heightMap = terrain.GetHeightMap();
        if (heightMap == null)
            return results;

        WaterManager waterManager = UnityEngine.Object.FindObjectOfType<WaterManager>();

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                int x = centerX + dx;
                int y = centerY + dy;
                if (!heightMap.IsValidPosition(x, y))
                    continue;

                CityCell cell = grid.GetCell(x, y);
                if (cell == null)
                    continue;

                results.AddRange(CheckMissingBorderCliffs(cell, x, y, heightMap, waterManager));
                results.AddRange(CheckHeightMapCellDesync(cell, x, y, heightMap));
                results.AddRange(CheckRedundantShoreCliff(cell, x, y, heightMap, terrain, waterManager));
            }
        }

        return results;
    }

    static IEnumerable<AgentBridgeAnomalyRecordDto> CheckMissingBorderCliffs(
        CityCell cell,
        int x,
        int y,
        HeightMap heightMap,
        WaterManager waterManager)
    {
        if (waterManager != null && waterManager.IsWaterAt(x, y))
            yield break;

        bool voidSouth = !heightMap.IsValidPosition(x - 1, y);
        bool voidEast = !heightMap.IsValidPosition(x, y - 1);

        if (voidSouth &&
            cell.height > TerrainManager.MIN_HEIGHT &&
            (cell.cliffFaces & CliffFaceFlags.South) != 0 &&
            !CellTransformHasCliffNamedChild(cell.transform))
        {
            yield return new AgentBridgeAnomalyRecordDto
            {
                rule = "missing_border_cliff",
                cell_x = x,
                cell_y = y,
                severity = "warning",
                message =
                    $"South map border: cliffFaces includes South but no cliff-named child under cell (height={cell.height}).",
            };
        }

        if (voidEast &&
            cell.height > TerrainManager.MIN_HEIGHT &&
            (cell.cliffFaces & CliffFaceFlags.East) != 0 &&
            !CellTransformHasCliffNamedChild(cell.transform))
        {
            yield return new AgentBridgeAnomalyRecordDto
            {
                rule = "missing_border_cliff",
                cell_x = x,
                cell_y = y,
                severity = "warning",
                message =
                    $"East map border: cliffFaces includes East but no cliff-named child under cell (height={cell.height}).",
            };
        }
    }

    static IEnumerable<AgentBridgeAnomalyRecordDto> CheckHeightMapCellDesync(
        CityCell cell,
        int x,
        int y,
        HeightMap heightMap)
    {
        int mapH = heightMap.GetHeight(x, y);
        if (mapH != cell.height)
        {
            yield return new AgentBridgeAnomalyRecordDto
            {
                rule = "heightmap_cell_desync",
                cell_x = x,
                cell_y = y,
                severity = "error",
                message = $"HeightMap height {mapH} != CityCell.height {cell.height} (invariant: keep both in sync).",
            };
        }
    }

    static IEnumerable<AgentBridgeAnomalyRecordDto> CheckRedundantShoreCliff(
        CityCell cell,
        int x,
        int y,
        HeightMap heightMap,
        TerrainManager terrain,
        WaterManager waterManager)
    {
        if (!terrain.DoesCellUseWaterShorePrimaryPrefab(cell))
            yield break;

        if (waterManager != null && waterManager.IsWaterAt(x, y))
            yield break;

        bool voidSouth = !heightMap.IsValidPosition(x - 1, y);
        bool voidEast = !heightMap.IsValidPosition(x, y - 1);

        if (voidSouth && CellTransformHasDirectionalBrownCliffChild(cell.transform, southOrEastSouth: true))
        {
            yield return new AgentBridgeAnomalyRecordDto
            {
                rule = "redundant_shore_cliff",
                cell_x = x,
                cell_y = y,
                severity = "warning",
                message =
                    "Water-shore primary cell toward south void: south-facing brown cliff-like child present; terrain rules suppress duplicate brown stacks toward off-grid void.",
            };
        }

        if (voidEast && CellTransformHasDirectionalBrownCliffChild(cell.transform, southOrEastSouth: false))
        {
            yield return new AgentBridgeAnomalyRecordDto
            {
                rule = "redundant_shore_cliff",
                cell_x = x,
                cell_y = y,
                severity = "warning",
                message =
                    "Water-shore primary cell toward east void: east-facing brown cliff-like child present; terrain rules suppress duplicate brown stacks toward off-grid void.",
            };
        }
    }

    static bool CellTransformHasCliffNamedChild(Transform root)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            string n = root.GetChild(i).name;
            if (!string.IsNullOrEmpty(n) && n.IndexOf("Cliff", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Brown land cliff stack (not water-cascade): Cliff in name, no Water; South or East in name → match void-facing stacks.
    /// </summary>
    static bool CellTransformHasDirectionalBrownCliffChild(Transform root, bool southOrEastSouth)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            string n = root.GetChild(i).name;
            if (string.IsNullOrEmpty(n))
                continue;
            if (n.IndexOf("Cliff", StringComparison.OrdinalIgnoreCase) < 0)
                continue;
            if (n.IndexOf("Water", StringComparison.OrdinalIgnoreCase) >= 0)
                continue;
            if (southOrEastSouth)
            {
                if (n.IndexOf("South", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            else
            {
                if (n.IndexOf("East", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
        }

        return false;
    }
}
