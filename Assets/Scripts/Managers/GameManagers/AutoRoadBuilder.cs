using UnityEngine;
using System.Collections.Generic;
using Territory.Core;
using Territory.Roads;
using Territory.Economy;
using Territory.Terrain;
using Territory.Zones;
using Territory.Utilities;

namespace Territory.Simulation
{
/// <summary>
/// Automatically extends the road network during simulation steps by finding optimal paths from existing road edges.
/// Coordinates with GridManager for pathfinding, RoadManager for road placement, and TerrainManager for terrain constraints.
/// </summary>
public class AutoRoadBuilder : MonoBehaviour
{
    #region Dependencies
    public GridManager gridManager;
    public RoadManager roadManager;
    public GrowthBudgetManager growthBudgetManager;
    public CityStats cityStats;
    public InterstateManager interstateManager;
    public TerrainManager terrainManager;
    public TerraformingService terraformingService;
    public AutoZoningManager autoZoningManager;
    public UrbanCentroidService urbanCentroidService;
    #endregion

    #region Configuration
    [Header("Budget per tick")]
    public int maxTilesPerTick = 10;
    /// <summary>Safety cap per tick; actual limit is driven by growth budget. Kept high so budget controls volume.</summary>
    private const int MaxPerTickSafetyCap = 300;

    [Header("Street project: segment-based growth")]
    public int minStreetLength = 4;
    /// <summary>When edges &lt; 4 (recovery), use this lower min length so short streets can open new fronts.</summary>
    public int minStreetLengthRecovery = 3;
    public int maxStreetLength = 20;
    public int maxActiveProjects = 5;
    /// <summary>Min distance from an existing parallel road when starting a new street from an edge (avoids adjacent parallel roads).</summary>
    public int minParallelSpacingFromEdge = 3;
    /// <summary>Min distance between two edges when starting new projects in the same tick (avoids multiple intersections in adjacent cells).</summary>
    public int minEdgeSpacing = 2;
    /// <summary>Max water tiles in a straight line for auto bridge (bridge "less than 6 tiles" = up to 5 water cells).</summary>
    public const int MaxBridgeWaterTiles = 5;
    /// <summary>When connecting to interstate or between clusters, prefer paths that stay this many cells away from existing roads (0 = no preference).</summary>
    public int minRoadSpacingWhenConnecting = 4;
    /// <summary>Unused; roads now prefer high-desirability directions to connect forests/water to urban core.</summary>
    [SerializeField] float desirabilityGrowthPenalty = 0f;

    [Header("Ring-dependent overrides (FEAT-32)")]
    [Tooltip("Extra concurrent projects when 2+ edges are in Inner.")]
    public int coreInnerExtraProjects = 2;
    [Tooltip("Reduction to minEdgeSpacing in Inner (allows more intersections).")]
    public int coreInnerMinEdgeSpacing = 1;

    /// <summary>Completed segment data published for AutoZoningManager to zone strips along.</summary>
    public struct CompletedSegment
    {
        public Vector2Int origin;
        public Vector2Int dir;
        public int length;
        public UrbanRing ring;
    }

    /// <summary>Segment with zoning progress; AutoZoningManager updates zonedUpToIndex and removes when done.</summary>
    public struct PendingZoningSegment
    {
        public CompletedSegment segment;
        public int zonedUpToIndex;
    }

    private struct StreetProject
    {
        public Vector2Int tip;
        public Vector2Int dir;
        public int targetLength;
    }

    private static readonly int[] Dx = { 1, -1, 0, 0 };
    private static readonly int[] Dy = { 0, 0, 1, -1 };

    /// <summary>Segments completed this tick; cleared at start of each ProcessTick.</summary>
    public List<CompletedSegment> CompletedSegmentsThisTick { get; private set; } = new List<CompletedSegment>();

    /// <summary>Segments built that still need zoning; AutoZoningManager removes when fully zoned.</summary>
    public List<PendingZoningSegment> PendingZoningSegments { get; private set; } = new List<PendingZoningSegment>();

    /// <summary>Cells expropriated this tick; must not be zoned until road is placed.</summary>
    public HashSet<Vector2Int> ExpropriatedCellsPendingRoad { get; private set; } = new HashSet<Vector2Int>();

    /// <summary>Positions placed via <see cref="RoadManager.PlaceRoadTileFromResolved"/> this tick; deduped batch refresh for T-junction/cross prefabs.</summary>
    private readonly List<Vector2Int> batchPlacedFromResolvedRoadCells = new List<Vector2Int>();

    #endregion

    #region Road Extension Logic
    string SimDateStr()
    {
        return cityStats != null ? cityStats.currentDate.ToString("yyyy-MM-dd") : "?";
    }

    /// <summary>Places a road tile from a resolved prefab (path pipeline).</summary>
    private bool PlaceRoadTileInBatch(RoadPrefabResolver.ResolvedRoadTile resolved)
    {
        roadManager.PlaceRoadTileFromResolved(resolved);
        batchPlacedFromResolvedRoadCells.Add(resolved.gridPos);
        return true;
    }

    /// <summary>Places a single road tile at position (no path context). Used for expropriated cells and edge extension.</summary>
    private bool PlaceRoadTileInBatch(Vector2 pos)
    {
        return roadManager.PlaceRoadTileAt(pos);
    }

    void Start()
    {
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (roadManager == null) roadManager = FindObjectOfType<RoadManager>();
        if (growthBudgetManager == null) growthBudgetManager = FindObjectOfType<GrowthBudgetManager>();
        if (cityStats == null) cityStats = FindObjectOfType<CityStats>();
        if (interstateManager == null) interstateManager = FindObjectOfType<InterstateManager>();
        if (terrainManager == null) terrainManager = FindObjectOfType<TerrainManager>();
        if (terraformingService == null) terraformingService = FindObjectOfType<TerraformingService>();
        if (autoZoningManager == null) autoZoningManager = FindObjectOfType<AutoZoningManager>();
        if (urbanCentroidService == null) urbanCentroidService = FindObjectOfType<UrbanCentroidService>();
    }

