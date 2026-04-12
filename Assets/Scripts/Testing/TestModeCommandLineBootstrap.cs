using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Territory.Persistence;

namespace Territory.Testing
{
    /// <summary>
    /// Resolve optional <b>test mode</b> scenario launch intent from CLI (<c>-testScenarioId</c> / <c>-testScenarioPath</c>)
    /// or, <b>Editor</b>-only, one-line queue file under <c>tools/fixtures/scenarios/.queued-test-scenario-id</c>
    /// (consumed on success → agents can trigger load before <c>enter_play_mode</c> without restarting Unity).
    /// Loads via <see cref="GameStartInfo"/> + <see cref="GameBootstrap"/> → <see cref="GameSaveManager.LoadGame"/>.
    /// Active only when <see cref="TestModeSecurity.IsTestModeEntryAllowed"/>.
    /// </summary>
    public static class TestModeCommandLineBootstrap
    {
        const string ArgScenarioId = "-testScenarioId";
        const string ArgScenarioPath = "-testScenarioPath";

#if UNITY_EDITOR
        /// <summary>Single-line <b>scenario id</b> (ASCII, <b>kebab-case</b>). Read once per Play session then deleted.</summary>
        public const string EditorQueuedScenarioFileName = ".queued-test-scenario-id";
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void ApplyCommandLineIntent()
        {
            if (!TryResolveLaunchIntent(Environment.GetCommandLineArgs(), out string scenarioId, out string absolutePath))
                return;

            if (!TestModeSecurity.IsTestModeEntryAllowed)
            {
                TestModeSecurity.LogBlockedAttempt($"{ArgScenarioId} / {ArgScenarioPath} / Editor queue file");
                return;
            }

            if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath))
            {
                Debug.LogError($"[TestMode] Scenario file not found: {absolutePath}");
                return;
            }

            TestModeSessionState.ActiveThisSession = true;
            TestModeSessionState.ResolvedScenarioPath = absolutePath;
            TestModeSessionState.ScenarioId = scenarioId;

            GameStartInfo.SetPendingLoadPath(absolutePath);

            int menuIndex = SceneManager.GetActiveScene().buildIndex;
            if (menuIndex == 0)
                SceneManager.LoadScene(1, LoadSceneMode.Single);

            EnsureHudDriver();
        }

        static bool TryResolveLaunchIntent(string[] commandLineArgs, out string scenarioId, out string absolutePath)
        {
            if (TryParse(commandLineArgs, out scenarioId, out absolutePath))
                return true;
#if UNITY_EDITOR
            return TryConsumeEditorQueuedScenario(out scenarioId, out absolutePath);
#else
            scenarioId = null;
            absolutePath = null;
            return false;
#endif
        }

#if UNITY_EDITOR
        static bool TryConsumeEditorQueuedScenario(out string scenarioId, out string absolutePath)
        {
            scenarioId = null;
            absolutePath = null;
            string queuePath = Path.Combine(ScenarioPathResolver.GetScenariosRootDirectory(), EditorQueuedScenarioFileName);
            if (!File.Exists(queuePath))
                return false;

            string raw;
            try
            {
                raw = File.ReadAllText(queuePath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TestMode] Could not read Editor queue file: {ex.Message}");
                return false;
            }

            string id = raw != null ? raw.Trim() : null;
            if (string.IsNullOrEmpty(id))
            {
                TryDeleteQueueFile(queuePath);
                return false;
            }

            if (!ScenarioPathResolver.TryResolveScenarioId(id, out absolutePath))
            {
                Debug.LogError($"[TestMode] Editor queue file lists unknown scenario id '{id}'. Expected under {ScenarioPathResolver.GetScenariosRootDirectory()}.");
                TryDeleteQueueFile(queuePath);
                return false;
            }

            TryDeleteQueueFile(queuePath);
            scenarioId = id;
            return true;
        }

        static void TryDeleteQueueFile(string queuePath)
        {
            try
            {
                if (File.Exists(queuePath))
                    File.Delete(queuePath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TestMode] Could not delete Editor queue file: {ex.Message}");
            }
        }
#endif

        static void EnsureHudDriver()
        {
            if (UnityEngine.Object.FindObjectOfType<TestModeHudDriver>() != null)
                return;
            var go = new GameObject(nameof(TestModeHudDriver));
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<TestModeHudDriver>();
        }

        /// <summary>
        /// Parse <c>-testScenarioId</c> or <c>-testScenarioPath</c>. Path wins if both present.
        /// Also used by <b>Editor</b> <c>-batchmode</c> <c>-executeMethod</c> batch runner (<c>AgentTestModeBatchRunner.Run</c>).
        /// </summary>
        public static bool TryParse(string[] args, out string scenarioId, out string absolutePath)
        {
            scenarioId = null;
            absolutePath = null;
            if (args == null || args.Length == 0)
                return false;

            string idArg = null;
            string pathArg = null;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == ArgScenarioId && i + 1 < args.Length)
                {
                    idArg = args[i + 1];
                    i++;
                }
                else if (args[i] == ArgScenarioPath && i + 1 < args.Length)
                {
                    pathArg = args[i + 1];
                    i++;
                }
            }

            if (!string.IsNullOrEmpty(pathArg))
            {
                absolutePath = Path.GetFullPath(pathArg);
                scenarioId = null;
                return true;
            }

            if (!string.IsNullOrEmpty(idArg))
            {
                if (ScenarioPathResolver.TryResolveScenarioId(idArg, out absolutePath))
                {
                    scenarioId = idArg.Trim();
                    return true;
                }

                Debug.LogError($"[TestMode] Unknown {ArgScenarioId} '{idArg}'. Expected folder under {ScenarioPathResolver.GetScenariosRootDirectory()} with save.json or flat .json.");
                return false;
            }

            return false;
        }
    }
}
