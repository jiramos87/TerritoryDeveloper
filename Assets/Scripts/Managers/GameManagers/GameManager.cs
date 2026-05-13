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
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GameSaveManager saveManager;

    void Start()
    {
        if (gridManager == null) Debug.LogWarning("[GameManager] gridManager inspector ref not wired — wire in CityScene.");
        if (saveManager == null) Debug.LogWarning("[GameManager] saveManager inspector ref not wired — wire in CityScene.");
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
