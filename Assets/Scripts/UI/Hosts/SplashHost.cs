using Territory.UI.Modals;
using Territory.UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// Stage 5.0 (TECH-32925) — MonoBehaviour Host for splash fullscreen panel.
    /// Shows studio logo + game title on boot.
    /// </summary>
    public sealed class SplashHost : MonoBehaviour
    {
        [SerializeField] UIDocument _doc;
        [SerializeField] string _studioLabel = "BACAYO STUDIO";
        [SerializeField] string _gameTitle = "Territory";

        SplashVM _vm;
        ModalCoordinator _coordinator;

        void OnEnable()
        {
            _vm = new SplashVM
            {
                StudioLabel = _studioLabel,
                GameTitle = _gameTitle,
                VersionLabel = $"v{Application.version}"
            };

            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.dataSource = _vm;
            else
                Debug.LogWarning("[SplashHost] UIDocument or rootVisualElement null on enable.");

            _coordinator = FindObjectOfType<ModalCoordinator>();
            if (_coordinator != null && _doc != null && _doc.rootVisualElement != null)
                _coordinator.RegisterMigratedPanel("splash", _doc.rootVisualElement);
        }

        void OnDisable()
        {
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.dataSource = null;
        }
    }
}
