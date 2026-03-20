using System.Collections.Generic;
using UnityEngine;
using Territory.Core;
using Territory.Terrain;
using Territory.Zones;

namespace Territory.Roads
{
/// <summary>
/// Centralized road prefab selection. Resolves prefab, world position, and sorting order
/// for path-based placement (uses PathTerraformPlan) or single-cell placement (uses
/// neighbor connectivity and live terrain). Extracted from RoadManager for reuse by
/// manual tools, interstate, and AutoRoadBuilder.
/// </summary>
public class RoadPrefabResolver
{
    private readonly GridManager gridManager;
    private readonly TerrainManager terrainManager;
    private readonly RoadManager roadManager;

    private static readonly int[] DirX = { 1, -1, 0, 0 };
    private static readonly int[] DirY = { 0, 0, 1, -1 };

    public RoadPrefabResolver(GridManager grid, TerrainManager terrain, RoadManager roads)
    {
        gridManager = grid;
        terrainManager = terrain;
        roadManager = roads;
    }

    /// <summary>
    /// Result of prefab resolution: prefab to instantiate, world position, and sorting order.
    /// </summary>
    public struct ResolvedRoadTile
    {
        public Vector2Int gridPos;
        public GameObject prefab;
        public Vector2 worldPos;
        public int sortingOrder;
    }

    /// <summary>
    /// Resolves prefabs for a full path using the terraform plan. Uses postTerraformSlopeType
    /// from the plan (Rule 5: terraform wins). Ensures continuity (Rule 1) and elbows at turns (Rule 2).
    /// </summary>
    public List<ResolvedRoadTile> ResolveForPath(List<Vector2> path, PathTerraformPlan plan)
    {
        var result = new List<ResolvedRoadTile>();
        if (path == null || path.Count == 0) return result;

        var pathCellSet = new HashSet<Vector2Int>();
        for (int j = 0; j < path.Count; j++)
            pathCellSet.Add(new Vector2Int(Mathf.RoundToInt(path[j].x), Mathf.RoundToInt(path[j].y)));

        int skipped = 0;
        for (int i = 0; i < path.Count; i++)
        {
            Vector2 curr = path[i];
            Vector2 prev = i > 0 ? path[i - 1] : (path.Count > 1 ? 2 * curr - path[1] : curr);

            PathTerraformPlan.CellPlan cellPlan = default;
            if (plan != null && plan.pathCells != null && i < plan.pathCells.Count)
                cellPlan = plan.pathCells[i];

            int x = (int)curr.x;
            int y = (int)curr.y;
            Cell cell = gridManager.GetCell(x, y);
            if (cell == null)
            {
                skipped++;
                continue;
            }

            int height = cell.GetCellInstanceHeight();
            TerrainSlopeType postSlope = cellPlan.postTerraformSlopeType;

            GameObject prefab = ResolvePrefabForPathCell(prev, curr, pathCellSet, height, postSlope);
#if UNITY_EDITOR
            if (Debug.isDebugBuild && prefab != null && !ValidatePrefabExitsMatchPath(curr, pathCellSet, prefab))
                Debug.LogWarning($"[RoadPrefab] Mismatch at ({curr.x},{curr.y}): prefab={prefab.name}");
#endif
            Vector2 worldPos = GetWorldPositionForPrefab(x, y, prefab, height, postSlope);
            int sortingOrder = gridManager.GetRoadSortingOrderForCell(x, y, height);

            result.Add(new ResolvedRoadTile
            {
                gridPos = new Vector2Int(x, y),
                prefab = prefab ?? roadManager.roadTilePrefab1,
                worldPos = worldPos,
                sortingOrder = sortingOrder
            });
        }
        return result;
    }

