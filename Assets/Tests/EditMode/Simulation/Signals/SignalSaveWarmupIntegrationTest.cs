using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Territory.Buildings;
using Territory.Core;
using Territory.Economy;
using Territory.Forests;
using Territory.Persistence;
using Territory.Simulation;
using Territory.Simulation.Signals;
using Territory.Simulation.Signals.Producers;
using Territory.Zones;

namespace Territory.Tests.EditMode.Simulation.Signals
{
    /// <summary>
    /// TECH-1793 — integration coverage for Stage 6 save schema (<c>tuningWeights</c>) +
    /// <see cref="SignalWarmupPass"/> determinism across save/load.
    /// Test 1: <see cref="GameSaveData"/> v6 round-trip preserves all 17
    ///   <see cref="SignalTuningWeightsData"/> fields bit-identically through
    ///   <see cref="JsonUtility.ToJson(object)"/> + <see cref="JsonUtility.FromJson{T}(string)"/>.
    /// Test 2: legacy schema-5 saves (no <c>tuningWeights</c> payload) survive migrate as null —
    ///   <see cref="SignalTuningWeightsAsset.RestoreFromData(SignalTuningWeightsData)"/> is a no-op,
    ///   live asset defaults preserved.
    /// Test 3: end-to-end determinism — apply <see cref="SignalWarmupPass.Run"/> across a serialize +
    ///   deserialize + restore cycle, assert byte-identical signal fields across all 12 ordinals.
    /// </summary>
    [TestFixture]
    public class SignalSaveWarmupIntegrationTest
    {
        private const int GRID = 20;
        private const int WARMUP_TICKS = 5;

        // Fixture refs (Test 3 only — built lazily inside the test).
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
        private SignalTuningWeightsAsset weightsAsset;

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
            if (weightsAsset != null) UnityEngine.Object.DestroyImmediate(weightsAsset);
        }

        [Test]
        public void V6RoundTripPreservesTuningWeights()
        {
            SignalTuningWeightsData payload = new SignalTuningWeightsData
            {
                happinessBaseline = 55f,
                weightEmployment = 42f,
                weightServices = 19f,
                weightForest = 11f,
                weightPollution = 13f,
                weightTax = 28f,
                weightDev = 14f,
                baseConvergenceRate = 0.17f,
                populationScaleFactor = 480f,
                comfortableTaxRate = 9f,
                maxTaxRateForScale = 51f,
                pollutionCap = 250f,
                maxForestBonus = 65f,
                serviceCoverageStub = 0.45f,
                parksBonus = 0.7f,
                pollutionPenalty = 0.55f,
                normalizationCap = 110f,
            };

            GameSaveData source = NewMinimalSchema6Payload();
            source.tuningWeights = payload;

            string json = JsonUtility.ToJson(source);
            GameSaveData restored = JsonUtility.FromJson<GameSaveData>(json);
            InvokeMigrate(restored);

            Assert.AreEqual(GameSaveData.CurrentSchemaVersion, restored.schemaVersion, "schemaVersion not preserved at current version");
            Assert.IsNotNull(restored.tuningWeights, "tuningWeights dropped during round-trip");

            // Iterate all fields reflectively — fail-fast on drift. Stage 7 (TECH-1889/1890/1891/1892)
            // adds 11 fields (3 PollutionLand + 3 PollutionWater + 4 LandValue + 1 income multiplier) → 28 total.
            // Stage 8 (TECH-1953) adds 5 CrimeSystem fields (crimeBase, crimeDensityWeight,
            // servicePoliceCoverage, servicePoliceConsumerScale, crimeHotspotThreshold) → 33 total.
            // Stage 9.A (TECH-2079) adds 6 service-tuning fields (serviceFire/Education/Health
            // Coverage + ConsumerScale pairs) → 39 total.
            // Stage 9.B (TECH-2136) adds 3 traffic-tuning fields (trafficBase,
            // trafficRoadwayDensityWeight, trafficLevelConsumerScale) → 42 total.
            FieldInfo[] fields = typeof(SignalTuningWeightsData).GetFields(BindingFlags.Public | BindingFlags.Instance);
            Assert.AreEqual(42, fields.Length, "SignalTuningWeightsData field count drift — expected 42 (Stage 6 17 + Stage 7 11 + Stage 8 5 + Stage 9.A 6 + Stage 9.B 3)");
            for (int i = 0; i < fields.Length; i++)
            {
                float a = (float)fields[i].GetValue(payload);
                float b = (float)fields[i].GetValue(restored.tuningWeights);
                Assert.AreEqual(a, b, $"field {fields[i].Name} drifted: source={a} restored={b}");
            }
        }

