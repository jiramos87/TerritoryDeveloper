using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Scaffold prefab library v0 under <c>Assets/UI/Prefabs/</c>: tool button, stat row, scroll shell, modal shell.
/// Menu: <b>Territory Developer → UI → Scaffold UI Prefab Library v0</b>.
/// </summary>
public static class UiPrefabLibraryScaffoldMenu
{
    const string MenuRoot = "Territory Developer/UI/";
    const string PrefabFolder = "Assets/UI/Prefabs";

    [MenuItem(MenuRoot + "Scaffold UI Prefab Library v0", priority = 10)]
    public static void ScaffoldV0Prefabs()
    {
        if (!AssetDatabase.IsValidFolder("Assets/UI"))
            AssetDatabase.CreateFolder("Assets", "UI");
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
            AssetDatabase.CreateFolder("Assets/UI", "Prefabs");

        var temp = new GameObject("_UiPrefabScaffoldRoot");
        try
        {
            CreateToolButtonPrefab(temp.transform);
            CreateStatRowPrefab(temp.transform);
            CreateScrollListShellPrefab(temp.transform);
            CreateModalShellPrefab(temp.transform);
        }
        finally
        {
            Object.DestroyImmediate(temp);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[UI Prefabs] v0 scaffold complete under " + PrefabFolder + ". Review in Inspector; re-run overwrites prefabs.");
    }

    static void CreateToolButtonPrefab(Transform parent)
    {
        var go = new GameObject("UI_ToolButton");
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(120, 40);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.196f, 0.196f, 0.196f, 1f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var tr = textGo.AddComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = tr.offsetMax = Vector2.zero;
        var text = textGo.AddComponent<Text>();
        text.text = "Tool";
        text.alignment = TextAnchor.MiddleCenter;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 12;
        text.color = Color.white;
        Save(go, "UI_ToolButton.prefab");
    }

    static void CreateStatRowPrefab(Transform parent)
    {
        var row = new GameObject("UI_StatRow");
        row.transform.SetParent(parent, false);
        var rowRect = row.AddComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(280, 36);
        var h = row.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 8;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = true;
        h.padding = new RectOffset(4, 4, 4, 4);

        var keyGo = new GameObject("Key");
        keyGo.transform.SetParent(row.transform, false);
        keyGo.AddComponent<RectTransform>();
        var keyLe = keyGo.AddComponent<LayoutElement>();
        keyLe.preferredWidth = 120;
        var keyText = keyGo.AddComponent<Text>();
        keyText.text = "Label";
        keyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        keyText.fontSize = 10;
        keyText.color = Color.white;
        keyText.alignment = TextAnchor.MiddleLeft;

        var valGo = new GameObject("Value");
        valGo.transform.SetParent(row.transform, false);
        valGo.AddComponent<RectTransform>();
        var valLe = valGo.AddComponent<LayoutElement>();
        valLe.flexibleWidth = 1;
        var valText = valGo.AddComponent<Text>();
        valText.text = "0";
        valText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        valText.fontSize = 36;
        valText.color = Color.white;
        valText.alignment = TextAnchor.MiddleRight;

        Save(row, "UI_StatRow.prefab");
    }

    static void CreateScrollListShellPrefab(Transform parent)
    {
        var scrollRoot = new GameObject("UI_ScrollListShell");
        scrollRoot.transform.SetParent(parent, false);
        var rootRect = scrollRoot.AddComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(320, 400);

        var scrollView = new GameObject("ScrollView");
        scrollView.transform.SetParent(scrollRoot.transform, false);
        var svRect = scrollView.AddComponent<RectTransform>();
        svRect.anchorMin = Vector2.zero;
        svRect.anchorMax = Vector2.one;
        svRect.offsetMin = new Vector2(8, 8);
        svRect.offsetMax = new Vector2(-8, -8);

        var scroll = scrollView.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.scrollSensitivity = 18f;

        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollView.transform, false);
        var vpRect = viewport.AddComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.offsetMin = vpRect.offsetMax = Vector2.zero;
        var vpImg = viewport.AddComponent<Image>();
        vpImg.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        var listContent = new GameObject("Content");
        listContent.transform.SetParent(viewport.transform, false);
        var cRect = listContent.AddComponent<RectTransform>();
        cRect.anchorMin = new Vector2(0, 1);
        cRect.anchorMax = new Vector2(1, 1);
        cRect.pivot = new Vector2(0.5f, 1);
        cRect.sizeDelta = Vector2.zero;
        var vlg = listContent.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4;
        vlg.childForceExpandHeight = false;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.padding = new RectOffset(4, 4, 4, 4);
        var csf = listContent.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scroll.content = cRect;
        scroll.viewport = vpRect;

        Save(scrollRoot, "UI_ScrollListShell.prefab");
    }

    static void CreateModalShellPrefab(Transform parent)
    {
        var modal = new GameObject("UI_ModalShell");
        modal.transform.SetParent(parent, false);
        var mRect = modal.AddComponent<RectTransform>();
        mRect.anchorMin = Vector2.zero;
        mRect.anchorMax = Vector2.one;
        mRect.offsetMin = mRect.offsetMax = Vector2.zero;
        var dim = modal.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.55f);
        dim.raycastTarget = true;

        var panel = new GameObject("Panel");
        panel.transform.SetParent(modal.transform, false);
        var pRect = panel.AddComponent<RectTransform>();
        pRect.anchorMin = pRect.anchorMax = new Vector2(0.5f, 0.5f);
        pRect.sizeDelta = new Vector2(480, 360);
        var pBg = panel.AddComponent<Image>();
        pBg.color = new Color(0.15f, 0.15f, 0.18f, 1f);

        var title = new GameObject("Title");
        title.transform.SetParent(panel.transform, false);
        var tRect = title.AddComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0, 1);
        tRect.anchorMax = new Vector2(1, 1);
        tRect.pivot = new Vector2(0.5f, 1);
        tRect.anchoredPosition = new Vector2(0, -8);
        tRect.sizeDelta = new Vector2(-16, 28);
        var tt = title.AddComponent<Text>();
        tt.text = "Title";
        tt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tt.fontSize = 14;
        tt.color = Color.white;
        tt.alignment = TextAnchor.MiddleLeft;

        Save(modal, "UI_ModalShell.prefab");
    }

    static void Save(GameObject instance, string fileName)
    {
        string path = Path.Combine(PrefabFolder, fileName).Replace('\\', '/');
        PrefabUtility.SaveAsPrefabAsset(instance, path);
    }
}
