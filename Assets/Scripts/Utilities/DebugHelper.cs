using System.Diagnostics;

namespace Territory.Utilities
{
/// <summary>
/// Debug logging helpers (no-op in all builds — use Unity Console filters or temporary logs when needed).
/// </summary>
public static class DebugHelper
{
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string message) { }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(string message) { }
}
}
