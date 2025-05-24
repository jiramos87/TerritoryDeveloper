using UnityEngine;

/// <summary>
/// Sparse forest implementation with minimal water consumption and environmental impact
/// </summary>
public class SparseForest : MonoBehaviour, IForest
{
    public int WaterConsumption { get; private set; }
    public int ConstructionCost { get; private set; }
    public int ForestSize { get; private set; }
    
    public GameObject Prefab { get; private set; }
    public GameObject forestPrefab;

    public Forest.ForestType ForestType { get; private set; }

    /// <summary>
    /// Initialize sparse forest with its characteristics
    /// </summary>
    public void Initialize()
    {
        WaterConsumption = 5; // Low water requirement
        ConstructionCost = 0; // Free to place
        Prefab = forestPrefab;
        ForestType = Forest.ForestType.Sparse;
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
