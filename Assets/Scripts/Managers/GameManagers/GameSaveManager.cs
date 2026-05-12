using System;
using System.Collections.Generic;
using System.IO;
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
using Domains.Save.Services;

namespace Territory.Persistence
{
/// <summary>THIN hub — serializes/deserializes game state; file ops delegated to <see cref="SaveService"/>. Locked fields UNCHANGED (#1-#3).</summary>
public class GameSaveManager : MonoBehaviour
{
    public string saveName;
    public string cityName;
    public DateTime realWorldSaveTime;
    [NonSerialized] public string regionId;
    [NonSerialized] public string countryId;
    [NonSerialized] private List<NeighborCityStub> _neighborStubs = new List<NeighborCityStub>();
    public IReadOnlyList<NeighborCityStub> NeighborStubs => (_neighborStubs ?? new List<NeighborCityStub>()).AsReadOnly();
    [NonSerialized] public List<NeighborCityBinding> neighborCityBindings = new List<NeighborCityBinding>();
    private const int DEFAULT_S_CAP = 10_000;
    public GridManager gridManager;
    public CityStats cityStats;
    public TimeManager timeManager;
    public InterstateManager interstateManager;
    public MiniMapController miniMapController;
    private SaveService _svc;

    void Start() { _svc = new SaveService(); _svc.WireDependencies(Application.persistentDataPath); }

    public void SaveGame(string customSaveName = null)
    {
        GameSaveData sd = BuildCurrentGameSaveData(customSaveName);
        _svc.WriteJson(_svc.BuildSavePath(sd.saveName), JsonUtility.ToJson(sd), out _);
        BlipEngine.Play(BlipId.SysSaveGame);
        PlayerPrefs.SetString("LastSavePath", _svc.BuildSavePath(sd.saveName)); PlayerPrefs.Save();
        if (GameNotificationManager.Instance != null) GameNotificationManager.Instance.PostNotification("Game saved successfully", GameNotificationManager.NotificationType.Success, 3f);
    }

    public bool TryWriteGameSaveToPath(string absolutePath, string saveNameForPayload, out string error)
    { bool ok = _svc.WriteJson(absolutePath, JsonUtility.ToJson(BuildCurrentGameSaveData(saveNameForPayload)), out error); if (ok) BlipEngine.Play(BlipId.SysSaveGame); return ok; }

    GameSaveData BuildCurrentGameSaveData(string customSaveName)
    {
        GameSaveData d = new GameSaveData { schemaVersion = GameSaveData.CurrentSchemaVersion };
        d.regionId = string.IsNullOrEmpty(regionId) ? Guid.NewGuid().ToString() : regionId;
        d.countryId = string.IsNullOrEmpty(countryId) ? Guid.NewGuid().ToString() : countryId;
        regionId = d.regionId; countryId = d.countryId;
        if (gridManager != null && gridManager.ParentRegionId == null) gridManager.HydrateParentIds(regionId, countryId);
        d.cityName = cityStats.cityName; d.realWorldSaveTime = DateTime.Now; d.realWorldSaveTimeTicks = DateTime.UtcNow.Ticks;
        d.saveName = customSaveName ?? $"{d.cityName}_{d.realWorldSaveTime:yyyyMMdd_HHmmss}";
        d.inGameTime = timeManager.GetCurrentInGameTime();
        d.gridData = gridManager.GetGridData(); d.gridWidth = gridManager.width; d.gridHeight = gridManager.height;
        WaterManager wm = FindObjectOfType<WaterManager>(); if (wm != null && wm.GetWaterMap() != null) d.waterMapData = wm.GetWaterMap().GetSerializableData();
        DistrictManager dm = FindObjectOfType<DistrictManager>(); d.districtMap = (dm != null && dm.Map != null) ? dm.Map.GetSerializableData() : null;
        HappinessComposer hc = FindObjectOfType<HappinessComposer>(); SignalTuningWeightsAsset wa = hc != null ? hc.Weights : null; d.tuningWeights = wa != null ? wa.CaptureSnapshot() : null;
        d.cityStats = cityStats.GetCityStatsData(); d.isConnectedToInterstate = interstateManager != null && interstateManager.IsConnectedToInterstate;
        RegionalMapManager rmm = FindObjectOfType<RegionalMapManager>(); if (rmm != null) { rmm.SyncCityNameToPlayerTerritory(); d.regionalMap = rmm.GetRegionalMapForSave(); }
        GrowthBudgetManager gbm = FindObjectOfType<GrowthBudgetManager>(); if (gbm != null) d.growthBudget = gbm.data.Clone();
        BudgetAllocationService bas = FindObjectOfType<BudgetAllocationService>(); d.budgetAllocation = bas != null ? bas.CaptureSaveData() : BudgetAllocationData.Default(DEFAULT_S_CAP);
        d.pendingProposals = new List<UrbanizationProposal>(); if (miniMapController != null) d.minimapActiveLayers = (int)miniMapController.GetActiveLayers();
        d.neighborStubs = _neighborStubs != null ? new List<NeighborCityStub>(_neighborStubs) : new List<NeighborCityStub>();
        d.neighborCityBindings = neighborCityBindings != null ? new List<NeighborCityBinding>(neighborCityBindings) : new List<NeighborCityBinding>();
        Territory.UI.UIManager uim = FindObjectOfType<Territory.UI.UIManager>(); d.overlayActive = uim != null ? uim.CaptureOverlayActiveForSave() : new List<bool>();
        return d;
    }

