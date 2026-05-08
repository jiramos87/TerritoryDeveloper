using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Territory.Tests.EditMode.Atomization.Terrain
{
    /// <summary>
    /// Tracer tests: assert HeightMapService extracted to Domains.Terrain.Services assembly
    /// + ITerrain facade interface present in Domains.Terrain.
    /// Red baseline: Domains/Terrain/ absent → asserts fail.
    /// Green: Terrain.asmdef + HeightMapService + ITerrain all present; compile-check exits 0.
    /// §Red-Stage Proof anchor: TerrainManagerAtomizationTests.cs::HeightMapService_is_in_domains_terrain_services_namespace
    /// </summary>
    public class TerrainManagerAtomizationTests
    {
        [Test]
        public void HeightMapService_is_in_domains_terrain_services_namespace()
        {
            Type serviceType = typeof(Domains.Terrain.Services.HeightMapService);
            Assert.AreEqual("Domains.Terrain.Services", serviceType.Namespace,
                $"Expected namespace 'Domains.Terrain.Services', got '{serviceType.Namespace}'");
        }

        [Test]
        public void ITerrain_facade_exists_in_domains_terrain_namespace()
        {
            Type ifaceType = typeof(Domains.Terrain.ITerrain);
            Assert.AreEqual("Domains.Terrain", ifaceType.Namespace,
                $"Expected namespace 'Domains.Terrain', got '{ifaceType.Namespace}'");
        }

        [Test]
        public void ITerrain_facade_exposes_GetOrCreateHeightMap_method()
        {
            Type ifaceType = typeof(Domains.Terrain.ITerrain);
            MethodInfo method = ifaceType.GetMethod("GetOrCreateHeightMap");
            Assert.IsNotNull(method, "ITerrain must expose GetOrCreateHeightMap()");
        }

        [Test]
        public void ITerrain_facade_exposes_CanPlaceRoad_method()
        {
            Type ifaceType = typeof(Domains.Terrain.ITerrain);
            MethodInfo method = ifaceType.GetMethod("CanPlaceRoad",
                new Type[] { typeof(int), typeof(int) });
            Assert.IsNotNull(method, "ITerrain must expose CanPlaceRoad(int x, int y)");
        }

        [Test]
        public void HeightMapService_MIN_HEIGHT_matches_TerrainManager()
        {
            // Behavior parity check: constant value must match TerrainManager.MIN_HEIGHT = 0
            var field = typeof(Domains.Terrain.Services.HeightMapService)
                .GetField("MIN_HEIGHT", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(field, "MIN_HEIGHT field must exist on HeightMapService");
            int value = (int)field.GetValue(null);
            Assert.AreEqual(0, value, $"MIN_HEIGHT behavior parity: expected 0, got {value}");
        }

        [Test]
        public void HeightMapService_MAX_HEIGHT_matches_TerrainManager()
        {
            // Behavior parity check: constant value must match TerrainManager.MAX_HEIGHT = 5
            var field = typeof(Domains.Terrain.Services.HeightMapService)
                .GetField("MAX_HEIGHT", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(field, "MAX_HEIGHT field must exist on HeightMapService");
            int value = (int)field.GetValue(null);
            Assert.AreEqual(5, value, $"MAX_HEIGHT behavior parity: expected 5, got {value}");
        }

        [Test]
        public void HeightMapService_GetOriginal40x40Heights_returns_40x40_array()
        {
            int[,] heights = Domains.Terrain.Services.HeightMapService.GetOriginal40x40Heights();
            Assert.AreEqual(40, heights.GetLength(0), "40x40 template: expected 40 rows");
            Assert.AreEqual(40, heights.GetLength(1), "40x40 template: expected 40 cols");
        }

        [Test]
        public void HeightMapService_GetOriginal40x40Heights_all_values_in_range()
        {
            int[,] heights = Domains.Terrain.Services.HeightMapService.GetOriginal40x40Heights();
            for (int r = 0; r < 40; r++)
                for (int c = 0; c < 40; c++)
                    Assert.IsTrue(heights[r, c] >= 1 && heights[r, c] <= 5,
                        $"Height [{r},{c}] = {heights[r,c]} out of range [1,5]");
        }

        [Test]
        public void terrain_asmdef_exists()
        {
            string path = Path.Combine(Application.dataPath, "Scripts", "Domains", "Terrain", "Terrain.asmdef");
            Assert.IsTrue(File.Exists(path), $"Terrain.asmdef not found at: {path}");
        }

        [Test]
        public void terrain_editor_asmdef_exists()
        {
            string path = Path.Combine(Application.dataPath, "Scripts", "Domains", "Terrain", "Editor", "Terrain.Editor.asmdef");
            Assert.IsTrue(File.Exists(path), $"Terrain.Editor.asmdef not found at: {path}");
        }

        [Test]
        public void terrain_asmdef_references_territory_developer_game()
        {
            string path = Path.Combine(Application.dataPath, "Scripts", "Domains", "Terrain", "Terrain.asmdef");
            Assert.IsTrue(File.Exists(path), "Terrain.asmdef absent");
            string content = File.ReadAllText(path);
            // Must reference TerritoryDeveloper.Game GUID
            Assert.IsTrue(content.Contains("7d8f9e2a1b4c5d6e7f8a9b0c1d2e3f4a"),
                "Terrain.asmdef must reference TerritoryDeveloper.Game (GUID 7d8f9e2a...)");
        }
    }
}
