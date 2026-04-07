using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Polls Postgres via <c>agent-bridge-dequeue.mjs</c>, dispatches bridge <c>kind</c> values to existing
/// <b>Territory Developer → Reports</b> entry points or bridge helpers, then completes jobs with <c>agent-bridge-complete.mjs</c>.
/// Requires <c>DATABASE_URL</c> (same as <see cref="EditorPostgresExportRegistrar"/>).
/// </summary>
public static class AgentBridgeCommandRunner
{
    const int PollEveryNFrames = 30;

    /// <summary>
    /// Full-game <see cref="ScreenCapture.CaptureScreenshot"/> writes after the game view renders.
    /// Poll on <see cref="EditorApplication.update"/> until the file exists or a short wall-clock budget
    /// elapses (keeps MCP <c>timeout_ms</c> ≤ 30s practical; frame counts alone are unreliable when the
    /// Editor update rate drops).
    /// </summary>
    const double DeferredScreenshotMaxWaitSeconds = 15.0;

    sealed class DeferredScreenshotWork
    {
        public string RepoRoot;
        public string CommandId;
        public string AbsolutePath;
        public string RepoRelativePath;
        public DateTime StartedUtc;
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
            case "export_agent_context":
                RunExportAgentContext(repoRoot, commandId);
                break;
            case "get_console_logs":
                RunGetConsoleLogs(repoRoot, commandId, dq.request_json);
                break;
            case "capture_screenshot":
                RunCaptureScreenshot(repoRoot, commandId, dq.request_json);
                break;
            default:
                TryFinalizeFailed(
                    repoRoot,
                    commandId,
                    $"Unknown kind '{dq.kind}'. Supported: export_agent_context, get_console_logs, capture_screenshot.");
                break;
        }
    }

    static void RunExportAgentContext(string repoRoot, string commandId)
    {
        AgentBridgeAgentContextOutcome outcome = AgentDiagnosticsReportsMenu.ExportAgentContextForAgentBridge();
        string responseJson = BuildAgentContextResponseJson(commandId, outcome);
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
            ScheduleDeferredScreenshotComplete(repoRoot, commandId, absPath, relPath);
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
        string repoRelativePath)
    {
        s_deferredScreenshots.Add(
            new DeferredScreenshotWork
            {
                RepoRoot = repoRoot,
                CommandId = commandId,
                AbsolutePath = absolutePath,
                RepoRelativePath = repoRelativePath,
                StartedUtc = DateTime.UtcNow,
            });
        EditorApplication.update -= PumpDeferredScreenshotCompletions;
        EditorApplication.update += PumpDeferredScreenshotCompletions;
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

        if (s_deferredScreenshots.Count == 0)
            EditorApplication.update -= PumpDeferredScreenshotCompletions;
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
        };

        if (outcome.Success && !outcome.PostgresOnly && !string.IsNullOrEmpty(outcome.ArtifactPathRepoRelative))
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
}

[Serializable]
class AgentBridgeLogLineDto
{
    public string timestamp_utc;
    public string severity;
    public string message;
    public string stack;
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
}
