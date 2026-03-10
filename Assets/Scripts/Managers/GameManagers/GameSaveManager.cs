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
using Territory.Terrain;
using Territory.UI;

namespace Territory.Persistence
{
/// <summary>
/// Handles serialization and deserialization of the complete game state.
/// Coordinates with GridManager for grid data, CityStats for city state, and TimeManager for time state.
/// </summary>
public class GameSaveManager : MonoBehaviour
{
    public string saveName;
    public string cityName;
    public DateTime realWorldSaveTime;

    // public PlayerSettingsData playerSettings;

    public GridManager gridManager;
    public CityStats cityStats;
    public TimeManager timeManager;
    public InterstateManager interstateManager;
    public MiniMapController miniMapController;

    public void SaveGame(string customSaveName = null)
    {
        GameSaveData saveData = new GameSaveData();
        saveData.cityName = cityStats.cityName;
        saveData.realWorldSaveTime = DateTime.Now;
        saveData.realWorldSaveTimeTicks = DateTime.UtcNow.Ticks;
        saveData.saveName = customSaveName ?? $"{saveData.cityName}_{saveData.realWorldSaveTime:yyyyMMdd_HHmmss}";

        saveData.inGameTime = timeManager.GetCurrentInGameTime();
        saveData.gridData = gridManager.GetGridData();
        saveData.gridWidth = gridManager.width;
        saveData.gridHeight = gridManager.height;
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
        // Proposal flow disabled: do not persist pending proposals
        saveData.pendingProposals = new List<UrbanizationProposal>();
        if (miniMapController != null)
            saveData.minimapActiveLayers = (int)miniMapController.GetActiveLayers();
        // saveData.playerSettings = GetPlayerSettings();

        string json = JsonUtility.ToJson(saveData);

        string path = Path.Combine(Application.persistentDataPath, saveData.saveName + ".json");
        File.WriteAllText(path, json);

        PlayerPrefs.SetString("LastSavePath", path);
        PlayerPrefs.Save();

        if (GameNotificationManager.Instance != null)
            GameNotificationManager.Instance.PostNotification("Game saved successfully", GameNotificationManager.NotificationType.Success, 3f);
    }

    /// <summary>
    /// Reads save file metadata for display and sorting. Uses realWorldSaveTimeTicks when present,
    /// otherwise falls back to file last-write time for older saves.
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
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Failed to read save metadata from {filePath}: {ex.Message}");
            return (Path.GetFileNameWithoutExtension(filePath), File.GetLastWriteTimeUtc(filePath));
        }
    }

    public void LoadGame(string saveFilePath)
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);

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
                waterManager.RestoreWaterMapFromGridData(saveData.gridData);

            gridManager.RestoreGrid(saveData.gridData);

            if (terrainManager != null)
            {
                terrainManager.RestoreWaterSlopesFromHeightMap();
                terrainManager.RestoreTerrainSlopesFromHeightMap();
            }

            GeographyManager geographyManager = FindObjectOfType<GeographyManager>();
            if (geographyManager != null)
                geographyManager.ReCalculateSortingOrderBasedOnHeight();

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
        else
        {
            Debug.LogWarning("Save file not found!");
        }
    }

    /// <summary>Migrates old saves that stored totalGrowthBudget (amount) to growthBudgetPercent.</summary>
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
        RegionalMapManager regionalMapManager = FindObjectOfType<RegionalMapManager>();
        if (regionalMapManager != null)
            regionalMapManager.ClearBorderSigns();
        gridManager.ResetGrid();

        GeographyManager geographyManager = FindObjectOfType<GeographyManager>();
        if (geographyManager != null)
            geographyManager.ReinitializeGeographyForNewGame();

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
    public string saveName;
    public string cityName;
    public DateTime realWorldSaveTime;
    public long realWorldSaveTimeTicks;
    public InGameTime inGameTime;
    public List<CellData> gridData;
    public int gridWidth;
    public int gridHeight;
    public bool isConnectedToInterstate;
    public RegionalMap regionalMap;

    // public PlayerSettingsData playerSettings;
    public CityStatsData cityStats;
    public GrowthBudgetData growthBudget;
    public List<UrbanizationProposal> pendingProposals;
    public int minimapActiveLayers;
}
}
