using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Territory.Core;
using Territory.Terrain;
using UnityEditor;
using UnityEngine;

// Stage 6.1 extract — observability kind handlers + deferred screenshot pump.
// Moved from AgentBridgeCommandRunner.cs stem to keep stem ≤200 LOC.

public static partial class AgentBridgeCommandRunner
{
    static void RunExportAgentContext(string repoRoot, string commandId, string requestJson)
    {
        int? seedX = null; int? seedY = null;
        if (TryParseRequestEnvelope(requestJson, out AgentBridgeRequestEnvelopeDto env, out _) && env?.bridge_params != null && !string.IsNullOrWhiteSpace(env.bridge_params.seed_cell))
        {
            string raw = env.bridge_params.seed_cell.Trim(); int comma = raw.IndexOf(',');
            if (comma > 0 && comma < raw.Length - 1 && int.TryParse(raw.Substring(0, comma).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int px) && int.TryParse(raw.Substring(comma + 1).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int py))
            { seedX = px; seedY = py; }
        }
        var outcome = AgentDiagnosticsReportsMenu.ExportAgentContextForAgentBridge(seedX, seedY, writeBridgeArtifactFile: true);
        CompleteOrFail(repoRoot, commandId, BridgeCommandService.BuildAgentContextResponseJson(commandId, outcome));
    }

    static void RunExportCellChunk(string repoRoot, string commandId, string requestJson)
    {
        if (!TryParseRequestEnvelope(requestJson, out AgentBridgeRequestEnvelopeDto env, out string parseErr)) { TryFinalizeFailed(repoRoot, commandId, parseErr); return; }
        if (!EditorApplication.isPlaying) { TryFinalizeFailed(repoRoot, commandId, "export_cell_chunk requires Play Mode with an initialized grid. Use enter_play_mode first."); return; }
        GridManager grid = UnityEngine.Object.FindObjectOfType<GridManager>();
        if (grid == null || !grid.isInitialized) { TryFinalizeFailed(repoRoot, commandId, "Grid not initialized — wait for play_mode_ready after enter_play_mode before calling export_cell_chunk."); return; }
        var p = env.bridge_params ?? new AgentBridgeParamsPayloadDto();
        var outcome = InterchangeJsonReportsMenu.ExportCellChunkForAgentBridge(p.origin_x, p.origin_y, p.chunk_width > 0 ? p.chunk_width : InterchangeJsonReportsMenu.DefaultChunkWidth, p.chunk_height > 0 ? p.chunk_height : InterchangeJsonReportsMenu.DefaultChunkHeight);
        CompleteOrFail(repoRoot, commandId, BridgeCommandService.BuildObservabilityResponseJson(commandId, outcome.Success, "postgres", true, Array.Empty<string>(), outcome.Success ? string.Empty : outcome.ErrorMessage, Array.Empty<AgentBridgeLogLineDto>()));
    }

    static void RunExportSortingDebug(string repoRoot, string commandId, string requestJson)
    {
        if (!TryParseRequestEnvelope(requestJson, out AgentBridgeRequestEnvelopeDto env, out string parseErr)) { TryFinalizeFailed(repoRoot, commandId, parseErr); return; }
        if (!EditorApplication.isPlaying) { TryFinalizeFailed(repoRoot, commandId, "export_sorting_debug requires Play Mode with an initialized grid. Use enter_play_mode first."); return; }
        GridManager grid = UnityEngine.Object.FindObjectOfType<GridManager>();
        if (grid == null || !grid.isInitialized) { TryFinalizeFailed(repoRoot, commandId, "Grid not initialized — wait for play_mode_ready after enter_play_mode before calling export_sorting_debug."); return; }
        TerrainManager terrain = grid.terrainManager;
        if (terrain == null) { TryFinalizeFailed(repoRoot, commandId, "TerrainManager not available — GridManager.terrainManager is null; cannot produce sorting debug export."); return; }
        var p = env.bridge_params ?? new AgentBridgeParamsPayloadDto();
        int? seedX = null; int? seedY = null;
        if (!string.IsNullOrWhiteSpace(p.seed_cell) && TryParseSeedCellString(p.seed_cell, out int sx, out int sy)) { seedX = sx; seedY = sy; }
        var outcome = AgentDiagnosticsReportsMenu.ExportSortingDebugForAgentBridge(seedX, seedY);
        CompleteOrFail(repoRoot, commandId, BridgeCommandService.BuildObservabilityResponseJson(commandId, outcome.Success, "postgres", true, Array.Empty<string>(), outcome.Success ? string.Empty : outcome.ErrorMessage, Array.Empty<AgentBridgeLogLineDto>()));
    }

