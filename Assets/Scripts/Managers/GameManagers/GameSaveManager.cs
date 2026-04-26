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

namespace Territory.Persistence
{
/// <summary>
/// Serialize + deserialize full game state.
/// Coordinates: <see cref="GridManager"/> (grid data), <see cref="WaterManager"/> (<see cref="WaterMapData"/>), <see cref="CityStats"/>, <see cref="TimeManager"/>.
/// Load → restore height map + water map before <see cref="GridManager.RestoreGrid"/> (snapshot visuals; no terrain/water slope regen or global sorting recalc).
/// Agent <b>test mode</b> resolves committed JSON under <c>tools/fixtures/scenarios/</c> via CLI (<see cref="Territory.Testing.TestModeCommandLineBootstrap"/>), loads through <see cref="LoadGame"/> only.
/// Scenario builder batch tooling writes snapshots via <see cref="TryWriteGameSaveToPath"/>.
/// </summary>
public class GameSaveManager : MonoBehaviour
{
    public string saveName;
    public string cityName;
    public DateTime realWorldSaveTime;

    /// <summary>Parent region id (GUID string) for the loaded / active city. Populated on load or first save.</summary>
    [NonSerialized] public string regionId;
    /// <summary>Parent country id (GUID string) for the loaded / active city. Populated on load or first save.</summary>
    [NonSerialized] public string countryId;
    /// <summary>
    /// In-memory neighbor stubs for the active session. Seeded by <see cref="Territory.Core.NeighborStubSeeder"/>
    /// in <see cref="NewGame"/>; restored from save in <see cref="LoadGame"/>.
    /// <see cref="BuildCurrentGameSaveData"/> writes this list into <see cref="GameSaveData.neighborStubs"/>.
    /// See <b>parent-scale stub</b> glossary term.
    /// </summary>
    [NonSerialized] private List<NeighborCityStub> _neighborStubs = new List<NeighborCityStub>();

    /// <summary>
    /// Read-only view of the in-memory neighbor stubs for the active session.
    /// Used by <see cref="Territory.Core.NeighborCityBindingRecorder"/> to match exits to stubs
    /// without exposing the backing list for mutation.
    /// </summary>
    public IReadOnlyList<NeighborCityStub> NeighborStubs => (_neighborStubs ?? new List<NeighborCityStub>()).AsReadOnly();

    /// <summary>
    /// In-memory interstate border exit bindings for the active session. Populated by
    /// <see cref="Territory.Core.NeighborCityBindingRecorder"/> when an interstate is built.
    /// Restored from save in <see cref="LoadGame"/>; written to save in <see cref="BuildCurrentGameSaveData"/>.
    /// Exposed for recorder mutation.
    /// </summary>
    [NonSerialized] public List<NeighborCityBinding> neighborCityBindings = new List<NeighborCityBinding>();

    // public PlayerSettingsData playerSettings;

    /// <summary>Default spending-cap (§) seeded into <see cref="BudgetAllocationData"/> during v3→v4 migration. MVP deterministic value.</summary>
    private const int DEFAULT_S_CAP = 10_000;

    public GridManager gridManager;
    public CityStats cityStats;
    public TimeManager timeManager;
    public InterstateManager interstateManager;
    public MiniMapController miniMapController;

    public void SaveGame(string customSaveName = null)
    {
        GameSaveData saveData = BuildCurrentGameSaveData(customSaveName);

        string path = Path.Combine(Application.persistentDataPath, saveData.saveName + ".json");
        File.WriteAllText(path, JsonUtility.ToJson(saveData));
        BlipEngine.Play(BlipId.SysSaveGame);

        PlayerPrefs.SetString("LastSavePath", path);
        PlayerPrefs.Save();

        if (GameNotificationManager.Instance != null)
            GameNotificationManager.Instance.PostNotification("Game saved successfully", GameNotificationManager.NotificationType.Success, 3f);
    }

