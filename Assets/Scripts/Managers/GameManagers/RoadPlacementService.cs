// long-file-allowed: legacy hub — scope outside current atomization plan; deferred to future sweep
using UnityEngine;
using System.Collections.Generic;
using Territory.Core;
using Territory.Terrain;
using Territory.Roads;
using Territory.Zones;
using Territory.Utilities;
using Territory.Economy;
using Territory.UI;
using Territory.Persistence;
using Territory.Audio;

namespace Domains.Roads.Services
{
/// <summary>
/// Pure POCO service absorbing placement, path-prep, bridge, and drawing logic from RoadManager (Stage 4.0 THIN).
/// No MonoBehaviour. Composed by RoadManager.Initialize(); RoadManager delegates all public calls here.
/// Invariant #2 (InvalidateRoadCache) preserved — only called from placement paths (TryCommitStreetStroke, PlaceInterstateFromPath).
/// Invariant #10 (PathTerraformPlan family) preserved — TryPrepareRoadPlacementPlan* all route through here.
/// </summary>
public class RoadPlacementService
{
    #region Injected dependencies
    private IGridManager _grid;
    private ITerrainManager _terrain;
    private ITerraformingService _terraforming;
    private IRoadManager _roadMgr;
    private RoadPrefabResolver _resolver;
    private TerrainManager TerrainMgr => _terrain as TerrainManager;
    private TerraformingService TerraformingSvc => _terraforming as TerraformingService;
    #endregion

    #region Drawing state (owned by RoadManager facade, mutated here via Draw methods)
    private bool _isDrawingRoad;
    private Vector2 _startPosition;
    private Vector2 _currentDrawCursorGrid;
    private int _manualRoadLongestPrefixHint;
    private bool _manualPreviewBridgeLocked;
    private Vector2Int _lockedBridgeLip;
    private Vector2Int _lockedBridgeNormal;
    private List<Vector2> _lockedBridgeChord;
    private readonly List<Vector2> _lipCandidateScratch = new List<Vector2>();

    public List<ResolvedRoadTile> PreviewResolvedTiles { get; } = new List<ResolvedRoadTile>();
    public List<GameObject> PreviewRoadTiles { get; } = new List<GameObject>();
    public List<Vector2> PreviewRoadGridPositions { get; } = new List<Vector2>();
    #endregion

    #region Placement path state
    private HashSet<Vector2> _placementPathPositions;
    private static readonly int[] DirX = { 1, -1, 0, 0 };
    private static readonly int[] DirY = { 0, 0, 1, -1 };
    #endregion

    // -----------------------------------------------------------------------
    // Construction
    // -----------------------------------------------------------------------
    public RoadPlacementService(IGridManager grid, ITerrainManager terrain, ITerraformingService terraforming, IRoadManager roadMgr)
    {
        _grid = grid;
        _terrain = terrain;
        _terraforming = terraforming;
        _roadMgr = roadMgr;
        EnsureResolver();
    }

    public void RefreshDependencies(IGridManager grid, ITerrainManager terrain, ITerraformingService terraforming, IRoadManager roadMgr)
    {
        _grid = grid;
        _terrain = terrain;
        _terraforming = terraforming;
        _roadMgr = roadMgr;
        _resolver = null;
        EnsureResolver();
    }

    // -----------------------------------------------------------------------
    // Drawing
    // -----------------------------------------------------------------------
    public void HandleRoadDrawing(Vector2 gridPosition, UIManager uiManager, CityStats cityStats = null)
    {
        Vector2 pos = new Vector2((int)gridPosition.x, (int)gridPosition.y);

        if (Input.GetMouseButtonUp(0) && _isDrawingRoad)
        {
            _isDrawingRoad = false;
            ClearPreview();
            if (!TryFinalizeManualRoadPlacement(cityStats, uiManager))
            {
                if (GameNotificationManager.Instance != null)
                    GameNotificationManager.Instance.PostWarning("Cannot place road along this path. Terrain or validation failed.");
            }
            ClearManualPreviewBridgeLock();
            ClearPreview();
            if (uiManager != null)
                uiManager.RestoreGhostPreview();
            return;
        }

        if (Input.GetMouseButtonUp(1))
        {
            var gridMgr = _grid as GridManager;
            if (gridMgr == null || gridMgr.cameraController == null || !gridMgr.cameraController.WasLastRightClickAPan)
            {
                _isDrawingRoad = false;
                ClearManualPreviewBridgeLock();
                ClearPreview();
                if (uiManager != null)
                    uiManager.RestoreGhostPreview();
            }
            return;
        }

        if (!_terrain.CanPlaceRoad((int)pos.x, (int)pos.y, allowWaterSlopeForWaterBridgeTrace: true))
        {
            if (_isDrawingRoad)
                ClearPreview();
            if (GameNotificationManager.Instance != null)
                GameNotificationManager.Instance.PostWarning("Cannot place road along this path.");
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            var intMgr = GetInterstateManager();
            if (intMgr != null && !intMgr.CanPlaceStreetFrom(pos))
            {
                if (GameNotificationManager.Instance != null)
                    GameNotificationManager.Instance.PostWarning("Streets must connect to the Interstate Highway or existing connected roads.");
                return;
            }
            _isDrawingRoad = true;
            ClearManualPreviewBridgeLock();
            _startPosition = pos;
            _currentDrawCursorGrid = pos;
            _manualRoadLongestPrefixHint = 0;
            if (uiManager != null)
                uiManager.HideGhostPreview();
        }
        else if (_isDrawingRoad && Input.GetMouseButton(0))
        {
            _currentDrawCursorGrid = pos;
            ClearPreview();
            List<Vector2> path = GetManualRoadPathWithOptionalBridgeExtension();
            DrawPreviewLineCore(path);
        }
    }

    public bool IsDrawingRoad() => _isDrawingRoad;
    public int GetPreviewRoadTileCount() => PreviewRoadGridPositions.Count;

    // -----------------------------------------------------------------------
    // Cost
    // -----------------------------------------------------------------------
    public const int CostPerTile = 50;
    public int GetRoadCostPerTile() => CostPerTile;
    public int GetRoadCostForTileCount(int count) => count * CostPerTile;

    // -----------------------------------------------------------------------
    // Path preparation — public (called by RoadManager delegates)
    // -----------------------------------------------------------------------
    public bool ValidateTerraformPlanWithContext(PathTerraformPlan plan, RoadPathValidationContext ctx)
    {
        if (plan == null) return false;
        if (!plan.isValid) return false;
        if (ctx.forbidCutThrough && plan.isCutThrough) return false;
        return true;
    }

    public bool TryPrepareRoadPlacementPlan(List<Vector2> pathRaw, RoadPathValidationContext ctx, bool postUserWarnings, out List<Vector2> expandedPath, out PathTerraformPlan plan)
    {
        if (!TryBuildFilteredPathForRoadPlan(pathRaw, postUserWarnings, out List<Vector2> filteredPath, relaxElbowNearWaterForWaterBridge: !ctx.forbidCutThrough))
        {
            expandedPath = null;
            plan = null;
            return false;
        }
        return TryPrepareFromFilteredPathList(filteredPath, ctx, postUserWarnings, out expandedPath, out plan);
    }

    public bool TryPrepareRoadPlacementPlanLongestValidPrefix(List<Vector2> pathRaw, RoadPathValidationContext ctx, bool postUserWarnings, ref int longestPrefixLengthHint, out List<Vector2> expandedPath, out PathTerraformPlan plan, out List<Vector2> filteredPathUsedOrNull)
    {
        expandedPath = null;
        plan = null;
        filteredPathUsedOrNull = null;
        if (pathRaw == null || pathRaw.Count == 0) return false;

        HeightMap heightMap = _terrain.GetHeightMap();
        if (heightMap == null) return false;

        int nRaw = pathRaw.Count;
        int startKRaw = Mathf.Min(nRaw, longestPrefixLengthHint > 0 ? longestPrefixLengthHint + 1 : nRaw);
        bool hasSlopeEligibleRawPrefix = _terrain != null
            && RoadStrokeTerrainRules.TruncatePathAtFirstDisallowedLandSlope(pathRaw, TerrainMgr).Count > 0;

        for (int kRaw = startKRaw; kRaw >= 1; kRaw--)
        {
            List<Vector2> rawPrefix = SubListCopy(pathRaw, kRaw);
            if (!TryBuildFilteredPathForRoadPlan(rawPrefix, postUserWarnings, out List<Vector2> filteredPrefix, relaxElbowNearWaterForWaterBridge: !ctx.forbidCutThrough))
                continue;
            if (!IsBridgePathValid(filteredPrefix, heightMap)) continue;
            if (!ValidateFeat44WaterBridgeRules(filteredPrefix, heightMap, postUserWarnings: false)) continue;
            if (HasTurnOnWaterOrCoast(filteredPrefix, heightMap)) continue;
            if (HasElbowTooCloseToWater(filteredPrefix, heightMap, relaxElbowNearWaterForWaterBridge: !ctx.forbidCutThrough)) continue;
            if (!TryPrepareFromFilteredPathList(filteredPrefix, ctx, false, out expandedPath, out plan)) continue;

            longestPrefixLengthHint = kRaw;
            filteredPathUsedOrNull = filteredPrefix;
            return true;
        }

        longestPrefixLengthHint = 0;
        if (postUserWarnings && hasSlopeEligibleRawPrefix && GameNotificationManager.Instance != null)
            GameNotificationManager.Instance.PostWarning("Road cannot extend further along this path. Terrain would exceed allowed height change.");
        return false;
    }

    public bool TryPrepareRoadPlacementPlanWithProgrammaticDeckSpanChord(List<Vector2> straightCardinalPath, Vector2Int segmentDir, RoadPathValidationContext ctx, out List<Vector2> expandedPath, out PathTerraformPlan plan)
    {
        expandedPath = null;
        plan = null;
        if (straightCardinalPath == null || straightCardinalPath.Count < 2 || _terraforming == null || _terrain == null || _grid == null) return false;
        if (segmentDir.x == 0 && segmentDir.y == 0) return false;
        if (Mathf.Abs(segmentDir.x) + Mathf.Abs(segmentDir.y) != 1) return false;

        HeightMap heightMap = _terrain.GetHeightMap();
        if (heightMap == null) return false;

        for (int i = 1; i < straightCardinalPath.Count; i++)
        {
            int px = (int)straightCardinalPath[i - 1].x, py = (int)straightCardinalPath[i - 1].y;
            int cx = (int)straightCardinalPath[i].x, cy = (int)straightCardinalPath[i].y;
            if (cx - px != segmentDir.x || cy - py != segmentDir.y) return false;
        }

        int wetIndex = -1;
        for (int i = 1; i < straightCardinalPath.Count; i++)
        {
            int cx2 = (int)straightCardinalPath[i].x, cy2 = (int)straightCardinalPath[i].y;
            if (IsWaterOrWaterSlope(cx2, cy2, heightMap)) { wetIndex = i; break; }
        }
        if (wetIndex < 1) return false;

        int lx = (int)straightCardinalPath[wetIndex - 1].x;
        int ly = (int)straightCardinalPath[wetIndex - 1].y;
        CityCell lipCell = _grid.GetCell(lx, ly);
        if (lipCell == null) return false;
        int lipH = lipCell.GetCellInstanceHeight();
        if (lipH <= 0) return false;

        WaterManager wm = ResolveWaterManager();
        if (!CellQualifiesForDeckDisplayLip(lx, ly, lipH, heightMap, wm)) return false;

        var normalsScratch = new List<Vector2Int>(4);
        CollectRelaxedLipBridgeNormals(lx, ly, lipH, heightMap, wm, normalsScratch);
        bool dirOk = false;
        for (int n = 0; n < normalsScratch.Count; n++)
        {
            if (normalsScratch[n].x == segmentDir.x && normalsScratch[n].y == segmentDir.y) { dirOk = true; break; }
        }
        if (!dirOk) return false;

        List<Vector2> chord = WalkStraightChordFromLipThroughWetToFarDry(lx, ly, segmentDir.x, segmentDir.y, lipH, heightMap);
        if (chord == null || chord.Count < 2) return false;

        var merged = new List<Vector2>(straightCardinalPath.Count + chord.Count);
        for (int i = 0; i < wetIndex; i++) merged.Add(straightCardinalPath[i]);
        AppendPathSkipDuplicateJoin(merged, chord);
        AppendStraightSuffixAlongProgrammaticChord(merged, straightCardinalPath, segmentDir);

        int minPrefixLen = wetIndex + chord.Count - 1;
        if (minPrefixLen < 2 || merged.Count < minPrefixLen) return false;

        for (int k = merged.Count; k >= minPrefixLen; k--)
        {
            List<Vector2> prefix = SubListCopy(merged, k);
            if (TryPrepareDeckSpanPlanFromAdjacentStroke(prefix, ctx, out expandedPath, out plan))
                return true;
        }
        return false;
    }

