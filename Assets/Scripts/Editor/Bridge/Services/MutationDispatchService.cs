using System;
using System.Collections.Generic;
using Territory.UI;
using Territory.UI.Themed;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// POCO dispatch table for all bridge mutation kinds (Editor-only).
/// Lives in the implicit editor assembly so it can reach Territory.Editor.Bridge.*,
/// Territory.UI.Themed.*, Territory.Catalog.*, CatalogPrefabRef without asmdef references.
/// Receives terminal callbacks (FinalizeFailed, CompleteOrFail) via MutationContext
/// so it has no dependency on AgentBridgeResponseFileDto or EditorPostgresBridgeJobs.
/// Owns full body for 24 mutation kinds: attach_component, remove_component,
/// assign_serialized_field, create_gameobject, delete_gameobject, find_gameobject,
/// set_transform, set_gameobject_active, set_gameobject_parent, save_scene,
/// open_scene, new_scene, instantiate_prefab, apply_prefab_overrides,
/// create_scriptable_object, modify_scriptable_object, refresh_asset_database,
/// move_asset, delete_asset, execute_menu_item, bake_ui_from_ir,
/// wire_asset_from_catalog, set_panel_visible, scene_replace_with_prefab.
/// </summary>
public static class MutationDispatchService
{
    /// <summary>Per-kind dispatch context. Callbacks bound by AgentBridgeCommandRunner.Mutations.cs.</summary>
    public struct MutationContext
    {
        public string RepoRoot;
        public string CommandId;
        /// <summary>Bound to AgentBridgeCommandRunner.TryFinalizeFailed (repoRoot, commandId, error).</summary>
        public Action<string, string, string> FinalizeFailed;
        /// <summary>Bound to AgentBridgeCommandRunner.CompleteOrFail (repoRoot, commandId, responseJson).</summary>
        public Action<string, string, string> CompleteOrFail;
    }

    // ── Public entry point ───────────────────────────────────────────────────

    /// <summary>
    /// Dispatch a mutation kind. Returns true when the kind was handled.
    /// </summary>
    public static bool Dispatch(MutationContext ctx, string kind, string requestJson)
    {
        switch (kind)
        {
            case "attach_component":          RunAttachComponent(ctx, requestJson);         return true;
            case "remove_component":          RunRemoveComponent(ctx, requestJson);         return true;
            case "assign_serialized_field":   RunAssignSerializedField(ctx, requestJson);   return true;
            case "create_gameobject":         RunCreateGameObject(ctx, requestJson);        return true;
            case "delete_gameobject":         RunDeleteGameObject(ctx, requestJson);        return true;
            case "find_gameobject":           RunFindGameObject(ctx, requestJson);          return true;
            case "set_transform":             RunSetTransform(ctx, requestJson);            return true;
            case "set_gameobject_active":     RunSetGameObjectActive(ctx, requestJson);     return true;
            case "set_gameobject_parent":     RunSetGameObjectParent(ctx, requestJson);     return true;
            case "save_scene":                RunSaveScene(ctx, requestJson);               return true;
            case "open_scene":                RunOpenScene(ctx, requestJson);               return true;
            case "new_scene":                 RunNewScene(ctx, requestJson);                return true;
            case "instantiate_prefab":        RunInstantiatePrefab(ctx, requestJson);       return true;
            case "apply_prefab_overrides":    RunApplyPrefabOverrides(ctx, requestJson);    return true;
            case "create_scriptable_object":  RunCreateScriptableObject(ctx, requestJson);  return true;
            case "modify_scriptable_object":  RunModifyScriptableObject(ctx, requestJson);  return true;
            case "refresh_asset_database":    RunRefreshAssetDatabase(ctx);                 return true;
            case "move_asset":                RunMoveAsset(ctx, requestJson);               return true;
            case "delete_asset":              RunDeleteAsset(ctx, requestJson);             return true;
            case "execute_menu_item":         RunExecuteMenuItem(ctx, requestJson);         return true;
            case "bake_ui_from_ir":           RunBakeUiFromIr(ctx, requestJson);            return true;
            case "wire_asset_from_catalog":   RunWireAssetFromCatalog(ctx, requestJson);    return true;
            case "set_panel_visible":         RunSetPanelVisible(ctx, requestJson);         return true;
            case "scene_replace_with_prefab": RunSceneReplaceWithPrefab(ctx, requestJson);  return true;
            default:                          return false;
        }
    }

    // ── Terminal op helpers ──────────────────────────────────────────────────

    static void Fail(MutationContext ctx, string error)
        => ctx.FinalizeFailed(ctx.RepoRoot, ctx.CommandId, error);

    static void Complete(MutationContext ctx, string storage, string mutationResultJson)
    {
        string json = BuildOkJson(ctx.CommandId, storage, mutationResultJson);
        ctx.CompleteOrFail(ctx.RepoRoot, ctx.CommandId, json);
    }

    // ── Minimal response envelope ────────────────────────────────────────────

