#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using Territory.Core;
using Territory.Persistence;
using Territory.Testing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Territory.Testing
{
    /// <summary>
    /// Unity <c>-batchmode</c> entry. Flow: open <see cref="AgentTestModeBatchRunner.MainScenePath"/> → enter <b>Play Mode</b> →
    /// load base <b>Save data</b> → apply <see cref="ScenarioDescriptorRuntimeApplier"/> → write <see cref="GameSaveData"/> JSON
    /// to <c>-outputScenarioSavePath</c> → exit Editor.
    /// </summary>
    public static class ScenarioDescriptorBatchBuilder
    {
        public const string ExecuteMethodName = "Territory.Testing.ScenarioDescriptorBatchBuilder.Run";

        public const string ArgDescriptorPath = "-scenarioDescriptorPath";
        public const string ArgOutputPath = "-outputScenarioSavePath";
        public const string ArgBaseSavePath = "-baseScenarioSavePath";

        const string StateFileName = ".scenario-descriptor-batch-state.json";
        const double GridWaitMaxSeconds = 120.0;
        const double ExitPlayWaitMaxSeconds = 90.0;

        enum BatchPhase
        {
            WaitGrid = 1,
            WaitStopped = 2
        }

        [Serializable]
        class ScenarioDescriptorBatchStateDto
        {
            public int phase;
            public string descriptor_path = "";
            public string base_save_path = "";
            public string output_save_path = "";
            public string save_name_for_payload = "";
            public int exit_code;
            public string error = "";
            public string started_utc = "";
        }

        static string GetStateFilePath()
        {
            string repoRoot = ScenarioPathResolver.GetRepositoryRoot();
            return Path.Combine(repoRoot, "tools", "reports", StateFileName);
        }

        static bool TryReadState(out ScenarioDescriptorBatchStateDto state)
        {
            state = null;
            string path = GetStateFilePath();
            if (!File.Exists(path))
                return false;
            try
            {
                string raw = File.ReadAllText(path);
                state = JsonUtility.FromJson<ScenarioDescriptorBatchStateDto>(raw);
                return state != null;
            }
            catch
            {
                return false;
            }
        }

        static void WriteState(ScenarioDescriptorBatchStateDto state)
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
                // best-effort
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

        /// <summary>Unity <c>-executeMethod Territory.Testing.ScenarioDescriptorBatchBuilder.Run</c>.</summary>
        public static void Run()
        {
            if (!TestModeSecurity.IsTestModeEntryAllowed)
            {
                FailImmediate(4, "Test mode is not allowed in this build.");
                return;
            }

            string[] args = Environment.GetCommandLineArgs();
            if (!TryParseArgs(args, out string descriptorPath, out string outputPath, out string baseSavePath))
            {
                FailImmediate(4, $"Missing {ArgDescriptorPath} or {ArgOutputPath}.");
                return;
            }

            if (!File.Exists(descriptorPath))
            {
                FailImmediate(4, $"Descriptor not found: {descriptorPath}");
                return;
            }

            if (string.IsNullOrEmpty(baseSavePath))
            {
                if (!ScenarioPathResolver.TryResolveScenarioId("reference-flat-32x32", out baseSavePath))
                {
                    FailImmediate(4, "No base save path and reference-flat-32x32 scenario is missing.");
                    return;
                }
            }
            else if (!File.Exists(baseSavePath))
            {
                FailImmediate(4, $"Base save not found: {baseSavePath}");
                return;
            }

            string descriptorJson;
            try
            {
                descriptorJson = File.ReadAllText(descriptorPath);
            }
            catch (Exception ex)
            {
                FailImmediate(4, $"Could not read descriptor: {ex.Message}");
                return;
            }

            ScenarioDescriptorV1 parsed = JsonUtility.FromJson<ScenarioDescriptorV1>(descriptorJson);
            string saveNamePayload = ScenarioDescriptorRuntimeApplier.ResolveSaveNameForExport(parsed);

            try
            {
                EditorSceneManager.OpenScene(AgentTestModeBatchRunner.MainScenePath, OpenSceneMode.Single);
            }
            catch (Exception ex)
            {
                FailImmediate(4, $"Could not open MainScene: {ex.Message}");
                return;
            }

            var state = new ScenarioDescriptorBatchStateDto
            {
                phase = (int)BatchPhase.WaitGrid,
                descriptor_path = descriptorPath,
                base_save_path = baseSavePath,
                output_save_path = outputPath,
                save_name_for_payload = saveNamePayload,
                exit_code = 0,
                error = string.Empty,
                started_utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
            };
            WriteState(state);

            Debug.Log("[ScenarioDescriptorBatch] State written; entering Play Mode.");
            EditorApplication.EnterPlaymode();
        }

        static bool TryParseArgs(string[] args, out string descriptorPath, out string outputPath, out string baseSavePath)
        {
            descriptorPath = null;
            outputPath = null;
            baseSavePath = null;
            if (args == null)
                return false;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == ArgDescriptorPath && i + 1 < args.Length)
                {
                    descriptorPath = args[i + 1];
                    i++;
                }
                else if (args[i] == ArgOutputPath && i + 1 < args.Length)
                {
                    outputPath = Path.GetFullPath(args[i + 1]);
                    i++;
                }
                else if (args[i] == ArgBaseSavePath && i + 1 < args.Length)
                {
                    baseSavePath = Path.GetFullPath(args[i + 1]);
                    i++;
                }
            }

            return !string.IsNullOrEmpty(descriptorPath) && !string.IsNullOrEmpty(outputPath);
        }

        static void OnEditorUpdate()
        {
            try
            {
                if (!TryReadState(out ScenarioDescriptorBatchStateDto state))
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
                Debug.LogError($"[ScenarioDescriptorBatch] Unexpected error: {ex.Message}");
                BeginExitSequence(6, ex.Message);
            }
        }

        static void PumpWaitGrid(ScenarioDescriptorBatchStateDto state)
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
                    BeginExitSequence(6, "GameSaveManager not found.");
                    return;
                }

                saveManager.LoadGame(state.base_save_path);

                string json = File.ReadAllText(state.descriptor_path);
                if (!ScenarioDescriptorRuntimeApplier.TryApplyFromJson(json, out string applyError))
                {
                    BeginExitSequence(6, applyError ?? "ScenarioDescriptorRuntimeApplier failed.");
                    return;
                }

                if (!saveManager.TryWriteGameSaveToPath(state.output_save_path, state.save_name_for_payload, out string writeErr))
                {
                    BeginExitSequence(6, writeErr ?? "TryWriteGameSaveToPath failed.");
                    return;
                }

                state.exit_code = 0;
                state.error = string.Empty;
                WriteState(state);
                Debug.Log($"[ScenarioDescriptorBatch] Wrote {state.output_save_path}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ScenarioDescriptorBatch] {ex}");
                state.exit_code = 6;
                state.error = ex.Message;
                WriteState(state);
            }

            state.phase = (int)BatchPhase.WaitStopped;
            state.started_utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            WriteState(state);
            EditorApplication.ExitPlaymode();
        }

        static void PumpWaitStopped(ScenarioDescriptorBatchStateDto state)
        {
            if (EditorApplication.isPlaying)
            {
                if (!TryParseStartedUtc(state.started_utc, out DateTime startedUtc))
                    startedUtc = DateTime.UtcNow;
                if ((DateTime.UtcNow - startedUtc).TotalSeconds > ExitPlayWaitMaxSeconds)
                {
                    Debug.LogError("[ScenarioDescriptorBatch] Timed out waiting for Play Mode to stop.");
                    FinishAndExitEditor(state.exit_code != 0 ? state.exit_code : 7);
                }

                return;
            }

            FinishAndExitEditor(state.exit_code);
        }

        static void BeginExitSequence(int exitCode, string error)
        {
            Debug.LogError($"[ScenarioDescriptorBatch] {error}");
            if (TryReadState(out ScenarioDescriptorBatchStateDto state))
            {
                state.exit_code = exitCode;
                state.error = error ?? string.Empty;
                WriteState(state);
            }

            if (EditorApplication.isPlaying)
            {
                if (TryReadState(out ScenarioDescriptorBatchStateDto st))
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
            Debug.LogError($"[ScenarioDescriptorBatch] {error}");
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
    }
}
#endif
