using System.IO;
using UnityEditor;
using UnityEngine;

namespace Territory.Editor
{
    /// <summary>
    /// AssetPostprocessor for .png files under Assets/UI/Sprites/.
    /// Routes to sprite-catalog DB row insert on import (idempotent via ON CONFLICT).
    /// Editor-only — no runtime dependency.
    /// Outside scope (non-UI/Sprites .png) → LogWarning, no insert.
    ///
    /// TECH-15231 — Stage 9.6 game-ui-catalog-bake (DEC-A25 sprite-catalog tier).
    /// </summary>
    public class SpriteCatalogPostprocessor : AssetPostprocessor
    {
        private const string ScopePrefix = "Assets/UI/Sprites/";

        // Suppress the "No OnPreprocessTexture" warning by implementing a no-op overload.
        private void OnPreprocessTexture() { }

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (var assetPath in importedAssets)
            {
                if (!assetPath.StartsWith(ScopePrefix)) continue;
                if (!assetPath.EndsWith(".png")) continue;

                var slug = Path.GetFileNameWithoutExtension(assetPath);
                TryUpsertRow(slug, assetPath);
            }
        }

        private static void TryUpsertRow(string slug, string assetPath)
        {
            var repoRoot = ResolveRepoRoot();
            if (string.IsNullOrEmpty(repoRoot))
            {
                Debug.LogWarning("[SpriteCatalogPostprocessor] Could not resolve repo root — skipping DB upsert.");
                return;
            }

            var scriptPath = Path.Combine(repoRoot, "tools", "postgres-ia", "sprite-catalog-upsert.mjs");
            if (!File.Exists(scriptPath))
            {
                Debug.LogWarning($"[SpriteCatalogPostprocessor] upsert script not found: {scriptPath}");
                return;
            }

            var nodeExe = EditorPostgresExportRegistrar.ResolveNodeExecutablePath();
            var args = $"\"{scriptPath}\" --slug \"{slug}\" --path \"{assetPath}\"";

            var psi = new System.Diagnostics.ProcessStartInfo(nodeExe, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            // Pass DATABASE_URL env if available via EditorPostgresExportRegistrar policy.
            var dbUrl = EditorPostgresExportRegistrar.ResolveEffectiveDatabaseUrl(repoRoot);
            if (!string.IsNullOrEmpty(dbUrl))
                psi.EnvironmentVariables["DATABASE_URL"] = dbUrl;

            psi.EnvironmentVariables["IA_REPO_ROOT"] = repoRoot;

            try
            {
                using var proc = System.Diagnostics.Process.Start(psi);
                var stdout = proc.StandardOutput.ReadToEnd();
                var stderr = proc.StandardError.ReadToEnd();
                proc.WaitForExit(10_000);

                if (!string.IsNullOrWhiteSpace(stdout))
                    Debug.Log($"[SpriteCatalogPostprocessor] {stdout.Trim()}");
                if (!string.IsNullOrWhiteSpace(stderr))
                    Debug.LogWarning($"[SpriteCatalogPostprocessor] stderr: {stderr.Trim()}");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[SpriteCatalogPostprocessor] upsert spawn failed: {ex.Message}");
            }
        }

        private static string ResolveRepoRoot()
        {
            // Walk up from Application.dataPath (Assets/) to find the repo root (contains .git).
            var dir = new DirectoryInfo(Application.dataPath).Parent;
            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, ".git")))
                    return dir.FullName;
                dir = dir.Parent;
            }
            return null;
        }
    }
}
