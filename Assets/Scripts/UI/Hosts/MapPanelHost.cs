using Territory.UI.Modals;
using Territory.UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// Stage 4.0 (TECH-32920) — MonoBehaviour Host for map-panel UI Toolkit migration.
    /// Effort 6 (post iter-29): wires minimap-surface backgroundImage from
    /// MiniMapController.MapTexture; layer toggles drive MiniMapController.SetActiveLayers.
    /// </summary>
    public sealed class MapPanelHost : MonoBehaviour
    {
        [SerializeField] UIDocument _doc;
        [SerializeField] MiniMapController _miniMapController;

        MapPanelVM _vm;
        ModalCoordinator _coordinator;
        VisualElement _minimapSurface;
        Toggle _toggleTerrain;
        Toggle _toggleZones;
        Toggle _toggleRoads;
        int _refreshFrame;

        private const float ZoomStep = 0.25f;
        private const float ZoomMin = 0.25f;
        private const float ZoomMax = 4f;

        void OnEnable()
        {
            _vm = new MapPanelVM();
            WireCommands();

            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(_vm);
            else
                Debug.LogWarning("[MapPanelHost] UIDocument or rootVisualElement null on enable — check PanelSettings wiring.");

            _coordinator = FindObjectOfType<ModalCoordinator>();
            if (_coordinator != null && _doc != null && _doc.rootVisualElement != null)
                _coordinator.RegisterMigratedPanel("map-panel", _doc.rootVisualElement);

            if (_miniMapController == null) _miniMapController = FindObjectOfType<MiniMapController>();

            if (_doc != null && _doc.rootVisualElement != null)
            {
                var root = _doc.rootVisualElement;
                _minimapSurface = root.Q<VisualElement>("minimap-surface");
                _toggleTerrain = root.Q<Toggle>("toggle-terrain");
                _toggleZones = root.Q<Toggle>("toggle-zones");
                _toggleRoads = root.Q<Toggle>("toggle-roads");
                if (_toggleTerrain != null) _toggleTerrain.RegisterValueChangedCallback(OnLayerChanged);
                if (_toggleZones != null) _toggleZones.RegisterValueChangedCallback(OnLayerChanged);
                if (_toggleRoads != null) _toggleRoads.RegisterValueChangedCallback(OnLayerChanged);

                if (_miniMapController != null)
                {
                    var active = _miniMapController.GetActiveLayers();
                    if (_toggleZones != null) _toggleZones.SetValueWithoutNotify((active & MiniMapLayer.Zones) != 0);
                    if (_toggleRoads != null) _toggleRoads.SetValueWithoutNotify((active & MiniMapLayer.Streets) != 0);
                    if (_toggleTerrain != null) _toggleTerrain.SetValueWithoutNotify((active & MiniMapLayer.Desirability) != 0);
                }
            }
        }

        void Update()
        {
            if (_minimapSurface == null || _miniMapController == null) return;
            _refreshFrame++;
            if (_refreshFrame < 30) return;
            _refreshFrame = 0;
            _miniMapController.RebuildTexture();
            var tex = _miniMapController.MapTexture;
            if (tex != null) _minimapSurface.style.backgroundImage = new StyleBackground(tex);
        }

        void OnLayerChanged(ChangeEvent<bool> _)
        {
            if (_miniMapController == null) return;
            MiniMapLayer layers = MiniMapLayer.None;
            if (_toggleZones != null && _toggleZones.value)    layers |= MiniMapLayer.Zones;
            if (_toggleRoads != null && _toggleRoads.value)    layers |= MiniMapLayer.Streets;
            if (_toggleTerrain != null && _toggleTerrain.value) layers |= MiniMapLayer.Desirability;
            _miniMapController.SetActiveLayers(layers);
            var tex = _miniMapController.MapTexture;
            if (_minimapSurface != null && tex != null) _minimapSurface.style.backgroundImage = new StyleBackground(tex);
        }

        void OnDisable()
        {
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(null);
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
