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
                if (item == null) continue;
                var effectivePanel = item.EffectivePanelRow();
                if (effectivePanel == null) continue;
                var path = DispatchOne(effectivePanel, item.children ?? Array.Empty<PanelChildRow>(), outDir);
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
                case "vstack":
                    return BakeVstack(panel, children, outDir);
                case "grid":
                    return BakeGrid(panel, children, outDir);
                case "free":
                    return BakeFree(panel, children, outDir);
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

        /// <summary>
        /// panels.json item row. Stage 9.10 (schema_version 3) adds top-level `slug` + `fields`
        /// alongside legacy `panel` block. JsonUtility reads both; code prefers `fields` when slug
        /// is set at item level (new shape), falls back to `panel.slug` for schema_version 1/2.
        /// </summary>
        [Serializable]
        public class PanelItem
        {
            /// <summary>Stage 9.10 — top-level slug (new shape). Empty in v1/v2 snapshots.</summary>
            public string slug;
            /// <summary>Stage 9.10 — panel fields block (new shape). Null in v1/v2 snapshots.</summary>
            public PanelFields fields;
            /// <summary>Legacy panel block (schema_version 1/2).</summary>
            public PanelRow panel;
            public PanelChildRow[] children;

            /// <summary>Resolve the effective panel row for dispatch. Prefers new shape when slug set.</summary>
            public PanelRow EffectivePanelRow()
            {
                if (!string.IsNullOrEmpty(slug) && fields != null)
                {
                    return new PanelRow
                    {
                        slug = slug,
                        layout = fields.layout_template ?? fields.layout ?? "vstack",
                        gap_px = fields.gap_px,
                        padding_json = fields.padding_json,
                        params_json = fields.params_json,
                        layout_template = fields.layout_template ?? fields.layout ?? "vstack",
                    };
                }
                return panel;
            }
        }

        /// <summary>Stage 9.10 — fields block in panels.json items[] (schema_version 3).</summary>
        [Serializable]
        public class PanelFields
        {
            public string layout_template;
            public string layout;
            public int gap_px;
            public string padding_json;
            public string params_json;
        }

        [Serializable]
        public class PanelRow
        {
            public string slug;
            public string layout;
            /// <summary>Stage 9.10 — canonical layout primitive key from panel_detail.layout_template.</summary>
            public string layout_template;
            public int gap_px;
            public string padding_json;
            public string params_json;
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
            /// <summary>Stage 9.10 — per-child layout routing metadata JSON string, e.g. {"zone":"left"}. Null on non-zoned children.</summary>
            public string layout_json;
        }
    }
}
