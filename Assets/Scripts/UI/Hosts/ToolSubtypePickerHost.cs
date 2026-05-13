using Territory.UI.Modals;
using Territory.UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// Stage 4.0 (TECH-32921) — MonoBehaviour Host for tool-subtype-picker UI Toolkit migration.
    /// Wires VM, pushes selection back into ToolService (stub — wire ToolService in next pass).
    /// Registers panel slug in ModalCoordinator migrated branch.
    /// Legacy SubtypePickerController remains alive until Stage 6 quarantine.
    /// </summary>
    public sealed class ToolSubtypePickerHost : MonoBehaviour
    {
        [SerializeField] UIDocument _doc;

        ToolSubtypePickerVM _vm;
        ModalCoordinator _coordinator;

        void OnEnable()
        {
            _vm = new ToolSubtypePickerVM();
            WireCommands();

            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(_vm);
            else
                Debug.LogWarning("[ToolSubtypePickerHost] UIDocument or rootVisualElement null on enable — check PanelSettings wiring.");

            _coordinator = FindObjectOfType<ModalCoordinator>();
            if (_coordinator != null && _doc != null && _doc.rootVisualElement != null)
                _coordinator.RegisterMigratedPanel("tool-subtype-picker", _doc.rootVisualElement);
        }

        void OnDisable()
        {
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(null);
        }

        void WireCommands()
        {
            _vm.CloseCommand = OnClose;
            _vm.SelectCommand = OnSubtypeSelected;
        }

        /// <summary>
        /// Show the picker for a given tool family; populate subtype list.
        /// Called by UIManager / toolbar when user activates a tool family.
        /// </summary>
        public void ShowForFamily(string familyLabel, System.Collections.Generic.List<ToolSubtypePickerVM.ToolSubtype> subtypes)
        {
            if (_vm == null) return;
            _vm.Title = $"Select {familyLabel}";
            _vm.Subtypes = subtypes;
            _vm.SelectedId = -1;

            if (_coordinator != null)
                _coordinator.Show("tool-subtype-picker");
        }

        void OnClose()
        {
            if (_coordinator != null)
                _coordinator.HideMigrated("tool-subtype-picker");
            else
                gameObject.SetActive(false);
        }

        void OnSubtypeSelected(int subtypeId)
        {
            if (_vm != null)
                _vm.SelectedId = subtypeId;
            // Push selection back to ToolService — stub; wire ToolService here.
            Debug.Log($"[ToolSubtypePickerHost] Subtype selected: id={subtypeId} — stub; wire ToolService.");
            OnClose();
        }
    }
}