    /// <summary>
    /// Resolves prefab for a single cell using neighbor connectivity. Used for RefreshRoadPrefabAt
    /// and PlaceRoadTileAt when no path context exists.
    /// </summary>
    public ResolvedRoadTile? ResolveForCell(Vector2 currGridPos, Vector2 prevGridPos)
    {
        int x = (int)currGridPos.x;
        int y = (int)currGridPos.y;
        Cell cell = gridManager.GetCell(x, y);
        if (cell == null) return null;

        bool hasLeft = IsRoadAt(currGridPos + new Vector2(-1, 0));
        bool hasRight = IsRoadAt(currGridPos + new Vector2(1, 0));
        bool hasUp = IsRoadAt(currGridPos + new Vector2(0, 1));
        bool hasDown = IsRoadAt(currGridPos + new Vector2(0, -1));

        int height = cell.GetCellInstanceHeight();
        GameObject prefab;
        if (height == 0)
        {
            bool hasHorizontal = hasLeft || hasRight;
            bool hasVertical = hasUp || hasDown;
            bool isHorizontal = hasHorizontal && !hasVertical
                ? true
                : !hasHorizontal && hasVertical
                    ? false
                    : Mathf.Abs((currGridPos - prevGridPos).x) >= Mathf.Abs((currGridPos - prevGridPos).y);
            prefab = isHorizontal ? roadManager.roadTileBridgeHorizontal : roadManager.roadTileBridgeVertical;
        }
        else if (terrainManager != null && terrainManager.IsWaterSlopeCell(x, y))
        {
            Vector2 dirIn = currGridPos - prevGridPos;
            bool isHorizontal = Mathf.Abs(dirIn.x) >= Mathf.Abs(dirIn.y);
            prefab = isHorizontal ? roadManager.roadTileBridgeHorizontal : roadManager.roadTileBridgeVertical;
        }
        else
        {
            prefab = SelectFromConnectivity(prevGridPos, currGridPos, hasLeft, hasRight, hasUp, hasDown, height);
        }
        Vector2 worldPos = GetWorldPositionForPrefab(x, y, prefab, height, terrainManager?.GetTerrainSlopeTypeAt(x, y) ?? TerrainSlopeType.Flat);

        return new ResolvedRoadTile
        {
            gridPos = new Vector2Int(x, y),
            prefab = prefab ?? roadManager.roadTilePrefab1,
            worldPos = worldPos,
            sortingOrder = gridManager.GetRoadSortingOrderForCell(x, y, height)
        };
    }

    /// <summary>
    /// Resolves prefab for ghost preview (single cell, no path). Slope cells get slope prefab, water gets bridge.
    /// </summary>
    public void ResolveForGhostPreview(Vector2 gridPos, out GameObject prefab, out Vector2 worldPos, out int sortingOrder)
    {
        int x = (int)gridPos.x;
        int y = (int)gridPos.y;
        prefab = roadManager.roadTilePrefab1;
        worldPos = gridManager.GetWorldPosition(x, y);
        sortingOrder = gridManager.GetRoadSortingOrderForCell(x, y, 0);

        Cell cell = gridManager.GetCell(x, y);
        if (cell == null) return;

        int height = cell.GetCellInstanceHeight();

        if (height == 0)
        {
            prefab = roadManager.roadTileBridgeVertical;
            worldPos = gridManager.GetWorldPositionVector(x, y, 1);
            sortingOrder = gridManager.GetRoadSortingOrderForCell(x, y, 1);
            return;
        }

        if (terrainManager != null && terrainManager.IsWaterSlopeCell(x, y))
        {
            prefab = roadManager.roadTileBridgeVertical;
            worldPos = gridManager.GetWorldPosition(x, y);
            sortingOrder = gridManager.GetRoadSortingOrderForCell(x, y, height);
            return;
        }

        GameObject slopePrefab = TryGetSlopePrefabForCell(new Vector2(x, y), height);
        if (slopePrefab != null)
        {
            prefab = slopePrefab;
            worldPos = GetWorldPositionForPrefab(x, y, slopePrefab, height, terrainManager?.GetTerrainSlopeTypeAt(x, y) ?? TerrainSlopeType.Flat);
            sortingOrder = gridManager.GetRoadSortingOrderForCell(x, y, height);
            return;
        }

        worldPos = GetWorldPositionForPrefab(x, y, roadManager.roadTilePrefab1, height, TerrainSlopeType.Flat);
        sortingOrder = gridManager.GetRoadSortingOrderForCell(x, y, height);
    }

