using UnityEngine;

public interface IForest
{
    Forest.ForestType ForestType { get; }
    int ConstructionCost { get; }
    int WaterConsumption { get; }
    GameObject Prefab { get; }

    int ForestSize { get; }

    GameObject GameObjectReference { get; }
}
