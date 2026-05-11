using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Stage 4 Layer 4 — runtime contract query bridge (TECH-28370 / TECH-28372).
// read_panel_state — live mounted/child/bind/action/controller counts for a panel slug.
// get_action_log  — tail of the action-fire.log telemetry written by UiActionRegistry.

public static partial class AgentBridgeCommandRunner
{
    // ── read_panel_state ──────────────────────────────────────────────────────

    /// <summary>
    /// <c>read_panel_state(panel_slug)</c> — query live runtime state of a baked panel.
    /// Returns <c>{ mounted, anchor_path, child_count, bind_count, action_count, controller_alive }</c>.
    /// Usable from PlayMode bridge calls. Does NOT require Edit Mode.
    /// </summary>
    static void RunReadPanelState(string repoRoot, string commandId, string requestJson)
    {
        if (!TryParseReadPanelStateParams(requestJson, out var dto, out string parseErr))
        {
            TryFinalizeFailed(repoRoot, commandId, parseErr);
            return;
        }

        string panelSlug = dto?.panel_slug ?? string.Empty;
        if (string.IsNullOrWhiteSpace(panelSlug))
        {
            TryFinalizeFailed(repoRoot, commandId, "params_invalid:panel_slug (required)");
            return;
        }

        // Search all root GameObjects for one whose name matches the panel slug.
        // Convention: baked panel GO root name == panel slug (ThemedPanel bake contract).
        GameObject panelRoot = null;
        var rootObjects = UnityEngine.SceneManagement.SceneManager
            .GetActiveScene()
            .GetRootGameObjects();
        foreach (var go in rootObjects)
        {
            if (string.Equals(go.name, panelSlug, StringComparison.OrdinalIgnoreCase))
            {
                panelRoot = go;
                break;
            }
        }

        // Also search all scene GOs (not just roots) in case panel is nested.
        if (panelRoot == null)
        {
            foreach (var go in rootObjects)
            {
                panelRoot = FindChildByName(go.transform, panelSlug);
                if (panelRoot != null) break;
            }
        }

        bool mounted = panelRoot != null && panelRoot.activeInHierarchy;
        string anchorPath = panelRoot != null ? BuildScenePath(panelRoot.transform) : string.Empty;
        int childCount = panelRoot != null ? panelRoot.transform.childCount : 0;

        // bind_count — count of components that implement IUiBindSubscriber (marker interface).
        // Fallback: count MonoBehaviours whose class name contains "Adapter" or "Binding".
        int bindCount = 0;
        int actionCount = 0;
        bool controllerAlive = false;

        if (panelRoot != null)
        {
            var monos = panelRoot.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var m in monos)
            {
                if (m == null) continue;
                string typeName = m.GetType().Name;
                if (typeName.IndexOf("Adapter", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    typeName.IndexOf("Binding", StringComparison.OrdinalIgnoreCase) >= 0)
                    bindCount++;
                if (typeName.IndexOf("Controller", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    typeName.IndexOf("Presenter", StringComparison.OrdinalIgnoreCase) >= 0)
                    controllerAlive = true;
            }

            // action_count — count UiActionTrigger components (each declares one action id).
            var triggers = panelRoot.GetComponentsInChildren<Territory.UI.Registry.UiActionTrigger>(true);
            actionCount = triggers != null ? triggers.Length : 0;
        }

        var stateDto = new AgentBridgePanelStateDto
        {
            panel_slug       = panelSlug,
            mounted          = mounted,
            anchor_path      = anchorPath,
            child_count      = childCount,
            bind_count       = bindCount,
            action_count     = actionCount,
            controller_alive = controllerAlive,
        };

        var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "read_panel_state");
        resp.panel_state_result = stateDto;
        CompleteOrFail(repoRoot, commandId, JsonUtility.ToJson(resp, true));
    }

    // ── get_action_log ────────────────────────────────────────────────────────

