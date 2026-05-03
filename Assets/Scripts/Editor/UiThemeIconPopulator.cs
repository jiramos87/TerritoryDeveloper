using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Territory.UI;

namespace Territory.UI.EditorTools
{
    /// <summary>
    /// Stage 13.3 T2 — populate <see cref="UiTheme.IconEntries"/> from
    /// <c>Assets/Sprites/Icons/*.png</c>. Canonical 27-slug catalog lives
    /// here (mirrors <c>tools/scripts/icons-svg-split.ts:CANONICAL_ICON_SLUGS</c>);
    /// missing PNG → entry written with null sprite so runtime fallback path
    /// resolves via <see cref="UiTheme.TryGetIcon"/> → <c>icon-info</c>.
    /// </summary>
    public static class UiThemeIconPopulator
    {
        /// <summary>
        /// Canonical 27-slug catalog. Mirrors <c>icons-svg-split.ts:CANONICAL_ICON_SLUGS</c>
        /// 1:1. Source SVG drives PNG availability; missing slugs map to null
        /// sprite (ThemedIcon falls back to icon-info at runtime).
        /// </summary>
        public static readonly string[] CanonicalIconSlugs =
        {
            // 22 currently shipped in cd-bundle/icons.svg
            "icon-select",
            "icon-road",
            "icon-zone-residential",
            "icon-zone-commercial",
            "icon-zone-industrial",
            "icon-bulldoze",
            "icon-power",
            "icon-water",
            "icon-services",
            "icon-landmark",
            "icon-desirability",
            "icon-pollution",
            "icon-land-value",
            "icon-heat",
            "icon-pause",
            "icon-play",
            "icon-fast-forward",
            "icon-step",
            "icon-alert",
            "icon-info",
            "icon-success",
            "icon-autosave",
            // 5 reserved-slug hooks awaiting designer SVG handoff
            "icon-happiness",
            "icon-population",
            "icon-money",
            "icon-bond",
            "icon-envelope",
        };

        private const string IconSpriteFolder = "Assets/Sprites/Icons";

        [MenuItem("Tools/Territory/UI/Populate UiTheme Icons")]
        public static void PopulateMenu()
        {
            var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(GuessThemePath());
            if (theme == null)
            {
                Debug.LogError(
                    $"[UiThemeIconPopulator] No UiTheme.asset selected. Select a UiTheme asset in Project pane and re-run, or assign default path.");
                return;
            }
            Populate(theme);
        }

        /// <summary>
        /// Populate <paramref name="theme"/> with the canonical 27-slug catalog.
        /// Each slug binds to <c>{IconSpriteFolder}/{slug}.png</c> when present;
        /// missing PNG → null sprite (runtime fallback path).
        /// </summary>
        public static void Populate(UiTheme theme)
        {
            if (theme == null) return;

            var entries = new List<UiTheme.IconKv>(CanonicalIconSlugs.Length);
            var resolved = 0;
            var missing = new List<string>();

            foreach (var slug in CanonicalIconSlugs)
            {
                var pngPath = $"{IconSpriteFolder}/{slug}.png";
                Sprite sprite = null;
                if (File.Exists(pngPath))
                {
                    sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
                }
                if (sprite != null) resolved++;
                else missing.Add(slug);

                entries.Add(new UiTheme.IconKv { slug = slug, sprite = sprite });
            }

            theme.IconEntries.Clear();
            theme.IconEntries.AddRange(entries);
            theme.InvalidateTokenCaches();
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();

            Debug.Log(
                $"[UiThemeIconPopulator] populated {resolved}/{CanonicalIconSlugs.Length} icons on '{theme.name}'. " +
                (missing.Count > 0
                    ? $"Missing (reserved-slug hooks): {string.Join(", ", missing)}"
                    : "All 27 slugs resolved."));
        }

        private static string GuessThemePath()
        {
            // Prefer current selection; otherwise fall back to first UiTheme asset
            // in the project. Inspector-first wiring: agent / human picks the
            // SO before invoking the menu.
            if (Selection.activeObject is UiTheme)
            {
                return AssetDatabase.GetAssetPath(Selection.activeObject);
            }
            var guids = AssetDatabase.FindAssets("t:UiTheme");
            if (guids != null && guids.Length > 0)
            {
                return AssetDatabase.GUIDToAssetPath(guids[0]);
            }
            return null;
        }
    }
}
