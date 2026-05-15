using System;
using System.Globalization;
using Territory.Core;
using Territory.Terrain;
using UnityEditor;
using UnityEngine;
using Territory.Core;
using Domains.Bridge.Services;

/// <summary>
/// Poll Postgres via <c>agent-bridge-dequeue.mjs</c> → dispatch bridge <c>kind</c> to existing
/// <b>Territory Developer → Reports</b> entry points or bridge helpers → complete jobs via <c>agent-bridge-complete.mjs</c>.
/// Requires <c>DATABASE_URL</c> — same as <see cref="EditorPostgresExportRegistrar"/>.
/// Play Mode bridge kinds use <see cref="SessionState"/> → completion survives domain reload.
/// Stage 6.1: bulk methods extracted to .PlayMode.cs / .Observability.cs / .PanelBlueprint.cs /
/// .CoreDtos.cs; response builders extracted to BridgeCommandService POCO.
/// </summary>
public static partial class AgentBridgeCommandRunner
{
    const int PollEveryNFrames = 30;

    const double EnterPlayModeGridWaitMaxSeconds = 24.0;
    const double DeferredScreenshotMaxWaitSeconds = 15.0;

    const string SessionEnterCommandIdKey  = "TerritoryDeveloper.AgentBridge.EnterPending.command_id";
    const string SessionEnterRepoRootKey   = "TerritoryDeveloper.AgentBridge.EnterPending.repo_root";
    const string SessionEnterStartedUtcKey = "TerritoryDeveloper.AgentBridge.EnterPending.started_utc";
    const string SessionExitCommandIdKey   = "TerritoryDeveloper.AgentBridge.ExitPending.command_id";
    const string SessionExitRepoRootKey    = "TerritoryDeveloper.AgentBridge.ExitPending.repo_root";

    sealed class DeferredScreenshotWork
    {
        public string RepoRoot;
        public string CommandId;
        public string AbsolutePath;
        public string RepoRelativePath;
        public DateTime StartedUtc;
        public DebugBundleCompletionStash DebugBundle;
    }

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

    static readonly System.Collections.Generic.List<DeferredScreenshotWork> s_deferredScreenshots =
        new System.Collections.Generic.List<DeferredScreenshotWork>();

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
        catch (Exception ex) { Debug.LogWarning($"[AgentBridge] Unexpected error in pump loop: {ex.Message}"); }

