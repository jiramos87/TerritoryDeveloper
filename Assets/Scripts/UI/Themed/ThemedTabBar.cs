using UnityEngine;
using UnityEngine.UI;

namespace Territory.UI.Themed
{
    /// <summary>Themed tab bar chrome variant — palette + frame_style token consumer.</summary>
    public class ThemedTabBar : ThemedPrimitiveBase
    {
        [SerializeField] private string _paletteSlug;
        [SerializeField] private string _frameStyleSlug;
        [SerializeField] private Image _tabStripImage;

        public override void ApplyTheme(UiTheme theme)
        {
            if (theme == null || _tabStripImage == null) return;
            if (theme.TryGetPalette(_paletteSlug, out var ramp) && ramp.ramp != null && ramp.ramp.Length > 0)
            {
                if (ColorUtility.TryParseHtmlString(ramp.ramp[0], out var c))
                {
                    _tabStripImage.color = c;
                }
            }
            if (theme.TryGetFrameStyle(_frameStyleSlug, out _))
            {
                // Frame edge stylistic refinement deferred — token consumed gracefully.
            }
        }
    }
}