    private GameObject ResolvePrefabForPathCell(Vector2 prev, Vector2 curr, HashSet<Vector2Int> pathCellSet, int height, TerrainSlopeType postSlope)
    {
        Vector2 dirIn = curr - prev;
        int dxIn = Mathf.RoundToInt(dirIn.x);
        int dyIn = Mathf.RoundToInt(dirIn.y);

        int cx = Mathf.RoundToInt(curr.x);
        int cy = Mathf.RoundToInt(curr.y);

        if (height == 0)
        {
            bool isHorizontal = Mathf.Abs(dirIn.x) >= Mathf.Abs(dirIn.y);
            return isHorizontal ? roadManager.roadTileBridgeHorizontal : roadManager.roadTileBridgeVertical;
        }

        if (terrainManager != null && terrainManager.IsWaterSlopeCell(cx, cy))
        {
            bool isHorizontal = Mathf.Abs(dirIn.x) >= Mathf.Abs(dirIn.y);
            return isHorizontal ? roadManager.roadTileBridgeHorizontal : roadManager.roadTileBridgeVertical;
        }

        bool pathLeft = PathCellContainsOrRoad(pathCellSet, cx - 1, cy);
        bool pathRight = PathCellContainsOrRoad(pathCellSet, cx + 1, cy);
        bool pathUp = PathCellContainsOrRoad(pathCellSet, cx, cy + 1);
        bool pathDown = PathCellContainsOrRoad(pathCellSet, cx, cy - 1);

        if (pathLeft || pathRight || pathUp || pathDown)
        {
            // Elbow mapping: single source of truth (Rule A, B). Left+Up=ElbowDownRight, Right+Up=ElbowDownLeft, Left+Down=ElbowUpRight, Right+Down=ElbowUpLeft.
            GameObject elbowPrefab = TryGetElbowPrefab(pathLeft, pathRight, pathUp, pathDown);
            if (elbowPrefab != null) return elbowPrefab;

            int cardinalCount = (pathLeft ? 1 : 0) + (pathRight ? 1 : 0) + (pathUp ? 1 : 0) + (pathDown ? 1 : 0);
            if (cardinalCount >= 3)
                return SelectFromConnectivity(prev, curr, pathLeft, pathRight, pathUp, pathDown, height);

            // Align slope prefab axis to travel direction (prev→curr), not to which path neighbors exist first (BUG-30 corner / T-adjacent cells).
            bool segmentHorizontal = (dxIn != 0 || dyIn != 0)
                ? Mathf.Abs(dxIn) >= Mathf.Abs(dyIn)
                : (pathLeft || pathRight);
            return TrySlopeForStraight(postSlope, segmentHorizontal) ?? (segmentHorizontal ? roadManager.roadTilePrefab2 : roadManager.roadTilePrefab1);
        }

        if (dxIn != 0 && dyIn != 0)
        {
            bool hasOrthogonalNeighbors = (pathLeft || pathRight) && (pathUp || pathDown);
            if (hasOrthogonalNeighbors)
            {
                if (dxIn == 1 && dyIn == 1) return roadManager.roadTilePrefabElbowUpLeft;
                if (dxIn == 1 && dyIn == -1) return roadManager.roadTilePrefabElbowDownLeft;
                if (dxIn == -1 && dyIn == 1) return roadManager.roadTilePrefabElbowUpRight;
                if (dxIn == -1 && dyIn == -1) return roadManager.roadTilePrefabElbowDownRight;
            }
        }

        GameObject slopePrefab = TrySlopeFromPostTerraform(postSlope, Mathf.Abs(dirIn.x) >= Mathf.Abs(dirIn.y));
        if (slopePrefab != null) return slopePrefab;

        return Mathf.Abs(dirIn.x) >= Mathf.Abs(dirIn.y) ? roadManager.roadTilePrefab2 : roadManager.roadTilePrefab1;
    }

