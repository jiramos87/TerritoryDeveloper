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
    /// Stage 9.B unit coverage for <see cref="TrafficProducer"/> (TECH-2136):
    /// road cells emit <c>trafficBase + trafficRoadwayDensityWeight × MooreNeighborRciCount</c>;
    /// off-road cells emit nothing (0); empty-RCI fixture emits trafficBase at road cells;
    /// SO round-trip retains the three Stage 9.B traffic tuning fields.
    /// </summary>
    [TestFixture]
    public class TrafficProducerTest
    {
        private const int GRID = 20;

        private GameObject gridGO;
        private GameObject registryGO;
        private GameObject producerGO;

        private GridManager grid;
        private SignalFieldRegistry registry;
        private TrafficProducer producer;
        private SignalTuningWeightsAsset weightsAsset;

        [SetUp]
        public void SetUp()
        {
            gridGO = new GameObject("TrafProdGrid");
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

            registryGO = new GameObject("TrafProdRegistry");
            registry = registryGO.AddComponent<SignalFieldRegistry>();
            registry.ResizeForMap(GRID, GRID);

            weightsAsset = ScriptableObject.CreateInstance<SignalTuningWeightsAsset>();

            producerGO = new GameObject("TrafProd");
            producer = producerGO.AddComponent<TrafficProducer>();
            FieldInfo gridField = typeof(TrafficProducer).GetField(
                "gridManager", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(gridField, "gridManager field missing");
            gridField.SetValue(producer, grid);
            FieldInfo weightsField = typeof(TrafficProducer).GetField(
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
        public void TrafficProducer_Tick_RoadCellsWithRciFlank_EmitBasePlusDensity()
        {
            // 5-cell road segment (5,5) → (5,9).
            int[,] roadCoords = { { 5, 5 }, { 5, 6 }, { 5, 7 }, { 5, 8 }, { 5, 9 } };
            for (int i = 0; i < roadCoords.GetLength(0); i++)
            {
                int x = roadCoords[i, 0];
                int y = roadCoords[i, 1];
                grid.cellArray[x, y].zoneType = Zone.ZoneType.Road;
            }
            // 4 R-Heavy cells north (column x=4).
            grid.cellArray[4, 5].zoneType = Zone.ZoneType.ResidentialHeavyBuilding;
            grid.cellArray[4, 6].zoneType = Zone.ZoneType.ResidentialHeavyBuilding;
            grid.cellArray[4, 7].zoneType = Zone.ZoneType.ResidentialHeavyBuilding;
            grid.cellArray[4, 8].zoneType = Zone.ZoneType.ResidentialHeavyBuilding;
            // 4 C-Medium cells south (column x=6).
            grid.cellArray[6, 5].zoneType = Zone.ZoneType.CommercialMediumBuilding;
            grid.cellArray[6, 6].zoneType = Zone.ZoneType.CommercialMediumBuilding;
            grid.cellArray[6, 7].zoneType = Zone.ZoneType.CommercialMediumBuilding;
            grid.cellArray[6, 8].zoneType = Zone.ZoneType.CommercialMediumBuilding;

            producer.EmitSignals(registry);

            SignalField field = registry.GetField(SimulationSignal.TrafficLevel);
            Assert.IsNotNull(field);

            // Each road cell at column x=5 has RCI neighbors at x=4 + x=6 (Moore 8-neighborhood).
            // §Acceptance: each road cell value ≥ trafficBase + trafficRoadwayDensityWeight × 1 (= 0.5 + 1.0 = 1.5f).
            for (int i = 0; i < roadCoords.GetLength(0); i++)
            {
                int x = roadCoords[i, 0];
                int y = roadCoords[i, 1];
                Assert.GreaterOrEqual(field.Get(x, y), 1.5f, $"road cell ({x},{y}) value {field.Get(x, y)} < 1.5");
            }
        }

        [Test]
        public void TrafficProducer_Tick_OffRoadCells_EmitZero()
        {
            // 5-cell road segment + RCI flank (same fixture as positive test).
            int[,] roadCoords = { { 5, 5 }, { 5, 6 }, { 5, 7 }, { 5, 8 }, { 5, 9 } };
            for (int i = 0; i < roadCoords.GetLength(0); i++)
            {
                int x = roadCoords[i, 0];
                int y = roadCoords[i, 1];
                grid.cellArray[x, y].zoneType = Zone.ZoneType.Road;
            }
            grid.cellArray[4, 5].zoneType = Zone.ZoneType.ResidentialHeavyBuilding;
            grid.cellArray[6, 5].zoneType = Zone.ZoneType.CommercialMediumBuilding;

            producer.EmitSignals(registry);

            SignalField field = registry.GetField(SimulationSignal.TrafficLevel);

            // Off-road cell at (0,0) — clearly far from road segment, must stay 0.
            Assert.AreEqual(0f, field.Get(0, 0), 1e-5f, "off-road cell (0,0) must be 0");
            Assert.AreEqual(0f, field.Get(GRID - 1, GRID - 1), 1e-5f, "off-road cell (19,19) must be 0");
            // RCI flank cells are RCI, not Road — must stay 0 (predicate filters non-road).
            Assert.AreEqual(0f, field.Get(4, 5), 1e-5f, "RCI flank cell (4,5) must be 0 (not road)");
            Assert.AreEqual(0f, field.Get(6, 5), 1e-5f, "RCI flank cell (6,5) must be 0 (not road)");
        }

        [Test]
        public void TrafficProducer_Tick_RoadCellsWithoutRciFlank_EmitBaseOnly()
        {
            // Road segment with NO RCI cells anywhere.
            int[,] roadCoords = { { 5, 5 }, { 5, 6 }, { 5, 7 }, { 5, 8 }, { 5, 9 } };
            for (int i = 0; i < roadCoords.GetLength(0); i++)
            {
                int x = roadCoords[i, 0];
                int y = roadCoords[i, 1];
                grid.cellArray[x, y].zoneType = Zone.ZoneType.Road;
            }

            producer.EmitSignals(registry);

            SignalField field = registry.GetField(SimulationSignal.TrafficLevel);
            float expected = weightsAsset.TrafficBase;
            for (int i = 0; i < roadCoords.GetLength(0); i++)
            {
                int x = roadCoords[i, 0];
                int y = roadCoords[i, 1];
                Assert.AreEqual(expected, field.Get(x, y), 1e-5f, $"road cell ({x},{y}) without RCI flank expected trafficBase ({expected})");
            }
        }

        [Test]
        public void TrafficProducer_NullRegistry_NoOp()
        {
            Assert.DoesNotThrow(() => producer.EmitSignals(null));
        }

        [Test]
        public void SignalTuningWeightsAsset_RoundTrip_RetainsTrafficFields()
        {
            FieldInfo trafficBaseField = typeof(SignalTuningWeightsAsset).GetField(
                "trafficBase", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo densityField = typeof(SignalTuningWeightsAsset).GetField(
                "trafficRoadwayDensityWeight", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo consumerScaleField = typeof(SignalTuningWeightsAsset).GetField(
                "trafficLevelConsumerScale", BindingFlags.NonPublic | BindingFlags.Instance);

            trafficBaseField.SetValue(weightsAsset, 0.7f);
            densityField.SetValue(weightsAsset, 1.4f);
            consumerScaleField.SetValue(weightsAsset, 0.42f);

            SignalTuningWeightsData snapshot = weightsAsset.CaptureSnapshot();

            SignalTuningWeightsAsset target = ScriptableObject.CreateInstance<SignalTuningWeightsAsset>();
            target.RestoreFromData(snapshot);

            Assert.AreEqual(0.7f, target.TrafficBase, 1e-5f);
            Assert.AreEqual(1.4f, target.TrafficRoadwayDensityWeight, 1e-5f);
            Assert.AreEqual(0.42f, target.TrafficLevelConsumerScale, 1e-5f);

            Object.DestroyImmediate(target);
        }
    }
}