        [Test]
        public void V5SaveBackwardCompatPreservesAssetDefaults()
        {
            // Legacy schema-5 payload — tuningWeights field absent / null on disk.
            GameSaveData legacy = NewMinimalSchema6Payload();
            legacy.schemaVersion = 5;
            legacy.tuningWeights = null;

            InvokeMigrate(legacy);

            Assert.AreEqual(GameSaveData.CurrentSchemaVersion, legacy.schemaVersion, "schemaVersion not bumped to current after migrate");
            Assert.IsNull(legacy.tuningWeights, "schema-5 → 6 migration must leave null tuningWeights untouched (LoadGame preserves live asset defaults)");

            // Confirm RestoreFromData is a no-op on null — live asset defaults must survive.
            SignalTuningWeightsAsset live = ScriptableObject.CreateInstance<SignalTuningWeightsAsset>();
            try
            {
                float baselineBefore = live.HappinessBaseline;
                float pollutionCapBefore = live.PollutionCap;
                float parksBefore = live.ParksBonus;

                live.RestoreFromData(legacy.tuningWeights);

                Assert.AreEqual(baselineBefore, live.HappinessBaseline, "HappinessBaseline mutated by null restore");
                Assert.AreEqual(pollutionCapBefore, live.PollutionCap, "PollutionCap mutated by null restore");
                Assert.AreEqual(parksBefore, live.ParksBonus, "ParksBonus mutated by null restore");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(live);
            }
        }