    private GameObject TrySlopeFromPostTerraform(TerrainSlopeType postSlope, bool isHorizontal)
    {
        if (postSlope == TerrainSlopeType.Flat) return null;
        switch (postSlope)
        {
            case TerrainSlopeType.North: return roadManager.roadTilePrefabNorthSlope;
            case TerrainSlopeType.South: return roadManager.roadTilePrefabSouthSlope;
            case TerrainSlopeType.East: return roadManager.roadTilePrefabEastSlope;
            case TerrainSlopeType.West: return roadManager.roadTilePrefabWestSlope;
            case TerrainSlopeType.SouthEastUp: return isHorizontal ? roadManager.roadTilePrefabEastSlope : roadManager.roadTilePrefabSouthSlope;
            case TerrainSlopeType.NorthEastUp: return isHorizontal ? roadManager.roadTilePrefabEastSlope : roadManager.roadTilePrefabNorthSlope;
            case TerrainSlopeType.SouthWestUp: return isHorizontal ? roadManager.roadTilePrefabWestSlope : roadManager.roadTilePrefabSouthSlope;
            case TerrainSlopeType.NorthWestUp: return isHorizontal ? roadManager.roadTilePrefabWestSlope : roadManager.roadTilePrefabNorthSlope;
            default: return null;
        }
    }

    private GameObject TrySlopeForStraight(TerrainSlopeType postSlope, bool isHorizontal)
    {
        if (postSlope == TerrainSlopeType.Flat) return null;
        bool isOrthogonal = postSlope == TerrainSlopeType.North || postSlope == TerrainSlopeType.South
            || postSlope == TerrainSlopeType.East || postSlope == TerrainSlopeType.West;
        bool isCornerSlope = postSlope == TerrainSlopeType.SouthEastUp || postSlope == TerrainSlopeType.NorthEastUp
            || postSlope == TerrainSlopeType.SouthWestUp || postSlope == TerrainSlopeType.NorthWestUp;
        if (!isOrthogonal && !isCornerSlope) return null;
        return TrySlopeFromPostTerraform(postSlope, isHorizontal);
    }

    /// <summary>
    /// Returns elbow prefab for exactly two cardinal neighbors. Single source of truth for Rule A (elbow connectivity).
    /// Mapping: Left+Up=ElbowDownRight, Right+Up=ElbowDownLeft, Left+Down=ElbowUpRight, Right+Down=ElbowUpLeft.
    /// Returns null if not an elbow case (more or fewer than 2 cardinal neighbors).
    /// </summary>
    private GameObject TryGetElbowPrefab(bool hasLeft, bool hasRight, bool hasUp, bool hasDown)
    {
        if (hasLeft && hasUp && !hasRight && !hasDown) return roadManager.roadTilePrefabElbowDownRight;
        if (hasRight && hasUp && !hasLeft && !hasDown) return roadManager.roadTilePrefabElbowDownLeft;
        if (hasLeft && hasDown && !hasRight && !hasUp) return roadManager.roadTilePrefabElbowUpRight;
        if (hasRight && hasDown && !hasLeft && !hasUp) return roadManager.roadTilePrefabElbowUpLeft;
        return null;
    }

    /// <summary>
    /// Validates that the resolved prefab's exits match the path's in/out directions (Rule B).
    /// Returns true if valid or not an elbow case; false if elbow prefab does not match path connectivity.
    /// </summary>
    private bool ValidatePrefabExitsMatchPath(Vector2 curr, HashSet<Vector2Int> pathCellSet, GameObject prefab)
    {
        if (prefab == null) return true;
        int cx = Mathf.RoundToInt(curr.x);
        int cy = Mathf.RoundToInt(curr.y);
        bool pathLeft = PathCellContainsOrRoad(pathCellSet, cx - 1, cy);
        bool pathRight = PathCellContainsOrRoad(pathCellSet, cx + 1, cy);
        bool pathUp = PathCellContainsOrRoad(pathCellSet, cx, cy + 1);
        bool pathDown = PathCellContainsOrRoad(pathCellSet, cx, cy - 1);
        GameObject expectedElbow = TryGetElbowPrefab(pathLeft, pathRight, pathUp, pathDown);
        if (expectedElbow == null) return true;
        return prefab == expectedElbow;
    }

