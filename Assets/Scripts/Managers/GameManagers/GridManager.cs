using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System;
using Territory.Zones;
using Territory.UI;
using Territory.Economy;
using Territory.Terrain;
using Territory.Forests;
using Territory.Buildings;
using Territory.Roads;
using Territory.Timing;

namespace Territory.Core
{
/// <summary>
/// Central hub for the isometric 2D grid. Manages cell arrays, coordinate conversion between
/// world and grid space, building placement and validation, bulldozing, sorting order assignment,
/// chunk-based culling, road caching, and A* pathfinding. Most managers depend on GridManager
/// for cell access via GetCell(x,y) and coordinate utilities.
/// Mouse-to-cell uses <see cref="RefineGridPositionForTerrainHeight"/> plus screen-space disambiguation in <see cref="GetCellFromWorldPoint"/>.
/// </summary>
public class GridManager : MonoBehaviour, IGridManager
{
    #region Dependencies
    public ZoneManager zoneManager;
    public UIManager uiManager;
    public CityStats cityStats;
    public CursorManager cursorManager;
    public TerrainManager terrainManager;
    public DemandManager demandManager;
    public WaterManager waterManager;
    public GameNotificationManager GameNotificationManager;
    public ForestManager forestManager;
    public CameraController cameraController;
    public RoadManager roadManager;
    public InterstateManager interstateManager;
    public BuildingSelectorMenuController buildingSelectorMenuController;

    // Helper services (initialized in InitializeGrid)
    private GridPathfinder pathfinder;
    private GridSortingOrderService sortingService;
    private BuildingPlacementService placementService;
    private ChunkCullingSystem chunkCulling;
    private RoadCacheService roadCache;

    /// <summary>Invoked when urban cells (buildings) are bulldozed. Args: list of grid positions.</summary>
    public System.Action<System.Collections.Generic.IReadOnlyList<Vector2Int>> onUrbanCellsBulldozed;

    /// <summary>Invoked when grid is restored from save. Listeners should invalidate caches.</summary>
    public System.Action onGridRestored;
    #endregion

    #region Grid Configuration
    public int width, height;
    public float tileWidth = 1f; // Full width of the tile
    public float tileHeight = 0.5f; // Effective height due to isometric perspective

    public float halfWidth;
    public float halfHeight;
    public GameObject[,] gridArray;
    public Cell[,] cellArray;
    public Vector2 mouseGridPosition;
    /// <summary>Last grid cell clicked (left or right button). (-1,-1) if none yet.</summary>
    public Vector2 selectedPoint = new Vector2(-1, -1);
    /// <summary>Grid position at right-click down; used to set selectedPoint on right-click up if not a pan.</summary>
    private Vector2 pendingRightClickGridPosition = new Vector2(-1, -1);
    public int mouseGridHeight;
    public int mouseGridSortingOrder;

    public bool isInitialized = false;

    [Header("Chunk Culling")]
    public int chunkSize = 16;
    private GameObject[,] chunkObjects;
    private bool[,] chunkActiveState;
    private int chunksX, chunksY;
    private Camera cachedCamera;
    /// <summary>When > 0, skip UpdateVisibility to avoid culling chunks incorrectly right after Load.</summary>
    private int skipChunkCullingFramesRemaining = 0;
    #endregion

    #region Initialization
    /// <summary>
    /// Bootstraps the grid: resolves dependencies, creates the cell/chunk arrays, generates terrain, and centers the camera.
    /// </summary>
    public void InitializeGrid()
    {
        halfWidth = tileWidth / 2f;
        halfHeight = tileHeight / 2f;

        // Default to 64x64 when not set in Inspector (balance between area and performance)
        if (width <= 0) width = 64;
        if (height <= 0) height = 64;

        if (zoneManager == null)
            zoneManager = FindObjectOfType<ZoneManager>();
        if (zoneManager != null)
            zoneManager.InitializeZonePrefabs();

        if (uiManager == null)
        {
            uiManager = FindObjectOfType<UIManager>();
        }

        if (cityStats == null)
        {
            cityStats = FindObjectOfType<CityStats>();
        }

        if (cursorManager == null)
        {
            cursorManager = FindObjectOfType<CursorManager>();
        }

        if (terrainManager == null)
        {
            terrainManager = FindObjectOfType<TerrainManager>();
        }

        if (waterManager == null)
        {
            waterManager = FindObjectOfType<WaterManager>();
        }

        if (cameraController == null)
        {
            cameraController = FindObjectOfType<CameraController>();
        }

        if (roadManager == null)
            roadManager = FindObjectOfType<RoadManager>();
        if (roadManager != null)
            roadManager.Initialize();

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

        if (interstateManager == null)
        {
            interstateManager = FindObjectOfType<InterstateManager>();
        }

        // Create sortingService before CreateGrid() so SetTileSortingOrder is available during grid creation
        sortingService = new GridSortingOrderService(this);

        CreateGrid();
        terrainManager.InitializeHeightMap();
        isInitialized = true;
        Vector3 centerWorldPosition = GetWorldPosition(
            width / 2, height / 2
        );

        cameraController.MoveCameraToMapCenter(centerWorldPosition);

        // Initialize helper services (sortingService already created above for CreateGrid)
        pathfinder = new GridPathfinder(this);
        placementService = new BuildingPlacementService(this, sortingService);
        chunkCulling = new ChunkCullingSystem(this, chunkSize, cachedCamera);
        chunkCulling.chunkObjects = chunkObjects;
        chunkCulling.chunkActiveState = chunkActiveState;
        roadCache = new RoadCacheService(this);
    }

    /// <param name="createBaseTiles">When false, cells are created without grass tiles (for Load). RestoreGrid will place all tiles from save.</param>
    void CreateGrid(bool createBaseTiles = true)
    {
        if (!zoneManager)
        {
            zoneManager = FindObjectOfType<ZoneManager>();
        }

        gridArray = new GameObject[width, height];
        cellArray = new Cell[width, height];

        chunksX = Mathf.CeilToInt((float)width / chunkSize);
        chunksY = Mathf.CeilToInt((float)height / chunkSize);
        chunkObjects = new GameObject[chunksX, chunksY];
        chunkActiveState = new bool[chunksX, chunksY];

        for (int cx = 0; cx < chunksX; cx++)
        {
            for (int cy = 0; cy < chunksY; cy++)
            {
                GameObject chunk = new GameObject($"Chunk_{cx}_{cy}");
                chunk.transform.SetParent(transform);
                chunkObjects[cx, cy] = chunk;
                chunkActiveState[cx, cy] = true;
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject gridCell = new GameObject($"Cell_{x}_{y}");

                float posX = (x - y) * (tileWidth / 2);
                float posY = (x + y) * (tileHeight / 2);

                gridCell.transform.position = new Vector3(posX, posY, 0);
                gridCell.transform.SetParent(chunkObjects[x / chunkSize, y / chunkSize].transform);

                CellData cellData = new CellData(x, y, 1);
                GameObject tilePrefab = zoneManager.GetRandomZonePrefab(Zone.ZoneType.Grass, 1);
                if (tilePrefab == null && zoneManager.grassPrefabs != null && zoneManager.grassPrefabs.Count > 0)
                    tilePrefab = zoneManager.grassPrefabs[0];
                if (tilePrefab != null)
                {
                    cellData.prefab = tilePrefab;
                    cellData.prefabName = tilePrefab.name;
                }

                Cell cellComponent = gridCell.AddComponent<Cell>();
                cellComponent.SetCellData(cellData);

                gridArray[x, y] = gridCell;
                cellArray[x, y] = cellComponent;

                if (createBaseTiles)
                {
                    GameObject zoneTile = Instantiate(
                        tilePrefab,
                        gridCell.transform.position,
                        Quaternion.identity
                    );
                    SetTileSortingOrder(zoneTile, Zone.ZoneType.Grass);

                    Zone zoneComponent = zoneTile.GetComponent<Zone>();
                    if (zoneComponent == null)
                    {
                        zoneComponent = zoneTile.AddComponent<Zone>();
                        zoneComponent.zoneType = Zone.ZoneType.Grass;
                    }
                }
            }
        }
    }
    #endregion

    #region Unity Lifecycle
    void Update()
    {
        try
        {
            if (!isInitialized)
            {
                return;
            }
            if (gridArray == null || gridArray.Length == 0)
            {
                return;
            }
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (uiManager.isBulldozeMode())
                {
                    uiManager.ExitBulldozeMode();
                }
                else if (uiManager.IsDetailsMode())
                {
                    uiManager.ExitDetailsMode();
                }
                else if (uiManager.IsBuildingPlacementMode())
                {
                    uiManager.ExitBuildingPlacementMode();
                }
                buildingSelectorMenuController.DeselectAndUnpressAllButtons();
            }

            if (cachedCamera == null) cachedCamera = Camera.main;
            Vector2 worldPoint = cachedCamera.ScreenToWorldPoint(Input.mousePosition);

            // USE: Height-aware grid position calculation
            // mouseGridPosition = GetGridPosition(worldPoint);
            Cell mouseGridCell = GetMouseGridCell(worldPoint);
            if (mouseGridCell == null)
            {
                return;
            }
            mouseGridPosition = new Vector2(mouseGridCell.x, mouseGridCell.y);
            mouseGridHeight = mouseGridCell.GetCellInstanceHeight();
            mouseGridSortingOrder = mouseGridCell.sortingOrder;

            if (mouseGridPosition.x == -1 && mouseGridPosition.y == -1)
            {
                return;
            }

            if (!IsValidGridPosition(mouseGridPosition))
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                selectedPoint = mouseGridPosition;
            }
            else if (Input.GetMouseButtonDown(1))
            {
                pendingRightClickGridPosition = mouseGridPosition;
            }
            else if (Input.GetMouseButtonUp(1) && cameraController != null && !cameraController.WasLastRightClickAPan && IsValidGridPosition(pendingRightClickGridPosition))
            {
                selectedPoint = pendingRightClickGridPosition;
            }

            if (uiManager.isBulldozeMode())
            {
                HandleBulldozerMode(mouseGridPosition);
            }

