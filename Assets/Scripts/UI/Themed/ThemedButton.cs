using UnityEngine;
using UnityEngine.UI;

namespace Territory.UI.Themed
{
    /// <summary>Themed Button chrome variant — palette + frame_style token consumer.</summary>
    public class ThemedButton : ThemedPrimitiveBase
    {
        [SerializeField] private string _paletteSlug;
        [SerializeField] private string _frameStyleSlug;
        [SerializeField] private Image _buttonImage;

        public override void ApplyTheme(UiTheme theme)
        {
            if (theme == null || _buttonImage == null) return;
            if (theme.TryGetPalette(_paletteSlug, out var ramp) && ramp.ramp != null && ramp.ramp.Length > 0)
            {
                if (ColorUtility.TryParseHtmlString(ramp.ramp[0], out var c))
                {
                    _buttonImage.color = c;
                }
            }
            if (theme.TryGetFrameStyle(_frameStyleSlug, out _))
            {
                // FrameStyle currently models edge + innerShadowAlpha (no sprite ref); sprite swap lands later.
            }
        }
    }
}