    /// <summary>Shared street validation (bridges + terraform; cut-through allowed). Uses <see cref="RoadManager.TryPrepareRoadPlacementPlanLongestValidPrefix"/>; for straight cardinal segments (street projects), falls back to programmatic lip→chord deck-span.</summary>
    /// <param name="straightBuildDirection">When set, segment must be a straight line in this cardinal direction (simulation street extension).</param>
    bool TryGetStreetPlacementPlan(List<Vector2> pathVec2, Vector2Int? straightBuildDirection, out List<Vector2> expandedPath, out PathTerraformPlan plan)
    {
        if (roadManager != null)
        {
            int hint = 0;
            var ctx = new RoadPathValidationContext { forbidCutThrough = false };
            bool prefixOk = roadManager.TryPrepareRoadPlacementPlanLongestValidPrefix(pathVec2, ctx, false, ref hint, out expandedPath, out plan, out _);
            bool progOk = false;
            List<Vector2> progExpanded = null;
            PathTerraformPlan progPlan = null;
            if (straightBuildDirection.HasValue)
                progOk = roadManager.TryPrepareRoadPlacementPlanWithProgrammaticDeckSpanChord(pathVec2, straightBuildDirection.Value, ctx, out progExpanded, out progPlan);

            if (!prefixOk && progOk)
            {
                expandedPath = progExpanded;
                plan = progPlan;
                return true;
            }
            if (!prefixOk && !progOk)
            {
                expandedPath = null;
                plan = null;
                return false;
            }
            if (prefixOk && !progOk)
                return true;

            bool strokeWet = roadManager.StrokeHasWaterOrWaterSlopeCells(pathVec2);
            bool preferProg = strokeWet || (progExpanded != null && expandedPath != null && progExpanded.Count > expandedPath.Count);
            if (preferProg)
            {
                expandedPath = progExpanded;
                plan = progPlan;
            }
            return true;
        }
        if (pathVec2 == null || pathVec2.Count == 0)
        {
            expandedPath = pathVec2;
            plan = new PathTerraformPlan { isValid = false };
            return false;
        }
        expandedPath = pathVec2.Count >= 2 ? TerraformingService.ExpandDiagonalStepsToCardinal(pathVec2) : pathVec2;
        plan = terraformingService != null ? terraformingService.ComputePathPlan(expandedPath) : new PathTerraformPlan { isValid = false };
        return plan.isValid;
    }

    public void ProcessTick()
    {
        if (growthBudgetManager == null || roadManager == null || gridManager == null || cityStats == null)
            return;
        if (!cityStats.simulateGrowth)
            return;

        CompletedSegmentsThisTick.Clear();
        batchPlacedFromResolvedRoadCells.Clear();

        var edges = gridManager.GetRoadEdgePositions();
        var allRoads = gridManager.GetAllRoadPositions();
        var roadSet = new HashSet<Vector2Int>(allRoads);
        int edgeCount = edges.Count;

        int available = growthBudgetManager.GetAvailableBudget(GrowthCategory.Roads);
        int costPerTile = RoadManager.RoadCostPerTile;
        int maxByBudget = costPerTile > 0 ? available / costPerTile : 0;
        int toPlace = Mathf.Min(MaxPerTickSafetyCap, maxByBudget);

        int placed = 0;
        int effectiveMinStreetLength = edgeCount < 4 ? minStreetLengthRecovery : minStreetLength;

        if (interstateManager != null && !interstateManager.IsConnectedToInterstate)
        {
            placed = TryConnectToInterstate(toPlace, allRoads);
            if (placed > 0)
            {
                FlushBatchRoadPrefabRefresh();
                gridManager.InvalidateRoadCache();
                return;
            }
        }

        List<List<Vector2Int>> clusters = GetRoadClusters(allRoads);
        if (clusters.Count > 1)
        {
            placed = TryConnectDisconnected(clusters, toPlace);
            if (placed > 0)
            {
                FlushBatchRoadPrefabRefresh();
                gridManager.InvalidateRoadCache();
                return;
            }
        }

        int innerEdgeCount = CountInnerEdges(edges);
        int effectiveMaxProjects = maxActiveProjects + (innerEdgeCount >= 2 ? coreInnerExtraProjects : 0);

        int newProjectsStarted = 0;
        while (toPlace > 0 && newProjectsStarted < effectiveMaxProjects)
        {
            if (!TryStartNewStreetProject(ref toPlace, effectiveMinStreetLength, edges, roadSet))
                break;
            newProjectsStarted++;
        }
        if (newProjectsStarted == 0 && toPlace > 0 && effectiveMinStreetLength > minStreetLengthRecovery)
        {
            if (TryStartNewStreetProject(ref toPlace, minStreetLengthRecovery, edges, roadSet))
                newProjectsStarted++;
        }

        // Refresh road data after street projects placed roads, so expropriation sees fresh state
        if (newProjectsStarted > 0)
        {
            gridManager.InvalidateRoadCache();
            edges = gridManager.GetRoadEdgePositions();
            roadSet = new HashSet<Vector2Int>(gridManager.GetAllRoadPositions());
        }

        if (newProjectsStarted == 0 && toPlace > 0)
        {
            if (TryExpropriateForLongBlockedSegment(edges, roadSet))
            {
                gridManager.InvalidateRoadCache();
                int placedExp = PlaceRoadsInExpropriatedCells(ref toPlace);
                if (placedExp > 0)
                    placed += placedExp;
                edges = gridManager.GetRoadEdgePositions();
                roadSet = new HashSet<Vector2Int>(gridManager.GetAllRoadPositions());
                if (TryStartNewStreetProject(ref toPlace, effectiveMinStreetLength, edges, roadSet))
                    newProjectsStarted++;
            }
        }

        if (toPlace > 0 && ExpropriatedCellsPendingRoad.Count > 0)
        {
            int placedExp = PlaceRoadsInExpropriatedCells(ref toPlace);
            if (placedExp > 0)
                placed += placedExp;
        }

        FlushBatchRoadPrefabRefresh();
        if (placed > 0 || newProjectsStarted > 0)
            gridManager.InvalidateRoadCache();
    }

    /// <summary>One deduplicated junction refresh after all <see cref="PlaceRoadTileFromResolved"/> placements in the tick.</summary>
    void FlushBatchRoadPrefabRefresh()
    {
        if (batchPlacedFromResolvedRoadCells.Count == 0 || roadManager == null)
            return;
        roadManager.RefreshRoadPrefabsAfterBatchPlacement(batchPlacedFromResolvedRoadCells);
        batchPlacedFromResolvedRoadCells.Clear();
    }