    static void RunGetConsoleLogs(string repoRoot, string commandId, string requestJson)
    {
        if (!TryParseRequestEnvelope(requestJson, out AgentBridgeRequestEnvelopeDto env, out string parseErr)) { TryFinalizeFailed(repoRoot, commandId, parseErr); return; }
        if (!string.Equals(env.kind, "get_console_logs", StringComparison.Ordinal)) { TryFinalizeFailed(repoRoot, commandId, "Request kind mismatch for get_console_logs."); return; }
        var p = env.bridge_params ?? new AgentBridgeParamsPayloadDto();
        int maxLines = p.max_lines > 0 ? Math.Min(p.max_lines, 2000) : 200;
        DateTime? sinceUtc = null;
        if (!string.IsNullOrWhiteSpace(p.since_utc)) { if (!DateTime.TryParse(p.since_utc, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime parsed)) { TryFinalizeFailed(repoRoot, commandId, "Invalid since_utc; use ISO-8601 UTC."); return; } sinceUtc = parsed; }
        string severityFilter = string.IsNullOrWhiteSpace(p.severity_filter) ? "all" : p.severity_filter.Trim();
        string tagFilter = string.IsNullOrWhiteSpace(p.tag_filter) ? null : p.tag_filter;
        List<AgentBridgeConsoleBuffer.LogEntry> lines = AgentBridgeConsoleBuffer.Query(sinceUtc, severityFilter, tagFilter, maxLines);
        var dtos = new AgentBridgeLogLineDto[lines.Count];
        for (int i = 0; i < lines.Count; i++) { var e = lines[i]; dtos[i] = new AgentBridgeLogLineDto { timestamp_utc = new DateTime(e.utcTicks, DateTimeKind.Utc).ToString("o"), severity = e.severity ?? "log", message = e.message ?? string.Empty, stack = e.stack ?? string.Empty }; }
        CompleteOrFail(repoRoot, commandId, BridgeCommandService.BuildObservabilityResponseJson(commandId, true, "console_buffer", false, Array.Empty<string>(), string.Empty, dtos));
    }

    static void RunCaptureScreenshot(string repoRoot, string commandId, string requestJson)
    {
        if (!TryParseRequestEnvelope(requestJson, out AgentBridgeRequestEnvelopeDto env, out string parseErr)) { TryFinalizeFailed(repoRoot, commandId, parseErr); return; }
        if (!string.Equals(env.kind, "capture_screenshot", StringComparison.Ordinal)) { TryFinalizeFailed(repoRoot, commandId, "Request kind mismatch for capture_screenshot."); return; }
        var p = env.bridge_params ?? new AgentBridgeParamsPayloadDto();
        if (!AgentBridgeScreenshotCapture.TryBeginCapture(repoRoot, p.camera ?? string.Empty, p.filename_stem ?? string.Empty, p.include_ui, out string relPath, out string absPath, out bool deferred, out string err))
        {
            CompleteOrFail(repoRoot, commandId, BridgeCommandService.BuildObservabilityResponseJson(commandId, false, "filesystem", false, Array.Empty<string>(), err ?? "Screenshot failed.", Array.Empty<AgentBridgeLogLineDto>())); return;
        }
        if (deferred) { ScheduleDeferredScreenshotComplete(repoRoot, commandId, absPath, relPath, null); return; }
        CompleteOrFail(repoRoot, commandId, BridgeCommandService.BuildObservabilityResponseJson(commandId, true, "filesystem", false, new[] { relPath }, string.Empty, Array.Empty<AgentBridgeLogLineDto>()));
    }

    static void ScheduleDeferredScreenshotComplete(string repoRoot, string commandId, string absolutePath, string repoRelativePath, DebugBundleCompletionStash debugBundleOrNull)
    {
        s_deferredScreenshots.Add(new DeferredScreenshotWork { RepoRoot = repoRoot, CommandId = commandId, AbsolutePath = absolutePath, RepoRelativePath = repoRelativePath, StartedUtc = DateTime.UtcNow, DebugBundle = debugBundleOrNull });
    }