            if (uiManager.IsDetailsMode() || Input.GetKey(KeyCode.LeftShift))
            {
                HandleShowTileDetails(mouseGridPosition);
            }

            HandleRaycast(mouseGridPosition);

        }
        catch (System.Exception ex)
        {
            Debug.LogError("Update error: " + ex);
        }
    }

    void LateUpdate()
    {
        if (!isInitialized || chunkObjects == null) return;
        if (skipChunkCullingFramesRemaining > 0)
        {
            skipChunkCullingFramesRemaining--;
            return;
        }
        chunkCulling?.UpdateVisibility();
    }
    #endregion

    #region Cell Queries
    /// <summary>
    /// Returns true if the grid position is within the grid bounds.
    /// </summary>
    /// <param name="gridPosition">Grid coordinates to validate.</param>
    /// <returns>True when both x and y are inside [0, width) and [0, height).</returns>
    public bool IsValidGridPosition(Vector2 gridPosition)
    {
        int gridX = (int)gridPosition.x;
        int gridY = (int)gridPosition.y;

        return gridX >= 0 && gridX < width && gridY >= 0 && gridY < height;
    }

    /// <summary>
    /// Returns true if the cell is occupied by a building (any tile of a multi-cell building footprint).
    /// </summary>
    public bool IsCellOccupiedByBuilding(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return false;
        Cell cell = cellArray[x, y];
        if (cell == null) return false;
        return cell.occupiedBuilding != null || IsZoneTypeBuilding(cell.zoneType);
    }

    bool IsZoneTypeBuilding(Zone.ZoneType zoneType)
    {
        return zoneType == Zone.ZoneType.Building ||
               zoneType == Zone.ZoneType.ResidentialLightBuilding || zoneType == Zone.ZoneType.ResidentialMediumBuilding || zoneType == Zone.ZoneType.ResidentialHeavyBuilding ||
               zoneType == Zone.ZoneType.CommercialLightBuilding || zoneType == Zone.ZoneType.CommercialMediumBuilding || zoneType == Zone.ZoneType.CommercialHeavyBuilding ||
               zoneType == Zone.ZoneType.IndustrialLightBuilding || zoneType == Zone.ZoneType.IndustrialMediumBuilding || zoneType == Zone.ZoneType.IndustrialHeavyBuilding;
    }

    /// <summary>
    /// Gets the footprint offset for a building (par = 0,0; impar = buildingSize/2).
    /// </summary>
    public void GetBuildingFootprintOffset(int buildingSize, out int offsetX, out int offsetY)
    {
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
    }

    /// <summary>
    /// Returns the pivot cell for a multi-cell building. If the given cell is part of the building footprint, finds and returns the pivot cell (isPivot=true).
    /// </summary>
    public GameObject GetBuildingPivotCell(Cell cell)
    {
        if (cell == null)
            return null;
        if (cell.occupiedBuilding == null || cell.buildingSize <= 1)
            return gridArray[(int)cell.x, (int)cell.y];

        int size = cell.buildingSize;
        int cx = (int)cell.x;
        int cy = (int)cell.y;

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                int px = cx - i;
                int py = cy - j;
                if (px >= 0 && px < width && py >= 0 && py < height)
                {
                    Cell pivotCandidate = cellArray[px, py];
                    if (pivotCandidate != null && pivotCandidate.isPivot)
                        return gridArray[px, py];
                }
            }
        }
        return gridArray[cx, cy];
    }
    #endregion

    #region Input and Placement Handlers
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
        Cell cellComponent = cellArray[(int)gridPosition.x, (int)gridPosition.y];
        Zone.ZoneType zoneType = cellComponent.zoneType;

        if (!CanBulldoze(cellComponent))
        {
            return;
        }

        HandleBulldozeTile(zoneType, cell);
    }

    void RestoreCellAttributes(Cell cellComponent)
    {
        cellComponent.buildingType = null;
        cellComponent.powerPlant = null;
        cellComponent.occupiedBuilding = null;
        cellComponent.population = 0;
        cellComponent.powerConsumption = 0;
        cellComponent.waterConsumption = 0;
        cellComponent.happiness = 0;
        cellComponent.zoneType = Zone.ZoneType.Grass;
        cellComponent.buildingSize = 1;
        cellComponent.prefab = zoneManager.GetRandomZonePrefab(Zone.ZoneType.Grass);
        cellComponent.prefabName = cellComponent.prefab.name;
        cellComponent.isPivot = false;
        cellComponent.SetTree(false);
    }

    void RestoreTile(GameObject cell)
    {
        Cell cellComponent = cell.GetComponent<Cell>();

        // Ensure only one grass child: remove any existing grass tiles before adding the new one
        List<Transform> toDestroy = new List<Transform>();
        for (int i = 0; i < cell.transform.childCount; i++)
        {
            Transform child = cell.transform.GetChild(i);
            Zone zone = child.GetComponent<Zone>();
            if (zone != null && zone.zoneType == Zone.ZoneType.Grass)
                toDestroy.Add(child);
        }
        foreach (Transform t in toDestroy)
            Destroy(t.gameObject);

        GameObject zoneTile = Instantiate(
            zoneManager.GetRandomZonePrefab(Zone.ZoneType.Grass),
            cellComponent.transformPosition,
            Quaternion.identity
        );

        zoneTile.transform.SetParent(cell.transform);

        int sortingOrder;
        if (terrainManager != null)
        {
            sortingOrder = terrainManager.CalculateTerrainSortingOrder((int)cellComponent.x, (int)cellComponent.y, cellComponent.height);
            SpriteRenderer sr = zoneTile.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = sortingOrder;
            }
            cellComponent.SetCellInstanceSortingOrder(sortingOrder);
        }
        else
        {
            sortingOrder = SetTileSortingOrder(zoneTile, Zone.ZoneType.Grass);
        }
    }

    void BulldozeTile(GameObject cell)
    {
        Cell cellComponent = cell.GetComponent<Cell>();

        // Capture sorting order BEFORE demolition resets it
        int preSortingOrder = cellComponent.sortingOrder;

        if (cellComponent.forestType != Forest.ForestType.None && forestManager != null)
            forestManager.RemoveForestFromCell((int)cellComponent.x, (int)cellComponent.y);

        RestoreCellAttributes(cellComponent);

        DestroyCellChildren(cell, new Vector2(cellComponent.x, cellComponent.y));

        // Show the bulldoze animation using the pre-captured sorting order
        if (uiManager != null)
        {
            uiManager.ShowDemolitionAnimation(cell, preSortingOrder);
        }
    }

    void BulldozeBuildingTiles(GameObject cell, Zone.ZoneType zoneType, bool showAnimation = true)
    {
        Cell cellComponent = cell.GetComponent<Cell>();

        int buildingSize = cellComponent.buildingSize;

        // Capture sorting order BEFORE demolition resets it
        int preSortingOrder = cellComponent.sortingOrder;

        // Show animation before demolishing for better visual effect
        if (showAnimation && uiManager != null)
        {
            uiManager.ShowDemolitionAnimationCentered(cell, buildingSize, preSortingOrder);
        }

        bool isBuilding = zoneType == Zone.ZoneType.ResidentialLightBuilding || zoneType == Zone.ZoneType.ResidentialMediumBuilding ||
            zoneType == Zone.ZoneType.ResidentialHeavyBuilding || zoneType == Zone.ZoneType.CommercialLightBuilding ||
            zoneType == Zone.ZoneType.CommercialMediumBuilding || zoneType == Zone.ZoneType.CommercialHeavyBuilding ||
            zoneType == Zone.ZoneType.IndustrialLightBuilding || zoneType == Zone.ZoneType.IndustrialMediumBuilding ||
            zoneType == Zone.ZoneType.IndustrialHeavyBuilding;

        if (buildingSize > 1)
        {
            // If clicked cell is not pivot, find pivot to get correct footprint
            GameObject pivotCellObj = cellComponent.isPivot ? cell : GetBuildingPivotCell(cellComponent);
            if (pivotCellObj == null)
                pivotCellObj = cell;
            Cell pivotCell = pivotCellObj.GetComponent<Cell>();

            GetBuildingFootprintOffset(buildingSize, out int offsetX, out int offsetY);

            List<Vector2Int> footprint = null;
            if (isBuilding)
                footprint = new List<Vector2Int>();

            for (int x = 0; x < buildingSize; x++)
            {
                for (int y = 0; y < buildingSize; y++)
                {
                    int gridX = (int)pivotCell.x + x - offsetX;
                    int gridY = (int)pivotCell.y + y - offsetY;

                    if (gridX >= 0 && gridX < width && gridY >= 0 && gridY < height)
                    {
                        if (footprint != null)
                            footprint.Add(new Vector2Int(gridX, gridY));
                        BulldozeTileWithoutAnimation(gridArray[gridX, gridY]);
                    }
                }
            }
            if (footprint != null && footprint.Count > 0)
                onUrbanCellsBulldozed?.Invoke(footprint);
        }
        else
        {
            BulldozeTileWithoutAnimation(cell);
            if (isBuilding)
                onUrbanCellsBulldozed?.Invoke(new List<Vector2Int> { new Vector2Int((int)cellComponent.x, (int)cellComponent.y) });
        }
    }

    // Create a version without animation for multi-tile cleanup
    void BulldozeTileWithoutAnimation(GameObject cell)
    {
        Cell cellComponent = cell.GetComponent<Cell>();

        if (cellComponent.forestType != Forest.ForestType.None && forestManager != null)
            forestManager.RemoveForestFromCell((int)cellComponent.x, (int)cellComponent.y);

        RestoreCellAttributes(cellComponent);
        DestroyCellChildren(cell, new Vector2(cellComponent.x, cellComponent.y));

        int gx = (int)cellComponent.x;
        int gy = (int)cellComponent.y;
        HeightMap hm = terrainManager != null ? terrainManager.GetOrCreateHeightMap() : null;
        bool restoredWaterSlope = hm != null && terrainManager.RestoreTerrainForCell(gx, gy, hm);
        if (!restoredWaterSlope)
            RestoreTile(cell);
        if (roadManager != null)
            roadManager.UpdateAdjacentRoadPrefabsAt(new Vector2(gx, gy));
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
            cityStats.HandleBuildingDemolition(zoneType, zoneManager.GetZoneAttributes(zoneType));
        }
    }

    void HandleBulldozeTile(Zone.ZoneType zoneType, GameObject cell, bool showAnimation = true)
    {
        Cell cellComponent = cell.GetComponent<Cell>();

        HandleBuildingStatsReset(cellComponent, zoneType);

        BulldozeBuildingTiles(cell, zoneType, showAnimation);
    }

    /// <summary>
    /// Demolish the building/zoning at the given grid position. Uses same stats and logic as manual bulldoze.
    /// Returns true if something was demolished. When showAnimation is false (e.g. expropriation), road cache is invalidated.
    /// </summary>
    public bool DemolishCellAt(Vector2 gridPosition, bool showAnimation = true)
    {
        int gx = (int)gridPosition.x;
        int gy = (int)gridPosition.y;
        if (gx < 0 || gx >= width || gy < 0 || gy >= height) return false;
        GameObject cell = gridArray[gx, gy];
        Cell cellComponent = cellArray[gx, gy];
        if (cellComponent == null || !CanBulldoze(cellComponent)) return false;

        Zone.ZoneType zoneType = cellComponent.zoneType;
        if (zoneType == Zone.ZoneType.Road)
            RemoveRoadFromCache(new Vector2Int(gx, gy));
        HandleBulldozeTile(zoneType, cell, showAnimation);
        return true;
    }

    bool CanBulldoze(Cell cell)
    {
        if (cell == null)
        {
            return false;
        }

        if (cell.isInterstate)
        {
            if (GameNotificationManager != null)
                GameNotificationManager.PostWarning("The Interstate Highway cannot be demolished.");
            return false;
        }

        if (cell.forestType != Forest.ForestType.None)
        {
            return true;
        }

        switch (cell.zoneType)
        {
            case Zone.ZoneType.Road:
            case Zone.ZoneType.Building:
            case Zone.ZoneType.ResidentialLightBuilding:
            case Zone.ZoneType.ResidentialMediumBuilding:
            case Zone.ZoneType.ResidentialHeavyBuilding:
            case Zone.ZoneType.CommercialLightBuilding:
            case Zone.ZoneType.CommercialMediumBuilding:
            case Zone.ZoneType.CommercialHeavyBuilding:
            case Zone.ZoneType.IndustrialLightBuilding:
            case Zone.ZoneType.IndustrialMediumBuilding:
            case Zone.ZoneType.IndustrialHeavyBuilding:
            case Zone.ZoneType.ResidentialLightZoning:
            case Zone.ZoneType.ResidentialMediumZoning:
            case Zone.ZoneType.ResidentialHeavyZoning:
            case Zone.ZoneType.CommercialLightZoning:
            case Zone.ZoneType.CommercialMediumZoning:
            case Zone.ZoneType.CommercialHeavyZoning:
            case Zone.ZoneType.IndustrialLightZoning:
            case Zone.ZoneType.IndustrialMediumZoning:
            case Zone.ZoneType.IndustrialHeavyZoning:
                return true;
            default:
                return false;
        }
    }

    void HandleShowTileDetails(Vector2 gridPosition)
    {
        if (Input.GetMouseButtonDown(0))
        {
            Cell cellComponent = cellArray[(int)gridPosition.x, (int)gridPosition.y];
            uiManager.ShowTileDetails(cellComponent);
        }
    }

    void HandleRaycast(Vector2 gridPosition)
    {
        Zone.ZoneType selectedZoneType = uiManager.GetSelectedZoneType();
        IBuilding selectedBuilding = uiManager.GetSelectedBuilding();
        IForest selectedForest = uiManager.GetSelectedForest();

        if (selectedZoneType == Zone.ZoneType.Road)
        {
            roadManager.HandleRoadDrawing(gridPosition);
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
            zoneManager.HandleZoning(mouseGridPosition);
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

    void HandleBuildingPlacement(Vector3 gridPosition, IBuilding selectedBuilding)
    {
        if (Input.GetMouseButtonDown(0))
        {
            placementService.PlaceBuilding(gridPosition, selectedBuilding);
        }
    }

    void HandleForestPlacement(Vector2 gridPosition, IForest selectedForest)
    {
        if (Input.GetMouseButtonDown(0))
        {
            forestManager.PlaceForest(gridPosition, selectedForest);
        }
    }

    /// <summary>
    /// Returns a user-facing demand feedback string (e.g. "✓ Demand: 80%") for the given zone type, considering employment and residential support.
    /// </summary>
    /// <param name="zoneType">The zone type to evaluate demand for.</param>
    /// <returns>A formatted demand feedback string, or empty if no demand manager exists.</returns>
    public string GetDemandFeedback(Zone.ZoneType zoneType)
    {
        if (demandManager == null)
            return "";

        float demandLevel = demandManager.GetDemandLevel(zoneType);
        bool canGrow = demandManager.CanZoneTypeGrow(zoneType);

        // Check if it's a residential building type
        Zone.ZoneType buildingType = zoneManager.GetBuildingZoneType(zoneType);
        bool isResidential = zoneManager.IsResidentialBuilding(buildingType);
        bool hasJobsAvailable = !isResidential || demandManager.CanPlaceResidentialBuilding();

        // Check if it's a commercial/industrial building type
        bool needsResidential = zoneManager.IsCommercialOrIndustrialBuilding(buildingType);
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
    #endregion

    #region Cell Destruction and Attributes
    /// <summary>
    /// Destroys all non-terrain children of the cell at the given grid position. Convenience overload that excludes nothing.
    /// </summary>
    /// <param name="cell">The cell GameObject whose children will be destroyed.</param>
    /// <param name="gridPosition">Grid coordinates of the cell (used for zone list bookkeeping).</param>
    public void DestroyCellChildren(GameObject cell, Vector2 gridPosition)
    {
        DestroyCellChildren(cell, gridPosition, null, false);
    }

    /// <summary>
    /// Destroys all children of the cell except the optional exclude object (e.g. the building being placed).
    /// Does not destroy terrain (flat grass) or slope children (land/water slope), unless
    /// <paramref name="destroyFlatGrass"/> is true (building/utility placement so the cell has a single visual layer).
    /// Used by DemolishCellAt (including expropriation with showAnimation: false): any child with
    /// ZoneCategory.Zoning is removed from the zone manager lists (removeZonedPositionFromList) before destroy.
    /// </summary>
    public void DestroyCellChildren(GameObject cell, Vector2 gridPosition, GameObject excludeFromDestroy)
    {
        DestroyCellChildren(cell, gridPosition, excludeFromDestroy, false);
    }

    /// <param name="destroyFlatGrass">When true, flat grass zone tiles are destroyed too (building placement and load restore).</param>
    public void DestroyCellChildren(GameObject cell, Vector2 gridPosition, GameObject excludeFromDestroy, bool destroyFlatGrass)
    {
        if (cell.transform.childCount == 0) return;

        var toDestroy = new List<GameObject>();
        foreach (Transform child in cell.transform)
        {
            if (excludeFromDestroy != null && child.gameObject == excludeFromDestroy)
                continue;

            // Do not destroy flat terrain (grass) or slope tiles (land or water), unless replacing with a building/road overlay
            Zone zone = child.GetComponent<Zone>();
            if (!destroyFlatGrass && zone != null && zone.zoneType == Zone.ZoneType.Grass)
                continue;
            if (terrainManager != null && (terrainManager.IsWaterSlopeObject(child.gameObject) || terrainManager.IsLandSlopeObject(child.gameObject)))
                continue;

            if (zone != null && zone.zoneCategory == Zone.ZoneCategory.Zoning)
            {
                zoneManager.removeZonedPositionFromList(gridPosition, zone.zoneType);
            }

            toDestroy.Add(child.gameObject);
        }
        foreach (GameObject go in toDestroy)
            Destroy(go);
    }

    /// <summary>
    /// Same as DestroyCellChildren but preserves the cell's forest object so zoning can be merged with forest.
    /// </summary>
    public void DestroyCellChildrenExceptForest(GameObject cell, Vector2 gridPosition)
    {
        if (cell.transform.childCount == 0) return;

        Cell cellComponent = cellArray[(int)gridPosition.x, (int)gridPosition.y];
        GameObject forestObject = (cellComponent != null && cellComponent.HasForest()) ? cellComponent.forestObject : null;

        List<Transform> toDestroy = new List<Transform>();
        foreach (Transform child in cell.transform)
        {
            if (forestObject != null && child.gameObject == forestObject)
                continue;
            toDestroy.Add(child);
        }

        foreach (Transform child in toDestroy)
        {
            Zone zone = child.GetComponent<Zone>();
            if (zone != null && zone.zoneCategory == Zone.ZoneCategory.Zoning)
                zoneManager.removeZonedPositionFromList(gridPosition, zone.zoneType);
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// Overwrites a cell's zone type, population, power consumption, happiness, prefab, and building size with the supplied values.
    /// </summary>
    /// <param name="cellComponent">The cell to update.</param>
    /// <param name="selectedZoneType">New zone type for the cell.</param>
    /// <param name="zoneAttributes">Attributes (population, power, happiness) to apply.</param>
    /// <param name="prefab">The prefab GameObject associated with this zone.</param>
    /// <param name="buildingSize">Footprint size of the building (1 for single-cell).</param>
    public void UpdateCellAttributes(Cell cellComponent, Zone.ZoneType selectedZoneType, ZoneAttributes zoneAttributes, GameObject prefab, int buildingSize)
    {
        cellComponent.zoneType = selectedZoneType;
        cellComponent.population = zoneAttributes.Population;
        cellComponent.powerConsumption = zoneAttributes.PowerConsumption;
        cellComponent.waterConsumption = zoneAttributes.WaterConsumption;
        cellComponent.happiness = zoneAttributes.Happiness;
        cellComponent.prefab = prefab;
        cellComponent.prefabName = prefab.name;
        cellComponent.buildingType = prefab.name;
        cellComponent.buildingSize = buildingSize;
        cellComponent.isPivot = false;
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
    #endregion

    #region Sorting Order
    /// <summary>
    /// Sets the sorting order of a tile using a legacy formula based on grid position. Prefers TerrainManager-based methods for new code.
    /// </summary>
    /// <param name="tile">The tile GameObject to update.</param>
    /// <param name="zoneType">Zone type used to determine the sorting layer offset.</param>
    /// <returns>The computed sorting order, or -1001 if the tile is outside the grid.</returns>
    public int SetTileSortingOrder(GameObject tile, Zone.ZoneType zoneType = Zone.ZoneType.Grass)
        => sortingService.SetTileSortingOrder(tile, zoneType);

    /// <summary>
    /// Sets sorting order for a zoning tile (RCI overlay) using TerrainManager so it renders below forest and buildings.
    /// </summary>
    public void SetZoningTileSortingOrder(GameObject tile, int x, int y)
        => sortingService.SetZoningTileSortingOrder(tile, x, y);

    /// <summary>
    /// Sets sorting order for a zone building (RCI) tile using TerrainManager so it renders above forest and terrain.
    /// </summary>
    public void SetZoneBuildingSortingOrder(GameObject tile, int x, int y)
        => sortingService.SetZoneBuildingSortingOrder(tile, x, y);

    /// <summary>
    /// Sets sorting order for a multi-cell building using the maximum order over its footprint so the whole building renders in front of all covered terrain.
    /// </summary>
    public void SetZoneBuildingSortingOrder(GameObject tile, int pivotX, int pivotY, int buildingSize)
        => sortingService.SetZoneBuildingSortingOrder(tile, pivotX, pivotY, buildingSize);

    /// <summary>
    /// Returns the sorting order to use for a road tile at (x, y) at the given height (e.g. 1 for bridge over water).
    /// </summary>
    public int GetRoadSortingOrderForCell(int x, int y, int height)
        => sortingService.GetRoadSortingOrderForCell(x, y, height);

    /// <summary>
    /// Sets sorting order for a road tile using TerrainManager so it renders below forest and buildings.
    /// </summary>
    public void SetRoadSortingOrder(GameObject tile, int x, int y)
        => sortingService.SetRoadSortingOrder(tile, x, y);

    /// <summary>
    /// Sets the sorting order for a sea-level tile so it renders behind all land content.
    /// </summary>
    /// <param name="tile">The tile GameObject to update.</param>
    /// <param name="cell">The cell this tile belongs to.</param>
    /// <returns>The computed sea-level sorting order.</returns>
    public int SetResortSeaLevelOrder(GameObject tile, Cell cell)
        => sortingService.SetResortSeaLevelOrder(tile, cell);
    #endregion

    #region Coordinate Conversion
    /// <summary>
    /// Converts a world-space point to isometric grid coordinates (ignoring height).
    /// </summary>
    /// <param name="worldPoint">Position in world space.</param>
    /// <returns>Grid coordinates as a Vector2 (x, y).</returns>
    public Vector2 GetGridPosition(Vector2 worldPoint)
    {
        float posX = worldPoint.x / (tileWidth / 2);
        float posY = worldPoint.y / (tileHeight / 2);

        int gridX = Mathf.RoundToInt((posY + posX) / 2);
        int gridY = Mathf.RoundToInt((posY - posX) / 2);

        return new Vector2(gridX, gridY);
    }

    /// <summary>
    /// Terrain height at a grid cell for mouse projection refinement (defaults to 1 if out of bounds).
    /// </summary>
    private int GetTerrainHeightForGridCell(int gridX, int gridY)
    {
        Cell c = GetCell(gridX, gridY);
        return c != null ? c.GetCellInstanceHeight() : 1;
    }

    /// <summary>
    /// Refines <see cref="GetGridPosition"/> by stripping the vertical offset used in <see cref="GetWorldPositionVector"/>
    /// for the current terrain height estimate, then iterating until stable. Fixes diagonal bias when cells are elevated.
    /// </summary>
    private Vector2 RefineGridPositionForTerrainHeight(Vector2 worldPoint)
    {
        if (width <= 0 || height <= 0)
            return GetGridPosition(worldPoint);

        Vector2 grid = GetGridPosition(worldPoint);
        const int maxIterations = 4;
        for (int iter = 0; iter < maxIterations; iter++)
        {
            int gx = Mathf.Clamp(Mathf.RoundToInt(grid.x), 0, width - 1);
            int gy = Mathf.Clamp(Mathf.RoundToInt(grid.y), 0, height - 1);
            int h = GetTerrainHeightForGridCell(gx, gy);
            float heightOffset = (h - 1) * (tileHeight / 2f);
            Vector2 planePoint = new Vector2(worldPoint.x, worldPoint.y - heightOffset);
            Vector2 next = GetGridPosition(planePoint);
            if (Mathf.Approximately(next.x, grid.x) && Mathf.Approximately(next.y, grid.y))
                return next;
            grid = next;
        }
        return grid;
    }

    /// <summary>
    /// Gets the screen-space bounds of the cell's base tile (first child with Zone Grass/Road or Zoning and SpriteRenderer).
    /// If none, uses the first child with SpriteRenderer (e.g. slope prefab) so hit-test works at all heights.
    /// Fallback: rect from cell.transformPosition and tile size when no such child exists.
    /// </summary>
    private bool TryGetCellBaseTileScreenBounds(Cell cell, Camera cam, out Rect screenRect)
    {
        screenRect = new Rect(0, 0, 0, 0);
        if (cell == null || cam == null) return false;

        SpriteRenderer tileRenderer = null;
        SpriteRenderer anyRenderer = null;
        for (int i = 0; i < cell.gameObject.transform.childCount; i++)
        {
            Transform child = cell.gameObject.transform.GetChild(i);
            Zone zone = child.GetComponent<Zone>();
            SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                if (anyRenderer == null)
                    anyRenderer = sr;
                if (zone != null && (zone.zoneType == Zone.ZoneType.Grass || zone.zoneType == Zone.ZoneType.Road || zone.zoneCategory == Zone.ZoneCategory.Zoning))
                {
                    tileRenderer = sr;
                    break;
                }
            }
        }

        SpriteRenderer boundsSource = tileRenderer != null ? tileRenderer : anyRenderer;
        Bounds worldBounds;
        if (boundsSource != null)
        {
            worldBounds = boundsSource.bounds;
        }
        else
        {
            Vector2 center = cell.transformPosition;
            worldBounds = new Bounds(center, new Vector3(tileWidth, tileHeight, 0f));
        }

        Vector3 worldMin = worldBounds.min;
        Vector3 worldMax = worldBounds.max;
        float zRef = worldBounds.center.z;
        Vector3 p0 = cam.WorldToScreenPoint(new Vector3(worldMin.x, worldMin.y, zRef));
        Vector3 p1 = cam.WorldToScreenPoint(new Vector3(worldMax.x, worldMin.y, zRef));
        Vector3 p2 = cam.WorldToScreenPoint(new Vector3(worldMin.x, worldMax.y, zRef));
        Vector3 p3 = cam.WorldToScreenPoint(new Vector3(worldMax.x, worldMax.y, zRef));

        float minX = Mathf.Min(p0.x, p1.x, p2.x, p3.x);
        float maxX = Mathf.Max(p0.x, p1.x, p2.x, p3.x);
        float minY = Mathf.Min(p0.y, p1.y, p2.y, p3.y);
        float maxY = Mathf.Max(p0.y, p1.y, p2.y, p3.y);

        // Inset rect toward center so hit areas don't overlap with neighbors (isometric AABB overlap).
        // 0.6 reduces gaps vs 0.75 while still avoiding excessive overlap in cut-through scenarios.
        const float insetFactor = 0.6f;
        float width = maxX - minX;
        float height = maxY - minY;
        float insetW = width * (1f - insetFactor) * 0.5f;
        float insetH = height * (1f - insetFactor) * 0.5f;
        minX += insetW;
        maxX -= insetW;
        minY += insetH;
        maxY -= insetH;

        screenRect = Rect.MinMaxRect(minX, minY, maxX, maxY);
        return true;
    }

    /// <summary>
    /// Resolves a world-space point to the best-matching cell using screen-space hit testing against neighboring cells.
    /// </summary>
    /// <param name="worldPoint">Position in world space.</param>
    /// <param name="gridPos">Initial grid position estimate (from GetGridPosition).</param>
    /// <returns>The cell whose base tile contains the mouse, or null if none match.</returns>
    public Cell GetCellFromWorldPoint(Vector2 worldPoint, Vector2 gridPos)
    {
        if (cachedCamera == null) cachedCamera = Camera.main;
        Camera cam = cachedCamera;
        if (cam == null) return null;

        int gridX = (int)gridPos.x;
        int gridY = (int)gridPos.y;

        // 9 candidate cells: center + 8 neighbors (3x3)
        List<Cell> candidates = new List<Cell>();
        int[] dx = { 0, 1, -1, 0, 0, 1, 1, -1, -1 };
        int[] dy = { 0, 0, 0, 1, -1, 1, -1, 1, -1 };
        for (int i = 0; i < 9; i++)
        {
            Cell c = GetCell(gridX + dx[i], gridY + dy[i]);
            if (c != null) candidates.Add(c);
        }

        Vector2 mouseScreen = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        List<Cell> rectHits = new List<Cell>();
        foreach (Cell cell in candidates)
        {
            if (!TryGetCellBaseTileScreenBounds(cell, cam, out Rect screenRect))
                continue;
            if (screenRect.Contains(mouseScreen))
                rectHits.Add(cell);
        }

        if (rectHits.Count == 1)
            return rectHits[0];

        if (rectHits.Count > 1)
            return PickCellHitClosestOnScreen(mouseScreen, rectHits, cam);

        if (candidates.Count > 0)
            return GetClosestCellByScreenDistance(mouseScreen, candidates, cam);

        return null;
    }

    /// <summary>
    /// When multiple cells' screen rects contain the cursor (isometric overlap), picks the one whose elevated
    /// world center projects closest to the mouse; ties break by higher <see cref="Cell.sortingOrder"/>.
    /// </summary>
    private Cell PickCellHitClosestOnScreen(Vector2 mouseScreen, List<Cell> hits, Camera cam)
    {
        if (hits == null || hits.Count == 0 || cam == null)
            return null;

        Cell best = null;
        float bestDistSq = float.MaxValue;
        int bestOrder = int.MinValue;
        const float tiePixels = 4f;
        float tieEpsSq = tiePixels * tiePixels;

        foreach (Cell cell in hits)
        {
            if (cell == null)
                continue;

            Vector2 worldCenter = GetCellWorldPosition(cell);
            Vector3 sc = cam.WorldToScreenPoint(new Vector3(worldCenter.x, worldCenter.y, cell.transform.position.z));
            float dx = mouseScreen.x - sc.x;
            float dy = mouseScreen.y - sc.y;
            float distSq = dx * dx + dy * dy;

            bool better = best == null;
            if (!better && distSq < bestDistSq - 0.001f)
                better = true;
            else if (!better && Mathf.Abs(distSq - bestDistSq) <= tieEpsSq && cell.sortingOrder > bestOrder)
                better = true;

            if (better)
            {
                best = cell;
                bestDistSq = distSq;
                bestOrder = cell.sortingOrder;
            }
        }

        return best;
    }

    /// <summary>
    /// Returns the cell whose screen-space center is closest to the given mouse position.
    /// Used as fallback when no cell's screen rect contains the mouse (e.g. gaps between insets).
    /// </summary>
    private Cell GetClosestCellByScreenDistance(Vector2 mouseScreen, List<Cell> candidates, Camera cam)
    {
        if (cam == null || candidates == null || candidates.Count == 0) return null;

        Cell closest = null;
        float minDistSq = float.MaxValue;

        foreach (Cell cell in candidates)
        {
            Vector2 worldCenter = GetCellWorldPosition(cell);
            Vector3 screenCenter = cam.WorldToScreenPoint(new Vector3(worldCenter.x, worldCenter.y, 0f));
            float dx = mouseScreen.x - screenCenter.x;
            float dy = mouseScreen.y - screenCenter.y;
            float distSq = dx * dx + dy * dy;
            if (distSq < minDistSq)
            {
                minDistSq = distSq;
                closest = cell;
            }
        }
        return closest;
    }

    /// <summary>
    /// Converts a mouse world point to grid coordinates, correcting for terrain height via screen-space hit testing.
    /// </summary>
    /// <param name="mouseWorldPoint">Mouse position in world space.</param>
    /// <returns>Height-aware grid coordinates.</returns>
    public Vector2 GetGridPositionWithHeight(Vector2 mouseWorldPoint)
    {
        Vector2 gridPos = RefineGridPositionForTerrainHeight(mouseWorldPoint);

        Cell cell = GetCellFromWorldPoint(mouseWorldPoint, gridPos);
        if (cell == null)
        {
            return gridPos;
        }

        Vector2 gridPosWithHeight = new Vector2(cell.x, cell.y);

        return gridPosWithHeight;
    }

    /// <summary>
    /// Returns the cell under the mouse, using screen-space hit testing with closest-cell fallback when no rect contains the mouse.
    /// </summary>
    /// <param name="mouseWorldPoint">Mouse position in world space.</param>
    /// <returns>The cell under the mouse, or null if outside the grid.</returns>
    public Cell GetMouseGridCell(Vector2 mouseWorldPoint)
    {
        Vector2 gridPos = RefineGridPositionForTerrainHeight(mouseWorldPoint);
        return GetCellFromWorldPoint(mouseWorldPoint, gridPos);
    }
    /// <summary>
    /// Converts grid coordinates and height to a world-space position (height shifts the tile upward).
    /// </summary>
    /// <param name="gridX">Grid X coordinate.</param>
    /// <param name="gridY">Grid Y coordinate.</param>
    /// <param name="height">Terrain height level (1 = base).</param>
    /// <returns>World-space position as a Vector2.</returns>
    public Vector2 GetWorldPositionVector(int gridX, int gridY, int height)
    {
        float heightOffset = (height - 1) * (tileHeight / 2);

        float posX = (gridX - gridY) * (tileWidth / 2);
        float posY = (gridX + gridY) * (tileHeight / 2) + heightOffset;

        return new Vector2(posX, posY);
    }

    private Vector2 GetWorldPositionVectorDown(int gridX, int gridY, int height)
    {
        float heightOffset = (height - 1) * (tileHeight / 2);
        float posX = (gridX - gridY) * (tileWidth / 2);
        float posY = (gridX + gridY) * (tileHeight / 2) - heightOffset;
        return new Vector2(posX, posY);
    }

    /// <summary>
    /// Returns the world-space position for the cell at (gridX, gridY), accounting for its current terrain height.
    /// </summary>
    /// <param name="gridX">Grid X coordinate.</param>
    /// <param name="gridY">Grid Y coordinate.</param>
    /// <returns>World-space position as a Vector2.</returns>
    public Vector2 GetWorldPosition(int gridX, int gridY)
    {
        Cell cell = cellArray[gridX, gridY];
        int height = cell.GetCellInstanceHeight();
        return GetWorldPositionVector(gridX, gridY, height);
    }

    /// <summary>
    /// Returns the world-space position of the given cell, accounting for its terrain height.
    /// </summary>
    /// <param name="cell">The cell to get the position for.</param>
    /// <returns>World-space position as a Vector2.</returns>
    public Vector2 GetCellWorldPosition(Cell cell)
    {
        int height = cell.GetCellInstanceHeight();
        return GetWorldPositionVector(cell.x, cell.y, height);
    }

    /// <summary>
    /// Returns the world position where a building should be placed (preview and actual placement).
    /// For size 1 uses pivot cell; for size > 1 uses center of footprint so preview and placement match.
    /// </summary>
    public Vector2 GetBuildingPlacementWorldPosition(Vector2 gridPos, int buildingSize)
    {
        Cell pivotCell = cellArray[(int)gridPos.x, (int)gridPos.y];
        if (buildingSize <= 1)
            return pivotCell.transformPosition;

        GetBuildingFootprintOffset(buildingSize, out int offsetX, out int offsetY);
        Vector2 sum = Vector2.zero;
        int count = 0;
        for (int x = 0; x < buildingSize; x++)
        {
            for (int y = 0; y < buildingSize; y++)
            {
                int gridX = (int)gridPos.x + x - offsetX;
                int gridY = (int)gridPos.y + y - offsetY;
                if (gridX >= 0 && gridX < width && gridY >= 0 && gridY < height)
                {
                    Cell c = cellArray[gridX, gridY];
                    sum += GetCellWorldPosition(c);
                    count++;
                }
            }
        }
        return count > 0 ? sum / count : pivotCell.transformPosition;
    }

    /// <summary>
    /// Returns the GameObject for the grid cell at the given position, or null if out of bounds.
    /// </summary>
    /// <param name="gridPos">Grid coordinates.</param>
    /// <returns>The cell GameObject, or null.</returns>
    public GameObject GetGridCell(Vector2 gridPos)
    {
        if (gridPos.x < 0 || gridPos.x >= gridArray.GetLength(0) ||
            gridPos.y < 0 || gridPos.y >= gridArray.GetLength(1))
        {
            return null;
        }
        return gridArray[(int)gridPos.x, (int)gridPos.y];
    }

    /// <summary>
    /// Sets the terrain height of the cell at the given grid position.
    /// </summary>
    /// <param name="gridPos">Grid coordinates of the cell.</param>
    /// <param name="height">New height value to assign.</param>
    /// <param name="skipWaterMembershipRefresh">When true, skips <see cref="WaterManager.OnLandCellHeightCommitted"/> (e.g. <see cref="TerrainManager.UpdateTileElevation"/> finishes membership in a finally block).</param>
    public void SetCellHeight(Vector2 gridPos, int height, bool skipWaterMembershipRefresh = false)
    {
        Cell cell = cellArray[(int)gridPos.x, (int)gridPos.y];
        cell.SetCellInstanceHeight(height);
        if (skipWaterMembershipRefresh)
            return;
        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();
        if (waterManager != null)
            waterManager.OnLandCellHeightCommitted((int)gridPos.x, (int)gridPos.y);
    }

    void DestroyPreviousZoning(GameObject cell)
    {
        if (cell.transform.childCount > 0)
        {
            var toDestroy = new List<GameObject>();
            foreach (Transform child in cell.transform)
                toDestroy.Add(child.gameObject);
            foreach (GameObject go in toDestroy)
                Destroy(go);
        }
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
    #endregion

    #region Building Validation and Placement
    /// <summary>
    /// Returns the reason why building placement would fail at this position, or null if placement would succeed.
    /// Used for debug UI and specific error messages.
    /// </summary>
    public string GetBuildingPlacementFailReason(Vector2 gridPosition, int buildingSize, bool isWaterPlant)
        => placementService.GetBuildingPlacementFailReason(gridPosition, buildingSize, isWaterPlant);

    /// <summary>
    /// Returns true if a building of the given size can be placed at the grid position. Infers water plant status from the currently selected building.
    /// </summary>
    /// <param name="gridPosition">Pivot grid coordinates for the building.</param>
    /// <param name="buildingSize">Footprint size of the building.</param>
    /// <returns>True if placement is valid.</returns>
    public bool canPlaceBuilding(Vector2 gridPosition, int buildingSize)
        => placementService.CanPlaceBuilding(gridPosition, buildingSize);

    /// <summary>
    /// Returns true if a building of the given size can be placed at the grid position, with explicit water plant flag.
    /// </summary>
    /// <param name="gridPosition">Pivot grid coordinates for the building.</param>
    /// <param name="buildingSize">Footprint size of the building.</param>
    /// <param name="isWaterPlant">Whether the building is a water plant (relaxed water adjacency rules).</param>
    /// <returns>True if placement is valid.</returns>
    public bool canPlaceBuilding(Vector2 gridPosition, int buildingSize, bool isWaterPlant)
        => placementService.CanPlaceBuilding(gridPosition, buildingSize, isWaterPlant);

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

    /// <summary>
    /// Place a building programmatically (e.g. auto resource planner). Caller is responsible for budget and affordability.
    /// Does not deduct money. Returns true if placed.
    /// </summary>
    public bool PlaceBuildingProgrammatic(Vector2 gridPos, IBuilding buildingTemplate)
        => placementService.PlaceBuildingProgrammatic(gridPos, buildingTemplate);
    #endregion

    #region Save and Restore
    /// <summary>Enable in Inspector or set to true to log RestoreGridCellVisuals details (roads, buildings, prefab misses).</summary>
    [Header("Load Debug")]
    [SerializeField] private bool restoreGridDebugLogs = false;

    /// <summary>
    /// Sets sorting order on a grass tile using grid coordinates directly, avoiding GetGridPosition
    /// which cannot account for height offset and would parent the tile to the wrong cell at h > 1.
    /// </summary>
    void SetGrassSortingOrderDirect(GameObject tile, int x, int y, int height, Cell cell)
    {
        if (terrainManager != null)
        {
            int sortingOrder = terrainManager.CalculateTerrainSortingOrder(x, y, height);
            SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.sortingOrder = sortingOrder;
            cell.SetCellInstanceSortingOrder(sortingOrder);
        }
    }

    static void ApplySavedSpriteSorting(GameObject obj, int sortingOrder)
    {
        if (obj == null) return;
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.sortingOrder = sortingOrder;
    }

    /// <summary>Lake/coast shore cells saved with Bay, water-slope prefabs, or a secondary shore prefab.</summary>
    static bool IsWaterShoreSavedCell(CellData cellData)
    {
        if (string.IsNullOrEmpty(cellData.prefabName))
            return false;
        return cellData.prefabName.Contains("Bay")
            || cellData.prefabName.Contains("SlopeWater")
            || cellData.prefabName.Contains("UpslopeWater")
            || !string.IsNullOrEmpty(cellData.secondaryPrefabName);
    }

    /// <summary>
    /// Restore order: water → terrain (grass/shore/slope) → zoning overlays → roads → building pivots/singles → multi-cell footprint non-pivots.
    /// </summary>
    int GetCellDataRestoreVisualPhase(CellData cellData)
    {
        if (zoneManager == null) return 1;
        Zone.ZoneType zoneType = zoneManager.GetZoneTypeFromZoneTypeString(cellData.zoneType);

        if (zoneType == Zone.ZoneType.Water) return 0;
        if (zoneType == Zone.ZoneType.Grass) return 1;
        if (ZoneManager.IsZoningType(zoneType)) return 2;
        if (zoneType == Zone.ZoneType.Road) return 3;
        if (IsZoneTypeBuilding(zoneType))
        {
            if (cellData.buildingSize > 1 && !cellData.isPivot) return 5;
            return 4;
        }
        return 1;
    }

    /// <summary>Stable sort of cell data for restore-visual pass so terrain exists before roads/buildings and pivot before footprint.</summary>
    List<CellData> SortCellDataForVisualRestore(List<CellData> gridData)
    {
        var list = new List<CellData>(gridData.Count);
        list.AddRange(gridData);
        list.Sort((a, b) =>
        {
            int pa = GetCellDataRestoreVisualPhase(a);
            int pb = GetCellDataRestoreVisualPhase(b);
            if (pa != pb) return pa.CompareTo(pb);
            if (a.y != b.y) return a.y.CompareTo(b.y);
            return a.x.CompareTo(b.x);
        });
        return list;
    }

    /// <summary>Finds building root on pivot cell after load (occupiedBuilding or children).</summary>
    GameObject FindBuildingRootOnPivotCell(Cell cell, GameObject gridCell)
    {
        if (cell == null || gridCell == null) return null;
        if (cell.occupiedBuilding != null) return cell.occupiedBuilding;
        PowerPlant pp = gridCell.GetComponentInChildren<PowerPlant>(true);
        if (pp != null) return pp.gameObject;
        WaterPlant wp = gridCell.GetComponentInChildren<WaterPlant>(true);
        if (wp != null) return wp.gameObject;
        foreach (Transform child in gridCell.transform)
        {
            Zone z = child.GetComponent<Zone>();
            if (z != null && z.zoneCategory == Zone.ZoneCategory.Building)
                return child.gameObject;
        }
        return null;
    }

    /// <summary>
    /// Re-applies sorting for all pivot buildings so neighbor caps match New Game after full terrain/water/roads restore.
    /// </summary>
    /// <returns>Number of buildings touched.</returns>
    int RecalculateBuildingSortingAfterLoad(out int sortOrderChangeCount)
    {
        sortOrderChangeCount = 0;
        if (cellArray == null || sortingService == null || gridArray == null) return 0;

        int touched = 0;
        var firstDeltas = new List<string>(5);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = cellArray[x, y];
                if (cell == null) continue;
                if (cell.buildingSize > 1 && !cell.isPivot) continue;
                if (!IsZoneTypeBuilding(cell.zoneType)) continue;

                int buildingSize = cell.buildingSize > 0 ? cell.buildingSize : 1;
                GameObject gridCell = gridArray[x, y];
                GameObject buildingGo = FindBuildingRootOnPivotCell(cell, gridCell);
                if (buildingGo == null) continue;

                int orderBefore = cell.sortingOrder;
                sortingService.SetZoneBuildingSortingOrder(buildingGo, x, y, buildingSize);
                touched++;
                if (cell.sortingOrder != orderBefore)
                {
                    sortOrderChangeCount++;
                    if (firstDeltas.Count < 5 && restoreGridDebugLogs)
                        firstDeltas.Add($"({x},{y}) pivotOrder {orderBefore} -> {cell.sortingOrder} size={buildingSize}");
                }
            }
        }

        if (restoreGridDebugLogs && firstDeltas.Count > 0)
        {
            Debug.Log($"[RestoreGrid] Building sort post-pass deltas (sample): {string.Join("; ", firstDeltas)}");
        }

        return touched;
    }

    void RestoreGridCellVisuals(CellData cellData, GameObject cell)
    {
        Zone.ZoneType zoneType = zoneManager.GetZoneTypeFromZoneTypeString(cellData.zoneType);

        cell.transform.position = new Vector3(cellData.transformPosition.x, cellData.transformPosition.y, cell.transform.position.z);
        cellArray[cellData.x, cellData.y].transformPosition = cellData.transformPosition;

        if (zoneType == Zone.ZoneType.Water && waterManager != null)
        {
            Cell cellComponent = cellArray[cellData.x, cellData.y];
            for (int i = cell.transform.childCount - 1; i >= 0; i--)
                Destroy(cell.transform.GetChild(i).gameObject);

            GameObject waterPrefab = waterManager.FindWaterPrefabByName(cellData.prefabName);
            if (waterPrefab == null)
            {
                Debug.LogWarning($"[RestoreGrid] Water prefab NOT FOUND for ({cellData.x},{cellData.y}) prefabName=\"{cellData.prefabName}\"");
                return;
            }

            int surfaceHeight = waterManager.GetWaterSurfaceHeight(cellData.x, cellData.y);
            if (surfaceHeight < 0)
                surfaceHeight = waterManager.seaLevel;

            int visualSurfaceHeight = Mathf.Max(TerrainManager.MIN_HEIGHT, surfaceHeight - 1);
            float halfCellHeight = tileHeight * 0.25f;
            Vector2 waterSurfaceWorld = GetWorldPositionVector(cellData.x, cellData.y, visualSurfaceHeight);
            Vector2 waterTileWorldPos = waterSurfaceWorld + new Vector2(0f, halfCellHeight);

            GameObject waterTile = Instantiate(waterPrefab, waterTileWorldPos, Quaternion.identity);
            Zone zone = waterTile.AddComponent<Zone>();
            zone.zoneType = Zone.ZoneType.Water;
            zone.zoneCategory = Zone.ZoneCategory.Water;
            waterTile.transform.SetParent(cell.transform);
            int waterSort = terrainManager != null
                ? terrainManager.CalculateTerrainSortingOrder(cellData.x, cellData.y, visualSurfaceHeight)
                : cellData.sortingOrder;
            ApplySavedSpriteSorting(waterTile, waterSort);
            cellComponent.SetCellInstanceSortingOrder(waterSort);
            if (restoreGridDebugLogs)
                Debug.Log($"[RestoreGrid] Water ({cellData.x},{cellData.y}) from save");
            return;
        }

        if (zoneType == Zone.ZoneType.Grass)
        {
            if (IsWaterShoreSavedCell(cellData) && terrainManager != null)
            {
                terrainManager.RestoreWaterShorePrefabsFromSave(
                    cellData.x, cellData.y,
                    cellData.prefabName,
                    cellData.secondaryPrefabName ?? "",
                    cellData.sortingOrder);
                if (forestManager != null)
                {
                    Forest.ForestType parsedForestType = Forest.ForestType.None;
                    if (!string.IsNullOrEmpty(cellData.forestType))
                        System.Enum.TryParse(cellData.forestType, out parsedForestType);
                    int forestOrder = cellArray[cellData.x, cellData.y].sortingOrder + 5;
                    forestManager.RestoreForestAt(cellData.x, cellData.y, parsedForestType, cellData.forestPrefabName, updateStats: false, savedSpriteSortingOrder: forestOrder);
                }
                return;
            }

            // Land slope cells (saved prefab name contains "Slope", not water-shore)
            if (!string.IsNullOrEmpty(cellData.prefabName) && cellData.prefabName.Contains("Slope"))
            {
                GameObject slopePrefab = terrainManager != null ? terrainManager.FindTerrainPrefabByName(cellData.prefabName) : null;
                if (slopePrefab != null && terrainManager != null)
                {
                    terrainManager.PlaceSlopeFromPrefab(cellData.x, cellData.y, slopePrefab, cellData.height);
                    foreach (Transform child in cell.transform)
                    {
                        ApplySavedSpriteSorting(child.gameObject, cellData.sortingOrder);
                        break;
                    }
                    cellArray[cellData.x, cellData.y].SetCellInstanceSortingOrder(cellData.sortingOrder);
                }
                else
                {
                    Debug.LogWarning($"[RestoreGrid] Slope prefab NOT FOUND for ({cellData.x},{cellData.y}) prefabName=\"{cellData.prefabName}\" - using grass fallback");
                    GameObject fallbackGrass = zoneManager.GetRandomZonePrefab(Zone.ZoneType.Grass, 1)
                        ?? (zoneManager.grassPrefabs != null && zoneManager.grassPrefabs.Count > 0 ? zoneManager.grassPrefabs[0] : null);
                    if (fallbackGrass != null)
                    {
                        for (int i = cell.transform.childCount - 1; i >= 0; i--)
                            Destroy(cell.transform.GetChild(i).gameObject);
                        Cell cc = cellArray[cellData.x, cellData.y];
                        GameObject zoneTile = Instantiate(fallbackGrass, cc.transformPosition, Quaternion.identity);
                        zoneTile.transform.SetParent(cell.transform);
                        ApplySavedSpriteSorting(zoneTile, cellData.sortingOrder);
                        cc.SetCellInstanceSortingOrder(cellData.sortingOrder);
                        Zone z = zoneTile.GetComponent<Zone>();
                        if (z == null) z = zoneTile.AddComponent<Zone>();
                        z.zoneType = Zone.ZoneType.Grass;
                    }
                }
                if (forestManager != null)
                {
                    Forest.ForestType parsedForestType = Forest.ForestType.None;
                    if (!string.IsNullOrEmpty(cellData.forestType))
                        System.Enum.TryParse(cellData.forestType, out parsedForestType);
                    int forestOrder = cellData.sortingOrder + 5;
                    forestManager.RestoreForestAt(cellData.x, cellData.y, parsedForestType, cellData.forestPrefabName, updateStats: false, savedSpriteSortingOrder: forestOrder);
                }
                return;
            }

            // Replace CreateGrid grass with saved prefab (or place if missing).
            // Never destroy without placing: only destroy when we have a valid prefab to place.
            GameObject grassPrefab = terrainManager != null ? terrainManager.FindTerrainPrefabByName(cellData.prefabName) : null;
            if (grassPrefab == null)
                grassPrefab = zoneManager.FindPrefabByName(cellData.prefabName);
            if (grassPrefab == null)
                grassPrefab = zoneManager.GetRandomZonePrefab(Zone.ZoneType.Grass, 1);
            if (grassPrefab == null)
                grassPrefab = zoneManager.GetRandomZonePrefab(Zone.ZoneType.Grass, Mathf.Clamp(cellData.height, 1, 5));
            if (grassPrefab == null && zoneManager.grassPrefabs != null && zoneManager.grassPrefabs.Count > 0)
                grassPrefab = zoneManager.grassPrefabs[0];

            if (grassPrefab != null)
            {
                // Destroy existing base tile only when we have a valid prefab to place
                for (int i = cell.transform.childCount - 1; i >= 0; i--)
                    Destroy(cell.transform.GetChild(i).gameObject);

                Cell cellComponent = cellArray[cellData.x, cellData.y];
                GameObject zoneTile = Instantiate(grassPrefab, cellComponent.transformPosition, Quaternion.identity);
                zoneTile.transform.SetParent(cell.transform);
                ApplySavedSpriteSorting(zoneTile, cellData.sortingOrder);
                cellComponent.SetCellInstanceSortingOrder(cellData.sortingOrder);

                Zone zoneComponent = zoneTile.GetComponent<Zone>();
                if (zoneComponent == null)
                {
                    zoneComponent = zoneTile.AddComponent<Zone>();
                    zoneComponent.zoneType = Zone.ZoneType.Grass;
                }
            }
            else
            {
                Debug.LogWarning($"[RestoreGrid] Grass prefab NOT FOUND for ({cellData.x},{cellData.y}) height={cellData.height} prefabName=\"{cellData.prefabName}\" - keeping CreateGrid base tile");
            }

            if (forestManager != null)
            {
                Forest.ForestType parsedForestType = Forest.ForestType.None;
                if (!string.IsNullOrEmpty(cellData.forestType))
                    System.Enum.TryParse(cellData.forestType, out parsedForestType);
                int forestOrder = cellData.sortingOrder + 5;
                forestManager.RestoreForestAt(cellData.x, cellData.y, parsedForestType, cellData.forestPrefabName, updateStats: false, savedSpriteSortingOrder: forestOrder);
            }
            return;
        }

        GameObject tilePrefab = zoneManager.FindPrefabByName(cellData.prefabName);
        if (tilePrefab == null && terrainManager != null)
            tilePrefab = terrainManager.FindTerrainPrefabByName(cellData.prefabName);

        // Fallback for Road: use default road prefab when saved prefab not found (avoids black squares)
        if (tilePrefab == null && zoneType == Zone.ZoneType.Road && roadManager != null)
        {
            var roadPrefabs = roadManager.GetRoadPrefabs();
            if (roadPrefabs != null && roadPrefabs.Count > 0)
                tilePrefab = roadPrefabs[0];
        }

        if (tilePrefab == null)
        {
            Debug.LogWarning($"[RestoreGrid] Prefab NOT FOUND for ({cellData.x},{cellData.y}) zoneType={cellData.zoneType} prefabName=\"{cellData.prefabName}\"");
        }

        if (tilePrefab != null)
        {
            bool isMultiCellUtilityBuilding = cellData.buildingSize > 1 && cellData.isPivot
                && (tilePrefab.GetComponent<PowerPlant>() != null || tilePrefab.GetComponent<WaterPlant>() != null);

            if (isMultiCellUtilityBuilding)
            {
                if (restoreGridDebugLogs)
                    Debug.Log($"[RestoreGrid] Building ({cellData.x},{cellData.y}) prefab={cellData.prefabName} size={cellData.buildingSize}");
                placementService.RestoreBuildingTile(tilePrefab, new Vector2(cellData.x, cellData.y), cellData.buildingSize);
            }
            else if (zoneType == Zone.ZoneType.Road && roadManager != null)
            {
                if (restoreGridDebugLogs)
                    Debug.Log($"[RestoreGrid] Road ({cellData.x},{cellData.y}) prefab={cellData.prefabName} interstate={cellData.isInterstate} height={cellData.height}");
                roadManager.RestoreRoadTile(new Vector2Int(cellData.x, cellData.y), tilePrefab, cellData.isInterstate, cellData.sortingOrder);
            }
            else if (ZoneManager.IsZoningType(zoneType))
            {
                zoneManager.RestoreZoneTile(tilePrefab, cell, zoneType);
            }
            else if (!(cellData.buildingSize > 1 && !cellData.isPivot))
            {
                zoneManager.PlaceZoneBuildingTile(tilePrefab, cell, cellData.buildingSize);
                Cell cellComponent = cellArray[cellData.x, cellData.y];
                PowerPlant powerPlant = cell.GetComponentInChildren<PowerPlant>();
                WaterPlant waterPlant = cell.GetComponentInChildren<WaterPlant>();
                UpdatePlacedBuildingCellAttributes(cellComponent, cellData.buildingSize, powerPlant, waterPlant, tilePrefab, zoneType, null);
                if (powerPlant != null)
                    cityStats.RegisterPowerPlant(powerPlant);
                if (waterPlant != null && waterManager != null)
                {
                    waterManager.RegisterWaterPlant(waterPlant);
                    cityStats.cityWaterOutput = waterManager.GetTotalWaterOutput();
                }
            }
        }

        zoneManager.addZonedTileToList(new Vector2(cellData.x, cellData.y), zoneType);

        // Restore forest state
        if (forestManager != null)
        {
            Forest.ForestType parsedForestType = Forest.ForestType.None;
            if (!string.IsNullOrEmpty(cellData.forestType))
                System.Enum.TryParse(cellData.forestType, out parsedForestType);
            int forestOrder = cellData.sortingOrder + 5;
            forestManager.RestoreForestAt(cellData.x, cellData.y, parsedForestType, cellData.forestPrefabName, updateStats: false, savedSpriteSortingOrder: forestOrder);
        }
    }

    /// <summary>
    /// Restores the grid from saved data, re-placing all zones and buildings and recalculating available zoned sections.
    /// Two-pass: first restores all cell data (positions, attributes), then places tiles so building placement
    /// uses correct transformPosition and runtime references are not overwritten.
    /// </summary>
    /// <param name="gridData">List of serialized cell data from a save file.</param>
    public void RestoreGrid(List<CellData> gridData)
    {
        if (gridData == null || cellArray == null) return;

        foreach (CellData cellData in gridData)
        {
            if (cellData.x < 0 || cellData.x >= width || cellData.y < 0 || cellData.y >= height)
                continue;
            cellArray[cellData.x, cellData.y].SetCellData(cellData);
        }

        List<CellData> sortedForVisuals = SortCellDataForVisualRestore(gridData);

        int roadsInSave = 0;
        int buildingsInSave = 0;
        foreach (CellData cellData in gridData)
        {
            if (cellData.x < 0 || cellData.x >= width || cellData.y < 0 || cellData.y >= height)
                continue;
            if (cellData.zoneType == "Road") roadsInSave++;
            if (cellData.buildingSize > 1 && cellData.isPivot && (cellData.buildingType == "PowerPlant" || cellData.buildingType == "WaterPlant")) buildingsInSave++;
        }

        foreach (CellData cellData in sortedForVisuals)
        {
            if (cellData.x < 0 || cellData.x >= width || cellData.y < 0 || cellData.y >= height)
                continue;

            GameObject cell = gridArray[cellData.x, cellData.y];
            RestoreGridCellVisuals(cellData, cell);
        }

        int postPassPivotOrderChanges = 0;
        int buildingsSortRecalculated = RecalculateBuildingSortingAfterLoad(out postPassPivotOrderChanges);

        if (forestManager != null)
            forestManager.RefreshForestStatistics();

        InvalidateRoadCache();
        onGridRestored?.Invoke();
        zoneManager.CalculateAvailableSquareZonedSections();

        // Post-load diagnostic: count empty cells and safety net
        RunPostLoadDiagnosticAndSafetyNet();

        skipChunkCullingFramesRemaining = 3;

        if (restoreGridDebugLogs)
        {
            Debug.Log($"[RestoreGrid] Complete. roadsInSave={roadsInSave} multiCellUtilityBuildingsInSave={buildingsInSave} " +
                      $"buildingSortPostPassTouched={buildingsSortRecalculated} pivotOrderChanges={postPassPivotOrderChanges}. Check for prefab NOT FOUND warnings above.");
        }
    }

    void RunPostLoadDiagnosticAndSafetyNet()
    {
        if (cellArray == null || zoneManager == null) return;

        int emptyCount = 0;
        var firstEmpty = new System.Collections.Generic.List<string>(10);
        GameObject fallbackGrass = zoneManager.GetRandomZonePrefab(Zone.ZoneType.Grass, 1)
            ?? (zoneManager.grassPrefabs != null && zoneManager.grassPrefabs.Count > 0 ? zoneManager.grassPrefabs[0] : null);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cellComponent = cellArray[x, y];
                if (cellComponent == null) continue;

                if (cellComponent.zoneType == Zone.ZoneType.Grass && gridArray[x, y].transform.childCount == 0)
                {
                    emptyCount++;
                    if (firstEmpty.Count < 10)
                        firstEmpty.Add($"({x},{y}) h={cellComponent.height} prefab=\"{cellComponent.prefabName}\"");

                    if (fallbackGrass != null)
                    {
                        GameObject zoneTile = Instantiate(fallbackGrass, cellComponent.transformPosition, Quaternion.identity);
                        zoneTile.transform.SetParent(gridArray[x, y].transform);
                        SetGrassSortingOrderDirect(zoneTile, x, y, cellComponent.height, cellComponent);
                        Zone z = zoneTile.GetComponent<Zone>();
                        if (z == null) { z = zoneTile.AddComponent<Zone>(); z.zoneType = Zone.ZoneType.Grass; }
                    }
                }
            }
        }

        if (emptyCount > 0)
        {
            Debug.LogWarning($"[RestoreGrid] Found {emptyCount} empty Grass cells. First 10: {string.Join(", ", firstEmpty)}. Safety net placed grass where possible.");
        }
    }

    /// <summary>
    /// Destroys all chunks and recreates a fresh empty grid, clearing all zoned positions.
    /// </summary>
    public void ResetGrid()
    {
        if (chunkObjects != null)
        {
            for (int cx = 0; cx < chunksX; cx++)
            {
                for (int cy = 0; cy < chunksY; cy++)
                {
                    if (chunkObjects[cx, cy] != null)
                        Destroy(chunkObjects[cx, cy]);
                }
            }
        }

        zoneManager.ClearZonedPositions();
        InvalidateRoadCache();
        onGridRestored?.Invoke();

        cellArray = null;
        CreateGrid();
    }

    /// <summary>
    /// Clears the grid generated by InitializeGeography and recreates a fresh empty grid.
    /// Used at the start of LoadGame so restoration runs on a clean slate instead of
    /// overwriting randomly-initialized terrain. Does not invoke onGridRestored (RestoreGrid will).
    /// </summary>
    public void ResetGridForLoad()
    {
        if (chunkObjects != null)
        {
            for (int cx = 0; cx < chunksX; cx++)
            {
                for (int cy = 0; cy < chunksY; cy++)
                {
                    if (chunkObjects[cx, cy] != null)
                        Destroy(chunkObjects[cx, cy]);
                }
            }
        }

        zoneManager.ClearZonedPositions();
        InvalidateRoadCache();

        cellArray = null;
        CreateGrid(createBaseTiles: true);

        if (chunkCulling != null)
        {
            chunkCulling.chunkObjects = chunkObjects;
            chunkCulling.chunkActiveState = chunkActiveState;
            chunkCulling.chunksX = chunksX;
            chunkCulling.chunksY = chunksY;
            // Ensure all chunks start visible after load (CreateGrid sets chunkActiveState true, but reset explicitly for safety)
            int maxCx = Mathf.Min(chunksX, chunkObjects != null ? chunkObjects.GetLength(0) : 0);
            int maxCy = Mathf.Min(chunksY, chunkObjects != null ? chunkObjects.GetLength(1) : 0);
            for (int cx = 0; cx < maxCx; cx++)
            {
                for (int cy = 0; cy < maxCy; cy++)
                {
                    if (chunkActiveState != null && cx < chunkActiveState.GetLength(0) && cy < chunkActiveState.GetLength(1))
                        chunkActiveState[cx, cy] = true;
                    if (chunkObjects[cx, cy] != null)
                        chunkObjects[cx, cy].SetActive(true);
                }
            }
        }
    }

    /// <summary>
    /// Returns the Cell at the given grid coordinates, or null if out of bounds.
    /// </summary>
    /// <param name="x">Grid X coordinate.</param>
    /// <param name="y">Grid Y coordinate.</param>
    /// <returns>The Cell component, or null.</returns>
    public Cell GetCell(int x, int y)
    {
        if (x >= 0 && x < width && y >= 0 && y < height)
        {
            return cellArray[x, y];
        }
        return null;
    }

    /// <summary>
    /// Serializes every cell in the grid into a list of CellData for saving.
    /// </summary>
    /// <returns>A list containing one CellData per grid cell.</returns>
    public List<CellData> GetGridData()
    {
        List<CellData> gridData = new List<CellData>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cellComponent = cellArray[x, y];
                CellData cellData = cellComponent.GetCellData();
                gridData.Add(cellData);
            }
        }

        return gridData;
    }

    /// <summary>
    /// Returns true if the cell is on the outer edge of the grid (first or last row/column).
    /// </summary>
    /// <param name="x">Grid X coordinate.</param>
    /// <param name="y">Grid Y coordinate.</param>
    /// <returns>True if the cell lies on the grid border.</returns>
    public bool isBorderCell(int x, int y)
    {
        if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
        {
            return true;
        }
        return false;
    }
    #endregion

    #region Road Cache and Pathfinding
    /// <summary>
    /// Marks the cached road positions as stale so they are rebuilt on the next query.
    /// </summary>
    public void InvalidateRoadCache()
        => roadCache.Invalidate();

    /// <summary>
    /// Adds a road position to the cache incrementally. Call when a road tile is placed.
    /// </summary>
    public void AddRoadToCache(Vector2Int pos)
        => roadCache.AddRoad(pos);

    /// <summary>
    /// Removes a road position from the cache incrementally. Call when a road tile is demolished.
    /// </summary>
    public void RemoveRoadFromCache(Vector2Int pos)
        => roadCache.RemoveRoad(pos);

    /// <summary>
    /// Returns all grid positions that contain a road, using a lazily rebuilt cache.
    /// </summary>
    /// <returns>Cached list of road positions.</returns>
    public List<Vector2Int> GetAllRoadPositions()
        => roadCache.GetAllRoadPositions();

    /// <summary>
    /// Returns road positions as a HashSet for O(1) Contains lookups.
    /// </summary>
    public HashSet<Vector2Int> GetRoadPositionsAsHashSet()
        => roadCache.GetRoadPositionsAsHashSet();

    /// <summary>
    /// Returns road positions that have at least one expandable (grass/forest/sea-level) cardinal neighbor, i.e. the road frontier.
    /// </summary>
    /// <returns>Cached list of road edge positions.</returns>
    public List<Vector2Int> GetRoadEdgePositions()
        => roadCache.GetRoadEdgePositions();

    /// <summary>Returns cells that are one step beyond each road edge in the natural extension direction. AutoZoningManager must not zone these.</summary>
    public HashSet<Vector2Int> GetRoadExtensionCells()
        => roadCache.GetRoadExtensionCells();

    /// <summary>Number of cardinal neighbors of (gx,gy) that are zoneable (Grass, Forest, or Flat/N-S/E-W slope). Used for road-reservation in auto-zoning.</summary>
    public int CountGrassNeighbors(int gx, int gy)
        => roadCache.CountGrassNeighbors(gx, gy);

    /// <summary>Number of cardinal neighbors of (gx,gy) that are roads. Used to identify axial termini for reserving perpendicular road generation.</summary>
    public int CountRoadNeighbors(int gx, int gy)
        => roadCache.CountRoadNeighbors(gx, gy);

    /// <summary>True if this neighbor cell is valid for zoning (Grass, Forest, or Flat/N-S/E-W slope).</summary>
    public bool IsZoneableNeighbor(Cell c, int x, int y)
        => roadCache.IsZoneableNeighbor(c, x, y);

    /// <summary>True if at least one of the 4 cardinal neighbors of (x,y) is a road.</summary>
    public bool IsAdjacentToRoad(int x, int y)
        => roadCache.IsAdjacentToRoad(x, y);

    /// <summary>Returns all grid cells within maxDistance (Manhattan) of any road. Cached and invalidated when roads change.</summary>
    public HashSet<Vector2Int> GetCellsWithinDistanceOfRoad(int maxDistance)
        => roadCache.GetCellsWithinDistanceOfRoad(maxDistance);

    /// <summary>True if (x,y) is within maxDistance (Manhattan) of any road cell.</summary>
    public bool IsWithinDistanceOfRoad(int x, int y, int maxDistance)
        => roadCache.IsWithinDistanceOfRoad(x, y, maxDistance);

    /// <summary>
    /// A* path over walkable cells (grass or road). Prefers flat terrain; cardinal slopes cost more; diagonal slopes are impassable.
    /// Max 200 nodes explored. Returns path including start and end, or empty if not found.
    /// </summary>
    public List<Vector2Int> FindPath(Vector2Int from, Vector2Int to)
        => pathfinder.FindPath(from, to);

    /// <summary>
    /// A* path with optional extra cost for cells close to existing roads, so paths tend to keep minDistanceFromRoad cells away and leave space for zones.
    /// When minDistanceFromRoad is 0, behaves like FindPath. When &gt; 0, adds penalty for stepping on cells within that Manhattan distance of any road.
    /// </summary>
    public List<Vector2Int> FindPathWithRoadSpacing(Vector2Int from, Vector2Int to, int minDistanceFromRoad)
        => pathfinder.FindPathWithRoadSpacing(from, to, minDistanceFromRoad);
    #endregion
}
}
