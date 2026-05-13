// registry-resolve-exempt: internal factory — constructs own sub-services (AutoBuildSimRulesService, AutoBuildCandidateScoringService) within Roads domain
using UnityEngine;
using System;
using System.Collections.Generic;
using Territory.Core;
using Territory.Roads;
using Territory.Economy;
using Territory.Simulation;
using Territory.Terrain;
using Territory.Zones;
using Territory.Utilities;
using Random = UnityEngine.Random;

namespace Domains.Roads.Services
{
/// <summary>
/// Orchestrator: delegates sim-rules to AutoBuildSimRulesService, candidate scoring to
/// AutoBuildCandidateScoringService. Facade (AutoRoadBuilder MonoBehaviour) composes this
/// in Start(). Invariants #2 (InvalidateRoadCache) + #10 (PathTerraformPlan) preserved.
/// No MonoBehaviour, no SerializeField.
/// </summary>
public class AutoBuildService
{
    #region Dependencies
    private IGridManager _gridManager;
    private IRoadManager _roadManager;
    private IGrowthBudgetManager _growthBudgetManager;
    private ICityStatsAuto _cityStats;
    private IInterstate _interstateManager;
    private ITerrainManager _terrainManager;
    private ITerraformingService _terraformingService;
    private object _autoZoningManager;
    private IUrbanCentroidService _urbanCentroidService;
    private AutoBuildSimRulesService _simRules;
    private AutoBuildCandidateScoringService _candidateScoring;
    #endregion

    #region Config params (bound from facade at Start)
    public int maxTilesPerTick;
    public int minStreetLength;
    public int minStreetLengthRecovery;
    public int maxStreetLength;
    public int maxActiveProjects;
    public int minParallelSpacingFromEdge;
    public int minEdgeSpacing;
    public int minRoadSpacingWhenConnecting;
    public int coreInnerExtraProjects;
    public int coreInnerMinEdgeSpacing;
    private const int MaxPerTickSafetyCap = 300;
    #endregion

    #region Callbacks
    public Action<Vector2Int, Vector2Int, int, UrbanRing> OnSegmentCompleted;
    public Action OnTickStart;
    #endregion

    private readonly List<Vector2Int> _batchPlacedFromResolvedRoadCells = new List<Vector2Int>();
    private readonly HashSet<Vector2Int> _expropriatedCellsPendingRoad;

    private struct StreetProject { public Vector2Int tip; public Vector2Int dir; public int targetLength; }

    public AutoBuildService(
        IGridManager gridManager, IRoadManager roadManager,
        IGrowthBudgetManager growthBudgetManager, ICityStatsAuto cityStats,
        IInterstate interstateManager, ITerrainManager terrainManager,
        ITerraformingService terraformingService, object autoZoningManager,
        IUrbanCentroidService urbanCentroidService,
        HashSet<Vector2Int> expropriatedCellsPendingRoad)
    {
        _gridManager = gridManager; _roadManager = roadManager;
        _growthBudgetManager = growthBudgetManager; _cityStats = cityStats;
        _interstateManager = interstateManager; _terrainManager = terrainManager;
        _terraformingService = terraformingService; _autoZoningManager = autoZoningManager;
        _urbanCentroidService = urbanCentroidService;
        _expropriatedCellsPendingRoad = expropriatedCellsPendingRoad;
        _simRules = new AutoBuildSimRulesService(gridManager, terrainManager, urbanCentroidService);
        _candidateScoring = new AutoBuildCandidateScoringService(gridManager, urbanCentroidService, _simRules);
    }

    public void RefreshDependencies(
        IGridManager gridManager, IRoadManager roadManager,
        IGrowthBudgetManager growthBudgetManager, ICityStatsAuto cityStats,
        IInterstate interstateManager, ITerrainManager terrainManager,
        ITerraformingService terraformingService, object autoZoningManager,
        IUrbanCentroidService urbanCentroidService)
    {
        _gridManager = gridManager; _roadManager = roadManager;
        _growthBudgetManager = growthBudgetManager; _cityStats = cityStats;
        _interstateManager = interstateManager; _terrainManager = terrainManager;
        _terraformingService = terraformingService; _autoZoningManager = autoZoningManager;
        _urbanCentroidService = urbanCentroidService;
        _simRules.RefreshDependencies(gridManager, terrainManager, urbanCentroidService);
        _candidateScoring.RefreshDependencies(gridManager, urbanCentroidService, _simRules);
    }

