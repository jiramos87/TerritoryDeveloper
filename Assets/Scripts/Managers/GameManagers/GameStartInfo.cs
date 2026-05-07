namespace Territory.Persistence
{
/// <summary>
/// Static holder → pass game start intent from MainMenu to CityScene. Set before scene transition
/// to signal load-save vs new-game vs continue-last-save.
/// </summary>
public static class GameStartInfo
{
    public enum StartMode
    {
        None,
        NewGame,
        Load
    }

    public static StartMode Mode { get; set; } = StartMode.None;
    public static string PendingLoadPath { get; set; }
    public static int MapSize { get; set; }
    public static int Seed { get; set; }
    public static int ScenarioIndex { get; set; }

    /// <summary>Set load path + mode=Load.</summary>
    public static void SetPendingLoadPath(string path)
    {
        PendingLoadPath = path;
        Mode = StartMode.Load;
    }

    /// <summary>Set mode=NewGame + clear pending load path.</summary>
    public static void SetStartModeNewGame()
    {
        Mode = StartMode.NewGame;
        PendingLoadPath = null;
    }

    /// <summary>Set mode=NewGame with map parameters.</summary>
    public static void SetStartModeNewGame(int mapSize, int seed, int scenarioIndex)
    {
        MapSize = mapSize;
        Seed = seed;
        ScenarioIndex = scenarioIndex;
        SetStartModeNewGame();
    }

    /// <summary>Clear start info after consumption.</summary>
    public static void Clear()
    {
        Mode = StartMode.None;
        PendingLoadPath = null;
        MapSize = 0;
        Seed = 0;
        ScenarioIndex = 0;
    }
}
}
