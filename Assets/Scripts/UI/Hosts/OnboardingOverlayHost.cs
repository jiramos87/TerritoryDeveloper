using Territory.UI.Modals;
using Territory.UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// Stage 5.0 (TECH-32924) — MonoBehaviour Host for onboarding-overlay modal.
    /// Wires step progression + skip; stub — wire OnboardingService in next pass.
    /// </summary>
    public sealed class OnboardingOverlayHost : MonoBehaviour
    {
        [SerializeField] UIDocument _doc;

        OnboardingOverlayVM _vm;
        ModalCoordinator _coordinator;

        void OnEnable()
        {
            _vm = new OnboardingOverlayVM();
            WireCommands();

            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(_vm);
            else
                Debug.LogWarning("[OnboardingOverlayHost] UIDocument or rootVisualElement null on enable.");
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
            Debug.Log($"[OnboardingOverlayHost] Step {_vm.CurrentStep}/{_vm.TotalSteps} — stub; wire OnboardingService.");
        }

        void OnSkip()
        {
            if (_coordinator != null)
                _coordinator.HideMigrated("onboarding-overlay");
            else
                gameObject.SetActive(false);
        }
    }
}
