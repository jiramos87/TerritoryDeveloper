using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Editor-only multi-scene <b>uGUI</b> snapshot for <b>UI design system</b> baseline inventory. Bounded JSON persisted via <b>Postgres</b> (<see cref="EditorPostgresExportRegistrar.TryPersistReport"/>).
/// Menu: <b>Territory Developer → Reports → Export UI Inventory (JSON)</b>.
/// </summary>
public static class UiInventoryReportsMenu
{
    const string MenuRoot = "Territory Developer/Reports/";
    const int SchemaVersion = 1;
    const int MaxNodesGlobal = 1200;
    const int MaxDepth = 24;
    const int MaxTextSampleChars = 80;
    const int MaxComponentsPerNode = 12;

    /// <summary>UI inventory scene allowlist. Extend for <b>CityScene</b> / <b>RegionScene</b> when shipped.</summary>
    static readonly string[] SceneAllowlist =
    {
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/CityScene.unity",
    };

    static int _nodesEmitted;

    [MenuItem(MenuRoot + "Export UI Inventory (JSON)", priority = 12)]
    public static void ExportUiInventory()
    {
        try
        {
            // Unity 2022.3: SaveCurrentModifiedScenesIfUserWantsTo (no "Cancel" suffix — that API is newer / different).
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("[UI Inventory] Export cancelled (scene save).");
                return;
            }

            _nodesEmitted = 0;
            var sceneDtos = new List<SceneInventoryDto>();

            foreach (string scenePath in SceneAllowlist)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
                {
                    Debug.LogWarning($"[UI Inventory] Skipping missing scene asset: {scenePath}");
                    continue;
                }

                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                var scene = SceneManager.GetActiveScene();
                var canvases = CollectRootCanvasesInActiveScene();
                var canvasDtos = new List<CanvasInventoryDto>();

                foreach (Canvas canvas in canvases)
                {
                    if (canvas == null)
                        continue;
                    if (_nodesEmitted >= MaxNodesGlobal)
                        break;
                    canvasDtos.Add(BuildCanvasInventory(canvas));
                }

                sceneDtos.Add(new SceneInventoryDto
                {
                    scene_asset_path = scenePath,
                    scene_name = scene.name,
                    canvases = canvasDtos.ToArray(),
                });
            }

            var report = new UiInventoryReportDto
            {
                artifact = "ui_inventory_dev",
                schema_version = SchemaVersion,
                exported_at_utc = DateTime.UtcNow.ToString("o"),
                unity_version = Application.unityVersion,
                notes = BuildNotes(sceneDtos.Count),
                scenes = sceneDtos.ToArray(),
            };

            string json = JsonUtility.ToJson(report, true);
            string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            string baseName = $"ui-inventory-{stamp}";

            bool dbOk = EditorPostgresExportRegistrar.TryPersistReport(
                EditorPostgresExportRegistrar.KindUiInventory,
                json,
                false,
                baseName,
                out _);

