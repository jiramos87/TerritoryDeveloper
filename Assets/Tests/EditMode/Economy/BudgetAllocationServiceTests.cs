using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Territory.Economy;

namespace Territory.Tests.EditMode.Economy
{
    /// <summary>
    /// EditMode regression tests for BudgetAllocationService.
    /// Locks Stage 1.3 envelope-budget behavior:
    ///   TryDraw block paths (envelope, treasury), happy path with dual decrement,
    ///   MonthlyReset, SetEnvelopePct normalization, and all-zero batch rejection.
    /// </summary>
    public class BudgetAllocationServiceTests
    {
        private GameObject cityStatsGO;
        private GameObject economyGO;
        private GameObject clampGO;
        private GameObject budgetGO;

        private CityStats cityStats;
        private EconomyManager economy;
        private TreasuryFloorClampService clamp;
        private BudgetAllocationService budget;

        private FieldInfo _envelopePctField;
        private FieldInfo _globalMonthlyCapField;
        private FieldInfo _currentMonthRemainingField;

        [SetUp]
        public void SetUp()
        {
            // 1. CityStats — public money field; safe to set before any Awake gate.
            cityStatsGO = new GameObject("TestCityStats");
            cityStats = cityStatsGO.AddComponent<CityStats>();
            cityStats.money = 5000;

            // 2. EconomyManager — Awake runs; FindObjectOfType<TreasuryFloorClampService>
            //    returns null (not yet in scene); logs a warning (expected).
            economyGO = new GameObject("TestEconomy");
            economy = economyGO.AddComponent<EconomyManager>();
            economy.cityStats = cityStats;
            // gameNotificationManager intentionally null — NPEs on notificationPanel in headless EditMode.

            // 3. TreasuryFloorClampService — Awake attempts FindObjectOfType<EconomyManager>.
            //    AddComponent timing in batch EditMode is non-deterministic; explicitly back-wire
            //    clamp.economy below (same reason we back-wire economy.treasuryFloorClamp in step 4).
            clampGO = new GameObject("TestTreasuryFloorClamp");
            clamp = clampGO.AddComponent<TreasuryFloorClampService>();

            // 3a. Back-wire economy into TreasuryFloorClampService (private [SerializeField]).
            FieldInfo clampEconomyField = typeof(TreasuryFloorClampService)
                .GetField("economy", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(clampEconomyField, "Reflection: economy not found on TreasuryFloorClampService");
            clampEconomyField.SetValue(clamp, economy);

            // 4. Back-wire clamp into EconomyManager (private [SerializeField]; Awake ran before clamp existed).
            FieldInfo clampField = typeof(EconomyManager)
                .GetField("treasuryFloorClamp", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(clampField, "Reflection: treasuryFloorClamp not found on EconomyManager");
            clampField.SetValue(economy, clamp);

            // 5. BudgetAllocationService — Awake finds economy + clamp via FindObjectOfType.
            //    ZoneSubTypeRegistry absent → LogError (expected; not used by TryDraw or MonthlyReset).
            budgetGO = new GameObject("TestBudgetAllocation");
            budget = budgetGO.AddComponent<BudgetAllocationService>();

            // 6. Back-wire treasuryFloor into BudgetAllocationService via reflection.
            //    FindObjectOfType in Awake is non-deterministic in EditMode batch runner
            //    when components are added programmatically; explicit wiring guarantees
            //    the clamp reference is the instance we created above.
            FieldInfo treasuryFloorField = typeof(BudgetAllocationService)
                .GetField("treasuryFloor", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(treasuryFloorField, "Reflection: treasuryFloor not found on BudgetAllocationService");
            treasuryFloorField.SetValue(budget, clamp);

            // Also back-wire economy into BudgetAllocationService for symmetry.
            FieldInfo budgetEconomyField = typeof(BudgetAllocationService)
                .GetField("economy", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(budgetEconomyField, "Reflection: economy not found on BudgetAllocationService");
            budgetEconomyField.SetValue(budget, economy);

            // Cache private backing-field refs (set per-test via SeedBudget).
            _envelopePctField = typeof(BudgetAllocationService)
                .GetField("envelopePct", BindingFlags.NonPublic | BindingFlags.Instance);
            _globalMonthlyCapField = typeof(BudgetAllocationService)
                .GetField("globalMonthlyCap", BindingFlags.NonPublic | BindingFlags.Instance);
            _currentMonthRemainingField = typeof(BudgetAllocationService)
                .GetField("currentMonthRemaining", BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(_envelopePctField, "Reflection: envelopePct not found on BudgetAllocationService");
            Assert.IsNotNull(_globalMonthlyCapField, "Reflection: globalMonthlyCap not found on BudgetAllocationService");
            Assert.IsNotNull(_currentMonthRemainingField, "Reflection: currentMonthRemaining not found on BudgetAllocationService");
        }

        [TearDown]
        public void TearDown()
        {
            if (budgetGO != null) Object.DestroyImmediate(budgetGO);
            if (clampGO != null) Object.DestroyImmediate(clampGO);
            if (economyGO != null) Object.DestroyImmediate(economyGO);
            if (cityStatsGO != null) Object.DestroyImmediate(cityStatsGO);
        }

        private void SeedBudget(int cap, float[] pcts, int[] remaining)
        {
            _globalMonthlyCapField.SetValue(budget, cap);

            float[] pctCopy = new float[7];
            System.Array.Copy(pcts, pctCopy, 7);
            _envelopePctField.SetValue(budget, pctCopy);

            int[] remCopy = new int[7];
            System.Array.Copy(remaining, remCopy, 7);
            _currentMonthRemainingField.SetValue(budget, remCopy);
        }

        private static float[] UniformPcts()
        {
            float[] p = new float[7];
            for (int i = 0; i < 7; i++) p[i] = 1f / 7f;
            return p;
        }

        /// <summary>
        /// (a) TryDraw returns false and leaves treasury unchanged when envelope[0] is exhausted.
        /// </summary>
        [Test]
        public void TryDraw_Blocks_WhenEnvelopeExhausted()
        {
            int[] rem = new int[7]; // all zero — envelope exhausted
            SeedBudget(1000, UniformPcts(), rem);
            int balanceBefore = clamp.CurrentBalance;

            bool result = budget.TryDraw(0, 100);

            Assert.IsFalse(result, "TryDraw must return false when envelope[0] == 0");
            Assert.AreEqual(balanceBefore, clamp.CurrentBalance,
                "Treasury must not change when blocked by envelope exhaustion");
        }

        /// <summary>
        /// (b) TryDraw returns false and leaves envelope unchanged when treasury cannot cover amount.
        /// </summary>
        [Test]
        public void TryDraw_Blocks_WhenTreasuryCannotAfford()
        {
            cityStats.money = 10; // below requested amount
            int[] rem = new int[] { 500, 500, 500, 500, 500, 500, 500 }; // fat envelope
            SeedBudget(1000, UniformPcts(), rem);

            bool result = budget.TryDraw(0, 100);

            Assert.IsFalse(result, "TryDraw must return false when treasury cannot afford the draw");
            int[] postRem = (int[])_currentMonthRemainingField.GetValue(budget);
            Assert.AreEqual(500, postRem[0],
                "Envelope must not decrement when blocked by insufficient treasury");
        }

        /// <summary>
        /// (c) TryDraw returns true and both envelope and treasury decrement by amount.
        /// </summary>
        [Test]
        public void TryDraw_Succeeds_WhenBothOk_AndBothDecrement()
        {
            cityStats.money = 5000;
            int[] rem = new int[] { 500, 500, 500, 500, 500, 500, 500 };
            SeedBudget(1000, UniformPcts(), rem);

            bool result = budget.TryDraw(0, 100);

            Assert.IsTrue(result, "TryDraw must return true when both envelope and treasury are sufficient");
            int[] postRem = (int[])_currentMonthRemainingField.GetValue(budget);
            Assert.AreEqual(400, postRem[0], "Envelope[0] must decrement by 100 on successful draw");
            Assert.AreEqual(4900, clamp.CurrentBalance, "Treasury must decrement by 100 on successful draw");
        }

        /// <summary>
        /// (d) MonthlyReset seeds currentMonthRemaining[i] == (int)(cap × envelopePct[i]) for all i.
        /// </summary>
        [Test]
        public void MonthlyReset_RestoresCurrentMonthRemaining()
        {
            int[] rem = new int[7]; // drained
            SeedBudget(1000, UniformPcts(), rem);

            budget.MonthlyReset();

            int[] postReset = (int[])_currentMonthRemainingField.GetValue(budget);
            float[] livePcts = (float[])_envelopePctField.GetValue(budget);
            int cap = (int)_globalMonthlyCapField.GetValue(budget);
            for (int i = 0; i < 7; i++)
            {
                int expected = (int)(cap * livePcts[i]);
                Assert.AreEqual(expected, postReset[i],
                    $"currentMonthRemaining[{i}] must equal (int)(cap × pct[{i}]) after MonthlyReset");
            }
        }

        /// <summary>
        /// (e) SetEnvelopePct normalizes envelopePct so the sum equals 1.0 within 1e-6.
        /// </summary>
        [Test]
        public void SetEnvelopePct_NormalizesSum_ToOne()
        {
            SeedBudget(1000, UniformPcts(), new int[7]);

            budget.SetEnvelopePct(0, 0.5f);

            float[] live = (float[])_envelopePctField.GetValue(budget);
            float sum = 0f;
            for (int i = 0; i < 7; i++) sum += live[i];
            Assert.AreEqual(1f, sum, 1e-6f,
                "envelopePct must sum to 1.0 (within 1e-6) after SetEnvelopePct");
        }

        /// <summary>
        /// (f) SetEnvelopePctsBatch with all-zero input is rejected and prior state is fully preserved.
        /// </summary>
        [Test]
        public void SetEnvelopePctsBatch_AllZeroInput_Rejected_PriorStatePreserved()
        {
            // Seed a known valid state via the public batch API.
            float[] validPcts = UniformPcts();
            budget.SetEnvelopePctsBatch(validPcts);
            float[] snapshot = (float[])((float[])_envelopePctField.GetValue(budget)).Clone();

            budget.SetEnvelopePctsBatch(new float[7]); // all zeros — must be rejected

            float[] live = (float[])_envelopePctField.GetValue(budget);
            for (int i = 0; i < 7; i++)
                Assert.AreEqual(snapshot[i], live[i], 1e-7f,
                    $"envelopePct[{i}] must be preserved after all-zero batch rejection");
        }
    }
}
