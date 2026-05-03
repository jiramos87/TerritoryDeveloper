using System;
using UnityEngine;
using UnityEngine.UI;

namespace Territory.UI.Themed
{
    /// <summary>Themed tab bar chrome variant — palette + frame_style token consumer.</summary>
    public class ThemedTabBar : ThemedPrimitiveBase
    {
        /// <summary>Stage 13.2 — bake-time tab descriptor mirrored from IR v2 `IrTab` (id + label + active).
        /// Runtime adapter (Stage 13.4) consumes <see cref="_pages"/> to drive page swap; bake handler
        /// writes one entry per `panel.tabs[]` element.</summary>
        [Serializable]
        public struct PageBinding
        {
            public string id;
            public string label;
            public bool active;
        }

        [SerializeField] private string _paletteSlug;
        [SerializeField] private string _frameStyleSlug;
        [SerializeField] private Image _tabStripImage;

        /// <summary>Stage 13.2 — bake-time IR v2 tab list. Empty when source panel carries no `tabs[]` block.</summary>
        [SerializeField] private PageBinding[] _pages;

        /// <summary>Stage 13.2 — read-only tab list snapshot for runtime adapters.</summary>
        public PageBinding[] Pages => _pages ?? Array.Empty<PageBinding>();

        public void SetActiveTab(int index) { }

        public override void ApplyTheme(UiTheme theme)
        {
            if (theme == null || _tabStripImage == null) return;
            if (theme.TryGetPalette(_paletteSlug, out var ramp) && ramp.ramp != null && ramp.ramp.Length > 0)
            {
                if (ColorUtility.TryParseHtmlString(ramp.ramp[0], out var c))
                {
                    _tabStripImage.color = c;
                }
            }
            if (theme.TryGetFrameStyle(_frameStyleSlug, out _))
            {
                // Frame edge stylistic refinement deferred — token consumed gracefully.
            }
        }
    }
}