    /// <summary>
    /// Write current scene state as <see cref="GameSaveData"/> JSON to absolute path (scenario builder / batch tooling).
    /// No <c>PlayerPrefs</c> or HUD notifications.
    /// </summary>
    /// <param name="absolutePath">Destination <c>.json</c> path.</param>
    /// <param name="saveNameForPayload"><see cref="GameSaveData.saveName"/>; null → <see cref="CityStats.cityName"/> + timestamp.</param>
    /// <param name="error">Set on write failure.</param>
    /// <returns>True → file written.</returns>
    public bool TryWriteGameSaveToPath(string absolutePath, string saveNameForPayload, out string error)
    {
        error = null;
        try
        {
            GameSaveData saveData = BuildCurrentGameSaveData(saveNameForPayload);
            string dir = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(absolutePath, JsonUtility.ToJson(saveData));
            BlipEngine.Play(BlipId.SysSaveGame);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    GameSaveData BuildCurrentGameSaveData(string customSaveName)
    {
        GameSaveData saveData = new GameSaveData();
        saveData.schemaVersion = GameSaveData.CurrentSchemaVersion;
        // Preserve ids across save/load cycles; allocate fresh GUIDs only on first-ever save (no prior load).
        saveData.regionId = string.IsNullOrEmpty(regionId) ? Guid.NewGuid().ToString() : regionId;
        saveData.countryId = string.IsNullOrEmpty(countryId) ? Guid.NewGuid().ToString() : countryId;
        // Write back to manager so subsequent saves in the same session reuse the same ids.
        regionId = saveData.regionId;
        countryId = saveData.countryId;
        // Defense-in-depth: hydrate GridManager for scenario-builder / test paths that bypass NewGame().
        if (gridManager != null && gridManager.ParentRegionId == null)
            gridManager.HydrateParentIds(regionId, countryId);
        saveData.cityName = cityStats.cityName;
        saveData.realWorldSaveTime = DateTime.Now;
        saveData.realWorldSaveTimeTicks = DateTime.UtcNow.Ticks;
        saveData.saveName = customSaveName ?? $"{saveData.cityName}_{saveData.realWorldSaveTime:yyyyMMdd_HHmmss}";

        saveData.inGameTime = timeManager.GetCurrentInGameTime();
        saveData.gridData = gridManager.GetGridData();
        saveData.gridWidth = gridManager.width;
        saveData.gridHeight = gridManager.height;
        WaterManager waterManagerForSave = FindObjectOfType<WaterManager>();
        if (waterManagerForSave != null && waterManagerForSave.GetWaterMap() != null)
            saveData.waterMapData = waterManagerForSave.GetWaterMap().GetSerializableData();
        DistrictManager districtManagerForSave = FindObjectOfType<DistrictManager>();
        saveData.districtMap = (districtManagerForSave != null && districtManagerForSave.Map != null)
            ? districtManagerForSave.Map.GetSerializableData()
            : null;
        saveData.cityStats = cityStats.GetCityStatsData();
        saveData.isConnectedToInterstate = interstateManager != null && interstateManager.IsConnectedToInterstate;
        RegionalMapManager regionalMapManager = FindObjectOfType<RegionalMapManager>();
        if (regionalMapManager != null)
        {
            regionalMapManager.SyncCityNameToPlayerTerritory();
            saveData.regionalMap = regionalMapManager.GetRegionalMapForSave();
        }
        GrowthBudgetManager growthBudgetManager = FindObjectOfType<GrowthBudgetManager>();
        if (growthBudgetManager != null)
            saveData.growthBudget = growthBudgetManager.data.Clone();
        BudgetAllocationService budgetAllocationSvc = FindObjectOfType<BudgetAllocationService>();
        saveData.budgetAllocation = budgetAllocationSvc != null
            ? budgetAllocationSvc.CaptureSaveData()
            : BudgetAllocationData.Default(DEFAULT_S_CAP);
        BondLedgerService bondLedgerSvc = FindObjectOfType<BondLedgerService>();
        saveData.bondRegistry = bondLedgerSvc != null
            ? bondLedgerSvc.CaptureSaveData()
            : new List<BondData>();
        saveData.pendingProposals = new List<UrbanizationProposal>();
        if (miniMapController != null)
            saveData.minimapActiveLayers = (int)miniMapController.GetActiveLayers();
        // Write in-memory stubs (seeded in NewGame / restored in LoadGame) into save payload.
        saveData.neighborStubs = _neighborStubs != null
            ? new List<NeighborCityStub>(_neighborStubs)
            : new List<NeighborCityStub>();
        // Write in-memory bindings (appended by NeighborCityBindingRecorder / restored in LoadGame).
        saveData.neighborCityBindings = neighborCityBindings != null
            ? new List<NeighborCityBinding>(neighborCityBindings)
            : new List<NeighborCityBinding>();
        return saveData;
    }

    /// <summary>
    /// Read save file metadata for display + sorting. Uses <c>realWorldSaveTimeTicks</c> when present,
    /// else falls back to file last-write time (older saves).
    /// </summary>
    public static (string displayName, DateTime sortDate) GetSaveMetadata(string filePath)
    {
        if (!File.Exists(filePath))
            return (null, DateTime.MinValue);
        try
        {
            string json = File.ReadAllText(filePath);
            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
            DateTime sortDate = data.realWorldSaveTimeTicks > 0
                ? new DateTime(data.realWorldSaveTimeTicks, DateTimeKind.Utc)
                : File.GetLastWriteTimeUtc(filePath);
            string displayName = !string.IsNullOrEmpty(data.saveName) ? data.saveName
                : (!string.IsNullOrEmpty(data.cityName) ? data.cityName : Path.GetFileNameWithoutExtension(filePath));
            return (displayName ?? Path.GetFileNameWithoutExtension(filePath), sortDate);
        }
        catch (System.Exception)
        {
            return (Path.GetFileNameWithoutExtension(filePath), File.GetLastWriteTimeUtc(filePath));
        }
    }

    public void LoadGame(string saveFilePath)
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);
            MigrateLoadedSaveData(saveData);
            // Cache parent ids + neighbor stubs on manager so subsequent saves preserve them.
            regionId = saveData.regionId;
            countryId = saveData.countryId;
            _neighborStubs = saveData.neighborStubs ?? new List<NeighborCityStub>();
            // Restore bindings (schema 3; empty list for schema ≤ 2 saves after migration).
            neighborCityBindings = saveData.neighborCityBindings ?? new List<NeighborCityBinding>();
            // Hydrate GridManager surface before RestoreGrid so consumers see non-null ids from grid-ready.
            gridManager.HydrateParentIds(saveData.regionId, saveData.countryId);
            // Hydrate neighbor stubs immediately after parent ids (one-shot pattern).
            gridManager.HydrateNeighborStubs(_neighborStubs);

            // Use saved grid dimensions (or infer from gridData for old saves) before reset
            if (saveData.gridWidth > 0 && saveData.gridHeight > 0)
            {
                gridManager.width = saveData.gridWidth;
                gridManager.height = saveData.gridHeight;
            }
            else if (saveData.gridData != null && saveData.gridData.Count > 0)
            {
                int maxX = 0, maxY = 0;
                foreach (var c in saveData.gridData)
                {
                    if (c.x > maxX) maxX = c.x;
                    if (c.y > maxY) maxY = c.y;
                }
                gridManager.width = maxX + 1;
                gridManager.height = maxY + 1;
            }

            // Clear grid generated by InitializeGeography so restoration runs on a clean slate.
            // Without this, Load works in-session but fails after Unity restart (cold start).
            gridManager.ResetGridForLoad();

            TerrainManager terrainManager = FindObjectOfType<TerrainManager>();
            if (terrainManager != null && saveData.gridData != null)
            {
                terrainManager.RestoreHeightMapFromGridData(saveData.gridData);
                terrainManager.ApplyRestoredPositionsToGrid();
            }

            WaterManager waterManager = FindObjectOfType<WaterManager>();
            if (waterManager != null && saveData.gridData != null)
                waterManager.RestoreWaterMapFromSaveData(saveData.waterMapData, gridManager.width, gridManager.height, saveData.gridData);

            gridManager.RestoreGrid(saveData.gridData);

            if (waterManager != null)
                waterManager.MigrateWaterBodyIdsAfterGridRestore();

            DistrictManager districtManagerForLoad = FindObjectOfType<DistrictManager>();
            if (districtManagerForLoad != null)
            {
                if (saveData.districtMap != null && districtManagerForLoad.Map != null)
                    districtManagerForLoad.Map.RestoreFromSerializableData(saveData.districtMap);
                else
                    districtManagerForLoad.Rebuild();
            }

            if (miniMapController != null)
                miniMapController.RebuildTexture();

            if (interstateManager != null)
            {
                interstateManager.RebuildFromGrid();
                interstateManager.CheckInterstateConnectivity();
            }
            cityStats.RestoreCityStatsData(saveData.cityStats);
            timeManager.RestoreInGameTime(saveData.inGameTime);
            RegionalMapManager regionalMapManager = FindObjectOfType<RegionalMapManager>();
            if (regionalMapManager != null && saveData.regionalMap != null)
            {
                regionalMapManager.RestoreRegionalMap(saveData.regionalMap);
                regionalMapManager.PlaceBorderSigns();
            }
            GrowthBudgetManager growthBudgetManager = FindObjectOfType<GrowthBudgetManager>();
            if (growthBudgetManager != null && saveData.growthBudget != null)
            {
                growthBudgetManager.data = saveData.growthBudget.Clone();
                MigrateGrowthBudgetFromLegacy(growthBudgetManager);
            }
            BudgetAllocationService budgetAllocationSvc = FindObjectOfType<BudgetAllocationService>();
            if (budgetAllocationSvc != null)
                budgetAllocationSvc.RestoreFromSaveData(saveData.budgetAllocation);
            BondLedgerService bondLedgerSvc = FindObjectOfType<BondLedgerService>();
            if (bondLedgerSvc != null)
                bondLedgerSvc.RestoreFromSaveData(saveData.bondRegistry);
            // Proposal flow disabled: clear any pending proposals on load
            UrbanizationProposalManager proposalManager = FindObjectOfType<UrbanizationProposalManager>();
            if (proposalManager != null)
                proposalManager.RestorePendingProposals(new List<UrbanizationProposal>());
            if (miniMapController != null)
            {
                var layers = (MiniMapLayer)saveData.minimapActiveLayers;
                if (layers == 0)
                    layers = MiniMapLayer.Streets | MiniMapLayer.Zones;
                miniMapController.SetActiveLayers(layers);
            }
        }
        else { }
    }