    private bool PlaceRoadTileInBatch(ResolvedRoadTile resolved)
    {
        _roadManager.PlaceRoadTileFromResolved(resolved);
        _batchPlacedFromResolvedRoadCells.Add(resolved.gridPos);
        return true;
    }

    private bool PlaceRoadTileInBatch(Vector2 pos) => _roadManager.PlaceRoadTileAt(pos);

    public void FlushBatchRoadPrefabRefresh()
    {
        if (_batchPlacedFromResolvedRoadCells.Count == 0 || _roadManager == null) return;
        _roadManager.RefreshRoadPrefabsAfterBatchPlacement(_batchPlacedFromResolvedRoadCells);
        _batchPlacedFromResolvedRoadCells.Clear();
    }

    public void ProcessTick()
    {
        if (_growthBudgetManager == null || _roadManager == null || _gridManager == null || _cityStats == null) return;
        if (!_cityStats.simulateGrowth) return;
        if (_cityStats.cityPowerOutput > 0 && !_cityStats.GetCityPowerAvailability()) return;

        OnTickStart?.Invoke();
        _batchPlacedFromResolvedRoadCells.Clear();

        var edges = _gridManager.GetRoadEdgePositions();
        var allRoads = _gridManager.GetAllRoadPositions();
        var roadSet = new HashSet<Vector2Int>(allRoads);
        int edgeCount = edges.Count;

        int available = _growthBudgetManager.GetAvailableBudget(GrowthCategory.Roads);
        int costPerTile = RoadConstants.RoadCostPerTile;
        int toPlace = Mathf.Min(MaxPerTickSafetyCap, costPerTile > 0 ? available / costPerTile : 0);

        int placed = 0;
        int effectiveMinStreetLength = edgeCount < 4 ? minStreetLengthRecovery : minStreetLength;

        if (_interstateManager != null && !_interstateManager.IsConnectedToInterstate)
        {
            placed = TryConnectToInterstate(toPlace, allRoads);
            if (placed > 0) { FlushBatchRoadPrefabRefresh(); _gridManager.InvalidateRoadCache(); return; }
        }

        List<List<Vector2Int>> clusters = _candidateScoring.GetRoadClusters(allRoads);
        if (clusters.Count > 1)
        {
            placed = TryConnectDisconnected(clusters, toPlace);
            if (placed > 0) { FlushBatchRoadPrefabRefresh(); _gridManager.InvalidateRoadCache(); return; }
        }

        int innerEdgeCount = _simRules.CountInnerEdges(edges);
        int effectiveMaxProjects = maxActiveProjects + (innerEdgeCount >= 2 ? coreInnerExtraProjects : 0);

        int newProjectsStarted = 0;
        while (toPlace > 0 && newProjectsStarted < effectiveMaxProjects)
        {
            if (!TryStartNewStreetProject(ref toPlace, effectiveMinStreetLength, edges, roadSet)) break;
            newProjectsStarted++;
        }
        if (newProjectsStarted == 0 && toPlace > 0 && effectiveMinStreetLength > minStreetLengthRecovery)
        {
            if (TryStartNewStreetProject(ref toPlace, minStreetLengthRecovery, edges, roadSet))
                newProjectsStarted++;
        }

        if (newProjectsStarted > 0)
        {
            _gridManager.InvalidateRoadCache();
            edges = _gridManager.GetRoadEdgePositions();
            roadSet = new HashSet<Vector2Int>(_gridManager.GetAllRoadPositions());
        }

        if (newProjectsStarted == 0 && toPlace > 0)
        {
            if (TryExpropriateForLongBlockedSegment(edges, roadSet))
            {
                _gridManager.InvalidateRoadCache();
                int placedExp = PlaceRoadsInExpropriatedCells(ref toPlace);
                if (placedExp > 0) placed += placedExp;
                edges = _gridManager.GetRoadEdgePositions();
                roadSet = new HashSet<Vector2Int>(_gridManager.GetAllRoadPositions());
                if (TryStartNewStreetProject(ref toPlace, effectiveMinStreetLength, edges, roadSet))
                    newProjectsStarted++;
            }
        }

        if (toPlace > 0 && _expropriatedCellsPendingRoad.Count > 0)
        {
            int placedExp = PlaceRoadsInExpropriatedCells(ref toPlace);
            if (placedExp > 0) placed += placedExp;
        }

        FlushBatchRoadPrefabRefresh();
        if (placed > 0 || newProjectsStarted > 0) _gridManager.InvalidateRoadCache();
    }

