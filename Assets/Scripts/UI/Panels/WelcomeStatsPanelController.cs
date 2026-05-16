using UnityEngine;
using UnityEngine.UIElements;
using Territory.SceneManagement;
using Territory.RegionScene;
using Territory.RegionScene.Evolution;

namespace Territory.UI.Panels
{
    /// <summary>CoreScene hub — shows WelcomeStatsPanel on region landing. Subscribes to ZoomTransitionController.StateChanged; pulls data from RegionData.</summary>
    public class WelcomeStatsPanelController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        private VisualElement _panel;
        private Label _populationValue;
        private Label _treasuryValue;
        private Label _elapsedValue;
        private Label _dormantValue;

        private ZoomTransitionController _transitionController;

        // Elapsed ticks since last city-context entry; incremented by TickClock / external callers.
        private int _elapsedTicksInCity;

        /// <summary>Last recorded elapsed ticks in city context. Set by ZoomTransitionController landing or external tick source.</summary>
        public int ElapsedTicksInCity
        {
            get => _elapsedTicksInCity;
            set => _elapsedTicksInCity = value >= 0 ? value : 0;
        }

        void Awake()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();

            if (uiDocument != null)
            {
                var root = uiDocument.rootVisualElement;
                _panel           = root.Q<VisualElement>("welcome-stats-panel");
                _populationValue = root.Q<Label>("welcome-stats-value-population");
                _treasuryValue   = root.Q<Label>("welcome-stats-value-treasury");
                _elapsedValue    = root.Q<Label>("welcome-stats-value-elapsed");
                _dormantValue    = root.Q<Label>("welcome-stats-value-dormant");
            }

            HidePanel();
        }

        void Start()
        {
            _transitionController = FindObjectOfType<ZoomTransitionController>();
            if (_transitionController != null)
                _transitionController.StateChanged += OnTransitionStateChanged;
        }

        /// <summary>Show panel populated with current RegionData stats.</summary>
        public void ShowWithData()
        {
            var regionData = FindRegionData();
            PopulateFields(regionData);

            if (_panel != null)
            {
                _panel.RemoveFromClassList("welcome-stats__panel--hidden");
                _panel.AddToClassList("welcome-stats__panel--visible");
            }
        }

        /// <summary>Hide panel.</summary>
        public void HidePanel()
        {
            if (_panel != null)
            {
                _panel.RemoveFromClassList("welcome-stats__panel--visible");
                _panel.AddToClassList("welcome-stats__panel--hidden");
            }
        }

        /// <summary>Whether panel is currently visible (class-list driven).</summary>
        public bool IsVisible => _panel != null && _panel.ClassListContains("welcome-stats__panel--visible");

        private void OnTransitionStateChanged(TransitionState state)
        {
            if (state == TransitionState.Landing)
                ShowWithData();
            else if (state == TransitionState.Idle && IsVisible)
                HidePanel();
        }

        private void PopulateFields(RegionData regionData)
        {
            int populationSum  = 0;
            int knownCityCount = 0;

            if (regionData != null)
            {
                var cells = regionData.AllCells;
                foreach (var cell in cells)
                {
                    if (cell == null) continue;
                    populationSum += cell.pop;
                    if (!string.IsNullOrEmpty(cell.owningCityId))
                        knownCityCount++;
                }
            }

            int dormantCount = knownCityCount > 0 ? knownCityCount - 1 : 0;

            if (_populationValue != null) _populationValue.text = populationSum.ToString("N0");
            if (_treasuryValue   != null) _treasuryValue.text   = "--";
            if (_elapsedValue    != null) _elapsedValue.text    = $"{_elapsedTicksInCity} ticks";
            if (_dormantValue    != null) _dormantValue.text    = dormantCount.ToString();
        }

        private RegionData FindRegionData()
        {
            var regionManager = FindObjectOfType<RegionManager>();
            if (regionManager == null) return null;
            // RegionData registered in ServiceRegistry from RegionManager.Awake.
            var registry = FindObjectOfType<Domains.Registry.ServiceRegistry>();
            return registry?.Resolve<RegionData>();
        }

        void OnDestroy()
        {
            if (_transitionController != null)
                _transitionController.StateChanged -= OnTransitionStateChanged;
        }
    }
}
