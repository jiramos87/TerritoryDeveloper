using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System;
using Territory.Core;
using Territory.Terrain;
using Territory.Economy;
using Territory.UI;
using Territory.Zones;

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
    /// <summary>Speeds longest-prefix search during drag: try extending from last valid filtered-path length first.</summary>
    private int manualRoadLongestPrefixHint;
    /// <summary>FEAT-44 diagnostics: verbose logs only while the road cursor is on this cell.</summary>
    const int RoadBridgeDiagCursorX = 62;
    const int RoadBridgeDiagCursorY = 124;
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

    bool ShouldLogRoadBridgeDiagnostics()
    {
        return currentDrawCursorGrid.x == RoadBridgeDiagCursorX && currentDrawCursorGrid.y == RoadBridgeDiagCursorY;
    }

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
        if (gridPosition.x == RoadBridgeDiagCursorX && gridPosition.y == RoadBridgeDiagCursorY)
        {
            Debug.Log($"[RoadManager] Trying to handle road drawing for cell ({gridPosition.x}, {gridPosition.y})");
        }
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
            startPosition = pos;
            currentDrawCursorGrid = pos;
            manualRoadLongestPrefixHint = 0;
            if (uiManager != null)
                uiManager.HideGhostPreview();
            if (ShouldLogRoadBridgeDiagnostics()) 
            {
                Debug.Log($"[RoadManager] Trying to hide ghost preview for cell ({currentDrawCursorGrid.x}, {currentDrawCursorGrid.y})");
            }
        }
        else if (isDrawingRoad && Input.GetMouseButton(0))
        {
            if (ShouldLogRoadBridgeDiagnostics()) 
            {
                Debug.Log($"[RoadManager] Trying to clear preview for cell ({currentDrawCursorGrid.x}, {currentDrawCursorGrid.y})");
            }
            currentDrawCursorGrid = pos;
            ClearPreview();
            if (ShouldLogRoadBridgeDiagnostics()) 
            {
                Debug.Log($"[RoadManager] Trying to get line for path with cell ({currentDrawCursorGrid.x}, {currentDrawCursorGrid.y})");
            }
            List<Vector2> path = GetManualRoadPathWithOptionalBridgeExtension();
            if (ShouldLogRoadBridgeDiagnostics()) 
            {
                Debug.Log($"[RoadManager] Trying to draw preview line for path with cell ({currentDrawCursorGrid.x}, {currentDrawCursorGrid.y})");
            }
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
        if (ShouldLogRoadBridgeDiagnostics()) 
        {
            Debug.Log($"[RoadManager] Trying to place road at cell ({currentDrawCursorGrid.x}, {currentDrawCursorGrid.y})");
        }
        if (!TryPrepareRoadPlacementPlanLongestValidPrefix(path, new RoadPathValidationContext { forbidCutThrough = false }, false, ref manualRoadLongestPrefixHint, out List<Vector2> expandedPath, out PathTerraformPlan plan, out _))
        {
            if (ShouldLogRoadBridgeDiagnostics()) 
            {
                Debug.Log($"[RoadManager] TryPrepareRoadPlacementPlanLongestValidPrefix failed for cell ({currentDrawCursorGrid.x}, {currentDrawCursorGrid.y})");
            }
            return false;
        }

        if (ShouldLogRoadBridgeDiagnostics()) 
        {
            Debug.Log($"[RoadManager] TryPrepareRoadPlacementPlanLongestValidPrefix succeeded for cell ({currentDrawCursorGrid.x}, {currentDrawCursorGrid.y})");
        }

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
        if (!TryBuildFilteredPathForRoadPlan(pathRaw, postUserWarnings, out List<Vector2> filteredPath))
        {
            expandedPath = null;
            plan = null;
            return false;
        }

        return TryPrepareFromFilteredPathList(filteredPath, ctx, postUserWarnings, out expandedPath, out plan);
    }

    /// <summary>
    /// Like <see cref="TryPrepareRoadPlacementPlan"/> but keeps the longest prefix of the filtered path that passes terraform + Phase-1 height checks.
    /// <paramref name="longestPrefixLengthHint"/> (manual drag): pass a ref field; on success it stores the filtered-path length used; reset to 0 on new stroke.
    /// Auto-road should pass a local int with value 0.
    /// </summary>
    /// <param name="filteredPathUsedOrNull">Filtered path (before diagonal expansion) that was accepted, or null on failure.</param>
    public bool TryPrepareRoadPlacementPlanLongestValidPrefix(List<Vector2> pathRaw, RoadPathValidationContext ctx, bool postUserWarnings, ref int longestPrefixLengthHint, out List<Vector2> expandedPath, out PathTerraformPlan plan, out List<Vector2> filteredPathUsedOrNull)
    {
        expandedPath = null;
        plan = null;
        filteredPathUsedOrNull = null;
        if (!TryBuildFilteredPathForRoadPlan(pathRaw, postUserWarnings, out List<Vector2> fullFiltered))
        {
            if (ShouldLogRoadBridgeDiagnostics())
                Debug.Log("[RoadManager] TryPrepareRoadPlacementPlanLongestValidPrefix: TryBuildFilteredPathForRoadPlan failed (see prior diag log).");
            return false;
        }

        int n = fullFiltered.Count;
        var heightMap = terrainManager.GetHeightMap();
        if (heightMap == null)
        {
            if (ShouldLogRoadBridgeDiagnostics())
                Debug.Log("[RoadManager] TryPrepareRoadPlacementPlanLongestValidPrefix: heightMap is null.");
            return false;
        }

        int startK = Mathf.Min(n, longestPrefixLengthHint > 0 ? longestPrefixLengthHint + 1 : n);
        for (int k = startK; k >= 1; k--)
        {
            var prefix = SubListCopy(fullFiltered, k);
            if (ShouldLogRoadBridgeDiagnostics())
            {
                Vector2 a = prefix[0], b = prefix[prefix.Count - 1];
                bool wetSeg = TryGetSingleBridgeRunBounds(prefix, heightMap, out int diagRs, out int diagRe);
                string wetDesc = wetSeg ? $"wetIndices[{diagRs},{diagRe}]" : "no_single_wet_run";
                Debug.Log($"[RoadManager] LongestPrefix probe k={k}/{n} ({a.x},{a.y})→({b.x},{b.y}) {wetDesc} startK={startK}");
            }
            if (!IsBridgePathValid(prefix, heightMap))
            {
                if (ShouldLogRoadBridgeDiagnostics())
                    Debug.Log($"[RoadManager] LongestPrefix k={k}: rejected — IsBridgePathValid.");
                continue;
            }
            if (!ValidateFeat44WaterBridgeRules(prefix, heightMap, postUserWarnings: false))
            {
                if (ShouldLogRoadBridgeDiagnostics())
                    Debug.Log($"[RoadManager] LongestPrefix k={k}: rejected — ValidateFeat44WaterBridgeRules (details above if logged).");
                continue;
            }
            if (HasTurnOnWaterOrCoast(prefix, heightMap))
            {
                if (ShouldLogRoadBridgeDiagnostics())
                    Debug.Log($"[RoadManager] LongestPrefix k={k}: rejected — HasTurnOnWaterOrCoast.");
                continue;
            }
            if (HasElbowTooCloseToWater(prefix, heightMap))
            {
                if (ShouldLogRoadBridgeDiagnostics())
                    Debug.Log($"[RoadManager] LongestPrefix k={k}: rejected — HasElbowTooCloseToWater.");
                continue;
            }
            if (!TryPrepareFromFilteredPathList(prefix, ctx, false, out expandedPath, out plan, longestPrefixDiagK: k))
                continue;

            if (ShouldLogRoadBridgeDiagnostics())
                Debug.Log($"[RoadManager] LongestPrefix k={k}: accepted (terraform + Phase1 OK).");
            longestPrefixLengthHint = k;
            filteredPathUsedOrNull = prefix;
            return true;
        }

        longestPrefixLengthHint = 0;
        LogLongestValidPrefixExhaustedDiagnosis(fullFiltered, heightMap, ctx, startK, n);
        if (postUserWarnings && GameNotificationManager.Instance != null)
            GameNotificationManager.Instance.PostWarning("Road cannot extend further along this path. Terrain would exceed allowed height change.");
        return false;
    }

    static List<Vector2> SubListCopy(List<Vector2> full, int count)
    {
        var p = new List<Vector2>(count);
        for (int i = 0; i < count; i++)
            p.Add(full[i]);
        return p;
    }

    /// <summary>
    /// Local dev: when longest-prefix search finds no valid k, explains the first failing gate on the full filtered path (cursor 62,124 only).
    /// </summary>
    void LogLongestValidPrefixExhaustedDiagnosis(List<Vector2> fullFiltered, HeightMap heightMap, RoadPathValidationContext ctx, int startK, int n)
    {
        if (ShouldLogRoadBridgeDiagnostics())
        {
            if (fullFiltered == null || n <= 0 || heightMap == null) return;
            Vector2 first = fullFiltered[0], last = fullFiltered[n - 1];
            Debug.Log(
                $"[RoadManager] LongestValidPrefix exhausted: filteredLen={n}, tried k from {startK} down to 1. Path ({first.x},{first.y}) → ({last.x},{last.y}).");

            var probe = SubListCopy(fullFiltered, n);
            if (!IsBridgePathValid(probe, heightMap))
            {
                Debug.Log("[RoadManager] LongestValidPrefix diag (k=n): IsBridgePathValid failed.");
                return;
            }
            if (!ValidateFeat44WaterBridgeRules(probe, heightMap, postUserWarnings: false))
            {
                Debug.Log("[RoadManager] LongestValidPrefix diag (k=n): ValidateFeat44WaterBridgeRules failed.");
                return;
            }
            if (HasTurnOnWaterOrCoast(probe, heightMap))
            {
                Debug.Log("[RoadManager] LongestValidPrefix diag (k=n): HasTurnOnWaterOrCoast failed.");
                return;
            }
            if (HasElbowTooCloseToWater(probe, heightMap))
            {
                Debug.Log("[RoadManager] LongestValidPrefix diag (k=n): HasElbowTooCloseToWater failed.");
                return;
            }
            if (!TryPrepareFromFilteredPathList(probe, ctx, false, out _, out _, longestPrefixDiagK: n))
            {
                Debug.Log("[RoadManager] LongestValidPrefix diag (k=n): TryPrepareFromFilteredPathList failed (see ValidateTerraformPlan / Phase1 logs above).");
                return;
            }
            Debug.Log("[RoadManager] LongestValidPrefix diag: full length passes all gates — failure may be from prefix ordering or stale hint (unexpected).");
        }
    }

    /// <summary>
    /// Filters raw path, checks adjacency, straightens bridges, validates bridge rules. Shared by full plan and longest-prefix search.
    /// </summary>
    bool TryBuildFilteredPathForRoadPlan(List<Vector2> pathRaw, bool postUserWarnings, out List<Vector2> filteredPath)
    {
        filteredPath = null;
        if (pathRaw == null || pathRaw.Count == 0 || terraformingService == null || terrainManager == null || gridManager == null)
        {
            if (ShouldLogRoadBridgeDiagnostics())
                Debug.Log("[RoadManager] TryBuildFilteredPathForRoadPlan: rejected (null path, empty path, or missing terraformingService / terrainManager / gridManager).");
            return false;
        }

        var heightMap = terrainManager.GetHeightMap();
        if (heightMap == null)
        {
            if (ShouldLogRoadBridgeDiagnostics())
                Debug.Log("[RoadManager] TryBuildFilteredPathForRoadPlan: heightMap is null.");
            return false;
        }

        var list = new List<Vector2>();
        for (int i = 0; i < pathRaw.Count; i++)
        {
            Vector2 gridPos = pathRaw[i];
            if (gridManager.GetCell((int)gridPos.x, (int)gridPos.y) != null)
                list.Add(gridPos);
        }
        if (list.Count == 0)
        {
            if (ShouldLogRoadBridgeDiagnostics())
                Debug.Log("[RoadManager] TryBuildFilteredPathForRoadPlan: no valid cells after filtering (all missing Cell).");
            return false;
        }

        if (!IsPathFullyAdjacent(list))
        {
            if (ShouldLogRoadBridgeDiagnostics())
                Debug.Log("[RoadManager] TryBuildFilteredPathForRoadPlan: IsPathFullyAdjacent failed (gaps in path).");
            if (postUserWarnings && GameNotificationManager.Instance != null)
                GameNotificationManager.Instance.PostWarning("Road path has gaps (e.g. over water). Draw a continuous path.");
            return false;
        }

        list = StraightenBridgeSegments(list, heightMap);
        if (!IsBridgePathValid(list, heightMap))
        {
            if (ShouldLogRoadBridgeDiagnostics())
                Debug.Log("[RoadManager] TryBuildFilteredPathForRoadPlan: IsBridgePathValid failed (straight axis-aligned water/shore span or multiple runs).");
            if (postUserWarnings && GameNotificationManager.Instance != null)
                GameNotificationManager.Instance.PostWarning("Cannot build a valid bridge here. Draw a straighter path over water.");
            return false;
        }
        if (!ValidateFeat44WaterBridgeRules(list, heightMap, postUserWarnings))
        {
            if (ShouldLogRoadBridgeDiagnostics())
                Debug.Log("[RoadManager] TryBuildFilteredPathForRoadPlan: ValidateFeat44WaterBridgeRules failed (FEAT-44 deck / registered water / intersection).");
            return false;
        }
        if (HasTurnOnWaterOrCoast(list, heightMap))
        {
            if (ShouldLogRoadBridgeDiagnostics())
                Debug.Log("[RoadManager] TryBuildFilteredPathForRoadPlan: HasTurnOnWaterOrCoast failed.");
            if (postUserWarnings && GameNotificationManager.Instance != null)
                GameNotificationManager.Instance.PostWarning("Bridges must be straight. Turns cannot be on water or coast.");
            return false;
        }
        if (HasElbowTooCloseToWater(list, heightMap))
        {
            if (ShouldLogRoadBridgeDiagnostics())
                Debug.Log("[RoadManager] TryBuildFilteredPathForRoadPlan: HasElbowTooCloseToWater failed.");
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
    /// Diagonal expansion, <see cref="TerraformingService.ComputePathPlan"/>, context validation, and <see cref="PathTerraformPlan.TryValidatePhase1Heights"/>.
    /// </summary>
    /// <param name="longestPrefixDiagK">When non-negative, longest-prefix probe index for Phase1 failure logs (only emitted when the road diagnostic cursor cell is active).</param>
    bool TryPrepareFromFilteredPathList(List<Vector2> filteredPath, RoadPathValidationContext ctx, bool postUserWarnings, out List<Vector2> expandedPath, out PathTerraformPlan plan, int longestPrefixDiagK = -1)
    {
        expandedPath = null;
        plan = null;
        var heightMap = terrainManager.GetHeightMap();
        if (heightMap == null || filteredPath == null || filteredPath.Count == 0) return false;

        expandedPath = TerraformingService.ExpandDiagonalStepsToCardinal(filteredPath);
        bool waterBridgeRelax = PathQualifiesForWaterBridgeTerraformRelaxation(expandedPath);
        plan = terraformingService.ComputePathPlan(expandedPath, waterBridgeRelax);
        if (ShouldLogRoadBridgeDiagnostics()
            && expandedPath != null && plan != null && plan.pathCells != null && expandedPath.Count != plan.pathCells.Count)
        {
            Debug.LogWarning(
                $"[RoadManager] Path/plan length mismatch (BUG-30 diagnostics): expandedPath={expandedPath.Count} plan.pathCells={plan.pathCells.Count}");
        }
        if (!ValidateTerraformPlanWithContext(plan, ctx))
        {
            if (ShouldLogRoadBridgeDiagnostics())
            {
                bool cut = plan != null && plan.isValid && ctx.forbidCutThrough && plan.isCutThrough;
                Debug.Log($"[RoadManager] TryPrepareFromFilteredPathList: ValidateTerraformPlanWithContext failed (plan valid={plan?.isValid}, cutThrough={plan?.isCutThrough}, forbidCutThrough={ctx.forbidCutThrough} → interstateCutReject={cut}).");
            }
            if (postUserWarnings && GameNotificationManager.Instance != null)
            {
                if (plan != null && plan.isValid && ctx.forbidCutThrough && plan.isCutThrough)
                    GameNotificationManager.Instance.PostWarning("Interstate cannot cut through hills. Choose a different route.");
                else
                    GameNotificationManager.Instance.PostWarning("Road cannot cross terrain with height difference greater than 1. Choose a different path.");
            }
            return false;
        }

        if (!plan.TryValidatePhase1Heights(heightMap, terrainManager,
                ShouldLogRoadBridgeDiagnostics() ? (msg => Debug.Log($"[PathTerraformPlan] {msg}")) : null))
        {
            if (ShouldLogRoadBridgeDiagnostics())
            {
                bool anyTerraform = PathPlanHasNonNoneTerraformAction(plan);
                string kTag = longestPrefixDiagK >= 0 ? $"longestPrefixK={longestPrefixDiagK} " : string.Empty;
                Debug.Log(
                    $"[RoadManager] TryPrepareFromFilteredPathList: TryValidatePhase1Heights failed ({kTag}filteredLen={filteredPath.Count} expandedLen={expandedPath?.Count ?? 0} " +
                    $"waterBridgeRelax={waterBridgeRelax} plan.waterBridgeTerraformRelaxation={plan?.waterBridgeTerraformRelaxation ?? false} " +
                    $"planValid={plan?.isValid ?? false} cutThrough={plan?.isCutThrough ?? false} pathCells={plan?.pathCells?.Count ?? 0} " +
                    $"anyNonNoneTerraformAction={anyTerraform}).");
            }
            if (postUserWarnings && GameNotificationManager.Instance != null)
                GameNotificationManager.Instance.PostWarning("Terrain cannot be modified safely (height difference would exceed 1). Choose a different path.");
            return false;
        }

        return true;
    }

    static bool PathPlanHasNonNoneTerraformAction(PathTerraformPlan plan)
    {
        if (plan == null) return false;
        if (plan.pathCells != null)
        {
            for (int i = 0; i < plan.pathCells.Count; i++)
            {
                if (plan.pathCells[i].action != TerraformingService.TerraformAction.None)
                    return true;
            }
        }
        if (plan.adjacentCells != null)
        {
            for (int i = 0; i < plan.adjacentCells.Count; i++)
            {
                if (plan.adjacentCells[i].action != TerraformingService.TerraformAction.None)
                    return true;
            }
        }
        return false;
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
    /// Picks a road neighbor to use as "previous" cell for prefab resolution. Deterministic when several roads touch (straight-through vs crossing).
    /// </summary>
    Vector2 PickPrevGridPosForRoadRefresh(Vector2 gridPos)
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

        Vector2 prevGridPos = PickPrevGridPosForRoadRefresh(gridPos);

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
        if (ShouldLogRoadBridgeDiagnostics()) 
        {
            Debug.Log($"[RoadManager] Trying to draw preview line for path with cell ({currentDrawCursorGrid.x}, {currentDrawCursorGrid.y})");
        }
        if (path == null || path.Count == 0) return;
        if (terraformingService == null || terrainManager == null || gridManager == null) return;
        if (roadPrefabResolver == null)
            roadPrefabResolver = new RoadPrefabResolver(gridManager, terrainManager, this);
        if (roadPrefabResolver == null) return;

        if (ShouldLogRoadBridgeDiagnostics()) 
        {
            Debug.Log($"[RoadManager] Trying to prepare road placement plan for path with cell ({currentDrawCursorGrid.x}, {currentDrawCursorGrid.y})");
        }
        if (!TryPrepareRoadPlacementPlanLongestValidPrefix(path, new RoadPathValidationContext { forbidCutThrough = false }, false, ref manualRoadLongestPrefixHint, out List<Vector2> expandedPath, out PathTerraformPlan plan, out _))
        {
            if (ShouldLogRoadBridgeDiagnostics()) 
            {
                Debug.Log($"[RoadManager] failed to prepare road placement plan for path with cell ({currentDrawCursorGrid.x}, {currentDrawCursorGrid.y})");
            }
            return;
        }

        if (ShouldLogRoadBridgeDiagnostics()) 
        {
            Debug.Log($"[RoadManager] successfuly prepared road placement plan for path with cell ({currentDrawCursorGrid.x}, {currentDrawCursorGrid.y})");
        }

        var resolved = roadPrefabResolver.ResolveForPath(expandedPath, plan);
        previewResolvedTiles.Clear();
        previewResolvedTiles.AddRange(resolved);

        if (ShouldLogRoadBridgeDiagnostics()) 
        {
            Debug.Log($"[RoadManager] successfuly resolved road placement plan for path with cell ({currentDrawCursorGrid.x}, {currentDrawCursorGrid.y})");
        }

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
        List<Vector2> raw = GetLine(startPosition, currentDrawCursorGrid);
        List<Vector2> extended = TryExtendPathAcrossWaterToOppositeLand(raw);
        if (extended != null)
        {
            if (ShouldLogRoadBridgeDiagnostics())
            {
                Debug.Log(
                    $"[RoadManager] GetManualRoadPathWithOptionalBridgeExtension: extended rawLen={raw.Count} → extendedLen={extended.Count} " +
                    $"(far-bank land appended for FEAT-44).");
            }
            manualRoadLongestPrefixHint = 0;
            return extended;
        }
        if (ShouldLogRoadBridgeDiagnostics() && raw != null && raw.Count > 0)
        {
            int lx = (int)raw[raw.Count - 1].x, ly = (int)raw[raw.Count - 1].y;
            var hm = terrainManager != null ? terrainManager.GetHeightMap() : null;
            bool endsWet = hm != null && IsWaterOrWaterSlope(lx, ly, hm);
            Debug.Log(
                $"[RoadManager] GetManualRoadPathWithOptionalBridgeExtension: no extension (rawLen={raw.Count} last=({lx},{ly}) endsOnWaterOrShore={endsWet}). " +
                "See TryExtendPathAcrossWaterToOppositeLand logs for skip reason when stroke ends wet.");
        }
        return raw;
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
        {
            if (ShouldLogRoadBridgeDiagnostics())
                Debug.Log("[RoadManager] TryExtendPathAcrossWaterToOppositeLand: skip (null path, len<2, or missing heightMap/gridManager/terrainManager).");
            return null;
        }

        int n = path.Count;
        int lx = (int)path[n - 1].x;
        int ly = (int)path[n - 1].y;
        if (!IsWaterOrWaterSlope(lx, ly, heightMap))
            return null;

        int wetStart = n - 1;
        while (wetStart > 0 && IsWaterOrWaterSlope((int)path[wetStart - 1].x, (int)path[wetStart - 1].y, heightMap))
            wetStart--;

        if (wetStart == 0)
        {
            if (ShouldLogRoadBridgeDiagnostics())
                Debug.Log("[RoadManager] TryExtendPathAcrossWaterToOppositeLand: skip wet run reaches path index 0 (no dry land-before anchor).");
            return null;
        }

        int bx = (int)path[wetStart - 1].x;
        int by = (int)path[wetStart - 1].y;
        if (IsWaterOrWaterSlope(bx, by, heightMap))
        {
            if (ShouldLogRoadBridgeDiagnostics())
                Debug.Log($"[RoadManager] TryExtendPathAcrossWaterToOppositeLand: skip cell before wet run still wet at ({bx},{by}).");
            return null;
        }

        int fwx = (int)path[wetStart].x;
        int fwy = (int)path[wetStart].y;
        int sdx = fwx - bx;
        int sdy = fwy - by;
        if (Mathf.Abs(sdx) > 1 || Mathf.Abs(sdy) > 1 || (sdx != 0 && sdy != 0))
        {
            if (ShouldLogRoadBridgeDiagnostics())
                Debug.Log(
                    $"[RoadManager] TryExtendPathAcrossWaterToOppositeLand: skip land→first-wet not single cardinal step land=({bx},{by}) firstWet=({fwx},{fwy}) delta=({sdx},{sdy}).");
            return null;
        }

        int dx = sdx != 0 ? (int)Mathf.Sign(sdx) : 0;
        int dy = sdy != 0 ? (int)Mathf.Sign(sdy) : 0;
        if (dx == 0 && dy == 0)
        {
            if (ShouldLogRoadBridgeDiagnostics())
                Debug.Log("[RoadManager] TryExtendPathAcrossWaterToOppositeLand: skip zero bridge axis delta.");
            return null;
        }

        Cell landBeforeCell = gridManager.GetCell(bx, by);
        if (landBeforeCell == null)
        {
            if (ShouldLogRoadBridgeDiagnostics())
                Debug.Log($"[RoadManager] TryExtendPathAcrossWaterToOppositeLand: skip null Cell at land-before ({bx},{by}).");
            return null;
        }
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
            {
                if (ShouldLogRoadBridgeDiagnostics())
                    Debug.Log($"[RoadManager] TryExtendPathAcrossWaterToOppositeLand: abort off map at step={step} from=({cx},{cy}) next=({nx},{ny}) axis=({dx},{dy}).");
                break;
            }

            if (IsWaterOrWaterSlope(nx, ny, heightMap))
            {
                if ((int)result[result.Count - 1].x != nx || (int)result[result.Count - 1].y != ny)
                    result.Add(new Vector2(nx, ny));
                cx = nx;
                cy = ny;
                continue;
            }

            if (gridManager.IsCellOccupiedByBuilding(nx, ny))
            {
                if (ShouldLogRoadBridgeDiagnostics())
                    Debug.Log($"[RoadManager] TryExtendPathAcrossWaterToOppositeLand: abort building at ({nx},{ny}) after step={step}.");
                break;
            }
            Cell farCell = gridManager.GetCell(nx, ny);
            if (farCell == null)
            {
                if (ShouldLogRoadBridgeDiagnostics())
                    Debug.Log($"[RoadManager] TryExtendPathAcrossWaterToOppositeLand: abort no Cell at ({nx},{ny}).");
                break;
            }
            if (farCell.isInterstate)
            {
                if (ShouldLogRoadBridgeDiagnostics())
                    Debug.Log($"[RoadManager] TryExtendPathAcrossWaterToOppositeLand: abort interstate at ({nx},{ny}).");
                break;
            }
            if (!terrainManager.CanPlaceRoad(nx, ny))
            {
                if (ShouldLogRoadBridgeDiagnostics())
                    Debug.Log($"[RoadManager] TryExtendPathAcrossWaterToOppositeLand: abort CanPlaceRoad false at ({nx},{ny}).");
                break;
            }
            if (farCell.GetCellInstanceHeight() != bridgeHeight)
            {
                if (ShouldLogRoadBridgeDiagnostics())
                    Debug.Log(
                        $"[RoadManager] TryExtendPathAcrossWaterToOppositeLand: abort height mismatch at ({nx},{ny}) farH={farCell.GetCellInstanceHeight()} bridgeH={bridgeHeight}.");
                break;
            }

            result.Add(new Vector2(nx, ny));
            if (ShouldLogRoadBridgeDiagnostics())
                Debug.Log($"[RoadManager] TryExtendPathAcrossWaterToOppositeLand: success appended land ({nx},{ny}) bridgeH={bridgeHeight} stepsFromCursorWet={step + 1}.");
            return result;
        }

        if (ShouldLogRoadBridgeDiagnostics())
            Debug.Log(
                $"[RoadManager] TryExtendPathAcrossWaterToOppositeLand: failed after scan lastWet=({cx},{cy}) axis=({dx},{dy}) bridgeH={bridgeHeight} (no opposite-bank land tile).");
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

        var straight = BresenhamStraightLine((int)path[runStart].x, (int)path[runStart].y, (int)path[runEnd].x, (int)path[runEnd].y);
        if (ShouldLogRoadBridgeDiagnostics())
        {
            Debug.Log(
                $"[RoadManager] ValidateFeat44: runStart={runStart} runEnd={runEnd} straightLen={straight.Count} runFrom=({path[runStart].x},{path[runStart].y}) runTo=({path[runEnd].x},{path[runEnd].y})");
        }

        foreach (var p in straight)
        {
            int px = (int)p.x, py = (int)p.y;
            if (!heightMap.IsValidPosition(px, py))
            {
                if (ShouldLogRoadBridgeDiagnostics())
                    Debug.Log($"[RoadManager] ValidateFeat44 FAIL: Bresenham cell ({px},{py}) invalid map position.");
                return FailFeat44(postUserWarnings, "Bridge span leaves the map.");
            }
            if (!IsWaterRelatedBridgeInteriorCell(px, py, heightMap, waterManager))
            {
                if (ShouldLogRoadBridgeDiagnostics())
                {
                    bool wos = IsWaterOrWaterSlope(px, py, heightMap);
                    bool slope = terrainManager != null && terrainManager.IsWaterSlopeCell(px, py);
                    bool openW = IsOpenWaterCellForBridge(px, py, heightMap, waterManager);
                    Debug.Log(
                        $"[RoadManager] ValidateFeat44 FAIL: Bresenham ({px},{py}) not water-related interior (IsWaterOrWaterSlope={wos}, IsWaterSlopeCell={slope}, IsOpenWaterCellForBridge={openW}).");
                }
                return FailFeat44(postUserWarnings, "Bridges may only cross registered water and shore, not dry gaps.");
            }
        }

        int landBefore = runStart - 1;
        int landAfter = runEnd + 1;
        if (landBefore < 0 || landAfter >= path.Count)
        {
            if (ShouldLogRoadBridgeDiagnostics())
            {
                int lastIdx = path.Count - 1;
                Debug.Log(
                    $"[RoadManager] ValidateFeat44 FAIL: land endpoints missing (landBeforeIdx={landBefore}, landAfterIdx={landAfter}, path.Count={path.Count}). " +
                    $"wetRunIndices=[{runStart},{runEnd}] lastPathCell=({path[lastIdx].x},{path[lastIdx].y}) " +
                    (landAfter >= path.Count
                        ? "Reason: path ends inside wet run — need dry land cell after last water/shore on stroke (extension or longer drag)."
                        : "Reason: wet run touches start of path — need land before first wet cell."));
            }
            return FailFeat44(postUserWarnings, "A water bridge needs land at both ends.");
        }
        if (IsWaterOrWaterSlope((int)path[landBefore].x, (int)path[landBefore].y, heightMap)
            || IsWaterOrWaterSlope((int)path[landAfter].x, (int)path[landAfter].y, heightMap))
        {
            if (ShouldLogRoadBridgeDiagnostics())
            {
                Debug.Log(
                    $"[RoadManager] ValidateFeat44 FAIL: endpoint still water/slope — before=({path[landBefore].x},{path[landBefore].y}) wos={IsWaterOrWaterSlope((int)path[landBefore].x, (int)path[landBefore].y, heightMap)}, after=({path[landAfter].x},{path[landAfter].y}) wos={IsWaterOrWaterSlope((int)path[landAfter].x, (int)path[landAfter].y, heightMap)}");
            }
            return FailFeat44(postUserWarnings, "A water bridge needs land at both ends.");
        }

        Cell cellBefore = gridManager.GetCell((int)path[landBefore].x, (int)path[landBefore].y);
        Cell cellAfter = gridManager.GetCell((int)path[landAfter].x, (int)path[landAfter].y);
        if (cellBefore == null || cellAfter == null)
        {
            if (ShouldLogRoadBridgeDiagnostics())
                Debug.Log($"[RoadManager] ValidateFeat44 FAIL: null Cell (beforeNull={cellBefore == null}, afterNull={cellAfter == null}) at landBefore=({path[landBefore].x},{path[landBefore].y}) landAfter=({path[landAfter].x},{path[landAfter].y}).");
            return false;
        }
        int bridgeHeight = cellBefore.GetCellInstanceHeight();
        if (cellAfter.GetCellInstanceHeight() != bridgeHeight)
        {
            if (ShouldLogRoadBridgeDiagnostics())
            {
                Debug.Log(
                    $"[RoadManager] ValidateFeat44 FAIL: bridge end heights differ — before=({path[landBefore].x},{path[landBefore].y}) h={bridgeHeight}, after=({path[landAfter].x},{path[landAfter].y}) h={cellAfter.GetCellInstanceHeight()}.");
            }
            return FailFeat44(postUserWarnings, "Bridge ends must be at the same terrain height.");
        }

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
            {
                if (ShouldLogRoadBridgeDiagnostics())
                {
                    Debug.Log(
                        $"[RoadManager] ValidateFeat44 FAIL: deck below S/bed at ({x},{y}) bridgeHeight={bridgeHeight} surfaceS={surfaceS} bed={bed}.");
                }
                return FailFeat44(postUserWarnings, "Bridge deck would sit below the water surface or bed here.");
            }
        }

        for (int i = runStart; i <= runEnd; i++)
        {
            int x = (int)path[i].x, y = (int)path[i].y;
            if (!IsWaterOrWaterSlope(x, y, heightMap)) continue;
            if (!IsRoadAt(new Vector2(x, y))) continue;
            if (!TryGetDominantRoadAxisAt(x, y, out bool existingHorizontal, out bool isJunction))
                continue;
            if (isJunction)
            {
                if (ShouldLogRoadBridgeDiagnostics())
                    Debug.Log($"[RoadManager] ValidateFeat44 FAIL: existing road junction on wet cell ({x},{y}).");
                return FailFeat44(postUserWarnings, "Crossing or joining bridge spans here is not supported.");
            }
            if (existingHorizontal != spanHorizontal)
            {
                if (ShouldLogRoadBridgeDiagnostics())
                {
                    Debug.Log(
                        $"[RoadManager] ValidateFeat44 FAIL: orthogonal bridge overlap at ({x},{y}) existingHorizontal={existingHorizontal} newSpanHorizontal={spanHorizontal}.");
                }
                return FailFeat44(postUserWarnings, "Another bridge already crosses this water at a different angle.");
            }
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
    /// True if the path has an elbow (turn) that is on water-slope or within 2 cells of water. Invalid.
    /// </summary>
    bool HasElbowTooCloseToWater(List<Vector2> path, HeightMap heightMap)
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
    /// Returns true if the path has valid bridge segments: at most one water run per crossing,
    /// each bridge is axis-aligned (horizontal or vertical), and each straight line only passes
    /// through water or water-slope cells.
    /// </summary>
    bool IsBridgePathValid(List<Vector2> path, HeightMap heightMap)
    {
        if (path == null || path.Count < 2 || heightMap == null) return true;

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
        if (runCount > 1) return false;

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
                        if (!IsWaterOrWaterSlope(px, py, heightMap)) return false;
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
                if (!IsWaterOrWaterSlope(px, py, heightMap)) return false;
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
        gridManager.SetRoadSortingOrder(roadTile, x, y);
        gridManager.AddRoadToCache(resolved.gridPos);
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
