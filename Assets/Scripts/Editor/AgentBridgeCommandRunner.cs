using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Territory.Core;
using Territory.Terrain;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Poll Postgres via <c>agent-bridge-dequeue.mjs</c> → dispatch bridge <c>kind</c> to existing
/// <b>Territory Developer → Reports</b> entry points or bridge helpers → complete jobs via <c>agent-bridge-complete.mjs</c>.
/// Requires <c>DATABASE_URL</c> — same as <see cref="EditorPostgresExportRegistrar"/>.
/// Play Mode bridge kinds use <see cref="SessionState"/> → completion survives domain reload on Play Mode enter/exit.
/// <c>get_compilation_status</c> completes synchronously; compile snapshot for IDE agents.
/// </summary>
public static partial class AgentBridgeCommandRunner
{
    const int PollEveryNFrames = 30;

    /// <summary>Wall-clock budget for <c>GridManager.isInitialized</c> after Play Mode start; under MCP <c>timeout_ms</c> 30s.</summary>
    const double EnterPlayModeGridWaitMaxSeconds = 24.0;

    const string SessionEnterCommandIdKey = "TerritoryDeveloper.AgentBridge.EnterPending.command_id";
    const string SessionEnterRepoRootKey = "TerritoryDeveloper.AgentBridge.EnterPending.repo_root";
    const string SessionEnterStartedUtcKey = "TerritoryDeveloper.AgentBridge.EnterPending.started_utc";

    const string SessionExitCommandIdKey = "TerritoryDeveloper.AgentBridge.ExitPending.command_id";
    const string SessionExitRepoRootKey = "TerritoryDeveloper.AgentBridge.ExitPending.repo_root";

    /// <summary>
    /// Full-game <see cref="ScreenCapture.CaptureScreenshot"/> writes after game view renders.
    /// Poll <see cref="EditorApplication.update"/> until file exists or short wall-clock budget elapses.
    /// Keeps MCP <c>timeout_ms</c> ≤ 30s practical; frame counts alone unreliable when Editor update rate drops.
    /// </summary>
    const double DeferredScreenshotMaxWaitSeconds = 15.0;

    sealed class DeferredScreenshotWork
    {
        public string RepoRoot;
        public string CommandId;
        public string AbsolutePath;
        public string RepoRelativePath;
        public DateTime StartedUtc;

        /// <summary>When set → completion builds <c>debug_context_bundle</c> JSON instead of plain screenshot observability.</summary>
        public DebugBundleCompletionStash DebugBundle;
    }

    /// <summary>Holds synchronous <c>debug_context_bundle</c> results while screenshot file still pending.</summary>
    sealed class DebugBundleCompletionStash
    {
        public string CellExportRelPath;
        public bool CellExportOk;
        public string CellExportError;
        public AgentBridgeLogLineDto[] ConsoleLines;
        public bool ConsoleSkipped;
        public AgentBridgeAnomalyRecordDto[] Anomalies;
        public bool AnomalyScanSkipped;
        public bool ScreenshotIncluded;
    }

    static readonly List<DeferredScreenshotWork> s_deferredScreenshots = new List<DeferredScreenshotWork>();

    static int s_frameCounter;

    [InitializeOnLoadMethod]
    static void RegisterUpdate()
    {
        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.update += OnEditorUpdate;
    }

    static void OnEditorUpdate()
    {
        try
        {
            PumpEnterPlayModeWait();
            PumpExitPlayModeWait();
            PumpDeferredScreenshotCompletions();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AgentBridge] Unexpected error in pump loop: {ex.Message}");
        }

