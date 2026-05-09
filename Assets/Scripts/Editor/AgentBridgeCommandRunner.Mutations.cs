using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Mutation-kind dispatch for <see cref="AgentBridgeCommandRunner"/> — Edit Mode only.
/// All Run* method bodies live in <see cref="MutationDispatchService"/> (POCO, Bridge.Editor assembly).
/// This partial retains shared helpers used by sibling partials
/// (SceneReplacePrefab.cs, Conformance.cs): <see cref="TryParseMutationParams{TDto}"/>,
/// <see cref="AssertEditMode"/>, <see cref="EscapeJsonString"/>,
/// <see cref="TryResolveGameObject"/>, <see cref="TryResolveComponentType"/>,
/// <see cref="TrySetSerializedProperty"/>, <see cref="TryParseVector3"/>,
/// <see cref="ExtractParamsJsonBlock"/>.
/// </summary>
public static partial class AgentBridgeCommandRunner
{
    // ── Mutation dispatch ────────────────────────────────────────────────────

    /// <summary>
    /// Dispatch a mutation kind from the command switch. Returns true if the kind was handled.
    /// </summary>
    internal static bool TryDispatchMutationKind(
        string kind,
        string repoRoot,
        string commandId,
        string requestJson)
    {
        var ctx = new MutationDispatchService.MutationContext
        {
            RepoRoot = repoRoot,
            CommandId = commandId,
            FinalizeFailed = TryFinalizeFailed,
            CompleteOrFail = CompleteOrFail,
        };
        return MutationDispatchService.Dispatch(ctx, kind, requestJson);
    }

    // ── Shared helpers (used by sibling partials) ────────────────────────────

    /// <summary>Resolve a scene-root-relative path in the active scene.</summary>
    static bool TryResolveGameObject(string path, out GameObject go, out string error)
    {
        go = null;
        error = null;
        if (string.IsNullOrWhiteSpace(path)) { error = "params_invalid:target_path (empty)"; return false; }
        go = GameObject.Find(path);
        if (go == null) { error = $"target_not_found:{path}"; return false; }
        return true;
    }

