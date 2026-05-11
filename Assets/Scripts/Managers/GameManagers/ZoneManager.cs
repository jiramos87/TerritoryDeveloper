using System.Collections.Generic;
using UnityEngine;
using Territory.Core;
using Territory.Roads;
using Territory.Economy;
using Territory.UI;
using Territory.Terrain;
using Territory.Buildings;
using Territory.Simulation;
using Domains.Registry;
using Domains.Grid;
using Domains.Water;
using Domains.Roads;
using Domains.Zones;
using Domains.Zones.Services;

namespace Territory.Zones
{
/// <summary>
/// Thin MonoBehaviour facade. Holds inspector refs + prefab fields. All logic delegated to ZonesService (Stage 4.2 THIN).
/// [SerializeField] field set UNCHANGED (locked #3). Hub registers as IZoneManager with ServiceRegistry.
/// ZonesService receives IGrid/IWater/IRoads via WireDependencies in Start (not Awake).
/// Invariant #11: no UrbanizationProposal reference.
/// </summary>
public class ZoneManager : MonoBehaviour, IZoneManager
{
    #region Dependencies (UNCHANGED)
    public GridManager gridManager;
    public PowerPlant powerPlantManager;
    public WaterPlant waterPlantManager;
    public RoadManager roadManager;
    public CityStats cityStats;
    public UIManager uiManager;
    public GameNotificationManager gameNotificationManager;
    public DemandManager demandManager;
    public WaterManager waterManager;
    public InterstateManager interstateManager;
    public SlopePrefabRegistry slopePrefabRegistry;
    [SerializeField] private ZoneSubTypeRegistry zoneSubTypeRegistry;
    [SerializeField] private PlacementValidator placementValidator;
    #endregion

    #region Zone Prefabs (UNCHANGED)
    public List<GameObject> lightResidential1x1Prefabs, lightResidential2x2Prefabs, lightResidential3x3Prefabs;
    public List<GameObject> mediumResidential1x1Prefabs, mediumResidential2x2Prefabs, mediumResidential3x3Prefabs;
    public List<GameObject> heavyResidential1x1Prefabs, heavyResidential2x2Prefabs, heavyResidential3x3Prefabs;
    public List<GameObject> lightCommercial1x1Prefabs, lightCommercial2x2Prefabs, lightCommercial3x3Prefabs;
    public List<GameObject> mediumCommercial1x1Prefabs, mediumCommercial2x2Prefabs, mediumCommercial3x3Prefabs;
    public List<GameObject> heavyCommercial1x1Prefabs, heavyCommercial2x2Prefabs, heavyCommercial3x3Prefabs;
    public List<GameObject> lightIndustrial1x1Prefabs, lightIndustrial2x2Prefabs, lightIndustrial3x3Prefabs;
    public List<GameObject> mediumIndustrial1x1Prefabs, mediumIndustrial2x2Prefabs, mediumIndustrial3x3Prefabs;
    public List<GameObject> heavyIndustrial1x1Prefabs, heavyIndustrial2x2Prefabs, heavyIndustrial3x3Prefabs;
    public List<GameObject> residentialLightZoningPrefabs, residentialMediumZoningPrefabs, residentialHeavyZoningPrefabs;
    public List<GameObject> commercialLightZoningPrefabs, commercialMediumZoningPrefabs, commercialHeavyZoningPrefabs;
    public List<GameObject> industrialLightZoningPrefabs, industrialMediumZoningPrefabs, industrialHeavyZoningPrefabs;
    public List<GameObject> roadPrefabs, grassPrefabs, waterPrefabs;

    [Header("Desirability-weighted spawn (FEAT-26)")]
    [SerializeField] private float baseSpawnWeight = 1.0f;
    [SerializeField] private float minDesirabilityThreshold = 2.0f;
    [SerializeField] private float lowDesirabilityPenalty = 0.1f;

    [Header("FEAT-43 Signal Desirability Source")]
    [SerializeField] private AutoZoningManager autoZoningManager;
    [SerializeField] private DesirabilityComposer desirabilityComposer;

    [Header("Stage 10 — Construction Stage Hook (city-sim-depth)")]
    [SerializeField] private ConstructionStageController constructionStageController;
    #endregion

    #region Events + Service
    /// <summary>Invoked when urban cell (zoning) added or removed. Args: (position, isAdded). Not invoked on zoning→building conversion.</summary>
    public System.Action<Vector2, bool> onUrbanCellChanged;

    private ZonesService _service;
    private ServiceRegistry _registry;
    #endregion

