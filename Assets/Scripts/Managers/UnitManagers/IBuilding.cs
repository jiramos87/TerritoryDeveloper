using UnityEngine;

namespace Territory.Zones
{
/// <summary>
/// Contract for placeable buildings. Props: BuildingType, ConstructionCost, Prefab, BuildingSize, GameObjectReference.
/// </summary>
public interface IBuilding
{
    Building.BuildingType BuildingType { get; }
    int ConstructionCost { get; }
    GameObject Prefab { get; }

    int BuildingSize { get; }

    GameObject GameObjectReference { get; }
}
}
