using UnityEngine;
using System.Collections.Generic;
using Territory.Core;
using Territory.Terrain;
using Territory.Economy;
using Territory.Persistence;
using Territory.UI;
using Territory.Zones;
using Domains.Roads.Services;

namespace Territory.Roads
{
/// <summary>
/// Thin MonoBehaviour facade. Holds inspector refs + prefab fields. All logic delegated to RoadPlacementService (Stage 4.0 THIN).
/// Invariant #2 (InvalidateRoadCache) owned by RoadPlacementService.
/// Invariant #10 (PathTerraformPlan family) owned by RoadPlacementService.
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
    [SerializeField] private GameSaveManager gameSaveManager;
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

    // IRoadManager prefab properties.
    GameObject IRoadManager.roadTilePrefab1 => roadTilePrefab1;
    GameObject IRoadManager.roadTilePrefab2 => roadTilePrefab2;
    GameObject IRoadManager.roadTilePrefabCrossing => roadTilePrefabCrossing;
    GameObject IRoadManager.roadTilePrefabTIntersectionUp => roadTilePrefabTIntersectionUp;
    GameObject IRoadManager.roadTilePrefabTIntersectionDown => roadTilePrefabTIntersectionDown;
    GameObject IRoadManager.roadTilePrefabTIntersectionLeft => roadTilePrefabTIntersectionLeft;
    GameObject IRoadManager.roadTilePrefabTIntersectionRight => roadTilePrefabTIntersectionRight;
    GameObject IRoadManager.roadTilePrefabElbowUpLeft => roadTilePrefabElbowUpLeft;
    GameObject IRoadManager.roadTilePrefabElbowUpRight => roadTilePrefabElbowUpRight;
    GameObject IRoadManager.roadTilePrefabElbowDownLeft => roadTilePrefabElbowDownLeft;
    GameObject IRoadManager.roadTilePrefabElbowDownRight => roadTilePrefabElbowDownRight;
    GameObject IRoadManager.roadTileBridgeVertical => roadTileBridgeVertical;
    GameObject IRoadManager.roadTileBridgeHorizontal => roadTileBridgeHorizontal;
    GameObject IRoadManager.roadTilePrefabEastSlope => roadTilePrefabEastSlope;
    GameObject IRoadManager.roadTilePrefabWestSlope => roadTilePrefabWestSlope;
    GameObject IRoadManager.roadTilePrefabNorthSlope => roadTilePrefabNorthSlope;
    GameObject IRoadManager.roadTilePrefabSouthSlope => roadTilePrefabSouthSlope;
    #endregion

    #region Service
    private RoadPlacementService _service;

    public void Initialize()
    {
        _service = new RoadPlacementService(gridManager, terrainManager, terraformingService, this);
        roadTilePrefabs = new List<GameObject>
        {
            roadTilePrefab1, roadTilePrefab2, roadTilePrefabCrossing,
            roadTilePrefabTIntersectionUp, roadTilePrefabTIntersectionDown,
            roadTilePrefabTIntersectionLeft, roadTilePrefabTIntersectionRight,
            roadTilePrefabElbowUpLeft, roadTilePrefabElbowUpRight,
            roadTilePrefabElbowDownLeft, roadTilePrefabElbowDownRight,
            roadTileBridgeVertical, roadTileBridgeHorizontal,
            roadTilePrefabEastSlope, roadTilePrefabWestSlope,
            roadTilePrefabNorthSlope, roadTilePrefabSouthSlope
        };
    }

    void Awake()
    {
        if (gameSaveManager == null)
            gameSaveManager = FindObjectOfType<GameSaveManager>();
    }

    private RoadPlacementService Svc()
    {
        if (_service == null)
            _service = new RoadPlacementService(gridManager, terrainManager, terraformingService, this);
        return _service;
    }
    #endregion

