using UnityEngine;

/// <summary>
/// Serializable data structure for saving and loading cell information.
/// Updated to support the new Forest.ForestType system while maintaining backward compatibility.
/// </summary>
[System.Serializable]
public class CellData
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
    public string zoneType;
    public GameObject occupiedBuilding;
    public string occupiedBuildingName;
    public bool isPivot;
    public PowerPlant powerPlant;
    public WaterPlant waterPlant;
    public Vector2 transformPosition;
    [Header("Grid Position")]
    public int x;
    public int y;
    public int sortingOrder;
    public int height;

    [Header("Forest Properties - New System")]
    public string forestType; // Store Forest.ForestType as string for serialization
    public string forestPrefabName;

    [Header("Forest Properties - Backward Compatibility")]
    public bool hasTree; // Keep for backward compatibility with old saves
    public string treePrefabName; // Keep for backward compatibility with old saves

    [Header("Desirability Properties")]
    public float desirability;
    public int closeForestCount;
    public int closeWaterCount;
    public GameObject prefab;

    /// <summary>
    /// Constructor with parameters for easy creation
    /// </summary>
    public CellData(int x, int y, int height = 1)
    {
        SetDefaults();
        this.x = x;
        this.y = y;
        this.height = height;
    }

    public void SetDefaults()
    {
        this.height = 1;
        this.sortingOrder = 0;
        this.transformPosition = new Vector2(0, 0);
        this.prefab = null;
        this.forestType = Forest.ForestType.None.ToString();
        this.forestPrefabName = "";
        this.hasTree = false;
        this.treePrefabName = "";
        this.desirability = 0f;
        this.closeForestCount = 0;
        this.closeWaterCount = 0;
        this.prefab = null;
        this.hasRoadAtLeft = false;
        this.hasRoadAtTop = false;
        this.hasRoadAtRight = false;
        this.hasRoadAtBottom = false;
        this.population = 0;
        this.powerOutput = 0;
        this.powerConsumption = 0;
        this.waterConsumption = 0;
        this.buildingType = "Grass";
        this.buildingSize = 1;
        this.happiness = 0;
        this.prefabName = "";
        this.zoneType = Zone.ZoneType.Grass.ToString();
        this.occupiedBuildingName = "";
        this.isPivot = false;
        this.powerPlant = null;
        this.waterPlant = null;
        this.occupiedBuilding = null;
    }

    /// <summary>
    /// Get the forest type as enum (with error handling)
    /// </summary>
    public Forest.ForestType GetForestType()
    {
        if (string.IsNullOrEmpty(forestType))
        {
            // Check backward compatibility
            return hasTree ? Forest.ForestType.Medium : Forest.ForestType.None;
        }

        if (System.Enum.TryParse(forestType, out Forest.ForestType result))
        {
            return result;
        }

        // Default fallback
        return Forest.ForestType.None;
    }

    /// <summary>
    /// Set the forest type from enum
    /// </summary>
    public void SetForestType(Forest.ForestType newForestType)
    {
        forestType = newForestType.ToString();

        // Update backward compatibility fields
        hasTree = newForestType != Forest.ForestType.None;
        if (!hasTree)
        {
            treePrefabName = "";
            forestPrefabName = "";
        }
    }

    /// <summary>
    /// Get the zone type as enum
    /// </summary>
    public Zone.ZoneType GetZoneType()
    {
        if (System.Enum.TryParse(zoneType, out Zone.ZoneType result))
        {
            return result;
        }
        return Zone.ZoneType.Grass; // Default fallback
    }

    /// <summary>
    /// Set the zone type from enum
    /// </summary>
    public void SetZoneType(Zone.ZoneType newZoneType)
    {
        zoneType = newZoneType.ToString();
    }

    /// <summary>
    /// Check if this cell has any forest
    /// </summary>
    public bool HasForest()
    {
        return GetForestType() != Forest.ForestType.None;
    }

    /// <summary>
    /// Validate and fix any inconsistent data
    /// </summary>
    public void ValidateData()
    {
        // Ensure population is not negative
        population = Mathf.Max(0, population);

        // Ensure power values are not negative
        powerOutput = Mathf.Max(0, powerOutput);
        powerConsumption = Mathf.Max(0, powerConsumption);
        waterConsumption = Mathf.Max(0, waterConsumption);

        // Ensure building size is at least 1
        buildingSize = Mathf.Max(1, buildingSize);

        // Ensure height is at least 1
        height = Mathf.Max(1, height);

        // Ensure forest counts are not negative
        closeForestCount = Mathf.Max(0, closeForestCount);
        closeWaterCount = Mathf.Max(0, closeWaterCount);

        // Sync backward compatibility fields
        Forest.ForestType currentForestType = GetForestType();
        hasTree = currentForestType != Forest.ForestType.None;
        if (hasTree && string.IsNullOrEmpty(treePrefabName) && !string.IsNullOrEmpty(forestPrefabName))
        {
            treePrefabName = forestPrefabName;
        }

        // Ensure string fields are not null
        if (buildingType == null) buildingType = "Grass";
        if (prefabName == null) prefabName = "";
        if (zoneType == null) zoneType = Zone.ZoneType.Grass.ToString();
        if (occupiedBuildingName == null) occupiedBuildingName = "";
        if (forestType == null) forestType = Forest.ForestType.None.ToString();
        if (forestPrefabName == null) forestPrefabName = "";
        if (treePrefabName == null) treePrefabName = "";
    }

    /// <summary>
    /// Create a copy of this CellData
    /// </summary>
    public CellData Clone()
    {
        CellData clone = new CellData(x, y, height);

        // Copy all fields
        clone.hasRoadAtLeft = hasRoadAtLeft;
        clone.hasRoadAtTop = hasRoadAtTop;
        clone.hasRoadAtRight = hasRoadAtRight;
        clone.hasRoadAtBottom = hasRoadAtBottom;
        clone.population = population;
        clone.powerOutput = powerOutput;
        clone.powerConsumption = powerConsumption;
        clone.waterConsumption = waterConsumption;
        clone.buildingType = buildingType;
        clone.buildingSize = buildingSize;
        clone.happiness = happiness;
        clone.prefabName = prefabName;
        clone.zoneType = zoneType;
        clone.occupiedBuildingName = occupiedBuildingName;
        clone.isPivot = isPivot;
        clone.powerPlant = powerPlant;
        clone.waterPlant = waterPlant;
        clone.x = x;
        clone.y = y;
        clone.sortingOrder = sortingOrder;
        clone.height = height;

        // Forest properties
        clone.forestType = forestType;
        clone.forestPrefabName = forestPrefabName;
        clone.hasTree = hasTree;
        clone.treePrefabName = treePrefabName;

        // Desirability properties
        clone.desirability = desirability;
        clone.closeForestCount = closeForestCount;
        clone.closeWaterCount = closeWaterCount;

        return clone;
    }
}
