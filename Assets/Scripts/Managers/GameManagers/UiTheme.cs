// long-file-allowed: legacy hub — scope outside current atomization plan; deferred to future sweep
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Territory.UI
{
    /// <summary>
    /// Serializable color + typography tokens for uGUI menus + HUD. Assign single asset instance from
    /// scenes/coordinators; extend fields as panels migrate off Inspector literals.
    /// </summary>
    /// <remarks>
    /// Stage 2 (T2.1) extension: dictionary-shaped token caches (palette / frame_style / font_face /
    /// motion_curve / illumination) keyed by slug. Field names mirror the historical sketchpad
    /// token shape so JsonUtility round-trips stay deterministic.
    /// Legacy flat-Color fields preserved for transition; consumers that already cache this SO in
    /// <c>Awake</c>/<c>Start</c> stay valid. Do NOT call <c>FindObjectOfType&lt;UiTheme&gt;</c> per frame
    /// (invariant #3) — cache the reference once.
    /// TECH-32928 Stage 6.0 — Quarantined. Runtime panels must consume USS var(--ds-*) tokens only.
    /// </remarks>
    [Obsolete("UiTheme quarantined (TECH-32928). Runtime panels must consume USS var(--ds-*) tokens via UI Toolkit TSS. Deletion deferred to uGUI purge plan.")]
    [CreateAssetMenu(fileName = "UiTheme", menuName = "Territory/UI/Ui Theme", order = 0)]
    public class UiTheme : ScriptableObject
    {
        // ----- Legacy flat-Color fields (preserved during Stage 2 transition) -----

        [Header("Primary actions")]
        [SerializeField] private Color primaryButtonColor = new Color(0.157f, 0.173f, 0.208f, 1f);
        [SerializeField] private Color primaryButtonTextColor = new Color(0.91f, 0.918f, 0.941f, 1f);
        [SerializeField] private int primaryButtonFontSize = 18;

        [Header("Menu (MainMenu strip)")]
        [SerializeField] private Color menuButtonColor = new Color(0.157f, 0.173f, 0.208f, 1f);
        [SerializeField] private Color menuButtonTextColor = new Color(0.91f, 0.918f, 0.941f, 1f);
        [SerializeField] private int menuButtonFontSize = 18;

        [Header("Surfaces (city HUD)")]
        [Tooltip("Deepest chrome: fullscreen tint base.")]
        [SerializeField] private Color surfaceBase = new Color(0.0667f, 0.0745f, 0.0941f, 1f);
        [Tooltip("HUD / popup card backgrounds (includes alpha for map bleed-through).")]
        [SerializeField] private Color surfaceCardHud = new Color(0.11f, 0.122f, 0.149f, 0.88f);
        [Tooltip("Toolbar strip background (slightly more opaque than HUD cards).")]
        [SerializeField] private Color surfaceToolbar = new Color(0.0667f, 0.0745f, 0.0941f, 0.94f);
        [Tooltip("Elevated controls: active tool, tooltips, dropdowns.")]
        [SerializeField] private Color surfaceElevated = new Color(0.157f, 0.173f, 0.208f, 1f);
        [Tooltip("1 px dividers and subtle panel edges.")]
        [SerializeField] private Color borderSubtle = new Color(0.18f, 0.2f, 0.251f, 1f);

        [Header("Text")]
        [SerializeField] private Color textPrimary = new Color(0.91f, 0.918f, 0.941f, 1f);
        [SerializeField] private Color textSecondary = new Color(0.545f, 0.561f, 0.643f, 1f);

        [Header("Accents")]
        [SerializeField] private Color accentPrimary = new Color(0.29f, 0.62f, 1f, 1f);
        [SerializeField] private Color accentPositive = new Color(0.204f, 0.78f, 0.349f, 1f);
        [SerializeField] private Color accentNegative = new Color(1f, 0.271f, 0.227f, 1f);

        [Header("Modal")]
        [Tooltip("Fullscreen dimmer behind popups.")]
        [SerializeField] private Color modalDimmerColor = new Color(0f, 0f, 0f, 0.667f);

        [Header("Typography (legacy Text)")]
        [SerializeField] private int fontSizeDisplay = 28;
        [SerializeField] private int fontSizeHeading = 18;
        [SerializeField] private int fontSizeBody = 14;
        [SerializeField] private int fontSizeCaption = 11;

        [Header("Spacing (px, reference for layout)")]
        [SerializeField] private int spacingUnit = 4;
        [SerializeField] private int panelPadding = 16;

        // ----- Stage 2 (T2.1) dict-shaped token caches -----
        // Field names mirror the historical sketchpad token shape for deterministic bake parity.

        [Header("IR token caches (Stage 2 — bake target)")]
        [SerializeField] private List<PaletteKv> paletteEntries = new List<PaletteKv>();
        [SerializeField] private List<FrameStyleKv> frameStyleEntries = new List<FrameStyleKv>();
        [SerializeField] private List<FontFaceKv> fontFaceEntries = new List<FontFaceKv>();
        [SerializeField] private List<MotionCurveKv> motionCurveEntries = new List<MotionCurveKv>();
        [SerializeField] private List<IlluminationKv> illuminationEntries = new List<IlluminationKv>();

        [Header("Icon slug map (Stage 13.3 — bake-time icon resolution)")]
        [SerializeField] private List<IconKv> iconEntries = new List<IconKv>();

        // Lazy-rebuilt runtime caches. Rebuild trigger: dict null OR slug count differs from list count.
        private Dictionary<string, PaletteRamp> _paletteCache;
        private Dictionary<string, FrameStyleSpec> _frameStyleCache;
        private Dictionary<string, FontFaceSpec> _fontFaceCache;
        private Dictionary<string, MotionCurveSpec> _motionCurveCache;
        private Dictionary<string, IlluminationSpec> _illuminationCache;
        private Dictionary<string, Sprite> _iconCache;

        // ----- Legacy accessors -----

        public Color PrimaryButtonColor => primaryButtonColor;
        public Color PrimaryButtonTextColor => primaryButtonTextColor;
        public int PrimaryButtonFontSize => primaryButtonFontSize;
        public Color MenuButtonColor => menuButtonColor;
        public Color MenuButtonTextColor => menuButtonTextColor;
        public int MenuButtonFontSize => menuButtonFontSize;

        public Color SurfaceBase => surfaceBase;
        public Color SurfaceCardHud => surfaceCardHud;
        public Color SurfaceToolbar => surfaceToolbar;
        public Color SurfaceElevated => surfaceElevated;
        public Color BorderSubtle => borderSubtle;
        public Color TextPrimary => textPrimary;
        public Color TextSecondary => textSecondary;
        public Color AccentPrimary => accentPrimary;
        public Color AccentPositive => accentPositive;
        public Color AccentNegative => accentNegative;
        public Color ModalDimmerColor => modalDimmerColor;
        public int FontSizeDisplay => fontSizeDisplay;
        public int FontSizeHeading => fontSizeHeading;
        public int FontSizeBody => fontSizeBody;
        public int FontSizeCaption => fontSizeCaption;
        public int SpacingUnit => spacingUnit;
        public int PanelPadding => panelPadding;

        // ----- Stage 2 dict accessors. Cache returned reference once per consumer; do NOT call inside `Update`. -----

        /// <summary>Try-get palette ramp by slug. Lazy-rebuilds dict cache on first call after deserialization.</summary>
        public bool TryGetPalette(string slug, out PaletteRamp value)
        {
            EnsurePaletteCache();
            return _paletteCache.TryGetValue(slug ?? string.Empty, out value);
        }

        /// <summary>Try-get frame-style spec by slug. Lazy-rebuilds dict cache on first call after deserialization.</summary>
        public bool TryGetFrameStyle(string slug, out FrameStyleSpec value)
        {
            EnsureFrameStyleCache();
            return _frameStyleCache.TryGetValue(slug ?? string.Empty, out value);
        }

        /// <summary>Try-get font-face spec by slug. Lazy-rebuilds dict cache on first call after deserialization.</summary>
        public bool TryGetFontFace(string slug, out FontFaceSpec value)
        {
            EnsureFontFaceCache();
            return _fontFaceCache.TryGetValue(slug ?? string.Empty, out value);
        }

        /// <summary>Try-get motion-curve spec by slug. Lazy-rebuilds dict cache on first call after deserialization.</summary>
        public bool TryGetMotionCurve(string slug, out MotionCurveSpec value)
        {
            EnsureMotionCurveCache();
            return _motionCurveCache.TryGetValue(slug ?? string.Empty, out value);
        }

        /// <summary>Try-get illumination spec by slug. Lazy-rebuilds dict cache on first call after deserialization.</summary>
        public bool TryGetIllumination(string slug, out IlluminationSpec value)
        {
            EnsureIlluminationCache();
            return _illuminationCache.TryGetValue(slug ?? string.Empty, out value);
        }

        /// <summary>Try-get icon Sprite by slug (Stage 13.3). Lazy-rebuilds dict cache on first call after deserialization.</summary>
        public bool TryGetIcon(string slug, out Sprite sprite)
        {
            EnsureIconCache();
            return _iconCache.TryGetValue(slug ?? string.Empty, out sprite);
        }

        // ----- Mutation surface (Editor-only contract — used by UiBakeHandler — Stage 2 T2.4). -----
        // Public visibility required because bake handler lives in Editor assembly. Runtime callers
        // must NOT mutate these lists directly; use IR JSON → bake pipeline.

        public List<PaletteKv> PaletteEntries => paletteEntries;
        public List<FrameStyleKv> FrameStyleEntries => frameStyleEntries;
        public List<FontFaceKv> FontFaceEntries => fontFaceEntries;
        public List<MotionCurveKv> MotionCurveEntries => motionCurveEntries;
        public List<IlluminationKv> IlluminationEntries => illuminationEntries;
        public List<IconKv> IconEntries => iconEntries;

        /// <summary>Force dict cache invalidation. Bake handler calls this after rewriting backing lists.</summary>
        public void InvalidateTokenCaches()
        {
            _paletteCache = null;
            _frameStyleCache = null;
            _fontFaceCache = null;
            _motionCurveCache = null;
            _illuminationCache = null;
            _iconCache = null;
        }

        // ----- Lazy-rebuild guards. Idempotent: rebuilds when dict null or count mismatches list. -----

        private void EnsurePaletteCache()
        {
            if (_paletteCache != null && _paletteCache.Count == paletteEntries.Count) return;
            var dict = new Dictionary<string, PaletteRamp>(paletteEntries.Count);
            foreach (var kv in paletteEntries)
            {
                if (string.IsNullOrEmpty(kv.slug)) continue;
                dict[kv.slug] = kv.value;
            }
            _paletteCache = dict;
        }

        private void EnsureFrameStyleCache()
        {
            if (_frameStyleCache != null && _frameStyleCache.Count == frameStyleEntries.Count) return;
            var dict = new Dictionary<string, FrameStyleSpec>(frameStyleEntries.Count);
            foreach (var kv in frameStyleEntries)
            {
                if (string.IsNullOrEmpty(kv.slug)) continue;
                dict[kv.slug] = kv.value;
            }
            _frameStyleCache = dict;
        }

        private void EnsureFontFaceCache()
        {
            if (_fontFaceCache != null && _fontFaceCache.Count == fontFaceEntries.Count) return;
            var dict = new Dictionary<string, FontFaceSpec>(fontFaceEntries.Count);
            foreach (var kv in fontFaceEntries)
            {
                if (string.IsNullOrEmpty(kv.slug)) continue;
                dict[kv.slug] = kv.value;
            }
            _fontFaceCache = dict;
        }

        private void EnsureMotionCurveCache()
        {
            if (_motionCurveCache != null && _motionCurveCache.Count == motionCurveEntries.Count) return;
            var dict = new Dictionary<string, MotionCurveSpec>(motionCurveEntries.Count);
            foreach (var kv in motionCurveEntries)
            {
                if (string.IsNullOrEmpty(kv.slug)) continue;
                dict[kv.slug] = kv.value;
            }
            _motionCurveCache = dict;
        }

        private void EnsureIlluminationCache()
        {
            if (_illuminationCache != null && _illuminationCache.Count == illuminationEntries.Count) return;
            var dict = new Dictionary<string, IlluminationSpec>(illuminationEntries.Count);
            foreach (var kv in illuminationEntries)
            {
                if (string.IsNullOrEmpty(kv.slug)) continue;
                dict[kv.slug] = kv.value;
            }
            _illuminationCache = dict;
        }

        private void EnsureIconCache()
        {
            if (_iconCache != null && _iconCache.Count == iconEntries.Count) return;
            var dict = new Dictionary<string, Sprite>(iconEntries.Count);
            foreach (var kv in iconEntries)
            {
                if (string.IsNullOrEmpty(kv.slug)) continue;
                dict[kv.slug] = kv.sprite;
            }
            _iconCache = dict;
        }

        // ----- DTO struct types. Field names mirror the historical sketchpad token shape. -----

        /// <summary>Palette ramp = ordered hex stops (low → high). Mirrors `IrTokenPalette.ramp`.</summary>
        [Serializable]
        public struct PaletteRamp
        {
            /// <summary>Ordered hex stop strings (e.g. "#RRGGBB" / "#RRGGBBAA"). Low → high.</summary>
            public string[] ramp;
        }

        /// <summary>Frame-style spec. Mirrors `IrTokenFrameStyle` + catalog_token_frame_style shape.</summary>
        [Serializable]
        public struct FrameStyleSpec
        {
            /// <summary>`single` | `double` (CD partner extends).</summary>
            public string edge;
            public float innerShadowAlpha;
            /// <summary>Catalog slug matching future catalog_token_frame_style.slug. Stage 19.3 wire_asset_from_catalog reads this.</summary>
            public string catalog_sprite_slug;
            /// <summary>Fallback sprite used when catalog row not yet resolved. May be null on first-pass entries.</summary>
            public Sprite sprite_ref_fallback;
        }

        /// <summary>Font-face spec. Mirrors `IrTokenFontFace` + catalog_token_font_face shape.</summary>
        [Serializable]
        public struct FontFaceSpec
        {
            public string family;
            public int weight;
            /// <summary>Catalog slug matching future catalog_token_font_face.slug. Stage 19.3 wire_asset_from_catalog reads this.</summary>
            public string font_catalog_slug;
            /// <summary>TMP font asset reference. May be null on first-pass entries.</summary>
            public TMP_FontAsset font_ref;
        }

        /// <summary>Motion-curve spec. Mirrors `IrTokenMotionCurve`. Optional fields default to 0.</summary>
        [Serializable]
        public struct MotionCurveSpec
        {
            /// <summary>`spring` | `cubic-bezier` | other (CD partner extends).</summary>
            public string kind;
            public float stiffness;
            public float damping;
            public float[] c1;
            public float[] c2;
            public float durationMs;
        }

        /// <summary>Illumination spec. Mirrors `IrTokenIllumination`.</summary>
        [Serializable]
        public struct IlluminationSpec
        {
            /// <summary>Hex string (e.g. "#RRGGBBAA").</summary>
            public string color;
            public float haloRadiusPx;
        }

        // ----- Concrete KvPair sibling types (Unity serializer rejects open generics). -----

        [Serializable]
        public struct PaletteKv
        {
            public string slug;
            public PaletteRamp value;
        }

        [Serializable]
        public struct FrameStyleKv
        {
            public string slug;
            public FrameStyleSpec value;
        }

        [Serializable]
        public struct FontFaceKv
        {
            public string slug;
            public FontFaceSpec value;
        }

        [Serializable]
        public struct MotionCurveKv
        {
            public string slug;
            public MotionCurveSpec value;
        }

        [Serializable]
        public struct IlluminationKv
        {
            public string slug;
            public IlluminationSpec value;
        }

        /// <summary>Icon slug → Sprite pair (Stage 13.3 — bake-time icon resolution).</summary>
        [Serializable]
        public struct IconKv
        {
            public string slug;
            public Sprite sprite;
        }
    }
}
