using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Territory.UI.Themed.Renderers
{
    /// <summary>
    /// Render-layer companion for <see cref="ThemedTooltip"/>; caches body label + arrow image
    /// refs in <c>Awake</c> and writes tooltip palette colors on <see cref="ApplyTheme"/>.
    /// Bake-time-attached only (Stage 9 lock).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ThemedTooltip))]
    public class ThemedTooltipRenderer : ThemedPrimitiveRendererBase
    {
        [Header("Token")]
        [SerializeField] private string _paletteSlug;

        [Header("Render targets")]
        [SerializeField] private Image _arrowImage;
        [SerializeField] private TMP_Text _bodyLabel;

        protected override void Awake()
        {
            base.Awake();
        }

        public override void ApplyTheme(UiTheme theme)
        {
            if (theme == null) return;
            if (!theme.TryGetPalette(_paletteSlug, out var ramp) || ramp.ramp == null || ramp.ramp.Length == 0) return;
            if (!ColorUtility.TryParseHtmlString(ramp.ramp[0], out var color)) return;
            if (_arrowImage != null) _arrowImage.color = color;
            if (_bodyLabel != null) _bodyLabel.color = color;
        }

        protected override void OnStateApplied()
        {
            // Tooltip body has no per-state tint variation; no-op.
        }
    }
}
