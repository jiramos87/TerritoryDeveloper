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
    /// Stage 9.C integration coverage for <see cref="SanitationConsumer"/> (TECH-2204):
    /// compares two fixtures — A no sanitation, B 1 sanitation cell at (4,2) (Moore-adjacent to
    /// R-Heavy cluster at (3,2)/(3,3)). Positive A: sub-type id 5 (Public Housing) reduces
    /// <see cref="SimulationSignal.WastePressure"/> at adjacent R-Heavy cells. Positive B: sub-type
    /// id 6 (Public Offices) ditto. Negative: sub-type id 0 (Police) — predicate excludes; no
    /// reduction. Floor-clamp invariant — <see cref="SignalField.Add"/> never goes below 0.
    /// </summary>
    [TestFixture]
    public class SanitationConsumerTest
    {
        private const int GRID = 20;

        private struct Fixture
        {
            public GameObject gridGO;
            public GameObject registryGO;
            public GameObject wasteProdGO;
            public GameObject sanitationConsGO;
            public GridManager grid;
            public SignalFieldRegistry registry;
            public WasteProducer wasteProducer;
            public SanitationConsumer sanitationConsumer;
            public SignalTuningWeightsAsset weightsAsset;

            public void Destroy()
            {
                if (wasteProdGO != null) Object.DestroyImmediate(wasteProdGO);
                if (sanitationConsGO != null) Object.DestroyImmediate(sanitationConsGO);
                if (registryGO != null) Object.DestroyImmediate(registryGO);
                if (gridGO != null) Object.DestroyImmediate(gridGO);
                if (weightsAsset != null) Object.DestroyImmediate(weightsAsset);
            }
        }

        // placeSanitation: -1 = none, otherwise sub-type id (5=Public Housing, 6=Public Offices, 0=Police etc).
        private static Fixture BuildFixture(string tag, int sanitationSubTypeId)
        {
            Fixture f = new Fixture();
            f.gridGO = new GameObject($"SanGrid_{tag}");
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

            // 4 ResidentialHeavy cells clustered at (2,2)/(2,3)/(3,2)/(3,3).
            int[,] heavy = { { 2, 2 }, { 2, 3 }, { 3, 2 }, { 3, 3 } };
            for (int i = 0; i < heavy.GetLength(0); i++)
            {
                f.grid.cellArray[heavy[i, 0], heavy[i, 1]].zoneType = Zone.ZoneType.ResidentialHeavyBuilding;
            }

            if (sanitationSubTypeId >= 0)
            {
                // Sanitation cell at (4,2) — Moore-adjacent to (3,2) + (3,3) R-Heavy cells.
                CityCell sanCell = f.grid.cellArray[4, 2];
                sanCell.zoneType = Zone.ZoneType.StateServiceLightBuilding;
                GameObject buildingGO = new GameObject($"SanBuilding_{tag}");
                buildingGO.transform.parent = sanCell.transform;
                Zone zoneComp = buildingGO.AddComponent<Zone>();
                zoneComp.SubTypeId = sanitationSubTypeId;
                sanCell.occupiedBuilding = buildingGO;
            }

            f.registryGO = new GameObject($"SanRegistry_{tag}");
            f.registry = f.registryGO.AddComponent<SignalFieldRegistry>();
            f.registry.ResizeForMap(GRID, GRID);

            f.weightsAsset = ScriptableObject.CreateInstance<SignalTuningWeightsAsset>();

            f.wasteProdGO = new GameObject($"WasteProd_{tag}");
            f.wasteProducer = f.wasteProdGO.AddComponent<WasteProducer>();
            WireField<WasteProducer>(f.wasteProducer, "gridManager", f.grid);
            WireField<WasteProducer>(f.wasteProducer, "tuningWeights", f.weightsAsset);

            f.sanitationConsGO = new GameObject($"SanCons_{tag}");
            f.sanitationConsumer = f.sanitationConsGO.AddComponent<SanitationConsumer>();
            WireField<SanitationConsumer>(f.sanitationConsumer, "gridManager", f.grid);
            WireField<SanitationConsumer>(f.sanitationConsumer, "tuningWeights", f.weightsAsset);

            return f;
        }

        private static void WireField<T>(T instance, string fieldName, object value)
        {
            FieldInfo info = typeof(T).GetField(
                fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(info, $"{typeof(T).Name}.{fieldName} field missing");
            info.SetValue(instance, value);
        }

        // Run producer (step 1) → skip diffusion (out-of-scope) → consumer (step 4 surrogate).
        private static void RunPipeline(Fixture f)
        {
            f.wasteProducer.EmitSignals(f.registry);
            f.sanitationConsumer.ConsumeSignals(f.registry, new DistrictSignalCache());
        }

        [Test]
        public void SanitationConsumer_Tick_PublicHousingSubType5_ReducesWasteAtAdjacentRciCells()
        {
            Fixture noSan = BuildFixture("NoSan", sanitationSubTypeId: -1);
            Fixture withSan = BuildFixture("PublicHousing", sanitationSubTypeId: 5);

            RunPipeline(noSan);
            RunPipeline(withSan);

            SignalField wasteA = noSan.registry.GetField(SimulationSignal.WastePressure);
            SignalField wasteB = withSan.registry.GetField(SimulationSignal.WastePressure);

            // (3,2) + (3,3) are Moore-adjacent to sanitation at (4,2); waste must be strictly less.
            Assert.Greater(wasteA.Get(3, 2), wasteB.Get(3, 2), "(3,2) waste not reduced by sub-type 5 sanitation");
            Assert.Greater(wasteA.Get(3, 3), wasteB.Get(3, 3), "(3,3) waste not reduced by sub-type 5 sanitation");

            noSan.Destroy();
            withSan.Destroy();
        }

        [Test]
        public void SanitationConsumer_Tick_PublicOfficesSubType6_ReducesWasteAtAdjacentRciCells()
        {
            Fixture noSan = BuildFixture("NoSan", sanitationSubTypeId: -1);
            Fixture withSan = BuildFixture("PublicOffices", sanitationSubTypeId: 6);

            RunPipeline(noSan);
            RunPipeline(withSan);

            SignalField wasteA = noSan.registry.GetField(SimulationSignal.WastePressure);
            SignalField wasteB = withSan.registry.GetField(SimulationSignal.WastePressure);

            Assert.Greater(wasteA.Get(3, 2), wasteB.Get(3, 2), "(3,2) waste not reduced by sub-type 6 sanitation");
            Assert.Greater(wasteA.Get(3, 3), wasteB.Get(3, 3), "(3,3) waste not reduced by sub-type 6 sanitation");

            noSan.Destroy();
            withSan.Destroy();
        }

        [Test]
        public void SanitationConsumer_Tick_PoliceSubType0_NoEffectOnWaste()
        {
            Fixture noSan = BuildFixture("NoSan", sanitationSubTypeId: -1);
            Fixture withPolice = BuildFixture("Police", sanitationSubTypeId: 0);

            RunPipeline(noSan);
            RunPipeline(withPolice);

            SignalField wasteA = noSan.registry.GetField(SimulationSignal.WastePressure);
            SignalField wasteB = withPolice.registry.GetField(SimulationSignal.WastePressure);

            // Police sub-type id 0 — predicate excludes; R-Heavy waste must match no-sanitation baseline.
            Assert.AreEqual(wasteA.Get(3, 2), wasteB.Get(3, 2), 1e-5f, "(3,2) waste mismatch — sub-type 0 should not reduce waste");
            Assert.AreEqual(wasteA.Get(3, 3), wasteB.Get(3, 3), 1e-5f, "(3,3) waste mismatch — sub-type 0 should not reduce waste");
            Assert.AreEqual(wasteA.Get(2, 2), wasteB.Get(2, 2), 1e-5f, "(2,2) waste mismatch — sub-type 0 should not reduce waste");

            noSan.Destroy();
            withPolice.Destroy();
        }

        [Test]
        public void SanitationConsumer_Tick_FloorClampWasteAtZero()
        {
            Fixture f = BuildFixture("FloorClamp", sanitationSubTypeId: 5);

            // Force massive sanitation scale to drive waste below zero pre-clamp.
            FieldInfo scaleField = typeof(SignalTuningWeightsAsset).GetField(
                "sanitationConsumerScale", BindingFlags.NonPublic | BindingFlags.Instance);
            scaleField.SetValue(f.weightsAsset, 1000f);

            RunPipeline(f);

            SignalField wasteField = f.registry.GetField(SimulationSignal.WastePressure);
            for (int x = 0; x < GRID; x++)
            {
                for (int y = 0; y < GRID; y++)
                {
                    Assert.GreaterOrEqual(wasteField.Get(x, y), 0f, $"cell ({x},{y}) waste negative — clamp violation");
                }
            }

            f.Destroy();
        }

        [Test]
        public void SanitationConsumer_NullRegistry_NoOp()
        {
            Fixture f = BuildFixture("NullReg", sanitationSubTypeId: 5);
            Assert.DoesNotThrow(() => f.sanitationConsumer.ConsumeSignals(null, new DistrictSignalCache()));
            f.Destroy();
        }
    }
}
