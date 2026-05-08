using System.Collections.Generic;
using UnityEngine;
using Territory.Core;
using Territory.Terrain;
using Territory.Zones;
using Territory.Roads;

namespace Domains.Roads.Services
{
/// <summary>
/// Pure prefab resolution logic extracted from RoadPrefabResolver (Stage 17 atomization).
/// No MonoBehaviour. Composed under Roads facade via RoadPrefabResolver thin wrapper.
/// Invariant #2 (InvalidateRoadCache) preserved in GridManager/RoadManager.
/// </summary>
public class PrefabResolverService
{
    private enum PathRouteTopology
    {
        Isolated,
        End,
        StraightThrough,
        Corner90,
        Junction
    }

    private readonly IGridManager gridManager;
    private readonly ITerrainManager terrainManager;
    private readonly IRoadManager roadManager;

    private readonly List<Vector2Int> scratchHighDeckBridgeNormals = new List<Vector2Int>(4);

    private static readonly int[] DirX = { 1, -1, 0, 0 };
    private static readonly int[] DirY = { 0, 0, 1, -1 };

    public PrefabResolverService(IGridManager grid, ITerrainManager terrain, IRoadManager roads)
    {
        gridManager = grid;
        terrainManager = terrain;
        roadManager = roads;
    }

    // ResolvedRoadTile struct lifted to Core (Assets/Scripts/Core/Roads/ResolvedRoadTile.cs) — Territory.Roads ns. Canonical replaces legacy nested.

    /// <summary>Resolve prefabs for full path via terraform plan.</summary>
    public List<ResolvedRoadTile> ResolveForPath(List<Vector2> path, PathTerraformPlan plan)
    {
        var result = new List<ResolvedRoadTile>();
        if (path == null || path.Count == 0) return result;

        var pathCellSet = new HashSet<Vector2Int>();
        for (int j = 0; j < path.Count; j++)
            pathCellSet.Add(new Vector2Int(Mathf.RoundToInt(path[j].x), Mathf.RoundToInt(path[j].y)));

        for (int i = 0; i < path.Count; i++)
        {
            Vector2 curr = path[i];
            Vector2 prev = i > 0 ? path[i - 1] : (path.Count > 1 ? 2 * curr - path[1] : curr);

            PathTerraformPlan.CellPlan cellPlan = default;
            if (plan != null && plan.pathCells != null && i < plan.pathCells.Count)
                cellPlan = plan.pathCells[i];

            int x = (int)curr.x;
            int y = (int)curr.y;
            CityCell cell = gridManager.GetCell(x, y);
            if (cell == null) continue;

            int height = cell.GetCellInstanceHeight();
            TerrainSlopeType postSlope = cellPlan.postTerraformSlopeType;

            bool allowLiveSlopeFallback = plan != null && !plan.isCutThrough && plan.pathCells != null && i < plan.pathCells.Count
                && cellPlan.action == TerraformAction.None;

            HeightMap pathHeightMap = terrainManager != null ? terrainManager.GetHeightMap() : null;
            GameObject prefab = ResolvePrefabForPathCell(prev, curr, pathCellSet, height, postSlope, allowLiveSlopeFallback, plan, pathHeightMap, pathOnlyNeighbors: true);
            TerrainSlopeType slopeForWorld = postSlope;
            if (postSlope == TerrainSlopeType.Flat && terrainManager != null && prefab != null && IsCardinalSlopeRoadPrefab(prefab))
            {
                TerrainSlopeType live = terrainManager.GetTerrainSlopeTypeAt(x, y);
                if (live != TerrainSlopeType.Flat)
                    slopeForWorld = live;
            }

            bool isBridgeDeck = IsBridgeDeckRoadPrefab(prefab);
            int sortHeight = height;
            Vector2 worldPos;
            if (isBridgeDeck && plan != null && plan.waterBridgeDeckDisplayHeight > 0)
            {
                sortHeight = plan.waterBridgeDeckDisplayHeight;
                worldPos = gridManager.GetWorldPositionVector(x, y, plan.waterBridgeDeckDisplayHeight);
            }
            else
                worldPos = GetWorldPositionForPrefab(x, y, prefab, height, slopeForWorld);

            int sortingOrder = gridManager.GetRoadSortingOrderForCell(x, y, sortHeight);

            bool hasPrevHint = i > 0;
            Vector2Int prevGrid = hasPrevHint
                ? new Vector2Int(Mathf.RoundToInt(path[i - 1].x), Mathf.RoundToInt(path[i - 1].y))
                : default;
            bool hasNextHint = i < path.Count - 1;
            Vector2Int nextGrid = hasNextHint
                ? new Vector2Int(Mathf.RoundToInt(path[i + 1].x), Mathf.RoundToInt(path[i + 1].y))
                : default;
            Vector2Int entryStep = GetCardinalGridStepRounded(curr - prev);
            Vector2 nextForStep = i < path.Count - 1 ? path[i + 1] : curr;
            Vector2Int exitStep = hasNextHint ? GetCardinalGridStepRounded(nextForStep - curr) : default;

            result.Add(new ResolvedRoadTile
            {
                gridPos = new Vector2Int(x, y),
                prefab = prefab ?? roadManager.roadTilePrefab1,
                worldPos = worldPos,
                sortingOrder = sortingOrder,
                hasSegmentPrevHint = hasPrevHint,
                segmentPrevGridPos = prevGrid,
                hasSegmentNextHint = hasNextHint,
                segmentNextGridPos = nextGrid,
                routeEntryStep = entryStep,
                routeExitStep = exitStep
            });
        }
        return result;
    }

