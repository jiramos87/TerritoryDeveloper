using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Stage 12 Step 14.2 — `ui_tree_walk` bridge kind (read-only).
// Walks every active Canvas in the active scene (or a single named root) and
// emits per-Rect layout + screen-space bounds + components + serialized
// fields. Complements `prefab_inspect` (Step 14.1): inspect = asset on disk;
// ui_tree_walk = instantiated runtime UI where layout has solved.

public static partial class AgentBridgeCommandRunner
{
    static void RunUiTreeWalk(string repoRoot, string commandId, string requestJson)
    {
        if (!TryParseUiTreeWalkParams(requestJson, out var dto, out string parseErr))
        {
            TryFinalizeFailed(repoRoot, commandId, parseErr);
            return;
        }

        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            TryFinalizeFailed(repoRoot, commandId, "scene_not_loaded");
            return;
        }

        // Force a layout solve so RectTransform.GetWorldCorners returns
        // post-layout values (matters in Edit Mode where the Canvas batcher
        // may not have ticked).
        Canvas.ForceUpdateCanvases();

        var canvases = CollectCanvases(scene, dto);
        if (canvases.Count == 0)
        {
            TryFinalizeFailed(repoRoot, commandId, "no_canvas_match");
            return;
        }

        bool includeFields = dto.include_serialized_fields;
        var canvasDtos = new List<AgentBridgeUiTreeCanvasDto>();
        int totalNodes = 0;
        int totalComponents = 0;
        int totalMissing = 0;

        try
        {
            foreach (var canvas in canvases)
            {
                var rt = canvas.transform as RectTransform;
                if (rt == null) continue;

                var rootNode = BuildUiTreeNode(rt, rt, canvas, includeFields,
                    out int nodeCount, out int componentCount, out int missingCount);

                totalNodes += nodeCount;
                totalComponents += componentCount;
                totalMissing += missingCount;

                var scaler = canvas.GetComponent<CanvasScaler>();
                canvasDtos.Add(new AgentBridgeUiTreeCanvasDto
                {
                    name = canvas.gameObject.name,
                    scene_path = ComputeScenePath(canvas.transform),
                    render_mode = canvas.renderMode.ToString(),
                    sort_order = canvas.sortingOrder,
                    is_active_in_hierarchy = canvas.gameObject.activeInHierarchy,
                    reference_resolution = scaler != null
                        ? FormatVector2(scaler.referenceResolution)
                        : null,
                    reference_pixels_per_unit = scaler != null
                        ? scaler.referencePixelsPerUnit
                        : 0f,
                    root = rootNode,
                });
            }
        }
        catch (Exception ex)
        {
            TryFinalizeFailed(repoRoot, commandId, $"walk_threw:{ex.GetType().Name}:{ex.Message}");
            return;
        }

        var walkDto = new AgentBridgeUiTreeWalkDto
        {
            scene_name = scene.name,
            scene_path = scene.path,
            canvas_count = canvasDtos.Count,
            node_count = totalNodes,
            component_count = totalComponents,
            missing_script_count = totalMissing,
            canvases = canvasDtos.ToArray(),
        };

