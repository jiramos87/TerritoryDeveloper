using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System;

public class GridManager : MonoBehaviour
{
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

    public int width, height;
    public float tileWidth = 1f; // Full width of the tile
    public float tileHeight = 0.5f; // Effective height due to isometric perspective

    public float halfWidth;
    public float halfHeight;
    public GameObject[,] gridArray;
    public Vector2 mouseGridPosition;
    /// <summary>Last grid cell clicked (left or right button). (-1,-1) if none yet.</summary>
    public Vector2 selectedPoint = new Vector2(-1, -1);
    public int mouseGridHeight;
    public int mouseGridSortingOrder;

    public bool isInitialized = false;

    [Header("Chunk Culling")]
    public int chunkSize = 16;
    private GameObject[,] chunkObjects;
    private bool[,] chunkActiveState;
    private int chunksX, chunksY;
    private Camera cachedCamera;
    private Vector3 lastCameraPosition;
    private float lastOrthoSize;

    public void InitializeGrid()
    {
        halfWidth = tileWidth / 2f;
        halfHeight = tileHeight / 2f;

        // Default to 64x64 when not set in Inspector (balance between area and performance)
        if (width <= 0) width = 64;
        if (height <= 0) height = 64;

        if (zoneManager == null)
        {
            zoneManager = FindObjectOfType<ZoneManager>();
            if (zoneManager != null)
            {
                zoneManager.InitializeZonePrefabs();
            }
        }

        if (zoneManager == null)
        {
            zoneManager.InitializeZonePrefabs();
        }

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
        {
            roadManager = FindObjectOfType<RoadManager>();
            roadManager.Initialize();
        }

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
        CreateGrid();
        terrainManager.InitializeHeightMap();
        isInitialized = true;
        Vector3 centerWorldPosition = GetWorldPosition(
            width / 2, height / 2
        );

        cameraController.MoveCameraToMapCenter(centerWorldPosition);
    }

    void CreateGrid()
    {
        if (!zoneManager)
        {
            zoneManager = FindObjectOfType<ZoneManager>();
        }

        gridArray = new GameObject[width, height];

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

                cellData.prefab = tilePrefab;
                cellData.prefabName = tilePrefab.name;

                Cell cellComponent = gridCell.AddComponent<Cell>();
                cellComponent.SetCellData(cellData);

                gridArray[x, y] = gridCell;

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

            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
                selectedPoint = mouseGridPosition;

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
        UpdateChunkVisibility();
    }

