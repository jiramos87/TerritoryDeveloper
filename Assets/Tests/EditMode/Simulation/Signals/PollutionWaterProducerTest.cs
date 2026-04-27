using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Territory.Core;
using Territory.Simulation.Signals;
using Territory.Simulation.Signals.Producers;
using Territory.Utilities.Compute;
using Territory.Zones;

namespace Territory.Tests.EditMode.Simulation.Signals
{
    /// <summary>
    /// Stage 7 unit coverage for <see cref="IndustrialPollutionWaterProducer"/>: producer-side
    /// gating only emits at industrial cells Moore-adjacent to registered open water; otherwise
    /// no contribution. Diffusion remains signal-agnostic. Uses a fake
    /// <see cref="IOpenWaterMapView"/> via test seam to bypass real <c>TerrainManager</c> setup.
    /// </summary>
    [TestFixture]
    public class PollutionWaterProducerTest
    {
        private const int GRID = 8;

        private GameObject gridGO;
        private GameObject registryGO;
        private GameObject producerGO;

        private GridManager grid;
        private SignalFieldRegistry registry;
        private IndustrialPollutionWaterProducer producer;
        private SignalTuningWeightsAsset weightsAsset;

        [SetUp]
        public void SetUp()
        {
            gridGO = new GameObject("PollWaterProdGrid");
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

            registryGO = new GameObject("PollWaterProdRegistry");
            registry = registryGO.AddComponent<SignalFieldRegistry>();
            registry.ResizeForMap(GRID, GRID);

            weightsAsset = ScriptableObject.CreateInstance<SignalTuningWeightsAsset>();

            producerGO = new GameObject("PollWaterProd");
            producer = producerGO.AddComponent<IndustrialPollutionWaterProducer>();
            FieldInfo gridField = typeof(IndustrialPollutionWaterProducer).GetField(
                "gridManager", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(gridField, "gridManager field missing");
            gridField.SetValue(producer, grid);
            FieldInfo weightsField = typeof(IndustrialPollutionWaterProducer).GetField(
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
        public void NoWaterAdjacency_NoEmission()
        {
            // Industrial cell with NO water tiles registered.
            grid.cellArray[2, 2].zoneType = Zone.ZoneType.IndustrialHeavyBuilding;
            producer.SetWaterViewOverride(new FakeWaterView(GRID, GRID, new HashSet<(int, int)>()));

            producer.EmitSignals(registry);

            SignalField field = registry.GetField(SimulationSignal.PollutionWater);
            Assert.IsNotNull(field);
            Assert.AreEqual(0f, field.Get(2, 2), 1e-5f, "No water-adjacent → no emission even at industrial cell");
        }

        [Test]
        public void WaterAdjacent_EmitsTierWeight()
        {
            // Heavy industrial at (2,2); water at Moore-neighbor (3,3).
            grid.cellArray[2, 2].zoneType = Zone.ZoneType.IndustrialHeavyBuilding;
            grid.cellArray[5, 5].zoneType = Zone.ZoneType.IndustrialMediumBuilding;
            grid.cellArray[6, 1].zoneType = Zone.ZoneType.IndustrialLightBuilding;

            HashSet<(int, int)> water = new HashSet<(int, int)>
            {
                (3, 3), // adjacent to (2,2) Heavy
                (4, 5), // adjacent to (5,5) Medium
                (7, 1), // adjacent to (6,1) Light
            };
            producer.SetWaterViewOverride(new FakeWaterView(GRID, GRID, water));

            producer.EmitSignals(registry);

            SignalField field = registry.GetField(SimulationSignal.PollutionWater);
            Assert.AreEqual(weightsAsset.PollutionWaterHeavy, field.Get(2, 2), 1e-5f);
            Assert.AreEqual(weightsAsset.PollutionWaterMedium, field.Get(5, 5), 1e-5f);
            Assert.AreEqual(weightsAsset.PollutionWaterLight, field.Get(6, 1), 1e-5f);
        }

        [Test]
        public void NonIndustrial_NoEmission_EvenWhenWaterAdjacent()
        {
            grid.cellArray[2, 2].zoneType = Zone.ZoneType.ResidentialHeavyBuilding;
            grid.cellArray[3, 3].zoneType = Zone.ZoneType.CommercialHeavyBuilding;

            HashSet<(int, int)> water = new HashSet<(int, int)>
            {
                (3, 2), // adjacent to (2,2) and (3,3)
            };
            producer.SetWaterViewOverride(new FakeWaterView(GRID, GRID, water));

            producer.EmitSignals(registry);

            SignalField field = registry.GetField(SimulationSignal.PollutionWater);
            Assert.AreEqual(0f, field.Get(2, 2), 1e-5f);
            Assert.AreEqual(0f, field.Get(3, 3), 1e-5f);
        }

        [Test]
        public void NullRegistry_NoOp()
        {
            Assert.DoesNotThrow(() => producer.EmitSignals(null));
        }

        private class FakeWaterView : IOpenWaterMapView
        {
            private readonly int _width;
            private readonly int _height;
            private readonly HashSet<(int, int)> _water;

            public FakeWaterView(int width, int height, HashSet<(int, int)> water)
            {
                _width = width;
                _height = height;
                _water = water;
            }

            public bool IsValidGridPosition(int x, int y)
            {
                return x >= 0 && x < _width && y >= 0 && y < _height;
            }

            public bool IsRegisteredOpenWaterAt(int x, int y)
            {
                return _water.Contains((x, y));
            }
        }
    }
}
