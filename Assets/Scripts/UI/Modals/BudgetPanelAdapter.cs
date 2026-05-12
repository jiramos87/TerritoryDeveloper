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
    /// Registers action.budget-panel-toggle / action.budget-panel-close action handlers
    /// (DB-canonical action ids per button_detail — TECH-29752).
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
        [SerializeField] private GrowthBudgetManager _growthBudgetManager;

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

        // Growth-budget bind ids (Stage 10 hotfix — Total/Zoning/Roads %).
        private static readonly string[] GrowthBindIds =
        {
            "growth.total",
            "growth.zoning",
            "growth.roads",
        };

        private void Awake()
        {
            if (_actionRegistry   == null) _actionRegistry   = FindObjectOfType<UiActionRegistry>();
            if (_bindRegistry     == null) _bindRegistry     = FindObjectOfType<UiBindRegistry>();
            if (_modalCoordinator == null) _modalCoordinator = FindObjectOfType<ModalCoordinator>();
            if (_economyManager   == null) _economyManager   = FindObjectOfType<EconomyManager>();
            if (_forecaster       == null) _forecaster       = FindObjectOfType<BudgetForecaster>();
            if (_growthBudgetManager == null) _growthBudgetManager = FindObjectOfType<GrowthBudgetManager>();
            if (_forecaster == null)
            {
                // Scene didn't wire one — instantiate at runtime so panel always has forecast flow.
                // Forecaster.Awake() lazy-resolves EconomyManager + UiBindRegistry via FindObjectOfType.
                var forecasterGo = new GameObject("BudgetForecaster (auto)");
                _forecaster = forecasterGo.AddComponent<BudgetForecaster>();
            }
            // Stage 13 hotfix — register in Awake instead of Start. Panel root is registered
            // with ModalCoordinator (SetActive false) immediately after AddComponent, so Start
            // never fires on this adapter. Awake runs once on AddComponent regardless of active.
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
            // DB-canonical action ids (button_detail.action_id — TECH-29752).
            _actionRegistry.Register("action.budget-panel-toggle", _ => OnBudgetOpen());
            _actionRegistry.Register("action.budget-panel-close",  _ => OnBudgetClose());
            // Legacy aliases — kept for editor scripts / test harnesses referencing old ids.
            _actionRegistry.Register("budget.open",  _ => OnBudgetOpen());
            _actionRegistry.Register("budget.close", _ => OnBudgetClose());
        }

        private const string DefaultRange = "3mo";

        private void OnBudgetOpen()
        {
            if (_modalCoordinator != null)
                _modalCoordinator.TryOpen("budget-panel");
            RefreshAllBinds(DefaultRange);
            // Default to 3mo range on every open — keeps range tab in sync with chart length.
            if (_bindRegistry != null)
                _bindRegistry.Set("budget.range", DefaultRange);
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

            // Funding / expense-row binds are owned by bake-time renderers
            // (ServiceRowController) — adapter only publishes; renderers subscribe.

            // Forecast + readout binds.
            foreach (var bindId in ForecastBindIds)
            {
                var sub = _bindRegistry.Subscribe<object>(bindId, _ => { });
                _subscriptions.Add(sub);
            }

            // Header + range binds.
            var headerSub = _bindRegistry.Subscribe<string>("budget.title",  _ => { });
            var rangeSub  = _bindRegistry.Subscribe<string>("budget.range",  OnRangeChanged);
            _subscriptions.Add(headerSub);
            _subscriptions.Add(rangeSub);

            // Growth-budget slider binds — dispatch SetCategoryPercent/SetGrowthBudgetPercent on change.
            foreach (var bindId in GrowthBindIds)
            {
                var id = bindId;
                var sub = _bindRegistry.Subscribe<float>(id, val => OnGrowthSliderChanged(id, val));
                _subscriptions.Add(sub);
            }
        }

        private void OnGrowthSliderChanged(string bindId, float value)
        {
            if (_growthBudgetManager == null) return;
            int pct = Mathf.Clamp(Mathf.RoundToInt(value), 0, 100);
            switch (bindId)
            {
                case "growth.total":   _growthBudgetManager.SetGrowthBudgetPercent(pct); break;
                case "growth.zoning":  _growthBudgetManager.SetCategoryPercent(GrowthCategory.Zoning, pct); break;
                case "growth.roads":   _growthBudgetManager.SetCategoryPercent(GrowthCategory.Roads,  pct); break;
            }
        }

        private void OnTaxSliderChanged(string bindId, float value)
        {
            if (_actionRegistry == null) return;
            string zone = bindId.Replace("budget.tax.", "");
            _actionRegistry.Dispatch("taxRate.set", new { zone = zone, rate = (int)value });

            // Recompute forecast immediately at current range.
            if (_forecaster != null && _economyManager != null && _bindRegistry != null)
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
                string range = DefaultRange;
                try { range = _bindRegistry.Get<string>("budget.range"); }
                catch (System.Collections.Generic.KeyNotFoundException) { /* range bind not set yet — fall back to default. */ }
                int months = MonthsForRange(string.IsNullOrEmpty(range) ? DefaultRange : range);
                var series = _forecaster.RecomputeRange(taxRates, treasury, expenses, months);
                float endBalance = series.Length > 0 ? series[series.Length - 1] : treasury;
                _bindRegistry.Set("budget.forecast.balance", endBalance);
                _bindRegistry.Set("budget.forecast.chart",   series);
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

        private void OnRangeChanged(string range)
        {
            if (string.IsNullOrEmpty(range)) return;
            RefreshAllBinds(range);
        }

        private static int MonthsForRange(string range)
        {
            switch (range)
            {
                case "1mo": return 1;
                case "3mo": return 3;
                case "6mo": return 6;
                default:    return 3;
            }
        }

        // Maps sub-type id (ZoneSubTypeRegistry order: police, fire, education, health, parks, ...)
        // to the budget.funding.{cat} bind key. Index = subTypeId.
        private static readonly string[] SubTypeFundingKey =
        {
            "budget.funding.police",
            "budget.funding.fire",
            "budget.funding.education",
            "budget.funding.health",
            "budget.funding.parks",
        };

        private void PushFundingFromContributors(int totalMaintenance)
        {
            if (_bindRegistry == null) return;

            // Zero-init all keys so previous values clear on refresh.
            foreach (var key in FundingBindIds)
                _bindRegistry.Set(key, 0f);

            int subTypeAccum = 0;
            int powerAccum   = 0;
            int roadsAccum   = 0;

            if (_economyManager != null)
            {
                var snapshot = _economyManager.GetMaintenanceContributorsSnapshot();
                foreach (var c in snapshot)
                {
                    int cost = c.GetMonthlyMaintenance();
                    if (cost <= 0) continue;
                    string id = c.GetContributorId();
                    int subType = c.GetSubTypeId();
                    if (id == "power-aggregate")
                    {
                        powerAccum += cost;
                    }
                    else if (id == "road-aggregate")
                    {
                        roadsAccum += cost;
                    }
                    else if (subType >= 0 && subType < SubTypeFundingKey.Length)
                    {
                        var key = SubTypeFundingKey[subType];
                        var prev = _bindRegistry.Get<float>(key);
                        _bindRegistry.Set(key, prev + cost);
                        subTypeAccum += cost;
                    }
                }
            }

            _bindRegistry.Set("budget.funding.power", (float)powerAccum);
            _bindRegistry.Set("budget.funding.roads", (float)roadsAccum);
            _bindRegistry.Set("budget.funding.maintenance", (float)totalMaintenance);

            // Categories not yet wired to a maintenance contributor — surface the residual
            // (total − accounted) split evenly so rows render non-zero once city has expenses.
            int accountedFor = powerAccum + roadsAccum + subTypeAccum;
            int residual = Mathf.Max(0, totalMaintenance - accountedFor);
            string[] proxyKeys = { "budget.funding.water", "budget.funding.waste", "budget.funding.transit" };
            int per = residual / proxyKeys.Length;
            foreach (var key in proxyKeys)
                _bindRegistry.Set(key, (float)per);
        }

        private void RefreshAllBinds(string rangeKind)
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

            // Live funding values per category — sourced from maintenance contributors.
            int expenses = _economyManager.GetProjectedMonthlyMaintenance();
            PushFundingFromContributors(expenses);

            // Growth-budget initial values.
            if (_growthBudgetManager != null)
            {
                _bindRegistry.Set("growth.total",  (float)_growthBudgetManager.GetGrowthBudgetPercent());
                _bindRegistry.Set("growth.zoning", (float)_growthBudgetManager.GetCategoryPercent(GrowthCategory.Zoning));
                _bindRegistry.Set("growth.roads",  (float)_growthBudgetManager.GetCategoryPercent(GrowthCategory.Roads));
            }

            // Forecast across N months (driven by range tab).
            if (_forecaster != null)
            {
                var taxRates = new TaxRates
                {
                    Residential = _economyManager.residentialIncomeTax,
                    Commercial  = _economyManager.commercialIncomeTax,
                    Industrial  = _economyManager.industrialIncomeTax,
                    General     = 0,
                };
                int months = MonthsForRange(rangeKind);
                var series = _forecaster.RecomputeRange(taxRates, treasury, expenses, months);
                float endBalance = series.Length > 0 ? series[series.Length - 1] : treasury;
                _bindRegistry.Set("budget.forecast.balance", endBalance);
                _bindRegistry.Set("budget.forecast.chart",   series);
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
                    Debug.LogError("[BudgetPanelAdapter] render-check: no bind subscribers found — adapter may not be wired.");
            }
        }
    }
}