        s_frameCounter++;
        if (s_frameCounter < PollEveryNFrames)
            return;
        s_frameCounter = 0;
        try
        {
            ProcessOnePendingCommandIfAny();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AgentBridge] Unexpected error in poll loop: {ex.Message}");
        }
    }

    static void ProcessOnePendingCommandIfAny()
    {
        string repoRoot = EditorPostgresExportRegistrar.GetRepositoryRoot();
        string dbUrl = EditorPostgresExportRegistrar.ResolveEffectiveDatabaseUrl(repoRoot);
        if (string.IsNullOrWhiteSpace(dbUrl))
            return;

        if (!EditorPostgresBridgeJobs.TryDequeue(repoRoot, out EditorPostgresBridgeJobs.DequeueStdoutDto dq, out string dqLog))
        {
            if (!string.IsNullOrEmpty(dqLog) && dqLog.Contains("exit=", StringComparison.Ordinal))
                Debug.LogWarning($"[AgentBridge] Dequeue failed: {dqLog}");
            return;
        }

        if (dq == null || dq.empty)
            return;

        string commandId = dq.command_id;
        if (string.IsNullOrEmpty(commandId))
        {
            Debug.LogWarning("[AgentBridge] Dequeue returned ok but missing command_id.");
            return;
        }

        if (string.IsNullOrEmpty(dq.kind))
        {
            TryFinalizeFailed(repoRoot, commandId, "Missing kind.");
            return;
        }

        switch (dq.kind)
        {
            // OBSERVATION kinds (11) — do not modify; regression gate TECH-412
            case "export_agent_context": // OBSERVATION — do not modify
                RunExportAgentContext(repoRoot, commandId, dq.request_json);
                break;
            case "get_console_logs": // OBSERVATION — do not modify
                RunGetConsoleLogs(repoRoot, commandId, dq.request_json);
                break;
            case "capture_screenshot": // OBSERVATION — do not modify
                RunCaptureScreenshot(repoRoot, commandId, dq.request_json);
                break;
            case "enter_play_mode": // OBSERVATION — do not modify
                RunEnterPlayMode(repoRoot, commandId);
                break;
            case "exit_play_mode": // OBSERVATION — do not modify
                RunExitPlayMode(repoRoot, commandId);
                break;
            case "get_play_mode_status": // OBSERVATION — do not modify
                RunGetPlayModeStatus(repoRoot, commandId);
                break;
            case "debug_context_bundle": // OBSERVATION — do not modify
                RunDebugContextBundle(repoRoot, commandId, dq.request_json);
                break;
            case "get_compilation_status": // OBSERVATION — do not modify
                RunGetCompilationStatus(repoRoot, commandId, dq.request_json);
                break;
            case "economy_balance_snapshot": // OBSERVATION — do not modify
                RunEconomyBalanceSnapshot(repoRoot, commandId, dq.request_json);
                break;
            case "prefab_manifest": // OBSERVATION — do not modify
                RunPrefabManifest(repoRoot, commandId, dq.request_json);
                break;
            case "sorting_order_debug": // OBSERVATION — do not modify
                RunSortingOrderDebug(repoRoot, commandId, dq.request_json);
                break;
            case "export_cell_chunk":
                RunExportCellChunk(repoRoot, commandId, dq.request_json);
                break;
            case "export_sorting_debug":
                RunExportSortingDebug(repoRoot, commandId, dq.request_json);
                break;
            case "catalog_preview":
                TryDispatchCatalogPreviewKind(dq.kind, repoRoot, commandId, dq.request_json);
                break;
            case "prefab_inspect": // OBSERVATION (Stage 12 Step 14.1) — read-only prefab hierarchy + serialized field dump
                RunPrefabInspect(repoRoot, commandId, dq.request_json);
                break;
            case "ui_tree_walk": // OBSERVATION (Stage 12 Step 14.2) — read-only scene Canvas walk + screen-space rects
                RunUiTreeWalk(repoRoot, commandId, dq.request_json);
                break;
            default:
                // Try mutation kinds (Phases 1-3)
                if (!TryDispatchMutationKind(dq.kind, repoRoot, commandId, dq.request_json))
                {
                    TryFinalizeFailed(
                        repoRoot,
                        commandId,
                        $"Unknown kind '{dq.kind}'. Observation kinds: export_agent_context, get_console_logs, capture_screenshot, enter_play_mode, exit_play_mode, get_play_mode_status, debug_context_bundle, get_compilation_status, economy_balance_snapshot, prefab_manifest, sorting_order_debug, export_cell_chunk, export_sorting_debug, catalog_preview, prefab_inspect, ui_tree_walk. Mutation kinds (Edit Mode): attach_component, remove_component, assign_serialized_field, create_gameobject, delete_gameobject, find_gameobject, set_transform, set_gameobject_active, set_gameobject_parent, save_scene, open_scene, new_scene, instantiate_prefab, apply_prefab_overrides, create_scriptable_object, modify_scriptable_object, refresh_asset_database, move_asset, delete_asset, execute_menu_item.");
                }
                break;
        }
    }

    static void FocusGameViewIfPossible()
    {
        try
        {
            Type gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
            if (gameViewType == null)
                return;
            EditorWindow.GetWindow(gameViewType);
        }
        catch
        {
            // Game view focus is best-effort for ScreenCapture / Overlay UI.
        }
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
        if (string.IsNullOrEmpty(cmdId))
            return;

        string repoRoot = SessionState.GetString(SessionEnterRepoRootKey, string.Empty);
        if (string.IsNullOrEmpty(repoRoot))
        {
            ClearEnterPlaySessionState();
            return;
        }

        string startedRaw = SessionState.GetString(SessionEnterStartedUtcKey, string.Empty);
        if (!DateTime.TryParse(
                startedRaw,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out DateTime startedUtc))
        {
            startedUtc = DateTime.UtcNow;
        }

        if ((DateTime.UtcNow - startedUtc).TotalSeconds > EnterPlayModeGridWaitMaxSeconds)
        {
            string failJson = BuildPlayModeBridgeResponseJson(
                cmdId,
                ok: false,
                storage: "play_mode",
                error: "GridManager did not finish initializing within 24 seconds; check Play Mode / scene load errors and retry enter_play_mode.",
                playModeState: "play_mode_loading",
                ready: false,
                alreadyPlaying: false,
                alreadyStopped: false,
                hasGridDimensions: false,
                gridWidth: 0,
                gridHeight: 0);
            ClearEnterPlaySessionState();
            CompleteOrFail(repoRoot, cmdId, failJson);
            return;
        }

        if (!EditorApplication.isPlaying)
        {
            string failJson = BuildPlayModeBridgeResponseJson(
                cmdId,
                ok: false,
                storage: "play_mode",
                error: "Play Mode was not active before GridManager initialized; enter_play_mode was cancelled or failed.",
                playModeState: "edit_mode",
                ready: false,
                alreadyPlaying: false,
                alreadyStopped: false,
                hasGridDimensions: false,
                gridWidth: 0,
                gridHeight: 0);
            ClearEnterPlaySessionState();
            CompleteOrFail(repoRoot, cmdId, failJson);
            return;
        }

        GridManager grid = UnityEngine.Object.FindObjectOfType<GridManager>();
        if (grid != null && grid.isInitialized)
        {
            string okJson = BuildPlayModeBridgeResponseJson(
                cmdId,
                ok: true,
                storage: "play_mode",
                error: string.Empty,
                playModeState: "play_mode_ready",
                ready: true,
                alreadyPlaying: false,
                alreadyStopped: false,
                hasGridDimensions: true,
                gridWidth: grid.width,
                gridHeight: grid.height);
            ClearEnterPlaySessionState();
            CompleteOrFail(repoRoot, cmdId, okJson);
        }
    }

    static void PumpExitPlayModeWait()
    {
        string cmdId = SessionState.GetString(SessionExitCommandIdKey, string.Empty);
        if (string.IsNullOrEmpty(cmdId))
            return;

        string repoRoot = SessionState.GetString(SessionExitRepoRootKey, string.Empty);
        if (string.IsNullOrEmpty(repoRoot))
        {
            ClearExitPlaySessionState();
            return;
        }

        if (EditorApplication.isPlaying)
            return;

        string okJson = BuildPlayModeBridgeResponseJson(
            cmdId,
            ok: true,
            storage: "play_mode",
            error: string.Empty,
            playModeState: "edit_mode",
            ready: false,
            alreadyPlaying: false,
            alreadyStopped: false,
            hasGridDimensions: false,
            gridWidth: 0,
            gridHeight: 0);
        ClearExitPlaySessionState();
        CompleteOrFail(repoRoot, cmdId, okJson);
    }

    static void RunEnterPlayMode(string repoRoot, string commandId)
    {
        string pendingEnter = SessionState.GetString(SessionEnterCommandIdKey, string.Empty);
        if (!string.IsNullOrEmpty(pendingEnter) && !string.Equals(pendingEnter, commandId, StringComparison.Ordinal))
        {
            TryFinalizeFailed(
                repoRoot,
                commandId,
                "Another enter_play_mode request is still waiting for GridManager; wait for it to complete before enqueueing a new one.");
            return;
        }

        if (!string.IsNullOrEmpty(SessionState.GetString(SessionExitCommandIdKey, string.Empty)))
        {
            TryFinalizeFailed(
                repoRoot,
                commandId,
                "exit_play_mode is in progress; wait for it to complete before enter_play_mode.");
            return;
        }

        if (EditorApplication.isPlaying)
        {
            GridManager grid = UnityEngine.Object.FindObjectOfType<GridManager>();
            if (grid != null && grid.isInitialized)
            {
                string json = BuildPlayModeBridgeResponseJson(
                    commandId,
                    ok: true,
                    storage: "play_mode",
                    error: string.Empty,
                    playModeState: "play_mode_ready",
                    ready: true,
                    alreadyPlaying: true,
                    alreadyStopped: false,
                    hasGridDimensions: true,
                    gridWidth: grid.width,
                    gridHeight: grid.height);
                CompleteOrFail(repoRoot, commandId, json);
                return;
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
            string json = BuildPlayModeBridgeResponseJson(
                commandId,
                ok: true,
                storage: "play_mode",
                error: string.Empty,
                playModeState: "edit_mode",
                ready: false,
                alreadyPlaying: false,
                alreadyStopped: true,
                hasGridDimensions: false,
                gridWidth: 0,
                gridHeight: 0);
            CompleteOrFail(repoRoot, commandId, json);
            return;
        }

        if (!string.IsNullOrEmpty(SessionState.GetString(SessionExitCommandIdKey, string.Empty)))
        {
            TryFinalizeFailed(
                repoRoot,
                commandId,
                "Another exit_play_mode request is already in progress.");
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
            string json = BuildPlayModeBridgeResponseJson(
                commandId,
                ok: true,
                storage: "play_mode",
                error: string.Empty,
                playModeState: "edit_mode",
                ready: false,
                alreadyPlaying: false,
                alreadyStopped: false,
                hasGridDimensions: false,
                gridWidth: 0,
                gridHeight: 0);
            CompleteOrFail(repoRoot, commandId, json);
            return;
        }

        GridManager grid = UnityEngine.Object.FindObjectOfType<GridManager>();
        bool init = grid != null && grid.isInitialized;
        string state = init ? "play_mode_ready" : "play_mode_loading";
        string jsonPlaying = BuildPlayModeBridgeResponseJson(
            commandId,
            ok: true,
            storage: "play_mode",
            error: string.Empty,
            playModeState: state,
            ready: init,
            alreadyPlaying: false,
            alreadyStopped: false,
            hasGridDimensions: init,
            gridWidth: init ? grid.width : 0,
            gridHeight: init ? grid.height : 0);
        CompleteOrFail(repoRoot, commandId, jsonPlaying);
    }

    /// <summary>
    /// Synchronous compilation snapshot for IDE agents → <see cref="EditorApplication.isCompiling"/>,
    /// <see cref="EditorUtility.scriptCompilationFailed"/>, + recent buffered Console error lines.
    /// </summary>
    static void RunGetCompilationStatus(string repoRoot, string commandId, string requestJson)
    {
        if (!TryParseRequestEnvelope(requestJson, out AgentBridgeRequestEnvelopeDto env, out string parseErr))
        {
            TryFinalizeFailed(repoRoot, commandId, parseErr);
            return;
        }

        if (!string.Equals(env.kind, "get_compilation_status", StringComparison.OrdinalIgnoreCase))
        {
            TryFinalizeFailed(repoRoot, commandId, "Request kind mismatch for get_compilation_status.");
            return;
        }

        bool compiling = EditorApplication.isCompiling;
        bool compilationFailed = EditorUtility.scriptCompilationFailed;

        List<AgentBridgeConsoleBuffer.LogEntry> rawErrors = AgentBridgeConsoleBuffer.Query(
            null,
            "error",
            null,
            30);

        const int maxMessages = 20;
        var messageDtos = new List<AgentBridgeLogLineDto>(Math.Min(rawErrors.Count, maxMessages));
        for (int i = 0; i < rawErrors.Count && messageDtos.Count < maxMessages; i++)
        {
            AgentBridgeConsoleBuffer.LogEntry e = rawErrors[i];
            messageDtos.Add(
                new AgentBridgeLogLineDto
                {
                    timestamp_utc = new DateTime(e.utcTicks, DateTimeKind.Utc).ToString("o"),
                    severity = e.severity ?? "error",
                    message = e.message ?? string.Empty,
                    stack = e.stack ?? string.Empty,
                });
        }

        string excerpt = string.Empty;
        if (messageDtos.Count > 0)
        {
            excerpt = messageDtos[0].message ?? string.Empty;
            if (excerpt.Length > 500)
                excerpt = excerpt.Substring(0, 500);
        }

        var compilation = new AgentBridgeCompilationStatusDto
        {
            compiling = compiling,
            compilation_failed = compilationFailed,
            last_error_excerpt = excerpt,
            recent_error_messages = messageDtos.ToArray(),
        };

        var resp = new AgentBridgeResponseFileDto
        {
            schema_version = 1,
            artifact = "unity_agent_bridge_response",
            command_id = commandId,
            ok = true,
            completed_at_utc = DateTime.UtcNow.ToString("o"),
            storage = "compilation_status",
            postgres_only = false,
            error = string.Empty,
            artifact_paths = Array.Empty<string>(),
            log_lines = Array.Empty<AgentBridgeLogLineDto>(),
            play_mode_state = string.Empty,
            ready = false,
            already_playing = false,
            already_stopped = false,
            has_grid_dimensions = false,
            grid_width = 0,
            grid_height = 0,
            compilation_status = compilation,
        };

        string json = JsonUtility.ToJson(resp, true);
        CompleteOrFail(repoRoot, commandId, json);
    }

    static string BuildPlayModeBridgeResponseJson(
        string commandId,
        bool ok,
        string storage,
        string error,
        string playModeState,
        bool ready,
        bool alreadyPlaying,
        bool alreadyStopped,
        bool hasGridDimensions,
        int gridWidth,
        int gridHeight)
    {
        var resp = new AgentBridgeResponseFileDto
        {
            schema_version = 1,
            artifact = "unity_agent_bridge_response",
            command_id = commandId,
            ok = ok,
            completed_at_utc = DateTime.UtcNow.ToString("o"),
            storage = storage,
            postgres_only = false,
            error = ok ? string.Empty : (error ?? string.Empty),
            artifact_paths = Array.Empty<string>(),
            log_lines = Array.Empty<AgentBridgeLogLineDto>(),
            play_mode_state = playModeState ?? string.Empty,
            ready = ready,
            already_playing = alreadyPlaying,
            already_stopped = alreadyStopped,
            has_grid_dimensions = hasGridDimensions,
            grid_width = gridWidth,
            grid_height = gridHeight,
        };
        return JsonUtility.ToJson(resp, true);
    }

    static void RunExportAgentContext(string repoRoot, string commandId, string requestJson)
    {
        int? seedX = null;
        int? seedY = null;
        if (TryParseRequestEnvelope(requestJson, out AgentBridgeRequestEnvelopeDto env, out _) &&
            env != null &&
            string.Equals(env.kind, "export_agent_context", StringComparison.OrdinalIgnoreCase) &&
            env.bridge_params != null &&
            !string.IsNullOrWhiteSpace(env.bridge_params.seed_cell))
        {
            string raw = env.bridge_params.seed_cell.Trim();
            int comma = raw.IndexOf(',');
            if (comma > 0 && comma < raw.Length - 1)
            {
                string xs = raw.Substring(0, comma).Trim();
                string ys = raw.Substring(comma + 1).Trim();
                if (int.TryParse(xs, NumberStyles.Integer, CultureInfo.InvariantCulture, out int px) &&
                    int.TryParse(ys, NumberStyles.Integer, CultureInfo.InvariantCulture, out int py))
                {
                    seedX = px;
                    seedY = py;
                }
            }
        }

        AgentBridgeAgentContextOutcome outcome = AgentDiagnosticsReportsMenu.ExportAgentContextForAgentBridge(
            seedX,
            seedY,
            writeBridgeArtifactFile: true);
        string responseJson = BuildAgentContextResponseJson(commandId, outcome);
        CompleteOrFail(repoRoot, commandId, responseJson);
    }

    static void RunExportCellChunk(string repoRoot, string commandId, string requestJson)
    {
        if (!TryParseRequestEnvelope(requestJson, out AgentBridgeRequestEnvelopeDto env, out string parseErr))
        {
            TryFinalizeFailed(repoRoot, commandId, parseErr);
            return;
        }

        if (!EditorApplication.isPlaying)
        {
            TryFinalizeFailed(repoRoot, commandId,
                "export_cell_chunk requires Play Mode with an initialized grid. Use enter_play_mode first.");
            return;
        }

        GridManager grid = UnityEngine.Object.FindObjectOfType<GridManager>();
        if (grid == null || !grid.isInitialized)
        {
            TryFinalizeFailed(repoRoot, commandId,
                "Grid not initialized — wait for play_mode_ready after enter_play_mode before calling export_cell_chunk.");
            return;
        }

        AgentBridgeParamsPayloadDto p = env.bridge_params ?? new AgentBridgeParamsPayloadDto();
        int x0 = p.origin_x;
        int y0 = p.origin_y;
        int w = p.chunk_width > 0 ? p.chunk_width : InterchangeJsonReportsMenu.DefaultChunkWidth;
        int h = p.chunk_height > 0 ? p.chunk_height : InterchangeJsonReportsMenu.DefaultChunkHeight;

        AgentBridgeCellChunkOutcome outcome = InterchangeJsonReportsMenu.ExportCellChunkForAgentBridge(x0, y0, w, h);

        string responseJson = BuildObservabilityResponseJson(
            commandId,
            ok: outcome.Success,
            storage: "postgres",
            postgresOnly: true,
            artifactPaths: Array.Empty<string>(),
            error: outcome.Success ? string.Empty : outcome.ErrorMessage,
            logLines: Array.Empty<AgentBridgeLogLineDto>());
        CompleteOrFail(repoRoot, commandId, responseJson);
    }

    static void RunExportSortingDebug(string repoRoot, string commandId, string requestJson)
    {
        if (!TryParseRequestEnvelope(requestJson, out AgentBridgeRequestEnvelopeDto env, out string parseErr))
        {
            TryFinalizeFailed(repoRoot, commandId, parseErr);
            return;
        }

        if (!EditorApplication.isPlaying)
        {
            TryFinalizeFailed(repoRoot, commandId,
                "export_sorting_debug requires Play Mode with an initialized grid. Use enter_play_mode first.");
            return;
        }

        GridManager grid = UnityEngine.Object.FindObjectOfType<GridManager>();
        if (grid == null || !grid.isInitialized)
        {
            TryFinalizeFailed(repoRoot, commandId,
                "Grid not initialized — wait for play_mode_ready after enter_play_mode before calling export_sorting_debug.");
            return;
        }

        TerrainManager terrain = grid.terrainManager;
        if (terrain == null)
        {
            TryFinalizeFailed(repoRoot, commandId,
                "TerrainManager not available — GridManager.terrainManager is null; cannot produce sorting debug export.");
            return;
        }

        AgentBridgeParamsPayloadDto p = env.bridge_params ?? new AgentBridgeParamsPayloadDto();
        int? seedX = null;
        int? seedY = null;
        if (!string.IsNullOrWhiteSpace(p.seed_cell))
        {
            if (TryParseSeedCellString(p.seed_cell, out int sx, out int sy))
            {
                seedX = sx;
                seedY = sy;
            }
        }

        AgentBridgeSortingDebugOutcome outcome = AgentDiagnosticsReportsMenu.ExportSortingDebugForAgentBridge(seedX, seedY);

        string responseJson = BuildObservabilityResponseJson(
            commandId,
            ok: outcome.Success,
            storage: "postgres",
            postgresOnly: true,
            artifactPaths: Array.Empty<string>(),
            error: outcome.Success ? string.Empty : outcome.ErrorMessage,
            logLines: Array.Empty<AgentBridgeLogLineDto>());
        CompleteOrFail(repoRoot, commandId, responseJson);
    }

    static void RunGetConsoleLogs(string repoRoot, string commandId, string requestJson)
    {
        if (!TryParseRequestEnvelope(requestJson, out AgentBridgeRequestEnvelopeDto env, out string parseErr))
        {
            TryFinalizeFailed(repoRoot, commandId, parseErr);
            return;
        }

        if (!string.Equals(env.kind, "get_console_logs", StringComparison.Ordinal))
        {
            TryFinalizeFailed(repoRoot, commandId, "Request kind mismatch for get_console_logs.");
            return;
        }

        AgentBridgeParamsPayloadDto p = env.bridge_params ?? new AgentBridgeParamsPayloadDto();
        int maxLines = p.max_lines > 0 ? p.max_lines : 200;
        if (maxLines > 2000)
            maxLines = 2000;

        DateTime? sinceUtc = null;
        if (!string.IsNullOrWhiteSpace(p.since_utc))
        {
            if (!DateTime.TryParse(
                    p.since_utc,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                    out DateTime parsed))
            {
                TryFinalizeFailed(repoRoot, commandId, "Invalid since_utc; use ISO-8601 UTC.");
                return;
            }

            sinceUtc = parsed;
        }

        string severityFilter = string.IsNullOrWhiteSpace(p.severity_filter) ? "all" : p.severity_filter.Trim();
        string tagFilter = string.IsNullOrWhiteSpace(p.tag_filter) ? null : p.tag_filter;

        List<AgentBridgeConsoleBuffer.LogEntry> lines = AgentBridgeConsoleBuffer.Query(
            sinceUtc,
            severityFilter,
            tagFilter,
            maxLines);

        var dtos = new AgentBridgeLogLineDto[lines.Count];
        for (int i = 0; i < lines.Count; i++)
        {
            AgentBridgeConsoleBuffer.LogEntry e = lines[i];
            dtos[i] = new AgentBridgeLogLineDto
            {
                timestamp_utc = new DateTime(e.utcTicks, DateTimeKind.Utc).ToString("o"),
                severity = e.severity ?? "log",
                message = e.message ?? string.Empty,
                stack = e.stack ?? string.Empty,
            };
        }

        string responseJson = BuildObservabilityResponseJson(
            commandId,
            ok: true,
            storage: "console_buffer",
            postgresOnly: false,
            artifactPaths: Array.Empty<string>(),
            error: string.Empty,
            logLines: dtos);
        CompleteOrFail(repoRoot, commandId, responseJson);
    }

    static void RunCaptureScreenshot(string repoRoot, string commandId, string requestJson)
    {
        if (!TryParseRequestEnvelope(requestJson, out AgentBridgeRequestEnvelopeDto env, out string parseErr))
        {
            TryFinalizeFailed(repoRoot, commandId, parseErr);
            return;
        }

        if (!string.Equals(env.kind, "capture_screenshot", StringComparison.Ordinal))
        {
            TryFinalizeFailed(repoRoot, commandId, "Request kind mismatch for capture_screenshot.");
            return;
        }

        AgentBridgeParamsPayloadDto p = env.bridge_params ?? new AgentBridgeParamsPayloadDto();
        string camera = p.camera ?? string.Empty;
        string stem = p.filename_stem ?? string.Empty;
        bool includeUi = p.include_ui;

        if (!AgentBridgeScreenshotCapture.TryBeginCapture(
                repoRoot,
                camera,
                stem,
                includeUi,
                out string relPath,
                out string absPath,
                out bool deferredToNextEditorFrame,
                out string err))
        {
            string responseJson = BuildObservabilityResponseJson(
                commandId,
                ok: false,
                storage: "filesystem",
                postgresOnly: false,
                artifactPaths: Array.Empty<string>(),
                error: err ?? "Screenshot failed.",
                logLines: Array.Empty<AgentBridgeLogLineDto>());
            CompleteOrFail(repoRoot, commandId, responseJson);
            return;
        }

        if (deferredToNextEditorFrame)
        {
            ScheduleDeferredScreenshotComplete(repoRoot, commandId, absPath, relPath, null);
            return;
        }

        string responseOk = BuildObservabilityResponseJson(
            commandId,
            ok: true,
            storage: "filesystem",
            postgresOnly: false,
            artifactPaths: new[] { relPath },
            error: string.Empty,
            logLines: Array.Empty<AgentBridgeLogLineDto>());
        CompleteOrFail(repoRoot, commandId, responseOk);
    }

    static void ScheduleDeferredScreenshotComplete(
        string repoRoot,
        string commandId,
        string absolutePath,
        string repoRelativePath,
        DebugBundleCompletionStash debugBundleOrNull)
    {
        s_deferredScreenshots.Add(
            new DeferredScreenshotWork
            {
                RepoRoot = repoRoot,
                CommandId = commandId,
                AbsolutePath = absolutePath,
                RepoRelativePath = repoRelativePath,
                StartedUtc = DateTime.UtcNow,
                DebugBundle = debugBundleOrNull,
            });
    }

    static void PumpDeferredScreenshotCompletions()
    {
        for (int i = s_deferredScreenshots.Count - 1; i >= 0; i--)
        {
            DeferredScreenshotWork w = s_deferredScreenshots[i];
            bool exists = File.Exists(w.AbsolutePath);
            double elapsedSec = (DateTime.UtcNow - w.StartedUtc).TotalSeconds;
            if (!exists && elapsedSec < DeferredScreenshotMaxWaitSeconds)
                continue;

            s_deferredScreenshots.RemoveAt(i);
            if (w.DebugBundle != null)
            {
                bool screenshotOk = exists;
                string shotPath = exists ? w.RepoRelativePath : string.Empty;
                string shotErr = exists
                    ? string.Empty
                    : "Screenshot file was not created within 15 seconds; keep the Game view visible and retry debug_context_bundle in Play Mode.";
                string bundleJson = BuildDebugBundleResponseJsonFromStash(
                    w.CommandId,
                    w.DebugBundle,
                    screenshotRelPath: shotPath,
                    screenshotOk: screenshotOk,
                    screenshotSkipped: false,
                    screenshotError: shotErr);
                CompleteOrFail(w.RepoRoot, w.CommandId, bundleJson);
                continue;
            }

            if (exists)
            {
                string okJson = BuildObservabilityResponseJson(
                    w.CommandId,
                    ok: true,
                    storage: "filesystem",
                    postgresOnly: false,
                    artifactPaths: new[] { w.RepoRelativePath },
                    error: string.Empty,
                    logLines: Array.Empty<AgentBridgeLogLineDto>());
                CompleteOrFail(w.RepoRoot, w.CommandId, okJson);
            }
            else
            {
                string failJson = BuildObservabilityResponseJson(
                    w.CommandId,
                    ok: false,
                    storage: "filesystem",
                    postgresOnly: false,
                    artifactPaths: Array.Empty<string>(),
                    error:
                    "Screenshot file was not created within 15 seconds; keep the Game view visible and retry capture_screenshot in Play Mode.",
                    logLines: Array.Empty<AgentBridgeLogLineDto>());
                CompleteOrFail(w.RepoRoot, w.CommandId, failJson);
            }
        }
    }

    static void RunDebugContextBundle(string repoRoot, string commandId, string requestJson)
    {
        if (!TryParseRequestEnvelope(requestJson, out AgentBridgeRequestEnvelopeDto env, out string parseErr))
        {
            TryFinalizeFailed(repoRoot, commandId, parseErr);
            return;
        }

        if (!string.Equals(env.kind, "debug_context_bundle", StringComparison.Ordinal))
        {
            TryFinalizeFailed(repoRoot, commandId, "Request kind mismatch for debug_context_bundle.");
            return;
        }

        if (!EditorApplication.isPlaying)
        {
            TryFinalizeFailed(repoRoot, commandId, "debug_context_bundle requires Play Mode.");
            return;
        }

        GridManager grid = UnityEngine.Object.FindObjectOfType<GridManager>();
        if (grid == null || !grid.isInitialized)
        {
            TryFinalizeFailed(
                repoRoot,
                commandId,
                "debug_context_bundle requires an initialized GridManager in Play Mode (wait for play_mode_ready after enter_play_mode).");
            return;
        }

        AgentBridgeParamsPayloadDto p = env.bridge_params ?? new AgentBridgeParamsPayloadDto();
        string seedRaw = string.IsNullOrWhiteSpace(p.seed_cell) ? string.Empty : p.seed_cell.Trim();
        if (!TryParseSeedCellString(seedRaw, out int seedX, out int seedY))
        {
            TryFinalizeFailed(
                repoRoot,
                commandId,
                "debug_context_bundle requires bridge_params.seed_cell as \"x,y\" (e.g. \"62,0\").");
            return;
        }

        bool includeScreenshot = RequestJsonContainsParamKey(requestJson, "include_screenshot")
            ? p.include_screenshot
            : true;
        bool includeConsole = RequestJsonContainsParamKey(requestJson, "include_console")
            ? p.include_console
            : true;
        bool includeAnomalyScan = RequestJsonContainsParamKey(requestJson, "include_anomaly_scan")
            ? p.include_anomaly_scan
            : true;

        AgentBridgeAgentContextOutcome outcome = AgentDiagnosticsReportsMenu.ExportAgentContextForAgentBridge(
            seedX,
            seedY,
            writeBridgeArtifactFile: true);

        AgentBridgeLogLineDto[] consoleLines = Array.Empty<AgentBridgeLogLineDto>();
        if (includeConsole)
        {
            int maxLines = p.max_lines > 0 ? p.max_lines : 200;
            if (maxLines > 2000)
                maxLines = 2000;

            DateTime? sinceUtc = null;
            if (!string.IsNullOrWhiteSpace(p.since_utc))
            {
                if (!DateTime.TryParse(
                        p.since_utc,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                        out DateTime parsed))
                {
                    TryFinalizeFailed(repoRoot, commandId, "Invalid since_utc; use ISO-8601 UTC.");
                    return;
                }

                sinceUtc = parsed;
            }

            string severityFilter = string.IsNullOrWhiteSpace(p.severity_filter) ? "all" : p.severity_filter.Trim();
            string tagFilter = string.IsNullOrWhiteSpace(p.tag_filter) ? null : p.tag_filter;

            List<AgentBridgeConsoleBuffer.LogEntry> lines = AgentBridgeConsoleBuffer.Query(
                sinceUtc,
                severityFilter,
                tagFilter,
                maxLines);

            consoleLines = new AgentBridgeLogLineDto[lines.Count];
            for (int i = 0; i < lines.Count; i++)
            {
                AgentBridgeConsoleBuffer.LogEntry e = lines[i];
                consoleLines[i] = new AgentBridgeLogLineDto
                {
                    timestamp_utc = new DateTime(e.utcTicks, DateTimeKind.Utc).ToString("o"),
                    severity = e.severity ?? "log",
                    message = e.message ?? string.Empty,
                    stack = e.stack ?? string.Empty,
                };
            }
        }

        AgentBridgeAnomalyRecordDto[] anomalies = Array.Empty<AgentBridgeAnomalyRecordDto>();
        if (includeAnomalyScan)
        {
            List<AgentBridgeAnomalyRecordDto> scan = AgentBridgeAnomalyScanner.ScanNeighborhood(seedX, seedY);
            anomalies = scan.ToArray();
        }

        var stash = new DebugBundleCompletionStash
        {
            CellExportRelPath = outcome.ArtifactPathRepoRelative ?? string.Empty,
            CellExportOk = outcome.Success,
            CellExportError = outcome.ErrorMessage ?? string.Empty,
            ConsoleLines = consoleLines,
            ConsoleSkipped = !includeConsole,
            Anomalies = anomalies,
            AnomalyScanSkipped = !includeAnomalyScan,
            ScreenshotIncluded = includeScreenshot,
        };

        if (!includeScreenshot)
        {
            string json = BuildDebugBundleResponseJsonFromStash(
                commandId,
                stash,
                screenshotRelPath: string.Empty,
                screenshotOk: true,
                screenshotSkipped: true,
                screenshotError: string.Empty);
            CompleteOrFail(repoRoot, commandId, json);
            return;
        }

        string stem = string.IsNullOrWhiteSpace(p.filename_stem) ? "debug-bundle" : p.filename_stem.Trim();
        if (!AgentBridgeScreenshotCapture.TryBeginCapture(
                repoRoot,
                string.Empty,
                stem,
                includeGameViewWithOverlayUi: true,
                out string relPath,
                out string absPath,
                out bool deferredToNextEditorFrame,
                out string err))
        {
            string failBundle = BuildDebugBundleResponseJsonFromStash(
                commandId,
                stash,
                screenshotRelPath: string.Empty,
                screenshotOk: false,
                screenshotSkipped: false,
                screenshotError: err ?? "Screenshot failed.");
            CompleteOrFail(repoRoot, commandId, failBundle);
            return;
        }

        if (deferredToNextEditorFrame)
        {
            ScheduleDeferredScreenshotComplete(repoRoot, commandId, absPath, relPath, stash);
            return;
        }

        string okJson = BuildDebugBundleResponseJsonFromStash(
            commandId,
            stash,
            screenshotRelPath: relPath ?? string.Empty,
            screenshotOk: true,
            screenshotSkipped: false,
            screenshotError: string.Empty);
        CompleteOrFail(repoRoot, commandId, okJson);
    }

    static bool RequestJsonContainsParamKey(string requestJson, string key)
    {
        if (string.IsNullOrEmpty(requestJson) || string.IsNullOrEmpty(key))
            return false;
        return requestJson.IndexOf($"\"{key}\"", StringComparison.Ordinal) >= 0;
    }

    static bool TryParseSeedCellString(string raw, out int x, out int y)
    {
        x = 0;
        y = 0;
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        string trimmed = raw.Trim();
        int comma = trimmed.IndexOf(',');
        if (comma <= 0 || comma >= trimmed.Length - 1)
            return false;
        string xs = trimmed.Substring(0, comma).Trim();
        string ys = trimmed.Substring(comma + 1).Trim();
        return int.TryParse(xs, NumberStyles.Integer, CultureInfo.InvariantCulture, out x) &&
               int.TryParse(ys, NumberStyles.Integer, CultureInfo.InvariantCulture, out y);
    }

    static string BuildDebugBundleResponseJsonFromStash(
        string commandId,
        DebugBundleCompletionStash s,
        string screenshotRelPath,
        bool screenshotOk,
        bool screenshotSkipped,
        string screenshotError)
    {
        bool overallOk = s.CellExportOk;
        if (s.ScreenshotIncluded && !screenshotSkipped && !screenshotOk)
            overallOk = false;

        string error = string.Empty;
        if (!overallOk)
        {
            if (!s.CellExportOk && !string.IsNullOrEmpty(s.CellExportError))
                error = s.CellExportError;
            else if (s.ScreenshotIncluded && !screenshotSkipped && !screenshotOk && !string.IsNullOrEmpty(screenshotError))
                error = screenshotError;
            else if (!s.CellExportOk)
                error = "CityCell export failed.";
            else
                error = "debug_context_bundle completed with errors.";
        }

        var bundle = new AgentBridgeBundleDto
        {
            cell_export = new AgentBridgeBundleCellExportDto
            {
                artifact_path = s.CellExportRelPath ?? string.Empty,
                ok = s.CellExportOk,
            },
            screenshot = new AgentBridgeBundleScreenshotDto
            {
                artifact_path = screenshotRelPath ?? string.Empty,
                ok = screenshotSkipped || screenshotOk,
                skipped = screenshotSkipped,
            },
            console = new AgentBridgeBundleConsoleDto
            {
                log_lines = s.ConsoleLines ?? Array.Empty<AgentBridgeLogLineDto>(),
                line_count = s.ConsoleLines != null ? s.ConsoleLines.Length : 0,
                skipped = s.ConsoleSkipped,
            },
            anomalies = s.Anomalies ?? Array.Empty<AgentBridgeAnomalyRecordDto>(),
            anomaly_count = s.Anomalies != null ? s.Anomalies.Length : 0,
            anomaly_scan_skipped = s.AnomalyScanSkipped,
        };

        var resp = new AgentBridgeResponseFileDto
        {
            schema_version = 1,
            artifact = "unity_agent_bridge_response",
            command_id = commandId,
            ok = overallOk,
            completed_at_utc = DateTime.UtcNow.ToString("o"),
            storage = "debug_context_bundle",
            postgres_only = false,
            error = error,
            artifact_paths = Array.Empty<string>(),
            log_lines = Array.Empty<AgentBridgeLogLineDto>(),
            play_mode_state = string.Empty,
            ready = false,
            already_playing = false,
            already_stopped = false,
            has_grid_dimensions = false,
            grid_width = 0,
            grid_height = 0,
            bundle = bundle,
        };

        return JsonUtility.ToJson(resp, true);
    }

    // ── economy_balance_snapshot ──────────────────────────────────────────

    static void RunEconomyBalanceSnapshot(string repoRoot, string commandId, string requestJson)
    {
        if (!TryParseRequestEnvelope(requestJson, out AgentBridgeRequestEnvelopeDto env, out string parseErr))
        {
            TryFinalizeFailed(repoRoot, commandId, parseErr);
            return;
        }

        var economyMgr = UnityEngine.Object.FindObjectOfType<Territory.Economy.EconomyManager>();
        var cityStats = UnityEngine.Object.FindObjectOfType<Territory.Economy.CityStats>();
        var demandMgr = UnityEngine.Object.FindObjectOfType<Territory.Economy.DemandManager>();

        if (cityStats == null)
        {
            TryFinalizeFailed(repoRoot, commandId, "CityStats not found in scene. Is a game loaded?");
            return;
        }

        var snapshot = new AgentBridgeEconomySnapshotDto
        {
            population = cityStats.population,
            happiness = cityStats.happiness,
            pollution = cityStats.pollution,
            money = cityStats.money,
            residential_tax = economyMgr != null ? economyMgr.residentialIncomeTax : 0,
            commercial_tax = economyMgr != null ? economyMgr.commercialIncomeTax : 0,
            industrial_tax = economyMgr != null ? economyMgr.industrialIncomeTax : 0,
            residential_building_count = cityStats.residentialBuildingCount,
            commercial_building_count = cityStats.commercialBuildingCount,
            industrial_building_count = cityStats.industrialBuildingCount,
        };

        if (demandMgr != null)
        {
            snapshot.residential_demand = demandMgr.residentialDemand?.demandLevel ?? 0f;
            snapshot.commercial_demand = demandMgr.commercialDemand?.demandLevel ?? 0f;
            snapshot.industrial_demand = demandMgr.industrialDemand?.demandLevel ?? 0f;
        }

        var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "economy_balance_snapshot");
        resp.economy_snapshot = snapshot;

        CompleteOrFail(repoRoot, commandId, JsonUtility.ToJson(resp, true));
    }

    // ── prefab_manifest ─────────────────────────────────────────────────

    static void RunPrefabManifest(string repoRoot, string commandId, string requestJson)
    {
        if (!TryParseRequestEnvelope(requestJson, out AgentBridgeRequestEnvelopeDto env, out string parseErr))
        {
            TryFinalizeFailed(repoRoot, commandId, parseErr);
            return;
        }

        var manifestEntries = new List<AgentBridgePrefabManifestEntryDto>();

        var allBehaviours = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true);
        foreach (var mb in allBehaviours)
        {
            if (mb == null) continue;
            string goPath = GetGameObjectPath(mb.gameObject);
            manifestEntries.Add(new AgentBridgePrefabManifestEntryDto
            {
                game_object_path = goPath,
                component_type = mb.GetType().Name,
                is_missing_script = false,
            });
        }

        var allGameObjects = UnityEngine.Object.FindObjectsOfType<GameObject>(true);
        foreach (var go in allGameObjects)
        {
            var components = go.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp == null)
                {
                    manifestEntries.Add(new AgentBridgePrefabManifestEntryDto
                    {
                        game_object_path = GetGameObjectPath(go),
                        component_type = "(missing script)",
                        is_missing_script = true,
                    });
                }
            }
        }

        var manifest = new AgentBridgePrefabManifestDto
        {
            total_monobehaviours = allBehaviours.Length,
            missing_script_count = manifestEntries.FindAll(e => e.is_missing_script).Count,
            entries = manifestEntries.ToArray(),
        };

        var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "prefab_manifest");
        resp.prefab_manifest = manifest;

        CompleteOrFail(repoRoot, commandId, JsonUtility.ToJson(resp, true));
    }

    static string GetGameObjectPath(GameObject go)
    {
        string path = go.name;
        Transform parent = go.transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }

    // ── sorting_order_debug ─────────────────────────────────────────────

    static void RunSortingOrderDebug(string repoRoot, string commandId, string requestJson)
    {
        if (!TryParseRequestEnvelope(requestJson, out AgentBridgeRequestEnvelopeDto env, out string parseErr))
        {
            TryFinalizeFailed(repoRoot, commandId, parseErr);
            return;
        }

        string seedCell = env.bridge_params?.seed_cell ?? string.Empty;
        if (string.IsNullOrWhiteSpace(seedCell))
        {
            TryFinalizeFailed(repoRoot, commandId, "seed_cell is required for sorting_order_debug (e.g. \"3,0\").");
            return;
        }

        string[] parts = seedCell.Split(',');
        if (parts.Length != 2 ||
            !int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int cellX) ||
            !int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int cellY))
        {
            TryFinalizeFailed(repoRoot, commandId, $"Invalid seed_cell format '{seedCell}'. Expected 'x,y' (e.g. '3,0').");
            return;
        }

        var gridMgr = UnityEngine.Object.FindObjectOfType<Territory.Core.GridManager>();
        if (gridMgr == null)
        {
            TryFinalizeFailed(repoRoot, commandId, "GridManager not found in scene.");
            return;
        }

        Territory.Core.CityCell cell = gridMgr.GetCell(cellX, cellY);
        if (cell == null)
        {
            TryFinalizeFailed(repoRoot, commandId, $"No cell at ({cellX},{cellY}).");
            return;
        }

        var rendererEntries = new List<AgentBridgeSortingRendererDto>();

        // Collect SpriteRenderers on the cell GameObject and children
        var spriteRenderers = cell.gameObject.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in spriteRenderers)
        {
            rendererEntries.Add(new AgentBridgeSortingRendererDto
            {
                name = sr.gameObject.name,
                sorting_layer = sr.sortingLayerName,
                sorting_order = sr.sortingOrder,
                sprite_name = sr.sprite != null ? sr.sprite.name : string.Empty,
                enabled = sr.enabled,
            });
        }

        var sortingDebug = new AgentBridgeSortingOrderDebugDto
        {
            cell_x = cellX,
            cell_y = cellY,
            cell_height = cell.height,
            renderer_count = rendererEntries.Count,
            renderers = rendererEntries.ToArray(),
        };

        var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "sorting_order_debug");
        resp.sorting_order_debug = sortingDebug;

        CompleteOrFail(repoRoot, commandId, JsonUtility.ToJson(resp, true));
    }

    static void CompleteOrFail(string repoRoot, string commandId, string responseJson)
    {
        if (!EditorPostgresBridgeJobs.TryCompleteSuccess(repoRoot, commandId, responseJson, out string completeLog))
        {
            Debug.LogWarning($"[AgentBridge] complete.mjs failed: {completeLog}");
            EditorPostgresBridgeJobs.TryCompleteFailed(
                repoRoot,
                commandId,
                "agent-bridge-complete.mjs failed after bridge command; see Unity Console for stderr.",
                out _);
        }
    }

    static void TryFinalizeFailed(string repoRoot, string commandId, string englishError)
    {
        if (!EditorPostgresBridgeJobs.TryCompleteFailed(repoRoot, commandId, englishError, out string log))
            Debug.LogWarning($"[AgentBridge] Could not mark job failed: {log}");
    }

    static bool TryParseRequestEnvelope(string requestJson, out AgentBridgeRequestEnvelopeDto env, out string error)
    {
        env = null;
        error = null;
        if (string.IsNullOrWhiteSpace(requestJson))
        {
            error = "Empty request_json.";
            return false;
        }

        string normalized = requestJson.Replace("\"params\":", "\"bridge_params\":", StringComparison.Ordinal);
        try
        {
            env = JsonUtility.FromJson<AgentBridgeRequestEnvelopeDto>(normalized);
        }
        catch (Exception ex)
        {
            error = $"Invalid request JSON: {ex.Message}";
            return false;
        }

        if (env == null || string.IsNullOrEmpty(env.kind))
        {
            error = "Missing kind in request.";
            return false;
        }

        return true;
    }

    static string BuildAgentContextResponseJson(string commandId, AgentBridgeAgentContextOutcome outcome)
    {
        var resp = new AgentBridgeResponseFileDto
        {
            schema_version = 1,
            artifact = "unity_agent_bridge_response",
            command_id = commandId,
            ok = outcome.Success,
            completed_at_utc = DateTime.UtcNow.ToString("o"),
            storage = outcome.Storage,
            postgres_only = outcome.PostgresOnly,
            error = outcome.Success ? string.Empty : outcome.ErrorMessage,
            log_lines = Array.Empty<AgentBridgeLogLineDto>(),
            play_mode_state = string.Empty,
            ready = false,
            already_playing = false,
            already_stopped = false,
            has_grid_dimensions = false,
            grid_width = 0,
            grid_height = 0,
        };

        if (outcome.Success && !string.IsNullOrEmpty(outcome.ArtifactPathRepoRelative))
            resp.artifact_paths = new[] { outcome.ArtifactPathRepoRelative };
        else
            resp.artifact_paths = Array.Empty<string>();

        return JsonUtility.ToJson(resp, true);
    }

    static string BuildObservabilityResponseJson(
        string commandId,
        bool ok,
        string storage,
        bool postgresOnly,
        string[] artifactPaths,
        string error,
        AgentBridgeLogLineDto[] logLines)
    {
        var resp = new AgentBridgeResponseFileDto
        {
            schema_version = 1,
            artifact = "unity_agent_bridge_response",
            command_id = commandId,
            ok = ok,
            completed_at_utc = DateTime.UtcNow.ToString("o"),
            storage = storage,
            postgres_only = postgresOnly,
            error = ok ? string.Empty : (error ?? string.Empty),
            artifact_paths = artifactPaths ?? Array.Empty<string>(),
            log_lines = logLines ?? Array.Empty<AgentBridgeLogLineDto>(),
            play_mode_state = string.Empty,
            ready = false,
            already_playing = false,
            already_stopped = false,
            has_grid_dimensions = false,
            grid_width = 0,
            grid_height = 0,
        };
        return JsonUtility.ToJson(resp, true);
    }
}

