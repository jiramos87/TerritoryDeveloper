using UnityEngine;
using UnityEngine.UIElements;
using Territory.IsoSceneCore.UI;

namespace Territory.RegionScene.UI
{
    /// <summary>Host MonoBehaviour for region-cell-hover panel (DEC-A28). Subscribes to IsoSceneCellHoverDispatcher; updates labels; pins to mouse position via left/top style.</summary>
    public sealed class RegionCellHoverPanel : MonoBehaviour
    {
        [SerializeField] private UIDocument _doc;
        [SerializeField] private IsoSceneUIShellHost _shellHost;

        private VisualElement _root;
        private Label _lblCoord;
        private Label _lblTerrain;
        private Label _lblHeight;
        private Label _lblCity;

        private void Awake()
        {
            if (_doc == null) _doc = GetComponent<UIDocument>();
        }

        private void Start()
        {
            if (_doc == null || _doc.rootVisualElement == null)
            {
                Debug.LogWarning("[RegionCellHoverPanel] UIDocument missing.");
                return;
            }

            _root = _doc.rootVisualElement.Q("region-cell-hover-panel");
            if (_root == null)
            {
                Debug.LogWarning("[RegionCellHoverPanel] root element 'region-cell-hover-panel' not found.");
                return;
            }

            _lblCoord   = _root.Q<Label>("lbl-coord");
            _lblTerrain = _root.Q<Label>("lbl-terrain");
            _lblHeight  = _root.Q<Label>("lbl-height");
            _lblCity    = _root.Q<Label>("lbl-city");

            // Register into IsoSceneUIShellHost hover slot (modal-slot fallback if no hover-slot)
            if (_shellHost != null)
            {
                var slot = _shellHost.Slot("modal-slot") ?? _shellHost.Slot("hud-slot");
                slot?.Add(_root);
            }

            Hide();
        }

        /// <summary>Show hover panel at screen position with terrain data. Called by RegionCellClickHandler on hover.</summary>
        public void Show(int x, int y, string terrainKind, int height, string cityHint, Vector2 screenPos)
        {
            if (_root == null) return;
            if (_lblCoord != null)   _lblCoord.text   = $"[{x}, {y}]";
            if (_lblTerrain != null) _lblTerrain.text = terrainKind;
            if (_lblHeight != null)  _lblHeight.text  = $"Height: {height}";
            if (_lblCity != null)    _lblCity.text    = string.IsNullOrEmpty(cityHint) ? "(no city)" : cityHint;

            // Pin to mouse via absolute position
            _root.style.left = screenPos.x + 16f;
            _root.style.top  = screenPos.y + 4f;
            _root.style.display = DisplayStyle.Flex;
        }

        /// <summary>Hide the hover panel.</summary>
        public void Hide()
        {
            if (_root == null) return;
            _root.style.display = DisplayStyle.None;
        }

        // ClickRoutesToInspectorAndHoverPanels — tracer anchor for stage3.0 test (shared with RegionCellClickHandler.cs anchor)
        public bool IsMounted => _root != null;
    }
}
