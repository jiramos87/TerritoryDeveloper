using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// wire_ui_documents bridge command — adds UIDocument GameObjects + Host MonoBehaviours
/// for every panel slug listed in <see cref="CityScenePanels"/>. Idempotent: skips slugs
/// whose *-uidoc GameObject already exists in the scene.
/// </summary>
public static partial class AgentBridgeCommandRunner
{
    private const string PanelSettingsAssetPath = "Assets/Settings/UI/PanelSettings.asset";
    private const string UxmlGeneratedRoot = "Assets/UI/Generated/";

    // (slug, host-class simple name) pairs for CityScene HUD panels.
    private static readonly (string slug, string hostClass)[] CityScenePanels =
    {
        ("hud-bar",              "HudBarHost"),
        ("toolbar",              "ToolbarHost"),
        ("time-controls",        "TimeControlsHost"),
        ("overlay-toggle-strip", "OverlayToggleStripHost"),
        ("mini-map",             "MiniMapHost"),
        ("zone-overlay",         "ZoneOverlayHost"),
        ("city-stats",           "CityStatsHost"),
        ("building-info",        "BuildingInfoHost"),
        ("tooltip",              "TooltipHost"),
        ("alerts-panel",         "AlertsPanelHost"),
        ("glossary-panel",       "GlossaryPanelHost"),
        ("notifications-toast",  "NotificationsToastHost"),
        ("growth-budget-panel",  "GrowthBudgetPanelHost"),
        ("budget-panel",         "BudgetPanelHost"),
        ("info-panel",           "InfoPanelHost"),
        ("stats-panel",          "StatsPanelHost"),
        ("pause-menu",           "PauseMenuHost"),
        ("onboarding-overlay",   "OnboardingOverlayHost"),
    };

    private static readonly (string slug, string hostClass)[] MainMenuPanels =
    {
        ("main-menu",     "MainMenuHost"),
        ("new-game-form", "NewGameFormHost"),
        ("load-view",     "LoadViewHost"),
        ("splash",        "SplashHost"),
        ("onboarding",    "OnboardingHost"),
        ("settings-view", "SettingsViewHost"),
        ("pause-menu",    "PauseMenuHost"),
        ("save-load-view","SaveLoadViewHost"),
    };

    [System.Serializable]
    private class WireUiDocumentsParamsDto
    {
        public string scene; // "CityScene" | "MainMenu" | "both"
    }

    [System.Serializable]
    private class WireUiDocumentsResultDto
    {
        public string status;
        public string[] wired;
        public string[] skipped;
        public string[] failed;
    }