    public bool StrokeLastCellIsFirmDryLand(IList<Vector2> stroke)
    {
        if (stroke == null || stroke.Count == 0 || _terrain == null || _grid == null) return false;
        HeightMap heightMap = _terrain.GetHeightMap();
        if (heightMap == null) return false;
        int x = (int)stroke[stroke.Count - 1].x;
        int y = (int)stroke[stroke.Count - 1].y;
        if (!heightMap.IsValidPosition(x, y)) return false;
        if (IsWaterOrWaterSlope(x, y, heightMap)) return false;
        if (_terrain.IsRegisteredOpenWaterAt(x, y)) return false;
        CityCell c = _grid.GetCell(x, y);
        return c != null && c.GetCellInstanceHeight() > 0;
    }

    public bool StrokeHasWaterOrWaterSlopeCells(IList<Vector2> stroke)
    {
        if (stroke == null || stroke.Count == 0 || _terrain == null) return false;
        HeightMap heightMap = _terrain.GetHeightMap();
        if (heightMap == null) return false;
        for (int i = 0; i < stroke.Count; i++)
        {
            int x = (int)stroke[i].x, y = (int)stroke[i].y;
            if (IsWaterOrWaterSlope(x, y, heightMap)) return true;
        }
        return false;
    }

    public bool TryExtendCardinalStreetPathWithBridgeChord(List<Vector2> pathVec2, Vector2Int dir)
    {
        if (pathVec2 == null || pathVec2.Count < 1 || _terraforming == null || _terrain == null || _grid == null) return false;
        if (dir.x == 0 && dir.y == 0) return false;
        if (Mathf.Abs(dir.x) + Mathf.Abs(dir.y) != 1) return false;

        HeightMap heightMap = _terrain.GetHeightMap();
        if (heightMap == null) return false;

        for (int i = 1; i < pathVec2.Count; i++)
        {
            int px = (int)pathVec2[i - 1].x, py = (int)pathVec2[i - 1].y;
            int cx = (int)pathVec2[i].x, cy = (int)pathVec2[i].y;
            if (cx - px != dir.x || cy - py != dir.y) return false;
        }

        int lx = (int)pathVec2[pathVec2.Count - 1].x;
        int ly = (int)pathVec2[pathVec2.Count - 1].y;
        int nx = lx + dir.x;
        int ny = ly + dir.y;
        if (!heightMap.IsValidPosition(nx, ny) || !IsWaterOrWaterSlope(nx, ny, heightMap)) return false;

        CityCell lipCell = _grid.GetCell(lx, ly);
        if (lipCell == null) return false;
        int lipH = lipCell.GetCellInstanceHeight();
        if (lipH <= 0) return false;

        WaterManager wm = ResolveWaterManager();
        if (!CellQualifiesForDeckDisplayLip(lx, ly, lipH, heightMap, wm)) return false;

        var normalsScratch = new List<Vector2Int>(4);
        CollectRelaxedLipBridgeNormals(lx, ly, lipH, heightMap, wm, normalsScratch);
        bool dirOk = false;
        for (int n = 0; n < normalsScratch.Count; n++)
        {
            if (normalsScratch[n].x == dir.x && normalsScratch[n].y == dir.y) { dirOk = true; break; }
        }
        if (!dirOk) return false;

        List<Vector2> chord = WalkStraightChordFromLipThroughWetToFarDry(lx, ly, dir.x, dir.y, lipH, heightMap);
        if (chord == null || chord.Count < 2) return false;

        AppendPathSkipDuplicateJoin(pathVec2, chord.GetRange(1, chord.Count - 1));
        return true;
    }

    // -----------------------------------------------------------------------
    // Placement — public
    // -----------------------------------------------------------------------
    public bool TryCommitStreetStrokeForScenarioBuild(List<Vector2> pathRaw, out string error, CityStats cityStats = null)
    {
        error = null;
        if (pathRaw == null || pathRaw.Count < 2) { error = "road stroke must list at least two cells"; return false; }
        if (_terraforming == null || _terrain == null || _grid == null) { error = "road stroke apply failed: TerrainManager or GridManager not available"; return false; }

        HeightMap heightMap = _terrain.GetHeightMap();
        if (heightMap == null) { error = "road stroke apply failed: HeightMap missing"; return false; }

        EnsureResolver();
        if (_resolver == null) { error = "road stroke apply failed: RoadPrefabResolver could not be created"; return false; }

        var streetCtx = new RoadPathValidationContext { forbidCutThrough = false };
        if (!TryPrepareRoadPlacementPlan(pathRaw, streetCtx, false, out List<Vector2> expandedPath, out PathTerraformPlan plan))
        {
            error = "road stroke rejected by road preparation (TryPrepareRoadPlacementPlan): invalid wet run, shore band, cut-through, or Phase-1 height feasibility";
            return false;
        }

        if (!plan.Apply(heightMap, _terrain)) { error = "road stroke PathTerraformPlan.Apply failed (terrain / HeightMap)"; return false; }

        List<ResolvedRoadTile> resolved = _resolver.ResolveForPath(expandedPath, plan);
        _placementPathPositions = new HashSet<Vector2>();
        foreach (ResolvedRoadTile r in resolved)
            _placementPathPositions.Add(new Vector2(r.gridPos.x, r.gridPos.y));
        for (int i = 0; i < resolved.Count; i++)
        {
            PlaceRoadTileFromResolved(resolved[i]);
            UpdateAdjacentRoadPrefabsAt(new Vector2(resolved[i].gridPos.x, resolved[i].gridPos.y));
        }

        RefreshAllAdjacentRoadsOutsidePath();
        _placementPathPositions = null;
        for (int i = 0; i < resolved.Count; i++)
        {
            if (IsBridgeDeckRoadPrefabInternal(resolved[i].prefab)) continue;
            RefreshRoadPrefabAt(new Vector2(resolved[i].gridPos.x, resolved[i].gridPos.y));
        }

        if (cityStats != null)
            cityStats.AddPowerConsumption(resolved.Count * ZoneAttributes.Road.PowerConsumption);
        _grid.InvalidateRoadCache(); // Invariant #2
        return true;
    }

    public void PlaceRoadTileFromResolved(ResolvedRoadTile resolved)
    {
        int x = resolved.gridPos.x;
        int y = resolved.gridPos.y;
        if (_grid.IsCellOccupiedByBuilding(x, y)) return;

        GameObject cell = _grid.GetGridCell(new Vector2(x, y));
        CityCell cellComponentCheck = _grid.GetCell(x, y);
        if (cellComponentCheck != null && cellComponentCheck.isInterstate) return;
        if (cell == null || cellComponentCheck == null) return;

        DestroyPreviousRoadTile(cell, new Vector2(x, y));
        cellComponentCheck.RemoveForestForBuilding();

        GameObject roadTile = Object.Instantiate(resolved.prefab, resolved.worldPos, Quaternion.identity);
        roadTile.transform.SetParent(cellComponentCheck.gameObject.transform);
        roadTile.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1);

        Zone zone = roadTile.AddComponent<Zone>();
        zone.zoneType = Zone.ZoneType.Road;

        UpdateRoadCellAttributes(cellComponentCheck, roadTile, Zone.ZoneType.Road);
        ApplyRoadRouteHintsFromResolved(cellComponentCheck, resolved);
        _grid.SetRoadSortingOrder(roadTile, x, y);
        _grid.AddRoadToCache(resolved.gridPos);
    }

    public bool CanPlaceRoadAt(Vector2 gridPos)
    {
        int gx = (int)gridPos.x, gy = (int)gridPos.y;
        if (!_terrain.CanPlaceRoad(gx, gy)) return false;
        if (_grid.IsCellOccupiedByBuilding(gx, gy)) return false;
        CityCell c = _grid.GetCell(gx, gy);
        if (c != null && c.isInterstate) return false;
        return true;
    }

    public bool PlaceRoadTileAt(Vector2 gridPos)
    {
        if (!CanPlaceRoadAt(gridPos)) return false;
        EnsureResolver();

        Vector2 prevGridPos = gridPos + new Vector2(0, 1);
        Vector2[] dirs = { new Vector2(-1, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(0, -1) };
        foreach (Vector2 d in dirs)
        {
            if (IsRoadAt(gridPos + d)) { prevGridPos = gridPos + d; break; }
        }

        GameObject cell = _grid.GetGridCell(gridPos);
        if (cell == null) return false;
        CityCell cellComponentCheck = _grid.GetCell((int)gridPos.x, (int)gridPos.y);
        if (cellComponentCheck != null && cellComponentCheck.isInterstate) return false;

        GameObject correctRoadPrefab = GetCorrectRoadPrefabInternal(prevGridPos, gridPos);

        DestroyPreviousRoadTile(cell, gridPos);

        CityCell cellComponent = _grid.GetCell((int)gridPos.x, (int)gridPos.y);
        cellComponent.RemoveForestForBuilding();
        int terrainHeight = cellComponent.GetCellInstanceHeight();
        Vector2 worldPos = GetRoadTileWorldPosition((int)gridPos.x, (int)gridPos.y, correctRoadPrefab, terrainHeight);

        GameObject roadTile = Object.Instantiate(correctRoadPrefab, worldPos, Quaternion.identity);
        roadTile.transform.SetParent(cellComponent.gameObject.transform);
        roadTile.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1);

        Zone zone = roadTile.AddComponent<Zone>();
        zone.zoneType = Zone.ZoneType.Road;

        UpdateRoadCellAttributes(cellComponent, roadTile, Zone.ZoneType.Road);
        _grid.SetRoadSortingOrder(roadTile, (int)gridPos.x, (int)gridPos.y);
        _grid.AddRoadToCache(new Vector2Int((int)gridPos.x, (int)gridPos.y));
        UpdateAdjacentRoadPrefabsAt(gridPos);
        return true;
    }

