using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Territory.Core;
using Territory.Simulation.Signals;
using Territory.Simulation.Signals.Producers;
using Territory.Zones;

namespace Territory.Tests.EditMode.Simulation.Signals
{
    /// <summary>
    /// Stage 8 unit coverage for <see cref="CrimeProducer"/> (TECH-1953):
    /// baseline at empty cells; +densityWeight×3 at ResidentialHeavy cells; producer
    /// must NOT read <see cref="SimulationSignal.ServicePolice"/> mid-tick (consumer
    /// owns reduction per <c>simulation-signals.md</c> §Interface contract step 4);
    /// <see cref="SignalTuningWeightsAsset"/> CaptureSnapshot/RestoreFromData round-trips
    /// the five new Crime/Police tuning fields.
    /// </summary>
    [TestFixture]
    public class CrimeProducerTest
    {
        private const int GRID = 20;

        private GameObject gridGO;
        private GameObject registryGO;
        private GameObject producerGO;

        private GridManager grid;
        private SignalFieldRegistry registry;
        private CrimeProducer producer;
        private SignalTuningWeightsAsset weightsAsset;

        [SetUp]
        public void SetUp()
        {
            gridGO = new GameObject("CrimeProdGrid");
            grid = gridGO.AddComponent<GridManager>();
            grid.width = GRID;
            grid.height = GRID;
            grid.tileWidth = 1f;
            grid.tileHeight = 0.5f;
            grid.cellArray = new CityCell[GRID, GRID];
            for (int x = 0; x < GRID; x++)
            {
                for (int y = 0; y < GRID; y++)
                {
                    GameObject cellGO = new GameObject($"Cell_{x}_{y}");
                    cellGO.transform.parent = gridGO.transform;
                    CityCell cell = cellGO.AddComponent<CityCell>();
                    cell.zoneType = Zone.ZoneType.None;
                    grid.cellArray[x, y] = cell;
                }
            }
            grid.isInitialized = true;

            registryGO = new GameObject("CrimeProdRegistry");
            registry = registryGO.AddComponent<SignalFieldRegistry>();
            registry.ResizeForMap(GRID, GRID);

            weightsAsset = ScriptableObject.CreateInstance<SignalTuningWeightsAsset>();

            producerGO = new GameObject("CrimeProd");
            producer = producerGO.AddComponent<CrimeProducer>();
            FieldInfo gridField = typeof(CrimeProducer).GetField(
                "gridManager", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(gridField, "gridManager field missing");
            gridField.SetValue(producer, grid);
            FieldInfo weightsField = typeof(CrimeProducer).GetField(
                "tuningWeights", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(weightsField, "tuningWeights field missing");
            weightsField.SetValue(producer, weightsAsset);
        }

        [TearDown]
        public void TearDown()
        {
            if (producerGO != null) Object.DestroyImmediate(producerGO);
            if (registryGO != null) Object.DestroyImmediate(registryGO);
            if (gridGO != null) Object.DestroyImmediate(gridGO);
            if (weightsAsset != null) Object.DestroyImmediate(weightsAsset);
        }

        [Test]
        public void CrimeProducer_Tick_AppliesBaseAtEmptyCells()
        {
            producer.EmitSignals(registry);

            SignalField field = registry.GetField(SimulationSignal.Crime);
            Assert.IsNotNull(field);
            Assert.AreEqual(weightsAsset.CrimeBase, field.Get(0, 0), 1e-5f);
            Assert.AreEqual(weightsAsset.CrimeBase, field.Get(10, 10), 1e-5f);
            Assert.AreEqual(weightsAsset.CrimeBase, field.Get(GRID - 1, GRID - 1), 1e-5f);
        }

        [Test]
        public void CrimeProducer_Tick_AppliesBasePlusDensityAtResidentialHeavy()
        {
            // 6 ResidentialHeavy cells per §Acceptance row 5.
            int[,] heavyCoords = { { 5, 5 }, { 5, 6 }, { 5, 7 }, { 6, 5 }, { 6, 6 }, { 6, 7 } };
            for (int i = 0; i < heavyCoords.GetLength(0); i++)
            {
                int x = heavyCoords[i, 0];
                int y = heavyCoords[i, 1];
                grid.cellArray[x, y].zoneType = Zone.ZoneType.ResidentialHeavyBuilding;
            }

            producer.EmitSignals(registry);

            SignalField field = registry.GetField(SimulationSignal.Crime);
            float expected = weightsAsset.CrimeBase + weightsAsset.CrimeDensityWeight * 3f;
            for (int i = 0; i < heavyCoords.GetLength(0); i++)
            {
                int x = heavyCoords[i, 0];
                int y = heavyCoords[i, 1];
                Assert.AreEqual(expected, field.Get(x, y), 1e-5f, $"cell ({x},{y}) expected {expected}");
                Assert.GreaterOrEqual(field.Get(x, y), 7.0f, $"cell ({x},{y}) below 7.0 floor");
            }
        }

        [Test]
        public void CrimeProducer_DoesNotReadServicePoliceField()
        {
            // Pre-seed ServicePolice with a sentinel; assert producer leaves it intact.
            SignalField policeField = registry.GetField(SimulationSignal.ServicePolice);
            policeField.Set(0, 0, 99f);
            policeField.Set(10, 10, 42f);

            producer.EmitSignals(registry);

            Assert.AreEqual(99f, policeField.Get(0, 0), 1e-5f, "producer mutated ServicePolice — contract violation");
            Assert.AreEqual(42f, policeField.Get(10, 10), 1e-5f, "producer mutated ServicePolice — contract violation");
        }

        [Test]
        public void SignalTuningWeightsAsset_RoundTrip_RetainsCrimeFields()
        {
            // Mutate via reflection (private setters not exposed).
            FieldInfo crimeBaseField = typeof(SignalTuningWeightsAsset).GetField(
                "crimeBase", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo densityField = typeof(SignalTuningWeightsAsset).GetField(
                "crimeDensityWeight", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo coverageField = typeof(SignalTuningWeightsAsset).GetField(
                "servicePoliceCoverage", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo scaleField = typeof(SignalTuningWeightsAsset).GetField(
                "servicePoliceConsumerScale", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo thresholdField = typeof(SignalTuningWeightsAsset).GetField(
                "crimeHotspotThreshold", BindingFlags.NonPublic | BindingFlags.Instance);

            crimeBaseField.SetValue(weightsAsset, 7.5f);
            densityField.SetValue(weightsAsset, 11.5f);
            coverageField.SetValue(weightsAsset, 13.5f);
            scaleField.SetValue(weightsAsset, 0.91f);
            thresholdField.SetValue(weightsAsset, 23.5f);

            SignalTuningWeightsData snapshot = weightsAsset.CaptureSnapshot();

            // Round-trip via fresh instance.
            SignalTuningWeightsAsset target = ScriptableObject.CreateInstance<SignalTuningWeightsAsset>();
            target.RestoreFromData(snapshot);

            Assert.AreEqual(7.5f, target.CrimeBase, 1e-5f);
            Assert.AreEqual(11.5f, target.CrimeDensityWeight, 1e-5f);
            Assert.AreEqual(13.5f, target.ServicePoliceCoverage, 1e-5f);
            Assert.AreEqual(0.91f, target.ServicePoliceConsumerScale, 1e-5f);
            Assert.AreEqual(23.5f, target.CrimeHotspotThreshold, 1e-5f);

            Object.DestroyImmediate(target);
        }

        [Test]
        public void CrimeProducer_NullRegistry_NoOp()
        {
            Assert.DoesNotThrow(() => producer.EmitSignals(null));
        }
    }
}
