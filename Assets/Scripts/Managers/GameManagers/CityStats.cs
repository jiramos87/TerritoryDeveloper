using UnityEngine; using UnityEngine.Serialization; using System.Collections.Generic;
using Territory.Timing; using Territory.Terrain; using Territory.Forests;
using Territory.Buildings; using Territory.Core; using Territory.Zones;
using Territory.Simulation; using Territory.Simulation.Signals;
using Domains.Economy.Services;

namespace Territory.Economy
{
/// <summary>THIN hub — delegates to <see cref="CityStatsService"/>. Serialized fields UNCHANGED (locked #3).</summary>
public class CityStats : MonoBehaviour, ICityStats, ICityStatsAuto
{
    #region Fields (locked #3)
    public TimeManager timeManager; public WaterManager waterManager; public ForestManager forestManager;
    [SerializeField] private HappinessComposer happinessComposer;
    [SerializeField] private SignalFieldRegistry signalFieldRegistry;
    [SerializeField] private SignalTickScheduler signalTickScheduler;
    private EmploymentManager _employmentManager; private EconomyManager _economyManager;
    private StatisticsManager _statisticsManager; private BudgetAllocationService budgetAllocationService;
    private CityStatsService _svc;
    public System.DateTime currentDate; public int population; public int money;
    [SerializeField][FormerlySerializedAs("happiness")] private float _happiness = 50f;
    [SerializeField][FormerlySerializedAs("pollution")] private float _pollution;
    public float happiness { get { return happinessComposer != null ? happinessComposer.Current : _happiness; } set { _happiness = value; } }
    public float pollution { get { return signalFieldRegistry != null ? (_svc?.GetPollution() ?? _pollution) : _pollution; } set { _pollution = value; } }
    public int residentialZoneCount, residentialBuildingCount, commercialZoneCount, commercialBuildingCount;
    public int industrialZoneCount, industrialBuildingCount; public float cityLandValueMean;
    public int residentialLightBuildingCount, residentialLightZoningCount;
    public int residentialMediumBuildingCount, residentialMediumZoningCount;
    public int residentialHeavyBuildingCount, residentialHeavyZoningCount;
    public int commercialLightBuildingCount, commercialLightZoningCount;
    public int commercialMediumBuildingCount, commercialMediumZoningCount;
    public int commercialHeavyBuildingCount, commercialHeavyZoningCount;
    public int industrialLightBuildingCount, industrialLightZoningCount;
    public int industrialMediumBuildingCount, industrialMediumZoningCount;
    public int industrialHeavyBuildingCount, industrialHeavyZoningCount;
    public int roadCount, grassCount, cityPowerConsumption, cityPowerOutput;
    int ICityStatsAuto.cityPowerOutput => cityPowerOutput;
    public string cityName; public int cityWaterConsumption, cityWaterOutput;
    [Header("Forest Statistics")] public int forestCellCount; public float forestCoveragePercentage;
    [Header("Simulation")] public bool simulateGrowth = false;
    bool ICityStatsAuto.simulateGrowth => simulateGrowth;
    public List<CommuneData> communes = new List<CommuneData>();
    [Header("Economy read-model (envelope)")] public int totalEnvelopeCap;
    public int[] envelopeRemainingPerSubType = new int[7];
    #endregion

    #region Milestone Tracking
    public System.Action<int> OnPopulationMilestone;
    static readonly int[] PopMilestones = { 1000, 5000, 10000, 25000, 50000, 100000 };
    readonly bool[] _mFired = new bool[6]; readonly System.DateTime[] _mDate = new System.DateTime[6];
    const int MilestoneDays = 30;
    #endregion

    #region Happiness constants
    const float HB = 50f, WE = 30f, WS = 20f, WF = 10f, WP = 10f;
    [Header("Happiness (tuning)")][SerializeField] float happinessWeightTax = 27f;
    [SerializeField] float happinessWeightDevelopment = 12f;
    [Range(0f,1f)][SerializeField] float happinessServiceCoverageStub = 0.4f;
    const float DemMul0 = 0.8f, DemMul1 = 1.2f, ComfTax = 10f, MaxTax = 50f, MaxForest = 60f;
    #endregion

