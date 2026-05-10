using TMPro;
using Territory.UI;
using Territory.UI.Registry;
using UnityEngine;
using UnityEngine.UI;

namespace Territory.UI.Modals
{
    /// <summary>
    /// Settings sub-view controller. Resolves content-slot via suffix-match walking
    /// (same algorithm as SlotAnchorResolver.ResolveByPanel) instead of a hand-typed scene path.
    /// Apply-time render-check asserts all 12 widgets present + bind-counts non-zero.
    /// Inv #3: resolver runs at mount/apply only, never per-frame.
    /// Stage 4 (TECH-27348) — bake-pipeline-hardening.
    /// </summary>
    public class SettingsViewController : MonoBehaviour
    {
        // Expected widget counts (F3 fixture — settings-view panel spec).
        private const int ExpectedSliderCount   = 3;
        private const int ExpectedToggleCount   = 5;
        private const int ExpectedDropdownCount = 1;
        private const int ExpectedHeaderCount   = 3;
        private const int ExpectedTotalWidgets  = ExpectedSliderCount + ExpectedToggleCount
                                                  + ExpectedDropdownCount + ExpectedHeaderCount; // 12

        [SerializeField] private UiBindRegistry _bindRegistry;

        /// <summary>Resolved slot anchor; null until Apply() called.</summary>
        private Transform _resolvedSlot;

        private void Awake()
        {
            if (_bindRegistry == null)
                _bindRegistry = FindObjectOfType<UiBindRegistry>();
        }

        /// <summary>
        /// Mount settings sub-view into the resolved slot. Resolves slot via
        /// <see cref="SlotAnchorResolver.ResolveByPanel"/> on every call (mount/apply only).
        /// Runs apply-time render-check: 12 widgets present + bind-counts non-zero.
        /// </summary>
        public void Apply()
        {
            // Inv #3 — slot resolution at apply only, never per-frame.
            _resolvedSlot = SlotAnchorResolver.ResolveByPanel("settings", transform);
            if (_resolvedSlot == null)
                _resolvedSlot = SlotAnchorResolver.ResolveByPanel("settings", transform.root);

            if (_resolvedSlot == null)
            {
                Debug.LogWarning("[SettingsViewController] settings slot not found — apply-time render-check skipped.");
                return;
            }

            ApplyTimeRenderCheck(_resolvedSlot);
        }

        /// <summary>
        /// Apply-time render-check: counts Slider, Toggle, TMP_Dropdown, and TMP_Text components
        /// under the resolved slot. Logs warning when counts don't meet spec.
        /// Bind-counts checked via UiBindRegistry subscription count.
        /// </summary>
        internal void ApplyTimeRenderCheck(Transform slot)
        {
            if (slot == null) return;

            var sliders   = slot.GetComponentsInChildren<Slider>(true);
            var toggles   = slot.GetComponentsInChildren<Toggle>(true);
            var dropdowns = slot.GetComponentsInChildren<TMP_Dropdown>(true);
            var headers   = slot.GetComponentsInChildren<TMP_Text>(true);

            int totalWidgets = sliders.Length + toggles.Length + dropdowns.Length + headers.Length;

            if (totalWidgets < ExpectedTotalWidgets)
            {
                Debug.LogWarning(
                    $"[SettingsViewController] render-check: expected {ExpectedTotalWidgets} widgets, " +
                    $"found {totalWidgets} (sliders={sliders.Length} toggles={toggles.Length} " +
                    $"dropdowns={dropdowns.Length} headers={headers.Length}).");
            }

            // Bind-count check — subscriptions mean widgets are wired via SettingsScreenDataAdapter.
            if (_bindRegistry != null)
            {
                var bindIds = new[]
                {
                    "settings.masterVolume",
                    "settings.musicVolume",
                    "settings.sfxVolume",
                    "settings.fullscreen",
                    "settings.vsync",
                    "settings.resolution",
                    "settings.scrollEdgePan",
                    "settings.monthlyBudgetNotifications",
                    "settings.autoSave",
                };
                int boundCount = 0;
                foreach (var id in bindIds)
                    if (_bindRegistry.HasSubscribers(id)) boundCount++;

                if (boundCount == 0)
                    Debug.LogWarning("[SettingsViewController] render-check: no bind subscriptions active — settings widgets may not be wired.");
            }
        }

        /// <summary>Exposed for PlayMode test — counts widget components under a given root.</summary>
        public static WidgetCounts CountWidgets(Transform root)
        {
            if (root == null) return default;
            return new WidgetCounts
            {
                Sliders   = root.GetComponentsInChildren<Slider>(true).Length,
                Toggles   = root.GetComponentsInChildren<Toggle>(true).Length,
                Dropdowns = root.GetComponentsInChildren<TMP_Dropdown>(true).Length,
                Headers   = root.GetComponentsInChildren<TMP_Text>(true).Length,
            };
        }

        /// <summary>Widget count snapshot. Used by apply-time render-check + PlayMode test assertions.</summary>
        public struct WidgetCounts
        {
            public int Sliders;
            public int Toggles;
            public int Dropdowns;
            public int Headers;
            public int Total => Sliders + Toggles + Dropdowns + Headers;
        }

    }
}