    private GameObject SelectFromConnectivity(Vector2 prevGridPos, Vector2 currGridPos, bool hasLeft, bool hasRight, bool hasUp, bool hasDown, int height)
    {
        Vector2 direction = currGridPos - prevGridPos;

        if (hasLeft && hasRight && hasUp && hasDown) return roadManager.roadTilePrefabCrossing;
        if (hasLeft && hasRight && hasUp && !hasDown) return roadManager.roadTilePrefabTIntersectionDown;
        if (hasLeft && hasRight && hasDown && !hasUp) return roadManager.roadTilePrefabTIntersectionUp;
        if (hasUp && hasDown && hasLeft && !hasRight) return roadManager.roadTilePrefabTIntersectionRight;
        if (hasUp && hasDown && hasRight && !hasLeft) return roadManager.roadTilePrefabTIntersectionLeft;
        // Elbow mapping: single source of truth (Rule A, B). Same as ResolvePrefabForPathCell.
        GameObject elbowPrefab = TryGetElbowPrefab(hasLeft, hasRight, hasUp, hasDown);
        if (elbowPrefab != null) return elbowPrefab;

        bool isHorizontal = hasLeft || hasRight;
        GameObject slopePrefab = TryGetSlopePrefabForStraightSegment(currGridPos, height, isHorizontal, prevGridPos);
        if (slopePrefab != null) return slopePrefab;

        if (hasLeft && hasRight && !hasUp && !hasDown)
            return height == 0 ? roadManager.roadTileBridgeHorizontal : roadManager.roadTilePrefab2;
        if (hasUp && hasDown && !hasLeft && !hasRight)
            return height == 0 ? roadManager.roadTileBridgeVertical : roadManager.roadTilePrefab1;
        if (hasLeft || hasRight)
            return height == 0 ? roadManager.roadTileBridgeHorizontal : roadManager.roadTilePrefab2;
        if (hasUp || hasDown)
            return height == 0 ? roadManager.roadTileBridgeVertical : roadManager.roadTilePrefab1;

        bool fallbackHorizontal = Mathf.Abs(direction.x) >= Mathf.Abs(direction.y);
        slopePrefab = TryGetSlopePrefabForStraightSegment(currGridPos, height, fallbackHorizontal, prevGridPos);
        if (slopePrefab != null) return slopePrefab;
        return fallbackHorizontal ? (height == 0 ? roadManager.roadTileBridgeHorizontal : roadManager.roadTilePrefab2)
            : (height == 0 ? roadManager.roadTileBridgeVertical : roadManager.roadTilePrefab1);
    }

    private GameObject TryGetSlopePrefabForCell(Vector2 currGridPos, int currentHeight)
    {
        Vector2? slopeDir = GetTerrainSlopeDirection(currGridPos, currentHeight);
        if (slopeDir.HasValue)
            return GetSlopePrefabForDirection(slopeDir.Value);

        if (terrainManager == null) return null;
        int x = (int)currGridPos.x;
        int y = (int)currGridPos.y;
        TerrainSlopeType slopeType = terrainManager.GetTerrainSlopeTypeAt(x, y);
        Vector2? diagonalFallback = GetOrthogonalDirectionForDiagonalSlope(slopeType);
        if (diagonalFallback.HasValue)
            return GetSlopePrefabForDirection(diagonalFallback.Value);
        return null;
    }

