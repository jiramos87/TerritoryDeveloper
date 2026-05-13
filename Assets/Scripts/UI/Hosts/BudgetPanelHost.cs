using Territory.UI.Modals;
using Territory.UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// Stage 4.0 (TECH-32917) — MonoBehaviour Host for budget-panel UI Toolkit migration.
    /// Registers panel slug in ModalCoordinator migrated branch.
    /// Sets UIDocument.rootVisualElement.dataSource = BudgetPanelVM.
    /// Legacy BudgetPanelAdapter remains alive until Stage 6 quarantine.
    /// </summary>
    public sealed class BudgetPanelHost : MonoBehaviour
    {
        [SerializeField] UIDocument _doc;

        BudgetPanelVM _vm;
        ModalCoordinator _coordinator;

        void OnEnable()
        {
            _vm = new BudgetPanelVM();
            WireCommands();

            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(_vm);
            else
                Debug.LogWarning("[BudgetPanelHost] UIDocument or rootVisualElement null on enable — check PanelSettings wiring.");

            _coordinator = FindObjectOfType<ModalCoordinator>();
            if (_coordinator != null && _doc != null && _doc.rootVisualElement != null)
                _coordinator.RegisterMigratedPanel("budget-panel", _doc.rootVisualElement);
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
            _vm.CancelCommand = OnCancel;
        }

        void OnClose()
        {
            if (_coordinator != null)
                _coordinator.HideMigrated("budget-panel");
            else
                gameObject.SetActive(false);
        }

        void OnApply()
        {
            Debug.Log("[BudgetPanelHost] Apply — stub; wire EconomyManager to flush tax rates.");
            OnClose();
        }

        void OnCancel()
        {
            Debug.Log("[BudgetPanelHost] Cancel — discarding pending changes.");
            OnClose();
        }
    }
}
