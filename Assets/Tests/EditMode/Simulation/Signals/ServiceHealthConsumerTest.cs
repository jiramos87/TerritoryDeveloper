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
    /// Stage 9.A unit + cross-family coverage for <see cref="ServiceHealthProducer"/> +
    /// <see cref="ServiceHealthConsumer"/> (TECH-2081). 20×20 grid; positive case sub-type 3
    /// (Health) at (15,15) → assert <see cref="SimulationSignal.ServiceHealth"/> field at
    /// (15,15) == <c>ServiceHealthCoverage</c>; negative case sub-type 0 (Police) → field zero
    /// everywhere; predicate-reject case (residential cell) → field zero. Cross-family integration
    /// places one cell per service sub-type and asserts each producer writes ONLY its own field —
    /// no contamination across <c>SimulationSignal.ServiceFire/Education/Health</c>.
    /// </summary>
    [TestFixture]
    public class ServiceHealthConsumerTest
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
            public ServiceHealthProducer producer;
            public ServiceHealthConsumer consumer;
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

            f.prodGO = new GameObject($"HealthProd_{tag}");
            f.producer = f.prodGO.AddComponent<ServiceHealthProducer>();
            WireField<ServiceHealthProducer>(f.producer, "gridManager", f.grid);
            WireField<ServiceHealthProducer>(f.producer, "tuningWeights", f.weightsAsset);

            f.consGO = new GameObject($"HealthCons_{tag}");
            f.consumer = f.consGO.AddComponent<ServiceHealthConsumer>();
            WireField<ServiceHealthConsumer>(f.consumer, "tuningWeights", f.weightsAsset);

            return f;
        }

        private static void PlaceServiceCell(GridManager grid, int x, int y, int subTypeId)
        {
            CityCell cell = grid.cellArray[x, y];
            cell.zoneType = Zone.ZoneType.StateServiceLightBuilding;
            GameObject buildingGO = new GameObject($"ServiceBuilding_{x}_{y}_subtype{subTypeId}");
            buildingGO.transform.parent = cell.transform;
            Zone zoneComp = buildingGO.AddComponent<Zone>();
            zoneComp.SubTypeId = subTypeId;
            cell.occupiedBuilding = buildingGO;
        }

        private static void PlaceServiceCell(Fixture f, int x, int y, int subTypeId)
        {
            PlaceServiceCell(f.grid, x, y, subTypeId);
        }

        private static void WireField<T>(T instance, string fieldName, object value)
        {
            FieldInfo info = typeof(T).GetField(
                fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(info, $"{typeof(T).Name}.{fieldName} field missing");
            info.SetValue(instance, value);
        }

        [Test]
        public void ServiceHealthProducer_Tick_EmitsCoverageAtHealthSubtypeCell()
        {
            Fixture f = BuildFixture("HealthPos");
            PlaceServiceCell(f, 15, 15, 3); // Health = sub-type 3

            f.producer.EmitSignals(f.registry);

            SignalField field = f.registry.GetField(SimulationSignal.ServiceHealth);
            Assert.AreEqual(f.weightsAsset.ServiceHealthCoverage, field.Get(15, 15), 1e-5f, "ServiceHealth field at health cell != ServiceHealthCoverage");

            f.Destroy();
        }

        [Test]
        public void ServiceHealthProducer_Tick_EmitsZeroAtPoliceSubtypeCell()
        {
            Fixture f = BuildFixture("HealthNeg");
            PlaceServiceCell(f, 15, 15, 0); // Police = sub-type 0

            f.producer.EmitSignals(f.registry);

            SignalField field = f.registry.GetField(SimulationSignal.ServiceHealth);
            for (int x = 0; x < GRID; x++)
            {
                for (int y = 0; y < GRID; y++)
                {
                    Assert.AreEqual(0f, field.Get(x, y), 1e-5f, $"ServiceHealth field non-zero at ({x},{y}) — police-only fixture");
                }
            }

            f.Destroy();
        }

        [Test]
        public void ServiceHealthProducer_Tick_EmitsZeroAtNonStateServiceCell()
        {
            Fixture f = BuildFixture("HealthResidential");
            CityCell cell = f.grid.cellArray[15, 15];
            cell.zoneType = Zone.ZoneType.ResidentialHeavyBuilding;

            f.producer.EmitSignals(f.registry);

            SignalField field = f.registry.GetField(SimulationSignal.ServiceHealth);
            for (int x = 0; x < GRID; x++)
            {
                for (int y = 0; y < GRID; y++)
                {
                    Assert.AreEqual(0f, field.Get(x, y), 1e-5f, $"ServiceHealth field non-zero at ({x},{y}) — residential-only fixture");
                }
            }

            f.Destroy();
        }

        [Test]
        public void ServiceHealthConsumer_AfterTick_PopulatesLastTickMeanByDistrict()
        {
            Fixture f = BuildFixture("HealthConsumer");
            PlaceServiceCell(f, 15, 15, 3);

            DistrictSignalCache cache = new DistrictSignalCache();
            cache.Set(0, SimulationSignal.ServiceHealth, 5f);

            f.consumer.ConsumeSignals(f.registry, cache);

            Assert.IsTrue(f.consumer.LastTickMeanByDistrict.ContainsKey(0), "consumer LastTickMeanByDistrict missing district 0");
            Assert.Greater(f.consumer.LastTickMeanByDistrict[0], 0f, "consumer LastTickMeanByDistrict[0] not non-zero");

            f.Destroy();
        }

        /// <summary>
        /// Cross-family integration assert (Stage 9.A acceptance): place one Fire (sub-type 1) +
        /// one Education (sub-type 2) + one Health (sub-type 3) cell on the same grid; run all
        /// three producers; assert each <see cref="SimulationSignal.ServiceFire"/> /
        /// <see cref="SimulationSignal.ServiceEducation"/> /
        /// <see cref="SimulationSignal.ServiceHealth"/> field is non-zero ONLY at its own cell —
        /// no cross-family contamination.
        /// </summary>
        [Test]
        public void Stage9A_AllThreeServiceProducers_NoCrossFamilyContamination()
        {
            // Build raw fixture (Health producer + consumer); we additionally instantiate Fire +
            // Education producers on the same grid + registry + weights.
            Fixture f = BuildFixture("CrossFamily");
            PlaceServiceCell(f, 1, 1, 1);  // Fire
            PlaceServiceCell(f, 2, 2, 2);  // Education
            PlaceServiceCell(f, 3, 3, 3);  // Health

            GameObject fireProdGO = new GameObject("FireProd_CrossFamily");
            ServiceFireProducer fireProd = fireProdGO.AddComponent<ServiceFireProducer>();
            WireField<ServiceFireProducer>(fireProd, "gridManager", f.grid);
            WireField<ServiceFireProducer>(fireProd, "tuningWeights", f.weightsAsset);

            GameObject eduProdGO = new GameObject("EduProd_CrossFamily");
            ServiceEducationProducer eduProd = eduProdGO.AddComponent<ServiceEducationProducer>();
            WireField<ServiceEducationProducer>(eduProd, "gridManager", f.grid);
            WireField<ServiceEducationProducer>(eduProd, "tuningWeights", f.weightsAsset);

            fireProd.EmitSignals(f.registry);
            eduProd.EmitSignals(f.registry);
            f.producer.EmitSignals(f.registry);

            SignalField fireField = f.registry.GetField(SimulationSignal.ServiceFire);
            SignalField eduField = f.registry.GetField(SimulationSignal.ServiceEducation);
            SignalField healthField = f.registry.GetField(SimulationSignal.ServiceHealth);

            Assert.AreEqual(f.weightsAsset.ServiceFireCoverage, fireField.Get(1, 1), 1e-5f, "Fire field missing at fire cell (1,1)");
            Assert.AreEqual(0f, fireField.Get(2, 2), 1e-5f, "Fire field bled into education cell (2,2)");
            Assert.AreEqual(0f, fireField.Get(3, 3), 1e-5f, "Fire field bled into health cell (3,3)");

            Assert.AreEqual(0f, eduField.Get(1, 1), 1e-5f, "Education field bled into fire cell (1,1)");
            Assert.AreEqual(f.weightsAsset.ServiceEducationCoverage, eduField.Get(2, 2), 1e-5f, "Education field missing at education cell (2,2)");
            Assert.AreEqual(0f, eduField.Get(3, 3), 1e-5f, "Education field bled into health cell (3,3)");

            Assert.AreEqual(0f, healthField.Get(1, 1), 1e-5f, "Health field bled into fire cell (1,1)");
            Assert.AreEqual(0f, healthField.Get(2, 2), 1e-5f, "Health field bled into education cell (2,2)");
            Assert.AreEqual(f.weightsAsset.ServiceHealthCoverage, healthField.Get(3, 3), 1e-5f, "Health field missing at health cell (3,3)");

            Object.DestroyImmediate(fireProdGO);
            Object.DestroyImmediate(eduProdGO);
            f.Destroy();
        }
    }
}