    /// <summary>Resolve a component type name across all loaded assemblies (case-insensitive short name).</summary>
    static bool TryResolveComponentType(string typeName, out Type type, out string error)
    {
        type = null;
        error = null;
        if (string.IsNullOrWhiteSpace(typeName)) { error = "params_invalid:component_type_name (empty)"; return false; }
        var candidates = new List<Type>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var t in assembly.GetTypes())
            {
                if (string.Equals(t.Name, typeName, StringComparison.OrdinalIgnoreCase) &&
                    typeof(Component).IsAssignableFrom(t))
                    candidates.Add(t);
            }
        }
        if (candidates.Count == 0) { error = $"type_not_found:{typeName}"; return false; }
        if (candidates.Count > 1)
        {
            var names = new System.Text.StringBuilder();
            for (int i = 0; i < candidates.Count; i++) { if (i > 0) names.Append(','); names.Append(candidates[i].FullName); }
            error = $"type_ambiguous:{typeName};candidates={names}";
            return false;
        }
        type = candidates[0];
        return true;
    }

    /// <summary>Validate that a mutation is being attempted only in Edit Mode.</summary>
    static bool AssertEditMode(out string error)
    {
        if (EditorApplication.isPlaying) { error = "edit_mode_required: mutation kinds are Edit Mode only"; return false; }
        error = null;
        return true;
    }

    /// <summary>
    /// Set a SerializedProperty value from a tagged-union
    /// (value_kind in object_ref | component_ref | asset_ref | int | float | bool | string | vector3).
    /// </summary>
    static bool TrySetSerializedProperty(
        SerializedProperty prop,
        string valueKind,
        string value,
        string valueObjectPath,
        out string error)
    {
        error = null;
        switch (valueKind)
        {
            case "object_ref":
            {
                if (string.IsNullOrWhiteSpace(valueObjectPath)) { error = "params_invalid:value_object_path (required for object_ref)"; return false; }
                GameObject refGo = GameObject.Find(valueObjectPath);
                if (refGo == null) { error = $"target_not_found:{valueObjectPath}"; return false; }
                prop.objectReferenceValue = refGo;
                return true;
            }
            case "component_ref":
            {
                if (string.IsNullOrWhiteSpace(valueObjectPath)) { error = "params_invalid:value_object_path (required for component_ref)"; return false; }
                if (string.IsNullOrWhiteSpace(value)) { error = "params_invalid:value (required for component_ref)"; return false; }
                GameObject refGo = GameObject.Find(valueObjectPath);
                if (refGo == null) { error = $"target_not_found:{valueObjectPath}"; return false; }
                if (!TryResolveComponentType(value, out Type compType, out string typeErr)) { error = typeErr; return false; }
                Component comp = refGo.GetComponent(compType);
                if (comp == null) { error = $"component_not_found:{value} on {valueObjectPath}"; return false; }
                prop.objectReferenceValue = comp;
                return true;
            }
            case "asset_ref":
            {
                if (string.IsNullOrWhiteSpace(valueObjectPath)) { error = "params_invalid:value_object_path (required for asset_ref)"; return false; }
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(valueObjectPath);
                if (asset == null) { error = $"asset_not_found:{valueObjectPath}"; return false; }
                prop.objectReferenceValue = asset;
                return true;
            }
            case "int":
            {
                if (!int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int iv))
                { error = $"params_invalid:value (cannot parse int: {value})"; return false; }
                prop.intValue = iv;
                return true;
            }
            case "float":
            {
                if (!float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float fv))
                { error = $"params_invalid:value (cannot parse float: {value})"; return false; }
                prop.floatValue = fv;
                return true;
            }
            case "bool":
            {
                if (!bool.TryParse(value, out bool bv)) { error = $"params_invalid:value (cannot parse bool: {value})"; return false; }
                prop.boolValue = bv;
                return true;
            }
            case "string":
            {
                prop.stringValue = value ?? string.Empty;
                return true;
            }
            case "vector3":
            {
                if (string.IsNullOrWhiteSpace(value)) { error = "params_invalid:value (vector3 expects \"x,y,z\")"; return false; }
                string[] parts = value.Split(',');
                if (parts.Length != 3 ||
                    !float.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float vx) ||
                    !float.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float vy) ||
                    !float.TryParse(parts[2].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float vz))
                { error = $"params_invalid:value (vector3 parse failed: {value})"; return false; }
                prop.vector3Value = new Vector3(vx, vy, vz);
                return true;
            }
            default:
                error = $"params_invalid:value_kind (unsupported: {valueKind}). Supported: object_ref, component_ref, asset_ref, int, float, bool, string, vector3";
                return false;
        }
    }

    static bool TryParseMutationParams<TDto>(string requestJson, out TDto dto, out string error)
        where TDto : class, new()
    {
        dto = null;
        error = null;
        if (string.IsNullOrWhiteSpace(requestJson)) { error = "params_invalid: empty request_json"; return false; }

        string normalized = requestJson.Replace("\"params\":", "\"bridge_params\":", StringComparison.Ordinal);
        AgentBridgeRequestEnvelopeDto env = null;
        try { env = UnityEngine.JsonUtility.FromJson<AgentBridgeRequestEnvelopeDto>(normalized); }
        catch (Exception ex) { error = $"params_invalid: {ex.Message}"; return false; }
        if (env == null) { error = "params_invalid: null envelope"; return false; }

        string paramsJson = ExtractParamsJsonBlock(requestJson);
        if (string.IsNullOrWhiteSpace(paramsJson)) { dto = new TDto(); return true; }

        try { dto = UnityEngine.JsonUtility.FromJson<TDto>(paramsJson); }
        catch (Exception ex) { error = $"params_invalid: {ex.Message}"; return false; }

        if (dto == null) dto = new TDto();
        return true;
    }

    /// <summary>Extract the raw JSON object value of the "params" key from a request envelope JSON string.</summary>
    static string ExtractParamsJsonBlock(string requestJson)
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

    static bool TryParseVector3(string raw, out Vector3 v)
    {
        v = Vector3.zero;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        string[] parts = raw.Split(',');
        if (parts.Length != 3) return false;
        return float.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v.x) &&
               float.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v.y) &&
               float.TryParse(parts[2].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v.z);
    }

    /// <summary>Escape a string for embedding inside a JSON string literal.</summary>
    static string EscapeJsonString(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    }
}

