using UnityEngine;
using Territory.UI.Themed;

namespace Territory.UI.Splash
{
    /// <summary>
    /// Bridges <see cref="MainMenuController"/> launch state into baked <c>splash.prefab</c>
    /// Themed SO refs. Mirrors Stage 6 <c>HudBarDataAdapter</c> wiring contract verbatim.
    /// </summary>
    /// <remarks>
    /// Read-only consumer. <see cref="UiTheme"/> Inspector-only (SO; no <c>FindObjectOfType</c>
    /// for SOs per Stage 6 precedent + invariant #3). MonoBehaviour producers fall back to
    /// <see cref="MonoBehaviour.FindObjectOfType{T}()"/> in <see cref="Awake"/> when Inspector
    /// slot empty (invariant #4). No runtime <c>AddComponent</c>.
    /// </remarks>
    public class SplashAdapter : MonoBehaviour
    {
        [Header("Producers")]
        [SerializeField] private MainMenuController _mainMenuController;

        [Header("Theme")]
        [SerializeField] private UiTheme _uiTheme;

        [Header("Consumers")]
        [SerializeField] private ThemedPanel _splashPanel;
        [SerializeField] private ThemedLabel _splashLabel;

        private void Awake()
        {
            // MonoBehaviour producer — Inspector first, FindObjectOfType fallback (invariant #4).
            // Producer typically lives in MainMenu scene; null-tolerant by design.
            if (_mainMenuController == null) _mainMenuController = FindObjectOfType<MainMenuController>();
            // _uiTheme is a ScriptableObject — Inspector-only assignment (Stage 6 precedent).
        }

        private void OnEnable()
        {
            ApplyThemeToConsumers();
        }

        private void OnDisable()
        {
            // No event subscription to tear down — MainMenuController exposes no event surface.
        }

        private void ApplyThemeToConsumers()
        {
            if (_uiTheme == null) return;
            if (_splashPanel != null) _splashPanel.ApplyTheme(_uiTheme);
            if (_splashLabel != null) _splashLabel.ApplyTheme(_uiTheme);
        }
    }
}
