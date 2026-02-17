using UnityEngine;
using System.Collections.Generic;

public class DenseForest : MonoBehaviour, IForest
{
  public int WaterConsumption { get; private set; }
  public int ConstructionCost { get; private set; }
  public int ForestSize { get; private set; }
  
  public GameObject Prefab { get; private set; }
  public GameObject forestPrefab;

  public Forest.ForestType ForestType { get; private set; }

  public void Initialize ()
  {
      WaterConsumption = 10; // Example value
      ConstructionCost = 0; // Example value
      Prefab = forestPrefab;
      ForestType = Forest.ForestType.Dense;
      ForestSize = 1; // Example value
  }

  public GameObject GameObjectReference => this == null ? null : gameObject;

  public int GetWaterConsumption()
  {
    return WaterConsumption;
  }
}