    bool TryGetStreetPlacementPlan(List<Vector2> pathVec2, Vector2Int? straightBuildDirection, out List<Vector2> expandedPath, out PathTerraformPlan plan)
    {
        if (_roadManager != null)
        {
            int hint = 0;
            var ctx = new RoadPathValidationContext { forbidCutThrough = false };
            bool prefixOk = _roadManager.TryPrepareRoadPlacementPlanLongestValidPrefix(pathVec2, ctx, false, ref hint, out expandedPath, out plan, out _);
            bool progOk = false;
            List<Vector2> progExpanded = null;
            PathTerraformPlan progPlan = null;
            if (straightBuildDirection.HasValue)
                progOk = _roadManager.TryPrepareRoadPlacementPlanWithProgrammaticDeckSpanChord(pathVec2, straightBuildDirection.Value, ctx, out progExpanded, out progPlan);

            if (!prefixOk && progOk) { expandedPath = progExpanded; plan = progPlan; return true; }
            if (!prefixOk && !progOk) { expandedPath = null; plan = null; return false; }
            if (prefixOk && !progOk) return true;

            bool strokeWet = _roadManager.StrokeHasWaterOrWaterSlopeCells(pathVec2);
            bool preferProg = strokeWet || (progExpanded != null && expandedPath != null && progExpanded.Count > expandedPath.Count);
            if (preferProg) { expandedPath = progExpanded; plan = progPlan; }
            return true;
        }
        if (pathVec2 == null || pathVec2.Count == 0)
        {
            expandedPath = pathVec2;
            plan = new PathTerraformPlan { isValid = false };
            return false;
        }
        expandedPath = pathVec2.Count >= 2 ? Domains.Terrain.Services.TerraformingService.ExpandDiagonalStepsToCardinal(pathVec2) : pathVec2;
        plan = _terraformingService != null ? _terraformingService.ComputePathPlan(expandedPath) : new PathTerraformPlan { isValid = false };
        return plan.isValid;
    }

