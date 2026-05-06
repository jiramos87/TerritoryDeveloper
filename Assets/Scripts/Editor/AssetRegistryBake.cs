using System.IO;
using Territory.Editor.Bridge;
using UnityEditor;
using UnityEngine;

namespace Territory.Editor
{
    /// <summary>
    /// Asset-registry bake entry point — Stage 9.2 game-ui-catalog-bake.
    /// Reads IR JSON seeded from asset-registry DB rows and delegates to UiBakeHandler.
    /// DEC-A25: DB-wins authority; bake output under Assets/UI/Prefabs/Generated is derived.
    ///
    /// §Playbook recipe: seed DB row → validate:asset-pipeline → bake → spot-check runtime.
    /// Tracer scope: demo-panel slug only; throwaway after Stage 9.3 retrofit.
    /// </summary>
    public static class AssetRegistryBake
    {
        private const string DefaultIrDir = "Assets/UI/IR";
        private const string DefaultOutDir = "Assets/UI/Prefabs/Generated";
        private const string DefaultThemeSo = "Assets/UI/Theme/DefaultUiTheme.asset";

        // ── Menu item (manual trigger) ───────────────────────────────────────

        [MenuItem("Territory/Asset Registry/Bake All (IR → Prefabs)")]
        public static void BakeAllFromMenu()
        {
            var result = BakeAll(DefaultIrDir, DefaultOutDir, DefaultThemeSo);
            if (result.error != null)
            {
                Debug.LogError($"[AssetRegistryBake] Bake failed: {result.error.error} — {result.error.details} @ {result.error.path}");
                EditorUtility.DisplayDialog("Bake Failed", $"{result.error.error}\n{result.error.details}", "OK");
            }
            else
            {
                Debug.Log($"[AssetRegistryBake] Bake complete. IR dir: {DefaultIrDir}");
                EditorUtility.DisplayDialog("Bake Complete", $"Asset registry bake succeeded.\nIR dir: {DefaultIrDir}", "OK");
            }
        }

        [MenuItem("Territory/Asset Registry/Bake Demo Panel (Tracer)")]
        public static void BakeDemoPanelFromMenu()
        {
            var irPath = $"{DefaultIrDir}/demo-panel.json";
            var result = BakeSingleIr(irPath, DefaultOutDir, DefaultThemeSo);
            if (result.error != null)
                Debug.LogError($"[AssetRegistryBake] demo-panel bake failed: {result.error.error} — {result.error.details}");
            else
                Debug.Log("[AssetRegistryBake] demo-panel bake complete → Assets/UI/Prefabs/Generated/demo-panel.prefab");
        }

        // ── Bridge integration point ─────────────────────────────────────────

        /// <summary>
        /// Bake one named IR JSON from <paramref name="irDir"/>.
        /// Called by AgentBridgeCommandRunner for kind=bake_ui_from_ir when slug is provided.
        /// Returns structured result; does not throw.
        /// </summary>
        public static UiBakeHandler.BakeResult BakeSingleIr(string irPath, string outDir, string themeSo)
        {
            if (!File.Exists(irPath))
            {
                return new UiBakeHandler.BakeResult
                {
                    root = null,
                    error = new UiBakeHandler.BakeError
                    {
                        error = "ir_not_found",
                        details = $"No IR JSON at: {irPath}",
                        path = irPath,
                    },
                };
            }

            return UiBakeHandler.Bake(new UiBakeHandler.BakeArgs
            {
                ir_path = irPath,
                out_dir = outDir,
                theme_so = themeSo,
            });
        }

        /// <summary>
        /// Bake all IR JSON files in <paramref name="irDir"/>. Returns on first error.
        /// Determinism mandate (DEC-A25): same IR state = identical prefab output every run.
        /// </summary>
        public static UiBakeHandler.BakeResult BakeAll(string irDir, string outDir, string themeSo)
        {
            if (!Directory.Exists(irDir))
            {
                return new UiBakeHandler.BakeResult
                {
                    root = null,
                    error = new UiBakeHandler.BakeError
                    {
                        error = "ir_dir_not_found",
                        details = $"IR directory does not exist: {irDir}",
                        path = irDir,
                    },
                };
            }

            var files = Directory.GetFiles(irDir, "*.json", SearchOption.TopDirectoryOnly);
            if (files.Length == 0)
            {
                Debug.LogWarning($"[AssetRegistryBake] No IR JSON files in {irDir} — nothing to bake.");
                return new UiBakeHandler.BakeResult { root = null, error = null };
            }

            System.Array.Sort(files); // deterministic ordering
            foreach (var f in files)
            {
                var result = BakeSingleIr(f, outDir, themeSo);
                if (result.error != null) return result;
            }

            AssetDatabase.Refresh();
            return new UiBakeHandler.BakeResult { root = null, error = null };
        }
    }
}
