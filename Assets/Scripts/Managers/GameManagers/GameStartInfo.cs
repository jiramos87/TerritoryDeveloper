namespace Territory.Persistence
{
/// <summary>
/// Static holder for passing game start intent from MainMenu to MainScene.
/// Used when transitioning from MainMenu to MainScene to indicate whether
/// to load a save, start new game, or continue last save.
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

    /// <summary>Sets the path to load and mode to Load.</summary>
    public static void SetPendingLoadPath(string path)
    {
        PendingLoadPath = path;
        Mode = StartMode.Load;
    }

    /// <summary>Sets mode to NewGame and clears any pending load path.</summary>
    public static void SetStartModeNewGame()
    {
        Mode = StartMode.NewGame;
        PendingLoadPath = null;
    }

    /// <summary>Clears all start info after consumption.</summary>
    public static void Clear()
    {
        Mode = StartMode.None;
        PendingLoadPath = null;
    }
}
}
