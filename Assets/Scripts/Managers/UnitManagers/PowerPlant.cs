using UnityEngine;
using System.Collections.Generic;
using Territory.Core.Common;
using Territory.Timing;
using Territory.Zones;

namespace Territory.Buildings
{
/// <summary>
/// MonoBehaviour for power plant buildings on grid. Tracks power capacity.
/// Coordinates with <see cref="AnimatorManager"/> for anim speed.
/// </summary>
public class PowerPlant : MonoBehaviour, IBuilding
{
    private const int PercentMax = GameConstants.PercentMax;

    private int powerOutput;
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
    public GameObject powerPlantNuclearPrefab;
    public Building.BuildingType BuildingType { get; private set; }
    private Animator animator;

    public void Initialize(string type, int constructionCost, int initialMaintenanceCost, int maxWorkers, int initialWorkers, int size, int baseOutput, GameObject prefab)
    {
        ConstructionCost = constructionCost;
        InitialMaintenanceCost = initialMaintenanceCost;
        MaxWorkers = maxWorkers;
        InitialWorkers = initialWorkers;
        BuildingSize = size;
        Prefab = prefab;
        BuildingType = Building.BuildingType.Power;
        BaseOutput = baseOutput;

        SetActive(true);
        SetProductivity(PercentMax, false);
        SetWorkers(PercentMax);

        animator = GetComponent<Animator>();

        AnimatorManager animatorManager = FindObjectOfType<AnimatorManager>();
        if (animatorManager != null)
        {
            animatorManager.RegisterAnimator(animator);
        }
    }

    public GameObject GameObjectReference => gameObject;

    public int PowerOutput => powerOutput;

    public void SetActive(bool isActive)
    {
        active = isActive;
        UpdatePowerOutput();
    }

    public void SetProductivity(int productivityLevel, bool updatePowerOutput = true)
    {
        productivity = Mathf.Clamp(productivityLevel, 0, PercentMax);

        if (updatePowerOutput)
        {
            UpdatePowerOutput();
        }
    }

    public void SetBudget(int percentage)
    {
        maintenanceCost = InitialMaintenanceCost * Mathf.Clamp(percentage, 0, PercentMax) / PercentMax;
        SetWorkers(percentage);
    }

    public void SetWorkers(int percentage)
    {
        currentWorkers = MaxWorkers * Mathf.Clamp(percentage, 0, PercentMax) / PercentMax;
        UpdatePowerOutput();
    }

    private void UpdatePowerOutput()
    {
        powerOutput = (active ? PercentMax : 0) * productivity / PercentMax * currentWorkers / MaxWorkers + BaseOutput;

        // Example: Notify CityStats of power output change
        // CityStats.Instance.UpdatePowerOutput();
    }

    public List<GameObject> GetPowerPlantPrefabs()
    {
        List<GameObject> powerPlantPrefabs = new List<GameObject>()
        {
            powerPlantNuclearPrefab
        };

        return powerPlantPrefabs;
    }
}
}