    private GameObject TryGetSlopePrefabForStraightSegment(Vector2 currGridPos, int currentHeight, bool isHorizontalLine, Vector2? neighborAlongRoad = null)
    {
        if (terrainManager == null) return null;
        int x = (int)currGridPos.x;
        int y = (int)currGridPos.y;
        TerrainSlopeType slopeType = terrainManager.GetTerrainSlopeTypeAt(x, y);
        if (slopeType == TerrainSlopeType.Flat) return null;

        bool isDiagonalSlope = slopeType == TerrainSlopeType.SouthEast || slopeType == TerrainSlopeType.SouthWest
            || slopeType == TerrainSlopeType.NorthEast || slopeType == TerrainSlopeType.NorthWest
            || slopeType == TerrainSlopeType.NorthEastUp || slopeType == TerrainSlopeType.NorthWestUp
            || slopeType == TerrainSlopeType.SouthEastUp || slopeType == TerrainSlopeType.SouthWestUp;

        if (isDiagonalSlope)
        {
            Vector2? diagonalDir = null;
            bool isDiagonalDownslope = slopeType == TerrainSlopeType.SouthEast || slopeType == TerrainSlopeType.SouthWest
                || slopeType == TerrainSlopeType.NorthEast || slopeType == TerrainSlopeType.NorthWest;

            if (isDiagonalDownslope && neighborAlongRoad.HasValue)
            {
                int nx = (int)neighborAlongRoad.Value.x;
                int ny = (int)neighborAlongRoad.Value.y;
                if (nx >= 0 && nx < gridManager.width && ny >= 0 && ny < gridManager.height)
                {
                    TerrainSlopeType neighborSlope = terrainManager.GetTerrainSlopeTypeAt(nx, ny);
                    if (IsComplementaryDiagonalPair(slopeType, neighborSlope))
                    {
                        int neighborHeight = GetNeighborHeight(x, y, nx - x, ny - y);
                        if (neighborHeight != int.MinValue)
                        {
                            TerrainSlopeType lowerCellSlope = currentHeight <= neighborHeight ? slopeType : neighborSlope;
                            diagonalDir = GetOrthogonalDirectionForDiagonalSlope(lowerCellSlope, isHorizontalLine);
                        }
                    }
                }
            }

            if (!diagonalDir.HasValue)
                diagonalDir = GetOrthogonalDirectionForDiagonalSlope(slopeType, isHorizontalLine);

            if (diagonalDir.HasValue)
                return GetSlopePrefabForDirection(diagonalDir.Value);
            return null;
        }

        Vector2? slopeDir = GetTerrainSlopeDirection(currGridPos, currentHeight);
        if (slopeDir.HasValue)
        {
            int dx = Mathf.RoundToInt(slopeDir.Value.x);
            int dy = Mathf.RoundToInt(slopeDir.Value.y);
            bool slopeParallelToLine = isHorizontalLine ? (dx != 0 && dy == 0) : (dx == 0 && dy != 0);
            if (slopeParallelToLine)
                return GetSlopePrefabForDirection(slopeDir.Value);
        }
        return null;
    }

    private static bool IsComplementaryDiagonalPair(TerrainSlopeType a, TerrainSlopeType b)
    {
        return (a == TerrainSlopeType.SouthEast && b == TerrainSlopeType.NorthWest)
            || (a == TerrainSlopeType.NorthWest && b == TerrainSlopeType.SouthEast)
            || (a == TerrainSlopeType.SouthWest && b == TerrainSlopeType.NorthEast)
            || (a == TerrainSlopeType.NorthEast && b == TerrainSlopeType.SouthWest);
    }

