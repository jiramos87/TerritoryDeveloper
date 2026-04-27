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
    /// Stage 8 unit coverage for <see cref="ServicePoliceProducer"/> +
    /// <see cref="ServicePoliceConsumer"/> (TECH-1954). Compare two fixtures:
    /// A — 4 ResidentialHeavy cells, no police; B — same residential cells +
    /// 1 police-equipped StateServiceLight. Police-fixture residential
    /// <see cref="SimulationSignal.Crime"/> must report strictly lower than no-police fixture.
    /// Floor-clamp invariant — <see cref="SignalField.Add"/> never goes below 0.
    /// </summary>
    [TestFixture]
    public class ServicePoliceConsumerTest
    {
        private const int GRID = 8;

        [SetUp]
        public void SetUp() { }

        private struct Fixture
        {
            public GameObject gridGO;
            public GameObject registryGO;
            public GameObject crimeProdGO;
            public GameObject policeProdGO;
            public GameObject policeConsGO;
            public GridManager grid;
            public SignalFieldRegistry registry;
            public CrimeProducer crimeProducer;
            public ServicePoliceProducer policeProducer;
            public ServicePoliceConsumer policeConsumer;
            public SignalTuningWeightsAsset weightsAsset;

            public void Destroy()
            {
                if (crimeProdGO != null) Object.DestroyImmediate(crimeProdGO);
                if (policeProdGO != null) Object.DestroyImmediate(policeProdGO);
                if (policeConsGO != null) Object.DestroyImmediate(policeConsGO);
                if (registryGO != null) Object.DestroyImmediate(registryGO);
                if (gridGO != null) Object.DestroyImmediate(gridGO);
                if (weightsAsset != null) Object.DestroyImmediate(weightsAsset);
            }
        }

        private static Fixture BuildFixture(string tag, bool placePolice)
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

            // 4 ResidentialHeavy cells.
            int[,] heavy = { { 2, 2 }, { 2, 3 }, { 3, 2 }, { 3, 3 } };
            for (int i = 0; i < heavy.GetLength(0); i++)
            {
                f.grid.cellArray[heavy[i, 0], heavy[i, 1]].zoneType = Zone.ZoneType.ResidentialHeavyBuilding;
            }

            if (placePolice)
            {
                // Police-equipped StateServiceLight at (2,4) adjacent to residential cluster.
                CityCell policeCell = f.grid.cellArray[2, 4];
                policeCell.zoneType = Zone.ZoneType.StateServiceLightBuilding;
                GameObject buildingGO = new GameObject("PoliceBuilding");
                buildingGO.transform.parent = policeCell.transform;
                Zone zoneComp = buildingGO.AddComponent<Zone>();
                zoneComp.SubTypeId = 0; // Police = id 0 per zone-sub-types.json.
                policeCell.occupiedBuilding = buildingGO;
            }

            f.registryGO = new GameObject($"Registry_{tag}");
            f.registry = f.registryGO.AddComponent<SignalFieldRegistry>();
            f.registry.ResizeForMap(GRID, GRID);

            f.weightsAsset = ScriptableObject.CreateInstance<SignalTuningWeightsAsset>();

            f.crimeProdGO = new GameObject($"CrimeProd_{tag}");
            f.crimeProducer = f.crimeProdGO.AddComponent<CrimeProducer>();
            WireField<CrimeProducer>(f.crimeProducer, "gridManager", f.grid);
            WireField<CrimeProducer>(f.crimeProducer, "tuningWeights", f.weightsAsset);

            f.policeProdGO = new GameObject($"PoliceProd_{tag}");
            f.policeProducer = f.policeProdGO.AddComponent<ServicePoliceProducer>();
            WireField<ServicePoliceProducer>(f.policeProducer, "gridManager", f.grid);
            WireField<ServicePoliceProducer>(f.policeProducer, "tuningWeights", f.weightsAsset);

            f.policeConsGO = new GameObject($"PoliceCons_{tag}");
            f.policeConsumer = f.policeConsGO.AddComponent<ServicePoliceConsumer>();
            WireField<ServicePoliceConsumer>(f.policeConsumer, "gridManager", f.grid);
            WireField<ServicePoliceConsumer>(f.policeConsumer, "tuningWeights", f.weightsAsset);

            return f;
        }

        private static void WireField<T>(T instance, string fieldName, object value)
        {
            FieldInfo info = typeof(T).GetField(
                fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(info, $"{typeof(T).Name}.{fieldName} field missing");
            info.SetValue(instance, value);
        }

        // Run producers (step 1) → skip diffusion (out-of-scope for cell-local consumer assertion) → consumer (step 4 surrogate).
        private static void RunPipeline(Fixture f)
        {
            f.crimeProducer.EmitSignals(f.registry);
            f.policeProducer.EmitSignals(f.registry);
            f.policeConsumer.ConsumeSignals(f.registry, new DistrictSignalCache());
        }

        [Test]
        public void ServicePoliceConsumer_Tick_ReducesCrimeAtPolicedCells()
        {
            Fixture noPolice = BuildFixture("NoPolice", placePolice: false);
            Fixture policed = BuildFixture("Policed", placePolice: true);

            RunPipeline(noPolice);
            RunPipeline(policed);

            // Force police signal at residential cells via direct field write so consumer subtracts.
            // (No-diffusion harness — police signal stays at producer cell otherwise.)
            SignalField policeFieldB = policed.registry.GetField(SimulationSignal.ServicePolice);
            policeFieldB.Set(2, 2, policed.weightsAsset.ServicePoliceCoverage);
            policeFieldB.Set(2, 3, policed.weightsAsset.ServicePoliceCoverage);
            policed.policeConsumer.ConsumeSignals(policed.registry, new DistrictSignalCache());

            SignalField crimeA = noPolice.registry.GetField(SimulationSignal.Crime);
            SignalField crimeB = policed.registry.GetField(SimulationSignal.Crime);
            Assert.Greater(crimeA.Get(2, 2), crimeB.Get(2, 2), "policed cell (2,2) Crime not reduced");
            Assert.Greater(crimeA.Get(2, 3), crimeB.Get(2, 3), "policed cell (2,3) Crime not reduced");

            noPolice.Destroy();
            policed.Destroy();
        }

        [Test]
        public void ServicePoliceConsumer_Tick_ClampsCrimeFloorAtZero()
        {
            Fixture f = BuildFixture("ClampZero", placePolice: true);

            // Force massive coverage to drive Crime below zero pre-clamp.
            FieldInfo scaleField = typeof(SignalTuningWeightsAsset).GetField(
                "servicePoliceConsumerScale", BindingFlags.NonPublic | BindingFlags.Instance);
            scaleField.SetValue(f.weightsAsset, 1000f);
            FieldInfo coverageField = typeof(SignalTuningWeightsAsset).GetField(
                "servicePoliceCoverage", BindingFlags.NonPublic | BindingFlags.Instance);
            coverageField.SetValue(f.weightsAsset, 1000f);

            f.crimeProducer.EmitSignals(f.registry);
            // Force police saturation at every cell.
            SignalField policeField = f.registry.GetField(SimulationSignal.ServicePolice);
            for (int x = 0; x < GRID; x++)
            for (int y = 0; y < GRID; y++)
            {
                policeField.Set(x, y, f.weightsAsset.ServicePoliceCoverage);
            }
            f.policeConsumer.ConsumeSignals(f.registry, new DistrictSignalCache());

            SignalField crimeField = f.registry.GetField(SimulationSignal.Crime);
            for (int x = 0; x < GRID; x++)
            for (int y = 0; y < GRID; y++)
            {
                Assert.GreaterOrEqual(crimeField.Get(x, y), 0f, $"cell ({x},{y}) crime negative — clamp violation");
            }

            f.Destroy();
        }

        [Test]
        public void ServicePoliceProducer_OnlyEmitsAtPoliceEquippedStateServiceCells()
        {
            Fixture f = BuildFixture("OnlyPolice", placePolice: true);

            // Add a non-police state-service cell (subtype 1 = Fire) at (4,4).
            CityCell fireCell = f.grid.cellArray[4, 4];
            fireCell.zoneType = Zone.ZoneType.StateServiceLightBuilding;
            GameObject fireBuildingGO = new GameObject("FireBuilding");
            fireBuildingGO.transform.parent = fireCell.transform;
            Zone fireZone = fireBuildingGO.AddComponent<Zone>();
            fireZone.SubTypeId = 1; // Fire = id 1 per zone-sub-types.json.
            fireCell.occupiedBuilding = fireBuildingGO;

            f.policeProducer.EmitSignals(f.registry);

            SignalField policeField = f.registry.GetField(SimulationSignal.ServicePolice);
            // Police cell (2,4) emits coverage.
            Assert.AreEqual(f.weightsAsset.ServicePoliceCoverage, policeField.Get(2, 4), 1e-5f);
            // Fire cell (4,4) — same StateServiceLight but SubTypeId != 0 — must be 0.
            Assert.AreEqual(0f, policeField.Get(4, 4), 1e-5f, "Fire cell (SubTypeId=1) should not emit ServicePolice");
            // Empty / residential cells stay 0.
            Assert.AreEqual(0f, policeField.Get(2, 2), 1e-5f);
            Assert.AreEqual(0f, policeField.Get(0, 0), 1e-5f);

            f.Destroy();
        }

        [Test]
        public void ServicePoliceProducer_EmitsCoverageValueAtPoliceCell()
        {
            Fixture f = BuildFixture("EmitCoverage", placePolice: true);

            f.policeProducer.EmitSignals(f.registry);

            SignalField policeField = f.registry.GetField(SimulationSignal.ServicePolice);
            Assert.AreEqual(f.weightsAsset.ServicePoliceCoverage, policeField.Get(2, 4), 1e-5f);

            f.Destroy();
        }
    }
}
