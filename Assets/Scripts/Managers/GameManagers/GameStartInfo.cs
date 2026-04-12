namespace Territory.Persistence
{
/// <summary>
/// Static holder → pass game start intent from MainMenu to MainScene. Set before scene transition
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

    /// <summary>Clear start info after consumption.</summary>
    public static void Clear()
    {
        Mode = StartMode.None;
        PendingLoadPath = null;
    }
}
}
