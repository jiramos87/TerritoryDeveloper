using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Territory.Tests.EditMode.Atomization.Water
{
    /// <summary>
    /// Tracer tests: assert WaterMapService + ShoreService extracted to Domains.Water.Services assembly
    /// + IWater facade interface present in Domains.Water.
    /// Red baseline: Domains/Water/ absent → asserts fail.
    /// Green: Water.asmdef + WaterMapService + ShoreService + IWater all present; compile-check exits 0.
    /// §Red-Stage Proof anchor: WaterMapAtomizationTests.cs::WaterMapService_is_in_domains_water_services_namespace
    /// </summary>
    public class WaterMapAtomizationTests
    {
        [Test]
        public void WaterMapService_is_in_domains_water_services_namespace()
        {
            Type serviceType = typeof(Domains.Water.Services.WaterMapService);
            Assert.AreEqual("Domains.Water.Services", serviceType.Namespace,
                $"Expected namespace 'Domains.Water.Services', got '{serviceType.Namespace}'");
        }

        [Test]
        public void ShoreService_is_in_domains_water_services_namespace()
        {
            Type serviceType = typeof(Domains.Water.Services.ShoreService);
            Assert.AreEqual("Domains.Water.Services", serviceType.Namespace,
                $"Expected namespace 'Domains.Water.Services', got '{serviceType.Namespace}'");
        }

        [Test]
        public void IWater_facade_exists_in_domains_water_namespace()
        {
            Type ifaceType = typeof(Domains.Water.IWater);
            Assert.AreEqual("Domains.Water", ifaceType.Namespace,
                $"Expected namespace 'Domains.Water', got '{ifaceType.Namespace}'");
        }

        [Test]
        public void IWater_facade_exposes_IsWaterAt_method()
        {
            Type ifaceType = typeof(Domains.Water.IWater);
            MethodInfo method = ifaceType.GetMethod("IsWaterAt",
                new Type[] { typeof(int), typeof(int) });
            Assert.IsNotNull(method, "IWater must expose IsWaterAt(int x, int y)");
        }

        [Test]
        public void IWater_facade_exposes_GetWaterBodyId_method()
        {
            Type ifaceType = typeof(Domains.Water.IWater);
            MethodInfo method = ifaceType.GetMethod("GetWaterBodyId",
                new Type[] { typeof(int), typeof(int) });
            Assert.IsNotNull(method, "IWater must expose GetWaterBodyId(int x, int y)");
        }

        [Test]
        public void IWater_facade_exposes_CreateRiverWaterBody_method()
        {
            Type ifaceType = typeof(Domains.Water.IWater);
            MethodInfo method = ifaceType.GetMethod("CreateRiverWaterBody",
                new Type[] { typeof(int) });
            Assert.IsNotNull(method, "IWater must expose CreateRiverWaterBody(int surfaceHeight)");
        }

        [Test]
        public void WaterMapService_LegacyPaintWaterBodyId_is_10001()
        {
            var field = typeof(Domains.Water.Services.WaterMapService)
                .GetField("LegacyPaintWaterBodyId", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(field, "LegacyPaintWaterBodyId field must exist on WaterMapService");
            int value = (int)field.GetValue(null);
            Assert.AreEqual(10001, value,
                $"LegacyPaintWaterBodyId behavior parity: expected 10001, got {value}");
        }

        [Test]
        public void WaterMapService_FormatVersionV3_is_3()
        {
            var field = typeof(Domains.Water.Services.WaterMapService)
                .GetField("FormatVersionV3", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(field, "FormatVersionV3 field must exist on WaterMapService");
            int value = (int)field.GetValue(null);
            Assert.AreEqual(3, value,
                $"FormatVersionV3 behavior parity: expected 3, got {value}");
        }

        [Test]
        public void WaterMapService_CanMergeWaterBodies_rejects_river_with_lake()
        {
            var lake = new Territory.Terrain.WaterBody(1, 2, Territory.Terrain.WaterBodyType.Lake);
            var river = new Territory.Terrain.WaterBody(2, 2, Territory.Terrain.WaterBodyType.River);
            bool result = Domains.Water.Services.WaterMapService.CanMergeWaterBodies(lake, river);
            Assert.IsFalse(result, "River + Lake merge must be rejected");
        }

        [Test]
        public void WaterMapService_CanMergeWaterBodies_accepts_lake_with_lake()
        {
            var lakeA = new Territory.Terrain.WaterBody(1, 2, Territory.Terrain.WaterBodyType.Lake);
            var lakeB = new Territory.Terrain.WaterBody(2, 2, Territory.Terrain.WaterBodyType.Lake);
            bool result = Domains.Water.Services.WaterMapService.CanMergeWaterBodies(lakeA, lakeB);
            Assert.IsTrue(result, "Lake + Lake merge must be accepted");
        }

        [Test]
        public void WaterMapService_CanMergeWaterBodies_accepts_river_with_river()
        {
            var riverA = new Territory.Terrain.WaterBody(1, 2, Territory.Terrain.WaterBodyType.River);
            var riverB = new Territory.Terrain.WaterBody(2, 2, Territory.Terrain.WaterBodyType.River);
            bool result = Domains.Water.Services.WaterMapService.CanMergeWaterBodies(riverA, riverB);
            Assert.IsTrue(result, "River + River merge must be accepted");
        }

        [Test]
        public void WaterMapService_ValidateSerializedData_rejects_null()
        {
            var svc = new Domains.Water.Services.WaterMapService(10, 10);
            bool result = svc.ValidateSerializedData(null, out string reason);
            Assert.IsFalse(result, "null data must be rejected");
            Assert.IsNotNull(reason, "reason must be set on null data");
        }

        [Test]
        public void ShoreService_GetShoreBandCells_returns_empty_on_dry_map()
        {
            var svc = new Domains.Water.Services.ShoreService();
            var waterMap = new Territory.Terrain.WaterMap(5, 5);
            var shore = svc.GetShoreBandCells(waterMap, 5, 5);
            Assert.AreEqual(0, shore.Count, "Empty water map must have no shore cells");
        }

        [Test]
        public void water_asmdef_exists()
        {
            string path = Path.Combine(Application.dataPath, "Scripts", "Domains", "Water", "Water.asmdef");
            Assert.IsTrue(File.Exists(path), $"Water.asmdef not found at: {path}");
        }

        [Test]
        public void water_editor_asmdef_exists()
        {
            string path = Path.Combine(Application.dataPath, "Scripts", "Domains", "Water", "Editor", "Water.Editor.asmdef");
            Assert.IsTrue(File.Exists(path), $"Water.Editor.asmdef not found at: {path}");
        }

        [Test]
        public void water_asmdef_references_territory_developer_game()
        {
            string path = Path.Combine(Application.dataPath, "Scripts", "Domains", "Water", "Water.asmdef");
            Assert.IsTrue(File.Exists(path), "Water.asmdef absent");
            string content = File.ReadAllText(path);
            Assert.IsTrue(content.Contains("7d8f9e2a1b4c5d6e7f8a9b0c1d2e3f4a"),
                "Water.asmdef must reference TerritoryDeveloper.Game (GUID 7d8f9e2a...)");
        }
    }
}
