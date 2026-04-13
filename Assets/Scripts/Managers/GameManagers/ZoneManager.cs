using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using Territory.Core;
using Territory.Roads;
using Territory.Economy;
using Territory.UI;
using Territory.Terrain;
using Territory.Buildings;

namespace Territory.Zones
{
/// <summary>
/// Handle RCI (residential, commercial, industrial) zone placement + zone tile rendering.
/// Manage zone overlays, building instantiation inside zones. Coords: <see cref="GridManager"/> (cell state), <see cref="RoadManager"/> (adjacency), <see cref="DemandManager"/> (growth eligibility).
/// </summary>
public class ZoneManager : MonoBehaviour, IZoneManager
{
    #region Dependencies
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
    #endregion

    #region Zone Prefabs and Configuration
    public List<GameObject> lightResidential1x1Prefabs;
    public List<GameObject> lightResidential2x2Prefabs;
    public List<GameObject> lightResidential3x3Prefabs;

    public List<GameObject> mediumResidential1x1Prefabs;
    public List<GameObject> mediumResidential2x2Prefabs;
    public List<GameObject> mediumResidential3x3Prefabs;

    public List<GameObject> heavyResidential1x1Prefabs;
    public List<GameObject> heavyResidential2x2Prefabs;
    public List<GameObject> heavyResidential3x3Prefabs;

    public List<GameObject> lightCommercial1x1Prefabs;
    public List<GameObject> lightCommercial2x2Prefabs;
    public List<GameObject> lightCommercial3x3Prefabs;

    public List<GameObject> mediumCommercial1x1Prefabs;
    public List<GameObject> mediumCommercial2x2Prefabs;
    public List<GameObject> mediumCommercial3x3Prefabs;

    public List<GameObject> heavyCommercial1x1Prefabs;
    public List<GameObject> heavyCommercial2x2Prefabs;
    public List<GameObject> heavyCommercial3x3Prefabs;

    public List<GameObject> lightIndustrial1x1Prefabs;
    public List<GameObject> lightIndustrial2x2Prefabs;
    public List<GameObject> lightIndustrial3x3Prefabs;

    public List<GameObject> mediumIndustrial1x1Prefabs;
    public List<GameObject> mediumIndustrial2x2Prefabs;
    public List<GameObject> mediumIndustrial3x3Prefabs;

    public List<GameObject> heavyIndustrial1x1Prefabs;
    public List<GameObject> heavyIndustrial2x2Prefabs;
    public List<GameObject> heavyIndustrial3x3Prefabs;

    public List<GameObject> residentialLightZoningPrefabs;
    public List<GameObject> residentialMediumZoningPrefabs;
    public List<GameObject> residentialHeavyZoningPrefabs;

    public List<GameObject> commercialLightZoningPrefabs;
    public List<GameObject> commercialMediumZoningPrefabs;
    public List<GameObject> commercialHeavyZoningPrefabs;

    public List<GameObject> industrialLightZoningPrefabs;
    public List<GameObject> industrialMediumZoningPrefabs;
    public List<GameObject> industrialHeavyZoningPrefabs;

    public List<GameObject> roadPrefabs;
    public List<GameObject> grassPrefabs;
    public List<GameObject> waterPrefabs;

    private Dictionary<(Zone.ZoneType, int), List<GameObject>> zonePrefabs;

    private bool isZoning = false;
    private Vector2 zoningStartGridPosition;
    private Vector2 zoningEndGridPosition;

    private List<Vector2> zonedResidentialLightPositions = new List<Vector2>();
    private List<Vector2> zonedResidentialMediumPositions = new List<Vector2>();
    private List<Vector2> zonedResidentialHeavyPositions = new List<Vector2>();

    private List<Vector2> zonedCommercialLightPositions = new List<Vector2>();
    private List<Vector2> zonedCommercialMediumPositions = new List<Vector2>();
    private List<Vector2> zonedCommercialHeavyPositions = new List<Vector2>();

    private List<Vector2> zonedIndustrialLightPositions = new List<Vector2>();
    private List<Vector2> zonedIndustrialMediumPositions = new List<Vector2>();
    private List<Vector2> zonedIndustrialHeavyPositions = new List<Vector2>();

    private List<GameObject> previewZoningTiles = new List<GameObject>();
    private Dictionary<Zone.ZoneType, List<List<Vector2>>> availableZoneSections =
      new Dictionary<Zone.ZoneType, List<List<Vector2>>>();

    private bool zonesSectionsDirty = true;

    [Header("Desirability-weighted spawn (FEAT-26)")]
    [SerializeField] private float baseSpawnWeight = 1.0f;
    [SerializeField] private float minDesirabilityThreshold = 2.0f;
    [SerializeField] private float lowDesirabilityPenalty = 0.1f;

    private const int MaxRoadDistanceForSpawning = 3;

    /// <summary>Invoked when urban cell (zoning) added or removed. Args: (position, isAdded). Not invoked on zoning→building conversion.</summary>
    public System.Action<Vector2, bool> onUrbanCellChanged;

    private bool isInitialized = false;
    #endregion

    #region Initialization
    /// <summary>
    /// Init zone prefabs dict early in Unity lifecycle.
    /// </summary>
    void Awake()
    {
        InitializeZonePrefabs();
    }

    /// <summary>
    /// Init zone prefabs dict.
    /// </summary>
    public void InitializeZonePrefabs()
    {
        if (isInitialized) return;

        zonePrefabs = new Dictionary<(Zone.ZoneType, int), List<GameObject>>
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
            { (Zone.ZoneType.Water, 1), waterPrefabs ?? new List<GameObject>() }
        };

        isInitialized = true;
    }

    void Start()
    {
        // Ensure initialization happened (should already be done in Awake, but double-check)
        if (!isInitialized)
        {
            InitializeZonePrefabs();
        }
    }
    #endregion

