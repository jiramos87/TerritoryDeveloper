using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Stage 12 Step 14.1 — `prefab_inspect` bridge kind (read-only).
// Loads a prefab asset, walks its GameObject hierarchy, dumps every component
// + every serialized field + RectTransform layout per node into JSON.
// Closes the closed-loop gap where the agent baked a prefab but had no
// structured way to read back what was written without screenshots.

public static partial class AgentBridgeCommandRunner
{
    static void RunPrefabInspect(string repoRoot, string commandId, string requestJson)
    {
        if (!TryParsePrefabInspectParams(requestJson, out var dto, out string parseErr))
        {
            TryFinalizeFailed(repoRoot, commandId, parseErr);
            return;
        }

        if (string.IsNullOrWhiteSpace(dto.prefab_path))
        {
            TryFinalizeFailed(repoRoot, commandId, "params_invalid:prefab_path");
            return;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(dto.prefab_path);
        if (prefab == null)
        {
            TryFinalizeFailed(repoRoot, commandId, $"asset_not_found:{dto.prefab_path}");
            return;
        }

        AgentBridgePrefabInspectNodeDto rootNode;
        int nodeCount;
        int componentCount;
        int missingScriptCount;
        try
        {
            rootNode = BuildPrefabInspectNode(prefab.transform, prefab.transform, out nodeCount, out componentCount, out missingScriptCount);
        }
        catch (Exception ex)
        {
            TryFinalizeFailed(repoRoot, commandId, $"inspect_threw:{ex.GetType().Name}:{ex.Message}");
            return;
        }

        var inspectDto = new AgentBridgePrefabInspectDto
        {
            prefab_path = dto.prefab_path,
            root_name = prefab.name,
            node_count = nodeCount,
            component_count = componentCount,
            missing_script_count = missingScriptCount,
            root = rootNode,
        };

        var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "prefab_inspect");
        resp.prefab_inspect_result = inspectDto;
        CompleteOrFail(repoRoot, commandId, JsonUtility.ToJson(resp, true));
    }

    static AgentBridgePrefabInspectNodeDto BuildPrefabInspectNode(
        Transform node,
        Transform root,
        out int nodeCount,
        out int componentCount,
        out int missingScriptCount)
    {
        nodeCount = 1;
        componentCount = 0;
        missingScriptCount = 0;

        var dto = new AgentBridgePrefabInspectNodeDto
        {
            name = node.gameObject.name,
            relative_path = ComputeRelativePath(node, root),
            active_self = node.gameObject.activeSelf,
            tag = node.gameObject.tag,
            layer = LayerMask.LayerToName(node.gameObject.layer),
        };

        // RectTransform-specific layout (UI prefabs only).
        if (node is RectTransform rt)
        {
            dto.rect_transform = new AgentBridgePrefabInspectRectDto
            {
                anchor_min = FormatVector2(rt.anchorMin),
                anchor_max = FormatVector2(rt.anchorMax),
                pivot = FormatVector2(rt.pivot),
                anchored_position = FormatVector2(rt.anchoredPosition),
                size_delta = FormatVector2(rt.sizeDelta),
                offset_min = FormatVector2(rt.offsetMin),
                offset_max = FormatVector2(rt.offsetMax),
                local_scale = FormatVector3(rt.localScale),
            };
        }

        // Components + serialized fields.
        var componentsList = new List<AgentBridgePrefabInspectComponentDto>();
        var rawComponents = node.gameObject.GetComponents<Component>();
        foreach (var comp in rawComponents)
        {
            componentCount++;
            if (comp == null)
            {
                missingScriptCount++;
                componentsList.Add(new AgentBridgePrefabInspectComponentDto
                {
                    type_name = "(missing script)",
                    is_missing_script = true,
                    fields = Array.Empty<AgentBridgePrefabInspectFieldDto>(),
                });
                continue;
            }

            componentsList.Add(new AgentBridgePrefabInspectComponentDto
            {
                type_name = comp.GetType().Name,
                full_type_name = comp.GetType().FullName,
                is_missing_script = false,
                fields = SnapshotSerializedFields(comp),
            });
        }
        dto.components = componentsList.ToArray();

        // Recurse children.
        var childrenList = new List<AgentBridgePrefabInspectNodeDto>();
        for (int i = 0; i < node.childCount; i++)
        {
            var childDto = BuildPrefabInspectNode(
                node.GetChild(i),
                root,
                out int childNodes,
                out int childComponents,
                out int childMissing);
            nodeCount += childNodes;
            componentCount += childComponents;
            missingScriptCount += childMissing;
            childrenList.Add(childDto);
        }
        dto.children = childrenList.ToArray();

        return dto;
    }

    static AgentBridgePrefabInspectFieldDto[] SnapshotSerializedFields(Component comp)
    {
        var so = new SerializedObject(comp);
        var iterator = so.GetIterator();
        var fields = new List<AgentBridgePrefabInspectFieldDto>();

        // Skip the Base script reference; iterate visible properties only.
        if (!iterator.NextVisible(true))
            return fields.ToArray();
        do
        {
            // m_Script is the implicit script binding; skip — adds noise + same value across many comps.
            if (iterator.name == "m_Script") continue;
            fields.Add(new AgentBridgePrefabInspectFieldDto
            {
                field_name = iterator.name,
                propertyType = iterator.propertyType.ToString(),
                value_str = FormatSerializedPropertyValue(iterator),
            });
        }
        while (iterator.NextVisible(false));
        return fields.ToArray();
    }

