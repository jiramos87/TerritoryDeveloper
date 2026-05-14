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
        VisualElement _runtimePanel;        // programmatic map-panel VE when scene UIDoc absent
        VisualElement _minimapSurface;
        Toggle _toggleTerrain;
        Toggle _toggleZones;
        Toggle _toggleRoads;
        int _refreshFrame;

        private const float ZoomStep = 0.25f;
        private const float ZoomMin = 0.25f;
        private const float ZoomMax = 4f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            var sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (sceneName != "CityScene") return;
            if (FindObjectOfType<MapPanelHost>() != null) { Debug.Log("[MapPanelHost] Bootstrap — already in scene, skip"); return; }
            var go = new GameObject("MapPanelHost-Runtime");
            go.AddComponent<MapPanelHost>();
            Debug.Log("[MapPanelHost] Bootstrap — spawned runtime instance (scene UIDoc was missing)");
        }

        void OnEnable()
        {
            Debug.Log($"[MapPanelHost] OnEnable on GameObject={name} _doc={(_doc!=null?"yes":"NULL")}");
            _vm = new MapPanelVM();
            WireCommands();

            if (_doc != null && _doc.rootVisualElement != null)
            {
                var rootEl = _doc.rootVisualElement;
                // iter-38 fix — root must be viewport-sized for .map-panel's bottom/right anchors
                // to resolve correctly. pickingMode=Ignore so the empty root never blocks the world.
                rootEl.style.position = Position.Absolute;
                rootEl.style.top = 0; rootEl.style.left = 0;
                rootEl.style.right = 0; rootEl.style.bottom = 0;
                rootEl.pickingMode = PickingMode.Ignore;
                rootEl.SetCompatDataSource(_vm);
            }
            else
                Debug.LogWarning("[MapPanelHost] UIDocument or rootVisualElement null on enable — check PanelSettings wiring.");

            _coordinator = FindObjectOfType<ModalCoordinator>();
            Debug.Log($"[MapPanelHost] coordinator={(_coordinator!=null?"found":"NULL")} doc.root={(_doc!=null && _doc.rootVisualElement!=null?"present":"NULL")}");

            // Programmatic fallback when no scene UIDoc is wired for map-panel.
            if (_doc == null || _doc.rootVisualElement == null)
            {
                BuildRuntimePanel();
            }

            if (_coordinator != null && _doc != null && _doc.rootVisualElement != null)
            {
                _coordinator.RegisterMigratedPanel("map-panel", _doc.rootVisualElement);
                Debug.Log("[MapPanelHost] map-panel slug registered with ModalCoordinator (scene UIDoc path)");
            }
            else if (_coordinator != null && _runtimePanel != null)
            {
                _coordinator.RegisterMigratedPanel("map-panel", _runtimePanel);
                Debug.Log("[MapPanelHost] map-panel slug registered with ModalCoordinator (runtime VE path)");
            }

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

        void BuildRuntimePanel()
        {
            // Attach to NotificationsToastHost UIDoc root (proven full-viewport overlay,
            // pickingMode=Ignore so it doesn't block world clicks).
            var toast = FindObjectOfType<NotificationsToastHost>();
            var anchorDoc = toast != null ? toast.GetComponent<UIDocument>() : null;
            if (anchorDoc == null || anchorDoc.rootVisualElement == null)
            {
                foreach (var d in FindObjectsOfType<UIDocument>())
                    if (d != null && d.rootVisualElement != null) { anchorDoc = d; break; }
            }
            if (anchorDoc == null || anchorDoc.rootVisualElement == null)
            {
                Debug.LogWarning("[MapPanelHost] BuildRuntimePanel — no anchor UIDocument found in scene; abort.");
                return;
            }

            _runtimePanel = new VisualElement { name = "map-panel" };
            _runtimePanel.AddToClassList("map-panel");
            _runtimePanel.style.position = Position.Absolute;
            _runtimePanel.style.bottom = 24f;
            _runtimePanel.style.right = 24f;
            _runtimePanel.style.flexDirection = FlexDirection.Column;
            _runtimePanel.style.display = DisplayStyle.None;

            var card = new VisualElement { name = "map-panel-card" };
            card.AddToClassList("map-panel__card");
            card.style.backgroundColor = Hex("#f5e6c8");
            var tan = Hex("#b89b5e");
            card.style.borderTopColor = tan; card.style.borderBottomColor = tan;
            card.style.borderLeftColor = tan; card.style.borderRightColor = tan;
            card.style.borderTopWidth = 3f; card.style.borderBottomWidth = 3f;
            card.style.borderLeftWidth = 3f; card.style.borderRightWidth = 3f;
            card.style.borderTopLeftRadius = 12f; card.style.borderTopRightRadius = 12f;
            card.style.borderBottomLeftRadius = 12f; card.style.borderBottomRightRadius = 12f;
            card.style.paddingTop = 16f; card.style.paddingBottom = 16f;
            card.style.paddingLeft = 18f; card.style.paddingRight = 18f;
            card.style.width = 360f;
            card.style.flexDirection = FlexDirection.Column;

            // Header.
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.alignItems = Align.Center;
            header.style.marginBottom = 10f;
            var title = new Label("Map");
            title.style.color = Hex("#3a2f1c");
            title.style.fontSize = 18f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.flexGrow = 1f;
            header.Add(title);
            var btnClose = new Button(() => OnClose()) { text = "X" };
            btnClose.style.backgroundColor = Hex("#ede4ce");
            btnClose.style.borderTopColor = tan; btnClose.style.borderBottomColor = tan;
            btnClose.style.borderLeftColor = tan; btnClose.style.borderRightColor = tan;
            btnClose.style.borderTopWidth = 1f; btnClose.style.borderBottomWidth = 1f;
            btnClose.style.borderLeftWidth = 1f; btnClose.style.borderRightWidth = 1f;
            btnClose.style.color = Hex("#3a2f1c");
            btnClose.style.fontSize = 14f;
            btnClose.style.height = 26f;
            btnClose.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(btnClose);
            card.Add(header);

            // Minimap surface.
            _minimapSurface = new VisualElement { name = "minimap-surface" };
            _minimapSurface.style.height = 240f;
            _minimapSurface.style.backgroundColor = Hex("#ede4ce");
            _minimapSurface.style.borderTopColor = tan; _minimapSurface.style.borderBottomColor = tan;
            _minimapSurface.style.borderLeftColor = tan; _minimapSurface.style.borderRightColor = tan;
            _minimapSurface.style.borderTopWidth = 1f; _minimapSurface.style.borderBottomWidth = 1f;
            _minimapSurface.style.borderLeftWidth = 1f; _minimapSurface.style.borderRightWidth = 1f;
            _minimapSurface.style.borderTopLeftRadius = 6f; _minimapSurface.style.borderTopRightRadius = 6f;
            _minimapSurface.style.borderBottomLeftRadius = 6f; _minimapSurface.style.borderBottomRightRadius = 6f;
            _minimapSurface.style.marginBottom = 10f;
            card.Add(_minimapSurface);

            // Layer toggles.
            var layersHeader = new Label("Layers");
            layersHeader.style.color = Hex("#6b5a3d");
            layersHeader.style.fontSize = 11f;
            layersHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            layersHeader.style.marginBottom = 4f;
            card.Add(layersHeader);

            var strip = new VisualElement();
            strip.style.flexDirection = FlexDirection.Row;
            strip.style.flexWrap = Wrap.Wrap;

            _toggleTerrain = new Toggle("Terrain") { name = "toggle-terrain" };
            _toggleZones = new Toggle("Zones") { name = "toggle-zones" };
            _toggleRoads = new Toggle("Roads") { name = "toggle-roads" };
            foreach (var t in new[] { _toggleTerrain, _toggleZones, _toggleRoads })
            {
                t.style.color = Hex("#3a2f1c");
                t.style.fontSize = 12f;
                t.style.marginRight = 12f;
                strip.Add(t);
            }
            card.Add(strip);

            _runtimePanel.Add(card);
            anchorDoc.rootVisualElement.Add(_runtimePanel);
            Debug.Log("[MapPanelHost] BuildRuntimePanel — programmatic map-panel attached to anchor UIDoc");
        }

        static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

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
