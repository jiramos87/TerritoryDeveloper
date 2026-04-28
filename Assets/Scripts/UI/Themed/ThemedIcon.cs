using UnityEngine;
using UnityEngine.UI;

namespace Territory.UI.Themed
{
    /// <summary>Themed icon variant — palette tint + Inspector-driven sprite ref (sprite registry deferred).</summary>
    public class ThemedIcon : ThemedPrimitiveBase
    {
        [SerializeField] private string _paletteSlug;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Sprite _spriteRef;

        public override void ApplyTheme(UiTheme theme)
        {
            if (theme == null || _iconImage == null) return;
            if (_spriteRef != null) _iconImage.sprite = _spriteRef;
            if (theme.TryGetPalette(_paletteSlug, out var ramp)
                && ramp.ramp != null
                && ramp.ramp.Length > 0
                && ColorUtility.TryParseHtmlString(ramp.ramp[0], out var c))
            {
                _iconImage.color = c;
            }
        }
    }
}
