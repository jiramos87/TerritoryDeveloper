using UnityEngine;
using UnityEngine.UI;

namespace Territory.UI.Themed
{
    /// <summary>Themed list variant — viewport palette tint; per-item token application delegated to per-item primitives.</summary>
    public class ThemedList : ThemedPrimitiveBase
    {
        [SerializeField] private string _paletteSlug;
        [SerializeField] private ScrollRect _viewport;
        [SerializeField] private GameObject _itemTemplate;

        public override void ApplyTheme(UiTheme theme)
        {
            if (theme == null || _viewport == null) return;
            var background = _viewport.GetComponent<Image>();
            if (background == null) return;
            if (theme.TryGetPalette(_paletteSlug, out var ramp)
                && ramp.ramp != null
                && ramp.ramp.Length > 0
                && ColorUtility.TryParseHtmlString(ramp.ramp[0], out var c))
            {
                background.color = c;
            }
            // _itemTemplate intentionally untouched — per-item Themed* primitives apply their own tokens.
        }
    }
}
