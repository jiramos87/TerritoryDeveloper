using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages forest placement, removal, and environmental effects in the city simulation.
/// Works with the IForest interface to support different forest types (Sparse, Dense, Dense).
/// </summary>
public class ForestManager : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;
    public WaterManager waterManager;
    public CityStats cityStats;
    public EconomyManager economyManager;
    public UIManager uiManager;
    public GameNotificationManager gameNotificationManager;
    public TerrainManager terrainManager;

    [Header("Forest Prefabs")]
    public GameObject sparseForestPrefab;
    public GameObject mediumForestPrefab;
    public GameObject denseForestPrefab;

    [Header("Forest Slope Prefabs (Medium)")]
    public GameObject forestNorthSlopePrefab;
    public GameObject forestSouthSlopePrefab;
    public GameObject forestEastSlopePrefab;
    public GameObject forestWestSlopePrefab;
    public GameObject forestNorthEastSlopePrefab;
    public GameObject forestNorthWestSlopePrefab;
    public GameObject forestSouthEastSlopePrefab;
    public GameObject forestSouthWestSlopePrefab;
    public GameObject forestNorthEastUpSlopePrefab;
    public GameObject forestNorthWestUpSlopePrefab;
    public GameObject forestSouthEastUpSlopePrefab;
    public GameObject forestSouthWestUpSlopePrefab;

    [Header("Forest Configuration")]
    public float desirabilityPerAdjacentForest = 2.0f; // Desirability bonus per adjacent forest
    public float demandBoostPercentage = 0.5f; // Percentage increase in demand per forest cell

    private ForestMap forestMap;
    private Dictionary<Forest.ForestType, int> forestTypeCounts;

    void Start()
    {
        if (gridManager == null)
            gridManager = FindObjectOfType<GridManager>();
        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();
        if (cityStats == null)
            cityStats = FindObjectOfType<CityStats>();
        if (economyManager == null)
            economyManager = FindObjectOfType<EconomyManager>();

        if (uiManager == null)
            uiManager = FindObjectOfType<UIManager>();
        // Use same TerrainManager as GridManager so we get the heightMap from InitializeHeightMap (correct slope prefabs).
        if (terrainManager == null && gridManager != null && gridManager.terrainManager != null)
            terrainManager = gridManager.terrainManager;
        if (terrainManager == null)
            terrainManager = FindObjectOfType<TerrainManager>();

        InitializeForestTypeCounts();
    }

    private void InitializeForestTypeCounts()
    {
        forestTypeCounts = new Dictionary<Forest.ForestType, int>
        {
            { Forest.ForestType.None, 0 },
            { Forest.ForestType.Sparse, 0 },
            { Forest.ForestType.Medium, 0 },
            { Forest.ForestType.Dense, 0 }
        };
    }

    public void InitializeForestMap()
    {
        if (gridManager != null)
        {
            // Use same TerrainManager as GridManager so heightMap is available (slope prefabs). Start() order is not guaranteed.
            if (terrainManager == null && gridManager.terrainManager != null)
                terrainManager = gridManager.terrainManager;
            if (terrainManager == null)
                terrainManager = FindObjectOfType<TerrainManager>();

            forestMap = new ForestMap(gridManager.width, gridManager.height);

            // Build int matrix (0 = None, 1 = Sparse, 2 = Medium, 3 = Dense); only place forest where validation allows
            int gridWidth = gridManager.width;
            int gridHeight = gridManager.height;
            int[,] initialForestCells = new int[gridHeight, gridWidth];
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    if (CanPlaceForestAt(x, y))
                        initialForestCells[y, x] = (Random.value < 0.5f) ? 2 : 0; // 2 = Medium (default type)
                    else
                        initialForestCells[y, x] = 0;
                }
            }

            forestMap.InitializeFromIntMatrix(initialForestCells);

            // Apply forest visuals only when TerrainManager has heightMap (needed for slope prefab selection).
            if (terrainManager != null)
            {
                terrainManager.EnsureHeightMapLoaded();
                if (terrainManager.GetHeightMap() != null)
                {
                    UpdateForestVisuals();
                }
                else
                {
                    StartCoroutine(DeferredUpdateForestVisuals());
                }
            }
            else
            {
                UpdateForestVisuals();
            }

            // Update statistics
            UpdateForestStatistics();

            // Calculate initial desirability for all cells
            UpdateAllCellDesirability();
        }
    }

    public bool IsForestAt(int x, int y)
    {
        if (forestMap == null) return false;
        return forestMap.GetForestType(x, y) != Forest.ForestType.None;
    }

    public Forest.ForestType GetForestTypeAt(int x, int y)
    {
        if (forestMap == null) return Forest.ForestType.None;
        return forestMap.GetForestType(x, y);
    }

    public bool PlaceForest(Vector2 gridPosition, IForest selectedForest)
    {
        int x = (int)gridPosition.x;
        int y = (int)gridPosition.y;

        if (!gridManager.IsValidGridPosition(gridPosition))
        {
            Debug.LogWarning($"Cannot place forest at invalid position: ({x}, {y})");
            return false;
        }

        Cell cellComponent = gridManager.GetCell(x, y);

        if (cellComponent == null)
        {
            Debug.LogError($"Cell component is null at position: ({x}, {y})");
            return false;
        }

        if (forestMap == null || !forestMap.IsValidPosition(x, y))
            return false;

        if (!CanPlaceForestAt(x, y))
            return false;

        if (!CanAffordForest(selectedForest))
        {
            gameNotificationManager.PostInfo($"Insufficient funds to place {selectedForest.ForestType} forest! Cost: {selectedForest.ConstructionCost}");
            return false;
        }

        if (!HasSufficientWaterForForest(selectedForest))
        {
            gameNotificationManager.PostInfo($"Insufficient water to place {selectedForest.ForestType} forest! Required: {selectedForest.WaterConsumption}");
            return false;
        }

        GameObject forestPrefab = GetForestPrefabForCell(x, y, selectedForest.ForestType);

        if (forestPrefab == null)
        {
            Debug.LogError($"No prefab available for {selectedForest.ForestType} forest!");
            return false;
        }

        Vector2 worldPos = cellComponent.transformPosition;

        int height = cellComponent.GetCellInstanceHeight();

        Quaternion rotation = forestPrefab.transform.rotation;
        GameObject forestObject = Instantiate(forestPrefab, worldPos, rotation);

        forestObject.transform.SetParent(cellComponent.gameObject.transform);

        SetForestSortingOrder(forestObject, x, y, height);

        cellComponent.SetTree(true, selectedForest.ForestType.ToString(), forestObject);

        forestMap.SetForestType(x, y, selectedForest.ForestType);

        if (selectedForest.ConstructionCost > 0)
        {
            economyManager.SpendMoney(selectedForest.ConstructionCost);
        }

        if (selectedForest.WaterConsumption > 0)
        {
            waterManager.AddWaterConsumption(selectedForest.WaterConsumption);
        }

        UpdateAdjacentDesirability(x, y, true);
        UpdateForestStatistics();

        // Only destroy the template instance if it's still valid (not already destroyed)
        var forestMono = selectedForest as MonoBehaviour;
        if (forestMono != null && forestMono.gameObject != forestObject)
        {
            Destroy(forestMono.gameObject);
        }

        return true;
    }

    public bool RemoveForestFromCell(int x, int y, bool refundCost = false)
    {
        if (forestMap == null || !forestMap.IsValidPosition(x, y))
            return false;

        Forest.ForestType currentType = forestMap.GetForestType(x, y);
        if (currentType == Forest.ForestType.None)
            return false;

        // Get the cell and remove forest
        GameObject cell = gridManager.gridArray[x, y];
        Cell cellComponent = cell.GetComponent<Cell>();

        // Store water consumption for refund calculation
        int waterToRefund = GetWaterConsumptionForForestType(currentType);
        int costToRefund = GetConstructionCostForForestType(currentType);

        // Remove the forest GameObject
        cellComponent.SetTree(false);

        // Update forest map
        forestMap.SetForestType(x, y, Forest.ForestType.None);

        // Refund costs if requested (not for automatic building removal)
        if (refundCost)
        {
            if (waterToRefund > 0)
            {
                waterManager.RemoveWaterConsumption(waterToRefund);
            }
            if (costToRefund > 0)
            {
                economyManager.AddMoney(costToRefund / 2); // 50% refund
            }
        }

        // Update adjacent cell desirability
        UpdateAdjacentDesirability(x, y, false);

        // Update statistics
        UpdateForestStatistics();

        return true;
    }

    private bool CanPlaceForestAt(int x, int y)
    {
        // Cannot place if already has forest
        if (forestMap.GetForestType(x, y) != Forest.ForestType.None)
            return false;

        // Cannot place on water
        if (waterManager != null && waterManager.IsWaterAt(x, y))
            return false;

        GameObject cell = gridManager.gridArray[x, y];
        Cell cellComponent = cell.GetComponent<Cell>();

        // Cannot place on river/coast edge (cell adjacent to water / height 0)
        if (IsRiverOrCoastEdge(x, y))
            return false;

        // Cannot place on roads or interstate
        if (cellComponent.zoneType == Zone.ZoneType.Road)
            return false;
        if (cellComponent.isInterstate)
            return false;

        // Cannot place on buildings (occupied building or zone type is a building)
        if (cellComponent.occupiedBuilding != null)
            return false;
        if (IsZoneTypeBlockingForest(cellComponent.zoneType))
            return false;

        return true;
    }

    /// <summary>
    /// True if this cell is on river/coast edge: land cell with at least one orthogonal neighbor at height 0 (water).
    /// </summary>
    private bool IsRiverOrCoastEdge(int x, int y)
    {
        int cellHeight = GetCellHeight(x, y);
        if (cellHeight <= TerrainManager.SEA_LEVEL)
            return true; // Water or below sea level
        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };
        for (int i = 0; i < 4; i++)
        {
            int nx = x + dx[i];
            int ny = y + dy[i];
            if (gridManager.IsValidGridPosition(new Vector2(nx, ny)))
            {
                int neighborHeight = GetCellHeight(nx, ny);
                if (neighborHeight == TerrainManager.SEA_LEVEL)
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Height map used for terrain logic (river edge, etc.). Prefer terrainManager; fallback to gridManager.terrainManager
    /// so we use initial/terrain heights, not Cell.height (water slope cells have Cell.height=0 but terrain height=1).
    /// </summary>
    private HeightMap GetTerrainHeightMap()
    {
        if (terrainManager != null && terrainManager.GetHeightMap() != null)
            return terrainManager.GetHeightMap();
        if (gridManager != null && gridManager.terrainManager != null && gridManager.terrainManager.GetHeightMap() != null)
            return gridManager.terrainManager.GetHeightMap();
        return null;
    }

    private int GetCellHeight(int x, int y)
    {
        HeightMap heightMap = GetTerrainHeightMap();
        if (heightMap != null && heightMap.IsValidPosition(x, y))
            return heightMap.GetHeight(x, y);
        GameObject cell = gridManager.gridArray[x, y];
        if (cell != null)
        {
            Cell c = cell.GetComponent<Cell>();
            if (c != null)
                return c.height;
        }
        return TerrainManager.SEA_LEVEL;
    }

    private bool IsZoneTypeBlockingForest(Zone.ZoneType zoneType)
    {
        switch (zoneType)
        {
            case Zone.ZoneType.Road:
            case Zone.ZoneType.Water:
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
                return true;
            default:
                return false;
        }
    }

    private bool CanAffordForest(IForest forest)
    {
        if (economyManager == null || forest.ConstructionCost <= 0)
            return true; // Free forests or no economy manager

        return economyManager.GetCurrentMoney() >= forest.ConstructionCost;
    }

    private bool HasSufficientWaterForForest(IForest forest)
    {
        if (cityStats == null || forest.WaterConsumption <= 0)
            return true; // Allow if we can't check or no water required

        // Check if city has enough water capacity
        int currentWaterConsumption = cityStats.GetTotalWaterConsumption();
        int currentWaterOutput = cityStats.GetTotalWaterOutput();

        return (currentWaterConsumption + forest.WaterConsumption) <= currentWaterOutput;
    }

    private IEnumerator DeferredUpdateForestVisuals()
    {
        yield return null;
        if (terrainManager != null)
            terrainManager.EnsureHeightMapLoaded();
        // Always place forest visuals; if heightMap is still null we use flat prefabs so the map is never empty.
        if (forestMap != null && gridManager != null)
            UpdateForestVisuals();
    }

    public void UpdateForestVisuals()
    {
        if (forestMap == null || gridManager == null) return;

        for (int x = 0; x < gridManager.width; x++)
        {
            for (int y = 0; y < gridManager.height; y++)
            {
                Forest.ForestType forestType = forestMap.GetForestType(x, y);
                if (forestType != Forest.ForestType.None)
                {
                    PlaceForestVisual(x, y, forestType);
                }
            }
        }
    }

    private void PlaceForestVisual(int x, int y, Forest.ForestType forestType)
    {
        GameObject cell = gridManager.gridArray[x, y];
        Cell cellComponent = cell.GetComponent<Cell>();

        // Don't place if cell already has a tree
        if (cellComponent.hasTree)
            return;

        GameObject forestPrefab = GetForestPrefabForCell(x, y, forestType);
        if (forestPrefab == null) return;

        Vector2 worldPos = cellComponent.transformPosition;
        // Use prefab's default rotation so slope prefabs keep their authored orientation (avoids "upside down" on slopes).
        Quaternion rotation = forestPrefab.transform.rotation;
        GameObject forestObject = Instantiate(forestPrefab, worldPos, rotation);

        forestObject.transform.SetParent(cellComponent.gameObject.transform);
        SetForestSortingOrder(forestObject, x, y, cellComponent.height);

        cellComponent.SetTree(true, forestType.ToString(), forestObject);
    }

    private void UpdateAdjacentDesirability(int centerX, int centerY, bool forestAdded)
    {
        var adjacentPositions = forestMap.GetPositionsAdjacentToForest(centerX, centerY);

        foreach (var pos in adjacentPositions)
        {
            GameObject cell = gridManager.gridArray[pos.x, pos.y];
            Cell cellComponent = cell.GetComponent<Cell>();

            // Update close forest count
            if (forestAdded)
                cellComponent.closeForestCount++;
            else
                cellComponent.closeForestCount = Mathf.Max(0, cellComponent.closeForestCount - 1);

            // Recalculate desirability
            cellComponent.UpdateDesirability();
        }
    }

    private void UpdateAllCellDesirability()
    {
        if (gridManager == null || forestMap == null) return;

        for (int x = 0; x < gridManager.width; x++)
        {
            for (int y = 0; y < gridManager.height; y++)
            {
                GameObject cell = gridManager.gridArray[x, y];
                Cell cellComponent = cell.GetComponent<Cell>();

                // Calculate adjacent forest count
                cellComponent.closeForestCount = forestMap.GetAdjacentForestCount(x, y);

                // Update desirability
                cellComponent.UpdateDesirability();
            }
        }
    }

    private void UpdateForestStatistics()
    {
        if (forestMap == null || cityStats == null) return;

        // Count each forest type
        forestTypeCounts = forestMap.GetForestTypeCounts();

        int totalForestCells = 0;
        foreach (var count in forestTypeCounts.Values)
        {
            totalForestCells += count;
        }

        // Update CityStats with forest information
        if (cityStats.GetComponent<CityStats>())
        {
            cityStats.SendMessage("UpdateForestStats",
                new ForestStatistics
                {
                    totalForestCells = totalForestCells,
                    forestCoveragePercentage = forestMap.GetForestCoveragePercentage(),
                    sparseForestCount = forestTypeCounts[Forest.ForestType.Dense],
                    mediumForestCount = forestTypeCounts[Forest.ForestType.Dense],
                    denseForestCount = forestTypeCounts[Forest.ForestType.Dense]
                },
                SendMessageOptions.DontRequireReceiver);
        }
    }

    public float GetForestDemandBoost()
    {
        int totalForestCells = 0;
        foreach (var count in forestTypeCounts.Values)
        {
            totalForestCells += count;
        }
        return totalForestCells * demandBoostPercentage;
    }

    private GameObject GetPrefabForForestType(Forest.ForestType forestType)
    {
        switch (forestType)
        {
            case Forest.ForestType.Sparse:
                return sparseForestPrefab;
            case Forest.ForestType.Medium:
                return mediumForestPrefab;
            case Forest.ForestType.Dense:
                return denseForestPrefab;
            default:
                Debug.LogWarning($"No prefab assigned for forest type: {forestType}");
                return null;
        }
    }

    /// <summary>
    /// Returns the forest prefab to use for this cell based on terrain (flat vs slope). On slopes uses the 12 slope prefabs (medium).
    /// </summary>
    private GameObject GetForestPrefabForCell(int x, int y, Forest.ForestType forestType)
    {
        if (terrainManager == null)
            return GetPrefabForForestType(forestType);

        TerrainSlopeType slopeType = terrainManager.GetTerrainSlopeTypeAt(x, y);
        if (slopeType == TerrainSlopeType.Flat)
            return GetPrefabForForestType(forestType);

        GameObject slopePrefab = GetSlopeForestPrefab(slopeType);
        if (slopePrefab != null)
            return slopePrefab;
        return GetPrefabForForestType(forestType);
    }

    private GameObject GetSlopeForestPrefab(TerrainSlopeType slopeType)
    {
        switch (slopeType)
        {
            case TerrainSlopeType.North: return forestNorthSlopePrefab;
            case TerrainSlopeType.South: return forestSouthSlopePrefab;
            case TerrainSlopeType.East: return forestEastSlopePrefab;
            case TerrainSlopeType.West: return forestWestSlopePrefab;
            case TerrainSlopeType.NorthEast: return forestNorthEastSlopePrefab;
            case TerrainSlopeType.NorthWest: return forestNorthWestSlopePrefab;
            case TerrainSlopeType.SouthEast: return forestSouthEastSlopePrefab;
            case TerrainSlopeType.SouthWest: return forestSouthWestSlopePrefab;
            case TerrainSlopeType.NorthEastUp: return forestNorthEastUpSlopePrefab;
            case TerrainSlopeType.NorthWestUp: return forestNorthWestUpSlopePrefab;
            case TerrainSlopeType.SouthEastUp: return forestSouthEastUpSlopePrefab;
            case TerrainSlopeType.SouthWestUp: return forestSouthWestUpSlopePrefab;
            default: return null;
        }
    }

    private int GetWaterConsumptionForForestType(Forest.ForestType forestType)
    {
        // You might want to make this configurable or get it from a data source
        switch (forestType)
        {
            case Forest.ForestType.Sparse:
                return 5;
            case Forest.ForestType.Medium:
                return 7;
            case Forest.ForestType.Dense:
                return 10;
            default:
                return 0;
        }
    }

    private int GetConstructionCostForForestType(Forest.ForestType forestType)
    {
        // You might want to make this configurable or get it from a data source
        switch (forestType)
        {
            case Forest.ForestType.Sparse:
                return 0;
            case Forest.ForestType.Medium:
                return 0;
            case Forest.ForestType.Dense:
                return 0;
            default:
                return 0;
        }
    }

    // Forest draws above terrain but below buildings (TerrainManager.BUILDING_OFFSET = 10)
    private const int FOREST_SORTING_OFFSET = 5;

    private void SetForestSortingOrder(GameObject forestObject, int x, int y, int cellHeight)
    {
        int sortingOrder;
        if (terrainManager != null)
        {
            int height = cellHeight;
            if (terrainManager.GetHeightMap() != null)
            {
                height = terrainManager.GetHeightMap().GetHeight(x, y);
            }
            sortingOrder = terrainManager.CalculateTerrainSortingOrder(x, y, height) + FOREST_SORTING_OFFSET;
        }
        else
        {
            int baseSortingOrder = -(y * 10 + x) - (cellHeight * 100);
            sortingOrder = baseSortingOrder - 50;
        }

        SpriteRenderer[] renderers = forestObject.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sr in renderers)
        {
            if (sr != null)
                sr.sortingOrder = sortingOrder;
        }
    }

    public ForestStatistics GetForestStatistics()
    {
        if (forestMap == null)
        {
            return new ForestStatistics
            {
                totalForestCells = 0,
                forestCoveragePercentage = 0f,
                sparseForestCount = 0,
                mediumForestCount = 0,
                denseForestCount = 0
            };
        }

        var counts = forestMap.GetForestTypeCounts();
        int totalForestCells = 0;
        foreach (var count in counts.Values)
        {
            totalForestCells += count;
        }

        return new ForestStatistics
        {
            totalForestCells = totalForestCells,
            forestCoveragePercentage = forestMap.GetForestCoveragePercentage(),
            sparseForestCount = counts[Forest.ForestType.Sparse],
            mediumForestCount = counts[Forest.ForestType.Medium],
            denseForestCount = counts[Forest.ForestType.Dense]
        };
    }

    public ForestMap GetForestMap() => forestMap;
}

/// <summary>
/// Data structure for forest statistics with type-specific counts
/// </summary>
[System.Serializable]
public struct ForestStatistics
{
    public int totalForestCells;
    public float forestCoveragePercentage;
    public int sparseForestCount;
    public int mediumForestCount;
    public int denseForestCount;
}
