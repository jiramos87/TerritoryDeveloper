using System.IO;
using UnityEditor;
using UnityEngine;

namespace Territory.Editor
{
    /// <summary>
    /// One-time backfill sweep — walks Assets/UI/Sprites/**/*.png and seeds sprite_catalog rows.
    /// Idempotent: uses ON CONFLICT (path) DO NOTHING in sprite-catalog-upsert.mjs.
    /// Logs inserted + skipped counts on completion.
    ///
    /// TECH-15232 — Stage 9.6 game-ui-catalog-bake (DEC-A25 sprite-catalog tier).
    /// </summary>
    public static class SpriteCatalogBackfill
    {
        private const string SpritesRoot = "Assets/UI/Sprites";
        private const string MenuPath = "Tools/Asset Pipeline/Backfill Sprite Catalog";

        [MenuItem(MenuPath)]
        public static void BackfillFromMenu()
        {
            var repoRoot = ResolveRepoRoot();
            if (string.IsNullOrEmpty(repoRoot))
            {
                Debug.LogError("[SpriteCatalogBackfill] Could not resolve repo root.");
                EditorUtility.DisplayDialog("Backfill Failed", "Could not resolve repo root.", "OK");
                return;
            }

            var scriptPath = Path.Combine(repoRoot, "tools", "postgres-ia", "sprite-catalog-upsert.mjs");
            if (!File.Exists(scriptPath))
            {
                Debug.LogError($"[SpriteCatalogBackfill] upsert script not found: {scriptPath}");
                EditorUtility.DisplayDialog("Backfill Failed", $"Script not found:\n{scriptPath}", "OK");
                return;
            }

            var spritesAbsPath = Path.Combine(Application.dataPath, "..", SpritesRoot);
            if (!Directory.Exists(spritesAbsPath))
            {
                Debug.LogWarning($"[SpriteCatalogBackfill] Sprites directory not found: {spritesAbsPath}");
                EditorUtility.DisplayDialog("Backfill", "No sprites directory found — nothing to backfill.", "OK");
                return;
            }

            var pngFiles = Directory.GetFiles(spritesAbsPath, "*.png", SearchOption.AllDirectories);
            var nodeExe = EditorPostgresExportRegistrar.ResolveNodeExecutablePath();
            var dbUrl = EditorPostgresExportRegistrar.ResolveEffectiveDatabaseUrl(repoRoot);

            int inserted = 0;
            int skipped = 0;
            int errors = 0;

            for (int i = 0; i < pngFiles.Length; i++)
            {
                var absFile = pngFiles[i];
                // Convert absolute FS path → repo-relative asset path (Assets/UI/Sprites/...).
                var assetPath = "Assets" + absFile
                    .Replace(Path.Combine(Application.dataPath, "..").Replace('\\', '/'), "")
                    .Replace(Application.dataPath.Replace('\\', '/'), "/Assets")
                    .Replace('\\', '/');

                // Normalize: ensure Assets/ prefix.
                if (!assetPath.StartsWith("Assets/"))
                {
                    // Fallback: compute relative to repoRoot.
                    var relToRepo = absFile.Replace(repoRoot, "").TrimStart('/', '\\').Replace('\\', '/');
                    assetPath = relToRepo;
                }

                var slug = Path.GetFileNameWithoutExtension(absFile);
                EditorUtility.DisplayProgressBar("Backfill Sprite Catalog", $"{slug} ({i + 1}/{pngFiles.Length})", (float)i / pngFiles.Length);

                var result = RunUpsert(nodeExe, scriptPath, slug, assetPath, dbUrl, repoRoot);
                if (result == UpsertResult.Inserted) inserted++;
                else if (result == UpsertResult.AlreadyPresent) skipped++;
                else errors++;
            }

            EditorUtility.ClearProgressBar();

            var summary = $"Backfill complete.\nInserted: {inserted} | Already present: {skipped} | Errors: {errors}";
            Debug.Log($"[SpriteCatalogBackfill] {summary}");
            EditorUtility.DisplayDialog("Backfill Sprite Catalog", summary, "OK");
        }

        private enum UpsertResult { Inserted, AlreadyPresent, Error }

        private static UpsertResult RunUpsert(string nodeExe, string scriptPath, string slug, string assetPath, string dbUrl, string repoRoot)
        {
            var args = $"\"{scriptPath}\" --slug \"{slug}\" --path \"{assetPath}\"";

            var psi = new System.Diagnostics.ProcessStartInfo(nodeExe, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            if (!string.IsNullOrEmpty(dbUrl))
                psi.EnvironmentVariables["DATABASE_URL"] = dbUrl;

            psi.EnvironmentVariables["IA_REPO_ROOT"] = repoRoot;

            try
            {
                using var proc = System.Diagnostics.Process.Start(psi);
                var stdout = proc.StandardOutput.ReadToEnd().Trim();
                proc.StandardError.ReadToEnd(); // drain stderr
                proc.WaitForExit(10_000);

                if (proc.ExitCode != 0) return UpsertResult.Error;
                return stdout.Contains("already-present") ? UpsertResult.AlreadyPresent : UpsertResult.Inserted;
            }
            catch
            {
                return UpsertResult.Error;
            }
        }

        private static string ResolveRepoRoot()
        {
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