    /// <summary>
    /// Migrate <paramref name="data"/> to <see cref="GameSaveData.CurrentSchemaVersion"/>.
    /// Run post-deserialize, pre-restore. Idempotent — safe to call on already-migrated data.
    /// Schema 0 → 1: allocate placeholder GUIDs for missing <c>regionId</c> / <c>countryId</c>.
    /// Schema 3 → 4: seed <c>stateServiceZones</c> empty + <c>budgetAllocation</c> equal-envelope default.
    /// Schema 4 → 5: <c>districtMap</c> left untouched (null stays null — <see cref="LoadGame"/> falls back to <c>DistrictManager.Rebuild()</c>; non-null payload preserved byte-identical).
    /// </summary>
    static void MigrateLoadedSaveData(GameSaveData data)
    {
        if (data.schemaVersion < 1 || string.IsNullOrEmpty(data.regionId))
            data.regionId = Guid.NewGuid().ToString();
        if (data.schemaVersion < 1 || string.IsNullOrEmpty(data.countryId))
            data.countryId = Guid.NewGuid().ToString();
        // Schema 2: neighborStubs absent in legacy (schema ≤ 1) saves → initialize to empty list.
        // Preserves any entries already deserialized from newer saves (forward-compat with placement).
        if (data.neighborStubs == null)
            data.neighborStubs = new List<NeighborCityStub>();
        // Schema 3: neighborCityBindings absent in schema ≤ 2 saves → initialize to empty list.
        if (data.neighborCityBindings == null)
            data.neighborCityBindings = new List<NeighborCityBinding>();
        // Schema 4: stateServiceZones + budgetAllocation absent in schema ≤ 3 saves → seed defaults.
        // Null-coalesce style: non-null fields (already-v4 saves) are preserved byte-identical.
        if (data.stateServiceZones == null)
            data.stateServiceZones = new List<StateServiceZoneData>();
        if (data.budgetAllocation == null)
            data.budgetAllocation = BudgetAllocationData.Default(DEFAULT_S_CAP);
        if (data.bondRegistry == null)
            data.bondRegistry = new List<BondData>();
        data.schemaVersion = GameSaveData.CurrentSchemaVersion;

        if (string.IsNullOrEmpty(data.regionId) || string.IsNullOrEmpty(data.countryId))
            throw new InvalidOperationException("[GameSaveManager] MigrateLoadedSaveData: parent ids still empty after migration — save data integrity error.");
        if (data.neighborStubs == null)
            throw new InvalidOperationException("[GameSaveManager] MigrateLoadedSaveData: neighborStubs null after migration — save data integrity error.");
        if (data.neighborCityBindings == null)
            throw new InvalidOperationException("[GameSaveManager] MigrateLoadedSaveData: neighborCityBindings null after migration — save data integrity error.");
        if (data.stateServiceZones == null)
            throw new InvalidOperationException("[GameSaveManager] MigrateLoadedSaveData: stateServiceZones null after migration — save data integrity error.");
        if (data.budgetAllocation == null)
            throw new InvalidOperationException("[GameSaveManager] MigrateLoadedSaveData: budgetAllocation null after migration — save data integrity error.");
        if (data.bondRegistry == null)
            throw new InvalidOperationException("[GameSaveManager] MigrateLoadedSaveData: bondRegistry null after migration — save data integrity error.");
    }

