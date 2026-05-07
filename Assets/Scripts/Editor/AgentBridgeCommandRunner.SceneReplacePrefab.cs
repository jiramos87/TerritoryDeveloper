using System;
using System.IO;
using Territory.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Stage 9.14 / TECH-22667 — scene_replace_with_prefab bridge kind.
/// Opens scene_path (or uses active scene), finds target_object_name root GameObject,
/// captures parent + RectTransform anchors + sibling index, destroys target,
/// PrefabUtility.InstantiatePrefab at same parent/sibling/anchors, saves scene.
/// Edit Mode only.
/// </summary>
public static partial class AgentBridgeCommandRunner
{
    static void RunSceneReplaceWithPrefab(string repoRoot, string commandId, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { TryFinalizeFailed(repoRoot, commandId, modeErr); return; }
        if (!TryParseMutationParams<SceneReplaceWithPrefabParamsDto>(requestJson, out var dto, out string parseErr)) { TryFinalizeFailed(repoRoot, commandId, parseErr); return; }

        if (string.IsNullOrWhiteSpace(dto.target_object_name)) { TryFinalizeFailed(repoRoot, commandId, "params_invalid:target_object_name"); return; }
        if (string.IsNullOrWhiteSpace(dto.prefab_path)) { TryFinalizeFailed(repoRoot, commandId, "params_invalid:prefab_path"); return; }

        // Open scene if specified and not already active.
        if (!string.IsNullOrWhiteSpace(dto.scene_path))
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.path != dto.scene_path)
            {
                var openedScene = EditorSceneManager.OpenScene(dto.scene_path, OpenSceneMode.Single);
                if (!openedScene.IsValid())
                {
                    TryFinalizeFailed(repoRoot, commandId, $"scene_not_found:{dto.scene_path}");
                    return;
                }
            }
        }

        // Find target by name in root of active scene.
        GameObject target = null;
        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var r in roots)
        {
            if (r.name == dto.target_object_name)
            {
                target = r;
                break;
            }
        }
        // Fallback: search recursively via GameObject.Find.
        if (target == null) target = GameObject.Find(dto.target_object_name);
        if (target == null)
        {
            TryFinalizeFailed(repoRoot, commandId, $"target_not_found:{dto.target_object_name}");
            return;
        }

        // Load prefab asset.
        var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(dto.prefab_path);
        if (prefabAsset == null)
        {
            TryFinalizeFailed(repoRoot, commandId, $"prefab_not_found:{dto.prefab_path}");
            return;
        }

        // Capture scene position metadata before destroy.
        Transform parentTransform = target.transform.parent;
        int siblingIndex = target.transform.GetSiblingIndex();
        string parentPath = parentTransform != null ? GetGameObjectPath(parentTransform.gameObject) : "";

        // Capture RectTransform if present.
        RectTransform rt = target.GetComponent<RectTransform>();
        Vector2 anchorMin = rt != null ? rt.anchorMin : Vector2.zero;
        Vector2 anchorMax = rt != null ? rt.anchorMax : Vector2.one;
        Vector2 pivot = rt != null ? rt.pivot : new Vector2(0.5f, 0.5f);
        Vector2 anchoredPosition = rt != null ? rt.anchoredPosition : Vector2.zero;
        Vector2 sizeDelta = rt != null ? rt.sizeDelta : Vector2.zero;
        var scene = target.scene;

        // Destroy the old GameObject.
        UnityEngine.Object.DestroyImmediate(target);

        // Instantiate prefab as linked prefab instance.
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, scene);
        if (instance == null)
        {
            TryFinalizeFailed(repoRoot, commandId, "prefab_instantiate_failed");
            return;
        }

        // Restore parent.
        if (parentTransform != null)
            instance.transform.SetParent(parentTransform, false);

        // Restore sibling index.
        instance.transform.SetSiblingIndex(siblingIndex);

        // Restore RectTransform anchors if instance also has one.
        RectTransform instanceRt = instance.GetComponent<RectTransform>();
        if (instanceRt != null && rt != null)
        {
            instanceRt.anchorMin = anchorMin;
            instanceRt.anchorMax = anchorMax;
            instanceRt.pivot = pivot;
            instanceRt.anchoredPosition = anchoredPosition;
            instanceRt.sizeDelta = sizeDelta;
        }

        // Attach CatalogPrefabRef with slug derived from prefab filename stem.
        string prefabSlug = Path.GetFileNameWithoutExtension(dto.prefab_path);
        var catalogRef = instance.GetComponent<CatalogPrefabRef>();
        if (catalogRef == null) catalogRef = instance.AddComponent<CatalogPrefabRef>();
        catalogRef.slug = prefabSlug;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "scene_replace_with_prefab");
        resp.mutation_result = $"{{\"replaced_object_name\":\"{dto.target_object_name}\",\"prefab_path\":\"{dto.prefab_path}\",\"sibling_index\":{siblingIndex},\"parent_path\":\"{parentPath}\",\"instance_name\":\"{instance.name}\"}}";
        CompleteOrFail(repoRoot, commandId, UnityEngine.JsonUtility.ToJson(resp, true));
    }
}

// ── DTO ──────────────────────────────────────────────────────────────────────

[Serializable]
class SceneReplaceWithPrefabParamsDto
{
    /// <summary>Asset path to the scene to open (e.g. 'Assets/Scenes/CityScene.unity'). Optional — uses active scene if omitted.</summary>
    public string scene_path;
    /// <summary>Name of the root GameObject to replace.</summary>
    public string target_object_name;
    /// <summary>Asset path to the prefab to instantiate in its place.</summary>
    public string prefab_path;
}
