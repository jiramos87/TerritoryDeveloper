using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System;
using Territory.Core;
using Territory.Terrain;
using Territory.Economy;
using Territory.UI;
using Territory.Zones;
using Territory.Utilities;

namespace Territory.Roads
{
/// <summary>
/// Shared terraform validation options for manual draw, interstate, and auto-road (same rules via <see cref="RoadManager.TryPrepareRoadPlacementPlan"/>).
/// </summary>
public struct RoadPathValidationContext
{
    /// <summary>When true (interstate), paths requiring cut-through hill flattening are invalid.</summary>
    public bool forbidCutThrough;
}

/// <summary>
/// Manages road placement, drawing, and prefab selection on the grid. Handles road preview
/// during drag, selects correct road prefab based on neighbor connectivity, and coordinates
/// with TerrainManager for slope adaptation and InterstateManager for highway connections.
/// Shared terraform validation: <see cref="TryPrepareRoadPlacementPlan"/> and <see cref="RoadPathValidationContext"/>.
/// </summary>
public class RoadManager : MonoBehaviour, IRoadManager
{
    #region Dependencies
    public TerrainManager terrainManager;
    public GridManager gridManager;
    public CityStats cityStats;
    public UIManager uiManager;
    public ZoneManager zoneManager;
    public InterstateManager interstateManager;
    public TerraformingService terraformingService;
    #endregion

    #region Road Drawing State
    private bool isDrawingRoad = false;
    private Vector2 startPosition;
    private static readonly int[] DirX = { 1, -1, 0, 0 };
    private static readonly int[] DirY = { 0, 0, 1, -1 };
    private RoadPrefabResolver roadPrefabResolver;
    private List<RoadPrefabResolver.ResolvedRoadTile> previewResolvedTiles = new List<RoadPrefabResolver.ResolvedRoadTile>();
    private HashSet<Vector2> placementPathPositions;
    /// <summary>Last grid cell under cursor during manual road drag (for mouse-up placement when release cell is invalid).</summary>
    private Vector2 currentDrawCursorGrid;
    /// <summary>Speeds longest-prefix search during drag: last accepted <b>raw</b> path prefix length (see <see cref="TryPrepareRoadPlacementPlanLongestValidPrefix"/>).</summary>
    private int manualRoadLongestPrefixHint;
    /// <summary>When true, manual preview keeps a fixed lip-to-exit chord; post-exit extension follows <see cref="GetLine"/> from the exit cell.</summary>
    private bool manualPreviewBridgeLocked;
    private Vector2Int lockedBridgeLip;
    /// <summary>Waterward cardinal from the lip (same sign as relaxed deck normal).</summary>
    private Vector2Int lockedBridgeNormal;
    private List<Vector2> lockedBridgeChord;
    private readonly List<Vector2> manualStrokeLipCandidateScratch = new List<Vector2>();
    #endregion

    #region Road Prefabs
    public List<GameObject> roadTilePrefabs;
    public GameObject roadTilePrefab1;
    public GameObject roadTilePrefab2;
    public GameObject roadTilePrefabCrossing;
    public GameObject roadTilePrefabTIntersectionUp;
    public GameObject roadTilePrefabTIntersectionDown;
    public GameObject roadTilePrefabTIntersectionLeft;
    public GameObject roadTilePrefabTIntersectionRight;
    public GameObject roadTilePrefabElbowUpLeft;
    public GameObject roadTilePrefabElbowUpRight;
    public GameObject roadTilePrefabElbowDownLeft;
    public GameObject roadTilePrefabElbowDownRight;
    public GameObject roadTileBridgeVertical;
    public GameObject roadTileBridgeHorizontal;
    public GameObject roadTilePrefabEastSlope;
    public GameObject roadTilePrefabWestSlope;
    public GameObject roadTilePrefabNorthSlope;
    public GameObject roadTilePrefabSouthSlope;
    private List<GameObject> previewRoadTiles = new List<GameObject>();
    private List<Vector2> previewRoadGridPositions = new List<Vector2>();

    /// <summary>
    /// Populates the road tile prefabs list from the individual prefab fields.
    /// </summary>
    public void Initialize()
    {
        if (gridManager != null && terrainManager != null)
            roadPrefabResolver = new RoadPrefabResolver(gridManager, terrainManager, this);
        roadTilePrefabs = new List<GameObject>
        {
            roadTilePrefab1,
            roadTilePrefab2,
            roadTilePrefabCrossing,
            roadTilePrefabTIntersectionUp,
            roadTilePrefabTIntersectionDown,
            roadTilePrefabTIntersectionLeft,
            roadTilePrefabTIntersectionRight,
            roadTilePrefabElbowUpLeft,
            roadTilePrefabElbowUpRight,
            roadTilePrefabElbowDownLeft,
            roadTilePrefabElbowDownRight,
            roadTileBridgeVertical,
            roadTileBridgeHorizontal,
            roadTilePrefabEastSlope,
            roadTilePrefabWestSlope,
            roadTilePrefabNorthSlope,
            roadTilePrefabSouthSlope
        };
    }
    #endregion

    /// <summary>
    /// True for horizontal/vertical bridge deck prefabs (do not re-resolve with <see cref="RefreshRoadPrefabAt"/> — would drop FEAT-44 deck height).
    /// </summary>
    bool IsBridgeDeckRoadPrefab(GameObject prefab)
    {
        if (prefab == null) return false;
        return prefab == roadTileBridgeHorizontal || prefab == roadTileBridgeVertical;
    }

    #region Road Drawing
    /// <summary>
    /// Handles the full road drawing input lifecycle: start on mouse down, preview line on drag, and place on mouse up.
    /// </summary>
    /// <param name="gridPosition">The current grid position under the cursor.</param>
    public void HandleRoadDrawing(Vector2 gridPosition)
    {
        Vector2 pos = new Vector2((int)gridPosition.x, (int)gridPosition.y);

        if (Input.GetMouseButtonUp(0) && isDrawingRoad)
        {
            isDrawingRoad = false;
            ClearPreview();
            if (!TryFinalizeManualRoadPlacement())
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
            if (gridManager == null || gridManager.cameraController == null || !gridManager.cameraController.WasLastRightClickAPan)
            {
                isDrawingRoad = false;
                ClearManualPreviewBridgeLock();
                ClearPreview();
                if (uiManager != null)
                    uiManager.RestoreGhostPreview();
            }
            return;
        }

        if (!terrainManager.CanPlaceRoad((int)pos.x, (int)pos.y, allowWaterSlopeForWaterBridgeTrace: true))
        {
            if (isDrawingRoad)
                ClearPreview();
            if (GameNotificationManager.Instance != null)
                GameNotificationManager.Instance.PostWarning("Cannot place road along this path.");
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (interstateManager != null && !interstateManager.CanPlaceStreetFrom(pos))
            {
                if (GameNotificationManager.Instance != null)
                    GameNotificationManager.Instance.PostWarning("Streets must connect to the Interstate Highway or existing connected roads.");
                return;
            }
            isDrawingRoad = true;
            ClearManualPreviewBridgeLock();
            startPosition = pos;
            currentDrawCursorGrid = pos;
            manualRoadLongestPrefixHint = 0;
            if (uiManager != null)
                uiManager.HideGhostPreview();
        }
        else if (isDrawingRoad && Input.GetMouseButton(0))
        {
            currentDrawCursorGrid = pos;
            ClearPreview();
            List<Vector2> path = GetManualRoadPathWithOptionalBridgeExtension();
            DrawPreviewLineCore(path);
        }
    }

    /// <summary>
    /// After preview terraform reverted: rebuild path from drag endpoints, validate, apply, place tiles, update economy. Streets allow cut-through.
    /// </summary>
    bool TryFinalizeManualRoadPlacement()
    {
        if (terraformingService == null || terrainManager == null || gridManager == null)
            return false;
        var heightMap = terrainManager.GetHeightMap();
        if (heightMap == null) return false;
        if (roadPrefabResolver == null)
            roadPrefabResolver = new RoadPrefabResolver(gridManager, terrainManager, this);
        if (roadPrefabResolver == null) return false;

        List<Vector2> path = GetManualRoadPathWithOptionalBridgeExtension();
        var manualCtx = new RoadPathValidationContext { forbidCutThrough = false };
        List<Vector2> expandedPath;
        PathTerraformPlan plan;
        if (!TryPrepareLockedDeckSpanBridgePlacement(path, manualCtx, out expandedPath, out plan)
            && !TryPrepareRoadPlacementPlanLongestValidPrefix(path, manualCtx, false, ref manualRoadLongestPrefixHint, out expandedPath, out plan, out _))
            return false;

        int tileCount = expandedPath.Count;
        int totalCost = CalculateTotalCost(tileCount);
        if (!cityStats.CanAfford(totalCost))
        {
            if (uiManager != null)
                uiManager.ShowInsufficientFundsTooltip("Road", totalCost);
            return false;
        }

        if (!plan.Apply(heightMap, terrainManager))
            return false;

        cityStats.RemoveMoney(totalCost);
        var resolved = roadPrefabResolver.ResolveForPath(expandedPath, plan);
        placementPathPositions = new HashSet<Vector2>();
        foreach (var r in resolved)
            placementPathPositions.Add(new Vector2(r.gridPos.x, r.gridPos.y));
        for (int i = 0; i < resolved.Count; i++)
        {
            PlaceRoadTileFromResolved(resolved[i]);
            UpdateAdjacentRoadPrefabsAt(new Vector2(resolved[i].gridPos.x, resolved[i].gridPos.y));
        }
        RefreshAllAdjacentRoadsOutsidePath();
        placementPathPositions = null;
        for (int i = 0; i < resolved.Count; i++)
        {
            if (IsBridgeDeckRoadPrefab(resolved[i].prefab))
                continue;
            RefreshRoadPrefabAt(new Vector2(resolved[i].gridPos.x, resolved[i].gridPos.y));
        }
        cityStats.AddPowerConsumption(resolved.Count * ZoneAttributes.Road.PowerConsumption);
        return true;
    }

    /// <summary>
    /// Scenario builder / batch tooling: commits a street <b>road stroke</b> through
    /// <see cref="TryPrepareRoadPlacementPlan"/> + <see cref="PathTerraformPlan.Apply"/> + resolve/place
    /// (same preparation family as manual placement), without affordability checks or money changes.
    /// </summary>
    /// <param name="pathRaw">Polyline in grid space (at least two cells).</param>
    /// <param name="error">Glossary-aligned reason when preparation or apply fails.</param>
    public bool TryCommitStreetStrokeForScenarioBuild(List<Vector2> pathRaw, out string error)
    {
        error = null;
        if (pathRaw == null || pathRaw.Count < 2)
        {
            error = "road stroke must list at least two cells";
            return false;
        }

        if (terraformingService == null || terrainManager == null || gridManager == null)
        {
            error = "road stroke apply failed: TerrainManager or GridManager not available";
            return false;
        }

        HeightMap heightMap = terrainManager.GetHeightMap();
        if (heightMap == null)
        {
            error = "road stroke apply failed: HeightMap missing";
            return false;
        }

        if (roadPrefabResolver == null)
            roadPrefabResolver = new RoadPrefabResolver(gridManager, terrainManager, this);
        if (roadPrefabResolver == null)
        {
            error = "road stroke apply failed: RoadPrefabResolver could not be created";
            return false;
        }

        var streetCtx = new RoadPathValidationContext { forbidCutThrough = false };
        if (!TryPrepareRoadPlacementPlan(pathRaw, streetCtx, false, out List<Vector2> expandedPath, out PathTerraformPlan plan))
        {
            error =
                "road stroke rejected by road preparation (TryPrepareRoadPlacementPlan): invalid wet run, shore band, cut-through, or Phase-1 height feasibility";
            return false;
        }

        if (!plan.Apply(heightMap, terrainManager))
        {
            error = "road stroke PathTerraformPlan.Apply failed (terrain / HeightMap)";
            return false;
        }

        List<RoadPrefabResolver.ResolvedRoadTile> resolved = roadPrefabResolver.ResolveForPath(expandedPath, plan);
        placementPathPositions = new HashSet<Vector2>();
        foreach (RoadPrefabResolver.ResolvedRoadTile r in resolved)
            placementPathPositions.Add(new Vector2(r.gridPos.x, r.gridPos.y));
        for (int i = 0; i < resolved.Count; i++)
        {
            PlaceRoadTileFromResolved(resolved[i]);
            UpdateAdjacentRoadPrefabsAt(new Vector2(resolved[i].gridPos.x, resolved[i].gridPos.y));
        }

        RefreshAllAdjacentRoadsOutsidePath();
        placementPathPositions = null;
        for (int i = 0; i < resolved.Count; i++)
        {
            if (IsBridgeDeckRoadPrefab(resolved[i].prefab))
                continue;
            RefreshRoadPrefabAt(new Vector2(resolved[i].gridPos.x, resolved[i].gridPos.y));
        }

        cityStats.AddPowerConsumption(resolved.Count * ZoneAttributes.Road.PowerConsumption);
        gridManager.InvalidateRoadCache();
        return true;
    }

    /// <summary>
    /// True if terraform plan is buildable under <paramref name="ctx"/> (e.g. interstate forbids cut-through).
    /// </summary>
    public bool ValidateTerraformPlanWithContext(PathTerraformPlan plan, RoadPathValidationContext ctx)
    {
        if (plan == null) return false;
        if (!plan.isValid) return false;
        if (ctx.forbidCutThrough && plan.isCutThrough) return false;
        return true;
    }

    /// <summary>
    /// Shared pipeline: filter cells, adjacency, bridge straightening/validation, cardinal expansion, <see cref="TerraformingService.ComputePathPlan"/>,
    /// context checks, and Phase-1 height validation (matches <see cref="PathTerraformPlan.Apply"/> feasibility). Does not apply terrain meshes.
    /// </summary>
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

    /// <summary>
    /// Like <see cref="TryPrepareRoadPlacementPlan"/> but keeps the longest <b>raw</b> path prefix for which the filtered path passes terraform + Phase-1 height checks.
    /// Tries <see cref="TryBuildFilteredPathForRoadPlan"/> on each prefix from longest to shortest so a failing tail (e.g. staircase into a second wet run) does not block preview of a valid bridge core.
    /// <paramref name="longestPrefixLengthHint"/> (manual drag): pass a ref field; on success it stores the <b>raw</b> prefix length (cell count from path start); reset to 0 on new stroke.
    /// Auto-road should pass a local int with value 0.
    /// </summary>
    /// <param name="filteredPathUsedOrNull">Filtered path (before diagonal expansion) that was accepted, or null on failure.</param>
    public bool TryPrepareRoadPlacementPlanLongestValidPrefix(List<Vector2> pathRaw, RoadPathValidationContext ctx, bool postUserWarnings, ref int longestPrefixLengthHint, out List<Vector2> expandedPath, out PathTerraformPlan plan, out List<Vector2> filteredPathUsedOrNull)
    {
        expandedPath = null;
        plan = null;
        filteredPathUsedOrNull = null;
        if (pathRaw == null || pathRaw.Count == 0)
            return false;

        HeightMap heightMap = terrainManager.GetHeightMap();
        if (heightMap == null)
            return false;

        int nRaw = pathRaw.Count;
        int startKRaw = Mathf.Min(nRaw, longestPrefixLengthHint > 0 ? longestPrefixLengthHint + 1 : nRaw);

        bool hasSlopeEligibleRawPrefix = terrainManager != null
            && RoadStrokeTerrainRules.TruncatePathAtFirstDisallowedLandSlope(pathRaw, terrainManager).Count > 0;

        for (int kRaw = startKRaw; kRaw >= 1; kRaw--)
        {
            List<Vector2> rawPrefix = SubListCopy(pathRaw, kRaw);
            if (!TryBuildFilteredPathForRoadPlan(rawPrefix, postUserWarnings, out List<Vector2> filteredPrefix, relaxElbowNearWaterForWaterBridge: !ctx.forbidCutThrough))
                continue;

            if (!IsBridgePathValid(filteredPrefix, heightMap))
                continue;
            if (!ValidateFeat44WaterBridgeRules(filteredPrefix, heightMap, postUserWarnings: false))
                continue;
            if (HasTurnOnWaterOrCoast(filteredPrefix, heightMap))
                continue;
            if (HasElbowTooCloseToWater(filteredPrefix, heightMap, relaxElbowNearWaterForWaterBridge: !ctx.forbidCutThrough))
                continue;
            if (!TryPrepareFromFilteredPathList(filteredPrefix, ctx, false, out expandedPath, out plan))
                continue;

            longestPrefixLengthHint = kRaw;
            filteredPathUsedOrNull = filteredPrefix;
            return true;
        }

        longestPrefixLengthHint = 0;
        if (postUserWarnings && hasSlopeEligibleRawPrefix && GameNotificationManager.Instance != null)
            GameNotificationManager.Instance.PostWarning("Road cannot extend further along this path. Terrain would exceed allowed height change.");
        return false;
    }

