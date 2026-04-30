using UnityEngine;
using UnityEngine.UI;

namespace Territory.UI.Themed
{
    /// <summary>Themed frame chrome — Image + Sliced sprite from <see cref="UiTheme.FrameStyleSpec.sprite_ref_fallback"/>.</summary>
    public class ThemedFrame : ThemedPrimitiveBase
    {
        [SerializeField] private string _frameStyleSlug;
        [SerializeField] private Image _frameImage;

        public override void ApplyTheme(UiTheme theme)
        {
            if (theme == null || _frameImage == null)
            {
                Debug.LogWarning("[ThemedFrame] missing theme or _frameImage; skipping ApplyTheme");
                return;
            }
            if (!theme.TryGetFrameStyle(_frameStyleSlug, out var spec))
            {
                Debug.LogWarning($"[ThemedFrame] frame_style slug '{_frameStyleSlug}' not in theme");
                return;
            }
            // sprite_ref_fallback may be null on first-pass entries — Image renders untouched in that case.
            _frameImage.sprite = spec.sprite_ref_fallback;
            _frameImage.type = Image.Type.Sliced;
        }
    }
}
