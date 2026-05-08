using System;
using System.Collections.Generic;

namespace Domains.Economy.Services
{
/// <summary>
/// Pure-C# economy stats service extracted from CityStats (Stage 11 tracer).
/// Tracks city money, population, power/water capacity, zone counts, forest stats.
/// MonoBehaviour facade (CityStats) delegates to this service for unit-testable logic.
/// </summary>
public class CityStatsService : IEconomy
{
    // Core financials
    private int _money;
    private int _population;

    // Power
    private int _powerConsumption;
    private int _powerOutput;

    // Water
    private int _waterConsumption;
    private int _waterOutput;

    // Happiness / Pollution (read-only backing; set externally by signal layer)
    private float _happiness = 50f;
    private float _pollution;

    // Forest
    private int _forestCellCount;
    private float _forestCoveragePercentage;

    // Zone counts (residential)
    private int _residentialZoneCount;
    private int _residentialBuildingCount;
    private int _residentialLightBuildingCount;
    private int _residentialLightZoningCount;
    private int _residentialMediumBuildingCount;
    private int _residentialMediumZoningCount;
    private int _residentialHeavyBuildingCount;
    private int _residentialHeavyZoningCount;

    // Zone counts (commercial)
    private int _commercialZoneCount;
    private int _commercialBuildingCount;
    private int _commercialLightBuildingCount;
    private int _commercialLightZoningCount;
    private int _commercialMediumBuildingCount;
    private int _commercialMediumZoningCount;
    private int _commercialHeavyBuildingCount;
    private int _commercialHeavyZoningCount;

    // Zone counts (industrial)
    private int _industrialZoneCount;
    private int _industrialBuildingCount;
    private int _industrialLightBuildingCount;
    private int _industrialLightZoningCount;
    private int _industrialMediumBuildingCount;
    private int _industrialMediumZoningCount;
    private int _industrialHeavyBuildingCount;
    private int _industrialHeavyZoningCount;

    // Roads / grass
    private int _roadCount;
    private int _grassCount;

    // Power plants (opaque list for aggregation)
    private readonly List<string> _powerPlantIds = new List<string>();
    private readonly Dictionary<string, int> _powerPlantOutputs = new Dictionary<string, int>();

    // City meta
    private string _cityName = string.Empty;
    private DateTime _currentDate;
    private bool _simulateGrowth;

    // Economy envelope
    private int _totalEnvelopeCap;
    private int[] _envelopeRemainingPerSubType = new int[7];

    // Land value
    private float _cityLandValueMean;

    /// <summary>Initial state: 20000 money, no population.</summary>
    public CityStatsService()
    {
        _money = 20000;
        _population = 0;
        _currentDate = new DateTime(2024, 8, 27);
    }

    // ---- IEconomy ----

    /// <inheritdoc/>
    public bool CanAfford(int cost) => _money >= cost;

    /// <inheritdoc/>
    public void AddMoney(int value) { _money += value; }

    /// <inheritdoc/>
    public void RemoveMoney(int value) { _money -= value; }

    /// <inheritdoc/>
    public int GetMoney() => _money;

    /// <inheritdoc/>
    public void AddPopulation(int value) { _population += value; }

    /// <inheritdoc/>
    public int GetPopulation() => _population;

    /// <inheritdoc/>
    public int GetTotalPowerOutput() => _powerOutput;

    /// <inheritdoc/>
    public int GetTotalPowerConsumption() => _powerConsumption;

    /// <inheritdoc/>
    public int GetTotalWaterOutput() => _waterOutput;

    /// <inheritdoc/>
    public int GetTotalWaterConsumption() => _waterConsumption;

    /// <inheritdoc/>
    public float GetHappiness() => _happiness;

    /// <inheritdoc/>
    public float GetPollution() => _pollution;

    /// <inheritdoc/>
    public int GetForestCellCount() => _forestCellCount;

    /// <inheritdoc/>
    public float GetForestCoveragePercentage() => _forestCoveragePercentage;

    // ---- Power tracking ----

    /// <summary>Register named power plant with given output value.</summary>
    public void RegisterPowerPlant(string id, int output)
    {
        if (_powerPlantIds.Contains(id)) return;
        _powerPlantIds.Add(id);
        _powerPlantOutputs[id] = output;
        RecalcPowerOutput();
    }