    internal static void RunWireUiDocuments(string repoRoot, string commandId, string requestJson)
    {
        if (!AssertEditMode(out string modeErr)) { TryFinalizeFailed(repoRoot, commandId, modeErr); return; }

        string targetScene = "CityScene";
        if (!string.IsNullOrWhiteSpace(requestJson))
        {
            try
            {
                var p = UnityEngine.JsonUtility.FromJson<WireUiDocumentsParamsDto>(requestJson);
                if (!string.IsNullOrWhiteSpace(p?.scene)) targetScene = p.scene;
            }
            catch { }
        }

        var wired   = new List<string>();
        var skipped = new List<string>();
        var failed  = new List<string>();

        AssetDatabase.Refresh();
        PanelSettings panelSettings = null;
        {
            var psGuids = AssetDatabase.FindAssets("t:PanelSettings");
            foreach (var g in psGuids)
            {
                panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(AssetDatabase.GUIDToAssetPath(g));
                if (panelSettings != null) break;
            }
        }
        if (panelSettings == null)
        {
            TryFinalizeFailed(repoRoot, commandId, $"panel_settings_not_found:no PanelSettings asset found in project");
            return;
        }

        if (targetScene == "CityScene" || targetScene == "both")
            WireScene("Assets/Scenes/CityScene.unity", CityScenePanels, panelSettings, wired, skipped, failed);
        if (targetScene == "MainMenu" || targetScene == "both")
            WireScene("Assets/Scenes/MainMenu.unity", MainMenuPanels, panelSettings, wired, skipped, failed);

        var result = new WireUiDocumentsResultDto
        {
            status  = failed.Count == 0 ? "ok" : "partial",
            wired   = wired.ToArray(),
            skipped = skipped.ToArray(),
            failed  = failed.ToArray(),
        };

        var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "wire_ui_documents");
        resp.result_json = UnityEngine.JsonUtility.ToJson(result, true);
        CompleteOrFail(repoRoot, commandId, UnityEngine.JsonUtility.ToJson(resp, true));
    }

    private static void WireScene(
        string scenePath,
        (string slug, string hostClass)[] panels,
        PanelSettings panelSettings,
        List<string> wired,
        List<string> skipped,
        List<string> failed)
    {
        var activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != scenePath)
        {
            var opened = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!opened.IsValid())
            {
                failed.Add($"scene_not_found:{scenePath}");
                return;
            }
        }

        foreach (var (slug, hostClass) in panels)
        {
            string goName = slug + "-uidoc";
            if (GameObject.Find(goName) != null)
            {
                skipped.Add(goName);
                continue;
            }

            string uxmlPath = UxmlGeneratedRoot + slug + ".uxml";
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            if (uxml == null)
            {
                failed.Add($"uxml_not_found:{uxmlPath}");
                continue;
            }

            Type hostType = ResolveHostType(hostClass);
            if (hostType == null)
            {
                failed.Add($"host_type_not_found:{hostClass}");
                continue;
            }

            try
            {
                var go  = new GameObject(goName);
                var doc = go.AddComponent<UIDocument>();
                doc.panelSettings   = panelSettings;
                doc.visualTreeAsset = uxml;

                var host = go.AddComponent(hostType) as MonoBehaviour;
                if (host != null)
                {
                    var field = hostType.GetField("_doc",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    field?.SetValue(host, doc);
                }

                wired.Add(goName);
            }
            catch (Exception ex)
            {
                failed.Add($"{goName}:{ex.Message}");
            }
        }

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }

    [UnityEditor.MenuItem("Tools/Territory/Wire CityScene UIDocuments")]
    public static void MenuWireCityScene() => RunWireCore("CityScene");

    [UnityEditor.MenuItem("Tools/Territory/Wire MainMenu UIDocuments")]
    public static void MenuWireMainMenu() => RunWireCore("MainMenu");

    [UnityEditor.MenuItem("Tools/Territory/Wire All UIDocuments")]
    public static void MenuWireAll() => RunWireCore("both");

    private static void RunWireCore(string targetScene)
    {
        var wired   = new List<string>();
        var skipped = new List<string>();
        var failed  = new List<string>();

        AssetDatabase.Refresh();
        PanelSettings panelSettings = null;
        var psGuids = AssetDatabase.FindAssets("t:PanelSettings");
        foreach (var g in psGuids)
        {
            panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(AssetDatabase.GUIDToAssetPath(g));
            if (panelSettings != null) break;
        }
        if (panelSettings == null) { Debug.LogError($"[UiWiring] PanelSettings not found via FindAssets (searched {psGuids.Length} hits)"); return; }

        if (targetScene == "CityScene" || targetScene == "both")
            WireScene("Assets/Scenes/CityScene.unity", CityScenePanels, panelSettings, wired, skipped, failed);
        if (targetScene == "MainMenu" || targetScene == "both")
            WireScene("Assets/Scenes/MainMenu.unity", MainMenuPanels, panelSettings, wired, skipped, failed);

        Debug.Log($"[UiWiring] wired={wired.Count} skipped={skipped.Count} failed={failed.Count}\n" +
                  $"wired: {string.Join(", ", wired)}\n" +
                  $"failed: {string.Join(", ", failed)}");
    }



    // Panels that are always-on HUD layer (sortingOrder = 0).
    private static readonly System.Collections.Generic.HashSet<string> HudPanelSlugs =
        new System.Collections.Generic.HashSet<string> {
            "hud-bar","toolbar","time-controls","overlay-toggle-strip","mini-map"
        };

    [UnityEditor.MenuItem("Tools/Territory/Set UIDocument SortingOrders")]
    public static void MenuSetSortingOrders()
    {
        int updated = 0;
        var docs = UnityEngine.Object.FindObjectsOfType<UIDocument>(true);
        foreach (var d in docs)
        {
            string name = d.gameObject.name;
            // Strip "-uidoc" suffix to get slug
            string slug = name.EndsWith("-uidoc") ? name.Substring(0, name.Length - 6) : name;
            int order = HudPanelSlugs.Contains(slug) ? 0 : 10;
            // pause-menu and onboarding-overlay = top-most modals
            if (slug == "pause-menu" || slug == "onboarding-overlay") order = 20;
            if ((int)d.sortingOrder != order)
            {
                d.sortingOrder = order;
                updated++;
            }
        }
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log($"[UiWiring] sortingOrders updated={updated} total={docs.Length}");
    }

    [UnityEditor.MenuItem("Tools/Territory/Disable Old UGUI UI Canvas")]
    public static void MenuDisableOldUguiCanvas()
    {
        int disabled = 0;
        var uiCanvas = GameObject.Find("UI Canvas");
        if (uiCanvas != null)
        {
            uiCanvas.SetActive(false);
            disabled++;
        }
        var ui = GameObject.Find("UI");
        if (ui != null && ui != uiCanvas)
        {
            // Don't blanket-disable "UI" — only verify candidates
        }
        // Also disable any direct old UGUI sibling panels at scene root.
        string[] oldSlugs = { "hud-bar","toolbar","glossary-panel","MiniMapPanel","ControlPanel","DebugPanel","ProposalUI","UIRegistries" };
        foreach (var n in oldSlugs)
        {
            var go = GameObject.Find(n);
            if (go != null && go.activeSelf) { go.SetActive(false); disabled++; }
        }
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log($"[UiWiring] disabled old UGUI GOs={disabled}");
    }


    [UnityEditor.MenuItem("Tools/Territory/Delete Orphan Old UGUI GOs")]
    public static void MenuDeleteOrphanOldUgui()
    {
        var orphanNames = new System.Collections.Generic.HashSet<string> {
            "hud-bar","toolbar","MiniMapPanel","glossary-panel","ControlPanel","DebugPanel","ProposalUI","UIRegistries","UI Canvas"
        };
        var activeScene = SceneManager.GetActiveScene();
        var toDelete = new System.Collections.Generic.List<GameObject>();

        // Pass 1: collect candidates safely.
        var allGos = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var go in allGos)
        {
            if (go == null) continue;
            if (!go.scene.IsValid()) continue;
            if (go.scene != activeScene) continue;
            if ((go.hideFlags & HideFlags.NotEditable) != 0) continue;
            if ((go.hideFlags & HideFlags.HideAndDontSave) != 0) continue;
            if (go.transform.parent != null) continue; // root only
            if (!orphanNames.Contains(go.name)) continue;
            toDelete.Add(go);
        }

        // Pass 2: delete.
        int deleted = 0;
        foreach (var go in toDelete)
        {
            if (go != null) { UnityEngine.Object.DestroyImmediate(go); deleted++; }
        }

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Debug.Log($"[UiWiring] deleted orphan old UGUI root GOs={deleted}");
    }

    private static Type ResolveHostType(string simpleClassName)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var t in asm.GetTypes())
            {
                if (t.Name == simpleClassName)
                    return t;
            }
        }
        return null;
    }
}
