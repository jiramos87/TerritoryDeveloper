using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Territory.UI.Themed
{
    /// <summary>Themed tooltip chrome variant — palette + font_face token consumer.</summary>
    public class ThemedTooltip : ThemedPrimitiveBase
    {
        [SerializeField] private string _paletteSlug;
        [SerializeField] private string _fontFaceSlug;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private TMP_Text _tmpText;

        public override void ApplyTheme(UiTheme theme)
        {
            if (theme == null) return;
            if (_backgroundImage != null
                && theme.TryGetPalette(_paletteSlug, out var ramp)
                && ramp.ramp != null
                && ramp.ramp.Length > 0
                && ColorUtility.TryParseHtmlString(ramp.ramp[0], out var c))
            {
                _backgroundImage.color = c;
            }
            if (_tmpText != null && theme.TryGetFontFace(_fontFaceSlug, out _))
            {
                // FontFaceSpec models family + weight; runtime fontAsset lookup deferred to Stage 6 catalog.
            }
        }
    }
}
