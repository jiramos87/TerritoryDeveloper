using UnityEngine;
using UnityEngine.UIElements;
using Territory.SceneManagement;

namespace Territory.UI.Panels
{
    /// <summary>CoreScene hub — shows WelcomeStatsPanel on region landing. Subscribes to ZoomTransitionController.StateChanged. Data injected via SetLandingData before Landing state fires. Invariant #3: cache refs in Awake/Start.</summary>
    public class WelcomeStatsPanelController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        private VisualElement _panel;
        private Label _populationLabel;
        private Label _treasuryLabel;
        private Label _elapsedLabel;
        private Label _dormantLabel;

        private ZoomTransitionController _transitionController;

        // Data fields — injected by a RegionScene-side wiring script before Landing fires.
        private int _populationSum;
        private long _elapsedTicksInCity;
        private int _dormantCityCount;

        /// <summary>Push landing stats. Call from RegionScene wiring before transition completes.</summary>
        public void SetLandingData(int populationSum, long elapsedTicks, int dormantCityCount)
        {
            _populationSum      = populationSum;
            _elapsedTicksInCity = elapsedTicks;
            _dormantCityCount   = dormantCityCount;
        }

        void Awake()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();

            if (uiDocument != null)
            {
                var root = uiDocument.rootVisualElement;
                _panel           = root.Q<VisualElement>("welcome-stats-panel");
                _populationLabel = root.Q<Label>("welcome-stats-population");
                _treasuryLabel   = root.Q<Label>("welcome-stats-treasury");
                _elapsedLabel    = root.Q<Label>("welcome-stats-elapsed");
                _dormantLabel    = root.Q<Label>("welcome-stats-dormant");
            }

            HidePanel();
        }

        void Start()
        {
            _transitionController = FindObjectOfType<ZoomTransitionController>();
            if (_transitionController != null)
                _transitionController.StateChanged += OnTransitionStateChanged;
        }

        void OnDestroy()
        {
            if (_transitionController != null)
                _transitionController.StateChanged -= OnTransitionStateChanged;
        }

        private void OnTransitionStateChanged(TransitionState state)
        {
            if (state == TransitionState.Landing)
            {
                ApplyFields();
                ShowPanel();
            }
            else if (state == TransitionState.Idle || state == TransitionState.TweeningOut)
            {
                HidePanel();
            }
        }

        private void ApplyFields()
        {
            if (_populationLabel != null) _populationLabel.text = _populationSum.ToString("N0");
            if (_treasuryLabel   != null) _treasuryLabel.text   = "--";
            if (_elapsedLabel    != null) _elapsedLabel.text    = $"{_elapsedTicksInCity} ticks";
            if (_dormantLabel    != null) _dormantLabel.text    = _dormantCityCount.ToString();
        }

        private void ShowPanel()
        {
            if (_panel == null) return;
            _panel.style.display = DisplayStyle.Flex;
            _panel.AddToClassList("welcome-stats__panel--visible");
        }

        private void HidePanel()
        {
            if (_panel == null) return;
            _panel.RemoveFromClassList("welcome-stats__panel--visible");
            _panel.style.display = DisplayStyle.None;
        }
    }
}
