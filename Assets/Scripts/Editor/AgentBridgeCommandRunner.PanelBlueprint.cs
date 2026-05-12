using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

// Stage 6.1 extract — validate_panel_blueprint handler + YAML parser.
// Moved from AgentBridgeCommandRunner.cs stem to keep stem ≤200 LOC.

public static partial class AgentBridgeCommandRunner
{
    /// <summary>
    /// <c>validate_panel_blueprint</c> — reads <c>tools/blueprints/panel-schema.yaml</c> +
    /// validates every child row in <c>Assets/UI/Snapshots/panels.json</c> for the given panel.
    /// Returns <c>{ok, panel_id, kindsChecked, missing: [{path, required, kind}]}</c>.
    /// </summary>
    static void RunValidatePanelBlueprint(string repoRoot, string commandId, string requestJson)
    {
        if (!TryParseRequestEnvelope(requestJson, out AgentBridgeRequestEnvelopeDto env, out string parseErr)) { TryFinalizeFailed(repoRoot, commandId, parseErr); return; }
        string panelId = env.bridge_params?.panel_id ?? string.Empty;
        if (string.IsNullOrWhiteSpace(panelId)) { TryFinalizeFailed(repoRoot, commandId, "validate_panel_blueprint: panel_id is required."); return; }
        string schemaPath = Path.Combine(repoRoot, "tools", "blueprints", "panel-schema.yaml");
        if (!File.Exists(schemaPath)) { TryFinalizeFailed(repoRoot, commandId, $"validate_panel_blueprint: panel-schema.yaml not found at {schemaPath}."); return; }
        string schemaText;
        try { schemaText = File.ReadAllText(schemaPath); }
        catch (Exception ex) { TryFinalizeFailed(repoRoot, commandId, $"validate_panel_blueprint: schema read failed: {ex.Message}"); return; }
        ParsePanelSchemaYaml(schemaText); // parse for side-effect validation
        string panelsPath = Path.Combine(repoRoot, "Assets", "UI", "Snapshots", "panels.json");
        if (!File.Exists(panelsPath)) { TryFinalizeFailed(repoRoot, commandId, $"validate_panel_blueprint: panels.json not found at {panelsPath}."); return; }
        try { File.ReadAllText(panelsPath); }
        catch (Exception ex) { TryFinalizeFailed(repoRoot, commandId, $"validate_panel_blueprint: panels.json read failed: {ex.Message}"); return; }
        string harnessPath = Path.Combine(repoRoot, "tools", "scripts", "validate-panel-blueprint-harness.mjs");
        if (!File.Exists(harnessPath)) { TryFinalizeFailed(repoRoot, commandId, $"validate_panel_blueprint: harness not found at {harnessPath}."); return; }
        string harnessOutput;
        try
        {
            var psi = new ProcessStartInfo("node", $"\"{harnessPath}\" --panel-id \"{EscapeJsonString(panelId)}\" --panels-file \"{panelsPath}\"")
            { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, WorkingDirectory = repoRoot };
            var proc = Process.Start(psi);
            harnessOutput = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(10000);
        }
        catch (Exception ex) { TryFinalizeFailed(repoRoot, commandId, $"validate_panel_blueprint: harness exec failed: {ex.Message}"); return; }
        harnessOutput = harnessOutput?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(harnessOutput)) { TryFinalizeFailed(repoRoot, commandId, "validate_panel_blueprint: harness returned empty output."); return; }
        var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "validate_panel_blueprint");
        resp.mutation_result = harnessOutput;
        CompleteOrFail(repoRoot, commandId, JsonUtility.ToJson(resp, true));
    }

    /// <summary>
    /// Structured YAML parser for <c>tools/blueprints/panel-schema.yaml</c>.
    /// Extracts <c>kinds[].kind → required_keys[]</c> map.
    /// </summary>
    static Dictionary<string, List<string>> ParsePanelSchemaYaml(string text)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        string currentKind = null; bool inRequiredKeys = false;
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.TrimEnd(); var trimmed = line.TrimStart();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;
            if (trimmed.StartsWith("- kind:")) { currentKind = trimmed.Substring("- kind:".Length).Trim().Trim('"', '\''); if (!result.ContainsKey(currentKind)) result[currentKind] = new List<string>(); inRequiredKeys = false; continue; }
            if (currentKind != null && trimmed.StartsWith("required_keys:")) { inRequiredKeys = true; continue; }
            if (inRequiredKeys && currentKind != null && trimmed.StartsWith("- ")) { var key = trimmed.Substring(2).Trim().Trim('"', '\''); if (!string.IsNullOrEmpty(key)) result[currentKind].Add(key); continue; }
            if (inRequiredKeys && !trimmed.StartsWith("- ") && !trimmed.StartsWith("#")) inRequiredKeys = false;
        }
        return result;
    }
}
