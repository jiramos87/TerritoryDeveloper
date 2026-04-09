#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using Territory.Core;
using Territory.Persistence;
using Territory.Simulation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Territory.Testing
{
    /// <summary>
    /// <b>Editor</b> <c>-batchmode</c> entry: open <see cref="MainScenePath"/>, enter <b>Play Mode</b>,
    /// resolve <b>test mode</b> scenario args via <see cref="TestModeCommandLineBootstrap.TryParse"/>,
    /// load through <see cref="GameSaveManager.LoadGame"/> only, optionally run bounded <see cref="SimulationManager.ProcessSimulationTick"/>,
    /// write <c>tools/reports/agent-testmode-batch-*.json</c>, then exit the Editor with <see cref="EditorApplication.Exit"/>.
    /// Persists in-flight state in <c>tools/reports/.agent-testmode-batch-state.json</c> (gitignored) across Play Mode domain reloads
    /// because <see cref="SessionState"/> is not reliable for this flow in <c>-batchmode</c>.
    /// </summary>
    public static class AgentTestModeBatchRunner
    {
        public const string ExecuteMethodName = "Territory.Testing.AgentTestModeBatchRunner.Run";

        /// <summary>Same scenario flags as <see cref="TestModeCommandLineBootstrap"/>.</summary>
        public const string ArgSimulationTicks = "-testSimulationTicks";

        public const string MainScenePath = "Assets/Scenes/MainScene.unity";

        /// <summary>Transient state file under <c>tools/reports/</c> (dotfile; ignored by <c>tools/reports/**</c>).</summary>
        public const string StateFileName = ".agent-testmode-batch-state.json";

        const double GridWaitMaxSeconds = 120.0;
        const double ExitPlayWaitMaxSeconds = 90.0;

        enum BatchPhase
        {
            WaitGrid = 1,
            WaitStopped = 2
        }

        [Serializable]
        class AgentTestModeBatchStateDto
        {
            public int phase;
            public string save_path = "";
            public string scenario_id = "";
            public int ticks_requested;
            public int ticks_applied;
            public int exit_code;
            public string error = "";
            public string started_utc = "";
        }

        [Serializable]
        class AgentTestModeBatchReportDto
        {
            public int schema_version = 1;
            public string kind = "agent-testmode-batch";
            public bool ok;
            public string scenario_id = "";
            public string save_path = "";
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
                state.exit_code = 0;
                state.error = string.Empty;
                WriteState(state);
                TryWriteReportFromState(state, ok: true, error: null);
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
            TryWriteReportBody(ok, error, exitCode, state.save_path, state.scenario_id, state.ticks_requested, state.ticks_applied);
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
            TryWriteReportBody(ok, error, exitCode, savePath, scenarioId, ticksRequested, ticksApplied);
        }

        static void TryWriteReportBody(
            bool ok,
            string error,
            int exitCode,
            string savePath,
            string scenarioId,
            int ticksRequested,
            int ticksApplied)
        {
            try
            {
                string repoRoot = ScenarioPathResolver.GetRepositoryRoot();
                string reportsDir = Path.Combine(repoRoot, "tools", "reports");
                Directory.CreateDirectory(reportsDir);
                string fileName = $"agent-testmode-batch-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
                string fullPath = Path.Combine(reportsDir, fileName);

                var dto = new AgentTestModeBatchReportDto
                {
                    ok = ok,
                    scenario_id = scenarioId ?? string.Empty,
                    save_path = savePath ?? string.Empty,
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
