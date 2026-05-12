using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Domains.Save.Services
{
    /// <summary>
    /// POCO service extracted from GameSaveManager (Stage 5.1 Tier-C NO-PORT).
    /// Pure file-I/O layer: path helpers, file listing, delete, JSON read/write primitives.
    /// Hub (GameSaveManager) owns scene-context logic and delegates file ops here.
    /// Invariants #3 (no per-frame FindObjectOfType), #4 (no new singletons) observed.
    /// </summary>
    public class SaveService
    {
        // ── Wired state ─────────────────────────────────────────────────────────────
        private string _saveDir;

        // ── Setup ────────────────────────────────────────────────────────────────────

        /// <summary>Wire save directory root. Call from hub Start or on demand.</summary>
        public void WireDependencies(string saveDir)
        {
            _saveDir = saveDir;
        }

        // ── Path helpers ─────────────────────────────────────────────────────────────

        /// <summary>Build absolute .json path for saveName under wired saveDir.</summary>
        public string BuildSavePath(string saveName)
        {
            string dir = string.IsNullOrEmpty(_saveDir) ? Application.persistentDataPath : _saveDir;
            return Path.Combine(dir, saveName + ".json");
        }

        // ── File I/O primitives ──────────────────────────────────────────────────────

        /// <summary>Write json to absolutePath. Creates intermediate directories. Returns true on success; sets error on failure.</summary>
        public bool WriteJson(string absolutePath, string json, out string error)
        {
            error = null;
            try
            {
                string dir = Path.GetDirectoryName(absolutePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(absolutePath, json);
                return true;
            }
            catch (Exception ex) { error = ex.Message; return false; }
        }

        /// <summary>Read all text from filePath. Returns null on failure.</summary>
        public string ReadJson(string filePath)
        {
            try { return File.Exists(filePath) ? File.ReadAllText(filePath) : null; }
            catch { return null; }
        }

        // ── Save-file discovery ──────────────────────────────────────────────────────

        /// <summary>True when at least one .json file exists in saveDir.</summary>
        public static bool HasAnySave(string saveDir)
        {
            if (!Directory.Exists(saveDir)) return false;
            string[] files = Directory.GetFiles(saveDir, "*.json");
            return files != null && files.Length > 0;
        }

        /// <summary>Delete save file at filePath plus .meta sidecar if present. No-op if absent.</summary>
        public static void DeleteSave(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            if (File.Exists(filePath)) File.Delete(filePath);
            string meta = filePath + ".meta";
            if (File.Exists(meta)) File.Delete(meta);
        }

        /// <summary>Returns .json file paths in saveDir sorted newest-first by file mtime.</summary>
        public static string[] GetSortedFilePaths(string saveDir)
        {
            if (!Directory.Exists(saveDir)) return Array.Empty<string>();
            string[] files = Directory.GetFiles(saveDir, "*.json");
            if (files == null || files.Length == 0) return Array.Empty<string>();
            List<(string path, DateTime mtime)> pairs = new List<(string, DateTime)>();
            foreach (string p in files) pairs.Add((p, File.GetLastWriteTimeUtc(p)));
            pairs.Sort((a, b) => b.mtime.CompareTo(a.mtime));
            string[] result = new string[pairs.Count];
            for (int i = 0; i < pairs.Count; i++) result[i] = pairs[i].path;
            return result;
        }
    }
}
