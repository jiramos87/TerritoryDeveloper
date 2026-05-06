using System;
using System.Collections.Generic;
using UnityEngine;

namespace Territory.UI
{
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

        private Dictionary<string, UiPanelDef> _panelIndex;
        private Dictionary<string, UiArchetypeDef> _archetypeIndex;
        private bool _indexed;

        private void Awake() => EnsureIndex();

        private void EnsureIndex()
        {
            if (_indexed) return;
            _indexed = true;
            _panelIndex = new Dictionary<string, UiPanelDef>(StringComparer.Ordinal);
            _archetypeIndex = new Dictionary<string, UiArchetypeDef>(StringComparer.Ordinal);
            if (_panels != null)
                foreach (var p in _panels)
                    if (p != null && !string.IsNullOrEmpty(p.slug))
                        _panelIndex[p.slug] = p;
            if (_archetypes != null)
                foreach (var a in _archetypes)
                    if (a != null && !string.IsNullOrEmpty(a.slug))
                        _archetypeIndex[a.slug] = a;
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

        // ── Defaults match 0080 seed migration ──

        private static UiPanelDef[] DefaultPanels() => new[]
        {
            new UiPanelDef
            {
                slug      = "subtype_picker",
                sizeDelta = new Vector2(0f, 88f),
                anchorMin = new Vector2(0.5f, 0f),
                anchorMax = new Vector2(0.5f, 0f),
                pivot     = new Vector2(0.5f, 0f),
                padding   = new Vector4(10f, 10f, 8f, 8f), // left, right, top, bottom
                spacing   = 8f,
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
                tileWidth     = 72f,
                tileHeight    = 72f,
                iconOffsetMin = new Vector2(6f, 18f),
                iconOffsetMax = new Vector2(-6f, -6f),
                captionHeight = 12f,
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