    static void PumpDeferredScreenshotCompletions()
    {
        for (int i = s_deferredScreenshots.Count - 1; i >= 0; i--)
        {
            DeferredScreenshotWork w = s_deferredScreenshots[i];
            bool exists = File.Exists(w.AbsolutePath);
            double elapsed = (DateTime.UtcNow - w.StartedUtc).TotalSeconds;
            if (!exists && elapsed < DeferredScreenshotMaxWaitSeconds) continue;
            s_deferredScreenshots.RemoveAt(i);
            if (w.DebugBundle != null)
            {
                bool ok = exists; string shotPath = exists ? w.RepoRelativePath : string.Empty;
                string shotErr = exists ? string.Empty : "Screenshot file was not created within 15 seconds; keep the Game view visible and retry debug_context_bundle in Play Mode.";
                CompleteOrFail(w.RepoRoot, w.CommandId, BuildDebugBundleResponseJsonFromStash(w.CommandId, w.DebugBundle, shotPath, ok, false, shotErr)); continue;
            }
            if (exists)
                CompleteOrFail(w.RepoRoot, w.CommandId, BridgeCommandService.BuildObservabilityResponseJson(w.CommandId, true, "filesystem", false, new[] { w.RepoRelativePath }, string.Empty, Array.Empty<AgentBridgeLogLineDto>()));
            else
                CompleteOrFail(w.RepoRoot, w.CommandId, BridgeCommandService.BuildObservabilityResponseJson(w.CommandId, false, "filesystem", false, Array.Empty<string>(), "Screenshot file was not created within 15 seconds; keep the Game view visible and retry capture_screenshot in Play Mode.", Array.Empty<AgentBridgeLogLineDto>()));
        }
    }

    static void RunDebugContextBundle(string repoRoot, string commandId, string requestJson)
    {
        if (!TryParseRequestEnvelope(requestJson, out AgentBridgeRequestEnvelopeDto env, out string parseErr)) { TryFinalizeFailed(repoRoot, commandId, parseErr); return; }
        if (!string.Equals(env.kind, "debug_context_bundle", StringComparison.Ordinal)) { TryFinalizeFailed(repoRoot, commandId, "Request kind mismatch for debug_context_bundle."); return; }
        if (!EditorApplication.isPlaying) { TryFinalizeFailed(repoRoot, commandId, "debug_context_bundle requires Play Mode."); return; }
        GridManager grid = UnityEngine.Object.FindObjectOfType<GridManager>();
        if (grid == null || !grid.isInitialized) { TryFinalizeFailed(repoRoot, commandId, "debug_context_bundle requires an initialized GridManager in Play Mode (wait for play_mode_ready after enter_play_mode)."); return; }
        var p = env.bridge_params ?? new AgentBridgeParamsPayloadDto();
        if (!TryParseSeedCellString(string.IsNullOrWhiteSpace(p.seed_cell) ? string.Empty : p.seed_cell.Trim(), out int seedX, out int seedY)) { TryFinalizeFailed(repoRoot, commandId, "debug_context_bundle requires bridge_params.seed_cell as \"x,y\" (e.g. \"62,0\")."); return; }
        bool includeScreenshot = RequestJsonContainsParamKey(requestJson, "include_screenshot") ? p.include_screenshot : true;
        bool includeConsole = RequestJsonContainsParamKey(requestJson, "include_console") ? p.include_console : true;
        bool includeAnomalyScan = RequestJsonContainsParamKey(requestJson, "include_anomaly_scan") ? p.include_anomaly_scan : true;
        var outcome = AgentDiagnosticsReportsMenu.ExportAgentContextForAgentBridge(seedX, seedY, writeBridgeArtifactFile: true);
        AgentBridgeLogLineDto[] consoleLines = Array.Empty<AgentBridgeLogLineDto>();
        if (includeConsole)
        {
            int maxLines = p.max_lines > 0 ? Math.Min(p.max_lines, 2000) : 200;
            DateTime? sinceUtc = null;
            if (!string.IsNullOrWhiteSpace(p.since_utc)) { if (!DateTime.TryParse(p.since_utc, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime parsed)) { TryFinalizeFailed(repoRoot, commandId, "Invalid since_utc; use ISO-8601 UTC."); return; } sinceUtc = parsed; }
            var lines = AgentBridgeConsoleBuffer.Query(sinceUtc, string.IsNullOrWhiteSpace(p.severity_filter) ? "all" : p.severity_filter.Trim(), string.IsNullOrWhiteSpace(p.tag_filter) ? null : p.tag_filter, maxLines);
            consoleLines = new AgentBridgeLogLineDto[lines.Count];
            for (int i = 0; i < lines.Count; i++) { var e = lines[i]; consoleLines[i] = new AgentBridgeLogLineDto { timestamp_utc = new DateTime(e.utcTicks, DateTimeKind.Utc).ToString("o"), severity = e.severity ?? "log", message = e.message ?? string.Empty, stack = e.stack ?? string.Empty }; }
        }
        AgentBridgeAnomalyRecordDto[] anomalies = includeAnomalyScan ? AgentBridgeAnomalyScanner.ScanNeighborhood(seedX, seedY).ToArray() : Array.Empty<AgentBridgeAnomalyRecordDto>();
        var stash = new DebugBundleCompletionStash { CellExportRelPath = outcome.ArtifactPathRepoRelative ?? string.Empty, CellExportOk = outcome.Success, CellExportError = outcome.ErrorMessage ?? string.Empty, ConsoleLines = consoleLines, ConsoleSkipped = !includeConsole, Anomalies = anomalies, AnomalyScanSkipped = !includeAnomalyScan, ScreenshotIncluded = includeScreenshot };
        if (!includeScreenshot) { CompleteOrFail(repoRoot, commandId, BuildDebugBundleResponseJsonFromStash(commandId, stash, string.Empty, true, true, string.Empty)); return; }
        string stem = string.IsNullOrWhiteSpace(p.filename_stem) ? "debug-bundle" : p.filename_stem.Trim();
        if (!AgentBridgeScreenshotCapture.TryBeginCapture(repoRoot, string.Empty, stem, true, out string relPath, out string absPath, out bool deferred, out string err)) { CompleteOrFail(repoRoot, commandId, BuildDebugBundleResponseJsonFromStash(commandId, stash, string.Empty, false, false, err ?? "Screenshot failed.")); return; }
        if (deferred) { ScheduleDeferredScreenshotComplete(repoRoot, commandId, absPath, relPath, stash); return; }
        CompleteOrFail(repoRoot, commandId, BuildDebugBundleResponseJsonFromStash(commandId, stash, relPath ?? string.Empty, true, false, string.Empty));
    }