[Serializable]
class AgentBridgeRequestEnvelopeDto
{
    public int schema_version;
    public string artifact;
    public string command_id;
    public string kind;
    public string requested_at_utc;
    public AgentBridgeParamsPayloadDto bridge_params;
}

[Serializable]
class AgentBridgeParamsPayloadDto
{
    public string since_utc;
    public string severity_filter;
    public string tag_filter;
    public int max_lines;
    public string camera;
    public string filename_stem;
    public bool include_ui;
    /// <summary><c>export_agent_context</c> / <c>export_sorting_debug</c>: optional <c>"cellX,cellY"</c> Moore neighborhood seed (e.g. <c>3,0</c>).</summary>
    public string seed_cell;

    /// <summary><c>export_cell_chunk</c>: origin X coordinate (defaults to 0).</summary>
    public int origin_x;
    /// <summary><c>export_cell_chunk</c>: origin Y coordinate (defaults to 0).</summary>
    public int origin_y;
    /// <summary><c>export_cell_chunk</c>: chunk width (defaults to 8 when 0 or negative).</summary>
    public int chunk_width;
    /// <summary><c>export_cell_chunk</c>: chunk height (defaults to 8 when 0 or negative).</summary>
    public int chunk_height;

    /// <summary><c>debug_context_bundle</c>: JSON omits key → runner defaults <c>true</c>.</summary>
    public bool include_screenshot;