    /// <summary>Migrate old saves storing <c>totalGrowthBudget</c> (amount) → <c>growthBudgetPercent</c>.</summary>
    static void MigrateGrowthBudgetFromLegacy(GrowthBudgetManager growthBudgetManager)
    {
        var d = growthBudgetManager.data;
        if (d.growthBudgetPercent == 0 && d.totalGrowthBudget > 0)
        {
            int money = growthBudgetManager.cityStats != null ? growthBudgetManager.cityStats.money : 20000;
            d.growthBudgetPercent = Mathf.Clamp(money > 0 ? (d.totalGrowthBudget * 100 / money) : 10, 0, 100);
        }
        else if (d.growthBudgetPercent == 0)
        {
            d.growthBudgetPercent = 10;
        }
    }

    public void NewGame()
    {
        MapGenerationSeed.RollNewMasterSeed();

        RegionalMapManager regionalMapManager = FindObjectOfType<RegionalMapManager>();
        if (regionalMapManager != null)
            regionalMapManager.ClearBorderSigns();
        gridManager.ResetGrid();

        // Allocate placeholder parent ids eagerly so GridManager surface is non-null before first save.
        regionId = Guid.NewGuid().ToString();
        countryId = Guid.NewGuid().ToString();
        gridManager.HydrateParentIds(regionId, countryId);

        GeographyManager geographyManager = FindObjectOfType<GeographyManager>();
        if (geographyManager != null)
            geographyManager.ReinitializeGeographyForNewGame();

        // Seed neighbor stubs after geography (interstate endpoints must exist first).
        // FindObjectOfType here is acceptable — NewGame is not a per-frame path (matches
        // RegionalMapManager / GeographyManager pattern above).
        _neighborStubs = new List<NeighborCityStub>();
        // Reset bindings on new game (re-seeded when interstate is placed).
        neighborCityBindings = new List<NeighborCityBinding>();
        InterstateManager interstateManagerForSeed = interstateManager != null
            ? interstateManager
            : FindObjectOfType<InterstateManager>();
        NeighborStubSeeder.SeedInitial(
            new GameSaveData { neighborStubs = _neighborStubs },
            interstateManagerForSeed,
            MapGenerationSeed.MasterSeed);
        // Hydrate GridManager stub cache after seeding so accessor is ready before first Update.
        gridManager.HydrateNeighborStubs(_neighborStubs);

        cityStats.ResetCityStats();
        if (regionalMapManager != null)
        {
            var playerTerritory = regionalMapManager.GetRegionalMap()?.GetPlayerTerritory();
            if (playerTerritory != null && !string.IsNullOrEmpty(playerTerritory.cityName))
                cityStats.cityName = playerTerritory.cityName;
        }
        timeManager.ResetInGameTime();
        GrowthBudgetManager growthBudgetManager = FindObjectOfType<GrowthBudgetManager>();
        if (growthBudgetManager != null)
            growthBudgetManager.data = new GrowthBudgetData();
        UrbanizationProposalManager proposalManager = FindObjectOfType<UrbanizationProposalManager>();
        if (proposalManager != null)
            proposalManager.RestorePendingProposals(new List<UrbanizationProposal>());
    }
}

