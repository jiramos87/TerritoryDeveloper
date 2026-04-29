using System;
using UnityEngine;
using Territory.UI.Themed;
using Territory.UI.Themed.Renderers;

namespace Territory.UI.Toolbar
{
    /// <summary>
    /// Bridges <see cref="UIManager"/> overlay-toggle state (Stage 7 — TECH-3235 partial) into baked
    /// <see cref="ThemedOverlayToggleRow"/> consumer slots on the new <c>overlay-toggle-strip</c> prefab.
    /// Toggle clicks call <see cref="UIManager.SetOverlayActive"/>; <see cref="UIManager.OnOverlayStateChanged"/>
    /// event drives row visual sync (event-driven; no per-frame <see cref="MonoBehaviour.Update"/> poll
    /// — overlay state changes only on user interaction).
    /// </summary>
    /// <remarks>
    /// Inspector consumer array <see cref="_overlayToggles"/> length = 5 (one entry per
    /// <see cref="OverlaySlug"/> value, fixed-index by enum integer). Per-channel null tolerance:
    /// missing slots logged once on <see cref="Awake"/>, no NRE on event callback. Producer
    /// (<see cref="UIManager"/>) cached in <see cref="Awake"/> with Inspector-first / FindObjectOfType
    /// fallback (invariants #3 + #4); <see cref="UiTheme"/> SO Inspector-only (Stage 6 precedent).
    /// </remarks>
    public class OverlayToggleDataAdapter : MonoBehaviour
    {
        [Header("Producer")]
        [SerializeField] private UIManager _uiManager;

        [Header("Theme")]
        [SerializeField] private UiTheme _uiTheme;

        /// <summary>Fixed-index by <see cref="OverlaySlug"/> integer. [0]=Terrain, [1]=Pollution, [2]=LandValue, [3]=RoadNetwork, [4]=TrafficFlow.</summary>
        [Header("Consumers — overlay-toggle rows (length 5)")]
        [SerializeField] private ThemedOverlayToggleRowRenderer[] _overlayToggles;

        private bool _subscribed;

        private void Awake()
        {
            if (_uiManager == null) _uiManager = FindObjectOfType<UIManager>();
            // UiTheme is a ScriptableObject — Inspector-only assignment (Stage 6 precedent).

            ValidateConsumerArray();
            SubscribeRowToggles();
            SubscribeOverlayState();
            ApplyInitialVisualState();
        }

        private void OnDestroy()
        {
            UnsubscribeRowToggles();
            UnsubscribeOverlayState();
        }

        private void ValidateConsumerArray()
        {
            int expected = Enum.GetValues(typeof(OverlaySlug)).Length;
            if (_overlayToggles == null || _overlayToggles.Length < expected)
            {
                Debug.LogWarning($"[OverlayToggleDataAdapter] _overlayToggles missing slots — expected {expected}, got {(_overlayToggles == null ? 0 : _overlayToggles.Length)}. Per-channel null tolerance applied.");
            }
        }

        private void SubscribeRowToggles()
        {
            if (_subscribed || _overlayToggles == null) return;
            // Row toggle child is a Unity Toggle — ThemedOverlayToggleRowRenderer subscribes its own
            // visual onValueChanged; adapter taps the same Toggle to drive UIManager state.
            for (int i = 0; i < _overlayToggles.Length; i++)
            {
                int idx = i;
                var renderer = _overlayToggles[i];
                if (renderer == null) continue;
                var toggle = renderer.GetComponentInChildren<UnityEngine.UI.Toggle>(true);
                if (toggle == null) continue;
                toggle.onValueChanged.AddListener(active => OnRowToggleClicked(idx, active));
            }
            _subscribed = true;
        }

        private void UnsubscribeRowToggles()
        {
            if (!_subscribed || _overlayToggles == null) return;
            for (int i = 0; i < _overlayToggles.Length; i++)
            {
                var renderer = _overlayToggles[i];
                if (renderer == null) continue;
                var toggle = renderer.GetComponentInChildren<UnityEngine.UI.Toggle>(true);
                if (toggle != null) toggle.onValueChanged.RemoveAllListeners();
            }
            _subscribed = false;
        }

        private void SubscribeOverlayState()
        {
            if (_uiManager == null) return;
            _uiManager.OnOverlayStateChanged += HandleOverlayStateChanged;
        }

        private void UnsubscribeOverlayState()
        {
            if (_uiManager == null) return;
            _uiManager.OnOverlayStateChanged -= HandleOverlayStateChanged;
        }

        private void OnRowToggleClicked(int index, bool active)
        {
            if (_uiManager == null) return;
            if (!TryIndexToSlug(index, out OverlaySlug slug)) return;
            _uiManager.SetOverlayActive(slug, active);
        }

        private void HandleOverlayStateChanged(OverlaySlug slug, bool active)
        {
            int idx = (int)slug;
            if (_overlayToggles == null || idx < 0 || idx >= _overlayToggles.Length) return;
            var renderer = _overlayToggles[idx];
            if (renderer == null) return;
            // notify=false — avoid feedback loop with row toggle subscription writing back into UIManager.
            renderer.SetIsOn(active, notify: false);
        }

        private void ApplyInitialVisualState()
        {
            if (_uiManager == null || _overlayToggles == null) return;
            var values = (OverlaySlug[])Enum.GetValues(typeof(OverlaySlug));
            for (int i = 0; i < values.Length && i < _overlayToggles.Length; i++)
            {
                var renderer = _overlayToggles[i];
                if (renderer == null) continue;
                renderer.SetIsOn(_uiManager.GetOverlayActive(values[i]), notify: false);
            }
        }

        private static bool TryIndexToSlug(int index, out OverlaySlug slug)
        {
            var values = (OverlaySlug[])Enum.GetValues(typeof(OverlaySlug));
            if (index < 0 || index >= values.Length)
            {
                slug = default;
                return false;
            }
            slug = values[index];
            return true;
        }
    }
}
