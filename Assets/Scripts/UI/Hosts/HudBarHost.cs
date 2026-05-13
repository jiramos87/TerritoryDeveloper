using Territory.Economy;
using Territory.Timing;
using Territory.UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// MonoBehaviour Host for hud-bar top-strip — wires HudBarVM and pushes live game state
    /// (money, date, weather, happiness) into UIDocument labels. Unity 2022.3 manual binding:
    /// queries Q&lt;Label&gt; refs at OnEnable + assigns .text on each Update (no runtime dataSource API).
    /// </summary>
    public sealed class HudBarHost : MonoBehaviour
    {
        [SerializeField] UIDocument _doc;

        [Header("Producers (Inspector first; Awake fallback)")]
        [SerializeField] CityStats _cityStats;
        [SerializeField] EconomyManager _economyManager;
        [SerializeField] TimeManager _timeManager;

        HudBarVM _vm;

        Label _moneyLbl;
        Label _budgetDeltaLbl;
        Label _cityNameLbl;
        Label _dateLbl;
        Label _happinessLbl;

        void Awake()
        {
            if (_cityStats == null)     _cityStats     = FindObjectOfType<CityStats>();
            if (_economyManager == null) _economyManager = FindObjectOfType<EconomyManager>();
            if (_timeManager == null)   _timeManager   = FindObjectOfType<TimeManager>();
        }

        void OnEnable()
        {
            _vm = new HudBarVM();

            if (_doc == null || _doc.rootVisualElement == null)
            {
                Debug.LogWarning("[HudBarHost] UIDocument or rootVisualElement null on enable — check PanelSettings wiring.");
                return;
            }

            var root = _doc.rootVisualElement;
            root.SetCompatDataSource(_vm);
            _moneyLbl = root.Q<Label>("money");
            _budgetDeltaLbl = root.Q<Label>("budget-delta");
            _cityNameLbl = root.Q<Label>("city-name");
            _dateLbl = root.Q<Label>("date");
            _happinessLbl = root.Q<Label>("happiness");

            PushSnapshot();
        }

        void OnDisable()
        {
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(null);
        }

        void Update()
        {
            PushSnapshot();
        }

        void PushSnapshot()
        {
            if (_vm == null) return;

            if (_cityStats != null)
            {
                _vm.Money = $"${_cityStats.money:N0}";
                _vm.CityName = string.IsNullOrEmpty(_cityStats.cityName) ? "—" : _cityStats.cityName;
                _vm.Happiness = $"{_cityStats.happiness:F0}";
            }
            if (_economyManager != null)
            {
                int delta = _economyManager.GetMonthlyIncomeDelta();
                _vm.BudgetDelta = delta >= 0 ? $"+{delta:N0}" : $"{delta:N0}";
            }
            if (_timeManager != null)
                _vm.Date = _timeManager.GetCurrentDate().ToString("MMM yyyy");

            if (_moneyLbl != null) _moneyLbl.text = _vm.Money;
            if (_budgetDeltaLbl != null) _budgetDeltaLbl.text = _vm.BudgetDelta;
            if (_cityNameLbl != null) _cityNameLbl.text = _vm.CityName;
            if (_dateLbl != null) _dateLbl.text = _vm.Date;
            if (_happinessLbl != null) _happinessLbl.text = _vm.Happiness;
        }
    }
}