    /// <summary>Unregister power plant and recalc output.</summary>
    public void UnregisterPowerPlant(string id)
    {
        _powerPlantIds.Remove(id);
        _powerPlantOutputs.Remove(id);
        RecalcPowerOutput();
    }

    /// <summary>Clear all plants and reset output.</summary>
    public void ResetPowerPlants()
    {
        _powerPlantIds.Clear();
        _powerPlantOutputs.Clear();
        _powerOutput = 0;
    }

    /// <summary>Count of registered plants.</summary>
    public int GetRegisteredPowerPlantCount() => _powerPlantIds.Count;

    /// <summary>True if power output > consumption.</summary>
    public bool GetCityPowerAvailability() => _powerOutput > _powerConsumption;

    /// <summary>Add to city power consumption.</summary>
    public void AddPowerConsumption(int value) { _powerConsumption += value; }

    /// <summary>Remove from city power consumption.</summary>
    public void RemovePowerConsumption(int value) { _powerConsumption -= value; }

    // ---- Water tracking ----

    /// <summary>Add to city water consumption.</summary>
    public void AddWaterConsumption(int value) { _waterConsumption += value; }

    /// <summary>Remove from city water consumption.</summary>
    public void RemoveWaterConsumption(int value) { _waterConsumption -= value; }

    /// <summary>Set total water output (synced from WaterManager).</summary>
    public void SetWaterOutput(int value) { _waterOutput = value; }

    /// <summary>Set total water consumption (synced from WaterManager).</summary>
    public void SetWaterConsumption(int value) { _waterConsumption = value; }

    /// <summary>True if water output > consumption.</summary>
    public bool GetCityWaterAvailability() => _waterOutput > _waterConsumption;

    // ---- Signal-layer setters ----

    /// <summary>Set happiness (written by HappinessComposer / signal layer).</summary>
    public void SetHappiness(float value) { _happiness = value; }

    /// <summary>Set pollution (written by signal layer mean computation).</summary>
    public void SetPollution(float value) { _pollution = value; }

    // ---- Forest ----

    /// <summary>Update forest stats from ForestManager.</summary>
    public void UpdateForestStats(int cellCount, float coveragePct)
    {
        _forestCellCount = cellCount;
        _forestCoveragePercentage = coveragePct;
    }

    // ---- Zone counts ----

    /// <summary>Increment residential zone count.</summary>
    public void AddResidentialZoneCount() { _residentialZoneCount++; }
    /// <summary>Decrement residential zone count.</summary>
    public void RemoveResidentialZoneCount() { _residentialZoneCount--; }
    /// <summary>Increment residential building count.</summary>
    public void AddResidentialBuildingCount() { _residentialBuildingCount++; }
    /// <summary>Decrement residential building count.</summary>
    public void RemoveResidentialBuildingCount() { _residentialBuildingCount--; }
    /// <summary>Increment commercial zone count.</summary>
    public void AddCommercialZoneCount() { _commercialZoneCount++; }
    /// <summary>Decrement commercial zone count.</summary>
    public void RemoveCommercialZoneCount() { _commercialZoneCount--; }
    /// <summary>Increment commercial building count.</summary>
    public void AddCommercialBuildingCount() { _commercialBuildingCount++; }
    /// <summary>Decrement commercial building count.</summary>
    public void RemoveCommercialBuildingCount() { _commercialBuildingCount--; }
    /// <summary>Increment industrial zone count.</summary>
    public void AddIndustrialZoneCount() { _industrialZoneCount++; }
    /// <summary>Decrement industrial zone count.</summary>
    public void RemoveIndustrialZoneCount() { _industrialZoneCount--; }
    /// <summary>Increment industrial building count.</summary>
    public void AddIndustrialBuildingCount() { _industrialBuildingCount++; }
    /// <summary>Decrement industrial building count.</summary>
    public void RemoveIndustrialBuildingCount() { _industrialBuildingCount--; }

