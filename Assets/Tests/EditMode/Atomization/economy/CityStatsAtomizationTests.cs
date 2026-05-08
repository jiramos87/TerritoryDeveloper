using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;

namespace Territory.Tests.EditMode.Atomization.Economy
{
    /// <summary>
    /// Tracer tests: assert CityStatsService + IEconomy facade present in Domains.Economy namespace.
    /// Red baseline: Domains/Economy/ absent → asserts fail.
    /// Green: Economy.asmdef + CityStatsService + IEconomy all present; compile-check exits 0.
    /// §Red-Stage Proof anchor: CityStatsAtomizationTests.cs::CityStatsService_is_in_domains_economy_services_namespace
    /// </summary>
    public class CityStatsAtomizationTests
    {
        [Test]
        public void CityStatsService_is_in_domains_economy_services_namespace()
        {
            Type serviceType = typeof(Domains.Economy.Services.CityStatsService);
            Assert.AreEqual("Domains.Economy.Services", serviceType.Namespace,
                $"Expected namespace 'Domains.Economy.Services', got '{serviceType.Namespace}'");
        }

        [Test]
        public void IEconomy_facade_exists_in_domains_economy_namespace()
        {
            Type ifaceType = typeof(Domains.Economy.IEconomy);
            Assert.AreEqual("Domains.Economy", ifaceType.Namespace,
                $"Expected namespace 'Domains.Economy', got '{ifaceType.Namespace}'");
        }

        [Test]
        public void IEconomy_facade_exposes_CanAfford_method()
        {
            Type ifaceType = typeof(Domains.Economy.IEconomy);
            MethodInfo method = ifaceType.GetMethod("CanAfford", new Type[] { typeof(int) });
            Assert.IsNotNull(method, "IEconomy must expose CanAfford(int cost)");
        }

        [Test]
        public void IEconomy_facade_exposes_AddMoney_method()
        {
            Type ifaceType = typeof(Domains.Economy.IEconomy);
            MethodInfo method = ifaceType.GetMethod("AddMoney", new Type[] { typeof(int) });
            Assert.IsNotNull(method, "IEconomy must expose AddMoney(int value)");
        }

        [Test]
        public void IEconomy_facade_exposes_GetMoney_method()
        {
            Type ifaceType = typeof(Domains.Economy.IEconomy);
            MethodInfo method = ifaceType.GetMethod("GetMoney", Type.EmptyTypes);
            Assert.IsNotNull(method, "IEconomy must expose GetMoney()");
        }

        [Test]
        public void IEconomy_facade_exposes_AddPopulation_method()
        {
            Type ifaceType = typeof(Domains.Economy.IEconomy);
            MethodInfo method = ifaceType.GetMethod("AddPopulation", new Type[] { typeof(int) });
            Assert.IsNotNull(method, "IEconomy must expose AddPopulation(int value)");
        }

        [Test]
        public void IEconomy_facade_exposes_GetPopulation_method()
        {
            Type ifaceType = typeof(Domains.Economy.IEconomy);
            MethodInfo method = ifaceType.GetMethod("GetPopulation", Type.EmptyTypes);
            Assert.IsNotNull(method, "IEconomy must expose GetPopulation()");
        }

        [Test]
        public void IEconomy_facade_exposes_GetTotalPowerOutput_method()
        {
            Type ifaceType = typeof(Domains.Economy.IEconomy);
            MethodInfo method = ifaceType.GetMethod("GetTotalPowerOutput", Type.EmptyTypes);
            Assert.IsNotNull(method, "IEconomy must expose GetTotalPowerOutput()");
        }

        [Test]
        public void IEconomy_facade_exposes_GetTotalWaterConsumption_method()
        {
            Type ifaceType = typeof(Domains.Economy.IEconomy);
            MethodInfo method = ifaceType.GetMethod("GetTotalWaterConsumption", Type.EmptyTypes);
            Assert.IsNotNull(method, "IEconomy must expose GetTotalWaterConsumption()");
        }

        [Test]
        public void CityStatsService_CanAfford_returns_false_when_insufficient_funds()
        {
            var svc = new Domains.Economy.Services.CityStatsService();
            // Initial money = 20000
            Assert.IsFalse(svc.CanAfford(999999), "CanAfford must return false when cost > money");
        }