    #region Zone Queries
    /// <summary>
    /// Return static <see cref="ZoneAttributes"/> for given zone type, or null if unrecognized.
    /// </summary>
    /// <param name="zoneType">Zone type to look up.</param>
    /// <returns>Corresponding <see cref="ZoneAttributes"/>, or null.</returns>
    public ZoneAttributes GetZoneAttributes(Zone.ZoneType zoneType)
    {
        switch (zoneType)
        {
            case Zone.ZoneType.ResidentialLightZoning:
                return ZoneAttributes.ResidentialLightZoning;
            case Zone.ZoneType.ResidentialMediumZoning:
                return ZoneAttributes.ResidentialMediumZoning;
            case Zone.ZoneType.ResidentialHeavyZoning:
                return ZoneAttributes.ResidentialHeavyZoning;
            case Zone.ZoneType.ResidentialLightBuilding:
                return ZoneAttributes.ResidentialLightBuilding;
            case Zone.ZoneType.ResidentialMediumBuilding:
                return ZoneAttributes.ResidentialMediumBuilding;
            case Zone.ZoneType.ResidentialHeavyBuilding:
                return ZoneAttributes.ResidentialHeavyBuilding;
            case Zone.ZoneType.CommercialLightZoning:
                return ZoneAttributes.CommercialLightZoning;
            case Zone.ZoneType.CommercialMediumZoning:
                return ZoneAttributes.CommercialMediumZoning;
            case Zone.ZoneType.CommercialHeavyZoning:
                return ZoneAttributes.CommercialHeavyZoning;
            case Zone.ZoneType.CommercialLightBuilding:
                return ZoneAttributes.CommercialLightBuilding;
            case Zone.ZoneType.CommercialMediumBuilding:
                return ZoneAttributes.CommercialMediumBuilding;
            case Zone.ZoneType.CommercialHeavyBuilding:
                return ZoneAttributes.CommercialHeavyBuilding;
            case Zone.ZoneType.IndustrialLightZoning:
                return ZoneAttributes.IndustrialLightZoning;
            case Zone.ZoneType.IndustrialMediumZoning:
                return ZoneAttributes.IndustrialMediumZoning;
            case Zone.ZoneType.IndustrialHeavyZoning:
                return ZoneAttributes.IndustrialHeavyZoning;
            case Zone.ZoneType.IndustrialLightBuilding:
                return ZoneAttributes.IndustrialLightBuilding;
            case Zone.ZoneType.IndustrialMediumBuilding:
                return ZoneAttributes.IndustrialMediumBuilding;
            case Zone.ZoneType.IndustrialHeavyBuilding:
                return ZoneAttributes.IndustrialHeavyBuilding;
            case Zone.ZoneType.Road:
                return ZoneAttributes.Road;
            case Zone.ZoneType.Grass:
                return ZoneAttributes.Grass;
            case Zone.ZoneType.Water:
                return ZoneAttributes.Water;
            default:
                return null;
        }
    }

    /// <summary>
    /// Return random prefab for given zone type + building size, or null if none available.
    /// </summary>
    /// <param name="zoneType">Zone type to fetch prefab for.</param>
    /// <param name="size">Building footprint size (1, 2, or 3).</param>
    /// <returns>Random matching prefab GameObject, or null.</returns>
    public GameObject GetRandomZonePrefab(Zone.ZoneType zoneType, int size = 1)
    {
        if (!isInitialized)
        {
            InitializeZonePrefabs();
        }

        var key = (zoneType, size);

        if (!zonePrefabs.ContainsKey(key))
        {
            return null;
        }

        List<GameObject> prefabs = zonePrefabs[key];
        if (prefabs == null || prefabs.Count == 0)
        {
            if (zoneType == Zone.ZoneType.IndustrialHeavyBuilding)
                return GetRandomZonePrefab(Zone.ZoneType.IndustrialMediumBuilding, size);
            if (zoneType == Zone.ZoneType.IndustrialMediumBuilding)
                return GetRandomZonePrefab(Zone.ZoneType.IndustrialLightBuilding, size);
            return null;
        }

        return prefabs[UnityEngine.Random.Range(0, prefabs.Count)];
    }

    /// <summary>
    /// Return default grass tile prefab.
    /// </summary>
    /// <returns>Grass prefab GameObject.</returns>
    public GameObject GetGrassPrefab()
    {
        return grassPrefabs[0];
    }

    /// <summary>
    /// Return default water tile prefab.
    /// </summary>
    /// <returns>Water prefab GameObject.</returns>
    public GameObject GetWaterPrefab()
    {
        return waterPrefabs[0];
    }