    /// <summary>
    /// Builds a complete street segment in one tick using the path pipeline. Strokes that cross water require a firm dry exit cell, enough remaining tile budget
    /// for every new road tile on the resolved path, and all-or-nothing placement (single lump <see cref="GrowthBudgetManager.TrySpend"/>); otherwise the plan is reverted and nothing is placed.
    /// </summary>
    private int BuildFullSegmentInOneTick(StreetProject p, ref int budgetRemaining, HashSet<Vector2Int> roadSet)
    {
        Vector2Int origin = p.tip;
        Vector2Int dir = p.dir;
        int w = gridManager.width, h = gridManager.height;

        var path = new List<Vector2Int>();
        int x = origin.x, y = origin.y;
        int maxLen = Mathf.Min(p.targetLength, budgetRemaining);

        for (int i = 0; i < maxLen; i++)
        {
            if (x < 0 || x >= w || y < 0 || y >= h) break;
            Cell c = gridManager.GetCell(x, y);
            if (c != null && c.zoneType == Zone.ZoneType.Road) break;
            if (!IsCellPlaceableForRoad(x, y)) break;
            if (!IsSuitableForRoad(x, y, dir)) break;
            path.Add(new Vector2Int(x, y));
            x += dir.x;
            y += dir.y;
        }

        if (path.Count == 0) return 0;

        var pathVec2 = new List<Vector2>();
        for (int i = 0; i < path.Count; i++)
            pathVec2.Add(new Vector2(path[i].x, path[i].y));

        if (roadManager != null && roadManager.TryExtendCardinalStreetPathWithBridgeChord(pathVec2, dir))
        {
            for (int i = path.Count; i < pathVec2.Count; i++)
                path.Add(new Vector2Int((int)pathVec2[i].x, (int)pathVec2[i].y));
        }

        const int maxShortenAttempts = 3;
        int shortenCount = 0;
        List<Vector2> expandedPath = null;
        PathTerraformPlan plan = null;
        while (path.Count >= 1 && shortenCount < maxShortenAttempts)
        {
            if (TryGetStreetPlacementPlan(pathVec2, dir, out expandedPath, out plan) && plan != null && plan.isValid)
                break;
            if (path.Count <= 1) break;
            path.RemoveAt(path.Count - 1);
            pathVec2.RemoveAt(pathVec2.Count - 1);
            shortenCount++;
        }

        if (!(plan != null && plan.isValid) && path.Count >= 2)
        {
            Vector2Int edge = new Vector2Int(origin.x - dir.x, origin.y - dir.y);
            Vector2Int target = path[path.Count - 1];
            var aStarPath = gridManager.FindPathForAutoSimulation(edge, target);
            if (aStarPath != null && aStarPath.Count >= 2)
            {
                path.Clear();
                pathVec2.Clear();
                for (int i = 0; i < aStarPath.Count; i++)
                {
                    if (aStarPath[i] == edge) continue;
                    path.Add(aStarPath[i]);
                    pathVec2.Add(new Vector2(aStarPath[i].x, aStarPath[i].y));
                }
                if (path.Count > 0)
                {
                    if (!TryGetStreetPlacementPlan(pathVec2, dir, out expandedPath, out plan) || plan == null || !plan.isValid)
                    {
                        path.Clear();
                        pathVec2.Clear();
                    }
                }
            }
        }

        if (path.Count == 0 || plan == null || !plan.isValid || expandedPath == null) return 0;

        var heightMap = terrainManager != null ? terrainManager.GetHeightMap() : null;

        bool waterCrossing = roadManager != null && roadManager.StrokeHasWaterOrWaterSlopeCells(expandedPath);
        bool atomicWaterBridge = waterCrossing;
        if (atomicWaterBridge && (roadManager == null || !roadManager.StrokeLastCellIsFirmDryLand(expandedPath)))
            return 0;

        List<RoadPrefabResolver.ResolvedRoadTile> resolvedPreApply = null;
        if (atomicWaterBridge && roadManager != null && !plan.HasTerraformHeightMutation())
            resolvedPreApply = roadManager.ResolvePathForRoads(expandedPath, plan);

        int CountNonRoadTilesOnResolved(List<RoadPrefabResolver.ResolvedRoadTile> r)
        {
            if (r == null || gridManager == null) return 0;
            int n = 0;
            for (int i = 0; i < r.Count; i++)
            {
                Cell c = gridManager.GetCell(r[i].gridPos.x, r[i].gridPos.y);
                if (c != null && c.zoneType != Zone.ZoneType.Road)
                    n++;
            }
            return n;
        }

        int CountNonRoadTilesOnExpandedPath()
        {
            int n = 0;
            for (int i = 0; i < expandedPath.Count; i++)
            {
                int x = (int)expandedPath[i].x, y = (int)expandedPath[i].y;
                Cell c = gridManager.GetCell(x, y);
                if (c != null && c.zoneType != Zone.ZoneType.Road)
                    n++;
            }
            return n;
        }

        if (atomicWaterBridge && growthBudgetManager != null)
        {
            int needPre = resolvedPreApply != null ? CountNonRoadTilesOnResolved(resolvedPreApply) : CountNonRoadTilesOnExpandedPath();
            if (needPre > budgetRemaining)
                return 0;
        }

        if (heightMap != null && !plan.Apply(heightMap, terrainManager))
            return 0;

        var resolved = roadManager != null ? roadManager.ResolvePathForRoads(expandedPath, plan) : new List<RoadPrefabResolver.ResolvedRoadTile>();
        if (resolved.Count == 0)
            return 0;

        int placed = 0;
        if (atomicWaterBridge)
        {
            int tilesToPlace = CountNonRoadTilesOnResolved(resolved);
            if (tilesToPlace > budgetRemaining)
            {
                if (heightMap != null)
                    plan.Revert(heightMap, terrainManager);
                return 0;
            }

            int lumpCost = tilesToPlace * RoadManager.RoadCostPerTile;
            if (tilesToPlace > 0)
            {
                if (growthBudgetManager == null || !growthBudgetManager.TrySpend(GrowthCategory.Roads, lumpCost))
                {
                    if (heightMap != null)
                        plan.Revert(heightMap, terrainManager);
                    return 0;
                }
                budgetRemaining -= tilesToPlace;
            }

            for (int i = 0; i < resolved.Count; i++)
            {
                Cell c = gridManager.GetCell(resolved[i].gridPos.x, resolved[i].gridPos.y);
                if (c != null && c.zoneType == Zone.ZoneType.Road)
                    continue;
                PlaceRoadTileInBatch(resolved[i]);
                placed++;
                roadSet.Add(resolved[i].gridPos);
            }

            if (placed == 0)
                return 0;
        }
        else
        {
            for (int i = 0; i < resolved.Count && budgetRemaining > 0; i++)
            {
                if (!growthBudgetManager.TrySpend(GrowthCategory.Roads, RoadManager.RoadCostPerTile)) break;
                if (PlaceRoadTileInBatch(resolved[i]))
                {
                    placed++;
                    budgetRemaining--;
                    roadSet.Add(resolved[i].gridPos);
                }
            }

            if (placed == 0) return 0;
        }

        UrbanRing ring = urbanCentroidService != null ? urbanCentroidService.GetUrbanRing(new Vector2(origin.x, origin.y)) : UrbanRing.Mid;
        var completed = new CompletedSegment { origin = origin, dir = dir, length = placed, ring = ring };
        CompletedSegmentsThisTick.Add(completed);
        PendingZoningSegments.Add(new PendingZoningSegment { segment = completed, zonedUpToIndex = -1 });
        return placed;
    }

