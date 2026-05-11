using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;
using Territory.Core;
using Territory.Zones;
using Territory.Persistence;

namespace Territory.Terrain
{
// TerrainSlopeType enum moved to Territory.Core leaf (Assets/Scripts/Core/Terrain/TerrainSlopeType.cs) — Stage 20.
// Stage 3.1: hub thinned to ≤200 LOC; private impl in TerrainManager.Impl.cs (partial).

/// <summary>
/// Thin hub: public API delegates to HeightWriteService / TerrainQueryService / TerrainInitService.
/// All field declarations, [SerializeField] sets, and invariant #2/#3 constraints preserved verbatim.
/// Private implementation: TerrainManager.Impl.cs (partial class).
/// </summary>
public partial class TerrainManager : MonoBehaviour, ITerrainManager
{
    // Diagnostics
    public static bool LogTerraformRestoreDiagnostics = false;

    #region Dependencies
    public GridManager gridManager;
    private HeightMap heightMap;
    public ZoneManager zoneManager;
    public WaterManager waterManager;
    /// <summary>ITerrainManager.Water — IWaterManager surface for Domains.* core-leaf consumers.</summary>
    public IWaterManager Water => waterManager;
    #endregion

    #region Slope Prefabs
    public GameObject northSlopePrefab;
    public GameObject southSlopePrefab;
    public GameObject eastSlopePrefab;
    public GameObject westSlopePrefab;
    public GameObject northEastSlopePrefab;
    public GameObject northWestSlopePrefab;
    public GameObject southEastSlopePrefab;
    public GameObject southWestSlopePrefab;
    public GameObject northEastUpslopePrefab;
    public GameObject northWestUpslopePrefab;
    public GameObject southEastUpslopePrefab;
    public GameObject southWestUpslopePrefab;
    #endregion

    #region Water Slope Prefabs
    public GameObject northSlopeWaterPrefab;
    public GameObject southSlopeWaterPrefab;
    public GameObject eastSlopeWaterPrefab;
    public GameObject westSlopeWaterPrefab;
    public GameObject northEastSlopeWaterPrefab;
    public GameObject northWestSlopeWaterPrefab;
    public GameObject southEastSlopeWaterPrefab;
    public GameObject southWestSlopeWaterPrefab;
    public GameObject northEastUpslopeWaterPrefab;
    public GameObject northWestUpslopeWaterPrefab;
    public GameObject southEastUpslopeWaterPrefab;
    public GameObject southWestUpslopeWaterPrefab;
    public GameObject seaLevelWaterPrefab;
    public GameObject southCliffWallPrefab;
    public GameObject eastCliffWallPrefab;
    public GameObject northCliffWallPrefab;
    public GameObject westCliffWallPrefab;
    [Header("Water–water cascade cliffs (BUG-42)")]
    public GameObject cliffWaterSouthPrefab;
    public GameObject cliffWaterEastPrefab;
    [Header("Cliff wall placement")]
    [Tooltip("Extra downward shift in world Y after grid math, in units of (tileHeight/2).")]
    [SerializeField] private float cliffWallPivotDownHeightSteps = 1.5f;
    [Tooltip("World X offset for South cliff art vs shared-edge midpoint, as a fraction of tileWidth.")]
    [SerializeField] private float cliffWallSouthFaceNudgeTileWidthFraction = 1.0f;
    [Tooltip("World Y offset for South cliff art vs shared-edge midpoint, as a fraction of tileHeight.")]
    [SerializeField] private float cliffWallSouthFaceNudgeTileHeightFraction = 1.0f;
    [Tooltip("World X offset for East cliff art vs shared-edge midpoint, as a fraction of tileWidth.")]
    [SerializeField] private float cliffWallEastFaceNudgeTileWidthFraction = -1.0f;
    [Tooltip("World Y offset for East cliff art vs shared-edge midpoint, as a fraction of tileHeight.")]
    [SerializeField] private float cliffWallEastFaceNudgeTileHeightFraction = 1.0f;
    [Tooltip("Extra Y drop for water-shore cliff when lower neighbor is on-grid, as fraction of tileHeight.")]
    [SerializeField] private float cliffWallWaterShoreYOffsetTileHeightFraction = 1.0f;
    public GameObject northEastBayPrefab;
    public GameObject northWestBayPrefab;
    public GameObject southEastBayPrefab;
    public GameObject southWestBayPrefab;
    #endregion

    #region Configuration
    public const int MIN_HEIGHT = 0;
    public const int MAX_HEIGHT = 5;
    public const int SEA_LEVEL = 0;
    public const int MAX_LAND_HEIGHT_ABOVE_ADJACENT_WATER_SURFACE_FOR_SHORE_PREFABS = 1;
    public const int TERRAIN_BASE_ORDER = 0;
    public const int SLOPE_OFFSET = 1;
    public const int BUILDING_OFFSET = 10;
    public const int EFFECT_OFFSET = 30;
    public const int DEPTH_MULTIPLIER = 100;
    public const int HEIGHT_MULTIPLIER = 10;
    private bool newGameFlatTerrainEnabled;
    private int newGameFlatTerrainHeight = 1;
    #endregion

    // — Public API: single-line delegates to services (invariant #2 path/namespace unchanged) —

