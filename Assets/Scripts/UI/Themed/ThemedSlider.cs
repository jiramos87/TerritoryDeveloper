using System;
using UnityEngine;
using UnityEngine.UI;

namespace Territory.UI.Themed
{
    /// <summary>Themed slider input variant — palette + frame_style token consumer (visuals only).</summary>
    /// <remarks>TECH-32929 Stage 6.0 — Quarantined; UI Toolkit Slider with USS class replaces.</remarks>
    [Obsolete("ThemedSlider quarantined (TECH-32929). Use UI Toolkit Slider with USS class. Deletion deferred to uGUI purge plan.")]
    public class ThemedSlider : ThemedPrimitiveBase
    {
        [SerializeField] private string _paletteSlug;
        [SerializeField] private string _frameStyleSlug;
        [SerializeField] private Image _sliderHandleImage;
        [SerializeField] private Image _sliderTrackImage;

        public event System.Action<float> OnValueChanged;

        protected override void Awake()
        {
            base.Awake();
            var slider = GetComponent<Slider>();
            if (slider != null) slider.onValueChanged.AddListener(v => OnValueChanged?.Invoke(v));
        }

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