    public static SaveFileMeta[] GetSaveFiles(string saveDir)
    { string[] paths = SaveService.GetSortedFilePaths(saveDir); var metas = new List<SaveFileMeta>(); foreach (string p in paths) { var m = GetSaveMetadata(p); if (m.displayName != null) metas.Add(new SaveFileMeta(p, m.displayName, m.sortDate)); } metas.Sort((a, b) => b.SortDate.CompareTo(a.SortDate)); return metas.ToArray(); }

    public static void DeleteSave(string filePath) => SaveService.DeleteSave(filePath);
    public static bool HasAnySave(string saveDir) => SaveService.HasAnySave(saveDir);

    public static SaveFileMeta GetMostRecentSave(string saveDir)
    { string[] paths = SaveService.GetSortedFilePaths(saveDir); SaveFileMeta best = null; foreach (string p in paths) { var m = GetSaveMetadata(p); if (m.displayName == null) continue; if (best == null || m.sortDate > best.SortDate) best = new SaveFileMeta(p, m.displayName, m.sortDate); } return best; }

    public static (string displayName, DateTime sortDate) GetSaveMetadata(string filePath)
    {
        if (!File.Exists(filePath)) return (null, DateTime.MinValue);
        try { string json = File.ReadAllText(filePath); GameSaveData data = JsonUtility.FromJson<GameSaveData>(json); DateTime sd2 = data.realWorldSaveTimeTicks > 0 ? new DateTime(data.realWorldSaveTimeTicks, DateTimeKind.Utc) : File.GetLastWriteTimeUtc(filePath); string name = !string.IsNullOrEmpty(data.saveName) ? data.saveName : (!string.IsNullOrEmpty(data.cityName) ? data.cityName : Path.GetFileNameWithoutExtension(filePath)); return (name ?? Path.GetFileNameWithoutExtension(filePath), sd2); }
        catch { return (Path.GetFileNameWithoutExtension(filePath), File.GetLastWriteTimeUtc(filePath)); }
    }

