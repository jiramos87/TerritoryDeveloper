#if UNITY_EDITOR
using System;
using System.IO;
using Domains.Testing.Dto;
using UnityEngine;

namespace Domains.Testing.Services
{
    /// <summary>
    /// Reads/writes/deletes the transient batch-runner state file
    /// (tools/reports/.agent-testmode-batch-state.json). Stage 13 tracer slice.
    /// </summary>
    public static class BatchStateService
    {
        /// <summary>Filename of the transient state dotfile under tools/reports/.</summary>
        public const string StateFileName = ".agent-testmode-batch-state.json";

        /// <summary>Returns full path to the state file given the repo root.</summary>
        public static string GetStateFilePath(string repoRoot)
            => Path.Combine(repoRoot, "tools", "reports", StateFileName);

        /// <summary>Reads state from disk. Returns false when absent, empty, or malformed.</summary>
        public static bool TryReadState(string repoRoot, out AgentTestModeBatchStateDto state)
        {
            state = null;
            string path = GetStateFilePath(repoRoot);
            if (!File.Exists(path))
                return false;
            try
            {
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                    return false;
                var dto = JsonUtility.FromJson<AgentTestModeBatchStateDto>(json);
                if (dto == null || dto.phase == 0)
                    return false;
                state = dto;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Writes state to disk; creates directory when absent.</summary>
        public static void WriteState(string repoRoot, AgentTestModeBatchStateDto state)
        {
            string path = GetStateFilePath(repoRoot);
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(path, JsonUtility.ToJson(state, prettyPrint: false));
        }

        /// <summary>Best-effort delete of the state file.</summary>
        public static void DeleteStateFile(string repoRoot)
        {
            try
            {
                string path = GetStateFilePath(repoRoot);
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // Best-effort cleanup.
            }
        }

        /// <summary>Parses ISO 8601 UTC string written by the runner. Returns false on failure.</summary>
        public static bool TryParseStartedUtc(string raw, out DateTime utc)
            => DateTime.TryParse(raw, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind, out utc);
    }
}
#endif
