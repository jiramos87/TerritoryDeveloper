using System;
using System.Collections.Generic;
using UnityEngine;
using Territory.Core;
using Territory.Economy;
using Territory.Timing;
using Territory.Roads;
using Territory.Geography;
using Territory.Simulation;
using Territory.Simulation.Signals;
using Territory.Terrain;
using Territory.UI;
using Territory.Audio;

namespace Territory.Persistence
{
/// <summary>Full game-state save payload. Serialized to JSON via JsonUtility.</summary>
[System.Serializable]
public class GameSaveData
{
    public int schemaVersion;
    public string regionId;
    public string countryId;
    public string saveName;
    public string cityName;
    public DateTime realWorldSaveTime;
    public long realWorldSaveTimeTicks;
    public InGameTime inGameTime;
    public List<CellData> gridData;
    public int gridWidth;
    public int gridHeight;
    public WaterMapData waterMapData;
    public DistrictMapData districtMap;
    public SignalTuningWeightsData tuningWeights;
    public bool isConnectedToInterstate;
    public RegionalMap regionalMap;
    public CityStatsData cityStats;
    public GrowthBudgetData growthBudget;
    public List<UrbanizationProposal> pendingProposals;
    public int minimapActiveLayers;
    public const int CurrentSchemaVersion = 6;
    public List<NeighborCityStub> neighborStubs = new List<NeighborCityStub>();
    public List<NeighborCityBinding> neighborCityBindings = new List<NeighborCityBinding>();
    public BudgetAllocationData budgetAllocation;
    public List<StateServiceZoneData> stateServiceZones = new List<StateServiceZoneData>();
    public List<bool> overlayActive = new List<bool>();
}

/// <summary>Lightweight save-file descriptor returned by GameSaveManager discovery APIs.</summary>
public sealed class SaveFileMeta
{
    public readonly string FilePath;
    public readonly string DisplayName;
    public readonly DateTime SortDate;
    public SaveFileMeta(string filePath, string displayName, DateTime sortDate)
    { FilePath = filePath; DisplayName = displayName; SortDate = sortDate; }
}
}
