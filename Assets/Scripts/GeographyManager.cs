using UnityEngine;

/// <summary>
/// GeographyManager coordinates the initialization and management of all geographical features:
/// terrain height, water bodies, and forests. It centralizes the loading of geographical data
/// and ensures proper initialization order.
/// </summary>
public class GeographyManager : MonoBehaviour
{
    [Header("Manager References")]
    public TerrainManager terrainManager;
    public WaterManager waterManager;
    public ForestManager forestManager;
    public GridManager gridManager;
    public ZoneManager zoneManager;
    
    [Header("Geography Configuration")]
    public bool initializeOnStart = true;
    public bool useTerrainForWater = true; // Whether to use terrain height for water placement
    
    // Current geographical data (for save/load operations)
    private GeographyData currentGeographyData;
    
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
        
        if (initializeOnStart)
        {
            InitializeGeography();
        }
    }

    public void InitializeGeography()
    {
        Debug.Log("GeographyManager: Initializing geography...");

        if (gridManager != null)
        {
            Debug.Log("GeographyManager: Initializing grid...");
            gridManager.InitializeGrid();
        }
        
        if (terrainManager != null)
        {
            Debug.Log("GeographyManager: Initializing terrain...");
            terrainManager.InitializeHeightMap();
        }
        
        if (waterManager != null)
        {
            Debug.Log("GeographyManager: Initializing water...");
            waterManager.InitializeWaterMap();
        }
        
        if (forestManager != null)
        {
            Debug.Log("GeographyManager: Initializing forests...");
            forestManager.InitializeForestMap();
        }
        
        currentGeographyData = CreateGeographyData();
        
        Debug.Log("GeographyManager: Geography initialization complete!");
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
        
        Debug.Log("GeographyManager: Loading geography from saved data...");
        
        // Load terrain data
        if (geographyData.hasTerrainData && terrainManager != null)
        {
            Debug.Log("GeographyManager: Loading terrain data...");
            terrainManager.InitializeHeightMap();
        }
        
        // Load water data
        if (geographyData.hasWaterData && waterManager != null)
        {
            Debug.Log($"GeographyManager: Loading water data ({geographyData.waterCellCount} water cells)...");
            waterManager.InitializeWaterMap();
        }
        
        // Load forest data
        if (geographyData.hasForestData && forestManager != null)
        {
            Debug.Log($"GeographyManager: Loading forest data ({geographyData.forestCellCount} forest cells)...");
            forestManager.InitializeForestMap();
        }
        
        Debug.Log("GeographyManager: Geography loading complete!");
    }

    public void ResetGeography()
    {
        Debug.Log("GeographyManager: Resetting geography...");
        
        // Reset forest data properly by removing all forests through ForestManager
        if (forestManager != null && forestManager.GetForestMap() != null)
        {
            ClearAllForests();
        }
        
        // Reset water data
        if (waterManager != null && waterManager.GetWaterMap() != null)
        {
            // Water manager would need a ClearAllWater method similar to forest
            Debug.Log("GeographyManager: Water reset not implemented yet");
        }
        
        // Note: Terrain is typically not reset as it's more structural
        
        currentGeographyData = new GeographyData();
        
        Debug.Log("GeographyManager: Geography reset complete!");
    }

    private void ClearAllForests()
    {
        if (forestManager == null || gridManager == null)
            return;

        ForestMap forestMap = forestManager.GetForestMap();
        if (forestMap == null)
            return;

        Debug.Log("GeographyManager: Clearing all forests...");

        // Get all forest positions before clearing
        var allForests = forestMap.GetAllForests();
        
        // Remove each forest properly through ForestManager
        foreach (var forestPos in allForests)
        {
            forestManager.RemoveForestFromCell(forestPos.x, forestPos.y, false);
        }

        Debug.Log($"GeographyManager: Cleared {allForests.Count} forest cells");
    }

    public void ClearForestsOfType(Forest.ForestType forestType)
    {
        if (forestManager == null || gridManager == null)
            return;

        ForestMap forestMap = forestManager.GetForestMap();
        if (forestMap == null)
            return;

        Debug.Log($"GeographyManager: Clearing all {forestType} forests...");

        // Get all forests of the specified type
        var forestsOfType = forestMap.GetAllForestsOfType(forestType);
        
        // Remove each forest properly through ForestManager
        foreach (var forestPos in forestsOfType)
        {
            forestManager.RemoveForestFromCell(forestPos.x, forestPos.y, false);
        }

        Debug.Log($"GeographyManager: Cleared {forestsOfType.Count} {forestType} forest cells");
    }

    public GeographyData GetCurrentGeographyData()
    {
        // Update with current state
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
                // Trees cannot be placed on water or existing forests
                if (waterManager != null && waterManager.IsWaterAt(x, y))
                    return false;
                if (forestManager != null && forestManager.IsForestAt(x, y))
                    return false;
                // Check for buildings
                if (gridManager != null && gridManager.gridArray != null)
                {
                    GameObject cell = gridManager.gridArray[x, y];
                    Cell cellComponent = cell.GetComponent<Cell>();
                    if (cellComponent != null && cellComponent.occupiedBuilding != null)
                        return false;
                }
                return true;
                
            case PlacementType.Water:
                // Water cannot be placed on existing water or forests
                if (waterManager != null && waterManager.IsWaterAt(x, y))
                    return false;
                if (forestManager != null && forestManager.IsForestAt(x, y))
                    return false;
                return true;
                
            case PlacementType.Building:
                // Buildings can be placed on most terrain but will remove trees
                if (waterManager != null && waterManager.IsWaterAt(x, y))
                    return false;
                return true;
                
            case PlacementType.Zone:
                // Zones can be placed anywhere except water
                if (waterManager != null && waterManager.IsWaterAt(x, y))
                    return false;
                return true;
                
            case PlacementType.Infrastructure:
                // Infrastructure (roads, power lines) can be placed anywhere except water
                if (waterManager != null && waterManager.IsWaterAt(x, y))
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
                GameObject cell = gridManager.gridArray[x, y];
                Cell cellComponent = cell.GetComponent<Cell>();
                
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