    /// <summary>
    /// True if the last cell of the stroke is dry land (not open water / water-slope) with positive instance height — expected exit for a completed water bridge.
    /// </summary>
    public bool StrokeLastCellIsFirmDryLand(IList<Vector2> stroke)
    {
        if (stroke == null || stroke.Count == 0 || terrainManager == null || gridManager == null)
            return false;
        HeightMap heightMap = terrainManager.GetHeightMap();
        if (heightMap == null)
            return false;
        int x = (int)stroke[stroke.Count - 1].x;
        int y = (int)stroke[stroke.Count - 1].y;
        if (!heightMap.IsValidPosition(x, y))
            return false;
        if (IsWaterOrWaterSlope(x, y, heightMap))
            return false;
        if (terrainManager.IsRegisteredOpenWaterAt(x, y))
            return false;
        Cell c = gridManager.GetCell(x, y);
        return c != null && c.GetCellInstanceHeight() > 0;
    }

    /// <summary>
    /// True if any grid cell on the stroke is open water, water-slope (shore), or otherwise treated as wet for bridge tracing (FEAT-44; high-deck first span may sit on shore).
    /// </summary>
    public bool StrokeHasWaterOrWaterSlopeCells(IList<Vector2> stroke)
    {
        if (stroke == null || stroke.Count == 0 || terrainManager == null)
            return false;
        HeightMap heightMap = terrainManager.GetHeightMap();
        if (heightMap == null)
            return false;
        for (int i = 0; i < stroke.Count; i++)
        {
            int x = (int)stroke[i].x, y = (int)stroke[i].y;
            if (IsWaterOrWaterSlope(x, y, heightMap))
                return true;
        }
        return false;
    }

    /// <summary>
    /// If the last cell is dry land and the next step along <paramref name="dir"/> is water or water-slope (shore), appends the FEAT-44 chord from that lip through wet to matching far dry land
    /// (<see cref="WalkStraightChordFromLipThroughWetToFarDry"/>). Used by <see cref="AutoRoadBuilder"/> so simulation strokes include the crossing before planning.
    /// </summary>
    public bool TryExtendCardinalStreetPathWithBridgeChord(List<Vector2> pathVec2, Vector2Int dir)
    {
        if (pathVec2 == null || pathVec2.Count < 1 || terraformingService == null || terrainManager == null || gridManager == null)
            return false;
        if (dir.x == 0 && dir.y == 0)
            return false;
        if (Mathf.Abs(dir.x) + Mathf.Abs(dir.y) != 1)
            return false;

        HeightMap heightMap = terrainManager.GetHeightMap();
        if (heightMap == null)
            return false;

        for (int i = 1; i < pathVec2.Count; i++)
        {
            int px = (int)pathVec2[i - 1].x, py = (int)pathVec2[i - 1].y;
            int cx = (int)pathVec2[i].x, cy = (int)pathVec2[i].y;
            if (cx - px != dir.x || cy - py != dir.y)
                return false;
        }

        int lx = (int)pathVec2[pathVec2.Count - 1].x;
        int ly = (int)pathVec2[pathVec2.Count - 1].y;
        int nx = lx + dir.x;
        int ny = ly + dir.y;
        if (!heightMap.IsValidPosition(nx, ny) || !IsWaterOrWaterSlope(nx, ny, heightMap))
            return false;

        Cell lipCell = gridManager.GetCell(lx, ly);
        if (lipCell == null)
            return false;
        int lipH = lipCell.GetCellInstanceHeight();
        if (lipH <= 0)
            return false;

        WaterManager wm = ResolveWaterManager();
        if (!CellQualifiesForDeckDisplayLipRelaxedRoadManager(lx, ly, lipH, heightMap, wm))
            return false;

        var normalsScratch = new List<Vector2Int>(4);
        CollectRelaxedLipBridgeNormalsRoadManager(lx, ly, lipH, heightMap, wm, normalsScratch);
        bool dirOk = false;
        for (int n = 0; n < normalsScratch.Count; n++)
        {
            if (normalsScratch[n].x == dir.x && normalsScratch[n].y == dir.y)
            {
                dirOk = true;
                break;
            }
        }
        if (!dirOk)
            return false;

        List<Vector2> chord = WalkStraightChordFromLipThroughWetToFarDry(lx, ly, dir.x, dir.y, lipH, heightMap);
        if (chord == null || chord.Count < 2)
            return false;

        int before = pathVec2.Count;
        AppendPathSkipDuplicateJoin(pathVec2, chord);
        return pathVec2.Count > before;
    }