    /// <summary>Resolve prefab for single cell via neighbor connectivity.</summary>
    public ResolvedRoadTile? ResolveForCell(Vector2 currGridPos, Vector2 prevGridPos)
    {
        int x = (int)currGridPos.x;
        int y = (int)currGridPos.y;
        CityCell cell = gridManager.GetCell(x, y);
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

            HeightMap hm = terrainManager != null ? terrainManager.GetHeightMap() : null;
            IWaterManager wm = ResolveWaterManagerForBridge();
            if (hm != null && wm != null && terrainManager != null
                && height > 0
                && !terrainManager.IsRegisteredOpenWaterAt(x, y) && !terrainManager.IsWaterSlopeCell(x, y)
                && DryLandCardinalLowerTouchesRegisteredWater(x, y, height, hm))
            {
                CollectHighDeckBridgeNormals(x, y, height, hm, scratchHighDeckBridgeNormals);
                Vector2 approachDir = currGridPos - prevGridPos;
                if (CardinalApproachParallelToHighDeckNormal(approachDir, scratchHighDeckBridgeNormals))
                {
                    int roadCount = (hasLeft ? 1 : 0) + (hasRight ? 1 : 0) + (hasUp ? 1 : 0) + (hasDown ? 1 : 0);
                    bool straightH = hasLeft && hasRight && !hasUp && !hasDown;
                    bool straightV = hasUp && hasDown && !hasLeft && !hasRight;
                    bool deadEnd = roadCount == 1;
                    if (straightH || straightV || deadEnd)
                    {
                        bool isHorizontal;
                        if (straightH || (deadEnd && (hasLeft || hasRight)))
                            isHorizontal = true;
                        else if (straightV || (deadEnd && (hasUp || hasDown)))
                            isHorizontal = false;
                        else
                            isHorizontal = Mathf.Abs((currGridPos - prevGridPos).x) >= Mathf.Abs((currGridPos - prevGridPos).y);
                        prefab = isHorizontal ? roadManager.roadTileBridgeHorizontal : roadManager.roadTileBridgeVertical;
                    }
                }
            }
        }

