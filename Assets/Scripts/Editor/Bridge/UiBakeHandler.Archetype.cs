using System.Collections.Generic;
using Territory.UI;
using Territory.UI.Themed;
using UnityEngine;

namespace Territory.Editor.Bridge
{
    // ── StudioControl interactive bake (Stage 4 T4.5) ───────────────────────
    // Implementation extracted to UiBakeHandler.ArchetypeImpl.cs (Stage 6.0 Tier-D).
    public static partial class UiBakeHandler
    {
        /// <summary>Known StudioControl kind slugs — Stage 4 T4.5 archetype roster.</summary>
        static readonly HashSet<string> _knownKinds = new HashSet<string>
        {
            "knob", "fader", "detent-ring",
            "vu-meter", "oscilloscope",
            "illuminated-button", "led", "segmented-readout",
            "themed-overlay-toggle-row",
            // Stage 8 Themed* modal primitive kinds.
            "themed-button", "themed-label", "themed-slider",
            "themed-toggle", "themed-tab-bar", "themed-list",
            // Stage 9 (game-ui-design-system) Themed* tooltip kind.
            "themed-tooltip",
            // Wave A1 (TECH-27064) archetypes.
            "view-slot", "confirm-button",
            // Wave A2 (TECH-27069) form + settings archetypes.
            "card-picker", "chip-picker", "text-input",
            "toggle-row", "slider-row", "dropdown-row", "section-header",
            // Wave A3 (TECH-27074) save-load-view archetypes.
            "save-controls-strip", "save-list",
            // Wave B1 (TECH-27079) subtype-picker-strip archetype.
            "subtype-picker-strip",
            // Wave B2 (TECH-27083) stats-panel archetypes.
            "tab-strip", "chart", "range-tabs", "stacked-bar-row", "service-row",
            // Wave B3 (TECH-27088) budget-panel archetypes.
            "slider-row-numeric", "expense-row", "readout-block",
            // Wave B5 (TECH-27098) HUD widget archetypes.
            "info-dock", "field-list", "minimap-canvas", "toast-stack", "toast-card",
        };

        public static bool IsKnownStudioControlKind(string kind)
        {
            return !string.IsNullOrEmpty(kind) && _knownKinds.Contains(kind);
        }

        /// <summary>Map IR <c>panel.kind</c> string to <see cref="PanelKind"/> enum index. Default = Modal (0).</summary>
        public static int ResolvePanelKindIndex(string kind)
        {
            if (string.IsNullOrEmpty(kind)) return (int)PanelKind.Modal;
            switch (kind)
            {
                case "modal": return (int)PanelKind.Modal;
                case "screen": return (int)PanelKind.Screen;
                case "hud": return (int)PanelKind.Hud;
                case "toolbar": return (int)PanelKind.Toolbar;
                case "side-rail":
                case "side_rail":
                case "sideRail": return (int)PanelKind.SideRail;
                default:
                    Debug.LogWarning($"[UiBakeHandler] panel.kind '{kind}' unknown — defaulting to modal");
                    return (int)PanelKind.Modal;
            }
        }
    }
}