    void Start()
    {
        _svc = new CityStatsService();
        if (forestManager == null) forestManager = FindObjectOfType<ForestManager>();
        if (_employmentManager == null) _employmentManager = FindObjectOfType<EmploymentManager>();
        if (_economyManager == null) _economyManager = FindObjectOfType<EconomyManager>();
        if (_statisticsManager == null) _statisticsManager = FindObjectOfType<StatisticsManager>();
        if (budgetAllocationService == null) budgetAllocationService = FindObjectOfType<BudgetAllocationService>();
        if (happinessComposer == null) happinessComposer = FindObjectOfType<HappinessComposer>();
        if (signalFieldRegistry == null) signalFieldRegistry = FindObjectOfType<SignalFieldRegistry>();
        if (signalTickScheduler == null) signalTickScheduler = FindObjectOfType<SignalTickScheduler>();
    }

    void SyncToSvc() { _svc.AddMoney(money - _svc.GetMoney()); _svc.AddPopulation(population - _svc.GetPopulation()); _svc.SetCurrentDate(currentDate); _svc.SetSimulateGrowth(simulateGrowth); if (!string.IsNullOrEmpty(cityName)) _svc.SetCityName(cityName); }

    public void RefreshEconomyReadModel()
    {
        if (envelopeRemainingPerSubType == null || envelopeRemainingPerSubType.Length != 7) envelopeRemainingPerSubType = new int[7];
        if (budgetAllocationService != null) { totalEnvelopeCap = budgetAllocationService.GlobalMonthlyCap; for (int i = 0; i < 7; i++) envelopeRemainingPerSubType[i] = budgetAllocationService.GetRemaining(i); }
        cityLandValueMean = (signalTickScheduler?.Cache != null) ? signalTickScheduler.Cache.MeanForSignal(SimulationSignal.LandValue) : 0f;
    }

    public void AddPopulation(int v) { int p = population; population += v; _svc.AddPopulation(v); CheckMilestones(p, population); }
    public void AddMoney(int v) { money += v; _svc.AddMoney(v); }
    public void RemoveMoney(int v) { money -= v; _svc.RemoveMoney(v); }
    void CheckMilestones(int prev, int next) { if (OnPopulationMilestone == null) return; for (int i = 0; i < PopMilestones.Length; i++) { int t = PopMilestones[i]; if (prev < t && next >= t) { if (_mFired[i] && (currentDate - _mDate[i]).TotalDays < MilestoneDays) continue; _mFired[i] = true; _mDate[i] = currentDate; OnPopulationMilestone.Invoke(t); } } }

