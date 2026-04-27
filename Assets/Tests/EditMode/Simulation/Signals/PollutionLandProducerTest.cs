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
    /// Stage 7 unit coverage for <see cref="IndustrialPollutionLandProducer"/>: emits
    /// <see cref="SimulationSignal.PollutionLand"/> at industrial cells per tier weight,
    /// zero elsewhere. Null-registry guard. Weights pulled from default
    /// <see cref="SignalTuningWeightsAsset"/> (Heavy=2.5, Medium=1.5, Light=0.5).
    /// </summary>
    [TestFixture]
    public class PollutionLandProducerTest
    {
        private const int GRID = 8;

        private GameObject gridGO;
        private GameObject registryGO;
        private GameObject producerGO;

        private GridManager grid;
        private SignalFieldRegistry registry;
        private IndustrialPollutionLandProducer producer;
        private SignalTuningWeightsAsset weightsAsset;

        [SetUp]
        public void SetUp()
        {
            gridGO = new GameObject("PollLandProdGrid");
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

            registryGO = new GameObject("PollLandProdRegistry");
            registry = registryGO.AddComponent<SignalFieldRegistry>();
            registry.ResizeForMap(GRID, GRID);

            weightsAsset = ScriptableObject.CreateInstance<SignalTuningWeightsAsset>();

            producerGO = new GameObject("PollLandProd");
            producer = producerGO.AddComponent<IndustrialPollutionLandProducer>();
            FieldInfo gridField = typeof(IndustrialPollutionLandProducer).GetField(
                "gridManager", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(gridField, "gridManager field missing");
            gridField.SetValue(producer, grid);
            FieldInfo weightsField = typeof(IndustrialPollutionLandProducer).GetField(
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
        public void EmitsAtIndustrialCells_ZeroElsewhere()
        {
            grid.cellArray[2, 2].zoneType = Zone.ZoneType.IndustrialHeavyBuilding;
            grid.cellArray[3, 3].zoneType = Zone.ZoneType.IndustrialMediumBuilding;
            grid.cellArray[4, 4].zoneType = Zone.ZoneType.IndustrialLightBuilding;

            producer.EmitSignals(registry);

            SignalField field = registry.GetField(SimulationSignal.PollutionLand);
            Assert.IsNotNull(field, "PollutionLand field missing");
            Assert.AreEqual(weightsAsset.PollutionLandHeavy, field.Get(2, 2), 1e-5f);
            Assert.AreEqual(weightsAsset.PollutionLandMedium, field.Get(3, 3), 1e-5f);
            Assert.AreEqual(weightsAsset.PollutionLandLight, field.Get(4, 4), 1e-5f);
            Assert.AreEqual(0f, field.Get(0, 0), 1e-5f);
            Assert.AreEqual(0f, field.Get(7, 7), 1e-5f);
            Assert.AreEqual(0f, field.Get(5, 5), 1e-5f);
        }

        [Test]
        public void HeavyTierWeightApplied()
        {
            grid.cellArray[1, 1].zoneType = Zone.ZoneType.IndustrialHeavyBuilding;

            producer.EmitSignals(registry);

            SignalField field = registry.GetField(SimulationSignal.PollutionLand);
            Assert.AreEqual(2.5f, field.Get(1, 1), 1e-5f, "Default heavy weight should be 2.5f");
        }

        [Test]
        public void NullRegistry_NoOp()
        {
            Assert.DoesNotThrow(() => producer.EmitSignals(null));
        }
    }
}
