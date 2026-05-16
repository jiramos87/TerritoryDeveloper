// long-file-allowed: legacy hub — scope outside current atomization plan; deferred to future sweep
using UnityEngine;
using UnityEngine.UI;
using Territory.Core.Common;
using Territory.Economy;
using Territory.Simulation;

namespace Territory.UI
{
/// <summary>
/// UI controller for growth budget allocation sliders. Player adjusts R/C/I growth budgets via <see cref="GrowthBudgetManager"/>.
/// </summary>
public class GrowthBudgetSlidersController : MonoBehaviour
{
    private const int PercentMax = GameConstants.PercentMax;

    public CityStats cityStats;
    public GrowthBudgetManager growthBudgetManager;

    [Header("Total budget (percent of projected monthly income)")]
    public Slider totalBudgetSlider;
    public Text totalBudgetLabel;

    [Header("Category sliders (0-100%, sum = 100)")]
    public Slider roadPercentSlider;
    public Text roadPercentLabel;
    public Slider energyPercentSlider;
    public Text energyPercentLabel;
    public Slider waterPercentSlider;
    public Text waterPercentLabel;
    public Slider zoningPercentSlider;
    public Text zoningPercentLabel;

    public GameObject slidersContainer;

    private bool isInternalAdjustment;

    void Start()
    {
        if (cityStats == null) cityStats = FindObjectOfType<CityStats>();
        if (growthBudgetManager == null) growthBudgetManager = FindObjectOfType<GrowthBudgetManager>();

        if (totalBudgetSlider != null)
        {
            totalBudgetSlider.minValue = 0;
            totalBudgetSlider.maxValue = PercentMax;
            totalBudgetSlider.wholeNumbers = true;
            totalBudgetSlider.onValueChanged.AddListener(OnTotalBudgetChanged);
        }
        if (totalBudgetLabel != null)
            totalBudgetLabel.supportRichText = true;
        if (roadPercentSlider != null) roadPercentSlider.onValueChanged.AddListener(_ => OnRoadPercentChanged());
        if (energyPercentSlider != null) energyPercentSlider.onValueChanged.AddListener(_ => OnEnergyPercentChanged());
        if (waterPercentSlider != null) waterPercentSlider.onValueChanged.AddListener(_ => OnWaterPercentChanged());
        if (zoningPercentSlider != null) zoningPercentSlider.onValueChanged.AddListener(_ => OnZoningPercentChanged());

        SyncFromManager();
    }

    void OnDestroy()
    {
        if (totalBudgetSlider != null) totalBudgetSlider.onValueChanged.RemoveAllListeners();
        if (roadPercentSlider != null) roadPercentSlider.onValueChanged.RemoveAllListeners();
        if (energyPercentSlider != null) energyPercentSlider.onValueChanged.RemoveAllListeners();
        if (waterPercentSlider != null) waterPercentSlider.onValueChanged.RemoveAllListeners();
        if (zoningPercentSlider != null) zoningPercentSlider.onValueChanged.RemoveAllListeners();
    }

    void Update()
    {
        if (slidersContainer != null && cityStats != null)
            slidersContainer.SetActive(cityStats.simulateGrowth);
    }

    void OnTotalBudgetChanged(float value)
    {
        if (growthBudgetManager != null)
        {
            growthBudgetManager.SetGrowthBudgetPercent(Mathf.RoundToInt(value));
            UpdateTotalBudgetLabel();
        }
    }

    void OnRoadPercentChanged()
    {
        if (isInternalAdjustment || growthBudgetManager == null || roadPercentSlider == null) return;
        RedistributeCategoryPercent(GrowthCategory.Roads, Mathf.Clamp(Mathf.RoundToInt(roadPercentSlider.value), 0, PercentMax));
    }

    void OnEnergyPercentChanged()
    {
        if (isInternalAdjustment || growthBudgetManager == null || energyPercentSlider == null) return;
        RedistributeCategoryPercent(GrowthCategory.Energy, Mathf.Clamp(Mathf.RoundToInt(energyPercentSlider.value), 0, PercentMax));
    }

