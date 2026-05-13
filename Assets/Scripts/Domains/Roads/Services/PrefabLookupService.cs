using System.Collections.Generic;
using UnityEngine;
using Territory.Core;
using Territory.Zones;
using Territory.Roads;

namespace Domains.Roads.Services
{
/// <summary>
/// Topology classification, neighbor lookup, connectivity helpers for prefab resolution.
/// Extracted from PrefabResolverService (Stage 7.3 Tier-E split).
/// </summary>
internal class PrefabLookupService
{
    private readonly IGridManager gridManager;
    private readonly IRoadManager roadManager;

    internal PrefabLookupService(IGridManager grid, IRoadManager roads)
    {
        gridManager = grid;
        roadManager = roads;
    }

    internal static Vector2Int GetCardinalGridStepRounded(Vector2 delta)
    {
        int x = Mathf.RoundToInt(delta.x);
        int y = Mathf.RoundToInt(delta.y);
        if (x == 0 && y == 0) return Vector2Int.zero;
        if (Mathf.Abs(x) >= Mathf.Abs(y) && x != 0) return new Vector2Int(x > 0 ? 1 : -1, 0);
        if (y != 0) return new Vector2Int(0, y > 0 ? 1 : -1);
        return Vector2Int.zero;
    }

    internal static PathRouteTopology ClassifyPathRouteTopology(bool pathLeft, bool pathRight, bool pathUp, bool pathDown)
    {
        int cardinalCount = (pathLeft ? 1 : 0) + (pathRight ? 1 : 0) + (pathUp ? 1 : 0) + (pathDown ? 1 : 0);
        if (cardinalCount >= 3) return PathRouteTopology.Junction;
        if (cardinalCount == 2)
        {
            bool straightH = pathLeft && pathRight && !pathUp && !pathDown;
            bool straightV = pathUp && pathDown && !pathLeft && !pathRight;
            if (straightH || straightV) return PathRouteTopology.StraightThrough;
            return PathRouteTopology.Corner90;
        }
        if (cardinalCount == 1) return PathRouteTopology.End;
        return PathRouteTopology.Isolated;
    }

    internal bool PathNeighborForResolve(HashSet<Vector2Int> pathCellSet, int nx, int ny, bool pathOnlyNeighbors)
    {
        if (pathCellSet != null && pathCellSet.Contains(new Vector2Int(nx, ny))) return true;
        if (!pathOnlyNeighbors) return IsRoadAt(new Vector2(nx, ny));
        return false;
    }

    internal bool IsRoadAt(Vector2 gridPos)
    {
        int gridX = Mathf.RoundToInt(gridPos.x);
        int gridY = Mathf.RoundToInt(gridPos.y);
        if (gridX < 0 || gridX >= gridManager.width || gridY < 0 || gridY >= gridManager.height) return false;
        var cell = gridManager.GetGridCell(new Vector2(gridX, gridY));
        if (cell == null || cell.transform.childCount == 0) return false;
        var cellComponent = gridManager.GetCell(gridX, gridY);
        if (cellComponent != null && cellComponent.zoneType == Zone.ZoneType.Road) return true;
        for (int i = 0; i < cell.transform.childCount; i++)
        {
            var zone = cell.transform.GetChild(i).GetComponent<Zone>();
            if (zone != null && zone.zoneType == Zone.ZoneType.Road) return true;
        }
        return false;
    }

    internal bool IsValidGrid(int gx, int gy) => gx >= 0 && gx < gridManager.width && gy >= 0 && gy < gridManager.height;

    internal int GetNeighborHeight(int gridX, int gridY, int dx, int dy)
    {
        int nx = gridX + dx;
        int ny = gridY + dy;
        if (nx < 0 || nx >= gridManager.width || ny < 0 || ny >= gridManager.height) return int.MinValue;
        CityCell c = gridManager.GetCell(nx, ny);
        return c != null ? c.GetCellInstanceHeight() : int.MinValue;
    }

    internal GameObject TryGetElbowPrefab(bool hasLeft, bool hasRight, bool hasUp, bool hasDown)
    {
        if (hasLeft && hasUp && !hasRight && !hasDown) return roadManager.roadTilePrefabElbowDownRight;
        if (hasRight && hasUp && !hasLeft && !hasDown) return roadManager.roadTilePrefabElbowDownLeft;
        if (hasLeft && hasDown && !hasRight && !hasUp) return roadManager.roadTilePrefabElbowUpRight;
        if (hasRight && hasDown && !hasLeft && !hasUp) return roadManager.roadTilePrefabElbowUpLeft;
        return null;
    }

    internal GameObject SelectFromConnectivity(Vector2 prevGridPos, Vector2 currGridPos,
        bool hasLeft, bool hasRight, bool hasUp, bool hasDown, int height,
        PrefabVariantPickService variantPick)
    {
        Vector2 direction = currGridPos - prevGridPos;
        if (hasLeft && hasRight && hasUp && hasDown) return roadManager.roadTilePrefabCrossing;
        if (hasLeft && hasRight && hasUp && !hasDown) return roadManager.roadTilePrefabTIntersectionDown;
        if (hasLeft && hasRight && hasDown && !hasUp) return roadManager.roadTilePrefabTIntersectionUp;
        if (hasUp && hasDown && hasLeft && !hasRight) return roadManager.roadTilePrefabTIntersectionRight;
        if (hasUp && hasDown && hasRight && !hasLeft) return roadManager.roadTilePrefabTIntersectionLeft;
        GameObject elbowPrefab = TryGetElbowPrefab(hasLeft, hasRight, hasUp, hasDown);
        if (elbowPrefab != null) return elbowPrefab;

        bool isHorizontal = hasLeft || hasRight;
        GameObject slopePrefab = variantPick.TryGetSlopePrefabForStraightSegment(currGridPos, height, isHorizontal, prevGridPos);
        if (slopePrefab != null) return slopePrefab;

        if (hasLeft && hasRight && !hasUp && !hasDown) return height == 0 ? roadManager.roadTileBridgeHorizontal : roadManager.roadTilePrefab2;
        if (hasUp && hasDown && !hasLeft && !hasRight) return height == 0 ? roadManager.roadTileBridgeVertical : roadManager.roadTilePrefab1;
        if (hasLeft || hasRight) return height == 0 ? roadManager.roadTileBridgeHorizontal : roadManager.roadTilePrefab2;
        if (hasUp || hasDown) return height == 0 ? roadManager.roadTileBridgeVertical : roadManager.roadTilePrefab1;

        bool fallbackHorizontal = Mathf.Abs(direction.x) >= Mathf.Abs(direction.y);
        slopePrefab = variantPick.TryGetSlopePrefabForStraightSegment(currGridPos, height, fallbackHorizontal, prevGridPos);
        if (slopePrefab != null) return slopePrefab;
        return fallbackHorizontal
            ? (height == 0 ? roadManager.roadTileBridgeHorizontal : roadManager.roadTilePrefab2)
            : (height == 0 ? roadManager.roadTileBridgeVertical : roadManager.roadTilePrefab1);
    }
}
}