    public void AddResidentialZoneCount() { residentialZoneCount++; _svc.AddResidentialZoneCount(); }
    public void RemoveResidentialZoneCount() { residentialZoneCount--; _svc.RemoveResidentialZoneCount(); }
    public void AddResidentialBuildingCount() { residentialBuildingCount++; _svc.AddResidentialBuildingCount(); }
    public void RemoveResidentialBuildingCount() { residentialBuildingCount--; _svc.RemoveResidentialBuildingCount(); }
    public void AddCommercialZoneCount() { commercialZoneCount++; _svc.AddCommercialZoneCount(); }
    public void RemoveCommercialZoneCount() { commercialZoneCount--; _svc.RemoveCommercialZoneCount(); }
    public void AddCommercialBuildingCount() { commercialBuildingCount++; _svc.AddCommercialBuildingCount(); }
    public void RemoveCommercialBuildingCount() { commercialBuildingCount--; _svc.RemoveCommercialBuildingCount(); }
    public void AddIndustrialZoneCount() { industrialZoneCount++; _svc.AddIndustrialZoneCount(); }
    public void RemoveIndustrialZoneCount() { industrialZoneCount--; _svc.RemoveIndustrialZoneCount(); }
    public void AddIndustrialBuildingCount() { industrialBuildingCount++; _svc.AddIndustrialBuildingCount(); }
    public void RemoveIndustrialBuildingCount() { industrialBuildingCount--; _svc.RemoveIndustrialBuildingCount(); }
    public void AddResidentialLightBuildingCount() { residentialLightBuildingCount++; AddResidentialBuildingCount(); }
    public void RemoveResidentialLightBuildingCount() { residentialLightBuildingCount--; RemoveResidentialBuildingCount(); }
    public void AddResidentialLightZoningCount() { residentialLightZoningCount++; AddResidentialZoneCount(); }
    public void RemoveResidentialLightZoningCount() { residentialLightZoningCount--; RemoveResidentialZoneCount(); }
    public void AddResidentialMediumBuildingCount() { residentialMediumBuildingCount++; AddResidentialBuildingCount(); }
    public void RemoveResidentialMediumBuildingCount() { residentialMediumBuildingCount--; RemoveResidentialBuildingCount(); }
    public void AddResidentialMediumZoningCount() { residentialMediumZoningCount++; AddResidentialZoneCount(); }
    public void RemoveResidentialMediumZoningCount() { residentialMediumZoningCount--; RemoveResidentialZoneCount(); }
    public void AddResidentialHeavyBuildingCount() { residentialHeavyBuildingCount++; AddResidentialBuildingCount(); }
    public void RemoveResidentialHeavyBuildingCount() { residentialHeavyBuildingCount--; RemoveResidentialBuildingCount(); }
    public void AddResidentialHeavyZoningCount() { residentialHeavyZoningCount++; AddResidentialZoneCount(); }
    public void RemoveResidentialHeavyZoningCount() { residentialHeavyZoningCount--; RemoveResidentialZoneCount(); }
    public void AddCommercialLightBuildingCount() { commercialLightBuildingCount++; AddCommercialBuildingCount(); }
    public void RemoveCommercialLightBuildingCount() { commercialLightBuildingCount--; RemoveCommercialBuildingCount(); }
    public void AddCommercialLightZoningCount() { commercialLightZoningCount++; AddCommercialZoneCount(); }
    public void RemoveCommercialLightZoningCount() { commercialLightZoningCount--; RemoveCommercialZoneCount(); }
    public void AddCommercialMediumBuildingCount() { commercialMediumBuildingCount++; AddCommercialBuildingCount(); }
    public void RemoveCommercialMediumBuildingCount() { commercialMediumBuildingCount--; RemoveCommercialBuildingCount(); }
    public void AddCommercialMediumZoningCount() { commercialMediumZoningCount++; AddCommercialZoneCount(); }
    public void RemoveCommercialMediumZoningCount() { commercialMediumZoningCount--; RemoveCommercialZoneCount(); }
    public void AddCommercialHeavyBuildingCount() { commercialHeavyBuildingCount++; AddCommercialBuildingCount(); }
    public void RemoveCommercialHeavyBuildingCount() { commercialHeavyBuildingCount--; RemoveCommercialBuildingCount(); }
    public void AddCommercialHeavyZoningCount() { commercialHeavyZoningCount++; AddCommercialZoneCount(); }
    public void RemoveCommercialHeavyZoningCount() { commercialHeavyZoningCount--; RemoveCommercialZoneCount(); }
    public void AddIndustrialLightBuildingCount() { industrialLightBuildingCount++; AddIndustrialBuildingCount(); }
    public void RemoveIndustrialLightBuildingCount() { industrialLightBuildingCount--; RemoveIndustrialBuildingCount(); }
    public void AddIndustrialLightZoningCount() { industrialLightZoningCount++; AddIndustrialZoneCount(); }
    public void RemoveIndustrialLightZoningCount() { industrialLightZoningCount--; RemoveIndustrialZoneCount(); }
    public void AddIndustrialMediumBuildingCount() { industrialMediumBuildingCount++; AddIndustrialBuildingCount(); }
    public void RemoveIndustrialMediumBuildingCount() { industrialMediumBuildingCount--; RemoveIndustrialBuildingCount(); }
    public void AddIndustrialMediumZoningCount() { industrialMediumZoningCount++; AddIndustrialZoneCount(); }
    public void RemoveIndustrialMediumZoningCount() { industrialMediumZoningCount--; RemoveIndustrialZoneCount(); }
    public void AddIndustrialHeavyBuildingCount() { industrialHeavyBuildingCount++; AddIndustrialBuildingCount(); }
    public void RemoveIndustrialHeavyBuildingCount() { industrialHeavyBuildingCount--; RemoveIndustrialBuildingCount(); }
    public void AddIndustrialHeavyZoningCount() { industrialHeavyZoningCount++; AddIndustrialZoneCount(); }
    public void RemoveIndustrialHeavyZoningCount() { industrialHeavyZoningCount--; RemoveIndustrialZoneCount(); }
    public void AddRoadCount() { roadCount++; _svc.AddRoadCount(); }
    public void AddGrassCount() { grassCount++; _svc.AddGrassCount(); }

