using System;
using System.Collections.Generic;
using UnityEngine;

namespace Territory.UI
{
    /// <summary>
    /// Button/toggle catalog entry — display_name and catalog slug from catalog_entity rows.
    /// Consumed by HudBarDataAdapter to resolve AUTO toggle caption from catalog instead of
    /// hardcoded string (TECH-19975 / Stage 9.13 AUTO toggle catalog wiring).
    /// </summary>
    [Serializable]
    public class UiButtonCatalogEntry
    {
        public string slug;
        public string displayName;
    }

    /// <summary>
    /// Panel shape DTO read from asset-registry panel row (subtype-picker).
    /// Fields mirror catalog_entity + panel_detail seed values (0080 migration).
    /// </summary>
    [Serializable]
    public class UiPanelDef
    {
        public string slug;
        public Vector2 sizeDelta;
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
        /// <summary>Padding: x=left, y=right, z=top, w=bottom (maps to RectOffset).</summary>
        public Vector4 padding;
        public float spacing;
    }

    /// <summary>
    /// Archetype DTO read from asset-registry archetype row (picker-tile-72).
    /// Fields mirror entity_version.params_json seed values (0080 migration).
    /// </summary>
    [Serializable]
    public class UiArchetypeDef
    {
        public string slug;
        public float tileWidth;
        public float tileHeight;
        /// <summary>Icon offsetMin (x=left, y=bottom) in local tile space.</summary>
        public Vector2 iconOffsetMin;
        /// <summary>Icon offsetMax (x=right, y=top — negative insets) in local tile space.</summary>
        public Vector2 iconOffsetMax;
        public float captionHeight;
        /// <summary>motion.hover enum value: tint | glow | scale.</summary>
        public string motionHover;

        // Stage 9.9 — slider-row archetype geometry (slider_row_2). Picker tiles ignore these.
        /// <summary>Row height for slider-row archetypes (slider_row_2).</summary>
        public float rowHeight;
        /// <summary>Left label column width for slider-row archetypes.</summary>
        public float labelWidth;
        /// <summary>Right value column width for slider-row archetypes.</summary>
        public float valueWidth;
    }

    /// <summary>
    /// Runtime UI asset catalog (TECH-15891 / Stage 9.7).
    /// <para>
    /// Hosts panel + archetype definitions sourced from asset-registry DB rows.
    /// Values match 0080 seed migration. MonoBehaviour scene host under Game Managers;
    /// Inspector <c>[SerializeField]</c> assignment on consumers; <c>FindObjectOfType</c>
    /// fallback per invariant #4 when slot empty.
    /// </para>
    /// <para>
    /// Sprite lookup routes to <see cref="Resources.Load{T}"/> under
    /// <c>UI/Sprites/Picker/</c> — same path stem as sprite_catalog rows.
    /// </para>
    /// </summary>
    public class UiAssetCatalog : MonoBehaviour
    {
        [Header("Panel definitions (asset-registry)")]
        [SerializeField] private UiPanelDef[] _panels = DefaultPanels();

        [Header("Archetype definitions (asset-registry)")]
        [SerializeField] private UiArchetypeDef[] _archetypes = DefaultArchetypes();

        [Header("Button catalog entries (Stage 9.13 — hud-bar controls)")]
        [SerializeField] private UiButtonCatalogEntry[] _buttons = DefaultButtons();

        private Dictionary<string, UiPanelDef> _panelIndex;
        private Dictionary<string, UiArchetypeDef> _archetypeIndex;
        private Dictionary<string, UiButtonCatalogEntry> _buttonIndex;
        private bool _indexed;

        private void Awake() => EnsureIndex();

        private void EnsureIndex()
        {
            if (_indexed) return;
            _indexed = true;
            _panelIndex = new Dictionary<string, UiPanelDef>(StringComparer.Ordinal);
            _archetypeIndex = new Dictionary<string, UiArchetypeDef>(StringComparer.Ordinal);
            _buttonIndex = new Dictionary<string, UiButtonCatalogEntry>(StringComparer.Ordinal);
            if (_panels != null)
                foreach (var p in _panels)
                    if (p != null && !string.IsNullOrEmpty(p.slug))
                        _panelIndex[p.slug] = p;
            if (_archetypes != null)
                foreach (var a in _archetypes)
                    if (a != null && !string.IsNullOrEmpty(a.slug))
                        _archetypeIndex[a.slug] = a;
            if (_buttons != null)
                foreach (var b in _buttons)
                    if (b != null && !string.IsNullOrEmpty(b.slug))
                        _buttonIndex[b.slug] = b;
        }

        /// <summary>Resolve panel def by slug. Returns false when not found (no fallback).</summary>
        public bool TryGetPanel(string slug, out UiPanelDef def)
        {
            EnsureIndex();
            return _panelIndex.TryGetValue(slug ?? string.Empty, out def);
        }

        /// <summary>Resolve archetype def by slug. Returns false when not found (no fallback).</summary>
        public bool TryGetArchetype(string slug, out UiArchetypeDef def)
        {
            EnsureIndex();
            return _archetypeIndex.TryGetValue(slug ?? string.Empty, out def);
        }