    #region Public API — single-line delegates
    public void HandleRoadDrawing(Vector2 gridPosition) => Svc().HandleRoadDrawing(gridPosition, uiManager, cityStats);
    public bool IsDrawingRoad() => Svc().IsDrawingRoad();
    public int GetPreviewRoadTileCount() => Svc().GetPreviewRoadTileCount();
    public int GetRoadCostPerTile() => Svc().GetRoadCostPerTile();
    public int GetRoadCostForTileCount(int tilesCount) => Svc().GetRoadCostForTileCount(tilesCount);
    public bool ValidateTerraformPlanWithContext(PathTerraformPlan plan, RoadPathValidationContext ctx) => Svc().ValidateTerraformPlanWithContext(plan, ctx);
    public bool TryPrepareRoadPlacementPlan(List<Vector2> pathRaw, RoadPathValidationContext ctx, bool postUserWarnings, out List<Vector2> expandedPath, out PathTerraformPlan plan) => Svc().TryPrepareRoadPlacementPlan(pathRaw, ctx, postUserWarnings, out expandedPath, out plan);
    public bool TryPrepareRoadPlacementPlanLongestValidPrefix(List<Vector2> pathRaw, RoadPathValidationContext ctx, bool postUserWarnings, ref int longestPrefixLengthHint, out List<Vector2> expandedPath, out PathTerraformPlan plan, out List<Vector2> filteredPathUsedOrNull) => Svc().TryPrepareRoadPlacementPlanLongestValidPrefix(pathRaw, ctx, postUserWarnings, ref longestPrefixLengthHint, out expandedPath, out plan, out filteredPathUsedOrNull);
    public bool TryPrepareRoadPlacementPlanWithProgrammaticDeckSpanChord(List<Vector2> straightCardinalPath, Vector2Int segmentDir, RoadPathValidationContext ctx, out List<Vector2> expandedPath, out PathTerraformPlan plan) => Svc().TryPrepareRoadPlacementPlanWithProgrammaticDeckSpanChord(straightCardinalPath, segmentDir, ctx, out expandedPath, out plan);
    public bool StrokeLastCellIsFirmDryLand(IList<Vector2> stroke) => Svc().StrokeLastCellIsFirmDryLand(stroke);
    public bool StrokeHasWaterOrWaterSlopeCells(IList<Vector2> stroke) => Svc().StrokeHasWaterOrWaterSlopeCells(stroke);
    public bool TryExtendCardinalStreetPathWithBridgeChord(List<Vector2> pathVec2, Vector2Int dir) => Svc().TryExtendCardinalStreetPathWithBridgeChord(pathVec2, dir);
    public bool TryCommitStreetStrokeForScenarioBuild(List<Vector2> pathRaw, out string error) => Svc().TryCommitStreetStrokeForScenarioBuild(pathRaw, out error, cityStats);
    public void PlaceRoadTileFromResolved(ResolvedRoadTile resolved) => Svc().PlaceRoadTileFromResolved(resolved);
    public bool CanPlaceRoadAt(Vector2 gridPos) => Svc().CanPlaceRoadAt(gridPos);
    public bool PlaceRoadTileAt(Vector2 gridPos) => Svc().PlaceRoadTileAt(gridPos);
    public void UpdateAdjacentRoadPrefabsAt(Vector2 gridPos) => Svc().UpdateAdjacentRoadPrefabsAt(gridPos);
    public void RefreshRoadPrefabsAfterBatchPlacement(IReadOnlyList<Vector2Int> newlyPlacedRoadCells) => Svc().RefreshRoadPrefabsAfterBatchPlacement(newlyPlacedRoadCells);
    public bool ValidateBridgePath(List<Vector2Int> path, HeightMap heightMap) => Svc().ValidateBridgePath(path, heightMap);
    public void GetRoadGhostPreviewForCell(Vector2 gridPos, out GameObject prefab, out Vector2 worldPos, out int sortingOrder) => Svc().GetRoadGhostPreviewForCell(gridPos, out prefab, out worldPos, out sortingOrder);
    public GameObject GetCorrectRoadPrefabForPath(Vector2 prevGridPos, Vector2 currGridPos, HashSet<Vector2Int> forceFlatCells = null) => Svc().GetCorrectRoadPrefabForPath(prevGridPos, currGridPos, forceFlatCells);
    public List<ResolvedRoadTile> ResolvePathForRoads(List<Vector2> path, PathTerraformPlan plan) => Svc().ResolvePathForRoads(path, plan);
    public bool ValidateInterstatePathForPlacement(List<Vector2Int> path) => Svc().ValidateInterstatePathForPlacement(path);
    public bool PlaceInterstateFromPath(List<Vector2Int> path) => Svc().PlaceInterstateFromPath(path, gameSaveManager, interstateManager);
    public void PlaceInterstateFromResolved(ResolvedRoadTile resolved) => Svc().PlaceInterstateFromResolved(resolved);
    public void PlaceInterstateTile(Vector2 prevGridPos, Vector2 currGridPos, bool isInterstate) => Svc().PlaceInterstateTile(prevGridPos, currGridPos, isInterstate);
    public void RestoreRoadTile(Vector2Int gridPos, GameObject prefab, bool isInterstate, int? savedSpriteSortingOrder = null) => Svc().RestoreRoadTile(gridPos, prefab, isInterstate, zoneManager, savedSpriteSortingOrder);
    public void ReplaceRoadTileAt(Vector2Int gridPos, GameObject newPrefab, bool keepInterstateTint) => Svc().ReplaceRoadTileAt(gridPos, newPrefab, keepInterstateTint);
    public List<GameObject> GetRoadPrefabs() => Svc().GetRoadPrefabs();
    public const int RoadCostPerTile = 50;
    #endregion
}
}
