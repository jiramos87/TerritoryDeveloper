using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GameSaveManager : MonoBehaviour
{
    public string saveName;
    public string cityName;
    public DateTime realWorldSaveTime;

    // public PlayerSettingsData playerSettings;

    public GridManager gridManager;
    public CityStats cityStats;
    public TimeManager timeManager;

    public void SaveGame(string customSaveName = null)
    {
        GameSaveData saveData = new GameSaveData();
        saveData.cityName = cityStats.cityName; 
        saveData.realWorldSaveTime = DateTime.Now;
        saveData.saveName = customSaveName ?? $"{saveData.cityName}_{saveData.realWorldSaveTime:yyyyMMdd_HHmmss}";

        saveData.inGameTime = timeManager.GetCurrentInGameTime();
        saveData.gridData = gridManager.GetGridData();
        saveData.cityStats = cityStats.GetCityStatsData();
        // saveData.playerSettings = GetPlayerSettings();

        string json = JsonUtility.ToJson(saveData);

        string path = Path.Combine(Application.persistentDataPath, saveData.saveName + ".json");
        File.WriteAllText(path, json);
    }

    public void LoadGame(string saveFilePath)
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);

            gridManager.RestoreGrid(saveData.gridData);
            cityStats.RestoreCityStatsData(saveData.cityStats);
            timeManager.RestoreInGameTime(saveData.inGameTime);
        }
        else
        {
            Debug.LogWarning("Save file not found!");
        }
    }

    public void NewGame()
    {
        gridManager.ResetGrid();
        cityStats.ResetCityStats();
        timeManager.ResetInGameTime();
    }
}

[System.Serializable]
public class GameSaveData
{
    public string saveName;
    public string cityName;
    public DateTime realWorldSaveTime;
    public InGameTime inGameTime;
    public List<CellData> gridData;

    // public PlayerSettingsData playerSettings;
    public CityStatsData cityStats;
}