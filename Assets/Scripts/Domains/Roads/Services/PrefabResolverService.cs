// registry-resolve-exempt: internal factory — constructs own sub-services (PrefabLookupService, PrefabVariantPickService, PrefabCacheService) within Roads domain
using System.Collections.Generic;
using UnityEngine;
using Territory.Core;
using Territory.Terrain;
using Territory.Zones;
using Territory.Roads;

namespace Domains.Roads.Services
{
/// <summary>
/// Topology enum shared by PrefabResolverService and PrefabLookupService.
/// </summary>
internal enum PathRouteTopology
{
    Isolated,
    End,
    StraightThrough,
    Corner90,
    Junction
}

/// <summary>
/// Thin orchestrator for road prefab resolution. Delegates to sub-services.
/// Facade contract preserved: ResolveForPath, ResolveForCell, ResolveForGhostPreview.
/// Invariant #2 (InvalidateRoadCache) preserved in GridManager/RoadManager.
/// Stage 7.3 Tier-E split: delegates to PrefabLookupService + PrefabVariantPickService + PrefabCacheService.
/// </summary>
public class PrefabResolverService
{
    private readonly IGridManager gridManager;
    private readonly ITerrainManager terrainManager;
    private readonly IRoadManager roadManager;

    private readonly PrefabLookupService lookup;
    private readonly PrefabVariantPickService variantPick;
    private readonly PrefabCacheService cache;

    private readonly List<Vector2Int> scratchHighDeckBridgeNormals = new List<Vector2Int>(4);

    /// <summary>Construct prefab resolver — wires lookup + variantPick + cache subservices.</summary>
    public PrefabResolverService(IGridManager grid, ITerrainManager terrain, IRoadManager roads)
    {
        gridManager = grid;
        terrainManager = terrain;
        roadManager = roads;

        lookup = new PrefabLookupService(grid, roads);
        variantPick = new PrefabVariantPickService(grid, terrain, roads, lookup);
        cache = new PrefabCacheService(grid, terrain, roads, lookup);
    }

    // ResolvedRoadTile struct lifted to Core (Assets/Scripts/Core/Roads/ResolvedRoadTile.cs) — Territory.Roads ns.

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
            if (postSlope == TerrainSlopeType.Flat && terrainManager != null && prefab != null && variantPick.IsCardinalSlopeRoadPrefab(prefab))
            {
                TerrainSlopeType live = terrainManager.GetTerrainSlopeTypeAt(x, y);
                if (live != TerrainSlopeType.Flat)
                    slopeForWorld = live;
            }

            bool isBridgeDeck = cache.IsBridgeDeckRoadPrefab(prefab);
            int sortHeight = height;
            Vector2 worldPos;
            if (isBridgeDeck && plan != null && plan.waterBridgeDeckDisplayHeight > 0)
            {
                sortHeight = plan.waterBridgeDeckDisplayHeight;
                worldPos = gridManager.GetWorldPositionVector(x, y, plan.waterBridgeDeckDisplayHeight);
            }
            else
                worldPos = variantPick.GetWorldPositionForPrefab(x, y, prefab, height, slopeForWorld);

            int sortingOrder = gridManager.GetRoadSortingOrderForCell(x, y, sortHeight);

            bool hasPrevHint = i > 0;
            Vector2Int prevGrid = hasPrevHint
                ? new Vector2Int(Mathf.RoundToInt(path[i - 1].x), Mathf.RoundToInt(path[i - 1].y))
                : default;
            bool hasNextHint = i < path.Count - 1;
            Vector2Int nextGrid = hasNextHint
                ? new Vector2Int(Mathf.RoundToInt(path[i + 1].x), Mathf.RoundToInt(path[i + 1].y))
                : default;
            Vector2Int entryStep = PrefabLookupService.GetCardinalGridStepRounded(curr - prev);
            Vector2 nextForStep = i < path.Count - 1 ? path[i + 1] : curr;
            Vector2Int exitStep = hasNextHint ? PrefabLookupService.GetCardinalGridStepRounded(nextForStep - curr) : default;

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

        bool hasLeft = lookup.IsRoadAt(currGridPos + new Vector2(-1, 0));
        bool hasRight = lookup.IsRoadAt(currGridPos + new Vector2(1, 0));
        bool hasUp = lookup.IsRoadAt(currGridPos + new Vector2(0, 1));
        bool hasDown = lookup.IsRoadAt(currGridPos + new Vector2(0, -1));

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
            prefab = lookup.SelectFromConnectivity(prevGridPos, currGridPos, hasLeft, hasRight, hasUp, hasDown, height, variantPick);

