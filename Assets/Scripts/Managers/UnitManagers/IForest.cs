using UnityEngine;

namespace Territory.Forests
{
/// <summary>
/// Contract for forest types. Props: ForestType, ConstructionCost, WaterConsumption, Prefab, ForestSize, GameObjectReference.
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
