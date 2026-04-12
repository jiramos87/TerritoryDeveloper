using UnityEngine;

namespace Territory.Forests
{
/// <summary>
/// Medium forest impl. Moderate water consumption + env impact.
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
    /// Init medium forest with characteristics.
    /// </summary>
    public void Initialize()
    {
        WaterConsumption = 3; // Moderate water requirement
        ConstructionCost = 0; // Free to place
        Prefab = forestPrefab;
        ForestType = Forest.ForestType.Medium;
        ForestSize = 1; // Single cell
    }

    public GameObject GameObjectReference => this == null ? null : gameObject;

    /// <summary>
    /// Water consumption for this forest type.
    /// </summary>
    public int GetWaterConsumption()
    {
        return WaterConsumption;
    }
}
}
