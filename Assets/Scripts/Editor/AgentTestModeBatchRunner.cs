#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using System.Text;
using Territory.Core;
using Territory.Economy;
using Territory.Persistence;
using Territory.Simulation;
using Territory.Terrain;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Territory.Testing
{
    /// <summary>
    /// <b>Editor</b> <c>-batchmode</c> entry. Flow: open <see cref="CityScenePath"/> → enter <b>Play Mode</b> →
    /// resolve <b>test mode</b> scenario args via <see cref="TestModeCommandLineBootstrap.TryParse"/> →
    /// load through <see cref="GameSaveManager.LoadGame"/> only → optionally run bounded <see cref="SimulationManager.ProcessSimulationTick"/> →
    /// write <c>tools/reports/agent-testmode-batch-*.json</c> → exit via <see cref="EditorApplication.Exit"/>.
    /// State persisted in <c>tools/reports/.agent-testmode-batch-state.json</c> (gitignored) across Play Mode domain reloads;
    /// <see cref="SessionState"/> unreliable in <c>-batchmode</c>.
    /// </summary>
    public static class AgentTestModeBatchRunner
    {
        public const string ExecuteMethodName = "Territory.Testing.AgentTestModeBatchRunner.Run";

        /// <summary>Same scenario flags as <see cref="TestModeCommandLineBootstrap"/>.</summary>
        public const string ArgSimulationTicks = "-testSimulationTicks";

        /// <summary>Optional committed JSON of <see cref="AgentTestModeBatchCitySnapshotDto"/>. Mismatch → exit 8.</summary>
        public const string ArgGoldenPath = "-testGoldenPath";

        /// <summary>
        /// When present, skip <see cref="GameSaveManager.LoadGame"/> and call <see cref="GameSaveManager.NewGame"/> +
        /// scripted interstate build instead. Gate for neighbor-stub smoke.
        /// </summary>
        public const string ArgNewGame = "-testNewGame";

        /// <summary>
        /// Optional integer master seed passed alongside <see cref="ArgNewGame"/>.
        /// When provided, calls <see cref="Territory.Persistence.MapGenerationSeed.SetSessionMasterSeed"/> before
        /// <see cref="GameSaveManager.NewGame"/> so the run is deterministic.
        /// </summary>
        public const string ArgTestSeed = "-testSeed";

        public const int ExitCodeGoldenMismatch = 8;

        /// <summary>
        /// Exit code emitted when <see cref="HeightIntegritySweep"/> finds at least one
        /// <c>HeightMap[x,y] != CityCell.height</c> violation (invariant #1 breach).
        /// Distinct from golden mismatch (<c>8</c>) so agents can branch on failure class.
        /// </summary>
        public const int ExitCodeHeightIntegrityFail = 9;

        public const string CityScenePath = "Assets/Scenes/CityScene.unity";

        /// <summary>Transient state file under <c>tools/reports/</c>; dotfile, gitignored by <c>tools/reports/**</c>.</summary>
        public const string StateFileName = ".agent-testmode-batch-state.json";

        const double GridWaitMaxSeconds = 120.0;
        const double ExitPlayWaitMaxSeconds = 90.0;

        enum BatchPhase
        {
            WaitGrid = 1,
            WaitStopped = 2
        }

        /// <summary>
        /// Integer <b>CityStats</b> slice for golden checks. Stable across platforms; no floats.
        /// Serialized → committed JSON under <c>tools/fixtures/scenarios/…</c>.
        /// schema_version 2 adds <c>regionId</c> / <c>countryId</c> GUID strings.
        /// </summary>
        [Serializable]
        public class AgentTestModeBatchCitySnapshotDto
        {
            public int schema_version = 2;
            public int simulation_ticks;
            public int population;
            public int money;
            public int roadCount;
            public int grassCount;
            public int residentialZoneCount;
            public int commercialZoneCount;
            public int industrialZoneCount;
            public int residentialBuildingCount;
            public int commercialBuildingCount;
            public int industrialBuildingCount;
            public int forestCellCount;
            public string regionId = "";
            public string countryId = "";
        }

        /// <summary>
        /// Neighbor-stub round-trip golden DTO. Sibling file convention:
        /// filename contains <c>neighbor-stubs</c> → dispatched to <see cref="TryCompareNeighborStubGolden"/>.
        /// <c>schema_version</c> 1 is the only defined version.
        /// Entries are sort-stable before compare: stubs by <c>id</c>, bindings by <c>(stubId, exitCellX, exitCellY)</c>.
        /// </summary>
        [Serializable]
        public class NeighborStubRoundtripGoldenDto
        {
            public int schema_version = 1;
            public NeighborStubGoldenEntry[] neighborStubs = System.Array.Empty<NeighborStubGoldenEntry>();
            public NeighborBindingGoldenEntry[] neighborCityBindings = System.Array.Empty<NeighborBindingGoldenEntry>();
        }

        /// <summary>One neighbor stub entry inside <see cref="NeighborStubRoundtripGoldenDto"/>.</summary>
        [Serializable]
        public class NeighborStubGoldenEntry
        {
            public string id = "";
            public string displayName = "";
            public string borderSide = "";
        }

        /// <summary>One binding entry inside <see cref="NeighborStubRoundtripGoldenDto"/>.</summary>
        [Serializable]
        public class NeighborBindingGoldenEntry
        {
            public string stubId = "";
            public int exitCellX;
            public int exitCellY;
            public string borderSide = "";
        }

        /// <summary>Single <c>HeightMap[x,y] != CityCell.height</c> mismatch row (first 10 only).</summary>
        [Serializable]
        public class HeightIntegrityViolationDto
        {
            public int x;
            public int y;
            public int heightMap;
            public int cell;
        }

        /// <summary>
        /// Result of one <see cref="HeightIntegritySweep"/> pass (post-load or post-tick).
        /// <c>violations == 0</c> means invariant #1 is satisfied for this pass.
        /// </summary>
        [Serializable]
        public class HeightIntegritySweepResultDto
        {
            public int checked_cells;
            public int violations;
            public HeightIntegrityViolationDto[] first_offenders = System.Array.Empty<HeightIntegrityViolationDto>();
        }

        /// <summary>
        /// Combined <c>height_integrity</c> object emitted in the batch report.
        /// Contains one result per sweep pass (post-load, post-tick).
        /// </summary>
        [Serializable]
        public class HeightIntegrityDto
        {
            public HeightIntegritySweepResultDto post_load;
            public HeightIntegritySweepResultDto post_tick;
        }

        /// <summary>
        /// Neighbor-stub smoke assertions for new-game runs. Emitted in report as <c>neighbor_stub_smoke</c>.
        /// <c>resolver_matches == binding_count</c> means every binding resolved to a non-null stub via
        /// <see cref="GridManager.GetNeighborStub"/>.
        /// </summary>
        [Serializable]
        public class NeighborStubSmokeResultDto
        {
            public int stub_count;
            public int binding_count;
            public int resolver_matches;
            /// <summary>True when stub_count >= 1, binding_count >= 1, resolver_matches == binding_count.</summary>
            public bool assertions_passed;
            public string failure_detail = "";
        }

        [Serializable]
        class AgentTestModeBatchStateDto
        {
            public int phase;
            public string save_path = "";
            public string scenario_id = "";
            public string golden_path = "";
            public string city_stats_snapshot_json = "";
            public bool golden_checked;
            public bool golden_matched;
            public string golden_diff = "";
            public int ticks_requested;
            public int ticks_applied;
            public int exit_code;
            public string error = "";
            public string started_utc = "";
            /// <summary>JSON of <see cref="HeightIntegrityDto"/>; empty until sweep runs.</summary>
            public string height_integrity_json = "";
            /// <summary>JSON of <see cref="NeighborStubRoundtripGoldenDto"/> projected from actual state; empty when golden is city-stats type.</summary>
            public string neighbor_stubs_snapshot_json = "";
            /// <summary>True when run was started with <see cref="ArgNewGame"/> (skip LoadGame, call NewGame instead).</summary>
            public bool new_game_mode;
            /// <summary>
            /// Optional master seed for deterministic new-game smoke. 0 = not pinned (NewGame rolls its own seed).
            /// Populated from <see cref="ArgTestSeed"/> when present alongside <see cref="ArgNewGame"/>.
            /// </summary>
            public int test_seed;
            /// <summary>JSON of <see cref="NeighborStubSmokeResultDto"/>; empty on load-path runs.</summary>
            public string neighbor_stub_smoke_json = "";
        }

        [Serializable]
        class AgentTestModeBatchReportDto
        {
            public int schema_version = 2;
            public string kind = "agent-testmode-batch";
            /// <summary>"load" (default) or "new-game" (when <see cref="ArgNewGame"/> was passed).</summary>
            public string mode = "load";
            public bool ok;
            public string scenario_id = "";
            public string save_path = "";
            public string golden_path = "";
            public bool golden_checked;
            public bool golden_matched;
            public string golden_diff = "";
            public AgentTestModeBatchCitySnapshotDto city_stats;
            /// <summary>
            /// Height integrity sweep results — post-load and post-tick passes.
            /// <c>null</c> when sweep could not run (grid/HeightMap unavailable).
            /// </summary>
            public HeightIntegrityDto height_integrity;
            /// <summary>
            /// Neighbor stubs observed post-load. Populated for any run (stub or city-stats golden path).
            /// Null when <see cref="GameSaveManager"/> was not found.
            /// </summary>
            public NeighborStubGoldenEntry[] neighbor_stubs;
            /// <summary>
            /// Neighbor city bindings observed post-load.
            /// </summary>
            public NeighborBindingGoldenEntry[] neighbor_city_bindings;
            /// <summary>
            /// New-game smoke assertions (stub seeding + binding + resolver). Non-null on new-game runs only.
            /// </summary>
            public NeighborStubSmokeResultDto neighbor_stub_smoke;
            public int simulation_ticks_requested;
            public int simulation_ticks_applied;
            public int exit_code;
            public string error = "";
            public string finished_at_utc = "";
        }

        static string GetStateFilePath()
        {
            string repoRoot = ScenarioPathResolver.GetRepositoryRoot();
            return Path.Combine(repoRoot, "tools", "reports", StateFileName);
        }

        static bool TryReadState(out AgentTestModeBatchStateDto state)
        {
            state = null;
            string path = GetStateFilePath();
            if (!File.Exists(path))
                return false;
            try
            {
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                    return false;
                var dto = JsonUtility.FromJson<AgentTestModeBatchStateDto>(json);
                if (dto == null || dto.phase == 0)
                    return false;
                state = dto;
                return true;
            }
            catch
            {
                return false;
            }
        }

        static void WriteState(AgentTestModeBatchStateDto state)
        {
            string path = GetStateFilePath();
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(path, JsonUtility.ToJson(state, prettyPrint: false));
        }

        static void DeleteStateFile()
        {
            try
            {
                string path = GetStateFilePath();
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // Best-effort cleanup.
            }
        }

        [InitializeOnLoadMethod]
        static void RegisterAfterDomainReload()
        {
            if (!File.Exists(GetStateFilePath()))
                return;
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }

        /// <summary>Unity <c>-executeMethod Territory.Testing.AgentTestModeBatchRunner.Run</c>.</summary>
        public static void Run()
        {
            if (!TestModeSecurity.IsTestModeEntryAllowed)
            {
                FailImmediate(4, "Test mode is not allowed in this build.");
                return;
            }

            string[] args = Environment.GetCommandLineArgs();
            bool newGameMode = ParseNewGameFlag(args);
            int testSeed = ParseTestSeed(args);

            // New-game mode: no save file required. Use scenario id for report label only (optional).
            if (newGameMode)
            {
                // Scenario id is optional on new-game path — use empty string when not supplied.
                TestModeCommandLineBootstrap.TryParse(args, out string ngScenarioId, out _);
                int ticksRequestedNg = ParseSimulationTicks(args);
                string goldenPathNg = ParseTestGoldenPath(args);
                if (!string.IsNullOrEmpty(goldenPathNg))
                {
                    goldenPathNg = Path.GetFullPath(goldenPathNg);
                    if (!File.Exists(goldenPathNg))
                    {
                        FailImmediate(4, $"Golden file not found: {goldenPathNg}");
                        return;
                    }
                }
                try
                {
                    EditorSceneManager.OpenScene(CityScenePath, OpenSceneMode.Single);
                }
                catch (Exception ex)
                {
                    FailImmediate(4, $"Could not open CityScene: {ex.Message}");
                    return;
                }
                var ngState = new AgentTestModeBatchStateDto
                {
                    phase = (int)BatchPhase.WaitGrid,
                    save_path = string.Empty,
                    scenario_id = ngScenarioId ?? string.Empty,
                    golden_path = goldenPathNg ?? string.Empty,
                    ticks_requested = ticksRequestedNg,
                    ticks_applied = 0,
                    exit_code = 0,
                    error = string.Empty,
                    started_utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                    new_game_mode = true,
                    test_seed = testSeed
                };
                WriteState(ngState);
                Debug.Log("[AgentTestModeBatch] New-game mode: state written; entering Play Mode.");
                EditorApplication.EnterPlaymode();
                return;
            }

            if (!TestModeCommandLineBootstrap.TryParse(args, out string scenarioId, out string savePath))
            {
                FailImmediate(4, "Missing or invalid -testScenarioId / -testScenarioPath.");
                return;
            }

            if (string.IsNullOrEmpty(savePath) || !File.Exists(savePath))
            {
                FailImmediate(4, $"Save file not found: {savePath}");
                return;
            }

            int ticksRequested = ParseSimulationTicks(args);
            string goldenPath = ParseTestGoldenPath(args);
            if (!string.IsNullOrEmpty(goldenPath))
            {
                goldenPath = Path.GetFullPath(goldenPath);
                if (!File.Exists(goldenPath))
                {
                    FailImmediate(4, $"Golden file not found: {goldenPath}");
                    return;
                }
            }

            try
            {
                EditorSceneManager.OpenScene(CityScenePath, OpenSceneMode.Single);
            }
            catch (Exception ex)
            {
                FailImmediate(4, $"Could not open CityScene: {ex.Message}");
                return;
            }

            var state = new AgentTestModeBatchStateDto
            {
                phase = (int)BatchPhase.WaitGrid,
                save_path = savePath,
                scenario_id = scenarioId ?? string.Empty,
                golden_path = goldenPath ?? string.Empty,
                ticks_requested = ticksRequested,
                ticks_applied = 0,
                exit_code = 0,
                error = string.Empty,
                started_utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
            };
            WriteState(state);

            Debug.Log("[AgentTestModeBatch] State written; entering Play Mode for load.");
            // Do not register EditorApplication.update here — EnterPlaymode domain reload clears it.
            // InitializeOnLoadMethod re-hooks from the state file after reload.
            EditorApplication.EnterPlaymode();
        }

        static int ParseSimulationTicks(string[] args)
        {
            if (args == null || args.Length == 0)
                return 0;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == ArgSimulationTicks && i + 1 < args.Length)
                {
                    if (int.TryParse(args[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int n))
                        return Mathf.Clamp(n, 0, 10_000);
                }
            }

            return 0;
        }

        static string ParseTestGoldenPath(string[] args)
        {
            if (args == null || args.Length == 0)
                return null;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == ArgGoldenPath && i + 1 < args.Length)
                    return args[i + 1]?.Trim();
            }

            return null;
        }

        /// <summary>Returns true when <see cref="ArgNewGame"/> is present on the command line.</summary>
        static bool ParseNewGameFlag(string[] args)
        {
            if (args == null) return false;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == ArgNewGame)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Parses optional <see cref="ArgTestSeed"/> integer. Returns 0 when absent (caller interprets as "not pinned").
        /// </summary>
        static int ParseTestSeed(string[] args)
        {
            if (args == null) return 0;
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == ArgTestSeed)
                {
                    if (int.TryParse(args[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int seed))
                        return seed;
                }
            }
            return 0;
        }

        static AgentTestModeBatchCitySnapshotDto BuildCitySnapshot(CityStats cityStats, int ticksApplied, GridManager grid)
        {
            if (cityStats == null)
                return null;
            return new AgentTestModeBatchCitySnapshotDto
            {
                schema_version = 2,
                simulation_ticks = ticksApplied,
                population = cityStats.population,
                money = cityStats.money,
                roadCount = cityStats.roadCount,
                grassCount = cityStats.grassCount,
                residentialZoneCount = cityStats.residentialZoneCount,
                commercialZoneCount = cityStats.commercialZoneCount,
                industrialZoneCount = cityStats.industrialZoneCount,
                residentialBuildingCount = cityStats.residentialBuildingCount,
                commercialBuildingCount = cityStats.commercialBuildingCount,
                industrialBuildingCount = cityStats.industrialBuildingCount,
                forestCellCount = cityStats.forestCellCount,
                regionId = grid != null ? (grid.ParentRegionId ?? "") : "",
                countryId = grid != null ? (grid.ParentCountryId ?? "") : ""
            };
        }

        /// <summary>
        /// Tolerant id comparison: golden sentinel <c>"&lt;guid&gt;"</c> accepts any valid GUID string (format D).
        /// Plain equality used for all other values (including exact GUIDs in seeded fixtures).
        /// </summary>
        static bool IdMatches(string goldenValue, string runtimeValue)
            => goldenValue == "<guid>"
                ? Guid.TryParseExact(runtimeValue, "D", out _)
                : goldenValue == runtimeValue;

        static bool TryCompareGolden(string goldenPath, AgentTestModeBatchCitySnapshotDto actual, int ticksRequested, out string diff)
        {
            diff = null;
            if (string.IsNullOrEmpty(goldenPath) || actual == null)
                return true;

            string json;
            try
            {
                json = File.ReadAllText(goldenPath);
            }
            catch (Exception ex)
            {
                diff = $"Could not read golden file: {ex.Message}";
                return false;
            }

            AgentTestModeBatchCitySnapshotDto expected = JsonUtility.FromJson<AgentTestModeBatchCitySnapshotDto>(json);
            if (expected == null)
            {
                diff = "Golden JSON deserialized to null.";
                return false;
            }

            if (expected.schema_version != 1 && expected.schema_version != 2)
            {
                diff = $"Unsupported golden schema_version {expected.schema_version} (expected 1 or 2).";
                return false;
            }

            if (expected.simulation_ticks != ticksRequested)
            {
                diff = $"Golden simulation_ticks {expected.simulation_ticks} does not match requested ticks {ticksRequested}.";
                return false;
            }

            var sb = new StringBuilder();
            void Cmp(string name, int a, int b)
            {
                if (a != b)
                    sb.AppendLine($"{name}: golden={a} actual={b}");
            }

            Cmp(nameof(actual.population), expected.population, actual.population);
            Cmp(nameof(actual.money), expected.money, actual.money);
            Cmp(nameof(actual.roadCount), expected.roadCount, actual.roadCount);
            Cmp(nameof(actual.grassCount), expected.grassCount, actual.grassCount);
            Cmp(nameof(actual.residentialZoneCount), expected.residentialZoneCount, actual.residentialZoneCount);
            Cmp(nameof(actual.commercialZoneCount), expected.commercialZoneCount, actual.commercialZoneCount);
            Cmp(nameof(actual.industrialZoneCount), expected.industrialZoneCount, actual.industrialZoneCount);
            Cmp(nameof(actual.residentialBuildingCount), expected.residentialBuildingCount, actual.residentialBuildingCount);
            Cmp(nameof(actual.commercialBuildingCount), expected.commercialBuildingCount, actual.commercialBuildingCount);
            Cmp(nameof(actual.industrialBuildingCount), expected.industrialBuildingCount, actual.industrialBuildingCount);
            Cmp(nameof(actual.forestCellCount), expected.forestCellCount, actual.forestCellCount);

            // schema_version 2: tolerant id comparison (sentinel "<guid>" or exact GUID string).
            if (expected.schema_version >= 2)
            {
                if (!IdMatches(expected.regionId ?? "", actual.regionId ?? ""))
                    sb.AppendLine($"regionId: golden={expected.regionId} actual={actual.regionId}");
                if (!IdMatches(expected.countryId ?? "", actual.countryId ?? ""))
                    sb.AppendLine($"countryId: golden={expected.countryId} actual={actual.countryId}");
            }

            if (sb.Length > 0)
            {
                diff = sb.ToString().TrimEnd();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns true when the golden file's basename (without extension) contains
        /// <c>neighbor-stubs</c> — dispatches to <see cref="TryCompareNeighborStubGolden"/>
        /// rather than the city-stats compare path.
        /// </summary>
        static bool IsNeighborStubGolden(string goldenPath)
        {
            if (string.IsNullOrEmpty(goldenPath))
                return false;
            string stem = Path.GetFileNameWithoutExtension(goldenPath);
            return stem.IndexOf("neighbor-stubs", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Project <paramref name="saveManager"/> in-memory stubs + bindings to the
        /// <see cref="NeighborStubRoundtripGoldenDto"/> shape (sorted; deterministic).
        /// Returns null when <paramref name="saveManager"/> is null.
        /// </summary>
        static NeighborStubRoundtripGoldenDto BuildNeighborStubSnapshot(GameSaveManager saveManager)
        {
            if (saveManager == null)
                return null;

            var stubs = new System.Collections.Generic.List<NeighborStubGoldenEntry>();
            foreach (var s in saveManager.NeighborStubs)
                stubs.Add(new NeighborStubGoldenEntry
                {
                    id = s.id ?? "",
                    displayName = s.displayName ?? "",
                    borderSide = s.borderSide.ToString()
                });
            stubs.Sort((a, b) => string.Compare(a.id, b.id, StringComparison.Ordinal));

            var bindings = new System.Collections.Generic.List<NeighborBindingGoldenEntry>();
            if (saveManager.neighborCityBindings != null)
            {
                foreach (var b in saveManager.neighborCityBindings)
                {
                    // Resolve borderSide string from matching stub (stubId → stub.borderSide).
                    string bSide = "";
                    foreach (var s in saveManager.NeighborStubs)
                    {
                        if (s.id == b.stubId)
                        {
                            bSide = s.borderSide.ToString();
                            break;
                        }
                    }

                    bindings.Add(new NeighborBindingGoldenEntry
                    {
                        stubId = b.stubId ?? "",
                        exitCellX = b.exitCellX,
                        exitCellY = b.exitCellY,
                        borderSide = bSide
                    });
                }
            }

            bindings.Sort((a, b) =>
            {
                int c = string.Compare(a.stubId, b.stubId, StringComparison.Ordinal);
                if (c != 0) return c;
                c = a.exitCellX.CompareTo(b.exitCellX);
                if (c != 0) return c;
                return a.exitCellY.CompareTo(b.exitCellY);
            });

            return new NeighborStubRoundtripGoldenDto
            {
                schema_version = 1,
                neighborStubs = stubs.ToArray(),
                neighborCityBindings = bindings.ToArray()
            };
        }

        /// <summary>
        /// Compare <paramref name="actual"/> (projected from in-memory state) against the committed
        /// golden JSON at <paramref name="goldenPath"/>. Returns true on match; false + diff string on mismatch.
        /// Stubs sorted by <c>id</c>; bindings by <c>(stubId, exitCellX, exitCellY)</c> before compare.
        /// </summary>
        static bool TryCompareNeighborStubGolden(string goldenPath, NeighborStubRoundtripGoldenDto actual, out string diff)
        {
            diff = null;
            if (string.IsNullOrEmpty(goldenPath) || actual == null)
                return true;

            string json;
            try
            {
                json = File.ReadAllText(goldenPath);
            }
            catch (Exception ex)
            {
                diff = $"Could not read neighbor-stub golden file: {ex.Message}";
                return false;
            }

            NeighborStubRoundtripGoldenDto expected = JsonUtility.FromJson<NeighborStubRoundtripGoldenDto>(json);
            if (expected == null)
            {
                diff = "Neighbor-stub golden JSON deserialized to null.";
                return false;
            }

            if (expected.schema_version != 1)
            {
                diff = $"Unsupported neighbor-stub golden schema_version {expected.schema_version} (expected 1).";
                return false;
            }

            // Sort expected entries the same way to make order-agnostic compare possible.
            var expStubs = new System.Collections.Generic.List<NeighborStubGoldenEntry>(expected.neighborStubs ?? System.Array.Empty<NeighborStubGoldenEntry>());
            expStubs.Sort((a, b) => string.Compare(a.id, b.id, StringComparison.Ordinal));
            var expBindings = new System.Collections.Generic.List<NeighborBindingGoldenEntry>(expected.neighborCityBindings ?? System.Array.Empty<NeighborBindingGoldenEntry>());
            expBindings.Sort((a, b) =>
            {
                int c = string.Compare(a.stubId, b.stubId, StringComparison.Ordinal);
                if (c != 0) return c;
                c = a.exitCellX.CompareTo(b.exitCellX);
                if (c != 0) return c;
                return a.exitCellY.CompareTo(b.exitCellY);
            });

            var sb = new StringBuilder();

            // Compare stubs.
            var actStubs = actual.neighborStubs ?? System.Array.Empty<NeighborStubGoldenEntry>();
            if (expStubs.Count != actStubs.Length)
                sb.AppendLine($"neighborStubs count: golden={expStubs.Count} actual={actStubs.Length}");
            else
            {
                for (int i = 0; i < expStubs.Count; i++)
                {
                    var e = expStubs[i];
                    var a = actStubs[i];
                    if (!IdMatches(e.id ?? "", a.id ?? ""))
                        sb.AppendLine($"stub[{i}].id: golden={e.id} actual={a.id}");
                    if (e.displayName != a.displayName)
                        sb.AppendLine($"stub[{i}].displayName: golden={e.displayName} actual={a.displayName}");
                    if (!string.Equals(e.borderSide, a.borderSide, StringComparison.OrdinalIgnoreCase))
                        sb.AppendLine($"stub[{i}].borderSide: golden={e.borderSide} actual={a.borderSide}");
                }
            }

            // Compare bindings.
            var actBindings = actual.neighborCityBindings ?? System.Array.Empty<NeighborBindingGoldenEntry>();
            if (expBindings.Count != actBindings.Length)
                sb.AppendLine($"neighborCityBindings count: golden={expBindings.Count} actual={actBindings.Length}");
            else
            {
                for (int i = 0; i < expBindings.Count; i++)
                {
                    var e = expBindings[i];
                    var a = actBindings[i];
                    if (!IdMatches(e.stubId ?? "", a.stubId ?? ""))
                        sb.AppendLine($"binding[{i}].stubId: golden={e.stubId} actual={a.stubId}");
                    if (e.exitCellX != a.exitCellX)
                        sb.AppendLine($"binding[{i}].exitCellX: golden={e.exitCellX} actual={a.exitCellX}");
                    if (e.exitCellY != a.exitCellY)
                        sb.AppendLine($"binding[{i}].exitCellY: golden={e.exitCellY} actual={a.exitCellY}");
                    if (!string.Equals(e.borderSide, a.borderSide, StringComparison.OrdinalIgnoreCase))
                        sb.AppendLine($"binding[{i}].borderSide: golden={e.borderSide} actual={a.borderSide}");
                }
            }

            if (sb.Length > 0)
            {
                diff = sb.ToString().TrimEnd();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Sweeps every grid cell and compares <c>HeightMap[x,y]</c> to <c>CityCell.height</c>
        /// (invariant #1). Returns a <see cref="HeightIntegritySweepResultDto"/> with violation count
        /// and up to 10 <see cref="HeightIntegrityViolationDto"/> rows.
        /// </summary>
        /// <remarks>
        /// Called post-load and post-tick so both the load path and the simulation-tick write path are
        /// exercised. <paramref name="grid"/> and <paramref name="hm"/> must both be non-null; caller
        /// is responsible for null-guards before invoking.
        /// </remarks>
        static HeightIntegritySweepResultDto HeightIntegritySweep(GridManager grid, HeightMap hm)
        {
            const int MaxOffenders = 10;
            int w = grid.width;
            int h = grid.height;
            int checkedCells = 0;
            int violations = 0;
            var offenders = new System.Collections.Generic.List<HeightIntegrityViolationDto>(MaxOffenders);

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    CityCell cell = grid.GetCell<CityCell>(x, y);
                    if (cell == null)
                        continue;
                    checkedCells++;
                    int hmHeight = hm.GetHeight(x, y);
                    int cellHeight = cell.height;
                    if (hmHeight != cellHeight)
                    {
                        violations++;
                        if (offenders.Count < MaxOffenders)
                        {
                            offenders.Add(new HeightIntegrityViolationDto
                            {
                                x = x,
                                y = y,
                                heightMap = hmHeight,
                                cell = cellHeight
                            });
                        }
                    }
                }
            }

            return new HeightIntegritySweepResultDto
            {
                checked_cells = checkedCells,
                violations = violations,
                first_offenders = offenders.ToArray()
            };
        }

        static void OnEditorUpdate()
        {
            try
            {
                if (!TryReadState(out AgentTestModeBatchStateDto state))
                {
                    EditorApplication.update -= OnEditorUpdate;
                    return;
                }

                if (state.phase == (int)BatchPhase.WaitGrid)
                    PumpWaitGrid(state);
                else if (state.phase == (int)BatchPhase.WaitStopped)
                    PumpWaitStopped(state);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AgentTestModeBatch] Unexpected error: {ex.Message}");
                BeginExitSequence(6, ex.Message);
            }
        }

        static void PumpWaitGrid(AgentTestModeBatchStateDto state)
        {
            if (!EditorApplication.isPlaying)
            {
                BeginExitSequence(6, "Play Mode was not active during batch wait (unexpected).");
                return;
            }

            if (!TryParseStartedUtc(state.started_utc, out DateTime startedUtc))
                startedUtc = DateTime.UtcNow;

            if ((DateTime.UtcNow - startedUtc).TotalSeconds > GridWaitMaxSeconds)
            {
                BeginExitSequence(6, $"GridManager did not finish initializing within {GridWaitMaxSeconds} seconds.");
                return;
            }

            GridManager grid = UnityEngine.Object.FindObjectOfType<GridManager>();
            if (grid == null || !grid.isInitialized)
                return;

            try
            {
                GameSaveManager saveManager = UnityEngine.Object.FindObjectOfType<GameSaveManager>();
                if (saveManager == null)
                {
                    BeginExitSequence(6, "GameSaveManager not found in the loaded scene.");
                    return;
                }

                if (state.new_game_mode)
                {
                    // --- New-game smoke path ---
                    // Delegate to NeighborStubSmokeDriver; it calls NewGame + GenerateAndPlaceInterstate.
                    NeighborStubSmokeDriver.RunNewGameSmoke(saveManager, grid, state.test_seed);
                }
                else
                {
                    saveManager.LoadGame(state.save_path);
                }

                // --- HeightMap integrity sweep — post-load (invariant #1) ---
                HeightMap hmPostLoad = grid.terrainManager != null
                    ? grid.terrainManager.GetOrCreateHeightMap()
                    : null;
                HeightIntegritySweepResultDto sweepPostLoad = hmPostLoad != null
                    ? HeightIntegritySweep(grid, hmPostLoad)
                    : null;
                if (sweepPostLoad != null && sweepPostLoad.violations > 0)
                    Debug.LogWarning($"[AgentTestModeBatch] Height integrity post-load: {sweepPostLoad.violations} violation(s).");

                int applied = 0;
                SimulationManager simulationManager = UnityEngine.Object.FindObjectOfType<SimulationManager>();
                if (state.ticks_requested > 0 && simulationManager == null)
                    Debug.LogWarning("[AgentTestModeBatch] SimulationManager not found; simulation ticks skipped.");
                if (simulationManager != null && state.ticks_requested > 0)
                {
                    for (int i = 0; i < state.ticks_requested; i++)
                    {
                        simulationManager.ProcessSimulationTick();
                        applied++;
                    }
                }

                // --- HeightMap integrity sweep — post-tick (invariant #1) ---
                // Only run when ticks were actually applied; reuses the same HeightMap instance.
                HeightIntegritySweepResultDto sweepPostTick = null;
                if (applied > 0 && hmPostLoad != null)
                {
                    sweepPostTick = HeightIntegritySweep(grid, hmPostLoad);
                    if (sweepPostTick.violations > 0)
                        Debug.LogWarning($"[AgentTestModeBatch] Height integrity post-tick: {sweepPostTick.violations} violation(s).");
                }

                // Persist height_integrity to state for report serialization.
                var hiDto = new HeightIntegrityDto
                {
                    post_load = sweepPostLoad ?? new HeightIntegritySweepResultDto(),
                    post_tick = sweepPostTick ?? new HeightIntegritySweepResultDto()
                };
                state.height_integrity_json = JsonUtility.ToJson(hiDto, prettyPrint: false);

                state.ticks_applied = applied;
                CityStats cityStats = UnityEngine.Object.FindObjectOfType<CityStats>();
                AgentTestModeBatchCitySnapshotDto snapshot = BuildCitySnapshot(cityStats, applied, grid);
                if (snapshot != null)
                    state.city_stats_snapshot_json = JsonUtility.ToJson(snapshot, prettyPrint: false);

                // Capture neighbor stub + binding projection for every run (reported regardless of golden type).
                NeighborStubRoundtripGoldenDto neighborSnapshot = BuildNeighborStubSnapshot(saveManager);
                if (neighborSnapshot != null)
                    state.neighbor_stubs_snapshot_json = JsonUtility.ToJson(neighborSnapshot, prettyPrint: false);

                // --- New-game smoke assertions ---
                if (state.new_game_mode)
                {
                    NeighborStubSmokeResultDto smokeResult = NeighborStubSmokeDriver.RunSmokeAssertions(saveManager, grid);
                    state.neighbor_stub_smoke_json = JsonUtility.ToJson(smokeResult, prettyPrint: false);
                    if (!smokeResult.assertions_passed && state.exit_code == 0)
                    {
                        state.exit_code = ExitCodeGoldenMismatch;
                        state.error = $"Neighbor-stub smoke assertions failed: {smokeResult.failure_detail}";
                    }
                }

                state.golden_checked = !string.IsNullOrEmpty(state.golden_path);
                state.golden_matched = true;
                state.golden_diff = string.Empty;
                if (state.golden_checked)
                {
                    bool goldenOk;
                    string gdiff = null;
                    if (IsNeighborStubGolden(state.golden_path))
                    {
                        // --- Neighbor-stub golden compare path ---
                        if (neighborSnapshot == null)
                        {
                            goldenOk = false;
                            gdiff = "GameSaveManager not found; cannot compare neighbor-stub golden.";
                        }
                        else
                        {
                            goldenOk = TryCompareNeighborStubGolden(state.golden_path, neighborSnapshot, out gdiff);
                        }
                    }
                    else
                    {
                        // --- City-stats golden compare path (existing behaviour) ---
                        if (snapshot == null)
                        {
                            goldenOk = false;
                            gdiff = "CityStats not found; cannot compare golden.";
                        }
                        else
                        {
                            goldenOk = TryCompareGolden(state.golden_path, snapshot, state.ticks_requested, out gdiff);
                        }
                    }

                    if (!goldenOk)
                    {
                        state.golden_matched = false;
                        state.golden_diff = gdiff ?? "Golden mismatch.";
                        state.exit_code = ExitCodeGoldenMismatch;
                        state.error = $"Golden mismatch:\n{state.golden_diff}";
                        WriteState(state);
                        TryWriteReportFromState(state, ok: false, error: state.error);
                    }
                    else
                    {
                        // Golden matched — still check height integrity.
                        bool hiViolation = (sweepPostLoad != null && sweepPostLoad.violations > 0)
                                        || (sweepPostTick != null && sweepPostTick.violations > 0);
                        if (hiViolation)
                        {
                            state.exit_code = ExitCodeHeightIntegrityFail;
                            state.error = "HeightMap / CityCell.height integrity violation (invariant #1).";
                            WriteState(state);
                            TryWriteReportFromState(state, ok: false, error: state.error);
                        }
                        else
                        {
                            state.exit_code = 0;
                            state.error = string.Empty;
                            WriteState(state);
                            TryWriteReportFromState(state, ok: true, error: null);
                        }
                    }
                }
                else
                {
                    bool hiViolation = (sweepPostLoad != null && sweepPostLoad.violations > 0)
                                    || (sweepPostTick != null && sweepPostTick.violations > 0);
                    if (hiViolation)
                    {
                        state.exit_code = ExitCodeHeightIntegrityFail;
                        state.error = "HeightMap / CityCell.height integrity violation (invariant #1).";
                        WriteState(state);
                        TryWriteReportFromState(state, ok: false, error: state.error);
                    }
                    else
                    {
                        state.exit_code = 0;
                        state.error = string.Empty;
                        WriteState(state);
                        TryWriteReportFromState(state, ok: true, error: null);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AgentTestModeBatch] Load or simulation failed: {ex}");
                state.exit_code = 6;
                state.error = ex.Message;
                WriteState(state);
                TryWriteReportFromState(state, ok: false, error: ex.Message);
            }

            state.phase = (int)BatchPhase.WaitStopped;
            state.started_utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            WriteState(state);
            EditorApplication.ExitPlaymode();
        }

        static void PumpWaitStopped(AgentTestModeBatchStateDto state)
        {
            if (EditorApplication.isPlaying)
            {
                if (!TryParseStartedUtc(state.started_utc, out DateTime startedUtc))
                    startedUtc = DateTime.UtcNow;
                if ((DateTime.UtcNow - startedUtc).TotalSeconds > ExitPlayWaitMaxSeconds)
                {
                    Debug.LogError("[AgentTestModeBatch] Timed out waiting for Play Mode to stop.");
                    int pendingCode = state.exit_code;
                    FinishAndExitEditor(pendingCode != 0 ? pendingCode : 7);
                }

                return;
            }

            FinishAndExitEditor(state.exit_code);
        }

        static void BeginExitSequence(int exitCode, string error)
        {
            Debug.LogError($"[AgentTestModeBatch] {error}");
            if (TryReadState(out AgentTestModeBatchStateDto state))
            {
                state.exit_code = exitCode;
                state.error = error ?? string.Empty;
                WriteState(state);
                TryWriteReportFromState(state, ok: false, error: error);
            }
            else
            {
                TryWriteReportImmediate(ok: false, error, exitCode);
            }

            if (EditorApplication.isPlaying)
            {
                if (TryReadState(out AgentTestModeBatchStateDto st))
                {
                    st.phase = (int)BatchPhase.WaitStopped;
                    st.started_utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
                    WriteState(st);
                }

                EditorApplication.ExitPlaymode();
            }
            else
            {
                FinishAndExitEditor(exitCode);
            }
        }

        static void FinishAndExitEditor(int exitCode)
        {
            DeleteStateFile();
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.Exit(exitCode);
        }

        static void FailImmediate(int exitCode, string error)
        {
            Debug.LogError($"[AgentTestModeBatch] {error}");
            TryWriteReportImmediate(ok: false, error, exitCode);
            EditorApplication.Exit(exitCode);
        }

        static bool TryParseStartedUtc(string raw, out DateTime utc)
        {
            return DateTime.TryParse(
                raw,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out utc);
        }

        static void TryWriteReportFromState(AgentTestModeBatchStateDto state, bool ok, string error)
        {
            int exitCode = state.exit_code != 0 ? state.exit_code : (ok ? 0 : 6);
            TryWriteReportBody(ok, error, exitCode, state.save_path, state.scenario_id, state.ticks_requested, state.ticks_applied, state);
        }

        static void TryWriteReportImmediate(bool ok, string error, int exitCode)
        {
            string savePath = string.Empty;
            string scenarioId = string.Empty;
            int ticksRequested = 0;
            int ticksApplied = 0;
            if (TestModeCommandLineBootstrap.TryParse(Environment.GetCommandLineArgs(), out string sid, out string sp))
            {
                savePath = sp ?? string.Empty;
                scenarioId = sid ?? string.Empty;
            }

            ticksRequested = ParseSimulationTicks(Environment.GetCommandLineArgs());
            TryWriteReportBody(ok, error, exitCode, savePath, scenarioId, ticksRequested, ticksApplied, null);
        }

        static void TryWriteReportBody(
            bool ok,
            string error,
            int exitCode,
            string savePath,
            string scenarioId,
            int ticksRequested,
            int ticksApplied,
            AgentTestModeBatchStateDto extraState)
        {
            try
            {
                string repoRoot = ScenarioPathResolver.GetRepositoryRoot();
                string reportsDir = Path.Combine(repoRoot, "tools", "reports");
                Directory.CreateDirectory(reportsDir);
                string fileName = $"agent-testmode-batch-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
                string fullPath = Path.Combine(reportsDir, fileName);

                AgentTestModeBatchCitySnapshotDto cityStatsDto = null;
                string goldenPath = string.Empty;
                bool goldenChecked = false;
                bool goldenMatched = false;
                string goldenDiff = string.Empty;
                HeightIntegrityDto hiDto = null;
                NeighborStubGoldenEntry[] neighborStubsArr = null;
                NeighborBindingGoldenEntry[] neighborBindingsArr = null;
                NeighborStubSmokeResultDto smokeDto = null;
                bool isNewGameMode = false;
                if (extraState != null)
                {
                    goldenPath = extraState.golden_path ?? string.Empty;
                    goldenChecked = extraState.golden_checked;
                    goldenMatched = extraState.golden_matched;
                    goldenDiff = extraState.golden_diff ?? string.Empty;
                    isNewGameMode = extraState.new_game_mode;
                    if (!string.IsNullOrEmpty(extraState.city_stats_snapshot_json))
                        cityStatsDto = JsonUtility.FromJson<AgentTestModeBatchCitySnapshotDto>(extraState.city_stats_snapshot_json);
                    if (!string.IsNullOrEmpty(extraState.height_integrity_json))
                        hiDto = JsonUtility.FromJson<HeightIntegrityDto>(extraState.height_integrity_json);
                    if (!string.IsNullOrEmpty(extraState.neighbor_stubs_snapshot_json))
                    {
                        var nSnap = JsonUtility.FromJson<NeighborStubRoundtripGoldenDto>(extraState.neighbor_stubs_snapshot_json);
                        if (nSnap != null)
                        {
                            neighborStubsArr = nSnap.neighborStubs;
                            neighborBindingsArr = nSnap.neighborCityBindings;
                        }
                    }
                    if (!string.IsNullOrEmpty(extraState.neighbor_stub_smoke_json))
                        smokeDto = JsonUtility.FromJson<NeighborStubSmokeResultDto>(extraState.neighbor_stub_smoke_json);
                }

                var dto = new AgentTestModeBatchReportDto
                {
                    mode = isNewGameMode ? "new-game" : "load",
                    ok = ok,
                    scenario_id = scenarioId ?? string.Empty,
                    save_path = savePath ?? string.Empty,
                    golden_path = goldenPath,
                    golden_checked = goldenChecked,
                    golden_matched = goldenMatched,
                    golden_diff = goldenDiff,
                    city_stats = cityStatsDto,
                    height_integrity = hiDto,
                    neighbor_stubs = neighborStubsArr,
                    neighbor_city_bindings = neighborBindingsArr,
                    neighbor_stub_smoke = smokeDto,
                    simulation_ticks_requested = ticksRequested,
                    simulation_ticks_applied = ticksApplied,
                    exit_code = exitCode,
                    error = error ?? string.Empty,
                    finished_at_utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
                };

                File.WriteAllText(fullPath, JsonUtility.ToJson(dto, prettyPrint: true));
                Debug.Log($"[AgentTestModeBatch] Report written: {fullPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AgentTestModeBatch] Failed to write report: {ex.Message}");
            }
        }
    }
}
#endif
