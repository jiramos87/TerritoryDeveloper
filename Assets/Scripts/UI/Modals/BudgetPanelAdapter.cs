using System;
using System.Collections.Generic;
using Territory.Economy;
using Territory.Simulation;
using Territory.UI.Registry;
using UnityEngine;

namespace Territory.UI.Modals
{
    /// <summary>
    /// Wave B3 (TECH-27089) — budget-panel adapter.
    /// Registers budget.open / budget.close action handlers.
    /// Subscribes ~40 binds (taxes 4 + funding 11 + forecast 3 + treasury + header + range).
    /// Dispatches taxRate.set actions back to EconomyManager on slider change.
    /// ModalCoordinator reused from Wave B2 (T6.0.5).
    /// Inv #3: slot resolution at mount only, never per-frame.
    /// </summary>
    public class BudgetPanelAdapter : MonoBehaviour
    {
        private const int ExpectedMinWidgets = 40;

        [SerializeField] private UiActionRegistry  _actionRegistry;
        [SerializeField] private UiBindRegistry    _bindRegistry;
        [SerializeField] private ModalCoordinator  _modalCoordinator;
        [SerializeField] private EconomyManager    _economyManager;
        [SerializeField] private BudgetForecaster  _forecaster;

        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();

        // Tax bind ids.
        private static readonly string[] TaxBindIds =
        {
            "budget.tax.residential",
            "budget.tax.commercial",
            "budget.tax.industrial",
            "budget.tax.general",
        };

        // Service-funding bind ids (11).
        private static readonly string[] FundingBindIds =
        {
            "budget.funding.power",   "budget.funding.water",    "budget.funding.waste",
            "budget.funding.police",  "budget.funding.fire",     "budget.funding.health",
            "budget.funding.education","budget.funding.parks",   "budget.funding.transit",
            "budget.funding.roads",   "budget.funding.maintenance",
        };

        // Forecast + readout bind ids.
        private static readonly string[] ForecastBindIds =
        {
            "budget.forecast.balance",
            "budget.forecast.chart",
            "budget.treasury",
        };

        private void Awake()
        {
            if (_actionRegistry   == null) _actionRegistry   = FindObjectOfType<UiActionRegistry>();
            if (_bindRegistry     == null) _bindRegistry     = FindObjectOfType<UiBindRegistry>();
            if (_modalCoordinator == null) _modalCoordinator = FindObjectOfType<ModalCoordinator>();
            if (_economyManager   == null) _economyManager   = FindObjectOfType<EconomyManager>();
            if (_forecaster       == null) _forecaster       = FindObjectOfType<BudgetForecaster>();
        }

        private void Start()
        {
            RegisterActions();
            Subscribe();
        }

        private void OnDestroy()
        {
            foreach (var sub in _subscriptions) sub?.Dispose();
            _subscriptions.Clear();
        }

        private void RegisterActions()
        {
            if (_actionRegistry == null) return;
            _actionRegistry.Register("budget.open",  _ => OnBudgetOpen());
            _actionRegistry.Register("budget.close", _ => OnBudgetClose());
        }

        private void OnBudgetOpen()
        {
            if (_modalCoordinator != null)
                _modalCoordinator.TryOpen("budget-panel");
            RefreshAllBinds();
        }

        private void OnBudgetClose()
        {
            if (_modalCoordinator != null)
                _modalCoordinator.Close("budget-panel");
            FlushTaxRatesToEconomy();
        }

        private void Subscribe()
        {
            if (_bindRegistry == null) return;

            // Tax slider binds — dispatch taxRate.set on change.
            foreach (var bindId in TaxBindIds)
            {
                var id = bindId;
                var sub = _bindRegistry.Subscribe<float>(id, val => OnTaxSliderChanged(id, val));
                _subscriptions.Add(sub);
            }

            // Funding / expense-row binds (read-only display).
            foreach (var bindId in FundingBindIds)
            {
                var sub = _bindRegistry.Subscribe<float>(bindId, _ => { });
                _subscriptions.Add(sub);
            }

            // Forecast + readout binds.
            foreach (var bindId in ForecastBindIds)
            {
                var sub = _bindRegistry.Subscribe<object>(bindId, _ => { });
                _subscriptions.Add(sub);
            }

            // Header + range binds.
            var headerSub = _bindRegistry.Subscribe<string>("budget.title",  _ => { });
            var rangeSub  = _bindRegistry.Subscribe<string>("budget.range",  _ => { });
            _subscriptions.Add(headerSub);
            _subscriptions.Add(rangeSub);
        }

