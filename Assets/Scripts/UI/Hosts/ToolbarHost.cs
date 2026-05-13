using Territory.UI.Modals;
using Territory.UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// Stage 5.0 (TECH-32923) — MonoBehaviour Host for toolbar HUD panel.
    /// Wires tool selection; stub — wire ToolService in next pass.
    /// </summary>
    public sealed class ToolbarHost : MonoBehaviour
    {
        [SerializeField] UIDocument _doc;

        ToolbarVM _vm;
        ModalCoordinator _coordinator;

        void OnEnable()
        {
            _vm = new ToolbarVM();
            _vm.SelectToolCommand = OnToolSelected;

            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.dataSource = _vm;
            else
                Debug.LogWarning("[ToolbarHost] UIDocument or rootVisualElement null on enable.");

            _coordinator = FindObjectOfType<ModalCoordinator>();
            if (_coordinator != null && _doc != null && _doc.rootVisualElement != null)
                _coordinator.RegisterMigratedPanel("toolbar", _doc.rootVisualElement);
        }

        void OnDisable()
        {
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.dataSource = null;
        }

        void OnToolSelected(string toolId)
        {
            if (_vm == null) return;
            _vm.ActiveTool = toolId;
            Debug.Log($"[ToolbarHost] Tool selected: {toolId} — stub; wire ToolService.");
        }
    }
}
