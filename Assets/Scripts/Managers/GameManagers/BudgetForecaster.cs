using Territory.Economy;
using Territory.UI.Registry;
using UnityEngine;

namespace Territory.Simulation
{
    /// <summary>
    /// Wave B3 (TECH-27089) — 3-month budget forecast service.
    /// Subscribes to tax slider binds via UiBindRegistry; recomputes forecast on change.
    /// Exposes Recompute(taxRates, currentTreasury, monthlyExpenses) for adapter + tests.
    /// MonoBehaviour; mount in CityScene (Inv #4).
    /// </summary>
    public class BudgetForecaster : MonoBehaviour
    {
        [SerializeField] private EconomyManager _economyManager;
        [SerializeField] private UiBindRegistry  _bindRegistry;

        private void Awake()
        {
            if (_economyManager == null) _economyManager = FindObjectOfType<EconomyManager>();
            if (_bindRegistry   == null) _bindRegistry   = FindObjectOfType<UiBindRegistry>();
        }

        private void Start()
        {
            SubscribeToTaxSliders();
        }

        private void SubscribeToTaxSliders()
        {
            if (_bindRegistry == null) return;

            _bindRegistry.Subscribe<float>("budget.tax.residential", _ => RecomputeFromManager());
            _bindRegistry.Subscribe<float>("budget.tax.commercial",  _ => RecomputeFromManager());
            _bindRegistry.Subscribe<float>("budget.tax.industrial",  _ => RecomputeFromManager());
            _bindRegistry.Subscribe<float>("budget.tax.general",     _ => RecomputeFromManager());
        }

        /// <summary>
        /// Compute 3-month forecast given tax rates, treasury balance, and monthly expenses.
        /// Returns ForecastResult with balance projection per month.
        /// </summary>
        public ForecastResult Recompute(TaxRates taxRates, int currentTreasury, int monthlyExpenses)
        {
            int monthlyIncome = ComputeMonthlyIncome(taxRates);
            int netPerMonth   = monthlyIncome - monthlyExpenses;

            return new ForecastResult
            {
                Month1Balance = currentTreasury + netPerMonth,
                Month2Balance = currentTreasury + netPerMonth * 2,
                Month3Balance = currentTreasury + netPerMonth * 3,
                MonthlyIncome  = monthlyIncome,
                MonthlyExpenses = monthlyExpenses,
                NetPerMonth    = netPerMonth,
            };
        }

        /// <summary>
        /// Projects treasury balance across <paramref name="monthCount"/> months given
        /// tax rates + monthly expenses. Returns float[] of length monthCount where
        /// arr[i] = currentTreasury + netPerMonth * (i + 1).
        /// </summary>
        public float[] RecomputeRange(TaxRates taxRates, int currentTreasury, int monthlyExpenses, int monthCount)
        {
            int safeCount = Mathf.Max(1, monthCount);
            int monthlyIncome = ComputeMonthlyIncome(taxRates);
            int netPerMonth   = monthlyIncome - monthlyExpenses;
            var result = new float[safeCount];
            for (int i = 0; i < safeCount; i++)
                result[i] = currentTreasury + netPerMonth * (i + 1);
            return result;
        }

        private void RecomputeFromManager()
        {
            if (_economyManager == null || _bindRegistry == null) return;

            var taxRates = new TaxRates
            {
                Residential = _economyManager.residentialIncomeTax,
                Commercial  = _economyManager.commercialIncomeTax,
                Industrial  = _economyManager.industrialIncomeTax,
                General     = 0,
            };

            int treasury  = _economyManager.GetCurrentMoney();
            int expenses  = _economyManager.GetProjectedMonthlyMaintenance();
            var result    = Recompute(taxRates, treasury, expenses);

            // Push forecast binds.
            _bindRegistry.Set("budget.treasury",        (float)treasury);
            _bindRegistry.Set("budget.forecast.balance", (float)result.Month3Balance);
            _bindRegistry.Set("budget.forecast.chart",   new float[]
            {
                result.Month1Balance,
                result.Month2Balance,
                result.Month3Balance,
            });
        }

        private static int ComputeMonthlyIncome(TaxRates rates)
        {
            // Simplified projection: sum of tax rates as proxy income weight.
            return rates.Residential + rates.Commercial + rates.Industrial + rates.General;
        }
    }

    /// <summary>3-month budget projection result. Plain data class.</summary>
    public sealed class ForecastResult
    {
        public int Month1Balance;
        public int Month2Balance;
        public int Month3Balance;
        public int MonthlyIncome;
        public int MonthlyExpenses;
        public int NetPerMonth;
    }

    /// <summary>Tax rates snapshot passed to BudgetForecaster.Recompute.</summary>
    public sealed class TaxRates
    {
        public int Residential;
        public int Commercial;
        public int Industrial;
        public int General;
    }
}
