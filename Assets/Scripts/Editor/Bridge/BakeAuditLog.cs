using System;
using System.Diagnostics;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Domains.UI.Editor.UiBake.Services
{
    /// <summary>Append-only bake audit log writer. Extracted from UiBakeHandler (TECH-31986).
    /// Delegates to bake-audit-write.mjs via node subprocess.</summary>
    public static class BakeAuditLog
    {
        /// <summary>Version tag for ia_ui_bake_history.bake_handler_version.</summary>
        public const string BakeHandlerVersion = "1.0";

        [Serializable]
        class AuditPayload
        {
            public string panel_slug;
            public string bake_handler_version;
            public DiffSummary diff_summary;
            public string commit_sha;
        }

        [Serializable]
        class DiffSummary
        {
            public string status = "written";
        }

        /// <summary>Best-effort: insert one row into ia_ui_bake_history via bake-audit-write.mjs.
        /// Failure logs a warning but does NOT abort the bake.</summary>
        public static void WriteRow(string repoRoot, string panelSlug)
        {
            if (string.IsNullOrEmpty(repoRoot) || string.IsNullOrEmpty(panelSlug)) return;

            string script = System.IO.Path.Combine(repoRoot, "tools", "postgres-ia", "bake-audit-write.mjs");
            if (!System.IO.File.Exists(script))
            {
                UnityEngine.Debug.LogWarning($"[UiBakeHandler] bake-audit-write.mjs not found at {script} — skipping audit row.");
                return;
            }

            string commitSha = ResolveGitCommitSha(repoRoot);
            var payloadObj = new AuditPayload
            {
                panel_slug           = panelSlug,
                bake_handler_version = BakeHandlerVersion,
                diff_summary         = new DiffSummary(),
                commit_sha           = commitSha ?? string.Empty,
            };
            string payloadJson = JsonUtility.ToJson(payloadObj);

            string tmpFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"bake-audit-{Guid.NewGuid():N}.json");
            try
            {
                System.IO.File.WriteAllText(tmpFile, payloadJson, new UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[UiBakeHandler] Failed to write audit payload temp file: {ex.Message}");
                return;
            }

            try
            {
                string nodeExe = Territory.Editor.Bridge.EditorPostgresExportRegistrar.ResolveNodeExecutablePath();
                string dbUrl   = Territory.Editor.Bridge.EditorPostgresExportRegistrar.ResolveEffectiveDatabaseUrl(repoRoot);
                if (string.IsNullOrWhiteSpace(dbUrl) || string.IsNullOrWhiteSpace(nodeExe)) return;

                var psi = new ProcessStartInfo
                {
                    FileName               = nodeExe,
                    Arguments              = $"\"{script}\" --payload-file \"{tmpFile}\"",
                    WorkingDirectory       = repoRoot,
                    UseShellExecute        = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    CreateNoWindow         = true,
                };
                psi.EnvironmentVariables["DATABASE_URL"] = dbUrl;
                psi.EnvironmentVariables["NODE_NO_WARNINGS"] = "1";

                using var proc = new Process { StartInfo = psi };
                proc.Start();
                bool finished = proc.WaitForExit(15_000);
                if (!finished)
                {
                    proc.Kill();
                    UnityEngine.Debug.LogWarning("[UiBakeHandler] bake-audit-write.mjs timed out — audit row skipped.");
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[UiBakeHandler] bake audit write failed: {ex.Message}");
            }
            finally
            {
                try { System.IO.File.Delete(tmpFile); } catch { /* ignored */ }
            }
        }

        static string ResolveGitCommitSha(string repoRoot)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName               = "git",
                    Arguments              = "rev-parse --short HEAD",
                    WorkingDirectory       = repoRoot,
                    UseShellExecute        = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    CreateNoWindow         = true,
                };
                using var proc = new Process { StartInfo = psi };
                proc.Start();
                string sha = proc.StandardOutput.ReadToEnd().Trim();
                proc.WaitForExit(5_000);
                return sha;
            }
            catch { return string.Empty; }
        }
    }
}