    public void UpdateAdjacentRoadPrefabsAt(Vector2 gridPos)
    {
        var toRefresh = new List<Vector2>();
        Vector2[] dirs = { new Vector2(-1, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(0, -1) };
        foreach (Vector2 d in dirs)
        {
            Vector2 n2 = gridPos + d;
            if (IsRoadAt(n2) && (_placementPathPositions == null || !_placementPathPositions.Contains(n2)))
                toRefresh.Add(n2);
        }
        foreach (Vector2 pos in toRefresh)
            RefreshRoadPrefabAt(pos);
    }

    public void RefreshRoadPrefabsAfterBatchPlacement(IReadOnlyList<Vector2Int> newlyPlacedRoadCells)
    {
        if (newlyPlacedRoadCells == null || newlyPlacedRoadCells.Count == 0 || _grid == null) return;
        var gridMgr = _grid as GridManager;
        int w = gridMgr != null ? gridMgr.width : int.MaxValue;
        int h = gridMgr != null ? gridMgr.height : int.MaxValue;

        var toRefresh = new HashSet<Vector2Int>();
        for (int i = 0; i < newlyPlacedRoadCells.Count; i++)
        {
            Vector2Int p = newlyPlacedRoadCells[i];
            toRefresh.Add(p);
            for (int d = 0; d < 4; d++)
            {
                int nx = p.x + DirX[d], ny = p.y + DirY[d];
                if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                if (IsRoadAt(new Vector2(nx, ny))) toRefresh.Add(new Vector2Int(nx, ny));
            }
        }

        foreach (Vector2Int pos in toRefresh)
        {
            if (!IsRoadAt(new Vector2(pos.x, pos.y))) continue;
            if (IsCellUsingBridgeDeckRoadPrefab(pos)) continue;
            RefreshRoadPrefabAt(new Vector2(pos.x, pos.y));
        }
    }

    public bool ValidateBridgePath(List<Vector2Int> path, HeightMap heightMap)
    {
        if (path == null || path.Count < 2 || heightMap == null) return true;
        var pathVec2 = new List<Vector2>();
        foreach (var p in path) pathVec2.Add(new Vector2(p.x, p.y));
        if (HasTurnOnWaterOrCoast(pathVec2, heightMap) || HasElbowTooCloseToWater(pathVec2, heightMap)) return false;
        if (HasTurnOnLastLandCellsBeforeWater(pathVec2, heightMap, 2)) return false;
        return true;
    }

    public void GetRoadGhostPreviewForCell(Vector2 gridPos, out GameObject prefab, out Vector2 worldPos, out int sortingOrder)
    {
        prefab = _roadMgr?.roadTilePrefab1;
        worldPos = _grid.GetWorldPosition((int)gridPos.x, (int)gridPos.y);
        sortingOrder = _grid.GetRoadSortingOrderForCell((int)gridPos.x, (int)gridPos.y, 0);
        EnsureResolver();
        if (_resolver != null)
            _resolver.ResolveForGhostPreview(gridPos, out prefab, out worldPos, out sortingOrder);
    }

    public GameObject GetCorrectRoadPrefabForPath(Vector2 prevGridPos, Vector2 currGridPos, HashSet<Vector2Int> forceFlatCells = null)
    {
        int gx = (int)currGridPos.x, gy = (int)currGridPos.y;
        var currInt = new Vector2Int(gx, gy);
        if (forceFlatCells != null && forceFlatCells.Contains(currInt))
        {
            Vector2 dir = currGridPos - prevGridPos;
            bool isHorizontal = Mathf.Abs(dir.x) >= Mathf.Abs(dir.y);
            return isHorizontal ? _roadMgr?.roadTilePrefab2 : _roadMgr?.roadTilePrefab1;
        }
        return GetCorrectRoadPrefabInternal(prevGridPos, currGridPos);
    }

    public List<ResolvedRoadTile> ResolvePathForRoads(List<Vector2> path, PathTerraformPlan plan)
    {
        EnsureResolver();
        if (_resolver == null) return new List<ResolvedRoadTile>();
        return _resolver.ResolveForPath(path, plan);
    }

    public bool ValidateInterstatePathForPlacement(List<Vector2Int> path)
    {
        if (path == null || path.Count == 0) return false;
        if (_terraforming == null || _terrain == null || _grid == null) return false;
        var pathVec2 = new List<Vector2>();
        for (int i = 0; i < path.Count; i++) pathVec2.Add(new Vector2(path[i].x, path[i].y));
        return TryPrepareRoadPlacementPlan(pathVec2, new RoadPathValidationContext { forbidCutThrough = true }, false, out _, out _);
    }

    public bool PlaceInterstateFromPath(List<Vector2Int> path, GameSaveManager gameSaveManager, InterstateManager interstateManager)
    {
        if (path == null || path.Count == 0) return false;
        EnsureResolver();
        if (_resolver == null || _terraforming == null || _terrain == null || _grid == null) return false;

        var pathVec2 = new List<Vector2>();
        for (int i = 0; i < path.Count; i++) pathVec2.Add(new Vector2(path[i].x, path[i].y));

        HeightMap heightMap = _terrain.GetHeightMap();
        if (heightMap == null) return false;

        if (!TryPrepareRoadPlacementPlan(pathVec2, new RoadPathValidationContext { forbidCutThrough = true }, false, out List<Vector2> expandedPath, out PathTerraformPlan plan))
            return false;
        if (!plan.Apply(heightMap, _terrain)) return false;

        var resolved = _resolver.ResolveForPath(expandedPath, plan);
        for (int i = 0; i < resolved.Count; i++)
            PlaceInterstateFromResolved(resolved[i]);

        _grid.InvalidateRoadCache(); // Invariant #2

        if (gameSaveManager != null && interstateManager != null)
        {
            NeighborCityBindingRecorder.RecordExits(
                gameSaveManager.neighborCityBindings,
                gameSaveManager.NeighborStubs,
                interstateManager,
                path);
        }
        else if (gameSaveManager == null)
        {
            Debug.LogWarning("[RoadPlacementService] PlaceInterstateFromPath: gameSaveManager null — binding not recorded.");
        }
        return true;
    }

    public void PlaceInterstateFromResolved(ResolvedRoadTile resolved)
    {
        int x = resolved.gridPos.x;
        int y = resolved.gridPos.y;
        if (_grid.IsCellOccupiedByBuilding(x, y)) return;

        GameObject cell = _grid.GetGridCell(new Vector2(x, y));
        CityCell cellComponent = _grid.GetCell(x, y);
        if (cell == null || cellComponent == null) return;

        DestroyPreviousRoadTile(cell, new Vector2(x, y));
        cellComponent.RemoveForestForBuilding();

        GameObject prefab = resolved.prefab ?? _roadMgr?.roadTilePrefab1;
        GameObject roadTile = Object.Instantiate(prefab, resolved.worldPos, Quaternion.identity);
        roadTile.transform.SetParent(cellComponent.gameObject.transform);
        roadTile.GetComponent<SpriteRenderer>().color = new Color(0.78f, 0.78f, 0.88f, 1f);

        Zone zone = roadTile.AddComponent<Zone>();
        zone.zoneType = Zone.ZoneType.Road;
        UpdateRoadCellAttributes(cellComponent, roadTile, Zone.ZoneType.Road);
        ApplyRoadRouteHintsFromResolved(cellComponent, resolved);
        cellComponent.isInterstate = true;

        _grid.SetRoadSortingOrder(roadTile, x, y);
        _grid.AddRoadToCache(resolved.gridPos);
    }

    public void PlaceInterstateTile(Vector2 prevGridPos, Vector2 currGridPos, bool isInterstate)
    {
        int gx = (int)currGridPos.x, gy = (int)currGridPos.y;
        if (_grid.IsCellOccupiedByBuilding(gx, gy)) return;

        GameObject cell = _grid.GetGridCell(currGridPos);
        CityCell cellComponent = _grid.GetCell(gx, gy);
        if (cellComponent == null) return;

        DestroyPreviousRoadTile(cell, currGridPos);
        cellComponent.RemoveForestForBuilding();

        GameObject correctRoadPrefab = GetCorrectRoadPrefabForPath(prevGridPos, currGridPos);
        int terrainHeight = cellComponent.GetCellInstanceHeight();
        Vector2 worldPos = GetRoadTileWorldPosition(gx, gy, correctRoadPrefab, terrainHeight);

        GameObject roadTile = Object.Instantiate(correctRoadPrefab, worldPos, Quaternion.identity);
        roadTile.transform.SetParent(cellComponent.gameObject.transform);

        roadTile.GetComponent<SpriteRenderer>().color = isInterstate
            ? new Color(0.78f, 0.78f, 0.88f, 1f)
            : new Color(1, 1, 1, 1);

        Zone zone = roadTile.AddComponent<Zone>();
        zone.zoneType = Zone.ZoneType.Road;
        UpdateRoadCellAttributes(cellComponent, roadTile, Zone.ZoneType.Road);
        if (isInterstate) cellComponent.isInterstate = true;

        _grid.SetRoadSortingOrder(roadTile, gx, gy);
        _grid.AddRoadToCache(new Vector2Int(gx, gy));
    }

    public void RestoreRoadTile(Vector2Int gridPos, GameObject prefab, bool isInterstate, ZoneManager zoneManager, int? savedSpriteSortingOrder = null)
    {
        GameObject cell = _grid.GetGridCell(new Vector2(gridPos.x, gridPos.y));
        if (cell == null) return;
        CityCell cellComponent = _grid.GetCell(gridPos.x, gridPos.y);
        if (cellComponent == null) return;

        var toDestroy = new List<(GameObject go, Zone zone)>();
        foreach (Transform child in cell.transform)
        {
            Zone z = child.GetComponent<Zone>();
            if (z != null) toDestroy.Add((child.gameObject, z));
        }
        foreach (var t in toDestroy)
        {
            if (t.zone.zoneCategory == Zone.ZoneCategory.Zoning)
                zoneManager.removeZonedPositionFromList(new Vector2(gridPos.x, gridPos.y), t.zone.zoneType);
            Object.Destroy(t.go);
        }

        cellComponent.RemoveForestForBuilding();

        int terrainHeight = cellComponent.GetCellInstanceHeight();
        Vector2 worldPos = GetRoadTileWorldPosition(gridPos.x, gridPos.y, prefab, terrainHeight);

        GameObject roadTile = Object.Instantiate(prefab, worldPos, Quaternion.identity);
        roadTile.transform.SetParent(cellComponent.gameObject.transform);

        roadTile.GetComponent<SpriteRenderer>().color = isInterstate
            ? new Color(0.78f, 0.78f, 0.88f, 1f)
            : new Color(1, 1, 1, 1);

        Zone roadZone = roadTile.AddComponent<Zone>();
        roadZone.zoneType = Zone.ZoneType.Road;
        UpdateRoadCellAttributes(cellComponent, roadTile, Zone.ZoneType.Road);
        if (isInterstate) cellComponent.isInterstate = true;

        if (savedSpriteSortingOrder.HasValue)
        {
            SpriteRenderer sr = roadTile.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = savedSpriteSortingOrder.Value;
            cellComponent.SetCellInstanceSortingOrder(savedSpriteSortingOrder.Value);
        }
        else
            _grid.SetRoadSortingOrder(roadTile, gridPos.x, gridPos.y);
        _grid.AddRoadToCache(gridPos);
    }

    public void ReplaceRoadTileAt(Vector2Int gridPos, GameObject newPrefab, bool keepInterstateTint)
    {
        GameObject cell = _grid.GetGridCell(new Vector2(gridPos.x, gridPos.y));
        if (cell == null) return;
        CityCell cellComponent = _grid.GetCell(gridPos.x, gridPos.y);
        if (cellComponent == null) return;

        var toDestroy = new List<GameObject>();
        foreach (Transform child in cell.transform)
        {
            Zone zone = child.GetComponent<Zone>();
            if (zone != null && zone.zoneType == Zone.ZoneType.Road)
                toDestroy.Add(child.gameObject);
        }
        foreach (GameObject go in toDestroy) Object.Destroy(go);

        int terrainHeight = cellComponent.GetCellInstanceHeight();
        Vector2 worldPos = GetRoadTileWorldPosition(gridPos.x, gridPos.y, newPrefab, terrainHeight);

        GameObject roadTile = Object.Instantiate(newPrefab, worldPos, Quaternion.identity);
        roadTile.transform.SetParent(cellComponent.gameObject.transform);

        roadTile.GetComponent<SpriteRenderer>().color = keepInterstateTint
            ? new Color(0.78f, 0.78f, 0.88f, 1f)
            : new Color(1, 1, 1, 1);

        Zone roadZone = roadTile.AddComponent<Zone>();
        roadZone.zoneType = Zone.ZoneType.Road;
        UpdateRoadCellAttributes(cellComponent, roadTile, Zone.ZoneType.Road);
        _grid.SetRoadSortingOrder(roadTile, gridPos.x, gridPos.y);
    }

    // -----------------------------------------------------------------------
    // Private — path helpers
    // -----------------------------------------------------------------------
    bool TryFinalizeManualRoadPlacement(CityStats cityStats, UIManager uiManager)
    {
        if (_terraforming == null || _terrain == null || _grid == null) return false;
        HeightMap heightMap = _terrain.GetHeightMap();
        if (heightMap == null) return false;
        EnsureResolver();
        if (_resolver == null) return false;

        List<Vector2> path = GetManualRoadPathWithOptionalBridgeExtension();
        var manualCtx = new RoadPathValidationContext { forbidCutThrough = false };
        if (!TryPrepareLockedDeckSpanBridgePlacement(path, manualCtx, out List<Vector2> expandedPath, out PathTerraformPlan plan)
            && !TryPrepareRoadPlacementPlanLongestValidPrefix(path, manualCtx, false, ref _manualRoadLongestPrefixHint, out expandedPath, out plan, out _))
            return false;

        int tileCount = expandedPath.Count;
        int totalCost = tileCount * CostPerTile;
        if (cityStats != null && !cityStats.CanAfford(totalCost))
        {
            if (uiManager != null) uiManager.ShowInsufficientFundsTooltip("Road", totalCost);
            return false;
        }

        if (!plan.Apply(heightMap, _terrain)) return false;

        if (cityStats != null) cityStats.RemoveMoney(totalCost);
        var resolved = _resolver.ResolveForPath(expandedPath, plan);
        _placementPathPositions = new HashSet<Vector2>();
        foreach (var r in resolved) _placementPathPositions.Add(new Vector2(r.gridPos.x, r.gridPos.y));
        for (int i = 0; i < resolved.Count; i++)
        {
            PlaceRoadTileFromResolved(resolved[i]);
            BlipEngine.Play(BlipId.ToolRoadTick);
            UpdateAdjacentRoadPrefabsAt(new Vector2(resolved[i].gridPos.x, resolved[i].gridPos.y));
        }
        RefreshAllAdjacentRoadsOutsidePath();
        _placementPathPositions = null;
        for (int i = 0; i < resolved.Count; i++)
        {
            if (IsBridgeDeckRoadPrefabInternal(resolved[i].prefab)) continue;
            RefreshRoadPrefabAt(new Vector2(resolved[i].gridPos.x, resolved[i].gridPos.y));
        }
        if (cityStats != null)
            cityStats.AddPowerConsumption(resolved.Count * ZoneAttributes.Road.PowerConsumption);
        BlipEngine.Play(BlipId.ToolRoadComplete);
        return true;
    }

    void DrawPreviewLineCore(List<Vector2> path)
    {
        if (path == null || path.Count == 0) return;
        if (_terraforming == null || _terrain == null || _grid == null) return;
        EnsureResolver();
        if (_resolver == null) return;

        var previewCtx = new RoadPathValidationContext { forbidCutThrough = false };
        if (TryPrepareLockedDeckSpanBridgePlacement(path, previewCtx, out List<Vector2> expandedPath, out PathTerraformPlan plan))
            _manualRoadLongestPrefixHint = path.Count;
        else if (!TryPrepareRoadPlacementPlanLongestValidPrefix(path, previewCtx, false, ref _manualRoadLongestPrefixHint, out expandedPath, out plan, out _))
            return;

        var resolved = _resolver.ResolveForPath(expandedPath, plan);
        PreviewResolvedTiles.Clear();
        PreviewResolvedTiles.AddRange(resolved);

        for (int i = 0; i < resolved.Count; i++)
        {
            var tile = resolved[i];
            CityCell cell2 = _grid.GetCell(tile.gridPos.x, tile.gridPos.y);
            if (cell2 == null) continue;
            GameObject previewTile = Object.Instantiate(tile.prefab, tile.worldPos, Quaternion.identity);
            previewTile.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0.5f);
            PreviewRoadTiles.Add(previewTile);
            PreviewRoadGridPositions.Add(new Vector2(tile.gridPos.x, tile.gridPos.y));
            previewTile.transform.SetParent(cell2.gameObject.transform);
        }
    }

