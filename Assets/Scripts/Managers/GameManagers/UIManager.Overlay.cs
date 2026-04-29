using System;
using System.Collections.Generic;
using Territory.UI.Toolbar;

namespace Territory.UI
{
    /// <summary>
    /// Overlay-toggle state surface (Stage 7 — TECH-3235). Single-manager state pattern matches
    /// existing <see cref="UIManager"/> partials (<c>UIManager.Toolbar.cs</c>, <c>UIManager.Hud.cs</c>);
    /// avoids new boot-order Manager class. Save-load round-trip via
    /// <see cref="GameSaveData.overlayActive"/> append-only field (no schema bump — empty list on legacy
    /// saves migrates to all-defaults at <see cref="LoadOverlayStateFromSaveData"/> boundary).
    /// </summary>
    public partial class UIManager
    {
        // ── Active-state map (default = all inactive) ──
        private readonly Dictionary<OverlaySlug, bool> _overlayState = new Dictionary<OverlaySlug, bool>
        {
            { OverlaySlug.Terrain, false },
            { OverlaySlug.Pollution, false },
            { OverlaySlug.LandValue, false },
            { OverlaySlug.RoadNetwork, false },
            { OverlaySlug.TrafficFlow, false },
        };

        /// <summary>Fired after <see cref="SetOverlayActive"/> writes a new value (only on change).</summary>
        public event Action<OverlaySlug, bool> OnOverlayStateChanged;

        /// <summary>Read current overlay-active state for the given slug. Defaults to <c>false</c> for unmapped slugs.</summary>
        public bool GetOverlayActive(OverlaySlug slug)
        {
            return _overlayState.TryGetValue(slug, out bool isActive) && isActive;
        }

        /// <summary>Write overlay-active state. No-op when value unchanged (event fires only on flip).</summary>
        public void SetOverlayActive(OverlaySlug slug, bool active)
        {
            bool prev = GetOverlayActive(slug);
            _overlayState[slug] = active;
            if (prev != active)
            {
                OnOverlayStateChanged?.Invoke(slug, active);
            }
        }

        /// <summary>
        /// Capture overlay-active state into a fixed-length <see cref="List{T}"/> ordered by
        /// <see cref="OverlaySlug"/> integer value. Save-side hook called from
        /// <see cref="GameSaveManager.BuildCurrentGameSaveData"/> (TECH-3235).
        /// </summary>
        public List<bool> CaptureOverlayActiveForSave()
        {
            var values = (OverlaySlug[])Enum.GetValues(typeof(OverlaySlug));
            var list = new List<bool>(values.Length);
            for (int i = 0; i < values.Length; i++)
            {
                list.Add(GetOverlayActive(values[i]));
            }
            return list;
        }

        /// <summary>
        /// Restore overlay-active state from a save list. Empty / null list → all defaults (false).
        /// Slugs missing from the list (legacy / shorter saves) keep their default. Extra entries
        /// past the enum length are ignored (forward-compat).
        /// </summary>
        public void LoadOverlayStateFromSaveData(List<bool> saved)
        {
            var values = (OverlaySlug[])Enum.GetValues(typeof(OverlaySlug));
            int loaded = saved != null ? saved.Count : 0;
            for (int i = 0; i < values.Length; i++)
            {
                bool active = (i < loaded) && saved[i];
                SetOverlayActive(values[i], active);
            }
        }
    }
}
