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
            /// <summary>Stage 13.3 — optional icon slug; bake handler wires a <see cref="ThemedIcon"/>
            /// child per tab when non-empty. Empty/null tabs render label-only.</summary>
            public string iconSlug;
        }

        [SerializeField] private string _paletteSlug;
        [SerializeField] private string _frameStyleSlug;
        [SerializeField] private Image _tabStripImage;

        /// <summary>Stage 13.2 — bake-time IR v2 tab list. Empty when source panel carries no `tabs[]` block.</summary>
        [SerializeField] private PageBinding[] _pages;

        /// <summary>Stage 13.2 — read-only tab list snapshot for runtime adapters.</summary>
        public PageBinding[] Pages => _pages ?? Array.Empty<PageBinding>();

        /// <summary>Stage 13.4 — bake-time per-tab page roots; bake handler assigns one entry per <see cref="_pages"/> slot.
        /// <see cref="SetActiveTab"/> toggles <c>SetActive(i == idx)</c> per element.</summary>
        [SerializeField] private GameObject[] _pageRoots;

        /// <summary>Stage 13.4 — bake-time per-tab indicator highlights (e.g. illumination Image on each tab cell).
        /// Optional: when null/empty, indicator snap is skipped silently.</summary>
        [SerializeField] private GameObject[] _tabIndicators;

        /// <summary>Stage 13.4 (TECH-9867) — bake-baked initial active tab index applied at <see cref="OnEnable"/>.
        /// Sourced from IR `panel.defaultTabIndex` (0 default; city-stats-handoff override = Infrastructure idx
        /// per D1). When out of range, OnEnable falls back silently (handled by SetActiveTab range guard).</summary>
        [SerializeField] private int _initialIndex = 0;

        private int _activeIndex = -1;

        /// <summary>Stage 13.4 — read-only active tab index for downstream tab-aware adapters.</summary>
        public int ActiveIndex => _activeIndex;

        /// <summary>Stage 13.4 — fires after <see cref="SetActiveTab"/> changes the active page (idempotent calls are silent).</summary>
        public event Action<int> OnActiveTabChanged;

        /// <summary>Stage 13.4 (TECH-9867) — re-apply <see cref="_initialIndex"/> on every panel open
        /// (D1 default-tab override always wins on re-open; no last-active preservation). Skipped silently
        /// when no page roots present (tabless panel) or _initialIndex out of range.</summary>
        protected virtual void OnEnable()
        {
            // Reset _activeIndex so re-open snaps back to default (D1: city-stats-handoff opens on Infrastructure).
            _activeIndex = -1;
            SetActiveTab(_initialIndex);
        }

        public void SetActiveTab(int index)
        {
            var pages = _pageRoots ?? Array.Empty<GameObject>();
            if (index < 0 || index >= pages.Length)
            {
                Debug.LogWarning($"ThemedTabBar.SetActiveTab: index {index} out of range [0,{pages.Length}) — no-op.");
                return;
            }
            if (_activeIndex == index) return;

            for (var i = 0; i < pages.Length; i++)
            {
                if (pages[i] != null) pages[i].SetActive(i == index);
            }

            var indicators = _tabIndicators;
            if (indicators != null)
            {
                for (var i = 0; i < indicators.Length; i++)
                {
                    if (indicators[i] != null) indicators[i].SetActive(i == index);
                }
            }

            _activeIndex = index;
            OnActiveTabChanged?.Invoke(index);
        }

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
