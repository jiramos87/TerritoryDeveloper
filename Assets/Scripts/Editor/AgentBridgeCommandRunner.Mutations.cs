using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Mutation kinds for <see cref="AgentBridgeCommandRunner"/> — Edit Mode only.
/// Phase 1: component lifecycle (attach_component, remove_component, assign_serialized_field) + catch-all (execute_menu_item).
/// Phase 2: GameObject lifecycle (create_gameobject, delete_gameobject, find_gameobject, set_transform, set_gameobject_active, set_gameobject_parent) + scene lifecycle (save_scene, open_scene, new_scene).
/// Phase 3: prefab lifecycle (instantiate_prefab, apply_prefab_overrides) + asset lifecycle (create_scriptable_object, modify_scriptable_object, refresh_asset_database, move_asset, delete_asset).
/// Safety: Each kind pre-checks target GO / asset existence; mutation kinds call EditorSceneManager.MarkSceneDirty; asset-mutation kinds call AssetDatabase.SaveAssets + Refresh.
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
        switch (kind)
        {
            // ── Component lifecycle ──────────────────────────────────────
            case "attach_component":
                RunAttachComponent(repoRoot, commandId, requestJson);
                return true;
            case "remove_component":
                RunRemoveComponent(repoRoot, commandId, requestJson);
                return true;
            case "assign_serialized_field":
                RunAssignSerializedField(repoRoot, commandId, requestJson);
                return true;

            // ── GameObject lifecycle ─────────────────────────────────────
            case "create_gameobject":
                RunCreateGameObject(repoRoot, commandId, requestJson);
                return true;
            case "delete_gameobject":
                RunDeleteGameObject(repoRoot, commandId, requestJson);
                return true;
            case "find_gameobject":
                RunFindGameObject(repoRoot, commandId, requestJson);
                return true;
            case "set_transform":
                RunSetTransform(repoRoot, commandId, requestJson);
                return true;
            case "set_gameobject_active":
                RunSetGameObjectActive(repoRoot, commandId, requestJson);
                return true;
            case "set_gameobject_parent":
                RunSetGameObjectParent(repoRoot, commandId, requestJson);
                return true;

            // ── Scene lifecycle ──────────────────────────────────────────
            case "save_scene":
                RunSaveScene(repoRoot, commandId, requestJson);
                return true;
            case "open_scene":
                RunOpenScene(repoRoot, commandId, requestJson);
                return true;
            case "new_scene":
                RunNewScene(repoRoot, commandId, requestJson);
                return true;

            // ── Prefab lifecycle ─────────────────────────────────────────
            case "instantiate_prefab":
                RunInstantiatePrefab(repoRoot, commandId, requestJson);
                return true;
            case "apply_prefab_overrides":
                RunApplyPrefabOverrides(repoRoot, commandId, requestJson);
                return true;

            // ── Asset lifecycle ──────────────────────────────────────────
            case "create_scriptable_object":
                RunCreateScriptableObject(repoRoot, commandId, requestJson);
                return true;
            case "modify_scriptable_object":
                RunModifyScriptableObject(repoRoot, commandId, requestJson);
                return true;
            case "refresh_asset_database":
                RunRefreshAssetDatabase(repoRoot, commandId);
                return true;
            case "move_asset":
                RunMoveAsset(repoRoot, commandId, requestJson);
                return true;
            case "delete_asset":
                RunDeleteAsset(repoRoot, commandId, requestJson);
                return true;

            // ── Catch-all ────────────────────────────────────────────────
            case "execute_menu_item":
                RunExecuteMenuItem(repoRoot, commandId, requestJson);
                return true;

            // ── Game UI bake (Stage 2) ───────────────────────────────────
            case "bake_ui_from_ir":
                RunBakeUiFromIr(repoRoot, commandId, requestJson);
                return true;

            // ── Game UI runtime — Play Mode allowed ──────────────────────
            // Step 16.10 — narrow `set_panel_visible(slug, active)` lets the bridge
            // toggle a ThemedPanel without simulating a keyboard Esc press; used by
            // closed-loop QA (claude_design_conformance) to surface pause + info-panel.
            case "set_panel_visible":
                RunSetPanelVisible(repoRoot, commandId, requestJson);
                return true;

            default:
                return false;
        }
    }

    // ── Shared helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Resolve a scene-root-relative path (e.g. "Managers/EconomyManager") in the active scene.
    /// Returns the first matching GO, or null on miss.
    /// </summary>
    static bool TryResolveGameObject(string path, out GameObject go, out string error)
    {
        go = null;
        error = null;

        if (string.IsNullOrWhiteSpace(path))
        {
            error = "params_invalid:target_path (empty)";
            return false;
        }

        go = GameObject.Find(path);
        if (go == null)
        {
            error = $"target_not_found:{path}";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Resolve a component type name across all loaded assemblies (case-insensitive short name).
    /// Returns null if not found; sets error to type_ambiguous:Name;candidates=A,B when multiple matches found.
    /// </summary>
    static bool TryResolveComponentType(string typeName, out Type type, out string error)
    {
        type = null;
        error = null;

        if (string.IsNullOrWhiteSpace(typeName))
        {
            error = "params_invalid:component_type_name (empty)";
            return false;
        }

        var candidates = new List<Type>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var t in assembly.GetTypes())
            {
                if (string.Equals(t.Name, typeName, StringComparison.OrdinalIgnoreCase) &&
                    typeof(Component).IsAssignableFrom(t))
                {
                    candidates.Add(t);
                }
            }
        }

        if (candidates.Count == 0)
        {
            error = $"type_not_found:{typeName}";
            return false;
        }

        if (candidates.Count > 1)
        {
            var names = new System.Text.StringBuilder();
            for (int i = 0; i < candidates.Count; i++)
            {
                if (i > 0) names.Append(',');
                names.Append(candidates[i].FullName);
            }
            error = $"type_ambiguous:{typeName};candidates={names}";
            return false;
        }

        type = candidates[0];
        return true;
    }

    /// <summary>
    /// Validate that a mutation is being attempted only in Edit Mode.
    /// Returns false + sets error when in Play Mode.
    /// </summary>
    static bool AssertEditMode(out string error)
    {
        if (EditorApplication.isPlaying)
        {
            error = "edit_mode_required: mutation kinds are Edit Mode only";
            return false;
        }
        error = null;
        return true;
    }

    static AgentBridgeResponseFileDto BuildMutationOkResponse(string commandId, string storage, string resultJson)
    {
        var resp = AgentBridgeResponseFileDto.CreateOk(commandId, storage);
        resp.mutation_result = resultJson;
        return resp;
    }

    // ── Phase 1: Component lifecycle ─────────────────────────────────────────

    static void RunAttachComponent(string repoRoot, string commandId, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { TryFinalizeFailed(repoRoot, commandId, modeErr); return; }
        if (!TryParseMutationParams<AttachComponentParamsDto>(requestJson, out var dto, out string parseErr)) { TryFinalizeFailed(repoRoot, commandId, parseErr); return; }

        if (string.IsNullOrWhiteSpace(dto.target_path)) { TryFinalizeFailed(repoRoot, commandId, "params_invalid:target_path"); return; }
        if (string.IsNullOrWhiteSpace(dto.component_type_name)) { TryFinalizeFailed(repoRoot, commandId, "params_invalid:component_type_name"); return; }

        if (!TryResolveGameObject(dto.target_path, out GameObject go, out string goErr)) { TryFinalizeFailed(repoRoot, commandId, goErr); return; }
        if (!TryResolveComponentType(dto.component_type_name, out Type compType, out string typeErr)) { TryFinalizeFailed(repoRoot, commandId, typeErr); return; }

        Component added = go.AddComponent(compType);
        EditorSceneManager.MarkSceneDirty(go.scene);

        var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "attach_component");
        resp.mutation_result = $"{{\"attached\":\"{compType.FullName}\",\"instance_id\":{added.GetInstanceID()},\"game_object\":\"{go.name}\"}}";
        CompleteOrFail(repoRoot, commandId, UnityEngine.JsonUtility.ToJson(resp, true));
    }

    static void RunRemoveComponent(string repoRoot, string commandId, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { TryFinalizeFailed(repoRoot, commandId, modeErr); return; }
        if (!TryParseMutationParams<RemoveComponentParamsDto>(requestJson, out var dto, out string parseErr)) { TryFinalizeFailed(repoRoot, commandId, parseErr); return; }

        if (string.IsNullOrWhiteSpace(dto.target_path)) { TryFinalizeFailed(repoRoot, commandId, "params_invalid:target_path"); return; }
        if (string.IsNullOrWhiteSpace(dto.component_type_name)) { TryFinalizeFailed(repoRoot, commandId, "params_invalid:component_type_name"); return; }

        if (!TryResolveGameObject(dto.target_path, out GameObject go, out string goErr)) { TryFinalizeFailed(repoRoot, commandId, goErr); return; }
        if (!TryResolveComponentType(dto.component_type_name, out Type compType, out string typeErr)) { TryFinalizeFailed(repoRoot, commandId, typeErr); return; }

        Component comp = go.GetComponent(compType);
        if (comp == null)
        {
            TryFinalizeFailed(repoRoot, commandId, $"component_not_found:{dto.component_type_name} on {dto.target_path}");
            return;
        }

        UnityEngine.Object.DestroyImmediate(comp);
        EditorSceneManager.MarkSceneDirty(go.scene);

        var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "remove_component");
        resp.mutation_result = $"{{\"removed\":\"{compType.FullName}\",\"game_object\":\"{go.name}\"}}";
        CompleteOrFail(repoRoot, commandId, UnityEngine.JsonUtility.ToJson(resp, true));
    }

    static void RunAssignSerializedField(string repoRoot, string commandId, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { TryFinalizeFailed(repoRoot, commandId, modeErr); return; }
        if (!TryParseMutationParams<AssignSerializedFieldParamsDto>(requestJson, out var dto, out string parseErr)) { TryFinalizeFailed(repoRoot, commandId, parseErr); return; }

        if (string.IsNullOrWhiteSpace(dto.target_path)) { TryFinalizeFailed(repoRoot, commandId, "params_invalid:target_path"); return; }
        if (string.IsNullOrWhiteSpace(dto.component_type_name)) { TryFinalizeFailed(repoRoot, commandId, "params_invalid:component_type_name"); return; }
        if (string.IsNullOrWhiteSpace(dto.field_name)) { TryFinalizeFailed(repoRoot, commandId, "params_invalid:field_name"); return; }
        if (string.IsNullOrWhiteSpace(dto.value_kind)) { TryFinalizeFailed(repoRoot, commandId, "params_invalid:value_kind"); return; }

        if (!TryResolveGameObject(dto.target_path, out GameObject go, out string goErr)) { TryFinalizeFailed(repoRoot, commandId, goErr); return; }
        if (!TryResolveComponentType(dto.component_type_name, out Type compType, out string typeErr)) { TryFinalizeFailed(repoRoot, commandId, typeErr); return; }

        Component comp = go.GetComponent(compType);
        if (comp == null)
        {
            TryFinalizeFailed(repoRoot, commandId, $"component_not_found:{dto.component_type_name} on {dto.target_path}");
            return;
        }

        var so = new SerializedObject(comp);
        SerializedProperty prop = so.FindProperty(dto.field_name);
        if (prop == null)
        {
            TryFinalizeFailed(repoRoot, commandId, $"field_not_found:{dto.field_name} on {dto.component_type_name}");
            return;
        }

        string setErr = null;
        bool setOk = TrySetSerializedProperty(prop, dto.value_kind, dto.value, dto.value_object_path, out setErr);
        if (!setOk) { TryFinalizeFailed(repoRoot, commandId, setErr); return; }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(go.scene);

        var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "assign_serialized_field");
        resp.mutation_result = $"{{\"field\":\"{dto.field_name}\",\"value_kind\":\"{dto.value_kind}\",\"component\":\"{compType.FullName}\",\"game_object\":\"{go.name}\"}}";
        CompleteOrFail(repoRoot, commandId, UnityEngine.JsonUtility.ToJson(resp, true));
    }

    /// <summary>
    /// Set a SerializedProperty value from a tagged-union (value_kind ∈ object_ref | component_ref | asset_ref | int | float | bool | string | vector3).
    /// For object_ref / component_ref / asset_ref, value_object_path is the scene path or asset path of the target object.
    /// For component_ref, value carries the short component type name to resolve on the target GO.
    /// For primitives, value is the string representation.
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
                if (string.IsNullOrWhiteSpace(valueObjectPath))
                {
                    error = "params_invalid:value_object_path (required for object_ref)";
                    return false;
                }
                GameObject refGo = GameObject.Find(valueObjectPath);
                if (refGo == null)
                {
                    error = $"target_not_found:{valueObjectPath}";
                    return false;
                }
                prop.objectReferenceValue = refGo;
                return true;
            }
            case "component_ref":
            {
                if (string.IsNullOrWhiteSpace(valueObjectPath))
                {
                    error = "params_invalid:value_object_path (required for component_ref — scene path of GO carrying component)";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(value))
                {
                    error = "params_invalid:value (required for component_ref — short component type name)";
                    return false;
                }
                GameObject refGo = GameObject.Find(valueObjectPath);
                if (refGo == null)
                {
                    error = $"target_not_found:{valueObjectPath}";
                    return false;
                }
                if (!TryResolveComponentType(value, out Type compType, out string typeErr))
                {
                    error = typeErr;
                    return false;
                }
                Component comp = refGo.GetComponent(compType);
                if (comp == null)
                {
                    error = $"component_not_found:{value} on {valueObjectPath}";
                    return false;
                }
                prop.objectReferenceValue = comp;
                return true;
            }
            case "asset_ref":
            {
                if (string.IsNullOrWhiteSpace(valueObjectPath))
                {
                    error = "params_invalid:value_object_path (required for asset_ref)";
                    return false;
                }
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(valueObjectPath);
                if (asset == null)
                {
                    error = $"asset_not_found:{valueObjectPath}";
                    return false;
                }
                prop.objectReferenceValue = asset;
                return true;
            }
            case "int":
            {
                if (!int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int iv))
                {
                    error = $"params_invalid:value (cannot parse int: {value})";
                    return false;
                }
                prop.intValue = iv;
                return true;
            }
            case "float":
            {
                if (!float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float fv))
                {
                    error = $"params_invalid:value (cannot parse float: {value})";
                    return false;
                }
                prop.floatValue = fv;
                return true;
            }
            case "bool":
            {
                if (!bool.TryParse(value, out bool bv))
                {
                    error = $"params_invalid:value (cannot parse bool: {value})";
                    return false;
                }
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
                // Expected format: "x,y,z"
                if (string.IsNullOrWhiteSpace(value))
                {
                    error = "params_invalid:value (vector3 expects \"x,y,z\")";
                    return false;
                }
                string[] parts = value.Split(',');
                if (parts.Length != 3 ||
                    !float.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float vx) ||
                    !float.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float vy) ||
                    !float.TryParse(parts[2].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float vz))
                {
                    error = $"params_invalid:value (vector3 parse failed: {value})";
                    return false;
                }
                prop.vector3Value = new Vector3(vx, vy, vz);
                return true;
            }
            default:
                error = $"params_invalid:value_kind (unsupported: {valueKind}). Supported: object_ref, component_ref, asset_ref, int, float, bool, string, vector3";
                return false;
        }
    }

    // ── Phase 1: Catch-all ───────────────────────────────────────────────────

    static void RunExecuteMenuItem(string repoRoot, string commandId, string requestJson)
    {
        if (!TryParseMutationParams<ExecuteMenuItemParamsDto>(requestJson, out var dto, out string parseErr)) { TryFinalizeFailed(repoRoot, commandId, parseErr); return; }

        if (string.IsNullOrWhiteSpace(dto.menu_path)) { TryFinalizeFailed(repoRoot, commandId, "params_invalid:menu_path"); return; }

        bool executed = EditorApplication.ExecuteMenuItem(dto.menu_path);
        if (!executed)
        {
            TryFinalizeFailed(repoRoot, commandId, $"menu_not_found:{dto.menu_path}");
            return;
        }

        var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "execute_menu_item");
        resp.mutation_result = $"{{\"menu_path\":\"{EscapeJsonString(dto.menu_path)}\"}}";
        CompleteOrFail(repoRoot, commandId, UnityEngine.JsonUtility.ToJson(resp, true));
    }

    // ── Phase 2: GameObject lifecycle ────────────────────────────────────────

    static void RunCreateGameObject(string repoRoot, string commandId, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { TryFinalizeFailed(repoRoot, commandId, modeErr); return; }
        if (!TryParseMutationParams<CreateGameObjectParamsDto>(requestJson, out var dto, out string parseErr)) { TryFinalizeFailed(repoRoot, commandId, parseErr); return; }

        if (string.IsNullOrWhiteSpace(dto.name)) { TryFinalizeFailed(repoRoot, commandId, "params_invalid:name"); return; }

        var go = new GameObject(dto.name);

        if (!string.IsNullOrWhiteSpace(dto.parent_path))
        {
            if (!TryResolveGameObject(dto.parent_path, out GameObject parent, out string parentErr))
            {
                UnityEngine.Object.DestroyImmediate(go);
                TryFinalizeFailed(repoRoot, commandId, parentErr);
                return;
            }
            go.transform.SetParent(parent.transform, false);
        }

        if (!string.IsNullOrWhiteSpace(dto.position))
        {
            if (TryParseVector3(dto.position, out Vector3 pos))
                go.transform.localPosition = pos;
        }

        EditorSceneManager.MarkSceneDirty(go.scene);

        var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "create_gameobject");
        resp.mutation_result = $"{{\"name\":\"{EscapeJsonString(go.name)}\",\"path\":\"{EscapeJsonString(GetGameObjectPath(go))}\",\"instance_id\":{go.GetInstanceID()}}}";
        CompleteOrFail(repoRoot, commandId, UnityEngine.JsonUtility.ToJson(resp, true));
    }

    static void RunDeleteGameObject(string repoRoot, string commandId, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { TryFinalizeFailed(repoRoot, commandId, modeErr); return; }
        if (!TryParseMutationParams<DeleteGameObjectParamsDto>(requestJson, out var dto, out string parseErr)) { TryFinalizeFailed(repoRoot, commandId, parseErr); return; }

        if (!TryResolveGameObject(dto.target_path, out GameObject go, out string goErr)) { TryFinalizeFailed(repoRoot, commandId, goErr); return; }

        Scene scene = go.scene;
        string name = go.name;
        UnityEngine.Object.DestroyImmediate(go);
        EditorSceneManager.MarkSceneDirty(scene);

        var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "delete_gameobject");
        resp.mutation_result = $"{{\"deleted\":\"{EscapeJsonString(dto.target_path)}\",\"name\":\"{EscapeJsonString(name)}\"}}";
        CompleteOrFail(repoRoot, commandId, UnityEngine.JsonUtility.ToJson(resp, true));
    }

    static void RunFindGameObject(string repoRoot, string commandId, string requestJson)
    {
        if (!TryParseMutationParams<FindGameObjectParamsDto>(requestJson, out var dto, out string parseErr)) { TryFinalizeFailed(repoRoot, commandId, parseErr); return; }

        if (string.IsNullOrWhiteSpace(dto.target_path)) { TryFinalizeFailed(repoRoot, commandId, "params_invalid:target_path"); return; }

        GameObject go = GameObject.Find(dto.target_path);
        bool exists = go != null;

        string componentsJson = "[]";
        int childrenCount = 0;
        if (exists)
        {
            childrenCount = go.transform.childCount;
            var compNames = new List<string>();
            foreach (Component c in go.GetComponents<Component>())
            {
                if (c != null)
                    compNames.Add(c.GetType().Name);
            }
            var sb = new System.Text.StringBuilder("[");
            for (int i = 0; i < compNames.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('"');
                sb.Append(EscapeJsonString(compNames[i]));
                sb.Append('"');
            }
            sb.Append(']');
            componentsJson = sb.ToString();
        }

        var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "find_gameobject");
        resp.mutation_result = $"{{\"path\":\"{EscapeJsonString(dto.target_path)}\",\"exists\":{(exists ? "true" : "false")},\"components\":{componentsJson},\"children_count\":{childrenCount}}}";
        CompleteOrFail(repoRoot, commandId, UnityEngine.JsonUtility.ToJson(resp, true));
    }

    static void RunSetTransform(string repoRoot, string commandId, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { TryFinalizeFailed(repoRoot, commandId, modeErr); return; }
        if (!TryParseMutationParams<SetTransformParamsDto>(requestJson, out var dto, out string parseErr)) { TryFinalizeFailed(repoRoot, commandId, parseErr); return; }

        if (!TryResolveGameObject(dto.target_path, out GameObject go, out string goErr)) { TryFinalizeFailed(repoRoot, commandId, goErr); return; }

        var so = new SerializedObject(go.transform);

        if (!string.IsNullOrWhiteSpace(dto.position) && TryParseVector3(dto.position, out Vector3 pos))
        {
            SerializedProperty posProp = so.FindProperty("m_LocalPosition");
            if (posProp != null) posProp.vector3Value = pos;
        }
        if (!string.IsNullOrWhiteSpace(dto.rotation) && TryParseVector3(dto.rotation, out Vector3 rot))
        {
            SerializedProperty rotProp = so.FindProperty("m_LocalRotation");
            if (rotProp != null) rotProp.quaternionValue = Quaternion.Euler(rot);
        }
        if (!string.IsNullOrWhiteSpace(dto.scale) && TryParseVector3(dto.scale, out Vector3 scl))
        {
            SerializedProperty sclProp = so.FindProperty("m_LocalScale");
            if (sclProp != null) sclProp.vector3Value = scl;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(go.scene);

        var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "set_transform");
        resp.mutation_result = $"{{\"game_object\":\"{EscapeJsonString(go.name)}\"}}";
        CompleteOrFail(repoRoot, commandId, UnityEngine.JsonUtility.ToJson(resp, true));
    }

    static void RunSetGameObjectActive(string repoRoot, string commandId, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { TryFinalizeFailed(repoRoot, commandId, modeErr); return; }
        if (!TryParseMutationParams<SetGameObjectActiveParamsDto>(requestJson, out var dto, out string parseErr)) { TryFinalizeFailed(repoRoot, commandId, parseErr); return; }

        if (!TryResolveGameObject(dto.target_path, out GameObject go, out string goErr)) { TryFinalizeFailed(repoRoot, commandId, goErr); return; }

        go.SetActive(dto.active);
        EditorSceneManager.MarkSceneDirty(go.scene);

        var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "set_gameobject_active");
        resp.mutation_result = $"{{\"game_object\":\"{EscapeJsonString(go.name)}\",\"active\":{(dto.active ? "true" : "false")}}}";
        CompleteOrFail(repoRoot, commandId, UnityEngine.JsonUtility.ToJson(resp, true));
    }

    // Step 16.10 — runtime ThemedPanel toggle by IR slug (Play Mode allowed).
    static void RunSetPanelVisible(string repoRoot, string commandId, string requestJson)
    {
        if (!TryParseMutationParams<SetPanelVisibleParamsDto>(requestJson, out var dto, out string parseErr)) { TryFinalizeFailed(repoRoot, commandId, parseErr); return; }
        if (string.IsNullOrWhiteSpace(dto.slug)) { TryFinalizeFailed(repoRoot, commandId, "params_invalid:slug"); return; }

        var panels = UnityEngine.Object.FindObjectsOfType<Territory.UI.Themed.ThemedPanel>(includeInactive: true);
        Territory.UI.Themed.ThemedPanel match = null;
        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] != null && panels[i].gameObject.name == dto.slug)
            {
                match = panels[i];
                break;
            }
        }

        if (match == null)
        {
            TryFinalizeFailed(repoRoot, commandId, $"panel_not_found:slug={dto.slug}");
            return;
        }

        match.gameObject.SetActive(dto.active);
        if (!EditorApplication.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(match.gameObject.scene);
        }

        var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "set_panel_visible");
        resp.mutation_result = $"{{\"slug\":\"{EscapeJsonString(dto.slug)}\",\"active\":{(dto.active ? "true" : "false")},\"play_mode\":{(EditorApplication.isPlaying ? "true" : "false")}}}";
        CompleteOrFail(repoRoot, commandId, UnityEngine.JsonUtility.ToJson(resp, true));
    }

    static void RunSetGameObjectParent(string repoRoot, string commandId, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { TryFinalizeFailed(repoRoot, commandId, modeErr); return; }
        if (!TryParseMutationParams<SetGameObjectParentParamsDto>(requestJson, out var dto, out string parseErr)) { TryFinalizeFailed(repoRoot, commandId, parseErr); return; }

        if (!TryResolveGameObject(dto.target_path, out GameObject go, out string goErr)) { TryFinalizeFailed(repoRoot, commandId, goErr); return; }

        Transform newParent = null;
        if (!string.IsNullOrWhiteSpace(dto.new_parent_path))
        {
            if (!TryResolveGameObject(dto.new_parent_path, out GameObject parentGo, out string parentErr))
            {
                TryFinalizeFailed(repoRoot, commandId, parentErr);
                return;
            }
            newParent = parentGo.transform;
        }

        go.transform.SetParent(newParent, dto.world_position_stays);
        EditorSceneManager.MarkSceneDirty(go.scene);

        var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "set_gameobject_parent");
        resp.mutation_result = $"{{\"game_object\":\"{EscapeJsonString(go.name)}\",\"new_parent\":\"{EscapeJsonString(dto.new_parent_path ?? "(root)")}\"}}";
        CompleteOrFail(repoRoot, commandId, UnityEngine.JsonUtility.ToJson(resp, true));
    }

    // ── Phase 2: Scene lifecycle ─────────────────────────────────────────────

    static void RunSaveScene(string repoRoot, string commandId, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { TryFinalizeFailed(repoRoot, commandId, modeErr); return; }
        if (!TryParseMutationParams<SaveSceneParamsDto>(requestJson, out var dto, out string parseErr)) { TryFinalizeFailed(repoRoot, commandId, parseErr); return; }

        Scene scene;
        if (!string.IsNullOrWhiteSpace(dto.scene_path))
        {
            scene = SceneManager.GetSceneByPath(dto.scene_path);
            if (!scene.IsValid())
            {
                TryFinalizeFailed(repoRoot, commandId, $"scene_not_found:{dto.scene_path}");
                return;
            }
        }
        else
        {
            scene = SceneManager.GetActiveScene();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene, scene.path);

        if (!saved)
        {
            TryFinalizeFailed(repoRoot, commandId, $"save_failed:{scene.path}");
            return;
        }

        var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "save_scene");
        resp.mutation_result = $"{{\"scene_path\":\"{EscapeJsonString(scene.path)}\"}}";
        CompleteOrFail(repoRoot, commandId, UnityEngine.JsonUtility.ToJson(resp, true));
    }

    static void RunOpenScene(string repoRoot, string commandId, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { TryFinalizeFailed(repoRoot, commandId, modeErr); return; }
        if (!TryParseMutationParams<OpenSceneParamsDto>(requestJson, out var dto, out string parseErr)) { TryFinalizeFailed(repoRoot, commandId, parseErr); return; }

        if (string.IsNullOrWhiteSpace(dto.scene_path)) { TryFinalizeFailed(repoRoot, commandId, "params_invalid:scene_path"); return; }

        OpenSceneMode mode = string.Equals(dto.mode, "additive", StringComparison.OrdinalIgnoreCase)
            ? OpenSceneMode.Additive
            : OpenSceneMode.Single;

        Scene opened = EditorSceneManager.OpenScene(dto.scene_path, mode);
        if (!opened.IsValid())
        {
            TryFinalizeFailed(repoRoot, commandId, $"open_scene_failed:{dto.scene_path}");
            return;
        }

        var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "open_scene");
        resp.mutation_result = $"{{\"scene_path\":\"{EscapeJsonString(opened.path)}\",\"mode\":\"{EscapeJsonString(dto.mode ?? "single")}\"}}";
        CompleteOrFail(repoRoot, commandId, UnityEngine.JsonUtility.ToJson(resp, true));
    }

    static void RunNewScene(string repoRoot, string commandId, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { TryFinalizeFailed(repoRoot, commandId, modeErr); return; }
        if (!TryParseMutationParams<NewSceneParamsDto>(requestJson, out var dto, out string parseErr)) { TryFinalizeFailed(repoRoot, commandId, parseErr); return; }

        NewSceneSetup setup = string.Equals(dto.setup_mode, "empty_scene", StringComparison.OrdinalIgnoreCase)
            ? NewSceneSetup.EmptyScene
            : NewSceneSetup.DefaultGameObjects;

        NewSceneMode mode = string.Equals(dto.mode, "additive", StringComparison.OrdinalIgnoreCase)
            ? NewSceneMode.Additive
            : NewSceneMode.Single;

        Scene created = EditorSceneManager.NewScene(setup, mode);

        var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "new_scene");
        resp.mutation_result = $"{{\"scene_name\":\"{EscapeJsonString(created.name)}\",\"setup_mode\":\"{EscapeJsonString(dto.setup_mode ?? "default_game_objects")}\",\"mode\":\"{EscapeJsonString(dto.mode ?? "single")}\"}}";
        CompleteOrFail(repoRoot, commandId, UnityEngine.JsonUtility.ToJson(resp, true));
    }

    // ── Phase 3: Prefab lifecycle ────────────────────────────────────────────

    static void RunInstantiatePrefab(string repoRoot, string commandId, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { TryFinalizeFailed(repoRoot, commandId, modeErr); return; }
        if (!TryParseMutationParams<InstantiatePrefabParamsDto>(requestJson, out var dto, out string parseErr)) { TryFinalizeFailed(repoRoot, commandId, parseErr); return; }

        if (string.IsNullOrWhiteSpace(dto.prefab_path)) { TryFinalizeFailed(repoRoot, commandId, "params_invalid:prefab_path"); return; }

        var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(dto.prefab_path);
        if (prefabAsset == null)
        {
            TryFinalizeFailed(repoRoot, commandId, $"asset_not_found:{dto.prefab_path}");
            return;
        }

        Transform parent = null;
        if (!string.IsNullOrWhiteSpace(dto.parent_path))
        {
            if (!TryResolveGameObject(dto.parent_path, out GameObject parentGo, out string parentErr))
            {
                TryFinalizeFailed(repoRoot, commandId, parentErr);
                return;
            }
            parent = parentGo.transform;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, parent);

        if (!string.IsNullOrWhiteSpace(dto.position) && TryParseVector3(dto.position, out Vector3 pos))
            instance.transform.localPosition = pos;

        EditorSceneManager.MarkSceneDirty(instance.scene);

        var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "instantiate_prefab");
        resp.mutation_result = $"{{\"name\":\"{EscapeJsonString(instance.name)}\",\"path\":\"{EscapeJsonString(GetGameObjectPath(instance))}\",\"instance_id\":{instance.GetInstanceID()}}}";
        CompleteOrFail(repoRoot, commandId, UnityEngine.JsonUtility.ToJson(resp, true));
    }

    static void RunApplyPrefabOverrides(string repoRoot, string commandId, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { TryFinalizeFailed(repoRoot, commandId, modeErr); return; }
        if (!TryParseMutationParams<ApplyPrefabOverridesParamsDto>(requestJson, out var dto, out string parseErr)) { TryFinalizeFailed(repoRoot, commandId, parseErr); return; }

        if (!TryResolveGameObject(dto.target_path, out GameObject go, out string goErr)) { TryFinalizeFailed(repoRoot, commandId, goErr); return; }

        PrefabUtility.ApplyPrefabInstance(go, InteractionMode.AutomatedAction);
        EditorSceneManager.MarkSceneDirty(go.scene);

        var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "apply_prefab_overrides");
        resp.mutation_result = $"{{\"game_object\":\"{EscapeJsonString(go.name)}\"}}";
        CompleteOrFail(repoRoot, commandId, UnityEngine.JsonUtility.ToJson(resp, true));
    }

    // ── Phase 3: Asset lifecycle ─────────────────────────────────────────────

    static void RunCreateScriptableObject(string repoRoot, string commandId, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { TryFinalizeFailed(repoRoot, commandId, modeErr); return; }
        if (!TryParseMutationParams<CreateScriptableObjectParamsDto>(requestJson, out var dto, out string parseErr)) { TryFinalizeFailed(repoRoot, commandId, parseErr); return; }

        if (string.IsNullOrWhiteSpace(dto.type_name)) { TryFinalizeFailed(repoRoot, commandId, "params_invalid:type_name"); return; }
        if (string.IsNullOrWhiteSpace(dto.asset_path)) { TryFinalizeFailed(repoRoot, commandId, "params_invalid:asset_path"); return; }

        // Resolve type across all assemblies — ScriptableObject-compatible types
        Type soType = null;
        var candidates = new List<Type>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var t in assembly.GetTypes())
            {
                if (string.Equals(t.Name, dto.type_name, StringComparison.OrdinalIgnoreCase) &&
                    typeof(ScriptableObject).IsAssignableFrom(t))
                {
                    candidates.Add(t);
                }
            }
        }

        if (candidates.Count == 0)
        {
            TryFinalizeFailed(repoRoot, commandId, $"type_not_found:{dto.type_name}");
            return;
        }
        if (candidates.Count > 1)
        {
            var names = new System.Text.StringBuilder();
            for (int i = 0; i < candidates.Count; i++)
            {
                if (i > 0) names.Append(',');
                names.Append(candidates[i].FullName);
            }
            TryFinalizeFailed(repoRoot, commandId, $"type_ambiguous:{dto.type_name};candidates={names}");
            return;
        }
        soType = candidates[0];

        ScriptableObject so = ScriptableObject.CreateInstance(soType);
        AssetDatabase.CreateAsset(so, dto.asset_path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "create_scriptable_object");
        resp.mutation_result = $"{{\"type\":\"{EscapeJsonString(soType.FullName)}\",\"asset_path\":\"{EscapeJsonString(dto.asset_path)}\"}}";
        CompleteOrFail(repoRoot, commandId, UnityEngine.JsonUtility.ToJson(resp, true));
    }

    static void RunModifyScriptableObject(string repoRoot, string commandId, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { TryFinalizeFailed(repoRoot, commandId, modeErr); return; }
        if (!TryParseMutationParams<ModifyScriptableObjectParamsDto>(requestJson, out var dto, out string parseErr)) { TryFinalizeFailed(repoRoot, commandId, parseErr); return; }

        if (string.IsNullOrWhiteSpace(dto.asset_path)) { TryFinalizeFailed(repoRoot, commandId, "params_invalid:asset_path"); return; }
        if (dto.field_writes == null || dto.field_writes.Length == 0) { TryFinalizeFailed(repoRoot, commandId, "params_invalid:field_writes (empty)"); return; }

        ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(dto.asset_path);
        if (asset == null)
        {
            TryFinalizeFailed(repoRoot, commandId, $"asset_not_found:{dto.asset_path}");
            return;
        }

        var so = new SerializedObject(asset);
        var applied = new List<string>();

        foreach (var fw in dto.field_writes)
        {
            if (fw == null || string.IsNullOrWhiteSpace(fw.field_name)) continue;
            SerializedProperty prop = so.FindProperty(fw.field_name);
            if (prop == null)
            {
                TryFinalizeFailed(repoRoot, commandId, $"field_not_found:{fw.field_name} on {dto.asset_path}");
                return;
            }
            if (!TrySetSerializedProperty(prop, fw.value_kind, fw.value, fw.value_object_path, out string setErr))
            {
                TryFinalizeFailed(repoRoot, commandId, setErr);
                return;
            }
            applied.Add(fw.field_name);
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "modify_scriptable_object");
        resp.mutation_result = $"{{\"asset_path\":\"{EscapeJsonString(dto.asset_path)}\",\"fields_written\":{applied.Count}}}";
        CompleteOrFail(repoRoot, commandId, UnityEngine.JsonUtility.ToJson(resp, true));
    }

    static void RunRefreshAssetDatabase(string repoRoot, string commandId)
    {
        AssetDatabase.Refresh();
        var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "refresh_asset_database");
        resp.mutation_result = "{\"refreshed\":true}";
        CompleteOrFail(repoRoot, commandId, UnityEngine.JsonUtility.ToJson(resp, true));
    }

    static void RunMoveAsset(string repoRoot, string commandId, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { TryFinalizeFailed(repoRoot, commandId, modeErr); return; }
        if (!TryParseMutationParams<MoveAssetParamsDto>(requestJson, out var dto, out string parseErr)) { TryFinalizeFailed(repoRoot, commandId, parseErr); return; }

        if (string.IsNullOrWhiteSpace(dto.asset_path)) { TryFinalizeFailed(repoRoot, commandId, "params_invalid:asset_path"); return; }
        if (string.IsNullOrWhiteSpace(dto.new_path)) { TryFinalizeFailed(repoRoot, commandId, "params_invalid:new_path"); return; }

        string moveError = AssetDatabase.MoveAsset(dto.asset_path, dto.new_path);
        if (!string.IsNullOrEmpty(moveError))
        {
            TryFinalizeFailed(repoRoot, commandId, $"move_failed:{moveError}");
            return;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "move_asset");
        resp.mutation_result = $"{{\"from\":\"{EscapeJsonString(dto.asset_path)}\",\"to\":\"{EscapeJsonString(dto.new_path)}\"}}";
        CompleteOrFail(repoRoot, commandId, UnityEngine.JsonUtility.ToJson(resp, true));
    }

    static void RunDeleteAsset(string repoRoot, string commandId, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { TryFinalizeFailed(repoRoot, commandId, modeErr); return; }
        if (!TryParseMutationParams<DeleteAssetParamsDto>(requestJson, out var dto, out string parseErr)) { TryFinalizeFailed(repoRoot, commandId, parseErr); return; }

        if (string.IsNullOrWhiteSpace(dto.asset_path)) { TryFinalizeFailed(repoRoot, commandId, "params_invalid:asset_path"); return; }

        bool deleted = AssetDatabase.DeleteAsset(dto.asset_path);
        if (!deleted)
        {
            TryFinalizeFailed(repoRoot, commandId, $"delete_failed:{dto.asset_path}");
            return;
        }

        AssetDatabase.Refresh();

        var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "delete_asset");
        resp.mutation_result = $"{{\"deleted\":\"{EscapeJsonString(dto.asset_path)}\"}}";
        CompleteOrFail(repoRoot, commandId, UnityEngine.JsonUtility.ToJson(resp, true));
    }

    // ── Game UI bake (Stage 2) ───────────────────────────────────────────────

    static void RunBakeUiFromIr(string repoRoot, string commandId, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { TryFinalizeFailed(repoRoot, commandId, modeErr); return; }
        if (!TryParseMutationParams<BakeUiFromIrParamsDto>(requestJson, out var dto, out string parseErr)) { TryFinalizeFailed(repoRoot, commandId, parseErr); return; }

        if (string.IsNullOrWhiteSpace(dto.ir_path)) { TryFinalizeFailed(repoRoot, commandId, "missing_arg:ir_path"); return; }
        if (string.IsNullOrWhiteSpace(dto.out_dir)) { TryFinalizeFailed(repoRoot, commandId, "missing_arg:out_dir"); return; }
        if (string.IsNullOrWhiteSpace(dto.theme_so)) { TryFinalizeFailed(repoRoot, commandId, "missing_arg:theme_so"); return; }

        var bakeArgs = new Territory.Editor.Bridge.UiBakeHandler.BakeArgs
        {
            ir_path = dto.ir_path,
            out_dir = dto.out_dir,
            theme_so = dto.theme_so,
        };

        Territory.Editor.Bridge.UiBakeHandler.BakeResult result;
        try
        {
            result = Territory.Editor.Bridge.UiBakeHandler.Bake(bakeArgs);
        }
        catch (Exception ex)
        {
            TryFinalizeFailed(repoRoot, commandId, $"bake_threw:{ex.GetType().Name}:{ex.Message}");
            return;
        }

        if (result == null)
        {
            TryFinalizeFailed(repoRoot, commandId, "bake_null_result");
            return;
        }

        if (result.error != null)
        {
            string detail = $"{result.error.error}:{result.error.details}@{result.error.path}";
            TryFinalizeFailed(repoRoot, commandId, detail);
            return;
        }

        var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "bake_ui_from_ir");
        resp.mutation_result = $"{{\"ir_path\":\"{EscapeJsonString(dto.ir_path)}\",\"theme_so\":\"{EscapeJsonString(dto.theme_so)}\",\"out_dir\":\"{EscapeJsonString(dto.out_dir)}\"}}";
        CompleteOrFail(repoRoot, commandId, UnityEngine.JsonUtility.ToJson(resp, true));
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    static bool TryParseMutationParams<TDto>(string requestJson, out TDto dto, out string error)
        where TDto : class, new()
    {
        dto = null;
        error = null;

        if (string.IsNullOrWhiteSpace(requestJson))
        {
            error = "params_invalid: empty request_json";
            return false;
        }

        // Normalize "params" → "bridge_params" so JsonUtility picks it up via AgentBridgeRequestEnvelopeDto
        string normalized = requestJson.Replace("\"params\":", "\"bridge_params\":", StringComparison.Ordinal);
        AgentBridgeRequestEnvelopeDto env = null;
        try
        {
            env = UnityEngine.JsonUtility.FromJson<AgentBridgeRequestEnvelopeDto>(normalized);
        }
        catch (Exception ex)
        {
            error = $"params_invalid: {ex.Message}";
            return false;
        }

        if (env == null)
        {
            error = "params_invalid: null envelope";
            return false;
        }

        // Re-extract params JSON from the raw requestJson and parse directly into TDto
        // since AgentBridgeParamsPayloadDto is a general bag. For typed DTOs we parse separately.
        string paramsJson = ExtractParamsJsonBlock(requestJson);
        if (string.IsNullOrWhiteSpace(paramsJson))
        {
            // No params block — return empty DTO (for kinds with no required params like refresh_asset_database)
            dto = new TDto();
            return true;
        }

        try
        {
            dto = UnityEngine.JsonUtility.FromJson<TDto>(paramsJson);
        }
        catch (Exception ex)
        {
            error = $"params_invalid: {ex.Message}";
            return false;
        }

        if (dto == null)
            dto = new TDto();

        return true;
    }

    /// <summary>
    /// Extract the raw JSON object value of the "params" key from a request envelope JSON string.
    /// Handles nested braces via brace counting; returns null if "params" key is not present.
    /// </summary>
    static string ExtractParamsJsonBlock(string requestJson)
    {
        if (string.IsNullOrEmpty(requestJson)) return null;

        // Find "params": or "bridge_params": key
        int keyIdx = requestJson.IndexOf("\"params\":", StringComparison.Ordinal);
        if (keyIdx < 0)
            keyIdx = requestJson.IndexOf("\"bridge_params\":", StringComparison.Ordinal);
        if (keyIdx < 0)
            return null;

        int braceStart = requestJson.IndexOf('{', keyIdx);
        if (braceStart < 0)
            return null;

        int depth = 0;
        for (int i = braceStart; i < requestJson.Length; i++)
        {
            char c = requestJson[i];
            if (c == '{') depth++;
            else if (c == '}')
            {
                depth--;
                if (depth == 0)
                    return requestJson.Substring(braceStart, i - braceStart + 1);
            }
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

// ── Mutation param DTOs ──────────────────────────────────────────────────────

// Component lifecycle

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
/// value_kind ∈ object_ref | component_ref | asset_ref | int | float | bool | string | vector3.
/// For object_ref: value_object_path = scene-root-relative GO path.
/// For component_ref: value_object_path = scene-root-relative GO path; value = short component type name resolved on that GO.
/// For asset_ref: value_object_path = asset path (e.g. "Assets/Prefabs/Foo.prefab").
/// For primitives: value = string representation.
/// For vector3: value = "x,y,z".
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

// Catch-all

[Serializable]
class ExecuteMenuItemParamsDto
{
    public string menu_path;
}

// GameObject lifecycle

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

// Step 16.10 — Game UI runtime toggle.
[Serializable]
class SetPanelVisibleParamsDto
{
    public string slug;
    public bool active;
}

// Scene lifecycle

[Serializable]
class SaveSceneParamsDto
{
    /// <summary>Asset path to scene, e.g. "Assets/Scenes/MainScene.unity". Omit for active scene.</summary>
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

// Prefab lifecycle

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
    /// <summary>Reserved for future use; currently always AutomatedAction.</summary>
    public string interaction_mode;
}

// Asset lifecycle

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

// Game UI bake (Stage 2)

[Serializable]
class BakeUiFromIrParamsDto
{
    /// <summary>Absolute or repo-relative path to IR JSON produced by Stage 1 transcribe.</summary>
    public string ir_path;
    /// <summary>Output dir for generated placeholder prefabs (T2.4 fills body).</summary>
    public string out_dir;
    /// <summary>Asset path to UiTheme ScriptableObject (e.g. "Assets/UI/Theme/DefaultUiTheme.asset").</summary>
    public string theme_so;
}