    [Serializable]
    sealed class MutationResponseEnvelope
    {
        public int schema_version = 1;
        public string artifact = "unity_agent_bridge_response";
        public string command_id;
        public bool ok = true;
        public string completed_at_utc;
        public string storage;
        public bool postgres_only = false;
        public string error = "";
        public string mutation_result;
    }

    static string BuildOkJson(string commandId, string storage, string mutationResultJson)
    {
        var env = new MutationResponseEnvelope
        {
            command_id = commandId,
            completed_at_utc = DateTime.UtcNow.ToString("o"),
            storage = storage,
            mutation_result = mutationResultJson,
        };
        return UnityEngine.JsonUtility.ToJson(env, true);
    }

    // ── GO / Component resolution ────────────────────────────────────────────

    static bool TryResolveGameObject(string path, out GameObject go, out string error)
    {
        go = null;
        error = null;
        if (string.IsNullOrWhiteSpace(path)) { error = "params_invalid:target_path (empty)"; return false; }
        go = GameObject.Find(path);
        if (go == null) { error = $"target_not_found:{path}"; return false; }
        return true;
    }

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
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < candidates.Count; i++) { if (i > 0) sb.Append(','); sb.Append(candidates[i].FullName); }
            error = $"type_ambiguous:{typeName};candidates={sb}";
            return false;
        }
        type = candidates[0];
        return true;
    }

    static bool AssertEditMode(out string error)
    {
        if (EditorApplication.isPlaying) { error = "edit_mode_required: mutation kinds are Edit Mode only"; return false; }
        error = null;
        return true;
    }

    static bool TrySetSerializedProperty(SerializedProperty prop, string valueKind, string value, string valueObjectPath, out string error)
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

    static bool TryParseParams<TDto>(string requestJson, out TDto dto, out string error) where TDto : class, new()
    {
        dto = null;
        error = null;
        if (string.IsNullOrWhiteSpace(requestJson)) { error = "params_invalid: empty request_json"; return false; }
        string paramsJson = ExtractParamsJsonBlock(requestJson);
        if (string.IsNullOrWhiteSpace(paramsJson)) { dto = new TDto(); return true; }
        try { dto = UnityEngine.JsonUtility.FromJson<TDto>(paramsJson); }
        catch (Exception ex) { error = $"params_invalid: {ex.Message}"; return false; }
        if (dto == null) dto = new TDto();
        return true;
    }

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

    static string EscapeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    }

    static string GetGoPath(GameObject go)
    {
        string path = go.name;
        Transform parent = go.transform.parent;
        while (parent != null) { path = parent.name + "/" + path; parent = parent.parent; }
        return path;
    }

    // ── Phase 1: Component lifecycle ─────────────────────────────────────────

    static void RunAttachComponent(MutationContext ctx, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { Fail(ctx, modeErr); return; }
        if (!TryParseParams<AttachComponentParams>(requestJson, out var dto, out string parseErr)) { Fail(ctx, parseErr); return; }
        if (string.IsNullOrWhiteSpace(dto.target_path)) { Fail(ctx, "params_invalid:target_path"); return; }
        if (string.IsNullOrWhiteSpace(dto.component_type_name)) { Fail(ctx, "params_invalid:component_type_name"); return; }
        if (!TryResolveGameObject(dto.target_path, out GameObject go, out string goErr)) { Fail(ctx, goErr); return; }
        if (!TryResolveComponentType(dto.component_type_name, out Type compType, out string typeErr)) { Fail(ctx, typeErr); return; }
        Component added = go.AddComponent(compType);
        EditorSceneManager.MarkSceneDirty(go.scene);
        Complete(ctx, "attach_component", $"{{\"attached\":\"{compType.FullName}\",\"instance_id\":{added.GetInstanceID()},\"game_object\":\"{EscapeJson(go.name)}\"}}");
    }

    static void RunRemoveComponent(MutationContext ctx, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { Fail(ctx, modeErr); return; }
        if (!TryParseParams<RemoveComponentParams>(requestJson, out var dto, out string parseErr)) { Fail(ctx, parseErr); return; }
        if (string.IsNullOrWhiteSpace(dto.target_path)) { Fail(ctx, "params_invalid:target_path"); return; }
        if (string.IsNullOrWhiteSpace(dto.component_type_name)) { Fail(ctx, "params_invalid:component_type_name"); return; }
        if (!TryResolveGameObject(dto.target_path, out GameObject go, out string goErr)) { Fail(ctx, goErr); return; }
        if (!TryResolveComponentType(dto.component_type_name, out Type compType, out string typeErr)) { Fail(ctx, typeErr); return; }
        Component comp = go.GetComponent(compType);
        if (comp == null) { Fail(ctx, $"component_not_found:{dto.component_type_name} on {dto.target_path}"); return; }
        UnityEngine.Object.DestroyImmediate(comp);
        EditorSceneManager.MarkSceneDirty(go.scene);
        Complete(ctx, "remove_component", $"{{\"removed\":\"{compType.FullName}\",\"game_object\":\"{EscapeJson(go.name)}\"}}");
    }

    static void RunAssignSerializedField(MutationContext ctx, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { Fail(ctx, modeErr); return; }
        if (!TryParseParams<AssignSerializedFieldParams>(requestJson, out var dto, out string parseErr)) { Fail(ctx, parseErr); return; }
        if (string.IsNullOrWhiteSpace(dto.target_path)) { Fail(ctx, "params_invalid:target_path"); return; }
        if (string.IsNullOrWhiteSpace(dto.component_type_name)) { Fail(ctx, "params_invalid:component_type_name"); return; }
        if (string.IsNullOrWhiteSpace(dto.field_name)) { Fail(ctx, "params_invalid:field_name"); return; }
        if (string.IsNullOrWhiteSpace(dto.value_kind)) { Fail(ctx, "params_invalid:value_kind"); return; }
        if (!TryResolveGameObject(dto.target_path, out GameObject go, out string goErr)) { Fail(ctx, goErr); return; }
        if (!TryResolveComponentType(dto.component_type_name, out Type compType, out string typeErr)) { Fail(ctx, typeErr); return; }
        Component comp = go.GetComponent(compType);
        if (comp == null) { Fail(ctx, $"component_not_found:{dto.component_type_name} on {dto.target_path}"); return; }
        var so = new SerializedObject(comp);
        SerializedProperty prop = so.FindProperty(dto.field_name);
        if (prop == null) { Fail(ctx, $"field_not_found:{dto.field_name} on {dto.component_type_name}"); return; }
        if (!TrySetSerializedProperty(prop, dto.value_kind, dto.value, dto.value_object_path, out string setErr)) { Fail(ctx, setErr); return; }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(go.scene);
        Complete(ctx, "assign_serialized_field", $"{{\"field\":\"{EscapeJson(dto.field_name)}\",\"value_kind\":\"{EscapeJson(dto.value_kind)}\",\"component\":\"{compType.FullName}\",\"game_object\":\"{EscapeJson(go.name)}\"}}");
    }

    // ── Phase 1: Catch-all ────────────────────────────────────────────────────

    static void RunExecuteMenuItem(MutationContext ctx, string requestJson)
    {
        if (!TryParseParams<ExecuteMenuItemParams>(requestJson, out var dto, out string parseErr)) { Fail(ctx, parseErr); return; }
        if (string.IsNullOrWhiteSpace(dto.menu_path)) { Fail(ctx, "params_invalid:menu_path"); return; }
        bool executed = EditorApplication.ExecuteMenuItem(dto.menu_path);
        if (!executed) { Fail(ctx, $"menu_not_found:{dto.menu_path}"); return; }
        Complete(ctx, "execute_menu_item", $"{{\"menu_path\":\"{EscapeJson(dto.menu_path)}\"}}");
    }

    // ── Phase 2: GameObject lifecycle ────────────────────────────────────────

    static void RunCreateGameObject(MutationContext ctx, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { Fail(ctx, modeErr); return; }
        if (!TryParseParams<CreateGameObjectParams>(requestJson, out var dto, out string parseErr)) { Fail(ctx, parseErr); return; }
        if (string.IsNullOrWhiteSpace(dto.name)) { Fail(ctx, "params_invalid:name"); return; }
        var go = new GameObject(dto.name);
        if (!string.IsNullOrWhiteSpace(dto.parent_path))
        {
            if (!TryResolveGameObject(dto.parent_path, out GameObject parent, out string parentErr))
            { UnityEngine.Object.DestroyImmediate(go); Fail(ctx, parentErr); return; }
            go.transform.SetParent(parent.transform, false);
        }
        if (!string.IsNullOrWhiteSpace(dto.position) && TryParseVector3(dto.position, out Vector3 pos))
            go.transform.localPosition = pos;
        EditorSceneManager.MarkSceneDirty(go.scene);
        Complete(ctx, "create_gameobject", $"{{\"name\":\"{EscapeJson(go.name)}\",\"path\":\"{EscapeJson(GetGoPath(go))}\",\"instance_id\":{go.GetInstanceID()}}}");
    }

    static void RunDeleteGameObject(MutationContext ctx, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { Fail(ctx, modeErr); return; }
        if (!TryParseParams<DeleteGameObjectParams>(requestJson, out var dto, out string parseErr)) { Fail(ctx, parseErr); return; }
        if (!TryResolveGameObject(dto.target_path, out GameObject go, out string goErr)) { Fail(ctx, goErr); return; }
        Scene scene = go.scene;
        string name = go.name;
        UnityEngine.Object.DestroyImmediate(go);
        EditorSceneManager.MarkSceneDirty(scene);
        Complete(ctx, "delete_gameobject", $"{{\"deleted\":\"{EscapeJson(dto.target_path)}\",\"name\":\"{EscapeJson(name)}\"}}");
    }

    static void RunFindGameObject(MutationContext ctx, string requestJson)
    {
        if (!TryParseParams<FindGameObjectParams>(requestJson, out var dto, out string parseErr)) { Fail(ctx, parseErr); return; }
        if (string.IsNullOrWhiteSpace(dto.target_path)) { Fail(ctx, "params_invalid:target_path"); return; }
        GameObject go = GameObject.Find(dto.target_path);
        bool exists = go != null;
        string componentsJson = "[]";
        int childrenCount = 0;
        if (exists)
        {
            childrenCount = go.transform.childCount;
            var compNames = new List<string>();
            foreach (Component c in go.GetComponents<Component>()) { if (c != null) compNames.Add(c.GetType().Name); }
            var sb = new System.Text.StringBuilder("[");
            for (int i = 0; i < compNames.Count; i++) { if (i > 0) sb.Append(','); sb.Append('"'); sb.Append(EscapeJson(compNames[i])); sb.Append('"'); }
            sb.Append(']');
            componentsJson = sb.ToString();
        }
        Complete(ctx, "find_gameobject", $"{{\"path\":\"{EscapeJson(dto.target_path)}\",\"exists\":{(exists ? "true" : "false")},\"components\":{componentsJson},\"children_count\":{childrenCount}}}");
    }

    static void RunSetTransform(MutationContext ctx, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { Fail(ctx, modeErr); return; }
        if (!TryParseParams<SetTransformParams>(requestJson, out var dto, out string parseErr)) { Fail(ctx, parseErr); return; }
        if (!TryResolveGameObject(dto.target_path, out GameObject go, out string goErr)) { Fail(ctx, goErr); return; }
        var so = new SerializedObject(go.transform);
        if (!string.IsNullOrWhiteSpace(dto.position) && TryParseVector3(dto.position, out Vector3 pos))
        { SerializedProperty p = so.FindProperty("m_LocalPosition"); if (p != null) p.vector3Value = pos; }
        if (!string.IsNullOrWhiteSpace(dto.rotation) && TryParseVector3(dto.rotation, out Vector3 rot))
        { SerializedProperty p = so.FindProperty("m_LocalRotation"); if (p != null) p.quaternionValue = Quaternion.Euler(rot); }
        if (!string.IsNullOrWhiteSpace(dto.scale) && TryParseVector3(dto.scale, out Vector3 scl))
        { SerializedProperty p = so.FindProperty("m_LocalScale"); if (p != null) p.vector3Value = scl; }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(go.scene);
        Complete(ctx, "set_transform", $"{{\"game_object\":\"{EscapeJson(go.name)}\"}}");
    }

    static void RunSetGameObjectActive(MutationContext ctx, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { Fail(ctx, modeErr); return; }
        if (!TryParseParams<SetGameObjectActiveParams>(requestJson, out var dto, out string parseErr)) { Fail(ctx, parseErr); return; }
        if (!TryResolveGameObject(dto.target_path, out GameObject go, out string goErr)) { Fail(ctx, goErr); return; }
        go.SetActive(dto.active);
        EditorSceneManager.MarkSceneDirty(go.scene);
        Complete(ctx, "set_gameobject_active", $"{{\"game_object\":\"{EscapeJson(go.name)}\",\"active\":{(dto.active ? "true" : "false")}}}");
    }

    static void RunSetGameObjectParent(MutationContext ctx, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { Fail(ctx, modeErr); return; }
        if (!TryParseParams<SetGameObjectParentParams>(requestJson, out var dto, out string parseErr)) { Fail(ctx, parseErr); return; }
        if (!TryResolveGameObject(dto.target_path, out GameObject go, out string goErr)) { Fail(ctx, goErr); return; }
        Transform newParent = null;
        if (!string.IsNullOrWhiteSpace(dto.new_parent_path))
        {
            if (!TryResolveGameObject(dto.new_parent_path, out GameObject parentGo, out string parentErr)) { Fail(ctx, parentErr); return; }
            newParent = parentGo.transform;
        }
        go.transform.SetParent(newParent, dto.world_position_stays);
        EditorSceneManager.MarkSceneDirty(go.scene);
        Complete(ctx, "set_gameobject_parent", $"{{\"game_object\":\"{EscapeJson(go.name)}\",\"new_parent\":\"{EscapeJson(dto.new_parent_path ?? "(root)")}\"}}");
    }

    // ── Game UI runtime toggle (Play Mode allowed) ────────────────────────────

    static void RunSetPanelVisible(MutationContext ctx, string requestJson)
    {
        if (!TryParseParams<SetPanelVisibleParams>(requestJson, out var dto, out string parseErr)) { Fail(ctx, parseErr); return; }
        if (string.IsNullOrWhiteSpace(dto.slug)) { Fail(ctx, "params_invalid:slug"); return; }
        var panels = UnityEngine.Object.FindObjectsOfType<ThemedPanel>(includeInactive: true);
        ThemedPanel match = null;
        for (int i = 0; i < panels.Length; i++) { if (panels[i] != null && panels[i].gameObject.name == dto.slug) { match = panels[i]; break; } }
        if (match == null) { Fail(ctx, $"panel_not_found:slug={dto.slug}"); return; }
        match.gameObject.SetActive(dto.active);
        if (!EditorApplication.isPlaying) EditorSceneManager.MarkSceneDirty(match.gameObject.scene);
        Complete(ctx, "set_panel_visible", $"{{\"slug\":\"{EscapeJson(dto.slug)}\",\"active\":{(dto.active ? "true" : "false")},\"play_mode\":{(EditorApplication.isPlaying ? "true" : "false")}}}");
    }

    // ── Phase 2: Scene lifecycle ──────────────────────────────────────────────

    static void RunSaveScene(MutationContext ctx, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { Fail(ctx, modeErr); return; }
        if (!TryParseParams<SaveSceneParams>(requestJson, out var dto, out string parseErr)) { Fail(ctx, parseErr); return; }
        Scene scene;
        if (!string.IsNullOrWhiteSpace(dto.scene_path))
        {
            scene = SceneManager.GetSceneByPath(dto.scene_path);
            if (!scene.IsValid()) { Fail(ctx, $"scene_not_found:{dto.scene_path}"); return; }
        }
        else
        {
            scene = SceneManager.GetActiveScene();
        }
        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene, scene.path);
        if (!saved) { Fail(ctx, $"save_failed:{scene.path}"); return; }
        Complete(ctx, "save_scene", $"{{\"scene_path\":\"{EscapeJson(scene.path)}\"}}");
    }

    static void RunOpenScene(MutationContext ctx, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { Fail(ctx, modeErr); return; }
        if (!TryParseParams<OpenSceneParams>(requestJson, out var dto, out string parseErr)) { Fail(ctx, parseErr); return; }
        if (string.IsNullOrWhiteSpace(dto.scene_path)) { Fail(ctx, "params_invalid:scene_path"); return; }
        OpenSceneMode mode = string.Equals(dto.mode, "additive", StringComparison.OrdinalIgnoreCase)
            ? OpenSceneMode.Additive : OpenSceneMode.Single;
        Scene opened = EditorSceneManager.OpenScene(dto.scene_path, mode);
        if (!opened.IsValid()) { Fail(ctx, $"open_scene_failed:{dto.scene_path}"); return; }
        Complete(ctx, "open_scene", $"{{\"scene_path\":\"{EscapeJson(opened.path)}\",\"mode\":\"{EscapeJson(dto.mode ?? "single")}\"}}");
    }

    static void RunNewScene(MutationContext ctx, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { Fail(ctx, modeErr); return; }
        if (!TryParseParams<NewSceneParams>(requestJson, out var dto, out string parseErr)) { Fail(ctx, parseErr); return; }
        NewSceneSetup setup = string.Equals(dto.setup_mode, "empty_scene", StringComparison.OrdinalIgnoreCase)
            ? NewSceneSetup.EmptyScene : NewSceneSetup.DefaultGameObjects;
        NewSceneMode mode = string.Equals(dto.mode, "additive", StringComparison.OrdinalIgnoreCase)
            ? NewSceneMode.Additive : NewSceneMode.Single;
        Scene created = EditorSceneManager.NewScene(setup, mode);
        Complete(ctx, "new_scene", $"{{\"scene_name\":\"{EscapeJson(created.name)}\",\"setup_mode\":\"{EscapeJson(dto.setup_mode ?? "default_game_objects")}\",\"mode\":\"{EscapeJson(dto.mode ?? "single")}\"}}");
    }

    // ── Phase 3: Prefab lifecycle ─────────────────────────────────────────────

    static void RunInstantiatePrefab(MutationContext ctx, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { Fail(ctx, modeErr); return; }
        if (!TryParseParams<InstantiatePrefabParams>(requestJson, out var dto, out string parseErr)) { Fail(ctx, parseErr); return; }
        if (string.IsNullOrWhiteSpace(dto.prefab_path)) { Fail(ctx, "params_invalid:prefab_path"); return; }
        var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(dto.prefab_path);
        if (prefabAsset == null) { Fail(ctx, $"asset_not_found:{dto.prefab_path}"); return; }
        Transform parent = null;
        if (!string.IsNullOrWhiteSpace(dto.parent_path))
        {
            if (!TryResolveGameObject(dto.parent_path, out GameObject parentGo, out string parentErr)) { Fail(ctx, parentErr); return; }
            parent = parentGo.transform;
        }
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, parent);
        if (!string.IsNullOrWhiteSpace(dto.position) && TryParseVector3(dto.position, out Vector3 pos))
            instance.transform.localPosition = pos;
        EditorSceneManager.MarkSceneDirty(instance.scene);
        Complete(ctx, "instantiate_prefab", $"{{\"name\":\"{EscapeJson(instance.name)}\",\"path\":\"{EscapeJson(GetGoPath(instance))}\",\"instance_id\":{instance.GetInstanceID()}}}");
    }

    static void RunApplyPrefabOverrides(MutationContext ctx, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { Fail(ctx, modeErr); return; }
        if (!TryParseParams<ApplyPrefabOverridesParams>(requestJson, out var dto, out string parseErr)) { Fail(ctx, parseErr); return; }
        if (!TryResolveGameObject(dto.target_path, out GameObject go, out string goErr)) { Fail(ctx, goErr); return; }
        PrefabUtility.ApplyPrefabInstance(go, InteractionMode.AutomatedAction);
        EditorSceneManager.MarkSceneDirty(go.scene);
        Complete(ctx, "apply_prefab_overrides", $"{{\"game_object\":\"{EscapeJson(go.name)}\"}}");
    }

    // ── Phase 3: Asset lifecycle ──────────────────────────────────────────────

    static void RunCreateScriptableObject(MutationContext ctx, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { Fail(ctx, modeErr); return; }
        if (!TryParseParams<CreateScriptableObjectParams>(requestJson, out var dto, out string parseErr)) { Fail(ctx, parseErr); return; }
        if (string.IsNullOrWhiteSpace(dto.type_name)) { Fail(ctx, "params_invalid:type_name"); return; }
        if (string.IsNullOrWhiteSpace(dto.asset_path)) { Fail(ctx, "params_invalid:asset_path"); return; }
        var candidates = new List<Type>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var t in assembly.GetTypes())
            {
                if (string.Equals(t.Name, dto.type_name, StringComparison.OrdinalIgnoreCase) &&
                    typeof(ScriptableObject).IsAssignableFrom(t))
                    candidates.Add(t);
            }
        }
        if (candidates.Count == 0) { Fail(ctx, $"type_not_found:{dto.type_name}"); return; }
        if (candidates.Count > 1)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < candidates.Count; i++) { if (i > 0) sb.Append(','); sb.Append(candidates[i].FullName); }
            Fail(ctx, $"type_ambiguous:{dto.type_name};candidates={sb}");
            return;
        }
        Type soType = candidates[0];
        ScriptableObject so = ScriptableObject.CreateInstance(soType);
        AssetDatabase.CreateAsset(so, dto.asset_path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Complete(ctx, "create_scriptable_object", $"{{\"type\":\"{EscapeJson(soType.FullName)}\",\"asset_path\":\"{EscapeJson(dto.asset_path)}\"}}");
    }

    static void RunModifyScriptableObject(MutationContext ctx, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { Fail(ctx, modeErr); return; }
        if (!TryParseParams<ModifyScriptableObjectParams>(requestJson, out var dto, out string parseErr)) { Fail(ctx, parseErr); return; }
        if (string.IsNullOrWhiteSpace(dto.asset_path)) { Fail(ctx, "params_invalid:asset_path"); return; }
        if (dto.field_writes == null || dto.field_writes.Length == 0) { Fail(ctx, "params_invalid:field_writes (empty)"); return; }
        ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(dto.asset_path);
        if (asset == null) { Fail(ctx, $"asset_not_found:{dto.asset_path}"); return; }
        var so = new SerializedObject(asset);
        var applied = new List<string>();
        foreach (var fw in dto.field_writes)
        {
            if (fw == null || string.IsNullOrWhiteSpace(fw.field_name)) continue;
            SerializedProperty prop = so.FindProperty(fw.field_name);
            if (prop == null) { Fail(ctx, $"field_not_found:{fw.field_name} on {dto.asset_path}"); return; }
            if (!TrySetSerializedProperty(prop, fw.value_kind, fw.value, fw.value_object_path, out string setErr)) { Fail(ctx, setErr); return; }
            applied.Add(fw.field_name);
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Complete(ctx, "modify_scriptable_object", $"{{\"asset_path\":\"{EscapeJson(dto.asset_path)}\",\"fields_written\":{applied.Count}}}");
    }

    static void RunRefreshAssetDatabase(MutationContext ctx)
    {
        AssetDatabase.Refresh();
        Complete(ctx, "refresh_asset_database", "{\"refreshed\":true}");
    }

    static void RunMoveAsset(MutationContext ctx, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { Fail(ctx, modeErr); return; }
        if (!TryParseParams<MoveAssetParams>(requestJson, out var dto, out string parseErr)) { Fail(ctx, parseErr); return; }
        if (string.IsNullOrWhiteSpace(dto.asset_path)) { Fail(ctx, "params_invalid:asset_path"); return; }
        if (string.IsNullOrWhiteSpace(dto.new_path)) { Fail(ctx, "params_invalid:new_path"); return; }
        string moveError = AssetDatabase.MoveAsset(dto.asset_path, dto.new_path);
        if (!string.IsNullOrEmpty(moveError)) { Fail(ctx, $"move_failed:{moveError}"); return; }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Complete(ctx, "move_asset", $"{{\"from\":\"{EscapeJson(dto.asset_path)}\",\"to\":\"{EscapeJson(dto.new_path)}\"}}");
    }

    static void RunDeleteAsset(MutationContext ctx, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { Fail(ctx, modeErr); return; }
        if (!TryParseParams<DeleteAssetParams>(requestJson, out var dto, out string parseErr)) { Fail(ctx, parseErr); return; }
        if (string.IsNullOrWhiteSpace(dto.asset_path)) { Fail(ctx, "params_invalid:asset_path"); return; }
        bool deleted = AssetDatabase.DeleteAsset(dto.asset_path);
        if (!deleted) { Fail(ctx, $"delete_failed:{dto.asset_path}"); return; }
        AssetDatabase.Refresh();
        Complete(ctx, "delete_asset", $"{{\"deleted\":\"{EscapeJson(dto.asset_path)}\"}}");
    }

    // ── Game UI bake ──────────────────────────────────────────────────────────

    static void RunBakeUiFromIr(MutationContext ctx, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { Fail(ctx, modeErr); return; }
        if (!TryParseParams<BakeUiFromIrParams>(requestJson, out var dto, out string parseErr)) { Fail(ctx, parseErr); return; }
        if (string.IsNullOrWhiteSpace(dto.ir_path)) { Fail(ctx, "missing_arg:ir_path"); return; }
        if (string.IsNullOrWhiteSpace(dto.out_dir)) { Fail(ctx, "missing_arg:out_dir"); return; }
        if (string.IsNullOrWhiteSpace(dto.theme_so)) { Fail(ctx, "missing_arg:theme_so"); return; }
        var bakeArgs = new Territory.Editor.Bridge.UiBakeHandler.BakeArgs
        {
            ir_path = dto.ir_path,
            out_dir = dto.out_dir,
            theme_so = dto.theme_so,
        };
        Territory.Editor.Bridge.UiBakeHandler.BakeResult result;
        try { result = Territory.Editor.Bridge.UiBakeHandler.Bake(bakeArgs); }
        catch (Exception ex) { Fail(ctx, $"bake_threw:{ex.GetType().Name}:{ex.Message}"); return; }
        if (result == null) { Fail(ctx, "bake_null_result"); return; }
        if (result.error != null) { Fail(ctx, $"{result.error.error}:{result.error.details}@{result.error.path}"); return; }
        var warningsJson = new System.Text.StringBuilder("[");
        if (result.warnings != null)
        {
            for (int i = 0; i < result.warnings.Count; i++)
            {
                var w = result.warnings[i];
                if (i > 0) warningsJson.Append(",");
                warningsJson.Append($"{{\"error\":\"{EscapeJson(w.error)}\",\"details\":\"{EscapeJson(w.details)}\",\"path\":\"{EscapeJson(w.path)}\"}}");
            }
        }
        warningsJson.Append("]");
        Complete(ctx, "bake_ui_from_ir", $"{{\"ir_path\":\"{EscapeJson(dto.ir_path)}\",\"theme_so\":\"{EscapeJson(dto.theme_so)}\",\"out_dir\":\"{EscapeJson(dto.out_dir)}\",\"warnings\":{warningsJson}}}");
    }

    // ── Asset pipeline composite ──────────────────────────────────────────────

    static void RunWireAssetFromCatalog(MutationContext ctx, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { Fail(ctx, modeErr); return; }
        if (!TryParseParams<WireAssetFromCatalogParams>(requestJson, out var dto, out string parseErr)) { Fail(ctx, parseErr); return; }
        var args = new Territory.Editor.Bridge.WireAssetFromCatalog.WireArgs
        {
            entity_id = dto.entity_id,
            cell_xy = dto.cell_xy,
            dry_run = dto.dry_run,
        };
        var catalogLoader = UnityEngine.Object.FindObjectOfType<Territory.Catalog.CatalogLoader>();
        Territory.Editor.Bridge.WireAssetFromCatalog.WireResult result;
        try
        {
            result = Territory.Editor.Bridge.WireAssetFromCatalog.Run(args, catalogLoader);
            if (result != null && result.ok && !args.dry_run)
                result = Territory.Editor.Bridge.Snapshot.RollbackDispatcher.WrapWithSnapshot(result, args, catalogLoader);
        }
        catch (Exception ex) { Fail(ctx, $"wire_threw:{ex.GetType().Name}:{ex.Message}"); return; }
        if (result == null) { Fail(ctx, "wire_null_result"); return; }
        Complete(ctx, "wire_asset_from_catalog", Territory.Editor.Bridge.WireAssetFromCatalog.ToBridgeJson(result));
    }

    // ── Scene replace with prefab ─────────────────────────────────────────────

    static void RunSceneReplaceWithPrefab(MutationContext ctx, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { Fail(ctx, modeErr); return; }
        if (!TryParseParams<SceneReplaceWithPrefabParams>(requestJson, out var dto, out string parseErr)) { Fail(ctx, parseErr); return; }
        if (string.IsNullOrWhiteSpace(dto.target_object_name)) { Fail(ctx, "params_invalid:target_object_name"); return; }
        if (string.IsNullOrWhiteSpace(dto.prefab_path)) { Fail(ctx, "params_invalid:prefab_path"); return; }

        if (!string.IsNullOrWhiteSpace(dto.scene_path))
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.path != dto.scene_path)
            {
                var openedScene = EditorSceneManager.OpenScene(dto.scene_path, OpenSceneMode.Single);
                if (!openedScene.IsValid()) { Fail(ctx, $"scene_not_found:{dto.scene_path}"); return; }
            }
        }

        GameObject target = null;
        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var r in roots) { if (r.name == dto.target_object_name) { target = r; break; } }
        if (target == null) target = GameObject.Find(dto.target_object_name);
        if (target == null) { Fail(ctx, $"target_not_found:{dto.target_object_name}"); return; }

        var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(dto.prefab_path);
        if (prefabAsset == null) { Fail(ctx, $"prefab_not_found:{dto.prefab_path}"); return; }

        Transform parentTransform = target.transform.parent;
        int siblingIndex = target.transform.GetSiblingIndex();
        string parentPath = parentTransform != null ? GetGoPath(parentTransform.gameObject) : "";
        RectTransform rt = target.GetComponent<RectTransform>();
        Vector2 anchorMin = rt != null ? rt.anchorMin : Vector2.zero;
        Vector2 anchorMax = rt != null ? rt.anchorMax : Vector2.one;
        Vector2 pivot = rt != null ? rt.pivot : new Vector2(0.5f, 0.5f);
        Vector2 anchoredPosition = rt != null ? rt.anchoredPosition : Vector2.zero;
        Vector2 sizeDelta = rt != null ? rt.sizeDelta : Vector2.zero;
        var scene = target.scene;

        UnityEngine.Object.DestroyImmediate(target);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, scene);
        if (instance == null) { Fail(ctx, "prefab_instantiate_failed"); return; }

        if (parentTransform != null) instance.transform.SetParent(parentTransform, false);
        instance.transform.SetSiblingIndex(siblingIndex);

        RectTransform instanceRt = instance.GetComponent<RectTransform>();
        if (instanceRt != null && rt != null)
        {
            instanceRt.anchorMin = anchorMin;
            instanceRt.anchorMax = anchorMax;
            instanceRt.pivot = pivot;
            instanceRt.anchoredPosition = anchoredPosition;
            instanceRt.sizeDelta = sizeDelta;
        }

        string prefabSlug = System.IO.Path.GetFileNameWithoutExtension(dto.prefab_path);
        var catalogRef = instance.GetComponent<CatalogPrefabRef>();
        if (catalogRef == null) catalogRef = instance.AddComponent<CatalogPrefabRef>();
        catalogRef.slug = prefabSlug;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Complete(ctx, "scene_replace_with_prefab", $"{{\"replaced_object_name\":\"{EscapeJson(dto.target_object_name)}\",\"prefab_path\":\"{EscapeJson(dto.prefab_path)}\",\"sibling_index\":{siblingIndex},\"parent_path\":\"{EscapeJson(parentPath)}\",\"instance_name\":\"{EscapeJson(instance.name)}\"}}");
    }

    // ── Param DTOs ────────────────────────────────────────────────────────────

    [Serializable] sealed class AttachComponentParams       { public string target_path; public string component_type_name; }
    [Serializable] sealed class RemoveComponentParams       { public string target_path; public string component_type_name; }
    [Serializable] sealed class AssignSerializedFieldParams { public string target_path; public string component_type_name; public string field_name; public string value_kind; public string value; public string value_object_path; }
    [Serializable] sealed class ExecuteMenuItemParams       { public string menu_path; }
    [Serializable] sealed class CreateGameObjectParams      { public string name; public string parent_path; public string position; }
    [Serializable] sealed class DeleteGameObjectParams      { public string target_path; }
    [Serializable] sealed class FindGameObjectParams        { public string target_path; }
    [Serializable] sealed class SetTransformParams          { public string target_path; public string position; public string rotation; public string scale; }
    [Serializable] sealed class SetGameObjectActiveParams   { public string target_path; public bool active; }
    [Serializable] sealed class SetGameObjectParentParams   { public string target_path; public string new_parent_path; public bool world_position_stays; }
    [Serializable] sealed class SetPanelVisibleParams       { public string slug; public bool active; }
    [Serializable] sealed class SaveSceneParams             { public string scene_path; }
    [Serializable] sealed class OpenSceneParams             { public string scene_path; public string mode; }
    [Serializable] sealed class NewSceneParams              { public string setup_mode; public string mode; }
    [Serializable] sealed class InstantiatePrefabParams     { public string prefab_path; public string parent_path; public string position; }
    [Serializable] sealed class ApplyPrefabOverridesParams  { public string target_path; public string interaction_mode; }
    [Serializable] sealed class CreateScriptableObjectParams { public string type_name; public string asset_path; }
    [Serializable] sealed class FieldWriteParam             { public string field_name; public string value_kind; public string value; public string value_object_path; }
    [Serializable] sealed class ModifyScriptableObjectParams { public string asset_path; public FieldWriteParam[] field_writes; }
    [Serializable] sealed class MoveAssetParams             { public string asset_path; public string new_path; }
    [Serializable] sealed class DeleteAssetParams           { public string asset_path; }
    [Serializable] sealed class BakeUiFromIrParams          { public string ir_path; public string out_dir; public string theme_so; }
    [Serializable] sealed class WireAssetFromCatalogParams  { public string entity_id; public string cell_xy; public bool dry_run; }
    [Serializable] sealed class SceneReplaceWithPrefabParams { public string scene_path; public string target_object_name; public string prefab_path; }
}
