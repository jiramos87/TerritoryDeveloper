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
    /// Stage 9.C unit coverage for <see cref="WasteProducer"/> (TECH-2200):
    /// RCI cells emit <c>wasteBase + (residentialDensityTier × wasteResidentialDensityWeight) +
    /// (commercialDensityTier × wasteCommercialDensityWeight) + (industrialDensityTier ×
    /// wasteIndustrialDensityWeight)</c>; non-RCI / empty cells emit value 0 (do NOT emit
    /// <c>wasteBase</c> at empty cells); SO round-trip retains the five Stage 9.C waste tuning
    /// fields.
    /// </summary>
    [TestFixture]
    public class WasteProducerTest
    {
        private const int GRID = 20;

        private GameObject gridGO;
        private GameObject registryGO;
        private GameObject producerGO;

        private GridManager grid;
        private SignalFieldRegistry registry;
        private WasteProducer producer;
        private SignalTuningWeightsAsset weightsAsset;

        [SetUp]
        public void SetUp()
        {
            gridGO = new GameObject("WasteProdGrid");
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

            registryGO = new GameObject("WasteProdRegistry");
            registry = registryGO.AddComponent<SignalFieldRegistry>();
            registry.ResizeForMap(GRID, GRID);

            weightsAsset = ScriptableObject.CreateInstance<SignalTuningWeightsAsset>();

            producerGO = new GameObject("WasteProd");
            producer = producerGO.AddComponent<WasteProducer>();
            FieldInfo gridField = typeof(WasteProducer).GetField(
                "gridManager", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(gridField, "gridManager field missing");
            gridField.SetValue(producer, grid);
            FieldInfo weightsField = typeof(WasteProducer).GetField(
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
        public void WasteProducer_Tick_ResidentialHeavyCells_EmitBasePlusThreeTimesResWeight()
        {
            // 6 ResidentialHeavyBuilding cells.
            int[,] heavy = { { 2, 2 }, { 2, 3 }, { 3, 2 }, { 3, 3 }, { 4, 2 }, { 4, 3 } };
            for (int i = 0; i < heavy.GetLength(0); i++)
            {
                grid.cellArray[heavy[i, 0], heavy[i, 1]].zoneType = Zone.ZoneType.ResidentialHeavyBuilding;
            }

            producer.EmitSignals(registry);

            SignalField field = registry.GetField(SimulationSignal.WastePressure);
            Assert.IsNotNull(field);

            // Expected: wasteBase + 3 × wasteResidentialDensityWeight = 0.5 + 3 × 1.0 = 3.5f.
            for (int i = 0; i < heavy.GetLength(0); i++)
            {
                int x = heavy[i, 0];
                int y = heavy[i, 1];
                Assert.GreaterOrEqual(field.Get(x, y), 3.5f, $"R-Heavy cell ({x},{y}) value {field.Get(x, y)} < 3.5");
            }
        }

        [Test]
        public void WasteProducer_Tick_CommercialMediumCells_EmitBasePlusTwoTimesComWeight()
        {
            // 4 CommercialMediumBuilding cells.
            int[,] medium = { { 8, 8 }, { 8, 9 }, { 9, 8 }, { 9, 9 } };
            for (int i = 0; i < medium.GetLength(0); i++)
            {
                grid.cellArray[medium[i, 0], medium[i, 1]].zoneType = Zone.ZoneType.CommercialMediumBuilding;
            }

            producer.EmitSignals(registry);

            SignalField field = registry.GetField(SimulationSignal.WastePressure);
            Assert.IsNotNull(field);

            // Expected: wasteBase + 2 × wasteCommercialDensityWeight = 0.5 + 2 × 1.5 = 3.5f.
            for (int i = 0; i < medium.GetLength(0); i++)
            {
                int x = medium[i, 0];
                int y = medium[i, 1];
                Assert.GreaterOrEqual(field.Get(x, y), 3.5f, $"C-Medium cell ({x},{y}) value {field.Get(x, y)} < 3.5");
            }
        }

        [Test]
        public void WasteProducer_Tick_IndustrialLightCells_EmitBasePlusOneTimesIndWeight()
        {
            // 2 IndustrialLightBuilding cells.
            int[,] light = { { 14, 14 }, { 15, 14 } };
            for (int i = 0; i < light.GetLength(0); i++)
            {
                grid.cellArray[light[i, 0], light[i, 1]].zoneType = Zone.ZoneType.IndustrialLightBuilding;
            }

            producer.EmitSignals(registry);

            SignalField field = registry.GetField(SimulationSignal.WastePressure);
            Assert.IsNotNull(field);

            // Expected: wasteBase + 1 × wasteIndustrialDensityWeight = 0.5 + 1 × 2.0 = 2.5f.
            for (int i = 0; i < light.GetLength(0); i++)
            {
                int x = light[i, 0];
                int y = light[i, 1];
                Assert.GreaterOrEqual(field.Get(x, y), 2.5f, $"I-Light cell ({x},{y}) value {field.Get(x, y)} < 2.5");
            }
        }

        [Test]
        public void WasteProducer_Tick_EmptyCells_EmitZero()
        {
            // Mixed RCI fixture with empty cells around them.
            grid.cellArray[2, 2].zoneType = Zone.ZoneType.ResidentialHeavyBuilding;
            grid.cellArray[8, 8].zoneType = Zone.ZoneType.CommercialMediumBuilding;
            grid.cellArray[14, 14].zoneType = Zone.ZoneType.IndustrialLightBuilding;

            producer.EmitSignals(registry);

            SignalField field = registry.GetField(SimulationSignal.WastePressure);
            // Empty (non-RCI) cells must stay 0 — wasteBase MUST NOT leak.
            Assert.AreEqual(0f, field.Get(0, 0), 1e-5f, "empty cell (0,0) must be 0");
            Assert.AreEqual(0f, field.Get(GRID - 1, GRID - 1), 1e-5f, "empty cell (19,19) must be 0");
            Assert.AreEqual(0f, field.Get(5, 5), 1e-5f, "empty cell (5,5) must be 0");
            Assert.AreEqual(0f, field.Get(10, 10), 1e-5f, "empty cell (10,10) must be 0");
        }

        [Test]
        public void WasteProducer_NullRegistry_NoOp()
        {
            Assert.DoesNotThrow(() => producer.EmitSignals(null));
        }

        [Test]
        public void SignalTuningWeightsAsset_RoundTrip_RetainsWasteFields()
        {
            FieldInfo wasteBaseField = typeof(SignalTuningWeightsAsset).GetField(
                "wasteBase", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo resWeightField = typeof(SignalTuningWeightsAsset).GetField(
                "wasteResidentialDensityWeight", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo comWeightField = typeof(SignalTuningWeightsAsset).GetField(
                "wasteCommercialDensityWeight", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo indWeightField = typeof(SignalTuningWeightsAsset).GetField(
                "wasteIndustrialDensityWeight", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo sanScaleField = typeof(SignalTuningWeightsAsset).GetField(
                "sanitationConsumerScale", BindingFlags.NonPublic | BindingFlags.Instance);

            wasteBaseField.SetValue(weightsAsset, 0.6f);
            resWeightField.SetValue(weightsAsset, 1.1f);
            comWeightField.SetValue(weightsAsset, 1.6f);
            indWeightField.SetValue(weightsAsset, 2.1f);
            sanScaleField.SetValue(weightsAsset, 0.42f);

            SignalTuningWeightsData snapshot = weightsAsset.CaptureSnapshot();

            SignalTuningWeightsAsset target = ScriptableObject.CreateInstance<SignalTuningWeightsAsset>();
            target.RestoreFromData(snapshot);

            Assert.AreEqual(0.6f, target.WasteBase, 1e-5f);
            Assert.AreEqual(1.1f, target.WasteResidentialDensityWeight, 1e-5f);
            Assert.AreEqual(1.6f, target.WasteCommercialDensityWeight, 1e-5f);
            Assert.AreEqual(2.1f, target.WasteIndustrialDensityWeight, 1e-5f);
            Assert.AreEqual(0.42f, target.SanitationConsumerScale, 1e-5f);

            Object.DestroyImmediate(target);
        }
    }
}
