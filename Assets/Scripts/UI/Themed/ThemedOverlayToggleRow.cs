using System;
using UnityEngine;

namespace Territory.UI.Themed
{
    /// <summary>Composite row primitive — toggle + label + icon; cascades <see cref="UiTheme"/> to children.</summary>
    /// <remarks>TECH-32929 Stage 6.0 — Quarantined; UXML overlay-toggle-row replaces.</remarks>
    [Obsolete("ThemedOverlayToggleRow quarantined (TECH-32929). Use UXML overlay-toggle-row pattern. Deletion deferred to uGUI purge plan.")]
    public class ThemedOverlayToggleRow : ThemedPrimitiveBase
    {
        [SerializeField] private ThemedToggle _toggle;
        [SerializeField] private ThemedLabel _label;
        [SerializeField] private ThemedIcon _icon;

        public override void ApplyTheme(UiTheme theme)
        {
            if (theme == null) return;
            // Cascade — explicit per-child null guard surfaces miswiring fast.
            if (_toggle != null) _toggle.ApplyTheme(theme);
            if (_label != null) _label.ApplyTheme(theme);
            if (_icon != null) _icon.ApplyTheme(theme);
        }
    }
}
