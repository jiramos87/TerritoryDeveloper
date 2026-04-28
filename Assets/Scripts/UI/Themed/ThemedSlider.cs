using UnityEngine;
using UnityEngine.UI;

namespace Territory.UI.Themed
{
    /// <summary>Themed slider input variant — palette + frame_style token consumer (visuals only).</summary>
    public class ThemedSlider : ThemedPrimitiveBase
    {
        [SerializeField] private string _paletteSlug;
        [SerializeField] private string _frameStyleSlug;
        [SerializeField] private Image _sliderHandleImage;
        [SerializeField] private Image _sliderTrackImage;

        public override void ApplyTheme(UiTheme theme)
        {
            if (theme == null) return;
            if (_sliderHandleImage != null
                && theme.TryGetPalette(_paletteSlug, out var ramp)
                && ramp.ramp != null
                && ramp.ramp.Length > 0
                && ColorUtility.TryParseHtmlString(ramp.ramp[0], out var c))
            {
                _sliderHandleImage.color = c;
            }
            if (_sliderTrackImage != null && theme.TryGetFrameStyle(_frameStyleSlug, out _))
            {
                // Track sprite swap deferred — frame_style consumed gracefully.
            }
        }
    }
}