    #region Lifecycle
    void Awake()
    {
        _registry = FindObjectOfType<ServiceRegistry>();
        if (autoZoningManager == null)      autoZoningManager = FindObjectOfType<AutoZoningManager>();
        if (desirabilityComposer == null)   desirabilityComposer = FindObjectOfType<DesirabilityComposer>();
        if (constructionStageController == null) constructionStageController = FindObjectOfType<ConstructionStageController>();

        _service = new ZonesService(gridManager, waterManager, roadManager, cityStats, uiManager,
            gameNotificationManager, demandManager, interstateManager, slopePrefabRegistry,
            autoZoningManager, desirabilityComposer, constructionStageController,
            powerPlantManager, waterPlantManager);
        _service.BaseSpawnWeight           = baseSpawnWeight;
        _service.MinDesirabilityThreshold  = minDesirabilityThreshold;
        _service.LowDesirabilityPenalty    = lowDesirabilityPenalty;
        _service.onUrbanCellChanged        = (p, a) => onUrbanCellChanged?.Invoke(p, a);
        _service.SetPrefabRegistry(BuildPrefabRegistry());
        _registry?.Register<IZoneManager>(this);
    }

    void Start()
    {
        // Wire cross-domain deps post-Awake (ServiceRegistry pattern: never resolve in Awake)
        _service.WireDependencies(
            _registry?.Resolve<IGrid>(),
            _registry?.Resolve<IWater>(),
            _registry?.Resolve<IRoads>());
        InitializeZonePrefabs();
    }

    public void InitializeZonePrefabs() => _service.SetPrefabRegistry(BuildPrefabRegistry());
    #endregion

    #region IZoneManager — single-line delegates
    public void HandleZoning(Vector2 p)                                                      => _service.HandleZoning(p);
    public ZoneAttributes GetZoneAttributes(Zone.ZoneType t)                                 => _service.GetZoneAttributes(t);
    public GameObject GetRandomZonePrefab(Zone.ZoneType t, int size = 1)                    => _service.GetRandomZonePrefab(t, size);
    public GameObject GetGrassPrefab()                                                       => _service.GetGrassPrefab();
    public GameObject GetWaterPrefab()                                                       => _service.GetWaterPrefab();
    public void PlaceZonedBuildings(Zone.ZoneType t)                                         => _service.PlaceZonedBuildings(t);
    public bool PlaceZoneAt(Vector2 p, Zone.ZoneType t)                                      => _service.PlaceZoneAt(p, t);
    public void ClearZonedPositions()                                                        => _service.ClearZonedPositions();
    public bool IsZoning()                                                                   => _service.IsZoning();
    public int  GetPreviewZoneCellCount()                                                    => _service.GetPreviewCellCount();
    public Zone.ZoneType GetBuildingZoneType(Zone.ZoneType t)                               => _service.GetBuildingZoneType(t);
    public bool IsResidentialBuilding(Zone.ZoneType t)                                      => _service.IsResidentialBuilding(t);
    public bool IsCommercialOrIndustrialBuilding(Zone.ZoneType t)                           => _service.IsCommercialOrIndustrialBuilding(t);
    public Zone.ZoneType GetZoneTypeFromZoneTypeString(string s)                            => _service.ParseZoneType(s);
    public GameObject FindPrefabByName(string n)                                             => _service.FindPrefabByName(n);
    public void CalculateAvailableSquareZonedSections()                                      => _service.CalculateAvailableSquareZonedSections();
    public void addZonedTileToList(Vector2 p, Zone.ZoneType t)                              => _service.AddZonedTileToList(p, t);
    public void removeZonedPositionFromList(Vector2 p, Zone.ZoneType t, bool conv = false)  => _service.RemoveZonedPositionFromList(p, t, conv);
    public void RemoveZonedSectionFromList(Vector2[] ps, Zone.ZoneType t)                   => _service.RemoveZonedSectionFromList(ps, t);
    public void RestoreZoneTile(GameObject pf, GameObject cell, Zone.ZoneType t)            => _service.RestoreZoneTile(pf, cell, t);
    public void PlaceZoneBuildingTile(GameObject pf, GameObject cell, int sz = 1)           => _service.PlaceZoneBuildingTile(pf, cell, sz);
    public bool PlaceStateServiceZoneAt(int cx, int cy, Zone.ZoneType t, int subId)         => _service.PlaceStateServiceZoneAt(cx, cy, t, subId, zoneSubTypeRegistry, placementValidator);
    public static bool IsZoningType(Zone.ZoneType t)         => new ZonePlacementService().IsZoningType(t);
    public static bool IsStateServiceZoneType(Zone.ZoneType t) => new ZonePlacementService().IsStateServiceZoneType(t);
    #endregion

