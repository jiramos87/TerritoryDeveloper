using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Territory.Core;
using Territory.Simulation;
using Territory.Simulation.Signals;
using Territory.Zones;

namespace Territory.Tests.EditMode.Simulation.Signals
{
    /// <summary>
    /// TECH-1725 — Stage 5 parity coverage for <see cref="DesirabilityComposer"/> + the
    /// FEAT-43 toggle wired through <see cref="ZoneManager.AverageSectionDesirability"/>.
    ///
    /// Locks four invariants over a fixed 20×20 fixture:
    /// (1) <c>CellValue</c> stays in <c>[0,1]</c> over a 30-tick warmup,
    /// (2) no NaN/Infinity creeps into any cell post-warmup,
    /// (3) toggle-off path bit-identical to the legacy <c>CityCell.desirability</c>
    /// average (<c>1e-5f</c>),
    /// (4) toggle-on path matches the <c>composer.CellValue</c> average (<c>1e-5f</c>).
    ///
    /// Stub producers emit <see cref="SimulationSignal.PollutionAir"/> at 6 industrial
    /// cells, <see cref="SimulationSignal.ServiceParks"/> at 4 park cells, and a uniform
    /// <see cref="SimulationSignal.LandValue"/> baseline of <c>50f</c> across the grid.
    /// </summary>
    [TestFixture]
    public class DesirabilityComposerParityTest
    {
        private const int GRID = 20;
        private const int WARMUP_TICKS = 30;
        private const float TOLERANCE = 1e-5f;

        // 5-cell test section that includes ≥1 industrial coord (3,3) + ≥1 park coord (10,10).
        private static readonly Vector2[] TestSection = new Vector2[]
        {
            new Vector2(3, 3),
            new Vector2(5, 5),
            new Vector2(7, 7),
            new Vector2(10, 10),
            new Vector2(12, 12),
        };

        private GameObject gridGO;
        private GameObject registryGO;
        private GameObject districtGO;
        private GameObject centroidGO;
        private GameObject composerGO;
        private GameObject pollutionProducerGO;
        private GameObject parksProducerGO;
        private GameObject landValueProducerGO;
        private GameObject schedulerGO;
        private GameObject autoZoningGO;
        private GameObject zoneManagerGO;
        private SignalMetadataRegistry metadataAsset;

        private GridManager grid;
        private SignalFieldRegistry registry;
        private DistrictManager districtManager;
        private UrbanCentroidService centroid;
        private DesirabilityComposer composer;
        private SignalTickScheduler scheduler;
        private AutoZoningManager autoZoningManager;
        private ZoneManager zoneManager;
        private SignalTuningWeightsAsset weightsAsset;

        // Industrial cells emit PollutionAir.
        private static readonly int[][] IndustrialCoords = new int[][]
        {
            new int[] { 2, 2 }, new int[] { 3, 2 }, new int[] { 2, 3 },
            new int[] { 3, 3 }, new int[] { 4, 2 }, new int[] { 4, 3 },
        };

        // Park cells emit ServiceParks.
        private static readonly int[][] ParkCoords = new int[][]
        {
            new int[] { 10, 10 }, new int[] { 11, 10 },
            new int[] { 10, 11 }, new int[] { 11, 11 },
        };

        [SetUp]
        public void SetUp()
        {
            // 1. GridManager — populate dims + cellArray with default-zoned CityCells.
            gridGO = new GameObject("ParityTestGridManager");
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
                    // Seed legacy desirability with a deterministic-but-non-uniform value so
                    // toggle-off path average is distinguishable from toggle-on path average.
                    cell.desirability = (float)(x * GRID + y) / (float)(GRID * GRID);
                    grid.cellArray[x, y] = cell;
                }
            }
            grid.isInitialized = true;

            // 2. SignalFieldRegistry — allocate fields at GRID dims.
            registryGO = new GameObject("ParityTestSignalFieldRegistry");
            registry = registryGO.AddComponent<SignalFieldRegistry>();
            registry.ResizeForMap(GRID, GRID);