    /// <summary>
    /// <c>get_action_log(since)</c> — return recent entries from the action-fire telemetry log.
    /// <paramref name="since"/> is an ISO-8601 UTC string; omit/empty → last 50 entries.
    /// </summary>
    static void RunGetActionLog(string repoRoot, string commandId, string requestJson)
    {
        if (!TryParseGetActionLogParams(requestJson, out var dto, out string parseErr))
        {
            TryFinalizeFailed(repoRoot, commandId, parseErr);
            return;
        }

        string logPath = Path.Combine(Application.persistentDataPath, "Diagnostics", "action-fire.log");

        if (!File.Exists(logPath))
        {
            // No log yet — return empty entries (not an error).
            var emptyResp = AgentBridgeResponseFileDto.CreateOk(commandId, "get_action_log");
            emptyResp.action_log_result = new AgentBridgeActionLogResultDto
            {
                log_path = logPath,
                entries  = Array.Empty<AgentBridgeActionLogEntryDto>(),
            };
            CompleteOrFail(repoRoot, commandId, JsonUtility.ToJson(emptyResp, true));
            return;
        }

        DateTime sinceUtc = DateTime.MinValue;
        if (!string.IsNullOrWhiteSpace(dto?.since))
        {
            if (!DateTime.TryParse(dto.since,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.RoundtripKind,
                    out sinceUtc))
                sinceUtc = DateTime.MinValue;
        }

        string[] lines;
        try { lines = File.ReadAllLines(logPath); }
        catch (Exception ex)
        {
            TryFinalizeFailed(repoRoot, commandId, $"get_action_log: read failed: {ex.Message}");
            return;
        }

        // Each line: JSON object {"action_id":"...","handler_class":"...","ts":"...","marker":"fired"}
        var entries = new System.Collections.Generic.List<AgentBridgeActionLogEntryDto>();
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            AgentBridgeActionLogEntryDto entry;
            try { entry = JsonUtility.FromJson<AgentBridgeActionLogEntryDto>(line); }
            catch { continue; }
            if (entry == null) continue;

            if (sinceUtc != DateTime.MinValue && !string.IsNullOrEmpty(entry.ts))
            {
                if (DateTime.TryParse(entry.ts,
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.RoundtripKind,
                        out DateTime entryTs))
                {
                    if (entryTs < sinceUtc) continue;
                }
            }
            entries.Add(entry);
        }

        // Cap at 200 most-recent entries.
        if (entries.Count > 200)
            entries = entries.GetRange(entries.Count - 200, 200);

