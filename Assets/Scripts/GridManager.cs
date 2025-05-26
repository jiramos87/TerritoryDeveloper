using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System;

public class GridManager : MonoBehaviour
{
    public int width, height;
    public float tileWidth = 1f; // Full width of the tile
    public float tileHeight = 0.5f; // Effective height due to isometric perspective
    public GameObject[,] gridArray;

    public ZoneManager zoneManager;
    public UIManager uiManager;

    public CityStats cityStats;
    public CursorManager cursorManager;

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

    public List<GameObject> roadTilePrefabs;
    private Vector2 startPosition;
    private bool isDrawingRoad = false;

    private bool isPlacingBuilding = false;

    private List<GameObject> previewRoadTiles = new List<GameObject>();

    private List<Vector2> previewRoadGridPositions = new List<Vector2>();
    private List<Vector2> adjacentRoadTiles = new List<Vector2>();

    private List<Vector2> zonedResidentialLightPositions = new List<Vector2>();
    private List<Vector2> zonedResidentialMediumPositions = new List<Vector2>();
    private List<Vector2> zonedResidentialHeavyPositions = new List<Vector2>();

    private List<Vector2> zonedCommercialLightPositions = new List<Vector2>();
    private List<Vector2> zonedCommercialMediumPositions = new List<Vector2>();
    private List<Vector2> zonedCommercialHeavyPositions = new List<Vector2>();

    private List<Vector2> zonedIndustrialLightPositions = new List<Vector2>();
    private List<Vector2> zonedIndustrialMediumPositions = new List<Vector2>();
    private List<Vector2> zonedIndustrialHeavyPositions = new List<Vector2>();

    public Vector2 mouseGridPosition;

    private bool isZoning = false;
    private Vector2 zoningStartGridPosition;
    private Vector2 zoningEndGridPosition;
    private List<GameObject> previewZoningTiles = new List<GameObject>();
    private Dictionary<Zone.ZoneType, List<List<Vector2>>> availableZoneSections =
      new Dictionary<Zone.ZoneType, List<List<Vector2>>>(); // Dictionary to store available zone sections
    
    public TerrainManager terrainManager;

    [Header("Demand System")]
    public DemandManager demandManager;

    public WaterManager waterManager;

    public GameNotificationManager GameNotificationManager;
    public ForestManager forestManager;

    public CameraController cameraController;

    // void Start()
    // {
    //     roadTilePrefabs = new List<GameObject>
    //     {
    //         roadTilePrefab1,
    //         roadTilePrefab2,
    //         roadTilePrefabCrossing,
    //         roadTilePrefabTIntersectionUp,
    //         roadTilePrefabTIntersectionDown,
    //         roadTilePrefabTIntersectionLeft,
    //         roadTilePrefabTIntersectionRight,
    //         roadTilePrefabElbowUpLeft,
    //         roadTilePrefabElbowUpRight,
    //         roadTilePrefabElbowDownLeft,
    //         roadTilePrefabElbowDownRight
    //     };

    //     if (demandManager == null)
    //     {
    //         demandManager = FindObjectOfType<DemandManager>();
    //     }

    //     if (GameNotificationManager == null)
    //     {
    //         GameNotificationManager = FindObjectOfType<GameNotificationManager>();
    //     }

    //     if (forestManager == null)
    //     {
    //         forestManager = FindObjectOfType<ForestManager>();
    //     }

    //     CreateGrid();
    // }

    public void InitializeGrid()
    {
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
            roadTilePrefabElbowDownRight
        };

        if (demandManager == null)
        {
            demandManager = FindObjectOfType<DemandManager>();
        }

        if (GameNotificationManager == null)
        {
            GameNotificationManager = FindObjectOfType<GameNotificationManager>();
        }

        if (forestManager == null)
        {
            forestManager = FindObjectOfType<ForestManager>();
        }

        CreateGrid();