    /// <summary><c>debug_context_bundle</c>: JSON omits key → runner defaults <c>true</c>.</summary>
    public bool include_console;

    /// <summary><c>debug_context_bundle</c>: JSON omits key → runner defaults <c>true</c>.</summary>
    public bool include_anomaly_scan;

    /// <summary><c>catalog_preview</c>: catalog entity id (maps to catalog_entity.entity_id UUID).</summary>
    public string catalog_entry_id;
}

[Serializable]
class AgentBridgeLogLineDto
{
    public string timestamp_utc;
    public string severity;
    public string message;
    public string stack;
}

/// <summary>Populated for <c>get_compilation_status</c> → <see cref="AgentBridgeResponseFileDto.compilation_status"/>.</summary>
[Serializable]
class AgentBridgeCompilationStatusDto
{
    public bool compiling;

    public bool compilation_failed;

    /// <summary>Short excerpt from most recent buffered error line; truncated.</summary>
    public string last_error_excerpt;

    /// <summary>Up to 20 recent Console lines severity <c>error</c> from <see cref="AgentBridgeConsoleBuffer"/>.</summary>
    public AgentBridgeLogLineDto[] recent_error_messages;
}

[Serializable]
class AgentBridgeResponseFileDto
{
    public int schema_version;
    public string artifact;
    public string command_id;
    public bool ok;
    public string completed_at_utc;
    public string storage;
    public string[] artifact_paths;
    public bool postgres_only;
    public string error;
    public AgentBridgeLogLineDto[] log_lines;

