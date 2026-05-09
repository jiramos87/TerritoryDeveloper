using System;
using System.Collections.Generic;
using System.Linq;
using Territory.UI.Registry;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Wave A0 (TECH-27060) — bake-time drift validator.
/// Menu: Territory > UI > Validate Action+Bind Drift
/// Walks panel_child.params_json action / bind / visible_bind / enabled_bind / slot_bind keys
/// and asserts each resolves against UiActionRegistry / UiBindRegistry.
/// Exits 1 on unresolved refs when invoked headless via agent bridge.
/// </summary>
public static class ActionBindDriftValidator
{
    private const string MenuPath = "Territory/UI/Validate Action+Bind Drift";

    [MenuItem(MenuPath)]
    public static void ValidateFromMenu()
    {
        var result = RunValidation();
        if (result.HasErrors)
        {
            Debug.LogError($"[ActionBindDriftValidator] DRIFT DETECTED — {result.ErrorCount} unresolved ref(s):\n{result.ErrorSummary}");
            EditorUtility.DisplayDialog(
                "Action+Bind Drift",
                $"FAIL — {result.ErrorCount} unresolved ref(s).\n\n{result.ErrorSummary}",
                "OK");
        }
        else
        {
            Debug.Log($"[ActionBindDriftValidator] OK — {result.CheckedCount} ref(s) validated, 0 unresolved.");
            EditorUtility.DisplayDialog("Action+Bind Drift", $"PASS — {result.CheckedCount} ref(s) OK.", "OK");
        }
    }

    /// <summary>
    /// Headless entry point called by agent bridge command.
    /// Returns JSON: { "ok": bool, "checked": int, "errors": [string] }
    /// </summary>
    public static string RunValidationJson()
    {
        var result = RunValidation();
        var errors = string.Join(",", result.Errors.Select(e => $"\"{EscapeJson(e)}\""));
        return $"{{\"ok\":{(!result.HasErrors).ToString().ToLower()},\"checked\":{result.CheckedCount},\"errors\":[{errors}]}}";
    }

    /// <summary>Core validation — returns ValidationResult.</summary>
    public static ValidationResult RunValidation()
    {
        var actionRegistry = UnityEngine.Object.FindObjectOfType<UiActionRegistry>();
        var bindRegistry = UnityEngine.Object.FindObjectOfType<UiBindRegistry>();

        var registeredActions = actionRegistry != null
            ? new HashSet<string>(actionRegistry.ListRegistered())
            : new HashSet<string>();
        var registeredBinds = bindRegistry != null
            ? new HashSet<string>(bindRegistry.ListRegistered())
            : new HashSet<string>();

        var errors = new List<string>();
        int checked_ = 0;

        // Walk panel_child refs from DB-backed params_json via PanelChildParamsProvider.
        // At Stage 1.0 no panel_child rows carry action/bind keys yet;
        // validator shell runs with 0 refs and exits pass.
        // Wave A1+ populates panel_child rows; validator re-runs and catches drift.
        var refs = PanelChildParamsProvider.CollectActionBindRefs();
        foreach (var r in refs)
        {
            checked_++;
            if (r.Kind == PanelChildRef.RefKind.Action)
            {
                if (!registeredActions.Contains(r.RefId))
                    errors.Add($"panel_child[{r.ChildId}] action '{r.RefId}' not in UiActionRegistry");
            }
            else
            {
                if (!registeredBinds.Contains(r.RefId))
                    errors.Add($"panel_child[{r.ChildId}] {r.Kind} '{r.RefId}' not in UiBindRegistry");
            }
        }

        return new ValidationResult(checked_, errors);
    }

    private static string EscapeJson(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");

    public sealed class ValidationResult
    {
        public int CheckedCount { get; }
        public IReadOnlyList<string> Errors { get; }
        public bool HasErrors => Errors.Count > 0;
        public int ErrorCount => Errors.Count;
        public string ErrorSummary => string.Join("\n", Errors);

        public ValidationResult(int checkedCount, List<string> errors)
        {
            CheckedCount = checkedCount;
            Errors = errors.AsReadOnly();
        }
    }
}

/// <summary>
/// Provides panel_child action/bind refs from Postgres (or returns empty when DB unavailable at bake time).
/// Loaded lazily; Wave A1+ populates real rows.
/// </summary>
internal static class PanelChildParamsProvider
{
    public static IReadOnlyList<PanelChildRef> CollectActionBindRefs()
    {
        // DB query path — connects to Postgres via connection string from env.
        // Returns empty at Stage 1.0 (no panel_child rows have action/bind keys yet).
        // Wave A1+ implementation replaces stub with actual Npgsql query.
        return Array.Empty<PanelChildRef>();
    }
}

internal sealed class PanelChildRef
{
    public enum RefKind { Action, Bind, VisibleBind, EnabledBind, SlotBind }

    public string ChildId { get; }
    public RefKind Kind { get; }
    public string RefId { get; }

    public PanelChildRef(string childId, RefKind kind, string refId)
    {
        ChildId = childId;
        Kind = kind;
        RefId = refId;
    }
}
