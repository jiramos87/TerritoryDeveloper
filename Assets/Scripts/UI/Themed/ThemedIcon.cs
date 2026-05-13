using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Territory.UI.Themed
{
    /// <summary>
    /// Themed icon — resolves <see cref="UiTheme.TryGetIcon"/> from
    /// <see cref="_iconSlug"/> and writes the result onto the wrapped
    /// <see cref="Image"/>. Missing slug path: log a deduped warning per
    /// slug per session and substitute the <c>icon-info</c> placeholder
    /// (Stage 13.3 — operator override accommodating reserved-slug hooks
    /// awaiting designer SVG handoff).
    /// </summary>
    /// <remarks>
    /// Inspector-driven <see cref="_spriteRef"/> still wins when present
    /// (legacy path preserved during transition). Palette tint
    /// (<see cref="_paletteSlug"/>) applies on top of whichever sprite
    /// resolves.
    /// TECH-32929 Stage 6.0 — Simple primitive quarantined; USS icon class / Background property replaces.
    /// </remarks>
    [Obsolete("ThemedIcon quarantined (TECH-32929). Use USS background-image / UI Toolkit VisualElement. Deletion deferred to uGUI purge plan.")]
    public class ThemedIcon : ThemedPrimitiveBase
    {
        private const string FallbackSlug = "icon-info";

        // Per-session warning dedup. Static so the first ApplyTheme that
        // misses on a slug logs once, subsequent ApplyTheme calls (re-bake,
        // theme refresh) silently substitute. Cleared on domain reload by
        // Unity, which is the desired session boundary.
        private static readonly HashSet<string> _warnedSlugs = new HashSet<string>();

        [SerializeField] private string _paletteSlug;
        [SerializeField] private string _iconSlug;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Sprite _spriteRef;

        /// <summary>
        /// Bake-time slug setter. UiBakeHandler writes the IR `iconSlug`
        /// onto the prefab via this so the field round-trips through the
        /// serialized Inspector (no runtime-only state).
        /// </summary>
        public string IconSlug
        {
            get => _iconSlug;
            set => _iconSlug = value;
        }

        public override void ApplyTheme(UiTheme theme)
        {
            if (theme == null || _iconImage == null) return;

            // Sprite resolution priority:
            //   1. Inspector-pinned _spriteRef (legacy / explicit override).
            //   2. theme.TryGetIcon(_iconSlug) — Stage 13.3 canonical path.
            //   3. icon-info fallback + per-slug deduped warning.
            Sprite resolved = _spriteRef;
            if (resolved == null && !string.IsNullOrEmpty(_iconSlug))
            {
                if (theme.TryGetIcon(_iconSlug, out var slugSprite) && slugSprite != null)
                {
                    resolved = slugSprite;
                }
                else
                {
                    if (_warnedSlugs.Add(_iconSlug))
                    {
                        Debug.LogWarning(
                            $"[ThemedIcon] icon slug '{_iconSlug}' not resolved on theme '{theme.name}'; substituting '{FallbackSlug}'.");
                    }
                    if (theme.TryGetIcon(FallbackSlug, out var fallbackSprite) && fallbackSprite != null)
                    {
                        resolved = fallbackSprite;
                    }
                }
            }

            if (resolved != null) _iconImage.sprite = resolved;

            if (theme.TryGetPalette(_paletteSlug, out var ramp)
                && ramp.ramp != null
                && ramp.ramp.Length > 0
                && ColorUtility.TryParseHtmlString(ramp.ramp[0], out var c))
            {
                _iconImage.color = c;
            }
        }
    }
}