    /// <summary>Populated for <c>enter_play_mode</c> / <c>exit_play_mode</c> / <c>get_play_mode_status</c> → <c>edit_mode</c> / <c>play_mode_loading</c> / <c>play_mode_ready</c>.</summary>
    public string play_mode_state;

    /// <summary>True → <see cref="GridManager"/> finished <c>InitializeGrid()</c>; play-mode bridge kinds only.</summary>
    public bool ready;

    public bool already_playing;
    public bool already_stopped;

    /// <summary>True → <see cref="grid_width"/> + <see cref="grid_height"/> valid.</summary>
    public bool has_grid_dimensions;

    public int grid_width;
    public int grid_height;

    /// <summary>Populated for <c>debug_context_bundle</c>.</summary>
    public AgentBridgeBundleDto bundle;

    /// <summary>Populated for <c>get_compilation_status</c>.</summary>
    public AgentBridgeCompilationStatusDto compilation_status;

    /// <summary>Populated for <c>economy_balance_snapshot</c>.</summary>
    public AgentBridgeEconomySnapshotDto economy_snapshot;

    /// <summary>Populated for <c>prefab_manifest</c>.</summary>
    public AgentBridgePrefabManifestDto prefab_manifest;

    /// <summary>Populated for <c>sorting_order_debug</c>.</summary>
    public AgentBridgeSortingOrderDebugDto sorting_order_debug;

