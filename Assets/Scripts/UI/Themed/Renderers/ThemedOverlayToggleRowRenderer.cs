using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Territory.UI.Themed.Renderers
{
    /// <summary>
    /// Render-layer companion for <see cref="ThemedOverlayToggleRow"/>; resolves child
    /// <see cref="TMP_Text"/> label + <see cref="Image"/> icon + <see cref="ThemedToggle"/>
    /// state references in <c>Awake</c> and writes label text + tint swap on toggle state
    /// changes. Bake-time-attached only (Stage 10 lock); never <c>AddComponent</c>'d at runtime.
    /// </summary>
    /// <remarks>
    /// Cache pattern matches invariant #3 (no per-frame <see cref="MonoBehaviour.FindObjectOfType{T}()"/>
    /// or <see cref="Component.GetComponentInChildren{T}()"/>). Subclasses
    /// <see cref="ThemedPrimitiveBase"/> rather than
    /// <see cref="Territory.UI.StudioControls.Renderers.StudioControlRendererBase"/> because
    /// <see cref="ThemedOverlayToggleRow"/> is a Themed primitive composite (not a
    /// <see cref="Territory.UI.StudioControls.StudioControlBase"/> subclass).
    /// </remarks>
    /// <remarks>TECH-32929 Stage 6.0 — Quarantined alongside ThemedOverlayToggleRow.</remarks>
    [Obsolete("ThemedOverlayToggleRowRenderer quarantined (TECH-32929). Deletion deferred to uGUI purge plan.")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ThemedOverlayToggleRow))]
    public class ThemedOverlayToggleRowRenderer : ThemedPrimitiveBase
    {
        [Header("Render targets")]
        [SerializeField] private TMP_Text _labelText;
        [SerializeField] private Image _iconImage;

        [Header("Initial overlay row data (bake-time written)")]
        [SerializeField] private string _overlayLabel;
        [SerializeField] private Color _activeTint = Color.white;
        [SerializeField] private Color _inactiveTint = new Color(0.55f, 0.55f, 0.55f, 1f);
        [SerializeField] private bool _initialActive;

        private ThemedOverlayToggleRow _row;
        private Toggle _unityToggle;
        private bool _subscribed;

        protected override void Awake()
        {
            base.Awake();
            _row = GetComponent<ThemedOverlayToggleRow>();
            ResolveSiblingToggle();
            ApplyInitialState();
            SubscribeToggle();
        }

        protected virtual void OnDestroy()
        {
            UnsubscribeToggle();
        }

        private void ResolveSiblingToggle()
        {
            // Toggle child of the row primitive — single Unity Toggle expected per row.
            if (_unityToggle != null) return;
            _unityToggle = GetComponentInChildren<Toggle>(true);
        }

        private void ApplyInitialState()
        {
            if (_labelText != null && !string.IsNullOrEmpty(_overlayLabel))
            {
                _labelText.text = _overlayLabel;
            }
            ApplyTint(_initialActive);
            if (_unityToggle != null)
            {
                _unityToggle.SetIsOnWithoutNotify(_initialActive);
            }
        }

        private void SubscribeToggle()
        {
            if (_subscribed || _unityToggle == null) return;
            _unityToggle.onValueChanged.AddListener(OnToggleValueChanged);
            _subscribed = true;
        }

        private void UnsubscribeToggle()
        {
            if (!_subscribed || _unityToggle == null) return;
            _unityToggle.onValueChanged.RemoveListener(OnToggleValueChanged);
            _subscribed = false;
        }

        private void OnToggleValueChanged(bool isOn)
        {
            ApplyTint(isOn);
        }

        private void ApplyTint(bool isOn)
        {
            if (_iconImage == null) return;
            _iconImage.color = isOn ? _activeTint : _inactiveTint;
        }

        /// <summary>Bake-time configuration entry point — writes label text + initial state into Inspector cache.</summary>
        public void Configure(string overlayLabel, bool initialActive)
        {
            _overlayLabel = overlayLabel;
            _initialActive = initialActive;
            if (_labelText != null && !string.IsNullOrEmpty(overlayLabel))
            {
                _labelText.text = overlayLabel;
            }
            ApplyTint(initialActive);
            if (_unityToggle != null)
            {
                _unityToggle.SetIsOnWithoutNotify(initialActive);
            }
        }

        /// <summary>Public read-only accessor for adapter consumers (TECH-3235).</summary>
        public bool IsOn => _unityToggle != null && _unityToggle.isOn;

        /// <summary>Adapter write path (TECH-3235 save-load restore + external state sync).</summary>
        public void SetIsOn(bool value, bool notify)
        {
            if (_unityToggle == null) return;
            if (notify)
            {
                _unityToggle.isOn = value;
            }
            else
            {
                _unityToggle.SetIsOnWithoutNotify(value);
                ApplyTint(value);
            }
        }
    }
}