        s_frameCounter++;
        if (s_frameCounter < PollEveryNFrames) return;
        s_frameCounter = 0;
        try { ProcessOnePendingCommandIfAny(); }
        catch (Exception ex) { Debug.LogWarning($"[AgentBridge] Unexpected error in poll loop: {ex.Message}"); }
    }

    static void ProcessOnePendingCommandIfAny()
    {
        string repoRoot = EditorPostgresExportRegistrar.GetRepositoryRoot();
        string dbUrl = EditorPostgresExportRegistrar.ResolveEffectiveDatabaseUrl(repoRoot);
        if (string.IsNullOrWhiteSpace(dbUrl)) return;
        if (!EditorPostgresBridgeJobs.TryDequeue(repoRoot, out EditorPostgresBridgeJobs.DequeueStdoutDto dq, out string dqLog))
        {
            if (!string.IsNullOrEmpty(dqLog) && dqLog.Contains("exit=", StringComparison.Ordinal))
                Debug.LogWarning($"[AgentBridge] Dequeue failed: {dqLog}");
            return;
        }
        if (dq == null || dq.empty) return;
        string commandId = dq.command_id;
        if (string.IsNullOrEmpty(commandId)) { Debug.LogWarning("[AgentBridge] Dequeue returned ok but missing command_id."); return; }
        if (string.IsNullOrEmpty(dq.kind)) { TryFinalizeFailed(repoRoot, commandId, "Missing kind."); return; }

        switch (dq.kind)
        {
            case "export_agent_context":         RunExportAgentContext(repoRoot, commandId, dq.request_json);         break;
            case "get_console_logs":             RunGetConsoleLogs(repoRoot, commandId, dq.request_json);             break;
            case "capture_screenshot":           RunCaptureScreenshot(repoRoot, commandId, dq.request_json);          break;
            case "enter_play_mode":              RunEnterPlayMode(repoRoot, commandId);                               break;
            case "exit_play_mode":               RunExitPlayMode(repoRoot, commandId);                                break;
            case "get_play_mode_status":         RunGetPlayModeStatus(repoRoot, commandId);                           break;
            case "debug_context_bundle":         RunDebugContextBundle(repoRoot, commandId, dq.request_json);         break;
            case "get_compilation_status":       RunGetCompilationStatus(repoRoot, commandId, dq.request_json);       break;
            case "economy_balance_snapshot":     RunEconomyBalanceSnapshot(repoRoot, commandId, dq.request_json);     break;
            case "prefab_manifest":              RunPrefabManifest(repoRoot, commandId, dq.request_json);             break;
            case "sorting_order_debug":          RunSortingOrderDebug(repoRoot, commandId, dq.request_json);          break;
            case "export_cell_chunk":            RunExportCellChunk(repoRoot, commandId, dq.request_json);            break;
            case "export_sorting_debug":         RunExportSortingDebug(repoRoot, commandId, dq.request_json);         break;
            case "catalog_preview":              TryDispatchCatalogPreviewKind(dq.kind, repoRoot, commandId, dq.request_json); break;
            case "prefab_inspect":               RunPrefabInspect(repoRoot, commandId, dq.request_json);              break;
            case "ui_tree_walk":                 RunUiTreeWalk(repoRoot, commandId, dq.request_json);                 break;
            case "claude_design_conformance":    RunClaudeDesignConformance(repoRoot, commandId, dq.request_json);    break;
            case "claude_design_check":          RunClaudeDesignCheck(repoRoot, commandId, dq.request_json);          break;
            case "validate_panel_blueprint":     RunValidatePanelBlueprint(repoRoot, commandId, dq.request_json);     break;
            case "read_panel_state":             RunReadPanelState(repoRoot, commandId, dq.request_json);             break;
            case "get_action_log":               RunGetActionLog(repoRoot, commandId, dq.request_json);               break;
            case "dispatch_action":              RunDispatchAction(repoRoot, commandId, dq.request_json);             break;
            case "wire_ui_documents":           RunWireUiDocuments(repoRoot, commandId, dq.request_json);           break;
            case "flag_flip":                   RunFlagFlip(repoRoot, commandId, dq.request_json);                   break;
            default:
                if (!TryDispatchMutationKind(dq.kind, repoRoot, commandId, dq.request_json))
                    TryFinalizeFailed(repoRoot, commandId, $"Unknown kind '{dq.kind}'. Observation kinds: export_agent_context, get_console_logs, capture_screenshot, enter_play_mode, exit_play_mode, get_play_mode_status, debug_context_bundle, get_compilation_status, economy_balance_snapshot, prefab_manifest, sorting_order_debug, export_cell_chunk, export_sorting_debug, catalog_preview, prefab_inspect, ui_tree_walk, claude_design_conformance, claude_design_check, validate_panel_blueprint, read_panel_state, get_action_log, dispatch_action, flag_flip. Mutation kinds (Edit Mode): attach_component, remove_component, assign_serialized_field, create_gameobject, delete_gameobject, find_gameobject, set_transform, set_gameobject_active, set_gameobject_parent, save_scene, open_scene, new_scene, instantiate_prefab, apply_prefab_overrides, create_scriptable_object, modify_scriptable_object, refresh_asset_database, move_asset, delete_asset, execute_menu_item.");
                break;
        }
    }

    static void CompleteOrFail(string repoRoot, string commandId, string responseJson)
    {
        if (!EditorPostgresBridgeJobs.TryCompleteSuccess(repoRoot, commandId, responseJson, out string completeLog))
        {
            Debug.LogWarning($"[AgentBridge] complete.mjs failed: {completeLog}");
            EditorPostgresBridgeJobs.TryCompleteFailed(repoRoot, commandId, "agent-bridge-complete.mjs failed after bridge command; see Unity Console for stderr.", out _);
        }
    }

    static void TryFinalizeFailed(string repoRoot, string commandId, string englishError)
    {
        if (!EditorPostgresBridgeJobs.TryCompleteFailed(repoRoot, commandId, englishError, out string log))
            Debug.LogWarning($"[AgentBridge] Could not mark job failed: {log}");
    }

    static bool TryParseRequestEnvelope(string requestJson, out AgentBridgeRequestEnvelopeDto env, out string error)
    {
        env = null; error = null;
        if (string.IsNullOrWhiteSpace(requestJson)) { error = "Empty request_json."; return false; }
        string normalized = requestJson.Replace("\"params\":", "\"bridge_params\":", StringComparison.Ordinal);
        try { env = JsonUtility.FromJson<AgentBridgeRequestEnvelopeDto>(normalized); }
        catch (Exception ex) { error = $"Invalid request JSON: {ex.Message}"; return false; }
        if (env == null || string.IsNullOrEmpty(env.kind)) { error = "Missing kind in request."; return false; }
        return true;
    }

    // Mirror of BridgeCommandService.BuildObservabilityResponseJson — kept for sibling partials
    // that were already calling the static member before Stage 6.1 extraction.
    static string BuildObservabilityResponseJson(string commandId, bool ok, string storage, bool postgresOnly, string[] artifactPaths, string error, AgentBridgeLogLineDto[] logLines)
        => BridgeCommandService.BuildObservabilityResponseJson(commandId, ok, storage, postgresOnly, artifactPaths, error, logLines);
}
