using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Collections.Generic;
using Territory.Core;
using Territory.Terrain;
using Territory.Forests;
using Territory.Zones;
using Territory.Buildings;
using Territory.Roads;
using Territory.Economy;
using Territory.UI;
using Territory.Utilities;
using Territory.Persistence;
using Domains.Geography;
using Domains.Geography.Services;
using Domains.Grid.Services;

namespace Territory.Geography
{
/// <summary>
/// GeographyManager coordinates init + management of all geographical features:
/// terrain height, water bodies (optional lakes/sea, optional rivers, optional test river), forests.
/// Inspector toggles: <see cref="generateStandardWaterBodies"/>, <see cref="generateProceduralRiversOnInit"/>, <see cref="generateTestRiverOnInit"/>, <see cref="useFlatTerrainOnNewGame"/>.
/// Optional interchange: <see cref="loadGeographyInitParamsFromStreamingAssets"/> loads <c>geography_init_params</c> from StreamingAssets.
/// Diagnostics: <see cref="BuildGeographyInitReportJson"/> for harness export (<c>tools/reports/last-geography-init.json</c>).
/// Implements <see cref="IGeography"/> facade (Domains.Geography Stage 15 atomization).
/// Pass-through: sorting body delegated to <see cref="GeographySortingOrderService"/> (TECH-26633).
/// </summary>
public class GeographyManager : MonoBehaviour, IGeography
{
    #region Dependencies
    [Header("Manager References")]
    public TerrainManager terrainManager;
    public WaterManager waterManager;
    public ForestManager forestManager;
    public GridManager gridManager;
    public ZoneManager zoneManager;
    public InterstateManager interstateManager;
    public RegionalMapManager regionalMapManager;
    #endregion

    #region Configuration
    [Header("Geography Configuration")]
    public bool initializeOnStart = true;
    public bool useTerrainForWater = true; // Whether to use terrain height for water placement
    [Tooltip("When true, procedural forests are generated after interstate placement. Disable to see terrain clearly during road/terraforming tests.")]
    public bool initializeForestsOnStart = true;

    [Header("Terrain (New Game / QA)")]
    [Tooltip("When true, the height map is a single uniform level (no 40×40 template, no procedural extension). Use for road/bridge/terraform experiments. Height 0 is sea level everywhere; use ≥1 for typical dry-land tests.")]
    public bool useFlatTerrainOnNewGame = false;

    [Range(TerrainManager.MIN_HEIGHT, TerrainManager.MAX_HEIGHT)]
    [Tooltip("Uniform terrain height when useFlatTerrainOnNewGame is enabled (clamped to valid range).")]
    public int flatTerrainHeight = 1;

    [Header("Water generation (New Game / InitializeGeography)")]
    [Tooltip("When true, InitializeWaterMap places lakes/sea from terrain as usual. When false, WaterMap starts empty (no procedural lake fill or height-based sea).")]
    public bool generateStandardWaterBodies = true;

    [Tooltip("When true, FEAT-38 procedural rivers run after standard water init. Ignored when standard water is disabled.")]
    public bool generateProceduralRiversOnInit = true;

    [Tooltip("When true, places the straight grid West→East test river after standard water and procedural rivers (centerline x = map width / 2, clamped to corridor margins; y west→east per isometric spec). Four equal segments with S=4,3,2,1.")]
    public bool generateTestRiverOnInit = false;

    [Tooltip("Four bed widths (1–3 cells effective; larger values clamp) for test river segments S=4,3,2,1. Length 4 when assigned.")]
    public int[] testRiverSegmentBedWidths = new int[] { 1, 2, 3, 2 };

    [Header("Geography interchange (TECH-41)")]
    [Tooltip("Load geography_init_params JSON from StreamingAssets at the start of each geography pipeline. When off, behavior matches pre–TECH-41 random session seed. When on, missing or invalid file falls back to random seed and Inspector river toggle.")]
    public bool loadGeographyInitParamsFromStreamingAssets = false;
    [Tooltip("Path relative to StreamingAssets (e.g. Config/geography-default.json).")]
    public string geographyInitParamsRelativePath = "Config/geography-default.json";

    /// <summary>Last successful <c>geography_init_params</c> load this session (for harness export). Null → interchange load off or failed.</summary>
    private GeographyInitParamsDto lastLoadedGeographyInitParams;

