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
    /// Stage 9.A unit coverage for <see cref="ServiceFireProducer"/> + <see cref="ServiceFireConsumer"/>
    /// (TECH-2079). 20×20 grid; positive case sub-type 1 (Fire) at (10,10) → assert
    /// <see cref="SimulationSignal.ServiceFire"/> field at (10,10) == <c>ServiceFireCoverage</c>;
    /// negative case sub-type 0 (Police) → field zero everywhere; predicate-reject case
    /// (residential cell) → field zero. Consumer cache-driven (mirrors
    /// <see cref="CrimeHotspotEventEmitterTest"/>) — populates <see cref="DistrictSignalCache"/>
    /// directly to assert <see cref="ServiceFireConsumer.LastTickMeanByDistrict"/> non-zero.
    /// SO round-trip retains all six Stage 9.A service tuning fields.
    /// </summary>
    [TestFixture]
    public class ServiceFireConsumerTest
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
            public ServiceFireProducer producer;
            public ServiceFireConsumer consumer;
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

            f.prodGO = new GameObject($"FireProd_{tag}");
            f.producer = f.prodGO.AddComponent<ServiceFireProducer>();
            WireField<ServiceFireProducer>(f.producer, "gridManager", f.grid);
            WireField<ServiceFireProducer>(f.producer, "tuningWeights", f.weightsAsset);

            f.consGO = new GameObject($"FireCons_{tag}");
            f.consumer = f.consGO.AddComponent<ServiceFireConsumer>();
            WireField<ServiceFireConsumer>(f.consumer, "tuningWeights", f.weightsAsset);

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
        public void ServiceFireProducer_Tick_EmitsCoverageAtFireSubtypeCell()
        {
            Fixture f = BuildFixture("FirePos");
            PlaceServiceCell(f, 10, 10, 1); // Fire = sub-type 1

            f.producer.EmitSignals(f.registry);

            SignalField field = f.registry.GetField(SimulationSignal.ServiceFire);
            Assert.AreEqual(f.weightsAsset.ServiceFireCoverage, field.Get(10, 10), 1e-5f, "ServiceFire field at fire cell != ServiceFireCoverage");

            f.Destroy();
        }

        [Test]
        public void ServiceFireProducer_Tick_EmitsZeroAtPoliceSubtypeCell()
        {
            Fixture f = BuildFixture("FireNeg");
            PlaceServiceCell(f, 10, 10, 0); // Police = sub-type 0

            f.producer.EmitSignals(f.registry);

            SignalField field = f.registry.GetField(SimulationSignal.ServiceFire);
            for (int x = 0; x < GRID; x++)
            {
                for (int y = 0; y < GRID; y++)
                {
                    Assert.AreEqual(0f, field.Get(x, y), 1e-5f, $"ServiceFire field non-zero at ({x},{y}) — police-only fixture");
                }
            }

            f.Destroy();
        }

        [Test]
        public void ServiceFireProducer_Tick_EmitsZeroAtNonStateServiceCell()
        {
            Fixture f = BuildFixture("FireResidential");
            CityCell cell = f.grid.cellArray[10, 10];
            cell.zoneType = Zone.ZoneType.ResidentialHeavyBuilding; // Predicate row 1 reject.

            f.producer.EmitSignals(f.registry);

            SignalField field = f.registry.GetField(SimulationSignal.ServiceFire);
            for (int x = 0; x < GRID; x++)
            {
                for (int y = 0; y < GRID; y++)
                {
                    Assert.AreEqual(0f, field.Get(x, y), 1e-5f, $"ServiceFire field non-zero at ({x},{y}) — residential-only fixture");
                }
            }

            f.Destroy();
        }

        [Test]
        public void ServiceFireConsumer_AfterTick_PopulatesLastTickMeanByDistrict()
        {
            Fixture f = BuildFixture("FireConsumer");
            PlaceServiceCell(f, 10, 10, 1);

            // Cache-driven: directly seed district 0 with a non-zero ServiceFire mean
            // (mirrors CrimeHotspotEventEmitterTest pattern — avoids full scheduler scaffolding).
            DistrictSignalCache cache = new DistrictSignalCache();
            cache.Set(0, SimulationSignal.ServiceFire, 5f);

            f.consumer.ConsumeSignals(f.registry, cache);

            Assert.IsTrue(f.consumer.LastTickMeanByDistrict.ContainsKey(0), "consumer LastTickMeanByDistrict missing district 0");
            Assert.Greater(f.consumer.LastTickMeanByDistrict[0], 0f, "consumer LastTickMeanByDistrict[0] not non-zero");

            f.Destroy();
        }

        [Test]
        public void SignalTuningWeightsAsset_RoundTrip_RetainsServiceFields()
        {
            SignalTuningWeightsAsset asset = ScriptableObject.CreateInstance<SignalTuningWeightsAsset>();

            float originalFireCov = asset.ServiceFireCoverage;
            float originalEduCov = asset.ServiceEducationCoverage;
            float originalHealthCov = asset.ServiceHealthCoverage;
            float originalFireScale = asset.ServiceFireConsumerScale;
            float originalEduScale = asset.ServiceEducationConsumerScale;
            float originalHealthScale = asset.ServiceHealthConsumerScale;

            SignalTuningWeightsData snapshot = asset.CaptureSnapshot();

            // Mutate live asset to confirm restore writes back.
            FieldInfo fireCov = typeof(SignalTuningWeightsAsset).GetField(
                "serviceFireCoverage", BindingFlags.NonPublic | BindingFlags.Instance);
            fireCov.SetValue(asset, 999f);

            asset.RestoreFromData(snapshot);

            Assert.AreEqual(originalFireCov, asset.ServiceFireCoverage, 1e-5f);
            Assert.AreEqual(originalEduCov, asset.ServiceEducationCoverage, 1e-5f);
            Assert.AreEqual(originalHealthCov, asset.ServiceHealthCoverage, 1e-5f);
            Assert.AreEqual(originalFireScale, asset.ServiceFireConsumerScale, 1e-5f);
            Assert.AreEqual(originalEduScale, asset.ServiceEducationConsumerScale, 1e-5f);
            Assert.AreEqual(originalHealthScale, asset.ServiceHealthConsumerScale, 1e-5f);

            Object.DestroyImmediate(asset);
        }
    }
}
