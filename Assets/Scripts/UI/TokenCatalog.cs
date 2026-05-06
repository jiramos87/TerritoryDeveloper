using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

namespace Territory.UI
{
    /// <summary>
    /// Token catalog binder (TECH-2095 / asset-pipeline Stage 10.1).
    /// <para>
    /// Loads the legacy per-kind token snapshot at <c>StreamingAssets/catalog/</c>
    /// at <see cref="Awake"/>, indexes rows by <c>slug</c> for the five DEC-A44 kinds
    /// (color / type-scale / motion / spacing / semantic), and exposes typed
    /// <c>TryGet*</c> accessors plus a depth-capped semantic alias resolver.
    /// </para>
    /// <para>
    /// Mirrors <see cref="GridAssetCatalog"/> wiring: scene host under
    /// <c>Game Managers</c>; <c>OnCatalogReloaded</c> ripples DEC-A44 token edits
    /// to scene listeners. Hot-reload (FileSystemWatcher) is Stage 13.1; this
    /// component exposes a public <see cref="Reload"/> entry point only.
    /// </para>
    /// <para>
    /// JSON shape: <c>{ schemaVersion, generatedAt, tokens: TokenRowDto[] }</c>
    /// where each row carries <c>slug</c>, <c>token_kind</c> discriminator, and
    /// per-kind <c>color</c> / <c>type_scale</c> / <c>motion</c> / <c>spacing</c>
    /// / <c>semantic</c> sub-DTOs. Per-kind sub-DTOs use flat
    /// <see cref="JsonUtility"/>-friendly fields (no nested unions).
    /// </para>
    /// </summary>
    public sealed class TokenCatalog : MonoBehaviour
    {
        /// <summary>Hard cap on semantic alias chain depth (matches Stage 8.1 panel-cycle budget + TECH-2094 PanelPreview cap).</summary>
        public const int SemanticDepthCap = 6;

        [Header("Snapshot")]
        [Tooltip("Legacy v1 path under Application.streamingAssetsPath. Superseded by per-kind exports under catalog/ + CatalogLoader (Stage 13.1, TECH-2675); leave empty unless reviving v1 in a fixture scene.")]
        [SerializeField] private string _streamingRelativePath = string.Empty;

        [Header("Events")]
        [SerializeField] private UnityEvent _onCatalogReloaded = new UnityEvent();

        /// <summary>Raised after a successful load or reload; safe to re-query indexes.</summary>
        public UnityEvent OnCatalogReloaded => _onCatalogReloaded;

        private readonly Dictionary<string, ColorTokenDto> _colors = new();
        private readonly Dictionary<string, TypeScaleTokenDto> _typeScales = new();
        private readonly Dictionary<string, MotionTokenDto> _motions = new();
        private readonly Dictionary<string, SpacingTokenDto> _spacings = new();
        private readonly Dictionary<string, SemanticTokenDto> _semantics = new();

        private void Awake() => LoadInternal();

        /// <summary>Public reload entry point (Stage 13.1 hot-reload plumbing will trigger this).</summary>
        public void Reload() => LoadInternal();

        /// <summary>Attempt to resolve a color token by slug.</summary>
        public bool TryGetColor(string slug, out ColorTokenDto row) => _colors.TryGetValue(slug ?? string.Empty, out row);

        /// <summary>Attempt to resolve a type-scale token by slug.</summary>
        public bool TryGetTypeScale(string slug, out TypeScaleTokenDto row) => _typeScales.TryGetValue(slug ?? string.Empty, out row);

        /// <summary>Attempt to resolve a motion token by slug.</summary>
        public bool TryGetMotion(string slug, out MotionTokenDto row) => _motions.TryGetValue(slug ?? string.Empty, out row);

        /// <summary>Attempt to resolve a spacing token by slug.</summary>
        public bool TryGetSpacing(string slug, out SpacingTokenDto row) => _spacings.TryGetValue(slug ?? string.Empty, out row);

        /// <summary>Attempt to resolve a semantic token by slug (no alias walk).</summary>
        public bool TryGetSemantic(string slug, out SemanticTokenDto row) => _semantics.TryGetValue(slug ?? string.Empty, out row);