        int sortHeight = height;
        Vector2 worldPos;
        HeightMap hmCell = terrainManager != null ? terrainManager.GetHeightMap() : null;
        bool needsDeckInference = IsBridgeDeckRoadPrefab(prefab)
            && terrainManager != null
            && (terrainManager.IsRegisteredOpenWaterAt(x, y)
                || terrainManager.IsWaterSlopeCell(x, y)
                || height == 0);
        bool dryCliffBridgeDeck = IsBridgeDeckRoadPrefab(prefab) && height > 0 && hmCell != null
            && terrainManager != null
            && !terrainManager.IsRegisteredOpenWaterAt(x, y) && !terrainManager.IsWaterSlopeCell(x, y)
            && DryLandCardinalLowerTouchesRegisteredWater(x, y, height, hmCell);
        if (needsDeckInference && TryInferWaterBridgeDeckDisplayHeight(x, y, out int deckH) && deckH > 0)
        {
            sortHeight = deckH;
            worldPos = gridManager.GetWorldPositionVector(x, y, deckH);
        }
        else if (dryCliffBridgeDeck)
        {
            sortHeight = height;
            worldPos = gridManager.GetWorldPositionVector(x, y, height);
        }
        else
            worldPos = GetWorldPositionForPrefab(x, y, prefab, height, terrainManager?.GetTerrainSlopeTypeAt(x, y) ?? TerrainSlopeType.Flat);

        return new ResolvedRoadTile
        {
            gridPos = new Vector2Int(x, y),
            prefab = prefab ?? roadManager.roadTilePrefab1,
            worldPos = worldPos,
            sortingOrder = gridManager.GetRoadSortingOrderForCell(x, y, sortHeight),
            hasSegmentPrevHint = false,
            segmentPrevGridPos = default,
            hasSegmentNextHint = false,
            segmentNextGridPos = default,
            routeEntryStep = default,
            routeExitStep = default
        };
    }

