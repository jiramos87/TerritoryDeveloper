using UnityEngine;

/// <summary>
/// Represents a single cell in the game grid with properties for buildings, roads, forests, and environmental factors.
/// Updated to support the new Forest.ForestType system for better forest management.
/// </summary>
public class Cell : MonoBehaviour
{
    [Header("Road Connections")]
    public bool hasRoadAtLeft;
    public bool hasRoadAtTop;
    public bool hasRoadAtRight;
    public bool hasRoadAtBottom;
    
    [Header("Building Properties")]
    public int population;
    public int powerOutput;
    public int powerConsumption;
    public int waterConsumption;
    public string buildingType;
    public int buildingSize;
    public int happiness;
    public string prefabName;
    public bool isPivot;
    public PowerPlant powerPlant { get; set; }
    public WaterPlant waterPlant { get; set; }
    public GameObject occupiedBuilding { get; set; }
    public GameObject prefab { get; set; }

    [Header("Grid Position")]
    public int x;
    public int y;
    public int sortingOrder;
    public int height;

    [Header("Forest Properties")]
    public Forest.ForestType forestType = Forest.ForestType.None; // Updated from hasTree
    public string forestPrefabName; // Updated from treePrefabName
    public GameObject forestObject; // Updated from treeObject

    [Header("Desirability Properties")]
    public float desirability;
    public int closeForestCount; // Count of adjacent forest cells
    public int closeWaterCount; // For future water desirability

    [Header("Zone Properties")]
    public Zone.ZoneType zoneType;

    private string occupiedBuildingName;

    void Start()
    {
        ZoneManager zoneManager = FindObjectOfType<ZoneManager>();
        GameObject grassPrefab = zoneManager.GetGrassPrefab();
        
        // Initialize default values
        zoneType = Zone.ZoneType.Grass;
        hasRoadAtLeft = false;
        hasRoadAtTop = false;
        hasRoadAtRight = false;
        hasRoadAtBottom = false;
        population = 0;
        powerOutput = 0;
        powerConsumption = 0;
        waterConsumption = 0;
        happiness = 0;
        buildingType = "Grass";
        buildingSize = 1;
        powerPlant = null;
        waterPlant = null;
        prefab = grassPrefab;
        prefabName = grassPrefab.name;
        occupiedBuildingName = "";
        isPivot = false;
        height = 1;
        
        // Initialize forest properties
        forestType = Forest.ForestType.None;
        forestPrefabName = "";
        forestObject = null;
        
        // Initialize desirability properties
        desirability = 0f;
        closeForestCount = 0;
        closeWaterCount = 0;
    }

    #region Building Property Getters
    public string GetBuildingType()
    {
        return buildingType;
    }

    public int GetBuildingSize()
    {
        return buildingSize;
    }

    public int GetPopulation()
    {
        return population;
    }

    public int GetPowerOutput()
    {
        return powerOutput;
    }

    public int GetPowerConsumption()
    {
        return powerConsumption;
    }
    
    public int GetWaterConsumption()
    {
        return waterConsumption;
    }

    public int GetHappiness()
    {
        return happiness;
    }

    public Zone.ZoneType GetZoneType()
    {
        return zoneType;
    }

    public string GetBuildingName()
    {
        if (occupiedBuilding != null)
        {
            return occupiedBuilding.name;
        }
        return "";
    }

    public GameObject GetCellPrefab()
    {
        return prefab;
    }

    public int GetSortingOrder()
    {
        return sortingOrder;
    }
    #endregion

    #region Forest Methods
    /// <summary>
    /// Check if this cell has any forest (not None type)
    /// </summary>
    public bool HasForest()
    {
        return forestType != Forest.ForestType.None;
    }

    /// <summary>
    /// Get the forest type of this cell
    /// </summary>
    public Forest.ForestType GetForestType()
    {
        return forestType;
    }

    /// <summary>
    /// Set or update the forest on this cell
    /// </summary>
    public void SetForest(Forest.ForestType newForestType, string prefabName = "", GameObject forestGameObject = null)
    {
        forestType = newForestType;
        forestPrefabName = prefabName;
        forestObject = forestGameObject;
        
        if (forestType == Forest.ForestType.None)
        {
            // Clean up forest references when forest is removed
            forestPrefabName = "";
            if (forestObject != null)
            {
                Destroy(forestObject);
                forestObject = null;
            }
        }
    }

    /// <summary>
    /// Backward compatibility method for existing tree-based code
    /// </summary>
    public void SetTree(bool hasTreeValue, string prefabName = "", GameObject treeGameObject = null)
    {
        if (hasTreeValue)
        {
            // Default to medium forest for backward compatibility
            SetForest(Forest.ForestType.Medium, prefabName, treeGameObject);
        }
        else
        {
            SetForest(Forest.ForestType.None, "", null);
        }
    }

    /// <summary>
    /// Backward compatibility property
    /// </summary>
    public bool hasTree
    {
        get { return HasForest(); }
        set { SetTree(value); }
    }

    /// <summary>
    /// Backward compatibility property
    /// </summary>
    public string treePrefabName
    {
        get { return forestPrefabName; }
        set { forestPrefabName = value; }
    }

    /// <summary>
    /// Backward compatibility property
    /// </summary>
    public GameObject treeObject
    {
        get { return forestObject; }
        set { forestObject = value; }
    }