    private bool TryStartNewStreetProject(ref int budgetRemaining, int effectiveMinStreetLength, List<Vector2Int> edges, HashSet<Vector2Int> roadSet)
    {
        if (edges.Count == 0) return false;

        RingStreetParams fallbackParams = new RingStreetParams
        {
            minLength = effectiveMinStreetLength,
            maxLength = maxStreetLength,
            parallelSpacing = minParallelSpacingFromEdge,
            parallelSpacingMin = minParallelSpacingFromEdge,
            parallelSpacingMax = minParallelSpacingFromEdge
        };

        var withScore = new List<KeyValuePair<Vector2Int, float>>(edges.Count);
        bool centroidShifted = urbanCentroidService != null && urbanCentroidService.CentroidShiftedRecently;
        foreach (Vector2Int e in edges)
        {
            UrbanRing eRing = urbanCentroidService != null ? urbanCentroidService.GetUrbanRing(new Vector2(e.x, e.y)) : UrbanRing.Mid;
            int ringPriority = urbanCentroidService != null ? GetRingPriority(eRing) : 4;
            if (centroidShifted && (eRing == UrbanRing.Inner))
                ringPriority += 3;
            int grass = CountGrassNeighbors(e);
            RingStreetParams edgeParams = urbanCentroidService != null ? urbanCentroidService.GetStreetParamsForRing(eRing) : fallbackParams;
            int spacing = GetEffectiveParallelSpacing(edgeParams);
            float bestUtil = 0f;
            for (int d = 0; d < 4; d++)
            {
                int nx = e.x + Dx[d], ny = e.y + Dy[d];
                if (nx < 0 || nx >= gridManager.width || ny < 0 || ny >= gridManager.height) continue;
                if (!IsCellPlaceableForRoad(nx, ny)) continue;
                Vector2Int dir = new Vector2Int(Dx[d], Dy[d]);
                if (!IsSuitableForRoad(nx, ny, dir)) continue;
                Vector2Int rdE = GetRoadDirectionAtEdge(e, roadSet);
                Vector2Int? exclE = (rdE.x != 0 || rdE.y != 0) && (dir.x * rdE.x + dir.y * rdE.y) == 0 ? (Vector2Int?)rdE : null;
                if (HasParallelRoadTooClose(e, dir, spacing, roadSet, exclE)) continue;
                Vector2Int tip = new Vector2Int(nx, ny);
                int len = HowFarWeCanBuild(tip, dir);
                int scoreMin = edgeParams.minLength;
                if (IsEdgeOnInterstate(e)) scoreMin = Mathf.Min(scoreMin, 2);
                if (len < scoreMin && len >= 2 && IsDirectionBlockedBySlopeOrWater(tip, dir, len)) scoreMin = 2;
                if (len < scoreMin) continue;
                float u = CalculateDirectionUtility(e, dir, 5, spacing, roadSet);
                if (u > bestUtil) bestUtil = u;
            }
            float score = ringPriority * 100f + grass * 5f + bestUtil * 10f;
            withScore.Add(new KeyValuePair<Vector2Int, float>(e, score));
        }
        withScore.Sort((a, b) => b.Value.CompareTo(a.Value));
        var consideredEdges = new HashSet<Vector2Int>();

        foreach (var kv in withScore)
        {
            Vector2Int edge = kv.Key;

            UrbanRing edgeRing = urbanCentroidService != null ? urbanCentroidService.GetUrbanRing(new Vector2(edge.x, edge.y)) : UrbanRing.Mid;
            RingStreetParams @params = fallbackParams;
            if (urbanCentroidService != null)
            {
                @params = urbanCentroidService.GetStreetParamsForRing(edgeRing);
                if (edgeRing != UrbanRing.Inner)
                    @params.minLength = Mathf.Max(@params.minLength, effectiveMinStreetLength);
                if (edgeRing == UrbanRing.Mid || edgeRing == UrbanRing.Outer || edgeRing == UrbanRing.Rural)
                    @params.minLength = Mathf.Max(@params.minLength, 3);
            }
            else
            {
                @params.parallelSpacing = minParallelSpacingFromEdge;
                @params.parallelSpacingMin = minParallelSpacingFromEdge;
                @params.parallelSpacingMax = minParallelSpacingFromEdge;
            }
            if (IsEdgeOnInterstate(edge))
                @params.minLength = Mathf.Min(@params.minLength, 2);

            int effectiveEdgeSpacing = GetEffectiveMinEdgeSpacing(edgeRing);
            if (effectiveEdgeSpacing > 0)
            {
                bool tooCloseToConsidered = false;
                foreach (Vector2Int c in consideredEdges)
                {
                    if (Mathf.Abs(edge.x - c.x) + Mathf.Abs(edge.y - c.y) < effectiveEdgeSpacing)
                    {
                        tooCloseToConsidered = true;
                        break;
                    }
                }
                if (tooCloseToConsidered) continue;
                consideredEdges.Add(edge);
            }

            var validDirections = new List<(Vector2Int dir, Vector2Int tip, int len)>();
            for (int d = 0; d < 4; d++)
            {
                int nx = edge.x + Dx[d], ny = edge.y + Dy[d];
                if (nx < 0 || nx >= gridManager.width || ny < 0 || ny >= gridManager.height)
                    continue;
                if (!IsCellPlaceableForRoad(nx, ny))
                    continue;
                Vector2Int dir = new Vector2Int(Dx[d], Dy[d]);
                Vector2Int tip = new Vector2Int(nx, ny);
                if (!IsSuitableForRoad(nx, ny, dir))
                    continue;
                int spacing = GetEffectiveParallelSpacing(@params);
                Vector2Int rdEdge = GetRoadDirectionAtEdge(edge, roadSet);
                Vector2Int? exclEdge = (rdEdge.x != 0 || rdEdge.y != 0) && (dir.x * rdEdge.x + dir.y * rdEdge.y) == 0 ? (Vector2Int?)rdEdge : null;
                if (HasParallelRoadTooClose(edge, dir, spacing, roadSet, exclEdge))
                    continue;
                int len = HowFarWeCanBuild(tip, dir);
                int effectiveMin = @params.minLength;
                if (len < effectiveMin && len >= 2 && IsDirectionBlockedBySlopeOrWater(tip, dir, len))
                    effectiveMin = 2;
                if (len < effectiveMin)
                    continue;
                validDirections.Add((dir, tip, len));
            }

            if (validDirections.Count > 0)
            {
                UrbanRing ringForSort = edgeRing;
                Vector2Int roadDir = GetRoadDirectionAtEdge(edge, roadSet);
                int sortSpacing = GetEffectiveParallelSpacing(@params);
                validDirections.Sort((a, b) =>
                {
                    if (ringForSort == UrbanRing.Inner)
                    {
                        bool aPerp = roadDir.x == 0 && roadDir.y == 0 ? false : (a.dir.x * roadDir.x + a.dir.y * roadDir.y) == 0;
                        bool bPerp = roadDir.x == 0 && roadDir.y == 0 ? false : (b.dir.x * roadDir.x + b.dir.y * roadDir.y) == 0;
                        if (aPerp != bPerp) return aPerp ? -1 : 1;
                    }
                    float utilA = CalculateDirectionUtility(edge, a.dir, 5, sortSpacing, roadSet);
                    float utilB = CalculateDirectionUtility(edge, b.dir, 5, sortSpacing, roadSet);
                    return utilB.CompareTo(utilA);
                });
            }

            foreach (var (dir, tip, len) in validDirections)
            {
                int targetLen = Mathf.Clamp(Random.Range(@params.minLength, @params.maxLength + 1), @params.minLength, len);
                var project = new StreetProject { tip = tip, dir = dir, targetLength = targetLen };
                int placed = BuildFullSegmentInOneTick(project, ref budgetRemaining, roadSet);
                if (placed > 0)
                    return true;
            }
        }

        return false;
    }
    #endregion

