using Territory.Economy;
using Territory.Timing;
using Territory.UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// MonoBehaviour Host — resolves HudBarVM, wires UIDocument.rootVisualElement.dataSource.
    /// Pumps live game state (money, date, weather, happiness) directly into VM properties.
    /// Lives on the HudBar UIDocument GameObject in CityScene (sidecar coexistence per Q2;
    /// legacy Canvas + HudBarDataAdapter remain alive until Stage 6.0 quarantine plan).
    /// </summary>
    public sealed class HudBarHost : MonoBehaviour
    {
        [SerializeField] UIDocument _doc;

        [Header("Producers (Inspector first; Awake fallback)")]
        [SerializeField] CityStats _cityStats;
        [SerializeField] EconomyManager _economyManager;
        [SerializeField] TimeManager _timeManager;

        HudBarVM _vm;

        // Change-detect caches — avoid PropertyChanged churn every frame.
        string _lastMoney;
        string _lastBudgetDelta;
        string _lastCityName;
        string _lastDate;
        string _lastHappiness;

        void Awake()
        {
            if (_cityStats == null)     _cityStats     = FindObjectOfType<CityStats>();
            if (_economyManager == null) _economyManager = FindObjectOfType<EconomyManager>();
            if (_timeManager == null)   _timeManager   = FindObjectOfType<TimeManager>();
        }

        void OnEnable()
        {
            _vm = new HudBarVM();

            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.dataSource = _vm;
            else
                Debug.LogWarning("[HudBarHost] UIDocument or rootVisualElement null on enable — check PanelSettings wiring.");

            // Seed VM on first enable so HUD shows values immediately.
            PushToVM();
        }

        void OnDisable()
        {
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.dataSource = null;
        }

        void Update()
        {
            PushToVM();
        }

        void PushToVM()
        {
            if (_vm == null) return;

            // Money
            if (_cityStats != null)
            {
                string money = $"${_cityStats.money:N0}";
                if (money != _lastMoney) { _vm.Money = money; _lastMoney = money; }
            }

            // Budget delta from EconomyManager
            if (_economyManager != null)
            {
                int delta = _economyManager.GetMonthlyIncomeDelta();
                string deltaStr = delta >= 0 ? $"+{delta:N0}" : $"{delta:N0}";
                if (deltaStr != _lastBudgetDelta) { _vm.BudgetDelta = deltaStr; _lastBudgetDelta = deltaStr; }
            }

            // City name
            if (_cityStats != null)
            {
                string name = string.IsNullOrEmpty(_cityStats.cityName) ? "—" : _cityStats.cityName;
                if (name != _lastCityName) { _vm.CityName = name; _lastCityName = name; }
            }

            // Date
            if (_timeManager != null)
            {
                string date = _timeManager.GetCurrentDate().Date.ToString("MMM yyyy");
                if (date != _lastDate) { _vm.Date = date; _lastDate = date; }
            }

            // Happiness (formatted 0–100)
            if (_cityStats != null)
            {
                string hap = $"{_cityStats.happiness:F0}";
                if (hap != _lastHappiness) { _vm.Happiness = hap; _lastHappiness = hap; }
            }
        }
    }
}