    static string FormatSerializedPropertyValue(SerializedProperty p)
    {
        try
        {
            switch (p.propertyType)
            {
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.ArraySize:
                case SerializedPropertyType.Character:
                    return p.intValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                case SerializedPropertyType.Float:
                    return p.floatValue.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                case SerializedPropertyType.Boolean:
                    return p.boolValue ? "true" : "false";
                case SerializedPropertyType.String:
                    return p.stringValue ?? string.Empty;
                case SerializedPropertyType.Color:
                    var c = p.colorValue;
                    return string.Format(System.Globalization.CultureInfo.InvariantCulture, "rgba({0:F3},{1:F3},{2:F3},{3:F3})", c.r, c.g, c.b, c.a);
                case SerializedPropertyType.Enum:
                    int idx = p.enumValueIndex;
                    if (idx >= 0 && p.enumDisplayNames != null && idx < p.enumDisplayNames.Length)
                        return p.enumDisplayNames[idx];
                    return p.intValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                case SerializedPropertyType.Vector2:
                    return FormatVector2(p.vector2Value);
                case SerializedPropertyType.Vector3:
                    return FormatVector3(p.vector3Value);
                case SerializedPropertyType.Vector4:
                    var v4 = p.vector4Value;
                    return string.Format(System.Globalization.CultureInfo.InvariantCulture, "({0},{1},{2},{3})", v4.x, v4.y, v4.z, v4.w);
                case SerializedPropertyType.Rect:
                    var r = p.rectValue;
                    return string.Format(System.Globalization.CultureInfo.InvariantCulture, "rect({0},{1},{2},{3})", r.x, r.y, r.width, r.height);
                case SerializedPropertyType.ObjectReference:
                    var o = p.objectReferenceValue;
                    if (o == null) return "(null)";
                    string assetPath = AssetDatabase.GetAssetPath(o);
                    if (!string.IsNullOrEmpty(assetPath))
                        return $"asset:{assetPath}#{o.GetType().Name}:{o.name}";
                    return $"scene:{o.GetType().Name}:{o.name}";
                case SerializedPropertyType.ManagedReference:
                    return p.managedReferenceFullTypename ?? "(null managed)";
                case SerializedPropertyType.Generic:
                    if (p.isArray) return $"array[{p.arraySize}]";
                    return "(generic)";
                default:
                    return $"({p.propertyType})";
            }
        }
        catch (Exception ex)
        {
            return $"(read_failed:{ex.GetType().Name})";
        }
    }

    static string FormatVector2(Vector2 v) =>
        string.Format(System.Globalization.CultureInfo.InvariantCulture, "({0},{1})", v.x, v.y);

    static string FormatVector3(Vector3 v) =>
        string.Format(System.Globalization.CultureInfo.InvariantCulture, "({0},{1},{2})", v.x, v.y, v.z);

    static string ComputeRelativePath(Transform node, Transform root)
    {
        if (node == root) return string.Empty;
        var stack = new List<string>();
        var cursor = node;
        while (cursor != null && cursor != root)
        {
            stack.Add(cursor.gameObject.name);
            cursor = cursor.parent;
        }
        stack.Reverse();
        return string.Join("/", stack);
    }

    static bool TryParsePrefabInspectParams(string requestJson, out PrefabInspectParamsDto dto, out string error)
    {
        dto = null;
        error = null;
        if (string.IsNullOrWhiteSpace(requestJson))
        {
            error = "params_invalid: empty request_json";
            return false;
        }

        // Reuse mutation-style ExtractParamsJsonBlock to parse the params object directly.
        string paramsJson = ExtractParamsJsonBlockInspect(requestJson);
        if (string.IsNullOrWhiteSpace(paramsJson))
        {
            dto = new PrefabInspectParamsDto();
            return true;
        }

        try
        {
            dto = JsonUtility.FromJson<PrefabInspectParamsDto>(paramsJson);
        }
        catch (Exception ex)
        {
            error = $"params_invalid: {ex.Message}";
            return false;
        }

        if (dto == null) dto = new PrefabInspectParamsDto();
        return true;
    }

    // Local copy of ExtractParamsJsonBlock — avoids cross-partial coupling.
    static string ExtractParamsJsonBlockInspect(string requestJson)
    {
        if (string.IsNullOrEmpty(requestJson)) return null;
        int keyIdx = requestJson.IndexOf("\"params\":", StringComparison.Ordinal);
        if (keyIdx < 0)
            keyIdx = requestJson.IndexOf("\"bridge_params\":", StringComparison.Ordinal);
        if (keyIdx < 0) return null;
        int braceStart = requestJson.IndexOf('{', keyIdx);
        if (braceStart < 0) return null;
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
}

[Serializable]
class PrefabInspectParamsDto
{
    public string prefab_path;
}

[Serializable]
public class AgentBridgePrefabInspectDto
{
    public string prefab_path;
    public string root_name;
    public int node_count;
    public int component_count;
    public int missing_script_count;
    public AgentBridgePrefabInspectNodeDto root;
}

[Serializable]
public class AgentBridgePrefabInspectNodeDto
{
    public string name;
    public string relative_path;
    public bool active_self;
    public string tag;
    public string layer;
    public AgentBridgePrefabInspectRectDto rect_transform;
    public AgentBridgePrefabInspectComponentDto[] components;
    public AgentBridgePrefabInspectNodeDto[] children;
}

[Serializable]
public class AgentBridgePrefabInspectRectDto
{
    public string anchor_min;
    public string anchor_max;
    public string pivot;
    public string anchored_position;
    public string size_delta;
    public string offset_min;
    public string offset_max;
    public string local_scale;
}

[Serializable]
public class AgentBridgePrefabInspectComponentDto
{
    public string type_name;
    public string full_type_name;
    public bool is_missing_script;
    public AgentBridgePrefabInspectFieldDto[] fields;
}

[Serializable]
public class AgentBridgePrefabInspectFieldDto
{
    public string field_name;
    public string propertyType;
    public string value_str;
}