        /// <summary>
        /// Resolve a sprite by sprite-catalog slug. Loads from Resources path
        /// <c>UI/Sprites/Picker/{slug}</c>. Returns false when resource absent.
        /// </summary>
        public bool TryGetSprite(string slug, out Sprite sprite)
        {
            sprite = null;
            if (string.IsNullOrEmpty(slug)) return false;
            // Resources.Load path: strip Assets/Resources/ prefix, no extension.
            sprite = Resources.Load<Sprite>($"UI/Sprites/Picker/{slug}");
            return sprite != null;
        }

        /// <summary>
        /// Read motion.hover enum for a given archetype slug.
        /// Consumed by tile-build hover branch (TECH-15892).
        /// </summary>
        public bool TryGetMotionHover(string archetypeSlug, out string hoverEnum)
        {
            EnsureIndex();
            hoverEnum = null;
            if (!_archetypeIndex.TryGetValue(archetypeSlug ?? string.Empty, out var def)) return false;
            hoverEnum = def.motionHover;
            return !string.IsNullOrEmpty(hoverEnum);
        }

        /// <summary>
        /// Resolve button catalog entry by catalog slug.
        /// Used by <see cref="Territory.UI.HUD.HudBarDataAdapter"/> to get
        /// AUTO toggle display_name from catalog (TECH-19975 Stage 9.13).
        /// Returns false when slug not registered.
        /// </summary>
        public bool TryGetButtonEntry(string slug, out UiButtonCatalogEntry entry)
        {
            EnsureIndex();
            return _buttonIndex.TryGetValue(slug ?? string.Empty, out entry);
        }

        // ── Defaults match seed migrations ──

        /// <summary>
        /// Default button catalog entries for hud-bar controls (Stage 9.13 / TECH-19975).
        /// Slug values match catalog_entity rows inserted by migration 0098.
        /// </summary>
        private static UiButtonCatalogEntry[] DefaultButtons() => new[]
        {
            new UiButtonCatalogEntry { slug = "hud-bar-build-residential-button", displayName = "Build Residential" },
            new UiButtonCatalogEntry { slug = "hud-bar-build-commercial-button",  displayName = "Build Commercial" },
            new UiButtonCatalogEntry { slug = "hud-bar-build-industrial-button",  displayName = "Build Industrial" },
            new UiButtonCatalogEntry { slug = "hud-bar-auto-button",              displayName = "AUTO" },
            new UiButtonCatalogEntry { slug = "hud-bar-budget-plus-button",       displayName = "Budget +" },
            new UiButtonCatalogEntry { slug = "hud-bar-budget-minus-button",      displayName = "Budget -" },
            new UiButtonCatalogEntry { slug = "hud-bar-budget-graph-button",      displayName = "Budget Graph" },
            new UiButtonCatalogEntry { slug = "hud-bar-map-button",               displayName = "Map" },
            new UiButtonCatalogEntry { slug = "hud-bar-pause-button",             displayName = "Pause" },
            new UiButtonCatalogEntry { slug = "hud-bar-play-button",              displayName = "Play" },
            new UiButtonCatalogEntry { slug = "hud-bar-speed-1-button",           displayName = "Speed 1" },
            new UiButtonCatalogEntry { slug = "hud-bar-speed-2-button",           displayName = "Speed 2" },
            new UiButtonCatalogEntry { slug = "hud-bar-speed-3-button",           displayName = "Speed 3" },
            new UiButtonCatalogEntry { slug = "hud-bar-speed-4-button",           displayName = "Speed 4" },
        };

        private static UiPanelDef[] DefaultPanels() => new[]
        {
            new UiPanelDef
            {
                slug      = "subtype_picker",
                // Bottom-left dock — sits between bottom of toolbar and bottom of screen.
                // Width grows via ContentSizeFitter; height fixed to fit larger icon tiles.
                sizeDelta = new Vector2(0f, 160f),
                anchorMin = new Vector2(0f, 0f),
                anchorMax = new Vector2(0f, 0f),
                pivot     = new Vector2(0f, 0f),
                padding   = new Vector4(12f, 12f, 12f, 12f), // left, right, top, bottom
                spacing   = 10f,
            },
            // Stage 9.9 (TECH-16992) — growth-budget panel. Top-right anchor near hud-bar BUDGET button.
            new UiPanelDef
            {
                slug      = "growth_budget_panel",
                sizeDelta = new Vector2(360f, 220f),
                anchorMin = new Vector2(1f, 1f),
                anchorMax = new Vector2(1f, 1f),
                pivot     = new Vector2(1f, 1f),
                padding   = new Vector4(12f, 12f, 12f, 12f),
                spacing   = 6f,
            }
        };

        private static UiArchetypeDef[] DefaultArchetypes() => new[]
        {
            new UiArchetypeDef
            {
                slug          = "picker_tile_72",
                // Tile bumped from 72 → 120 px square so icon sprites carry placement info
                // at glance distance. Caption stays at 14 px (small but legible).
                tileWidth     = 120f,
                tileHeight    = 120f,
                iconOffsetMin = new Vector2(8f, 24f),
                iconOffsetMax = new Vector2(-8f, -8f),
                captionHeight = 16f,
                motionHover   = "tint",
            },
            // Stage 9.9 (TECH-16992) — slider-row archetype for growth-budget panel.
            new UiArchetypeDef
            {
                slug        = "slider_row_2",
                rowHeight   = 40f,
                labelWidth  = 110f,
                valueWidth  = 48f,
                motionHover = "tint",
            }
        };
    }
}
