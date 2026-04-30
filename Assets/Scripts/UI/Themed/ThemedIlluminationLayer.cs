using UnityEngine;
using UnityEngine.UI;

namespace Territory.UI.Themed
{
    /// <summary>Themed illumination overlay — sibling Image color + halo radius from <see cref="UiTheme.IlluminationSpec"/>.</summary>
    public class ThemedIlluminationLayer : ThemedPrimitiveBase
    {
        [SerializeField] private string _illuminationSlug;
        [SerializeField] private Image _overlayImage;

        private float _haloRadiusPx;

        /// <summary>Resolved halo radius in px (cached on <see cref="ApplyTheme"/> hit; default 0).</summary>
        public float HaloRadiusPx => _haloRadiusPx;

        public override void ApplyTheme(UiTheme theme)
        {
            if (theme == null || _overlayImage == null)
            {
                Debug.LogWarning("[ThemedIlluminationLayer] missing theme or _overlayImage; skipping ApplyTheme");
                return;
            }
            if (!theme.TryGetIllumination(_illuminationSlug, out var spec))
            {
                Debug.LogWarning($"[ThemedIlluminationLayer] illumination slug '{_illuminationSlug}' not in theme");
                return;
            }
            if (!ColorUtility.TryParseHtmlString(spec.color, out var c))
            {
                Debug.LogWarning($"[ThemedIlluminationLayer] illumination color '{spec.color}' parse failed");
                return;
            }
            _overlayImage.color = c;
            _haloRadiusPx = spec.haloRadiusPx;
        }
    }
}
