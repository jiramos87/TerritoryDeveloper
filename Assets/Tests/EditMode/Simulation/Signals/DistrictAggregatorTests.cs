using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Territory.Simulation.Signals;

namespace Territory.Tests.EditMode.Simulation.Signals
{
    /// <summary>EditMode coverage for <see cref="DistrictAggregator.Aggregate"/> — Mean / P90 / empty-NaN / GetAll dictionary §Acceptance branches.</summary>
    [TestFixture]
    public class DistrictAggregatorTests
    {
        private GameObject registryGO;
        private SignalFieldRegistry registry;
        private SignalMetadataRegistry metadataAsset;

        [SetUp]
        public void SetUp()
        {
            registryGO = new GameObject("DistrictAggregatorTestsRegistry");
            registry = registryGO.AddComponent<SignalFieldRegistry>();
            registry.ResizeForMap(4, 4);

            metadataAsset = ScriptableObject.CreateInstance<SignalMetadataRegistry>();
            FieldInfo entriesField = typeof(SignalMetadataRegistry).GetField(
                "entries", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(entriesField, "SignalMetadataRegistry.entries field missing");
            int signalCount = Enum.GetValues(typeof(SimulationSignal)).Length;
            SignalMetadataRegistry.Entry[] entries = new SignalMetadataRegistry.Entry[signalCount];
            for (int i = 0; i < entries.Length; i++)
            {
                entries[i] = new SignalMetadataRegistry.Entry
                {
                    diffusionRadius = 0f,
                    decayPerStep = 0f,
                    anisotropy = new Vector2(1f, 1f),
                    rollup = RollupRule.Mean,
                };
            }
            // Override Crime → P90 for the P90 branch test.
            entries[(int)SimulationSignal.Crime].rollup = RollupRule.P90;
            entriesField.SetValue(metadataAsset, entries);
        }

        [TearDown]
        public void TearDown()
        {
            if (registryGO != null) UnityEngine.Object.DestroyImmediate(registryGO);
            if (metadataAsset != null) UnityEngine.Object.DestroyImmediate(metadataAsset);
        }

        [Test]
        public void Aggregate_Mean_PollutionAir_FourCellDistrict()
        {
            // 4x4 map, all cells district 0.
            DistrictMap map = new DistrictMap(4, 4);
            for (int x = 0; x < 4; x++)
                for (int y = 0; y < 4; y++)
                    map.SetDistrictId(x, y, 0);

            // Seed 4 cells in PollutionAir field with values 0.0, 0.5, 1.0, 1.5; rest stays 0.
            SignalField pa = registry.GetField(SimulationSignal.PollutionAir);
            // Width=4, Height=4 → resize already at those dims.
            pa.Set(0, 0, 0.0f);
            pa.Set(1, 0, 0.5f);
            pa.Set(2, 0, 1.0f);
            pa.Set(3, 0, 1.5f);
            // Remaining 12 cells stay at 0.
            // Mean over 16 cells = 3.0 / 16 = 0.1875.

            DistrictSignalCache cache = new DistrictSignalCache();
            DistrictAggregator.Aggregate(registry, map, metadataAsset, cache);

            float meanD0 = cache.Get(0, SimulationSignal.PollutionAir);
            Assert.AreEqual(0.1875f, meanD0, 1e-5, "Mean rollup over 16-cell district 0 mismatch");
        }

        [Test]
        public void Aggregate_P90_Crime_TenCellDistrict()
        {
            // 10x1 effective layout — but DistrictMap supports any 2D; use 10x1 here via 4x4 width/height workaround.
            // Simpler: build 10-cell district by tagging 10 cells district 0; remaining 6 cells district 3 (out of test scope).
            DistrictMap map = new DistrictMap(4, 4);
            // Default = 0 for all cells; override last 6 cells to district 3 to avoid contamination.
            // Tag first 10 cells (row-major) district 0, rest district 3.
            int tagged = 0;
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    if (tagged < 10)
                    {
                        map.SetDistrictId(x, y, 0);
                        tagged++;
                    }
                    else
                    {
                        map.SetDistrictId(x, y, 3);
                    }
                }
            }

            // Build a SignalField of dims 4x4 (16 cells); fill district-0 cells with sorted ascending values 0.0..0.9.
            SignalField crime = registry.GetField(SimulationSignal.Crime);
            float[] values = { 0.0f, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f };
            int idx = 0;
            for (int y = 0; y < 4 && idx < 10; y++)
            {
                for (int x = 0; x < 4 && idx < 10; x++)
                {
                    crime.Set(x, y, values[idx]);
                    idx++;
                }
            }

            DistrictSignalCache cache = new DistrictSignalCache();
            DistrictAggregator.Aggregate(registry, map, metadataAsset, cache);

            float p90 = cache.Get(0, SimulationSignal.Crime);
            // P90 over 10 sorted values: index = floor(0.9 * 9) = 8 → value 0.8.
            Assert.AreEqual(0.8f, p90, 1e-5, "P90 rollup over 10-cell district 0 mismatch");
        }

        [Test]
        public void Aggregate_EmptyDistrict_ReturnsNaN()
        {
            // All cells district 0 → district 3 has zero cells.
            DistrictMap map = new DistrictMap(4, 4);
            for (int x = 0; x < 4; x++)
                for (int y = 0; y < 4; y++)
                    map.SetDistrictId(x, y, 0);

            DistrictSignalCache cache = new DistrictSignalCache();
            DistrictAggregator.Aggregate(registry, map, metadataAsset, cache);

            Assert.IsTrue(float.IsNaN(cache.Get(3, SimulationSignal.PollutionAir)), "Empty district 3 must surface NaN sentinel");
            Assert.IsTrue(float.IsNaN(cache.Get(3, SimulationSignal.Crime)), "Empty district 3 must surface NaN sentinel for P90 too");
        }

        [Test]
        public void Aggregate_GetAll_TwelveSignals()
        {
            DistrictMap map = new DistrictMap(4, 4);
            for (int x = 0; x < 4; x++)
                for (int y = 0; y < 4; y++)
                    map.SetDistrictId(x, y, 0);

            DistrictSignalCache cache = new DistrictSignalCache();
            DistrictAggregator.Aggregate(registry, map, metadataAsset, cache);

            var all = cache.GetAll(0);
            int signalCount = Enum.GetValues(typeof(SimulationSignal)).Length;
            Assert.AreEqual(signalCount, all.Count, "GetAll(0) must surface every SimulationSignal ordinal exactly once");
        }
    }
}