// ── Mutation param DTOs (kept for sibling partials: SceneReplacePrefab.cs uses TryParseMutationParams) ──

[Serializable]
class AttachComponentParamsDto
{
    public string target_path;
    public string component_type_name;
}

[Serializable]
class RemoveComponentParamsDto
{
    public string target_path;
    public string component_type_name;
}

/// <summary>
/// value_kind in object_ref | component_ref | asset_ref | int | float | bool | string | vector3.
/// </summary>
[Serializable]
class AssignSerializedFieldParamsDto
{
    public string target_path;
    public string component_type_name;
    public string field_name;
    public string value_kind;
    public string value;
    /// <summary>Scene path (for object_ref) or asset path (for asset_ref).</summary>
    public string value_object_path;
}

[Serializable]
class ExecuteMenuItemParamsDto
{
    public string menu_path;
}

[Serializable]
class CreateGameObjectParamsDto
{
    public string name;
    public string parent_path;
    public string position;
}

[Serializable]
class DeleteGameObjectParamsDto
{
    public string target_path;
}

[Serializable]
class FindGameObjectParamsDto
{
    public string target_path;
}

[Serializable]
class SetTransformParamsDto
{
    public string target_path;
    public string position;
    public string rotation;
    public string scale;
}

[Serializable]
class SetGameObjectActiveParamsDto
{
    public string target_path;
    public bool active;
}

[Serializable]
class SetGameObjectParentParamsDto
{
    public string target_path;
    public string new_parent_path;
    public bool world_position_stays;
}

[Serializable]
class SetPanelVisibleParamsDto
{
    public string slug;
    public bool active;
}

[Serializable]
class SaveSceneParamsDto
{
    /// <summary>Asset path to scene. Omit for active scene.</summary>
    public string scene_path;
}

[Serializable]
class OpenSceneParamsDto
{
    public string scene_path;
    /// <summary>"single" (default) or "additive".</summary>
    public string mode;
}

[Serializable]
class NewSceneParamsDto
{
    /// <summary>"default_game_objects" (default) or "empty_scene".</summary>
    public string setup_mode;
    /// <summary>"single" (default) or "additive".</summary>
    public string mode;
}

[Serializable]
class InstantiatePrefabParamsDto
{
    public string prefab_path;
    public string parent_path;
    public string position;
}

[Serializable]
class ApplyPrefabOverridesParamsDto
{
    public string target_path;
    /// <summary>Reserved for future use.</summary>
    public string interaction_mode;
}

[Serializable]
class CreateScriptableObjectParamsDto
{
    public string type_name;
    public string asset_path;
}

[Serializable]
class FieldWriteDto
{
    public string field_name;
    public string value_kind;
    public string value;
    public string value_object_path;
}

[Serializable]
class ModifyScriptableObjectParamsDto
{
    public string asset_path;
    public FieldWriteDto[] field_writes;
}

[Serializable]
class MoveAssetParamsDto
{
    public string asset_path;
    public string new_path;
}

[Serializable]
class DeleteAssetParamsDto
{
    public string asset_path;
}

[Serializable]
class BakeUiFromIrParamsDto
{
    /// <summary>Absolute or repo-relative path to IR JSON.</summary>
    public string ir_path;
    /// <summary>Output dir for generated placeholder prefabs.</summary>
    public string out_dir;
    /// <summary>Asset path to UiTheme ScriptableObject.</summary>
    public string theme_so;
}

[Serializable]
class WireAssetFromCatalogParamsDto
{
    /// <summary>Catalog entity_id to wire onto the scene.</summary>
    public string entity_id;
    /// <summary>Cell coordinate token, e.g. "5_7".</summary>
    public string cell_xy;
    /// <summary>When true, returns proposed mutations without scene mutation.</summary>
    public bool dry_run;
}
