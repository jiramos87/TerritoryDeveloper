#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using System.Text;
using Territory.Core;
using Territory.Economy;
using Territory.Persistence;
using Territory.Simulation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Territory.Testing
{
    /// <summary>
    /// <b>Editor</b> <c>-batchmode</c> entry. Flow: open <see cref="MainScenePath"/> → enter <b>Play Mode</b> →
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

        public const int ExitCodeGoldenMismatch = 8;

        public const string MainScenePath = "Assets/Scenes/MainScene.unity";

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
        /// </summary>
        [Serializable]
        public class AgentTestModeBatchCitySnapshotDto
        {
            public int schema_version = 1;
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
        }

        [Serializable]
        class AgentTestModeBatchReportDto
        {
            public int schema_version = 2;
            public string kind = "agent-testmode-batch";
            public bool ok;
            public string scenario_id = "";
            public string save_path = "";
            public string golden_path = "";
            public bool golden_checked;
            public bool golden_matched;
            public string golden_diff = "";
            public AgentTestModeBatchCitySnapshotDto city_stats;
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
                EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            }
            catch (Exception ex)
            {
                FailImmediate(4, $"Could not open MainScene: {ex.Message}");
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

        static AgentTestModeBatchCitySnapshotDto BuildCitySnapshot(CityStats cityStats, int ticksApplied)
        {
            if (cityStats == null)
                return null;
            return new AgentTestModeBatchCitySnapshotDto
            {
                schema_version = 1,
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
                forestCellCount = cityStats.forestCellCount
            };
        }

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

            if (expected.schema_version != 1)
            {
                diff = $"Unsupported golden schema_version {expected.schema_version} (expected 1).";
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

            if (sb.Length > 0)
            {
                diff = sb.ToString().TrimEnd();
                return false;
            }

            return true;
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

                saveManager.LoadGame(state.save_path);

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

                state.ticks_applied = applied;
                CityStats cityStats = UnityEngine.Object.FindObjectOfType<CityStats>();
                AgentTestModeBatchCitySnapshotDto snapshot = BuildCitySnapshot(cityStats, applied);
                if (snapshot != null)
                    state.city_stats_snapshot_json = JsonUtility.ToJson(snapshot, prettyPrint: false);

                state.golden_checked = !string.IsNullOrEmpty(state.golden_path);
                state.golden_matched = true;
                state.golden_diff = string.Empty;
                if (state.golden_checked)
                {
                    if (snapshot == null)
                    {
                        state.golden_matched = false;
                        state.golden_diff = "CityStats not found; cannot compare golden.";
                        state.exit_code = ExitCodeGoldenMismatch;
                        state.error = state.golden_diff;
                        WriteState(state);
                        TryWriteReportFromState(state, ok: false, error: state.error);
                    }
                    else if (!TryCompareGolden(state.golden_path, snapshot, state.ticks_requested, out string gdiff))
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
                        state.exit_code = 0;
                        state.error = string.Empty;
                        WriteState(state);
                        TryWriteReportFromState(state, ok: true, error: null);
                    }
                }
                else
                {
                    state.exit_code = 0;
                    state.error = string.Empty;
                    WriteState(state);
                    TryWriteReportFromState(state, ok: true, error: null);
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
                if (extraState != null)
                {
                    goldenPath = extraState.golden_path ?? string.Empty;
                    goldenChecked = extraState.golden_checked;
                    goldenMatched = extraState.golden_matched;
                    goldenDiff = extraState.golden_diff ?? string.Empty;
                    if (!string.IsNullOrEmpty(extraState.city_stats_snapshot_json))
                        cityStatsDto = JsonUtility.FromJson<AgentTestModeBatchCitySnapshotDto>(extraState.city_stats_snapshot_json);
                }

                var dto = new AgentTestModeBatchReportDto
                {
                    ok = ok,
                    scenario_id = scenarioId ?? string.Empty,
                    save_path = savePath ?? string.Empty,
                    golden_path = goldenPath,
                    golden_checked = goldenChecked,
                    golden_matched = goldenMatched,
                    golden_diff = goldenDiff,
                    city_stats = cityStatsDto,
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
