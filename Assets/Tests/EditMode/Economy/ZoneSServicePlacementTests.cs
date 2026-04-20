using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Territory.Core;
using Territory.Economy;
using Territory.Zones;

namespace Territory.Tests.EditMode.Economy
{
    /// <summary>
    /// EditMode integration tests for ZoneSService.PlaceStateServiceZone.
    /// Locks Stage 6 Examples 1 (place police OK) and 2 (envelope exhausted blocks placement).
    /// </summary>
    public class ZoneSServicePlacementTests
    {
        private GameObject cityStatsGO;
        private GameObject economyGO;
        private GameObject clampGO;
        private GameObject budgetGO;
        private GameObject registryGO;
        private GameObject gridGO;
        private GameObject zoneManagerGO;
        private GameObject serviceGO;
        private GameObject cellGO;

        private CityStats cityStats;
        private EconomyManager economy;
        private TreasuryFloorClampService clamp;
        private BudgetAllocationService budget;
        private ZoneSubTypeRegistry registry;
        private GridManager grid;
        private ZoneManager zoneManager;
        private ZoneSService service;

        [SetUp]
        public void SetUp()
        {
            cityStatsGO = new GameObject("TestCityStats");
            cityStats = cityStatsGO.AddComponent<CityStats>();
            cityStats.money = 10000;

            economyGO = new GameObject("TestEconomy");
            economy = economyGO.AddComponent<EconomyManager>();
            economy.cityStats = cityStats;

            clampGO = new GameObject("TestClamp");
            clamp = clampGO.AddComponent<TreasuryFloorClampService>();
            SetPrivateField(typeof(TreasuryFloorClampService), clamp, "economy", economy);
            SetPrivateField(typeof(EconomyManager), economy, "treasuryFloorClamp", clamp);

            budgetGO = new GameObject("TestBudget");
            budget = budgetGO.AddComponent<BudgetAllocationService>();
            SetPrivateField(typeof(BudgetAllocationService), budget, "economy", economy);
            SetPrivateField(typeof(BudgetAllocationService), budget, "treasuryFloor", clamp);
            SetPrivateField(typeof(EconomyManager), economy, "budgetAllocation", budget);

            registryGO = new GameObject("TestRegistry");
            registry = registryGO.AddComponent<ZoneSubTypeRegistry>();
            SetupRegistryEntries();

            gridGO = new GameObject("TestGrid");
            grid = gridGO.AddComponent<GridManager>();
            grid.width = 4;
            grid.height = 4;
            grid.cellArray = new CityCell[4, 4];

            cellGO = new GameObject("TestCell");
            var cityCell = cellGO.AddComponent<CityCell>();
            cityCell.x = 1;
            cityCell.y = 1;
            cityCell.zoneType = Zone.ZoneType.Grass;
            grid.cellArray[1, 1] = cityCell;

            zoneManagerGO = new GameObject("TestZoneManager");
            zoneManager = zoneManagerGO.AddComponent<ZoneManager>();
            zoneManager.gridManager = grid;
            zoneManager.cityStats = cityStats;

            serviceGO = new GameObject("TestZoneSService");
            service = serviceGO.AddComponent<ZoneSService>();
            SetPrivateField(typeof(ZoneSService), service, "budgetAllocation", budget);
            SetPrivateField(typeof(ZoneSService), service, "treasuryFloorClamp", clamp);
            SetPrivateField(typeof(ZoneSService), service, "registry", registry);
            SetPrivateField(typeof(ZoneSService), service, "zoneManager", zoneManager);
            SetPrivateField(typeof(ZoneSService), service, "grid", grid);
        }

        [TearDown]
        public void TearDown()
        {
            if (cellGO != null) Object.DestroyImmediate(cellGO);
            if (serviceGO != null) Object.DestroyImmediate(serviceGO);
            if (zoneManagerGO != null) Object.DestroyImmediate(zoneManagerGO);
            if (gridGO != null) Object.DestroyImmediate(gridGO);
            if (registryGO != null) Object.DestroyImmediate(registryGO);
            if (budgetGO != null) Object.DestroyImmediate(budgetGO);
            if (clampGO != null) Object.DestroyImmediate(clampGO);
            if (economyGO != null) Object.DestroyImmediate(economyGO);
            if (cityStatsGO != null) Object.DestroyImmediate(cityStatsGO);
        }