    /// <summary>Populated for <c>catalog_preview</c>: JSON string with resolved, entry_id, screenshot_path.</summary>
    public string catalog_preview_result;

    /// <summary>Populated for <c>prefab_inspect</c> (Stage 12 Step 14.1): hierarchy + components + serialized fields.</summary>
    public AgentBridgePrefabInspectDto prefab_inspect_result;

    /// <summary>Populated for <c>ui_tree_walk</c> (Stage 12 Step 14.2): scene Canvas walk + screen-space rects.</summary>
    public AgentBridgeUiTreeWalkDto ui_tree_walk_result;

    /// <summary>
    /// Populated for mutation kinds (attach_component, remove_component, assign_serialized_field,
    /// create_gameobject, delete_gameobject, find_gameobject, set_transform, set_gameobject_active,
    /// set_gameobject_parent, save_scene, open_scene, new_scene, instantiate_prefab,
    /// apply_prefab_overrides, create_scriptable_object, modify_scriptable_object,
    /// refresh_asset_database, move_asset, delete_asset, execute_menu_item).
    /// JSON string carrying kind-specific result fields.
    /// </summary>
    public string mutation_result;

    /// <summary>Factory → response with all default fields pre-filled.</summary>
    public static AgentBridgeResponseFileDto CreateOk(string commandId, string storage)
    {
        return new AgentBridgeResponseFileDto
        {
            schema_version = 1,
            artifact = "unity_agent_bridge_response",
            command_id = commandId,
            ok = true,
            completed_at_utc = DateTime.UtcNow.ToString("o"),
            storage = storage,
            postgres_only = false,
            error = string.Empty,
            artifact_paths = Array.Empty<string>(),
            log_lines = Array.Empty<AgentBridgeLogLineDto>(),
            play_mode_state = string.Empty,
            ready = false,
            already_playing = false,
            already_stopped = false,
            has_grid_dimensions = false,
            grid_width = 0,
            grid_height = 0,
        };
    }
}