    public void ClearPreview()
    {
        PreviewResolvedTiles.Clear();
        foreach (GameObject t in PreviewRoadTiles) if (t != null) Object.Destroy(t);
        PreviewRoadTiles.Clear();
        PreviewRoadGridPositions.Clear();
    }

    void ClearManualPreviewBridgeLock()
    {
        _manualPreviewBridgeLocked = false;
        _lockedBridgeChord?.Clear();
    }

    List<Vector2> GetManualRoadPathWithOptionalBridgeExtension()
    {
        Vector2 cursor = _currentDrawCursorGrid;
        int cx = (int)cursor.x, cy = (int)cursor.y;

        if (_manualPreviewBridgeLocked && _lockedBridgeChord != null && _lockedBridgeChord.Count >= 2)
        {
            int lx = _lockedBridgeLip.x, ly = _lockedBridgeLip.y;
            int ndx = _lockedBridgeNormal.x, ndy = _lockedBridgeNormal.y;
            int dot = (cx - lx) * ndx + (cy - ly) * ndy;
            if (dot <= 0)
                ClearManualPreviewBridgeLock();
            else
            {
                var approach = GetLine(_startPosition, new Vector2(lx, ly));
                if (approach != null && approach.Count > 0)
                {
                    var merged = new List<Vector2>(approach.Count + _lockedBridgeChord.Count + 16);
                    merged.AddRange(approach);
                    AppendPathSkipDuplicateJoin(merged, _lockedBridgeChord);
                    int ex = (int)merged[merged.Count - 1].x, ey = (int)merged[merged.Count - 1].y;
                    if (cx != ex || cy != ey)
                    {
                        var tail = GetLine(new Vector2(ex, ey), cursor);
                        if (tail != null && tail.Count > 0) AppendPathSkipDuplicateJoin(merged, tail);
                    }
                    List<Vector2> extended = TryExtendPathAcrossWaterToOppositeLand(merged);
                    if (extended != null) { _manualRoadLongestPrefixHint = 0; return extended; }
                    return merged;
                }
                ClearManualPreviewBridgeLock();
            }
        }

        List<Vector2> rawLine = GetLine(_startPosition, cursor);
        List<Vector2> flexed = TryFlexBridgePreviewPathAcrossLipPlane(_startPosition, cursor);
        List<Vector2> raw = (flexed != null && flexed.Count >= 2) ? flexed : rawLine;

        List<Vector2> extended2 = TryExtendPathAcrossWaterToOppositeLand(raw);
        if (extended2 != null) { _manualRoadLongestPrefixHint = 0; return extended2; }
        return raw;
    }

    bool TryPrepareLockedDeckSpanBridgePlacement(List<Vector2> mergedPath, RoadPathValidationContext ctx, out List<Vector2> expandedPath, out PathTerraformPlan plan)
    {
        expandedPath = null; plan = null;
        if (!_manualPreviewBridgeLocked || _lockedBridgeChord == null || _lockedBridgeChord.Count < 2) return false;
        if (mergedPath == null || mergedPath.Count < 2 || _terraforming == null || _terrain == null || _grid == null) return false;

        int chordEnd = FindLockedChordEndIndexInMergedPath(mergedPath, _lockedBridgeChord);
        if (chordEnd < 0) return false;

        int minLen = chordEnd + 1;
        for (int k = mergedPath.Count; k >= minLen; k--)
        {
            List<Vector2> prefix = SubListCopy(mergedPath, k);
            if (TryPrepareDeckSpanPlanFromAdjacentStroke(prefix, ctx, out expandedPath, out plan)) return true;
        }
        return false;
    }

    bool TryPrepareDeckSpanPlanFromAdjacentStroke(List<Vector2> adjacentPath, RoadPathValidationContext ctx, out List<Vector2> expandedPath, out PathTerraformPlan plan)
    {
        expandedPath = null; plan = null;
        if (adjacentPath == null || adjacentPath.Count < 2 || _terraforming == null || _terrain == null) return false;

        HeightMap heightMap = _terrain.GetHeightMap();
        if (heightMap == null) return false;

        List<Vector2> pathForPlan = RoadStrokeTerrainRules.TruncatePathAtFirstDisallowedLandSlope(adjacentPath, TerrainMgr);
        if (pathForPlan == null || pathForPlan.Count < 2) return false;

        if (!IsPathFullyAdjacent(pathForPlan)) return false;
        if (!IsBridgePathValid(pathForPlan, heightMap)) return false;
        if (!ValidateFeat44WaterBridgeRules(pathForPlan, heightMap, postUserWarnings: false)) return false;
        if (HasTurnOnWaterOrCoast(pathForPlan, heightMap)) return false;
        if (HasElbowTooCloseToWater(pathForPlan, heightMap, relaxElbowNearWaterForWaterBridge: !ctx.forbidCutThrough)) return false;

        expandedPath = TerraformingService.ExpandDiagonalStepsToCardinal(pathForPlan);
        var svc = TerraformingSvc;
        if (svc == null || !svc.TryBuildDeckSpanOnlyWaterBridgePlan(expandedPath, out plan)) return false;
        if (!ValidateTerraformPlanWithContext(plan, ctx)) return false;
        if (!plan.TryValidatePhase1Heights(heightMap, _terrain, null, null)) return false;
        return true;
    }

    bool TryBuildFilteredPathForRoadPlan(List<Vector2> pathRaw, bool postUserWarnings, out List<Vector2> filteredPath, bool relaxElbowNearWaterForWaterBridge = false)
    {
        filteredPath = null;
        if (pathRaw == null || pathRaw.Count == 0 || _terraforming == null || _terrain == null || _grid == null) return false;

        HeightMap heightMap = _terrain.GetHeightMap();
        if (heightMap == null) return false;

        var list = new List<Vector2>();
        for (int i = 0; i < pathRaw.Count; i++)
        {
            Vector2 gridPos2 = pathRaw[i];
            if (_grid.GetCell((int)gridPos2.x, (int)gridPos2.y) != null) list.Add(gridPos2);
        }
        if (list.Count == 0) return false;

        list = RoadStrokeTerrainRules.TruncatePathAtFirstDisallowedLandSlope(list, TerrainMgr);
        if (list.Count == 0) return false;

        if (!IsPathFullyAdjacent(list))
        {
            if (postUserWarnings && GameNotificationManager.Instance != null)
                GameNotificationManager.Instance.PostWarning("Road path has gaps (e.g. over water). Draw a continuous path.");
            return false;
        }

        list = StraightenBridgeSegments(list, heightMap);
        if (!IsBridgePathValid(list, heightMap))
        {
            if (postUserWarnings && GameNotificationManager.Instance != null)
                GameNotificationManager.Instance.PostWarning("Cannot build a valid bridge here. Draw a straighter path over water.");
            return false;
        }
        if (!ValidateFeat44WaterBridgeRules(list, heightMap, postUserWarnings)) return false;
        if (HasTurnOnWaterOrCoast(list, heightMap))
        {
            if (postUserWarnings && GameNotificationManager.Instance != null)
                GameNotificationManager.Instance.PostWarning("Bridges must be straight. Turns cannot be on water or coast.");
            return false;
        }
        if (HasElbowTooCloseToWater(list, heightMap, relaxElbowNearWaterForWaterBridge))
        {
            if (postUserWarnings && GameNotificationManager.Instance != null)
                GameNotificationManager.Instance.PostWarning("Turns must be at least 2 cells away from water.");
            return false;
        }

        filteredPath = list;
        return true;
    }

    bool TryPrepareFromFilteredPathList(List<Vector2> filteredPath, RoadPathValidationContext ctx, bool postUserWarnings, out List<Vector2> expandedPath, out PathTerraformPlan plan)
    {
        expandedPath = null; plan = null;
        HeightMap heightMap = _terrain.GetHeightMap();
        if (heightMap == null || filteredPath == null || filteredPath.Count == 0) return false;

        expandedPath = TerraformingService.ExpandDiagonalStepsToCardinal(filteredPath);
        bool waterBridgeRelax = PathQualifiesForWaterBridgeTerraformRelaxation(expandedPath)
            || PathQualifiesForWaterAdjacentDeckTerraformRelaxation(expandedPath);
        plan = _terraforming.ComputePathPlan(expandedPath, waterBridgeRelax);
        if (!ValidateTerraformPlanWithContext(plan, ctx))
        {
            if (postUserWarnings && GameNotificationManager.Instance != null)
            {
                if (plan != null && plan.isValid && ctx.forbidCutThrough && plan.isCutThrough)
                    GameNotificationManager.Instance.PostWarning("Interstate cannot cut through hills. Choose a different route.");
                else
                    GameNotificationManager.Instance.PostWarning("Road cannot cross terrain with height difference greater than 1. Choose a different path.");
            }
            return false;
        }
        if (!plan.TryValidatePhase1Heights(heightMap, _terrain, null, null))
        {
            if (postUserWarnings && GameNotificationManager.Instance != null)
                GameNotificationManager.Instance.PostWarning("Terrain cannot be modified safely (height difference would exceed 1). Choose a different path.");
            return false;
        }
        return true;
    }

    bool PathQualifiesForWaterBridgeTerraformRelaxation(List<Vector2> expandedPath)
    {
        if (expandedPath == null || expandedPath.Count < 3 || _terrain == null) return false;
        HeightMap hm = _terrain.GetHeightMap();
        if (hm == null) return false;
        if (!TryGetSingleBridgeRunBounds(expandedPath, hm, out int rs, out int re)) return false;
        if (rs < 1 || re >= expandedPath.Count - 1) return false;
        if (IsWaterOrWaterSlope((int)expandedPath[rs - 1].x, (int)expandedPath[rs - 1].y, hm)) return false;
        if (IsWaterOrWaterSlope((int)expandedPath[re + 1].x, (int)expandedPath[re + 1].y, hm)) return false;
        return ValidateFeat44WaterBridgeRules(expandedPath, hm, postUserWarnings: false);
    }

    bool PathQualifiesForWaterAdjacentDeckTerraformRelaxation(List<Vector2> expandedPath)
    {
        if (expandedPath == null || expandedPath.Count == 0 || _terrain == null) return false;
        if (PathQualifiesForWaterBridgeTerraformRelaxation(expandedPath)) return false;
        HeightMap hm = _terrain.GetHeightMap();
        if (hm == null) return false;
        WaterManager wm = ResolveWaterManager();
        int[] cdx = { 1, -1, 0, 0 };
        int[] cdy2 = { 0, 0, 1, -1 };
        foreach (Vector2 p in expandedPath)
        {
            int x = (int)p.x, y = (int)p.y;
            if (_terrain.IsRegisteredOpenWaterAt(x, y)) continue;
            int h = hm.IsValidPosition(x, y) ? hm.GetHeight(x, y) : TerrainManager.MIN_HEIGHT;
            for (int d = 0; d < 4; d++)
            {
                int nx = x + cdx[d], ny = y + cdy2[d];
                if (!hm.IsValidPosition(nx, ny)) continue;
                int hn = hm.GetHeight(nx, ny);
                if (hn >= h) continue;
                if (DryCellTouchesRegisteredWaterForHighDeck(nx, ny, wm)) return true;
            }
        }
        return false;
    }

    bool DryCellTouchesRegisteredWaterForHighDeck(int x, int y, WaterManager wm)
    {
        if (_terrain == null) return false;
        if (_terrain.IsRegisteredOpenWaterAt(x, y) || _terrain.IsWaterSlopeCell(x, y)) return true;
        if (wm == null) return false;
        int[] mdx = { 1, -1, 0, 0, 1, 1, -1, -1 };
        int[] mdy = { 0, 0, 1, -1, 1, -1, 1, -1 };
        for (int d = 0; d < 8; d++) { if (wm.IsWaterAt(x + mdx[d], y + mdy[d])) return true; }
        return false;
    }

    bool IsWaterOrWaterSlope(int x, int y, HeightMap heightMap)
    {
        if (heightMap == null || !heightMap.IsValidPosition(x, y) || _terrain == null) return false;
        if (_terrain.IsRegisteredOpenWaterAt(x, y)) return true;
        return _terrain.IsWaterSlopeCell(x, y);
    }