    private int BuildFullSegmentInOneTick(StreetProject p, ref int budgetRemaining, HashSet<Vector2Int> roadSet)
    {
        Vector2Int origin = p.tip;
        Vector2Int dir = p.dir;
        int w = _gridManager.width, h = _gridManager.height;
        var path = new List<Vector2Int>();
        int x = origin.x, y = origin.y;
        int maxLen = Mathf.Min(p.targetLength, budgetRemaining);

        for (int i = 0; i < maxLen; i++)
        {
            if (x < 0 || x >= w || y < 0 || y >= h) break;
            CityCell c = _gridManager.GetCell(x, y);
            if (c != null && c.zoneType == Zone.ZoneType.Road) break;
            if (!_simRules.IsCellPlaceableForRoad(x, y)) break;
            if (!_simRules.IsSuitableForRoad(x, y, dir)) break;
            path.Add(new Vector2Int(x, y));
            x += dir.x; y += dir.y;
        }
        if (path.Count == 0) return 0;

        var pathVec2 = new List<Vector2>(path.Count);
        for (int i = 0; i < path.Count; i++) pathVec2.Add(new Vector2(path[i].x, path[i].y));

        if (_roadManager != null && _roadManager.TryExtendCardinalStreetPathWithBridgeChord(pathVec2, dir))
            for (int i = path.Count; i < pathVec2.Count; i++)
                path.Add(new Vector2Int((int)pathVec2[i].x, (int)pathVec2[i].y));

        const int maxShortenAttempts = 3;
        int shortenCount = 0;
        List<Vector2> expandedPath = null;
        PathTerraformPlan plan = null;
        while (path.Count >= 1 && shortenCount < maxShortenAttempts)
        {
            if (TryGetStreetPlacementPlan(pathVec2, dir, out expandedPath, out plan) && plan != null && plan.isValid) break;
            if (path.Count <= 1) break;
            path.RemoveAt(path.Count - 1); pathVec2.RemoveAt(pathVec2.Count - 1); shortenCount++;
        }

        if (!(plan != null && plan.isValid) && path.Count >= 2)
        {
            Vector2Int edge = new Vector2Int(origin.x - dir.x, origin.y - dir.y);
            var aStarPath = _gridManager.FindPathForAutoSimulation(edge, path[path.Count - 1]);
            if (aStarPath != null && aStarPath.Count >= 2)
            {
                path.Clear(); pathVec2.Clear();
                for (int i = 0; i < aStarPath.Count; i++)
                {
                    if (aStarPath[i] == edge) continue;
                    path.Add(aStarPath[i]); pathVec2.Add(new Vector2(aStarPath[i].x, aStarPath[i].y));
                }
                if (path.Count > 0 && (!TryGetStreetPlacementPlan(pathVec2, dir, out expandedPath, out plan) || plan == null || !plan.isValid))
                { path.Clear(); pathVec2.Clear(); }
            }
        }

        if (path.Count == 0 || plan == null || !plan.isValid || expandedPath == null) return 0;

        var heightMap = _terrainManager != null ? _terrainManager.GetHeightMap() : null;
        bool waterCrossing = _roadManager != null && _roadManager.StrokeHasWaterOrWaterSlopeCells(expandedPath);
        bool atomicWaterBridge = waterCrossing;
        if (atomicWaterBridge && (_roadManager == null || !_roadManager.StrokeLastCellIsFirmDryLand(expandedPath))) return 0;

        List<ResolvedRoadTile> resolvedPreApply = null;
        if (atomicWaterBridge && _roadManager != null && !plan.HasTerraformHeightMutation())
            resolvedPreApply = _roadManager.ResolvePathForRoads(expandedPath, plan);

        int CountNonRoad(List<ResolvedRoadTile> r)
        {
            if (r == null || _gridManager == null) return 0;
            int n = 0;
            for (int i = 0; i < r.Count; i++) { CityCell c = _gridManager.GetCell(r[i].gridPos.x, r[i].gridPos.y); if (c != null && c.zoneType != Zone.ZoneType.Road) n++; }
            return n;
        }
        int CountNonRoadExpanded()
        {
            int n = 0;
            for (int i = 0; i < expandedPath.Count; i++) { int ex = (int)expandedPath[i].x, ey = (int)expandedPath[i].y; CityCell c = _gridManager.GetCell(ex, ey); if (c != null && c.zoneType != Zone.ZoneType.Road) n++; }
            return n;
        }

        if (atomicWaterBridge && _growthBudgetManager != null)
        {
            int needPre = resolvedPreApply != null ? CountNonRoad(resolvedPreApply) : CountNonRoadExpanded();
            if (needPre > budgetRemaining) return 0;
        }

        if (heightMap != null && !plan.Apply(heightMap, _terrainManager)) return 0;

        var resolved = _roadManager != null ? _roadManager.ResolvePathForRoads(expandedPath, plan) : new List<ResolvedRoadTile>();
        if (resolved.Count == 0) return 0;

        int placed = 0;
        if (atomicWaterBridge)
        {
            int tilesToPlace = CountNonRoad(resolved);
            if (tilesToPlace > budgetRemaining) { if (heightMap != null) plan.Revert(heightMap, _terrainManager); return 0; }
            int lumpCost = tilesToPlace * RoadConstants.RoadCostPerTile;
            if (tilesToPlace > 0 && (_growthBudgetManager == null || !_growthBudgetManager.TrySpend(GrowthCategory.Roads, lumpCost)))
            { if (heightMap != null) plan.Revert(heightMap, _terrainManager); return 0; }
            if (tilesToPlace > 0) budgetRemaining -= tilesToPlace;
            for (int i = 0; i < resolved.Count; i++)
            {
                CityCell c = _gridManager.GetCell(resolved[i].gridPos.x, resolved[i].gridPos.y);
                if (c != null && c.zoneType == Zone.ZoneType.Road) continue;
                PlaceRoadTileInBatch(resolved[i]); placed++; roadSet.Add(resolved[i].gridPos);
            }
            if (placed == 0) return 0;
        }
        else
        {
            for (int i = 0; i < resolved.Count && budgetRemaining > 0; i++)
            {
                if (!_growthBudgetManager.TrySpend(GrowthCategory.Roads, RoadConstants.RoadCostPerTile)) break;
                if (PlaceRoadTileInBatch(resolved[i])) { placed++; budgetRemaining--; roadSet.Add(resolved[i].gridPos); }
            }
            if (placed == 0) return 0;
        }

        UrbanRing ring = _urbanCentroidService != null ? _urbanCentroidService.GetUrbanRing(new Vector2(origin.x, origin.y)) : UrbanRing.Mid;
        OnSegmentCompleted?.Invoke(origin, dir, placed, ring);
        return placed;
    }

