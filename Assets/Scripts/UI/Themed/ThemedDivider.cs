using UnityEngine;
using UnityEngine.UI;

namespace Territory.UI.Themed
{
    /// <summary>Themed divider archetype — Image color from palette ramp mid-stop + LayoutElement preferredHeight.</summary>
    public class ThemedDivider : ThemedPrimitiveBase
    {
        [SerializeField] private string _paletteSlug;
        [SerializeField] private Image _dividerImage;
        [SerializeField] private float _thickness = 1f;

        public override void ApplyTheme(UiTheme theme)
        {
            if (theme == null || _dividerImage == null)
            {
                Debug.LogWarning("[ThemedDivider] missing theme or _dividerImage; skipping ApplyTheme");
                return;
            }
            if (!theme.TryGetPalette(_paletteSlug, out var ramp)
                || ramp.ramp == null
                || ramp.ramp.Length == 0)
            {
                Debug.LogWarning($"[ThemedDivider] palette slug '{_paletteSlug}' not in theme or ramp empty");
                return;
            }
            // Stage 1.3 (T1.3.5) — pick mid-ramp stop ("subtle border" position) per §Pending Decisions §1.
            int idx = ramp.ramp.Length / 2;
            if (ColorUtility.TryParseHtmlString(ramp.ramp[idx], out var c))
            {
                _dividerImage.color = c;
            }

            var le = GetComponent<LayoutElement>();
            if (le != null)
            {
                le.preferredHeight = _thickness;
            }
        }
    }
}
