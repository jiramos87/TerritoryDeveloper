// TECH-11926 / game-ui-catalog-bake Stage 1.0 — CatalogBakeHandler dispatcher.
//
// Reads `Assets/UI/Snapshots/panels.json` (per-kind snapshot per
// `ia/specs/catalog-architecture.md §5.2`) and dispatches each panel to the
// matching layout-primitive partial (`BakeHstack`, future `BakeVstack`/etc.)
// to emit a Unity prefab under `Assets/UI/Prefabs/Generated/{slug}.prefab`.
//
// Editor-only — file lives under `Assets/Editor/Bake/` (Unity implicit
// Editor assembly). No MonoBehaviour, no scene state mutation, no
// GridManager / HeightMap touch.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TerritoryDeveloper.Editor.Bake
{
    /// <summary>
    /// Static dispatcher that bakes published catalog panels into Unity prefabs.
    /// Invoked from EditMode tests and (future) menu items. Does NOT auto-bake on
    /// domain reload — `[InitializeOnLoad]` only ensures the type is loaded.
    /// </summary>
    [InitializeOnLoad]
    public static partial class CatalogBakeHandler
    {
        /// <summary>Default output directory for baked prefabs (gitignored).</summary>
        public const string DefaultOutDir = "Assets/UI/Prefabs/Generated";

        static CatalogBakeHandler()
        {
            // Intentional no-op: keep the type alive on domain reload so partial
            // class methods (BakeHstack) are reachable from tests and menu items.
        }

        /// <summary>
        /// Editor menu entry — bake every panel from the canonical snapshot path
        /// (`Assets/UI/Snapshots/panels.json`) to the canonical output dir
        /// (`Assets/UI/Prefabs/Generated/`). Tracer-stage convenience for
        /// agents wiring scenes via `execute_menu_item` bridge kind.
        /// </summary>
        [MenuItem("Tools/Bake/Catalog Bake From Snapshot")]
        public static void BakeFromCanonicalSnapshot()
        {
            const string snapshotPath = "Assets/UI/Snapshots/panels.json";
            BakeFromSnapshot(snapshotPath, DefaultOutDir);
        }

        /// <summary>
        /// Read `snapshotPath`, dispatch each `PanelItem` per its `layout` primitive,
        /// and write resulting prefabs under `outDir`. Idempotent — overwrites
        /// existing prefabs at the same asset path.
        /// </summary>
        /// <param name="snapshotPath">Repo-relative path to panels.json (per-kind snapshot).</param>
        /// <param name="outDir">Repo-relative output directory (created if missing).</param>
        /// <returns>List of asset paths written (one per panel).</returns>
        public static IReadOnlyList<string> BakeFromSnapshot(string snapshotPath, string outDir)
        {
            if (string.IsNullOrEmpty(snapshotPath))
                throw new ArgumentException("snapshotPath required", nameof(snapshotPath));
            if (string.IsNullOrEmpty(outDir))
                outDir = DefaultOutDir;

            if (!File.Exists(snapshotPath))
                throw new FileNotFoundException($"snapshot not found at {snapshotPath}", snapshotPath);

            var snapshot = LoadSnapshot(snapshotPath);
            if (snapshot == null || snapshot.items == null || snapshot.items.Length == 0)
                return Array.Empty<string>();

            EnsureDirExists(outDir);

            var written = new List<string>(snapshot.items.Length);
            foreach (var item in snapshot.items)
            {
                if (item == null || item.panel == null) continue;
                var path = DispatchOne(item.panel, item.children ?? Array.Empty<PanelChildRow>(), outDir);
                if (!string.IsNullOrEmpty(path)) written.Add(path);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return written;
        }

        static string DispatchOne(PanelRow panel, PanelChildRow[] children, string outDir)
        {
            switch (panel.layout)
            {
                case "hstack":
                    return BakeHstack(panel, children, outDir);
                case "modal":
                    return BakeModal(panel, children, outDir);
                default:
                    throw new NotSupportedException($"layout primitive not supported: {panel.layout}");
            }
        }

        static PanelsSnapshot LoadSnapshot(string snapshotPath)
        {
            var json = File.ReadAllText(snapshotPath);
            return JsonUtility.FromJson<PanelsSnapshot>(json);
        }

        static void EnsureDirExists(string outDir)
        {
            if (Directory.Exists(outDir)) return;
            Directory.CreateDirectory(outDir);
            AssetDatabase.Refresh();
        }

        // ─── DTOs (JsonUtility requires [Serializable] + public fields) ────────

        [Serializable]
        public class PanelsSnapshot
        {
            public string snapshot_id;
            public string kind;
            public int schema_version;
            public PanelItem[] items;
        }

        [Serializable]
        public class PanelItem
        {
            public PanelRow panel;
            public PanelChildRow[] children;
        }

        [Serializable]
        public class PanelRow
        {
            public string slug;
            public string layout;
            public int gap_px;
            public string padding_json;
        }

        [Serializable]
        public class PanelChildRow
        {
            public int ord;
            public string kind;
            public string params_json;
            public string sprite_ref;
            // Stage 2 (TECH-11930): button state sprite refs resolved from button_detail join.
            public string hover_sprite_ref;
            public string pressed_sprite_ref;
            public string disabled_sprite_ref;
        }
    }
}