    public void AddZoneBuildingCount(Zone.ZoneType t) { switch(t) { case Zone.ZoneType.ResidentialLightBuilding: AddResidentialLightBuildingCount(); break; case Zone.ZoneType.ResidentialLightZoning: AddResidentialLightZoningCount(); break; case Zone.ZoneType.ResidentialMediumBuilding: AddResidentialMediumBuildingCount(); break; case Zone.ZoneType.ResidentialMediumZoning: AddResidentialMediumZoningCount(); break; case Zone.ZoneType.ResidentialHeavyBuilding: AddResidentialHeavyBuildingCount(); break; case Zone.ZoneType.ResidentialHeavyZoning: AddResidentialHeavyZoningCount(); break; case Zone.ZoneType.CommercialLightBuilding: AddCommercialLightBuildingCount(); break; case Zone.ZoneType.CommercialLightZoning: AddCommercialLightZoningCount(); break; case Zone.ZoneType.CommercialMediumBuilding: AddCommercialMediumBuildingCount(); break; case Zone.ZoneType.CommercialMediumZoning: AddCommercialMediumZoningCount(); break; case Zone.ZoneType.CommercialHeavyBuilding: AddCommercialHeavyBuildingCount(); break; case Zone.ZoneType.CommercialHeavyZoning: AddCommercialHeavyZoningCount(); break; case Zone.ZoneType.IndustrialLightBuilding: AddIndustrialLightBuildingCount(); break; case Zone.ZoneType.IndustrialLightZoning: AddIndustrialLightZoningCount(); break; case Zone.ZoneType.IndustrialMediumBuilding: AddIndustrialMediumBuildingCount(); break; case Zone.ZoneType.IndustrialMediumZoning: AddIndustrialMediumZoningCount(); break; case Zone.ZoneType.IndustrialHeavyBuilding: AddIndustrialHeavyBuildingCount(); break; case Zone.ZoneType.IndustrialHeavyZoning: AddIndustrialHeavyZoningCount(); break; case Zone.ZoneType.Road: AddRoadCount(); break; case Zone.ZoneType.Grass: AddGrassCount(); break; } }
    public void RemoveZoneBuildingCount(Zone.ZoneType t) { switch(t) { case Zone.ZoneType.ResidentialLightBuilding: RemoveResidentialLightBuildingCount(); break; case Zone.ZoneType.ResidentialLightZoning: RemoveResidentialLightZoningCount(); break; case Zone.ZoneType.ResidentialMediumBuilding: RemoveResidentialMediumBuildingCount(); break; case Zone.ZoneType.ResidentialMediumZoning: RemoveResidentialMediumZoningCount(); break; case Zone.ZoneType.ResidentialHeavyBuilding: RemoveResidentialHeavyBuildingCount(); break; case Zone.ZoneType.ResidentialHeavyZoning: RemoveResidentialHeavyZoningCount(); break; case Zone.ZoneType.CommercialLightBuilding: RemoveCommercialLightBuildingCount(); break; case Zone.ZoneType.CommercialLightZoning: RemoveCommercialLightZoningCount(); break; case Zone.ZoneType.CommercialMediumBuilding: RemoveCommercialMediumBuildingCount(); break; case Zone.ZoneType.CommercialMediumZoning: RemoveCommercialMediumZoningCount(); break; case Zone.ZoneType.CommercialHeavyBuilding: RemoveCommercialHeavyBuildingCount(); break; case Zone.ZoneType.CommercialHeavyZoning: RemoveCommercialHeavyZoningCount(); break; case Zone.ZoneType.IndustrialLightBuilding: RemoveIndustrialLightBuildingCount(); break; case Zone.ZoneType.IndustrialLightZoning: RemoveIndustrialLightZoningCount(); break; case Zone.ZoneType.IndustrialMediumBuilding: RemoveIndustrialMediumBuildingCount(); break; case Zone.ZoneType.IndustrialMediumZoning: RemoveIndustrialMediumZoningCount(); break; case Zone.ZoneType.IndustrialHeavyBuilding: RemoveIndustrialHeavyBuildingCount(); break; case Zone.ZoneType.IndustrialHeavyZoning: RemoveIndustrialHeavyZoningCount(); break; case Zone.ZoneType.Road: roadCount--; _svc.RemoveRoadCount(); break; case Zone.ZoneType.Grass: grassCount--; _svc.RemoveGrassCount(); break; } }