    /// <summary>
    /// Remove forest when building spawns on this cell
    /// </summary>
    public void RemoveForestForBuilding()
    {
        if (HasForest())
        {
            // Notify ForestManager to update statistics and adjacent desirability
            ForestManager forestManager = FindObjectOfType<ForestManager>();
            if (forestManager != null)
            {
                forestManager.RemoveForestFromCell(x, y, false); // false = don't refund cost since it's automatic removal
            }
        }
    }

    /// <summary>
    /// Backward compatibility method
    /// </summary>
    public void RemoveTreeForBuilding()
    {
        RemoveForestForBuilding();
    }
    #endregion

    #region Desirability Methods
    /// <summary>
    /// Calculate the total desirability of this cell based on nearby forests and water
    /// </summary>
    public void UpdateDesirability()
    {
        // Base desirability calculation
        float forestDesirability = closeForestCount * 2.0f; // Each adjacent forest adds 2.0 desirability
        float waterDesirability = closeWaterCount * 3.0f;   // Each adjacent water adds 3.0 desirability (future)
        
        desirability = forestDesirability + waterDesirability;
    }

    /// <summary>
    /// Get the current desirability value
    /// </summary>
    public float GetDesirability()
    {
        return desirability;
    }

    /// <summary>
    /// Add to the close forest count (called by ForestManager)
    /// </summary>
    public void AddCloseForest()
    {
        closeForestCount++;
        UpdateDesirability();
    }

    /// <summary>
    /// Remove from the close forest count (called by ForestManager)
    /// </summary>
    public void RemoveCloseForest()
    {
        closeForestCount = Mathf.Max(0, closeForestCount - 1);
        UpdateDesirability();
    }
    #endregion

    #region Save/Load Data Methods
    /// <summary>
    /// Get cell data for saving
    /// </summary>
    public CellData GetCellData()
    {
        CellData cellData = new CellData
        {
            hasRoadAtLeft = hasRoadAtLeft,
            hasRoadAtTop = hasRoadAtTop,
            hasRoadAtRight = hasRoadAtRight,
            hasRoadAtBottom = hasRoadAtBottom,
            population = population,
            powerOutput = powerOutput,
            powerConsumption = powerConsumption,
            waterConsumption = waterConsumption,
            buildingType = buildingType,
            buildingSize = buildingSize,
            x = x,
            y = y,
            happiness = happiness,
            prefabName = prefabName,
            zoneType = zoneType.ToString(),
            occupiedBuildingName = occupiedBuilding != null ? occupiedBuilding.name : "",
            isPivot = isPivot,
            sortingOrder = sortingOrder,
            height = height,
            powerPlant = powerPlant,
            waterPlant = waterPlant,
            
            // Forest properties (updated)
            forestType = forestType.ToString(), // Store as string for serialization
            forestPrefabName = forestPrefabName,
            
            // Backward compatibility for old save files
            hasTree = HasForest(),
            treePrefabName = forestPrefabName,
            
            // Desirability properties
            desirability = desirability,
            closeForestCount = closeForestCount,
            closeWaterCount = closeWaterCount
        };

        return cellData;
    }

    /// <summary>
    /// Set cell data from loaded save
    /// </summary>
    public void SetCellData(CellData cellData)
    {
        hasRoadAtLeft = cellData.hasRoadAtLeft;
        hasRoadAtTop = cellData.hasRoadAtTop;
        hasRoadAtRight = cellData.hasRoadAtRight;
        hasRoadAtBottom = cellData.hasRoadAtBottom;
        population = cellData.population;
        powerOutput = cellData.powerOutput;
        powerConsumption = cellData.powerConsumption;
        waterConsumption = cellData.waterConsumption;
        buildingType = cellData.buildingType;
        buildingSize = cellData.buildingSize;
        x = cellData.x;
        y = cellData.y;
        happiness = cellData.happiness;
        prefabName = cellData.prefabName;
        zoneType = (Zone.ZoneType)System.Enum.Parse(typeof(Zone.ZoneType), cellData.zoneType);
        occupiedBuildingName = cellData.occupiedBuildingName;
        isPivot = cellData.isPivot;
        height = cellData.height;
        powerPlant = cellData.powerPlant;
        waterPlant = cellData.waterPlant;
        
        // Forest properties (updated with backward compatibility)
        if (!string.IsNullOrEmpty(cellData.forestType))
        {
            // New save format with forest types
            if (System.Enum.TryParse(cellData.forestType, out Forest.ForestType parsedForestType))
            {
                forestType = parsedForestType;
            }
            else
            {
                forestType = Forest.ForestType.None;
            }
            forestPrefabName = cellData.forestPrefabName;
        }
        else
        {
            // Backward compatibility with old save format
            if (cellData.hasTree)
            {
                forestType = Forest.ForestType.Medium; // Default to medium for old saves
                forestPrefabName = cellData.treePrefabName;
            }
            else
            {
                forestType = Forest.ForestType.None;
                forestPrefabName = "";
            }
        }
        
        // Desirability properties
        desirability = cellData.desirability;
        closeForestCount = cellData.closeForestCount;
        closeWaterCount = cellData.closeWaterCount;
    }
    #endregion
}