    // Sub-type zone accessors
    /// <summary>Get residential zone count.</summary>
    public int GetResidentialZoneCount() => _residentialZoneCount;
    /// <summary>Get residential building count.</summary>
    public int GetResidentialBuildingCount() => _residentialBuildingCount;
    /// <summary>Get commercial zone count.</summary>
    public int GetCommercialZoneCount() => _commercialZoneCount;
    /// <summary>Get commercial building count.</summary>
    public int GetCommercialBuildingCount() => _commercialBuildingCount;
    /// <summary>Get industrial zone count.</summary>
    public int GetIndustrialZoneCount() => _industrialZoneCount;
    /// <summary>Get industrial building count.</summary>
    public int GetIndustrialBuildingCount() => _industrialBuildingCount;
    /// <summary>Get road count.</summary>
    public int GetRoadCount() => _roadCount;
    /// <summary>Get grass count.</summary>
    public int GetGrassCount() => _grassCount;
    /// <summary>Increment road count.</summary>
    public void AddRoadCount() { _roadCount++; }
    /// <summary>Decrement road count.</summary>
    public void RemoveRoadCount() { _roadCount--; }
    /// <summary>Increment grass count.</summary>
    public void AddGrassCount() { _grassCount++; }
    /// <summary>Decrement grass count.</summary>
    public void RemoveGrassCount() { _grassCount--; }

    // ---- Economy envelope ----

    /// <summary>Set total envelope cap from BudgetAllocationService.</summary>
    public void SetEnvelopeCap(int cap) { _totalEnvelopeCap = cap; }

    /// <summary>Get total envelope cap.</summary>
    public int GetEnvelopeCap() => _totalEnvelopeCap;

    /// <summary>Set remaining draw for given sub-type index (0–6).</summary>
    public void SetEnvelopeRemaining(int index, int value)
    {
        if (index >= 0 && index < _envelopeRemainingPerSubType.Length)
            _envelopeRemainingPerSubType[index] = value;
    }

    /// <summary>Get remaining draw for given sub-type index (0–6).</summary>
    public int GetEnvelopeRemaining(int index)
    {
        if (index >= 0 && index < _envelopeRemainingPerSubType.Length)
            return _envelopeRemainingPerSubType[index];
        return 0;
    }

    // ---- Land value ----

    /// <summary>Set city-wide mean land value (from SignalTickScheduler cache).</summary>
    public void SetCityLandValueMean(float value) { _cityLandValueMean = value; }

    /// <summary>Get city-wide mean land value.</summary>
    public float GetCityLandValueMean() => _cityLandValueMean;

    // ---- City meta ----

    /// <summary>Get city name.</summary>
    public string GetCityName() => _cityName;

    /// <summary>Set city name (trimmed, non-null).</summary>
    public void SetCityName(string name)
    {
        if (!string.IsNullOrWhiteSpace(name))
            _cityName = name.Trim();
    }

    /// <summary>Get current simulation date.</summary>
    public DateTime GetCurrentDate() => _currentDate;

    /// <summary>Set current simulation date.</summary>
    public void SetCurrentDate(DateTime date) { _currentDate = date; }

    /// <summary>Get simulate-growth flag.</summary>
    public bool GetSimulateGrowth() => _simulateGrowth;

    /// <summary>Set simulate-growth flag.</summary>
    public void SetSimulateGrowth(bool value) { _simulateGrowth = value; }

    // ---- Reset ----

    /// <summary>Reset all stats to new-game defaults.</summary>
    public void Reset()
    {
        ResetPowerPlants();
        _money = 20000;
        _population = 0;
        _happiness = 50f;
        _pollution = 0f;
        _currentDate = new DateTime(2024, 8, 27);
        _residentialZoneCount = 0;
        _commercialZoneCount = 0;
        _industrialZoneCount = 0;
        _roadCount = 0;
        _grassCount = 0;
        _powerConsumption = 0;
        _powerOutput = 0;
        _waterConsumption = 0;
        _waterOutput = 0;
        _forestCellCount = 0;
        _forestCoveragePercentage = 0f;
        _simulateGrowth = false;
        _cityLandValueMean = 0f;
        _totalEnvelopeCap = 0;
        _envelopeRemainingPerSubType = new int[7];
    }

    // ---- Private helpers ----

    private void RecalcPowerOutput()
    {
        int total = 0;
        foreach (var kv in _powerPlantOutputs)
            total += kv.Value;
        _powerOutput = total;
    }
}
}
