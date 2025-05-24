using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages forest placement, removal, and environmental effects in the city simulation.
/// Works with the IForest interface to support different forest types (Sparse, Medium, Dense).
/// </summary>
public class ForestManager : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;
    public WaterManager waterManager;
    public CityStats cityStats;
    public EconomyManager economyManager;
    public UIManager uiManager;
    
    [Header("Forest Prefabs")]
    public GameObject sparseForestPrefab;
    public GameObject mediumForestPrefab;
    public GameObject denseForestPrefab;
    
    [Header("Forest Configuration")]
    public float desirabilityPerAdjacentForest = 2.0f; // Desirability bonus per adjacent forest
    public float demandBoostPercentage = 0.5f; // Percentage increase in demand per forest cell
    
    private ForestMap forestMap;
    private Dictionary<Forest.ForestType, int> forestTypeCounts;
    
    // Define the initial forest cells matrix (ForestType values)
    private Forest.ForestType[,] initialForestCells = new Forest.ForestType[,] {
        {Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None},
        {Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None},
        {Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None},
        {Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None},
        {Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.Medium, Forest.ForestType.Dense, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None},
        {Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.Sparse, Forest.ForestType.Medium, Forest.ForestType.Dense, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None},
        {Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.Sparse, Forest.ForestType.Medium, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None},
        {Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None},
        {Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None},
        {Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None},
        {Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None},
        {Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None},
        {Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None},
        {Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None},
        {Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None},
        {Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None},
        {Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.Dense, Forest.ForestType.Medium, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None},
        {Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.Sparse, Forest.ForestType.Medium, Forest.ForestType.Dense, Forest.ForestType.Medium, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None},
        {Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.Sparse, Forest.ForestType.Medium, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None},
        {Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None, Forest.ForestType.None}
    };

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
            forestMap = new ForestMap(gridManager.width, gridManager.height);
            
            // Initialize from predefined matrix
            forestMap.InitializeFromMatrix(initialForestCells);
            
            // Apply forest visuals to the grid
            UpdateForestVisuals();
            
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

    public bool PlaceForest(Vector3 gridPosition, IForest selectedForest)
    {
        int x = (int)gridPosition.x;
        int y = (int)gridPosition.y;

        if (forestMap == null || !forestMap.IsValidPosition(x, y))
            return false;

        if (!CanPlaceForestAt(x, y))
            return false;

        if (!CanAffordForest(selectedForest))
        {
            Debug.Log($"Insufficient funds to place {selectedForest.ForestType} forest! Cost: {selectedForest.ConstructionCost}");
            return false;
        }

        if (!HasSufficientWaterForForest(selectedForest))
        {
            Debug.Log($"Insufficient water to place {selectedForest.ForestType} forest! Required: {selectedForest.WaterConsumption}");
            return false;
        }

        GameObject cell = gridManager.gridArray[x, y];
        Cell cellComponent = cell.GetComponent<Cell>();
        
        GameObject forestPrefab = uiManager.GetForestPrefabForType(selectedForest.ForestType);

        if (forestPrefab == null)
        {
            Debug.LogError($"No prefab available for {selectedForest.ForestType} forest!");
            return false;
        }

        Vector2 worldPos = gridManager.GetWorldPosition(x, y);
        GameObject forestObject = Instantiate(forestPrefab, worldPos, Quaternion.identity);
        
        forestObject.transform.SetParent(cell.transform);
        
        // Pass grid coordinates and cell height for proper isometric sorting
        SetForestSortingOrder(forestObject, x, y, cellComponent.height);
        
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
        
        if (selectedForest.GameObjectReference != null && selectedForest.GameObjectReference != forestObject)
        {
            Destroy(selectedForest.GameObjectReference);
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

        // Cannot place on buildings (check if cell has occupied building)
        GameObject cell = gridManager.gridArray[x, y];
        Cell cellComponent = cell.GetComponent<Cell>();
        
        if (cellComponent.occupiedBuilding != null)
            return false;

        return true;
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

        GameObject forestPrefab = GetPrefabForForestType(forestType);
        if (forestPrefab == null) return;

        Vector2 worldPos = gridManager.GetWorldPosition(x, y);
        GameObject forestObject = Instantiate(forestPrefab, worldPos, Quaternion.identity);
        
        forestObject.transform.SetParent(cell.transform);
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
                    sparseForestCount = forestTypeCounts[Forest.ForestType.Sparse],
                    mediumForestCount = forestTypeCounts[Forest.ForestType.Medium],
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

    private void SetForestSortingOrder(GameObject forestObject, int x, int y, int cellHeight)
    {
        SpriteRenderer spriteRenderer = forestObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            int baseSortingOrder = -(y * 10 + x) - (cellHeight * 100);
            
            spriteRenderer.sortingOrder = baseSortingOrder - 50;
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