    /// <summary>Advisory <c>map</c> from last successful interchange load → dimension mismatch warning.</summary>
    private GeographyInitMapDto geographyInitMapAdvisory;
    /// <summary>Set → overrides <see cref="generateProceduralRiversOnInit"/> for current pipeline run.</summary>
    private bool? riversEnabledFromInterchange;

    // Current geographical data (for save/load operations)
    private GeographyData currentGeographyData;

    private MiniMapController miniMapController;

    /// <summary>
    /// True after <see cref="InitializeGeography"/> completes its full pipeline (terrain → water → rivers →
    /// interstate → forests → desirability → sorting). Time-driven systems must wait for this flag before
    /// reading grid data (init-race guard — see `ia/specs/unity-development-context.md` §6).
    /// </summary>
    public bool IsInitialized { get; private set; }

    // Sorting-order service — initialized in Awake so it's available before Start callbacks.
    private GeographySortingOrderService _sortingService;
    #endregion

    #region Initialization
    void Awake()
    {
        _sortingService = new GeographySortingOrderService(gridManager, terrainManager);
    }

    void Start()
    {
        // Find managers if not assigned
        if (zoneManager == null)
            zoneManager = FindObjectOfType<ZoneManager>();
        if (terrainManager == null)
            terrainManager = FindObjectOfType<TerrainManager>();
        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();
        if (forestManager == null)
            forestManager = FindObjectOfType<ForestManager>();
        if (gridManager == null)
            gridManager = FindObjectOfType<GridManager>();
        if (interstateManager == null)
            interstateManager = FindObjectOfType<InterstateManager>();
        if (regionalMapManager == null)
            regionalMapManager = FindObjectOfType<RegionalMapManager>();
        if (miniMapController == null)
            miniMapController = FindObjectOfType<MiniMapController>();

        // Re-init sorting service now that managers are resolved via FindObjectOfType.
        _sortingService = new GeographySortingOrderService(gridManager, terrainManager);

        if (initializeOnStart)
        {
            InitializeGeography();
        }
    }

    public void InitializeGeography()
    {
        ApplyGeographyInitInterchangeAtPipelineStart();

        if (regionalMapManager != null)
        {
            regionalMapManager.InitializeRegionalMap();
        }

        if (terrainManager == null)
            terrainManager = FindObjectOfType<TerrainManager>();
        if (terrainManager != null)
            terrainManager.SetNewGameFlatTerrainOptions(useFlatTerrainOnNewGame, flatTerrainHeight);

        if (gridManager != null)
        {
            gridManager.InitializeGrid();
        }

        WarnIfGeographyInitMapMismatchAgainstGrid();

        if (waterManager != null)
        {
            waterManager.SetGenerateStandardWater(generateStandardWaterBodies);
            waterManager.InitializeWaterMap();
            if (generateStandardWaterBodies && GetEffectiveProceduralRiversOnInit())
                waterManager.GenerateProceduralRiversForNewGame();
            if (generateTestRiverOnInit)
                waterManager.GenerateTestRiver(testRiverSegmentBedWidths);
        }

        // Interstate runs after terrain (from InitializeGrid), water, and rivers so pathing and tiles use final height/water state.
        if (interstateManager != null)
        {
            const int maxInterstateAttempts = 3;
            bool placed = false;
            for (int attempt = 0; attempt < maxInterstateAttempts; attempt++)
            {
                placed = interstateManager.GenerateAndPlaceInterstate(attempt);
                if (placed) break;
            }
            if (!placed)
                placed = interstateManager.TryGenerateInterstateDeterministic();
        }

        if (forestManager != null && initializeForestsOnStart)
        {
            forestManager.InitializeForestMap();
        }

        InitializeWaterDesirability();

        currentGeographyData = CreateGeographyData();
        ReCalculateSortingOrderBasedOnHeight();

        if (regionalMapManager != null)
        {
            regionalMapManager.PlaceBorderSigns();
        }

        if (interstateManager != null && GameNotificationManager.Instance != null)
        {
            if (interstateManager.InterstatePositions != null && interstateManager.InterstatePositions.Count >= 2)
            {
                CityStats cityStats = FindObjectOfType<CityStats>();
                string cityName = (cityStats != null && !string.IsNullOrEmpty(cityStats.cityName)) ? cityStats.cityName : "your city";
                GameNotificationManager.Instance.PostNotification(
                    "Welcome to " + cityName + "! An Interstate Highway crosses your territory. Build a road connecting to it to start developing your city.",
                    GameNotificationManager.NotificationType.Info,
                    8f
                );
            }
            else
            {
                DebugHelper.LogWarning("GeographyManager: Interstate could not be placed (no valid path). Game continues without interstate.");
            }
        }

        NotifyMiniMapAfterGeographyReady();

        // Signal geography ready so TimeManager daily-tick gate can pass.
        IsInitialized = true;
    }

