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
        Button _btnZn, _btnRd, _btnFr, _btnCt;
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
            if (_miniMapController == null)
            {
                Debug.Log("[MapPanelHost] MiniMapController missing in scene — spawning runtime instance");
                var ctrlGo = new GameObject("MiniMapController-Runtime");
                _miniMapController = ctrlGo.AddComponent<MiniMapController>();
            }

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
            if (_minimapSurface == null) return;
            if (_miniMapController == null) _miniMapController = FindObjectOfType<MiniMapController>();
            if (_miniMapController == null) return;
            _refreshFrame++;
            if (_refreshFrame < 30) return;
            _refreshFrame = 0;
            _miniMapController.RebuildTexture();
            var tex = _miniMapController.MapTexture;
            if (tex != null)
            {
                _minimapSurface.style.backgroundImage = new StyleBackground(tex);
            }
            else
            {
                Debug.Log("[MapPanelHost] Update — MiniMapController.MapTexture still null (grid not initialized?)");
            }
        }

        void OnLayerChanged(ChangeEvent<bool> _)
        {
            // Legacy callback path retained for scene-UIDoc Toggle wiring (no-op when toggles absent).
            if (_miniMapController == null) return;
            MiniMapLayer layers = MiniMapLayer.None;
            if (_toggleZones != null && _toggleZones.value)    layers |= MiniMapLayer.Zones;
            if (_toggleRoads != null && _toggleRoads.value)    layers |= MiniMapLayer.Streets;
            if (_toggleTerrain != null && _toggleTerrain.value) layers |= MiniMapLayer.Desirability;
            if (layers != MiniMapLayer.None) _miniMapController.SetActiveLayers(layers);
            _miniMapController.RebuildTexture();
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

            // iter-40 — strip the panel chrome per user QA: no title/close-X/Layers header.
            // The MAP is now just a cream-bordered map square with layer toggles as a top
            // strip overlay INSIDE the square.
            _runtimePanel = new VisualElement { name = "map-panel" };
            _runtimePanel.AddToClassList("map-panel");
            _runtimePanel.style.position = Position.Absolute;
            _runtimePanel.style.bottom = 24f;
            _runtimePanel.style.right = 24f;
            _runtimePanel.style.flexDirection = FlexDirection.Column;
            _runtimePanel.style.display = DisplayStyle.None;

            var tan = Hex("#b89b5e");

            // Minimap surface = the whole panel. Cream border, no card chrome.
            _minimapSurface = new VisualElement { name = "minimap-surface" };
            _minimapSurface.style.width = 320f;
            _minimapSurface.style.height = 240f;
            _minimapSurface.style.backgroundColor = Hex("#ede4ce");
            _minimapSurface.style.borderTopColor = tan; _minimapSurface.style.borderBottomColor = tan;
            _minimapSurface.style.borderLeftColor = tan; _minimapSurface.style.borderRightColor = tan;
            _minimapSurface.style.borderTopWidth = 3f; _minimapSurface.style.borderBottomWidth = 3f;
            _minimapSurface.style.borderLeftWidth = 3f; _minimapSurface.style.borderRightWidth = 3f;
            _minimapSurface.style.borderTopLeftRadius = 8f; _minimapSurface.style.borderTopRightRadius = 8f;
            _minimapSurface.style.borderBottomLeftRadius = 8f; _minimapSurface.style.borderBottomRightRadius = 8f;
            _minimapSurface.style.flexDirection = FlexDirection.Column;
            _minimapSurface.style.justifyContent = Justify.FlexStart;
            _minimapSurface.style.alignItems = Align.Stretch;

            // Top-strip layer toggle bar overlay (inside the map square) — square mini buttons.
            var strip = new VisualElement();
            strip.style.flexDirection = FlexDirection.Row;
            strip.style.alignItems = Align.Center;
            strip.style.justifyContent = Justify.FlexStart;
            strip.style.backgroundColor = new StyleColor(new Color(0.96f, 0.90f, 0.78f, 0.85f));
            strip.style.borderBottomColor = tan;
            strip.style.borderBottomWidth = 1f;
            strip.style.paddingTop = 4f; strip.style.paddingBottom = 4f;
            strip.style.paddingLeft = 6f; strip.style.paddingRight = 6f;

            _btnZn = BuildLayerButton("Zn", MiniMapLayer.Zones,  tan); strip.Add(_btnZn);
            _btnRd = BuildLayerButton("Rd", MiniMapLayer.Streets, tan); strip.Add(_btnRd);
            _btnFr = BuildLayerButton("Fr", MiniMapLayer.Forests, tan); strip.Add(_btnFr);
            _btnCt = BuildLayerButton("Ct", MiniMapLayer.Centroid, tan); strip.Add(_btnCt);
            _minimapSurface.Add(strip);

            _runtimePanel.Add(_minimapSurface);
            anchorDoc.rootVisualElement.Add(_runtimePanel);
            Debug.Log("[MapPanelHost] BuildRuntimePanel — chrome-less map square attached to anchor UIDoc");
        }

        Button BuildLayerButton(string label, MiniMapLayer layer, Color tan)
        {
            var b = new Button(() => ToggleLayer(layer)) { text = label };
            b.style.width = 28f;
            b.style.height = 22f;
            b.style.marginRight = 4f;
            b.style.paddingTop = 0; b.style.paddingBottom = 0;
            b.style.paddingLeft = 0; b.style.paddingRight = 0;
            b.style.fontSize = 11f;
            b.style.unityFontStyleAndWeight = FontStyle.Bold;
            b.style.color = Hex("#3a2f1c");
            b.style.borderTopColor = tan; b.style.borderBottomColor = tan;
            b.style.borderLeftColor = tan; b.style.borderRightColor = tan;
            b.style.borderTopWidth = 1f; b.style.borderBottomWidth = 1f;
            b.style.borderLeftWidth = 1f; b.style.borderRightWidth = 1f;
            b.style.borderTopLeftRadius = 3f; b.style.borderTopRightRadius = 3f;
            b.style.borderBottomLeftRadius = 3f; b.style.borderBottomRightRadius = 3f;
            RefreshLayerButton(b, layer);
            return b;
        }

        void ToggleLayer(MiniMapLayer layer)
        {
            if (_miniMapController == null) _miniMapController = FindObjectOfType<MiniMapController>();
            if (_miniMapController == null) return;
            _miniMapController.ToggleLayer(layer);
            RefreshAllLayerButtons();
            _miniMapController.RebuildTexture();
            var tex = _miniMapController.MapTexture;
            if (_minimapSurface != null && tex != null) _minimapSurface.style.backgroundImage = new StyleBackground(tex);
        }

        void RefreshAllLayerButtons()
        {
            if (_miniMapController == null) return;
            RefreshLayerButton(_btnZn, MiniMapLayer.Zones);
            RefreshLayerButton(_btnRd, MiniMapLayer.Streets);
            RefreshLayerButton(_btnFr, MiniMapLayer.Forests);
            RefreshLayerButton(_btnCt, MiniMapLayer.Centroid);
        }

        void RefreshLayerButton(Button b, MiniMapLayer layer)
        {
            if (b == null || _miniMapController == null) return;
            bool active = _miniMapController.IsLayerActive(layer);
            b.style.backgroundColor = active ? Hex("#5b7fa8") : Hex("#ede4ce");
            b.style.color = active ? Hex("#f5e6c8") : Hex("#3a2f1c");
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
