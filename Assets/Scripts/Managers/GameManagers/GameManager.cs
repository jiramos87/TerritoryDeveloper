using System.Collections.Generic;
using UnityEngine;
using Territory.Core;

namespace Territory.Persistence
{
/// <summary>
/// Entry point for game initialization and save/load orchestration.
/// Coordinates with GridManager for grid restoration and GameSaveManager for persistence.
/// </summary>
public class GameManager : MonoBehaviour
{
    private GridManager gridManager;
    private GameSaveManager saveManager;

    void Start()
    {
        gridManager = FindObjectOfType<GridManager>();
        saveManager = FindObjectOfType<GameSaveManager>();
    }

    public void SaveGame(string saveName = null)
    {
        saveManager.SaveGame(saveName);
    }

    public void LoadGame(string saveFilePath)
    {
        Debug.Log("LoadGame saveFilePath: " + saveFilePath);
        saveManager.LoadGame(saveFilePath);
    }

    public void RestoreGame(List<CellData> savedGridData)
    {
        gridManager.RestoreGrid(savedGridData);
    }

    public void CreateNewGame()
    {
        saveManager.NewGame();
    }
}
}