    private bool GetEffectiveProceduralRiversOnInit()
    {
        return riversEnabledFromInterchange ?? generateProceduralRiversOnInit;
    }

    /// <summary>
    /// Load <c>geography_init_params</c> from StreamingAssets when enabled; set <see cref="MapGenerationSeed"/> + optional river override.
    /// </summary>
    private void ApplyGeographyInitInterchangeAtPipelineStart()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        UnityEngine.Profiling.Profiler.BeginSample("GeographyInitParams.Load");
#endif
        geographyInitMapAdvisory = null;
        riversEnabledFromInterchange = null;
        lastLoadedGeographyInitParams = null;
        try
        {
            if (!loadGeographyInitParamsFromStreamingAssets)
            {
                MapGenerationSeed.EnsureSessionMasterSeed();
                return;
            }

            string path = Path.Combine(Application.streamingAssetsPath, geographyInitParamsRelativePath);
            if (!GeographyInitParamsLoader.TryLoadFromPath(path, out GeographyInitParamsDto dto, out string err))
            {
                if (!string.IsNullOrEmpty(err))
                    DebugHelper.LogWarning($"GeographyManager: Geography init interchange — {err}");
                MapGenerationSeed.EnsureSessionMasterSeed();
                return;
            }

            lastLoadedGeographyInitParams = dto;
            MapGenerationSeed.SetSessionMasterSeed(dto.seed);
            if (dto.rivers != null)
                riversEnabledFromInterchange = dto.rivers.enabled;
            if (dto.map != null)
                geographyInitMapAdvisory = dto.map;

            if (dto.water != null)
                DebugHelper.Log($"GeographyManager: Interchange water.seaBias={dto.water.seaBias} (reserved for FEAT-46; not applied).");
            if (dto.forest != null)
                DebugHelper.Log($"GeographyManager: Interchange forest.coverageTarget={dto.forest.coverageTarget} (reserved for FEAT-46; not applied).");

            DebugHelper.Log($"GeographyManager: Loaded geography interchange StreamingAssets/{geographyInitParamsRelativePath} (seed applied; procedural rivers={(riversEnabledFromInterchange.HasValue ? riversEnabledFromInterchange.Value.ToString() : "Inspector")}).");
        }
        finally
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UnityEngine.Profiling.Profiler.EndSample();
#endif
        }
    }

    private void WarnIfGeographyInitMapMismatchAgainstGrid()
    {
        if (geographyInitMapAdvisory == null || gridManager == null)
            return;
        if (gridManager.width != geographyInitMapAdvisory.width || gridManager.height != geographyInitMapAdvisory.height)
        {
            DebugHelper.LogWarning(
                $"GeographyManager: Interchange map ({geographyInitMapAdvisory.width}x{geographyInitMapAdvisory.height}) does not match grid ({gridManager.width}x{gridManager.height}). Interchange map is advisory only.");
        }
    }

    /// <summary>Strip <c>interchange_snapshot_json</c> when Unity <see cref="JsonUtility"/> emits <c>""</c> for null string → key omitted.</summary>
    private static readonly Regex StripEmptyInterchangeSnapshotJsonProperty = new Regex(
        @"\r?\n[ \t]*""interchange_snapshot_json""\s*:\s*"""",?\s*|,\s*""interchange_snapshot_json""\s*:\s*""""",
        RegexOptions.CultureInvariant);

    /// <summary>
    /// JSON snapshot for geography harness (<c>tools/reports/last-geography-init.json</c>). Use in Play Mode after init. Not Save data.
    /// </summary>
    public string BuildGeographyInitReportJson()
    {
        MapGenerationSeed.EnsureSessionMasterSeed();
        var root = new GeographyInitReportRootDto
        {
            artifact = "geography_init_report",
            schema_version = 1,
            exported_utc_unix_seconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            master_seed = MapGenerationSeed.MasterSeed,
            interchange_file_was_applied = lastLoadedGeographyInitParams != null,
            interchange_snapshot_json = lastLoadedGeographyInitParams != null
                ? JsonUtility.ToJson(lastLoadedGeographyInitParams)
                : null,
            map_width = gridManager != null ? gridManager.width : 0,
            map_height = gridManager != null ? gridManager.height : 0,
            generate_standard_water_bodies = generateStandardWaterBodies,
            procedural_rivers_enabled_effective = GetEffectiveProceduralRiversOnInit(),
            generate_test_river_on_init = generateTestRiverOnInit,
            forest_coverage_percentage = GetForestCoverageForReport(),
            notes = "Not Save data. interchange_snapshot_json set when StreamingAssets geography_init_params loaded successfully this pipeline run."
        };
        string json = JsonUtility.ToJson(root, true);
        if (lastLoadedGeographyInitParams == null)
            json = StripEmptyInterchangeSnapshotJsonProperty.Replace(json, string.Empty);
        return json;
    }

    private float GetForestCoverageForReport()
    {
        if (forestManager == null)
            return 0f;
        var map = forestManager.GetForestMap();
        return map != null ? map.GetForestCoveragePercentage() : 0f;
    }

    /// <summary>
    /// Refresh mini-map texture once terrain, water, interstate, forests (if any), sorting ready.
    /// </summary>
    private void NotifyMiniMapAfterGeographyReady()
    {
        if (miniMapController == null)
            miniMapController = FindObjectOfType<MiniMapController>();
        if (miniMapController != null)
            miniMapController.RebuildTexture();
    }

    /// <summary>
    /// Populate <c>closeWaterCount</c> for all cells + recalc desirability.
    /// Delegates to <see cref="GeographyWaterDesirabilityService.Apply"/> (Domains.Geography Stage 15).
    /// Call after <see cref="ForestManager.InitializeForestMap"/> → both forest + water contribute to desirability.
    /// </summary>
    private void InitializeWaterDesirability()
    {
        if (gridManager == null || waterManager == null) return;
        GeographyWaterDesirabilityService.Apply(
            gridManager.width,
            gridManager.height,
            (x, y) => gridManager.GetCell(x, y),
            (x, y) => waterManager.IsWaterAt(x, y));
    }

    private GeographyData CreateGeographyData()
    {
        GeographyData data = new GeographyData();

        // Collect terrain data
        if (terrainManager != null)
        {
            data.hasTerrainData = true;
            data.terrainWidth = gridManager.width;
            data.terrainHeight = gridManager.height;
            // Note: Actual height data would be collected from terrainManager if needed
        }

        // Collect water data
        if (waterManager != null && waterManager.GetWaterMap() != null)
        {
            data.hasWaterData = true;
            data.waterCellCount = GetWaterCellCount();
        }

        // Collect forest data
        if (forestManager != null && forestManager.GetForestMap() != null)
        {
            data.hasForestData = true;
            var forestStats = forestManager.GetForestStatistics();
            data.forestCellCount = forestStats.totalForestCells;
            data.forestCoveragePercentage = forestStats.forestCoveragePercentage;
            data.sparseForestCount = forestStats.sparseForestCount;
            data.mediumForestCount = forestStats.mediumForestCount;
            data.denseForestCount = forestStats.denseForestCount;
        }

        return data;
    }

    public void LoadGeography(GeographyData geographyData)
    {
        currentGeographyData = geographyData;

        if (geographyData.hasTerrainData && terrainManager != null)
        {
            terrainManager.InitializeHeightMap();
        }

        if (geographyData.hasWaterData && waterManager != null)
        {
            waterManager.InitializeWaterMap();
        }

        if (geographyData.hasForestData && forestManager != null)
        {
            forestManager.InitializeForestMap();
        }

        InitializeWaterDesirability();
        ReCalculateSortingOrderBasedOnHeight();

        NotifyMiniMapAfterGeographyReady();
    }

    /// <summary>
    /// Re-init geography after ResetGrid (New Game). Apply terrain, water, interstate, forests, recalc sorting order.
    /// Call after <see cref="GridManager.ResetGrid"/>.
    /// </summary>
    public void ReinitializeGeographyForNewGame()
    {
        ApplyGeographyInitInterchangeAtPipelineStart();
        WarnIfGeographyInitMapMismatchAgainstGrid();

        if (regionalMapManager != null)
            regionalMapManager.InitializeRegionalMap();

        if (terrainManager != null)
            terrainManager.InitializeHeightMap();

        if (waterManager != null)
        {
            waterManager.SetGenerateStandardWater(generateStandardWaterBodies);
            waterManager.InitializeWaterMap();
            if (generateStandardWaterBodies && GetEffectiveProceduralRiversOnInit())
                waterManager.GenerateProceduralRiversForNewGame();
            if (generateTestRiverOnInit)
                waterManager.GenerateTestRiver(testRiverSegmentBedWidths);
        }

        // Interstate after terrain, water, and rivers (same order as InitializeGeography).
        if (interstateManager != null)
        {
            if (terrainManager == null || terrainManager.GetHeightMap() == null || gridManager == null)
            {
                DebugHelper.LogWarning("GeographyManager: Skipping interstate - terrainManager, heightMap, or gridManager missing.");
            }
            else
            {
                const int maxInterstateAttempts = 3;
                bool placed = false;
                for (int attempt = 0; attempt < maxInterstateAttempts; attempt++)
                {
                    placed = interstateManager.GenerateAndPlaceInterstate(attempt);
                    if (placed) break;
                }
                if (!placed)
                    placed = interstateManager.TryGenerateInterstateDeterministic();
            }
        }

        if (forestManager != null && initializeForestsOnStart)
            forestManager.InitializeForestMap();

        InitializeWaterDesirability();
        ReCalculateSortingOrderBasedOnHeight();

        if (regionalMapManager != null)
            regionalMapManager.PlaceBorderSigns();

        if (interstateManager != null && GameNotificationManager.Instance != null)
        {
            if (interstateManager.InterstatePositions != null && interstateManager.InterstatePositions.Count >= 2)
            {
                CityStats cityStats = FindObjectOfType<CityStats>();
                string cityName = (cityStats != null && !string.IsNullOrEmpty(cityStats.cityName)) ? cityStats.cityName : "your city";
                GameNotificationManager.Instance.PostNotification(
                    "Welcome to " + cityName + "! An Interstate Highway crosses your territory. Build a road connecting to it to start developing your city.",
                    GameNotificationManager.NotificationType.Info,
                    8f
                );
            }
            else
            {
                DebugHelper.LogWarning("GeographyManager: Interstate could not be placed (no valid path). Game continues without interstate.");
            }
        }

        NotifyMiniMapAfterGeographyReady();
    }
    #endregion

    #region Terrain Setup — pass-through delegates to GeographySortingOrderService
    /// <summary>
    /// Recalculate sprite sorting orders for all grid cells based on height + content type.
    /// Delegates to <see cref="GeographySortingOrderService.ReCalculateSortingOrderBasedOnHeight"/>.
    /// </summary>
    public void ReCalculateSortingOrderBasedOnHeight() => _sortingService.ReCalculateSortingOrderBasedOnHeight();
    #endregion

    #region Forest Setup
    public void ResetGeography()
    {
        if (forestManager != null && forestManager.GetForestMap() != null)
        {
            ClearAllForests();
        }
        if (waterManager != null && waterManager.GetWaterMap() != null)
        {
            // Water manager would need a ClearAllWater method similar to forest
            DebugHelper.Log("GeographyManager: Water reset not implemented yet");
        }

        currentGeographyData = new GeographyData();

        DebugHelper.Log("GeographyManager: Geography reset complete!");
    }

    private void ClearAllForests()
    {
        if (forestManager == null || gridManager == null)
            return;

        ForestMap forestMap = forestManager.GetForestMap();
        if (forestMap == null)
            return;

        var allForests = forestMap.GetAllForests();

        foreach (var forestPos in allForests)
        {
            forestManager.RemoveForestFromCell(forestPos.x, forestPos.y, false);
        }
    }

    public void ClearForestsOfType(Forest.ForestType forestType)
    {
        if (forestManager == null || gridManager == null)
            return;

        ForestMap forestMap = forestManager.GetForestMap();
        if (forestMap == null)
            return;

        var forestsOfType = forestMap.GetAllForestsOfType(forestType);

        foreach (var forestPos in forestsOfType)
        {
            forestManager.RemoveForestFromCell(forestPos.x, forestPos.y, false);
        }
    }
    #endregion

    #region Utility Methods
    public GeographyData GetCurrentGeographyData()
    {
        currentGeographyData = CreateGeographyData();
        return currentGeographyData;
    }

    public void UpdateGeographyStatistics()
    {
        currentGeographyData = CreateGeographyData();
    }

    public bool IsPositionSuitableForPlacement(int x, int y, PlacementType placementType)
    {
        switch (placementType)
        {
            case PlacementType.Forest:
                if (waterManager != null && waterManager.IsWaterAt(x, y))
                    return false;
                if (forestManager != null && forestManager.IsForestAt(x, y))
                    return false;
                if (gridManager != null && gridManager.gridArray != null)
                {
                    CityCell cellComponent = gridManager.GetCell(x, y);
                    if (cellComponent != null && cellComponent.occupiedBuilding != null)
                        return false;
                }
                return true;

            case PlacementType.Water:
                if (waterManager != null && waterManager.IsWaterAt(x, y))
                    return false;
                if (forestManager != null && forestManager.IsForestAt(x, y))
                    return false;
                return true;

            case PlacementType.Building:
                if (waterManager != null && waterManager.IsWaterAt(x, y))
                    return false;
                return true;

            case PlacementType.Zone:
                if (waterManager != null && waterManager.IsWaterAt(x, y))
                    return false;
                return true;

            case PlacementType.Infrastructure:
                if (waterManager != null && waterManager.IsWaterAt(x, y))
                    return false;
                if (gridManager != null && gridManager.IsCellOccupiedByBuilding(x, y))
                    return false;
                return true;

            default:
                return true;
        }
    }

    public EnvironmentalBonus GetEnvironmentalBonus(int x, int y)
    {
        EnvironmentalBonus bonus = new EnvironmentalBonus();

        if (gridManager != null && gridManager.gridArray != null)
        {
            if (x >= 0 && x < gridManager.width && y >= 0 && y < gridManager.height)
            {
                CityCell cellComponent = gridManager.GetCell(x, y);

                if (cellComponent != null)
                {
                    bonus.desirability = cellComponent.desirability;
                    bonus.adjacentForests = cellComponent.closeForestCount;
                    bonus.adjacentWater = cellComponent.closeWaterCount;
                    bonus.forestType = cellComponent.GetForestType();
                }
            }
        }

        return bonus;
    }

    public ForestRegionInfo GetForestRegionInfo(int centerX, int centerY, int radius)
    {
        ForestRegionInfo info = new ForestRegionInfo();

        if (forestManager == null)
            return info;

        ForestMap forestMap = forestManager.GetForestMap();
        if (forestMap == null)
            return info;

        // Count forest types in the region
        for (int x = centerX - radius; x <= centerX + radius; x++)
        {
            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                if (forestMap.IsValidPosition(x, y))
                {
                    Forest.ForestType forestType = forestMap.GetForestType(x, y);
                    switch (forestType)
                    {
                        case Forest.ForestType.Sparse:
                            info.sparseCount++;
                            break;
                        case Forest.ForestType.Medium:
                            info.mediumCount++;
                            break;
                        case Forest.ForestType.Dense:
                            info.denseCount++;
                            break;
                    }
                    info.totalCells++;
                }
            }
        }

        info.forestCoverage = info.totalCells > 0 ?
            (float)(info.sparseCount + info.mediumCount + info.denseCount) / info.totalCells * 100f : 0f;

        return info;
    }

    // Helper method to count water cells
    private int GetWaterCellCount()
    {
        int count = 0;
        if (waterManager != null && gridManager != null)
        {
            for (int x = 0; x < gridManager.width; x++)
            {
                for (int y = 0; y < gridManager.height; y++)
                {
                    if (waterManager.IsWaterAt(x, y))
                        count++;
                }
            }
        }
        return count;
    }
    #endregion
}
}
