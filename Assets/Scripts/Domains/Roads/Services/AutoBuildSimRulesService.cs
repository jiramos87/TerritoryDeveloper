using UnityEngine;
using System.Collections.Generic;
using Territory.Core;
using Territory.Economy;
using Territory.Utilities;
using Territory.Simulation;
using Territory.Roads;
using Territory.Terrain;
using Territory.Zones;
using Random = UnityEngine.Random;

namespace Domains.Roads.Services
{
/// <summary>
/// Sim-rules sub-service: terrain/cell suitability predicates + direction utility scoring.
/// Extracted from AutoBuildService (Stage 7.0 Tier-E split). No MonoBehaviour.
/// </summary>
public class AutoBuildSimRulesService
{
    // Cardinal offsets shared across sub-services (duplicate avoids cross-service coupling)
    public static readonly int[] Dx = { 1, -1, 0, 0 };
    public static readonly int[] Dy = { 0, 0, 1, -1 };

    private IGridManager _gridManager;
    private ITerrainManager _terrainManager;
    private IUrbanCentroidService _urbanCentroidService;

    public const int MaxBridgeWaterTiles = 5;

    /// <summary>Construct sim-rules service — grid + terrain + centroid.</summary>
    public AutoBuildSimRulesService(
        IGridManager gridManager,
        ITerrainManager terrainManager,
        IUrbanCentroidService urbanCentroidService)
    {
        _gridManager = gridManager;
        _terrainManager = terrainManager;
        _urbanCentroidService = urbanCentroidService;
    }

    /// <summary>Re-wire dependencies after registry resolve.</summary>
    public void RefreshDependencies(
        IGridManager gridManager,
        ITerrainManager terrainManager,
        IUrbanCentroidService urbanCentroidService)
    {
        _gridManager = gridManager;
        _terrainManager = terrainManager;
        _urbanCentroidService = urbanCentroidService;
    }

    /// <summary>True if cell can host an auto-placed road tile (land or water bridge).</summary>
    public bool IsCellPlaceableForRoad(int x, int y)
    {
        CityCell c = _gridManager.GetCell(x, y);
        if (c == null || c.zoneType == Zone.ZoneType.Road) return false;
        if (_gridManager.IsCellOccupiedByBuilding(x, y) || c.isInterstate) return false;
        bool isLandPlaceable = AutoSimulationRoadRules.IsAutoRoadLandCell(_gridManager, x, y);
        bool isWaterForBridge = c.GetCellInstanceHeight() == 0;
        if (!isLandPlaceable && !isWaterForBridge) return false;
        if (_terrainManager == null) return false;
        return _terrainManager.CanPlaceRoad(x, y, allowWaterSlopeForWaterBridgeTrace: true);
    }

    /// <summary>Reason text for why cell fails IsCellPlaceableForRoad.</summary>
    public string GetCellPlaceableRejectReason(int x, int y)
    {
        CityCell c = _gridManager.GetCell(x, y);
        if (c == null) return "null cell";
        if (c.zoneType == Zone.ZoneType.Road) return "already road";
        if (_gridManager.IsCellOccupiedByBuilding(x, y)) return "building";
        if (c.isInterstate) return "interstate";
        bool isLand = AutoSimulationRoadRules.IsAutoRoadLandCell(_gridManager, x, y);
        bool isWater = c.GetCellInstanceHeight() == 0;
        if (!isLand && !isWater) return "zone not grass/light-zoning/water";
        if (_terrainManager != null && !_terrainManager.CanPlaceRoad(x, y, allowWaterSlopeForWaterBridgeTrace: true)) return "terrain/slope";
        return "unknown";
    }

    /// <summary>True if cell slope kind permits a road in given direction.</summary>
    public bool IsSuitableForRoad(int x, int y, Vector2Int streetDir)
    {
        CityCell c = _gridManager.GetCell(x, y);
        if (c != null && c.GetCellInstanceHeight() == 0)
            return true;
        if (_terrainManager != null && _terrainManager.IsWaterSlopeCell(x, y))
            return _terrainManager.CanPlaceRoad(x, y, allowWaterSlopeForWaterBridgeTrace: true);
        if (_terrainManager == null) return true;
        TerrainSlopeType slope = _terrainManager.GetTerrainSlopeTypeAt(x, y);
        switch (slope)
        {
            case TerrainSlopeType.Flat:
            case TerrainSlopeType.North:
            case TerrainSlopeType.South:
            case TerrainSlopeType.East:
            case TerrainSlopeType.West:
            case TerrainSlopeType.NorthEast:
            case TerrainSlopeType.NorthWest:
            case TerrainSlopeType.SouthEast:
            case TerrainSlopeType.SouthWest:
            case TerrainSlopeType.NorthEastUp:
            case TerrainSlopeType.NorthWestUp:
            case TerrainSlopeType.SouthEastUp:
            case TerrainSlopeType.SouthWestUp:
                return true;
            default:
                return false;
        }
    }

