using UnityEngine;

namespace Territory.Forests
{
/// <summary>
/// Interface contract for forest types.
/// Defines ForestType, ConstructionCost, WaterConsumption, Prefab, ForestSize, and GameObjectReference properties.
/// </summary>
public interface IForest
{
    Forest.ForestType ForestType { get; }
    int ConstructionCost { get; }
    int WaterConsumption { get; }
    GameObject Prefab { get; }

    int ForestSize { get; }

    GameObject GameObjectReference { get; }
}
}
