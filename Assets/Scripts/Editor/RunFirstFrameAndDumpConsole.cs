using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Batch-mode entrypoint: load CityScene, wait one first frame, dump LogEntries to stdout as JSON.
/// Used by <c>verify:local</c> ship gate in tandem with <c>tools/scripts/console-scan.mjs</c>.
///
/// Usage (batch mode):
///   Unity -batchmode -projectPath &lt;repo&gt; -executeMethod RunFirstFrameAndDumpConsole.Run
///         -outputPath /tmp/unity-console.json -logFile /tmp/unity-batch.log -quit
/// </summary>
public static class RunFirstFrameAndDumpConsole
{
    const string CityScenePath = "Assets/Scenes/CityScene.unity";
    const string FallbackOutputPath = "/tmp/unity-console.json";

    /// <summary>Called by Unity in <c>-batchmode -executeMethod RunFirstFrameAndDumpConsole.Run</c>.</summary>
    public static void Run()
    {
        string outputPath = FallbackOutputPath;

        // Parse -outputPath from command line args.
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "-outputPath")
            {
                outputPath = args[i + 1];
                break;
            }
        }

        // Subscribe to log messages before loading scene.
        var collected = new List<LogLineRecord>();
        Application.logMessageReceived += (condition, stack, type) =>
        {
            collected.Add(new LogLineRecord
            {
                severity = LogTypeToSeverity(type),
                message = condition ?? string.Empty,
                stack = stack ?? string.Empty,
                timestamp_utc = DateTime.UtcNow.ToString("o"),
            });
        };

        // Load CityScene in single mode.
        try
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                CityScenePath,
                UnityEditor.SceneManagement.OpenSceneMode.Single);

            if (!scene.IsValid())
            {
                Debug.LogError($"[RunFirstFrameAndDumpConsole] Could not load scene: {CityScenePath}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RunFirstFrameAndDumpConsole] Scene load exception: {ex.Message}");
        }

        // Flush via delayCall so the first-frame logs are captured.
        EditorApplication.delayCall += () => DumpAndQuit(collected, outputPath);
    }

    static void DumpAndQuit(List<LogLineRecord> lines, string outputPath)
    {
        try
        {
            var sb = new System.Text.StringBuilder();
            sb.Append('[');
            for (int i = 0; i < lines.Count; i++)
            {
                if (i > 0) sb.Append(',');
                var l = lines[i];
                sb.Append('{');
                sb.Append($"\"severity\":\"{Esc(l.severity)}\",");
                sb.Append($"\"message\":\"{Esc(l.message)}\",");
                sb.Append($"\"stack\":\"{Esc(l.stack)}\",");
                sb.Append($"\"timestamp_utc\":\"{Esc(l.timestamp_utc)}\"");
                sb.Append('}');
            }
            sb.Append(']');
            File.WriteAllText(outputPath, sb.ToString());
            Debug.Log($"[RunFirstFrameAndDumpConsole] Dumped {lines.Count} log entries to {outputPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RunFirstFrameAndDumpConsole] Write failed: {ex.Message}");
        }

        EditorApplication.Exit(0);
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

    static string Esc(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    }

    struct LogLineRecord
    {
        public string severity;
        public string message;
        public string stack;
        public string timestamp_utc;
    }
}
