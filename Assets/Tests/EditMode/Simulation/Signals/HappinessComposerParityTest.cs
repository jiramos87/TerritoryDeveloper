using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Territory.Buildings;
using Territory.Core;
using Territory.Economy;
using Territory.Forests;
using Territory.Simulation;
using Territory.Simulation.Signals;
using Territory.Simulation.Signals.Producers;
using Territory.Zones;

namespace Territory.Tests.EditMode.Simulation.Signals
{
    /// <summary>
    /// Stage 4 parity coverage for <see cref="HappinessComposer"/>: 30-tick warmup over a
    /// fixed 20×20 grid with 6 industrial-heavy + 12 forest + 1 nuclear power plant. Asserts
    /// (a) <see cref="HappinessComposer.Current"/> sits within ±5 of the legacy
    /// <c>CityStats.ComputeTargetHappiness</c> result invoked at the same fixture state,
    /// (b) no NaN/Infinity creeps into the composer over the warmup, and
    /// (c) the diffused <see cref="SimulationSignal.PollutionAir"/> field stays non-negative
    /// despite the forest sink subtracting absorption.
    /// </summary>
    [TestFixture]
    public class HappinessComposerParityTest
    {
        private const int GRID = 20;
        private const int WARMUP_TICKS = 30;

        private GameObject gridGO;
        private GameObject forestGO;
        private GameObject employmentGO;
        private GameObject economyGO;
        private GameObject cityStatsGO;
        private GameObject registryGO;
        private GameObject districtGO;
        private GameObject centroidGO;
        private GameObject composerGO;
        private GameObject industrialProducerGO;
        private GameObject powerPlantProducerGO;
        private GameObject forestSinkGO;
        private GameObject powerPlantGO;
        private GameObject schedulerGO;
        private SignalMetadataRegistry metadataAsset;