    public bool CanAfford(int cost) => money >= cost;
    public void RegisterPowerPlant(PowerPlant p) { _svc.RegisterPowerPlant(p.GetInstanceID().ToString(), p.PowerOutput); cityPowerOutput = _svc.GetTotalPowerOutput(); }
    public void UnregisterPowerPlant(PowerPlant p) { _svc.UnregisterPowerPlant(p.GetInstanceID().ToString()); cityPowerOutput = _svc.GetTotalPowerOutput(); }
    public void ResetPowerPlants() { _svc?.ResetPowerPlants(); cityPowerOutput = 0; }
    public int GetRegisteredPowerPlantCount() => _svc?.GetRegisteredPowerPlantCount() ?? 0;
    public int GetTotalPowerOutput() => cityPowerOutput;
    public void AddPowerConsumption(int v) { cityPowerConsumption += v; _svc.AddPowerConsumption(v); }
    public void RemovePowerConsumption(int v) { cityPowerConsumption -= v; _svc.RemovePowerConsumption(v); }
    public int GetTotalPowerConsumption() => cityPowerConsumption;
    public bool GetCityPowerAvailability() => cityPowerOutput > cityPowerConsumption;

    public void PerformMonthlyUpdates() { }
    public void PerformDailyUpdates() { currentDate = timeManager.GetCurrentDate(); if (_employmentManager != null) { _employmentManager.UpdateEmployment(); } if (_statisticsManager != null) _statisticsManager.UpdateStatistics(); UpdateForestStatistics(); if (_employmentManager != null) _employmentManager.RefreshRCIDemandAfterDailyStats(); RefreshEconomyReadModel(); }
    public void RefreshHappinessAfterPolicyChange() { if (_employmentManager != null) _employmentManager.RefreshRCIDemandAfterDailyStats(); }

    public void HandleZoneBuildingPlacement(Zone.ZoneType t, ZoneAttributes a) { RemoveMoney(a.ConstructionCost); AddPopulation(a.Population); AddZoneBuildingCount(t); AddPowerConsumption(a.PowerConsumption); AddWaterConsumption(a.WaterConsumption); }
    public void HandleBuildingDemolition(Zone.ZoneType t, ZoneAttributes a) { AddMoney(a.ConstructionCost / 5); AddPopulation(-a.Population); RemoveZoneBuildingCount(t); RemovePowerConsumption(a.PowerConsumption); RemoveWaterConsumption(a.WaterConsumption); }
    public void UpdateForestStats(ForestStatistics s) { forestCellCount = s.totalForestCells; forestCoveragePercentage = s.forestCoveragePercentage; _svc.UpdateForestStats(forestCellCount, forestCoveragePercentage); }
    void UpdateForestStatistics() { if (forestManager != null) UpdateForestStats(forestManager.GetForestStatistics()); }
    public float GetForestHappinessBonus() { float b = forestCellCount * 1.0f; if (forestCellCount > 20) b = 20f + (forestCellCount - 20) * 0.5f; return Mathf.Min(b, MaxForest); }
    public void RecalculatePollution() { } public void RecalculateHappiness() { }
    public float GetNormalizedHappiness() => Mathf.Clamp01(happiness / 100f);
    public float GetHappinessDemandMultiplier() { float n = Mathf.Clamp01(ComputeTargetHappiness() / 100f); return DemMul0 + n * (DemMul1 - DemMul0); }
    float ComputeTargetHappiness() { float empl = _employmentManager != null ? _employmentManager.GetEmploymentRate() / 100f : 0.5f; float tax = 0f; if (_economyManager != null) { float mx = Mathf.Max(_economyManager.residentialIncomeTax, Mathf.Max(_economyManager.commercialIncomeTax, _economyManager.industrialIncomeTax)); if (mx > ComfTax) tax = -Mathf.Clamp01((mx - ComfTax) / (MaxTax - ComfTax)); } float dev = (residentialZoneCount + commercialZoneCount + industrialZoneCount) > 0 ? Mathf.Clamp01((float)(residentialBuildingCount + commercialBuildingCount + industrialBuildingCount) / (residentialZoneCount + commercialZoneCount + industrialZoneCount)) : 0f; return Mathf.Clamp(HB + empl * WE + tax * happinessWeightTax + Mathf.Clamp01(happinessServiceCoverageStub) * WS + Mathf.Clamp01(GetForestHappinessBonus() / MaxForest) * WF + dev * happinessWeightDevelopment - Mathf.Clamp01(pollution / 200f) * WP, 0f, 100f); }

