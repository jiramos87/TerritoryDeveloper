using System;
using UnityEngine;
using UnityEngine.UI;

namespace Territory.UI.Themed
{
    /// <summary>Themed toggle input variant — palette + frame_style token consumer.</summary>
    /// <remarks>TECH-32929 Stage 6.0 — Quarantined; UI Toolkit Toggle with USS class replaces.</remarks>
    [Obsolete("ThemedToggle quarantined (TECH-32929). Use UI Toolkit Toggle with USS class. Deletion deferred to uGUI purge plan.")]
    public class ThemedToggle : ThemedPrimitiveBase
    {
        [SerializeField] private string _paletteSlug;
        [SerializeField] private string _frameStyleSlug;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _checkmarkImage;

        public event System.Action<bool> OnToggled;

        protected override void Awake()
        {
            base.Awake();
            var toggle = GetComponent<Toggle>();
            if (toggle != null) toggle.onValueChanged.AddListener(v => OnToggled?.Invoke(v));
        }

        public override void ApplyTheme(UiTheme theme)
        {
            if (theme == null) return;
            if (_backgroundImage != null
                && theme.TryGetPalette(_paletteSlug, out var ramp)
                && ramp.ramp != null
                && ramp.ramp.Length > 0
                && ColorUtility.TryParseHtmlString(ramp.ramp[0], out var c))
            {
                _backgroundImage.color = c;
            }
            if (_checkmarkImage != null && theme.TryGetFrameStyle(_frameStyleSlug, out _))
            {
                // Checkmark sprite swap deferred — token consumed; sprite source pending Stage 6 catalog.
            }
        }
    }
}
