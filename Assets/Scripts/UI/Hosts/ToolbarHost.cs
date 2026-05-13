using Territory.UI.Modals;
using Territory.UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// MonoBehaviour Host for the toolbar HUD. Renders the explicit 9-tile grid authored in
    /// toolbar.uxml (no ListView). Each tile button.clicked routes to OnToolSelected which:
    ///   - opens the HUD-level subtype picker for zone-r/c/i + services (multi-tier tools), or
    ///   - sets the active tool directly for road / power / water / landmark / bulldoze.
    /// Picker is invoked via a direct ToolSubtypePickerHost reference — NOT via
    /// ModalCoordinator, so the picker stays HUD-anchored and doesn't trigger modal-pause
    /// ownership semantics (correction per Goal D screenshot in the recovery plan).
    /// </summary>
    public sealed class ToolbarHost : MonoBehaviour
    {
        [SerializeField] UIDocument _doc;
        [SerializeField] ToolSubtypePickerHost _subtypePicker;

        // 9 tiles — order matches toolbar.uxml + plan §6 Phase D table.
        static readonly string[] TileSlugs = new[] {
            "zone-r", "zone-c", "zone-i",
            "road", "services",
            "building-power", "building-water",
            "landmark", "bulldoze",
        };

        // Slugs that open the subtype picker. Others select directly.
        static readonly System.Collections.Generic.HashSet<string> HasSubtypes = new()
        {
            "zone-r", "zone-c", "zone-i", "services",
        };

        ToolbarVM _vm;
        readonly System.Collections.Generic.Dictionary<string, Button> _btns = new();

        void Awake()
        {
            if (_subtypePicker == null) _subtypePicker = FindObjectOfType<ToolSubtypePickerHost>();
        }

        void OnEnable()
        {
            _vm = new ToolbarVM();
            _vm.SelectToolCommand = OnToolSelected;

            if (_doc == null || _doc.rootVisualElement == null)
            {
                Debug.LogWarning("[ToolbarHost] UIDocument or rootVisualElement null on enable.");
                return;
            }
            var root = _doc.rootVisualElement;
            root.style.position = Position.Absolute;
            root.style.top = 0;
            root.style.left = 0;
            root.style.right = 0;
            root.style.bottom = 0;
            root.SetCompatDataSource(_vm);

            _btns.Clear();
            foreach (var slug in TileSlugs)
            {
                var name = "tool-" + slug;
                var btn = root.Q<Button>(name);
                if (btn == null) continue;
                _btns[slug] = btn;
                btn.clicked += () => OnToolSelected(slug);
            }
        }

        void OnDisable()
        {
            _btns.Clear();
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(null);
        }

        void OnToolSelected(string parentSlug)
        {
            if (_vm == null) return;
            _vm.ActiveTool = parentSlug;

            // Highlight the active tile.
            foreach (var kv in _btns)
                kv.Value.EnableInClassList("icon-btn--active", kv.Key == parentSlug);

            if (HasSubtypes.Contains(parentSlug))
            {
                if (_subtypePicker != null) _subtypePicker.Open(parentSlug);
                else Debug.LogWarning("[ToolbarHost] Subtype picker host not wired — Open(" + parentSlug + ") dropped.");
            }
            else
            {
                ApplyTool(parentSlug, tier: null);
            }
        }

        void ApplyTool(string parentSlug, string tier)
        {
            var uim = FindObjectOfType<UIManager>();
            if (uim == null) return;
            switch (parentSlug)
            {
                case "bulldoze":
                    uim.bulldozeMode = !uim.bulldozeMode;
                    break;
                default:
                    int hash = (parentSlug + ":" + (tier ?? "")).GetHashCode();
                    uim.SetCurrentSubTypeId(System.Math.Abs(hash) % 100000);
                    break;
            }
        }
    }
}