    /// <summary>Resolve prefab for ghost preview (single cell, no path).</summary>
    public void ResolveForGhostPreview(Vector2 gridPos, out GameObject prefab, out Vector2 worldPos, out int sortingOrder)
    {
        int x = (int)gridPos.x;
        int y = (int)gridPos.y;
        prefab = roadManager.roadTilePrefab1;
        worldPos = gridManager.GetWorldPosition(x, y);
        sortingOrder = gridManager.GetRoadSortingOrderForCell(x, y, 0);

        CityCell cell = gridManager.GetCell(x, y);
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

        HeightMap ghostHm = terrainManager != null ? terrainManager.GetHeightMap() : null;
        if (height > 0 && ghostHm != null && terrainManager != null
            && !terrainManager.IsRegisteredOpenWaterAt(x, y)
            && DryLandCardinalLowerTouchesRegisteredWater(x, y, height, ghostHm))
        {
            Vector2 towardLower = InferStrongestCardinalTowardQualifyingLowerNeighbor(x, y, height, ghostHm);
            bool isHorizontal = Mathf.Abs(towardLower.x) >= Mathf.Abs(towardLower.y);
            prefab = isHorizontal ? roadManager.roadTileBridgeHorizontal : roadManager.roadTileBridgeVertical;
            worldPos = gridManager.GetWorldPositionVector(x, y, height);
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

    private GameObject ResolvePrefabForPathCell(Vector2 prev, Vector2 curr, HashSet<Vector2Int> pathCellSet, int height, TerrainSlopeType postSlope, bool allowLiveSlopeFallback, PathTerraformPlan plan, HeightMap heightMap, bool pathOnlyNeighbors)
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

        if (TryGetHighCliffLipBridgePrefab(plan, cx, cy, height, heightMap, dirIn, out GameObject cliffDeckPrefab))
            return cliffDeckPrefab;

        bool pathLeft = PathNeighborForResolve(pathCellSet, cx - 1, cy, pathOnlyNeighbors);
        bool pathRight = PathNeighborForResolve(pathCellSet, cx + 1, cy, pathOnlyNeighbors);
        bool pathUp = PathNeighborForResolve(pathCellSet, cx, cy + 1, pathOnlyNeighbors);
        bool pathDown = PathNeighborForResolve(pathCellSet, cx, cy - 1, pathOnlyNeighbors);

        PathRouteTopology topo = ClassifyPathRouteTopology(pathLeft, pathRight, pathUp, pathDown);

        if (topo == PathRouteTopology.Junction)
            return SelectFromConnectivity(prev, curr, pathLeft, pathRight, pathUp, pathDown, height);

        if (topo == PathRouteTopology.Corner90)
        {
            GameObject elbowPrefab = TryGetElbowPrefab(pathLeft, pathRight, pathUp, pathDown);
            if (elbowPrefab != null) return elbowPrefab;
            return SelectFromConnectivity(prev, curr, pathLeft, pathRight, pathUp, pathDown, height);
        }

        if (topo == PathRouteTopology.StraightThrough || topo == PathRouteTopology.End
            || ((pathLeft || pathRight || pathUp || pathDown) && topo != PathRouteTopology.Isolated))
        {
            bool segmentHorizontal = (dxIn != 0 || dyIn != 0)
                ? Mathf.Abs(dxIn) >= Mathf.Abs(dyIn)
                : (pathLeft || pathRight);
            GameObject straightSlope = TrySlopeForStraight(postSlope, segmentHorizontal);
            if (straightSlope != null) return straightSlope;
            if (allowLiveSlopeFallback && postSlope == TerrainSlopeType.Flat)
            {
                GameObject liveSlope = TryGetSlopePrefabForStraightSegment(curr, height, segmentHorizontal, prev);
                if (liveSlope != null) return liveSlope;
            }
            return segmentHorizontal ? roadManager.roadTilePrefab2 : roadManager.roadTilePrefab1;
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

        bool horizontalSeg = Mathf.Abs(dirIn.x) >= Mathf.Abs(dirIn.y);
        GameObject slopePrefab = TrySlopeFromPostTerraform(postSlope, horizontalSeg);
        if (slopePrefab != null) return slopePrefab;

        if (allowLiveSlopeFallback && postSlope == TerrainSlopeType.Flat)
        {
            GameObject liveSlope = TryGetSlopePrefabForStraightSegment(curr, height, horizontalSeg, prev);
            if (liveSlope != null) return liveSlope;
        }

        return horizontalSeg ? roadManager.roadTilePrefab2 : roadManager.roadTilePrefab1;
    }

    static Vector2Int GetCardinalGridStepRounded(Vector2 delta)
    {
        int x = Mathf.RoundToInt(delta.x);
        int y = Mathf.RoundToInt(delta.y);
        if (x == 0 && y == 0) return Vector2Int.zero;
        if (Mathf.Abs(x) >= Mathf.Abs(y) && x != 0) return new Vector2Int(x > 0 ? 1 : -1, 0);
        if (y != 0) return new Vector2Int(0, y > 0 ? 1 : -1);
        return Vector2Int.zero;
    }

    static PathRouteTopology ClassifyPathRouteTopology(bool pathLeft, bool pathRight, bool pathUp, bool pathDown)
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

    bool PathNeighborForResolve(HashSet<Vector2Int> pathCellSet, int nx, int ny, bool pathOnlyNeighbors)
    {
        if (pathCellSet != null && pathCellSet.Contains(new Vector2Int(nx, ny))) return true;
        if (!pathOnlyNeighbors) return IsRoadAt(new Vector2(nx, ny));
        return false;
    }

    IWaterManager ResolveWaterManagerForBridge()
    {
        return terrainManager?.Water;
    }

    bool DryCellTouchesRegisteredWaterMoore(int x, int y, IWaterManager wm)
    {
        if (terrainManager == null) return false;
        if (terrainManager.IsRegisteredOpenWaterAt(x, y) || terrainManager.IsWaterSlopeCell(x, y)) return true;
        if (wm == null) return false;
        int[] mdx = { 1, -1, 0, 0, 1, 1, -1, -1 };
        int[] mdy = { 0, 0, 1, -1, 1, -1, 1, -1 };
        for (int d = 0; d < 8; d++)
        {
            if (wm.IsWaterAt(x + mdx[d], y + mdy[d])) return true;
        }
        return false;
    }

    bool LowerCardinalQualifiesAsHighDeckWaterStep(int nx, int ny, int hn, int lipHeight, IWaterManager wm)
    {
        if (hn >= lipHeight) return false;
        if (terrainManager.IsRegisteredOpenWaterAt(nx, ny) || terrainManager.IsWaterSlopeCell(nx, ny)) return true;
        return DryCellTouchesRegisteredWaterMoore(nx, ny, wm);
    }

    bool DryLandCardinalLowerTouchesRegisteredWater(int x, int y, int h, HeightMap heightMap)
    {
        if (heightMap == null || terrainManager == null || !heightMap.IsValidPosition(x, y)) return false;
        IWaterManager wm = ResolveWaterManagerForBridge();
        int[] cdx = { 1, -1, 0, 0 };
        int[] cdy = { 0, 0, 1, -1 };
        for (int d = 0; d < 4; d++)
        {
            int nx = x + cdx[d], ny = y + cdy[d];
            if (!heightMap.IsValidPosition(nx, ny)) continue;
            int hn = heightMap.GetHeight(nx, ny);
            if (LowerCardinalQualifiesAsHighDeckWaterStep(nx, ny, hn, h, wm)) return true;
        }
        return false;
    }

    Vector2 InferStrongestCardinalTowardQualifyingLowerNeighbor(int x, int y, int h, HeightMap heightMap)
    {
        IWaterManager wm = ResolveWaterManagerForBridge();
        int bestDelta = -1;
        Vector2 best = Vector2.right;
        int[] cdx = { 1, -1, 0, 0 };
        int[] cdy = { 0, 0, 1, -1 };
        for (int d = 0; d < 4; d++)
        {
            int nx = x + cdx[d], ny = y + cdy[d];
            if (!heightMap.IsValidPosition(nx, ny)) continue;
            int hn = heightMap.GetHeight(nx, ny);
            if (!LowerCardinalQualifiesAsHighDeckWaterStep(nx, ny, hn, h, wm)) continue;
            int delta = h - hn;
            if (delta > bestDelta) { bestDelta = delta; best = new Vector2(nx - x, ny - y); }
        }
        return bestDelta >= 0 ? best : Vector2.right;
    }

    void CollectHighDeckBridgeNormals(int x, int y, int h, HeightMap heightMap, List<Vector2Int> outNormals)
    {
        outNormals.Clear();
        if (heightMap == null || terrainManager == null || !heightMap.IsValidPosition(x, y)) return;
        IWaterManager wm = ResolveWaterManagerForBridge();
        int[] cdx = { 1, -1, 0, 0 };
        int[] cdy = { 0, 0, 1, -1 };
        for (int d = 0; d < 4; d++)
        {
            int nx = x + cdx[d], ny = y + cdy[d];
            if (!heightMap.IsValidPosition(nx, ny)) continue;
            int hn = heightMap.GetHeight(nx, ny);
            if (!LowerCardinalQualifiesAsHighDeckWaterStep(nx, ny, hn, h, wm)) continue;
            var step = new Vector2Int(cdx[d], cdy[d]);
            if (!outNormals.Contains(step)) outNormals.Add(step);
        }
    }

    bool CardinalApproachParallelToHighDeckNormal(Vector2 dirIn, List<Vector2Int> bridgeNormals)
    {
        if (bridgeNormals == null || bridgeNormals.Count == 0) return false;
        if (Mathf.Approximately(dirIn.x, 0f) && Mathf.Approximately(dirIn.y, 0f)) return true;
        int rdx = Mathf.RoundToInt(dirIn.x);
        int rdy = Mathf.RoundToInt(dirIn.y);
        if (Mathf.Abs(rdx) + Mathf.Abs(rdy) != 1) return false;
        for (int i = 0; i < bridgeNormals.Count; i++)
        {
            Vector2Int n = bridgeNormals[i];
            if (rdx == n.x && rdy == n.y) return true;
            if (rdx == -n.x && rdy == -n.y) return true;
        }
        return false;
    }

    bool TryGetHighCliffLipBridgePrefab(PathTerraformPlan plan, int cx, int cy, int height, HeightMap heightMap, Vector2 dirIn, out GameObject prefab)
    {
        prefab = null;
        if (plan == null || !plan.waterBridgeTerraformRelaxation || plan.waterBridgeDeckDisplayHeight <= 0) return false;
        if (height != plan.waterBridgeDeckDisplayHeight || heightMap == null || terrainManager == null) return false;
        if (terrainManager.IsRegisteredOpenWaterAt(cx, cy) || terrainManager.IsWaterSlopeCell(cx, cy)) return false;
        if (!DryLandCardinalLowerTouchesRegisteredWater(cx, cy, height, heightMap)) return false;

        CollectHighDeckBridgeNormals(cx, cy, height, heightMap, scratchHighDeckBridgeNormals);
        if (scratchHighDeckBridgeNormals.Count == 0) return false;
        if (!CardinalApproachParallelToHighDeckNormal(dirIn, scratchHighDeckBridgeNormals)) return false;

        bool isHorizontal;
        if (Mathf.Approximately(dirIn.x, 0f) && Mathf.Approximately(dirIn.y, 0f))
        {
            Vector2 toward = InferStrongestCardinalTowardQualifyingLowerNeighbor(cx, cy, height, heightMap);
            isHorizontal = Mathf.Abs(toward.x) >= Mathf.Abs(toward.y);
        }
        else
            isHorizontal = Mathf.Abs(dirIn.x) >= Mathf.Abs(dirIn.y);
        prefab = isHorizontal ? roadManager.roadTileBridgeHorizontal : roadManager.roadTileBridgeVertical;
        return true;
    }

    bool IsBridgeDeckRoadPrefab(GameObject prefab)
    {
        if (prefab == null || roadManager == null) return false;
        return prefab == roadManager.roadTileBridgeHorizontal || prefab == roadManager.roadTileBridgeVertical;
    }

    bool TryInferWaterBridgeDeckDisplayHeight(int x, int y, out int deckH)
    {
        deckH = 0;
        if (terrainManager == null || gridManager == null) return false;
        int best = 0;
        for (int d = 0; d < 4; d++)
        {
            int nx = x + DirX[d], ny = y + DirY[d];
            if (!IsValidGrid(nx, ny) || !IsRoadAt(new Vector2(nx, ny))) continue;
            if (terrainManager.IsRegisteredOpenWaterAt(nx, ny)) continue;
            CityCell cn = gridManager.GetCell(nx, ny);
            if (cn != null) best = Mathf.Max(best, cn.GetCellInstanceHeight());
        }
        if (best > 0) { deckH = best; return true; }

        for (int d = 0; d < 4; d++)
        {
            int cx = x + DirX[d], cy = y + DirY[d];
            if (!IsValidGrid(cx, cy) || !IsRoadAt(new Vector2(cx, cy))) continue;
            while (terrainManager.IsRegisteredOpenWaterAt(cx, cy))
            {
                int ax = cx + DirX[d], ay = cy + DirY[d];
                if (!IsValidGrid(ax, ay) || !IsRoadAt(new Vector2(ax, ay))) break;
                cx = ax; cy = ay;
            }
            if (IsValidGrid(cx, cy) && IsRoadAt(new Vector2(cx, cy)) && !terrainManager.IsRegisteredOpenWaterAt(cx, cy))
            {
                CityCell c = gridManager.GetCell(cx, cy);
                if (c != null) best = Mathf.Max(best, c.GetCellInstanceHeight());
            }
        }
        if (best > 0) { deckH = best; return true; }
        return false;
    }

    bool IsValidGrid(int gx, int gy) => gx >= 0 && gx < gridManager.width && gy >= 0 && gy < gridManager.height;

    private bool IsCardinalSlopeRoadPrefab(GameObject prefab)
    {
        if (prefab == null) return false;
        return prefab == roadManager.roadTilePrefabNorthSlope || prefab == roadManager.roadTilePrefabSouthSlope
            || prefab == roadManager.roadTilePrefabEastSlope || prefab == roadManager.roadTilePrefabWestSlope;
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

    private GameObject TryGetElbowPrefab(bool hasLeft, bool hasRight, bool hasUp, bool hasDown)
    {
        if (hasLeft && hasUp && !hasRight && !hasDown) return roadManager.roadTilePrefabElbowDownRight;
        if (hasRight && hasUp && !hasLeft && !hasDown) return roadManager.roadTilePrefabElbowDownLeft;
        if (hasLeft && hasDown && !hasRight && !hasUp) return roadManager.roadTilePrefabElbowUpRight;
        if (hasRight && hasDown && !hasLeft && !hasUp) return roadManager.roadTilePrefabElbowUpLeft;
        return null;
    }

    private GameObject SelectFromConnectivity(Vector2 prevGridPos, Vector2 currGridPos, bool hasLeft, bool hasRight, bool hasUp, bool hasDown, int height)
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
        GameObject slopePrefab = TryGetSlopePrefabForStraightSegment(currGridPos, height, isHorizontal, prevGridPos);
        if (slopePrefab != null) return slopePrefab;

        if (hasLeft && hasRight && !hasUp && !hasDown) return height == 0 ? roadManager.roadTileBridgeHorizontal : roadManager.roadTilePrefab2;
        if (hasUp && hasDown && !hasLeft && !hasRight) return height == 0 ? roadManager.roadTileBridgeVertical : roadManager.roadTilePrefab1;
        if (hasLeft || hasRight) return height == 0 ? roadManager.roadTileBridgeHorizontal : roadManager.roadTilePrefab2;
        if (hasUp || hasDown) return height == 0 ? roadManager.roadTileBridgeVertical : roadManager.roadTilePrefab1;

        bool fallbackHorizontal = Mathf.Abs(direction.x) >= Mathf.Abs(direction.y);
        slopePrefab = TryGetSlopePrefabForStraightSegment(currGridPos, height, fallbackHorizontal, prevGridPos);
        if (slopePrefab != null) return slopePrefab;
        return fallbackHorizontal
            ? (height == 0 ? roadManager.roadTileBridgeHorizontal : roadManager.roadTilePrefab2)
            : (height == 0 ? roadManager.roadTileBridgeVertical : roadManager.roadTilePrefab1);
    }

    private GameObject TryGetSlopePrefabForCell(Vector2 currGridPos, int currentHeight)
    {
        Vector2? slopeDir = GetTerrainSlopeDirection(currGridPos, currentHeight);
        if (slopeDir.HasValue) return GetSlopePrefabForDirection(slopeDir.Value);
        if (terrainManager == null) return null;
        int x = (int)currGridPos.x;
        int y = (int)currGridPos.y;
        TerrainSlopeType slopeType = terrainManager.GetTerrainSlopeTypeAt(x, y);
        Vector2? diagonalFallback = GetOrthogonalDirectionForDiagonalSlope(slopeType);
        if (diagonalFallback.HasValue) return GetSlopePrefabForDirection(diagonalFallback.Value);
        return null;
    }

    private static TerrainSlopeType GetSlopeTypeFromTravelVector(int dx, int dy)
    {
        if (Mathf.Abs(dx) >= Mathf.Abs(dy) && dx != 0) return dx > 0 ? TerrainSlopeType.North : TerrainSlopeType.South;
        if (dy != 0) return dy > 0 ? TerrainSlopeType.West : TerrainSlopeType.East;
        return TerrainSlopeType.Flat;
    }

    private GameObject TryRampRoadPrefabFromPrevTravel(Vector2 currGridPos, int currentHeight, Vector2 prevGridPos)
    {
        Vector2 dir = currGridPos - prevGridPos;
        int dx = Mathf.RoundToInt(dir.x);
        int dy = Mathf.RoundToInt(dir.y);
        if (dx == 0 && dy == 0) return null;

        int px = Mathf.RoundToInt(prevGridPos.x);
        int py = Mathf.RoundToInt(prevGridPos.y);
        if (px < 0 || px >= gridManager.width || py < 0 || py >= gridManager.height) return null;
        CityCell prevCell = gridManager.GetCell(px, py);
        if (prevCell == null) return null;

        int x = Mathf.RoundToInt(currGridPos.x);
        int y = Mathf.RoundToInt(currGridPos.y);
        int nx = x + dx, ny = y + dy;
        int dSeg;
        if (nx >= 0 && nx < gridManager.width && ny >= 0 && ny < gridManager.height)
        {
            CityCell nextCell = gridManager.GetCell(nx, ny);
            dSeg = nextCell != null ? nextCell.GetCellInstanceHeight() - currentHeight : currentHeight - prevCell.GetCellInstanceHeight();
        }
        else
            dSeg = currentHeight - prevCell.GetCellInstanceHeight();

        TerrainSlopeType rampSlopeType;
        if (dSeg > 0) rampSlopeType = GetSlopeTypeFromTravelVector(-dx, -dy);
        else if (dSeg < 0) rampSlopeType = GetSlopeTypeFromTravelVector(dx, dy);
        else return null;

        if (rampSlopeType == TerrainSlopeType.Flat) return null;
        bool isHorizontal = Mathf.Abs(dx) >= Mathf.Abs(dy);
        return TrySlopeFromPostTerraform(rampSlopeType, isHorizontal);
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
            if (neighborAlongRoad.HasValue)
            {
                GameObject travelRamp = TryRampRoadPrefabFromPrevTravel(currGridPos, currentHeight, neighborAlongRoad.Value);
                if (travelRamp != null) return travelRamp;
            }

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

            if (!diagonalDir.HasValue) diagonalDir = GetOrthogonalDirectionForDiagonalSlope(slopeType, isHorizontalLine);
            if (diagonalDir.HasValue) return GetSlopePrefabForDirection(diagonalDir.Value);
            return null;
        }

        Vector2? slopeDir = GetTerrainSlopeDirection(currGridPos, currentHeight);
        if (slopeDir.HasValue)
        {
            int dx = Mathf.RoundToInt(slopeDir.Value.x);
            int dy = Mathf.RoundToInt(slopeDir.Value.y);
            bool slopeParallelToLine = isHorizontalLine ? (dx != 0 && dy == 0) : (dx == 0 && dy != 0);
            if (slopeParallelToLine) return GetSlopePrefabForDirection(slopeDir.Value);
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
            default: return null;
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
            if (nh - currentHeight == 1) directionToHigher = new Vector2(DirX[i], DirY[i]);
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

    static bool TerrainSlopeIsDiagonalOrCornerUp(TerrainSlopeType slopeType)
    {
        return slopeType == TerrainSlopeType.NorthEast || slopeType == TerrainSlopeType.NorthWest
            || slopeType == TerrainSlopeType.SouthEast || slopeType == TerrainSlopeType.SouthWest
            || slopeType == TerrainSlopeType.NorthEastUp || slopeType == TerrainSlopeType.NorthWestUp
            || slopeType == TerrainSlopeType.SouthEastUp || slopeType == TerrainSlopeType.SouthWestUp;
    }

    private Vector2 GetWorldPositionForPrefab(int x, int y, GameObject prefab, int terrainHeight, TerrainSlopeType slopeType)
    {
        if (terrainHeight == 0) return gridManager.GetWorldPositionVector(x, y, 1);

        bool anchorAtUpperLikeElbow = IsDiagonalRoadPrefab(prefab)
            || (IsCardinalSlopeRoadPrefab(prefab) && TerrainSlopeIsDiagonalOrCornerUp(slopeType));
        if (!anchorAtUpperLikeElbow) return gridManager.GetWorldPosition(x, y);
        if (slopeType == TerrainSlopeType.Flat) return gridManager.GetWorldPosition(x, y);

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
            else return gridManager.GetWorldPosition(x, y);
        }

        if (upperX < 0 || upperX >= gridManager.width || upperY < 0 || upperY >= gridManager.height)
            return gridManager.GetWorldPosition(x, y);

        CityCell upperCell = gridManager.GetCell(upperX, upperY);
        if (upperCell == null) return gridManager.GetWorldPosition(x, y);

        int upperHeight = upperCell.GetCellInstanceHeight();
        return gridManager.GetWorldPositionVector(upperX, upperY, upperHeight);
    }

    private bool TryGetUpperNeighborFromSlopeType(TerrainSlopeType slopeType, int x, int y, out int upperX, out int upperY)
    {
        upperX = x; upperY = y;
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

    private int GetNeighborHeight(int gridX, int gridY, int dx, int dy)
    {
        int nx = gridX + dx;
        int ny = gridY + dy;
        if (nx < 0 || nx >= gridManager.width || ny < 0 || ny >= gridManager.height) return int.MinValue;
        CityCell c = gridManager.GetCell(nx, ny);
        return c != null ? c.GetCellInstanceHeight() : int.MinValue;
    }

    private bool IsRoadAt(Vector2 gridPos)
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
}
}
