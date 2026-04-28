using TMPro;
using UnityEngine;

namespace Territory.UI.Themed
{
    /// <summary>Themed label text variant — font_face + palette token consumer.</summary>
    public class ThemedLabel : ThemedPrimitiveBase
    {
        [SerializeField] private string _fontFaceSlug;
        [SerializeField] private string _paletteSlug;
        [SerializeField] private TMP_Text _tmpText;

        public override void ApplyTheme(UiTheme theme)
        {
            if (theme == null || _tmpText == null) return;
            if (theme.TryGetPalette(_paletteSlug, out var ramp)
                && ramp.ramp != null
                && ramp.ramp.Length > 0
                && ColorUtility.TryParseHtmlString(ramp.ramp[0], out var c))
            {
                _tmpText.color = c;
            }
            if (theme.TryGetFontFace(_fontFaceSlug, out _))
            {
                // FontFaceSpec models family + weight; runtime fontAsset binding deferred to Stage 6 catalog.
            }
        }
    }
}