        [Test]
        public void WarmupRunDeterminismAcrossSaveLoad()
        {
            BuildHeavyFixture();

            // Inject non-default tuning into the live asset so the round-trip carries real bits.
            SetWeightsField(weightsAsset, "happinessBaseline", 55f);
            SetWeightsField(weightsAsset, "weightEmployment", 42f);
            SetWeightsField(weightsAsset, "pollutionCap", 250f);
            SetWeightsField(weightsAsset, "parksBonus", 0.7f);

            // Pre-warmup snapshot of live asset state (post-injection).
            SignalTuningWeightsData preTuningSnapshot = weightsAsset.CaptureSnapshot();

            // Run A — initial warmup; capture per-signal field snapshots.
            SignalWarmupPass.Run(registry, districtManager, scheduler, WARMUP_TICKS);
            int signalCount = Enum.GetValues(typeof(SimulationSignal)).Length;
            float[][,] snapshotsA = new float[signalCount][,];
            for (int s = 0; s < signalCount; s++)
            {
                SignalField field = registry.GetField((SimulationSignal)s);
                Assert.IsNotNull(field, $"signal field {s} missing after Run A");
                snapshotsA[s] = field.Snapshot();
            }

            // Serialize tuningWeights through GameSaveData round-trip + migrate.
            GameSaveData saveData = NewMinimalSchema6Payload();
            saveData.tuningWeights = weightsAsset.CaptureSnapshot();
            string json = JsonUtility.ToJson(saveData);
            GameSaveData restored = JsonUtility.FromJson<GameSaveData>(json);
            InvokeMigrate(restored);

            // Tear + rebuild fixture so producer / registry state is fresh.
            TearDown();
            BuildHeavyFixture();

            // Restore the round-tripped tuning into the freshly-built asset.
            weightsAsset.RestoreFromData(restored.tuningWeights);

            // Sanity — restored asset bits must equal pre-snapshot bits across all 17 fields.
            SignalTuningWeightsData postTuningSnapshot = weightsAsset.CaptureSnapshot();
            FieldInfo[] tuningFields = typeof(SignalTuningWeightsData).GetFields(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < tuningFields.Length; i++)
            {
                float a = (float)tuningFields[i].GetValue(preTuningSnapshot);
                float b = (float)tuningFields[i].GetValue(postTuningSnapshot);
                Assert.AreEqual(a, b, $"tuning field {tuningFields[i].Name} drifted across round-trip: pre={a} post={b}");
            }

            // Run B — same warmup, restored asset, fresh registry.
            SignalWarmupPass.Run(registry, districtManager, scheduler, WARMUP_TICKS);
            for (int s = 0; s < signalCount; s++)
            {
                SignalField field = registry.GetField((SimulationSignal)s);
                Assert.IsNotNull(field, $"signal field {s} missing after Run B");
                float[,] b = field.Snapshot();
                float[,] a = snapshotsA[s];
                Assert.AreEqual(a.GetLength(0), b.GetLength(0), $"signal {s} width drift");
                Assert.AreEqual(a.GetLength(1), b.GetLength(1), $"signal {s} height drift");
                for (int x = 0; x < a.GetLength(0); x++)
                {
                    for (int y = 0; y < a.GetLength(1); y++)
                    {
                        Assert.AreEqual(a[x, y], b[x, y],
                            $"signal {s} cell ({x},{y}) diverged across save/load: A={a[x, y]} B={b[x, y]}");
                    }
                }
            }
        }

        // --- helpers ---

        private static GameSaveData NewMinimalSchema6Payload()
        {
            return new GameSaveData
            {
                schemaVersion = GameSaveData.CurrentSchemaVersion,
                regionId = Guid.NewGuid().ToString(),
                countryId = Guid.NewGuid().ToString(),
            };
        }