        /// <summary>
        /// Read <c>motion.hover</c> enum from a motion token by archetype slug.
        /// Delegates to the motion token index; <c>MotionTokenDto.curve</c> carries the hover kind string
        /// when authored as archetype-scoped motion tokens (TECH-15891 / Stage 9.7).
        /// Returns false when slug absent or <c>curve</c> empty.
        /// </summary>
        public bool TryGetMotionHover(string archetypeSlug, out string hoverEnum)
        {
            hoverEnum = null;
            if (!_motions.TryGetValue(archetypeSlug ?? string.Empty, out var row)) return false;
            hoverEnum = row.curve;
            return !string.IsNullOrEmpty(hoverEnum);
        }

        /// <summary>
        /// Walk the semantic alias chain starting at <paramref name="slug"/> up to
        /// <see cref="SemanticDepthCap"/> hops, returning the terminal target slug
        /// + <c>token_role</c>. Returns <c>false</c> when slug missing, alias
        /// breaks, or depth cap exceeded.
        /// </summary>
        public bool TryResolveSemantic(string slug, out string targetSlug, out string tokenRole)
        {
            targetSlug = null;
            tokenRole = null;
            if (string.IsNullOrEmpty(slug)) return false;

            string cursor = slug;
            for (int depth = 0; depth <= SemanticDepthCap; depth++)
            {
                if (!_semantics.TryGetValue(cursor, out var row))
                {
                    return false;
                }

                // Terminal alias: target_slug empty → row's own token_role wins.
                if (string.IsNullOrEmpty(row.target_slug))
                {
                    targetSlug = cursor;
                    tokenRole = row.token_role ?? string.Empty;
                    return true;
                }

                cursor = row.target_slug;
            }

            // Exceeded depth cap without terminating.
            return false;
        }

        private void LoadInternal()
        {
            ClearIndexes();

            if (string.IsNullOrEmpty(_streamingRelativePath))
            {
                // v1 path superseded by per-kind exports + CatalogLoader (Stage 13.1, TECH-2675). Empty = expected.
                return;
            }

            string full = Path.Combine(Application.streamingAssetsPath, _streamingRelativePath);
            if (!File.Exists(full))
            {
                Debug.LogError($"[TokenCatalog] Snapshot file not found: {full}");
                return;
            }

            string text;
            try
            {
                text = File.ReadAllText(full);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TokenCatalog] Read failed: {ex.Message}");
                return;
            }

            if (!TryParseSnapshotJson(text, out var root, out var err))
            {
                Debug.LogError($"[TokenCatalog] Parse failed: {err}");
                return;
            }

            RebuildIndexes(root);

            if (_onCatalogReloaded != null) _onCatalogReloaded.Invoke();
        }

        private void ClearIndexes()
        {
            _colors.Clear();
            _typeScales.Clear();
            _motions.Clear();
            _spacings.Clear();
            _semantics.Clear();
        }

        /// <summary>Parse a snapshot JSON string into the root DTO. Public for tests.</summary>
        public static bool TryParseSnapshotJson(string json, out TokenCatalogSnapshotDto root, out string err)
        {
            root = null;
            err = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                err = "JSON is null or empty.";
                return false;
            }

