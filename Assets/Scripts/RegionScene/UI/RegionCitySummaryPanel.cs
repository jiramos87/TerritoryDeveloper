using UnityEngine;
using UnityEngine.UIElements;
using Territory.IsoSceneCore.UI;

namespace Territory.RegionScene.UI
{
    /// <summary>Host MonoBehaviour for region-city-summary panel (DEC-A28). Registers into IsoSceneUIShellHost modal-slot. Right-click over city cell opens this panel. Enter City stays disabled — scene transition deferred.</summary>
    public sealed class RegionCitySummaryPanel : MonoBehaviour
    {
        [SerializeField] private UIDocument _doc;
        [SerializeField] private IsoSceneUIShellHost _shellHost;

        private VisualElement _root;
        private Label _lblCityName;
        private Label _lblPop;
        private Label _lblUrban;
        private Button _btnEnterCity;

        private void Awake()
        {
            if (_doc == null) _doc = GetComponent<UIDocument>();
        }

        private void Start()
        {
            if (_doc == null || _doc.rootVisualElement == null)
            {
                Debug.LogWarning("[RegionCitySummaryPanel] UIDocument missing.");
                return;
            }

            _root = _doc.rootVisualElement.Q("region-city-summary");
            if (_root == null)
            {
                Debug.LogWarning("[RegionCitySummaryPanel] root element 'region-city-summary' not found.");
                return;
            }

            _lblCityName  = _root.Q<Label>("lbl-city-name");
            _lblPop       = _root.Q<Label>("lbl-pop");
            _lblUrban     = _root.Q<Label>("lbl-urban");
            _btnEnterCity = _root.Q<Button>("btn-enter-city");

            // Enter City disabled — scene transition deferred
            if (_btnEnterCity != null)
            {
                _btnEnterCity.AddToClassList("disabled");
                _btnEnterCity.SetEnabled(false);
                // no-op click; deferred per region-scene-prototype handoff.deferred list
                _btnEnterCity.clicked += () => Debug.Log("[RegionCitySummaryPanel] Enter City — scene transition not yet available.");
            }

            // Register into modal-slot (modal-slot single-child contract)
            if (_shellHost != null)
            {
                var slot = _shellHost.Slot("modal-slot");
                slot?.Add(_root);
            }

            Hide();
        }

        /// <summary>Show city summary for the given cell data.</summary>
        public void Show(string cityName, int pop, float urbanKm2)
        {
            if (_root == null) return;
            if (_lblCityName != null) _lblCityName.text = cityName;
            if (_lblPop != null)      _lblPop.text      = $"Pop: {pop:N0}";
            if (_lblUrban != null)    _lblUrban.text    = $"Urban: {urbanKm2:F1} km²";
            _root.style.display = DisplayStyle.Flex;
        }

        /// <summary>Hide city summary.</summary>
        public void Hide()
        {
            if (_root == null) return;
            _root.style.display = DisplayStyle.None;
        }

        public bool IsMounted => _root != null;

        /// <summary>True when btn-enter-city carries 'disabled' class. Assertion target for stage-3.0 test.</summary>
        public bool EnterCityIsDisabled => _btnEnterCity != null && _btnEnterCity.ClassListContains("disabled");
    }
}