        private static void InvokeMigrate(GameSaveData data)
        {
            MethodInfo migrate = typeof(GameSaveManager).GetMethod(
                "MigrateLoadedSaveData",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(migrate, "GameSaveManager.MigrateLoadedSaveData missing");
            migrate.Invoke(null, new object[] { data });
        }

        private static void InjectField(MonoBehaviour target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(
                name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"{target.GetType().Name}.{name} field missing");
            field.SetValue(target, value);
        }

        private static void SetWeightsField(SignalTuningWeightsAsset asset, string name, float value)
        {
            FieldInfo field = typeof(SignalTuningWeightsAsset).GetField(
                name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"SignalTuningWeightsAsset.{name} field missing");
            field.SetValue(asset, value);
        }

        private static Vector3 IsometricGridToWorld(int gridX, int gridY, float tileWidth, float tileHeight)
        {
            float worldX = (gridX - gridY) * tileWidth * 0.5f;
            float worldY = (gridX + gridY) * tileHeight * 0.5f;
            return new Vector3(worldX, worldY, 0f);
        }

        /// <summary>
        /// Build the canonical 20×20 fixture (mirrors <c>SignalWarmupPassIdempotencyTest.SetUp</c>):
        /// grid + forest + employment + economy + city stats + registry + centroid + districts +
        /// power plant + tuning asset + composer + 3 producers + metadata + scheduler.
        /// </summary>
        private void BuildHeavyFixture()
        {
            // 1. GridManager.
            gridGO = new GameObject("SaveWarmupTestGridManager");
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

            // 2. ForestManager.
            forestGO = new GameObject("SaveWarmupTestForestManager");
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

            // 3. EmploymentManager.
            employmentGO = new GameObject("SaveWarmupTestEmploymentManager");
            employmentManager = employmentGO.AddComponent<EmploymentManager>();
            employmentManager.unemploymentRate = 50f;

            // 4. EconomyManager.
            economyGO = new GameObject("SaveWarmupTestEconomyManager");
            economyManager = economyGO.AddComponent<EconomyManager>();
            economyManager.residentialIncomeTax = 10;
            economyManager.commercialIncomeTax = 10;
            economyManager.industrialIncomeTax = 10;

            // 5. CityStats.
            cityStatsGO = new GameObject("SaveWarmupTestCityStats");
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
            FieldInfo employmentField = typeof(CityStats).GetField(
                "_employmentManager", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(employmentField, "CityStats._employmentManager field missing");
            employmentField.SetValue(cityStats, employmentManager);
            FieldInfo economyField = typeof(CityStats).GetField(
                "_economyManager", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(economyField, "CityStats._economyManager field missing");
            economyField.SetValue(cityStats, economyManager);

            // 6. SignalFieldRegistry.
            registryGO = new GameObject("SaveWarmupTestSignalFieldRegistry");
            registry = registryGO.AddComponent<SignalFieldRegistry>();
            registry.ResizeForMap(GRID, GRID);

            // 7. UrbanCentroidService + DistrictManager.
            centroidGO = new GameObject("SaveWarmupTestUrbanCentroidService");
            centroid = centroidGO.AddComponent<UrbanCentroidService>();
            centroid.gridManager = grid;
            centroid.RecalculateFromGrid();

            districtGO = new GameObject("SaveWarmupTestDistrictManager");
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

            // 8. PowerPlant fixture.
            powerPlantGO = new GameObject("SaveWarmupTestPowerPlant");
            powerPlant = powerPlantGO.AddComponent<PowerPlant>();
            powerPlantGO.transform.position = IsometricGridToWorld(15, 15, grid.tileWidth, grid.tileHeight);

            // 9. SignalTuningWeightsAsset.
            weightsAsset = ScriptableObject.CreateInstance<SignalTuningWeightsAsset>();

            // 10. HappinessComposer.
            composerGO = new GameObject("SaveWarmupTestHappinessComposer");
            composer = composerGO.AddComponent<HappinessComposer>();
            InjectField(composer, "cityStats", cityStats);
            InjectField(composer, "employmentManager", employmentManager);
            InjectField(composer, "economyManager", economyManager);
            InjectField(composer, "forestManager", forestManager);
            InjectField(composer, "districtManager", districtManager);
            InjectField(composer, "weights", weightsAsset);
            MethodInfo composerAwake = typeof(HappinessComposer).GetMethod(
                "Awake", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(composerAwake, "HappinessComposer.Awake method missing");
            composerAwake.Invoke(composer, null);

            // 11. Producers.
            industrialProducerGO = new GameObject("SaveWarmupTestIndustrialProducer");
            industrialProducer = industrialProducerGO.AddComponent<IndustrialPollutionProducer>();
            InjectField(industrialProducer, "gridManager", grid);

            powerPlantProducerGO = new GameObject("SaveWarmupTestPowerPlantProducer");
            powerPlantProducer = powerPlantProducerGO.AddComponent<PowerPlantPollutionProducer>();
            InjectField(powerPlantProducer, "gridManager", grid);
            powerPlantProducer.RefreshCache();

            forestSinkGO = new GameObject("SaveWarmupTestForestSink");
            forestSink = forestSinkGO.AddComponent<ForestPollutionSink>();
            InjectField(forestSink, "gridManager", grid);
            InjectField(forestSink, "forestManager", forestManager);

            // 12. SignalMetadataRegistry.
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

            // 13. SignalTickScheduler.
            schedulerGO = new GameObject("SaveWarmupTestSignalTickScheduler");
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
            MethodInfo schedAwake = typeof(SignalTickScheduler).GetMethod(
                "Awake", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(schedAwake, "SignalTickScheduler.Awake method missing");
            schedAwake.Invoke(scheduler, null);
        }
    }
}
