using Territory.UI.Modals;
using Territory.UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// Stage 4.0 (TECH-32920) — MonoBehaviour Host for map-panel UI Toolkit migration.
    /// Wires VM + input pipeline + minimap texture surface.
    /// Registers panel slug in ModalCoordinator migrated branch.
    /// Legacy MapPanelAdapter remains alive until Stage 6 quarantine.
    /// </summary>
    public sealed class MapPanelHost : MonoBehaviour
    {
        [SerializeField] UIDocument _doc;

        MapPanelVM _vm;
        ModalCoordinator _coordinator;

        private const float ZoomStep = 0.25f;
        private const float ZoomMin = 0.25f;
        private const float ZoomMax = 4f;

        void OnEnable()
        {
            _vm = new MapPanelVM();
            WireCommands();

            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.dataSource = _vm;
            else
                Debug.LogWarning("[MapPanelHost] UIDocument or rootVisualElement null on enable — check PanelSettings wiring.");

            _coordinator = FindObjectOfType<ModalCoordinator>();
            if (_coordinator != null && _doc != null && _doc.rootVisualElement != null)
                _coordinator.RegisterMigratedPanel("map-panel", _doc.rootVisualElement);
        }

        void OnDisable()
        {
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.dataSource = null;
        }

        void WireCommands()
        {
            _vm.CloseCommand = OnClose;
            _vm.ZoomInCommand = OnZoomIn;
            _vm.ZoomOutCommand = OnZoomOut;
        }

        void OnClose()
        {
            if (_coordinator != null)
                _coordinator.HideMigrated("map-panel");
            else
                gameObject.SetActive(false);
        }

        void OnZoomIn()
        {
            if (_vm == null) return;
            _vm.Zoom = UnityEngine.Mathf.Clamp(_vm.Zoom + ZoomStep, ZoomMin, ZoomMax);
            Debug.Log($"[MapPanelHost] Zoom in → {_vm.ZoomLabel}");
        }

        void OnZoomOut()
        {
            if (_vm == null) return;
            _vm.Zoom = UnityEngine.Mathf.Clamp(_vm.Zoom - ZoomStep, ZoomMin, ZoomMax);
            Debug.Log($"[MapPanelHost] Zoom out → {_vm.ZoomLabel}");
        }
    }
}