    void OnWaterPercentChanged()
    {
        if (isInternalAdjustment || growthBudgetManager == null || waterPercentSlider == null) return;
        RedistributeCategoryPercent(GrowthCategory.Water, Mathf.Clamp(Mathf.RoundToInt(waterPercentSlider.value), 0, PercentMax));
    }

    void OnZoningPercentChanged()
    {
        if (isInternalAdjustment || growthBudgetManager == null || zoningPercentSlider == null) return;
        RedistributeCategoryPercent(GrowthCategory.Zoning, Mathf.Clamp(Mathf.RoundToInt(zoningPercentSlider.value), 0, PercentMax));
    }

    /// <summary>
    /// Redistribute: changed category gets newValue, other 3 split (100 - newValue) proportionally. Sum = 100 always.
    /// </summary>
    void RedistributeCategoryPercent(GrowthCategory changed, int newValue)
    {
        int r = growthBudgetManager.GetCategoryPercent(GrowthCategory.Roads);
        int e = growthBudgetManager.GetCategoryPercent(GrowthCategory.Energy);
        int w = growthBudgetManager.GetCategoryPercent(GrowthCategory.Water);
        int z = growthBudgetManager.GetCategoryPercent(GrowthCategory.Zoning);

        switch (changed)
        {
            case GrowthCategory.Roads: r = newValue; break;
            case GrowthCategory.Energy: e = newValue; break;
            case GrowthCategory.Water: w = newValue; break;
            case GrowthCategory.Zoning: z = newValue; break;
        }

        int remainder = PercentMax - newValue;
        if (remainder < 0) remainder = 0;

        int otherR = changed == GrowthCategory.Roads ? 0 : r;
        int otherE = changed == GrowthCategory.Energy ? 0 : e;
        int otherW = changed == GrowthCategory.Water ? 0 : w;
        int otherZ = changed == GrowthCategory.Zoning ? 0 : z;
        int otherSum = otherR + otherE + otherW + otherZ;

        if (otherSum <= 0)
        {
            int part = remainder / 3;
            int third = remainder - 2 * part;
            if (changed == GrowthCategory.Roads) { e = part; w = part; z = third; }
            else if (changed == GrowthCategory.Energy) { r = part; w = part; z = third; }
            else if (changed == GrowthCategory.Water) { r = part; e = part; z = third; }
            else { r = part; e = part; w = third; }
        }
        else
        {
            if (changed != GrowthCategory.Roads) r = Mathf.RoundToInt(remainder * (float)otherR / otherSum);
            if (changed != GrowthCategory.Energy) e = Mathf.RoundToInt(remainder * (float)otherE / otherSum);
            if (changed != GrowthCategory.Water) w = Mathf.RoundToInt(remainder * (float)otherW / otherSum);
            if (changed != GrowthCategory.Zoning) z = remainder - (changed == GrowthCategory.Roads ? 0 : r) - (changed == GrowthCategory.Energy ? 0 : e) - (changed == GrowthCategory.Water ? 0 : w);
            int sum = r + e + w + z;
            if (sum != PercentMax)
            {
                if (changed != GrowthCategory.Roads) r = Mathf.Clamp(r + (PercentMax - sum), 0, PercentMax);
                else if (changed != GrowthCategory.Energy) e = Mathf.Clamp(e + (PercentMax - sum), 0, PercentMax);
                else if (changed != GrowthCategory.Water) w = Mathf.Clamp(w + (PercentMax - sum), 0, PercentMax);
                else z = Mathf.Clamp(z + (PercentMax - sum), 0, PercentMax);
            }
        }

        ApplyRedistributedPercents(r, e, w, z);
    }

