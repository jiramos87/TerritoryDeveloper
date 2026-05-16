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
    // Starting state constants (mirrored from CityStats hub defaults).
    private const int StartingMoney = 20000;
    private const float StartingHappiness = 50f;
    private const int StartYear = 2024;
    private const int StartMonth = 8;
    private const int StartDay = 27;

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
    private float _happiness = StartingHappiness;
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
        _money = StartingMoney;
        _population = 0;
        _currentDate = new DateTime(StartYear, StartMonth, StartDay);
    }

    // ---- IEconomy ----

    /// <inheritdoc/>
    /// <summary>True if money covers cost.</summary>
    public bool CanAfford(int cost) => _money >= cost;

    /// <inheritdoc/>
    /// <summary>Add value to money pool.</summary>
    public void AddMoney(int value) { _money += value; }

    /// <inheritdoc/>
    /// <summary>Subtract value from money pool.</summary>
    public void RemoveMoney(int value) { _money -= value; }

    /// <inheritdoc/>
    /// <summary>Current money.</summary>
    public int GetMoney() => _money;

    /// <inheritdoc/>
    /// <summary>Add value to population.</summary>
    public void AddPopulation(int value) { _population += value; }

    /// <inheritdoc/>
    /// <summary>Current population.</summary>
    public int GetPopulation() => _population;

    /// <inheritdoc/>
    /// <summary>Total power output.</summary>
    public int GetTotalPowerOutput() => _powerOutput;

    /// <inheritdoc/>
    /// <summary>Total power consumption.</summary>
    public int GetTotalPowerConsumption() => _powerConsumption;

    /// <inheritdoc/>
    /// <summary>Total water output.</summary>
    public int GetTotalWaterOutput() => _waterOutput;

    /// <inheritdoc/>
    /// <summary>Total water consumption.</summary>
    public int GetTotalWaterConsumption() => _waterConsumption;

    /// <inheritdoc/>
    /// <summary>Current happiness.</summary>
    public float GetHappiness() => _happiness;

    /// <inheritdoc/>
    /// <summary>Current pollution.</summary>
    public float GetPollution() => _pollution;

    /// <inheritdoc/>
    /// <summary>Forest cell count.</summary>
    public int GetForestCellCount() => _forestCellCount;

    /// <inheritdoc/>
    /// <summary>Forest coverage percentage.</summary>
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

    // Sub-type zone mutators (light/medium/heavy per R/C/I)
    /// <summary>Increment residential light building count + aggregate.</summary>
    public void AddResidentialLightBuildingCount() { _residentialLightBuildingCount++; AddResidentialBuildingCount(); }
    /// <summary>Decrement residential light building count + aggregate.</summary>
    public void RemoveResidentialLightBuildingCount() { _residentialLightBuildingCount--; RemoveResidentialBuildingCount(); }
    /// <summary>Increment residential light zoning count + aggregate.</summary>
    public void AddResidentialLightZoningCount() { _residentialLightZoningCount++; AddResidentialZoneCount(); }
    /// <summary>Decrement residential light zoning count + aggregate.</summary>
    public void RemoveResidentialLightZoningCount() { _residentialLightZoningCount--; RemoveResidentialZoneCount(); }
    /// <summary>Increment residential medium building count + aggregate.</summary>
    public void AddResidentialMediumBuildingCount() { _residentialMediumBuildingCount++; AddResidentialBuildingCount(); }
    /// <summary>Decrement residential medium building count + aggregate.</summary>
    public void RemoveResidentialMediumBuildingCount() { _residentialMediumBuildingCount--; RemoveResidentialBuildingCount(); }
    /// <summary>Increment residential medium zoning count + aggregate.</summary>
    public void AddResidentialMediumZoningCount() { _residentialMediumZoningCount++; AddResidentialZoneCount(); }
    /// <summary>Decrement residential medium zoning count + aggregate.</summary>
    public void RemoveResidentialMediumZoningCount() { _residentialMediumZoningCount--; RemoveResidentialZoneCount(); }
    /// <summary>Increment residential heavy building count + aggregate.</summary>
    public void AddResidentialHeavyBuildingCount() { _residentialHeavyBuildingCount++; AddResidentialBuildingCount(); }
    /// <summary>Decrement residential heavy building count + aggregate.</summary>
    public void RemoveResidentialHeavyBuildingCount() { _residentialHeavyBuildingCount--; RemoveResidentialBuildingCount(); }
    /// <summary>Increment residential heavy zoning count + aggregate.</summary>
    public void AddResidentialHeavyZoningCount() { _residentialHeavyZoningCount++; AddResidentialZoneCount(); }
    /// <summary>Decrement residential heavy zoning count + aggregate.</summary>
    public void RemoveResidentialHeavyZoningCount() { _residentialHeavyZoningCount--; RemoveResidentialZoneCount(); }

    /// <summary>Increment commercial light building count + aggregate.</summary>
    public void AddCommercialLightBuildingCount() { _commercialLightBuildingCount++; AddCommercialBuildingCount(); }
    /// <summary>Decrement commercial light building count + aggregate.</summary>
    public void RemoveCommercialLightBuildingCount() { _commercialLightBuildingCount--; RemoveCommercialBuildingCount(); }
    /// <summary>Increment commercial light zoning count + aggregate.</summary>
    public void AddCommercialLightZoningCount() { _commercialLightZoningCount++; AddCommercialZoneCount(); }
    /// <summary>Decrement commercial light zoning count + aggregate.</summary>
    public void RemoveCommercialLightZoningCount() { _commercialLightZoningCount--; RemoveCommercialZoneCount(); }
    /// <summary>Increment commercial medium building count + aggregate.</summary>
    public void AddCommercialMediumBuildingCount() { _commercialMediumBuildingCount++; AddCommercialBuildingCount(); }
    /// <summary>Decrement commercial medium building count + aggregate.</summary>
    public void RemoveCommercialMediumBuildingCount() { _commercialMediumBuildingCount--; RemoveCommercialBuildingCount(); }
    /// <summary>Increment commercial medium zoning count + aggregate.</summary>
    public void AddCommercialMediumZoningCount() { _commercialMediumZoningCount++; AddCommercialZoneCount(); }
    /// <summary>Decrement commercial medium zoning count + aggregate.</summary>
    public void RemoveCommercialMediumZoningCount() { _commercialMediumZoningCount--; RemoveCommercialZoneCount(); }
    /// <summary>Increment commercial heavy building count + aggregate.</summary>
    public void AddCommercialHeavyBuildingCount() { _commercialHeavyBuildingCount++; AddCommercialBuildingCount(); }
    /// <summary>Decrement commercial heavy building count + aggregate.</summary>
    public void RemoveCommercialHeavyBuildingCount() { _commercialHeavyBuildingCount--; RemoveCommercialBuildingCount(); }
    /// <summary>Increment commercial heavy zoning count + aggregate.</summary>
    public void AddCommercialHeavyZoningCount() { _commercialHeavyZoningCount++; AddCommercialZoneCount(); }
    /// <summary>Decrement commercial heavy zoning count + aggregate.</summary>
    public void RemoveCommercialHeavyZoningCount() { _commercialHeavyZoningCount--; RemoveCommercialZoneCount(); }

    /// <summary>Increment industrial light building count + aggregate.</summary>
    public void AddIndustrialLightBuildingCount() { _industrialLightBuildingCount++; AddIndustrialBuildingCount(); }
    /// <summary>Decrement industrial light building count + aggregate.</summary>
    public void RemoveIndustrialLightBuildingCount() { _industrialLightBuildingCount--; RemoveIndustrialBuildingCount(); }
    /// <summary>Increment industrial light zoning count + aggregate.</summary>
    public void AddIndustrialLightZoningCount() { _industrialLightZoningCount++; AddIndustrialZoneCount(); }
    /// <summary>Decrement industrial light zoning count + aggregate.</summary>
    public void RemoveIndustrialLightZoningCount() { _industrialLightZoningCount--; RemoveIndustrialZoneCount(); }
    /// <summary>Increment industrial medium building count + aggregate.</summary>
    public void AddIndustrialMediumBuildingCount() { _industrialMediumBuildingCount++; AddIndustrialBuildingCount(); }
    /// <summary>Decrement industrial medium building count + aggregate.</summary>
    public void RemoveIndustrialMediumBuildingCount() { _industrialMediumBuildingCount--; RemoveIndustrialBuildingCount(); }
    /// <summary>Increment industrial medium zoning count + aggregate.</summary>
    public void AddIndustrialMediumZoningCount() { _industrialMediumZoningCount++; AddIndustrialZoneCount(); }
    /// <summary>Decrement industrial medium zoning count + aggregate.</summary>
    public void RemoveIndustrialMediumZoningCount() { _industrialMediumZoningCount--; RemoveIndustrialZoneCount(); }
    /// <summary>Increment industrial heavy building count + aggregate.</summary>
    public void AddIndustrialHeavyBuildingCount() { _industrialHeavyBuildingCount++; AddIndustrialBuildingCount(); }
    /// <summary>Decrement industrial heavy building count + aggregate.</summary>
    public void RemoveIndustrialHeavyBuildingCount() { _industrialHeavyBuildingCount--; RemoveIndustrialBuildingCount(); }
    /// <summary>Increment industrial heavy zoning count + aggregate.</summary>
    public void AddIndustrialHeavyZoningCount() { _industrialHeavyZoningCount++; AddIndustrialZoneCount(); }
    /// <summary>Decrement industrial heavy zoning count + aggregate.</summary>
    public void RemoveIndustrialHeavyZoningCount() { _industrialHeavyZoningCount--; RemoveIndustrialZoneCount(); }

    // Sub-type zone accessors (light/medium/heavy)
    /// <summary>Get residential light building count.</summary>
    public int GetResidentialLightBuildingCount() => _residentialLightBuildingCount;
    /// <summary>Get residential light zoning count.</summary>
    public int GetResidentialLightZoningCount() => _residentialLightZoningCount;
    /// <summary>Get residential medium building count.</summary>
    public int GetResidentialMediumBuildingCount() => _residentialMediumBuildingCount;
    /// <summary>Get residential medium zoning count.</summary>
    public int GetResidentialMediumZoningCount() => _residentialMediumZoningCount;
    /// <summary>Get residential heavy building count.</summary>
    public int GetResidentialHeavyBuildingCount() => _residentialHeavyBuildingCount;
    /// <summary>Get residential heavy zoning count.</summary>
    public int GetResidentialHeavyZoningCount() => _residentialHeavyZoningCount;
    /// <summary>Get commercial light building count.</summary>
    public int GetCommercialLightBuildingCount() => _commercialLightBuildingCount;
    /// <summary>Get commercial light zoning count.</summary>
    public int GetCommercialLightZoningCount() => _commercialLightZoningCount;
    /// <summary>Get commercial medium building count.</summary>
    public int GetCommercialMediumBuildingCount() => _commercialMediumBuildingCount;
    /// <summary>Get commercial medium zoning count.</summary>
    public int GetCommercialMediumZoningCount() => _commercialMediumZoningCount;
    /// <summary>Get commercial heavy building count.</summary>
    public int GetCommercialHeavyBuildingCount() => _commercialHeavyBuildingCount;
    /// <summary>Get commercial heavy zoning count.</summary>
    public int GetCommercialHeavyZoningCount() => _commercialHeavyZoningCount;
    /// <summary>Get industrial light building count.</summary>
    public int GetIndustrialLightBuildingCount() => _industrialLightBuildingCount;
    /// <summary>Get industrial light zoning count.</summary>
    public int GetIndustrialLightZoningCount() => _industrialLightZoningCount;
    /// <summary>Get industrial medium building count.</summary>
    public int GetIndustrialMediumBuildingCount() => _industrialMediumBuildingCount;
    /// <summary>Get industrial medium zoning count.</summary>
    public int GetIndustrialMediumZoningCount() => _industrialMediumZoningCount;
    /// <summary>Get industrial heavy building count.</summary>
    public int GetIndustrialHeavyBuildingCount() => _industrialHeavyBuildingCount;
    /// <summary>Get industrial heavy zoning count.</summary>
    public int GetIndustrialHeavyZoningCount() => _industrialHeavyZoningCount;

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
        _money = StartingMoney;
        _population = 0;
        _happiness = StartingHappiness;
        _pollution = 0f;
        _currentDate = new DateTime(StartYear, StartMonth, StartDay);
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