    private Vector2? GetOrthogonalDirectionForDiagonalSlope(TerrainSlopeType slopeType, bool? isHorizontalLine = null)
    {
        switch (slopeType)
        {
            case TerrainSlopeType.NorthEast:
                if (isHorizontalLine == true) return new Vector2(0, -1);
                if (isHorizontalLine == false) return new Vector2(-1, 0);
                return new Vector2(-1, 0);
            case TerrainSlopeType.NorthWest:
                if (isHorizontalLine == true) return new Vector2(0, 1);
                if (isHorizontalLine == false) return new Vector2(-1, 0);
                return new Vector2(-1, 0);
            case TerrainSlopeType.SouthEast:
                if (isHorizontalLine == true) return new Vector2(0, -1);
                if (isHorizontalLine == false) return new Vector2(1, 0);
                return new Vector2(1, 0);
            case TerrainSlopeType.SouthWest:
                if (isHorizontalLine == true) return new Vector2(0, 1);
                if (isHorizontalLine == false) return new Vector2(1, 0);
                return new Vector2(1, 0);
            case TerrainSlopeType.SouthEastUp:
                if (isHorizontalLine == true) return new Vector2(1, 0);
                if (isHorizontalLine == false) return new Vector2(0, 1);
                return new Vector2(0, 1);
            case TerrainSlopeType.NorthEastUp:
                if (isHorizontalLine == true) return new Vector2(-1, 0);
                if (isHorizontalLine == false) return new Vector2(0, 1);
                return new Vector2(0, 1);
            case TerrainSlopeType.SouthWestUp:
                if (isHorizontalLine == true) return new Vector2(1, 0);
                if (isHorizontalLine == false) return new Vector2(0, -1);
                return new Vector2(0, -1);
            case TerrainSlopeType.NorthWestUp:
                if (isHorizontalLine == true) return new Vector2(-1, 0);
                if (isHorizontalLine == false) return new Vector2(0, -1);
                return new Vector2(0, -1);
            default:
                return null;
        }
    }

    private Vector2? GetTerrainSlopeDirection(Vector2 currGridPos, int currentHeight)
    {
        if (currentHeight == 0) return null;
        int x = (int)currGridPos.x;
        int y = (int)currGridPos.y;
        Vector2? directionToHigher = null;
        for (int i = 0; i < 4; i++)
        {
            int nh = GetNeighborHeight(x, y, DirX[i], DirY[i]);
            if (nh == int.MinValue) continue;
            if (nh - currentHeight == 1)
                directionToHigher = new Vector2(DirX[i], DirY[i]);
        }
        if (!directionToHigher.HasValue) return null;
        int dxi = Mathf.RoundToInt(directionToHigher.Value.x);
        int dyi = Mathf.RoundToInt(directionToHigher.Value.y);
        bool isCardinal = (Mathf.Abs(dxi) == 1 && dyi == 0) || (dxi == 0 && Mathf.Abs(dyi) == 1);
        return isCardinal ? (Vector2?)directionToHigher.Value : null;
    }

    private GameObject GetSlopePrefabForDirection(Vector2 cardinalDirection)
    {
        int dx = Mathf.RoundToInt(cardinalDirection.x);
        int dy = Mathf.RoundToInt(cardinalDirection.y);
        if (dx == 1 && dy == 0) return roadManager.roadTilePrefabSouthSlope;
        if (dx == -1 && dy == 0) return roadManager.roadTilePrefabNorthSlope;
        if (dx == 0 && dy == 1) return roadManager.roadTilePrefabEastSlope;
        if (dx == 0 && dy == -1) return roadManager.roadTilePrefabWestSlope;
        return null;
    }

    private Vector2 GetWorldPositionForPrefab(int x, int y, GameObject prefab, int terrainHeight, TerrainSlopeType slopeType)
    {
        if (terrainHeight == 0)
            return gridManager.GetWorldPositionVector(x, y, 1);

        if (!IsDiagonalRoadPrefab(prefab))
            return gridManager.GetWorldPosition(x, y);

        // Cut-through: when terrain was flattened, place elbow at cell position (not upper neighbor).
        if (slopeType == TerrainSlopeType.Flat)
            return gridManager.GetWorldPosition(x, y);

        int upperX = x, upperY = y;
        bool foundFromPlan = TryGetUpperNeighborFromSlopeType(slopeType, x, y, out upperX, out upperY);
        if (!foundFromPlan)
        {
            Vector2? slopeDir = GetTerrainSlopeDirection(new Vector2(x, y), terrainHeight);
            if (slopeDir.HasValue)
            {
                upperX = x + Mathf.RoundToInt(slopeDir.Value.x);
                upperY = y + Mathf.RoundToInt(slopeDir.Value.y);
            }
            else
                return gridManager.GetWorldPosition(x, y);
        }

        if (upperX < 0 || upperX >= gridManager.width || upperY < 0 || upperY >= gridManager.height)
            return gridManager.GetWorldPosition(x, y);

        Cell upperCell = gridManager.GetCell(upperX, upperY);
        if (upperCell == null)
            return gridManager.GetWorldPosition(x, y);

        int upperHeight = upperCell.GetCellInstanceHeight();
        return gridManager.GetWorldPositionVector(upperX, upperY, upperHeight);
    }