    static void RunEconomyBalanceSnapshot(string repoRoot, string commandId, string requestJson)
    {
        if (!TryParseRequestEnvelope(requestJson, out _, out string parseErr)) { TryFinalizeFailed(repoRoot, commandId, parseErr); return; }
        var economyMgr = UnityEngine.Object.FindObjectOfType<Territory.Economy.EconomyManager>();
        var cityStats = UnityEngine.Object.FindObjectOfType<Territory.Economy.CityStats>();
        var demandMgr = UnityEngine.Object.FindObjectOfType<Territory.Economy.DemandManager>();
        if (cityStats == null) { TryFinalizeFailed(repoRoot, commandId, "CityStats not found in scene. Is a game loaded?"); return; }
        var snapshot = new AgentBridgeEconomySnapshotDto { population = cityStats.population, happiness = cityStats.happiness, pollution = cityStats.pollution, money = cityStats.money, residential_tax = economyMgr != null ? economyMgr.residentialIncomeTax : 0, commercial_tax = economyMgr != null ? economyMgr.commercialIncomeTax : 0, industrial_tax = economyMgr != null ? economyMgr.industrialIncomeTax : 0, residential_building_count = cityStats.residentialBuildingCount, commercial_building_count = cityStats.commercialBuildingCount, industrial_building_count = cityStats.industrialBuildingCount };
        if (demandMgr != null) { snapshot.residential_demand = demandMgr.residentialDemand?.demandLevel ?? 0f; snapshot.commercial_demand = demandMgr.commercialDemand?.demandLevel ?? 0f; snapshot.industrial_demand = demandMgr.industrialDemand?.demandLevel ?? 0f; }
        var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "economy_balance_snapshot"); resp.economy_snapshot = snapshot;
        CompleteOrFail(repoRoot, commandId, UnityEngine.JsonUtility.ToJson(resp, true));
    }

    static void RunPrefabManifest(string repoRoot, string commandId, string requestJson)
    {
        if (!TryParseRequestEnvelope(requestJson, out _, out string parseErr)) { TryFinalizeFailed(repoRoot, commandId, parseErr); return; }
        var manifestEntries = new List<AgentBridgePrefabManifestEntryDto>();
        var allBehaviours = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true);
        foreach (var mb in allBehaviours) { if (mb == null) continue; manifestEntries.Add(new AgentBridgePrefabManifestEntryDto { game_object_path = GetGameObjectPath(mb.gameObject), component_type = mb.GetType().Name, is_missing_script = false }); }
        var allGameObjects = UnityEngine.Object.FindObjectsOfType<GameObject>(true);
        foreach (var go in allGameObjects) { foreach (var comp in go.GetComponents<Component>()) { if (comp == null) manifestEntries.Add(new AgentBridgePrefabManifestEntryDto { game_object_path = GetGameObjectPath(go), component_type = "(missing script)", is_missing_script = true }); } }
        var manifest = new AgentBridgePrefabManifestDto { total_monobehaviours = allBehaviours.Length, missing_script_count = manifestEntries.FindAll(e => e.is_missing_script).Count, entries = manifestEntries.ToArray() };
        var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "prefab_manifest"); resp.prefab_manifest = manifest;
        CompleteOrFail(repoRoot, commandId, UnityEngine.JsonUtility.ToJson(resp, true));
    }

    static string GetGameObjectPath(GameObject go)
    {
        string path = go.name; Transform parent = go.transform.parent;
        while (parent != null) { path = parent.name + "/" + path; parent = parent.parent; }
        return path;
    }

    static void RunSortingOrderDebug(string repoRoot, string commandId, string requestJson)
    {
        if (!TryParseRequestEnvelope(requestJson, out AgentBridgeRequestEnvelopeDto env, out string parseErr)) { TryFinalizeFailed(repoRoot, commandId, parseErr); return; }
        string seedCell = env.bridge_params?.seed_cell ?? string.Empty;
        if (string.IsNullOrWhiteSpace(seedCell)) { TryFinalizeFailed(repoRoot, commandId, "seed_cell is required for sorting_order_debug (e.g. \"3,0\")."); return; }
        string[] parts = seedCell.Split(',');
        if (parts.Length != 2 || !int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int cellX) || !int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int cellY)) { TryFinalizeFailed(repoRoot, commandId, $"Invalid seed_cell format '{seedCell}'. Expected 'x,y' (e.g. '3,0')."); return; }
        var gridMgr = UnityEngine.Object.FindObjectOfType<Territory.Core.GridManager>();
        if (gridMgr == null) { TryFinalizeFailed(repoRoot, commandId, "GridManager not found in scene."); return; }
        Territory.Core.CityCell cell = gridMgr.GetCell(cellX, cellY);
        if (cell == null) { TryFinalizeFailed(repoRoot, commandId, $"No cell at ({cellX},{cellY})."); return; }
        var rendererEntries = new List<AgentBridgeSortingRendererDto>();
        foreach (var sr in cell.gameObject.GetComponentsInChildren<SpriteRenderer>(true))
            rendererEntries.Add(new AgentBridgeSortingRendererDto { name = sr.gameObject.name, sorting_layer = sr.sortingLayerName, sorting_order = sr.sortingOrder, sprite_name = sr.sprite != null ? sr.sprite.name : string.Empty, enabled = sr.enabled });
        var sortingDebug = new AgentBridgeSortingOrderDebugDto { cell_x = cellX, cell_y = cellY, cell_height = cell.height, renderer_count = rendererEntries.Count, renderers = rendererEntries.ToArray() };
        var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "sorting_order_debug"); resp.sorting_order_debug = sortingDebug;
        CompleteOrFail(repoRoot, commandId, UnityEngine.JsonUtility.ToJson(resp, true));
    }

    static void RunGetCompilationStatus(string repoRoot, string commandId, string requestJson)
    {
        if (!TryParseRequestEnvelope(requestJson, out AgentBridgeRequestEnvelopeDto env, out string parseErr)) { TryFinalizeFailed(repoRoot, commandId, parseErr); return; }
        if (!string.Equals(env.kind, "get_compilation_status", StringComparison.OrdinalIgnoreCase)) { TryFinalizeFailed(repoRoot, commandId, "Request kind mismatch for get_compilation_status."); return; }
        bool compiling = EditorApplication.isCompiling; bool compilationFailed = EditorUtility.scriptCompilationFailed;
        List<AgentBridgeConsoleBuffer.LogEntry> rawErrors = AgentBridgeConsoleBuffer.Query(null, "error", null, 30);
        const int maxMessages = 20;
        var messageDtos = new List<AgentBridgeLogLineDto>(Math.Min(rawErrors.Count, maxMessages));
        for (int i = 0; i < rawErrors.Count && messageDtos.Count < maxMessages; i++) { var e = rawErrors[i]; messageDtos.Add(new AgentBridgeLogLineDto { timestamp_utc = new DateTime(e.utcTicks, DateTimeKind.Utc).ToString("o"), severity = e.severity ?? "error", message = e.message ?? string.Empty, stack = e.stack ?? string.Empty }); }
        string excerpt = messageDtos.Count > 0 ? (messageDtos[0].message?.Length > 500 ? messageDtos[0].message.Substring(0, 500) : messageDtos[0].message ?? string.Empty) : string.Empty;
        var compilation = new AgentBridgeCompilationStatusDto { compiling = compiling, compilation_failed = compilationFailed, last_error_excerpt = excerpt, recent_error_messages = messageDtos.ToArray() };
        var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "compilation_status"); resp.compilation_status = compilation;
        CompleteOrFail(repoRoot, commandId, UnityEngine.JsonUtility.ToJson(resp, true));
    }

    static string BuildDebugBundleResponseJsonFromStash(string commandId, DebugBundleCompletionStash s, string screenshotRelPath, bool screenshotOk, bool screenshotSkipped, string screenshotError)
    {
        bool overallOk = s.CellExportOk;
        if (s.ScreenshotIncluded && !screenshotSkipped && !screenshotOk) overallOk = false;
        string error = string.Empty;
        if (!overallOk)
        {
            if (!s.CellExportOk && !string.IsNullOrEmpty(s.CellExportError)) error = s.CellExportError;
            else if (s.ScreenshotIncluded && !screenshotSkipped && !screenshotOk && !string.IsNullOrEmpty(screenshotError)) error = screenshotError;
            else if (!s.CellExportOk) error = "CityCell export failed.";
            else error = "debug_context_bundle completed with errors.";
        }
        var bundle = new AgentBridgeBundleDto { cell_export = new AgentBridgeBundleCellExportDto { artifact_path = s.CellExportRelPath ?? string.Empty, ok = s.CellExportOk }, screenshot = new AgentBridgeBundleScreenshotDto { artifact_path = screenshotRelPath ?? string.Empty, ok = screenshotSkipped || screenshotOk, skipped = screenshotSkipped }, console = new AgentBridgeBundleConsoleDto { log_lines = s.ConsoleLines ?? Array.Empty<AgentBridgeLogLineDto>(), line_count = s.ConsoleLines != null ? s.ConsoleLines.Length : 0, skipped = s.ConsoleSkipped }, anomalies = s.Anomalies ?? Array.Empty<AgentBridgeAnomalyRecordDto>(), anomaly_count = s.Anomalies != null ? s.Anomalies.Length : 0, anomaly_scan_skipped = s.AnomalyScanSkipped };
        var resp = new AgentBridgeResponseFileDto { schema_version = 1, artifact = "unity_agent_bridge_response", command_id = commandId, ok = overallOk, completed_at_utc = DateTime.UtcNow.ToString("o"), storage = "debug_context_bundle", postgres_only = false, error = error, artifact_paths = Array.Empty<string>(), log_lines = Array.Empty<AgentBridgeLogLineDto>(), play_mode_state = string.Empty, ready = false, already_playing = false, already_stopped = false, has_grid_dimensions = false, grid_width = 0, grid_height = 0, bundle = bundle };
        return UnityEngine.JsonUtility.ToJson(resp, true);
    }

    static bool RequestJsonContainsParamKey(string requestJson, string key) =>
        !string.IsNullOrEmpty(requestJson) && !string.IsNullOrEmpty(key) && requestJson.IndexOf($"\"{key}\"", StringComparison.Ordinal) >= 0;

    static bool TryParseSeedCellString(string raw, out int x, out int y)
    {
        x = 0; y = 0;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        string trimmed = raw.Trim(); int comma = trimmed.IndexOf(',');
        if (comma <= 0 || comma >= trimmed.Length - 1) return false;
        return int.TryParse(trimmed.Substring(0, comma).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out x) && int.TryParse(trimmed.Substring(comma + 1).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out y);
    }
}
