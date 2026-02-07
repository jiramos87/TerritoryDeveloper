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
    public BuildingSelectorMenuController buildingSelectorMenuController;

    public int width, height;
    public float tileWidth = 1f; // Full width of the tile
    public float tileHeight = 0.5f; // Effective height due to isometric perspective

    public float halfWidth;
    public float halfHeight;
    public GameObject[,] gridArray;
    public Vector2 mouseGridPosition;
    public int mouseGridHeight;
    public int mouseGridSortingOrder;

    public bool isInitialized = false;

    public void InitializeGrid()
    {
        halfWidth = tileWidth / 2f;
        halfHeight = tileHeight / 2f;

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

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject gridCell = new GameObject($"Cell_{x}_{y}");

                float posX = (x - y) * (tileWidth / 2);
                float posY = (x + y) * (tileHeight / 2);

                gridCell.transform.position = new Vector3(posX, posY, 0);
                gridCell.transform.SetParent(transform);

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

                PolygonCollider2D polygonCollider = zoneTile.GetComponent<PolygonCollider2D>();
                if (polygonCollider == null)
                {
                    polygonCollider = zoneTile.AddComponent<PolygonCollider2D>();
                }

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

            Vector2 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);

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
    public bool IsValidGridPosition(Vector2 gridPosition)
    {
        int gridX = (int)gridPosition.x;
        int gridY = (int)gridPosition.y;

        return gridX >= 0 && gridX < width && gridY >= 0 && gridY < height;
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
        Cell cellComponent = cell.GetComponent<Cell>();
        GameObject zoneTile = Instantiate(
            zoneManager.GetRandomZonePrefab(Zone.ZoneType.Grass),
            cellComponent.transformPosition,
            Quaternion.identity
        );

        int sortingOrder = SetTileSortingOrder(zoneTile, Zone.ZoneType.Grass);
    }

    void BulldozeTile(GameObject cell)
    {
        Cell cellComponent = cell.GetComponent<Cell>();

        // Capture sorting order BEFORE demolition resets it
        int preSortingOrder = cellComponent.sortingOrder;

        RestoreCellAttributes(cellComponent);

        DestroyCellChildren(cell, new Vector2(cellComponent.x, cellComponent.y));

        RestoreTile(cell);

        // Show the bulldoze animation using the pre-captured sorting order
        if (uiManager != null)
        {
            uiManager.ShowDemolitionAnimation(cell, preSortingOrder);
        }
    }

    void BulldozeBuildingTiles(GameObject cell)
    {
        Cell cellComponent = cell.GetComponent<Cell>();

        int buildingSize = cellComponent.buildingSize;

        // Capture sorting order BEFORE demolition resets it
        int preSortingOrder = cellComponent.sortingOrder;

        // Show animation before demolishing for better visual effect
        if (uiManager != null)
        {
            uiManager.ShowDemolitionAnimationCentered(cell, buildingSize, preSortingOrder);
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
            cityStats.HandleBuildingDemolition(zoneType, zoneManager.GetZoneAttributes(zoneType));
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

        if (cell.transform.childCount > 0)
        {
            foreach (Transform child in cell.transform)
            {
                Zone zone = child.GetComponent<Zone>();

                if (zone && zone.zoneCategory == Zone.ZoneCategory.Zoning)
                {
                    zoneManager.removeZonedPositionFromList(gridPosition, zone.zoneType);
                }

                DestroyImmediate(child.gameObject);
            }
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

    public Cell GetCellFromWorldPoint(Vector2 worldPoint, Vector2 gridPos)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPoint);
        if (hits.Length == 0)
        {
            return null;
        }

        Vector2 highestSortingOrderTransformPosition = Vector2.zero;
        int highestSortingOrder = int.MinValue;

        foreach (Collider2D hit in hits)
        {
            SpriteRenderer spriteRenderer = hit.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                continue;

            int sortingOrder = spriteRenderer.sortingOrder;

            if (sortingOrder > highestSortingOrder)
            {
                highestSortingOrder = sortingOrder;
                highestSortingOrderTransformPosition = hit.transform.position;
            }
        }

        // look in the vecinity downwards in the gridArray for the cell with the cell.transformPosition == highestSortingOrderTransformPosition
        for (int i = 0; i < 5; i++)
        {
            Vector2 currentPosition = new Vector2(gridPos.x - i, gridPos.y - i);
            GameObject gridArrayCell = GetGridCell(currentPosition);
            if (gridArrayCell == null)
            {
                continue;
            }
            Cell cell = gridArrayCell.GetComponent<Cell>();

            if (cell == null)
            {
                continue;
            }
            if (cell.transformPosition == highestSortingOrderTransformPosition)
            {
                return cell;
            }
        }
        return null;
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

    public bool canPlaceBuilding(Vector2 gridPosition, int buildingSize)
    {
        if (buildingSize == 0)
        {
            return false;
        }

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

        if (!terrainManager.CanPlaceBuildingInTerrain(gridPosition, buildingSize))
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

        if (waterManager != null && uiManager.GetSelectedBuilding() != null &&
            uiManager.GetSelectedBuilding() is WaterPlant)
        {
            bool adjacentToWater = false;

            for (int x = 0; x < buildingSize; x++)
            {
                for (int y = 0; y < buildingSize; y++)
                {
                    int gridX = (int)gridPosition.x + x - offsetX;
                    int gridY = (int)gridPosition.y + y - offsetY;

                    adjacentToWater = waterManager.IsAdjacentToWater(gridX, gridY);
                    if (adjacentToWater) break;
                }
                if (adjacentToWater)
                {
                    return true;
                }
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

        Cell cell = gridArray[(int)pivotGridPos.x, (int)pivotGridPos.y].GetComponent<Cell>();

        // Get the world position for the pivot cell
        Vector2 position = cell.transformPosition;

        // For even-sized buildings, adjust the visual position slightly
        if (buildingSize > 1 && buildingSize % 2 == 0)
        {
            position.x += tileWidth / 4f; // Small visual adjustment for even-sized buildings
        }

        // Create the building
        GameObject building = Instantiate(buildingPrefab, position, Quaternion.identity);
        building.transform.SetParent(gridArray[(int)pivotGridPos.x, (int)pivotGridPos.y].transform);

        // adjust the building's position to match the grid cell based on its size

        int sortingOrder = SetTileSortingOrder(building, Zone.ZoneType.Building);
        if (buildingSize > 1 && buildingSize % 2 == 0)
        {
            building.transform.position += new Vector3(tileWidth / 4f, 0, 0);
        }

        gridArray[(int)pivotGridPos.x, (int)pivotGridPos.y].GetComponent<Cell>().sortingOrder = sortingOrder;

        HandleBuildingPlacementAttributesUpdate(iBuilding, pivotGridPos, building, buildingPrefab);
    }

    void LoadBuildingTile(GameObject prefab, Vector2 gridPos, int buildingSize)
    {
        Cell cell = gridArray[(int)gridPos.x, (int)gridPos.y].GetComponent<Cell>();
        Vector2 worldPos = GetCellWorldPosition(cell);
        GameObject building = Instantiate(prefab, worldPos, Quaternion.identity);
        building.transform.SetParent(cell.gameObject.transform);

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
            cityStats.RemoveMoney(iBuilding.ConstructionCost);

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
        foreach (GameObject cell in gridArray)
        {
            Destroy(cell);
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
}
