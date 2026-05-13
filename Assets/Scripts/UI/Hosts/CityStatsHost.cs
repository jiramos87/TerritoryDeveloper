using Territory.Economy;
using Territory.Timing;
using Territory.UI.Modals;
using Territory.UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// MonoBehaviour Host for city-stats HUD panel — binds population/happiness/funds/day to the
    /// in-game managers (CityStats + EconomyManager + TimeManager). Polls every PollInterval seconds.
    /// Unity 2022.3 manual binding: directly mutates Q&lt;Label&gt; elements (no runtime dataSource API).
    /// </summary>
    public sealed class CityStatsHost : MonoBehaviour
    {
        const float PollInterval = 0.25f;

        [SerializeField] UIDocument _doc;

        CityStatsVM _vm;
        ModalCoordinator _coordinator;
        CityStats _cityStats;
        EconomyManager _economy;
        TimeManager _time;
        Label _popValue;
        Label _hapValue;
        Label _fundsValue;
        Label _dayValue;
        float _nextPollTime;

        void OnEnable()
        {
            _vm = new CityStatsVM();

            if (_doc == null || _doc.rootVisualElement == null)
            {
                Debug.LogWarning("[CityStatsHost] UIDocument or rootVisualElement null on enable.");
                return;
            }

            var root = _doc.rootVisualElement;
            root.SetCompatDataSource(_vm);
            _popValue = root.Q<Label>("pop-value");
            _hapValue = root.Q<Label>("hap-value");
            _fundsValue = root.Q<Label>("funds-value");
            _dayValue = root.Q<Label>("day-value");

            // city-stats is HUD (always visible) — do NOT call RegisterMigratedPanel
            // (that sets display:none for modal hide-by-default semantics).
            _coordinator = FindObjectOfType<ModalCoordinator>();

            ResolveManagers();
            PushSnapshot();
        }

        void OnDisable()
        {
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(null);
        }

        void Update()
        {
            if (Time.unscaledTime < _nextPollTime) return;
            _nextPollTime = Time.unscaledTime + PollInterval;
            if (_cityStats == null || _economy == null || _time == null)
                ResolveManagers();
            PushSnapshot();
        }

        void ResolveManagers()
        {
            if (_cityStats == null) _cityStats = FindObjectOfType<CityStats>();
            if (_economy == null) _economy = FindObjectOfType<EconomyManager>();
            if (_time == null) _time = FindObjectOfType<TimeManager>();
        }

        void PushSnapshot()
        {
            if (_vm == null) return;
            if (_cityStats != null)
            {
                _vm.Population = _cityStats.population;
                _vm.Happiness = Mathf.RoundToInt(_cityStats.happiness);
            }
            if (_economy != null)
                _vm.Funds = _economy.GetCurrentMoney();
            if (_time != null)
                _vm.Day = _time.GetCurrentDate().Day;

            if (_popValue != null) _popValue.text = _vm.Population.ToString("N0");
            if (_hapValue != null) _hapValue.text = _vm.Happiness.ToString();
            if (_fundsValue != null) _fundsValue.text = "$" + _vm.Funds.ToString("N0");
            if (_dayValue != null) _dayValue.text = _vm.Day.ToString();
        }

        /// <summary>External override (tests / cutscenes) — bypass live manager polling.</summary>
        public void SetStats(int population, int happiness, int funds, int day)
        {
            if (_vm == null) return;
            _vm.Population = population;
            _vm.Happiness = happiness;
            _vm.Funds = funds;
            _vm.Day = day;
            if (_popValue != null) _popValue.text = population.ToString("N0");
            if (_hapValue != null) _hapValue.text = happiness.ToString();
            if (_fundsValue != null) _fundsValue.text = "$" + funds.ToString("N0");
            if (_dayValue != null) _dayValue.text = day.ToString();
        }
    }
}