    bool CellQualifiesForDeckDisplayLip(int x, int y, int h, HeightMap heightMap, WaterManager wm)
    {
        if (heightMap == null || _terrain == null || !heightMap.IsValidPosition(x, y)) return false;
        int[] cdx = { 1, -1, 0, 0 };
        int[] cdy = { 0, 0, 1, -1 };
        for (int d = 0; d < 4; d++)
        {
            int nx = x + cdx[d], ny = y + cdy[d];
            if (!heightMap.IsValidPosition(nx, ny)) continue;
            int hn = heightMap.GetHeight(nx, ny);
            if (hn >= h) continue;
            if (_terrain.IsRegisteredOpenWaterAt(nx, ny) || _terrain.IsWaterSlopeCell(nx, ny)) return true;
            if (DryCellTouchesRegisteredWaterForHighDeck(nx, ny, wm)) return true;
        }
        return false;
    }

    void CollectRelaxedLipBridgeNormals(int x, int y, int h, HeightMap heightMap, WaterManager wm, List<Vector2Int> outNormals)
    {
        outNormals.Clear();
        int[] cdx = { 1, -1, 0, 0 };
        int[] cdy = { 0, 0, 1, -1 };
        for (int d = 0; d < 4; d++)
        {
            int nx = x + cdx[d], ny = y + cdy[d];
            if (!heightMap.IsValidPosition(nx, ny)) continue;
            int hn = heightMap.GetHeight(nx, ny);
            if (hn >= h) continue;
            if (_terrain.IsRegisteredOpenWaterAt(nx, ny) || _terrain.IsWaterSlopeCell(nx, ny))
            { outNormals.Add(new Vector2Int(cdx[d], cdy[d])); continue; }
            if (DryCellTouchesRegisteredWaterForHighDeck(nx, ny, wm))
                outNormals.Add(new Vector2Int(cdx[d], cdy[d]));
        }
    }

    List<Vector2> WalkStraightChordFromLipThroughWetToFarDry(int lx, int ly, int ddx, int ddy, int bridgeHeight, HeightMap heightMap)
    {
        var result = new List<Vector2> { new Vector2(lx, ly) };
        int nx2 = lx + ddx, ny2 = ly + ddy;
        if (!heightMap.IsValidPosition(nx2, ny2)) return null;
        if (!IsWaterOrWaterSlope(nx2, ny2, heightMap)) return null;
        int cx2 = nx2, cy2 = ny2;
        result.Add(new Vector2(cx2, cy2));
        var gridMgr = _grid as GridManager;
        int maxSteps = gridMgr != null ? Mathf.Max(gridMgr.width, gridMgr.height) + 2 : 256;
        for (int step = 0; step < maxSteps; step++)
        {
            int ax = cx2 + ddx, ay = cy2 + ddy;
            if (!heightMap.IsValidPosition(ax, ay)) break;
            if (IsWaterOrWaterSlope(ax, ay, heightMap)) { result.Add(new Vector2(ax, ay)); cx2 = ax; cy2 = ay; continue; }
            if (_grid.IsCellOccupiedByBuilding(ax, ay)) break;
            CityCell farCell = _grid.GetCell(ax, ay);
            if (farCell == null || farCell.isInterstate) break;
            if (!_terrain.CanPlaceRoad(ax, ay)) break;
            if (farCell.GetCellInstanceHeight() != bridgeHeight) break;
            result.Add(new Vector2(ax, ay));
            return result;
        }
        return null;
    }

    List<Vector2> TryExtendPathAcrossWaterToOppositeLand(List<Vector2> path)
    {
        HeightMap heightMap = _terrain != null ? _terrain.GetHeightMap() : null;
        if (path == null || path.Count < 2 || heightMap == null || _grid == null || _terrain == null) return null;

        int n2 = path.Count;
        int lx2 = (int)path[n2 - 1].x, ly2 = (int)path[n2 - 1].y;
        if (!IsWaterOrWaterSlope(lx2, ly2, heightMap)) return null;

        int wetStart = n2 - 1;
        while (wetStart > 0 && IsWaterOrWaterSlope((int)path[wetStart - 1].x, (int)path[wetStart - 1].y, heightMap))
            wetStart--;
        if (wetStart == 0) return null;

        int bx = (int)path[wetStart - 1].x, by = (int)path[wetStart - 1].y;
        if (IsWaterOrWaterSlope(bx, by, heightMap)) return null;

        int fwx = (int)path[wetStart].x, fwy = (int)path[wetStart].y;
        int sdx = fwx - bx, sdy = fwy - by;
        if (Mathf.Abs(sdx) > 1 || Mathf.Abs(sdy) > 1 || (sdx != 0 && sdy != 0)) return null;

        int dx2 = sdx != 0 ? (int)Mathf.Sign(sdx) : 0;
        int dy2 = sdy != 0 ? (int)Mathf.Sign(sdy) : 0;
        if (dx2 == 0 && dy2 == 0) return null;

        CityCell landBeforeCell = _grid.GetCell(bx, by);
        if (landBeforeCell == null) return null;
        int bridgeHeight = landBeforeCell.GetCellInstanceHeight();

        int cx3 = lx2, cy3 = ly2;
        var result = new List<Vector2>(path);
        var gridMgr = _grid as GridManager;
        int maxSteps = gridMgr != null ? Mathf.Max(gridMgr.width, gridMgr.height) + 2 : 256;
        for (int step = 0; step < maxSteps; step++)
        {
            int nx3 = cx3 + dx2, ny3 = cy3 + dy2;
            if (!heightMap.IsValidPosition(nx3, ny3)) break;
            if (IsWaterOrWaterSlope(nx3, ny3, heightMap))
            {
                if ((int)result[result.Count - 1].x != nx3 || (int)result[result.Count - 1].y != ny3)
                    result.Add(new Vector2(nx3, ny3));
                cx3 = nx3; cy3 = ny3; continue;
            }
            if (_grid.IsCellOccupiedByBuilding(nx3, ny3)) break;
            CityCell farCell = _grid.GetCell(nx3, ny3);
            if (farCell == null || farCell.isInterstate) break;
            if (!_terrain.CanPlaceRoad(nx3, ny3)) break;
            if (farCell.GetCellInstanceHeight() != bridgeHeight) break;
            result.Add(new Vector2(nx3, ny3));
            return result;
        }
        return null;
    }

    List<Vector2> TryFlexBridgePreviewPathAcrossLipPlane(Vector2 strokeStart, Vector2 cursor)
    {
        HeightMap heightMap = _terrain != null ? _terrain.GetHeightMap() : null;
        if (heightMap == null || _grid == null || _terrain == null) return null;

        CollectManualStrokeLipCandidateCells(strokeStart, cursor, _lipCandidateScratch);
        if (_lipCandidateScratch.Count < 1) return null;

        WaterManager wm = ResolveWaterManager();
        int cx4 = (int)cursor.x, cy4 = (int)cursor.y;
        var normalsScratch = new List<Vector2Int>(4);

        for (int i = 0; i < _lipCandidateScratch.Count; i++)
        {
            int lx3 = (int)_lipCandidateScratch[i].x, ly3 = (int)_lipCandidateScratch[i].y;
            if (!heightMap.IsValidPosition(lx3, ly3)) continue;
            if (_terrain.IsRegisteredOpenWaterAt(lx3, ly3)) continue;
            CityCell lipCell = _grid.GetCell(lx3, ly3);
            if (lipCell == null) continue;
            int lipH = lipCell.GetCellInstanceHeight();
            if (lipH <= 0) continue;
            if (!CellQualifiesForDeckDisplayLip(lx3, ly3, lipH, heightMap, wm)) continue;

            CollectRelaxedLipBridgeNormals(lx3, ly3, lipH, heightMap, wm, normalsScratch);
            if (normalsScratch.Count == 0) continue;

            int bestDx = 0, bestDy = 0, bestDot = int.MinValue;
            for (int n2 = 0; n2 < normalsScratch.Count; n2++)
            {
                int ddx2 = normalsScratch[n2].x, ddy2 = normalsScratch[n2].y;
                int dot = (cx4 - lx3) * ddx2 + (cy4 - ly3) * ddy2;
                if (dot > bestDot) { bestDot = dot; bestDx = ddx2; bestDy = ddy2; }
            }
            if (bestDot <= 0) continue;

            var approach = GetLine(strokeStart, new Vector2(lx3, ly3));
            if (approach == null || approach.Count == 0) continue;

            var chord = WalkStraightChordFromLipThroughWetToFarDry(lx3, ly3, bestDx, bestDy, lipH, heightMap);
            if (chord == null || chord.Count < 2) continue;

            var mergedCore = new List<Vector2>(approach.Count + chord.Count + 2);
            mergedCore.AddRange(approach);
            AppendPathSkipDuplicateJoin(mergedCore, chord);
            if (mergedCore.Count < 2 || !IsPathFullyAdjacent(mergedCore)) continue;

            _manualPreviewBridgeLocked = true;
            _lockedBridgeLip = new Vector2Int(lx3, ly3);
            _lockedBridgeNormal = new Vector2Int(bestDx, bestDy);
            if (_lockedBridgeChord == null) _lockedBridgeChord = new List<Vector2>(chord.Count);
            else { _lockedBridgeChord.Clear(); _lockedBridgeChord.Capacity = Mathf.Max(_lockedBridgeChord.Capacity, chord.Count); }
            for (int c = 0; c < chord.Count; c++) _lockedBridgeChord.Add(chord[c]);

            var merged2 = new List<Vector2>(mergedCore.Count + 8);
            merged2.AddRange(mergedCore);
            int ex2 = (int)merged2[merged2.Count - 1].x, ey2 = (int)merged2[merged2.Count - 1].y;
            if (cx4 != ex2 || cy4 != ey2)
            {
                var tail = GetLine(new Vector2(ex2, ey2), cursor);
                if (tail != null && tail.Count > 0) AppendPathSkipDuplicateJoin(merged2, tail);
            }
            if (merged2.Count < 2) continue;
            return merged2;
        }
        return null;
    }

    void CollectManualStrokeLipCandidateCells(Vector2 strokeStart, Vector2 cursor, List<Vector2> orderedUniqueOut)
    {
        orderedUniqueOut.Clear();
        var seen = new HashSet<(int, int)>();
        void AddPathCellsInReverse(List<Vector2> p)
        {
            if (p == null) return;
            for (int i = p.Count - 1; i >= 0; i--)
            {
                int x = (int)p[i].x, y = (int)p[i].y;
                if (seen.Add((x, y))) orderedUniqueOut.Add(new Vector2(x, y));
            }
        }
        AddPathCellsInReverse(GetLine(strokeStart, cursor));
        int sx = (int)strokeStart.x, sy = (int)strokeStart.y;
        int cxi = (int)cursor.x, cyi = (int)cursor.y;
        AddPathCellsInReverse(GetLine(strokeStart, new Vector2(cxi, sy)));
        AddPathCellsInReverse(GetLine(strokeStart, new Vector2(sx, cyi)));
    }

    List<Vector2> GetLine(Vector2 start, Vector2 end)
    {
        var gridMgr = _grid as GridManager;
        if (gridMgr == null) return GetLineBresenham(start, end);
        Vector2Int from = new Vector2Int(Mathf.Clamp((int)start.x, 0, gridMgr.width - 1), Mathf.Clamp((int)start.y, 0, gridMgr.height - 1));
        Vector2Int to = new Vector2Int(Mathf.Clamp((int)end.x, 0, gridMgr.width - 1), Mathf.Clamp((int)end.y, 0, gridMgr.height - 1));
        var path = gridMgr.FindPath(from, to);
        if (path != null && path.Count > 0)
        {
            var line = new List<Vector2>(path.Count);
            for (int i = 0; i < path.Count; i++) line.Add(new Vector2(path[i].x, path[i].y));
            return line;
        }
        return GetLineBresenham(start, end);
    }

    List<Vector2> GetLineBresenham(Vector2 start, Vector2 end)
    {
        var gridMgr = _grid as GridManager;
        int w = gridMgr != null ? gridMgr.width - 1 : 1000;
        int h = gridMgr != null ? gridMgr.height - 1 : 1000;
        int x0 = Mathf.Clamp((int)start.x, 0, w), y0 = Mathf.Clamp((int)start.y, 0, h);
        int x1 = Mathf.Clamp((int)end.x, 0, w), y1 = Mathf.Clamp((int)end.y, 0, h);
        int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        var line = new List<Vector2>();
        while (true)
        {
            line.Add(new Vector2(x0, y0));
            if (x0 == x1 && y0 == y1) break;
            int e2 = err * 2;
            bool movedX = false, movedY = false;
            if (e2 > -dy) { err -= dy; x0 += sx; movedX = true; }
            if (e2 < dx) { err += dx; y0 += sy; movedY = true; }
            if (movedX && movedY) line.Add(new Vector2(x0 - sx, y0));
        }
        return line;
    }