    #region Road Placement
    /// <summary>
    /// When no new street projects can start, only expropriate if a long segment (length > maxLength for ring)
    /// has both perpendicular strips fully occupied. Demolishes L cells in one perpendicular direction from
    /// a road cell at distance L from an intersection. Never expropriates for interstate or cluster connection.
    /// </summary>
    private bool TryExpropriateForLongBlockedSegment(List<Vector2Int> edges, HashSet<Vector2Int> roadSet)
    {
        var segments = GetStraightSegmentsFromGrid(roadSet, edges);
        int w = gridManager.width, h = gridManager.height;

        foreach (var (origin, dir, length) in segments)
        {
            Vector2 mid = new Vector2(origin.x + (length / 2) * dir.x, origin.y + (length / 2) * dir.y);
            UrbanRing ring = urbanCentroidService != null ? urbanCentroidService.GetUrbanRing(mid) : UrbanRing.Mid;
            RingStreetParams ringParams = urbanCentroidService != null ? urbanCentroidService.GetStreetParamsForRing(ring) : new RingStreetParams { minLength = 4, maxLength = 20 };

            if (length <= ringParams.maxLength) continue;
            if (!IsSegmentFullyBlocked(origin, dir, length, roadSet)) continue;

            Vector2Int end = new Vector2Int(origin.x + (length - 1) * dir.x, origin.y + (length - 1) * dir.y);
            int roadNeighborsOrigin = gridManager.CountRoadNeighbors(origin.x, origin.y);
            int roadNeighborsEnd = gridManager.CountRoadNeighbors(end.x, end.y);

            Vector2Int intersection;
            Vector2Int anchorDir;
            if (roadNeighborsEnd >= 2)
            {
                intersection = end;
                anchorDir = new Vector2Int(-dir.x, -dir.y);
            }
            else if (roadNeighborsOrigin >= 2)
            {
                intersection = origin;
                anchorDir = dir;
            }
            else
                continue;

            int L = Mathf.Clamp(Random.Range(ringParams.minLength, Mathf.Min(ringParams.minLength + 2, ringParams.maxLength)), 1, length - 1);
            Vector2Int anchor = new Vector2Int(intersection.x + L * anchorDir.x, intersection.y + L * anchorDir.y);
            if (!roadSet.Contains(anchor)) continue;

            Vector2Int perp = new Vector2Int(-dir.y, dir.x);
            int perpSign = Random.value < 0.5f ? 1 : -1;

            var demolished = new List<Vector2Int>();
            for (int j = 1; j <= L; j++)
            {
                Vector2Int cell = new Vector2Int(anchor.x + perpSign * j * perp.x, anchor.y + perpSign * j * perp.y);
                if (cell.x < 0 || cell.x >= w || cell.y < 0 || cell.y >= h) continue;

                Cell c = gridManager.GetCell(cell.x, cell.y);
                if (c == null) continue;
                if (c.GetCellInstanceHeight() == 0) continue;
                string bt = c.GetBuildingType();
                if (bt == "PowerPlant" || bt == "WaterPlant") continue;

                if (gridManager.DemolishCellAt(new Vector2(cell.x, cell.y), showAnimation: false))
                {
                    demolished.Add(cell);
                    ExpropriatedCellsPendingRoad.Add(cell);
                }
            }

            if (demolished.Count > 0)
                return true;
        }
        return false;
    }

    /// <summary>Place roads in expropriated cells to prevent opportunistic zoning. Returns tiles placed.</summary>
    private int PlaceRoadsInExpropriatedCells(ref int budgetRemaining)
    {
        int placed = 0;
        var toPlace = new List<Vector2Int>(ExpropriatedCellsPendingRoad);
        foreach (Vector2Int pos in toPlace)
        {
            if (budgetRemaining <= 0) break;
            Cell c = gridManager.GetCell(pos.x, pos.y);
            if (c == null || c.zoneType == Zone.ZoneType.Road)
            {
                ExpropriatedCellsPendingRoad.Remove(pos);
                continue;
            }
            if (!IsCellPlaceableForRoad(pos.x, pos.y)) continue;
            if (!growthBudgetManager.TrySpend(GrowthCategory.Roads, RoadManager.RoadCostPerTile)) continue;
            if (PlaceRoadTileInBatch(new Vector2(pos.x, pos.y)))
            {
                ExpropriatedCellsPendingRoad.Remove(pos);
                placed++;
                budgetRemaining--;
            }
        }
        return placed;
    }

    /// <summary>Place at most one road tile from current road edges (used after expropriation to fill freed space first).</summary>
    private int TryPlaceOneTileFromEdges(List<Vector2Int> edges)
    {
        if (edges.Count == 0)
        {
            return 0;
        }
        var withScore = new List<KeyValuePair<Vector2Int, int>>(edges.Count);
        foreach (Vector2Int e in edges)
            withScore.Add(new KeyValuePair<Vector2Int, int>(e, CountGrassNeighbors(e)));
        withScore.Sort((a, b) => b.Value.CompareTo(a.Value));
        foreach (var kv in withScore)
        {
            Vector2Int edge = kv.Key;
            for (int d = 0; d < 4; d++)
            {
                int nx = edge.x + Dx[d], ny = edge.y + Dy[d];
                if (nx < 0 || nx >= gridManager.width || ny < 0 || ny >= gridManager.height)
                    continue;
                if (!IsCellPlaceableForRoad(nx, ny))
                    continue;
                if (!growthBudgetManager.TrySpend(GrowthCategory.Roads, RoadManager.RoadCostPerTile))
                    continue;
                if (PlaceRoadTileInBatch(new Vector2(nx, ny)))
                    return 1;
                return 0; // presupuesto gastado pero colocación falló
            }
        }
        return 0;
    }
    #endregion

    #region Path Planning
    private int HowFarWeCanBuild(Vector2Int start, Vector2Int dir)
    {
        int count = 0;
        int waterRun = 0;
        int x = start.x, y = start.y;
        int w = gridManager.width, h = gridManager.height;
        while (x >= 0 && x < w && y >= 0 && y < h)
        {
            if (!IsCellPlaceableForRoad(x, y))
                break;
            if (!IsSuitableForRoad(x, y, dir))
                break;
            Cell c = gridManager.GetCell(x, y);
            bool isWater = c != null && c.GetCellInstanceHeight() == 0;
            if (isWater)
            {
                int landStep = -1;
                for (int k = 1; k <= MaxBridgeWaterTiles; k++)
                {
                    int nx = x + k * dir.x, ny = y + k * dir.y;
                    if (nx < 0 || nx >= w || ny < 0 || ny >= h) break;
                    Cell nc = gridManager.GetCell(nx, ny);
                    if (nc == null || nc.GetCellInstanceHeight() == 0) continue;
                    if (!IsCellPlaceableForRoad(nx, ny) || !IsSuitableForRoad(nx, ny, dir)) continue;
                    landStep = k;
                    break;
                }
                if (landStep < 0)
                    break;
                count += (1 + landStep);
                x += (landStep + 1) * dir.x;
                y += (landStep + 1) * dir.y;
                waterRun = 0;
                continue;
            }
            waterRun = 0;
            count++;
            x += dir.x;
            y += dir.y;
        }
        return count;
    }

    /// <summary>Average desirability of cells along a direction from start (FEAT-26). Samples up to sampleCount cells.</summary>
    private float GetAverageDesirabilityInDirection(Vector2Int start, Vector2Int dir, int sampleCount)
    {
        float sum = 0f;
        int count = 0;
        int x = start.x, y = start.y;
        int w = gridManager.width, h = gridManager.height;
        for (int k = 0; k < sampleCount; k++)
        {
            x += dir.x;
            y += dir.y;
            if (x < 0 || x >= w || y < 0 || y >= h) break;
            Cell c = gridManager.GetCell(x, y);
            if (c != null)
            {
                sum += c.desirability;
                count++;
            }
        }
        return count > 0 ? sum / count : 0f;
    }

