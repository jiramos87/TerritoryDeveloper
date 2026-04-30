using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Territory.UI.Themed.Renderers
{
    /// <summary>
    /// Render-layer companion for <see cref="ThemedToggle"/>; caches child checkmark
    /// <see cref="Image"/> + label <see cref="TMP_Text"/> refs in <c>Awake</c> and writes
    /// palette colors on <see cref="ApplyTheme"/>. Bake-time-attached only (Stage 10 lock).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ThemedToggle))]
    public class ThemedToggleRenderer : ThemedPrimitiveBase
    {
        [Header("Token")]
        [SerializeField] private string _paletteSlug;

        [Header("Render targets")]
        [SerializeField] private Image _checkmarkImage;
        [SerializeField] private TMP_Text _labelText;

        protected override void Awake()
        {
            base.Awake();
        }

        public override void ApplyTheme(UiTheme theme)
        {
            if (theme == null) return;
            if (!theme.TryGetPalette(_paletteSlug, out var ramp) || ramp.ramp == null || ramp.ramp.Length == 0) return;
            if (!ColorUtility.TryParseHtmlString(ramp.ramp[0], out var baseColor)) return;
            if (_checkmarkImage != null) _checkmarkImage.color = baseColor;
            if (_labelText != null) _labelText.color = baseColor;
        }
    }
}
