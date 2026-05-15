using UnityEngine;
using UnityEngine.UIElements;
using Territory.IsoSceneCore.UI;

namespace Territory.RegionScene.UI
{
    /// <summary>Host MonoBehaviour for region-cell-inspector panel (DEC-A28). Registers into IsoSceneUIShellHost modal-slot. Shows terrain + pop + urban-area on left-click.</summary>
    public sealed class RegionCellInspectorPanel : MonoBehaviour
    {
        [SerializeField] private UIDocument _doc;
        [SerializeField] private IsoSceneUIShellHost _shellHost;

        private VisualElement _root;
        private Label _lblTitle;
        private Label _lblTerrain;
        private Label _lblHeight;
        private Label _lblWater;
        private Label _lblCliff;
        private Label _lblPop;
        private Label _lblUrban;

        private void Awake()
        {
            if (_doc == null) _doc = GetComponent<UIDocument>();
        }

        private void Start()
        {
            if (_doc == null || _doc.rootVisualElement == null)
            {
                Debug.LogWarning("[RegionCellInspectorPanel] UIDocument missing.");
                return;
            }

            _root = _doc.rootVisualElement.Q("region-cell-inspector");
            if (_root == null)
            {
                Debug.LogWarning("[RegionCellInspectorPanel] root element 'region-cell-inspector' not found.");
                return;
            }

            _lblTitle   = _root.Q<Label>("lbl-title");
            _lblTerrain = _root.Q<Label>("lbl-terrain");
            _lblHeight  = _root.Q<Label>("lbl-height");
            _lblWater   = _root.Q<Label>("lbl-water");
            _lblCliff   = _root.Q<Label>("lbl-cliff");
            _lblPop     = _root.Q<Label>("lbl-pop");
            _lblUrban   = _root.Q<Label>("lbl-urban");

            // Register into modal-slot (modal-slot single-child contract: IsoSceneUIShellHost owns slot clearing)
            if (_shellHost != null)
            {
                var slot = _shellHost.Slot("modal-slot");
                slot?.Add(_root);
            }

            Hide();
        }

        /// <summary>Show inspector for the given cell. Pass cityPop=-1 + cityUrban=-1 when cell has no owning city.</summary>
        public void Show(int x, int y, string terrainKind, int height, bool hasWater, bool hasCliff, int cityPop = -1, float cityUrbanKm2 = -1f)
        {
            if (_root == null) return;
            if (_lblTitle != null)   _lblTitle.text   = $"Cell [{x}, {y}]";
            if (_lblTerrain != null) _lblTerrain.text = $"Terrain: {terrainKind}";
            if (_lblHeight != null)  _lblHeight.text  = $"Height: {height}";
            if (_lblWater != null)   _lblWater.text   = $"Water: {(hasWater ? "yes" : "no")}";
            if (_lblCliff != null)   _lblCliff.text   = $"Cliff: {(hasCliff ? "yes" : "no")}";
            if (_lblPop != null)     _lblPop.text     = cityPop >= 0 ? $"Pop: {cityPop:N0}" : "";
            if (_lblUrban != null)   _lblUrban.text   = cityUrbanKm2 >= 0f ? $"Urban: {cityUrbanKm2:F1} km²" : "";
            _root.style.display = DisplayStyle.Flex;
        }

        /// <summary>Hide inspector.</summary>
        public void Hide()
        {
            if (_root == null) return;
            _root.style.display = DisplayStyle.None;
        }

        public bool IsMounted => _root != null;
    }
}
