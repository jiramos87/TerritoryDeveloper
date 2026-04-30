using UnityEngine;
using UnityEngine.UI;

namespace Territory.UI.Themed
{
    /// <summary>Themed count / status badge — composes background <see cref="Image"/> + foreground <see cref="ThemedLabel"/>.</summary>
    public class ThemedBadge : ThemedPrimitiveBase
    {
        [SerializeField] private string _paletteSlug;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private ThemedLabel _label;

        /// <summary>Badge text proxy → child <c>_label.Detail</c>; null-guard returns <see cref="string.Empty"/>.</summary>
        public string Detail
        {
            get => _label != null ? _label.Detail : string.Empty;
            set { if (_label != null) _label.Detail = value; }
        }

        public override void ApplyTheme(UiTheme theme)
        {
            if (theme == null || _backgroundImage == null)
            {
                Debug.LogWarning("[ThemedBadge] missing theme or _backgroundImage; skipping ApplyTheme");
                return;
            }
            if (!theme.TryGetPalette(_paletteSlug, out var ramp)
                || ramp.ramp == null
                || ramp.ramp.Length == 0)
            {
                Debug.LogWarning($"[ThemedBadge] palette slug '{_paletteSlug}' not in theme or ramp empty");
                return;
            }
            // Stage 1.3 (T1.3.6) — lightest ramp stop mirrors ThemedButton fill convention so badge
            // separates from panel background fill (which uses ramp[1]).
            int idx = ramp.ramp.Length - 1;
            if (ColorUtility.TryParseHtmlString(ramp.ramp[idx], out var c))
            {
                _backgroundImage.color = c;
            }
        }
    }
}