    #region Prefab registry builder
    private ZonePrefabRegistry BuildPrefabRegistry() =>
        new ZonePrefabRegistry(new Dictionary<(Zone.ZoneType, int), List<GameObject>>
        {
            { (Zone.ZoneType.ResidentialLightBuilding, 1), lightResidential1x1Prefabs ?? new List<GameObject>() },
            { (Zone.ZoneType.ResidentialLightBuilding, 2), lightResidential2x2Prefabs ?? new List<GameObject>() },
            { (Zone.ZoneType.ResidentialLightBuilding, 3), lightResidential3x3Prefabs ?? new List<GameObject>() },
            { (Zone.ZoneType.ResidentialMediumBuilding, 1), mediumResidential1x1Prefabs ?? new List<GameObject>() },
            { (Zone.ZoneType.ResidentialMediumBuilding, 2), mediumResidential2x2Prefabs ?? new List<GameObject>() },
            { (Zone.ZoneType.ResidentialMediumBuilding, 3), mediumResidential3x3Prefabs ?? new List<GameObject>() },
            { (Zone.ZoneType.ResidentialHeavyBuilding, 1), heavyResidential1x1Prefabs ?? new List<GameObject>() },
            { (Zone.ZoneType.ResidentialHeavyBuilding, 2), heavyResidential2x2Prefabs ?? new List<GameObject>() },
            { (Zone.ZoneType.ResidentialHeavyBuilding, 3), heavyResidential3x3Prefabs ?? new List<GameObject>() },
            { (Zone.ZoneType.CommercialLightBuilding, 1), lightCommercial1x1Prefabs ?? new List<GameObject>() },
            { (Zone.ZoneType.CommercialLightBuilding, 2), lightCommercial2x2Prefabs ?? new List<GameObject>() },
            { (Zone.ZoneType.CommercialLightBuilding, 3), lightCommercial3x3Prefabs ?? new List<GameObject>() },
            { (Zone.ZoneType.CommercialMediumBuilding, 1), mediumCommercial1x1Prefabs ?? new List<GameObject>() },
            { (Zone.ZoneType.CommercialMediumBuilding, 2), mediumCommercial2x2Prefabs ?? new List<GameObject>() },
            { (Zone.ZoneType.CommercialMediumBuilding, 3), mediumCommercial3x3Prefabs ?? new List<GameObject>() },
            { (Zone.ZoneType.CommercialHeavyBuilding, 1), heavyCommercial1x1Prefabs ?? new List<GameObject>() },
            { (Zone.ZoneType.CommercialHeavyBuilding, 2), heavyCommercial2x2Prefabs ?? new List<GameObject>() },
            { (Zone.ZoneType.CommercialHeavyBuilding, 3), heavyCommercial3x3Prefabs ?? new List<GameObject>() },
            { (Zone.ZoneType.IndustrialLightBuilding, 1), lightIndustrial1x1Prefabs ?? new List<GameObject>() },
            { (Zone.ZoneType.IndustrialLightBuilding, 2), lightIndustrial2x2Prefabs ?? new List<GameObject>() },
            { (Zone.ZoneType.IndustrialLightBuilding, 3), lightIndustrial3x3Prefabs ?? new List<GameObject>() },
            { (Zone.ZoneType.IndustrialMediumBuilding, 1), mediumIndustrial1x1Prefabs ?? new List<GameObject>() },
            { (Zone.ZoneType.IndustrialMediumBuilding, 2), mediumIndustrial2x2Prefabs ?? new List<GameObject>() },
            { (Zone.ZoneType.IndustrialMediumBuilding, 3), mediumIndustrial3x3Prefabs ?? new List<GameObject>() },
            { (Zone.ZoneType.IndustrialHeavyBuilding, 1), heavyIndustrial1x1Prefabs ?? new List<GameObject>() },
            { (Zone.ZoneType.IndustrialHeavyBuilding, 2), heavyIndustrial2x2Prefabs ?? new List<GameObject>() },
            { (Zone.ZoneType.IndustrialHeavyBuilding, 3), heavyIndustrial3x3Prefabs ?? new List<GameObject>() },
            { (Zone.ZoneType.ResidentialLightZoning, 1), residentialLightZoningPrefabs ?? new List<GameObject>() },
            { (Zone.ZoneType.ResidentialMediumZoning, 1), residentialMediumZoningPrefabs ?? new List<GameObject>() },
            { (Zone.ZoneType.ResidentialHeavyZoning, 1), residentialHeavyZoningPrefabs ?? new List<GameObject>() },
            { (Zone.ZoneType.CommercialLightZoning, 1), commercialLightZoningPrefabs ?? new List<GameObject>() },
            { (Zone.ZoneType.CommercialMediumZoning, 1), commercialMediumZoningPrefabs ?? new List<GameObject>() },
            { (Zone.ZoneType.CommercialHeavyZoning, 1), commercialHeavyZoningPrefabs ?? new List<GameObject>() },
            { (Zone.ZoneType.IndustrialLightZoning, 1), industrialLightZoningPrefabs ?? new List<GameObject>() },
            { (Zone.ZoneType.IndustrialMediumZoning, 1), industrialMediumZoningPrefabs ?? new List<GameObject>() },
            { (Zone.ZoneType.IndustrialHeavyZoning, 1), industrialHeavyZoningPrefabs ?? new List<GameObject>() },
            { (Zone.ZoneType.Grass, 1), grassPrefabs ?? new List<GameObject>() },
            { (Zone.ZoneType.Road, 1), roadPrefabs ?? new List<GameObject>() },
            { (Zone.ZoneType.Water, 1), waterPrefabs ?? new List<GameObject>() },
        });
    #endregion
}
}