            // 3. UrbanCentroidService + DistrictManager — composer reads districtManager ref.
            centroidGO = new GameObject("ParityTestUrbanCentroidService");
            centroid = centroidGO.AddComponent<UrbanCentroidService>();
            centroid.gridManager = grid;
            centroid.RecalculateFromGrid();

            districtGO = new GameObject("ParityTestDistrictManager");
            districtManager = districtGO.AddComponent<DistrictManager>();
            FieldInfo dmCentroidField = typeof(DistrictManager).GetField(
                "centroid", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo dmGridField = typeof(DistrictManager).GetField(
                "grid", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(dmCentroidField, "DistrictManager.centroid field missing");
            Assert.IsNotNull(dmGridField, "DistrictManager.grid field missing");
            dmCentroidField.SetValue(districtManager, centroid);
            dmGridField.SetValue(districtManager, grid);
            MethodInfo dmAwake = typeof(DistrictManager).GetMethod(
                "Awake", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(dmAwake, "DistrictManager.Awake method missing");
            dmAwake.Invoke(districtManager, null);

            // 4. DesirabilityComposer — Inspector-style ref injection (incl. tuning weights SO) + force Start to alloc cells.
            weightsAsset = ScriptableObject.CreateInstance<SignalTuningWeightsAsset>();
            composerGO = new GameObject("ParityTestDesirabilityComposer");
            composer = composerGO.AddComponent<DesirabilityComposer>();
            InjectField(composer, "gridManager", grid);
            InjectField(composer, "districtManager", districtManager);
            InjectField(composer, "weights", weightsAsset);
            MethodInfo composerStart = typeof(DesirabilityComposer).GetMethod(
                "Start", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(composerStart, "DesirabilityComposer.Start method missing");
            composerStart.Invoke(composer, null);

            // 5. Stub producers — PollutionAir / ServiceParks / LandValue baseline.
            pollutionProducerGO = new GameObject("ParityTestPollutionProducer");
            StubPollutionProducer pollutionProducer = pollutionProducerGO.AddComponent<StubPollutionProducer>();

            parksProducerGO = new GameObject("ParityTestParksProducer");
            StubParksProducer parksProducer = parksProducerGO.AddComponent<StubParksProducer>();

            landValueProducerGO = new GameObject("ParityTestLandValueProducer");
            StubLandValueProducer landValueProducer = landValueProducerGO.AddComponent<StubLandValueProducer>();

            // 6. SignalMetadataRegistry — uniform diffusion entries (12 signals).
            metadataAsset = ScriptableObject.CreateInstance<SignalMetadataRegistry>();
            FieldInfo entriesField = typeof(SignalMetadataRegistry).GetField(
                "entries", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(entriesField, "SignalMetadataRegistry.entries field missing");
            int signalCount = System.Enum.GetValues(typeof(SimulationSignal)).Length;
            SignalMetadataRegistry.Entry[] entries = new SignalMetadataRegistry.Entry[signalCount];
            for (int i = 0; i < entries.Length; i++)
            {
                entries[i] = new SignalMetadataRegistry.Entry
                {
                    diffusionRadius = 2f,
                    decayPerStep = 0.05f,
                    anisotropy = new Vector2(1f, 1f),
                    rollup = RollupRule.Mean,
                };
            }
            entriesField.SetValue(metadataAsset, entries);

            // 7. SignalTickScheduler — wire registry + metadata + producer/consumer lists via reflection.
            schedulerGO = new GameObject("ParityTestSignalTickScheduler");
            scheduler = schedulerGO.AddComponent<SignalTickScheduler>();
            FieldInfo schedRegistry = typeof(SignalTickScheduler).GetField(
                "registry", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo schedMetadata = typeof(SignalTickScheduler).GetField(
                "metadata", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo schedDistrict = typeof(SignalTickScheduler).GetField(
                "districtManager", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo schedProducerList = typeof(SignalTickScheduler).GetField(
                "producerList", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo schedConsumerList = typeof(SignalTickScheduler).GetField(
                "consumerList", BindingFlags.NonPublic | BindingFlags.Instance);
            schedRegistry.SetValue(scheduler, registry);
            schedMetadata.SetValue(scheduler, metadataAsset);
            schedDistrict.SetValue(scheduler, districtManager);
            schedProducerList.SetValue(scheduler, new List<MonoBehaviour>
            {
                pollutionProducer, parksProducer, landValueProducer,
            });
            schedConsumerList.SetValue(scheduler, new List<MonoBehaviour> { composer });
            MethodInfo schedAwake = typeof(SignalTickScheduler).GetMethod(
                "Awake", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(schedAwake, "SignalTickScheduler.Awake method missing");
            schedAwake.Invoke(scheduler, null);

            // 8. AutoZoningManager — toggle host (default useSignalDesirability=false).
            autoZoningGO = new GameObject("ParityTestAutoZoningManager");
            autoZoningManager = autoZoningGO.AddComponent<AutoZoningManager>();
            autoZoningManager.gridManager = grid;
            InjectField(autoZoningManager, "desirabilityComposer", composer);

            // 9. ZoneManager — gridManager + autoZoningManager + desirabilityComposer wired.
            zoneManagerGO = new GameObject("ParityTestZoneManager");
            zoneManager = zoneManagerGO.AddComponent<ZoneManager>();
            zoneManager.gridManager = grid;
            InjectField(zoneManager, "autoZoningManager", autoZoningManager);
            InjectField(zoneManager, "desirabilityComposer", composer);
        }

        [TearDown]
        public void TearDown()
        {
            if (zoneManagerGO != null) Object.DestroyImmediate(zoneManagerGO);
            if (autoZoningGO != null) Object.DestroyImmediate(autoZoningGO);
            if (schedulerGO != null) Object.DestroyImmediate(schedulerGO);
            if (landValueProducerGO != null) Object.DestroyImmediate(landValueProducerGO);
            if (parksProducerGO != null) Object.DestroyImmediate(parksProducerGO);
            if (pollutionProducerGO != null) Object.DestroyImmediate(pollutionProducerGO);
            if (composerGO != null) Object.DestroyImmediate(composerGO);
            if (districtGO != null) Object.DestroyImmediate(districtGO);
            if (centroidGO != null) Object.DestroyImmediate(centroidGO);
            if (registryGO != null) Object.DestroyImmediate(registryGO);
            if (gridGO != null) Object.DestroyImmediate(gridGO);
            if (metadataAsset != null) Object.DestroyImmediate(metadataAsset);
            if (weightsAsset != null) Object.DestroyImmediate(weightsAsset);
        }

        [Test]
        public void CellValueInUnitInterval()
        {
            for (int i = 0; i < WARMUP_TICKS; i++)
            {
                scheduler.Tick(1f);
            }

            for (int x = 0; x < GRID; x++)
            {
                for (int y = 0; y < GRID; y++)
                {
                    float v = composer.CellValue(x, y);
                    Assert.That(v, Is.InRange(0f, 1f),
                        $"DesirabilityComposer.CellValue({x},{y})={v} outside [0,1] after {WARMUP_TICKS} ticks");
                }
            }
        }

        [Test]
        public void NoNaNOrInfinityOverWarmup()
        {
            for (int i = 0; i < WARMUP_TICKS; i++)
            {
                scheduler.Tick(1f);
                for (int x = 0; x < GRID; x++)
                {
                    for (int y = 0; y < GRID; y++)
                    {
                        float v = composer.CellValue(x, y);
                        Assert.IsFalse(float.IsNaN(v),
                            $"DesirabilityComposer.CellValue({x},{y}) went NaN at tick {i + 1}");
                        Assert.IsFalse(float.IsInfinity(v),
                            $"DesirabilityComposer.CellValue({x},{y}) went Infinity at tick {i + 1}");
                    }
                }
            }
        }

        [Test]
        public void ToggleOffPreservesLegacyPath()
        {
            // Toggle OFF — useSignalDesirability=false (default). ZoneManager reads CityCell.desirability.
            InjectField(autoZoningManager, "useSignalDesirability", false);
            for (int i = 0; i < WARMUP_TICKS; i++)
            {
                scheduler.Tick(1f);
            }

            float zmAvg = InvokeAverageSectionDesirability(new List<Vector2>(TestSection));

            // Direct legacy formula — sum of CityCell.desirability / N.
            float total = 0f;
            for (int i = 0; i < TestSection.Length; i++)
            {
                CityCell c = grid.GetCell((int)TestSection[i].x, (int)TestSection[i].y);
                total += c != null ? c.desirability : 0f;
            }
            float expected = total / TestSection.Length;

            Assert.AreEqual(expected, zmAvg, TOLERANCE,
                $"ZoneManager.AverageSectionDesirability toggle-off ({zmAvg}) drifted from legacy CityCell.desirability avg ({expected}) > {TOLERANCE}");
        }

        [Test]
        public void ToggleOnSwitchesPath()
        {
            // Toggle ON — useSignalDesirability=true + composer wired. ZoneManager reads composer.CellValue.
            InjectField(autoZoningManager, "useSignalDesirability", true);
            Assert.IsTrue(autoZoningManager.IsSignalDesirabilityEnabled,
                "FEAT-43 toggle did not engage despite useSignalDesirability=true + composer wired");

            for (int i = 0; i < WARMUP_TICKS; i++)
            {
                scheduler.Tick(1f);
            }

            float zmAvg = InvokeAverageSectionDesirability(new List<Vector2>(TestSection));

            // Direct composer formula — sum of CellValue(sx,sy) / N.
            float total = 0f;
            for (int i = 0; i < TestSection.Length; i++)
            {
                total += composer.CellValue((int)TestSection[i].x, (int)TestSection[i].y);
            }
            float expected = total / TestSection.Length;

            Assert.AreEqual(expected, zmAvg, TOLERANCE,
                $"ZoneManager.AverageSectionDesirability toggle-on ({zmAvg}) drifted from composer.CellValue avg ({expected}) > {TOLERANCE}");
        }

        // --- helpers ---

        private static void InjectField(MonoBehaviour target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(
                name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"{target.GetType().Name}.{name} field missing");
            field.SetValue(target, value);
        }

        private float InvokeAverageSectionDesirability(List<Vector2> section)
        {
            MethodInfo method = typeof(ZoneManager).GetMethod(
                "AverageSectionDesirability", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, "ZoneManager.AverageSectionDesirability method missing");
            return (float)method.Invoke(zoneManager, new object[] { section });
        }

        // --- stub producers ---

        /// <summary>Emits <see cref="SimulationSignal.PollutionAir"/> at the 6 industrial coords.</summary>
        private class StubPollutionProducer : MonoBehaviour, ISignalProducer
        {
            public void EmitSignals(SignalFieldRegistry registry)
            {
                if (registry == null) return;
                SignalField field = registry.GetField(SimulationSignal.PollutionAir);
                if (field == null) return;
                for (int i = 0; i < IndustrialCoords.Length; i++)
                {
                    field.Add(IndustrialCoords[i][0], IndustrialCoords[i][1], 4f);
                }
            }
        }

        /// <summary>Emits <see cref="SimulationSignal.ServiceParks"/> at the 4 park coords.</summary>
        private class StubParksProducer : MonoBehaviour, ISignalProducer
        {
            public void EmitSignals(SignalFieldRegistry registry)
            {
                if (registry == null) return;
                SignalField field = registry.GetField(SimulationSignal.ServiceParks);
                if (field == null) return;
                for (int i = 0; i < ParkCoords.Length; i++)
                {
                    field.Add(ParkCoords[i][0], ParkCoords[i][1], 6f);
                }
            }
        }

        /// <summary>Emits a uniform <see cref="SimulationSignal.LandValue"/> baseline of <c>50f</c> across the grid.</summary>
        private class StubLandValueProducer : MonoBehaviour, ISignalProducer
        {
            public void EmitSignals(SignalFieldRegistry registry)
            {
                if (registry == null) return;
                SignalField field = registry.GetField(SimulationSignal.LandValue);
                if (field == null) return;
                for (int x = 0; x < GRID; x++)
                {
                    for (int y = 0; y < GRID; y++)
                    {
                        field.Add(x, y, 50f);
                    }
                }
            }
        }
    }
}