    // -----------------------------------------------------------------------
    // Private — placement helpers
    // -----------------------------------------------------------------------
    void RefreshRoadPrefabAt(Vector2 gridPos)
    {
        if (_grid.IsCellOccupiedByBuilding((int)gridPos.x, (int)gridPos.y)) return;
        GameObject cell = _grid.GetGridCell(gridPos);
        if (cell == null) return;
        CityCell cellComponentCheck = _grid.GetCell((int)gridPos.x, (int)gridPos.y);
        if (cellComponentCheck == null) return;

        InvalidateRoadRouteHintsIfTopologyMismatch(gridPos, cellComponentCheck);
        Vector2 prevGridPos = PickPrevGridPosForRoadRefresh(gridPos, cellComponentCheck);

        EnsureResolver();
        GameObject correctRoadPrefab;
        Vector2 worldPos;
        if (_resolver != null)
        {
            var resolved = _resolver.ResolveForCell(gridPos, prevGridPos);
            correctRoadPrefab = resolved.HasValue ? resolved.Value.prefab : _roadMgr?.roadTilePrefab1;
            worldPos = resolved.HasValue ? resolved.Value.worldPos : _grid.GetWorldPosition((int)gridPos.x, (int)gridPos.y);
        }
        else
        {
            correctRoadPrefab = GetCorrectRoadPrefabInternal(prevGridPos, gridPos);
            CityCell cc2 = _grid.GetCell((int)gridPos.x, (int)gridPos.y);
            int terrainHeight2 = cc2 != null ? cc2.GetCellInstanceHeight() : 0;
            worldPos = GetRoadTileWorldPosition((int)gridPos.x, (int)gridPos.y, correctRoadPrefab, terrainHeight2);
        }

        DestroyPreviousRoadTile(cell, gridPos);
        cellComponentCheck.RemoveForestForBuilding();

        GameObject roadTile = Object.Instantiate(correctRoadPrefab, worldPos, Quaternion.identity);
        roadTile.transform.SetParent(cellComponentCheck.gameObject.transform);
        roadTile.GetComponent<SpriteRenderer>().color = cellComponentCheck.isInterstate
            ? new Color(0.78f, 0.78f, 0.88f, 1f) : new Color(1, 1, 1, 1);

        Zone zone = roadTile.AddComponent<Zone>();
        zone.zoneType = Zone.ZoneType.Road;
        UpdateRoadCellAttributes(cellComponentCheck, roadTile, Zone.ZoneType.Road);
        _grid.SetRoadSortingOrder(roadTile, (int)gridPos.x, (int)gridPos.y);
        _grid.AddRoadToCache(new Vector2Int((int)gridPos.x, (int)gridPos.y));
    }

