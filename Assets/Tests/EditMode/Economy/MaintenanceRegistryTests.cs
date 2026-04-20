using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Territory.Economy;

namespace Territory.Tests.EditMode.Economy
{
    /// <summary>
    /// EditMode regression tests for the IMaintenanceContributor registry.
    /// Locks Stage 5 behavior: deterministic sorted iteration, sub-type envelope
    /// draw vs general-pool treasury spend, road adapter parity with legacy formula.
    /// </summary>
    public class MaintenanceRegistryTests
    {
        private GameObject cityStatsGO;
        private GameObject economyGO;
        private GameObject clampGO;
        private GameObject budgetGO;

        private CityStats cityStats;
        private EconomyManager economy;
        private TreasuryFloorClampService clamp;
        private BudgetAllocationService budget;

        [SetUp]
        public void SetUp()
        {
            cityStatsGO = new GameObject("TestCityStats");
            cityStats = cityStatsGO.AddComponent<CityStats>();
            cityStats.money = 50000;

            economyGO = new GameObject("TestEconomy");
            economy = economyGO.AddComponent<EconomyManager>();
            economy.cityStats = cityStats;
            economy.maintenanceCostPerRoadCell = 8;
            economy.maintenanceCostPerPowerPlant = 350;

            clampGO = new GameObject("TestClamp");
            clamp = clampGO.AddComponent<TreasuryFloorClampService>();

            budgetGO = new GameObject("TestBudget");
            budget = budgetGO.AddComponent<BudgetAllocationService>();

            SetPrivateField(typeof(TreasuryFloorClampService), clamp, "economy", economy);
            SetPrivateField(typeof(EconomyManager), economy, "treasuryFloorClamp", clamp);
            SetPrivateField(typeof(EconomyManager), economy, "budgetAllocation", budget);
            SetPrivateField(typeof(BudgetAllocationService), budget, "treasuryFloor", clamp);
        }

        [TearDown]
        public void TearDown()
        {
            if (budgetGO != null) Object.DestroyImmediate(budgetGO);
            if (clampGO != null) Object.DestroyImmediate(clampGO);
            if (economyGO != null) Object.DestroyImmediate(economyGO);
            if (cityStatsGO != null) Object.DestroyImmediate(cityStatsGO);
        }

        [Test]
        public void Contributors_Sorted_By_Id_Ordinal()
        {
            var callOrder = new List<string>();

            economy.RegisterMaintenanceContributor(new FakeContributor("z-last", 10, -1, callOrder));
            economy.RegisterMaintenanceContributor(new FakeContributor("a-first", 20, -1, callOrder));
            economy.RegisterMaintenanceContributor(new FakeContributor("m-middle", 15, -1, callOrder));

            var snapshot = economy.GetMaintenanceContributorsSnapshot();
            snapshot.Sort((a, b) => string.Compare(a.GetContributorId(), b.GetContributorId(), System.StringComparison.Ordinal));

            foreach (var c in snapshot)
            {
                callOrder.Add(c.GetContributorId());
                c.GetMonthlyMaintenance();
            }

            Assert.AreEqual(3, callOrder.Count);
            Assert.AreEqual("a-first", callOrder[0]);
            Assert.AreEqual("m-middle", callOrder[1]);
            Assert.AreEqual("z-last", callOrder[2]);
        }

        [Test]
        public void Register_Unregister_Roundtrip()
        {
            var c = new FakeContributor("test", 100, -1);
            economy.RegisterMaintenanceContributor(c);
            Assert.AreEqual(1, economy.MaintenanceContributorCount);

            economy.UnregisterMaintenanceContributor(c);
            Assert.AreEqual(0, economy.MaintenanceContributorCount);
        }

        [Test]
        public void Clear_Removes_All_Contributors()
        {
            economy.RegisterMaintenanceContributor(new FakeContributor("a", 10, -1));
            economy.RegisterMaintenanceContributor(new FakeContributor("b", 20, -1));
            Assert.AreEqual(2, economy.MaintenanceContributorCount);

            economy.ClearMaintenanceContributors();
            Assert.AreEqual(0, economy.MaintenanceContributorCount);
        }

        [Test]
        public void Duplicate_Registration_Ignored()
        {
            var c = new FakeContributor("dup", 50, -1);
            economy.RegisterMaintenanceContributor(c);
            economy.RegisterMaintenanceContributor(c);
            Assert.AreEqual(1, economy.MaintenanceContributorCount);
        }

        [Test]
        public void GetProjectedMonthlyMaintenance_Sums_Contributors()
        {
            economy.RegisterMaintenanceContributor(new FakeContributor("a", 100, -1));
            economy.RegisterMaintenanceContributor(new FakeContributor("b", 250, -1));
            economy.RegisterMaintenanceContributor(new FakeContributor("c", 0, -1));

            Assert.AreEqual(350, economy.GetProjectedMonthlyMaintenance());
        }

        [Test]
        public void RoadAdapter_Matches_Legacy_Formula()
        {
            cityStats.roadCount = 25;

            var go = new GameObject("TestRoadAdapter");
            var adapter = go.AddComponent<RoadMaintenanceContributor>();
            SetPrivateField(typeof(RoadMaintenanceContributor), adapter, "cityStats", cityStats);
            SetPrivateField(typeof(RoadMaintenanceContributor), adapter, "economy", economy);

            int expected = Mathf.Max(0, cityStats.roadCount) * economy.maintenanceCostPerRoadCell;
            Assert.AreEqual(expected, adapter.GetMonthlyMaintenance());
            Assert.AreEqual("road-aggregate", adapter.GetContributorId());
            Assert.AreEqual(-1, adapter.GetSubTypeId());

            Object.DestroyImmediate(go);
        }

        [Test]
        public void PowerPlantAdapter_Matches_Legacy_Formula()
        {
            var go = new GameObject("TestPowerAdapter");
            var adapter = go.AddComponent<PowerPlantMaintenanceContributor>();
            SetPrivateField(typeof(PowerPlantMaintenanceContributor), adapter, "cityStats", cityStats);
            SetPrivateField(typeof(PowerPlantMaintenanceContributor), adapter, "economy", economy);

            int plantCount = cityStats.GetRegisteredPowerPlantCount();
            int expected = Mathf.Max(0, plantCount) * economy.maintenanceCostPerPowerPlant;
            Assert.AreEqual(expected, adapter.GetMonthlyMaintenance());
            Assert.AreEqual("power-aggregate", adapter.GetContributorId());
            Assert.AreEqual(-1, adapter.GetSubTypeId());

            Object.DestroyImmediate(go);
        }

        private static void SetPrivateField(System.Type type, object target, string fieldName, object value)
        {
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"Reflection: {fieldName} not found on {type.Name}");
            field.SetValue(target, value);
        }

        private class FakeContributor : IMaintenanceContributor
        {
            private readonly string _id;
            private readonly int _cost;
            private readonly int _subTypeId;
            private readonly List<string> _callLog;

            public FakeContributor(string id, int cost, int subTypeId, List<string> callLog = null)
            {
                _id = id;
                _cost = cost;
                _subTypeId = subTypeId;
                _callLog = callLog;
            }

            public int GetMonthlyMaintenance()
            {
                _callLog?.Add(_id);
                return _cost;
            }

            public string GetContributorId() => _id;
            public int GetSubTypeId() => _subTypeId;
        }
    }
}
