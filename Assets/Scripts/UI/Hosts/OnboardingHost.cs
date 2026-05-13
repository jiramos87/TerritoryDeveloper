using Territory.UI.Modals;
using Territory.UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// Stage 5.0 (TECH-32925) — MonoBehaviour Host for onboarding fullscreen panel.
    /// Wires step flow + skip; stub — wire OnboardingService in next pass.
    /// </summary>
    public sealed class OnboardingHost : MonoBehaviour
    {
        [SerializeField] UIDocument _doc;

        OnboardingVM _vm;
        ModalCoordinator _coordinator;

        void OnEnable()
        {
            _vm = new OnboardingVM();
            WireCommands();

            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(_vm);
            else
                Debug.LogWarning("[OnboardingHost] UIDocument or rootVisualElement null on enable.");
        }

        void OnDisable()
        {
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(null);
        }

        void WireCommands()
        {
            _vm.NextCommand = OnNext;
            _vm.SkipCommand = OnSkip;
        }

        void OnNext()
        {
            if (_vm == null) return;
            if (_vm.CurrentStep < _vm.TotalSteps)
                _vm.CurrentStep++;
            else
                OnSkip();
        }

        void OnSkip()
        {
            if (_coordinator != null)
                _coordinator.HideMigrated("onboarding");
            else
                gameObject.SetActive(false);
            Debug.Log("[OnboardingHost] Onboarding complete/skipped — stub; wire OnboardingService.");
        }
    }
}
