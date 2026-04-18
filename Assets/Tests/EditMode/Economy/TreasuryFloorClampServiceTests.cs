using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Territory.Economy;

namespace Territory.Tests.EditMode.Economy
{
    /// <summary>
    /// EditMode regression tests for TreasuryFloorClampService.
    /// Locks Stage 1.2 floor-clamp behavior:
    ///   TrySpend happy path, floor-clamp block, CanAfford boundary,
    ///   and SpendMoney delegation regression.
    /// </summary>
    public class TreasuryFloorClampServiceTests
    {
        private GameObject cityStatsGO;
        private GameObject economyGO;
        private GameObject clampGO;

        private CityStats cityStats;
        private EconomyManager economy;
        private TreasuryFloorClampService clamp;

        [SetUp]
        public void SetUp()
        {
            // 1. CityStats — public money field, safe to set directly before any Awake gate.
            cityStatsGO = new GameObject("TestCityStats");
            cityStats = cityStatsGO.AddComponent<CityStats>();
            cityStats.money = 200;

            // 2. EconomyManager — Awake runs; FindObjectOfType<TreasuryFloorClampService>
            //    returns null at this point (not yet in scene), logs a warning (expected).
            economyGO = new GameObject("TestEconomy");
            economy = economyGO.AddComponent<EconomyManager>();

            // Wire cityStats directly (Start doesn't fire in EditMode).
            economy.cityStats = cityStats;
            // gameNotificationManager intentionally null: GameNotificationManager.Awake
            // calls InitializeComponents() which NPEs on a null notificationPanel serialized
            // field (assigned only via Inspector). Notification emission is not assertable
            // in headless EditMode without full UI scaffold; block-path tests assert on the
            // observable clamp behavior (return value + balance unchanged) instead.

            // 3. TreasuryFloorClampService — Awake uses FindObjectOfType<EconomyManager>
            //    which finds economyGO from step 2.
            clampGO = new GameObject("TestTreasuryFloorClamp");
            clamp = clampGO.AddComponent<TreasuryFloorClampService>();
            // Awake resolved economy via FindObjectOfType — no manual wiring needed.

            // 4. Back-wire treasuryFloorClamp into EconomyManager (private [SerializeField]).
            //    EconomyManager.Awake ran before the clamp existed; patch via reflection.
            FieldInfo clampField = typeof(EconomyManager)
                .GetField("treasuryFloorClamp", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(clampField, "Reflection: treasuryFloorClamp field not found on EconomyManager");
            clampField.SetValue(economy, clamp);
        }

        [TearDown]
        public void TearDown()
        {
            if (clampGO != null) Object.DestroyImmediate(clampGO);
            if (economyGO != null) Object.DestroyImmediate(economyGO);
            if (cityStatsGO != null) Object.DestroyImmediate(cityStatsGO);
        }

        /// <summary>
        /// Happy path: TrySpend(100) at balance=200 succeeds, balance drops to 100.
        /// </summary>
        [Test]
        public void TrySpend_Succeeds_WhenAmountLessThanBalance()
        {
            bool result = clamp.TrySpend(100, "Test spend");

            Assert.IsTrue(result, "TrySpend should return true when amount <= balance");
            Assert.AreEqual(100, clamp.CurrentBalance,
                "Balance should decrease by the spent amount");
        }

        /// <summary>
        /// Block path: TrySpend(300) at balance=200 fails, balance stays at 200.
        /// Notification emission is not assertable in headless EditMode (GameNotificationManager
        /// requires Inspector-assigned UI refs that cannot be wired before Awake fires).
        /// The clamp block is confirmed by the false return + unchanged balance.
        /// </summary>
        [Test]
        public void TrySpend_Blocks_WhenAmountGreaterThanBalance()
        {
            bool result = clamp.TrySpend(300, "Test overspend");

            Assert.IsFalse(result, "TrySpend should return false when amount > balance");
            Assert.AreEqual(200, clamp.CurrentBalance,
                "Balance must remain unchanged when TrySpend is blocked");
        }

        /// <summary>
        /// Boundary: CanAfford(200) true at exact balance; CanAfford(201) false one above.
        /// </summary>
        [Test]
        public void CanAfford_BoundaryExact()
        {
            Assert.IsTrue(clamp.CanAfford(200),
                "CanAfford(200) must be true when balance == 200");
            Assert.IsFalse(clamp.CanAfford(201),
                "CanAfford(201) must be false when balance == 200");
        }

        /// <summary>
        /// Regression (TECH-381): EconomyManager.SpendMoney delegates to TreasuryFloorClampService.
        /// SpendMoney(300) at balance=200 must leave money unchanged (floor-clamp blocks the spend).
        /// </summary>
        [Test]
        public void SpendMoney_Regression_BlocksNegative()
        {
            bool result = economy.SpendMoney(300, "Test regression");

            Assert.IsFalse(result, "SpendMoney should return false when amount > balance");
            Assert.AreEqual(200, cityStats.money,
                "cityStats.money must stay at 200 — floor clamp prevents the deduction");
        }
    }
}