            HeightMap hm = terrainManager != null ? terrainManager.GetHeightMap() : null;
            IWaterManager wm = cache.ResolveWaterManagerForBridge();
            if (hm != null && wm != null && terrainManager != null
                && height > 0
                && !terrainManager.IsRegisteredOpenWaterAt(x, y) && !terrainManager.IsWaterSlopeCell(x, y)
                && cache.DryLandCardinalLowerTouchesRegisteredWater(x, y, height, hm))
            {
                cache.CollectHighDeckBridgeNormals(x, y, height, hm, scratchHighDeckBridgeNormals);
                Vector2 approachDir = currGridPos - prevGridPos;
                if (cache.CardinalApproachParallelToHighDeckNormal(approachDir, scratchHighDeckBridgeNormals))
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
        bool needsDeckInference = cache.IsBridgeDeckRoadPrefab(prefab)
            && terrainManager != null
            && (terrainManager.IsRegisteredOpenWaterAt(x, y)
                || terrainManager.IsWaterSlopeCell(x, y)
                || height == 0);
        bool dryCliffBridgeDeck = cache.IsBridgeDeckRoadPrefab(prefab) && height > 0 && hmCell != null
            && terrainManager != null
            && !terrainManager.IsRegisteredOpenWaterAt(x, y) && !terrainManager.IsWaterSlopeCell(x, y)
            && cache.DryLandCardinalLowerTouchesRegisteredWater(x, y, height, hmCell);
        if (needsDeckInference && cache.TryInferWaterBridgeDeckDisplayHeight(x, y, out int deckH) && deckH > 0)
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
            worldPos = variantPick.GetWorldPositionForPrefab(x, y, prefab, height, terrainManager?.GetTerrainSlopeTypeAt(x, y) ?? TerrainSlopeType.Flat);

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
            && cache.DryLandCardinalLowerTouchesRegisteredWater(x, y, height, ghostHm))
        {
            Vector2 towardLower = cache.InferStrongestCardinalTowardQualifyingLowerNeighbor(x, y, height, ghostHm);
            bool isHorizontal = Mathf.Abs(towardLower.x) >= Mathf.Abs(towardLower.y);
            prefab = isHorizontal ? roadManager.roadTileBridgeHorizontal : roadManager.roadTileBridgeVertical;
            worldPos = gridManager.GetWorldPositionVector(x, y, height);
            sortingOrder = gridManager.GetRoadSortingOrderForCell(x, y, height);
            return;
        }

        GameObject slopePrefab = variantPick.TryGetSlopePrefabForCell(new Vector2(x, y), height);
        if (slopePrefab != null)
        {
            prefab = slopePrefab;
            worldPos = variantPick.GetWorldPositionForPrefab(x, y, slopePrefab, height, terrainManager?.GetTerrainSlopeTypeAt(x, y) ?? TerrainSlopeType.Flat);
            sortingOrder = gridManager.GetRoadSortingOrderForCell(x, y, height);
            return;
        }

        worldPos = variantPick.GetWorldPositionForPrefab(x, y, roadManager.roadTilePrefab1, height, TerrainSlopeType.Flat);
        sortingOrder = gridManager.GetRoadSortingOrderForCell(x, y, height);
    }

    private GameObject ResolvePrefabForPathCell(Vector2 prev, Vector2 curr, HashSet<Vector2Int> pathCellSet,
        int height, TerrainSlopeType postSlope, bool allowLiveSlopeFallback,
        PathTerraformPlan plan, HeightMap heightMap, bool pathOnlyNeighbors)
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

        if (cache.TryGetHighCliffLipBridgePrefab(plan, cx, cy, height, heightMap, dirIn, scratchHighDeckBridgeNormals, out GameObject cliffDeckPrefab))
            return cliffDeckPrefab;

        bool pathLeft = lookup.PathNeighborForResolve(pathCellSet, cx - 1, cy, pathOnlyNeighbors);
        bool pathRight = lookup.PathNeighborForResolve(pathCellSet, cx + 1, cy, pathOnlyNeighbors);
        bool pathUp = lookup.PathNeighborForResolve(pathCellSet, cx, cy + 1, pathOnlyNeighbors);
        bool pathDown = lookup.PathNeighborForResolve(pathCellSet, cx, cy - 1, pathOnlyNeighbors);

        PathRouteTopology topo = PrefabLookupService.ClassifyPathRouteTopology(pathLeft, pathRight, pathUp, pathDown);

        if (topo == PathRouteTopology.Junction)
            return lookup.SelectFromConnectivity(prev, curr, pathLeft, pathRight, pathUp, pathDown, height, variantPick);

        if (topo == PathRouteTopology.Corner90)
        {
            GameObject elbowPrefab = lookup.TryGetElbowPrefab(pathLeft, pathRight, pathUp, pathDown);
            if (elbowPrefab != null) return elbowPrefab;
            return lookup.SelectFromConnectivity(prev, curr, pathLeft, pathRight, pathUp, pathDown, height, variantPick);
        }

        if (topo == PathRouteTopology.StraightThrough || topo == PathRouteTopology.End
            || ((pathLeft || pathRight || pathUp || pathDown) && topo != PathRouteTopology.Isolated))
        {
            bool segmentHorizontal = (dxIn != 0 || dyIn != 0)
                ? Mathf.Abs(dxIn) >= Mathf.Abs(dyIn)
                : (pathLeft || pathRight);
            GameObject straightSlope = variantPick.TrySlopeForStraight(postSlope, segmentHorizontal);
            if (straightSlope != null) return straightSlope;
            if (allowLiveSlopeFallback && postSlope == TerrainSlopeType.Flat)
            {
                GameObject liveSlope = variantPick.TryGetSlopePrefabForStraightSegment(curr, height, segmentHorizontal, prev);
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
        GameObject slopePrefab = variantPick.TrySlopeFromPostTerraform(postSlope, horizontalSeg);
        if (slopePrefab != null) return slopePrefab;

        if (allowLiveSlopeFallback && postSlope == TerrainSlopeType.Flat)
        {
            GameObject liveSlope = variantPick.TryGetSlopePrefabForStraightSegment(curr, height, horizontalSeg, prev);
            if (liveSlope != null) return liveSlope;
        }

        return horizontalSeg ? roadManager.roadTilePrefab2 : roadManager.roadTilePrefab1;
    }
}
}
