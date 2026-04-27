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
    /// Stage 9.A unit coverage for <see cref="ServiceEducationProducer"/> +
    /// <see cref="ServiceEducationConsumer"/> (TECH-2080). 20×20 grid; positive case sub-type 2
    /// (Education) at (5,5) → assert <see cref="SimulationSignal.ServiceEducation"/> field at
    /// (5,5) == <c>ServiceEducationCoverage</c>; negative case sub-type 1 (Fire) → field zero
    /// everywhere; predicate-reject case (residential cell) → field zero. Consumer cache-driven
    /// mirroring <see cref="CrimeHotspotEventEmitterTest"/>.
    /// </summary>
    [TestFixture]
    public class ServiceEducationConsumerTest
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
            public ServiceEducationProducer producer;
            public ServiceEducationConsumer consumer;
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

            f.prodGO = new GameObject($"EduProd_{tag}");
            f.producer = f.prodGO.AddComponent<ServiceEducationProducer>();
            WireField<ServiceEducationProducer>(f.producer, "gridManager", f.grid);
            WireField<ServiceEducationProducer>(f.producer, "tuningWeights", f.weightsAsset);

            f.consGO = new GameObject($"EduCons_{tag}");
            f.consumer = f.consGO.AddComponent<ServiceEducationConsumer>();
            WireField<ServiceEducationConsumer>(f.consumer, "tuningWeights", f.weightsAsset);

            return f;
        }

        private static void PlaceServiceCell(Fixture f, int x, int y, int subTypeId)
        {
            CityCell cell = f.grid.cellArray[x, y];
            cell.zoneType = Zone.ZoneType.StateServiceLightBuilding;
            GameObject buildingGO = new GameObject($"ServiceBuilding_{x}_{y}_subtype{subTypeId}");
            buildingGO.transform.parent = cell.transform;
            Zone zoneComp = buildingGO.AddComponent<Zone>();
            zoneComp.SubTypeId = subTypeId;
            cell.occupiedBuilding = buildingGO;
        }

        private static void WireField<T>(T instance, string fieldName, object value)
        {
            FieldInfo info = typeof(T).GetField(
                fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(info, $"{typeof(T).Name}.{fieldName} field missing");
            info.SetValue(instance, value);
        }

        [Test]
        public void ServiceEducationProducer_Tick_EmitsCoverageAtEducationSubtypeCell()
        {
            Fixture f = BuildFixture("EduPos");
            PlaceServiceCell(f, 5, 5, 2); // Education = sub-type 2

            f.producer.EmitSignals(f.registry);

            SignalField field = f.registry.GetField(SimulationSignal.ServiceEducation);
            Assert.AreEqual(f.weightsAsset.ServiceEducationCoverage, field.Get(5, 5), 1e-5f, "ServiceEducation field at education cell != ServiceEducationCoverage");

            f.Destroy();
        }

        [Test]
        public void ServiceEducationProducer_Tick_EmitsZeroAtFireSubtypeCell()
        {
            Fixture f = BuildFixture("EduNeg");
            PlaceServiceCell(f, 5, 5, 1); // Fire = sub-type 1

            f.producer.EmitSignals(f.registry);

            SignalField field = f.registry.GetField(SimulationSignal.ServiceEducation);
            for (int x = 0; x < GRID; x++)
            {
                for (int y = 0; y < GRID; y++)
                {
                    Assert.AreEqual(0f, field.Get(x, y), 1e-5f, $"ServiceEducation field non-zero at ({x},{y}) — fire-only fixture");
                }
            }

            f.Destroy();
        }

        [Test]
        public void ServiceEducationProducer_Tick_EmitsZeroAtNonStateServiceCell()
        {
            Fixture f = BuildFixture("EduResidential");
            CityCell cell = f.grid.cellArray[5, 5];
            cell.zoneType = Zone.ZoneType.ResidentialHeavyBuilding;

            f.producer.EmitSignals(f.registry);

            SignalField field = f.registry.GetField(SimulationSignal.ServiceEducation);
            for (int x = 0; x < GRID; x++)
            {
                for (int y = 0; y < GRID; y++)
                {
                    Assert.AreEqual(0f, field.Get(x, y), 1e-5f, $"ServiceEducation field non-zero at ({x},{y}) — residential-only fixture");
                }
            }

            f.Destroy();
        }

        [Test]
        public void ServiceEducationConsumer_AfterTick_PopulatesLastTickMeanByDistrict()
        {
            Fixture f = BuildFixture("EduConsumer");
            PlaceServiceCell(f, 5, 5, 2);

            DistrictSignalCache cache = new DistrictSignalCache();
            cache.Set(0, SimulationSignal.ServiceEducation, 5f);

            f.consumer.ConsumeSignals(f.registry, cache);

            Assert.IsTrue(f.consumer.LastTickMeanByDistrict.ContainsKey(0), "consumer LastTickMeanByDistrict missing district 0");
            Assert.Greater(f.consumer.LastTickMeanByDistrict[0], 0f, "consumer LastTickMeanByDistrict[0] not non-zero");

            f.Destroy();
        }
    }
}
