using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Territory.UI.Themed.Renderers
{
    /// <summary>
    /// Render-layer companion for <see cref="ThemedList"/>; caches row Image + row TMP_Text refs
    /// in <c>Awake</c> and writes palette colors on <see cref="ApplyTheme"/>. Per-row child layout
    /// is bake-time injected (vertical stack); runtime <c>Populate</c> stays on <see cref="ThemedList"/>.
    /// Bake-time-attached only (Stage 9 lock).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ThemedList))]
    public class ThemedListRenderer : ThemedPrimitiveRendererBase
    {
        [Header("Token")]
        [SerializeField] private string _paletteSlug;

        [Header("Render targets")]
        [SerializeField] private Image _rowBackground;
        [SerializeField] private TMP_Text _rowLabel;

        protected override void Awake()
        {
            base.Awake();
        }

        public override void ApplyTheme(UiTheme theme)
        {
            if (theme == null) return;
            if (!theme.TryGetPalette(_paletteSlug, out var ramp) || ramp.ramp == null || ramp.ramp.Length == 0) return;
            if (!ColorUtility.TryParseHtmlString(ramp.ramp[0], out var color)) return;
            if (_rowBackground != null) _rowBackground.color = color;
            if (_rowLabel != null) _rowLabel.color = color;
        }

        protected override void OnStateApplied()
        {
            // Row state changes (selection / hover) are owned by the per-row primitives at runtime; no-op.
        }
    }
}
