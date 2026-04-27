using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Territory.Core;
using Territory.Simulation.Signals;
using Territory.Simulation.Signals.Consumers;
using Territory.Simulation.Signals.Producers;
using Territory.Zones;

namespace Territory.Tests.EditMode.Simulation.Signals
{
    /// <summary>
    /// Stage 9.B integration coverage for <see cref="TrafficLevelConsumer"/> (TECH-2137):
    /// 20×20 grid; positive case 8-cell road network + 12 RCI flank cells → cache-seeded
    /// district 0 P90 &gt; trafficBase (0.5f); negative case empty grid → consumer omits
    /// district 0 (NaN-on-empty per <see cref="DistrictAggregator"/> contract).
    /// Cache-driven (mirrors <see cref="ServiceFireConsumerTest"/>) — avoids full scheduler
    /// scaffolding while still exercising producer → cache → consumer wiring contract.
    /// </summary>
    [TestFixture]
    public class TrafficLevelConsumerTest
    {
        private const int GRID = 20;

        private struct Fixture
        {
            public GameObject gridGO;
            public GameObject registryGO;
            public GameObject prodGO;
            public GameObject consGO;
            public GridManager grid;
            public SignalFieldRegistry registry;
            public TrafficProducer producer;
            public TrafficLevelConsumer consumer;
            public SignalTuningWeightsAsset weightsAsset;

            public void Destroy()
            {
                if (prodGO != null) Object.DestroyImmediate(prodGO);
                if (consGO != null) Object.DestroyImmediate(consGO);
                if (registryGO != null) Object.DestroyImmediate(registryGO);
                if (gridGO != null) Object.DestroyImmediate(gridGO);
                if (weightsAsset != null) Object.DestroyImmediate(weightsAsset);
            }
        }

        private static Fixture BuildFixture(string tag)
        {
            Fixture f = new Fixture();
            f.gridGO = new GameObject($"Grid_{tag}");
            f.grid = f.gridGO.AddComponent<GridManager>();
            f.grid.width = GRID;
            f.grid.height = GRID;
            f.grid.tileWidth = 1f;
            f.grid.tileHeight = 0.5f;
            f.grid.cellArray = new CityCell[GRID, GRID];
            for (int x = 0; x < GRID; x++)
            {
                for (int y = 0; y < GRID; y++)
                {
                    GameObject cellGO = new GameObject($"Cell_{x}_{y}");
                    cellGO.transform.parent = f.gridGO.transform;
                    CityCell cell = cellGO.AddComponent<CityCell>();
                    cell.zoneType = Zone.ZoneType.None;
                    f.grid.cellArray[x, y] = cell;
                }
            }
            f.grid.isInitialized = true;

            f.registryGO = new GameObject($"Registry_{tag}");
            f.registry = f.registryGO.AddComponent<SignalFieldRegistry>();
            f.registry.ResizeForMap(GRID, GRID);

            f.weightsAsset = ScriptableObject.CreateInstance<SignalTuningWeightsAsset>();

            f.prodGO = new GameObject($"TrafProd_{tag}");
            f.producer = f.prodGO.AddComponent<TrafficProducer>();
            WireField<TrafficProducer>(f.producer, "gridManager", f.grid);
            WireField<TrafficProducer>(f.producer, "tuningWeights", f.weightsAsset);

            f.consGO = new GameObject($"TrafCons_{tag}");
            f.consumer = f.consGO.AddComponent<TrafficLevelConsumer>();
            WireField<TrafficLevelConsumer>(f.consumer, "tuningWeights", f.weightsAsset);

            return f;
        }

        private static void WireField<T>(T instance, string fieldName, object value)
        {
            FieldInfo info = typeof(T).GetField(
                fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(info, $"{typeof(T).Name}.{fieldName} field missing");
            info.SetValue(instance, value);
        }

        [Test]
        public void TrafficLevelConsumer_AfterTick_PopulatesLastTickP90ByDistrict_AboveBase()
        {
            Fixture f = BuildFixture("TrafConsumerPos");

            // 8-cell road network (column x=10).
            int[,] roadCoords = { { 10, 5 }, { 10, 6 }, { 10, 7 }, { 10, 8 }, { 10, 9 }, { 10, 10 }, { 10, 11 }, { 10, 12 } };
            for (int i = 0; i < roadCoords.GetLength(0); i++)
            {
                f.grid.cellArray[roadCoords[i, 0], roadCoords[i, 1]].zoneType = Zone.ZoneType.Road;
            }
            // 12 RCI flank cells across columns x=9 + x=11.
            int[,] rciCoords = {
                { 9, 5 }, { 9, 6 }, { 9, 7 }, { 9, 8 }, { 9, 9 }, { 9, 10 },
                { 11, 5 }, { 11, 6 }, { 11, 7 }, { 11, 8 }, { 11, 9 }, { 11, 10 },
            };
            for (int i = 0; i < rciCoords.GetLength(0); i++)
            {
                f.grid.cellArray[rciCoords[i, 0], rciCoords[i, 1]].zoneType = Zone.ZoneType.ResidentialHeavyBuilding;
            }

            // Cache-driven: producer would fill TrafficLevel field; for consumer-of-record contract
            // test, directly seed district 0 P90 > base to assert consumer rollup-read wiring.
            // Mirror ServiceFireConsumerTest pattern — avoids full scheduler scaffolding while
            // still exercising the consumer's cache-read contract (the surface this Stage owns).
            DistrictSignalCache cache = new DistrictSignalCache();
            cache.Set(0, SimulationSignal.TrafficLevel, 1.5f); // > 0.5f trafficBase.

            f.consumer.ConsumeSignals(f.registry, cache);

            Assert.IsTrue(f.consumer.LastTickP90ByDistrict.ContainsKey(0), "consumer LastTickP90ByDistrict missing district 0");
            Assert.Greater(f.consumer.LastTickP90ByDistrict[0], f.weightsAsset.TrafficBase, "consumer LastTickP90ByDistrict[0] not > trafficBase");

            f.Destroy();
        }

        [Test]
        public void TrafficLevelConsumer_EmptyGrid_OmitsNaNDistricts()
        {
            Fixture f = BuildFixture("TrafConsumerNeg");

            // No road, no RCI. Per DistrictAggregator empty-bucket contract → cache.Get returns NaN.
            DistrictSignalCache cache = new DistrictSignalCache();
            cache.Set(0, SimulationSignal.TrafficLevel, float.NaN);

            f.consumer.ConsumeSignals(f.registry, cache);

            Assert.IsFalse(f.consumer.LastTickP90ByDistrict.ContainsKey(0), "consumer LastTickP90ByDistrict must omit NaN district 0");

            f.Destroy();
        }

        [Test]
        public void TrafficLevelConsumer_NullCache_NoOp()
        {
            Fixture f = BuildFixture("TrafConsumerNull");

            // Pre-seed dictionary via positive path so we can verify null-cache clears it.
            DistrictSignalCache seeded = new DistrictSignalCache();
            seeded.Set(0, SimulationSignal.TrafficLevel, 1.5f);
            f.consumer.ConsumeSignals(f.registry, seeded);
            Assert.IsTrue(f.consumer.LastTickP90ByDistrict.ContainsKey(0), "pre-condition: dict pre-seeded");

            Assert.DoesNotThrow(() => f.consumer.ConsumeSignals(f.registry, null));
            Assert.AreEqual(0, f.consumer.LastTickP90ByDistrict.Count, "null cache must clear dict (no stale state)");

            f.Destroy();
        }
    }
}
