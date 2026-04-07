using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Ring buffer of Unity Console lines for the IDE agent bridge (<c>get_console_logs</c>).
/// Subscribes on first Editor load; clears on script domain reload. Thread-safe adds via lock.
/// </summary>
[InitializeOnLoad]
public static class AgentBridgeConsoleBuffer
{
    const int MaxEntries = 2000;
    const int MaxMessageChars = 4000;
    const int MaxStackChars = 2000;

    static readonly List<LogEntry> s_entries = new List<LogEntry>(512);
    static readonly object s_lock = new object();
    static bool s_subscribed;

    [Serializable]
    public struct LogEntry
    {
        public long utcTicks;
        public string severity;
        public string message;
        public string stack;
    }

    static AgentBridgeConsoleBuffer()
    {
        EnsureSubscribed();
        AssemblyReloadEvents.beforeAssemblyReload += Clear;
    }

    static void EnsureSubscribed()
    {
        if (s_subscribed)
            return;
        Application.logMessageReceived += OnLogMessage;
        s_subscribed = true;
    }

    static void OnLogMessage(string condition, string stackTrace, LogType type)
    {
        Append(condition, stackTrace, type);
    }

    static void Append(string condition, string stackTrace, LogType type)
    {
        if (condition == null)
            condition = string.Empty;
        if (stackTrace == null)
            stackTrace = string.Empty;
        if (condition.Length > MaxMessageChars)
            condition = condition.Substring(0, MaxMessageChars) + "…";
        if (stackTrace.Length > MaxStackChars)
            stackTrace = stackTrace.Substring(0, MaxStackChars) + "…";

        var entry = new LogEntry
        {
            utcTicks = DateTime.UtcNow.Ticks,
            severity = LogTypeToSeverity(type),
            message = condition,
            stack = stackTrace,
        };

        lock (s_lock)
        {
            s_entries.Add(entry);
            while (s_entries.Count > MaxEntries)
                s_entries.RemoveAt(0);
        }
    }

    static string LogTypeToSeverity(LogType type)
    {
        return type switch
        {
            LogType.Error => "error",
            LogType.Assert => "error",
            LogType.Warning => "warning",
            LogType.Log => "log",
            LogType.Exception => "error",
            _ => "log",
        };
    }

    /// <summary>Clears buffered lines (domain reload and explicit tests).</summary>
    public static void Clear()
    {
        lock (s_lock)
        {
            s_entries.Clear();
        }
    }

    /// <summary>
    /// Returns up to <paramref name="maxLines"/> matching entries, oldest first among the tail window.
    /// </summary>
    public static List<LogEntry> Query(
        DateTime? sinceUtc,
        string severityFilter,
        string tagFilter,
        int maxLines)
    {
        if (maxLines < 1)
            maxLines = 1;
        if (maxLines > 2000)
            maxLines = 2000;

        string sev = string.IsNullOrEmpty(severityFilter) ? "all" : severityFilter.Trim().ToLowerInvariant();
        if (sev != "all" && sev != "log" && sev != "warning" && sev != "error")
            sev = "all";

        string tag = string.IsNullOrEmpty(tagFilter) ? null : tagFilter.Trim();
        long sinceTicks = sinceUtc?.Ticks ?? 0;

        List<LogEntry> snapshot;
        lock (s_lock)
        {
            snapshot = new List<LogEntry>(s_entries);
        }

        var matched = new List<LogEntry>();
        for (int i = 0; i < snapshot.Count; i++)
        {
            LogEntry e = snapshot[i];
            if (e.utcTicks < sinceTicks)
                continue;
            if (sev != "all" && !string.Equals(e.severity, sev, StringComparison.OrdinalIgnoreCase))
                continue;
            if (tag != null)
            {
                bool hit = (e.message != null && e.message.IndexOf(tag, StringComparison.OrdinalIgnoreCase) >= 0)
                    || (e.stack != null && e.stack.IndexOf(tag, StringComparison.OrdinalIgnoreCase) >= 0);
                if (!hit)
                    continue;
            }

            matched.Add(e);
        }

        if (matched.Count <= maxLines)
            return matched;

        int skip = matched.Count - maxLines;
        return matched.GetRange(skip, maxLines);
    }
}