        var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "get_action_log");
        resp.action_log_result = new AgentBridgeActionLogResultDto
        {
            log_path = logPath,
            entries  = entries.ToArray(),
        };
        CompleteOrFail(repoRoot, commandId, JsonUtility.ToJson(resp, true));
    }

    // ── dispatch_action ───────────────────────────────────────────────────────

    /// <summary>
    /// <c>dispatch_action(action_id)</c> — synthetic bridge mutation; invokes
    /// <see cref="Territory.UI.Registry.UiActionRegistry"/> directly without OS event.
    /// Usable in PlayMode only (registry lives at runtime).
    /// </summary>
    static void RunDispatchAction(string repoRoot, string commandId, string requestJson)
    {
        if (!TryParseDispatchActionParams(requestJson, out var dto, out string parseErr))
        {
            TryFinalizeFailed(repoRoot, commandId, parseErr);
            return;
        }

        string actionId = dto?.action_id ?? string.Empty;
        if (string.IsNullOrWhiteSpace(actionId))
        {
            TryFinalizeFailed(repoRoot, commandId, "params_invalid:action_id (required)");
            return;
        }

        // Locate UiActionRegistry in the active scene.
        var registry = UnityEngine.Object.FindObjectOfType<Territory.UI.Registry.UiActionRegistry>();
        if (registry == null)
        {
            TryFinalizeFailed(repoRoot, commandId, "dispatch_action: UiActionRegistry not found in active scene — ensure PlayMode + registry mounted.");
            return;
        }

        bool dispatched = registry.Dispatch(actionId, null);

        string resultJson = $"{{\"action_id\":\"{EscapeJsonString(actionId)}\",\"dispatched\":{(dispatched ? "true" : "false")}}}";
        var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "dispatch_action");
        resp.mutation_result = resultJson;
        CompleteOrFail(repoRoot, commandId, JsonUtility.ToJson(resp, true));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static GameObject FindChildByName(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (string.Equals(child.gameObject.name, name, StringComparison.OrdinalIgnoreCase))
                return child.gameObject;
            var found = FindChildByName(child, name);
            if (found != null) return found;
        }
        return null;
    }

    static string BuildScenePath(Transform t)
    {
        if (t == null) return string.Empty;
        var stack = new System.Collections.Generic.List<string>();
        var cur = t;
        while (cur != null) { stack.Add(cur.gameObject.name); cur = cur.parent; }
        stack.Reverse();
        return string.Join("/", stack);
    }

    // ── Param parsers ─────────────────────────────────────────────────────────

    static bool TryParseReadPanelStateParams(string requestJson, out ReadPanelStateParamsDto dto, out string error)
    {
        dto   = new ReadPanelStateParamsDto();
        error = null;
        if (string.IsNullOrWhiteSpace(requestJson)) return true;
        string paramsJson = ExtractParamsJsonBlockQueries(requestJson);
        if (string.IsNullOrWhiteSpace(paramsJson)) return true;
        try { dto = JsonUtility.FromJson<ReadPanelStateParamsDto>(paramsJson) ?? dto; }
        catch (Exception ex) { error = $"params_invalid: {ex.Message}"; return false; }
        return true;
    }

    static bool TryParseGetActionLogParams(string requestJson, out GetActionLogParamsDto dto, out string error)
    {
        dto   = new GetActionLogParamsDto();
        error = null;
        if (string.IsNullOrWhiteSpace(requestJson)) return true;
        string paramsJson = ExtractParamsJsonBlockQueries(requestJson);
        if (string.IsNullOrWhiteSpace(paramsJson)) return true;
        try { dto = JsonUtility.FromJson<GetActionLogParamsDto>(paramsJson) ?? dto; }
        catch (Exception ex) { error = $"params_invalid: {ex.Message}"; return false; }
        return true;
    }

    static bool TryParseDispatchActionParams(string requestJson, out DispatchActionParamsDto dto, out string error)
    {
        dto   = new DispatchActionParamsDto();
        error = null;
        if (string.IsNullOrWhiteSpace(requestJson)) return true;
        string paramsJson = ExtractParamsJsonBlockQueries(requestJson);
        if (string.IsNullOrWhiteSpace(paramsJson)) return true;
        try { dto = JsonUtility.FromJson<DispatchActionParamsDto>(paramsJson) ?? dto; }
        catch (Exception ex) { error = $"params_invalid: {ex.Message}"; return false; }
        return true;
    }

    // Local copy of ExtractParamsJsonBlock to avoid cross-partial coupling.
    static string ExtractParamsJsonBlockQueries(string requestJson)
    {
        if (string.IsNullOrEmpty(requestJson)) return null;
        int keyIdx = requestJson.IndexOf("\"params\":", StringComparison.Ordinal);
        if (keyIdx < 0) keyIdx = requestJson.IndexOf("\"bridge_params\":", StringComparison.Ordinal);
        if (keyIdx < 0) return null;
        int braceStart = requestJson.IndexOf('{', keyIdx);
        if (braceStart < 0) return null;
        int depth = 0;
        for (int i = braceStart; i < requestJson.Length; i++)
        {
            char c = requestJson[i];
            if (c == '{') depth++;
            else if (c == '}') { depth--; if (depth == 0) return requestJson.Substring(braceStart, i - braceStart + 1); }
        }
        return null;
    }
}

// ── Param DTOs ────────────────────────────────────────────────────────────────

[Serializable]
class ReadPanelStateParamsDto
{
    public string panel_slug;
}

[Serializable]
class GetActionLogParamsDto
{
    /// <summary>ISO-8601 UTC lower bound; omit for tail entries.</summary>
    public string since;
}

[Serializable]
class DispatchActionParamsDto
{
    public string action_id;
}

// ── Response DTOs ─────────────────────────────────────────────────────────────

[Serializable]
public class AgentBridgePanelStateDto
{
    public string panel_slug;
    public bool   mounted;
    public string anchor_path;
    public int    child_count;
    public int    bind_count;
    public int    action_count;
    public bool   controller_alive;
}

[Serializable]
public class AgentBridgeActionLogResultDto
{
    public string                        log_path;
    public AgentBridgeActionLogEntryDto[] entries;
}

[Serializable]
public class AgentBridgeActionLogEntryDto
{
    public string action_id;
    public string handler_class;
    public string ts;
    public string marker;
}