    public void LoadGame(string saveFilePath)
    {
        string json = _svc.ReadJson(saveFilePath); if (json == null) return;
        GameSaveData d = JsonUtility.FromJson<GameSaveData>(json);
        MigrateLoadedSaveData(d); RemapStateServiceZoneEntityIds(d.stateServiceZones);
        regionId = d.regionId; countryId = d.countryId;
        _neighborStubs = d.neighborStubs ?? new List<NeighborCityStub>(); neighborCityBindings = d.neighborCityBindings ?? new List<NeighborCityBinding>();
        gridManager.HydrateParentIds(d.regionId, d.countryId); gridManager.HydrateNeighborStubs(_neighborStubs);
        if (d.gridWidth > 0 && d.gridHeight > 0) { gridManager.width = d.gridWidth; gridManager.height = d.gridHeight; }
        else if (d.gridData != null && d.gridData.Count > 0) { int mx = 0, my = 0; foreach (var c in d.gridData) { if (c.x > mx) mx = c.x; if (c.y > my) my = c.y; } gridManager.width = mx + 1; gridManager.height = my + 1; }
        gridManager.ResetGridForLoad();
        TerrainManager tm = FindObjectOfType<TerrainManager>(); if (tm != null && d.gridData != null) { tm.RestoreHeightMapFromGridData(d.gridData); tm.ApplyRestoredPositionsToGrid(); }
        WaterManager wm = FindObjectOfType<WaterManager>(); if (wm != null && d.gridData != null) wm.RestoreWaterMapFromSaveData(d.waterMapData, gridManager.width, gridManager.height, d.gridData);
        gridManager.RestoreGrid(d.gridData); if (wm != null) wm.MigrateWaterBodyIdsAfterGridRestore();
        DistrictManager dm = FindObjectOfType<DistrictManager>(); if (dm != null) { if (d.districtMap != null && dm.Map != null) dm.Map.RestoreFromSerializableData(d.districtMap); else dm.Rebuild(); }
        HappinessComposer hc = FindObjectOfType<HappinessComposer>(); SignalTuningWeightsAsset wa = hc != null ? hc.Weights : null; if (wa != null && d.tuningWeights != null) wa.RestoreFromData(d.tuningWeights);
        SignalFieldRegistry sfr = FindObjectOfType<SignalFieldRegistry>(); SignalTickScheduler sts = FindObjectOfType<SignalTickScheduler>();
        if (sfr != null && sts != null) SignalWarmupPass.Run(sfr, dm, sts); else Debug.LogWarning("[GameSaveManager] LoadGame: SignalFieldRegistry or SignalTickScheduler missing.");
        if (miniMapController != null) miniMapController.RebuildTexture();
        if (interstateManager != null) { interstateManager.RebuildFromGrid(); interstateManager.CheckInterstateConnectivity(); }
        cityStats.RestoreCityStatsData(d.cityStats); timeManager.RestoreInGameTime(d.inGameTime);
        RegionalMapManager rmm = FindObjectOfType<RegionalMapManager>(); if (rmm != null && d.regionalMap != null) { rmm.RestoreRegionalMap(d.regionalMap); rmm.PlaceBorderSigns(); }
        GrowthBudgetManager gbm = FindObjectOfType<GrowthBudgetManager>(); if (gbm != null && d.growthBudget != null) { gbm.data = d.growthBudget.Clone(); MigrateGrowthBudgetFromLegacy(gbm); }
        BudgetAllocationService bas = FindObjectOfType<BudgetAllocationService>(); if (bas != null) bas.RestoreFromSaveData(d.budgetAllocation);
        UrbanizationProposalManager upm = FindObjectOfType<UrbanizationProposalManager>(); if (upm != null) upm.RestorePendingProposals(new List<UrbanizationProposal>());
        if (miniMapController != null) { var layers = (MiniMapLayer)d.minimapActiveLayers; if (layers == 0) layers = MiniMapLayer.Streets | MiniMapLayer.Zones; miniMapController.SetActiveLayers(layers); }
        Territory.UI.UIManager uim = FindObjectOfType<Territory.UI.UIManager>(); if (uim != null) uim.LoadOverlayStateFromSaveData(d.overlayActive);
    }