    /// <summary>Returns the dominant road direction at this edge (for perpendicular preference). Zero if no clear direction.</summary>
    private Vector2Int GetRoadDirectionAtEdge(Vector2Int edge, HashSet<Vector2Int> roadSet)
    {
        int roadX = 0, roadY = 0;
        for (int d = 0; d < 4; d++)
        {
            var n = new Vector2Int(edge.x + Dx[d], edge.y + Dy[d]);
            if (roadSet.Contains(n))
            {
                roadX += Dx[d];
                roadY += Dy[d];
            }
        }
        if (roadX == 0 && roadY == 0) return Vector2Int.zero;
        if (Mathf.Abs(roadX) >= Mathf.Abs(roadY))
            return new Vector2Int(roadX > 0 ? 1 : -1, 0);
        return new Vector2Int(0, roadY > 0 ? 1 : -1);
    }

    private static int GetRingPriority(UrbanRing ring)
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

    /// <summary>Min edge spacing; lower in Inner to allow more intersections (FEAT-32).</summary>
    private int GetEffectiveMinEdgeSpacing(UrbanRing ring)
    {
        if ((ring == UrbanRing.Inner) && minEdgeSpacing > coreInnerMinEdgeSpacing)
            return minEdgeSpacing - coreInnerMinEdgeSpacing;
        return minEdgeSpacing;
    }

    private int CountInnerEdges(List<Vector2Int> edges)
    {
        if (urbanCentroidService == null || edges == null) return 0;
        int count = 0;
        foreach (Vector2Int e in edges)
        {
            UrbanRing ring = urbanCentroidService.GetUrbanRing(new Vector2(e.x, e.y));
            if (ring == UrbanRing.Inner)
                count++;
        }
        return count;
    }

    private static int GetEffectiveParallelSpacing(RingStreetParams p)
    {
        return p.parallelSpacingMax > p.parallelSpacingMin
            ? Random.Range(p.parallelSpacingMin, p.parallelSpacingMax + 1)
            : p.parallelSpacing;
    }

    /// <summary>Enumerates straight segments from the road grid. Each segment is (origin, dir, length).</summary>
    private List<(Vector2Int origin, Vector2Int dir, int length)> GetStraightSegmentsFromGrid(HashSet<Vector2Int> roadSet, List<Vector2Int> edges)
    {
        var segments = new List<(Vector2Int origin, Vector2Int dir, int length)>();
        var seen = new HashSet<(int ox, int oy, int dx, int dy)>();
        int w = gridManager.width, h = gridManager.height;

        foreach (Vector2Int edge in edges)
        {
            Vector2Int roadDir = GetRoadDirectionAtEdge(edge, roadSet);
            if (roadDir.x == 0 && roadDir.y == 0) continue;

            Vector2Int pos = edge;
            while (true)
            {
                int px = pos.x - roadDir.x, py = pos.y - roadDir.y;
                if (px < 0 || px >= w || py < 0 || py >= h) break;
                Vector2Int prev = new Vector2Int(px, py);
                if (!roadSet.Contains(prev)) break;
                pos = prev;
            }
            Vector2Int origin = pos;

            int length = 0;
            pos = origin;
            while (pos.x >= 0 && pos.x < w && pos.y >= 0 && pos.y < h && roadSet.Contains(pos))
            {
                length++;
                pos = new Vector2Int(pos.x + roadDir.x, pos.y + roadDir.y);
            }

            if (length < 2) continue;

            Vector2Int end = new Vector2Int(origin.x + (length - 1) * roadDir.x, origin.y + (length - 1) * roadDir.y);
            Vector2Int canonOrigin = (origin.x < end.x || (origin.x == end.x && origin.y <= end.y)) ? origin : end;
            Vector2Int canonDir = (origin.x < end.x || (origin.x == end.x && origin.y <= end.y)) ? roadDir : new Vector2Int(-roadDir.x, -roadDir.y);
            var key = (canonOrigin.x, canonOrigin.y, canonDir.x, canonDir.y);
            if (seen.Contains(key)) continue;
            seen.Add(key);

            segments.Add((canonOrigin, canonDir, length));
        }
        return segments;
    }

    /// <summary>True if both perpendicular strips (left and right) are fully occupied for k in 0..length-2.</summary>
    private bool IsSegmentFullyBlocked(Vector2Int origin, Vector2Int dir, int length, HashSet<Vector2Int> roadSet)
    {
        Vector2Int perp = new Vector2Int(-dir.y, dir.x);
        int w = gridManager.width, h = gridManager.height;

        for (int k = 0; k <= length - 2; k++)
        {
            for (int j = 1; j <= 4; j++)
            {
                Vector2Int left = new Vector2Int(origin.x + k * dir.x + j * perp.x, origin.y + k * dir.y + j * perp.y);
                Vector2Int right = new Vector2Int(origin.x + k * dir.x - j * perp.x, origin.y + k * dir.y - j * perp.y);

                foreach (Vector2Int cell in new[] { left, right })
                {
                    if (cell.x < 0 || cell.x >= w || cell.y < 0 || cell.y >= h) continue;
                    if (roadSet.Contains(cell)) continue;

                    Cell c = gridManager.GetCell(cell.x, cell.y);
                    if (c == null) return false;
                    if (c.GetCellInstanceHeight() == 0) continue;
                    if (c.zoneType == Zone.ZoneType.Grass || c.HasForest()) return false;
                }
            }
        }
        return true;
    }

