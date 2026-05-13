using Territory.UI.Modals;
using Territory.UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// Stage 4.0 (TECH-32918) — MonoBehaviour Host for info-panel UI Toolkit migration.
    /// Wires selection-event subscription to InfoPanelVM.
    /// Registers panel slug in ModalCoordinator migrated branch.
    /// Legacy InfoPanelAdapter remains alive until Stage 6 quarantine.
    /// </summary>
    public sealed class InfoPanelHost : MonoBehaviour
    {
        [SerializeField] UIDocument _doc;

        InfoPanelVM _vm;
        ModalCoordinator _coordinator;

        void OnEnable()
        {
            _vm = new InfoPanelVM();
            WireCommands();

            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(_vm);
            else
                Debug.LogWarning("[InfoPanelHost] UIDocument or rootVisualElement null on enable — check PanelSettings wiring.");

            _coordinator = FindObjectOfType<ModalCoordinator>();
            if (_coordinator != null && _doc != null && _doc.rootVisualElement != null)
                _coordinator.RegisterMigratedPanel("info-panel", _doc.rootVisualElement);
        }

        void OnDisable()
        {
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(null);
        }

        void WireCommands()
        {
            _vm.CloseCommand = OnClose;
            _vm.DemolishCommand = OnDemolish;
        }

        /// <summary>Called by selection event sources (e.g. GridManager world-select) to populate VM.</summary>
        public void ShowForSelection(int x, int y, string entityType, string fieldsSummary = "")
        {
            _vm?.SetSelection(x, y, entityType, fieldsSummary);
            if (_coordinator != null)
                _coordinator.Show("info-panel");
        }

        void OnClose()
        {
            if (_coordinator != null)
                _coordinator.HideMigrated("info-panel");
            else
                gameObject.SetActive(false);
        }

        void OnDemolish()
        {
            Debug.Log("[InfoPanelHost] Demolish requested — stub; wire GridManager.DemolishAt.");
            OnClose();
        }
    }
}
