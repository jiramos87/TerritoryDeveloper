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
    /// Stage 7 unit coverage for <see cref="LandValueProducer"/>: baseline + park bonus −
    /// industrial penalty + density-tier bonus over Moore neighborhood (TECH-1891).
    /// Forest proxies park-adjacency until a dedicated ParkBuilding enum lands.
    /// </summary>
    [TestFixture]
    public class LandValueProducerTest
    {
        private const int GRID = 8;

        private GameObject gridGO;
        private GameObject registryGO;
        private GameObject producerGO;

        private GridManager grid;
        private SignalFieldRegistry registry;
        private LandValueProducer producer;
        private SignalTuningWeightsAsset weightsAsset;

        [SetUp]
        public void SetUp()
        {
            gridGO = new GameObject("LVProdGrid");
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

            registryGO = new GameObject("LVProdRegistry");
            registry = registryGO.AddComponent<SignalFieldRegistry>();
            registry.ResizeForMap(GRID, GRID);

            weightsAsset = ScriptableObject.CreateInstance<SignalTuningWeightsAsset>();

            producerGO = new GameObject("LVProd");
            producer = producerGO.AddComponent<LandValueProducer>();
            FieldInfo gridField = typeof(LandValueProducer).GetField(
                "gridManager", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(gridField, "gridManager field missing");
            gridField.SetValue(producer, grid);
            FieldInfo weightsField = typeof(LandValueProducer).GetField(
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
        public void EmitsBaselineEverywhere()
        {
            producer.EmitSignals(registry);

            SignalField field = registry.GetField(SimulationSignal.LandValue);
            Assert.IsNotNull(field);
            Assert.AreEqual(weightsAsset.LandValueBase, field.Get(0, 0), 1e-5f);
            Assert.AreEqual(weightsAsset.LandValueBase, field.Get(4, 4), 1e-5f);
            Assert.AreEqual(weightsAsset.LandValueBase, field.Get(7, 7), 1e-5f);
        }

        [Test]
        public void ParkAdjacency_AddsBonus_PerForestNeighbor()
        {
            // Cell (4,4) surrounded by 3 forest neighbors.
            grid.cellArray[3, 3].zoneType = Zone.ZoneType.Forest;
            grid.cellArray[4, 3].zoneType = Zone.ZoneType.Forest;
            grid.cellArray[5, 3].zoneType = Zone.ZoneType.Forest;

            producer.EmitSignals(registry);

            SignalField field = registry.GetField(SimulationSignal.LandValue);
            float expected = weightsAsset.LandValueBase + weightsAsset.LandValueParkBonus * 3f;
            Assert.AreEqual(expected, field.Get(4, 4), 1e-5f);
        }

        [Test]
        public void IndustrialAdjacency_SubtractsPenalty_PerIndustrialNeighbor()
        {
            // Cell (4,4) surrounded by 2 industrial neighbors.
            grid.cellArray[3, 3].zoneType = Zone.ZoneType.IndustrialHeavyBuilding;
            grid.cellArray[5, 5].zoneType = Zone.ZoneType.IndustrialMediumBuilding;

            producer.EmitSignals(registry);

            SignalField field = registry.GetField(SimulationSignal.LandValue);
            float expected = weightsAsset.LandValueBase - weightsAsset.LandValueIndustrialPenalty * 2f;
            Assert.AreEqual(expected, field.Get(4, 4), 1e-5f);
        }

        [Test]
        public void ResidentialHeavy_AddsDensityTier3Bonus()
        {
            grid.cellArray[4, 4].zoneType = Zone.ZoneType.ResidentialHeavyBuilding;

            producer.EmitSignals(registry);

            SignalField field = registry.GetField(SimulationSignal.LandValue);
            float expected = weightsAsset.LandValueBase + weightsAsset.LandValueDensityBonus * 3f;
            Assert.AreEqual(expected, field.Get(4, 4), 1e-5f);
        }

        [Test]
        public void NullRegistry_NoOp()
        {
            Assert.DoesNotThrow(() => producer.EmitSignals(null));
        }
    }
}