    /// <summary>True if cell beyond tip+len*dir blocked by water or slope.</summary>
    public bool IsDirectionBlockedBySlopeOrWater(Vector2Int tip, Vector2Int dir, int len)
    {
        int bx = tip.x + len * dir.x, by = tip.y + len * dir.y;
        if (bx < 0 || bx >= _gridManager.width || by < 0 || by >= _gridManager.height) return false;
        CityCell c = _gridManager.GetCell(bx, by);
        if (c == null) return false;
        if (c.GetCellInstanceHeight() == 0) return true;
        if (_terrainManager == null) return false;
        TerrainSlopeType slope = _terrainManager.GetTerrainSlopeTypeAt(bx, by);
        return slope != TerrainSlopeType.Flat;
    }

    /// <summary>Count of cardinal grass/forest neighbors at road cell.</summary>
    public int CountGrassNeighbors(Vector2Int roadPos)
    {
        int count = 0;
        for (int i = 0; i < 4; i++)
        {
            int nx = roadPos.x + Dx[i], ny = roadPos.y + Dy[i];
            if (nx < 0 || nx >= _gridManager.width || ny < 0 || ny >= _gridManager.height) continue;
            CityCell c = _gridManager.GetCell(nx, ny);
            if (c != null && (c.zoneType == Zone.ZoneType.Grass || c.HasForest())) count++;
        }
        return count;
    }

    /// <summary>True if edge cell sits on an interstate.</summary>
    public bool IsEdgeOnInterstate(Vector2Int edge)
    {
        CityCell c = _gridManager.GetCell(edge.x, edge.y);
        return c != null && c.isInterstate;
    }

    /// <summary>True if parallel road exists within minSpacing perpendicular to dir.</summary>
    public bool HasParallelRoadTooClose(Vector2Int edge, Vector2Int dir, int minSpacing, HashSet<Vector2Int> roadSet, Vector2Int? excludeAlongDir = null)
    {
        Vector2Int perp = new Vector2Int(-dir.y, dir.x);
        HashSet<Vector2Int> excludeSet = null;
        if (excludeAlongDir.HasValue)
        {
            excludeSet = new HashSet<Vector2Int>();
            Vector2Int e = excludeAlongDir.Value;
            for (int k = -minSpacing; k <= minSpacing; k++)
            {
                if (k == 0) continue;
                Vector2Int p = new Vector2Int(edge.x + e.x * k, edge.y + e.y * k);
                excludeSet.Add(p);
            }
        }
        for (int s = 1; s <= minSpacing; s++)
        {
            Vector2Int offset = new Vector2Int(edge.x + perp.x * s, edge.y + perp.y * s);
            if (offset.x < 0 || offset.x >= _gridManager.width || offset.y < 0 || offset.y >= _gridManager.height)
                continue;
            if (excludeSet != null && excludeSet.Contains(offset)) continue;
            if (roadSet.Contains(offset))
                return true;
            Vector2Int otherSide = new Vector2Int(edge.x - perp.x * s, edge.y - perp.y * s);
            if (otherSide.x >= 0 && otherSide.x < _gridManager.width && otherSide.y >= 0 && otherSide.y < _gridManager.height)
            {
                if (excludeSet != null && excludeSet.Contains(otherSide)) continue;
                if (roadSet.Contains(otherSide))
                    return true;
            }
        }
        return false;
    }

    /// <summary>Mean cell desirability along direction over sampleCount steps.</summary>
    public float GetAverageDesirabilityInDirection(Vector2Int start, Vector2Int dir, int sampleCount)
    {
        float sum = 0f;
        int count = 0;
        int x = start.x, y = start.y;
        int w = _gridManager.width, h = _gridManager.height;
        for (int k = 0; k < sampleCount; k++)
        {
            x += dir.x;
            y += dir.y;
            if (x < 0 || x >= w || y < 0 || y >= h) break;
            CityCell c = _gridManager.GetCell(x, y);
            if (c != null)
            {
                sum += c.desirability;
                count++;
            }
        }
        return count > 0 ? sum / count : 0f;
    }

