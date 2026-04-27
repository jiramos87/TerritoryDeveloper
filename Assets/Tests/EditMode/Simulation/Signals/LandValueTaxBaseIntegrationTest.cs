using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Territory.Economy;
using Territory.Simulation.Signals;

namespace Territory.Tests.EditMode.Simulation.Signals
{
    /// <summary>
    /// Stage 7 integration coverage for TECH-1892 — confirms <see cref="EconomyManager.GetProjectedMonthlyIncome"/>
    /// applies the land-value tax-base bonus driven by <see cref="CityStats.cityLandValueMean"/> +
    /// <see cref="SignalTuningWeightsAsset.LandValueIncomeMultiplier"/>.
    /// At cityLandValueMean=100 and multiplier=0.005 → +50% bonus on baseSum.
    /// </summary>
    [TestFixture]
    public class LandValueTaxBaseIntegrationTest
    {
        private GameObject cityStatsGO;
        private GameObject economyGO;
        private CityStats cityStats;
        private EconomyManager economyManager;
        private SignalTuningWeightsAsset weightsAsset;

        [SetUp]
        public void SetUp()
        {
            cityStatsGO = new GameObject("LVIntegCityStats");
            cityStats = cityStatsGO.AddComponent<CityStats>();
            cityStats.simulateGrowth = false;
            cityStats.residentialBuildingCount = 10;
            cityStats.commercialBuildingCount = 5;
            cityStats.industrialBuildingCount = 3;

            economyGO = new GameObject("LVIntegEconomy");
            economyManager = economyGO.AddComponent<EconomyManager>();
            economyManager.cityStats = cityStats;
            economyManager.residentialIncomeTax = 10;
            economyManager.commercialIncomeTax = 10;
            economyManager.industrialIncomeTax = 10;

            weightsAsset = ScriptableObject.CreateInstance<SignalTuningWeightsAsset>();
            FieldInfo weightsField = typeof(EconomyManager).GetField(
                "tuningWeights", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(weightsField, "EconomyManager.tuningWeights field missing");
            weightsField.SetValue(economyManager, weightsAsset);
        }

        [TearDown]
        public void TearDown()
        {
            if (economyGO != null) Object.DestroyImmediate(economyGO);
            if (cityStatsGO != null) Object.DestroyImmediate(cityStatsGO);
            if (weightsAsset != null) Object.DestroyImmediate(weightsAsset);
        }

        [Test]
        public void ZeroLandValueMean_NoBonus()
        {
            cityStats.cityLandValueMean = 0f;

            int income = economyManager.GetProjectedMonthlyIncome();

            int expectedBase = (10 + 5 + 3) * 10; // 180
            Assert.AreEqual(expectedBase, income);
        }

        [Test]
        public void LandValueMean100_AppliesFiftyPercentBonus()
        {
            // Default LandValueIncomeMultiplier=0.005f → at mean=100 multiplier=1.5f → +50%.
            cityStats.cityLandValueMean = 100f;

            int income = economyManager.GetProjectedMonthlyIncome();

            int baseSum = (10 + 5 + 3) * 10; // 180
            int expected = Mathf.RoundToInt(baseSum * 1.5f); // 270
            Assert.AreEqual(expected, income);
        }

        [Test]
        public void NegativeLandValueMean_FallsBackToBase_NoBonus()
        {
            cityStats.cityLandValueMean = -50f;

            int income = economyManager.GetProjectedMonthlyIncome();

            Assert.AreEqual((10 + 5 + 3) * 10, income, "Negative mean → defensive fallback to base sum");
        }

        [Test]
        public void NaNLandValueMean_FallsBackToBase_NoBonus()
        {
            cityStats.cityLandValueMean = float.NaN;

            int income = economyManager.GetProjectedMonthlyIncome();

            Assert.AreEqual((10 + 5 + 3) * 10, income, "NaN mean → defensive fallback to base sum");
        }
    }
}