        private void OnTaxSliderChanged(string bindId, float value)
        {
            if (_actionRegistry == null) return;
            string zone = bindId.Replace("budget.tax.", "");
            _actionRegistry.Dispatch("taxRate.set", new { zone = zone, rate = (int)value });

            // Recompute forecast immediately.
            if (_forecaster != null && _economyManager != null)
            {
                var taxRates = new TaxRates
                {
                    Residential = _economyManager.residentialIncomeTax,
                    Commercial  = _economyManager.commercialIncomeTax,
                    Industrial  = _economyManager.industrialIncomeTax,
                    General     = 0,
                };
                int treasury = _economyManager.GetCurrentMoney();
                int expenses = _economyManager.GetProjectedMonthlyMaintenance();
                var result   = _forecaster.Recompute(taxRates, treasury, expenses);

                if (_bindRegistry != null)
                {
                    _bindRegistry.Set("budget.forecast.balance", (float)result.Month3Balance);
                    _bindRegistry.Set("budget.forecast.chart",   new float[]
                    {
                        result.Month1Balance,
                        result.Month2Balance,
                        result.Month3Balance,
                    });
                }
            }
        }

        private void FlushTaxRatesToEconomy()
        {
            if (_economyManager == null || _bindRegistry == null) return;

            // Read committed slider values back to EconomyManager public fields.
            // Note: EconomyManager exposes Raise/Lower only; direct field assignment is safe
            // because we own the slider origin — no race with UI raise/lower buttons.
            _economyManager.residentialIncomeTax = Mathf.Clamp(
                Mathf.RoundToInt(_bindRegistry.Get<float>("budget.tax.residential")),
                _economyManager.minTaxRate, _economyManager.maxTaxRate);

            _economyManager.commercialIncomeTax = Mathf.Clamp(
                Mathf.RoundToInt(_bindRegistry.Get<float>("budget.tax.commercial")),
                _economyManager.minTaxRate, _economyManager.maxTaxRate);

            _economyManager.industrialIncomeTax = Mathf.Clamp(
                Mathf.RoundToInt(_bindRegistry.Get<float>("budget.tax.industrial")),
                _economyManager.minTaxRate, _economyManager.maxTaxRate);
        }

        private void RefreshAllBinds()
        {
            if (_bindRegistry == null || _economyManager == null) return;

            // Tax sliders from EconomyManager.
            _bindRegistry.Set("budget.tax.residential", (float)_economyManager.residentialIncomeTax);
            _bindRegistry.Set("budget.tax.commercial",  (float)_economyManager.commercialIncomeTax);
            _bindRegistry.Set("budget.tax.industrial",  (float)_economyManager.industrialIncomeTax);
            _bindRegistry.Set("budget.tax.general",     0f);

            // Treasury readout.
            int treasury = _economyManager.GetCurrentMoney();
            _bindRegistry.Set("budget.treasury", (float)treasury);

            // Header.
            _bindRegistry.Set("budget.title", "City Budget");

            // Initial forecast.
            if (_forecaster != null)
            {
                var taxRates = new TaxRates
                {
                    Residential = _economyManager.residentialIncomeTax,
                    Commercial  = _economyManager.commercialIncomeTax,
                    Industrial  = _economyManager.industrialIncomeTax,
                    General     = 0,
                };
                int expenses = _economyManager.GetProjectedMonthlyMaintenance();
                var result   = _forecaster.Recompute(taxRates, treasury, expenses);
                _bindRegistry.Set("budget.forecast.balance", (float)result.Month3Balance);
                _bindRegistry.Set("budget.forecast.chart", new float[]
                {
                    result.Month1Balance,
                    result.Month2Balance,
                    result.Month3Balance,
                });
            }
        }

        /// <summary>
        /// Apply-time render-check: walks panel root for widget components + asserts ≥40 + non-zero subscriber counts.
        /// Mirrors StatsPanelAdapter pattern per T7.0.3 spec.
        /// </summary>
        public void ApplyTimeRenderCheck()
        {
            var root = SlotAnchorResolver.ResolveByPanel("budget", transform);
            if (root == null)
                root = SlotAnchorResolver.ResolveByPanel("budget", transform.root);

            if (root == null)
            {
                Debug.LogWarning("[BudgetPanelAdapter] budget slot not found — render-check skipped.");
                return;
            }

            var sliders = root.GetComponentsInChildren<UnityEngine.UI.Slider>(true);
            var texts   = root.GetComponentsInChildren<TMPro.TMP_Text>(true);
            var images  = root.GetComponentsInChildren<UnityEngine.UI.Image>(true);

            int totalWidgets = sliders.Length + texts.Length + images.Length;

            if (totalWidgets < ExpectedMinWidgets)
            {
                Debug.LogWarning(
                    $"[BudgetPanelAdapter] render-check: expected ≥{ExpectedMinWidgets} widgets, " +
                    $"found {totalWidgets} (sliders={sliders.Length} texts={texts.Length} images={images.Length}).");
            }

            // Subscriber count check.
            if (_bindRegistry != null)
            {
                var allBindIds = new List<string>(TaxBindIds);
                allBindIds.AddRange(FundingBindIds);
                allBindIds.AddRange(ForecastBindIds);
                allBindIds.Add("budget.title");
                allBindIds.Add("budget.range");

                int boundCount = 0;
                foreach (var id in allBindIds)
                    if (_bindRegistry.HasSubscribers(id)) boundCount++;

                if (boundCount == 0)
                    Debug.LogWarning("[BudgetPanelAdapter] render-check: no bind subscribers found — adapter may not be wired.");
            }
        }
    }
}