    void RefreshAllAdjacentRoadsOutsidePath()
    {
        if (_placementPathPositions == null) return;
        var toRefresh = new HashSet<Vector2>();
        Vector2[] dirs = { new Vector2(-1, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(0, -1) };
        foreach (Vector2 pathPos in _placementPathPositions)
        {
            foreach (Vector2 d in dirs)
            {
                Vector2 n2 = pathPos + d;
                if (IsRoadAt(n2) && !_placementPathPositions.Contains(n2)) toRefresh.Add(n2);
            }
        }
        foreach (Vector2 pos in toRefresh) RefreshRoadPrefabAt(pos);
    }

    void DestroyPreviousRoadTile(GameObject cell, Vector2 gridPos)
    {
        if (_grid != null)
        {
            CityCell cleared = _grid.GetCell((int)gridPos.x, (int)gridPos.y);
            cleared?.ClearRoadRouteHints();
        }
        if (cell.transform.childCount > 0)
        {
            var toDestroy = new List<(GameObject go, Zone zone)>();
            foreach (Transform child in cell.transform)
            {
                Zone zone = child.GetComponent<Zone>();
                if (zone != null && zone.zoneType == Zone.ZoneType.Road) toDestroy.Add((child.gameObject, zone));
            }
            foreach (var t in toDestroy)
            {
                _grid.RemoveRoadFromCache(new Vector2Int((int)gridPos.x, (int)gridPos.y));
                Object.Destroy(t.go);
            }
        }
    }

    static void UpdateRoadCellAttributes(CityCell cellComponent, GameObject roadTile, Zone.ZoneType zoneType)
    {
        cellComponent.zoneType = zoneType;
        cellComponent.prefab = roadTile;
        cellComponent.prefabName = roadTile.name;
        cellComponent.buildingType = "Road";
        cellComponent.powerPlant = null;
        cellComponent.population = 0;
        cellComponent.powerConsumption = 0;
        cellComponent.happiness = 0;
        cellComponent.isPivot = false;
    }

    static void ApplyRoadRouteHintsFromResolved(CityCell cell, ResolvedRoadTile resolved)
    {
        if (cell == null) return;
        cell.hasRoadSegmentPrevHint = resolved.hasSegmentPrevHint;
        cell.roadSegmentPrevGrid = resolved.segmentPrevGridPos;
        cell.hasRoadSegmentNextHint = resolved.hasSegmentNextHint;
        cell.roadSegmentNextGrid = resolved.segmentNextGridPos;
        cell.roadRouteEntryStep = resolved.routeEntryStep;
        cell.roadRouteExitStep = resolved.routeExitStep;
        cell.hasRoadRouteDirHints = resolved.hasSegmentPrevHint || resolved.hasSegmentNextHint
            || resolved.routeEntryStep != Vector2Int.zero || resolved.routeExitStep != Vector2Int.zero;
    }

    bool IsRoadAt(Vector2 gridPos)
    {
        var gridMgr = _grid as GridManager;
        if (gridMgr == null) return false;
        int gridX = Mathf.RoundToInt(gridPos.x), gridY = Mathf.RoundToInt(gridPos.y);
        if (gridX < 0 || gridX >= gridMgr.width || gridY < 0 || gridY >= gridMgr.height) return false;
        var cell = gridMgr.GetGridCell(new Vector2(gridX, gridY));
        if (cell == null || cell.transform.childCount == 0) return false;
        var cellComponent = gridMgr.GetCell(gridX, gridY);
        if (cellComponent != null && cellComponent.zoneType == Zone.ZoneType.Road) return true;
        for (int i = 0; i < cell.transform.childCount; i++)
        {
            var zone = cell.transform.GetChild(i).GetComponent<Zone>();
            if (zone != null && zone.zoneType == Zone.ZoneType.Road) return true;
        }
        return false;
    }

    bool IsBridgeDeckRoadPrefabInternal(GameObject prefab)
    {
        if (prefab == null || _roadMgr == null) return false;
        return prefab == _roadMgr.roadTileBridgeHorizontal || prefab == _roadMgr.roadTileBridgeVertical;
    }

    bool IsCellUsingBridgeDeckRoadPrefab(Vector2Int gridPos)
    {
        GameObject cell = _grid.GetGridCell(new Vector2(gridPos.x, gridPos.y));
        if (cell == null) return false;
        for (int i = 0; i < cell.transform.childCount; i++)
        {
            Transform t = cell.transform.GetChild(i);
            Zone z = t.GetComponent<Zone>();
            if (z == null || z.zoneType != Zone.ZoneType.Road) continue;
            string n2 = t.gameObject.name;
            if (_roadMgr != null && _roadMgr.roadTileBridgeHorizontal != null && n2.StartsWith(_roadMgr.roadTileBridgeHorizontal.name)) return true;
            if (_roadMgr != null && _roadMgr.roadTileBridgeVertical != null && n2.StartsWith(_roadMgr.roadTileBridgeVertical.name)) return true;
            return false;
        }
        return false;
    }

    GameObject GetCorrectRoadPrefabInternal(Vector2 prevGridPos, Vector2 currGridPos)
    {
        EnsureResolver();
        if (_resolver == null) return _roadMgr?.roadTilePrefab1;
        var resolved = _resolver.ResolveForCell(currGridPos, prevGridPos);
        return resolved.HasValue ? resolved.Value.prefab : _roadMgr?.roadTilePrefab1;
    }

    void EnsureResolver()
    {
        if (_resolver != null) return;
        var grid = _grid as GridManager;
        var terrain = _terrain as TerrainManager;
        var roads = _roadMgr as RoadManager;
        if (grid != null && terrain != null && roads != null)
            _resolver = new RoadPrefabResolver(grid, terrain, roads);
    }

    Vector2 GetRoadTileWorldPosition(int x, int y, GameObject prefab, int terrainHeight)
    {
        if (terrainHeight == 0) return _grid.GetWorldPositionVector(x, y, 1);
        if (!IsDiagonalRoadPrefab(prefab)) return _grid.GetWorldPosition(x, y);
        int upperX = x, upperY = y;
        Vector2? slopeDir = GetTerrainSlopeDirection(new Vector2(x, y), terrainHeight);
        if (slopeDir.HasValue) { upperX = x + Mathf.RoundToInt(slopeDir.Value.x); upperY = y + Mathf.RoundToInt(slopeDir.Value.y); }
        else if (_terrain != null)
        {
            TerrainSlopeType slopeType = _terrain.GetTerrainSlopeTypeAt(x, y);
            switch (slopeType)
            {
                case TerrainSlopeType.SouthEast: upperX = x + 1; upperY = y + 1; break;
                case TerrainSlopeType.SouthWest: upperX = x + 1; upperY = y - 1; break;
                case TerrainSlopeType.NorthEast: upperX = x - 1; upperY = y + 1; break;
                case TerrainSlopeType.NorthWest: upperX = x - 1; upperY = y - 1; break;
                case TerrainSlopeType.SouthEastUp: case TerrainSlopeType.SouthWestUp: upperX = x + 1; upperY = y; break;
                case TerrainSlopeType.NorthEastUp: case TerrainSlopeType.NorthWestUp: upperX = x - 1; upperY = y; break;
                default: return _grid.GetWorldPosition(x, y);
            }
        }
        else return _grid.GetWorldPosition(x, y);
        return _grid.GetWorldPositionVector(upperX, upperY, terrainHeight);
    }

    bool IsDiagonalRoadPrefab(GameObject prefab)
    {
        if (prefab == null || _roadMgr == null) return false;
        return prefab == _roadMgr.roadTilePrefabElbowUpLeft || prefab == _roadMgr.roadTilePrefabElbowUpRight
            || prefab == _roadMgr.roadTilePrefabElbowDownLeft || prefab == _roadMgr.roadTilePrefabElbowDownRight;
    }

    Vector2? GetTerrainSlopeDirection(Vector2 currGridPos, int currentHeight)
    {
        if (currentHeight == 0) return null;
        int x = (int)currGridPos.x, y = (int)currGridPos.y;
        Vector2? directionToHigher = null;
        for (int i = 0; i < 4; i++)
        {
            int nx = x + DirX[i], ny = y + DirY[i];
            if (nx < 0 || ny < 0) continue;
            CityCell c = _grid.GetCell(nx, ny);
            if (c == null) continue;
            int nh = c.GetCellInstanceHeight();
            if (nh - currentHeight == 1) directionToHigher = new Vector2(DirX[i], DirY[i]);
        }
        if (!directionToHigher.HasValue) return null;
        int dxi = Mathf.RoundToInt(directionToHigher.Value.x), dyi = Mathf.RoundToInt(directionToHigher.Value.y);
        return ((Mathf.Abs(dxi) == 1 && dyi == 0) || (dxi == 0 && Mathf.Abs(dyi) == 1))
            ? (Vector2?)directionToHigher.Value : null;
    }

    Vector2 PickPrevGridPosForRoadRefresh(Vector2 gridPos, CityCell cellOrNull)
    {
        if (cellOrNull != null && TryPrevGridPosFromStoredRoadSegment(gridPos, cellOrNull, out Vector2 hinted)) return hinted;
        return PickPrevGridPosForRoadRefreshConnectivityOnly(gridPos);
    }

    bool TryPrevGridPosFromStoredRoadSegment(Vector2 gridPos, CityCell cell, out Vector2 prev)
    {
        prev = gridPos;
        Vector2 hint;
        if (cell.hasRoadSegmentPrevHint) hint = new Vector2(cell.roadSegmentPrevGrid.x, cell.roadSegmentPrevGrid.y);
        else if (cell.hasRoadRouteDirHints && (Mathf.Abs(cell.roadRouteEntryStep.x) + Mathf.Abs(cell.roadRouteEntryStep.y) == 1))
            hint = gridPos - new Vector2(cell.roadRouteEntryStep.x, cell.roadRouteEntryStep.y);
        else return false;

        int gx = (int)gridPos.x, gy = (int)gridPos.y;
        int hx = Mathf.RoundToInt(hint.x), hy = Mathf.RoundToInt(hint.y);
        if (Mathf.Abs(gx - hx) + Mathf.Abs(gy - hy) != 1) { cell.ClearRoadRouteHints(); return false; }
        if (!IsRoadAt(hint)) { cell.ClearRoadRouteHints(); return false; }

        bool rW = IsRoadAt(gridPos + new Vector2(-1, 0)), rE = IsRoadAt(gridPos + new Vector2(1, 0));
        bool rN = IsRoadAt(gridPos + new Vector2(0, 1)), rS = IsRoadAt(gridPos + new Vector2(0, -1));
        bool straH = rW && rE && !rN && !rS, straV = rN && rS && !rW && !rE;
        if (straH) { if (hint == gridPos + new Vector2(-1, 0) || hint == gridPos + new Vector2(1, 0)) { prev = hint; return true; } }
        else if (straV) { if (hint == gridPos + new Vector2(0, 1) || hint == gridPos + new Vector2(0, -1)) { prev = hint; return true; } }
        cell.ClearRoadRouteHints();
        return false;
    }

    Vector2 PickPrevGridPosForRoadRefreshConnectivityOnly(Vector2 gridPos)
    {
        bool rW = IsRoadAt(gridPos + new Vector2(-1, 0)), rE = IsRoadAt(gridPos + new Vector2(1, 0));
        bool rN = IsRoadAt(gridPos + new Vector2(0, 1)), rS = IsRoadAt(gridPos + new Vector2(0, -1));
        int hc = (rW ? 1 : 0) + (rE ? 1 : 0), vc = (rN ? 1 : 0) + (rS ? 1 : 0);
        int total = hc + vc;
        if (total == 0) return gridPos;
        if (total == 1) { if (rW) return gridPos + new Vector2(-1, 0); if (rE) return gridPos + new Vector2(1, 0); if (rN) return gridPos + new Vector2(0, 1); return gridPos + new Vector2(0, -1); }
        if (hc == 2 && vc <= 1) { if (rW) return gridPos + new Vector2(-1, 0); return gridPos + new Vector2(1, 0); }
        if (vc == 2 && hc <= 1) { if (rN) return gridPos + new Vector2(0, 1); return gridPos + new Vector2(0, -1); }
        var candidates = new List<Vector2>(4);
        if (rW) candidates.Add(gridPos + new Vector2(-1, 0));
        if (rE) candidates.Add(gridPos + new Vector2(1, 0));
        if (rN) candidates.Add(gridPos + new Vector2(0, 1));
        if (rS) candidates.Add(gridPos + new Vector2(0, -1));
        candidates.Sort((a, b) => { int c = a.x.CompareTo(b.x); return c != 0 ? c : a.y.CompareTo(b.y); });
        return candidates[0];
    }

    void InvalidateRoadRouteHintsIfTopologyMismatch(Vector2 gridPos, CityCell cell)
    {
        if (cell == null || !cell.hasRoadRouteDirHints) return;
        bool rW = IsRoadAt(gridPos + new Vector2(-1, 0)), rE = IsRoadAt(gridPos + new Vector2(1, 0));
        bool rN = IsRoadAt(gridPos + new Vector2(0, 1)), rS = IsRoadAt(gridPos + new Vector2(0, -1));
        int c2 = (rW ? 1 : 0) + (rE ? 1 : 0) + (rN ? 1 : 0) + (rS ? 1 : 0);
        bool straH = rW && rE && !rN && !rS, straV = rN && rS && !rW && !rE, dead = c2 == 1;
        if (straH || straV)
        {
            if (cell.hasRoadSegmentNextHint && !IsRoadAt(new Vector2(cell.roadSegmentNextGrid.x, cell.roadSegmentNextGrid.y)))
                cell.ClearRoadRouteHints();
            return;
        }
        if (dead) return;
        cell.ClearRoadRouteHints();
    }

    // -----------------------------------------------------------------------
    // Private — bridge validation helpers
    // -----------------------------------------------------------------------
    bool ValidateFeat44WaterBridgeRules(List<Vector2> path, HeightMap heightMap, bool postUserWarnings)
    {
        if (path == null || path.Count < 2 || heightMap == null || _grid == null) return true;
        if (!TryGetSingleBridgeRunBounds(path, heightMap, out int runStart, out int runEnd)) return true;

        WaterManager waterManager = ResolveWaterManager();
        var pathStrokeCells = BuildPathGridCellSet(path);
        var straight = BresenhamStraightLine((int)path[runStart].x, (int)path[runStart].y, (int)path[runEnd].x, (int)path[runEnd].y);

        foreach (var p in straight)
        {
            int px = (int)p.x, py = (int)p.y;
            if (!heightMap.IsValidPosition(px, py)) return FailFeat44(postUserWarnings, "Bridge span leaves the map.");
            var gp = new Vector2Int(px, py);
            if (!IsWaterRelatedBridgeInteriorCell(px, py, heightMap, waterManager) && !pathStrokeCells.Contains(gp))
                return FailFeat44(postUserWarnings, "Bridges may only cross registered water and shore, not dry gaps.");
        }

        int landBefore = runStart - 1, landAfter = runEnd + 1;
        if (landBefore < 0 || landAfter >= path.Count) return FailFeat44(postUserWarnings, "A water bridge needs land at both ends.");
        if (IsWaterOrWaterSlope((int)path[landBefore].x, (int)path[landBefore].y, heightMap)
            || IsWaterOrWaterSlope((int)path[landAfter].x, (int)path[landAfter].y, heightMap))
            return FailFeat44(postUserWarnings, "A water bridge needs land at both ends.");

        CityCell cellBefore = _grid.GetCell((int)path[landBefore].x, (int)path[landBefore].y);
        CityCell cellAfter = _grid.GetCell((int)path[landAfter].x, (int)path[landAfter].y);
        if (cellBefore == null || cellAfter == null) return false;
        int bridgeHeight = cellBefore.GetCellInstanceHeight();
        if (cellAfter.GetCellInstanceHeight() != bridgeHeight) return FailFeat44(postUserWarnings, "Bridge ends must be at the same terrain height.");

        bool spanHorizontal = (int)path[runStart].y == (int)path[runEnd].y;
        for (int i = runStart; i <= runEnd; i++)
        {
            int x = (int)path[i].x, y = (int)path[i].y;
            if (!IsOpenWaterCellForBridge(x, y, heightMap, waterManager)) continue;
            int surfaceS = waterManager != null ? waterManager.GetWaterSurfaceHeight(x, y) : -1;
            if (surfaceS < 0) surfaceS = heightMap.GetHeight(x, y);
            int bed = heightMap.GetHeight(x, y);
            if (bridgeHeight < surfaceS || bridgeHeight < bed)
                return FailFeat44(postUserWarnings, "Bridge deck would sit below the water surface or bed here.");
        }

        for (int i = runStart; i <= runEnd; i++)
        {
            int x = (int)path[i].x, y = (int)path[i].y;
            if (!IsWaterOrWaterSlope(x, y, heightMap)) continue;
            if (!IsRoadAt(new Vector2(x, y))) continue;
            if (!TryGetDominantRoadAxisAt(x, y, out bool existingHorizontal, out bool isJunction)) continue;
            if (isJunction) return FailFeat44(postUserWarnings, "Crossing or joining bridge spans here is not supported.");
            if (existingHorizontal != spanHorizontal) return FailFeat44(postUserWarnings, "Another bridge already crosses this water at a different angle.");
        }
        return true;
    }

    bool IsOpenWaterCellForBridge(int x, int y, HeightMap heightMap, WaterManager waterManager)
    {
        if (heightMap == null || !heightMap.IsValidPosition(x, y)) return false;
        return waterManager != null && waterManager.IsWaterAt(x, y);
    }

    bool IsWaterRelatedBridgeInteriorCell(int x, int y, HeightMap heightMap, WaterManager waterManager)
    {
        if (!IsWaterOrWaterSlope(x, y, heightMap)) return false;
        if (IsOpenWaterCellForBridge(x, y, heightMap, waterManager)) return true;
        if (_terrain != null && _terrain.IsWaterSlopeCell(x, y))
        {
            if (waterManager == null) return true;
            int[] dx = { 1, -1, 0, 0, 1, 1, -1, -1 };
            int[] dy = { 0, 0, 1, -1, 1, -1, 1, -1 };
            for (int d = 0; d < 8; d++) { if (waterManager.IsWaterAt(x + dx[d], y + dy[d])) return true; }
            return false;
        }
        return true;
    }

    bool TryGetSingleBridgeRunBounds(List<Vector2> path, HeightMap heightMap, out int runStart, out int runEnd)
    {
        runStart = -1; runEnd = -1;
        int runs = 0; bool inRun = false; int rs = -1, re = -1;
        for (int i = 0; i < path.Count; i++)
        {
            bool w = IsWaterOrWaterSlope((int)path[i].x, (int)path[i].y, heightMap);
            if (w) { if (!inRun) { runs++; rs = i; inRun = true; } re = i; } else inRun = false;
        }
        if (runs != 1 || rs < 0) return false;
        runStart = rs; runEnd = re;
        return runEnd >= runStart;
    }

    bool TryGetDominantRoadAxisAt(int x, int y, out bool horizontal, out bool isJunction)
    {
        horizontal = false; isJunction = false;
        bool L = IsRoadAt(new Vector2(x - 1, y)), R = IsRoadAt(new Vector2(x + 1, y));
        bool U = IsRoadAt(new Vector2(x, y + 1)), D = IsRoadAt(new Vector2(x, y - 1));
        bool hConn = L || R, vConn = U || D;
        if (hConn && vConn) { isJunction = true; return true; }
        if (hConn) { horizontal = true; return true; }
        if (vConn) { horizontal = false; return true; }
        return false;
    }

    bool HasTurnOnWaterOrCoast(List<Vector2> path, HeightMap heightMap)
    {
        if (path == null || path.Count < 3 || heightMap == null) return false;
        for (int i = 1; i < path.Count - 1; i++)
        {
            int dxIn = (int)(path[i].x - path[i - 1].x), dyIn = (int)(path[i].y - path[i - 1].y);
            int dxOut = (int)(path[i + 1].x - path[i].x), dyOut = (int)(path[i + 1].y - path[i].y);
            if ((dxIn != dxOut || dyIn != dyOut) && IsWaterOrWaterSlope((int)path[i].x, (int)path[i].y, heightMap))
                return true;
        }
        return false;
    }

    bool HasElbowTooCloseToWater(List<Vector2> path, HeightMap heightMap, bool relaxElbowNearWaterForWaterBridge = false)
    {
        if (path == null || path.Count < 3 || heightMap == null) return false;
        for (int i = 1; i < path.Count - 1; i++)
        {
            int dxIn = (int)(path[i].x - path[i - 1].x), dyIn = (int)(path[i].y - path[i - 1].y);
            int dxOut = (int)(path[i + 1].x - path[i].x), dyOut = (int)(path[i + 1].y - path[i].y);
            if (dxIn != dxOut || dyIn != dyOut)
            {
                int x = (int)path[i].x, y = (int)path[i].y;
                if (relaxElbowNearWaterForWaterBridge && IsElbowExemptWaterBridgeNearWaterRelaxation(i, path, heightMap)) continue;
                if (!IsAtLeastTwoCellsFromWater(x, y, heightMap)) return true;
            }
        }
        return false;
    }

    bool IsElbowExemptWaterBridgeNearWaterRelaxation(int elbowIndex, List<Vector2> path, HeightMap heightMap)
    {
        if (path == null || elbowIndex < 1 || elbowIndex >= path.Count - 1 || heightMap == null || _grid == null || _terrain == null) return false;
        WaterManager wm = ResolveWaterManager();
        int cx = (int)path[elbowIndex].x, cy = (int)path[elbowIndex].y;
        if (IsWaterOrWaterSlope(cx, cy, heightMap)) return false;
        CityCell lipCell = _grid.GetCell(cx, cy);
        if (lipCell != null) { int h = lipCell.GetCellInstanceHeight(); if (h > 0 && CellQualifiesForDeckDisplayLip(cx, cy, h, heightMap, wm)) return true; }
        int wetDist = int.MaxValue;
        for (int j = 0; j < path.Count; j++)
        {
            int px = (int)path[j].x, py = (int)path[j].y;
            if (!IsWaterOrWaterSlope(px, py, heightMap)) continue;
            int d = Mathf.Max(Mathf.Abs(px - cx), Mathf.Abs(py - cy));
            if (d < wetDist) wetDist = d;
        }
        return wetDist != int.MaxValue && wetDist <= 2;
    }

    bool IsAtLeastTwoCellsFromWater(int x, int y, HeightMap heightMap)
    {
        if (heightMap == null || !heightMap.IsValidPosition(x, y) || _terrain == null) return false;
        if (_terrain.IsRegisteredOpenWaterAt(x, y) || _terrain.IsWaterSlopeCell(x, y)) return false;
        int[] dx = { 0, 1, -1, 0, 0 }, dy = { 0, 0, 0, 1, -1 };
        for (int d = 0; d < 5; d++)
        {
            int nx = x + dx[d], ny = y + dy[d];
            if (!heightMap.IsValidPosition(nx, ny)) continue;
            if (_terrain.IsRegisteredOpenWaterAt(nx, ny) || _terrain.IsWaterSlopeCell(nx, ny)) return false;
        }
        return true;
    }

    bool HasTurnOnLastLandCellsBeforeWater(List<Vector2> path, HeightMap heightMap, int n = 2)
    {
        if (path == null || path.Count < n + 2 || heightMap == null || n < 1) return false;
        for (int i = 1; i < path.Count; i++)
        {
            if (!IsWaterOrWaterSlope((int)path[i].x, (int)path[i].y, heightMap)) continue;
            int firstWater = i, landEnd = i - 1, landStart = i - n;
            if (landStart < 0) continue;
            Vector2Int dirApproach = new Vector2Int((int)path[firstWater].x - (int)path[landEnd].x, (int)path[firstWater].y - (int)path[landEnd].y);
            if (dirApproach.x == 0 && dirApproach.y == 0) continue;
            for (int j = landStart; j < landEnd; j++)
            {
                Vector2Int dir = new Vector2Int((int)path[j + 1].x - (int)path[j].x, (int)path[j + 1].y - (int)path[j].y);
                if (dir.x != dirApproach.x || dir.y != dirApproach.y) return true;
            }
            while (i < path.Count && IsWaterOrWaterSlope((int)path[i].x, (int)path[i].y, heightMap)) i++;
            i--;
        }
        return false;
    }

    bool IsBridgePathValid(List<Vector2> path, HeightMap heightMap)
    {
        if (path == null || path.Count < 2 || heightMap == null) return true;
        var pathCells = BuildPathGridCellSet(path);
        int runCount = 0; bool inRun = false;
        for (int i = 0; i < path.Count; i++)
        {
            bool isWet = IsWaterOrWaterSlope((int)path[i].x, (int)path[i].y, heightMap);
            if (isWet && !inRun) { runCount++; inRun = true; } else if (!isWet) inRun = false;
        }
        if (runCount > 1 && !WetStrokeCellsFormSingleAxisSpanFullyCovered(path, heightMap, pathCells)) return false;

        inRun = false; int runStart = -1, runEnd = -1;
        for (int i = 0; i < path.Count; i++)
        {
            bool isWet = IsWaterOrWaterSlope((int)path[i].x, (int)path[i].y, heightMap);
            if (isWet && !inRun) { runStart = i; runEnd = i; inRun = true; }
            else if (isWet) runEnd = i;
            else
            {
                if (inRun && runEnd > runStart && !ValidateBridgeSegmentAxis(path, runStart, runEnd, heightMap, pathCells)) return false;
                inRun = false;
            }
        }
        if (inRun && runEnd > runStart && !ValidateBridgeSegmentAxis(path, runStart, runEnd, heightMap, pathCells)) return false;
        return true;
    }

    bool ValidateBridgeSegmentAxis(List<Vector2> path, int runStart, int runEnd, HeightMap heightMap, HashSet<Vector2Int> pathCells)
    {
        int x0 = (int)path[runStart].x, y0 = (int)path[runStart].y;
        int x1 = (int)path[runEnd].x, y1 = (int)path[runEnd].y;
        if (x0 != x1 && y0 != y1) return false;
        var straight = BresenhamStraightLine(x0, y0, x1, y1);
        foreach (var p in straight)
        {
            int px = (int)p.x, py = (int)p.y;
            if (!heightMap.IsValidPosition(px, py)) return false;
            if (!IsWaterOrWaterSlope(px, py, heightMap) && !pathCells.Contains(new Vector2Int(px, py))) return false;
        }
        return true;
    }

    bool WetStrokeCellsFormSingleAxisSpanFullyCovered(List<Vector2> path, HeightMap heightMap, HashSet<Vector2Int> pathStrokeCells)
    {
        if (path == null || heightMap == null || pathStrokeCells == null) return false;
        var distinctY = new HashSet<int>(); var distinctX = new HashSet<int>();
        int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
        bool anyWet = false;
        for (int i = 0; i < path.Count; i++)
        {
            int x = (int)path[i].x, y = (int)path[i].y;
            if (!IsWaterOrWaterSlope(x, y, heightMap)) continue;
            anyWet = true; distinctY.Add(y); distinctX.Add(x);
            if (x < minX) minX = x; if (x > maxX) maxX = x;
            if (y < minY) minY = y; if (y > maxY) maxY = y;
        }
        if (!anyWet) return true;
        if (distinctY.Count > 1 && distinctX.Count > 1) return false;
        if (distinctY.Count == 1)
        {
            int y = minY;
            for (int x = minX; x <= maxX; x++) { if (!heightMap.IsValidPosition(x, y)) return false; if (!IsWaterOrWaterSlope(x, y, heightMap) && !pathStrokeCells.Contains(new Vector2Int(x, y))) return false; }
            return true;
        }
        if (distinctX.Count == 1)
        {
            int x = minX;
            for (int y = minY; y <= maxY; y++) { if (!heightMap.IsValidPosition(x, y)) return false; if (!IsWaterOrWaterSlope(x, y, heightMap) && !pathStrokeCells.Contains(new Vector2Int(x, y))) return false; }
            return true;
        }
        return false;
    }

    bool IsPathFullyAdjacent(List<Vector2> path)
    {
        if (path == null || path.Count < 2) return true;
        for (int i = 1; i < path.Count; i++)
        {
            int dx = Mathf.Abs((int)path[i].x - (int)path[i - 1].x);
            int dy = Mathf.Abs((int)path[i].y - (int)path[i - 1].y);
            if (dx > 1 || dy > 1) return false;
        }
        return true;
    }

    // -----------------------------------------------------------------------
    // Private — static helpers
    // -----------------------------------------------------------------------
    static List<Vector2> GetCardinalPath(Vector2 from, Vector2 to)
    {
        var path = new List<Vector2>();
        int x0 = (int)from.x, y0 = (int)from.y;
        int x1 = (int)to.x, y1 = (int)to.y;
        int sx = x1 > x0 ? 1 : (x1 < x0 ? -1 : 0);
        int sy = y1 > y0 ? 1 : (y1 < y0 ? -1 : 0);
        for (int x = x0 + sx; sx != 0 && x != x1 + sx; x += sx)
            path.Add(new Vector2(x, y0));
        int lastX = path.Count > 0 ? (int)path[path.Count - 1].x : x0;
        for (int y = y0 + sy; sy != 0 && y != y1 + sy; y += sy)
            path.Add(new Vector2(lastX, y));
        return path;
    }

    List<Vector2> StraightenBridgeSegments(List<Vector2> path, HeightMap heightMap)
    {
        if (path == null || path.Count < 2 || heightMap == null) return path;
        var result = new List<Vector2>();
        int i = 0;
        while (i < path.Count)
        {
            int x = (int)path[i].x, y = (int)path[i].y;
            if (!IsWaterOrWaterSlope(x, y, heightMap)) { result.Add(path[i]); i++; continue; }
            int j = i;
            while (j < path.Count) { int xj = (int)path[j].x, yj = (int)path[j].y; if (!IsWaterOrWaterSlope(xj, yj, heightMap)) break; j++; }
            j--;
            if (j > i)
            {
                int x0 = (int)path[i].x, y0 = (int)path[i].y, x1 = (int)path[j].x, y1 = (int)path[j].y;
                List<Vector2> straight;
                if (x0 == x1 || y0 == y1) { straight = BresenhamStraightLine(x0, y0, x1, y1); }
                else
                {
                    int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
                    int ex, ey;
                    straight = new List<Vector2>();
                    if (dx >= dy) { int sx = x1 > x0 ? 1 : -1; ex = x0; ey = y0; while (true) { if (!heightMap.IsValidPosition(ex, ey)) break; straight.Add(new Vector2(ex, ey)); if (!IsWaterOrWaterSlope(ex, ey, heightMap)) break; if (ex == x1) break; ex += sx; } }
                    else { int sy = y1 > y0 ? 1 : -1; ex = x0; ey = y0; while (true) { if (!heightMap.IsValidPosition(ex, ey)) break; straight.Add(new Vector2(ex, ey)); if (!IsWaterOrWaterSlope(ex, ey, heightMap)) break; if (ey == y1) break; ey += sy; } }
                }
                foreach (var p in straight) result.Add(p);
                if (j + 1 < path.Count)
                {
                    Vector2 bridgeEnd = straight[straight.Count - 1], nextLand = path[j + 1];
                    if (Mathf.Abs((int)bridgeEnd.x - (int)nextLand.x) > 1 || Mathf.Abs((int)bridgeEnd.y - (int)nextLand.y) > 1)
                    { foreach (var p in GetCardinalPath(bridgeEnd, nextLand)) result.Add(p); i = j + 2; continue; }
                }
            }
            else { result.Add(path[i]); }
            i = j + 1;
        }
        return result;
    }

    static List<Vector2> BresenhamStraightLine(int x0, int y0, int x1, int y1)
    {
        var line = new List<Vector2>();
        int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        while (true)
        {
            line.Add(new Vector2(x0, y0));
            if (x0 == x1 && y0 == y1) break;
            int e2 = err * 2;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }
        return line;
    }

    static HashSet<Vector2Int> BuildPathGridCellSet(List<Vector2> path)
    {
        var set = new HashSet<Vector2Int>();
        if (path == null) return set;
        for (int i = 0; i < path.Count; i++) set.Add(new Vector2Int(Mathf.RoundToInt(path[i].x), Mathf.RoundToInt(path[i].y)));
        return set;
    }

    static List<Vector2> SubListCopy(List<Vector2> full, int count)
    {
        var p = new List<Vector2>(count);
        for (int i = 0; i < count; i++) p.Add(full[i]);
        return p;
    }

    static void AppendPathSkipDuplicateJoin(List<Vector2> acc, List<Vector2> tail)
    {
        if (tail == null || tail.Count == 0) return;
        int start = 0;
        if (acc.Count > 0) { var a = acc[acc.Count - 1]; var b = tail[0]; if ((int)a.x == (int)b.x && (int)a.y == (int)b.y) start = 1; }
        for (int i = start; i < tail.Count; i++) acc.Add(tail[i]);
    }

    static void AppendStraightSuffixAlongProgrammaticChord(List<Vector2> merged, List<Vector2> straightPath, Vector2Int segmentDir)
    {
        if (merged == null || straightPath == null || merged.Count == 0 || straightPath.Count == 0) return;
        Vector2 end = merged[merged.Count - 1];
        int idx = -1;
        for (int i = 0; i < straightPath.Count; i++) { if ((int)straightPath[i].x == (int)end.x && (int)straightPath[i].y == (int)end.y) { idx = i; break; } }
        if (idx < 0 || idx >= straightPath.Count - 1) return;
        for (int i = idx + 1; i < straightPath.Count; i++)
        {
            Vector2 prev = merged[merged.Count - 1], cur = straightPath[i];
            if ((int)cur.x != (int)prev.x + segmentDir.x || (int)cur.y != (int)prev.y + segmentDir.y) break;
            merged.Add(cur);
        }
    }

    static int FindLockedChordEndIndexInMergedPath(List<Vector2> mergedPath, List<Vector2> chord)
    {
        if (mergedPath == null || chord == null || chord.Count < 2 || mergedPath.Count < chord.Count) return -1;
        int m = mergedPath.Count, n = chord.Count;
        for (int s = 0; s <= m - n; s++)
        {
            bool match = true;
            for (int j = 0; j < n; j++) { if ((int)mergedPath[s + j].x != (int)chord[j].x || (int)mergedPath[s + j].y != (int)chord[j].y) { match = false; break; } }
            if (match) return s + n - 1;
        }
        return -1;
    }

    static bool FailFeat44(bool postUserWarnings, string message)
    {
        if (postUserWarnings && GameNotificationManager.Instance != null && !string.IsNullOrEmpty(message))
            GameNotificationManager.Instance.PostWarning(message);
        return false;
    }

    // -----------------------------------------------------------------------
    // Private — dependency resolution helpers
    // -----------------------------------------------------------------------
    WaterManager ResolveWaterManager()
    {
        var terrainMgr = _terrain as TerrainManager;
        if (terrainMgr != null && terrainMgr.waterManager != null) return terrainMgr.waterManager;
        return Object.FindObjectOfType<WaterManager>();
    }

    InterstateManager GetInterstateManager()
    {
        return Object.FindObjectOfType<InterstateManager>();
    }

    public List<GameObject> GetRoadPrefabs()
    {
        if (_roadMgr == null) return new List<GameObject>();
        return new List<GameObject>
        {
            _roadMgr.roadTilePrefab1, _roadMgr.roadTilePrefab2, _roadMgr.roadTilePrefabCrossing,
            _roadMgr.roadTilePrefabTIntersectionUp, _roadMgr.roadTilePrefabTIntersectionDown,
            _roadMgr.roadTilePrefabTIntersectionLeft, _roadMgr.roadTilePrefabTIntersectionRight,
            _roadMgr.roadTilePrefabElbowUpLeft, _roadMgr.roadTilePrefabElbowUpRight,
            _roadMgr.roadTilePrefabElbowDownLeft, _roadMgr.roadTilePrefabElbowDownRight,
            _roadMgr.roadTileBridgeVertical, _roadMgr.roadTileBridgeHorizontal,
            _roadMgr.roadTilePrefabEastSlope, _roadMgr.roadTilePrefabWestSlope,
            _roadMgr.roadTilePrefabNorthSlope, _roadMgr.roadTilePrefabSouthSlope
        };
    }
}
}