            if (dbOk)
                Debug.Log($"[UI Inventory] Stored in Postgres (nodes sampled: {_nodesEmitted}).");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UI Inventory] Export failed: {ex.Message}\n{ex}");
        }
    }

    static string BuildNotes(int sceneCount)
    {
        return $"UI design system baseline inventory. scenes_exported={sceneCount}, max_nodes={MaxNodesGlobal}, max_depth={MaxDepth}. " +
               "SampleScene.unity is excluded unless added to allowlist. Nodes: RectTransform with Graphic, LayoutGroup, or canvas root.";
    }

    static List<Canvas> CollectRootCanvasesInActiveScene()
    {
        var scene = SceneManager.GetActiveScene();
        var found = new List<Canvas>();
        var roots = scene.GetRootGameObjects();
        foreach (GameObject root in roots)
        {
            foreach (Canvas c in root.GetComponentsInChildren<Canvas>(true))
            {
                if (c == null)
                    continue;
                if (IsNestedUnderAnotherCanvas(c))
                    continue;
                found.Add(c);
            }
        }

        return found;
    }

    static bool IsNestedUnderAnotherCanvas(Canvas canvas)
    {
        Transform p = canvas.transform.parent;
        while (p != null)
        {
            if (p.GetComponent<Canvas>() != null)
                return true;
            p = p.parent;
        }

        return false;
    }

    static CanvasInventoryDto BuildCanvasInventory(Canvas canvas)
    {
        Transform root = canvas.transform;
        var nodes = new List<UiNodeDto>();
        CollectUiNodes(root, root, nodes, 0);

        var dto = new CanvasInventoryDto
        {
            path = SceneRelativePath(root),
            render_mode = canvas.renderMode.ToString(),
            scaler = BuildScalerDto(canvas),
            nodes = nodes.ToArray(),
        };
        return dto;
    }

    static CanvasScalerDto BuildScalerDto(Canvas canvas)
    {
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            // JsonUtility can mis-handle null nested objects; use a sentinel row.
            return new CanvasScalerDto
            {
                ui_scale_mode = "(none)",
                reference_resolution_x = 0,
                reference_resolution_y = 0,
                match_width_or_height = 0f,
            };
        }

        return new CanvasScalerDto
        {
            ui_scale_mode = scaler.uiScaleMode.ToString(),
            reference_resolution_x = Mathf.RoundToInt(scaler.referenceResolution.x),
            reference_resolution_y = Mathf.RoundToInt(scaler.referenceResolution.y),
            match_width_or_height = scaler.matchWidthOrHeight,
        };
    }

    static void CollectUiNodes(Transform canvasRoot, Transform t, List<UiNodeDto> list, int depth)
    {
        if (_nodesEmitted >= MaxNodesGlobal)
            return;
        if (depth > MaxDepth)
            return;

        if (t != canvasRoot && t.GetComponent<Canvas>() != null)
            return;

        var rt = t.GetComponent<RectTransform>();
        if (rt != null)
        {
            var graphic = t.GetComponent<Graphic>();
            var layout = t.GetComponent<LayoutGroup>();
            bool isCanvasRoot = t == canvasRoot;
            if (graphic != null || layout != null || isCanvasRoot)
            {
                list.Add(BuildNodeDto(canvasRoot, t, graphic));
                _nodesEmitted++;
            }
        }

        if (_nodesEmitted >= MaxNodesGlobal)
            return;

        for (int i = 0; i < t.childCount; i++)
        {
            Transform ch = t.GetChild(i);
            if (ch.GetComponent<Canvas>() != null && ch != canvasRoot)
                continue;
            CollectUiNodes(canvasRoot, ch, list, depth + 1);
        }
    }

    static UiNodeDto BuildNodeDto(Transform canvasRoot, Transform t, Graphic graphic)
    {
        var dto = new UiNodeDto
        {
            path = PathFromCanvasRoot(canvasRoot, t),
            active = t.gameObject.activeSelf,
            anchor_min = "",
            anchor_max = "",
            color_rgba = "",
            font_size = 0,
            font_name = "",
            text_sample = "",
        };

        var rt = t.GetComponent<RectTransform>();
        if (rt != null)
        {
            dto.anchor_min = $"{rt.anchorMin.x:F3},{rt.anchorMin.y:F3}";
            dto.anchor_max = $"{rt.anchorMax.x:F3},{rt.anchorMax.y:F3}";
        }

        dto.components = CollectComponentNames(t);

        if (graphic != null)
        {
            Color g = graphic.color;
            dto.color_rgba = $"{g.r:F3},{g.g:F3},{g.b:F3},{g.a:F3}";
        }

        var text = t.GetComponent<Text>();
        if (text != null)
        {
            dto.font_size = text.fontSize;
            dto.font_name = text.font != null ? text.font.name : "";
            dto.text_sample = Truncate(SanitizeOneLine(text.text), MaxTextSampleChars);
        }

        var tmp = t.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            if (dto.font_size <= 0)
                dto.font_size = Mathf.RoundToInt(tmp.fontSize);
            if (string.IsNullOrEmpty(dto.font_name) && tmp.font != null)
                dto.font_name = tmp.font.name;
            if (string.IsNullOrEmpty(dto.text_sample))
                dto.text_sample = Truncate(SanitizeOneLine(tmp.text ?? ""), MaxTextSampleChars);
        }

        return dto;
    }

    static string[] CollectComponentNames(Transform t)
    {
        var names = new List<string>();
        foreach (Component comp in t.GetComponents<Component>())
        {
            if (comp == null)
                continue;
            string n = comp.GetType().Name;
            if (n == "Transform" || n == "RectTransform")
                continue;
            if (names.Contains(n))
                continue;
            names.Add(n);
            if (names.Count >= MaxComponentsPerNode)
                break;
        }

        return names.ToArray();
    }

    static string PathFromCanvasRoot(Transform canvasRoot, Transform leaf)
    {
        var parts = new List<string>();
        Transform x = leaf;
        while (x != null)
        {
            parts.Add(x.name);
            if (x == canvasRoot)
                break;
            x = x.parent;
        }

        parts.Reverse();
        return string.Join("/", parts);
    }

    static string SceneRelativePath(Transform t)
    {
        var parts = new List<string>();
        Transform x = t;
        while (x != null)
        {
            parts.Add(x.name);
            x = x.parent;
        }

        parts.Reverse();
        return string.Join("/", parts);
    }

    static string SanitizeOneLine(string s)
    {
        if (string.IsNullOrEmpty(s))
            return "";
        string t = s.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
        return t.Trim();
    }

    static string Truncate(string s, int maxLen)
    {
        if (string.IsNullOrEmpty(s))
            return "";
        if (s.Length <= maxLen)
            return s;
        return s.Substring(0, maxLen) + "...";
    }
}

[Serializable]
class UiInventoryReportDto
{
    public string artifact;
    public int schema_version;
    public string exported_at_utc;
    public string unity_version;
    public string notes;
    public SceneInventoryDto[] scenes = Array.Empty<SceneInventoryDto>();
}

[Serializable]
class SceneInventoryDto
{
    public string scene_asset_path;
    public string scene_name;
    public CanvasInventoryDto[] canvases = Array.Empty<CanvasInventoryDto>();
}

[Serializable]
class CanvasInventoryDto
{
    public string path;
    public string render_mode;
    public CanvasScalerDto scaler;
    public UiNodeDto[] nodes = Array.Empty<UiNodeDto>();
}

[Serializable]
class CanvasScalerDto
{
    public string ui_scale_mode;
    public int reference_resolution_x;
    public int reference_resolution_y;
    public float match_width_or_height;
}

[Serializable]
class UiNodeDto
{
    public string path;
    public bool active;
    public string[] components = Array.Empty<string>();
    public string anchor_min;
    public string anchor_max;
    public string color_rgba;
    public int font_size;
    public string font_name;
    public string text_sample;
}