    /// <summary>
    /// Search all zone, road, power plant, water plant prefab lists for prefab matching name.
    /// Strips "(Clone)" suffix before compare.
    /// </summary>
    /// <param name="prefabName">Prefab name to search (may include "(Clone)" suffix).</param>
    /// <returns>Matching prefab GameObject, or null if not found.</returns>
    public GameObject FindPrefabByName(string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName)) return null;
        string trimmedName = prefabName.Replace("(Clone)", "");

        // Search zonePrefabs first (grass, zoning, etc.) - does not depend on road/power/water managers
        if (zonePrefabs != null)
        {
            foreach (var prefabList in zonePrefabs.Values)
            {
                if (prefabList == null) continue;
                foreach (GameObject prefab in prefabList)
                {
                    if (prefab != null && prefab.name == trimmedName)
                        return prefab;
                }
            }
        }

        // Search road prefabs (only if list is available)
        if (roadManager != null)
        {
            var roadPrefabs = roadManager.GetRoadPrefabs();
            if (roadPrefabs != null)
            {
                foreach (var prefab in roadPrefabs)
                {
                    if (prefab != null && prefab.name == trimmedName)
                        return prefab;
                }
            }
        }

        // Search power plant prefabs
        if (powerPlantManager != null)
        {
            var powerPlantPrefabs = powerPlantManager.GetPowerPlantPrefabs();
            if (powerPlantPrefabs != null)
            {
                foreach (var prefab in powerPlantPrefabs)
                {
                    if (prefab != null && prefab.name == trimmedName)
                        return prefab;
                }
            }
        }

        // Search water plant prefabs
        if (waterPlantManager != null)
        {
            var waterPlantPrefabs = waterPlantManager.GetWaterPlantPrefabs();
            if (waterPlantPrefabs != null)
            {
                foreach (var prefab in waterPlantPrefabs)
                {
                    if (prefab != null && prefab.name == trimmedName)
                        return prefab;
                }
            }
        }

        if (slopePrefabRegistry != null)
        {
            GameObject slopePrefab = slopePrefabRegistry.FindByName(trimmedName);
            if (slopePrefab != null) return slopePrefab;
        }

        return null;
    }

    /// <summary>
    /// Return slope variant of flat prefab for cell at (x,y), or flat prefab itself if flat terrain or no variant exists.
    /// </summary>
    private GameObject GetSlopeAwarePrefab(GameObject flatPrefab, int x, int y)
    {
        if (slopePrefabRegistry == null || flatPrefab == null) return flatPrefab;
        if (gridManager == null || gridManager.terrainManager == null) return flatPrefab;

        TerrainSlopeType slopeType = gridManager.terrainManager.GetTerrainSlopeTypeAt(x, y);
        if (slopeType == TerrainSlopeType.Flat) return flatPrefab;

        GameObject slopeVariant = slopePrefabRegistry.GetSlopeVariant(flatPrefab, slopeType);
        return slopeVariant ?? flatPrefab;
    }
    #endregion

    #region Zone Placement
    /// <summary>
    /// Handle full zoning input lifecycle: start on mouse down, preview on drag, place on mouse up.
    /// </summary>
    /// <param name="gridPosition">Current grid position under cursor.</param>
    public void HandleZoning(Vector2 gridPosition)
    {
        if (Input.GetMouseButtonDown(0) && !isZoning)
        {
            StartZoning(gridPosition);
        }
        else if (Input.GetMouseButton(0) && isZoning)
        {
            UpdateZoningPreview(gridPosition);
        }
        else if (Input.GetMouseButtonUp(0) && isZoning)
        {
            PlaceZoning(gridPosition);
        }
    }

    void StartZoning(Vector2 gridPosition)
    {
        isZoning = true;
        zoningStartGridPosition = gridPosition;
        zoningEndGridPosition = gridPosition;
        ClearPreviewTiles();
        if (uiManager != null)
        {
            uiManager.HideGhostPreview();
        }
    }

    void ClearPreviewTiles()
    {
        foreach (var tile in previewZoningTiles)
        {
            Destroy(tile);
        }
        previewZoningTiles.Clear();
    }

    /// <summary>
    /// Return true when player actively dragging to zone area (mouse held after initial click).
    /// </summary>
    public bool IsZoning()
    {
        return isZoning;
    }

    /// <summary>
    /// Return cell count in current zoning preview rect (while dragging). Same rect logic as UpdateZoningPreview.
    /// </summary>
    public int GetPreviewZoneCellCount()
    {
        if (!isZoning)
            return 0;

        Vector2Int start = Vector2Int.FloorToInt(zoningStartGridPosition);
        Vector2Int end = Vector2Int.FloorToInt(zoningEndGridPosition);

        Vector2Int topLeft = new Vector2Int(Mathf.Min(start.x, end.x), Mathf.Max(start.y, end.y));
        Vector2Int bottomRight = new Vector2Int(Mathf.Max(start.x, end.x), Mathf.Min(start.y, end.y));

        int width = bottomRight.x - topLeft.x + 1;
        int height = topLeft.y - bottomRight.y + 1;
        return width * height;
    }

    void UpdateZoningPreview(Vector2 gridPosition)
    {
        zoningEndGridPosition = gridPosition;
        ClearPreviewTiles();

        Vector2Int start = Vector2Int.FloorToInt(zoningStartGridPosition);
        Vector2Int end = Vector2Int.FloorToInt(zoningEndGridPosition);

        Vector2Int topLeft = new Vector2Int(Mathf.Min(start.x, end.x), Mathf.Max(start.y, end.y));
        Vector2Int bottomRight = new Vector2Int(Mathf.Max(start.x, end.x), Mathf.Min(start.y, end.y));

        for (int x = topLeft.x; x <= bottomRight.x; x++)
        {
            for (int y = bottomRight.y; y <= topLeft.y; y++)
            {
                if (canPlaceZone(GetZoneAttributes(uiManager.GetSelectedZoneType()), new Vector2(x, y)))
                {
                    CityCell cell = gridManager.GetCell(x, y);
                    Vector2 worldPos = cell.transformPosition;

                    GameObject zoningPrefab = GetSlopeAwarePrefab(GetRandomZonePrefab(uiManager.GetSelectedZoneType()), x, y);

                    GameObject previewZoningTile = Instantiate(
                      zoningPrefab,
                      worldPos,
                      Quaternion.identity
                    );
                    previewZoningTile.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0.5f); // Set transparency
                    previewZoningTiles.Add(previewZoningTile);
                }
            }
        }
    }

    bool canPlaceZone(ZoneAttributes zoneAttributes, Vector3 gridPosition, bool requireInterstate = true)
    {
        if (zoneAttributes == null)
            return false;

        if (requireInterstate && interstateManager != null)
        {
            interstateManager.CheckInterstateConnectivity();
            if (!interstateManager.IsConnectedToInterstate)
            {
                if (gameNotificationManager != null)
                    gameNotificationManager.PostWarning("Connect a road to the Interstate Highway before zoning.");
                return false;
            }
        }

        if (!cityStats.CanAfford(zoneAttributes.ConstructionCost))
            return false;

        if (!gridManager.canPlaceBuilding(gridPosition, 1))
            return false;

        // Manual zone placement is always allowed - the restrictions are on building spawning
        return true;
    }

    void PlaceZoning(Vector2 gridPosition)
    {
        isZoning = false;
        ClearPreviewTiles();

        // Calculate the rectangle corners
        Vector2Int start = Vector2Int.FloorToInt(zoningStartGridPosition);
        Vector2Int end = Vector2Int.FloorToInt(zoningEndGridPosition);

        Vector2Int topLeft = new Vector2Int(Mathf.Min(start.x, end.x), Mathf.Max(start.y, end.y));
        Vector2Int bottomRight = new Vector2Int(Mathf.Max(start.x, end.x), Mathf.Min(start.y, end.y));

        // Place definitive zoning tiles
        for (int x = topLeft.x; x <= bottomRight.x; x++)
        {
            for (int y = bottomRight.y; y <= topLeft.y; y++)
            {
                if (canPlaceZone(GetZoneAttributes(uiManager.GetSelectedZoneType()), new Vector2(x, y)))
                {
                    PlaceZone(new Vector2(x, y));
                }
            }
        }

        CalculateAvailableSquareZonedSections();

        if (uiManager != null)
        {
            uiManager.RestoreGhostPreview();
        }
    }

    /// <summary>
    /// Recalc all available contiguous square sections (1x1, 2x2, 3x3) per zoning type.
    /// Call after placing/removing zones so buildings can spawn in valid sections.
    /// Skips recalc if no zoning changes since last run.
    /// </summary>
    public void CalculateAvailableSquareZonedSections()
    {
        if (!zonesSectionsDirty)
            return;
        zonesSectionsDirty = false;
        availableZoneSections.Clear();
        var validZoneTypes = GetValidZoneTypes();

        foreach (Zone.ZoneType zoneType in validZoneTypes)
        {
            List<List<Vector2>> sections = CalculateSectionsForZoneType(zoneType);
            availableZoneSections.Add(zoneType, sections);
        }
    }

    private static readonly Zone.ZoneType[] ValidZoneTypesForSections =
    {
        Zone.ZoneType.ResidentialLightZoning, Zone.ZoneType.ResidentialMediumZoning, Zone.ZoneType.ResidentialHeavyZoning,
        Zone.ZoneType.CommercialLightZoning, Zone.ZoneType.CommercialMediumZoning, Zone.ZoneType.CommercialHeavyZoning,
        Zone.ZoneType.IndustrialLightZoning, Zone.ZoneType.IndustrialMediumZoning, Zone.ZoneType.IndustrialHeavyZoning,
        Zone.ZoneType.Grass, Zone.ZoneType.Water
    };

    private Zone.ZoneType[] GetValidZoneTypes()
    {
        return ValidZoneTypesForSections;
    }

    private List<List<Vector2>> CalculateSectionsForZoneType(Zone.ZoneType zoneType)
    {
        List<List<Vector2>> sections = new List<List<Vector2>>();

        for (int size = 1; size <= 3; size++)
        {
            var zonedPositions = GetZonedPositions(zoneType);
            if (zonedPositions.Count == 0) continue;

            sections.AddRange(CalculateSectionsForSize(zonedPositions, size));
        }

        return sections;
    }

    private IReadOnlyList<Vector2> GetZonedPositions(Zone.ZoneType zoneType)
    {
        switch (zoneType)
        {
            case Zone.ZoneType.ResidentialLightZoning:
                return zonedResidentialLightPositions;
            case Zone.ZoneType.ResidentialMediumZoning:
                return zonedResidentialMediumPositions;
            case Zone.ZoneType.ResidentialHeavyZoning:
                return zonedResidentialHeavyPositions;
            case Zone.ZoneType.CommercialLightZoning:
                return zonedCommercialLightPositions;
            case Zone.ZoneType.CommercialMediumZoning:
                return zonedCommercialMediumPositions;
            case Zone.ZoneType.CommercialHeavyZoning:
                return zonedCommercialHeavyPositions;
            case Zone.ZoneType.IndustrialLightZoning:
                return zonedIndustrialLightPositions;
            case Zone.ZoneType.IndustrialMediumZoning:
                return zonedIndustrialMediumPositions;
            case Zone.ZoneType.IndustrialHeavyZoning:
                return zonedIndustrialHeavyPositions;
            default:
                return new List<Vector2>();
        }
    }

    private List<List<Vector2>> CalculateSectionsForSize(IReadOnlyList<Vector2> zonedPositions, int size)
    {
        List<List<Vector2>> sections = new List<List<Vector2>>();
        var usedPositions = new HashSet<Vector2>();
        var zonedSet = new HashSet<Vector2>(zonedPositions);

        for (int i = zonedPositions.Count - 1; i >= 0; i--)
        {
            Vector2 start = zonedPositions[i];
            if (usedPositions.Contains(start)) continue;

            List<Vector2> section = GetSquareSection(start, size, zonedSet, usedPositions);

            if (section.Count == size * size)
            {
                sections.Add(section);
                foreach (var pos in section)
                {
                    usedPositions.Add(pos);
                }
            }
        }

        return sections;
    }

    // Helper method to get square sections of a given size
    private List<Vector2> GetSquareSection(Vector2 start, int size, HashSet<Vector2> availablePositions, HashSet<Vector2> excludedPositions = null)
    {
        List<Vector2> section = new List<Vector2>();
        excludedPositions = excludedPositions ?? new HashSet<Vector2>();

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector2 newPosition = new Vector2(start.x + x, start.y + y);
                if (availablePositions.Contains(newPosition) && !excludedPositions.Contains(newPosition))
                {
                    section.Add(newPosition);
                }
            }
        }

        return section;
    }

    void PlaceZone(Vector3 gridPosition)
    {
        CityCell cell = gridManager.GetCell((int)gridPosition.x, (int)gridPosition.y);
        Vector2 worldPosition = cell.transformPosition;
        Zone.ZoneType selectedZoneType = uiManager.GetSelectedZoneType();

        var zoneAttributes = GetZoneAttributes(selectedZoneType);

        // Check if player can afford the zone
        if (zoneAttributes == null)
            return;

        if (!cityStats.CanAfford(zoneAttributes.ConstructionCost))
        {
            uiManager.ShowInsufficientFundsTooltip(selectedZoneType.ToString(), zoneAttributes.ConstructionCost);
            return;
        }

        if (canPlaceZone(zoneAttributes, gridPosition))
        {
            gridManager.DestroyCellChildrenExceptForest(cell.gameObject, gridPosition);

            GameObject zonePrefab = GetSlopeAwarePrefab(GetRandomZonePrefab(selectedZoneType), (int)gridPosition.x, (int)gridPosition.y);

            if (zonePrefab == null)
            {
                return;
            }

            GameObject zoneTile = Instantiate(
              zonePrefab,
              worldPosition,
              Quaternion.identity
            );

            Zone zone = zoneTile.AddComponent<Zone>();
            zone.zoneType = selectedZoneType;
            zone.zoneCategory = Zone.ZoneCategory.Zoning;

            UpdatePlacedZoneCellAttributes(cell, selectedZoneType, zonePrefab, zoneAttributes);

            gridManager.SetZoningTileSortingOrder(zoneTile, (int)gridPosition.x, (int)gridPosition.y);

            addZonedTileToList(gridPosition, selectedZoneType);

            cityStats.AddZoneBuildingCount(selectedZoneType);
            cityStats.RemoveMoney(zoneAttributes.ConstructionCost);
        }
        else
        {
            gameNotificationManager.PostError("Cannot place zone here.");
        }
    }

    /// <summary>
    /// Place zone at grid position with given zone type (programmatic/auto-zoning).
    /// Caller owns affordability + budget. Skips interstate check so auto-zoning works if connectivity lags.
    /// </summary>
    public bool PlaceZoneAt(Vector2 gridPosition, Zone.ZoneType zoneType)
    {
        var zoneAttributes = GetZoneAttributes(zoneType);
        if (zoneAttributes == null)
            return false;
        if (!cityStats.CanAfford(zoneAttributes.ConstructionCost))
            return false;
        if (!canPlaceZone(zoneAttributes, gridPosition, requireInterstate: false))
            return false;

        CityCell cell = gridManager.GetCell((int)gridPosition.x, (int)gridPosition.y);
        if (cell == null) return false;
        Vector2 worldPosition = cell.transformPosition;

        gridManager.DestroyCellChildrenExceptForest(cell.gameObject, gridPosition);

        GameObject zonePrefab = GetSlopeAwarePrefab(GetRandomZonePrefab(zoneType, 1), (int)gridPosition.x, (int)gridPosition.y);
        if (zonePrefab == null)
            return false;

        GameObject zoneTile = Instantiate(zonePrefab, worldPosition, Quaternion.identity);
        Zone zone = zoneTile.AddComponent<Zone>();
        zone.zoneType = zoneType;
        zone.zoneCategory = Zone.ZoneCategory.Zoning;

        UpdatePlacedZoneCellAttributes(cell, zoneType, zonePrefab, zoneAttributes);
        gridManager.SetZoningTileSortingOrder(zoneTile, (int)gridPosition.x, (int)gridPosition.y);
        addZonedTileToList(gridPosition, zoneType);
        cityStats.AddZoneBuildingCount(zoneType);
        return true;
    }

    /// <summary>
    /// Restore zoning overlay tile from save. Uses SetZoningTileSortingOrder so renders correctly (below roads + buildings). Use instead of PlaceZoneBuildingTile for zoning types.
    /// </summary>
    public void RestoreZoneTile(GameObject prefab, GameObject gridCell, Zone.ZoneType zoneType)
    {
        CityCell cell = gridCell.GetComponent<CityCell>();
        if (cell == null) return;

        Vector2 worldPosition = cell.transformPosition;
        gridManager.DestroyCellChildrenExceptForest(gridCell, new Vector2(cell.x, cell.y));

        GameObject zoneTile = Instantiate(prefab, worldPosition, Quaternion.identity);
        zoneTile.transform.SetParent(gridCell.transform);

        Zone zone = zoneTile.GetComponent<Zone>();
        if (zone == null) zone = zoneTile.AddComponent<Zone>();
        zone.zoneType = zoneType;
        zone.zoneCategory = Zone.ZoneCategory.Zoning;

        var zoneAttributes = GetZoneAttributes(zoneType);
        if (zoneAttributes != null)
            UpdatePlacedZoneCellAttributes(cell, zoneType, prefab, zoneAttributes);
        gridManager.SetZoningTileSortingOrder(zoneTile, cell.x, cell.y);
    }

    /// <summary>
    /// Return true if zone type is zoning overlay (empty zone awaiting building spawn).
    /// </summary>
    public static bool IsZoningType(Zone.ZoneType zoneType)
    {
        return zoneType == Zone.ZoneType.ResidentialLightZoning || zoneType == Zone.ZoneType.ResidentialMediumZoning
            || zoneType == Zone.ZoneType.ResidentialHeavyZoning || zoneType == Zone.ZoneType.CommercialLightZoning
            || zoneType == Zone.ZoneType.CommercialMediumZoning || zoneType == Zone.ZoneType.CommercialHeavyZoning
            || zoneType == Zone.ZoneType.IndustrialLightZoning || zoneType == Zone.ZoneType.IndustrialMediumZoning
            || zoneType == Zone.ZoneType.IndustrialHeavyZoning;
    }

    #endregion

    #region Zone Building Placement
    /// <summary>
    /// Remove grid position from tracked zoned-positions list for specified zoning type.
    /// </summary>
    /// <param name="zonedPosition">Grid position to remove.</param>
    /// <param name="zoneType">Zoning type list to remove from.</param>
    public void removeZonedPositionFromList(Vector2 zonedPosition, Zone.ZoneType zoneType, bool isConversionToBuilding = false)
    {
        zonesSectionsDirty = true;
        if (!isConversionToBuilding)
            onUrbanCellChanged?.Invoke(zonedPosition, false);
        switch (zoneType)
        {
            case Zone.ZoneType.ResidentialLightZoning:
                zonedResidentialLightPositions.Remove(zonedPosition);
                break;
            case Zone.ZoneType.ResidentialMediumZoning:
                zonedResidentialMediumPositions.Remove(zonedPosition);
                break;
            case Zone.ZoneType.ResidentialHeavyZoning:
                zonedResidentialHeavyPositions.Remove(zonedPosition);
                break;
            case Zone.ZoneType.CommercialLightZoning:
                zonedCommercialLightPositions.Remove(zonedPosition);
                break;
            case Zone.ZoneType.CommercialMediumZoning:
                zonedCommercialMediumPositions.Remove(zonedPosition);
                break;
            case Zone.ZoneType.CommercialHeavyZoning:
                zonedCommercialHeavyPositions.Remove(zonedPosition);
                break;
            case Zone.ZoneType.IndustrialLightZoning:
                zonedIndustrialLightPositions.Remove(zonedPosition);
                break;
            case Zone.ZoneType.IndustrialMediumZoning:
                zonedIndustrialMediumPositions.Remove(zonedPosition);
                break;
            case Zone.ZoneType.IndustrialHeavyZoning:
                zonedIndustrialHeavyPositions.Remove(zonedPosition);
                break;
        }
    }

    /// <summary>
    /// Attempt to place building on random available zoned section of given zoning type.
    /// Checks demand, power, water availability before placement.
    /// </summary>
    /// <param name="zoningType">Zoning type to place building for (e.g. ResidentialLightZoning).</param>
    public void PlaceZonedBuildings(Zone.ZoneType zoningType)
    {
        if (availableZoneSections.Count == 0)
        {
            return;
        }

        var sectionResult = GetRandomAvailableSection(zoningType);
        if (!sectionResult.HasValue || sectionResult.Value.size == 0)
        {
            return;
        }

        Zone.ZoneType buildingZoneType = GetBuildingZoneType(zoningType);

        if (IsResidentialBuilding(buildingZoneType))
        {
            int availableJobs = demandManager != null ? demandManager.GetAvailableJobs() : 0;

            if (!CanPlaceResidentialBuilding())
            {
                return;
            }

            if (demandManager != null && !demandManager.GetResidentialDemand().canGrow)
            {
                return;
            }
        }
        else
        {
            if (!CanPlaceCommercialOrIndustrialBuilding(buildingZoneType))
            {
                return;
            }

            // Demand as spawn probability: low demand reduces spawn rate instead of blocking
            if (demandManager != null)
            {
                float demandFactor = demandManager.GetDemandSpawnFactor(zoningType);
                if (UnityEngine.Random.value > demandFactor)
                {
                    return;
                }
            }
        }

        // Check both power and water availability
        if (!cityStats.GetCityPowerAvailability())
        {
            return;
        }

        // Check water availability
        if (waterManager != null && !waterManager.GetCityWaterAvailability())
        {
            return;
        }

        Vector2[] section = sectionResult.Value.section;
        int buildingSize = (int)System.Math.Sqrt(section.Length);

        ZoneAttributes zoneAttributes = GetZoneAttributes(buildingZoneType);

        PlaceZoneBuilding(section, buildingZoneType, zoneAttributes, zoningType, buildingSize);
    }

    private (int size, Vector2[] section)? GetRandomAvailableSection(Zone.ZoneType zoneType)
    {
        if (!availableZoneSections.ContainsKey(zoneType) || availableZoneSections[zoneType].Count == 0)
            return null;

        var allSections = new List<(int size, List<Vector2> section)>();
        var sections = availableZoneSections[zoneType];

        for (int i = 0; i < sections.Count; i++)
        {
            int count = sections[i].Count;
            int size = (int)System.Math.Sqrt(count);
            if (size >= 1 && size <= 3 && count == size * size && IsSectionWithinDistanceOfRoad(sections[i]))
                allSections.Add((size, sections[i]));
        }

        if (allSections.Count == 0)
            return null;

        var selected = GetWeightedSection(zoneType, allSections);
        if (selected.section == null)
            return null;

        var result = new Vector2[selected.section.Count];
        for (int i = 0; i < selected.section.Count; i++)
            result[i] = selected.section[i];
        return (selected.size, result);
    }

    private (int size, List<Vector2> section) GetWeightedSection(Zone.ZoneType zoneType, List<(int size, List<Vector2> section)> candidates)
    {
        if (candidates == null || candidates.Count == 0)
            return (0, null);

        bool isIndustrial = IsIndustrialZoning(zoneType);
        float[] weights = new float[candidates.Count];
        float maxDesir = 0f;

        for (int i = 0; i < candidates.Count; i++)
        {
            float avg = AverageSectionDesirability(candidates[i].section);
            if (avg > maxDesir) maxDesir = avg;
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            float avgDesir = AverageSectionDesirability(candidates[i].section);

            if (isIndustrial)
            {
                weights[i] = (maxDesir - avgDesir) + baseSpawnWeight;
            }
            else
            {
                weights[i] = avgDesir + baseSpawnWeight;
                if (avgDesir < minDesirabilityThreshold)
                    weights[i] *= lowDesirabilityPenalty;
            }

            if (weights[i] < 0) weights[i] = 0;
        }

        float totalWeight = 0;
        for (int i = 0; i < weights.Length; i++)
            totalWeight += weights[i];

        if (totalWeight <= 0)
        {
            int idx = UnityEngine.Random.Range(0, candidates.Count);
            return candidates[idx];
        }

        float roll = UnityEngine.Random.value * totalWeight;
        float cumulative = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (roll <= cumulative)
                return candidates[i];
        }
        return candidates[candidates.Count - 1];
    }

    private bool IsSectionWithinDistanceOfRoad(List<Vector2> section)
    {
        if (section == null || gridManager == null) return false;
        for (int i = 0; i < section.Count; i++)
        {
            int x = (int)section[i].x, y = (int)section[i].y;
            if (gridManager.IsWithinDistanceOfRoad(x, y, MaxRoadDistanceForSpawning))
                return true;
        }
        return false;
    }

    private float AverageSectionDesirability(List<Vector2> section)
    {
        if (section == null || section.Count == 0 || gridManager == null) return 0f;
        float total = 0f;
        for (int i = 0; i < section.Count; i++)
        {
            CityCell c = gridManager.GetCell((int)section[i].x, (int)section[i].y);
            if (c != null)
                total += c.desirability;
        }
        return total / section.Count;
    }

    private static bool IsIndustrialZoning(Zone.ZoneType zoneType)
    {
        return zoneType == Zone.ZoneType.IndustrialLightZoning || zoneType == Zone.ZoneType.IndustrialMediumZoning || zoneType == Zone.ZoneType.IndustrialHeavyZoning;
    }

    /// <summary>
    /// Map zoning overlay type → corresponding building zone type (e.g. ResidentialLightZoning → ResidentialLightBuilding).
    /// </summary>
    /// <param name="zoningType">Zoning overlay type.</param>
    /// <returns>Corresponding building zone type, or Grass if unrecognized.</returns>
    public Zone.ZoneType GetBuildingZoneType(Zone.ZoneType zoningType)
    {
        switch (zoningType)
        {
            case Zone.ZoneType.ResidentialLightZoning:
                return Zone.ZoneType.ResidentialLightBuilding;
            case Zone.ZoneType.ResidentialMediumZoning:
                return Zone.ZoneType.ResidentialMediumBuilding;
            case Zone.ZoneType.ResidentialHeavyZoning:
                return Zone.ZoneType.ResidentialHeavyBuilding;
            case Zone.ZoneType.CommercialLightZoning:
                return Zone.ZoneType.CommercialLightBuilding;
            case Zone.ZoneType.CommercialMediumZoning:
                return Zone.ZoneType.CommercialMediumBuilding;
            case Zone.ZoneType.CommercialHeavyZoning:
                return Zone.ZoneType.CommercialHeavyBuilding;
            case Zone.ZoneType.IndustrialLightZoning:
                return Zone.ZoneType.IndustrialLightBuilding;
            case Zone.ZoneType.IndustrialMediumZoning:
                return Zone.ZoneType.IndustrialMediumBuilding;
            case Zone.ZoneType.IndustrialHeavyZoning:
                return Zone.ZoneType.IndustrialHeavyBuilding;
            default:
                return Zone.ZoneType.Grass;
        }
    }

    /// <summary>
    /// Return true if zone type is any residential building density (light, medium, heavy).
    /// </summary>
    /// <param name="zoneType">Zone type to check.</param>
    /// <returns>True if residential building.</returns>
    public bool IsResidentialBuilding(Zone.ZoneType zoneType)
    {
        return (zoneType == Zone.ZoneType.ResidentialLightBuilding ||
                zoneType == Zone.ZoneType.ResidentialMediumBuilding ||
                zoneType == Zone.ZoneType.ResidentialHeavyBuilding);
    }

    /// <summary>
    /// Return true if zone type is any commercial or industrial building density.
    /// </summary>
    /// <param name="zoneType">Zone type to check.</param>
    /// <returns>True if commercial or industrial building.</returns>
    public bool IsCommercialOrIndustrialBuilding(Zone.ZoneType zoneType)
    {
        return (zoneType == Zone.ZoneType.CommercialLightBuilding ||
                zoneType == Zone.ZoneType.CommercialMediumBuilding ||
                zoneType == Zone.ZoneType.CommercialHeavyBuilding ||
                zoneType == Zone.ZoneType.IndustrialLightBuilding ||
                zoneType == Zone.ZoneType.IndustrialMediumBuilding ||
                zoneType == Zone.ZoneType.IndustrialHeavyBuilding);
    }

    private bool CanPlaceResidentialBuilding()
    {
        if (demandManager == null) return true;

        return demandManager.CanPlaceResidentialBuilding();
    }


    private bool CanPlaceCommercialOrIndustrialBuilding(Zone.ZoneType buildingType)
    {
        if (demandManager == null) return true;

        return demandManager.CanPlaceCommercialOrIndustrialBuilding(buildingType);
    }

    private bool CanZoneTypeGrowBasedOnDemand(Zone.ZoneType zoningType)
    {
        if (demandManager == null)
        {
            return true; // If no demand manager, allow all growth
        }

        return demandManager.CanZoneTypeGrow(zoningType);
    }

    /// <summary>
    /// Add grid position to tracked zoned-positions list for specified zoning type.
    /// </summary>
    /// <param name="zonedPosition">Grid position to add.</param>
    /// <param name="zoneType">Zoning type list to add to.</param>
    public void addZonedTileToList(Vector2 zonedPosition, Zone.ZoneType zoneType)
    {
        zonesSectionsDirty = true;
        onUrbanCellChanged?.Invoke(zonedPosition, true);
        switch (zoneType)
        {
            case Zone.ZoneType.ResidentialLightZoning:
                zonedResidentialLightPositions.Add(zonedPosition);
                break;
            case Zone.ZoneType.ResidentialMediumZoning:
                zonedResidentialMediumPositions.Add(zonedPosition);
                break;
            case Zone.ZoneType.ResidentialHeavyZoning:
                zonedResidentialHeavyPositions.Add(zonedPosition);
                break;
            case Zone.ZoneType.CommercialLightZoning:
                zonedCommercialLightPositions.Add(zonedPosition);
                break;
            case Zone.ZoneType.CommercialMediumZoning:
                zonedCommercialMediumPositions.Add(zonedPosition);
                break;
            case Zone.ZoneType.CommercialHeavyZoning:
                zonedCommercialHeavyPositions.Add(zonedPosition);
                break;
            case Zone.ZoneType.IndustrialLightZoning:
                zonedIndustrialLightPositions.Add(zonedPosition);
                break;
            case Zone.ZoneType.IndustrialMediumZoning:
                zonedIndustrialMediumPositions.Add(zonedPosition);
                break;
            case Zone.ZoneType.IndustrialHeavyZoning:
                zonedIndustrialHeavyPositions.Add(zonedPosition);
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Parse string → Zone.ZoneType enum value.
    /// </summary>
    /// <param name="zoneTypeString">String representation of zone type.</param>
    /// <returns>Parsed Zone.ZoneType.</returns>
    public Zone.ZoneType GetZoneTypeFromZoneTypeString(string zoneTypeString)
    {
        if (string.IsNullOrEmpty(zoneTypeString))
            return Zone.ZoneType.Grass;
        if (System.Enum.TryParse(zoneTypeString, out Zone.ZoneType result))
            return result;
        return Zone.ZoneType.Grass;
    }

    void PlaceZoneBuilding(Vector2[] section, Zone.ZoneType selectedZoneType, ZoneAttributes zoneAttributes, Zone.ZoneType zoningType, int buildingSize)
    {
        Vector2 firstPos = section[0];
        GameObject prefab = GetSlopeAwarePrefab(GetRandomZonePrefab(selectedZoneType, buildingSize), (int)firstPos.x, (int)firstPos.y);

        if (prefab == null)
        {
            return;
        }

        foreach (Vector2 zonedPosition in section)
        {
            GameObject cell = gridManager.GetGridCell(zonedPosition);
            CityCell cellComponent = gridManager.GetCell((int)zonedPosition.x, (int)zonedPosition.y);
            if (cellComponent != null)
                cellComponent.RemoveForestForBuilding();

            gridManager.DestroyCellChildren(cell, zonedPosition, null, destroyFlatGrass: true);

            gridManager.UpdateCellAttributes(cellComponent, selectedZoneType, zoneAttributes, prefab, buildingSize);

            removeZonedPositionFromList(zonedPosition, zoningType, isConversionToBuilding: true);
            cityStats.RemoveZoneBuildingCount(zoningType);
        }

        GameObject firstPositionGridCell = gridManager.GetGridCell(firstPos);
        gridManager.GetCell((int)firstPos.x, (int)firstPos.y).isPivot = true;

        PlaceZoneBuildingTile(prefab, firstPositionGridCell, buildingSize);

        UpdateZonedBuildingPlacementStats(selectedZoneType, zoneAttributes);
    }

    void UpdateZonedBuildingPlacementStats(Zone.ZoneType selectedZoneType, ZoneAttributes zoneAttributes)
    {
        cityStats.HandleZoneBuildingPlacement(selectedZoneType, zoneAttributes);

        cityStats.AddPowerConsumption(zoneAttributes.PowerConsumption);
    }

    /// <summary>
    /// Instantiate zone building prefab on given grid cell, set sorting order + pivot.
    /// For multi-tile buildings, only pivot cell gets tile placed.
    /// </summary>
    /// <param name="prefab">Building prefab to instantiate.</param>
    /// <param name="gridCell">Grid cell GameObject to place building on.</param>
    /// <param name="buildingSize">Building footprint size (1, 2, or 3).</param>
    public void PlaceZoneBuildingTile(GameObject prefab, GameObject gridCell, int buildingSize = 1)
    {
        CityCell cell = gridCell.GetComponent<CityCell>();

        if (buildingSize > 1 && !cell.isPivot)
        {
            return;
        }

        Vector3 worldPosition = cell.transformPosition;

        if (buildingSize > 1 && cell.zoneType != Zone.ZoneType.Building)
        {
            Vector3 offset = new Vector3(0, -(buildingSize - 1) * gridManager.tileHeight / 2, 0);
            worldPosition -= offset;
        }

        gridManager.DestroyCellChildren(gridCell, new Vector2(cell.x, cell.y), null, destroyFlatGrass: true);

        GameObject zoneTile = Instantiate(
          prefab,
          worldPosition,
          Quaternion.identity
        );

        cell.isPivot = true;

        gridManager.SetZoneBuildingSortingOrder(zoneTile, (int)cell.x, (int)cell.y, buildingSize);
    }

    void UpdatePlacedZoneCellAttributes(CityCell cellComponent, Zone.ZoneType selectedZoneType, GameObject zonePrefab, ZoneAttributes zoneAttributes)
    {
        cellComponent.zoneType = selectedZoneType;
        cellComponent.population = zoneAttributes.Population;
        cellComponent.powerConsumption = zoneAttributes.PowerConsumption;
        cellComponent.waterConsumption = zoneAttributes.WaterConsumption;
        cellComponent.happiness = zoneAttributes.Happiness;
        cellComponent.prefab = zonePrefab;
        cellComponent.prefabName = zonePrefab.name;
        cellComponent.buildingType = null;
        cellComponent.buildingSize = 1;
        cellComponent.powerPlant = null;
        cellComponent.occupiedBuilding = null;
        cellComponent.isPivot = false;
    }
    #endregion

    #region Zone Removal
    private Zone.ZoneType GetDemandZoneType(Zone.ZoneType zoningType)
    {
        switch (zoningType)
        {
            // Residential
            case Zone.ZoneType.ResidentialLightZoning:
            case Zone.ZoneType.ResidentialMediumZoning:
            case Zone.ZoneType.ResidentialHeavyZoning:
                return Zone.ZoneType.ResidentialLightZoning; // Use light as representative

            // Commercial
            case Zone.ZoneType.CommercialLightZoning:
            case Zone.ZoneType.CommercialMediumZoning:
            case Zone.ZoneType.CommercialHeavyZoning:
                return Zone.ZoneType.CommercialLightZoning; // Use light as representative

            // Industrial
            case Zone.ZoneType.IndustrialLightZoning:
            case Zone.ZoneType.IndustrialMediumZoning:
            case Zone.ZoneType.IndustrialHeavyZoning:
                return Zone.ZoneType.IndustrialLightZoning; // Use light as representative

            default:
                return zoningType;
        }
    }

    /// <summary>
    /// Remove specific section of zoned positions from available zone sections cache.
    /// </summary>
    /// <param name="zonedPositions">Array of positions defining section to remove.</param>
    /// <param name="zoneType">Zone type whose section list to update.</param>
    public void RemoveZonedSectionFromList(Vector2[] zonedPositions, Zone.ZoneType zoneType)
    {
        if (!availableZoneSections.ContainsKey(zoneType))
            return;
        var sections = availableZoneSections[zoneType];
        for (int i = 0; i < sections.Count; i++)
        {
            var section = sections[i];
            if (section.Count != zonedPositions.Length)
                continue;
            bool match = true;
            for (int j = 0; j < section.Count; j++)
            {
                if (section[j] != zonedPositions[j])
                {
                    match = false;
                    break;
                }
            }
            if (match)
            {
                sections.RemoveAt(i);
                return;
            }
        }
    }

    /// <summary>
    /// Clear all tracked zoned-position lists + available zone sections cache.
    /// </summary>
    public void ClearZonedPositions()
    {
        zonedResidentialLightPositions.Clear();
        zonedResidentialMediumPositions.Clear();
        zonedResidentialHeavyPositions.Clear();
        zonedCommercialLightPositions.Clear();
        zonedCommercialMediumPositions.Clear();
        zonedCommercialHeavyPositions.Clear();
        zonedIndustrialLightPositions.Clear();
        zonedIndustrialMediumPositions.Clear();
        zonedIndustrialHeavyPositions.Clear();

        availableZoneSections.Clear();
        zonesSectionsDirty = true;
    }
    #endregion
}
}