    private bool TryStartNewStreetProject(ref int budgetRemaining, int effectiveMinStreetLength, List<Vector2Int> edges, HashSet<Vector2Int> roadSet)
    {
        if (edges.Count == 0) return false;
        var candidates = _candidateScoring.RankCandidateEdges(
            edges, roadSet, effectiveMinStreetLength, minParallelSpacingFromEdge, minEdgeSpacing, coreInnerMinEdgeSpacing);
        foreach (var (edge, @params, edgeRing, validDirections) in candidates)
        {
            foreach (var (dir, tip, len) in validDirections)
            {
                int targetLen = Mathf.Clamp(Random.Range(@params.minLength, @params.maxLength + 1), @params.minLength, len);
                var project = new StreetProject { tip = tip, dir = dir, targetLength = targetLen };
                int placed = BuildFullSegmentInOneTick(project, ref budgetRemaining, roadSet);
                if (placed > 0) return true;
            }
        }
        return false;
    }

    private bool TryExpropriateForLongBlockedSegment(List<Vector2Int> edges, HashSet<Vector2Int> roadSet)
    {
        var segments = _candidateScoring.GetStraightSegmentsFromGrid(roadSet, edges);
        int w = _gridManager.width, h = _gridManager.height;
        foreach (var (origin, dir, length) in segments)
        {
            Vector2 mid = new Vector2(origin.x + (length / 2) * dir.x, origin.y + (length / 2) * dir.y);
            UrbanRing ring = _urbanCentroidService != null ? _urbanCentroidService.GetUrbanRing(mid) : UrbanRing.Mid;
            RingStreetParams ringParams = _urbanCentroidService != null ? _urbanCentroidService.GetStreetParamsForRing(ring) : new RingStreetParams { minLength = 4, maxLength = 20 };
            if (length <= ringParams.maxLength) continue;
            if (!_candidateScoring.IsSegmentFullyBlocked(origin, dir, length, roadSet)) continue;

            Vector2Int end = new Vector2Int(origin.x + (length - 1) * dir.x, origin.y + (length - 1) * dir.y);
            int roadNeighborsOrigin = _gridManager.CountRoadNeighbors(origin.x, origin.y);
            int roadNeighborsEnd = _gridManager.CountRoadNeighbors(end.x, end.y);
            Vector2Int intersection, anchorDir;
            if (roadNeighborsEnd >= 2) { intersection = end; anchorDir = new Vector2Int(-dir.x, -dir.y); }
            else if (roadNeighborsOrigin >= 2) { intersection = origin; anchorDir = dir; }
            else continue;

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
                CityCell c = _gridManager.GetCell(cell.x, cell.y);
                if (c == null || c.GetCellInstanceHeight() == 0) continue;
                string bt = c.GetBuildingType();
                if (bt == "PowerPlant" || bt == "WaterPlant") continue;
                if (_gridManager.DemolishCellAt(new Vector2(cell.x, cell.y), withAnimation: false))
                { demolished.Add(cell); _expropriatedCellsPendingRoad.Add(cell); }
            }
            if (demolished.Count > 0) return true;
        }
        return false;
    }

    private int PlaceRoadsInExpropriatedCells(ref int budgetRemaining)
    {
        int placed = 0;
        var toPlace = new List<Vector2Int>(_expropriatedCellsPendingRoad);
        foreach (Vector2Int pos in toPlace)
        {
            if (budgetRemaining <= 0) break;
            CityCell c = _gridManager.GetCell(pos.x, pos.y);
            if (c == null || c.zoneType == Zone.ZoneType.Road) { _expropriatedCellsPendingRoad.Remove(pos); continue; }
            if (!_simRules.IsCellPlaceableForRoad(pos.x, pos.y)) continue;
            if (!_growthBudgetManager.TrySpend(GrowthCategory.Roads, RoadConstants.RoadCostPerTile)) continue;
            if (PlaceRoadTileInBatch(new Vector2(pos.x, pos.y))) { _expropriatedCellsPendingRoad.Remove(pos); placed++; budgetRemaining--; }
        }
        return placed;
    }

