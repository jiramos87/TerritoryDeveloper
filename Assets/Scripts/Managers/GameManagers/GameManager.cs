using System.Collections.Generic;
using UnityEngine;

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
        saveManager.SaveGame();
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