        [Test]
        public void Example1_PlacePolice_Success()
        {
            SetEnvelope(subTypeId: 0, remaining: 1200, pct: 1f / 7f, cap: 10000);
            cityStats.money = 10000;

            bool result = service.PlaceStateServiceZone(1, 1, 0);

            Assert.IsTrue(result, "Placement should succeed with sufficient envelope and treasury");

            int[] remaining = GetPrivateField<int[]>(typeof(BudgetAllocationService), budget, "currentMonthRemaining");
            Assert.AreEqual(700, remaining[0], "Envelope should be 1200 - 500 = 700");
            Assert.AreEqual(9500, cityStats.money, "Treasury should be 10000 - 500 = 9500");

            CityCell cell = grid.GetCell(1, 1);
            Assert.AreEqual(Zone.ZoneType.StateServiceLightZoning, cell.zoneType,
                "Cell zone type should be StateServiceLightZoning");

            Zone zone = cell.gameObject.GetComponentInChildren<Zone>();
            Assert.IsNotNull(zone, "Zone component should exist on cell GO");
            Assert.AreEqual(0, zone.SubTypeId, "Zone subTypeId should be POLICE (0)");
        }

        [Test]
        public void Example2_PlaceFire_EnvelopeExhausted()
        {
            SetEnvelope(subTypeId: 1, remaining: 200, pct: 1f / 7f, cap: 10000);
            cityStats.money = 10000;

            bool result = service.PlaceStateServiceZone(1, 1, 1);

            Assert.IsFalse(result, "Placement should fail when envelope insufficient");

            int[] remaining = GetPrivateField<int[]>(typeof(BudgetAllocationService), budget, "currentMonthRemaining");
            Assert.AreEqual(200, remaining[1], "Envelope should be unchanged at 200");
            Assert.AreEqual(10000, cityStats.money, "Treasury should be unchanged at 10000");
            Assert.AreEqual(Zone.ZoneType.Grass, grid.GetCell(1, 1).zoneType,
                "Cell zone type should remain Grass (no placement)");
        }

        [Test]
        public void PlaceStateServiceZone_InvalidSubType_ReturnsFalse()
        {
            SetEnvelope(subTypeId: 0, remaining: 5000, pct: 1f / 7f, cap: 10000);
            cityStats.money = 10000;

            bool result = service.PlaceStateServiceZone(1, 1, 99);

            Assert.IsFalse(result, "Invalid sub-type should return false");
            Assert.AreEqual(10000, cityStats.money, "Treasury unchanged on invalid sub-type");
        }

        [Test]
        public void PlaceStateServiceZone_ContributorRegistered()
        {
            SetEnvelope(subTypeId: 0, remaining: 1200, pct: 1f / 7f, cap: 10000);
            cityStats.money = 10000;

            service.PlaceStateServiceZone(1, 1, 0);

            var contributor = grid.GetCell(1, 1).gameObject
                .GetComponent<StateServiceMaintenanceContributor>();
            Assert.IsNotNull(contributor,
                "StateServiceMaintenanceContributor should be attached after placement");
            Assert.AreEqual(0, contributor.ConfiguredSubTypeId,
                "Contributor subTypeId should match placement");
        }

        private void SetupRegistryEntries()
        {
            var entries = new ZoneSubTypeRegistry.ZoneSubTypeEntry[]
            {
                new ZoneSubTypeRegistry.ZoneSubTypeEntry
                    { id = 0, displayName = "Police", prefabPath = "", iconPath = "", baseCost = 500, monthlyUpkeep = 50 },
                new ZoneSubTypeRegistry.ZoneSubTypeEntry
                    { id = 1, displayName = "Fire", prefabPath = "", iconPath = "", baseCost = 600, monthlyUpkeep = 60 },
            };
            SetPrivateField(typeof(ZoneSubTypeRegistry), registry, "_entries", entries);
        }

        private void SetEnvelope(int subTypeId, int remaining, float pct, int cap)
        {
            int[] arr = GetPrivateField<int[]>(typeof(BudgetAllocationService), budget, "currentMonthRemaining");
            arr[subTypeId] = remaining;

            float[] pcts = GetPrivateField<float[]>(typeof(BudgetAllocationService), budget, "envelopePct");
            pcts[subTypeId] = pct;

            SetPrivateField(typeof(BudgetAllocationService), budget, "globalMonthlyCap", cap);
        }

        private static void SetPrivateField(System.Type type, object target, string fieldName, object value)
        {
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"Reflection: {fieldName} not found on {type.Name}");
            field.SetValue(target, value);
        }

        private static T GetPrivateField<T>(System.Type type, object target, string fieldName)
        {
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"Reflection: {fieldName} not found on {type.Name}");
            return (T)field.GetValue(target);
        }
    }
}