[System.Serializable]
public class GameSaveData
{
    /// <summary>Save schema version. 0 = legacy. Current = <see cref="CurrentSchemaVersion"/>.</summary>
    public int schemaVersion;
    /// <summary>Parent region GUID (string). Populated at new-game init or legacy-save migration. Non-null post-load.</summary>
    public string regionId;
    /// <summary>Parent country GUID (string). Populated at new-game init or legacy-save migration. Non-null post-load.</summary>
    public string countryId;

    public string saveName;
    public string cityName;
    public DateTime realWorldSaveTime;
    public long realWorldSaveTimeTicks;
    public InGameTime inGameTime;
    public List<CellData> gridData;
    public int gridWidth;
    public int gridHeight;
    /// <summary>Serialized <see cref="Territory.Terrain.WaterMap"/> state. Null → legacy saves.</summary>
    public WaterMapData waterMapData;
    /// <summary>Serialized <see cref="Territory.Simulation.Signals.DistrictMap"/> state. Null → schema ≤ 4 saves; <see cref="LoadGame"/> falls back to <c>DistrictManager.Rebuild()</c>. Added schema 5.</summary>
    public DistrictMapData districtMap;
    public bool isConnectedToInterstate;
    public RegionalMap regionalMap;

    // public PlayerSettingsData playerSettings;
    public CityStatsData cityStats;
    public GrowthBudgetData growthBudget;
    public List<UrbanizationProposal> pendingProposals;
    public int minimapActiveLayers;

