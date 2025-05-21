using UnityEngine;
using System.Collections.Generic;

public class WaterPlant : MonoBehaviour, IBuilding
{
    private int waterOutput;
    private int maintenanceCost;
    private int currentWorkers;
    private bool active;
    private int productivity;

    public int ConstructionCost { get; private set; }
    public int InitialMaintenanceCost { get; private set; }
    public int MaxWorkers { get; private set; }
    public int InitialWorkers { get; private set; }
    public int BuildingSize { get; private set; }
    public int BaseOutput { get; private set; }
    public GameObject Prefab { get; private set; }
    public GameObject waterPlantPrefab;
    public Building.BuildingType BuildingType { get; private set; }

    // Animator properties (commented out until animator is implemented)
    // private Animator animator;

    public void Initialize(string type, int constructionCost, int initialMaintenanceCost, int maxWorkers, int initialWorkers, int size, int baseOutput, GameObject prefab)
    {
        Debug.Log($"WaterPlant Initialize called with type: {type}, constructionCost: {constructionCost}, size: {size}");
        
        try {
            ConstructionCost = constructionCost;
            InitialMaintenanceCost = initialMaintenanceCost;
            MaxWorkers = maxWorkers;
            InitialWorkers = initialWorkers;
            BuildingSize = size;
            BaseOutput = baseOutput;
            Prefab = prefab;
            BuildingType = Building.BuildingType.Water;

            SetActive(true);
            SetProductivity(100, false);
            SetWorkers(100); // Use percentage instead of InitialWorkers

            // Initialize water output
            UpdateWaterOutput();

            Debug.Log($"WaterPlant successfully initialized. WaterOutput: {waterOutput}, BuildingSize: {BuildingSize}");
        }
        catch (System.Exception ex) {
            Debug.LogError($"Error in WaterPlant.Initialize: {ex.Message}\n{ex.StackTrace}");
        }
    }

    public GameObject GameObjectReference => gameObject;

    public int WaterOutput => waterOutput;

    public void SetActive(bool isActive)
    {
        active = isActive;
        UpdateWaterOutput();
    }

    public void SetProductivity(int productivityLevel, bool updateWaterOutput = true)
    {
        productivity = Mathf.Clamp(productivityLevel, 0, 100);

        if (updateWaterOutput)
        {
            UpdateWaterOutput();
        }
    }

    public void SetBudget(int percentage)
    {
        maintenanceCost = InitialMaintenanceCost * Mathf.Clamp(percentage, 0, 100) / 100;
        SetWorkers(percentage);
    }

    public void SetWorkers(int percentage)
    {
        currentWorkers = MaxWorkers * Mathf.Clamp(percentage, 0, 100) / 100;
        UpdateWaterOutput();
    }

    private void UpdateWaterOutput()
    {
        float activeMultiplier = active ? 1f : 0f;
        float productivityPercent = productivity / 100f;
        float workersPercent = (float)currentWorkers / MaxWorkers;
        
        waterOutput = Mathf.RoundToInt(activeMultiplier * productivityPercent * workersPercent * BaseOutput);
    }

    public List<GameObject> GetWaterPlantPrefabs()
    {
        List<GameObject> waterPlantPrefabs = new List<GameObject>
        {
            waterPlantPrefab
        };

        return waterPlantPrefabs;
    }
}