        private GridManager grid;
        private ForestManager forestManager;
        private EmploymentManager employmentManager;
        private EconomyManager economyManager;
        private CityStats cityStats;
        private SignalFieldRegistry registry;
        private DistrictManager districtManager;
        private UrbanCentroidService centroid;
        private HappinessComposer composer;
        private IndustrialPollutionProducer industrialProducer;
        private PowerPlantPollutionProducer powerPlantProducer;
        private ForestPollutionSink forestSink;
        private PowerPlant powerPlant;
        private SignalTickScheduler scheduler;

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
                    grid.cellArray[x, y] = cell;
                }
            }
            grid.isInitialized = true;

            // Stamp 6 industrial-heavy zones at deterministic coords.
            int[][] industrialCoords = new int[][]
            {
                new int[] { 2, 2 }, new int[] { 3, 2 }, new int[] { 2, 3 },
                new int[] { 3, 3 }, new int[] { 4, 2 }, new int[] { 4, 3 },
            };
            for (int i = 0; i < industrialCoords.Length; i++)
            {
                grid.cellArray[industrialCoords[i][0], industrialCoords[i][1]].zoneType =
                    Zone.ZoneType.IndustrialHeavyBuilding;
            }

            // 2. ForestManager — inject private forestMap with 12 forest cells.
            forestGO = new GameObject("ParityTestForestManager");
            forestManager = forestGO.AddComponent<ForestManager>();
            forestManager.gridManager = grid;
            ForestMap forestMap = new ForestMap(GRID, GRID);
            int[][] forestCoords = new int[][]
            {
                new int[] { 10, 10 }, new int[] { 11, 10 }, new int[] { 12, 10 },
                new int[] { 10, 11 }, new int[] { 11, 11 }, new int[] { 12, 11 },
                new int[] { 10, 12 }, new int[] { 11, 12 }, new int[] { 12, 12 },
                new int[] { 13, 10 }, new int[] { 13, 11 }, new int[] { 13, 12 },
            };
            for (int i = 0; i < forestCoords.Length; i++)
            {
                forestMap.SetForestType(forestCoords[i][0], forestCoords[i][1], Forest.ForestType.Medium);
            }
            FieldInfo forestMapField = typeof(ForestManager).GetField(
                "forestMap", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(forestMapField, "ForestManager.forestMap field missing");
            forestMapField.SetValue(forestManager, forestMap);

            // 3. EmploymentManager — unemploymentRate=50 → GetEmploymentRate() returns 50.
            employmentGO = new GameObject("ParityTestEmploymentManager");
            employmentManager = employmentGO.AddComponent<EmploymentManager>();
            employmentManager.unemploymentRate = 50f;

            // 4. EconomyManager — defaults already 10/10/10 (comfortable tax band).
            economyGO = new GameObject("ParityTestEconomyManager");
            economyManager = economyGO.AddComponent<EconomyManager>();
            economyManager.residentialIncomeTax = 10;
            economyManager.commercialIncomeTax = 10;
            economyManager.industrialIncomeTax = 10;

            // 5. CityStats — populate counts so legacy + composer paths read identical inputs.
            cityStatsGO = new GameObject("ParityTestCityStats");
            cityStats = cityStatsGO.AddComponent<CityStats>();
            cityStats.simulateGrowth = false;
            cityStats.population = 100;
            cityStats.forestManager = forestManager;
            cityStats.forestCellCount = forestCoords.Length;
            cityStats.residentialZoneCount = 6;
            cityStats.residentialBuildingCount = 6;
            cityStats.commercialZoneCount = 6;
            cityStats.commercialBuildingCount = 6;
            cityStats.industrialZoneCount = industrialCoords.Length;
            cityStats.industrialBuildingCount = industrialCoords.Length;
            // Inject CityStats private deps so ComputeTargetHappiness reads stub managers.
            FieldInfo employmentField = typeof(CityStats).GetField(
                "_employmentManager", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(employmentField, "CityStats._employmentManager field missing");
            employmentField.SetValue(cityStats, employmentManager);
            FieldInfo economyField = typeof(CityStats).GetField(
                "_economyManager", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(economyField, "CityStats._economyManager field missing");
            economyField.SetValue(cityStats, economyManager);

            // 6. SignalFieldRegistry — allocate fields at GRID dims.
            registryGO = new GameObject("ParityTestSignalFieldRegistry");
            registry = registryGO.AddComponent<SignalFieldRegistry>();
            registry.ResizeForMap(GRID, GRID);

            // 7. UrbanCentroidService + DistrictManager — Inner-ring rollup target for composer.
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
            // Re-trigger Awake so DistrictMap allocates with the injected refs.
            MethodInfo dmAwake = typeof(DistrictManager).GetMethod(
                "Awake", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(dmAwake, "DistrictManager.Awake method missing");
            dmAwake.Invoke(districtManager, null);

            // 8. PowerPlant fixture at (15, 15) — bounds-safe center quadrant.
            powerPlantGO = new GameObject("ParityTestPowerPlant");
            powerPlant = powerPlantGO.AddComponent<PowerPlant>();
            // World-pos chosen so GridManager.GetGridPosition rounds back to (15, 15).
            powerPlantGO.transform.position = IsometricGridToWorld(15, 15, grid.tileWidth, grid.tileHeight);

            // 9. HappinessComposer — Inspector-style ref injection.
            composerGO = new GameObject("ParityTestHappinessComposer");
            composer = composerGO.AddComponent<HappinessComposer>();
            InjectField(composer, "cityStats", cityStats);
            InjectField(composer, "employmentManager", employmentManager);
            InjectField(composer, "economyManager", economyManager);
            InjectField(composer, "forestManager", forestManager);
            InjectField(composer, "districtManager", districtManager);

            // 10. Producers — Inspector-style ref injection.
            industrialProducerGO = new GameObject("ParityTestIndustrialProducer");
            industrialProducer = industrialProducerGO.AddComponent<IndustrialPollutionProducer>();
            InjectField(industrialProducer, "gridManager", grid);

            powerPlantProducerGO = new GameObject("ParityTestPowerPlantProducer");
            powerPlantProducer = powerPlantProducerGO.AddComponent<PowerPlantPollutionProducer>();
            InjectField(powerPlantProducer, "gridManager", grid);
            powerPlantProducer.RefreshCache();

            forestSinkGO = new GameObject("ParityTestForestSink");
            forestSink = forestSinkGO.AddComponent<ForestPollutionSink>();
            InjectField(forestSink, "gridManager", grid);
            InjectField(forestSink, "forestManager", forestManager);

            // 11. SignalMetadataRegistry — uniform diffusion entries (12 signals).
            metadataAsset = ScriptableObject.CreateInstance<SignalMetadataRegistry>();
            FieldInfo entriesField = typeof(SignalMetadataRegistry).GetField(
                "entries", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(entriesField, "SignalMetadataRegistry.entries field missing");
            int signalCount = Enum.GetValues(typeof(SimulationSignal)).Length;
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

            // 12. SignalTickScheduler — wire registry + metadata + producer/consumer lists via reflection.
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
                industrialProducer, powerPlantProducer, forestSink,
            });
            schedConsumerList.SetValue(scheduler, new List<MonoBehaviour> { composer });
            // Re-trigger Awake so cached typed lists pick up the producers + consumer.
            MethodInfo schedAwake = typeof(SignalTickScheduler).GetMethod(
                "Awake", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(schedAwake, "SignalTickScheduler.Awake method missing");
            schedAwake.Invoke(scheduler, null);
        }

        [TearDown]
        public void TearDown()
        {
            if (schedulerGO != null) UnityEngine.Object.DestroyImmediate(schedulerGO);
            if (forestSinkGO != null) UnityEngine.Object.DestroyImmediate(forestSinkGO);
            if (powerPlantProducerGO != null) UnityEngine.Object.DestroyImmediate(powerPlantProducerGO);
            if (industrialProducerGO != null) UnityEngine.Object.DestroyImmediate(industrialProducerGO);
            if (composerGO != null) UnityEngine.Object.DestroyImmediate(composerGO);
            if (powerPlantGO != null) UnityEngine.Object.DestroyImmediate(powerPlantGO);
            if (districtGO != null) UnityEngine.Object.DestroyImmediate(districtGO);
            if (centroidGO != null) UnityEngine.Object.DestroyImmediate(centroidGO);
            if (registryGO != null) UnityEngine.Object.DestroyImmediate(registryGO);
            if (cityStatsGO != null) UnityEngine.Object.DestroyImmediate(cityStatsGO);
            if (economyGO != null) UnityEngine.Object.DestroyImmediate(economyGO);
            if (employmentGO != null) UnityEngine.Object.DestroyImmediate(employmentGO);
            if (forestGO != null) UnityEngine.Object.DestroyImmediate(forestGO);
            if (gridGO != null) UnityEngine.Object.DestroyImmediate(gridGO);
            if (metadataAsset != null) UnityEngine.Object.DestroyImmediate(metadataAsset);
        }

        [Test]
        public void CurrentWithinFivePointsOfLegacy()
        {
            // Snapshot legacy ComputeTargetHappiness at fixture state (private — invoke via reflection).
            MethodInfo legacyMethod = typeof(CityStats).GetMethod(
                "ComputeTargetHappiness", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(legacyMethod, "CityStats.ComputeTargetHappiness method missing");
            float legacyTarget = (float)legacyMethod.Invoke(cityStats, null);

            // Warmup the composer over the signal pipeline.
            for (int i = 0; i < WARMUP_TICKS; i++)
            {
                scheduler.Tick(1f);
            }

            float delta = Mathf.Abs(legacyTarget - composer.Current);
            Assert.LessOrEqual(delta, 5f,
                $"HappinessComposer.Current ({composer.Current}) drifted >5 from legacy target ({legacyTarget}) after {WARMUP_TICKS} ticks");
        }

        [Test]
        public void NoNaNOrInfinityOverWarmup()
        {
            for (int i = 0; i < WARMUP_TICKS; i++)
            {
                scheduler.Tick(1f);
                Assert.IsFalse(float.IsNaN(composer.Current),
                    $"HappinessComposer.Current went NaN at tick {i + 1}");
                Assert.IsFalse(float.IsInfinity(composer.Current),
                    $"HappinessComposer.Current went Infinity at tick {i + 1}");
            }
        }

        [Test]
        public void PollutionAirNonNegativeAfterForestSink()
        {
            for (int i = 0; i < WARMUP_TICKS; i++)
            {
                scheduler.Tick(1f);
            }

            SignalField field = registry.GetField(SimulationSignal.PollutionAir);
            Assert.IsNotNull(field, "PollutionAir field missing from registry");
            for (int x = 0; x < field.Width; x++)
            {
                for (int y = 0; y < field.Height; y++)
                {
                    float v = field.Get(x, y);
                    Assert.GreaterOrEqual(v, 0f,
                        $"PollutionAir cell ({x},{y}) went negative ({v}) — floor-clamp invariant broken");
                }
            }
        }

        // --- helpers ---

        private static void InjectField(MonoBehaviour target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(
                name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"{target.GetType().Name}.{name} field missing");
            field.SetValue(target, value);
        }

        /// <summary>
        /// Inverse of <see cref="GridManager.GetGridPosition"/> for the planar isometric mapping.
        /// Returns a world-space position that rounds back to (gridX, gridY) under
        /// <c>IsometricGridMath.WorldToGridPlanar</c>.
        /// </summary>
        private static Vector3 IsometricGridToWorld(int gridX, int gridY, float tileWidth, float tileHeight)
        {
            float worldX = (gridX - gridY) * tileWidth * 0.5f;
            float worldY = (gridX + gridY) * tileHeight * 0.5f;
            return new Vector3(worldX, worldY, 0f);
        }
    }
}
