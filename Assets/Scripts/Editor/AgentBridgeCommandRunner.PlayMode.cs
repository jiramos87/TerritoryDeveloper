using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;

// Stage 6.1 extract — play-mode pump + enter/exit/status handlers.
// Moved from AgentBridgeCommandRunner.cs stem to keep stem ≤200 LOC.

public static partial class AgentBridgeCommandRunner
{
    static void FocusGameViewIfPossible()
    {
        try
        {
            Type gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
            if (gameViewType == null) return;
            EditorWindow.GetWindow(gameViewType);
        }
        catch { }
    }

    static void ClearEnterPlaySessionState()
    {
        SessionState.EraseString(SessionEnterCommandIdKey);
        SessionState.EraseString(SessionEnterRepoRootKey);
        SessionState.EraseString(SessionEnterStartedUtcKey);
    }

    static void ClearExitPlaySessionState()
    {
        SessionState.EraseString(SessionExitCommandIdKey);
        SessionState.EraseString(SessionExitRepoRootKey);
    }

    static void PumpEnterPlayModeWait()
    {
        string cmdId = SessionState.GetString(SessionEnterCommandIdKey, string.Empty);
        if (string.IsNullOrEmpty(cmdId)) return;
        string repoRoot = SessionState.GetString(SessionEnterRepoRootKey, string.Empty);
        if (string.IsNullOrEmpty(repoRoot)) { ClearEnterPlaySessionState(); return; }
        string startedRaw = SessionState.GetString(SessionEnterStartedUtcKey, string.Empty);
        if (!DateTime.TryParse(startedRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime startedUtc))
            startedUtc = DateTime.UtcNow;
        if ((DateTime.UtcNow - startedUtc).TotalSeconds > EnterPlayModeGridWaitMaxSeconds)
        {
            string failJson = BridgeCommandService.BuildPlayModeBridgeResponseJson(cmdId, false, "play_mode", "GridManager did not finish initializing within 24 seconds; check Play Mode / scene load errors and retry enter_play_mode.", "play_mode_loading", false, false, false, false, 0, 0);
            ClearEnterPlaySessionState(); CompleteOrFail(repoRoot, cmdId, failJson); return;
        }
        if (!EditorApplication.isPlaying)
        {
            string failJson = BridgeCommandService.BuildPlayModeBridgeResponseJson(cmdId, false, "play_mode", "Play Mode was not active before GridManager initialized; enter_play_mode was cancelled or failed.", "edit_mode", false, false, false, false, 0, 0);
            ClearEnterPlaySessionState(); CompleteOrFail(repoRoot, cmdId, failJson); return;
        }
        GridManager grid = UnityEngine.Object.FindObjectOfType<GridManager>();
        if (grid != null && grid.isInitialized)
        {
            string okJson = BridgeCommandService.BuildPlayModeBridgeResponseJson(cmdId, true, "play_mode", string.Empty, "play_mode_ready", true, false, false, true, grid.width, grid.height);
            ClearEnterPlaySessionState(); CompleteOrFail(repoRoot, cmdId, okJson);
        }
    }

    static void PumpExitPlayModeWait()
    {
        string cmdId = SessionState.GetString(SessionExitCommandIdKey, string.Empty);
        if (string.IsNullOrEmpty(cmdId)) return;
        string repoRoot = SessionState.GetString(SessionExitRepoRootKey, string.Empty);
        if (string.IsNullOrEmpty(repoRoot)) { ClearExitPlaySessionState(); return; }
        if (EditorApplication.isPlaying) return;
        string okJson = BridgeCommandService.BuildPlayModeBridgeResponseJson(cmdId, true, "play_mode", string.Empty, "edit_mode", false, false, false, false, 0, 0);
        ClearExitPlaySessionState(); CompleteOrFail(repoRoot, cmdId, okJson);
    }

    static void RunEnterPlayMode(string repoRoot, string commandId)
    {
        string pendingEnter = SessionState.GetString(SessionEnterCommandIdKey, string.Empty);
        if (!string.IsNullOrEmpty(pendingEnter) && !string.Equals(pendingEnter, commandId, StringComparison.Ordinal))
        {
            TryFinalizeFailed(repoRoot, commandId, "Another enter_play_mode request is still waiting for GridManager; wait for it to complete before enqueueing a new one.");
            return;
        }
        if (!string.IsNullOrEmpty(SessionState.GetString(SessionExitCommandIdKey, string.Empty)))
        {
            TryFinalizeFailed(repoRoot, commandId, "exit_play_mode is in progress; wait for it to complete before enter_play_mode.");
            return;
        }
        if (EditorApplication.isPlaying)
        {
            GridManager grid = UnityEngine.Object.FindObjectOfType<GridManager>();
            if (grid != null && grid.isInitialized)
            {
                string json = BridgeCommandService.BuildPlayModeBridgeResponseJson(commandId, true, "play_mode", string.Empty, "play_mode_ready", true, true, false, true, grid.width, grid.height);
                CompleteOrFail(repoRoot, commandId, json); return;
            }
            SessionState.SetString(SessionEnterCommandIdKey, commandId);
            SessionState.SetString(SessionEnterRepoRootKey, repoRoot);
            SessionState.SetString(SessionEnterStartedUtcKey, DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
            return;
        }
        FocusGameViewIfPossible();
        SessionState.SetString(SessionEnterCommandIdKey, commandId);
        SessionState.SetString(SessionEnterRepoRootKey, repoRoot);
        SessionState.SetString(SessionEnterStartedUtcKey, DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
        EditorApplication.EnterPlaymode();
    }

    static void RunExitPlayMode(string repoRoot, string commandId)
    {
        if (!EditorApplication.isPlaying)
        {
            string json = BridgeCommandService.BuildPlayModeBridgeResponseJson(commandId, true, "play_mode", string.Empty, "edit_mode", false, false, true, false, 0, 0);
            CompleteOrFail(repoRoot, commandId, json); return;
        }
        if (!string.IsNullOrEmpty(SessionState.GetString(SessionExitCommandIdKey, string.Empty)))
        {
            TryFinalizeFailed(repoRoot, commandId, "Another exit_play_mode request is already in progress.");
            return;
        }
        ClearEnterPlaySessionState();
        SessionState.SetString(SessionExitCommandIdKey, commandId);
        SessionState.SetString(SessionExitRepoRootKey, repoRoot);
        EditorApplication.ExitPlaymode();
    }

    static void RunGetPlayModeStatus(string repoRoot, string commandId)
    {
        if (!EditorApplication.isPlaying)
        {
            CompleteOrFail(repoRoot, commandId, BridgeCommandService.BuildPlayModeBridgeResponseJson(commandId, true, "play_mode", string.Empty, "edit_mode", false, false, false, false, 0, 0));
            return;
        }
        GridManager grid = UnityEngine.Object.FindObjectOfType<GridManager>();
        bool init = grid != null && grid.isInitialized;
        string state = init ? "play_mode_ready" : "play_mode_loading";
        CompleteOrFail(repoRoot, commandId, BridgeCommandService.BuildPlayModeBridgeResponseJson(commandId, true, "play_mode", string.Empty, state, init, false, false, init, init ? grid.width : 0, init ? grid.height : 0));
    }
}