    /// <summary>
    /// When <see cref="TryPrepareRoadPlacementPlanLongestValidPrefix"/> fails on a straight cardinal stroke (e.g. simulation street segment), rebuilds the wet run using
    /// the same lip→chord walk as manual preview (<see cref="WalkStraightChordFromLipThroughWetToFarDry"/>) and prepares a deck-span-only plan via <see cref="TryPrepareDeckSpanPlanFromAdjacentStroke"/>.
    /// </summary>
    public bool TryPrepareRoadPlacementPlanWithProgrammaticDeckSpanChord(List<Vector2> straightCardinalPath, Vector2Int segmentDir, RoadPathValidationContext ctx, out List<Vector2> expandedPath, out PathTerraformPlan plan)
    {
        expandedPath = null;
        plan = null;
        if (straightCardinalPath == null || straightCardinalPath.Count < 2 || terraformingService == null || terrainManager == null || gridManager == null)
            return false;
        if (segmentDir.x == 0 && segmentDir.y == 0)
            return false;
        if (Mathf.Abs(segmentDir.x) + Mathf.Abs(segmentDir.y) != 1)
            return false;

        HeightMap heightMap = terrainManager.GetHeightMap();
        if (heightMap == null)
            return false;

        for (int i = 1; i < straightCardinalPath.Count; i++)
        {
            int px = (int)straightCardinalPath[i - 1].x, py = (int)straightCardinalPath[i - 1].y;
            int cx = (int)straightCardinalPath[i].x, cy = (int)straightCardinalPath[i].y;
            if (cx - px != segmentDir.x || cy - py != segmentDir.y)
                return false;
        }

        int wetIndex = -1;
        for (int i = 1; i < straightCardinalPath.Count; i++)
        {
            int cx = (int)straightCardinalPath[i].x, cy = (int)straightCardinalPath[i].y;
            if (IsWaterOrWaterSlope(cx, cy, heightMap))
            {
                wetIndex = i;
                break;
            }
        }
        if (wetIndex < 1)
            return false;

        int lx = (int)straightCardinalPath[wetIndex - 1].x;
        int ly = (int)straightCardinalPath[wetIndex - 1].y;
        Cell lipCell = gridManager.GetCell(lx, ly);
        if (lipCell == null)
            return false;
        int lipH = lipCell.GetCellInstanceHeight();
        if (lipH <= 0)
            return false;

        WaterManager wm = ResolveWaterManager();
        if (!CellQualifiesForDeckDisplayLipRelaxedRoadManager(lx, ly, lipH, heightMap, wm))
            return false;

        var normalsScratch = new List<Vector2Int>(4);
        CollectRelaxedLipBridgeNormalsRoadManager(lx, ly, lipH, heightMap, wm, normalsScratch);
        bool dirOk = false;
        for (int n = 0; n < normalsScratch.Count; n++)
        {
            if (normalsScratch[n].x == segmentDir.x && normalsScratch[n].y == segmentDir.y)
            {
                dirOk = true;
                break;
            }
        }
        if (!dirOk)
            return false;

        List<Vector2> chord = WalkStraightChordFromLipThroughWetToFarDry(lx, ly, segmentDir.x, segmentDir.y, lipH, heightMap);
        if (chord == null || chord.Count < 2)
            return false;

        var merged = new List<Vector2>(straightCardinalPath.Count + chord.Count);
        for (int i = 0; i < wetIndex; i++)
            merged.Add(straightCardinalPath[i]);
        AppendPathSkipDuplicateJoin(merged, chord);
        AppendStraightSuffixAlongProgrammaticChord(merged, straightCardinalPath, segmentDir);

        int minPrefixLen = wetIndex + chord.Count - 1;
        if (minPrefixLen < 2 || merged.Count < minPrefixLen)
            return false;

        for (int k = merged.Count; k >= minPrefixLen; k--)
        {
            List<Vector2> prefix = SubListCopy(merged, k);
            if (TryPrepareDeckSpanPlanFromAdjacentStroke(prefix, ctx, out expandedPath, out plan))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Validates an adjacent stroke for bridge rules and builds a deck-span-only terraform plan (shared by locked manual chord and programmatic auto chord).
    /// </summary>
    bool TryPrepareDeckSpanPlanFromAdjacentStroke(List<Vector2> adjacentPath, RoadPathValidationContext ctx, out List<Vector2> expandedPath, out PathTerraformPlan plan)
    {
        expandedPath = null;
        plan = null;
        if (adjacentPath == null || adjacentPath.Count < 2 || terraformingService == null || terrainManager == null)
            return false;

        HeightMap heightMap = terrainManager.GetHeightMap();
        if (heightMap == null)
            return false;

        List<Vector2> pathForPlan = RoadStrokeTerrainRules.TruncatePathAtFirstDisallowedLandSlope(adjacentPath, terrainManager);
        if (pathForPlan == null || pathForPlan.Count < 2)
            return false;

        if (!IsPathFullyAdjacent(pathForPlan))
            return false;
        if (!IsBridgePathValid(pathForPlan, heightMap))
            return false;
        if (!ValidateFeat44WaterBridgeRules(pathForPlan, heightMap, postUserWarnings: false))
            return false;
        if (HasTurnOnWaterOrCoast(pathForPlan, heightMap))
            return false;
        if (HasElbowTooCloseToWater(pathForPlan, heightMap, relaxElbowNearWaterForWaterBridge: !ctx.forbidCutThrough))
            return false;

        expandedPath = TerraformingService.ExpandDiagonalStepsToCardinal(pathForPlan);
        if (!terraformingService.TryBuildDeckSpanOnlyWaterBridgePlan(expandedPath, out plan))
            return false;
        if (!ValidateTerraformPlanWithContext(plan, ctx))
            return false;
        if (!plan.TryValidatePhase1Heights(heightMap, terrainManager, null, null))
            return false;

        return true;
    }

    static void AppendStraightSuffixAlongProgrammaticChord(List<Vector2> merged, List<Vector2> straightPath, Vector2Int segmentDir)
    {
        if (merged == null || straightPath == null || merged.Count == 0 || straightPath.Count == 0)
            return;
        Vector2 end = merged[merged.Count - 1];
        int idx = -1;
        for (int i = 0; i < straightPath.Count; i++)
        {
            if ((int)straightPath[i].x == (int)end.x && (int)straightPath[i].y == (int)end.y)
            {
                idx = i;
                break;
            }
        }
        if (idx < 0 || idx >= straightPath.Count - 1)
            return;
        for (int i = idx + 1; i < straightPath.Count; i++)
        {
            Vector2 prev = merged[merged.Count - 1];
            Vector2 cur = straightPath[i];
            if ((int)cur.x != (int)prev.x + segmentDir.x || (int)cur.y != (int)prev.y + segmentDir.y)
                break;
            merged.Add(cur);
        }
    }

    static List<Vector2> SubListCopy(List<Vector2> full, int count)
    {
        var p = new List<Vector2>(count);
        for (int i = 0; i < count; i++)
            p.Add(full[i]);
        return p;
    }

    /// <summary>
    /// Last index in <paramref name="mergedPath"/> where <paramref name="chord"/> appears as a contiguous subsequence (first match).
    /// </summary>
    static int FindLockedChordEndIndexInMergedPath(List<Vector2> mergedPath, List<Vector2> chord)
    {
        if (mergedPath == null || chord == null || chord.Count < 2 || mergedPath.Count < chord.Count)
            return -1;
        int m = mergedPath.Count;
        int n = chord.Count;
        for (int s = 0; s <= m - n; s++)
        {
            bool match = true;
            for (int j = 0; j < n; j++)
            {
                if ((int)mergedPath[s + j].x != (int)chord[j].x || (int)mergedPath[s + j].y != (int)chord[j].y)
                {
                    match = false;
                    break;
                }
            }
            if (match)
                return s + n - 1;
        }
        return -1;
    }

    /// <summary>
    /// When <see cref="manualPreviewBridgeLocked"/> and <see cref="lockedBridgeChord"/> are set, prepares placement using a no-mutation deck-span plan
    /// (straight chord over water/cliffs at lip height) instead of <see cref="ComputePathPlan"/>, so preview/commit are not blocked by cut-through Phase-1
    /// on complex tails. Tries longest merged-path prefix from full length down to the end of the locked chord that still passes stroke validation.
    /// </summary>
    bool TryPrepareLockedDeckSpanBridgePlacement(List<Vector2> mergedPath, RoadPathValidationContext ctx, out List<Vector2> expandedPath, out PathTerraformPlan plan)
    {
        expandedPath = null;
        plan = null;
        if (!manualPreviewBridgeLocked || lockedBridgeChord == null || lockedBridgeChord.Count < 2)
            return false;
        if (mergedPath == null || mergedPath.Count < 2 || terraformingService == null || terrainManager == null || gridManager == null)
            return false;

        int chordEnd = FindLockedChordEndIndexInMergedPath(mergedPath, lockedBridgeChord);
        if (chordEnd < 0)
            return false;

        int minLen = chordEnd + 1;

        for (int k = mergedPath.Count; k >= minLen; k--)
        {
            List<Vector2> prefix = SubListCopy(mergedPath, k);
            if (TryPrepareDeckSpanPlanFromAdjacentStroke(prefix, ctx, out expandedPath, out plan))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Filters raw path, checks adjacency, straightens bridges, validates bridge rules. Shared by full plan and longest-prefix search.
    /// When <paramref name="relaxElbowNearWaterForWaterBridge"/> is true (streets / manual roads), <see cref="HasElbowTooCloseToWater"/> allows elbows at high-deck lips and near in-stroke wet cells; interstate keeps it false via <see cref="RoadPathValidationContext.forbidCutThrough"/>.
    /// </summary>
    bool TryBuildFilteredPathForRoadPlan(List<Vector2> pathRaw, bool postUserWarnings, out List<Vector2> filteredPath, bool relaxElbowNearWaterForWaterBridge = false)
    {
        filteredPath = null;
        if (pathRaw == null || pathRaw.Count == 0 || terraformingService == null || terrainManager == null || gridManager == null)
            return false;

        var heightMap = terrainManager.GetHeightMap();
        if (heightMap == null)
            return false;

        var list = new List<Vector2>();
        for (int i = 0; i < pathRaw.Count; i++)
        {
            Vector2 gridPos = pathRaw[i];
            if (gridManager.GetCell((int)gridPos.x, (int)gridPos.y) != null)
                list.Add(gridPos);
        }
        if (list.Count == 0)
            return false;

        list = RoadStrokeTerrainRules.TruncatePathAtFirstDisallowedLandSlope(list, terrainManager);
        if (list.Count == 0)
        {
            filteredPath = null;
            return false;
        }

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
        if (!ValidateFeat44WaterBridgeRules(list, heightMap, postUserWarnings))
            return false;
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

    /// <summary>
    /// True when <paramref name="expandedPath"/> is a completed FEAT-44 water bridge (land — wet run — land); enables relaxed terraform beside cliffs / water edges.
    /// </summary>
    bool PathQualifiesForWaterBridgeTerraformRelaxation(List<Vector2> expandedPath)
    {
        if (expandedPath == null || expandedPath.Count < 3 || terrainManager == null)
            return false;
        HeightMap hm = terrainManager.GetHeightMap();
        if (hm == null)
            return false;
        if (!TryGetSingleBridgeRunBounds(expandedPath, hm, out int rs, out int re))
            return false;
        if (rs < 1 || re >= expandedPath.Count - 1)
            return false;
        if (IsWaterOrWaterSlope((int)expandedPath[rs - 1].x, (int)expandedPath[rs - 1].y, hm))
            return false;
        if (IsWaterOrWaterSlope((int)expandedPath[re + 1].x, (int)expandedPath[re + 1].y, hm))
            return false;
        return ValidateFeat44WaterBridgeRules(expandedPath, hm, postUserWarnings: false);
    }

    /// <summary>
    /// True when a dry path cell has a cardinal neighbor strictly lower that touches registered water (Moore), i.e. high deck / cliff lip above a cauce.
    /// Enables the same <see cref="PathTerraformPlan.waterBridgeTerraformRelaxation"/> pipeline as full FEAT-44 spans so single-tile or partial strokes can plan without <see cref="TerraformingService"/> steep-neighbor rejection.
    /// </summary>
    bool PathQualifiesForWaterAdjacentDeckTerraformRelaxation(List<Vector2> expandedPath)
    {
        if (expandedPath == null || expandedPath.Count == 0 || terrainManager == null)
            return false;
        if (PathQualifiesForWaterBridgeTerraformRelaxation(expandedPath))
            return false;

        HeightMap hm = terrainManager.GetHeightMap();
        if (hm == null)
            return false;

        WaterManager wm = ResolveWaterManager();

        int[] cdx = { 1, -1, 0, 0 };
        int[] cdy = { 0, 0, 1, -1 };

        foreach (Vector2 p in expandedPath)
        {
            int x = (int)p.x, y = (int)p.y;
            if (terrainManager.IsRegisteredOpenWaterAt(x, y))
                continue;

            int h = hm.IsValidPosition(x, y) ? hm.GetHeight(x, y) : TerrainManager.MIN_HEIGHT;

            for (int d = 0; d < 4; d++)
            {
                int nx = x + cdx[d];
                int ny = y + cdy[d];
                if (!hm.IsValidPosition(nx, ny))
                    continue;
                int hn = hm.GetHeight(nx, ny);
                if (hn >= h)
                    continue;
                if (DryCellTouchesRegisteredWaterForHighDeck(nx, ny, wm))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Open water, water-slope, or dry cell Moore-adjacent to <see cref="WaterManager.IsWaterAt"/> (registered water map).
    /// </summary>
    bool DryCellTouchesRegisteredWaterForHighDeck(int x, int y, WaterManager wm)
    {
        if (terrainManager == null)
            return false;
        if (terrainManager.IsRegisteredOpenWaterAt(x, y) || terrainManager.IsWaterSlopeCell(x, y))
            return true;
        if (wm == null)
            return false;

        int[] mdx = { 1, -1, 0, 0, 1, 1, -1, -1 };
        int[] mdy = { 0, 0, 1, -1, 1, -1, 1, -1 };
        for (int d = 0; d < 8; d++)
        {
            int ax = x + mdx[d];
            int ay = y + mdy[d];
            if (wm.IsWaterAt(ax, ay))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Diagonal expansion, <see cref="TerraformingService.ComputePathPlan"/>, context validation, and <see cref="PathTerraformPlan.TryValidatePhase1Heights"/>.
    /// </summary>
    bool TryPrepareFromFilteredPathList(List<Vector2> filteredPath, RoadPathValidationContext ctx, bool postUserWarnings, out List<Vector2> expandedPath, out PathTerraformPlan plan)
    {
        expandedPath = null;
        plan = null;
        var heightMap = terrainManager.GetHeightMap();
        if (heightMap == null || filteredPath == null || filteredPath.Count == 0) return false;

        expandedPath = TerraformingService.ExpandDiagonalStepsToCardinal(filteredPath);
        bool waterBridgeRelax = PathQualifiesForWaterBridgeTerraformRelaxation(expandedPath)
            || PathQualifiesForWaterAdjacentDeckTerraformRelaxation(expandedPath);
        plan = terraformingService.ComputePathPlan(expandedPath, waterBridgeRelax);
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

        if (!plan.TryValidatePhase1Heights(heightMap, terrainManager, null, null))
        {
            if (postUserWarnings && GameNotificationManager.Instance != null)
                GameNotificationManager.Instance.PostWarning("Terrain cannot be modified safely (height difference would exceed 1). Choose a different path.");
            return false;
        }

        return true;
    }

    int CalculateTotalCost(int tilesCount)
    {
        return GetRoadCostForTileCount(tilesCount);
    }

    /// <summary>
    /// Returns true when the player is actively drawing a road (mouse held after initial click).
    /// </summary>
    public bool IsDrawingRoad()
    {
        return isDrawingRoad;
    }

    /// <summary>
    /// Returns the number of tiles in the current road preview path (while drawing).
    /// </summary>
    public int GetPreviewRoadTileCount()
    {
        return previewRoadGridPositions.Count;
    }

    /// <summary>
    /// Returns the cost per road tile (50).
    /// </summary>
    public int GetRoadCostPerTile()
    {
        return 50;
    }

    /// <summary>
    /// Returns the total cost for placing the given number of road tiles.
    /// </summary>
    /// <param name="tilesCount">Number of road tiles.</param>
    /// <returns>Total construction cost.</returns>
    public int GetRoadCostForTileCount(int tilesCount)
    {
        return tilesCount * 50;
    }

    bool isAdjacentRoadInPreview(Vector2 gridPos)
    {
        foreach (Vector2 previewGridPos in previewRoadGridPositions)
        {
            if (gridPos == previewGridPos)
            {
                return true;
            }
        }
        return false;
    }

    void UpdateAdjacentRoadPrefabs(Vector2 gridPos, int i)
    {
        Vector2[] dirs = { new Vector2(-1, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(0, -1) };
        foreach (Vector2 d in dirs)
        {
            Vector2 n = gridPos + d;
            if (IsRoadAt(n) && !isAdjacentRoadInPreview(n))
                RefreshRoadPrefabAt(n);
        }
    }

    void IRoadManager.UpdateAdjacentRoadPrefabsAt(Vector2 gridPos) => UpdateAdjacentRoadPrefabsAt(gridPos);

    /// <summary>
    /// Final pass: refreshes all road cells adjacent to the placement path but not in the path.
    /// Ensures junction prefabs (T, crossing) are correct after all tiles are placed.
    /// </summary>
    void RefreshAllAdjacentRoadsOutsidePath()
    {
        if (placementPathPositions == null) return;
        var toRefresh = new HashSet<Vector2>();
        Vector2[] dirs = { new Vector2(-1, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(0, -1) };
        foreach (Vector2 pathPos in placementPathPositions)
        {
            foreach (Vector2 d in dirs)
            {
                Vector2 n = pathPos + d;
                if (IsRoadAt(n) && !placementPathPositions.Contains(n))
                    toRefresh.Add(n);
            }
        }
        foreach (Vector2 pos in toRefresh)
            RefreshRoadPrefabAt(pos);
    }

    /// <summary>
    /// Refreshes prefabs of all road tiles adjacent to the given position so they connect correctly.
    /// Use after programmatic placement (e.g. AutoRoadBuilder) so existing roads update to T-junctions/crossings.
    /// Road cache is updated incrementally by the placement caller (AddRoadToCache).
    /// </summary>
    /// <param name="gridPos">Grid position of the newly placed road.</param>
    public void UpdateAdjacentRoadPrefabsAt(Vector2 gridPos)
    {
        var toRefresh = new List<Vector2>();
        Vector2[] dirs = { new Vector2(-1, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(0, -1) };
        foreach (Vector2 d in dirs)
        {
            Vector2 n = gridPos + d;
            if (IsRoadAt(n) && (placementPathPositions == null || !placementPathPositions.Contains(n)))
                toRefresh.Add(n);
        }
        foreach (Vector2 pos in toRefresh)
            RefreshRoadPrefabAt(pos);
    }

    /// <summary>
    /// After batch placement via <see cref="PlaceRoadTileFromResolved"/> (AUTO / programmatic), re-resolves each unique affected road cell once:
    /// every newly placed cell plus cardinal road neighbors. Skips bridge deck tiles (FEAT-44). Deduplicates for performance.
    /// </summary>
    public void RefreshRoadPrefabsAfterBatchPlacement(IReadOnlyList<Vector2Int> newlyPlacedRoadCells)
    {
        if (newlyPlacedRoadCells == null || newlyPlacedRoadCells.Count == 0 || gridManager == null)
            return;

        var toRefresh = new HashSet<Vector2Int>();
        for (int i = 0; i < newlyPlacedRoadCells.Count; i++)
        {
            Vector2Int p = newlyPlacedRoadCells[i];
            toRefresh.Add(p);
            for (int d = 0; d < 4; d++)
            {
                int nx = p.x + DirX[d], ny = p.y + DirY[d];
                if (nx < 0 || nx >= gridManager.width || ny < 0 || ny >= gridManager.height)
                    continue;
                if (IsRoadAt(new Vector2(nx, ny)))
                    toRefresh.Add(new Vector2Int(nx, ny));
            }
        }

        foreach (Vector2Int pos in toRefresh)
        {
            if (!IsRoadAt(new Vector2(pos.x, pos.y)))
                continue;
            if (IsCellUsingBridgeDeckRoadPrefab(pos))
                continue;
            RefreshRoadPrefabAt(new Vector2(pos.x, pos.y));
        }
    }

    /// <summary>True if the road visual on this cell is a horizontal/vertical bridge deck (do not batch re-resolve).</summary>
    bool IsCellUsingBridgeDeckRoadPrefab(Vector2Int gridPos)
    {
        GameObject cell = gridManager.GetGridCell(new Vector2(gridPos.x, gridPos.y));
        if (cell == null) return false;
        for (int i = 0; i < cell.transform.childCount; i++)
        {
            Transform t = cell.transform.GetChild(i);
            Zone z = t.GetComponent<Zone>();
            if (z == null || z.zoneType != Zone.ZoneType.Road)
                continue;
            string n = t.gameObject.name;
            if (roadTileBridgeHorizontal != null && n.StartsWith(roadTileBridgeHorizontal.name))
                return true;
            if (roadTileBridgeVertical != null && n.StartsWith(roadTileBridgeVertical.name))
                return true;
            return false;
        }
        return false;
    }

    /// <summary>
    /// Picks a road neighbor as "previous" for <see cref="RefreshRoadPrefabAt"/>. Uses stored path segment hint on straight-through segments so
    /// slope resolution matches <see cref="RoadPrefabResolver.ResolveForPath"/> travel order (BUG-51); otherwise same rules as legacy connectivity-only picker.
    /// </summary>
    Vector2 PickPrevGridPosForRoadRefresh(Vector2 gridPos, Cell cellOrNull)
    {
        if (cellOrNull != null && TryPrevGridPosFromStoredRoadSegment(gridPos, cellOrNull, out Vector2 hinted))
            return hinted;
        return PickPrevGridPosForRoadRefreshConnectivityOnly(gridPos);
    }

    /// <summary>
    /// Clears BUG-51 route hints when the cell is no longer a straight-through segment, dead end, or stored successor is missing.
    /// </summary>
    void InvalidateRoadRouteHintsIfTopologyMismatch(Vector2 gridPos, Cell cell)
    {
        if (cell == null || !cell.hasRoadRouteDirHints)
            return;

        bool roadWest = IsRoadAt(gridPos + new Vector2(-1, 0));
        bool roadEast = IsRoadAt(gridPos + new Vector2(1, 0));
        bool roadNorth = IsRoadAt(gridPos + new Vector2(0, 1));
        bool roadSouth = IsRoadAt(gridPos + new Vector2(0, -1));
        int c = (roadWest ? 1 : 0) + (roadEast ? 1 : 0) + (roadNorth ? 1 : 0) + (roadSouth ? 1 : 0);
        bool straightHorizontal = roadWest && roadEast && !roadNorth && !roadSouth;
        bool straightVertical = roadNorth && roadSouth && !roadWest && !roadEast;
        bool deadEnd = c == 1;

        if (straightHorizontal || straightVertical)
        {
            if (cell.hasRoadSegmentNextHint)
            {
                Vector2 nxt = new Vector2(cell.roadSegmentNextGrid.x, cell.roadSegmentNextGrid.y);
                if (!IsRoadAt(nxt))
                {
                    cell.ClearRoadRouteHints();
                    return;
                }
            }
            return;
        }

        if (deadEnd)
            return;

        cell.ClearRoadRouteHints();
    }

    static bool IsUnitCardinalGridStep(Vector2Int step)
    {
        return Mathf.Abs(step.x) + Mathf.Abs(step.y) == 1;
    }

    /// <summary>
    /// When the cell has a valid straight-through topology (exactly two opposite cardinal road neighbors), returns the stored predecessor if it is one of those neighbors.
    /// Otherwise clears the hint and returns false.
    /// </summary>
    bool TryPrevGridPosFromStoredRoadSegment(Vector2 gridPos, Cell cell, out Vector2 prev)
    {
        prev = gridPos;
        Vector2 hint;
        if (cell.hasRoadSegmentPrevHint)
            hint = new Vector2(cell.roadSegmentPrevGrid.x, cell.roadSegmentPrevGrid.y);
        else if (cell.hasRoadRouteDirHints && IsUnitCardinalGridStep(cell.roadRouteEntryStep))
            hint = gridPos - new Vector2(cell.roadRouteEntryStep.x, cell.roadRouteEntryStep.y);
        else
            return false;

        int gx = (int)gridPos.x;
        int gy = (int)gridPos.y;
        int hx = Mathf.RoundToInt(hint.x);
        int hy = Mathf.RoundToInt(hint.y);
        if (Mathf.Abs(gx - hx) + Mathf.Abs(gy - hy) != 1)
        {
            cell.ClearRoadRouteHints();
            return false;
        }

        if (!IsRoadAt(hint))
        {
            cell.ClearRoadRouteHints();
            return false;
        }

        bool roadWest = IsRoadAt(gridPos + new Vector2(-1, 0));
        bool roadEast = IsRoadAt(gridPos + new Vector2(1, 0));
        bool roadNorth = IsRoadAt(gridPos + new Vector2(0, 1));
        bool roadSouth = IsRoadAt(gridPos + new Vector2(0, -1));

        bool straightHorizontal = roadWest && roadEast && !roadNorth && !roadSouth;
        bool straightVertical = roadNorth && roadSouth && !roadWest && !roadEast;

        if (straightHorizontal)
        {
            Vector2 w = gridPos + new Vector2(-1, 0);
            Vector2 e = gridPos + new Vector2(1, 0);
            if (hint == w || hint == e)
            {
                prev = hint;
                return true;
            }
        }
        else if (straightVertical)
        {
            Vector2 n = gridPos + new Vector2(0, 1);
            Vector2 s = gridPos + new Vector2(0, -1);
            if (hint == n || hint == s)
            {
                prev = hint;
                return true;
            }
        }

        cell.ClearRoadRouteHints();
        return false;
    }

    /// <summary>
    /// Deterministic road neighbor for refresh when no path-order hint applies (legacy behavior).
    /// </summary>
    Vector2 PickPrevGridPosForRoadRefreshConnectivityOnly(Vector2 gridPos)
    {
        bool roadWest = IsRoadAt(gridPos + new Vector2(-1, 0));
        bool roadEast = IsRoadAt(gridPos + new Vector2(1, 0));
        bool roadNorth = IsRoadAt(gridPos + new Vector2(0, 1));
        bool roadSouth = IsRoadAt(gridPos + new Vector2(0, -1));
        int horizCount = (roadWest ? 1 : 0) + (roadEast ? 1 : 0);
        int vertCount = (roadNorth ? 1 : 0) + (roadSouth ? 1 : 0);
        int total = horizCount + vertCount;
        if (total == 0)
            return gridPos;
        if (total == 1)
        {
            if (roadWest) return gridPos + new Vector2(-1, 0);
            if (roadEast) return gridPos + new Vector2(1, 0);
            if (roadNorth) return gridPos + new Vector2(0, 1);
            return gridPos + new Vector2(0, -1);
        }

        if (horizCount == 2 && vertCount <= 1)
        {
            if (roadWest) return gridPos + new Vector2(-1, 0);
            return gridPos + new Vector2(1, 0);
        }
        if (vertCount == 2 && horizCount <= 1)
        {
            if (roadNorth) return gridPos + new Vector2(0, 1);
            return gridPos + new Vector2(0, -1);
        }

        var candidates = new List<Vector2>(4);
        if (roadWest) candidates.Add(gridPos + new Vector2(-1, 0));
        if (roadEast) candidates.Add(gridPos + new Vector2(1, 0));
        if (roadNorth) candidates.Add(gridPos + new Vector2(0, 1));
        if (roadSouth) candidates.Add(gridPos + new Vector2(0, -1));
        candidates.Sort((a, b) =>
        {
            int c = a.x.CompareTo(b.x);
            return c != 0 ? c : a.y.CompareTo(b.y);
        });
        return candidates[0];
    }

    void RefreshRoadPrefabAt(Vector2 gridPos)
    {
        if (gridManager.IsCellOccupiedByBuilding((int)gridPos.x, (int)gridPos.y))
            return;
        GameObject cell = gridManager.GetGridCell(gridPos);
        if (cell == null) return;
        Cell cellComponentCheck = gridManager.GetCell((int)gridPos.x, (int)gridPos.y);
        if (cellComponentCheck == null) return;

        InvalidateRoadRouteHintsIfTopologyMismatch(gridPos, cellComponentCheck);

        Vector2 prevGridPos = PickPrevGridPosForRoadRefresh(gridPos, cellComponentCheck);

        if (roadPrefabResolver == null && gridManager != null && terrainManager != null)
            roadPrefabResolver = new RoadPrefabResolver(gridManager, terrainManager, this);

        GameObject correctRoadPrefab;
        Vector2 worldPos;
        if (roadPrefabResolver != null)
        {
            var resolved = roadPrefabResolver.ResolveForCell(gridPos, prevGridPos);
            if (resolved.HasValue)
            {
                correctRoadPrefab = resolved.Value.prefab;
                worldPos = resolved.Value.worldPos;
            }
            else
            {
                correctRoadPrefab = roadTilePrefab1;
                worldPos = gridManager.GetWorldPosition((int)gridPos.x, (int)gridPos.y);
            }
        }
        else
        {
            correctRoadPrefab = GetCorrectRoadPrefab(prevGridPos, gridPos, false, false);
            int terrainHeight = cellComponentCheck.GetCellInstanceHeight();
            worldPos = GetRoadTileWorldPosition((int)gridPos.x, (int)gridPos.y, correctRoadPrefab, terrainHeight);
        }

        DestroyPreviousRoadTile(cell, gridPos);
        cellComponentCheck.RemoveForestForBuilding();

        GameObject roadTile = Instantiate(correctRoadPrefab, worldPos, Quaternion.identity);
        roadTile.transform.SetParent(cellComponentCheck.gameObject.transform);
        roadTile.GetComponent<SpriteRenderer>().color = cellComponentCheck.isInterstate
            ? new Color(0.78f, 0.78f, 0.88f, 1f)
            : new Color(1, 1, 1, 1);

        Zone zone = roadTile.AddComponent<Zone>();
        zone.zoneType = Zone.ZoneType.Road;

        UpdateRoadCellAttributes(cellComponentCheck, roadTile, Zone.ZoneType.Road);
        gridManager.SetRoadSortingOrder(roadTile, (int)gridPos.x, (int)gridPos.y);
        gridManager.AddRoadToCache(new Vector2Int((int)gridPos.x, (int)gridPos.y));
    }

    /// <summary>
    /// Ghost road tiles only — does not call <see cref="PathTerraformPlan.Apply"/> so dragging never mutates the heightmap (commit applies in <see cref="TryFinalizeManualRoadPlacement"/>).
    /// Caller must clear prior preview instances first.
    /// </summary>
    void DrawPreviewLineCore(List<Vector2> path)
    {
        if (path == null || path.Count == 0) return;
        if (terraformingService == null || terrainManager == null || gridManager == null) return;
        if (roadPrefabResolver == null)
            roadPrefabResolver = new RoadPrefabResolver(gridManager, terrainManager, this);
        if (roadPrefabResolver == null) return;

        var previewCtx = new RoadPathValidationContext { forbidCutThrough = false };
        List<Vector2> expandedPath;
        PathTerraformPlan plan;
        if (TryPrepareLockedDeckSpanBridgePlacement(path, previewCtx, out expandedPath, out plan))
            manualRoadLongestPrefixHint = path.Count;
        else if (!TryPrepareRoadPlacementPlanLongestValidPrefix(path, previewCtx, false, ref manualRoadLongestPrefixHint, out expandedPath, out plan, out _))
            return;

        var resolved = roadPrefabResolver.ResolveForPath(expandedPath, plan);
        previewResolvedTiles.Clear();
        previewResolvedTiles.AddRange(resolved);

        for (int i = 0; i < resolved.Count; i++)
        {
            var tile = resolved[i];
            Cell cell = gridManager.GetCell(tile.gridPos.x, tile.gridPos.y);
            if (cell == null) continue;

            GameObject previewTile = Instantiate(tile.prefab, tile.worldPos, Quaternion.identity);
            SetPreviewRoadTileDetails(previewTile);
            previewRoadTiles.Add(previewTile);
            previewRoadGridPositions.Add(new Vector2(tile.gridPos.x, tile.gridPos.y));
            previewTile.transform.SetParent(cell.gameObject.transform);
        }
    }

    /// <summary>
    /// Returns grid cells from start to end. Uses A* pathfinding to prefer flat terrain and go around hills;
    /// falls back to Bresenham line when pathfinding finds no route (e.g. blocked by water).
    /// </summary>
    List<Vector2> GetLine(Vector2 start, Vector2 end)
    {
        Vector2Int from = new Vector2Int(Mathf.Clamp((int)start.x, 0, gridManager.width - 1), Mathf.Clamp((int)start.y, 0, gridManager.height - 1));
        Vector2Int to = new Vector2Int(Mathf.Clamp((int)end.x, 0, gridManager.width - 1), Mathf.Clamp((int)end.y, 0, gridManager.height - 1));

        if (gridManager != null)
        {
            var path = gridManager.FindPath(from, to);
            if (path != null && path.Count > 0)
            {
                var line = new List<Vector2>(path.Count);
                for (int i = 0; i < path.Count; i++)
                    line.Add(new Vector2(path[i].x, path[i].y));
                return line;
            }
        }

        return GetLineBresenham(start, end);
    }

    /// <summary>
    /// Raw stroke from <see cref="GetLine"/> plus, when the cursor ends on water/shore with a cardinal approach from dry land,
    /// an auto-completed opposite-bank land cell along the same axis (same bridge height). Resets <see cref="manualRoadLongestPrefixHint"/>
    /// when extension applies so longest-prefix search tries the full crossing first.
    /// </summary>
    List<Vector2> GetManualRoadPathWithOptionalBridgeExtension()
    {
        Vector2 cursor = currentDrawCursorGrid;
        int cx = (int)cursor.x;
        int cy = (int)cursor.y;

        if (manualPreviewBridgeLocked && lockedBridgeChord != null && lockedBridgeChord.Count >= 2)
        {
            int lx = lockedBridgeLip.x;
            int ly = lockedBridgeLip.y;
            int ndx = lockedBridgeNormal.x;
            int ndy = lockedBridgeNormal.y;
            int dot = (cx - lx) * ndx + (cy - ly) * ndy;
            if (dot <= 0)
                ClearManualPreviewBridgeLock();
            else
            {
                var approach = GetLine(startPosition, new Vector2(lx, ly));
                if (approach != null && approach.Count > 0)
                {
                    var merged = new List<Vector2>(approach.Count + lockedBridgeChord.Count + 16);
                    merged.AddRange(approach);
                    AppendPathSkipDuplicateJoin(merged, lockedBridgeChord);
                    int ex = (int)merged[merged.Count - 1].x;
                    int ey = (int)merged[merged.Count - 1].y;
                    if (cx != ex || cy != ey)
                    {
                        var tail = GetLine(new Vector2(ex, ey), cursor);
                        if (tail != null && tail.Count > 0)
                            AppendPathSkipDuplicateJoin(merged, tail);
                    }

                    List<Vector2> rawLocked = merged;
                    List<Vector2> extended = TryExtendPathAcrossWaterToOppositeLand(rawLocked);
                    if (extended != null)
                    {
                        manualRoadLongestPrefixHint = 0;
                        return extended;
                    }
                    return rawLocked;
                }

                ClearManualPreviewBridgeLock();
            }
        }

        List<Vector2> rawLine = GetLine(startPosition, cursor);
        List<Vector2> flexed = TryFlexBridgePreviewPathAcrossLipPlane(startPosition, cursor);
        List<Vector2> raw = rawLine;
        if (flexed != null && flexed.Count >= 2)
            raw = flexed;

        List<Vector2> extended2 = TryExtendPathAcrossWaterToOppositeLand(raw);
        if (extended2 != null)
        {
            manualRoadLongestPrefixHint = 0;
            return extended2;
        }
        return raw;
    }

    void ClearManualPreviewBridgeLock()
    {
        manualPreviewBridgeLocked = false;
        if (lockedBridgeChord != null)
            lockedBridgeChord.Clear();
    }

    /// <summary>
    /// Orders unique grid cells (cursor-proximate first) from the main stroke, a horizontal arm, and a vertical arm so deck lips
    /// on the entry row/column are found even when <see cref="GetLine"/> start-to-cursor misses them.
    /// </summary>
    void CollectManualStrokeLipCandidateCells(Vector2 strokeStart, Vector2 cursor, List<Vector2> orderedUniqueOut)
    {
        orderedUniqueOut.Clear();
        var seen = new HashSet<(int, int)>();
        void AddPathCellsInReverse(List<Vector2> path)
        {
            if (path == null) return;
            for (int i = path.Count - 1; i >= 0; i--)
            {
                int x = (int)path[i].x;
                int y = (int)path[i].y;
                if (seen.Add((x, y)))
                    orderedUniqueOut.Add(new Vector2(x, y));
            }
        }

        AddPathCellsInReverse(GetLine(strokeStart, cursor));
        int sx = (int)strokeStart.x;
        int sy = (int)strokeStart.y;
        int cxi = (int)cursor.x;
        int cyi = (int)cursor.y;
        AddPathCellsInReverse(GetLine(strokeStart, new Vector2(cxi, sy)));
        AddPathCellsInReverse(GetLine(strokeStart, new Vector2(sx, cyi)));
    }

    /// <summary>
    /// When the cursor lies strictly past a high-deck lip along a bridge cardinal (waterward dot &gt; 0), rebuilds the stroke as
    /// <see cref="GetLine"/> from stroke start to the lip, a straight axis-aligned chord through water/slope (FEAT-44 style),
    /// then <see cref="GetLine"/> from chord end to cursor. Locks the lip-to-exit chord for the rest of the drag while the cursor
    /// stays past the entry plane. Candidates include horizontal/vertical arms so the lip is found for arbitrary cursor Y.
    /// </summary>
    List<Vector2> TryFlexBridgePreviewPathAcrossLipPlane(Vector2 strokeStart, Vector2 cursor)
    {
        HeightMap heightMap = terrainManager != null ? terrainManager.GetHeightMap() : null;
        if (heightMap == null || gridManager == null || terrainManager == null)
            return null;

        CollectManualStrokeLipCandidateCells(strokeStart, cursor, manualStrokeLipCandidateScratch);
        if (manualStrokeLipCandidateScratch.Count < 1)
            return null;

        WaterManager wm = ResolveWaterManager();
        int cx = (int)cursor.x;
        int cy = (int)cursor.y;
        var normalsScratch = new List<Vector2Int>(4);

        for (int i = 0; i < manualStrokeLipCandidateScratch.Count; i++)
        {
            int lx = (int)manualStrokeLipCandidateScratch[i].x;
            int ly = (int)manualStrokeLipCandidateScratch[i].y;
            if (!heightMap.IsValidPosition(lx, ly))
                continue;
            if (terrainManager.IsRegisteredOpenWaterAt(lx, ly))
                continue;
            Cell lipCell = gridManager.GetCell(lx, ly);
            if (lipCell == null)
                continue;
            int lipH = lipCell.GetCellInstanceHeight();
            if (lipH <= 0)
                continue;
            if (!CellQualifiesForDeckDisplayLipRelaxedRoadManager(lx, ly, lipH, heightMap, wm))
                continue;

            CollectRelaxedLipBridgeNormalsRoadManager(lx, ly, lipH, heightMap, wm, normalsScratch);
            if (normalsScratch.Count == 0)
                continue;

            int bestDx = 0, bestDy = 0;
            int bestDot = int.MinValue;
            for (int n = 0; n < normalsScratch.Count; n++)
            {
                int ddx = normalsScratch[n].x;
                int ddy = normalsScratch[n].y;
                int dot = (cx - lx) * ddx + (cy - ly) * ddy;
                if (dot > bestDot)
                {
                    bestDot = dot;
                    bestDx = ddx;
                    bestDy = ddy;
                }
            }

            if (bestDot <= 0)
                continue;

            var approach = GetLine(strokeStart, new Vector2(lx, ly));
            if (approach == null || approach.Count == 0)
                continue;

            var chord = WalkStraightChordFromLipThroughWetToFarDry(lx, ly, bestDx, bestDy, lipH, heightMap);
            if (chord == null || chord.Count < 2)
                continue;

            var mergedCore = new List<Vector2>(approach.Count + chord.Count + 2);
            mergedCore.AddRange(approach);
            AppendPathSkipDuplicateJoin(mergedCore, chord);
            if (mergedCore.Count < 2 || !IsPathFullyAdjacent(mergedCore))
                continue;

            manualPreviewBridgeLocked = true;
            lockedBridgeLip = new Vector2Int(lx, ly);
            lockedBridgeNormal = new Vector2Int(bestDx, bestDy);
            if (lockedBridgeChord == null)
                lockedBridgeChord = new List<Vector2>(chord.Count);
            else
            {
                lockedBridgeChord.Clear();
                lockedBridgeChord.Capacity = Mathf.Max(lockedBridgeChord.Capacity, chord.Count);
            }
            for (int c = 0; c < chord.Count; c++)
                lockedBridgeChord.Add(chord[c]);

            var merged = new List<Vector2>(mergedCore.Count + 8);
            merged.AddRange(mergedCore);
            int ex = (int)merged[merged.Count - 1].x;
            int ey = (int)merged[merged.Count - 1].y;
            if (cx != ex || cy != ey)
            {
                var tail = GetLine(new Vector2(ex, ey), cursor);
                if (tail != null && tail.Count > 0)
                    AppendPathSkipDuplicateJoin(merged, tail);
            }

            if (merged.Count < 2)
                continue;

            return merged;
        }

        return null;
    }

    static void AppendPathSkipDuplicateJoin(List<Vector2> acc, List<Vector2> tail)
    {
        if (tail == null || tail.Count == 0)
            return;
        int start = 0;
        if (acc.Count > 0)
        {
            var a = acc[acc.Count - 1];
            var b = tail[0];
            if ((int)a.x == (int)b.x && (int)a.y == (int)b.y)
                start = 1;
        }
        for (int i = start; i < tail.Count; i++)
            acc.Add(tail[i]);
    }

    /// <summary>
    /// Same geometry as relaxed deck-lip assignment in <see cref="TerraformingService"/>: cardinal lower toward open water, water-slope, or dry touching registered water.
    /// </summary>
    bool CellQualifiesForDeckDisplayLipRelaxedRoadManager(int x, int y, int h, HeightMap heightMap, WaterManager wm)
    {
        if (heightMap == null || terrainManager == null || !heightMap.IsValidPosition(x, y))
            return false;
        int[] cdx = { 1, -1, 0, 0 };
        int[] cdy = { 0, 0, 1, -1 };
        for (int d = 0; d < 4; d++)
        {
            int nx = x + cdx[d];
            int ny = y + cdy[d];
            if (!heightMap.IsValidPosition(nx, ny))
                continue;
            int hn = heightMap.GetHeight(nx, ny);
            if (hn >= h)
                continue;
            if (terrainManager.IsRegisteredOpenWaterAt(nx, ny) || terrainManager.IsWaterSlopeCell(nx, ny))
                return true;
            if (DryCellTouchesRegisteredWaterForHighDeck(nx, ny, wm))
                return true;
        }
        return false;
    }

    void CollectRelaxedLipBridgeNormalsRoadManager(int x, int y, int h, HeightMap heightMap, WaterManager wm, List<Vector2Int> outNormals)
    {
        outNormals.Clear();
        int[] cdx = { 1, -1, 0, 0 };
        int[] cdy = { 0, 0, 1, -1 };
        for (int d = 0; d < 4; d++)
        {
            int nx = x + cdx[d];
            int ny = y + cdy[d];
            if (!heightMap.IsValidPosition(nx, ny))
                continue;
            int hn = heightMap.GetHeight(nx, ny);
            if (hn >= h)
                continue;
            if (terrainManager.IsRegisteredOpenWaterAt(nx, ny) || terrainManager.IsWaterSlopeCell(nx, ny))
            {
                outNormals.Add(new Vector2Int(cdx[d], cdy[d]));
                continue;
            }
            if (DryCellTouchesRegisteredWaterForHighDeck(nx, ny, wm))
                outNormals.Add(new Vector2Int(cdx[d], cdy[d]));
        }
    }

    /// <summary>
    /// Lip cell plus straight cardinal steps through water/slope, ending on first opposite dry land that matches bridge height (FEAT-44 chord intent).
    /// </summary>
    List<Vector2> WalkStraightChordFromLipThroughWetToFarDry(int lx, int ly, int ddx, int ddy, int bridgeHeight, HeightMap heightMap)
    {
        var result = new List<Vector2> { new Vector2(lx, ly) };

        int nx = lx + ddx;
        int ny = ly + ddy;
        if (!heightMap.IsValidPosition(nx, ny))
            return null;
        if (!IsWaterOrWaterSlope(nx, ny, heightMap))
            return null;

        int cx = nx;
        int cy = ny;
        result.Add(new Vector2(cx, cy));

        int maxSteps = Mathf.Max(gridManager.width, gridManager.height) + 2;

        for (int step = 0; step < maxSteps; step++)
        {
            int ax = cx + ddx;
            int ay = cy + ddy;
            if (!heightMap.IsValidPosition(ax, ay))
                break;

            if (IsWaterOrWaterSlope(ax, ay, heightMap))
            {
                result.Add(new Vector2(ax, ay));
                cx = ax;
                cy = ay;
                continue;
            }

            if (gridManager.IsCellOccupiedByBuilding(ax, ay))
                break;
            Cell farCell = gridManager.GetCell(ax, ay);
            if (farCell == null)
                break;
            if (farCell.isInterstate)
                break;
            if (!terrainManager.CanPlaceRoad(ax, ay))
                break;
            int farH = farCell.GetCellInstanceHeight();
            if (farH != bridgeHeight)
                break;

            result.Add(new Vector2(ax, ay));
            return result;
        }

        return null;
    }

    /// <summary>
    /// Bresenham line from start to end. Used as fallback when pathfinding finds no route.
    /// Diagonal steps are split into two cardinal steps (staircase) so road placement matches interstate-style elbows.
    /// </summary>
    List<Vector2> GetLineBresenham(Vector2 start, Vector2 end)
    {
        List<Vector2> line = new List<Vector2>();

        int x0 = Mathf.Clamp((int)start.x, 0, gridManager.width - 1);
        int y0 = Mathf.Clamp((int)start.y, 0, gridManager.height - 1);
        int x1 = Mathf.Clamp((int)end.x, 0, gridManager.width - 1);
        int y1 = Mathf.Clamp((int)end.y, 0, gridManager.height - 1);

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            line.Add(new Vector2(x0, y0));

            if (x0 == x1 && y0 == y1) break;

            int e2 = err * 2;
            bool movedX = false;
            bool movedY = false;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
                movedX = true;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
                movedY = true;
            }

            if (movedX && movedY)
            {
                line.Add(new Vector2(x0 - sx, y0));
            }
        }

        return line;
    }

    /// <summary>
    /// True if cell is registered open water (WaterMap) or water-shore slope. Used to define bridge wet runs (geography spec water map).
    /// </summary>
    bool IsWaterOrWaterSlope(int x, int y, HeightMap heightMap)
    {
        if (heightMap == null || !heightMap.IsValidPosition(x, y) || terrainManager == null) return false;
        if (terrainManager.IsRegisteredOpenWaterAt(x, y)) return true;
        return terrainManager.IsWaterSlopeCell(x, y);
    }

    /// <summary>
    /// If <paramref name="path"/> ends on water or shore, the step before the trailing wet run is dry land, and the step into the run
    /// is a single cardinal move, extends along that axis through wet cells until the first dry land that passes
    /// <see cref="TerrainManager.CanPlaceRoad(int,int)"/> at the same instance height as that land-before cell (FEAT-44 far endpoint).
    /// Returns null when no extension is needed or none is found.
    /// </summary>
    List<Vector2> TryExtendPathAcrossWaterToOppositeLand(List<Vector2> path)
    {
        HeightMap heightMap = terrainManager != null ? terrainManager.GetHeightMap() : null;
        if (path == null || path.Count < 2 || heightMap == null || gridManager == null || terrainManager == null)
            return null;

        int n = path.Count;
        int lx = (int)path[n - 1].x;
        int ly = (int)path[n - 1].y;
        if (!IsWaterOrWaterSlope(lx, ly, heightMap))
            return null;

        int wetStart = n - 1;
        while (wetStart > 0 && IsWaterOrWaterSlope((int)path[wetStart - 1].x, (int)path[wetStart - 1].y, heightMap))
            wetStart--;

        if (wetStart == 0)
            return null;

        int bx = (int)path[wetStart - 1].x;
        int by = (int)path[wetStart - 1].y;
        if (IsWaterOrWaterSlope(bx, by, heightMap))
            return null;

        int fwx = (int)path[wetStart].x;
        int fwy = (int)path[wetStart].y;
        int sdx = fwx - bx;
        int sdy = fwy - by;
        if (Mathf.Abs(sdx) > 1 || Mathf.Abs(sdy) > 1 || (sdx != 0 && sdy != 0))
            return null;

        int dx = sdx != 0 ? (int)Mathf.Sign(sdx) : 0;
        int dy = sdy != 0 ? (int)Mathf.Sign(sdy) : 0;
        if (dx == 0 && dy == 0)
            return null;

        Cell landBeforeCell = gridManager.GetCell(bx, by);
        if (landBeforeCell == null)
            return null;
        int bridgeHeight = landBeforeCell.GetCellInstanceHeight();

        int cx = lx;
        int cy = ly;
        var result = new List<Vector2>(path);
        int maxSteps = Mathf.Max(gridManager.width, gridManager.height) + 2;

        for (int step = 0; step < maxSteps; step++)
        {
            int nx = cx + dx;
            int ny = cy + dy;
            if (!heightMap.IsValidPosition(nx, ny))
                break;

            if (IsWaterOrWaterSlope(nx, ny, heightMap))
            {
                if ((int)result[result.Count - 1].x != nx || (int)result[result.Count - 1].y != ny)
                    result.Add(new Vector2(nx, ny));
                cx = nx;
                cy = ny;
                continue;
            }

            if (gridManager.IsCellOccupiedByBuilding(nx, ny))
                break;
            Cell farCell = gridManager.GetCell(nx, ny);
            if (farCell == null)
                break;
            if (farCell.isInterstate)
                break;
            if (!terrainManager.CanPlaceRoad(nx, ny))
                break;
            if (farCell.GetCellInstanceHeight() != bridgeHeight)
                break;

            result.Add(new Vector2(nx, ny));
            return result;
        }

        return null;
    }

    WaterManager ResolveWaterManager()
    {
        if (terrainManager != null && terrainManager.waterManager != null)
            return terrainManager.waterManager;
        return FindObjectOfType<WaterManager>();
    }

    /// <summary>
    /// Open water for bridge rules: registered in <see cref="WaterMap"/> (logical surface S; bed height may be above reference sea).
    /// </summary>
    bool IsOpenWaterCellForBridge(int x, int y, HeightMap heightMap, WaterManager waterManager)
    {
        if (heightMap == null || !heightMap.IsValidPosition(x, y)) return false;
        return waterManager != null && waterManager.IsWaterAt(x, y);
    }

    /// <summary>
    /// FEAT-44: interior of a water bridge must be registered water and/or shore (water slope), not dry land gaps.
    /// </summary>
    bool IsWaterRelatedBridgeInteriorCell(int x, int y, HeightMap heightMap, WaterManager waterManager)
    {
        if (!IsWaterOrWaterSlope(x, y, heightMap)) return false;
        if (IsOpenWaterCellForBridge(x, y, heightMap, waterManager)) return true;
        if (terrainManager != null && terrainManager.IsWaterSlopeCell(x, y))
        {
            if (waterManager == null) return true;
            int[] dx = { 1, -1, 0, 0, 1, 1, -1, -1 };
            int[] dy = { 0, 0, 1, -1, 1, -1, 1, -1 };
            for (int d = 0; d < 8; d++)
            {
                int nx = x + dx[d], ny = y + dy[d];
                if (waterManager.IsWaterAt(nx, ny)) return true;
            }
            return false;
        }
        return true;
    }

    /// <summary>
    /// FEAT-44: deck height vs logical surface and bed on open water; land endpoints match; no orthogonal bridge overlap on wet cells.
    /// </summary>
    bool ValidateFeat44WaterBridgeRules(List<Vector2> path, HeightMap heightMap, bool postUserWarnings)
    {
        if (path == null || path.Count < 2 || heightMap == null || gridManager == null)
            return true;

        if (!TryGetSingleBridgeRunBounds(path, heightMap, out int runStart, out int runEnd))
            return true;

        WaterManager waterManager = ResolveWaterManager();
        var pathStrokeCells = BuildPathGridCellSet(path);

        var straight = BresenhamStraightLine((int)path[runStart].x, (int)path[runStart].y, (int)path[runEnd].x, (int)path[runEnd].y);

        foreach (var p in straight)
        {
            int px = (int)p.x, py = (int)p.y;
            if (!heightMap.IsValidPosition(px, py))
                return FailFeat44(postUserWarnings, "Bridge span leaves the map.");
            var gp = new Vector2Int(px, py);
            if (!IsWaterRelatedBridgeInteriorCell(px, py, heightMap, waterManager) && !pathStrokeCells.Contains(gp))
                return FailFeat44(postUserWarnings, "Bridges may only cross registered water and shore, not dry gaps.");
        }

        int landBefore = runStart - 1;
        int landAfter = runEnd + 1;
        if (landBefore < 0 || landAfter >= path.Count)
            return FailFeat44(postUserWarnings, "A water bridge needs land at both ends.");
        if (IsWaterOrWaterSlope((int)path[landBefore].x, (int)path[landBefore].y, heightMap)
            || IsWaterOrWaterSlope((int)path[landAfter].x, (int)path[landAfter].y, heightMap))
            return FailFeat44(postUserWarnings, "A water bridge needs land at both ends.");

        Cell cellBefore = gridManager.GetCell((int)path[landBefore].x, (int)path[landBefore].y);
        Cell cellAfter = gridManager.GetCell((int)path[landAfter].x, (int)path[landAfter].y);
        if (cellBefore == null || cellAfter == null)
            return false;
        int bridgeHeight = cellBefore.GetCellInstanceHeight();
        int afterH = cellAfter.GetCellInstanceHeight();
        if (afterH != bridgeHeight)
            return FailFeat44(postUserWarnings, "Bridge ends must be at the same terrain height.");

        bool spanHorizontal = (int)path[runStart].y == (int)path[runEnd].y;

        for (int i = runStart; i <= runEnd; i++)
        {
            int x = (int)path[i].x, y = (int)path[i].y;
            if (!IsOpenWaterCellForBridge(x, y, heightMap, waterManager)) continue;

            int surfaceS = waterManager != null ? waterManager.GetWaterSurfaceHeight(x, y) : -1;
            if (surfaceS < 0)
                surfaceS = heightMap.GetHeight(x, y);
            int bed = heightMap.GetHeight(x, y);
            if (bridgeHeight < surfaceS || bridgeHeight < bed)
                return FailFeat44(postUserWarnings, "Bridge deck would sit below the water surface or bed here.");
        }

        for (int i = runStart; i <= runEnd; i++)
        {
            int x = (int)path[i].x, y = (int)path[i].y;
            if (!IsWaterOrWaterSlope(x, y, heightMap)) continue;
            if (!IsRoadAt(new Vector2(x, y))) continue;
            if (!TryGetDominantRoadAxisAt(x, y, out bool existingHorizontal, out bool isJunction))
                continue;
            if (isJunction)
                return FailFeat44(postUserWarnings, "Crossing or joining bridge spans here is not supported.");
            if (existingHorizontal != spanHorizontal)
                return FailFeat44(postUserWarnings, "Another bridge already crosses this water at a different angle.");
        }

        return true;
    }

    static bool FailFeat44(bool postUserWarnings, string message)
    {
        if (postUserWarnings && GameNotificationManager.Instance != null && !string.IsNullOrEmpty(message))
            GameNotificationManager.Instance.PostWarning(message);
        return false;
    }

    /// <summary>
    /// Returns bounds [runStart, runEnd] inclusive of the single water/shore run, or false if there is no bridge segment.
    /// </summary>
    bool TryGetSingleBridgeRunBounds(List<Vector2> path, HeightMap heightMap, out int runStart, out int runEnd)
    {
        runStart = -1;
        runEnd = -1;
        int runs = 0;
        bool inRun = false;
        int rs = -1, re = -1;
        for (int i = 0; i < path.Count; i++)
        {
            int x = (int)path[i].x, y = (int)path[i].y;
            bool w = IsWaterOrWaterSlope(x, y, heightMap);
            if (w)
            {
                if (!inRun)
                {
                    runs++;
                    rs = i;
                    inRun = true;
                }
                re = i;
            }
            else
            {
                inRun = false;
            }
        }
        if (runs != 1 || rs < 0) return false;
        runStart = rs;
        runEnd = re;
        return runEnd >= runStart;
    }

    /// <summary>
    /// Cardinal road axis through an existing road tile, or junction if both axes are used.
    /// </summary>
    bool TryGetDominantRoadAxisAt(int x, int y, out bool horizontal, out bool isJunction)
    {
        horizontal = false;
        isJunction = false;
        bool L = IsRoadAt(new Vector2(x - 1, y));
        bool R = IsRoadAt(new Vector2(x + 1, y));
        bool U = IsRoadAt(new Vector2(x, y + 1));
        bool D = IsRoadAt(new Vector2(x, y - 1));
        bool hConn = L || R;
        bool vConn = U || D;
        if (hConn && vConn)
        {
            isJunction = true;
            return true;
        }
        if (hConn)
        {
            horizontal = true;
            return true;
        }
        if (vConn)
        {
            horizontal = false;
            return true;
        }
        return false;
    }

    /// <summary>
    /// True if cell is at least 2 cells from any water (not on water, not adjacent to water).
    /// Elbows must satisfy this to be valid.
    /// </summary>
    bool IsAtLeastTwoCellsFromWater(int x, int y, HeightMap heightMap)
    {
        if (heightMap == null || !heightMap.IsValidPosition(x, y) || terrainManager == null) return false;
        if (terrainManager.IsRegisteredOpenWaterAt(x, y) || terrainManager.IsWaterSlopeCell(x, y)) return false;
        int[] dx = { 0, 1, -1, 0, 0 };
        int[] dy = { 0, 0, 0, 1, -1 };
        for (int d = 0; d < 5; d++)
        {
            int nx = x + dx[d], ny = y + dy[d];
            if (!heightMap.IsValidPosition(nx, ny)) continue;
            if (terrainManager.IsRegisteredOpenWaterAt(nx, ny) || terrainManager.IsWaterSlopeCell(nx, ny))
                return false;
        }
        return true;
    }

    /// <summary>
    /// True if the path has a turn (direction change) on water or water-slope. Invalid.
    /// </summary>
    bool HasTurnOnWaterOrCoast(List<Vector2> path, HeightMap heightMap)
    {
        if (path == null || path.Count < 3 || heightMap == null) return false;
        for (int i = 1; i < path.Count - 1; i++)
        {
            Vector2 prev = path[i - 1], curr = path[i], next = path[i + 1];
            int dxIn = (int)(curr.x - prev.x), dyIn = (int)(curr.y - prev.y);
            int dxOut = (int)(next.x - curr.x), dyOut = (int)(next.y - curr.y);
            if (dxIn != dxOut || dyIn != dyOut)
            {
                int x = (int)curr.x, y = (int)curr.y;
                if (IsWaterOrWaterSlope(x, y, heightMap))
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// True if the elbow vertex is a relaxed high-deck lip, or dry and within Chebyshev distance 2 of some water/slope cell on the same stroke (bridge / bank context).
    /// </summary>
    bool IsElbowExemptWaterBridgeNearWaterRelaxation(int elbowIndex, List<Vector2> path, HeightMap heightMap)
    {
        if (path == null || elbowIndex < 1 || elbowIndex >= path.Count - 1 || heightMap == null || gridManager == null || terrainManager == null)
            return false;

        WaterManager wm = ResolveWaterManager();
        int cx = (int)path[elbowIndex].x, cy = (int)path[elbowIndex].y;
        if (IsWaterOrWaterSlope(cx, cy, heightMap))
            return false;

        Cell lipCell = gridManager.GetCell(cx, cy);
        if (lipCell != null)
        {
            int h = lipCell.GetCellInstanceHeight();
            if (h > 0 && CellQualifiesForDeckDisplayLipRelaxedRoadManager(cx, cy, h, heightMap, wm))
                return true;
        }

        int wetDist = int.MaxValue;
        for (int j = 0; j < path.Count; j++)
        {
            int px = (int)path[j].x, py = (int)path[j].y;
            if (!IsWaterOrWaterSlope(px, py, heightMap))
                continue;
            int d = Mathf.Max(Mathf.Abs(px - cx), Mathf.Abs(py - cy));
            if (d < wetDist)
                wetDist = d;
        }

        if (wetDist == int.MaxValue)
            return false;
        return wetDist <= 2;
    }

    /// <summary>
    /// True if the path has an elbow (turn) that is on water-slope or within 2 cells of water. Invalid.
    /// When <paramref name="relaxElbowNearWaterForWaterBridge"/> is true, skips the check for elbows exempted by <see cref="IsElbowExemptWaterBridgeNearWaterRelaxation"/> (manual/street water-bridge previews).
    /// </summary>
    bool HasElbowTooCloseToWater(List<Vector2> path, HeightMap heightMap, bool relaxElbowNearWaterForWaterBridge = false)
    {
        if (path == null || path.Count < 3 || heightMap == null) return false;
        for (int i = 1; i < path.Count - 1; i++)
        {
            Vector2 prev = path[i - 1], curr = path[i], next = path[i + 1];
            int dxIn = (int)(curr.x - prev.x), dyIn = (int)(curr.y - prev.y);
            int dxOut = (int)(next.x - curr.x), dyOut = (int)(next.y - curr.y);
            if (dxIn != dxOut || dyIn != dyOut)
            {
                int x = (int)curr.x, y = (int)curr.y;
                if (relaxElbowNearWaterForWaterBridge && IsElbowExemptWaterBridgeNearWaterRelaxation(i, path, heightMap))
                    continue;
                if (!IsAtLeastTwoCellsFromWater(x, y, heightMap))
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Validates bridge path for interstate/auto-road: no turns on water or coast, elbows at least 2 cells from water.
    /// Rule F: last N land cells before water must be collinear (approach perpendicular to water).
    /// </summary>
    public bool ValidateBridgePath(List<Vector2Int> path, HeightMap heightMap)
    {
        if (path == null || path.Count < 2 || heightMap == null) return true;
        var pathVec2 = new List<Vector2>();
        foreach (var p in path) pathVec2.Add(new Vector2(p.x, p.y));
        if (HasTurnOnWaterOrCoast(pathVec2, heightMap) || HasElbowTooCloseToWater(pathVec2, heightMap))
            return false;
        if (HasTurnOnLastLandCellsBeforeWater(pathVec2, heightMap, 2))
            return false;
        return true;
    }

    /// <summary>
    /// Rule F: True if the last n land cells before any water segment have a turn (not collinear with bridge axis).
    /// Bridge approach must be perpendicular; no turn on the last land cells before water.
    /// </summary>
    bool HasTurnOnLastLandCellsBeforeWater(List<Vector2> path, HeightMap heightMap, int n = 2)
    {
        if (path == null || path.Count < n + 2 || heightMap == null || n < 1) return false;
        for (int i = 1; i < path.Count; i++)
        {
            int x = (int)path[i].x, y = (int)path[i].y;
            if (!IsWaterOrWaterSlope(x, y, heightMap)) continue;
            int firstWater = i;
            int landEnd = i - 1;
            int landStart = i - n;
            if (landStart < 0) continue;
            Vector2Int dirApproach = new Vector2Int((int)path[firstWater].x - (int)path[landEnd].x, (int)path[firstWater].y - (int)path[landEnd].y);
            if (dirApproach.x == 0 && dirApproach.y == 0) continue;
            for (int j = landStart; j < landEnd; j++)
            {
                Vector2Int dir = new Vector2Int((int)path[j + 1].x - (int)path[j].x, (int)path[j + 1].y - (int)path[j].y);
                if (dir.x != dirApproach.x || dir.y != dirApproach.y)
                    return true;
            }
            while (i < path.Count && IsWaterOrWaterSlope((int)path[i].x, (int)path[i].y, heightMap))
                i++;
            i--;
        }
        return false;
    }

    /// <summary>
    /// Replaces runs of consecutive water and water-slope cells with a straight axis-aligned line.
    /// Bridges must be horizontal or vertical; diagonal runs are aligned to the dominant axis.
    /// </summary>
    List<Vector2> StraightenBridgeSegments(List<Vector2> path, HeightMap heightMap)
    {
        if (path == null || path.Count < 2 || heightMap == null) return path;

        var result = new List<Vector2>();
        int i = 0;
        while (i < path.Count)
        {
            int x = (int)path[i].x;
            int y = (int)path[i].y;
            if (!IsWaterOrWaterSlope(x, y, heightMap))
            {
                result.Add(path[i]);
                i++;
                continue;
            }
            int j = i;
            while (j < path.Count)
            {
                int xj = (int)path[j].x, yj = (int)path[j].y;
                if (!IsWaterOrWaterSlope(xj, yj, heightMap))
                    break;
                j++;
            }
            j--;
            if (j > i)
            {
                int x0 = (int)path[i].x, y0 = (int)path[i].y;
                int x1 = (int)path[j].x, y1 = (int)path[j].y;
                List<Vector2> straight;
                if (x0 == x1 || y0 == y1)
                {
                    straight = BresenhamStraightLine(x0, y0, x1, y1);
                }
                else
                {
                    int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
                    int ex, ey;
                    if (dx >= dy)
                    {
                        int sx = x1 > x0 ? 1 : -1;
                        ex = x0;
                        ey = y0;
                        straight = new List<Vector2>();
                        while (true)
                        {
                            if (!heightMap.IsValidPosition(ex, ey)) break;
                            straight.Add(new Vector2(ex, ey));
                            if (!IsWaterOrWaterSlope(ex, ey, heightMap)) break;
                            if (ex == x1) break;
                            ex += sx;
                        }
                    }
                    else
                    {
                        int sy = y1 > y0 ? 1 : -1;
                        ex = x0;
                        ey = y0;
                        straight = new List<Vector2>();
                        while (true)
                        {
                            if (!heightMap.IsValidPosition(ex, ey)) break;
                            straight.Add(new Vector2(ex, ey));
                            if (!IsWaterOrWaterSlope(ex, ey, heightMap)) break;
                            if (ey == y1) break;
                            ey += sy;
                        }
                    }
                }
                foreach (var p in straight)
                    result.Add(p);
                if (j + 1 < path.Count)
                {
                    Vector2 bridgeEnd = straight[straight.Count - 1];
                    Vector2 nextLand = path[j + 1];
                    int gapX = Mathf.Abs((int)bridgeEnd.x - (int)nextLand.x);
                    int gapY = Mathf.Abs((int)bridgeEnd.y - (int)nextLand.y);
                    if (gapX > 1 || gapY > 1)
                    {
                        foreach (var p in GetCardinalPath(bridgeEnd, nextLand))
                            result.Add(p);
                        i = j + 2;
                        continue;
                    }
                }
            }
            else
            {
                result.Add(path[i]);
            }
            i = j + 1;
        }
        return result;
    }

    /// <summary>
    /// Grid cells visited by the stroke (for Bresenham chord checks: dry cliff/rim on the chord is allowed if the player path includes it).
    /// </summary>
    static HashSet<Vector2Int> BuildPathGridCellSet(List<Vector2> path)
    {
        var set = new HashSet<Vector2Int>();
        if (path == null) return set;
        for (int i = 0; i < path.Count; i++)
            set.Add(new Vector2Int(Mathf.RoundToInt(path[i].x), Mathf.RoundToInt(path[i].y)));
        return set;
    }

    /// <summary>
    /// When the polyline leaves water to dry land and re-enters the same wet corridor (e.g. bridge out, land at far bank, stroke back along the chord),
    /// <see cref="IsBridgePathValid"/> counts multiple wet runs. This verifies all wet cells lie on one horizontal or vertical line and every cell
    /// on that axis between min and max is wet/slope or part of <paramref name="pathStrokeCells"/>, so two separate river crossings stay rejected.
    /// </summary>
    bool WetStrokeCellsFormSingleAxisSpanFullyCovered(List<Vector2> path, HeightMap heightMap, HashSet<Vector2Int> pathStrokeCells)
    {
        if (path == null || heightMap == null || pathStrokeCells == null)
            return false;

        var distinctY = new HashSet<int>();
        var distinctX = new HashSet<int>();
        int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
        bool anyWet = false;

        for (int i = 0; i < path.Count; i++)
        {
            int x = (int)path[i].x, y = (int)path[i].y;
            if (!IsWaterOrWaterSlope(x, y, heightMap))
                continue;
            anyWet = true;
            distinctY.Add(y);
            distinctX.Add(x);
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }

        if (!anyWet)
            return true;

        if (distinctY.Count > 1 && distinctX.Count > 1)
            return false;

        if (distinctY.Count == 1)
        {
            int y = minY;
            for (int x = minX; x <= maxX; x++)
            {
                if (!heightMap.IsValidPosition(x, y))
                    return false;
                var gp = new Vector2Int(x, y);
                if (!IsWaterOrWaterSlope(x, y, heightMap) && !pathStrokeCells.Contains(gp))
                    return false;
            }
            return true;
        }

        if (distinctX.Count == 1)
        {
            int x = minX;
            for (int y = minY; y <= maxY; y++)
            {
                if (!heightMap.IsValidPosition(x, y))
                    return false;
                var gp = new Vector2Int(x, y);
                if (!IsWaterOrWaterSlope(x, y, heightMap) && !pathStrokeCells.Contains(gp))
                    return false;
            }
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns true if the path has valid bridge segments: normally at most one contiguous water run; if the stroke re-enters the same wet span
    /// after dry land (same-axis backtrack), <see cref="WetStrokeCellsFormSingleAxisSpanFullyCovered"/> must pass. Each bridge is axis-aligned,
    /// and each straight wet segment only passes through water/slope or dry cells on the stroke along that chord (FEAT-44).
    /// </summary>
    bool IsBridgePathValid(List<Vector2> path, HeightMap heightMap)
    {
        if (path == null || path.Count < 2 || heightMap == null) return true;

        var pathCells = BuildPathGridCellSet(path);

        int runCount = 0;
        bool inRun = false;
        for (int i = 0; i < path.Count; i++)
        {
            int x = (int)path[i].x, y = (int)path[i].y;
            bool isWaterOrSlope = IsWaterOrWaterSlope(x, y, heightMap);
            if (isWaterOrSlope && !inRun)
            {
                runCount++;
                inRun = true;
            }
            else if (!isWaterOrSlope)
            {
                inRun = false;
            }
        }
        if (runCount > 1)
        {
            if (!WetStrokeCellsFormSingleAxisSpanFullyCovered(path, heightMap, pathCells))
                return false;
        }

        inRun = false;
        int runStart = -1, runEnd = -1;
        for (int i = 0; i < path.Count; i++)
        {
            int x = (int)path[i].x, y = (int)path[i].y;
            bool isWaterOrSlope = IsWaterOrWaterSlope(x, y, heightMap);
            if (isWaterOrSlope && !inRun)
            {
                runStart = i;
                runEnd = i;
                inRun = true;
            }
            else if (isWaterOrSlope)
            {
                runEnd = i;
            }
            else
            {
                if (inRun && runEnd > runStart)
                {
                    int x0 = (int)path[runStart].x, y0 = (int)path[runStart].y;
                    int x1 = (int)path[runEnd].x, y1 = (int)path[runEnd].y;
                    bool isAxisAligned = (x0 == x1) || (y0 == y1);
                    if (!isAxisAligned) return false;

                    var straight = BresenhamStraightLine(x0, y0, x1, y1);
                    foreach (var p in straight)
                    {
                        int px = (int)p.x, py = (int)p.y;
                        if (!heightMap.IsValidPosition(px, py)) return false;
                        var gp = new Vector2Int(px, py);
                        if (!IsWaterOrWaterSlope(px, py, heightMap) && !pathCells.Contains(gp)) return false;
                    }
                }
                inRun = false;
            }
        }
        if (inRun && runEnd > runStart)
        {
            int x0 = (int)path[runStart].x, y0 = (int)path[runStart].y;
            int x1 = (int)path[runEnd].x, y1 = (int)path[runEnd].y;
            bool isAxisAligned = (x0 == x1) || (y0 == y1);
            if (!isAxisAligned) return false;

            var straight = BresenhamStraightLine(x0, y0, x1, y1);
            foreach (var p in straight)
            {
                int px = (int)p.x, py = (int)p.y;
                if (!heightMap.IsValidPosition(px, py)) return false;
                var gp = new Vector2Int(px, py);
                if (!IsWaterOrWaterSlope(px, py, heightMap) && !pathCells.Contains(gp)) return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Cardinal path from A to B (horizontal then vertical). Excludes start point.
    /// Used to connect bridge end to next land cell when axis alignment creates a gap.
    /// </summary>
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

    /// <summary>
    /// Bresenham straight line (no staircase). Used for bridge segments.
    /// </summary>
    static List<Vector2> BresenhamStraightLine(int x0, int y0, int x1, int y1)
    {
        var line = new List<Vector2>();
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
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

    /// <summary>
    /// Returns true if every consecutive pair in the path is adjacent (within 1 cell).
    /// Used to reject paths with gaps (e.g. over water) that would create loose corners.
    /// </summary>
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

    void ClearPreview()
    {
        previewResolvedTiles.Clear();

        foreach (GameObject previewTile in previewRoadTiles)
        {
            Destroy(previewTile);
        }
        previewRoadTiles.Clear();
        previewRoadGridPositions.Clear();
    }

    #endregion

    #region Road Prefab Selection
    /// <summary>
    /// Returns the correct road prefab for a cell. Delegates to RoadPrefabResolver.ResolveForCell.
    /// </summary>
    GameObject GetCorrectRoadPrefab(Vector2 prevGridPos, Vector2 currGridPos, bool isCenterRoadTile = true, bool isPreview = false, List<Vector2> path = null, int pathIndex = -1)
    {
        if (roadPrefabResolver == null && gridManager != null && terrainManager != null)
            roadPrefabResolver = new RoadPrefabResolver(gridManager, terrainManager, this);
        if (roadPrefabResolver == null) return roadTilePrefab1;
        var resolved = roadPrefabResolver.ResolveForCell(currGridPos, prevGridPos);
        return resolved.HasValue ? resolved.Value.prefab : roadTilePrefab1;
    }

    /// <summary>
    /// Returns the height of the neighbor at (gridX + dx, gridY + dy), or int.MinValue if out of bounds.
    /// Only use cardinal offsets: (dx, dy) one of (±1, 0) or (0, ±1).
    /// </summary>
    int GetNeighborHeight(int gridX, int gridY, int dx, int dy)
    {
        int nx = gridX + dx;
        int ny = gridY + dy;
        if (nx < 0 || nx >= gridManager.width || ny < 0 || ny >= gridManager.height)
            return int.MinValue;
        Cell c = gridManager.GetCell(nx, ny);
        return c != null ? c.GetCellInstanceHeight() : int.MinValue;
    }

    /// <summary>
    /// Returns the cardinal direction for the slope prefab only when there is adjacent higher ground
    /// (so we're on a slope). When we only have a lower neighbor (first flat tile after a slope),
    /// returns null so a flat road prefab is used.
    /// </summary>
    Vector2? GetTerrainSlopeDirection(Vector2 currGridPos, int currentHeight)
    {
        if (currentHeight == 0) return null;
        int x = (int)currGridPos.x;
        int y = (int)currGridPos.y;
        Vector2? directionToHigher = null;
        for (int i = 0; i < 4; i++)
        {
            int nh = GetNeighborHeight(x, y, DirX[i], DirY[i]);
            if (nh == int.MinValue) continue;
            int diff = nh - currentHeight;
            if (diff == 1)
                directionToHigher = new Vector2(DirX[i], DirY[i]);
        }
        if (!directionToHigher.HasValue) return null;
        int dxi = Mathf.RoundToInt(directionToHigher.Value.x);
        int dyi = Mathf.RoundToInt(directionToHigher.Value.y);
        bool isCardinal = (Mathf.Abs(dxi) == 1 && dyi == 0) || (dxi == 0 && Mathf.Abs(dyi) == 1);
        return isCardinal ? (Vector2?)directionToHigher.Value : null;
    }

    /// <summary>
    /// Returns true if the prefab is a diagonal road (elbow, used when route is diagonal on sloped terrain).
    /// Only these prefabs use higher positioning for correct visual integration with the slope.
    /// </summary>
    bool IsDiagonalRoadPrefab(GameObject prefab)
    {
        if (prefab == null) return false;
        return prefab == roadTilePrefabElbowUpLeft || prefab == roadTilePrefabElbowUpRight
            || prefab == roadTilePrefabElbowDownLeft || prefab == roadTilePrefabElbowDownRight;
    }

    /// <summary>
    /// Returns the world position for a road tile. For diagonal road prefabs on sloped terrain,
    /// uses the upper cell's position so the ramp renders with more height in the same cell.
    /// Orthogonal slope prefabs (East/West/North/South) use the current cell position.
    /// </summary>
    Vector2 GetRoadTileWorldPosition(int x, int y, GameObject prefab, int terrainHeight)
    {
        if (terrainHeight == 0)
            return gridManager.GetWorldPositionVector(x, y, 1);

        if (!IsDiagonalRoadPrefab(prefab))
            return gridManager.GetWorldPosition(x, y);

        int upperX = x, upperY = y;
        Vector2? slopeDir = GetTerrainSlopeDirection(new Vector2(x, y), terrainHeight);
        if (slopeDir.HasValue)
        {
            upperX = x + Mathf.RoundToInt(slopeDir.Value.x);
            upperY = y + Mathf.RoundToInt(slopeDir.Value.y);
        }
        else if (terrainManager != null)
        {
            TerrainSlopeType slopeType = terrainManager.GetTerrainSlopeTypeAt(x, y);
            switch (slopeType)
            {
                case TerrainSlopeType.SouthEast: upperX = x + 1; upperY = y + 1; break;
                case TerrainSlopeType.SouthWest: upperX = x + 1; upperY = y - 1; break;
                case TerrainSlopeType.NorthEast: upperX = x - 1; upperY = y + 1; break;
                case TerrainSlopeType.NorthWest: upperX = x - 1; upperY = y - 1; break;
                case TerrainSlopeType.SouthEastUp: upperX = x + 1; upperY = y; break;
                case TerrainSlopeType.NorthEastUp: upperX = x - 1; upperY = y; break;
                case TerrainSlopeType.SouthWestUp: upperX = x + 1; upperY = y; break;
                case TerrainSlopeType.NorthWestUp: upperX = x - 1; upperY = y; break;
                default: return gridManager.GetWorldPosition(x, y);
            }
        }
        else
        {
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

    /// <summary>
    /// Returns the correct road prefab, world position and sorting order for the single-cell ghost preview at the given grid position.
    /// Used when hovering with the road tool (no line drawn): slope cells get slope prefab, water gets bridge at height 1, else flat road.
    /// </summary>
    public void GetRoadGhostPreviewForCell(Vector2 gridPos, out GameObject prefab, out Vector2 worldPos, out int sortingOrder)
    {
        prefab = roadTilePrefab1;
        worldPos = gridManager.GetWorldPosition((int)gridPos.x, (int)gridPos.y);
        sortingOrder = gridManager.GetRoadSortingOrderForCell((int)gridPos.x, (int)gridPos.y, 0);

        if (roadPrefabResolver == null && gridManager != null && terrainManager != null)
            roadPrefabResolver = new RoadPrefabResolver(gridManager, terrainManager, this);
        if (roadPrefabResolver != null)
            roadPrefabResolver.ResolveForGhostPreview(gridPos, out prefab, out worldPos, out sortingOrder);
    }
    #endregion

    #region Road Placement
    bool IsRoadAt(Vector2 gridPos)
    {
        bool isRoad = false;
        int gridX = Mathf.RoundToInt(gridPos.x);
        int gridY = Mathf.RoundToInt(gridPos.y);

        if (gridX >= 0 && gridX < gridManager.width && gridY >= 0 && gridY < gridManager.height)
        {
            isRoad = IsAnyChildRoad(gridX, gridY);

            return isRoad;
        }

        return false;
    }

    bool IsAnyChildRoad(int gridX, int gridY)
    {
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

    void SetPreviewRoadTileDetails(GameObject previewTile)
    {
        gridManager.SetTileSortingOrder(previewTile, Zone.ZoneType.Road);
        // No Zone component on ghost tiles — avoids zoning/decor systems treating preview as real road (FEAT-44 drag).
        previewTile.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0.5f);
    }

    void SetRoadTileZoneDetails(GameObject roadTile)
    {
        Zone zone = roadTile.AddComponent<Zone>();
        zone.zoneType = Zone.ZoneType.Road;
    }

    /// <summary>
    /// Places a single road tile from a resolved prefab. Used by path pipeline (manual draw, interstate, AutoRoadBuilder).
    /// </summary>
    public void PlaceRoadTileFromResolved(RoadPrefabResolver.ResolvedRoadTile resolved)
    {
        int x = resolved.gridPos.x;
        int y = resolved.gridPos.y;
        if (gridManager.IsCellOccupiedByBuilding(x, y))
            return;

        GameObject cell = gridManager.GetGridCell(new Vector2(x, y));
        Cell cellComponentCheck = gridManager.GetCell(x, y);
        if (cellComponentCheck != null && cellComponentCheck.isInterstate)
            return;
        if (cell == null || cellComponentCheck == null) return;

        DestroyPreviousRoadTile(cell, new Vector2(x, y));
        cellComponentCheck.RemoveForestForBuilding();

        GameObject roadTile = Instantiate(resolved.prefab, resolved.worldPos, Quaternion.identity);
        roadTile.transform.SetParent(cellComponentCheck.gameObject.transform);
        roadTile.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1);

        Zone zone = roadTile.AddComponent<Zone>();
        zone.zoneType = Zone.ZoneType.Road;

        UpdateRoadCellAttributes(cellComponentCheck, roadTile, Zone.ZoneType.Road);
        ApplyRoadRouteHintsFromResolved(cellComponentCheck, resolved);
        gridManager.SetRoadSortingOrder(roadTile, x, y);
        gridManager.AddRoadToCache(resolved.gridPos);
    }

    /// <summary>Copies path route hints from resolved placement for BUG-51 route-first refresh alignment.</summary>
    static void ApplyRoadRouteHintsFromResolved(Cell cell, RoadPrefabResolver.ResolvedRoadTile resolved)
    {
        if (cell == null)
            return;
        cell.hasRoadSegmentPrevHint = resolved.hasSegmentPrevHint;
        cell.roadSegmentPrevGrid = resolved.segmentPrevGridPos;
        cell.hasRoadSegmentNextHint = resolved.hasSegmentNextHint;
        cell.roadSegmentNextGrid = resolved.segmentNextGridPos;
        cell.roadRouteEntryStep = resolved.routeEntryStep;
        cell.roadRouteExitStep = resolved.routeExitStep;
        cell.hasRoadRouteDirHints = resolved.hasSegmentPrevHint || resolved.hasSegmentNextHint
            || resolved.routeEntryStep != Vector2Int.zero || resolved.routeExitStep != Vector2Int.zero;
    }

    void PlaceRoadTile(Vector2 gridPos, int i = 0, bool isAdjacent = false)
    {
        if (gridManager.IsCellOccupiedByBuilding((int)gridPos.x, (int)gridPos.y))
            return;

        GameObject cell = gridManager.GetGridCell(gridPos);
        Cell cellComponentCheck = gridManager.GetCell((int)gridPos.x, (int)gridPos.y);
        if (cellComponentCheck != null && cellComponentCheck.isInterstate)
            return;

        bool isCenterRoadTile = !isAdjacent;
        bool isPreview = false;

        Vector2 prevGridPos = (i == 0 && previewRoadGridPositions.Count > 1)
            ? 2 * gridPos - previewRoadGridPositions[1]
            : (i == 0 ? gridPos : previewRoadGridPositions[i - 1]);

        GameObject correctRoadPrefab = GetCorrectRoadPrefab(
            prevGridPos,
            gridPos,
            isCenterRoadTile,
            isPreview,
            previewRoadGridPositions,
            i
        );

        DestroyPreviousRoadTile(cell, gridPos);

        Cell cellComponent = gridManager.GetCell((int)gridPos.x, (int)gridPos.y);
        cellComponent.RemoveForestForBuilding();
        int terrainHeight = cellComponent.GetCellInstanceHeight();
        Vector2 worldPos = GetRoadTileWorldPosition((int)gridPos.x, (int)gridPos.y, correctRoadPrefab, terrainHeight);

        GameObject roadTile = Instantiate(
            correctRoadPrefab,
            worldPos,
            Quaternion.identity
        );

        roadTile.transform.SetParent(cellComponent.gameObject.transform);

        roadTile.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1);

        Zone.ZoneType zoneType = Zone.ZoneType.Road;

        Zone zone = roadTile.AddComponent<Zone>();
        zone.zoneType = zoneType;

        UpdateRoadCellAttributes(cellComponent, roadTile, zoneType);

        gridManager.SetRoadSortingOrder(roadTile, (int)gridPos.x, (int)gridPos.y);
        gridManager.AddRoadToCache(new Vector2Int((int)gridPos.x, (int)gridPos.y));
    }

    void DestroyPreviousRoadTile(GameObject cell, Vector2 gridPos)
    {
        if (gridManager != null)
        {
            Cell cleared = gridManager.GetCell((int)gridPos.x, (int)gridPos.y);
            cleared?.ClearRoadRouteHints();
        }

        if (cell.transform.childCount > 0)
        {
            var toDestroy = new List<(GameObject go, Zone zone)>();
            foreach (Transform child in cell.transform)
            {
                Zone zone = child.GetComponent<Zone>();
                if (zone != null && zone.zoneType == Zone.ZoneType.Road)
                    toDestroy.Add((child.gameObject, zone));
            }
            foreach (var t in toDestroy)
            {
                gridManager.RemoveRoadFromCache(new Vector2Int((int)gridPos.x, (int)gridPos.y));
                Destroy(t.go);
            }
        }
    }


    void UpdateRoadCellAttributes(Cell cellComponent, GameObject roadTile, Zone.ZoneType zoneType)
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
    #endregion

    #region Road Update
    /// <summary>
    /// Returns true if a road can be placed at the given grid position (terrain, not building, not interstate).
    /// </summary>
    public bool CanPlaceRoadAt(Vector2 gridPos)
    {
        int gx = (int)gridPos.x;
        int gy = (int)gridPos.y;
        if (!terrainManager.CanPlaceRoad(gx, gy))
            return false;
        if (gridManager.IsCellOccupiedByBuilding(gx, gy))
            return false;
        Cell c = gridManager.GetCell(gx, gy);
        if (c != null && c.isInterstate)
            return false;
        return true;
    }

    bool IRoadManager.PlaceRoadTileAt(Vector2 gridPos) => PlaceRoadTileAt(gridPos);

    /// <summary>
    /// Places a single road tile at the given grid position. Uses existing road neighbors to pick prefab.
    /// Caller is responsible for affordability and budget. Returns true if placed.
    /// Updates road cache incrementally (AddRoadToCache).
    /// </summary>
    /// <param name="gridPos">Grid position to place the road.</param>
    public bool PlaceRoadTileAt(Vector2 gridPos)
    {
        if (!CanPlaceRoadAt(gridPos))
            return false;

        Vector2 prevGridPos = gridPos + new Vector2(0, 1);
        Vector2[] dirs = { new Vector2(-1, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(0, -1) };
        foreach (Vector2 d in dirs)
        {
            if (IsRoadAt(gridPos + d))
            {
                prevGridPos = gridPos + d;
                break;
            }
        }

        GameObject cell = gridManager.GetGridCell(gridPos);
        if (cell == null) return false;
        Cell cellComponentCheck = gridManager.GetCell((int)gridPos.x, (int)gridPos.y);
        if (cellComponentCheck != null && cellComponentCheck.isInterstate)
            return false;

        bool isCenterRoadTile = true;
        bool isPreview = false;
        GameObject correctRoadPrefab = GetCorrectRoadPrefab(prevGridPos, gridPos, isCenterRoadTile, isPreview);

        DestroyPreviousRoadTile(cell, gridPos);

        Cell cellComponent = gridManager.GetCell((int)gridPos.x, (int)gridPos.y);
        cellComponent.RemoveForestForBuilding();
        int terrainHeight = cellComponent.GetCellInstanceHeight();
        Vector2 worldPos = GetRoadTileWorldPosition((int)gridPos.x, (int)gridPos.y, correctRoadPrefab, terrainHeight);

        GameObject roadTile = Instantiate(correctRoadPrefab, worldPos, Quaternion.identity);
        roadTile.transform.SetParent(cellComponent.gameObject.transform);
        roadTile.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1);

        Zone zone = roadTile.AddComponent<Zone>();
        zone.zoneType = Zone.ZoneType.Road;

        UpdateRoadCellAttributes(cellComponent, roadTile, Zone.ZoneType.Road);
        gridManager.SetRoadSortingOrder(roadTile, (int)gridPos.x, (int)gridPos.y);
        gridManager.AddRoadToCache(new Vector2Int((int)gridPos.x, (int)gridPos.y));
        UpdateAdjacentRoadPrefabsAt(gridPos);
        return true;
    }

    public const int RoadCostPerTile = 50;

    /// <summary>
    /// Returns the list of all road tile prefabs.
    /// </summary>
    /// <returns>The road tile prefabs list.</returns>
    public List<GameObject> GetRoadPrefabs()
    {
        return roadTilePrefabs;
    }
    #endregion

    #region Utility Methods
    /// <summary>
    /// Returns the correct road prefab for a cell in a path (for interstate placement).
    /// When forceFlatCells contains currGridPos, returns flat road (horizontal/vertical) regardless of terrain slope.
    /// </summary>
    public GameObject GetCorrectRoadPrefabForPath(Vector2 prevGridPos, Vector2 currGridPos, HashSet<Vector2Int> forceFlatCells = null)
    {
        int gx = (int)currGridPos.x, gy = (int)currGridPos.y;
        var currInt = new Vector2Int(gx, gy);
        if (forceFlatCells != null && forceFlatCells.Contains(currInt))
        {
            Vector2 dir = currGridPos - prevGridPos;
            bool isHorizontal = Mathf.Abs(dir.x) >= Mathf.Abs(dir.y);
            GameObject flatPrefab = isHorizontal ? roadTilePrefab2 : roadTilePrefab1;
            return flatPrefab;
        }
        return GetCorrectRoadPrefab(prevGridPos, currGridPos, true, false);
    }

    /// <summary>
    /// Resolves road prefabs for a path using the terraform plan. Used by AutoRoadBuilder for path-based placement.
    /// </summary>
    public List<RoadPrefabResolver.ResolvedRoadTile> ResolvePathForRoads(List<Vector2> path, PathTerraformPlan plan)
    {
        if (roadPrefabResolver == null && gridManager != null && terrainManager != null)
            roadPrefabResolver = new RoadPrefabResolver(gridManager, terrainManager, this);
        if (roadPrefabResolver == null) return new List<RoadPrefabResolver.ResolvedRoadTile>();
        return roadPrefabResolver.ResolveForPath(path, plan);
    }

    /// <summary>
    /// Returns true if the path would be valid for interstate placement (bridge, plan, etc).
    /// Does not place. Used to evaluate candidate paths before choosing the best.
    /// </summary>
    public bool ValidateInterstatePathForPlacement(List<Vector2Int> path)
    {
        if (path == null || path.Count == 0) return false;
        if (terraformingService == null || terrainManager == null || gridManager == null) return false;

        var pathVec2 = new List<Vector2>();
        for (int i = 0; i < path.Count; i++)
            pathVec2.Add(new Vector2(path[i].x, path[i].y));

        return TryPrepareRoadPlacementPlan(pathVec2, new RoadPathValidationContext { forbidCutThrough = true }, false, out _, out _);
    }

    /// <summary>
    /// Places interstate tiles along a path using the centralized terraform + resolve pipeline.
    /// Call from InterstateManager after route generation.
    /// </summary>
    /// <returns>True if placement succeeded (plan valid and Apply succeeded).</returns>
    public bool PlaceInterstateFromPath(List<Vector2Int> path)
    {
        if (path == null || path.Count == 0)
            return false;
        if (roadPrefabResolver == null && gridManager != null && terrainManager != null)
            roadPrefabResolver = new RoadPrefabResolver(gridManager, terrainManager, this);
        if (roadPrefabResolver == null || terraformingService == null || terrainManager == null || gridManager == null)
            return false;

        var pathVec2 = new List<Vector2>();
        for (int i = 0; i < path.Count; i++)
            pathVec2.Add(new Vector2(path[i].x, path[i].y));

        var heightMap = terrainManager.GetHeightMap();
        if (heightMap == null)
            return false;

        if (!TryPrepareRoadPlacementPlan(pathVec2, new RoadPathValidationContext { forbidCutThrough = true }, false, out List<Vector2> expandedPath, out PathTerraformPlan plan))
            return false;
        if (!plan.Apply(heightMap, terrainManager))
            return false;

        var resolved = roadPrefabResolver.ResolveForPath(expandedPath, plan);
        for (int i = 0; i < resolved.Count; i++)
        {
            PlaceInterstateFromResolved(resolved[i]);
        }
        return true;
    }

    /// <summary>
    /// Places an interstate tile from a resolved road tile. Applies interstate tint and sets isInterstate on the cell.
    /// </summary>
    public void PlaceInterstateFromResolved(RoadPrefabResolver.ResolvedRoadTile resolved)
    {
        int x = resolved.gridPos.x;
        int y = resolved.gridPos.y;
        if (gridManager.IsCellOccupiedByBuilding(x, y))
            return;

        GameObject cell = gridManager.GetGridCell(new Vector2(x, y));
        Cell cellComponent = gridManager.GetCell(x, y);
        if (cell == null || cellComponent == null)
            return;

        DestroyPreviousRoadTile(cell, new Vector2(x, y));
        cellComponent.RemoveForestForBuilding();

        GameObject prefab = resolved.prefab ?? roadTilePrefab1;
        GameObject roadTile = Instantiate(prefab, resolved.worldPos, Quaternion.identity);
        roadTile.transform.SetParent(cellComponent.gameObject.transform);
        roadTile.GetComponent<SpriteRenderer>().color = new Color(0.78f, 0.78f, 0.88f, 1f);

        Zone zone = roadTile.AddComponent<Zone>();
        zone.zoneType = Zone.ZoneType.Road;
        UpdateRoadCellAttributes(cellComponent, roadTile, Zone.ZoneType.Road);
        ApplyRoadRouteHintsFromResolved(cellComponent, resolved);
        cellComponent.isInterstate = true;

        gridManager.SetRoadSortingOrder(roadTile, x, y);
        gridManager.AddRoadToCache(resolved.gridPos);
    }

    /// <summary>
    /// Place a single road tile for the interstate at currGridPos. Clears forest and applies interstate tint.
    /// </summary>
    public void PlaceInterstateTile(Vector2 prevGridPos, Vector2 currGridPos, bool isInterstate)
    {
        Vector2 gridPos = currGridPos;
        int gx = (int)gridPos.x;
        int gy = (int)gridPos.y;
        if (gridManager.IsCellOccupiedByBuilding(gx, gy)) return;

        GameObject cell = gridManager.GetGridCell(gridPos);
        Cell cellComponent = gridManager.GetCell(gx, gy);
        if (cellComponent == null) return;

        DestroyPreviousRoadTile(cell, gridPos);
        cellComponent.RemoveForestForBuilding();

        GameObject correctRoadPrefab = GetCorrectRoadPrefabForPath(prevGridPos, currGridPos);
        int terrainHeight = cellComponent.GetCellInstanceHeight();
        Vector2 worldPos = GetRoadTileWorldPosition(gx, gy, correctRoadPrefab, terrainHeight);

        GameObject roadTile = Instantiate(correctRoadPrefab, worldPos, Quaternion.identity);
        roadTile.transform.SetParent(cellComponent.gameObject.transform);

        if (isInterstate)
            roadTile.GetComponent<SpriteRenderer>().color = new Color(0.78f, 0.78f, 0.88f, 1f);
        else
            roadTile.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1);

        Zone zone = roadTile.AddComponent<Zone>();
        zone.zoneType = Zone.ZoneType.Road;
        UpdateRoadCellAttributes(cellComponent, roadTile, Zone.ZoneType.Road);
        if (isInterstate)
            cellComponent.isInterstate = true;

        gridManager.SetRoadSortingOrder(roadTile, gx, gy);
        gridManager.AddRoadToCache(new Vector2Int(gx, gy));
    }

    /// <summary>
    /// Restores a road tile from save data. Uses the exact prefab, applies interstate tint when needed,
    /// and sets correct sorting order. Call during RestoreGrid for Road cells.
    /// </summary>
    /// <param name="gridPos">Grid position to restore the road at.</param>
    /// <param name="prefab">Road prefab to instantiate (from saved prefabName).</param>
    /// <param name="isInterstate">Whether this cell is part of the interstate (applies gray tint).</param>
    /// <param name="savedSpriteSortingOrder">When set (load restore), applies persisted sorting instead of recalculating.</param>
    public void RestoreRoadTile(Vector2Int gridPos, GameObject prefab, bool isInterstate, int? savedSpriteSortingOrder = null)
    {
        GameObject cell = gridManager.GetGridCell(new Vector2(gridPos.x, gridPos.y));
        if (cell == null) return;
        Cell cellComponent = gridManager.GetCell(gridPos.x, gridPos.y);
        if (cellComponent == null) return;

        var toDestroy = new List<(GameObject go, Zone zone)>();
        foreach (Transform child in cell.transform)
        {
            Zone z = child.GetComponent<Zone>();
            if (z != null)
                toDestroy.Add((child.gameObject, z));
        }
        foreach (var t in toDestroy)
        {
            if (t.zone.zoneCategory == Zone.ZoneCategory.Zoning)
                zoneManager.removeZonedPositionFromList(new Vector2(gridPos.x, gridPos.y), t.zone.zoneType);
            Destroy(t.go);
        }

        cellComponent.RemoveForestForBuilding();

        int terrainHeight = cellComponent.GetCellInstanceHeight();
        Vector2 worldPos = GetRoadTileWorldPosition(gridPos.x, gridPos.y, prefab, terrainHeight);

        GameObject roadTile = Instantiate(prefab, worldPos, Quaternion.identity);
        roadTile.transform.SetParent(cellComponent.gameObject.transform);

        if (isInterstate)
            roadTile.GetComponent<SpriteRenderer>().color = new Color(0.78f, 0.78f, 0.88f, 1f);
        else
            roadTile.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1);

        Zone zone = roadTile.AddComponent<Zone>();
        zone.zoneType = Zone.ZoneType.Road;
        UpdateRoadCellAttributes(cellComponent, roadTile, Zone.ZoneType.Road);
        if (isInterstate)
            cellComponent.isInterstate = true;

        if (savedSpriteSortingOrder.HasValue)
        {
            SpriteRenderer sr = roadTile.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.sortingOrder = savedSpriteSortingOrder.Value;
            cellComponent.SetCellInstanceSortingOrder(savedSpriteSortingOrder.Value);
        }
        else
            gridManager.SetRoadSortingOrder(roadTile, gridPos.x, gridPos.y);
        gridManager.AddRoadToCache(gridPos);
    }

    /// <summary>
    /// Replace the road tile at the given position with a new prefab (e.g. after all interstate tiles placed to fix junctions). Preserves isInterstate and tint.
    /// Road remains at same position, so cache does not need updating.
    /// </summary>
    public void ReplaceRoadTileAt(Vector2Int gridPos, GameObject newPrefab, bool keepInterstateTint)
    {
        GameObject cell = gridManager.GetGridCell(new Vector2(gridPos.x, gridPos.y));
        if (cell == null) return;
        Cell cellComponent = gridManager.GetCell(gridPos.x, gridPos.y);
        if (cellComponent == null) return;

        string terrainPrefabName = null;
        string fallbackNonRoad = null;
        var toDestroy = new List<GameObject>();
        foreach (Transform child in cell.transform)
        {
            Zone zone = child.GetComponent<Zone>();
            if (zone != null && zone.zoneType == Zone.ZoneType.Road)
                toDestroy.Add(child.gameObject);
            else
            {
                string name = child.gameObject.name.Replace("(Clone)", "");
                if (name.Contains("Slope") || name.Contains("Grass"))
                    terrainPrefabName = name;
                else if (fallbackNonRoad == null)
                    fallbackNonRoad = name;
            }
        }
        if (terrainPrefabName == null)
            terrainPrefabName = fallbackNonRoad;
        foreach (GameObject go in toDestroy)
            Destroy(go);

        int terrainHeight = cellComponent.GetCellInstanceHeight();
        Vector2 worldPos = GetRoadTileWorldPosition(gridPos.x, gridPos.y, newPrefab, terrainHeight);

        GameObject roadTile = Instantiate(newPrefab, worldPos, Quaternion.identity);
        roadTile.transform.SetParent(cellComponent.gameObject.transform);

        if (keepInterstateTint)
            roadTile.GetComponent<SpriteRenderer>().color = new Color(0.78f, 0.78f, 0.88f, 1f);
        else
            roadTile.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1);

        Zone roadZone = roadTile.AddComponent<Zone>();
        roadZone.zoneType = Zone.ZoneType.Road;
        UpdateRoadCellAttributes(cellComponent, roadTile, Zone.ZoneType.Road);

        gridManager.SetRoadSortingOrder(roadTile, gridPos.x, gridPos.y);
    }
    #endregion
}
}
