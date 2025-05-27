using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class ZoneManager : MonoBehaviour
{
    public GridManager gridManager;
    public PowerPlant powerPlantManager;
    public WaterPlant waterPlantManager;
    public RoadManager roadManager;
    public CityStats cityStats;
    public UIManager uiManager;
    public GameNotificationManager gameNotificationManager;
    public DemandManager demandManager;
    public WaterManager waterManager;

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

    void Start()
    {
        zonePrefabs = new Dictionary<(Zone.ZoneType, int), List<GameObject>>
        {
            { (Zone.ZoneType.ResidentialLightBuilding, 1), lightResidential1x1Prefabs },
            { (Zone.ZoneType.ResidentialLightBuilding, 2), lightResidential2x2Prefabs },
            { (Zone.ZoneType.ResidentialLightBuilding, 3), lightResidential3x3Prefabs },
            { (Zone.ZoneType.ResidentialMediumBuilding, 1), mediumResidential1x1Prefabs },
            { (Zone.ZoneType.ResidentialMediumBuilding, 2), mediumResidential2x2Prefabs },
            { (Zone.ZoneType.ResidentialMediumBuilding, 3), mediumResidential3x3Prefabs },
            { (Zone.ZoneType.ResidentialHeavyBuilding, 1), heavyResidential1x1Prefabs },
            { (Zone.ZoneType.ResidentialHeavyBuilding, 2), heavyResidential2x2Prefabs },
            { (Zone.ZoneType.ResidentialHeavyBuilding, 3), heavyResidential3x3Prefabs },
            { (Zone.ZoneType.CommercialLightBuilding, 1), lightCommercial1x1Prefabs },
            { (Zone.ZoneType.CommercialLightBuilding, 2), lightCommercial2x2Prefabs },
            { (Zone.ZoneType.CommercialLightBuilding, 3), lightCommercial3x3Prefabs },
            { (Zone.ZoneType.CommercialMediumBuilding, 1), mediumCommercial1x1Prefabs },
            { (Zone.ZoneType.CommercialMediumBuilding, 2), mediumCommercial2x2Prefabs },
            { (Zone.ZoneType.CommercialMediumBuilding, 3), mediumCommercial3x3Prefabs },
            { (Zone.ZoneType.CommercialHeavyBuilding, 1), heavyCommercial1x1Prefabs },
            { (Zone.ZoneType.CommercialHeavyBuilding, 2), heavyCommercial2x2Prefabs },
            { (Zone.ZoneType.CommercialHeavyBuilding, 3), heavyCommercial3x3Prefabs },
            { (Zone.ZoneType.IndustrialLightBuilding, 1), lightIndustrial1x1Prefabs },
            { (Zone.ZoneType.IndustrialLightBuilding, 2), lightIndustrial2x2Prefabs },
            { (Zone.ZoneType.IndustrialLightBuilding, 3), lightIndustrial3x3Prefabs },
            { (Zone.ZoneType.IndustrialMediumBuilding, 1), mediumIndustrial1x1Prefabs },
            { (Zone.ZoneType.IndustrialMediumBuilding, 2), mediumIndustrial2x2Prefabs },
            { (Zone.ZoneType.IndustrialMediumBuilding, 3), mediumIndustrial3x3Prefabs },
            { (Zone.ZoneType.IndustrialHeavyBuilding, 1), heavyIndustrial1x1Prefabs },
            { (Zone.ZoneType.IndustrialHeavyBuilding, 2), heavyIndustrial2x2Prefabs },
            { (Zone.ZoneType.IndustrialHeavyBuilding, 3), heavyIndustrial3x3Prefabs },
            { (Zone.ZoneType.ResidentialLightZoning, 1), residentialLightZoningPrefabs },
            { (Zone.ZoneType.ResidentialMediumZoning, 1), residentialMediumZoningPrefabs },
            { (Zone.ZoneType.ResidentialHeavyZoning, 1), residentialHeavyZoningPrefabs },
            { (Zone.ZoneType.CommercialLightZoning, 1), commercialLightZoningPrefabs },
            { (Zone.ZoneType.CommercialMediumZoning, 1), commercialMediumZoningPrefabs },
            { (Zone.ZoneType.CommercialHeavyZoning, 1), commercialHeavyZoningPrefabs },
            { (Zone.ZoneType.IndustrialLightZoning, 1), industrialLightZoningPrefabs },
            { (Zone.ZoneType.IndustrialMediumZoning, 1), industrialMediumZoningPrefabs },
            { (Zone.ZoneType.IndustrialHeavyZoning, 1), industrialHeavyZoningPrefabs },
            { (Zone.ZoneType.Grass, 1), grassPrefabs },
            { (Zone.ZoneType.Road, 1), roadPrefabs },
            { (Zone.ZoneType.Water, 1), waterPrefabs }
        };
    }

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

    public GameObject GetRandomZonePrefab(Zone.ZoneType zoneType, int size = 1)
    {
        var key = (zoneType, size);

        if (!zonePrefabs.ContainsKey(key)) return null;

        List<GameObject> prefabs = zonePrefabs[key];
        if (prefabs.Count == 0) return null;

        return prefabs[UnityEngine.Random.Range(0, prefabs.Count)];
    }

    public GameObject GetGrassPrefab()
    {
        return grassPrefabs[0];
    }

    public GameObject GetWaterPrefab()
    {
        return waterPrefabs[0];
    }

    public GameObject FindPrefabByName(string prefabName)
    {
        string trimmedName = prefabName.Replace("(Clone)", "");

        List<GameObject> roadPrefabs = roadManager.GetRoadPrefabs();
        List<GameObject> powerPlantPrefabs = powerPlantManager.GetPowerPlantPrefabs();
        List<GameObject> waterPlantPrefabs = waterPlantManager.GetWaterPlantPrefabs();

        if (roadPrefabs == null || powerPlantPrefabs == null || waterPlantPrefabs == null)
        {
            Debug.LogWarning("One or more prefab lists are null.");
            return null;
        }

        foreach (var prefabList in zonePrefabs.Values)
        {
            foreach (GameObject prefab in prefabList)
            {
                if (prefab.name == trimmedName)
                {
                    return prefab;
                }
            }
        }

        foreach (var prefab in roadPrefabs)
        {
            if (prefab.name == trimmedName)
            {
                return prefab;
            }
        }

        foreach (var prefab in powerPlantPrefabs)
        {
            if (prefab.name == trimmedName)
            {
                return prefab;
            }
        }

        foreach (var prefab in waterPlantPrefabs)
        {
            if (prefab.name == trimmedName)
            {
                return prefab;
            }
        }

        Debug.LogWarning($"Prefab with name {trimmedName} not found.");
        return null;
    }

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
    }

    void ClearPreviewTiles()
    {
        foreach (var tile in previewZoningTiles)
        {
            Destroy(tile);
        }
        previewZoningTiles.Clear();
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
                    Vector2 worldPos = gridManager.GetWorldPosition(x, y);

                    GameObject zoningPrefab = GetRandomZonePrefab(uiManager.GetSelectedZoneType());

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

    bool canPlaceZone(ZoneAttributes zoneAttributes, Vector3 gridPosition)
    {
        if (zoneAttributes == null)
            return false;
            
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
    }

    public void CalculateAvailableSquareZonedSections()
    {
        availableZoneSections.Clear();
        var validZoneTypes = GetValidZoneTypes();
        
        foreach (Zone.ZoneType zoneType in validZoneTypes)
        {
            List<List<Vector2>> sections = CalculateSectionsForZoneType(zoneType);
            availableZoneSections.Add(zoneType, sections);
        }
    }

    private IEnumerable<Zone.ZoneType> GetValidZoneTypes()
    {
        var excludedTypes = new[]
        {
            Zone.ZoneType.None, Zone.ZoneType.Road, Zone.ZoneType.Building,
            Zone.ZoneType.ResidentialLightBuilding, Zone.ZoneType.ResidentialMediumBuilding,
            Zone.ZoneType.ResidentialHeavyBuilding, Zone.ZoneType.CommercialLightBuilding,
            Zone.ZoneType.CommercialMediumBuilding, Zone.ZoneType.CommercialHeavyBuilding,
            Zone.ZoneType.IndustrialLightBuilding, Zone.ZoneType.IndustrialMediumBuilding,
            Zone.ZoneType.IndustrialHeavyBuilding
        };

        return System.Enum.GetValues(typeof(Zone.ZoneType))
                  .Cast<Zone.ZoneType>()
                  .Where(type => !excludedTypes.Contains(type));
    }

    private List<List<Vector2>> CalculateSectionsForZoneType(Zone.ZoneType zoneType)
    {
        List<List<Vector2>> sections = new List<List<Vector2>>();
        
        for (int size = 1; size <= 3; size++)
        {
            var zonedPositions = GetZonedPositions(zoneType).ToList();
            if (!zonedPositions.Any()) continue;
            
            sections.AddRange(CalculateSectionsForSize(zonedPositions, size));
        }
        
        return sections;
    }

    private Vector2[] GetZonedPositions(Zone.ZoneType zoneType)
    {
        switch (zoneType)
        {
            case Zone.ZoneType.ResidentialLightZoning:
                return zonedResidentialLightPositions.ToArray();
            case Zone.ZoneType.ResidentialMediumZoning:
                return zonedResidentialMediumPositions.ToArray();
            case Zone.ZoneType.ResidentialHeavyZoning:
                return zonedResidentialHeavyPositions.ToArray();
            case Zone.ZoneType.CommercialLightZoning:
                return zonedCommercialLightPositions.ToArray();
            case Zone.ZoneType.CommercialMediumZoning:
                return zonedCommercialMediumPositions.ToArray();
            case Zone.ZoneType.CommercialHeavyZoning:
                return zonedCommercialHeavyPositions.ToArray();
            case Zone.ZoneType.IndustrialLightZoning:
                return zonedIndustrialLightPositions.ToArray();
            case Zone.ZoneType.IndustrialMediumZoning:
                return zonedIndustrialMediumPositions.ToArray();
            case Zone.ZoneType.IndustrialHeavyZoning:
                return zonedIndustrialHeavyPositions.ToArray();
            default:
                return new Vector2[0];
        }
    }

    private List<List<Vector2>> CalculateSectionsForSize(List<Vector2> zonedPositions, int size)
    {
        List<List<Vector2>> sections = new List<List<Vector2>>();
        
        for (int i = zonedPositions.Count - 1; i >= 0; i--)
        {
            Vector2 start = zonedPositions[i];
            List<Vector2> section = GetSquareSection(start, size, zonedPositions);
            
            if (section.Count == size * size)
            {
                sections.Add(section);
                foreach (var pos in section)
                {
                    zonedPositions.Remove(pos);
                }
            }
        }
        
        return sections;
    }

    // Helper method to get square sections of a given size
    private List<Vector2> GetSquareSection(Vector2 start, int size, List<Vector2> availablePositions)
    {
        List<Vector2> section = new List<Vector2>();

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector2 newPosition = new Vector2(start.x + x, start.y + y);
                if (availablePositions.Contains(newPosition))
                {
                    section.Add(newPosition);
                }
            }
        }

        return section;
    }

    void PlaceZone(Vector3 gridPosition)
    {
        Vector2 worldPosition = gridManager.GetWorldPosition((int)gridPosition.x, (int)gridPosition.y);
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
            GameObject cell = gridManager.GetGridCell(gridPosition);

            gridManager.DestroyCellChildren(cell, gridPosition);

            GameObject zonePrefab = GetRandomZonePrefab(selectedZoneType);
            
            if (zonePrefab == null)
            {
                return;
            }

            GameObject zoneTile = Instantiate(
              zonePrefab,
              worldPosition,
              Quaternion.identity
            );
            zoneTile.transform.SetParent(cell.transform);

            Zone zone = zoneTile.AddComponent<Zone>();
            zone.zoneType = selectedZoneType;
            zone.zoneCategory = Zone.ZoneCategory.Zoning;

            UpdatePlacedZoneCellAttributes(cell, selectedZoneType, zonePrefab, zoneAttributes);

            int sortingOrder = gridManager.SetTileSortingOrder(zoneTile, selectedZoneType);
            cell.GetComponent<Cell>().sortingOrder = sortingOrder;

            addZonedTileToList(gridPosition, selectedZoneType);

            cityStats.AddZoneBuildingCount(selectedZoneType);
        }
        else
        {
            gameNotificationManager.PostError("Cannot place zone here.");
        }
    }

    public void removeZonedPositionFromList(Vector2 zonedPosition, Zone.ZoneType zoneType)
    {
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
            
            // For commercial/industrial, check normal demand
            if (!CanZoneTypeGrowBasedOnDemand(zoningType))
            {
                return;
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
        Dictionary<int, Vector2[]> availableSections = new Dictionary<int, Vector2[]>();

        for (int i = 1; i <= 3; i++)
        {
            Vector2[] section = GetRandomAvailableSizeSection(zoneType, i);

            if (section != null && section.Length > 0)
            {
                availableSections.Add(i, section);
            }
        }

        if (availableSections.Count > 0)
        {
            int randomSize = availableSections.Keys.ElementAt(UnityEngine.Random.Range(0, availableSections.Keys.Count));

            return (randomSize, availableSections[randomSize]);
        }

        return (0, null);
    }

    private Vector2[] GetRandomAvailableSizeSection(Zone.ZoneType zoneType, int buildingSize)
    {
        if (availableZoneSections.ContainsKey(zoneType) && availableZoneSections[zoneType].Count > 0)
        {
            // Find sections that fit the building size
            var possibleSections = availableZoneSections[zoneType].Where(section => buildingSize * buildingSize <= section.Count).ToList();

            if (possibleSections.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, possibleSections.Count);

                return possibleSections[randomIndex].ToArray();
            }
        }
        return null;
    }

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

    public bool IsResidentialBuilding(Zone.ZoneType zoneType)
    {
        return (zoneType == Zone.ZoneType.ResidentialLightBuilding ||
                zoneType == Zone.ZoneType.ResidentialMediumBuilding ||
                zoneType == Zone.ZoneType.ResidentialHeavyBuilding);
    }

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

    public void addZonedTileToList(Vector2 zonedPosition, Zone.ZoneType zoneType)
    {
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

    public Zone.ZoneType GetZoneTypeFromZoneTypeString(string zoneTypeString)
    {
        return (Zone.ZoneType)System.Enum.Parse(typeof(Zone.ZoneType), zoneTypeString);
    }

    void PlaceZoneBuilding(Vector2[] section, Zone.ZoneType selectedZoneType, ZoneAttributes zoneAttributes, Zone.ZoneType zoningType, int buildingSize)
    {
        GameObject prefab = GetRandomZonePrefab(selectedZoneType, buildingSize);

        if (prefab == null)
        {
            return;
        }

        foreach (Vector2 zonedPosition in section)
        {
            GameObject cell = gridManager.GetGridCell(zonedPosition);

            gridManager.DestroyCellChildren(cell, zonedPosition);

            gridManager.UpdateCellAttributes(cell.GetComponent<Cell>(), selectedZoneType, zoneAttributes, prefab, buildingSize);

            removeZonedPositionFromList(zonedPosition, zoningType);
        }

        Vector2 firstPosition = section[0];

        GameObject firstPositionGridCell = gridManager.GetGridCell(firstPosition);
        firstPositionGridCell.GetComponent<Cell>().isPivot = true;

        PlaceZoneBuildingTile(prefab, firstPositionGridCell, buildingSize);

        UpdateZonedBuildingPlacementStats(selectedZoneType, zoneAttributes);
    }

    void UpdateZonedBuildingPlacementStats(Zone.ZoneType selectedZoneType, ZoneAttributes zoneAttributes)
    {
        cityStats.HandleZoneBuildingPlacement(selectedZoneType, zoneAttributes);

        cityStats.AddPowerConsumption(zoneAttributes.PowerConsumption);
    }

    public void PlaceZoneBuildingTile(GameObject prefab, GameObject gridCell, int buildingSize = 1)
    {
        Cell cell = gridCell.GetComponent<Cell>();

        if (buildingSize > 1 && !cell.isPivot)
        {
            return;
        }

        Vector3 worldPosition = gridCell.transform.position;

        if (buildingSize > 1 && cell.zoneType != Zone.ZoneType.Building)
        {
          Vector3 offset = new Vector3(0, -(buildingSize - 1) * gridManager.tileHeight / 2, 0);
          worldPosition -= offset;
        }

        gridManager.DestroyCellChildren(gridCell, new Vector2(cell.x, cell.y));

        GameObject zoneTile = Instantiate(
          prefab,
          worldPosition,
          Quaternion.identity
        );
        zoneTile.transform.SetParent(gridCell.transform);

        cell.isPivot = true;

        int sortingOrder = gridManager.SetTileSortingOrder(zoneTile, cell.zoneType);

        cell.sortingOrder = sortingOrder;
    }

    void UpdatePlacedZoneCellAttributes(GameObject cell, Zone.ZoneType selectedZoneType, GameObject zonePrefab, ZoneAttributes zoneAttributes)
    {
        Cell cellComponent = cell.GetComponent<Cell>();
        cellComponent.zoneType = selectedZoneType;
        cellComponent.population = zoneAttributes.Population;
        cellComponent.powerConsumption = zoneAttributes.PowerConsumption;
        cellComponent.happiness = zoneAttributes.Happiness;
        cellComponent.prefab = zonePrefab;
        cellComponent.prefabName = zonePrefab.name;
        cellComponent.buildingType = null;
        cellComponent.buildingSize = 1;
        cellComponent.powerPlant = null;
        cellComponent.occupiedBuilding = null;
        cellComponent.isPivot = false;
    }

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

    public void RemoveZonedSectionFromList(Vector2[] zonedPositions, Zone.ZoneType zoneType)
    {
        if (availableZoneSections.ContainsKey(zoneType))
        {
            var sectionToRemove = availableZoneSections[zoneType].FirstOrDefault(section => section.SequenceEqual(zonedPositions));
            if (sectionToRemove != null)
            {
                availableZoneSections[zoneType].Remove(sectionToRemove);
            }
        }
    }

    public void ClearZonedPositions() {
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
    }
}