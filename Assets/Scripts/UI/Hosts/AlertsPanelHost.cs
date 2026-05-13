using Territory.UI.Modals;
using Territory.UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// Stage 5.0 (TECH-32924) — MonoBehaviour Host for alerts-panel modal.
    /// Wires alert list + clear; stub — wire AlertService in next pass.
    /// </summary>
    public sealed class AlertsPanelHost : MonoBehaviour
    {
        [SerializeField] UIDocument _doc;

        AlertsPanelVM _vm;
        ModalCoordinator _coordinator;

        void OnEnable()
        {
            _vm = new AlertsPanelVM();
            WireCommands();

            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.dataSource = _vm;
            else
                Debug.LogWarning("[AlertsPanelHost] UIDocument or rootVisualElement null on enable.");

            _coordinator = FindObjectOfType<ModalCoordinator>();
            if (_coordinator != null && _doc != null && _doc.rootVisualElement != null)
                _coordinator.RegisterMigratedPanel("alerts-panel", _doc.rootVisualElement);
        }

        void OnDisable()
        {
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.dataSource = null;
        }

        void WireCommands()
        {
            _vm.CloseCommand = OnClose;
            _vm.ClearAllCommand = OnClearAll;
        }

        void OnClose()
        {
            if (_coordinator != null)
                _coordinator.HideMigrated("alerts-panel");
            else
                gameObject.SetActive(false);
        }

        void OnClearAll()
        {
            if (_vm != null)
                _vm.Alerts = new System.Collections.Generic.List<AlertsPanelVM.Alert>();
            Debug.Log("[AlertsPanelHost] Alerts cleared — stub; wire AlertService.");
        }
    }
}
