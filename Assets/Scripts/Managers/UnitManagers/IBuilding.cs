using UnityEngine;

public interface IBuilding
{
    Building.BuildingType BuildingType { get; }
    int ConstructionCost { get; }
    GameObject Prefab { get; }

    int BuildingSize { get; }

    GameObject GameObjectReference { get; }
}
