using UnityEngine;
using Territory.Zones;
using Territory.Forests;
using Territory.Terrain;

namespace Territory.Core
{
/// <summary>
/// Represents a single cell in the game grid with properties for buildings, roads, forests, and environmental factors.
/// Updated to support the new Forest.ForestType system for better forest management.
/// </summary>
public class CityCell : CellBase
{
    [Header("Road Connections")]
    public bool hasRoadAtLeft;
    public bool hasRoadAtTop;
    public bool hasRoadAtRight;
    public bool hasRoadAtBottom;

    /// <summary>
    /// Runtime-only: cardinal grid position of the path predecessor from last path-based road placement, used so refresh
    /// prefab resolution matches path travel order on straight segments (BUG-51). Not saved in CellData.
    /// </summary>
    public bool hasRoadSegmentPrevHint;
    public Vector2Int roadSegmentPrevGrid;
    /// <summary>Runtime-only: path successor cell from last path-based placement (BUG-51 route-first refresh).</summary>
    public bool hasRoadSegmentNextHint;
    public Vector2Int roadSegmentNextGrid;
    /// <summary>Runtime-only: true when <see cref="roadRouteEntryStep"/> / <see cref="roadRouteExitStep"/> or segment hints are set.</summary>
    public bool hasRoadRouteDirHints;
    /// <summary>Runtime-only: cardinal step <c>curr - prev</c> on the placed path.</summary>
    public Vector2Int roadRouteEntryStep;
    /// <summary>Runtime-only: cardinal step <c>next - curr</c> on the placed path (zero if path end).</summary>
    public Vector2Int roadRouteExitStep;

    [Header("Building Properties")]
    public int population;
    public int powerOutput;
    public int powerConsumption;
    public int waterConsumption;
    public string buildingType;
    public int buildingSize;
    public int happiness;
    public string prefabName;
    /// <summary>Optional second terrain/shore prefab name (multi-child shore cells).</summary>
    public string secondaryPrefabName;
    public bool isPivot;
    public MonoBehaviour powerPlant { get; set; }
    public MonoBehaviour waterPlant { get; set; }
    public GameObject occupiedBuilding { get; set; }
    public GameObject prefab { get; set; }

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

    /// <summary>
    /// Stage 10 (city-sim-depth) — pivot-cell construction stage 0..3 driven by
    /// <see cref="Territory.Simulation.ConstructionStageController"/>. Runtime-only;
    /// NOT persisted to <see cref="CellData"/> this Stage. Re-enters stage 0 on load
    /// until persistence-Stage wires it.
    /// </summary>
    [Header("Construction Stage Properties — Stage 10 runtime-only (not in CellData)")]
    public int constructionStage;

    /// <summary>
    /// Stage 10 (city-sim-depth) — per-pivot accumulator (in-game days) used by
    /// <see cref="Territory.Simulation.ConstructionStageController.ProcessTick"/> to
    /// time per-stage advances at <c>effectiveTime / 4f</c>. Resets to 0 on each
    /// stage boundary. Runtime-only; NOT persisted to <see cref="CellData"/>.
    /// </summary>
    public float constructionDayAccumulator;

    [Header("Water Properties")]
    public WaterBodyType waterBodyType = WaterBodyType.None;

    /// <summary>
    /// <see cref="Territory.Terrain.WaterBody.id"/> for open water and dry shoreline membership (0 = none).
    /// For registered water cells, must match <see cref="Territory.Terrain.WaterMap"/> at this coordinate.
    /// </summary>
    public int waterBodyId;

    /// <summary>
    /// Logical cardinal cliff faces (N/S/E/W). Set when a risco exists toward that neighbor even if prefabs are skipped
    /// (hidden north/west faces or underwater cull).
    /// </summary>
    [Header("Terrain — cliff faces (logical)")]
    public CliffFaceFlags cliffFaces = CliffFaceFlags.None;

    [Header("Interstate Properties")]
    public bool isInterstate = false;

    private string occupiedBuildingName;

