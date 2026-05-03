using UnityEngine;
using Territory.UI.Themed;

namespace Territory.UI.Onboarding
{
    /// <summary>
    /// First-launch onboarding flow controller bridging <see cref="PlayerPrefs"/>
    /// <c>onboarding-complete</c> flag to baked <c>onboarding-overlay.prefab</c>
    /// Themed SO refs. Mirrors Stage 6 <c>HudBarDataAdapter</c> wiring contract.
    /// </summary>
    /// <remarks>
    /// Read-only consumer. <see cref="UiTheme"/> Inspector-only (SO; no <c>FindObjectOfType</c>
    /// for SOs per invariant #3). Step transitions follow consume + dismiss precedent —
    /// <see cref="MarkOnboardingComplete"/> flips the persisted flag exactly once.
    /// No runtime <c>AddComponent</c>.
    /// </remarks>
    public class OnboardingAdapter : MonoBehaviour
    {
        /// <summary>PlayerPrefs key persisting first-launch consume flag.</summary>
        public const string OnboardingCompleteKey = "onboarding-complete";

        [Header("Theme")]
        [SerializeField] private UiTheme _uiTheme;

        [Header("Consumers")]
        [SerializeField] private ThemedPanel _overlayPanel;
        [SerializeField] private ThemedLabel _stepLabel;
        [SerializeField] private ThemedButton _dismissButton;

        private void Awake()
        {
            // _uiTheme is a ScriptableObject — Inspector-only assignment (invariant #3 cache contract).
            // No MonoBehaviour producer; PlayerPrefs is the persistence layer.
        }

        private void OnEnable()
        {
            ApplyThemeToConsumers();
            // Visibility gated on flag — null-tolerant. Operator binds visibility at scene level.
            if (IsOnboardingComplete())
            {
                gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            // No event subscription to tear down.
        }

        /// <summary>Reads the persisted onboarding-complete flag. Default <c>false</c> on fresh install.</summary>
        public bool IsOnboardingComplete()
        {
            return PlayerPrefs.GetInt(OnboardingCompleteKey, 0) == 1;
        }

        /// <summary>Flips the persisted onboarding-complete flag and dismisses the overlay.</summary>
        public void MarkOnboardingComplete()
        {
            PlayerPrefs.SetInt(OnboardingCompleteKey, 1);
            PlayerPrefs.Save();
            gameObject.SetActive(false);
        }

        private void ApplyThemeToConsumers()
        {
            if (_uiTheme == null) return;
            if (_overlayPanel != null) _overlayPanel.ApplyTheme(_uiTheme);
            if (_stepLabel != null) _stepLabel.ApplyTheme(_uiTheme);
            if (_dismissButton != null) _dismissButton.ApplyTheme(_uiTheme);
        }
    }
}