    void ApplyRedistributedPercents(int road, int energy, int water, int zoning)
    {
        int sum = road + energy + water + zoning;
        if (sum != PercentMax)
        {
            road = Mathf.Clamp(road * PercentMax / Mathf.Max(1, sum), 0, PercentMax);
            energy = Mathf.Clamp(energy * PercentMax / Mathf.Max(1, sum), 0, PercentMax);
            water = Mathf.Clamp(water * PercentMax / Mathf.Max(1, sum), 0, PercentMax);
            zoning = PercentMax - road - energy - water;
            zoning = Mathf.Clamp(zoning, 0, PercentMax);
        }

        if (growthBudgetManager != null)
        {
            growthBudgetManager.SetCategoryPercent(GrowthCategory.Roads, road);
            growthBudgetManager.SetCategoryPercent(GrowthCategory.Energy, energy);
            growthBudgetManager.SetCategoryPercent(GrowthCategory.Water, water);
            growthBudgetManager.SetCategoryPercent(GrowthCategory.Zoning, zoning);
        }

        isInternalAdjustment = true;
        if (roadPercentSlider != null) roadPercentSlider.value = road;
        if (energyPercentSlider != null) energyPercentSlider.value = energy;
        if (waterPercentSlider != null) waterPercentSlider.value = water;
        if (zoningPercentSlider != null) zoningPercentSlider.value = zoning;
        isInternalAdjustment = false;

        UpdateAllLabels();
    }

    void SyncFromManager()
    {
        if (growthBudgetManager == null) return;
        if (totalBudgetSlider != null) totalBudgetSlider.value = growthBudgetManager.GetGrowthBudgetPercent();

        int r = growthBudgetManager.GetCategoryPercent(GrowthCategory.Roads);
        int e = growthBudgetManager.GetCategoryPercent(GrowthCategory.Energy);
        int w = growthBudgetManager.GetCategoryPercent(GrowthCategory.Water);
        int z = growthBudgetManager.GetCategoryPercent(GrowthCategory.Zoning);
        int sum = r + e + w + z;
        if (sum != PercentMax && sum > 0)
        {
            r = Mathf.RoundToInt((float)PercentMax * r / sum);
            e = Mathf.RoundToInt((float)PercentMax * e / sum);
            w = Mathf.RoundToInt((float)PercentMax * w / sum);
            z = PercentMax - r - e - w;
            growthBudgetManager.SetCategoryPercent(GrowthCategory.Roads, r);
            growthBudgetManager.SetCategoryPercent(GrowthCategory.Energy, e);
            growthBudgetManager.SetCategoryPercent(GrowthCategory.Water, w);
            growthBudgetManager.SetCategoryPercent(GrowthCategory.Zoning, z);
        }

        isInternalAdjustment = true;
        if (roadPercentSlider != null) roadPercentSlider.value = growthBudgetManager.GetCategoryPercent(GrowthCategory.Roads);
        if (energyPercentSlider != null) energyPercentSlider.value = growthBudgetManager.GetCategoryPercent(GrowthCategory.Energy);
        if (waterPercentSlider != null) waterPercentSlider.value = growthBudgetManager.GetCategoryPercent(GrowthCategory.Water);
        if (zoningPercentSlider != null) zoningPercentSlider.value = growthBudgetManager.GetCategoryPercent(GrowthCategory.Zoning);
        isInternalAdjustment = false;

        UpdateAllLabels();
    }

    void UpdateAllLabels()
    {
        UpdateTotalBudgetLabel();
        if (roadPercentLabel != null && roadPercentSlider != null) roadPercentLabel.text = Mathf.RoundToInt(roadPercentSlider.value) + "%";
        if (energyPercentLabel != null && energyPercentSlider != null) energyPercentLabel.text = Mathf.RoundToInt(energyPercentSlider.value) + "%";
        if (waterPercentLabel != null && waterPercentSlider != null) waterPercentLabel.text = Mathf.RoundToInt(waterPercentSlider.value) + "%";
        if (zoningPercentLabel != null && zoningPercentSlider != null) zoningPercentLabel.text = Mathf.RoundToInt(zoningPercentSlider.value) + "%";
    }

    void UpdateTotalBudgetLabel()
    {
        if (totalBudgetLabel != null && growthBudgetManager != null)
        {
            int pct = growthBudgetManager.GetGrowthBudgetPercent();
            int amount = growthBudgetManager.GetTotalBudget();
            totalBudgetLabel.text = pct + "%\n$" + amount.ToString("N0");
        }
    }
}
}