[Serializable]
class AgentBridgeBundleDto
{
    public AgentBridgeBundleCellExportDto cell_export;
    public AgentBridgeBundleScreenshotDto screenshot;
    public AgentBridgeBundleConsoleDto console;
    public AgentBridgeAnomalyRecordDto[] anomalies;
    public int anomaly_count;
    public bool anomaly_scan_skipped;
}

/// <summary>Populated for <c>economy_balance_snapshot</c> → response <c>economy_snapshot</c>.</summary>
[Serializable]
class AgentBridgeEconomySnapshotDto
{
    public int population;
    public float happiness;
    public float pollution;
    public int money;
    public int residential_tax;
    public int commercial_tax;
    public int industrial_tax;
    public int residential_building_count;
    public int commercial_building_count;
    public int industrial_building_count;
    public float residential_demand;
    public float commercial_demand;
    public float industrial_demand;
}

/// <summary>Populated for <c>prefab_manifest</c> → response <c>prefab_manifest</c>.</summary>
[Serializable]
class AgentBridgePrefabManifestDto
{
    public int total_monobehaviours;
    public int missing_script_count;
    public AgentBridgePrefabManifestEntryDto[] entries;
}

[Serializable]
class AgentBridgePrefabManifestEntryDto
{
    public string game_object_path;
    public string component_type;
    public bool is_missing_script;
}

/// <summary>Populated for <c>sorting_order_debug</c> → response <c>sorting_order_debug</c>.</summary>
[Serializable]
class AgentBridgeSortingOrderDebugDto
{
    public int cell_x;
    public int cell_y;
    public int cell_height;
    public int renderer_count;
    public AgentBridgeSortingRendererDto[] renderers;
}

[Serializable]
class AgentBridgeSortingRendererDto
{
    public string name;
    public string sorting_layer;
    public int sorting_order;
    public string sprite_name;
    public bool enabled;
}

[Serializable]
class AgentBridgeBundleCellExportDto
{
    public string artifact_path;
    public bool ok;
}

[Serializable]
class AgentBridgeBundleScreenshotDto
{
    public string artifact_path;
    public bool ok;
    public bool skipped;
}

[Serializable]
class AgentBridgeBundleConsoleDto
{
    public AgentBridgeLogLineDto[] log_lines;
    public int line_count;
    public bool skipped;
}

[Serializable]
public class AgentBridgeAnomalyRecordDto
{
    public string rule;
    public int cell_x;
    public int cell_y;
    public string severity;
    public string message;
}
