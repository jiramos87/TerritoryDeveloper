using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Territory.Economy;

namespace Territory.Tests.EditMode.Economy
{
    /// <summary>
    /// End-to-end Example 3 from zone-s economy exploration: treasury 1200 → issue 5000 @ 24 mo,
    /// then one monthly repayment. Save capture round-trip on bond registry.
    /// </summary>
    public class BondIssuanceIntegrationTests
    {
        private GameObject cityStatsGO;
        private GameObject economyGO;
        private GameObject clampGO;
        private GameObject ledgerGO;

        private CityStats cityStats;
        private EconomyManager economy;
        private TreasuryFloorClampService clamp;
        private BondLedgerService ledger;

        [SetUp]
        public void SetUp()
        {
            cityStatsGO = new GameObject("Ex3CityStats");
            cityStats = cityStatsGO.AddComponent<CityStats>();
            cityStats.money = 1200;

            economyGO = new GameObject("Ex3Economy");
            economy = economyGO.AddComponent<EconomyManager>();
            economy.cityStats = cityStats;

            clampGO = new GameObject("Ex3Clamp");
            clamp = clampGO.AddComponent<TreasuryFloorClampService>();

            FieldInfo clampField = typeof(EconomyManager)
                .GetField("treasuryFloorClamp", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(clampField);
            clampField.SetValue(economy, clamp);

            ledgerGO = new GameObject("Ex3Ledger");
            ledger = ledgerGO.AddComponent<BondLedgerService>();

            FieldInfo econField = typeof(BondLedgerService)
                .GetField("economy", BindingFlags.NonPublic | BindingFlags.Instance);
            econField.SetValue(ledger, economy);

            FieldInfo floorField = typeof(BondLedgerService)
                .GetField("treasuryFloor", BindingFlags.NonPublic | BindingFlags.Instance);
            floorField.SetValue(ledger, clamp);
        }

        [TearDown]
        public void TearDown()
        {
            if (ledgerGO != null) Object.DestroyImmediate(ledgerGO);
            if (clampGO != null) Object.DestroyImmediate(clampGO);
            if (economyGO != null) Object.DestroyImmediate(economyGO);
            if (cityStatsGO != null) Object.DestroyImmediate(cityStatsGO);
        }

        [Test]
        public void Example3_Issue5000At24Months_TreasuryAndRepaymentMatchDoc()
        {
            Assert.AreEqual(1200, cityStats.money);
            bool ok = ledger.TryIssueBond(0, 5000, 24);
            Assert.IsTrue(ok);
            Assert.AreEqual(6200, cityStats.money);

            BondData bond = ledger.GetActiveBond(0);
            Assert.IsNotNull(bond);
            Assert.AreEqual(233, bond.monthlyRepayment);

            ledger.ProcessMonthlyRepayment(0);

            Assert.AreEqual(5967, cityStats.money);
            Assert.IsNotNull(ledger.GetActiveBond(0));
            Assert.AreEqual(23, ledger.GetActiveBond(0).monthsRemaining);
        }

        [Test]
        public void Example3_SaveCapture_RoundTrip_PreservesBondFields()
        {
            ledger.TryIssueBond(0, 5000, 24);
            ledger.ProcessMonthlyRepayment(0);

            List<BondData> captured = ledger.CaptureSaveData();
            Assert.AreEqual(1, captured.Count);

            var freshGo = new GameObject("Ex3FreshLedger");
            var fresh = freshGo.AddComponent<BondLedgerService>();

            FieldInfo econField = typeof(BondLedgerService)
                .GetField("economy", BindingFlags.NonPublic | BindingFlags.Instance);
            econField.SetValue(fresh, economy);

            FieldInfo floorField = typeof(BondLedgerService)
                .GetField("treasuryFloor", BindingFlags.NonPublic | BindingFlags.Instance);
            floorField.SetValue(fresh, clamp);

            fresh.RestoreFromSaveData(captured);

            BondData restored = fresh.GetActiveBond(0);
            Assert.IsNotNull(restored);
            Assert.AreEqual(23, restored.monthsRemaining);
            Assert.AreEqual(233, restored.monthlyRepayment);

            Object.DestroyImmediate(freshGo);
        }
    }
}
