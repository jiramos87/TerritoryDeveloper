using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Territory.Economy;

namespace Territory.Tests.EditMode.Economy
{
    /// <summary>
    /// EditMode regression tests for BondLedgerService.
    /// Locks Stage 4 bond-ledger behavior:
    ///   issuance happy path, duplicate-tier block, monthly repayment,
    ///   arrears on insufficient funds, bond completion removal, save/load round-trip.
    /// </summary>
    public class BondLedgerServiceTests
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
            cityStatsGO = new GameObject("TestCityStats");
            cityStats = cityStatsGO.AddComponent<CityStats>();
            cityStats.money = 10000;

            economyGO = new GameObject("TestEconomy");
            economy = economyGO.AddComponent<EconomyManager>();
            economy.cityStats = cityStats;

            clampGO = new GameObject("TestTreasuryFloorClamp");
            clamp = clampGO.AddComponent<TreasuryFloorClampService>();

            FieldInfo clampField = typeof(EconomyManager)
                .GetField("treasuryFloorClamp", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(clampField, "Reflection: treasuryFloorClamp field not found on EconomyManager");
            clampField.SetValue(economy, clamp);

            ledgerGO = new GameObject("TestBondLedger");
            ledger = ledgerGO.AddComponent<BondLedgerService>();

            FieldInfo econField = typeof(BondLedgerService)
                .GetField("economy", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(econField, "Reflection: economy field not found on BondLedgerService");
            econField.SetValue(ledger, economy);

            FieldInfo floorField = typeof(BondLedgerService)
                .GetField("treasuryFloor", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(floorField, "Reflection: treasuryFloor field not found on BondLedgerService");
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
        public void TryIssueBond_Succeeds_WhenNoActive_BalanceCredited_RegistryPopulated()
        {
            int before = cityStats.money;
            bool ok = ledger.TryIssueBond(0, 5000, 24);

            Assert.IsTrue(ok, "TryIssueBond should succeed when no active bond on tier");
            Assert.AreEqual(before + 5000, cityStats.money,
                "Treasury should be credited with principal");
            Assert.IsNotNull(ledger.GetActiveBond(0),
                "Active bond should exist on tier 0 after issuance");
            Assert.AreEqual(24, ledger.GetActiveBond(0).monthsRemaining);
        }

        [Test]
        public void TryIssueBond_Rejects_DuplicateTier()
        {
            ledger.TryIssueBond(0, 5000, 24);
            int balanceAfterFirst = cityStats.money;

            bool second = ledger.TryIssueBond(0, 3000, 12);

            Assert.IsFalse(second, "Second issuance on same tier should be rejected");
            Assert.AreEqual(balanceAfterFirst, cityStats.money,
                "Treasury should not change on rejected issuance");
        }

        [Test]
        public void ProcessMonthlyRepayment_Decrements_And_Spends()
        {
            ledger.TryIssueBond(0, 5000, 24);
            BondData bond = ledger.GetActiveBond(0);
            int repayment = bond.monthlyRepayment;
            int balanceBefore = cityStats.money;

            ledger.ProcessMonthlyRepayment(0);

            Assert.AreEqual(balanceBefore - repayment, cityStats.money,
                "Treasury should be decremented by monthly repayment");
            Assert.AreEqual(23, ledger.GetActiveBond(0).monthsRemaining,
                "monthsRemaining should decrement by 1");
            Assert.IsFalse(ledger.GetActiveBond(0).arrears);
        }

        [Test]
        public void ProcessMonthlyRepayment_FlagsArrears_WhenInsufficientFunds()
        {
            ledger.TryIssueBond(0, 5000, 24);
            BondData bond = ledger.GetActiveBond(0);
            cityStats.money = 0;

            ledger.ProcessMonthlyRepayment(0);

            Assert.IsTrue(ledger.GetActiveBond(0).arrears,
                "Arrears should be flagged when treasury cannot cover repayment");
            Assert.AreEqual(0, cityStats.money,
                "Balance should remain at 0 (floor clamp blocks the spend)");
            Assert.AreEqual(24, ledger.GetActiveBond(0).monthsRemaining,
                "monthsRemaining should NOT decrement on failed repayment");
        }

        [Test]
        public void Bond_ClearsFromRegistry_WhenCompleted()
        {
            ledger.TryIssueBond(0, 1000, 1);
            Assert.IsNotNull(ledger.GetActiveBond(0));

            ledger.ProcessMonthlyRepayment(0);

            Assert.IsNull(ledger.GetActiveBond(0),
                "Bond should be removed from registry when monthsRemaining reaches 0");
        }

        [Test]
        public void SaveLoadRoundTrip_PreservesAllFields()
        {
            ledger.TryIssueBond(0, 5000, 24);
            BondData original = ledger.GetActiveBond(0);
            original.arrears = true;
            original.monthsRemaining = 18;

            List<BondData> captured = ledger.CaptureSaveData();
            Assert.AreEqual(1, captured.Count);

            var fresh = new GameObject("FreshLedger");
            var freshLedger = fresh.AddComponent<BondLedgerService>();
            freshLedger.RestoreFromSaveData(captured);

            BondData restored = freshLedger.GetActiveBond(0);
            Assert.IsNotNull(restored, "Restored bond should exist on tier 0");
            Assert.AreEqual(original.scaleTier, restored.scaleTier);
            Assert.AreEqual(original.principal, restored.principal);
            Assert.AreEqual(original.termMonths, restored.termMonths);
            Assert.AreEqual(original.monthlyRepayment, restored.monthlyRepayment);
            Assert.AreEqual(original.fixedInterestRate, restored.fixedInterestRate, 1e-6f);
            Assert.AreEqual(original.issuedOnDate, restored.issuedOnDate);
            Assert.AreEqual(original.monthsRemaining, restored.monthsRemaining);
            Assert.AreEqual(original.arrears, restored.arrears);

            Object.DestroyImmediate(fresh);
        }

        [Test]
        public void TryIssueBond_Rejects_NonPositiveInputs()
        {
            Assert.IsFalse(ledger.TryIssueBond(0, 0, 24), "Zero principal should be rejected");
            Assert.IsFalse(ledger.TryIssueBond(0, -100, 24), "Negative principal should be rejected");
            Assert.IsFalse(ledger.TryIssueBond(0, 5000, 0), "Zero term should be rejected");
            Assert.IsFalse(ledger.TryIssueBond(0, 5000, -1), "Negative term should be rejected");
        }
    }
}
