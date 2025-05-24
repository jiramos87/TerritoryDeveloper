using UnityEngine;

/// <summary>
/// Medium forest implementation with moderate water consumption and environmental impact
/// </summary>
public class MediumForest : MonoBehaviour, IForest
{
    public int WaterConsumption { get; private set; }
    public int ConstructionCost { get; private set; }
    public int ForestSize { get; private set; }
    
    public GameObject Prefab { get; private set; }
    public GameObject forestPrefab;

    public Forest.ForestType ForestType { get; private set; }

    /// <summary>
    /// Initialize medium forest with its characteristics
    /// </summary>
    public void Initialize()
    {
        WaterConsumption = 7; // Moderate water requirement
        ConstructionCost = 0; // Free to place
        Prefab = forestPrefab;
        ForestType = Forest.ForestType.Medium;
        ForestSize = 1; // Single cell
    }

    public GameObject GameObjectReference => gameObject;

    /// <summary>
    /// Get water consumption for this forest type
    /// </summary>
    public int GetWaterConsumption()
    {
        return WaterConsumption;
    }
}
