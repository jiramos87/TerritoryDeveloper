using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Territory.UI.Themed.Renderers
{
    /// <summary>
    /// Render-layer companion for <see cref="ThemedSlider"/>; caches child track/fill/thumb
    /// <see cref="Image"/> refs + value <see cref="TMP_Text"/> ref in <c>Awake</c> and writes
    /// palette colors on <see cref="ApplyTheme"/>. Bake-time-attached only (Stage 10 lock).
    /// </summary>
    /// <remarks>TECH-32929 Stage 6.0 — Quarantined alongside ThemedSlider.</remarks>
    [Obsolete("ThemedSliderRenderer quarantined (TECH-32929). Deletion deferred to uGUI purge plan.")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ThemedSlider))]
    public class ThemedSliderRenderer : ThemedPrimitiveBase
    {
        [Header("Token")]
        [SerializeField] private string _paletteSlug;

        [Header("Render targets")]
        [SerializeField] private Image _trackImage;
        [SerializeField] private Image _fillImage;
        [SerializeField] private Image _thumbImage;
        [SerializeField] private TMP_Text _valueText;

        protected override void Awake()
        {
            base.Awake();
        }

        public override void ApplyTheme(UiTheme theme)
        {
            if (theme == null) return;
            if (!theme.TryGetPalette(_paletteSlug, out var ramp) || ramp.ramp == null || ramp.ramp.Length == 0) return;
            if (!ColorUtility.TryParseHtmlString(ramp.ramp[0], out var baseColor)) return;
            var fillColor = ramp.ramp.Length > 1 && ColorUtility.TryParseHtmlString(ramp.ramp[1], out var fc) ? fc : baseColor;
            if (_trackImage != null) _trackImage.color = baseColor;
            if (_fillImage != null) _fillImage.color = fillColor;
            if (_thumbImage != null) _thumbImage.color = baseColor;
        }
    }
}