    /// <summary>Count grass/forest cells within radius along sampleLen path.</summary>
    public int CountUnzonedCellsNearPath(Vector2Int start, Vector2Int dir, int sampleLen, int radius)
    {
        int count = 0;
        int w = _gridManager.width, h = _gridManager.height;
        int x = start.x, y = start.y;
        for (int k = 0; k < sampleLen; k++)
        {
            x += dir.x;
            y += dir.y;
            if (x < 0 || x >= w || y < 0 || y >= h) break;
            for (int rx = -radius; rx <= radius; rx++)
            {
                for (int ry = -radius; ry <= radius; ry++)
                {
                    int nx = x + rx, ny = y + ry;
                    if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                    CityCell c = _gridManager.GetCell(nx, ny);
                    if (c != null && (c.zoneType == Zone.ZoneType.Grass || c.HasForest()))
                        count++;
                }
            }
        }
        return count;
    }

    /// <summary>True if roads flank both sides of edge within parallelSpacing.</summary>
    public bool IsDirectionEnclosed(Vector2Int edge, Vector2Int dir, int parallelSpacing, HashSet<Vector2Int> roadSet)
    {
        Vector2Int perp = new Vector2Int(-dir.y, dir.x);
        bool hasRoadLeft = false, hasRoadRight = false;
        for (int s = 1; s <= Mathf.Min(parallelSpacing, 5); s++)
        {
            int x = edge.x + dir.x * s, y = edge.y + dir.y * s;
            if (x < 0 || x >= _gridManager.width || y < 0 || y >= _gridManager.height) break;
            Vector2Int left = new Vector2Int(edge.x + perp.x * s, edge.y + perp.y * s);
            Vector2Int right = new Vector2Int(edge.x - perp.x * s, edge.y - perp.y * s);
            if (left.x >= 0 && left.x < _gridManager.width && left.y >= 0 && left.y < _gridManager.height && roadSet.Contains(left))
                hasRoadLeft = true;
            if (right.x >= 0 && right.x < _gridManager.width && right.y >= 0 && right.y < _gridManager.height && roadSet.Contains(right))
                hasRoadRight = true;
        }
        return hasRoadLeft && hasRoadRight;
    }

    /// <summary>Score direction = 2×desirability + unzoned − 50 if enclosed.</summary>
    public float CalculateDirectionUtility(Vector2Int edge, Vector2Int dir, int sampleLen, int parallelSpacing, HashSet<Vector2Int> roadSet)
    {
        float desir = GetAverageDesirabilityInDirection(edge, dir, sampleLen);
        int unzoned = CountUnzonedCellsNearPath(edge, dir, sampleLen, 2);
        bool enclosed = IsDirectionEnclosed(edge, dir, parallelSpacing, roadSet);
        return desir * 2f + unzoned * 1f - (enclosed ? 50f : 0f);
    }

    /// <summary>Ring-priority score (Inner highest).</summary>
    public static int GetRingPriority(UrbanRing ring)
    {
        switch (ring)
        {
            case UrbanRing.Inner: return 6;
            case UrbanRing.Mid: return 5;
            case UrbanRing.Outer: return 4;
            case UrbanRing.Rural: return 3;
            default: return 4;
        }
    }

    /// <summary>Inner ring min edge spacing override; else passthrough.</summary>
    public int GetEffectiveMinEdgeSpacing(UrbanRing ring, int minEdgeSpacing, int coreInnerMinEdgeSpacing)
    {
        if ((ring == UrbanRing.Inner) && minEdgeSpacing > coreInnerMinEdgeSpacing)
            return minEdgeSpacing - coreInnerMinEdgeSpacing;
        return minEdgeSpacing;
    }

    /// <summary>Random parallel spacing in [min,max+1]; fallback to fixed.</summary>
    public static int GetEffectiveParallelSpacing(RingStreetParams p)
    {
        return p.parallelSpacingMax > p.parallelSpacingMin
            ? Random.Range(p.parallelSpacingMin, p.parallelSpacingMax + 1)
            : p.parallelSpacing;
    }

    /// <summary>Count inner-ring cells in edge list via centroid service.</summary>
    public int CountInnerEdges(List<Vector2Int> edges)
    {
        if (_urbanCentroidService == null || edges == null) return 0;
        int count = 0;
        foreach (Vector2Int e in edges)
        {
            UrbanRing ring = _urbanCentroidService.GetUrbanRing(new Vector2(e.x, e.y));
            if (ring == UrbanRing.Inner)
                count++;
        }
        return count;
    }
}
}