    /// <summary>Shared path-connection logic used by interstate + cluster connect.</summary>
    private int ExecutePathConnection(List<Vector2Int> path, int maxTiles, HashSet<Vector2Int> roadSet)
    {
        if (path == null || path.Count <= 1) return 0;
        var pathVec2 = new List<Vector2>(path.Count);
        for (int i = 0; i < path.Count; i++) pathVec2.Add(new Vector2(path[i].x, path[i].y));
        List<Vector2> expandedPath = null;
        PathTerraformPlan plan = null;
        while (path.Count > 1)
        {
            if (TryGetStreetPlacementPlan(pathVec2, null, out expandedPath, out plan) && plan != null && plan.isValid) break;
            path.RemoveAt(path.Count - 1); pathVec2.RemoveAt(pathVec2.Count - 1);
        }
        if (path.Count <= 1 || expandedPath == null || plan == null || !plan.isValid) return 0;
        var heightMap = _terrainManager.GetHeightMap();
        if (heightMap == null || !plan.Apply(heightMap, _terrainManager)) return 0;

        var resolved = _roadManager.ResolvePathForRoads(expandedPath, plan);
        var resolvedByPos = new Dictionary<Vector2Int, ResolvedRoadTile>(resolved.Count);
        for (int j = 0; j < resolved.Count; j++) resolvedByPos[resolved[j].gridPos] = resolved[j];
        var expandedPathInt = new List<Vector2Int>(expandedPath.Count);
        for (int k = 0; k < expandedPath.Count; k++) expandedPathInt.Add(new Vector2Int((int)expandedPath[k].x, (int)expandedPath[k].y));

        int minParallel = Mathf.Max(3, minRoadSpacingWhenConnecting);
        int placed = 0;
        for (int i = 1; i < expandedPathInt.Count && placed < maxTiles; i++)
        {
            Vector2Int pt = expandedPathInt[i];
            if (!resolvedByPos.TryGetValue(pt, out var resolvedTile)) continue;
            if (_gridManager.GetCell(pt.x, pt.y)?.zoneType == Zone.ZoneType.Road) { roadSet.Add(pt); continue; }
            Vector2Int dir = new Vector2Int(pt.x - expandedPathInt[i - 1].x, pt.y - expandedPathInt[i - 1].y);
            if ((dir.x != 0 || dir.y != 0) && _simRules.HasParallelRoadTooClose(pt, dir, minParallel, roadSet)) continue;
            if (_growthBudgetManager.TrySpend(GrowthCategory.Roads, RoadConstants.RoadCostPerTile) && PlaceRoadTileInBatch(resolvedTile))
            { placed++; roadSet.Add(pt); }
        }
        return placed;
    }

    private int TryConnectToInterstate(int maxTiles, List<Vector2Int> roadPositions)
    {
        if (_interstateManager == null || _gridManager == null || _roadManager == null || _terraformingService == null || _terrainManager == null) return 0;
        if (roadPositions.Count == 0) return 0;
        var interstatePositions = _interstateManager.InterstatePositions;
        if (interstatePositions == null || interstatePositions.Count == 0) return 0;
        Vector2Int from = roadPositions[Random.Range(0, roadPositions.Count)];
        Vector2Int to = interstatePositions[Random.Range(0, interstatePositions.Count)];
        var path = minRoadSpacingWhenConnecting > 0
            ? _gridManager.FindPathWithRoadSpacingForAutoSimulation(from, to, minRoadSpacingWhenConnecting)
            : _gridManager.FindPathForAutoSimulation(from, to);
        if ((path == null || path.Count <= 1) && minRoadSpacingWhenConnecting > 0)
            path = _gridManager.FindPathForAutoSimulation(from, to);
        var roadSet = new HashSet<Vector2Int>(_gridManager.GetAllRoadPositions());
        return ExecutePathConnection(path, maxTiles, roadSet);
    }

    private int TryConnectDisconnected(List<List<Vector2Int>> clusters, int maxTiles)
    {
        if (clusters.Count < 2) return 0;
        if (_terraformingService == null || _terrainManager == null || _roadManager == null) return 0;
        Vector2Int a = clusters[0][Random.Range(0, clusters[0].Count)];
        Vector2Int b = clusters[1][Random.Range(0, clusters[1].Count)];
        var path = minRoadSpacingWhenConnecting > 0
            ? _gridManager.FindPathWithRoadSpacingForAutoSimulation(a, b, minRoadSpacingWhenConnecting)
            : _gridManager.FindPathForAutoSimulation(a, b);
        if ((path == null || path.Count <= 1) && minRoadSpacingWhenConnecting > 0)
            path = _gridManager.FindPathForAutoSimulation(a, b);
        var roadSet = new HashSet<Vector2Int>(_gridManager.GetAllRoadPositions());
        return ExecutePathConnection(path, maxTiles, roadSet);
    }
}
}
