using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using Territory.Geography;

namespace Territory.Tests.EditMode.Atomization.Geography
{
    /// <summary>
    /// Tracer tests: assert GeographyWaterDesirabilityService extracted to Domains.Geography.Services assembly
    /// + IGeography facade interface present in Domains.Geography.
    /// Red baseline: Domains/Geography/ absent → asserts fail.
    /// Green: Geography.asmdef + GeographyWaterDesirabilityService + IGeography all present; compile-check exits 0.
    /// §Red-Stage Proof anchor: GeographyManagerAtomizationTests.cs::GeographyWaterDesirabilityService_is_in_domains_geography_services_namespace
    /// </summary>
    public class GeographyManagerAtomizationTests
    {
        [Test]
        public void GeographyWaterDesirabilityService_is_in_domains_geography_services_namespace()
        {
            Type serviceType = typeof(Domains.Geography.Services.GeographyWaterDesirabilityService);
            Assert.AreEqual("Domains.Geography.Services", serviceType.Namespace,
                $"Expected namespace 'Domains.Geography.Services', got '{serviceType.Namespace}'");
        }

        [Test]
        public void IGeography_facade_exists_in_domains_geography_namespace()
        {
            Type ifaceType = typeof(Domains.Geography.IGeography);
            Assert.AreEqual("Domains.Geography", ifaceType.Namespace,
                $"Expected namespace 'Domains.Geography', got '{ifaceType.Namespace}'");
        }

        [Test]
        public void IGeography_facade_exposes_IsInitialized_property()
        {
            Type ifaceType = typeof(Domains.Geography.IGeography);
            PropertyInfo prop = ifaceType.GetProperty("IsInitialized");
            Assert.IsNotNull(prop, "IGeography must expose IsInitialized property");
        }

        [Test]
        public void IGeography_facade_exposes_IsPositionSuitableForPlacement_method()
        {
            Type ifaceType = typeof(Domains.Geography.IGeography);
            MethodInfo method = ifaceType.GetMethod("IsPositionSuitableForPlacement",
                new Type[] { typeof(int), typeof(int), typeof(PlacementType) });
            Assert.IsNotNull(method, "IGeography must expose IsPositionSuitableForPlacement(int x, int y, PlacementType)");
        }

        [Test]
        public void IGeography_facade_exposes_GetEnvironmentalBonus_method()
        {
            Type ifaceType = typeof(Domains.Geography.IGeography);
            MethodInfo method = ifaceType.GetMethod("GetEnvironmentalBonus",
                new Type[] { typeof(int), typeof(int) });
            Assert.IsNotNull(method, "IGeography must expose GetEnvironmentalBonus(int x, int y)");
        }

        [Test]
        public void IGeography_facade_exposes_ReCalculateSortingOrderBasedOnHeight_method()
        {
            Type ifaceType = typeof(Domains.Geography.IGeography);
            MethodInfo method = ifaceType.GetMethod("ReCalculateSortingOrderBasedOnHeight");
            Assert.IsNotNull(method, "IGeography must expose ReCalculateSortingOrderBasedOnHeight()");
        }

        [Test]
        public void GeographyWaterDesirabilityService_Apply_method_exists_with_correct_signature()
        {
            Type serviceType = typeof(Domains.Geography.Services.GeographyWaterDesirabilityService);
            MethodInfo method = serviceType.GetMethod("Apply",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new Type[]
                {
                    typeof(int),
                    typeof(int),
                    typeof(Func<int, int, Territory.Core.CityCell>),
                    typeof(Func<int, int, bool>)
                },
                null);
            Assert.IsNotNull(method, "GeographyWaterDesirabilityService.Apply must be a static method with (int, int, Func<int,int,CityCell>, Func<int,int,bool>) signature");
        }

        [Test]
        public void geography_asmdef_exists()
        {
            string path = Path.Combine(Application.dataPath, "Scripts", "Domains", "Geography", "Geography.asmdef");
            Assert.IsTrue(File.Exists(path), $"Geography.asmdef not found at: {path}");
        }

        [Test]
        public void geography_asmdef_references_territory_developer_game()
        {
            string path = Path.Combine(Application.dataPath, "Scripts", "Domains", "Geography", "Geography.asmdef");
            Assert.IsTrue(File.Exists(path), "Geography.asmdef absent");
            string content = File.ReadAllText(path);
            Assert.IsTrue(content.Contains("7d8f9e2a1b4c5d6e7f8a9b0c1d2e3f4a"),
                "Geography.asmdef must reference TerritoryDeveloper.Game (GUID 7d8f9e2a...)");
        }

        [Test]
        public void GeographyManager_implements_IGeography_interface()
        {
            Type managerType = typeof(Territory.Geography.GeographyManager);
            Type ifaceType = typeof(Domains.Geography.IGeography);
            Assert.IsTrue(ifaceType.IsAssignableFrom(managerType),
                "GeographyManager must implement IGeography");
        }
    }
}