    private bool TryGetUpperNeighborFromSlopeType(TerrainSlopeType slopeType, int x, int y, out int upperX, out int upperY)
    {
        upperX = x;
        upperY = y;
        switch (slopeType)
        {
            case TerrainSlopeType.North: upperX = x + 1; upperY = y; return true;
            case TerrainSlopeType.South: upperX = x - 1; upperY = y; return true;
            case TerrainSlopeType.East: upperX = x; upperY = y - 1; return true;
            case TerrainSlopeType.West: upperX = x; upperY = y + 1; return true;
            case TerrainSlopeType.SouthEast: upperX = x + 1; upperY = y + 1; return true;
            case TerrainSlopeType.SouthWest: upperX = x + 1; upperY = y - 1; return true;
            case TerrainSlopeType.NorthEast: upperX = x - 1; upperY = y + 1; return true;
            case TerrainSlopeType.NorthWest: upperX = x - 1; upperY = y - 1; return true;
            case TerrainSlopeType.SouthEastUp: upperX = x + 1; upperY = y; return true;
            case TerrainSlopeType.NorthEastUp: upperX = x - 1; upperY = y; return true;
            case TerrainSlopeType.SouthWestUp: upperX = x + 1; upperY = y; return true;
            case TerrainSlopeType.NorthWestUp: upperX = x - 1; upperY = y; return true;
            default: return false;
        }
    }

    private bool IsDiagonalRoadPrefab(GameObject prefab)
    {
        if (prefab == null) return false;
        return prefab == roadManager.roadTilePrefabElbowUpLeft || prefab == roadManager.roadTilePrefabElbowUpRight
            || prefab == roadManager.roadTilePrefabElbowDownLeft || prefab == roadManager.roadTilePrefabElbowDownRight;
    }

    /// <summary>
    /// True if the adjacent cell is part of the current path (any segment) or already has a road.
    /// </summary>
    private bool PathCellContainsOrRoad(HashSet<Vector2Int> pathCellSet, int nx, int ny)
    {
        if (pathCellSet != null && pathCellSet.Contains(new Vector2Int(nx, ny)))
            return true;
        return IsRoadAt(new Vector2(nx, ny));
    }

    private int GetNeighborHeight(int gridX, int gridY, int dx, int dy)
    {
        int nx = gridX + dx;
        int ny = gridY + dy;
        if (nx < 0 || nx >= gridManager.width || ny < 0 || ny >= gridManager.height)
            return int.MinValue;
        Cell c = gridManager.GetCell(nx, ny);
        return c != null ? c.GetCellInstanceHeight() : int.MinValue;
    }

    private bool IsRoadAt(Vector2 gridPos)
    {
        int gridX = Mathf.RoundToInt(gridPos.x);
        int gridY = Mathf.RoundToInt(gridPos.y);
        if (gridX < 0 || gridX >= gridManager.width || gridY < 0 || gridY >= gridManager.height)
            return false;
        var cell = gridManager.GetGridCell(new Vector2(gridX, gridY));
        if (cell == null || cell.transform.childCount == 0) return false;
        var cellComponent = gridManager.GetCell(gridX, gridY);
        if (cellComponent != null && cellComponent.zoneType == Zone.ZoneType.Road) return true;
        for (int i = 0; i < cell.transform.childCount; i++)
        {
            var zone = cell.transform.GetChild(i).GetComponent<Zone>();
            if (zone != null && zone.zoneType == Zone.ZoneType.Road)
                return true;
        }
        return false;
    }
}
}
