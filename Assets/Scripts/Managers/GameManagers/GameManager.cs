using System.Collections.Generic;
using UnityEngine;
using Territory.Core;
using Territory.IsoSceneCore;
using Domains.Registry;

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

    void Awake()
    {
        // T1.0.2 — persist across additive scene loads (CoreScene shell pattern, DEC-A29).
        // Hub stays in CityScene but survives unload sequence during zoom transition.
        DontDestroyOnLoad(this.gameObject);
    }

    void Start()
    {
        // Fallback resolves so CreateNewGame path doesn't NPE on saveManager.NewGame()
        // (which seeds money to $20,000).
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (saveManager == null) saveManager = FindObjectOfType<GameSaveManager>();

        // Register IsoSceneTickBus + bridge TimeManager → bus.Publish (invariant #12 — Start only)
        var registry = FindObjectOfType<ServiceRegistry>();
        if (registry != null)
        {
            var tickBus = new IsoSceneTickBus();
            registry.Register<IsoSceneTickBus>(tickBus);

            var timeManager = FindObjectOfType<Territory.Timing.TimeManager>();
            if (timeManager != null)
                timeManager.RegisterTickBus(tickBus);
        }
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