            try
            {
                string t = json.Trim();
                if (!t.StartsWith("{", StringComparison.Ordinal))
                {
                    err = "JSON must be a single object.";
                    return false;
                }

                var parsed = JsonUtility.FromJson<TokenCatalogSnapshotDto>(t);
                if (parsed == null)
                {
                    err = "JsonUtility returned null root.";
                    return false;
                }

                if (parsed.schemaVersion < 1)
                {
                    err = "Missing or invalid schemaVersion (expected >= 1).";
                    return false;
                }

                if (parsed.tokens == null) parsed.tokens = Array.Empty<TokenRowDto>();
                root = parsed;
                return true;
            }
            catch (Exception ex)
            {
                err = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Build per-kind indexes from a parsed snapshot. Duplicate slugs within a
        /// kind keep the first row + log a warning. Public so tests can exercise
        /// indexing without touching <see cref="StreamingAssets"/>.
        /// </summary>
        public void RebuildIndexes(TokenCatalogSnapshotDto data)
        {
            ClearIndexes();
            if (data == null || data.tokens == null) return;

            foreach (var row in data.tokens)
            {
                if (row == null) continue;
                if (string.IsNullOrEmpty(row.slug))
                {
                    Debug.LogWarning("[TokenCatalog] Skipping row with empty slug.");
                    continue;
                }

                switch (row.token_kind)
                {
                    case "color":
                        if (_colors.ContainsKey(row.slug))
                        {
                            Debug.LogWarning($"[TokenCatalog] Duplicate color slug '{row.slug}'. Keeping first row.");
                            continue;
                        }
                        _colors.Add(row.slug, row.color ?? new ColorTokenDto());
                        break;
                    case "type-scale":
                        if (_typeScales.ContainsKey(row.slug))
                        {
                            Debug.LogWarning($"[TokenCatalog] Duplicate type-scale slug '{row.slug}'. Keeping first row.");
                            continue;
                        }
                        _typeScales.Add(row.slug, row.type_scale ?? new TypeScaleTokenDto());
                        break;
                    case "motion":
                        if (_motions.ContainsKey(row.slug))
                        {
                            Debug.LogWarning($"[TokenCatalog] Duplicate motion slug '{row.slug}'. Keeping first row.");
                            continue;
                        }
                        _motions.Add(row.slug, row.motion ?? new MotionTokenDto());
                        break;
                    case "spacing":
                        if (_spacings.ContainsKey(row.slug))
                        {
                            Debug.LogWarning($"[TokenCatalog] Duplicate spacing slug '{row.slug}'. Keeping first row.");
                            continue;
                        }
                        _spacings.Add(row.slug, row.spacing ?? new SpacingTokenDto());
                        break;
                    case "semantic":
                        if (_semantics.ContainsKey(row.slug))
                        {
                            Debug.LogWarning($"[TokenCatalog] Duplicate semantic slug '{row.slug}'. Keeping first row.");
                            continue;
                        }
                        _semantics.Add(row.slug, row.semantic ?? new SemanticTokenDto());
                        break;
                    default:
                        Debug.LogWarning($"[TokenCatalog] Unknown token_kind '{row.token_kind}' for slug '{row.slug}'.");
                        break;
                }
            }
        }
    }

    /// <summary>Root DTO matching the export written by Stage 13.1's snapshot exporter.</summary>
    [Serializable]
    public class TokenCatalogSnapshotDto
    {
        public int schemaVersion;
        public string generatedAt;
        public TokenRowDto[] tokens;
    }

    /// <summary>One token row with discriminator + per-kind sub-DTO; only the matching kind field is populated.</summary>
    [Serializable]
    public class TokenRowDto
    {
        public string slug;
        public string token_kind;
        public ColorTokenDto color;
        public TypeScaleTokenDto type_scale;
        public MotionTokenDto motion;
        public SpacingTokenDto spacing;
        public SemanticTokenDto semantic;
    }

    /// <summary>Color token: hex string OR HSL triple. Empty hex means HSL is authoritative.</summary>
    [Serializable]
    public class ColorTokenDto
    {
        public string hex;
        public float h;
        public float s;
        public float l;
    }

    /// <summary>Type-scale token: font family + size + line height.</summary>
    [Serializable]
    public class TypeScaleTokenDto
    {
        public string font_family;
        public float size_px;
        public float line_height;
    }

    /// <summary>Motion token: curve enum + duration + optional cubic-bezier 4-tuple.</summary>
    [Serializable]
    public class MotionTokenDto
    {
        public string curve;
        public float duration_ms;
        public float[] cubic_bezier;
    }

    /// <summary>Spacing token: pixel scalar.</summary>
    [Serializable]
    public class SpacingTokenDto
    {
        public float px;
    }

    /// <summary>Semantic alias: target slug + role label. Empty target_slug means terminal.</summary>
    [Serializable]
    public class SemanticTokenDto
    {
        public string target_slug;
        public string token_role;
    }
}
