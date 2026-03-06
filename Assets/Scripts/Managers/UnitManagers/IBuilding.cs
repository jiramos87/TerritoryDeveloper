using UnityEngine;

namespace Territory.Zones
{
/// <summary>
/// Interface contract for placeable buildings.
/// Defines BuildingType, ConstructionCost, Prefab, BuildingSize, and GameObjectReference properties.
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