    public CityStatsData GetCityStatsData() => new CityStatsData { currentDate=currentDate, population=population, money=money, happiness=happiness, pollution=pollution, residentialZoneCount=residentialZoneCount, residentialBuildingCount=residentialBuildingCount, commercialZoneCount=commercialZoneCount, commercialBuildingCount=commercialBuildingCount, industrialZoneCount=industrialZoneCount, industrialBuildingCount=industrialBuildingCount, residentialLightBuildingCount=residentialLightBuildingCount, residentialLightZoningCount=residentialLightZoningCount, residentialMediumBuildingCount=residentialMediumBuildingCount, residentialMediumZoningCount=residentialMediumZoningCount, residentialHeavyBuildingCount=residentialHeavyBuildingCount, residentialHeavyZoningCount=residentialHeavyZoningCount, commercialLightBuildingCount=commercialLightBuildingCount, commercialLightZoningCount=commercialLightZoningCount, commercialMediumBuildingCount=commercialMediumBuildingCount, commercialMediumZoningCount=commercialMediumZoningCount, commercialHeavyBuildingCount=commercialHeavyBuildingCount, commercialHeavyZoningCount=commercialHeavyZoningCount, industrialLightBuildingCount=industrialLightBuildingCount, industrialLightZoningCount=industrialLightZoningCount, industrialMediumBuildingCount=industrialMediumBuildingCount, industrialMediumZoningCount=industrialMediumZoningCount, industrialHeavyBuildingCount=industrialHeavyBuildingCount, industrialHeavyZoningCount=industrialHeavyZoningCount, roadCount=roadCount, grassCount=grassCount, cityPowerConsumption=cityPowerConsumption, cityPowerOutput=cityPowerOutput, cityWaterConsumption=cityWaterConsumption, cityWaterOutput=cityWaterOutput, cityName=cityName, forestCellCount=forestCellCount, forestCoveragePercentage=forestCoveragePercentage, simulateGrowth=simulateGrowth, communes=communes };
    public void RestoreCityStatsData(CityStatsData d) { currentDate=d.currentDate; population=d.population; money=d.money; happiness=Mathf.Clamp(d.happiness,0f,100f); pollution=Mathf.Max(d.pollution,0f); residentialZoneCount=d.residentialZoneCount; residentialBuildingCount=d.residentialBuildingCount; commercialZoneCount=d.commercialZoneCount; commercialBuildingCount=d.commercialBuildingCount; industrialZoneCount=d.industrialZoneCount; industrialBuildingCount=d.industrialBuildingCount; residentialLightBuildingCount=d.residentialLightBuildingCount; residentialLightZoningCount=d.residentialLightZoningCount; residentialMediumBuildingCount=d.residentialMediumBuildingCount; residentialMediumZoningCount=d.residentialMediumZoningCount; residentialHeavyBuildingCount=d.residentialHeavyBuildingCount; residentialHeavyZoningCount=d.residentialHeavyZoningCount; commercialLightBuildingCount=d.commercialLightBuildingCount; commercialLightZoningCount=d.commercialLightZoningCount; commercialMediumBuildingCount=d.commercialMediumBuildingCount; commercialMediumZoningCount=d.commercialMediumZoningCount; commercialHeavyBuildingCount=d.commercialHeavyBuildingCount; commercialHeavyZoningCount=d.commercialHeavyZoningCount; industrialLightBuildingCount=d.industrialLightBuildingCount; industrialLightZoningCount=d.industrialLightZoningCount; industrialMediumBuildingCount=d.industrialMediumBuildingCount; industrialMediumZoningCount=d.industrialMediumZoningCount; industrialHeavyBuildingCount=d.industrialHeavyBuildingCount; industrialHeavyZoningCount=d.industrialHeavyZoningCount; roadCount=d.roadCount; grassCount=d.grassCount; cityPowerConsumption=d.cityPowerConsumption; cityPowerOutput=d.cityPowerOutput; cityWaterConsumption=d.cityWaterConsumption; cityWaterOutput=d.cityWaterOutput; cityName=d.cityName; forestCellCount=d.forestCellCount; forestCoveragePercentage=d.forestCoveragePercentage; simulateGrowth=d.simulateGrowth; communes=d.communes!=null?new List<CommuneData>(d.communes):new List<CommuneData>(); if(_svc!=null)SyncToSvc(); }
    public void ResetCityStats() { ResetPowerPlants(); population=0; money=20000; happiness=50f; pollution=0f; currentDate=new System.DateTime(2024,8,27); residentialZoneCount=0; commercialZoneCount=0; industrialZoneCount=0; roadCount=0; grassCount=0; cityPowerConsumption=0; cityPowerOutput=0; forestCellCount=0; forestCoveragePercentage=0f; simulateGrowth=false; communes?.Clear(); if(communes==null)communes=new List<CommuneData>(); _svc?.Reset(); }
    public void SetCityName(string n) { if(!string.IsNullOrWhiteSpace(n)){cityName=n.Trim();_svc?.SetCityName(cityName);} }
    public EmploymentManager GetEmploymentManager() => _employmentManager;
    public void AddWaterConsumption(int v) { cityWaterConsumption+=v; waterManager?.AddWaterConsumption(v); } public void RemoveWaterConsumption(int v) { cityWaterConsumption-=v; waterManager?.RemoveWaterConsumption(v); }
    public int GetTotalWaterConsumption() => cityWaterConsumption; public int GetTotalWaterOutput() => cityWaterOutput;
    public bool GetCityWaterAvailability() { if(waterManager!=null){cityWaterOutput=waterManager.GetTotalWaterOutput();cityWaterConsumption=waterManager.GetTotalWaterConsumption();} return cityWaterOutput>cityWaterConsumption; }
    public void UpdateWaterOutput() { if(waterManager!=null)cityWaterOutput=waterManager.GetTotalWaterOutput(); }
    public int GetForestCellCount() => forestCellCount; public float GetForestCoveragePercentage() => forestCoveragePercentage;
}

[System.Serializable]
public struct CityStatsData
{
    public System.DateTime currentDate; public int population, money; public float happiness, pollution;
    public int residentialZoneCount, residentialBuildingCount, commercialZoneCount, commercialBuildingCount;
    public int industrialZoneCount, industrialBuildingCount;
    public int residentialLightBuildingCount, residentialLightZoningCount, residentialMediumBuildingCount, residentialMediumZoningCount;
    public int residentialHeavyBuildingCount, residentialHeavyZoningCount;
    public int commercialLightBuildingCount, commercialLightZoningCount, commercialMediumBuildingCount, commercialMediumZoningCount;
    public int commercialHeavyBuildingCount, commercialHeavyZoningCount;
    public int industrialLightBuildingCount, industrialLightZoningCount, industrialMediumBuildingCount, industrialMediumZoningCount;
    public int industrialHeavyBuildingCount, industrialHeavyZoningCount;
    public int roadCount, grassCount, cityPowerConsumption, cityPowerOutput, cityWaterConsumption, cityWaterOutput;
    public string cityName; public int forestCellCount; public float forestCoveragePercentage;
    public bool simulateGrowth; public List<CommuneData> communes;
}
}
