using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Territory.UI.Themed.Renderers
{
    /// <summary>
    /// Render-layer companion for <see cref="ThemedTabBar"/>; caches active-tab indicator
    /// <see cref="Image"/> + per-tab label <see cref="TMP_Text"/> refs in <c>Awake</c> and
    /// writes palette colors on <see cref="ApplyTheme"/>. Bake-time-attached only (Stage 10 lock).
    /// </summary>
    /// <remarks>TECH-32929 Stage 6.0 — Quarantined alongside ThemedTabBar.</remarks>
    [Obsolete("ThemedTabBarRenderer quarantined (TECH-32929). Deletion deferred to uGUI purge plan.")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ThemedTabBar))]
    public class ThemedTabBarRenderer : ThemedPrimitiveRendererBase
    {
        [Header("Token")]
        [SerializeField] private string _paletteSlug;

        [Header("Render targets")]
        [SerializeField] private Image _activeTabIndicator;
        [SerializeField] private TMP_Text _tabLabel;

        protected override void Awake()
        {
            base.Awake();
        }

        public override void ApplyTheme(UiTheme theme)
        {
            if (theme == null || _activeTabIndicator == null) return;
            if (!theme.TryGetPalette(_paletteSlug, out var ramp) || ramp.ramp == null || ramp.ramp.Length == 0) return;
            if (!ColorUtility.TryParseHtmlString(ramp.ramp[0], out var activeColor)) return;
            _activeTabIndicator.color = activeColor;
            if (_tabLabel != null) _tabLabel.color = activeColor;
        }

        protected override void OnStateApplied()
        {
            // Active-tab tint already follows ApplyTheme; runtime tab switch updates color via state holder hook.
        }
    }
}