    public GameObject FindTerrainPrefabByName(string prefabName) => FindTerrainPrefabByNameImpl(prefabName);
    public bool IsRegisteredOpenWaterAt(int x, int y) => IsRegisteredOpenWaterAtImpl(x, y);
    public bool ShouldSkipRoadTerraformSurfaceAt(int x, int y, HeightMap map) => ShouldSkipRoadTerraformSurfaceAtImpl(x, y, map);
    public void SetNewGameFlatTerrainOptions(bool enabled, int uniformHeight) => SetNewGameFlatTerrainOptionsImpl(enabled, uniformHeight);
    public void StartTerrainGeneration() => StartTerrainGenerationImpl();
    public HeightMap GetHeightMap() => heightMap;
    public HeightMap GetOrCreateHeightMap() => GetOrCreateHeightMapImpl();
    public void InitializeHeightMap() => InitializeHeightMapImpl();
    public void RestoreHeightMapFromGridData(List<CellData> gridData) => RestoreHeightMapFromGridDataImpl(gridData);
    public void ApplyRestoredPositionsToGrid() => ApplyRestoredPositionsToGridImpl();
    public void EnsureHeightMapLoaded() => EnsureHeightMapLoadedImpl();
    public void ApplyHeightMapToGrid() => ApplyHeightMapToGridImpl();
    public void ApplyHeightMapToRegion(int minX, int minY, int maxX, int maxY) => ApplyHeightMapToRegionImpl(minX, minY, maxX, maxY);
    public void RefreshShoreTerrainAfterWaterUpdate(WaterManager wm, bool expandSecondChebyshevRing = false) => RefreshShoreTerrainAfterWaterUpdateImpl(wm, expandSecondChebyshevRing);
    public bool RestoreTerrainForCell(int x, int y, HeightMap useHeightMap = null, bool forceFlat = false, TerrainSlopeType? forceSlopeType = null, ISet<Vector2Int> terraformCutCorridorCells = null) => RestoreTerrainForCellImpl(x, y, useHeightMap, forceFlat, forceSlopeType, terraformCutCorridorCells);
    public bool IsWaterSlopeCell(int x, int y) => IsWaterSlopeCellImpl(x, y);
    public bool IsDryShoreOrRimMembershipEligible(int x, int y) => IsDryShoreOrRimMembershipEligibleImpl(x, y);
    public bool IsWaterOrSeaAtNeighbor(int nx, int ny) => IsWaterOrSeaAtNeighborImpl(nx, ny);
    public void PlaceSlopeFromPrefab(int x, int y, GameObject slopePrefab, int cellHeight = -1) => PlaceSlopeFromPrefabImpl(x, y, slopePrefab, cellHeight);
    public void RestoreWaterSlopesFromHeightMap() => RestoreWaterSlopesFromHeightMapImpl();
    public void RestoreTerrainSlopesFromHeightMap() => RestoreTerrainSlopesFromHeightMapImpl();
    public void RestoreWaterShorePrefabsFromSave(int x, int y, string primaryName, string secondaryName, int savedPrimarySort) => RestoreWaterShorePrefabsFromSaveImpl(x, y, primaryName, secondaryName, savedPrimarySort);
    public void RefreshWaterCascadeCliffs(WaterManager wm) => RefreshWaterCascadeCliffs_Impl(wm);
    public bool DoesCellUseWaterShorePrimaryPrefab(CityCell cell) => DoesCellUseWaterShorePrimaryPrefabImpl(cell);
    public int CalculateTerrainSortingOrder(int x, int y, int height) => CalculateTerrainSortingOrderImpl(x, y, height);
    public int CalculateWaterSlopeSortingOrder(int x, int y) => CalculateWaterSlopeSortingOrderImpl(x, y);
    public int CalculateShoreBaySortingOrder(int x, int y) => CalculateShoreBaySortingOrderImpl(x, y);
    public int CalculateSlopeSortingOrder(int x, int y, int height) => CalculateSlopeSortingOrderImpl(x, y, height);
    public int CalculateBuildingSortingOrder(int x, int y, int height) => CalculateBuildingSortingOrderImpl(x, y, height);
    public int CalculateSortingOrder(int x, int y, ObjectType objectType) => CalculateSortingOrderImpl(x, y, objectType);
    public bool IsWaterSlopeObject(GameObject obj) => IsWaterSlopeObjectImpl(obj);
    public bool IsLandSlopeObject(GameObject obj) => IsLandSlopeObjectImpl(obj);
    public bool IsSeaLevelWaterObject(GameObject obj) => IsSeaLevelWaterObjectImpl(obj);
    public bool IsShoreBayObject(GameObject obj) => IsShoreBayObjectImpl(obj);
    public bool IsCliffStackTerrainObject(GameObject obj) => IsCliffStackTerrainObjectImpl(obj);
    public TerrainSlopeType GetTerrainSlopeTypeAt(int x, int y) => GetTerrainSlopeTypeAtImpl(x, y);
    public void ModifyTerrain(int x, int y, int newHeight) => ModifyTerrainImpl(x, y, newHeight);
    public bool CanPlaceBuildingInTerrain(Vector2 gridPosition, int size, out string failReason, bool allowCoastalSlope = false, bool allowWaterInFootprint = false) => CanPlaceBuildingInTerrainImpl(gridPosition, size, out failReason, allowCoastalSlope, allowWaterInFootprint);
    public bool CanPlaceRoad(int x, int y) => CanPlaceRoadImpl(x, y);
    public bool CanPlaceRoad(int x, int y, bool allowWaterSlopeForWaterBridgeTrace) => CanPlaceRoadImpl(x, y, allowWaterSlopeForWaterBridgeTrace);

    // ObjectType enum — kept in hub (public contract)
    public enum ObjectType { Terrain, Slope, Road, Utility, Building, Effect }
}
}