    private int CountUnzonedCellsNearPath(Vector2Int start, Vector2Int dir, int sampleLen, int radius)
    {
        int count = 0;
        int w = gridManager.width, h = gridManager.height;
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
                    Cell c = gridManager.GetCell(nx, ny);
                    if (c != null && (c.zoneType == Zone.ZoneType.Grass || c.HasForest()))
                        count++;
                }
            }
        }
        return count;
    }

    private bool IsDirectionEnclosed(Vector2Int edge, Vector2Int dir, int parallelSpacing, HashSet<Vector2Int> roadSet)
    {
        Vector2Int perp = new Vector2Int(-dir.y, dir.x);
        bool hasRoadLeft = false, hasRoadRight = false;
        for (int s = 1; s <= Mathf.Min(parallelSpacing, 5); s++)
        {
            int x = edge.x + dir.x * s, y = edge.y + dir.y * s;
            if (x < 0 || x >= gridManager.width || y < 0 || y >= gridManager.height) break;
            Vector2Int left = new Vector2Int(edge.x + perp.x * s, edge.y + perp.y * s);
            Vector2Int right = new Vector2Int(edge.x - perp.x * s, edge.y - perp.y * s);
            if (left.x >= 0 && left.x < gridManager.width && left.y >= 0 && left.y < gridManager.height && roadSet.Contains(left))
                hasRoadLeft = true;
            if (right.x >= 0 && right.x < gridManager.width && right.y >= 0 && right.y < gridManager.height && roadSet.Contains(right))
                hasRoadRight = true;
        }
        return hasRoadLeft && hasRoadRight;
    }

    private float CalculateDirectionUtility(Vector2Int edge, Vector2Int dir, int sampleLen, int parallelSpacing, HashSet<Vector2Int> roadSet)
    {
        float desir = GetAverageDesirabilityInDirection(edge, dir, sampleLen);
        int unzoned = CountUnzonedCellsNearPath(edge, dir, sampleLen, 2);
        bool enclosed = IsDirectionEnclosed(edge, dir, parallelSpacing, roadSet);
        return desir * 2f + unzoned * 1f - (enclosed ? 50f : 0f);
    }

    /// <summary>
    /// True if a road can be placed at (x,y): cell exists, not building/road/interstate, (Grass or has forest or water for bridge), and RoadManager allows placement.
    /// </summary>
    private bool IsCellPlaceableForRoad(int x, int y)
    {
        Cell c = gridManager.GetCell(x, y);
        if (c == null || c.zoneType == Zone.ZoneType.Road) return false;
        if (gridManager.IsCellOccupiedByBuilding(x, y) || c.isInterstate) return false;
        bool isLandPlaceable = AutoSimulationRoadRules.IsAutoRoadLandCell(gridManager, x, y);
        bool isWaterForBridge = c.GetCellInstanceHeight() == 0;
        if (!isLandPlaceable && !isWaterForBridge) return false;
        if (terrainManager == null) return false;
        return terrainManager.CanPlaceRoad(x, y, allowWaterSlopeForWaterBridgeTrace: true);
    }

    /// <summary>Returns a short reason why the cell is not placeable for road (for debug logs).</summary>
    private string GetCellPlaceableRejectReason(int x, int y)
    {
        Cell c = gridManager.GetCell(x, y);
        if (c == null) return "null cell";
        if (c.zoneType == Zone.ZoneType.Road) return "already road";
        if (gridManager.IsCellOccupiedByBuilding(x, y)) return "building";
        if (c.isInterstate) return "interstate";
        bool isLand = AutoSimulationRoadRules.IsAutoRoadLandCell(gridManager, x, y);
        bool isWater = c.GetCellInstanceHeight() == 0;
        if (!isLand && !isWater) return "zone not grass/light-zoning/water";
        if (terrainManager != null && !terrainManager.CanPlaceRoad(x, y, allowWaterSlopeForWaterBridgeTrace: true)) return "terrain/slope";
        return "unknown";
    }

    /// <summary>
    /// Number of cardinal neighbors of this road cell that are Grass (available for extension or zoning).
    /// </summary>
    private int CountGrassNeighbors(Vector2Int roadPos)
    {
        int count = 0;
        for (int i = 0; i < 4; i++)
        {
            int nx = roadPos.x + Dx[i], ny = roadPos.y + Dy[i];
            if (nx < 0 || nx >= gridManager.width || ny < 0 || ny >= gridManager.height) continue;
            Cell c = gridManager.GetCell(nx, ny);
            if (c != null && (c.zoneType == Zone.ZoneType.Grass || c.HasForest())) count++;
        }
        return count;
    }

    /// <summary>True if this edge (road cell) is on the interstate. Used to relax minLength for interstate connections.</summary>
    private bool IsEdgeOnInterstate(Vector2Int edge)
    {
        Cell c = gridManager.GetCell(edge.x, edge.y);
        return c != null && c.isInterstate;
    }

    /// <summary>True if the cell at tip + len*dir (the first unbuildable cell) is slope or water. Used for slope-connection mode.</summary>
    private bool IsDirectionBlockedBySlopeOrWater(Vector2Int tip, Vector2Int dir, int len)
    {
        int bx = tip.x + len * dir.x, by = tip.y + len * dir.y;
        if (bx < 0 || bx >= gridManager.width || by < 0 || by >= gridManager.height) return false;
        Cell c = gridManager.GetCell(bx, by);
        if (c == null) return false;
        if (c.GetCellInstanceHeight() == 0) return true;
        if (terrainManager == null) return false;
        TerrainSlopeType slope = terrainManager.GetTerrainSlopeTypeAt(bx, by);
        return slope != TerrainSlopeType.Flat;
    }

    /// <summary>
    /// True if (x,y) is valid for a road in direction streetDir: flat, cardinal, diagonal and corner slopes allowed; terraforming handles diagonal/corner. Water (height 0) is always suitable for bridge. Shore (water-slope) cells use the same bridge-trace gate as pathfinding (FEAT-44).
    /// </summary>
    private bool IsSuitableForRoad(int x, int y, Vector2Int streetDir)
    {
        Cell c = gridManager.GetCell(x, y);
        if (c != null && c.GetCellInstanceHeight() == 0)
            return true;
        if (terrainManager != null && terrainManager.IsWaterSlopeCell(x, y))
            return terrainManager.CanPlaceRoad(x, y, allowWaterSlopeForWaterBridgeTrace: true);
        if (terrainManager == null) return true;
        TerrainSlopeType slope = terrainManager.GetTerrainSlopeTypeAt(x, y);
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

    /// <summary>
    /// True if there is already a road parallel to dir within minSpacing tiles of edge (would create too-dense grid).
    /// When branching perpendicular from a parent street, pass excludeAlongDir = parent direction so we don't count the parent segment as "parallel".
    /// </summary>
    private bool HasParallelRoadTooClose(Vector2Int edge, Vector2Int dir, int minSpacing, HashSet<Vector2Int> roadSet, Vector2Int? excludeAlongDir = null)
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
            if (offset.x < 0 || offset.x >= gridManager.width || offset.y < 0 || offset.y >= gridManager.height)
                continue;
            if (excludeSet != null && excludeSet.Contains(offset)) continue;
            if (roadSet.Contains(offset))
                return true;
            Vector2Int otherSide = new Vector2Int(edge.x - perp.x * s, edge.y - perp.y * s);
            if (otherSide.x >= 0 && otherSide.x < gridManager.width && otherSide.y >= 0 && otherSide.y < gridManager.height)
            {
                if (excludeSet != null && excludeSet.Contains(otherSide)) continue;
                if (roadSet.Contains(otherSide))
                    return true;
            }
        }
        return false;
    }
    #endregion

    #region Utility Methods
    /// <summary>Finds path from road network to interstate and places road tiles using the path pipeline. Never expropriates; if path is blocked by buildings, returns 0.</summary>
    private int TryConnectToInterstate(int maxTiles, List<Vector2Int> roadPositions)
    {
        if (interstateManager == null || gridManager == null || roadManager == null || terraformingService == null || terrainManager == null) return 0;
        if (roadPositions.Count == 0) return 0;
        var interstatePositions = interstateManager.InterstatePositions;
        if (interstatePositions == null || interstatePositions.Count == 0) return 0;

        Vector2Int from = roadPositions[Random.Range(0, roadPositions.Count)];
        Vector2Int to = interstatePositions[Random.Range(0, interstatePositions.Count)];
        var path = minRoadSpacingWhenConnecting > 0
            ? gridManager.FindPathWithRoadSpacingForAutoSimulation(from, to, minRoadSpacingWhenConnecting)
            : gridManager.FindPathForAutoSimulation(from, to);
        if (path == null || path.Count <= 1)
        {
            if (minRoadSpacingWhenConnecting > 0)
                path = gridManager.FindPathForAutoSimulation(from, to);
            if (path == null || path.Count <= 1) return 0;
        }
        var roadSet = new HashSet<Vector2Int>(gridManager.GetAllRoadPositions());
        int minParallel = Mathf.Max(3, minRoadSpacingWhenConnecting);

        var pathVec2 = new List<Vector2>();
        for (int i = 0; i < path.Count; i++)
            pathVec2.Add(new Vector2(path[i].x, path[i].y));

        List<Vector2> expandedPath = null;
        PathTerraformPlan plan = null;
        while (path.Count > 1)
        {
            if (TryGetStreetPlacementPlan(pathVec2, null, out expandedPath, out plan) && plan != null && plan.isValid)
                break;
            path.RemoveAt(path.Count - 1);
            pathVec2.RemoveAt(pathVec2.Count - 1);
        }
        if (path.Count <= 1 || expandedPath == null || plan == null || !plan.isValid) return 0;

        var heightMap = terrainManager.GetHeightMap();
        if (heightMap == null || !plan.Apply(heightMap, terrainManager))
            return 0;

        var resolved = roadManager.ResolvePathForRoads(expandedPath, plan);
        var resolvedByPos = new Dictionary<Vector2Int, RoadPrefabResolver.ResolvedRoadTile>();
        for (int j = 0; j < resolved.Count; j++)
            resolvedByPos[resolved[j].gridPos] = resolved[j];

        var expandedPathInt = new List<Vector2Int>();
        for (int k = 0; k < expandedPath.Count; k++)
            expandedPathInt.Add(new Vector2Int((int)expandedPath[k].x, (int)expandedPath[k].y));

        int placed = 0;
        for (int i = 1; i < expandedPathInt.Count && placed < maxTiles; i++)
        {
            Vector2Int p = expandedPathInt[i];
            if (!resolvedByPos.TryGetValue(p, out var resolvedTile)) continue;
            if (gridManager.GetCell(p.x, p.y)?.zoneType == Zone.ZoneType.Road)
            {
                roadSet.Add(p);
                continue;
            }
            Vector2Int dir = new Vector2Int(p.x - expandedPathInt[i - 1].x, p.y - expandedPathInt[i - 1].y);
            if ((dir.x != 0 || dir.y != 0) && HasParallelRoadTooClose(p, dir, minParallel, roadSet))
                continue;
            if (growthBudgetManager.TrySpend(GrowthCategory.Roads, RoadManager.RoadCostPerTile) && PlaceRoadTileInBatch(resolvedTile))
            {
                placed++;
                roadSet.Add(p);
            }
        }
        return placed;
    }

    private List<List<Vector2Int>> GetRoadClusters(List<Vector2Int> all)
    {
        var roadSet = new HashSet<Vector2Int>(all);
        var visited = new HashSet<Vector2Int>();
        var clusters = new List<List<Vector2Int>>();
        foreach (Vector2Int p in all)
        {
            if (visited.Contains(p)) continue;
            var cluster = new List<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(p);
            while (queue.Count > 0)
            {
                Vector2Int c = queue.Dequeue();
                if (visited.Contains(c)) continue;
                visited.Add(c);
                cluster.Add(c);
                for (int i = 0; i < 4; i++)
                {
                    int nx = c.x + Dx[i], ny = c.y + Dy[i];
                    var n = new Vector2Int(nx, ny);
                    if (roadSet.Contains(n) && !visited.Contains(n))
                        queue.Enqueue(n);
                }
            }
            if (cluster.Count > 0)
                clusters.Add(cluster);
        }
        return clusters;
    }

    /// <summary>Connects two road clusters via FindPath using the path pipeline. Never expropriates; if path is blocked by buildings, returns 0.</summary>
    private int TryConnectDisconnected(List<List<Vector2Int>> clusters, int maxTiles)
    {
        if (clusters.Count < 2) return 0;
        if (terraformingService == null || terrainManager == null || roadManager == null) return 0;
        Vector2Int a = clusters[0][Random.Range(0, clusters[0].Count)];
        Vector2Int b = clusters[1][Random.Range(0, clusters[1].Count)];
        var path = minRoadSpacingWhenConnecting > 0
            ? gridManager.FindPathWithRoadSpacingForAutoSimulation(a, b, minRoadSpacingWhenConnecting)
            : gridManager.FindPathForAutoSimulation(a, b);
        if (path == null || path.Count <= 1)
        {
            if (minRoadSpacingWhenConnecting > 0)
                path = gridManager.FindPathForAutoSimulation(a, b);
            if (path == null || path.Count <= 1) return 0;
        }
        var roadSet = new HashSet<Vector2Int>(gridManager.GetAllRoadPositions());
        int minParallel = Mathf.Max(3, minRoadSpacingWhenConnecting);

        var pathVec2 = new List<Vector2>();
        for (int i = 0; i < path.Count; i++)
            pathVec2.Add(new Vector2(path[i].x, path[i].y));

        List<Vector2> expandedPath = null;
        PathTerraformPlan plan = null;
        while (path.Count > 1)
        {
            if (TryGetStreetPlacementPlan(pathVec2, null, out expandedPath, out plan) && plan != null && plan.isValid)
                break;
            path.RemoveAt(path.Count - 1);
            pathVec2.RemoveAt(pathVec2.Count - 1);
        }
        if (path.Count <= 1 || expandedPath == null || plan == null || !plan.isValid) return 0;

        var heightMap = terrainManager.GetHeightMap();
        if (heightMap == null || !plan.Apply(heightMap, terrainManager))
            return 0;

        var resolved = roadManager.ResolvePathForRoads(expandedPath, plan);
        var resolvedByPos = new Dictionary<Vector2Int, RoadPrefabResolver.ResolvedRoadTile>();
        for (int j = 0; j < resolved.Count; j++)
            resolvedByPos[resolved[j].gridPos] = resolved[j];

        var expandedPathInt = new List<Vector2Int>();
        for (int k = 0; k < expandedPath.Count; k++)
            expandedPathInt.Add(new Vector2Int((int)expandedPath[k].x, (int)expandedPath[k].y));

        int placed = 0;
        for (int i = 1; i < expandedPathInt.Count && placed < maxTiles; i++)
        {
            Vector2Int p = expandedPathInt[i];
            if (!resolvedByPos.TryGetValue(p, out var resolvedTile)) continue;
            if (gridManager.GetCell(p.x, p.y)?.zoneType == Zone.ZoneType.Road)
            {
                roadSet.Add(p);
                continue;
            }
            Vector2Int dir = new Vector2Int(p.x - expandedPathInt[i - 1].x, p.y - expandedPathInt[i - 1].y);
            if ((dir.x != 0 || dir.y != 0) && HasParallelRoadTooClose(p, dir, minParallel, roadSet))
                continue;
            if (growthBudgetManager.TrySpend(GrowthCategory.Roads, RoadManager.RoadCostPerTile) && PlaceRoadTileInBatch(resolvedTile))
            {
                placed++;
                roadSet.Add(p);
            }
        }
        return placed;
    }
    #endregion
}
}