    public CityCell(CellData cellData)
    {
        this.x = cellData.x;
        this.y = cellData.y;
        this.height = cellData.height;
        this.sortingOrder = cellData.sortingOrder;
        this.transformPosition = cellData.transformPosition;
        this.prefab = cellData.prefab;
        this.prefabName = cellData.prefabName;
        this.secondaryPrefabName = cellData.secondaryPrefabName ?? "";
        this.zoneType = System.Enum.TryParse(cellData.zoneType, out Zone.ZoneType parsedZone) ? parsedZone : Zone.ZoneType.Grass;
        this.waterBodyType = cellData.GetWaterBodyType();
        this.waterBodyId = cellData.waterBodyId;
        this.occupiedBuildingName = cellData.occupiedBuildingName;
        this.isPivot = cellData.isPivot;
        this.sortingOrder = cellData.sortingOrder;
        this.transformPosition = cellData.transformPosition;
        this.prefab = cellData.prefab;
        this.prefabName = cellData.prefabName;
        this.forestType = System.Enum.TryParse(cellData.forestType, out Forest.ForestType parsedForest) ? parsedForest : Forest.ForestType.None;
        this.forestPrefabName = cellData.forestPrefabName;
        this.desirability = cellData.desirability;
        this.closeForestCount = cellData.closeForestCount;
        this.closeWaterCount = cellData.closeWaterCount;
        this.hasRoadAtLeft = cellData.hasRoadAtLeft;
        this.hasRoadAtTop = cellData.hasRoadAtTop;
        this.hasRoadAtRight = cellData.hasRoadAtRight;
        this.hasRoadAtBottom = cellData.hasRoadAtBottom;
        this.population = cellData.population;
        this.powerOutput = cellData.powerOutput;
        this.powerConsumption = cellData.powerConsumption;
        this.waterConsumption = cellData.waterConsumption;
        this.buildingType = cellData.buildingType;
        this.buildingSize = cellData.buildingSize;
        this.happiness = cellData.happiness;
        this.occupiedBuilding = cellData.occupiedBuilding;
        this.powerPlant = cellData.powerPlant;
        this.waterPlant = cellData.waterPlant;
        this.isInterstate = cellData.isInterstate;
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

    /// <summary>Clears all BUG-51 path route hints (prev/next grid and entry/exit steps).</summary>
    public void ClearRoadRouteHints()
    {
        hasRoadSegmentPrevHint = false;
        hasRoadSegmentNextHint = false;
        hasRoadRouteDirHints = false;
        roadRouteEntryStep = default;
        roadRouteExitStep = default;
    }

    /// <summary>Delegates to <see cref="ClearRoadRouteHints"/>.</summary>
    public void ClearRoadSegmentPrevHint() => ClearRoadRouteHints();

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

    public void SetCellInstanceHeight(int height)
    {
        this.height = height;
    }

    public int GetCellInstanceHeight()
    {
        return this.height;
    }

    public void SetCellInstanceSortingOrder(int sortingOrder)
    {
        this.sortingOrder = sortingOrder;
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
        GameObject toDestroy = (newForestType == Forest.ForestType.None && forestObject != null) ? forestObject : null;

        forestType = newForestType;
        forestPrefabName = prefabName;
        forestObject = forestGameObject;

        if (forestType == Forest.ForestType.None)
        {
            forestPrefabName = "";
            forestObject = null;
            if (toDestroy != null)
                Destroy(toDestroy);
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
    /// Get cell data for saving. Copies all serializable state from this CityCell to CellData.
    /// </summary>
    public CellData GetCellData()
    {
        CellData cellData = new CellData(x, y, height);

        // Roads
        cellData.hasRoadAtLeft = hasRoadAtLeft;
        cellData.hasRoadAtTop = hasRoadAtTop;
        cellData.hasRoadAtRight = hasRoadAtRight;
        cellData.hasRoadAtBottom = hasRoadAtBottom;

        // Building
        cellData.population = population;
        cellData.powerOutput = powerOutput;
        cellData.powerConsumption = powerConsumption;
        cellData.waterConsumption = waterConsumption;
        cellData.buildingType = buildingType ?? "Grass";
        cellData.buildingSize = buildingSize;
        cellData.happiness = happiness;
        cellData.prefabName = prefabName ?? "";
        cellData.secondaryPrefabName = secondaryPrefabName ?? "";
        cellData.zoneType = zoneType.ToString();
        cellData.waterBodyType = waterBodyType.ToString();
        cellData.waterBodyId = waterBodyId;
        cellData.occupiedBuildingName = GetBuildingName();
        if (string.IsNullOrEmpty(cellData.occupiedBuildingName) && prefabName != null)
            cellData.occupiedBuildingName = prefabName;
        cellData.isPivot = isPivot;

        // Grid (x, y, height from constructor)
        cellData.transformPosition = transformPosition;
        cellData.sortingOrder = sortingOrder;

        // Forest
        cellData.forestType = forestType.ToString();
        cellData.forestPrefabName = forestPrefabName ?? "";
        cellData.hasTree = forestType != Forest.ForestType.None;
        cellData.treePrefabName = forestPrefabName ?? "";

        // Desirability
        cellData.desirability = desirability;
        cellData.closeForestCount = closeForestCount;
        cellData.closeWaterCount = closeWaterCount;

        // Interstate
        cellData.isInterstate = isInterstate;

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
        prefab = cellData.prefab;
        prefabName = cellData.prefabName;
        secondaryPrefabName = cellData.secondaryPrefabName ?? "";
        zoneType = System.Enum.TryParse(cellData.zoneType, out Zone.ZoneType parsedZoneType) ? parsedZoneType : Zone.ZoneType.Grass;
        waterBodyType = cellData.GetWaterBodyType();
        waterBodyId = cellData.waterBodyId;
        occupiedBuildingName = cellData.occupiedBuildingName;
        isPivot = cellData.isPivot;
        height = cellData.height;
        powerPlant = cellData.powerPlant;
        waterPlant = cellData.waterPlant;
        transformPosition = cellData.transformPosition;
        sortingOrder = cellData.sortingOrder;

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

        // Interstate properties
        isInterstate = cellData.isInterstate;
    }
    #endregion
}
}
