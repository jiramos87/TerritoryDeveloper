using System.Collections.Generic;
using UnityEngine;
using Territory.Core;

namespace Territory.Persistence
{
/// <summary>
/// Entry point for game init + save/load orchestration.
/// Coords with <see cref="GridManager"/> (grid restore) + <see cref="GameSaveManager"/> (persistence).
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
