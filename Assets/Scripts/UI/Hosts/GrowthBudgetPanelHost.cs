using Territory.UI.Modals;
using Territory.UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// Stage 5.0 (TECH-32925) — MonoBehaviour Host for growth-budget-panel modal.
    /// Wires budget sliders + apply; stub — wire EconomyManager in next pass.
    /// </summary>
    public sealed class GrowthBudgetPanelHost : MonoBehaviour
    {
        [SerializeField] UIDocument _doc;

        GrowthBudgetPanelVM _vm;
        ModalCoordinator _coordinator;

        void OnEnable()
        {
            _vm = new GrowthBudgetPanelVM();
            WireCommands();

            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(_vm);
            else
                Debug.LogWarning("[GrowthBudgetPanelHost] UIDocument or rootVisualElement null on enable.");

            _coordinator = FindObjectOfType<ModalCoordinator>();
            if (_coordinator != null && _doc != null && _doc.rootVisualElement != null)
                _coordinator.RegisterMigratedPanel("growth-budget-panel", _doc.rootVisualElement);
        }

        void OnDisable()
        {
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(null);
        }

        void WireCommands()
        {
            _vm.CloseCommand = OnClose;
            _vm.ApplyCommand = OnApply;
            _vm.CancelCommand = OnClose;
        }

        void OnClose()
        {
            if (_coordinator != null)
                _coordinator.HideMigrated("growth-budget-panel");
            else
                gameObject.SetActive(false);
        }

        void OnApply()
        {
            Debug.Log($"[GrowthBudgetPanelHost] Apply: infra={_vm?.InfrastructureBudget} services={_vm?.ServicesBudget} — stub; wire EconomyManager.");
            OnClose();
        }
    }
}