        var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "ui_tree_walk");
        resp.ui_tree_walk_result = walkDto;
        CompleteOrFail(repoRoot, commandId, JsonUtility.ToJson(resp, true));
    }

    static List<Canvas> CollectCanvases(Scene scene, UiTreeWalkParamsDto dto)
    {
        var hits = new List<Canvas>();
        bool nameFilter = !string.IsNullOrWhiteSpace(dto.root_path);

        foreach (var root in scene.GetRootGameObjects())
        {
            var rootCanvases = root.GetComponentsInChildren<Canvas>(true);
            foreach (var c in rootCanvases)
            {
                if (c == null) continue;
                if (nameFilter)
                {
                    string scenePath = ComputeScenePath(c.transform);
                    if (!string.Equals(scenePath, dto.root_path, StringComparison.Ordinal) &&
                        !string.Equals(c.gameObject.name, dto.root_path, StringComparison.Ordinal))
                        continue;
                }
                if (dto.active_only && !c.gameObject.activeInHierarchy) continue;
                hits.Add(c);
            }
        }
        return hits;
    }

    static AgentBridgeUiTreeNodeDto BuildUiTreeNode(
        RectTransform node,
        RectTransform root,
        Canvas owningCanvas,
        bool includeSerializedFields,
        out int nodeCount,
        out int componentCount,
        out int missingScriptCount)
    {
        nodeCount = 1;
        componentCount = 0;
        missingScriptCount = 0;

        var dto = new AgentBridgeUiTreeNodeDto
        {
            name = node.gameObject.name,
            relative_path = ComputeRelativePath(node, root),
            active_self = node.gameObject.activeSelf,
            active_in_hierarchy = node.gameObject.activeInHierarchy,
            tag = node.gameObject.tag,
            layer = LayerMask.LayerToName(node.gameObject.layer),
            rect_transform = new AgentBridgePrefabInspectRectDto
            {
                anchor_min = FormatVector2(node.anchorMin),
                anchor_max = FormatVector2(node.anchorMax),
                pivot = FormatVector2(node.pivot),
                anchored_position = FormatVector2(node.anchoredPosition),
                size_delta = FormatVector2(node.sizeDelta),
                offset_min = FormatVector2(node.offsetMin),
                offset_max = FormatVector2(node.offsetMax),
                local_scale = FormatVector3(node.localScale),
            },
            screen_rect = ComputeScreenRect(node, owningCanvas),
        };

        // Components — reuse prefab_inspect snapshot helper.
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
                fields = includeSerializedFields
                    ? SnapshotSerializedFields(comp)
                    : Array.Empty<AgentBridgePrefabInspectFieldDto>(),
            });
        }
        dto.components = componentsList.ToArray();

        // Recurse RectTransform children only — non-UI children are
        // out of scope for a Canvas walk.
        var childrenList = new List<AgentBridgeUiTreeNodeDto>();
        for (int i = 0; i < node.childCount; i++)
        {
            var childRt = node.GetChild(i) as RectTransform;
            if (childRt == null) continue;
            var childDto = BuildUiTreeNode(
                childRt,
                root,
                owningCanvas,
                includeSerializedFields,
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

    static AgentBridgeUiTreeScreenRectDto ComputeScreenRect(RectTransform rt, Canvas canvas)
    {
        if (rt == null || canvas == null) return null;
        try
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            // corners[0] = bottom-left (world), corners[2] = top-right (world).
            Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;
            Vector2 bl = cam != null
                ? (Vector2)RectTransformUtility.WorldToScreenPoint(cam, corners[0])
                : new Vector2(corners[0].x, corners[0].y);
            Vector2 tr = cam != null
                ? (Vector2)RectTransformUtility.WorldToScreenPoint(cam, corners[2])
                : new Vector2(corners[2].x, corners[2].y);
            return new AgentBridgeUiTreeScreenRectDto
            {
                x = bl.x,
                y = bl.y,
                width = tr.x - bl.x,
                height = tr.y - bl.y,
            };
        }
        catch
        {
            return null;
        }
    }

    static string ComputeScenePath(Transform t)
    {
        if (t == null) return string.Empty;
        var stack = new List<string>();
        var cursor = t;
        while (cursor != null)
        {
            stack.Add(cursor.gameObject.name);
            cursor = cursor.parent;
        }
        stack.Reverse();
        return string.Join("/", stack);
    }

    static bool TryParseUiTreeWalkParams(string requestJson, out UiTreeWalkParamsDto dto, out string error)
    {
        dto = null;
        error = null;
        if (string.IsNullOrWhiteSpace(requestJson))
        {
            error = "params_invalid: empty request_json";
            return false;
        }

        string paramsJson = ExtractParamsJsonBlockInspect(requestJson);
        if (string.IsNullOrWhiteSpace(paramsJson))
        {
            dto = new UiTreeWalkParamsDto();
            return true;
        }

        try
        {
            dto = JsonUtility.FromJson<UiTreeWalkParamsDto>(paramsJson);
        }
        catch (Exception ex)
        {
            error = $"params_invalid: {ex.Message}";
            return false;
        }

        if (dto == null) dto = new UiTreeWalkParamsDto();
        return true;
    }
}

[Serializable]
class UiTreeWalkParamsDto
{
    public string root_path;                // optional: filter to a named Canvas (scene path or short name)
    public bool active_only = true;         // skip inactive Canvases by default
    public bool include_serialized_fields;  // default false — keeps payload small
}

[Serializable]
public class AgentBridgeUiTreeWalkDto
{
    public string scene_name;
    public string scene_path;
    public int canvas_count;
    public int node_count;
    public int component_count;
    public int missing_script_count;
    public AgentBridgeUiTreeCanvasDto[] canvases;
}

[Serializable]
public class AgentBridgeUiTreeCanvasDto
{
    public string name;
    public string scene_path;
    public string render_mode;
    public string reference_resolution;
    public float reference_pixels_per_unit;
    public int sort_order;
    public bool is_active_in_hierarchy;
    public AgentBridgeUiTreeNodeDto root;
}

[Serializable]
public class AgentBridgeUiTreeNodeDto
{
    public string name;
    public string relative_path;
    public bool active_self;
    public bool active_in_hierarchy;
    public string tag;
    public string layer;
    public AgentBridgePrefabInspectRectDto rect_transform;
    public AgentBridgeUiTreeScreenRectDto screen_rect;
    public AgentBridgePrefabInspectComponentDto[] components;
    public AgentBridgeUiTreeNodeDto[] children;
}

[Serializable]
public class AgentBridgeUiTreeScreenRectDto
{
    public float x;
    public float y;
    public float width;
    public float height;
}