    static void MigrateLoadedSaveData(GameSaveData d)
    {
        if (d.schemaVersion < 1 || string.IsNullOrEmpty(d.regionId)) d.regionId = Guid.NewGuid().ToString();
        if (d.schemaVersion < 1 || string.IsNullOrEmpty(d.countryId)) d.countryId = Guid.NewGuid().ToString();
        if (d.neighborStubs == null) d.neighborStubs = new List<NeighborCityStub>();
        if (d.neighborCityBindings == null) d.neighborCityBindings = new List<NeighborCityBinding>();
        if (d.stateServiceZones == null) d.stateServiceZones = new List<StateServiceZoneData>();
        if (d.budgetAllocation == null) d.budgetAllocation = BudgetAllocationData.Default(DEFAULT_S_CAP);
        if (d.overlayActive == null) d.overlayActive = new List<bool>();
        d.schemaVersion = GameSaveData.CurrentSchemaVersion;
        if (string.IsNullOrEmpty(d.regionId) || string.IsNullOrEmpty(d.countryId)) throw new InvalidOperationException("[GameSaveManager] MigrateLoadedSaveData: parent ids empty after migration.");
        if (d.neighborStubs == null) throw new InvalidOperationException("[GameSaveManager] neighborStubs null after migration.");
        if (d.neighborCityBindings == null) throw new InvalidOperationException("[GameSaveManager] neighborCityBindings null after migration.");
        if (d.stateServiceZones == null) throw new InvalidOperationException("[GameSaveManager] stateServiceZones null after migration.");
        if (d.budgetAllocation == null) throw new InvalidOperationException("[GameSaveManager] budgetAllocation null after migration.");
    }

    void RemapStateServiceZoneEntityIds(List<StateServiceZoneData> zones)
    { if (zones == null || zones.Count == 0) return; Territory.Catalog.CatalogLoader cl = FindObjectOfType<Territory.Catalog.CatalogLoader>(); if (cl == null) return; for (int i = 0; i < zones.Count; i++) { var z = zones[i]; if (z == null || !string.IsNullOrEmpty(z.entityId) || z.subTypeId < 0) continue; if (cl.TryResolveByLegacyAssetId(z.subTypeId, out var resolved)) z.entityId = resolved; } }

    static void MigrateGrowthBudgetFromLegacy(GrowthBudgetManager gbm)
    { var d = gbm.data; if (d.growthBudgetPercent == 0 && d.totalGrowthBudget > 0) { int m = gbm.cityStats != null ? gbm.cityStats.money : 20000; d.growthBudgetPercent = Mathf.Clamp(m > 0 ? (d.totalGrowthBudget * 100 / m) : 10, 0, 100); } else if (d.growthBudgetPercent == 0) d.growthBudgetPercent = 10; }

    public void NewGame()
    {
        MapGenerationSeed.RollNewMasterSeed();
        RegionalMapManager rmm = FindObjectOfType<RegionalMapManager>(); if (rmm != null) rmm.ClearBorderSigns();
        gridManager.ResetGrid();
        regionId = Guid.NewGuid().ToString(); countryId = Guid.NewGuid().ToString();
        gridManager.HydrateParentIds(regionId, countryId);
        GeographyManager gm = FindObjectOfType<GeographyManager>(); if (gm != null) gm.ReinitializeGeographyForNewGame();
        _neighborStubs = new List<NeighborCityStub>(); neighborCityBindings = new List<NeighborCityBinding>();
        InterstateManager im = interstateManager != null ? interstateManager : FindObjectOfType<InterstateManager>();
        NeighborStubSeeder.SeedInitial(new GameSaveData { neighborStubs = _neighborStubs }, im, MapGenerationSeed.MasterSeed);
        gridManager.HydrateNeighborStubs(_neighborStubs); cityStats.ResetCityStats();
        if (rmm != null) { var pt = rmm.GetRegionalMap()?.GetPlayerTerritory(); if (pt != null && !string.IsNullOrEmpty(pt.cityName)) cityStats.cityName = pt.cityName; }
        timeManager.ResetInGameTime();
        GrowthBudgetManager gbm = FindObjectOfType<GrowthBudgetManager>(); if (gbm != null) gbm.data = new GrowthBudgetData();
        UrbanizationProposalManager upm = FindObjectOfType<UrbanizationProposalManager>(); if (upm != null) upm.RestorePendingProposals(new List<UrbanizationProposal>());
    }
}
}
