using System.Collections.Generic;
using Domains.UI.Editor.UiBake.Services;
using Territory.UI;
using Territory.UI.StudioControls;
using Territory.UI.StudioControls.Renderers;
using Territory.UI.Themed;
using Territory.UI.Themed.Renderers;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Domains.UI.Editor.UiBake.Services
{
    /// <summary>StudioControl child instantiation POCO. Extracted from UiBakeHandler.FrameImpl.cs (TECH-31980).
    /// Sub-dispatch on kind string; each archetype is a private case. Constructor takes BakeContext.</summary>
    public class StudioControlBaker
    {
        readonly BakeContext _ctx;

        // ── Known kind roster — mirrors UiBakeHandler._knownKinds ────────────────
        static readonly HashSet<string> _knownKinds = new HashSet<string>
        {
            "knob", "fader", "detent-ring",
            "vu-meter", "oscilloscope",
            "illuminated-button", "led", "segmented-readout",
            "themed-overlay-toggle-row",
            "themed-button", "themed-label", "themed-slider",
            "themed-toggle", "themed-tab-bar", "themed-list",
            "themed-tooltip",
            "view-slot", "confirm-button",
            "card-picker", "chip-picker", "text-input",
            "toggle-row", "slider-row", "dropdown-row", "section-header",
            "save-controls-strip", "save-list",
            "subtype-picker-strip",
            "tab-strip", "chart", "range-tabs", "stacked-bar-row", "service-row",
            "slider-row-numeric", "expense-row", "readout-block",
            "info-dock", "field-list", "minimap-canvas", "toast-stack", "toast-card",
        };

        public StudioControlBaker(BakeContext ctx)
        {
            _ctx = ctx;
        }

        /// <summary>Check if kind is a known StudioControl archetype slug.</summary>
        public static bool IsKnownKind(string kind)
        {
            return !string.IsNullOrEmpty(kind) && _knownKinds.Contains(kind);
        }

        /// <summary>Instantiate a named panel child of the given kind under panelRoot. Returns null for unknown kinds.</summary>
        public GameObject InstantiateChild(
            string kind,
            Transform panelRoot,
            ref int duplicateCounter,
            UiTheme theme,
            string label = null,
            string iconSpriteSlug = null)
        {
            // Delegates to the Bridge entry which owns the full switch body.
            return Territory.Editor.Bridge.UiBakeHandler.InstantiatePanelChild(
                kind, panelRoot, ref duplicateCounter, theme, label, iconSpriteSlug);
        }
    }
}