    void UpdateChunkVisibility()
    {
        if (cachedCamera == null) cachedCamera = Camera.main;
        if (cachedCamera == null) return;

        Vector3 camPos = cachedCamera.transform.position;
        float orthoSize = cachedCamera.orthographicSize;
        if (camPos == lastCameraPosition && Mathf.Approximately(orthoSize, lastOrthoSize))
            return;
        lastCameraPosition = camPos;
        lastOrthoSize = orthoSize;

        float halfH = orthoSize;
        float halfW = orthoSize * cachedCamera.aspect;
        Vector2 bl = new Vector2(camPos.x - halfW, camPos.y - halfH);
        Vector2 br = new Vector2(camPos.x + halfW, camPos.y - halfH);
        Vector2 tl = new Vector2(camPos.x - halfW, camPos.y + halfH);
        Vector2 tr = new Vector2(camPos.x + halfW, camPos.y + halfH);

        int minGridX = Mathf.Min((int)GetGridPosition(bl).x, (int)GetGridPosition(br).x, (int)GetGridPosition(tl).x, (int)GetGridPosition(tr).x);
        int maxGridX = Mathf.Max((int)GetGridPosition(bl).x, (int)GetGridPosition(br).x, (int)GetGridPosition(tl).x, (int)GetGridPosition(tr).x);
        int minGridY = Mathf.Min((int)GetGridPosition(bl).y, (int)GetGridPosition(br).y, (int)GetGridPosition(tl).y, (int)GetGridPosition(tr).y);
        int maxGridY = Mathf.Max((int)GetGridPosition(bl).y, (int)GetGridPosition(br).y, (int)GetGridPosition(tl).y, (int)GetGridPosition(tr).y);

        int minCX = Mathf.Max(0, minGridX / chunkSize - 1);
        int maxCX = Mathf.Min(chunksX - 1, maxGridX / chunkSize + 1);
        int minCY = Mathf.Max(0, minGridY / chunkSize - 1);
        int maxCY = Mathf.Min(chunksY - 1, maxGridY / chunkSize + 1);

        for (int cx = 0; cx < chunksX; cx++)
        {
            for (int cy = 0; cy < chunksY; cy++)
            {
                bool shouldBeActive = cx >= minCX && cx <= maxCX && cy >= minCY && cy <= maxCY;
                if (chunkActiveState[cx, cy] != shouldBeActive)
                {
                    chunkActiveState[cx, cy] = shouldBeActive;
                    chunkObjects[cx, cy].SetActive(shouldBeActive);
                }
            }
        }
    }

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
        Cell cell = gridArray[x, y].GetComponent<Cell>();
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
    /// Returns the maximum sorting order that any content on the cell at (x,y) would have
    /// (terrain, forest +5, road +3, building +10, etc.). Used so the building can place itself
    /// behind "front" adjacent cells and let forest/terrain draw on top.
    /// </summary>
    private int GetCellMaxContentSortingOrder(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return int.MinValue;
        if (terrainManager == null) return int.MinValue;

        GameObject cellObj = gridArray[x, y];
        Cell cell = cellObj != null ? cellObj.GetComponent<Cell>() : null;
        if (cell == null) return int.MinValue;

        int terrainOrder = terrainManager.CalculateTerrainSortingOrder(x, y, cell.height);
        int maxOrder = terrainOrder;

        if (cell.GetComponent<SpriteRenderer>() != null)
            maxOrder = Mathf.Max(maxOrder, terrainOrder);

        for (int i = 0; i < cellObj.transform.childCount; i++)
        {
            GameObject child = cellObj.transform.GetChild(i).gameObject;
            if (child.GetComponent<SpriteRenderer>() == null) continue;

            int order;
            if (terrainManager.IsWaterSlopeObject(child))
                order = terrainManager.CalculateWaterSlopeSortingOrder(x, y);
            else if (cell.forestObject != null && cell.forestObject == child)
                order = terrainOrder + 5;
            else
            {
                Zone zone = child.GetComponent<Zone>();
                if (zone != null)
                {
                    if (zone.zoneCategory == Zone.ZoneCategory.Zoning) order = terrainOrder + 0;
                    else if (zone.zoneType == Zone.ZoneType.Road) order = terrainOrder + ROAD_SORTING_OFFSET;
                    else if (zone.zoneCategory == Zone.ZoneCategory.Building) order = terrainOrder + 10;
                    else order = terrainOrder;
                }
                else
                    order = terrainOrder;
            }
            maxOrder = Mathf.Max(maxOrder, order);
        }
        return maxOrder;
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
                    Cell pivotCandidate = gridArray[px, py].GetComponent<Cell>();
                    if (pivotCandidate != null && pivotCandidate.isPivot)
                        return gridArray[px, py];
                }
            }
        }
        return gridArray[cx, cy];
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
        Cell cellComponent = cell.GetComponent<Cell>();
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
            DestroyImmediate(t.gameObject);

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

    void BulldozeBuildingTiles(GameObject cell, bool showAnimation = true)
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

        if (buildingSize > 1)
        {
            // If clicked cell is not pivot, find pivot to get correct footprint
            GameObject pivotCellObj = cellComponent.isPivot ? cell : GetBuildingPivotCell(cellComponent);
            if (pivotCellObj == null)
                pivotCellObj = cell;
            Cell pivotCell = pivotCellObj.GetComponent<Cell>();

            GetBuildingFootprintOffset(buildingSize, out int offsetX, out int offsetY);

            for (int x = 0; x < buildingSize; x++)
            {
                for (int y = 0; y < buildingSize; y++)
                {
                    int gridX = (int)pivotCell.x + x - offsetX;
                    int gridY = (int)pivotCell.y + y - offsetY;

                    if (gridX >= 0 && gridX < width && gridY >= 0 && gridY < height)
                    {
                        GameObject adjacentCell = gridArray[gridX, gridY];
                        BulldozeTileWithoutAnimation(adjacentCell);
                    }
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

        BulldozeBuildingTiles(cell, showAnimation);
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
        Cell cellComponent = cell.GetComponent<Cell>();
        if (cellComponent == null || !CanBulldoze(cellComponent)) return false;

        Zone.ZoneType zoneType = cellComponent.zoneType;
        HandleBulldozeTile(zoneType, cell, showAnimation);
        if (!showAnimation)
            InvalidateRoadCache();
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
            GameObject cell = gridArray[(int)gridPosition.x, (int)gridPosition.y];
            Cell cellComponent = cell.GetComponent<Cell>();

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
            PlaceBuilding(gridPosition, selectedBuilding);
        }
    }

    void HandleForestPlacement(Vector2 gridPosition, IForest selectedForest)
    {
        if (Input.GetMouseButtonDown(0))
        {
            forestManager.PlaceForest(gridPosition, selectedForest);
        }
    }

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

    public void DestroyCellChildren(GameObject cell, Vector2 gridPosition)
    {
        DestroyCellChildren(cell, gridPosition, null);
    }

    /// <summary>
    /// Destroys all children of the cell except the optional exclude object (e.g. the building being placed).
    /// Does not destroy terrain (flat grass) or slope children (land/water slope).
    /// Used by DemolishCellAt (including expropriation with showAnimation: false): any child with
    /// ZoneCategory.Zoning is removed from the zone manager lists (removeZonedPositionFromList) before destroy.
    /// </summary>
    public void DestroyCellChildren(GameObject cell, Vector2 gridPosition, GameObject excludeFromDestroy)
    {
        if (cell.transform.childCount == 0) return;

        foreach (Transform child in cell.transform)
        {
            if (excludeFromDestroy != null && child.gameObject == excludeFromDestroy)
                continue;

            // Do not destroy flat terrain (grass) or slope tiles (land or water)
            Zone zone = child.GetComponent<Zone>();
            if (zone != null && zone.zoneType == Zone.ZoneType.Grass)
                continue;
            if (terrainManager != null && (terrainManager.IsWaterSlopeObject(child.gameObject) || terrainManager.IsLandSlopeObject(child.gameObject)))
                continue;

            if (zone != null && zone.zoneCategory == Zone.ZoneCategory.Zoning)
            {
                zoneManager.removeZonedPositionFromList(gridPosition, zone.zoneType);
            }

            DestroyImmediate(child.gameObject);
        }
    }

    /// <summary>
    /// Same as DestroyCellChildren but preserves the cell's forest object so zoning can be merged with forest.
    /// </summary>
    public void DestroyCellChildrenExceptForest(GameObject cell, Vector2 gridPosition)
    {
        if (cell.transform.childCount == 0) return;

        Cell cellComponent = cell.GetComponent<Cell>();
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
            DestroyImmediate(child.gameObject);
        }
    }

    public void UpdateCellAttributes(Cell cellComponent, Zone.ZoneType selectedZoneType, ZoneAttributes zoneAttributes, GameObject prefab, int buildingSize)
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

    public int SetTileSortingOrder(GameObject tile, Zone.ZoneType zoneType = Zone.ZoneType.Grass)
    {
        Vector3 gridPos = GetGridPosition(tile.transform.position);

        int x = (int)gridPos.x;
        int y = (int)gridPos.y;
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return -1001;
        }

        Cell cell = gridArray[x, y].GetComponent<Cell>();
        tile.transform.SetParent(cell.gameObject.transform);
        SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();

        int baseSortingOrder = (y * width + x);

        int sortingOrder;
        switch (zoneType)
        {
            case Zone.ZoneType.Grass:
                sortingOrder = -(baseSortingOrder + 100000);
                break;
            default:
                sortingOrder = -(baseSortingOrder + 50000);
                break;
        }
        sr.sortingOrder = sortingOrder;
        cell.SetCellInstanceSortingOrder(sortingOrder);
        return sortingOrder;
    }

    /// <summary>
    /// Sets sorting order for a zoning tile (RCI overlay) using TerrainManager so it renders below forest and buildings.
    /// </summary>
    public void SetZoningTileSortingOrder(GameObject tile, int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;

        Cell cell = gridArray[x, y].GetComponent<Cell>();
        if (cell == null) return;

        tile.transform.SetParent(cell.gameObject.transform);

        if (terrainManager == null)
        {
            SetTileSortingOrder(tile, Zone.ZoneType.Grass);
            return;
        }

        int cellHeight = cell.height;
        if (terrainManager.GetHeightMap() != null)
            cellHeight = terrainManager.GetHeightMap().GetHeight(x, y);

        int sortingOrder = terrainManager.CalculateTerrainSortingOrder(x, y, cellHeight);

        SpriteRenderer[] renderers = tile.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sr in renderers)
        {
            if (sr != null)
                sr.sortingOrder = sortingOrder;
        }
    }

    /// <summary>
    /// Sets sorting order for a zone building (RCI) tile using TerrainManager so it renders above forest and terrain.
    /// </summary>
    public void SetZoneBuildingSortingOrder(GameObject tile, int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;

        Cell cell = gridArray[x, y].GetComponent<Cell>();
        if (cell == null) return;

        tile.transform.SetParent(cell.gameObject.transform);

        if (terrainManager == null)
        {
            SetTileSortingOrder(tile, Zone.ZoneType.Building);
            return;
        }

        int cellHeight = cell.height;
        if (terrainManager.GetHeightMap() != null)
            cellHeight = terrainManager.GetHeightMap().GetHeight(x, y);

        int sortingOrder = terrainManager.CalculateBuildingSortingOrder(x, y, cellHeight);

        SpriteRenderer[] renderers = tile.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sr in renderers)
        {
            if (sr != null)
                sr.sortingOrder = sortingOrder;
        }
        cell.SetCellInstanceSortingOrder(sortingOrder);
    }

    /// <summary>
    /// Sets sorting order for a multi-cell building using the maximum order over its footprint so the whole building renders in front of all covered terrain.
    /// </summary>
    public void SetZoneBuildingSortingOrder(GameObject tile, int pivotX, int pivotY, int buildingSize)
    {
        if (buildingSize <= 1)
        {
            SetZoneBuildingSortingOrder(tile, pivotX, pivotY);
            return;
        }
        if (pivotX < 0 || pivotX >= width || pivotY < 0 || pivotY >= height) return;
        Cell pivotCell = gridArray[pivotX, pivotY].GetComponent<Cell>();
        if (pivotCell == null) return;
        tile.transform.SetParent(pivotCell.gameObject.transform);
        if (terrainManager == null)
        {
            SetTileSortingOrder(tile, Zone.ZoneType.Building);
            return;
        }
        GetBuildingFootprintOffset(buildingSize, out int offsetX, out int offsetY);
        int minFx = pivotX - offsetX;
        int minFy = pivotY - offsetY;
        int maxFx = minFx + buildingSize - 1;
        int maxFy = minFy + buildingSize - 1;

        int maxOrder = int.MinValue;

        for (int x = 0; x < buildingSize; x++)
        {
            for (int y = 0; y < buildingSize; y++)
            {
                int gridX = pivotX + x - offsetX;
                int gridY = pivotY + y - offsetY;
                if (gridX < 0 || gridX >= width || gridY < 0 || gridY >= height) continue;
                Cell cell = gridArray[gridX, gridY].GetComponent<Cell>();
                if (cell == null) continue;
                int cellHeight = cell.height;
                if (terrainManager.GetHeightMap() != null)
                    cellHeight = terrainManager.GetHeightMap().GetHeight(gridX, gridY);
                int order = terrainManager.CalculateBuildingSortingOrder(gridX, gridY, cellHeight);
                if (order > maxOrder) maxOrder = order;
            }
        }
        if (maxOrder == int.MinValue) return;

        // Front = left or top. Back = south-east face only: right column (ax==maxFx+1, ay>=minFy) and bottom row (ay==maxFy+1).
        // This explicitly excludes top-right corner (e.g. 29,8) so the floor is driven only by (29,9),(29,10),(29,11) and bottom row.
        int minFrontAdjacentContentOrder = int.MaxValue;
        int maxBackAdjacentContentOrder = int.MinValue;
        for (int ax = minFx - 1; ax <= maxFx + 1; ax++)
        {
            for (int ay = minFy - 1; ay <= maxFy + 1; ay++)
            {
                if (ax >= minFx && ax <= maxFx && ay >= minFy && ay <= maxFy) continue;
                if (ax < 0 || ax >= width || ay < 0 || ay >= height) continue;
                int contentOrder = GetCellMaxContentSortingOrder(ax, ay);
                if (contentOrder == int.MinValue) continue;
                bool isFront = (ax < minFx) || (ay < minFy);
                bool isBackSouthEast = (ax == maxFx + 1 && ay >= minFy && ay <= maxFy + 1) || (ay == maxFy + 1 && ax >= minFx && ax <= maxFx + 1);
                if (isFront && contentOrder < minFrontAdjacentContentOrder)
                    minFrontAdjacentContentOrder = contentOrder;
                if (isBackSouthEast && contentOrder > maxBackAdjacentContentOrder)
                    maxBackAdjacentContentOrder = contentOrder;
            }
        }
        // Apply floor first so we're always in front of back south-east tiles
        if (maxBackAdjacentContentOrder != int.MinValue)
        {
            int orderInFrontOfBack = maxBackAdjacentContentOrder + 1;
            if (orderInFrontOfBack > maxOrder)
                maxOrder = orderInFrontOfBack;
        }
        // Cap (go behind front row) only when it wouldn't hide the building. If current maxOrder is already > front content, skip cap so building stays visible.
        if (minFrontAdjacentContentOrder != int.MaxValue)
        {
            int orderBehindFront = minFrontAdjacentContentOrder - 1;
            bool skipCapForVisibility = orderBehindFront < maxOrder && maxOrder > minFrontAdjacentContentOrder;
            if (orderBehindFront < maxOrder && !skipCapForVisibility)
                maxOrder = orderBehindFront;
        }

        SpriteRenderer[] renderers = tile.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sr in renderers)
        {
            if (sr != null)
                sr.sortingOrder = maxOrder;
        }
        pivotCell.SetCellInstanceSortingOrder(maxOrder);
    }

    const int ROAD_SORTING_OFFSET = 3;

    /// <summary>
    /// Returns the sorting order to use for a road tile at (x, y) at the given height (e.g. 1 for bridge over water).
    /// </summary>
    public int GetRoadSortingOrderForCell(int x, int y, int height)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return 0;
        if (terrainManager == null) return 0;
        return terrainManager.CalculateTerrainSortingOrder(x, y, height) + ROAD_SORTING_OFFSET;
    }

    /// <summary>
    /// Sets sorting order for a road tile using TerrainManager so it renders below forest and buildings.
    /// </summary>
    public void SetRoadSortingOrder(GameObject tile, int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;

        Cell cell = gridArray[x, y].GetComponent<Cell>();
        if (cell == null) return;

        tile.transform.SetParent(cell.gameObject.transform);

        if (terrainManager == null)
        {
            SetTileSortingOrder(tile, Zone.ZoneType.Road);
            return;
        }

        int cellHeight = cell.height;
        if (terrainManager.GetHeightMap() != null)
            cellHeight = terrainManager.GetHeightMap().GetHeight(x, y);

        int sortingOrder = terrainManager.CalculateTerrainSortingOrder(x, y, cellHeight) + ROAD_SORTING_OFFSET;

        SpriteRenderer[] renderers = tile.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sr in renderers)
        {
            if (sr != null)
                sr.sortingOrder = sortingOrder;
        }
    }

    public int SetResortSeaLevelOrder(GameObject tile, Cell cell)
    {
        int x = (int)cell.x;
        int y = (int)cell.y;

        tile.transform.SetParent(cell.gameObject.transform);

        int baseSortingOrder = (y * width + x);
        int sortingOrder = -(baseSortingOrder + 110000);

        SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();

        if (sr != null)
        {
            sr.sortingOrder = sortingOrder;
        }
        cell.sortingOrder = sortingOrder;
        return sortingOrder;
    }

    public Vector2 GetGridPosition(Vector2 worldPoint)
    {
        float posX = worldPoint.x / (tileWidth / 2);
        float posY = worldPoint.y / (tileHeight / 2);

        int gridX = Mathf.RoundToInt((posY + posX) / 2);
        int gridY = Mathf.RoundToInt((posY - posX) / 2);

        return new Vector2(gridX, gridY);
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
        Vector3 p0 = cam.WorldToScreenPoint(new Vector3(worldMin.x, worldMin.y, 0f));
        Vector3 p1 = cam.WorldToScreenPoint(new Vector3(worldMax.x, worldMin.y, 0f));
        Vector3 p2 = cam.WorldToScreenPoint(new Vector3(worldMin.x, worldMax.y, 0f));
        Vector3 p3 = cam.WorldToScreenPoint(new Vector3(worldMax.x, worldMax.y, 0f));

        float minX = Mathf.Min(p0.x, p1.x, p2.x, p3.x);
        float maxX = Mathf.Max(p0.x, p1.x, p2.x, p3.x);
        float minY = Mathf.Min(p0.y, p1.y, p2.y, p3.y);
        float maxY = Mathf.Max(p0.y, p1.y, p2.y, p3.y);

        // Inset rect toward center so hit areas don't overlap with neighbors (isometric AABB overlap).
        const float insetFactor = 0.75f;
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

    public Cell GetCellFromWorldPoint(Vector2 worldPoint, Vector2 gridPos)
    {
        if (cachedCamera == null) cachedCamera = Camera.main;
        Camera cam = cachedCamera;
        if (cam == null) return null;

        int gridX = (int)gridPos.x;
        int gridY = (int)gridPos.y;

        // 5 candidate cells: center + 4 cross neighbors
        List<Cell> candidates = new List<Cell>();
        int[] dx = { 0, 1, -1, 0, 0 };
        int[] dy = { 0, 0, 0, 1, -1 };
        for (int i = 0; i < 5; i++)
        {
            GameObject go = GetGridCell(new Vector2(gridX + dx[i], gridY + dy[i]));
            if (go == null) continue;
            Cell c = go.GetComponent<Cell>();
            if (c != null) candidates.Add(c);
        }

        Vector2 mouseScreen = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        Cell best = null;
        int bestOrder = int.MinValue;

        foreach (Cell cell in candidates)
        {
            if (!TryGetCellBaseTileScreenBounds(cell, cam, out Rect screenRect))
                continue;
            if (!screenRect.Contains(mouseScreen))
                continue;
            if (cell.sortingOrder > bestOrder)
            {
                bestOrder = cell.sortingOrder;
                best = cell;
            }
        }

        return best;
    }

    public Vector2 GetGridPositionWithHeight(Vector2 mouseWorldPoint)
    {
        Vector2 gridPos = GetGridPosition(mouseWorldPoint);

        Cell cell = GetCellFromWorldPoint(mouseWorldPoint, gridPos);
        if (cell == null)
        {
            return gridPos;
        }

        Vector2 gridPosWithHeight = new Vector2(cell.x, cell.y);

        return gridPosWithHeight;
    }

    public Cell GetMouseGridCell(Vector2 mouseWorldPoint)
    {
        Vector2 gridPos = GetGridPosition(mouseWorldPoint);
        Cell cell = GetCellFromWorldPoint(mouseWorldPoint, gridPos);

        if (cell == null)
        {
            Cell gridPosCell = GetCell((int)gridPos.x, (int)gridPos.y);
            return gridPosCell;
        }
        return cell;
    }
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

    public Vector2 GetWorldPosition(int gridX, int gridY)
    {
        Cell cell = gridArray[gridX, gridY].GetComponent<Cell>();
        int height = cell.GetCellInstanceHeight();
        return GetWorldPositionVector(gridX, gridY, height);
    }

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
        Cell pivotCell = gridArray[(int)gridPos.x, (int)gridPos.y].GetComponent<Cell>();
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
                    Cell c = gridArray[gridX, gridY].GetComponent<Cell>();
                    sum += GetCellWorldPosition(c);
                    count++;
                }
            }
        }
        return count > 0 ? sum / count : pivotCell.transformPosition;
    }

    public GameObject GetGridCell(Vector2 gridPos)
    {
        if (gridPos.x < 0 || gridPos.x >= gridArray.GetLength(0) ||
            gridPos.y < 0 || gridPos.y >= gridArray.GetLength(1))
        {
            return null;
        }
        return gridArray[(int)gridPos.x, (int)gridPos.y];
    }

    public void SetCellHeight(Vector2 gridPos, int height)
    {

        Cell cell = gridArray[(int)gridPos.x, (int)gridPos.y].GetComponent<Cell>();
        cell.SetCellInstanceHeight(height);
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

    /// <summary>
    /// Returns the reason why building placement would fail at this position, or null if placement would succeed.
    /// Used for debug UI and specific error messages.
    /// </summary>
    public string GetBuildingPlacementFailReason(Vector2 gridPosition, int buildingSize, bool isWaterPlant)
    {
        TryValidateBuildingPlacement(gridPosition, buildingSize, isWaterPlant, out string failReason);
        return failReason;
    }

    /// <summary>
    /// Returns true if the cell is water: either in WaterMap or at sea level (height 0).
    /// Uses height map when valid; otherwise falls back to the Cell's height from the grid (same source as terrain visuals).
    /// </summary>
    private bool IsCellWater(int x, int y)
    {
        bool fromWaterMap = waterManager != null && waterManager.IsWaterAt(x, y);

        int h = -1;
        var heightMap = terrainManager != null ? terrainManager.GetHeightMap() : null;
        bool validHeightMap = heightMap != null && heightMap.IsValidPosition(x, y);
        if (validHeightMap)
            h = heightMap.GetHeight(x, y);

        bool fromHeightMap = validHeightMap && h == TerrainManager.SEA_LEVEL;

        bool fromCellHeight = false;
        if (!fromHeightMap && x >= 0 && x < width && y >= 0 && y < height)
        {
            Cell cell = gridArray[x, y].GetComponent<Cell>();
            if (cell != null)
            {
                h = cell.GetCellInstanceHeight();
                fromCellHeight = h == TerrainManager.SEA_LEVEL;
            }
        }

        if (fromWaterMap) return true;
        if (fromHeightMap || fromCellHeight) return true;
        return false;
    }

    /// <summary>
    /// Returns true if at least one cell in the perimeter ring around the building footprint is water.
    /// Perimeter = full ring around the footprint rectangle (includes diagonal corners of the ring).
    /// </summary>
    private bool HasWaterInFootprintPerimeter(Vector2 gridPosition, int buildingSize)
    {
        GetBuildingFootprintOffset(buildingSize, out int offsetX, out int offsetY);
        int minFx = (int)gridPosition.x - offsetX;
        int minFy = (int)gridPosition.y - offsetY;
        int maxFx = minFx + buildingSize - 1;
        int maxFy = minFy + buildingSize - 1;

        for (int px = minFx - 1; px <= maxFx + 1; px++)
        {
            for (int py = minFy - 1; py <= maxFy + 1; py++)
            {
                if (px >= minFx && px <= maxFx && py >= minFy && py <= maxFy)
                    continue;
                if (px >= 0 && px < width && py >= 0 && py < height)
                {
                    if (IsCellWater(px, py))
                        return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Validates water plant placement for any footprint size (1x1, 2x2, 3x3, etc.).
    /// Allows water tiles in footprint; requires at least one land tile and adjacency to water (water in perimeter ring).
    /// </summary>
    private bool TryValidateWaterPlantPlacement(Vector2 gridPosition, int buildingSize, out string failReason)
    {
        failReason = null;
        GetBuildingFootprintOffset(buildingSize, out int offsetX, out int offsetY);

        for (int x = 0; x < buildingSize; x++)
        {
            for (int y = 0; y < buildingSize; y++)
            {
                int gridX = (int)gridPosition.x + x - offsetX;
                int gridY = (int)gridPosition.y + y - offsetY;
                if (gridX < 0 || gridX >= width || gridY < 0 || gridY >= height)
                {
                    failReason = "Footprint out of grid bounds.";
                    return false;
                }
            }
        }

        if (!terrainManager.CanPlaceBuildingInTerrain(gridPosition, buildingSize, out failReason, true, true))
            return false;

        bool hasLandTile = false;
        for (int x = 0; x < buildingSize; x++)
        {
            for (int y = 0; y < buildingSize; y++)
            {
                int gridX = (int)gridPosition.x + x - offsetX;
                int gridY = (int)gridPosition.y + y - offsetY;
                if (waterManager == null || !waterManager.IsWaterAt(gridX, gridY))
                {
                    hasLandTile = true;
                    Cell cell = gridArray[gridX, gridY].GetComponent<Cell>();
                    if (cell.zoneType != Zone.ZoneType.Grass)
                    {
                        failReason = $"Tile ({gridX},{gridY}) is not Grass (current: {cell.zoneType}).";
                        return false;
                    }
                }
            }
        }

        if (!hasLandTile)
        {
            failReason = "Water plant must have at least one land tile in footprint.";
            return false;
        }

        if (waterManager != null)
        {
            if (!HasWaterInFootprintPerimeter(gridPosition, buildingSize))
            {
                failReason = "Water plant must be adjacent to water.";
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Validates building placement and optionally returns the first failure reason.
    /// </summary>
    private bool TryValidateBuildingPlacement(Vector2 gridPosition, int buildingSize, bool isWaterPlant, out string failReason)
    {
        failReason = null;

        if (buildingSize == 0)
        {
            failReason = "Building size is 0.";
            return false;
        }

        if (interstateManager != null)
        {
            interstateManager.CheckInterstateConnectivity();
            if (!interstateManager.IsConnectedToInterstate)
            {
                failReason = "No connection to Interstate Highway.";
                if (GameNotificationManager != null)
                    GameNotificationManager.PostWarning("Connect a road to the Interstate Highway before building.");
                return false;
            }
        }

        if (isWaterPlant)
            return TryValidateWaterPlantPlacement(gridPosition, buildingSize, out failReason);

        bool allowCoastalSlope = false;
        GetBuildingFootprintOffset(buildingSize, out int offsetX, out int offsetY);

        if (!terrainManager.CanPlaceBuildingInTerrain(gridPosition, buildingSize, out failReason, allowCoastalSlope, false))
        {
            if (string.IsNullOrEmpty(failReason))
                failReason = "Terrain: slope, water in footprint, height mismatch, or out of bounds.";
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
                    failReason = "Footprint out of grid bounds.";
                    return false;
                }

                Cell cell = gridArray[gridX, gridY].GetComponent<Cell>();
                if (cell.isInterstate)
                {
                    failReason = "Cannot build on Interstate Highway.";
                    return false;
                }
                if (cell.zoneType != Zone.ZoneType.Grass)
                {
                    failReason = $"Tile ({gridX},{gridY}) is not Grass (current: {cell.zoneType}).";
                    return false;
                }
            }
        }

        return true;
    }

    public bool canPlaceBuilding(Vector2 gridPosition, int buildingSize)
    {
        bool isWaterPlant = uiManager != null && uiManager.GetSelectedBuilding() is WaterPlant;
        return TryValidateBuildingPlacement(gridPosition, buildingSize, isWaterPlant, out _);
    }

    public bool canPlaceBuilding(Vector2 gridPosition, int buildingSize, bool isWaterPlant)
    {
        return TryValidateBuildingPlacement(gridPosition, buildingSize, isWaterPlant, out _);
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
        GetBuildingFootprintOffset(buildingSize, out int offsetX, out int offsetY);

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

                    cell.RemoveForestForBuilding();
                    UpdatePlacedBuildingCellAttributes(cell, buildingSize, powerPlant, waterPlant, buildingPrefab, Zone.ZoneType.Building, building);

                    // Destroy children of every footprint cell (grass, etc.). On pivot cell, exclude the building.
                    bool isPivot = (gridX == gridPos.x && gridY == gridPos.y);
                    DestroyCellChildren(gridCell, new Vector2(gridX, gridY), isPivot ? building : null);
                    if (isPivot)
                        SetCellAsBuildingPivot(cell);
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

        Vector2 pivotGridPos = new Vector2(gridPos.x, gridPos.y);
        Vector2 position = GetBuildingPlacementWorldPosition(pivotGridPos, buildingSize);

        Vector3 worldPosition = new Vector3(position.x, position.y, 0f);
        GameObject building = Instantiate(buildingPrefab, worldPosition, Quaternion.identity);
        building.transform.SetParent(gridArray[(int)pivotGridPos.x, (int)pivotGridPos.y].transform);

        SetZoneBuildingSortingOrder(building, (int)pivotGridPos.x, (int)pivotGridPos.y, buildingSize);

        HandleBuildingPlacementAttributesUpdate(iBuilding, pivotGridPos, building, buildingPrefab);
    }

    void LoadBuildingTile(GameObject prefab, Vector2 gridPos, int buildingSize)
    {
        Cell pivotCell = gridArray[(int)gridPos.x, (int)gridPos.y].GetComponent<Cell>();
        Vector2 worldPos = GetBuildingPlacementWorldPosition(gridPos, buildingSize);

        Vector3 position = new Vector3(worldPos.x, worldPos.y, 0f);
        GameObject building = Instantiate(prefab, position, Quaternion.identity);
        building.transform.SetParent(pivotCell.gameObject.transform);

        SetZoneBuildingSortingOrder(building, (int)gridPos.x, (int)gridPos.y, buildingSize);
    }

    /// <summary>
    /// Place a building programmatically (e.g. auto resource planner). Caller is responsible for budget and affordability.
    /// Does not deduct money. Returns true if placed.
    /// </summary>
    public bool PlaceBuildingProgrammatic(Vector2 gridPos, IBuilding buildingTemplate)
    {
        if (buildingTemplate == null) return false;
        int buildingSize = buildingTemplate.BuildingSize;
        bool isWaterPlant = buildingTemplate is WaterPlant;
        if (!TryValidateBuildingPlacement(gridPos, buildingSize, isWaterPlant, out _))
            return false;

        Vector2 pivotGridPos = new Vector2(gridPos.x, gridPos.y);
        Vector2 position = GetBuildingPlacementWorldPosition(pivotGridPos, buildingSize);
        Vector3 worldPosition = new Vector3(position.x, position.y, 0f);
        GameObject building = Instantiate(buildingTemplate.Prefab, worldPosition, Quaternion.identity);
        building.transform.SetParent(gridArray[(int)pivotGridPos.x, (int)pivotGridPos.y].transform);
        SetZoneBuildingSortingOrder(building, (int)pivotGridPos.x, (int)pivotGridPos.y, buildingSize);

        IBuilding placedIBuilding = building.GetComponent<PowerPlant>() as IBuilding ?? building.GetComponent<WaterPlant>() as IBuilding;
        if (placedIBuilding != null)
            HandleBuildingPlacementAttributesUpdate(placedIBuilding, pivotGridPos, building, buildingTemplate.Prefab);
        return true;
    }

    void PlaceBuilding(Vector2 gridPos, IBuilding iBuilding)
    {
        if (!cityStats.CanAfford(iBuilding.ConstructionCost))
        {
            uiManager.ShowInsufficientFundsTooltip("building", iBuilding.ConstructionCost);
            return;
        }

        bool isWaterPlant = iBuilding is WaterPlant;
        if (canPlaceBuilding(gridPos, iBuilding.BuildingSize))
        {
            cityStats.RemoveMoney(iBuilding.ConstructionCost);

            PlaceBuildingTile(iBuilding, gridPos);

            GameNotificationManager.Instance.PostBuildingConstructed(
                iBuilding.Prefab.name
            );
        }
        else
        {
            string reason = GetBuildingPlacementFailReason(gridPos, iBuilding.BuildingSize, isWaterPlant);
            GameNotificationManager.Instance.PostBuildingPlacementError(
                string.IsNullOrEmpty(reason) ? "Cannot place building here, area is not available." : reason
            );
        }
    }

    void RestoreGridCell(CellData cellData, GameObject cell)
    {
        cell.GetComponent<Cell>().SetCellData(cellData);

        Zone.ZoneType zoneType = zoneManager.GetZoneTypeFromZoneTypeString(cellData.zoneType);

        GameObject tilePrefab = zoneManager.FindPrefabByName(cellData.prefabName);
        if (tilePrefab != null)
        {
            zoneManager.PlaceZoneBuildingTile(tilePrefab, cell, cellData.buildingSize);
            UpdatePlacedBuildingCellAttributes(cell.GetComponent<Cell>(), cellData.buildingSize, cellData.powerPlant, cellData.waterPlant, tilePrefab, zoneType, null);
        }

        zoneManager.addZonedTileToList(new Vector2(cellData.x, cellData.y), zoneType);

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

        zoneManager.CalculateAvailableSquareZonedSections();
    }

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

    public List<CellData> GetGridData()
    {
        List<CellData> gridData = new List<CellData>();

        foreach (GameObject cell in gridArray)
        {
            Cell cellComponent = cell.GetComponent<Cell>();
            CellData cellData = cellComponent.GetCellData();
            gridData.Add(cellData);
        }

        return gridData;
    }

    public bool isBorderCell(int x, int y)
    {
        if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
        {
            return true;
        }
        return false;
    }

    // Road/path helpers for auto-growth (cache invalidated when grid changes)
    private List<Vector2Int> cachedRoadPositions;
    private List<Vector2Int> cachedRoadEdgePositions;
    private bool roadCacheDirty = true;

    public void InvalidateRoadCache()
    {
        roadCacheDirty = true;
    }

    public List<Vector2Int> GetAllRoadPositions()
    {
        if (!roadCacheDirty && cachedRoadPositions != null)
            return cachedRoadPositions;
        cachedRoadPositions = new List<Vector2Int>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell c = GetCell(x, y);
                if (c != null && c.zoneType == Zone.ZoneType.Road)
                    cachedRoadPositions.Add(new Vector2Int(x, y));
            }
        }
        cachedRoadEdgePositions = null;
        roadCacheDirty = false;
        return cachedRoadPositions;
    }

    public List<Vector2Int> GetRoadEdgePositions()
    {
        if (cachedRoadEdgePositions != null && !roadCacheDirty)
            return cachedRoadEdgePositions;
        List<Vector2Int> all = GetAllRoadPositions();
        cachedRoadEdgePositions = new List<Vector2Int>();
        foreach (Vector2Int p in all)
        {
            bool hasExpandableNeighbor = false;
            if (IsValidGridPosition(new Vector2(p.x + 1, p.y))) { Cell n = GetCell(p.x + 1, p.y); if (n != null && (n.zoneType == Zone.ZoneType.Grass || n.HasForest() || n.GetCellInstanceHeight() == 0)) hasExpandableNeighbor = true; }
            if (!hasExpandableNeighbor && IsValidGridPosition(new Vector2(p.x - 1, p.y))) { Cell n = GetCell(p.x - 1, p.y); if (n != null && (n.zoneType == Zone.ZoneType.Grass || n.HasForest() || n.GetCellInstanceHeight() == 0)) hasExpandableNeighbor = true; }
            if (!hasExpandableNeighbor && IsValidGridPosition(new Vector2(p.x, p.y + 1))) { Cell n = GetCell(p.x, p.y + 1); if (n != null && (n.zoneType == Zone.ZoneType.Grass || n.HasForest() || n.GetCellInstanceHeight() == 0)) hasExpandableNeighbor = true; }
            if (!hasExpandableNeighbor && IsValidGridPosition(new Vector2(p.x, p.y - 1))) { Cell n = GetCell(p.x, p.y - 1); if (n != null && (n.zoneType == Zone.ZoneType.Grass || n.HasForest() || n.GetCellInstanceHeight() == 0)) hasExpandableNeighbor = true; }
            if (hasExpandableNeighbor)
                cachedRoadEdgePositions.Add(p);
        }
        return cachedRoadEdgePositions;
    }

    /// <summary>Number of cardinal neighbors of (gx,gy) that are zoneable (Grass, Forest, or Flat/N-S/E-W slope). Used for road-reservation in auto-zoning.</summary>
    public int CountGrassNeighbors(int gx, int gy)
    {
        if (gx < 0 || gx >= width || gy < 0 || gy >= height) return 0;
        int count = 0;
        int[] dx = { 1, -1, 0, 0 }, dy = { 0, 0, 1, -1 };
        for (int i = 0; i < 4; i++)
        {
            int nx = gx + dx[i], ny = gy + dy[i];
            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
            {
                Cell c = GetCell(nx, ny);
                if (c != null && IsZoneableNeighbor(c, nx, ny))
                    count++;
            }
        }
        return count;
    }

    /// <summary>True if this neighbor cell is valid for zoning (Grass, Forest, or Flat/N-S/E-W slope).</summary>
    public bool IsZoneableNeighbor(Cell c, int x, int y)
    {
        if (c == null) return false;
        if (c.zoneType == Zone.ZoneType.Grass || c.HasForest()) return true;
        if (terrainManager == null) return false;
        TerrainSlopeType slope = terrainManager.GetTerrainSlopeTypeAt(x, y);
        return slope == TerrainSlopeType.Flat || slope == TerrainSlopeType.North || slope == TerrainSlopeType.South || slope == TerrainSlopeType.East || slope == TerrainSlopeType.West;
    }

    /// <summary>True if at least one of the 4 cardinal neighbors of (x,y) is a road.</summary>
    public bool IsAdjacentToRoad(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return false;
        int[] dx = { 1, -1, 0, 0 }, dy = { 0, 0, 1, -1 };
        for (int i = 0; i < 4; i++)
        {
            int nx = x + dx[i], ny = y + dy[i];
            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
            {
                Cell c = GetCell(nx, ny);
                if (c != null && c.zoneType == Zone.ZoneType.Road)
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// A* path over walkable cells (grass or road). Prefers flat terrain; cardinal slopes cost more; diagonal slopes are impassable.
    /// Max 200 nodes explored. Returns path including start and end, or empty if not found.
    /// </summary>
    public List<Vector2Int> FindPath(Vector2Int from, Vector2Int to)
    {
        return FindPathWithRoadSpacing(from, to, 0);
    }

    /// <summary>
    /// A* path with optional extra cost for cells close to existing roads, so paths tend to keep minDistanceFromRoad cells away and leave space for zones.
    /// When minDistanceFromRoad is 0, behaves like FindPath. When &gt; 0, adds penalty for stepping on cells within that Manhattan distance of any road.
    /// </summary>
    public List<Vector2Int> FindPathWithRoadSpacing(Vector2Int from, Vector2Int to, int minDistanceFromRoad)
    {
        const int maxNodes = 200;
        const int roadProximityPenalty = 18;
        HashSet<Vector2Int> roadSet = null;
        if (minDistanceFromRoad > 0)
        {
            var roads = GetAllRoadPositions();
            roadSet = new HashSet<Vector2Int>(roads);
        }

        if (!IsWalkable(from.x, from.y) || !IsWalkable(to.x, to.y))
            return new List<Vector2Int>();
        var open = new List<Vector2Int>();
        var closed = new HashSet<Vector2Int>();
        var gScore = new Dictionary<Vector2Int, int>();
        var fScore = new Dictionary<Vector2Int, int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        gScore[from] = 0;
        fScore[from] = Heuristic(from, to);
        open.Add(from);
        int explored = 0;
        while (open.Count > 0 && explored < maxNodes)
        {
            explored++;
            open.Sort((a, b) => (fScore.ContainsKey(a) ? fScore[a] : int.MaxValue).CompareTo(fScore.ContainsKey(b) ? fScore[b] : int.MaxValue));
            Vector2Int current = open[0];
            open.RemoveAt(0);
            if (current == to)
            {
                var path = new List<Vector2Int>();
                while (cameFrom.ContainsKey(current))
                {
                    path.Add(current);
                    current = cameFrom[current];
                }
                path.Add(from);
                path.Reverse();
                return path;
            }
            closed.Add(current);
            foreach (Vector2Int neighbor in GetWalkableNeighbors(current))
            {
                if (closed.Contains(neighbor)) continue;
                int stepCost = GetRoadStepCost(neighbor.x, neighbor.y);
                if (stepCost == int.MaxValue) continue;
                if (minDistanceFromRoad > 0 && roadSet != null)
                {
                    int dist = MinManhattanDistanceToSet(neighbor.x, neighbor.y, roadSet);
                    if (dist > 0 && dist < minDistanceFromRoad)
                        stepCost += roadProximityPenalty;
                }
                int tentative = (gScore.ContainsKey(current) ? gScore[current] : int.MaxValue) + stepCost;
                if (!gScore.ContainsKey(neighbor) || tentative < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentative;
                    fScore[neighbor] = tentative + Heuristic(neighbor, to);
                    if (!open.Contains(neighbor))
                        open.Add(neighbor);
                }
            }
        }
        return new List<Vector2Int>();
    }

    /// <summary>Minimum Manhattan distance from (x,y) to any position in the set.</summary>
    private static int MinManhattanDistanceToSet(int x, int y, HashSet<Vector2Int> set)
    {
        int min = int.MaxValue;
        foreach (Vector2Int p in set)
        {
            int d = Mathf.Abs(x - p.x) + Mathf.Abs(y - p.y);
            if (d < min) min = d;
        }
        return min == int.MaxValue ? int.MaxValue : min;
    }

    /// <summary>
    /// Cost to step onto (x,y) for road pathfinding: 1 = flat, higher = slope, MaxValue = impassable (e.g. diagonal slope).
    /// </summary>
    private int GetRoadStepCost(int x, int y)
    {
        if (!IsWalkable(x, y)) return int.MaxValue;
        if (terrainManager == null) return 1;
        TerrainSlopeType t = terrainManager.GetTerrainSlopeTypeAt(x, y);
        switch (t)
        {
            case TerrainSlopeType.Flat: return 1;
            case TerrainSlopeType.North:
            case TerrainSlopeType.South:
            case TerrainSlopeType.East:
            case TerrainSlopeType.West: return 8;
            default: return int.MaxValue;
        }
    }

    private bool IsWalkable(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return false;
        Cell c = GetCell(x, y);
        if (c == null) return false;
        return c.zoneType == Zone.ZoneType.Grass || c.zoneType == Zone.ZoneType.Road;
    }

    private List<Vector2Int> GetWalkableNeighbors(Vector2Int p)
    {
        var list = new List<Vector2Int>();
        int[] dx = { 1, -1, 0, 0 }, dy = { 0, 0, 1, -1 };
        for (int i = 0; i < 4; i++)
        {
            int nx = p.x + dx[i], ny = p.y + dy[i];
            if (IsWalkable(nx, ny))
                list.Add(new Vector2Int(nx, ny));
        }
        return list;
    }

    private static int Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