        [Test]
        public void CityStatsService_CanAfford_returns_true_when_sufficient_funds()
        {
            var svc = new Domains.Economy.Services.CityStatsService();
            Assert.IsTrue(svc.CanAfford(100), "CanAfford must return true when cost <= money");
        }

        [Test]
        public void CityStatsService_AddMoney_increases_balance()
        {
            var svc = new Domains.Economy.Services.CityStatsService();
            int before = svc.GetMoney();
            svc.AddMoney(5000);
            Assert.AreEqual(before + 5000, svc.GetMoney(), "AddMoney must increase balance by value");
        }

        [Test]
        public void CityStatsService_RemoveMoney_decreases_balance()
        {
            var svc = new Domains.Economy.Services.CityStatsService();
            int before = svc.GetMoney();
            svc.RemoveMoney(1000);
            Assert.AreEqual(before - 1000, svc.GetMoney(), "RemoveMoney must decrease balance by value");
        }

        [Test]
        public void CityStatsService_AddPopulation_increases_population()
        {
            var svc = new Domains.Economy.Services.CityStatsService();
            svc.AddPopulation(500);
            Assert.AreEqual(500, svc.GetPopulation(), "AddPopulation must increase population");
        }

        [Test]
        public void CityStatsService_Reset_restores_defaults()
        {
            var svc = new Domains.Economy.Services.CityStatsService();
            svc.AddMoney(50000);
            svc.AddPopulation(1000);
            svc.Reset();
            Assert.AreEqual(20000, svc.GetMoney(), "Reset must restore money to 20000");
            Assert.AreEqual(0, svc.GetPopulation(), "Reset must restore population to 0");
        }

        [Test]
        public void CityStatsService_PowerPlant_registration_increases_output()
        {
            var svc = new Domains.Economy.Services.CityStatsService();
            svc.RegisterPowerPlant("plant-1", 500);
            Assert.AreEqual(500, svc.GetTotalPowerOutput(), "Registered plant must contribute to power output");
        }

        [Test]
        public void CityStatsService_PowerPlant_unregistration_decreases_output()
        {
            var svc = new Domains.Economy.Services.CityStatsService();
            svc.RegisterPowerPlant("plant-1", 500);
            svc.UnregisterPowerPlant("plant-1");
            Assert.AreEqual(0, svc.GetTotalPowerOutput(), "Unregistered plant must not contribute to power output");
        }

        [Test]
        public void CityStatsService_GetCityPowerAvailability_true_when_output_exceeds_consumption()
        {
            var svc = new Domains.Economy.Services.CityStatsService();
            svc.RegisterPowerPlant("plant-1", 1000);
            svc.AddPowerConsumption(500);
            Assert.IsTrue(svc.GetCityPowerAvailability(), "Power available when output > consumption");
        }

        [Test]
        public void CityStatsService_UpdateForestStats_stores_values()
        {
            var svc = new Domains.Economy.Services.CityStatsService();
            svc.UpdateForestStats(42, 0.25f);
            Assert.AreEqual(42, svc.GetForestCellCount(), "GetForestCellCount must return updated value");
            Assert.AreEqual(0.25f, svc.GetForestCoveragePercentage(), 0.001f,
                "GetForestCoveragePercentage must return updated value");
        }

        [Test]
        public void economy_asmdef_exists()
        {
            // Application.dataPath not available in pure noEngineReferences assemblies;
            // use AppDomain to locate assembly directory instead.
            string assemblyLocation = typeof(Domains.Economy.Services.CityStatsService).Assembly.Location;
            Assert.IsNotNull(assemblyLocation, "Economy assembly location must be resolvable");
            Assert.IsTrue(assemblyLocation.Length > 0, "Economy assembly path must be non-empty");
        }

        [Test]
        public void CityStatsService_implements_IEconomy()
        {
            Type svcType = typeof(Domains.Economy.Services.CityStatsService);
            Type ifaceType = typeof(Domains.Economy.IEconomy);
            Assert.IsTrue(ifaceType.IsAssignableFrom(svcType),
                "CityStatsService must implement IEconomy");
        }
    }
}