        cameraController.MoveCameraToMapCenter();
    }

    void CreateGrid()
    {
        gridArray = new GameObject[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject gridCell = new GameObject($"Cell_{x}_{y}");
                        
                // Calculate the isometric position with height consideration
                float posX = (x - y) * (tileWidth / 2);
                float posY = (x + y) * (tileHeight / 2);
                
                // The cell's position will be updated by TerrainManager when heights are applied
                gridCell.transform.position = new Vector3(posX, posY, 0);
                gridCell.transform.SetParent(transform);

                Cell cellComponent = gridCell.AddComponent<Cell>();
                cellComponent.x = x;
                cellComponent.y = y;
                cellComponent.zoneType = Zone.ZoneType.Grass;
                cellComponent.population = 0;
                cellComponent.powerConsumption = 0;
                cellComponent.powerOutput = 0;
                cellComponent.waterConsumption = 0;
                cellComponent.happiness = 0;
                cellComponent.buildingType = null;
                cellComponent.buildingSize = 1;
                cellComponent.powerPlant = null;
                cellComponent.height = 1; // Set initial height
                cellComponent.waterPlant = null;
                cellComponent.occupiedBuilding = null;
                cellComponent.isPivot = false;
                cellComponent.sortingOrder = 0;
                cellComponent.desirability = 0;
                GameObject tilePrefab = zoneManager.GetRandomZonePrefab(Zone.ZoneType.Grass, 1);

                cellComponent.prefab = tilePrefab;
                cellComponent.prefabName = tilePrefab.name;
                cellComponent.isPivot = false;

                gridArray[x, y] = gridCell;

                // Instantiate a random zone tile
                GameObject zoneTile = Instantiate(
                    tilePrefab,
                    gridCell.transform.position,
                    Quaternion.identity
                );
                zoneTile.transform.SetParent(gridCell.transform);

                int sortingOrder = SetTileSortingOrder(zoneTile, Zone.ZoneType.Grass);
                cellComponent.sortingOrder = sortingOrder;

                // Ensure the zoneTile has a PolygonCollider2D
                PolygonCollider2D polygonCollider = zoneTile.GetComponent<PolygonCollider2D>();
                if (polygonCollider == null)
                {
                    polygonCollider = zoneTile.AddComponent<PolygonCollider2D>();
                }

                // Ensure the zoneTile has a Zone component
                Zone zoneComponent = zoneTile.GetComponent<Zone>();
                if (zoneComponent == null)
                {
                    zoneComponent = zoneTile.AddComponent<Zone>();
                    zoneComponent.zoneType = Zone.ZoneType.Grass;
                }
            }
        }
    }

    ZoneAttributes GetZoneAttributes(Zone.ZoneType zoneType)
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

    void Update()
    {
        try
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Vector2 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseGridPosition = GetGridPosition(worldPoint);

            if (mouseGridPosition.x < 0 || mouseGridPosition.x >= width || mouseGridPosition.y < 0 || mouseGridPosition.y >= height)
            {
                return;
            }

            if (uiManager.isBulldozeMode())
            {
                HandleBulldozerMode(mouseGridPosition);
            }

            if (uiManager.IsDetailsMode() || Input.GetKey(KeyCode.LeftShift))
            {
                HandleShowTileDetails(mouseGridPosition);
            }

            if (IsInRoadDrawingMode() || IsInWaterPlacementMode())
            {
                HandleRaycast(mouseGridPosition);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Update error: " + ex);
        }
    }

    bool IsInWaterPlacementMode()
    {
        return uiManager.GetSelectedZoneType() == Zone.ZoneType.Water;
    }

    void HandleBulldozerMode(Vector2 gridPosition)
    {
        if (Input.GetMouseButtonDown(0))
        {
            HandleBulldozerClick(gridPosition);
        }
    }

    void HandleBulldozerClick(Vector2 gridPosition)
    {
        GameObject cell = gridArray[(int)gridPosition.x, (int)gridPosition.y];
        Zone.ZoneType zoneType = cell.GetComponent<Cell>().zoneType;

        HandleBulldozeTile(zoneType, cell);
    }

    void RestoreCellAttributes(Cell cellComponent)
    {
        cellComponent.buildingType = null;
        cellComponent.powerPlant = null;
        cellComponent.population = 0;
        cellComponent.powerConsumption = 0;
        cellComponent.happiness = 0;
        cellComponent.zoneType = Zone.ZoneType.Grass;
        cellComponent.buildingSize = 1;
        cellComponent.prefab = zoneManager.GetRandomZonePrefab(Zone.ZoneType.Grass);
        cellComponent.prefabName = cellComponent.prefab.name;
        cellComponent.isPivot = false;
    }

    void RestoreTile(GameObject cell)
    {
        GameObject zoneTile = Instantiate(
            zoneManager.GetRandomZonePrefab(Zone.ZoneType.Grass),
            cell.transform.position,
            Quaternion.identity
        );

        zoneTile.transform.SetParent(cell.transform);

        int sortingOrder = SetTileSortingOrder(zoneTile, Zone.ZoneType.Grass);
        cell.GetComponent<Cell>().sortingOrder = sortingOrder;
    }

    void BulldozeTile(GameObject cell)
    {
        Cell cellComponent = cell.GetComponent<Cell>();
        
        RestoreCellAttributes(cellComponent);
        
        DestroyCellChildren(cell, new Vector2(cellComponent.x, cellComponent.y));
        
        RestoreTile(cell);
        
        // Show the bulldoze animation
        if (uiManager != null)
        {
            uiManager.ShowDemolitionAnimation(cell);
        }
    }

    void BulldozeBuildingTiles(GameObject cell)
    {
        Cell cellComponent = cell.GetComponent<Cell>();

        int buildingSize = cellComponent.buildingSize;

        // Show animation before demolishing for better visual effect
        if (uiManager != null)
        {
            uiManager.ShowDemolitionAnimationCentered(cell, buildingSize);
        }

        if (buildingSize > 1)
        {
            for (int x = 0; x < buildingSize; x++)
            {
                for (int y = 0; y < buildingSize; y++)
                {
                    int gridX = (int)cellComponent.x + x - buildingSize / 2;
                    int gridY = (int)cellComponent.y + y - buildingSize / 2;

                    GameObject adjacentCell = gridArray[gridX, gridY];

                    BulldozeTileWithoutAnimation(adjacentCell); // Don't show animation for each tile
                }
            }
        }
        else
        {
            BulldozeTileWithoutAnimation(cell);
        }
    }

    // Create a version without animation for multi-tile cleanup
    void BulldozeTileWithoutAnimation(GameObject cell)
    {
        Cell cellComponent = cell.GetComponent<Cell>();
        
        RestoreCellAttributes(cellComponent);
        DestroyCellChildren(cell, new Vector2(cellComponent.x, cellComponent.y));
        RestoreTile(cell);
    }

    void HandleBuildingStatsReset(Cell cellComponent, Zone.ZoneType zoneType)
    {
        string buildingType = cellComponent.GetBuildingType();

        if (buildingType == "PowerPlant")
        {
            PowerPlant powerPlant = cellComponent.powerPlant;
            cityStats.UnregisterPowerPlant(powerPlant);
        }
        
        if (buildingType == "WaterPlant")
        {
            WaterPlant waterPlant = cellComponent.waterPlant;
            if (waterManager != null && waterPlant != null)
            {
                waterManager.UnregisterWaterPlant(waterPlant);
            }
        }

        if (buildingType == null && zoneType != Zone.ZoneType.Grass)
        {
            cityStats.HandleBuildingDemolition(zoneType, GetZoneAttributes(zoneType));
        }
    }

    void HandleBulldozeTile(Zone.ZoneType zoneType, GameObject cell)
    {
        Cell cellComponent = cell.GetComponent<Cell>();

        HandleBuildingStatsReset(cellComponent, zoneType);

        BulldozeBuildingTiles(cell);
    }

    void HandleShowTileDetails(Vector2 gridPosition)
    {
        if (Input.GetMouseButtonDown(0))
        {
            GameObject cell = gridArray[(int)gridPosition.x, (int)gridPosition.y];
            Cell cellComponent = cell.GetComponent<Cell>();

            uiManager.ShowTileDetails(cellComponent);
        }
    }

    bool IsInRoadDrawingMode()
    {
        return uiManager.GetSelectedZoneType() == Zone.ZoneType.Road ||
          uiManager.GetSelectedZoneType() == Zone.ZoneType.ResidentialLightZoning ||
          uiManager.GetSelectedZoneType() == Zone.ZoneType.ResidentialMediumZoning ||
          uiManager.GetSelectedZoneType() == Zone.ZoneType.ResidentialHeavyZoning ||
          uiManager.GetSelectedZoneType() == Zone.ZoneType.ResidentialLightBuilding ||
          uiManager.GetSelectedZoneType() == Zone.ZoneType.ResidentialMediumBuilding ||
          uiManager.GetSelectedZoneType() == Zone.ZoneType.ResidentialHeavyBuilding ||
          uiManager.GetSelectedZoneType() == Zone.ZoneType.CommercialLightZoning ||
          uiManager.GetSelectedZoneType() == Zone.ZoneType.CommercialMediumZoning ||
          uiManager.GetSelectedZoneType() == Zone.ZoneType.CommercialHeavyZoning ||
          uiManager.GetSelectedZoneType() == Zone.ZoneType.CommercialLightBuilding ||
          uiManager.GetSelectedZoneType() == Zone.ZoneType.CommercialMediumBuilding ||
          uiManager.GetSelectedZoneType() == Zone.ZoneType.CommercialHeavyBuilding ||
          uiManager.GetSelectedZoneType() == Zone.ZoneType.IndustrialLightZoning ||
          uiManager.GetSelectedZoneType() == Zone.ZoneType.IndustrialMediumZoning ||
          uiManager.GetSelectedZoneType() == Zone.ZoneType.IndustrialHeavyZoning ||
          uiManager.GetSelectedZoneType() == Zone.ZoneType.IndustrialLightBuilding ||
          uiManager.GetSelectedZoneType() == Zone.ZoneType.IndustrialMediumBuilding ||
          uiManager.GetSelectedZoneType() == Zone.ZoneType.IndustrialHeavyBuilding ||
          uiManager.GetSelectedZoneType() == Zone.ZoneType.Grass ||
          uiManager.GetSelectedBuilding() != null;
    }

    void HandleRaycast(Vector2 gridPosition)
    {
        GameObject cell = gridArray[(int)gridPosition.x, (int)gridPosition.y];

        Zone.ZoneType selectedZoneType = uiManager.GetSelectedZoneType();
        IBuilding selectedBuilding = uiManager.GetSelectedBuilding();
        IForest selectedForest = uiManager.GetSelectedForest();

        if (selectedZoneType == Zone.ZoneType.Road)
        {
            HandleRoadDrawing(gridPosition);
        }
        else if (selectedZoneType == Zone.ZoneType.Water)
        {
            HandleWaterPlacement(gridPosition);
        }
        else if (selectedBuilding != null)
        {
            HandleBuildingPlacement(gridPosition, selectedBuilding);
        }
        else if (selectedForest != null)
        {
            HandleForestPlacement(gridPosition, selectedForest);
        }
        else if (isInZoningMode())
        {
            HandleZoning(mouseGridPosition);
        }
    }

    void HandleWaterPlacement(Vector2 gridPosition)
    {
        if (Input.GetMouseButton(0))
        {
            // Check if player can afford water placement
            if (!cityStats.CanAfford(ZoneAttributes.Water.ConstructionCost))
            {
                uiManager.ShowInsufficientFundsTooltip("Water", ZoneAttributes.Water.ConstructionCost);
                return;
            }
            
            if (waterManager != null)
            {
                waterManager.PlaceWater((int)gridPosition.x, (int)gridPosition.y);
                
                // Deduct cost for water placement
                cityStats.RemoveMoney(ZoneAttributes.Water.ConstructionCost);
            }
        }
    }

    private bool isInZoningMode()
    {
        return uiManager.GetSelectedZoneType() != Zone.ZoneType.Grass &&
          uiManager.GetSelectedZoneType() != Zone.ZoneType.Road &&
          uiManager.GetSelectedZoneType() != Zone.ZoneType.None;
    }

    void HandleZoning(Vector2 gridPosition)
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
                    Vector2 worldPos = GetWorldPosition(x, y);

                    GameObject zoningPrefab = zoneManager.GetRandomZonePrefab(uiManager.GetSelectedZoneType());

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

    void ClearPreviewTiles()
    {
        foreach (var tile in previewZoningTiles)
        {
            Destroy(tile);
        }
        previewZoningTiles.Clear();
    }

    void HandleBuildingPlacement(Vector3 gridPosition, IBuilding selectedBuilding)
    {
        if (Input.GetMouseButtonDown(0))
        {
            PlaceBuilding(gridPosition, selectedBuilding);
        }
    }

    void HandleForestPlacement(Vector3 gridPosition, IForest selectedForest)
    {
        if (Input.GetMouseButtonDown(0))
        {
            forestManager.PlaceForest(gridPosition, selectedForest);
        }
    }
        

    void HandleTilePlacement(Vector3 gridPosition)
    {
        if (Input.GetMouseButtonDown(0))
        {
            PlaceZone(gridPosition);
        }
    }

    bool canPlaceZone(ZoneAttributes zoneAttributes, Vector3 gridPosition)
    {
        if (zoneAttributes == null)
            return false;
            
        if (!cityStats.CanAfford(zoneAttributes.ConstructionCost))
            return false;
            
        if (!canPlaceBuilding(gridPosition, 1))
            return false;
            
        // Manual zone placement is always allowed - the restrictions are on building spawning
        return true;
    }

    public string GetDemandFeedback(Zone.ZoneType zoneType)
    {
        if (demandManager == null)
            return "";
            
        float demandLevel = demandManager.GetDemandLevel(zoneType);
        bool canGrow = demandManager.CanZoneTypeGrow(zoneType);
        
        // Check if it's a residential building type
        Zone.ZoneType buildingType = GetBuildingZoneType(zoneType);
        bool isResidential = IsResidentialBuilding(buildingType);
        bool hasJobsAvailable = !isResidential || demandManager.CanPlaceResidentialBuilding();
        
        // Check if it's a commercial/industrial building type
        bool needsResidential = IsCommercialOrIndustrialBuilding(buildingType);
        bool hasResidentialSupport = !needsResidential || demandManager.CanPlaceCommercialOrIndustrialBuilding(buildingType);
        
        string feedback = "";
        
        if (canGrow && hasJobsAvailable && hasResidentialSupport)
        {
            feedback = $"✓ Demand: {demandLevel:F0}%";
        }
        else if (!canGrow)
        {
            feedback = $"✗ Low Demand: {demandLevel:F0}%";
        }
        else if (!hasJobsAvailable)
        {
            feedback = $"✗ No Jobs Available (Demand: {demandLevel:F0}%)";
        }
        else if (!hasResidentialSupport)
        {
            feedback = $"✗ Need Residents First (Demand: {demandLevel:F0}%)";
        }
        
        return feedback;
    }

    private bool IsCommercialOrIndustrialBuilding(Zone.ZoneType zoneType)
    {
        return (zoneType == Zone.ZoneType.CommercialLightBuilding ||
                zoneType == Zone.ZoneType.CommercialMediumBuilding ||
                zoneType == Zone.ZoneType.CommercialHeavyBuilding ||
                zoneType == Zone.ZoneType.IndustrialLightBuilding ||
                zoneType == Zone.ZoneType.IndustrialMediumBuilding ||
                zoneType == Zone.ZoneType.IndustrialHeavyBuilding);
    }

    void DestroyCellChildren(GameObject cell, Vector2 gridPosition)
    {

        if (cell.transform.childCount > 0)
        {
            foreach (Transform child in cell.transform)
            {
                Zone zone = child.GetComponent<Zone>();

                if (zone && zone.zoneCategory == Zone.ZoneCategory.Zoning)
                {
                    removeZonedPositionFromList(gridPosition, zone.zoneType);
                }

                DestroyImmediate(child.gameObject);
            }
        }
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

    void PlaceZone(Vector3 gridPosition)
    {
        Vector2 worldPosition = GetWorldPosition((int)gridPosition.x, (int)gridPosition.y);
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
            GameObject cell = gridArray[(int)gridPosition.x, (int)gridPosition.y];

            DestroyCellChildren(cell, gridPosition);

            GameObject zonePrefab = zoneManager.GetRandomZonePrefab(selectedZoneType);
            
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

            int sortingOrder = SetTileSortingOrder(zoneTile, selectedZoneType);
            cell.GetComponent<Cell>().sortingOrder = sortingOrder;

            addZonedTileToList(gridPosition, selectedZoneType);

            cityStats.AddZoneBuildingCount(selectedZoneType);
        }
        else
        {
            GameNotificationManager.PostError("Cannot place zone here.");
        }
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

    public bool IsResidentialBuilding(Zone.ZoneType zoneType)
    {
        return (zoneType == Zone.ZoneType.ResidentialLightBuilding ||
                zoneType == Zone.ZoneType.ResidentialMediumBuilding ||
                zoneType == Zone.ZoneType.ResidentialHeavyBuilding);
    }

     private bool CanZoneTypeGrowBasedOnDemand(Zone.ZoneType zoningType)
    {
        if (demandManager == null)
        {
            return true; // If no demand manager, allow all growth
        }
        
        return demandManager.CanZoneTypeGrow(zoningType);
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

    private Zone.ZoneType GetBuildingZoneType(Zone.ZoneType zoningType)
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

    void UpdateCellAttributes(Cell cellComponent, Zone.ZoneType selectedZoneType, ZoneAttributes zoneAttributes, GameObject prefab, int buildingSize)
    {
        cellComponent.zoneType = selectedZoneType;
        cellComponent.population = zoneAttributes.Population;
        cellComponent.powerConsumption = zoneAttributes.PowerConsumption;
        cellComponent.happiness = zoneAttributes.Happiness;
        cellComponent.prefab = prefab;
        cellComponent.prefabName = prefab.name;
        cellComponent.buildingType = prefab.name;
        cellComponent.buildingSize = buildingSize;
        cellComponent.isPivot = false;
    }

    void PlaceZoneBuildingTile(GameObject prefab, GameObject gridCell, int buildingSize = 1)
    {
        Cell cell = gridCell.GetComponent<Cell>();

        if (buildingSize > 1 && !cell.isPivot)
        {
            return;
        }

        Vector3 worldPosition = gridCell.transform.position;

        if (buildingSize > 1 && cell.zoneType != Zone.ZoneType.Building)
        {
          Vector3 offset = new Vector3(0, -(buildingSize - 1) * tileHeight / 2, 0);
          worldPosition -= offset;
        }

        DestroyCellChildren(gridCell, new Vector2(cell.x, cell.y));

        GameObject zoneTile = Instantiate(
          prefab,
          worldPosition,
          Quaternion.identity
        );
        zoneTile.transform.SetParent(gridCell.transform);

        cell.isPivot = true;

        int sortingOrder = SetTileSortingOrder(zoneTile, cell.zoneType);

        cell.sortingOrder = sortingOrder;
    }

    void UpdateZonedBuildingPlacementStats(Zone.ZoneType selectedZoneType, ZoneAttributes zoneAttributes)
    {
        cityStats.HandleZoneBuildingPlacement(selectedZoneType, zoneAttributes);

        cityStats.AddPowerConsumption(zoneAttributes.PowerConsumption);
    }

    void PlaceZoneBuilding(Vector2[] section, Zone.ZoneType selectedZoneType, ZoneAttributes zoneAttributes, Zone.ZoneType zoningType, int buildingSize)
    {
        GameObject prefab = zoneManager.GetRandomZonePrefab(selectedZoneType, buildingSize);

        if (prefab == null)
        {
            return;
        }

        foreach (Vector2 zonedPosition in section)
        {
            GameObject cell = gridArray[(int)zonedPosition.x, (int)zonedPosition.y];

            DestroyCellChildren(cell, zonedPosition);

            UpdateCellAttributes(cell.GetComponent<Cell>(), selectedZoneType, zoneAttributes, prefab, buildingSize);

            removeZonedPositionFromList(zonedPosition, zoningType);
        }

        Vector2 firstPosition = section[0];
        gridArray[(int)firstPosition.x, (int)firstPosition.y].GetComponent<Cell>().isPivot = true;

        PlaceZoneBuildingTile(prefab, gridArray[(int)firstPosition.x, (int)firstPosition.y], buildingSize);

        UpdateZonedBuildingPlacementStats(selectedZoneType, zoneAttributes);
    }

    private Vector2 FindCenterPosition(Vector2[] section)
    {
        float x = 0;
        float y = 0;

        foreach (Vector2 position in section)
        {
            x += position.x;
            y += position.y;
        }

        x /= section.Length;
        y /= section.Length;

        return new Vector2(x, y);
    }

    void removeZonedPositionFromList(Vector2 zonedPosition, Zone.ZoneType zoneType)
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

    void RemoveZonedSectionFromList(Vector2[] zonedPositions, Zone.ZoneType zoneType)
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

    public int SetTileSortingOrder(GameObject tile, Zone.ZoneType zoneType = Zone.ZoneType.Grass)
    {
        Vector3 gridPos = GetGridPosition(tile.transform.position);

        int x = (int)gridPos.x;
        int y = (int)gridPos.y;
        Cell cell = gridArray[x, y].GetComponent<Cell>();
        
        SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
        var sortingOrder = 0;
        
        switch (zoneType)
        {
            case Zone.ZoneType.Grass:
                sortingOrder = -1001 - (cell.height * 100); // Adjust based on height
                sr.sortingOrder = sortingOrder;
                return sortingOrder;
            default:
                sortingOrder = -(y * 10 + x) - 100 - (cell.height * 100); // Adjust based on height
                sr.sortingOrder = sortingOrder;
                return sortingOrder;
        }
    }


    void HandleRoadDrawing(Vector2 gridPosition)
    {
        if (!terrainManager.CanPlaceRoad((int)gridPosition.x, (int)gridPosition.y))
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            isDrawingRoad = true;
            startPosition = gridPosition;
        }
        else if (isDrawingRoad && Input.GetMouseButton(0))
        {
            Vector3 currentMousePosition = gridPosition;
            DrawPreviewLine(startPosition, currentMousePosition);
        }
        
        if (Input.GetMouseButtonUp(0) && isDrawingRoad)
        {
            isDrawingRoad = false;
            DrawRoadLine(true);
            ClearPreview(true);
        }

        if (Input.GetMouseButtonDown(1))
        {
            isDrawingRoad = false;
            ClearPreview();
        }
    }

    public Vector2 GetGridPosition(Vector2 worldPoint)
    {
        float posX = worldPoint.x / (tileWidth / 2);
        float posY = worldPoint.y / (tileHeight / 2);

        int gridX = Mathf.RoundToInt((posY + posX) / 2);
        int gridY = Mathf.RoundToInt((posY - posX) / 2);

        return new Vector2(gridX, gridY);
    }

    public Vector2 GetWorldPosition(int gridX, int gridY)
    {
        Cell cell = gridArray[gridX, gridY].GetComponent<Cell>();
        float heightOffset = (cell.height - 1) * (tileHeight / 2); 
        
        float posX = (gridX - gridY) * (tileWidth / 2);
        float posY = (gridX + gridY) * (tileHeight / 2) + heightOffset;

        return new Vector2(posX, posY);
    }

    void DrawPreviewLine(Vector2 start, Vector2 end)
    {
        ClearPreview();
        List<Vector2> path = GetLine(start, end);

        for (int i = 0; i < path.Count; i++)
        {
            Vector2 gridPos = path[i];

            DrawPreviewRoadTile(gridPos, path, i, true);
        }
    }

    Vector2[] GetRoadColliderPoints()
    {
        Vector2[] points = new Vector2[4];
        points[0] = new Vector2(-0.5f, 0f);
        points[1] = new Vector2(0f, 0.25f);
        points[2] = new Vector2(0.5f, 0f);
        points[3] = new Vector2(0f, -0.25f);

        return points;
    }

    void SetPreviewTileCollider(GameObject previewTile)
    {
        PolygonCollider2D collider = previewTile.AddComponent<PolygonCollider2D>();
        collider.points = GetRoadColliderPoints();
        collider.isTrigger = true;
    }

    void SetRoadTileZoneDetails(GameObject roadTile)
    {
        Zone zone = roadTile.AddComponent<Zone>();
        zone.zoneType = Zone.ZoneType.Road;
    }

    void SetPreviewRoadTileDetails(GameObject previewTile)
    {
      SetPreviewTileCollider(previewTile);
      int sortingOrder = SetTileSortingOrder(previewTile, Zone.ZoneType.Road);

      SetRoadTileZoneDetails(previewTile);
      previewTile.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0.5f);
    }

    void DrawPreviewRoadTile(Vector2 gridPos, List<Vector2> path, int i, bool isCenterRoadTile = true)
    {
      Vector2 prevGridPos = i == 0 ? (path.Count > 1 ? path[1] : gridPos) : path[i - 1];

      bool isPreview = true;

      GameObject roadPrefab = GetCorrectRoadPrefab(prevGridPos, gridPos, isCenterRoadTile, isPreview);

      Vector2 worldPos = GetWorldPosition((int)gridPos.x, (int)gridPos.y);

      GameObject previewTile = Instantiate(
          roadPrefab,
          worldPos,
          Quaternion.identity
      );

      SetPreviewRoadTileDetails(previewTile);
      
      previewRoadTiles.Add(previewTile);

      previewRoadGridPositions.Add(new Vector2(gridPos.x, gridPos.y));

      GameObject cell = gridArray[(int)gridPos.x, (int)gridPos.y];

      previewTile.transform.SetParent(cell.transform);
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
        foreach (Vector2 adjacentRoadTile in adjacentRoadTiles)
        {
            bool isAdjacent = true;

            if (!isAdjacentRoadInPreview(adjacentRoadTile))
            {
                PlaceRoadTile(adjacentRoadTile, i, isAdjacent);
            }
        }
    }

    void ClearPreview(bool isEnd = false)
    {
        foreach (GameObject previewTile in previewRoadTiles)
        {
            Destroy(previewTile);
        }
        previewRoadTiles.Clear();
        previewRoadGridPositions.Clear();
    }

    void DestroyPreviousZoning(GameObject cell)
    {
        if (cell.transform.childCount > 0)
        {
            foreach (Transform child in cell.transform)
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    void DestroyPreviousRoadTile(GameObject cell, Vector2 gridPos)
    {
        if (cell.transform.childCount > 0)
        {
            if (cell.GetComponent<Cell>().zoneType == Zone.ZoneType.Road)
            {
                DestroyImmediate(cell.transform.GetChild(0).gameObject);
            }

            foreach (Transform child in cell.transform)
            {
                Zone zone = child.GetComponent<Zone>();
                if (zone != null)
                {
                    DestroyImmediate(child.gameObject);
                    if (zone.zoneCategory == Zone.ZoneCategory.Zoning)
                    {
                        removeZonedPositionFromList(gridPos, zone.zoneType);
                    }
                }
            }
        }
    }

    void UpdateRoadCellAttributes(GameObject cell, GameObject roadTile, Zone.ZoneType zoneType)
    {
        Cell cellComponent = cell.GetComponent<Cell>();
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

    void PlaceRoadTile(Vector2 gridPos, int i = 0, bool isAdjacent = false)
    {
        GameObject cell = gridArray[(int)gridPos.x, (int)gridPos.y];
        
        bool isCenterRoadTile = !isAdjacent;
        bool isPreview = false;

        Vector2 prevGridPos = isAdjacent
            ? (i == 0 ? gridPos : previewRoadGridPositions[i - 1])
            : new Vector2(0, 0);

        GameObject correctRoadPrefab = GetCorrectRoadPrefab(
            prevGridPos,
            gridPos,
            isCenterRoadTile,
            isPreview
        );

        DestroyPreviousRoadTile(cell, gridPos);

        GameObject roadTile = Instantiate(
            correctRoadPrefab,
            GetWorldPosition((int)gridPos.x, (int)gridPos.y),
            Quaternion.identity
        );

        roadTile.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1);

        Zone.ZoneType zoneType = Zone.ZoneType.Road;

        Zone zone = roadTile.AddComponent<Zone>();
        zone.zoneType = zoneType;

        UpdateRoadCellAttributes(cell, roadTile, zoneType);

        int sortingOrder = SetTileSortingOrder(roadTile, zoneType);
        cell.GetComponent<Cell>().sortingOrder = sortingOrder;

        roadTile.transform.SetParent(cell.transform);
    }

    void DrawRoadLine(bool calculateCost = true)
    {
        if (calculateCost)
        {
            int totalCost = CalculateTotalCost(previewRoadGridPositions.Count);
            
            // Check if player can afford the road
            if (!cityStats.CanAfford(totalCost))
            {
                uiManager.ShowInsufficientFundsTooltip("Road", totalCost);
                ClearPreview();
                isDrawingRoad = false;
                return;
            }
            
            // Deduct the cost if we can afford it
            cityStats.RemoveMoney(totalCost);
        }
        
        for (int i = 0; i < previewRoadGridPositions.Count; i++)
        {
            Vector2 gridPos = previewRoadGridPositions[i];

            PlaceRoadTile(gridPos, i, false);

            UpdateAdjacentRoadPrefabs(gridPos, i);
        }

        if (calculateCost)
        {
            int roadPowerConsumption = previewRoadGridPositions.Count * ZoneAttributes.Road.PowerConsumption;
            cityStats.AddPowerConsumption(roadPowerConsumption);
        }
    }

    GameObject GetCorrectRoadPrefab(Vector2 prevGridPos, Vector2 currGridPos, bool isCenterRoadTile = true, bool isPreview = false)
    {
        Vector2 direction = currGridPos - prevGridPos;
        if (isPreview)
        {
          
          if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
          {
              return roadTilePrefab2;
          }
          else
          {
              return roadTilePrefab1;
          }
        }

        bool hasLeft = IsRoadAt(currGridPos + new Vector2(-1, 0));
        bool hasRight = IsRoadAt(currGridPos + new Vector2(1, 0));
        bool hasUp = IsRoadAt(currGridPos + new Vector2(0, 1));
        bool hasDown = IsRoadAt(currGridPos + new Vector2(0, -1));

        if (isCenterRoadTile) {
          UpdateAdjacentRoadTilesArray(currGridPos, hasLeft, hasRight, hasUp, hasDown, isPreview);
        }

        if (hasLeft && hasRight && hasUp && hasDown)
        {
            return roadTilePrefabCrossing;
        }
        else if (hasLeft && hasRight && hasUp && !hasDown)
        {
            return roadTilePrefabTIntersectionDown;
        }
        else if (hasLeft && hasRight && hasDown && !hasUp)
        {
            return roadTilePrefabTIntersectionUp;
        }
        else if (hasUp && hasDown && hasLeft && !hasRight)
        {
            return roadTilePrefabTIntersectionRight;
        }
        else if (hasUp && hasDown && hasRight && !hasLeft)
        {
            return roadTilePrefabTIntersectionLeft;
        }
        else if (hasLeft && hasUp && !hasRight && !hasDown)
        {
            return roadTilePrefabElbowDownRight;
        }
        else if (hasRight && hasUp && !hasLeft && !hasDown)
        {
            return roadTilePrefabElbowDownLeft;
        }
        else if (hasLeft && hasDown && !hasRight && !hasUp)
        {
            return roadTilePrefabElbowUpRight;
        }
        else if (hasRight && hasDown && !hasLeft && !hasUp)
        {
            return roadTilePrefabElbowUpLeft;
        }
        else if (hasLeft || hasRight)
        {
          return roadTilePrefab2;
        }

        else if (hasUp || hasDown)
        {
          return roadTilePrefab1;
        }

        // If no intersection or elbow, fall back to horizontal/vertical

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            return roadTilePrefab2;
        }
        else
        {
            return roadTilePrefab1;
        }
    }

    void UpdateAdjacentRoadTilesArray(Vector2 currGridPos, bool hasLeft, bool hasRight, bool hasUp, bool hasDown, bool isPreview)
    {
        adjacentRoadTiles.Clear();

        if (hasLeft)
        {
            adjacentRoadTiles.Add(new Vector2(currGridPos.x - 1, currGridPos.y));
        }
        if (hasRight)
        {
            adjacentRoadTiles.Add(new Vector2(currGridPos.x + 1, currGridPos.y));
        }
        if (hasUp)
        {
            adjacentRoadTiles.Add(new Vector2(currGridPos.x, currGridPos.y + 1));
        }
        if (hasDown)
        {
            adjacentRoadTiles.Add(new Vector2(currGridPos.x, currGridPos.y - 1));
        }
    }

    bool IsAnyChildRoad(int gridX, int gridY)
    {
        var cell = gridArray[gridX, gridY];
        if (cell == null || cell.transform.childCount == 0) return false;
        
        var cellComponent = cell.GetComponent<Cell>();
        if (cellComponent?.zoneType == Zone.ZoneType.Road) return true;
        
        return cell.transform
            .Cast<Transform>()
            .Select(child => child.GetComponent<Zone>())
            .Any(zone => zone != null && zone.zoneType == Zone.ZoneType.Road);
    }

    bool IsRoadAt(Vector2 gridPos)
    {
        bool isRoad = false;
        int gridX = Mathf.RoundToInt(gridPos.x);
        int gridY = Mathf.RoundToInt(gridPos.y);

        if (gridX >= 0 && gridX < width && gridY >= 0 && gridY < height)
        {
            isRoad = IsAnyChildRoad(gridX, gridY);

            return isRoad;
        }

        return false;
    }

    bool IsWithinGrid(Vector2 position)
    {
        foreach (GameObject cell in gridArray)
        {
            Vector3 position3D = new Vector3(position.x, position.y, 0);
            if (cell.transform.position == position3D)
            {
                return true;
            }
        }
        return false;
    }

    int CalculateTotalCost(int tilesCount)
    {
        return tilesCount * 50;
    }

    List<Vector2> GetLine(Vector2 start, Vector2 end)
    {
        List<Vector2> line = new List<Vector2>();

        int x0 = (int)start.x;
        int y0 = (int)start.y;
        int x1 = (int)end.x;
        int y1 = (int)end.y;

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
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }

        return line;
    }

    bool canPlaceBuilding(Vector2 gridPosition, int buildingSize)
    {
        if (buildingSize == 0)
            return false;
        
        int offsetX = 0;
        int offsetY = 0;
        
        if (buildingSize % 2 == 0)
        {
            offsetX = 0;
            offsetY = 0;
        }
        else
        {
            offsetX = buildingSize / 2;
            offsetY = buildingSize / 2;
        }
        

        if (!terrainManager.CanPlaceBuilding((int)gridPosition.x, (int)gridPosition.y, buildingSize))
        {
            return false;
        }

        for (int x = 0; x < buildingSize; x++)
        {
            for (int y = 0; y < buildingSize; y++)
            {
                int gridX = (int)gridPosition.x + x - offsetX;
                int gridY = (int)gridPosition.y + y - offsetY;
                
                if (gridX < 0 || gridX >= width || gridY < 0 || gridY >= height)
                {
                    return false;
                }

                Cell cell = gridArray[gridX, gridY].GetComponent<Cell>();

                if (cell.zoneType != Zone.ZoneType.Grass)
                {
                    return false;
                }
            }
        }
        
        if (uiManager.GetSelectedBuilding() != null && 
            uiManager.GetSelectedBuilding() is WaterPlant)
        {
            bool adjacentToWater = false;
            
            for (int x = 0; x < buildingSize; x++)
            {
                for (int y = 0; y < buildingSize; y++)
                {
                    int gridX = (int)gridPosition.x + x - offsetX;
                    int gridY = (int)gridPosition.y + y - offsetY;
                    
                    if (waterManager != null && waterManager.IsAdjacentToWater(gridX, gridY))
                    {
                        adjacentToWater = true;
                        break;
                    }
                }
                if (adjacentToWater) break;
            }
            
            if (!adjacentToWater)
            {
                GameNotificationManager.Instance.PostBuildingPlacementError("Water plant must be adjacent to water");
                return false;
            }
        }
        
        return true;
    }

    void UpdatePlacedBuildingCellAttributes(Cell cell, int buildingSize, PowerPlant powerPlant, WaterPlant waterPlant, GameObject buildingPrefab, Zone.ZoneType zoneType = Zone.ZoneType.Building, GameObject building = null)
    {
        cell.occupiedBuilding = building;
        cell.buildingSize = buildingSize;
        cell.prefab = buildingPrefab;
        cell.prefabName = buildingPrefab.name;
        cell.zoneType = zoneType;

        if (powerPlant != null)
        {
            cell.buildingType = "PowerPlant";
            cell.powerPlant = powerPlant;
        }
        
        if (waterPlant != null)
        {
            cell.buildingType = "WaterPlant";
            cell.waterPlant = waterPlant;
        }
    }

    void SetCellAsBuildingPivot(Cell cell)
    {
        cell.isPivot = true;
    }

    void UpdateBuildingTilesAttributes(Vector2 gridPos, GameObject building, int buildingSize, PowerPlant powerPlant, WaterPlant waterPlant, GameObject buildingPrefab)
    {
        
        int offsetX = 0;
        int offsetY = 0;
        
        // Determine the proper offset based on building size
        if (buildingSize % 2 == 0)
        {
            // Even-sized buildings - use cursor position as the top-left corner
            offsetX = 0;
            offsetY = 0;
        }
        else
        {
            // Odd-sized buildings - center the building on the cursor position
            offsetX = buildingSize / 2;
            offsetY = buildingSize / 2;
        }
        
        for (int x = 0; x < buildingSize; x++)
        {
            for (int y = 0; y < buildingSize; y++)
            {
                int gridX = (int)gridPos.x + x - offsetX;
                int gridY = (int)gridPos.y + y - offsetY;

                // Ensure we're within grid bounds
                if (gridX >= 0 && gridX < width && gridY >= 0 && gridY < height)
                {
                    GameObject gridCell = gridArray[gridX, gridY];
                    Cell cell = gridCell.GetComponent<Cell>();

                    UpdatePlacedBuildingCellAttributes(cell, buildingSize, powerPlant, waterPlant, buildingPrefab, Zone.ZoneType.Building, building);

                    if (gridX == gridPos.x && gridY == gridPos.y)
                    {
                        DestroyCellChildren(gridCell, new Vector2(gridX, gridY));
                        SetCellAsBuildingPivot(cell);
                    }
                }
            }
        }
    }

    void HandleBuildingPlacementAttributesUpdate(IBuilding iBuilding, Vector2 gridPos, GameObject building, GameObject buildingPrefab)
    {      
        int buildingSize = iBuilding.BuildingSize;
        PowerPlant powerPlant = iBuilding.GameObjectReference.GetComponent<PowerPlant>();
        WaterPlant waterPlant = iBuilding.GameObjectReference.GetComponent<WaterPlant>();

        if (powerPlant != null)
        {
            cityStats.RegisterPowerPlant(powerPlant);
        }
        
        if (waterPlant != null && waterManager != null)
        {
            waterManager.RegisterWaterPlant(waterPlant);
            cityStats.cityWaterOutput = waterManager.GetTotalWaterOutput();
        }
        
        UpdateBuildingTilesAttributes(gridPos, building, buildingSize, powerPlant, waterPlant, buildingPrefab);

        cursorManager.RemovePreview();
    }

    void PlaceBuildingTile(IBuilding iBuilding, Vector2 gridPos)
    {
        GameObject buildingPrefab = iBuilding.Prefab;
        int buildingSize = iBuilding.BuildingSize;
        
        // Calculate the adjusted grid position for the actual pivot cell
        Vector2 pivotGridPos = new Vector2(gridPos.x, gridPos.y);
        
        // Get the world position for the pivot cell
        Vector2 position = GetWorldPosition((int)pivotGridPos.x, (int)pivotGridPos.y);
        
        // For even-sized buildings, adjust the visual position slightly
        if (buildingSize > 1 && buildingSize % 2 == 0)
        {
            position.x += tileWidth / 4f; // Small visual adjustment for even-sized buildings
        }
        
        // Create the building
        GameObject building = Instantiate(buildingPrefab, position, Quaternion.identity);
        building.transform.SetParent(gridArray[(int)pivotGridPos.x, (int)pivotGridPos.y].transform);

        // adjust the building's position to match the grid cell based on its size

        if (buildingSize > 1 && buildingSize % 2 == 0)
        {
            building.transform.position += new Vector3(tileWidth / 4f, 0, 0);
        }

        int sortingOrder = SetTileSortingOrder(building, Zone.ZoneType.Building);
        gridArray[(int)pivotGridPos.x, (int)pivotGridPos.y].GetComponent<Cell>().sortingOrder = sortingOrder;

        HandleBuildingPlacementAttributesUpdate(iBuilding, pivotGridPos, building, buildingPrefab);
    }

    void LoadBuildingTile(GameObject prefab, Vector2 gridPos, int buildingSize)
    {
        GameObject building = Instantiate(prefab, GetWorldPosition((int)gridPos.x, (int)gridPos.y), Quaternion.identity);
        building.transform.SetParent(gridArray[(int)gridPos.x, (int)gridPos.y].transform);

        int sortingOrder = SetTileSortingOrder(building, Zone.ZoneType.Building);
        gridArray[(int)gridPos.x, (int)gridPos.y].GetComponent<Cell>().sortingOrder = sortingOrder;
    }

    void PlaceBuilding(Vector2 gridPos, IBuilding iBuilding)
    {
        if (!cityStats.CanAfford(iBuilding.ConstructionCost))
        {
            uiManager.ShowInsufficientFundsTooltip("building", iBuilding.ConstructionCost);
            return;
        }
        
        if (canPlaceBuilding(gridPos, iBuilding.BuildingSize))
        {
            // Deduct the cost BEFORE placing the building
            cityStats.RemoveMoney(iBuilding.ConstructionCost);
            
            // Then place the building
            PlaceBuildingTile(iBuilding, gridPos);

            GameNotificationManager.Instance.PostBuildingConstructed(
                iBuilding.Prefab.name
            );
        }
        else
        {
            GameNotificationManager.Instance.PostBuildingPlacementError(
                "Cannot place building here, area is not available."
            );
        }
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

        return Enum.GetValues(typeof(Zone.ZoneType))
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

    public List<CellData> GetGridData()
    {
        List<CellData> gridData = new List<CellData>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject cell = gridArray[x, y];
                Cell cellComponent = cell.GetComponent<Cell>();

                CellData cellData = cellComponent.GetCellData();

                gridData.Add(cellData);
            }
        }

        return gridData;
    }

    void RestoreGridCell(CellData cellData, GameObject cell)
    {
        cell.GetComponent<Cell>().SetCellData(cellData);

        Zone.ZoneType zoneType = GetZoneTypeFromZoneTypeString(cellData.zoneType);

        GameObject tilePrefab = zoneManager.FindPrefabByName(cellData.prefabName);
        if (tilePrefab != null)
        {
            PlaceZoneBuildingTile(tilePrefab, cell, cellData.buildingSize);
            UpdatePlacedBuildingCellAttributes(cell.GetComponent<Cell>(), cellData.buildingSize, cellData.powerPlant, cellData.waterPlant, tilePrefab, zoneType, null);
        }

        addZonedTileToList(new Vector2(cellData.x, cellData.y), zoneType);

        PowerPlant powerPlant = cellData.powerPlant;
        if (powerPlant != null)
        {
            cityStats.RegisterPowerPlant(powerPlant);
        }
    }

    public void RestoreGrid(List<CellData> gridData)
    {
        foreach (CellData cellData in gridData)
        {
            GameObject cell = gridArray[cellData.x, cellData.y];

            RestoreGridCell(cellData, cell);
        }

        CalculateAvailableSquareZonedSections();
    }

    public List<GameObject> GetRoadPrefabs()
    {
        return roadTilePrefabs;
    }

    private Zone.ZoneType GetZoneTypeFromZoneTypeString(string zoneTypeString)
    {
        return (Zone.ZoneType)Enum.Parse(typeof(Zone.ZoneType), zoneTypeString);
    }

    public void ResetGrid()
    {
        foreach (GameObject cell in gridArray)
        {
            Destroy(cell);
        }

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

        CreateGrid();
    }

    public Cell GetCell(int x, int y)
    {
        if (x >= 0 && x < width && y >= 0 && y < height)
        {
            GameObject cell = gridArray[x, y];

            Cell cellComponent = cell.GetComponent<Cell>();

            return cellComponent;
        }
        return null;
    }
}