    /// <summary>
    /// Current save schema version. Bump when adding migration-required fields.
    /// Schema 2 adds <c>neighborStubs</c> (see <b>parent-scale stub</b> glossary term).
    /// Schema 3 adds <c>neighborCityBindings</c> (interstate border exit bindings).
    /// Schema 4 adds <c>budgetAllocation</c> + <c>stateServiceZones</c> (envelope budget + state-service zone registry — Stage 1.3 Phase 3)
    /// + <c>bondRegistry</c> (bond ledger active bonds — Stage 4).
    /// Schema 5 adds <c>districtMap</c> (Stage 3 District layer — per-cell <see cref="DistrictMap"/> ordinal round-trip).
    /// </summary>
    public const int CurrentSchemaVersion = 5;

    /// <summary>
    /// Neighbor-city stubs at this city's interstate map borders.
    /// Non-null post-load; empty list is a valid steady state (zero interstate exits).
    /// See <b>parent-scale stub</b> glossary term.
    /// </summary>
    public List<NeighborCityStub> neighborStubs = new List<NeighborCityStub>();

    /// <summary>
    /// Interstate border exit bindings. Each entry records one exit cell's link to a
    /// <see cref="NeighborCityStub"/> (stubId + grid coord). Non-null post-load (schema ≤ 2
    /// saves migrate to empty list). Dedupe key: (stubId, exitCellX, exitCellY).
    /// Added schema 3.
    /// </summary>
    public List<NeighborCityBinding> neighborCityBindings = new List<NeighborCityBinding>();

    /// <summary>
    /// Envelope budget snapshot (pct shares + cap + current-month remaining).
    /// Null on legacy v3 saves — migration branch seeds via <see cref="BudgetAllocationData.Default"/>.
    /// Added schema 4.
    /// </summary>
    public BudgetAllocationData budgetAllocation;

    /// <summary>
    /// State-service zone placement registry.
    /// Empty list on fresh games; populated in Step 2 placement task.
    /// Added schema 4.
    /// </summary>
    public List<StateServiceZoneData> stateServiceZones = new List<StateServiceZoneData>();

    /// <summary>
    /// Active bond registry serialized as list (JsonUtility does not support Dictionary).
    /// Each entry is one active bond keyed by <see cref="BondData.scaleTier"/>.
    /// Empty list on fresh games; populated when bonds are issued.
    /// Added schema 4 (Stage 4 — bond ledger).
    /// </summary>
    public List<BondData> bondRegistry = new List<BondData>();
}
}
