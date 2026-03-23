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

namespace Territory.Geography
{
/// <summary>
/// GeographyManager coordinates the initialization and management of all geographical features:
/// terrain height, water bodies, and forests. It centralizes the loading of geographical data
/// and ensures proper initialization order.
/// </summary>
public class GeographyManager : MonoBehaviour
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

    // Current geographical data (for save/load operations)
    private GeographyData currentGeographyData;

    private MiniMapController miniMapController;
    #endregion

    #region Initialization
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

        if (initializeOnStart)
        {
            InitializeGeography();
        }
    }

    public void InitializeGeography()
    {
        MapGenerationSeed.EnsureSessionMasterSeed();

        if (regionalMapManager != null)
        {
            regionalMapManager.InitializeRegionalMap();
        }

        if (gridManager != null)
        {
            gridManager.InitializeGrid();
        }

        if (waterManager != null)
        {
            waterManager.InitializeWaterMap();
        }

        // Interstate runs after terrain (from InitializeGrid) and water so pathing and tiles use final height/water state.
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
    }

    /// <summary>
    /// Refreshes the mini-map texture once terrain, water, interstate, forests (if any), and sorting are ready.
    /// </summary>
    private void NotifyMiniMapAfterGeographyReady()
    {
        if (miniMapController == null)
            miniMapController = FindObjectOfType<MiniMapController>();
        if (miniMapController != null)
            miniMapController.RebuildTexture();
    }

    /// <summary>
    /// Populates closeWaterCount for all cells and recalculates desirability.
    /// Call after ForestManager.InitializeForestMap() so both forest and water contribute to desirability.
    /// </summary>
    private void InitializeWaterDesirability()
    {
        if (gridManager == null || waterManager == null) return;

        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };

        for (int x = 0; x < gridManager.width; x++)
        {
            for (int y = 0; y < gridManager.height; y++)
            {
                Cell cell = gridManager.GetCell(x, y);
                if (cell == null) continue;

                int count = 0;
                for (int d = 0; d < 4; d++)
                {
                    int nx = x + dx[d];
                    int ny = y + dy[d];
                    if (nx >= 0 && nx < gridManager.width && ny >= 0 && ny < gridManager.height &&
                        waterManager.IsWaterAt(nx, ny))
                    {
                        count++;
                    }
                }
                cell.closeWaterCount = count;
                cell.UpdateDesirability();
            }
        }
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
    /// Re-initializes geography after ResetGrid (New Game). Applies terrain, water, interstate, forests,
    /// and recalculates sorting order. Call after gridManager.ResetGrid().
    /// </summary>
    public void ReinitializeGeographyForNewGame()
    {
        if (regionalMapManager != null)
            regionalMapManager.InitializeRegionalMap();

        if (terrainManager != null)
            terrainManager.InitializeHeightMap();

        if (waterManager != null)
            waterManager.InitializeWaterMap();

        // Interstate after terrain and water (same order as InitializeGeography).
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

    #region Terrain Setup
    /// <summary>
    /// If cell (x, y) is immediately east of a multi-cell building (i.e. x == that building's maxFx+1),
    /// returns true and the building's current sorting order so we can draw this cell's content on top (buildingOrder+1).
    /// </summary>
    private bool TryGetEastAdjacentBuildingOrder(int x, int y, out int buildingOrder)
    {
        buildingOrder = 0;
        if (gridManager == null || x <= 0) return false;
        int width = gridManager.width;
        int height = gridManager.height;
        if (y < 0 || y >= height) return false;

        int westX = x - 1;
        Cell westCell = gridManager.GetCell(westX, y);
        if (westCell == null || westCell.buildingSize <= 1) return false;

        GameObject pivotObj = gridManager.GetBuildingPivotCell(westCell);
        if (pivotObj == null) return false;
        Cell pivotCell = pivotObj.GetComponent<Cell>();
        if (pivotCell == null || pivotCell.buildingSize <= 1) return false;

        gridManager.GetBuildingFootprintOffset(pivotCell.buildingSize, out int offsetX, out int offsetY);
        int maxFx = (int)pivotCell.x - offsetX + pivotCell.buildingSize - 1;
        if (westX != maxFx) return false;

        buildingOrder = pivotCell.sortingOrder;
        return true;
    }

    /// <summary>
    /// If cell (x, y) is immediately south of a multi-cell building (i.e. (x+1, y) is the building's minFx column),
    /// returns true and the building's current sorting order so we can draw this cell's content on top (buildingOrder+1).
    /// </summary>
    private bool TryGetSouthAdjacentBuildingOrder(int x, int y, out int buildingOrder)
    {
        buildingOrder = 0;
        if (gridManager == null) return false;
        int width = gridManager.width;
        int height = gridManager.height;
        if (x < 0 || x >= width - 1 || y < 0 || y >= height) return false;

        int northX = x + 1;
        Cell northCell = gridManager.GetCell(northX, y);
        if (northCell == null || northCell.buildingSize <= 1) return false;

        GameObject pivotObj = gridManager.GetBuildingPivotCell(northCell);
        if (pivotObj == null) return false;
        Cell pivotCell = pivotObj.GetComponent<Cell>();
        if (pivotCell == null || pivotCell.buildingSize <= 1) return false;

        gridManager.GetBuildingFootprintOffset(pivotCell.buildingSize, out int offsetX, out int offsetY);
        int minFx = (int)pivotCell.x - offsetX;
        if (northX != minFx) return false;

        buildingOrder = pivotCell.sortingOrder;
        return true;
    }

    /// <summary>
    /// If cell (x, y) is immediately west of a multi-cell building (i.e. (x, y+1) is the building's minFy row),
    /// returns true and the building's current sorting order so we can draw this cell's content on top (buildingOrder+1).
    /// </summary>
    private bool TryGetWestAdjacentBuildingOrder(int x, int y, out int buildingOrder)
    {
        buildingOrder = 0;
        if (gridManager == null) return false;
        int width = gridManager.width;
        int height = gridManager.height;
        if (x < 0 || x >= width || y < 0 || y >= height - 1) return false;

        int eastY = y + 1;
        Cell eastCell = gridManager.GetCell(x, eastY);
        if (eastCell == null || eastCell.buildingSize <= 1) return false;

        GameObject pivotObj = gridManager.GetBuildingPivotCell(eastCell);
        if (pivotObj == null) return false;
        Cell pivotCell = pivotObj.GetComponent<Cell>();
        if (pivotCell == null || pivotCell.buildingSize <= 1) return false;

        gridManager.GetBuildingFootprintOffset(pivotCell.buildingSize, out int offsetX, out int offsetY);
        int minFy = (int)pivotCell.y - offsetY;
        if (eastY != minFy) return false;

        buildingOrder = pivotCell.sortingOrder;
        return true;
    }

    /// <summary>
    /// Returns the maximum sorting order that any content on the cell at (x,y) would have.
    /// Used so the building can place itself behind "front" adjacent cells.
    /// </summary>
    private int GetCellMaxContentSortingOrder(int x, int y)
    {
        if (gridManager == null || terrainManager == null) return int.MinValue;
        int width = gridManager.width;
        int height = gridManager.height;
        if (x < 0 || x >= width || y < 0 || y >= height) return int.MinValue;

        GameObject cellObj = gridManager.gridArray[x, y];
        Cell cell = gridManager.GetCell(x, y);
        if (cell == null) return int.MinValue;

        int terrainOrder = terrainManager.CalculateTerrainSortingOrder(x, y, cell.height);
        int maxOrder = terrainOrder;

        if (cellObj.GetComponent<SpriteRenderer>() != null)
            maxOrder = Mathf.Max(maxOrder, terrainOrder);

        for (int i = 0; i < cellObj.transform.childCount; i++)
        {
            GameObject child = cellObj.transform.GetChild(i).gameObject;
            if (child.GetComponent<SpriteRenderer>() == null) continue;

            int order;
            if (terrainManager.IsWaterSlopeObject(child))
                order = terrainManager.CalculateWaterSlopeSortingOrder(x, y);
            else if (terrainManager.IsBayObject(child))
                order = terrainManager.CalculateBayShoreSortingOrder(x, y);
            else if (cell.forestObject != null && cell.forestObject == child)
                order = terrainOrder + 5;
            else
            {
                Zone zone = child.GetComponent<Zone>();
                if (zone != null)
                {
                    if (zone.zoneType == Zone.ZoneType.Road) order = terrainOrder + GridSortingOrderService.ROAD_SORTING_OFFSET;
                    else if (zone.zoneCategory == Zone.ZoneCategory.Zoning) order = terrainOrder + 0;
                    else if (zone.zoneCategory == Zone.ZoneCategory.Building) order = terrainOrder + 10;
                    else order = terrainOrder;
                }
                else if (child.GetComponent<PowerPlant>() != null || child.GetComponent<WaterPlant>() != null)
                {
                    if (cell.buildingSize > 1)
                        order = GetMultiCellBuildingMaxSortingOrder(x, y, cell.buildingSize);
                    else
                        order = terrainOrder + 10;
                }
                else
                    order = terrainOrder;
            }
            maxOrder = Mathf.Max(maxOrder, order);
        }
        return maxOrder;
    }

    /// <summary>
    /// Returns the maximum sorting order over a multi-cell building's footprint, capped so the building
    /// draws behind "front" adjacent cells (left and top) so forest/terrain can draw on top.
    /// Ensures the building always draws above content on its own footprint (e.g. grass during LoadGame).
    /// </summary>
    private int GetMultiCellBuildingMaxSortingOrder(int pivotX, int pivotY, int buildingSize)
    {
        if (gridManager == null || terrainManager == null || buildingSize <= 1)
            return terrainManager != null ? terrainManager.CalculateTerrainSortingOrder(pivotX, pivotY, 0) + 10 : 0;

        gridManager.GetBuildingFootprintOffset(buildingSize, out int offsetX, out int offsetY);
        int minFx = pivotX - offsetX;
        int minFy = pivotY - offsetY;
        int maxFx = minFx + buildingSize - 1;
        int maxFy = minFy + buildingSize - 1;

        int maxOrder = int.MinValue;
        int width = gridManager.width;
        int height = gridManager.height;

        for (int x = 0; x < buildingSize; x++)
        {
            for (int y = 0; y < buildingSize; y++)
            {
                int gridX = pivotX + x - offsetX;
                int gridY = pivotY + y - offsetY;
                if (gridX < 0 || gridX >= width || gridY < 0 || gridY >= height) continue;

                Cell cell = gridManager.GetCell(gridX, gridY);
                if (cell == null) continue;

                int cellHeight = cell.height;
                if (terrainManager.GetHeightMap() != null)
                    cellHeight = terrainManager.GetHeightMap().GetHeight(gridX, gridY);

                int order = terrainManager.CalculateBuildingSortingOrder(gridX, gridY, cellHeight);
                if (order > maxOrder) maxOrder = order;
            }
        }

        // Front = left or top. Back = south-east face only: right column (ax==maxFx+1, ay>=minFy) and bottom row (ay==maxFy+1).
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
        // Apply floor first so we're always in front of back south-east tiles (use +2 buffer to avoid grass overlapping building)
        if (maxBackAdjacentContentOrder != int.MinValue)
        {
            int orderInFrontOfBack = maxBackAdjacentContentOrder + 2;
            if (orderInFrontOfBack > maxOrder)
                maxOrder = orderInFrontOfBack;
        }
        // Cap only when it wouldn't hide the building (same logic as GridManager)
        if (minFrontAdjacentContentOrder != int.MaxValue)
        {
            int orderBehindFront = minFrontAdjacentContentOrder - 1;
            bool skipCapForVisibility = orderBehindFront < maxOrder && maxOrder > minFrontAdjacentContentOrder;
            if (orderBehindFront < maxOrder && !skipCapForVisibility)
                maxOrder = orderBehindFront;
        }

        // Floor: building must always draw on top of content on its own footprint (e.g. grass left during LoadGame before deferred destroy).
        int maxFootprintContentOrder = int.MinValue;
        for (int fx = minFx; fx <= maxFx; fx++)
        {
            for (int fy = minFy; fy <= maxFy; fy++)
            {
                if (fx < 0 || fx >= width || fy < 0 || fy >= height) continue;
                int contentOrder = GetCellMaxContentSortingOrder(fx, fy);
                if (contentOrder != int.MinValue && contentOrder > maxFootprintContentOrder)
                    maxFootprintContentOrder = contentOrder;
            }
        }
        if (maxFootprintContentOrder != int.MinValue && maxOrder < maxFootprintContentOrder + 1)
            maxOrder = maxFootprintContentOrder + 1;

        return maxOrder != int.MinValue ? maxOrder : terrainManager.CalculateTerrainSortingOrder(pivotX, pivotY, 0) + 10;
    }

    public void ReCalculateSortingOrderBasedOnHeight()
    {
        if (gridManager == null)
        {
            return;
        }

        for (int x = 0; x < gridManager.width; x++)
        {
            for (int y = 0; y < gridManager.height; y++)
            {

                GameObject cell = gridManager.gridArray[x, y];
                Cell cellComponent = gridManager.GetCell(x, y);

                if (cellComponent == null)
                {
                    DebugHelper.LogWarning($"Cell component missing at ({x}, {y})");
                    continue;
                }

                if (cellComponent.sortingOrder == -1001)
                {
                    continue;
                }

                List<GameObject> objectsToSort = new List<GameObject>();

                if (cell.GetComponent<SpriteRenderer>() != null)
                {
                    objectsToSort.Add(cell);
                }

                for (int i = 0; i < cell.transform.childCount; i++)
                {
                    GameObject child = cell.transform.GetChild(i).gameObject;
                    if (child.GetComponent<SpriteRenderer>() != null)
                    {
                        objectsToSort.Add(child);
                    }
                }
                int maxCellSortingOrder = cellComponent.sortingOrder;
                foreach (GameObject obj in objectsToSort)
                {
                    int newSortingOrder;
                    if (terrainManager != null && terrainManager.IsWaterSlopeObject(obj))
                    {
                        newSortingOrder = terrainManager.CalculateWaterSlopeSortingOrder(x, y);
                    }
                    else if (terrainManager != null && terrainManager.IsBayObject(obj))
                    {
                        newSortingOrder = terrainManager.CalculateBayShoreSortingOrder(x, y);
                    }
                    else if (terrainManager != null && terrainManager.IsSeaLevelWaterObject(obj))
                    {
                        // Water below terrain at height 1; offset < DEPTH_MULTIPLIER (100) so depth ordering works with water slopes
                        newSortingOrder = terrainManager.CalculateTerrainSortingOrder(x, y, 0) - 50;
                    }
                    else if (terrainManager != null && terrainManager.IsLandSlopeObject(obj))
                    {
                        newSortingOrder = terrainManager.CalculateSlopeSortingOrder(x, y, cellComponent.height);
                    }
                    else
                    {
                        int terrainOrder = terrainManager != null
                            ? terrainManager.CalculateTerrainSortingOrder(x, y, cellComponent.height)
                            : 0;
                        if (cellComponent.forestObject != null && cellComponent.forestObject == obj)
                        {
                            newSortingOrder = terrainOrder + 5;
                        }
                        else
                        {
                            Zone zone = obj.GetComponent<Zone>();
                            if (zone != null)
                            {
                                if (zone.zoneType == Zone.ZoneType.Water)
                                {
                                    // Bridge cell: ensure water is explicitly below the bridge
                                    bool hasRoadInCell = false;
                                    for (int j = 0; j < cell.transform.childCount; j++)
                                    {
                                        Zone z = cell.transform.GetChild(j).GetComponent<Zone>();
                                        if (z != null && z.zoneType == Zone.ZoneType.Road) { hasRoadInCell = true; break; }
                                    }
                                    if (hasRoadInCell && terrainManager != null)
                                    {
                                        int bridgeOrder = terrainManager.CalculateTerrainSortingOrder(x, y, 1) + GridSortingOrderService.ROAD_SORTING_OFFSET;
                                        newSortingOrder = bridgeOrder - 100;  // Guarantee water renders below bridge
                                    }
                                    else
                                    {
                                        // Water below terrain at height 1; offset < DEPTH_MULTIPLIER (100) so depth ordering works with water slopes
                                        newSortingOrder = terrainManager.CalculateTerrainSortingOrder(x, y, 0) - 50;
                                    }
                                }
                                else if (zone.zoneType == Zone.ZoneType.Road)
                                {
                                    // Bridge over water: use height 1 so bridge renders above water (matches SetRoadSortingOrder)
                                    int effectiveHeight = (cellComponent.height == 0) ? 1 : cellComponent.height;
                                    newSortingOrder = (terrainManager != null
                                        ? terrainManager.CalculateTerrainSortingOrder(x, y, effectiveHeight)
                                        : 0) + GridSortingOrderService.ROAD_SORTING_OFFSET;
                                }
                                else if (zone.zoneCategory == Zone.ZoneCategory.Zoning)
                                    newSortingOrder = terrainOrder + 0;
                                else if (zone.zoneCategory == Zone.ZoneCategory.Building)
                                {
                                    // Multi-cell buildings: use max order over footprint so the whole building renders in front of adjacent terrain/forest
                                    if (cellComponent.buildingSize > 1)
                                        newSortingOrder = GetMultiCellBuildingMaxSortingOrder(x, y, cellComponent.buildingSize);
                                    else
                                        newSortingOrder = terrainOrder + 10;
                                }
                                else
                                    newSortingOrder = terrainOrder;
                            }
                            else if (obj.GetComponent<PowerPlant>() != null || obj.GetComponent<WaterPlant>() != null)
                            {
                                if (cellComponent.buildingSize > 1)
                                    newSortingOrder = GetMultiCellBuildingMaxSortingOrder(x, y, cellComponent.buildingSize);
                                else
                                    newSortingOrder = terrainOrder + 10;
                            }
                            else
                            {
                                newSortingOrder = terrainOrder;
                            }
                        }
                        // Cells east/south/west of a multi-cell building: draw their content on top. Skip for water so it stays below bridges/roads.
                        Zone zoneForBoost = obj.GetComponent<Zone>();
                        if (zoneForBoost == null || zoneForBoost.zoneType != Zone.ZoneType.Water)
                        {
                            if (TryGetEastAdjacentBuildingOrder(x, y, out int eastBuildingOrder))
                                newSortingOrder = Mathf.Max(newSortingOrder, eastBuildingOrder + 1);
                            if (TryGetSouthAdjacentBuildingOrder(x, y, out int southBuildingOrder))
                                newSortingOrder = Mathf.Max(newSortingOrder, southBuildingOrder + 1);
                            if (TryGetWestAdjacentBuildingOrder(x, y, out int westBuildingOrder))
                                newSortingOrder = Mathf.Max(newSortingOrder, westBuildingOrder + 1);
                        }
                    }
                    SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>();
                    foreach (SpriteRenderer sr in renderers)
                    {
                        if (sr != null)
                            sr.sortingOrder = newSortingOrder;
                    }
                    if (newSortingOrder > maxCellSortingOrder)
                        maxCellSortingOrder = newSortingOrder;
                }
                cellComponent.sortingOrder = maxCellSortingOrder;
            }
        }
    }
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
                    Cell cellComponent = gridManager.GetCell(x, y);
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
                Cell cellComponent = gridManager.GetCell(x, y);

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

public enum PlacementType
{
    Forest,
    Water,
    Building,
    Zone,
    Infrastructure
}

[System.Serializable]
public struct EnvironmentalBonus
{
    public float desirability;
    public int adjacentForests;
    public int adjacentWater;
    public Forest.ForestType forestType;

    public float GetTotalBonus()
    {
        float bonus = desirability + (adjacentForests * 2f) + (adjacentWater * 3f);

        // Add bonus based on forest type on this cell
        switch (forestType)
        {
            case Forest.ForestType.Sparse:
                bonus += 1f;
                break;
            case Forest.ForestType.Medium:
                bonus += 2f;
                break;
            case Forest.ForestType.Dense:
                bonus += 3f;
                break;
        }

        return bonus;
    }
}

[System.Serializable]
public struct GeographyData
{
    [Header("Terrain Data")]
    public bool hasTerrainData;
    public int terrainWidth;
    public int terrainHeight;

    [Header("Water Data")]
    public bool hasWaterData;
    public int waterCellCount;

    [Header("Forest Data")]
    public bool hasForestData;
    public int forestCellCount;
    public float forestCoveragePercentage;
    public int sparseForestCount;
    public int mediumForestCount;
    public int denseForestCount;
}

[System.Serializable]
public struct ForestRegionInfo
{
    public int sparseCount;
    public int mediumCount;
    public int denseCount;
    public int totalCells;
    public float forestCoverage;

    public int GetTotalForests()
    {
        return sparseCount + mediumCount + denseCount;
    }
}
}
