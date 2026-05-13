using UnityEngine;
using Territory.Core;
using Territory.Terrain;
using Territory.Forests;
using Territory.Zones;
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
/// Init/load/reinit bodies delegated to <see cref="GeographyInitService"/>.
/// Query/utility bodies delegated to <see cref="GeographyQueryService"/>.
/// Clear/reset bodies delegated to <see cref="GeographyClearService"/>.
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
    public bool useTerrainForWater = true;
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

    /// <summary>Current geographical data (for save/load operations). Written by services.</summary>
    internal GeographyData currentGeographyData;

    private MiniMapController _miniMapController;
    private GeographySortingOrderService _sortingService;
    private GeographyInitService _initService;
    private GeographyQueryService _queryService;
    private GeographyClearService _clearService;

    /// <summary>
    /// Fired on the false→true transition of <see cref="IsInitialized"/>.
    /// Subscribers (e.g. <see cref="Domains.Geography.Services.LoadingVeilController"/>) deactivate
    /// loading overlays or trigger deferred work.
    /// </summary>
    public event System.Action OnGeographyInitialized;

    private bool _isInitialized;

    /// <summary>
    /// True after <see cref="InitializeGeography"/> completes its full pipeline (terrain → water → rivers →
    /// interstate → forests → desirability → sorting). Time-driven systems must wait for this flag before
    /// reading grid data (init-race guard). Fires <see cref="OnGeographyInitialized"/> on first set to true.
    /// </summary>
    public bool IsInitialized
    {
        get => _isInitialized;
        internal set
        {
            bool prev = _isInitialized;
            _isInitialized = value;
            if (!prev && value)
                OnGeographyInitialized?.Invoke();
        }
    }
    #endregion

    #region Initialization
    void Awake()
    {
        _sortingService = new GeographySortingOrderService(gridManager, terrainManager);
        _initService  = new GeographyInitService(this);
        _queryService = new GeographyQueryService(this);
        _clearService = new GeographyClearService(this);
    }

    void Start()
    {
        if (zoneManager == null)       zoneManager       = FindObjectOfType<ZoneManager>();
        if (terrainManager == null)    terrainManager    = FindObjectOfType<TerrainManager>();
        if (waterManager == null)      waterManager      = FindObjectOfType<WaterManager>();
        if (forestManager == null)     forestManager     = FindObjectOfType<ForestManager>();
        if (gridManager == null)       gridManager       = FindObjectOfType<GridManager>();
        if (interstateManager == null) interstateManager = FindObjectOfType<InterstateManager>();
        if (regionalMapManager == null)regionalMapManager= FindObjectOfType<RegionalMapManager>();
        if (_miniMapController == null)_miniMapController= FindObjectOfType<MiniMapController>();

        _sortingService = new GeographySortingOrderService(gridManager, terrainManager);
        _initService    = new GeographyInitService(this);
        _queryService   = new GeographyQueryService(this);
        _clearService   = new GeographyClearService(this);

        if (initializeOnStart)
            InitializeGeography();
    }
    #endregion

    #region Public API — delegated to services
    public void InitializeGeography()               => _initService.InitializeGeography();
    public void ReinitializeGeographyForNewGame()   => _initService.ReinitializeGeographyForNewGame();
    public void LoadGeography(GeographyData data)   => _initService.LoadGeography(data);
    public string BuildGeographyInitReportJson()    => _initService.BuildGeographyInitReportJson();

    public void ResetGeography()                    => _clearService.ResetGeography();
    public void ClearForestsOfType(Forest.ForestType forestType) => _clearService.ClearForestsOfType(forestType);

    public GeographyData GetCurrentGeographyData()          { currentGeographyData = _queryService.CreateGeographyData(); return currentGeographyData; }
    public void UpdateGeographyStatistics()                 => currentGeographyData = _queryService.CreateGeographyData();
    public bool IsPositionSuitableForPlacement(int x, int y, PlacementType placementType) => _queryService.IsPositionSuitableForPlacement(x, y, placementType);
    public EnvironmentalBonus GetEnvironmentalBonus(int x, int y)                         => _queryService.GetEnvironmentalBonus(x, y);
    public ForestRegionInfo GetForestRegionInfo(int centerX, int centerY, int radius)     => _queryService.GetForestRegionInfo(centerX, centerY, radius);

    public void ReCalculateSortingOrderBasedOnHeight() => _sortingService.ReCalculateSortingOrderBasedOnHeight();
    #endregion

    #region Internal helpers — called by services
    internal GeographyData CreateGeographyData() => _queryService.CreateGeographyData();

    internal void InitializeWaterDesirability()
    {
        if (gridManager == null || waterManager == null) return;
        GeographyWaterDesirabilityService.Apply(
            gridManager.width,
            gridManager.height,
            (x, y) => gridManager.GetCell(x, y),
            (x, y) => waterManager.IsWaterAt(x, y));
    }

    internal void NotifyMiniMap()
    {
        if (_miniMapController == null)
            _miniMapController = FindObjectOfType<MiniMapController>();
        if (_miniMapController != null)
            _miniMapController.RebuildTexture();
    }
    #endregion
}
}
