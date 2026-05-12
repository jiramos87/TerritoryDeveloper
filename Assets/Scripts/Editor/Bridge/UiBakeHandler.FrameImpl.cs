using System;
using System.Collections.Generic;
using Territory.UI;
using Territory.UI.Editor;
using Territory.UI.Modals;
using Territory.UI.StudioControls;
using Territory.UI.StudioControls.Renderers;
using Territory.UI.Themed;
using Territory.UI.Themed.Renderers;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Territory.Editor.Bridge
{
    // Overflow implementation extracted from UiBakeHandler.Frame.cs by Stage 6.0 Tier-D.
    // Thin shell stays in UiBakeHandler.Frame.cs (≤200 LOC); all logic lives here.
    public static partial class UiBakeHandler
    {
        // ── Panel prefab bake (Stage 3 T3.5) ────────────────────────────────────

        static BakeError SavePanelPrefab(IrPanel panel, string assetPath, string dir, UiTheme theme)
        {
            // dir kept for future variant resolution; not consumed in T7.0 path.
            _ = dir;

            GameObject go = null;
            try
            {
                // Anti-loss guard (Stage 13.7 fallout fix): refuse silent placeholder
                // overwrite when an existing prefab on disk already carries non-default
                // RectTransform. Prevents wholesale wipe of authored positioning state
                // across re-bakes.
                if (ExistingPrefabHasNonDefaultRect(assetPath))
                {
                    return new BakeError
                    {
                        error = "panel_layout_rect_missing",
                        details = $"panel '{panel.slug}' would overwrite existing prefab at '{assetPath}' " +
                                  $"which carries non-default RectTransform. Refusing overwrite — " +
                                  $"hand-edit the prefab anchors in the Unity Inspector or wait for " +
                                  $"the catalog-snapshot rect surface (DEC-A24 §3 D2) before re-bake.",
                        path = assetPath,
                    };
                }

                go = new GameObject(panel.slug);
                var rootRect = go.AddComponent<RectTransform>();
                rootRect.anchorMin = new Vector2(0f, 1f);
                rootRect.anchorMax = new Vector2(0f, 1f);
                rootRect.pivot = new Vector2(0f, 1f);
                rootRect.anchoredPosition = new Vector2(8f, -8f);
                rootRect.sizeDelta = new Vector2(200f, 80f);
                Debug.LogWarning(
                    $"[UiBakeHandler] panel '{panel.slug}' baked with top-left sentinel 200×80 rect. " +
                    $"Hand-edit anchors in the Inspector; rect catalog wiring is a follow-up Task.");
                var bgImage = go.AddComponent<Image>();
                bgImage.color = Color.white;
                bgImage.raycastTarget = string.Equals(panel.kind, "modal", StringComparison.Ordinal);
                var themedPanel = go.AddComponent<ThemedPanel>();
                WireThemeRef(themedPanel, theme);

                var so = new SerializedObject(themedPanel);
                var slotsProp = so.FindProperty("_slots");
                var childrenProp = so.FindProperty("_children");
                var paletteProp = so.FindProperty("_paletteSlug");
                var bgProp = so.FindProperty("_backgroundImage");
                var kindProp = so.FindProperty("_kind");
                if (paletteProp != null) paletteProp.stringValue = "chassis-graphite";
                if (bgProp != null) bgProp.objectReferenceValue = bgImage;
                if (kindProp != null) kindProp.enumValueIndex = ResolvePanelKindIndex(panel.kind);

                var childrenList = new List<GameObject>();

                int slotCount = panel.slots != null ? panel.slots.Length : 0;
                slotsProp.arraySize = slotCount;
                var perKindCounters = new Dictionary<string, int>();
                for (int s = 0; s < slotCount; s++)
                {
                    var slot = panel.slots[s];
                    var slotProp = slotsProp.GetArrayElementAtIndex(s);
                    var slugProp = slotProp.FindPropertyRelative("slug");
                    var acceptsProp = slotProp.FindPropertyRelative("accepts");

                    slugProp.stringValue = slot?.name ?? string.Empty;

                    var slotAccepts = slot?.accepts ?? Array.Empty<string>();
                    acceptsProp.arraySize = slotAccepts.Length;
                    for (int a = 0; a < slotAccepts.Length; a++)
                    {
                        acceptsProp.GetArrayElementAtIndex(a).stringValue = slotAccepts[a] ?? string.Empty;
                    }

                    var slotChildren = slot?.children ?? Array.Empty<string>();
                    var slotLabels = slot?.labels;
                    var slotIconSpriteSlugs = slot?.iconSpriteSlugs;
                    for (int c = 0; c < slotChildren.Length; c++)
                    {
                        var childKind = slotChildren[c];
                        if (string.IsNullOrEmpty(childKind)) continue;
                        if (!IsKnownStudioControlKind(childKind))
                        {
                            Debug.LogWarning(
                                $"[UiBakeHandler] panel={panel.slug} slot={slot?.name} child kind={childKind} unknown — skipped");
                            continue;
                        }
                        if (!perKindCounters.TryGetValue(childKind, out int kindCounter))
                        {
                            kindCounter = 0;
                        }
                        var childLabel = (slotLabels != null && c < slotLabels.Length) ? slotLabels[c] : null;
                        var childIconSpriteSlug = (slotIconSpriteSlugs != null && c < slotIconSpriteSlugs.Length) ? slotIconSpriteSlugs[c] : null;
                        var childGo = InstantiatePanelChild(childKind, go.transform, ref kindCounter, theme, childLabel, childIconSpriteSlug);
                        perKindCounters[childKind] = kindCounter;
                        if (childGo == null) continue;
                        childrenList.Add(childGo);
                    }
                }

                childrenProp.arraySize = childrenList.Count;
                for (int i = 0; i < childrenList.Count; i++)
                {
                    childrenProp.GetArrayElementAtIndex(i).objectReferenceValue = childrenList[i];
                }

                so.ApplyModifiedPropertiesWithoutUndo();

                var panelRow = new IrInteractive
                {
                    slug = panel.slug,
                    kind = "themed-panel",
                    juice = null,
                };
                AttachJuiceComponents(go, panelRow);

                if (panel.slug == "pause-menu" && childrenList.Count >= 6)
                {
                    var adapter = go.AddComponent<PauseMenuDataAdapter>();
                    var aSo = new SerializedObject(adapter);
                    void BindBtn(string fieldName, int idx)
                    {
                        if (idx < 0 || idx >= childrenList.Count) return;
                        var btnComp = childrenList[idx]?.GetComponent<ThemedButton>();
                        if (btnComp == null) return;
                        var prop = aSo.FindProperty(fieldName);
                        if (prop != null) prop.objectReferenceValue = btnComp;
                    }
                    BindBtn("_resumeButton", 0);
                    BindBtn("_saveButton", 1);
                    BindBtn("_loadButton", 2);
                    BindBtn("_settingsButton", 3);
                    BindBtn("_mainMenuButton", 4);
                    BindBtn("_quitButton", 5);

                    void BindPrefab(string fieldName, string prefabPath)
                    {
                        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                        if (prefab == null) return;
                        var prop = aSo.FindProperty(fieldName);
                        if (prop != null) prop.objectReferenceValue = prefab;
                    }
                    BindPrefab("_settingsViewPrefab", "Assets/UI/Prefabs/Generated/settings-view.prefab");
                    BindPrefab("_saveLoadViewPrefab", "Assets/UI/Prefabs/Generated/save-load-view.prefab");

                    aSo.ApplyModifiedPropertiesWithoutUndo();
                }

                if (panel.slug == "info-panel" && childrenList.Count > 0)
                {
                    var adapter = go.AddComponent<Territory.UI.HUD.InfoPanelAdapter>();
                    var aSo = new SerializedObject(adapter);
                    var widgetsProp = aSo.FindProperty("_widgets");
                    if (widgetsProp != null)
                    {
                        widgetsProp.arraySize = childrenList.Count;
                        for (int i = 0; i < childrenList.Count; i++)
                        {
                            var sc = childrenList[i]?.GetComponent<StudioControlBase>();
                            widgetsProp.GetArrayElementAtIndex(i).objectReferenceValue = sc;
                        }
                        aSo.ApplyModifiedPropertiesWithoutUndo();
                    }
                }

                EmitRowChildren(panel, go.transform, theme);

                if (panel.tabs != null && panel.tabs.Length > 0)
                {
                    WireTabBarPages(panel, childrenList);
                }

                BakePanelArchetype(panel, go, theme);
                ApplySpacing(panel, go);

                Image borderTop = null, borderBottom = null, borderLeft = null, borderRight = null;
                {
                    const string FallbackFrameSlug = "ui/frame/default";
                    string frameSlug = !string.IsNullOrEmpty(panel.frame_style_slug)
                        ? panel.frame_style_slug
                        : FallbackFrameSlug;
                    var frameSprite = AtlasIndex.Resolve(frameSlug);
                    if (frameSprite == null && panel.frame_style_slug != null)
                    {
                        frameSprite = AtlasIndex.Resolve(FallbackFrameSlug);
                    }
                    if (frameSprite != null) bgImage.sprite = frameSprite;

                    float thickness = ResolveBorderThickness(panel.frame_style_slug);
                    borderTop = SpawnBorderStrip(go.transform, "BorderTop", BorderEdge.Top, thickness);
                    borderBottom = SpawnBorderStrip(go.transform, "BorderBottom", BorderEdge.Bottom, thickness);
                    borderLeft = SpawnBorderStrip(go.transform, "BorderLeft", BorderEdge.Left, thickness);
                    borderRight = SpawnBorderStrip(go.transform, "BorderRight", BorderEdge.Right, thickness);

                    var panelSo2 = new SerializedObject(themedPanel);
                    var frameSlugProp = panelSo2.FindProperty("_frameStyleSlug");
                    if (frameSlugProp != null) frameSlugProp.stringValue = frameSlug;
                    var topProp = panelSo2.FindProperty("_borderTop");
                    if (topProp != null) topProp.objectReferenceValue = borderTop;
                    var botProp = panelSo2.FindProperty("_borderBottom");
                    if (botProp != null) botProp.objectReferenceValue = borderBottom;
                    var lftProp = panelSo2.FindProperty("_borderLeft");
                    if (lftProp != null) lftProp.objectReferenceValue = borderLeft;
                    var rgtProp = panelSo2.FindProperty("_borderRight");
                    if (rgtProp != null) rgtProp.objectReferenceValue = borderRight;
                    panelSo2.ApplyModifiedPropertiesWithoutUndo();
                }

                if (!string.IsNullOrEmpty(panel.illumination_slug))
                {
                    var illumGo = new GameObject("IlluminationLayer", typeof(RectTransform));
                    illumGo.transform.SetParent(go.transform, worldPositionStays: false);
                    var illumImg = illumGo.AddComponent<Image>();
                    illumImg.raycastTarget = false;
                    illumImg.color = new Color(1f, 1f, 1f, 0f);
                    var illumLayer = illumGo.AddComponent<ThemedIlluminationLayer>();
                    WireThemeRef(illumLayer, theme);
                    var illumSo = new SerializedObject(illumLayer);
                    var illumSlugProp = illumSo.FindProperty("_illuminationSlug");
                    if (illumSlugProp != null) illumSlugProp.stringValue = panel.illumination_slug;
                    var illumImgProp = illumSo.FindProperty("_overlayImage");
                    if (illumImgProp != null) illumImgProp.objectReferenceValue = illumImg;
                    illumSo.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(go, assetPath);
                return null;
            }
            catch (Exception ex)
            {
                return new BakeError
                {
                    error = "prefab_write_failed",
                    details = ex.Message,
                    path = assetPath,
                };
            }
            finally
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
        }

        // ── Stage 13.2 (TECH-9854) — IR v2 row hierarchy + tab page wiring ──────

        static void EmitRowChildren(IrPanel panel, Transform panelRoot, UiTheme theme)
        {
            if (panel?.rows == null || panel.rows.Length == 0) return;
            if (panelRoot == null) return;

            bool isHeader;
            for (int r = 0; r < panel.rows.Length; r++)
            {
                var row = panel.rows[r];
                if (row == null) continue;
                isHeader = string.Equals(row.kind, "header", StringComparison.Ordinal);

                var rowName = string.IsNullOrEmpty(row.label) ? $"Row {r}" : $"Row {r} ({row.label})";
                var rowGo = new GameObject(rowName, typeof(RectTransform));
                rowGo.transform.SetParent(panelRoot, worldPositionStays: false);
                var rowLe = rowGo.AddComponent<LayoutElement>();
                rowLe.preferredHeight = isHeader ? 28f : 24f;
                rowLe.flexibleWidth = 1f;
                var rowHlg = rowGo.AddComponent<HorizontalLayoutGroup>();
                rowHlg.childAlignment = TextAnchor.MiddleLeft;
                rowHlg.childControlWidth = true;
                rowHlg.childControlHeight = true;
                rowHlg.childForceExpandWidth = true;
                rowHlg.childForceExpandHeight = true;
                rowHlg.spacing = 8f;
                rowHlg.padding = new RectOffset(8, 8, 0, 0);

                if (!string.IsNullOrEmpty(row.iconSlug))
                {
                    SpawnRowIcon(rowGo.transform, row.iconSlug, theme, rowLe.preferredHeight);
                }

                var captionGo = new GameObject("Caption", typeof(RectTransform));
                captionGo.transform.SetParent(rowGo.transform, worldPositionStays: false);
                var captionTmp = captionGo.AddComponent<TextMeshProUGUI>();
                captionTmp.text = row.label ?? string.Empty;
                captionTmp.fontSize = isHeader ? 14f : 12f;
                captionTmp.fontStyle = isHeader ? FontStyles.Bold : FontStyles.Normal;
                captionTmp.alignment = TextAlignmentOptions.MidlineLeft;
                captionTmp.color = Color.white;
                captionTmp.raycastTarget = false;
                var captionLe = captionGo.AddComponent<LayoutElement>();
                captionLe.flexibleWidth = 1f;
                captionLe.preferredHeight = rowLe.preferredHeight;

                if (!isHeader)
                {
                    var valueGo = new GameObject("Value", typeof(RectTransform));
                    valueGo.transform.SetParent(rowGo.transform, worldPositionStays: false);
                    var valueTmp = valueGo.AddComponent<TextMeshProUGUI>();
                    valueTmp.text = row.value ?? string.Empty;
                    valueTmp.fontSize = 12f;
                    valueTmp.alignment = TextAlignmentOptions.MidlineRight;
                    valueTmp.color = Color.white;
                    valueTmp.raycastTarget = false;
                    var valueLe = valueGo.AddComponent<LayoutElement>();
                    valueLe.flexibleWidth = 1f;
                    valueLe.preferredHeight = rowLe.preferredHeight;
                }
            }
        }

        static void SpawnRowIcon(Transform rowRoot, string iconSlug, UiTheme theme, float rowHeight)
        {
            if (rowRoot == null || string.IsNullOrEmpty(iconSlug)) return;
            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(rowRoot, worldPositionStays: false);
            var img = iconGo.AddComponent<Image>();
            img.color = Color.white;
            img.raycastTarget = false;
            img.preserveAspect = true;
            var themedIcon = iconGo.AddComponent<ThemedIcon>();
            WireThemeRef(themedIcon, theme);
            themedIcon.IconSlug = iconSlug;
            var so = new SerializedObject(themedIcon);
            var imgProp = so.FindProperty("_iconImage");
            if (imgProp != null) imgProp.objectReferenceValue = img;
            var slugProp = so.FindProperty("_iconSlug");
            if (slugProp != null) slugProp.stringValue = iconSlug;
            so.ApplyModifiedPropertiesWithoutUndo();

            float side = rowHeight > 0f ? rowHeight - 4f : 20f;
            var le = iconGo.AddComponent<LayoutElement>();
            le.preferredWidth = side;
            le.preferredHeight = side;
            le.flexibleWidth = 0f;
        }

        static void WireTabBarPages(IrPanel panel, List<GameObject> panelChildren)
        {
            if (panel?.tabs == null || panel.tabs.Length == 0) return;
            if (panelChildren == null) return;

            ThemedTabBar tabBar = null;
            for (int i = 0; i < panelChildren.Count; i++)
            {
                if (panelChildren[i] == null) continue;
                tabBar = panelChildren[i].GetComponent<ThemedTabBar>();
                if (tabBar != null) break;
            }
            if (tabBar == null)
            {
                Debug.LogWarning(
                    $"[UiBakeHandler] panel={panel.slug} carries tabs[] but no themed-tab-bar slot child — pages[] wiring skipped");
                return;
            }

            var so = new SerializedObject(tabBar);
            var pagesProp = so.FindProperty("_pages");
            if (pagesProp == null)
            {
                Debug.LogWarning($"[UiBakeHandler] panel={panel.slug} ThemedTabBar._pages property missing — wiring skipped");
                return;
            }

            pagesProp.arraySize = panel.tabs.Length;
            for (int t = 0; t < panel.tabs.Length; t++)
            {
                var tab = panel.tabs[t];
                var elem = pagesProp.GetArrayElementAtIndex(t);
                var idProp = elem.FindPropertyRelative("id");
                var labelProp = elem.FindPropertyRelative("label");
                var activeProp = elem.FindPropertyRelative("active");
                var iconSlugProp = elem.FindPropertyRelative("iconSlug");
                if (idProp != null) idProp.stringValue = tab?.id ?? string.Empty;
                if (labelProp != null) labelProp.stringValue = tab?.label ?? string.Empty;
                if (activeProp != null) activeProp.boolValue = tab?.active ?? false;
                if (iconSlugProp != null) iconSlugProp.stringValue = tab?.iconSlug ?? string.Empty;
            }

            var initialProp = so.FindProperty("_initialIndex");
            if (initialProp != null)
            {
                int requested = panel.defaultTabIndex;
                if (requested < 0 || requested >= panel.tabs.Length)
                {
                    Debug.LogWarning(
                        $"[UiBakeHandler] panel={panel.slug} defaultTabIndex={requested} out of range [0,{panel.tabs.Length}) — clamping to 0");
                    requested = 0;
                }
                initialProp.intValue = requested;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EmitTabCells(tabBar, panel.tabs.Length);
        }

        static void EmitTabCells(ThemedTabBar tabBar, int tabCount)
        {
            if (tabBar == null || tabCount <= 0) return;
            var barRoot = tabBar.gameObject;
            var indicatorGos = new GameObject[tabCount];

            float cellWidth = 1f / tabCount;
            for (int i = 0; i < tabCount; i++)
            {
                var cellName = $"TabCell_{i}";
                var existing = barRoot.transform.Find(cellName);
                GameObject cellGo;
                if (existing == null)
                {
                    cellGo = new GameObject(cellName, typeof(RectTransform));
                    cellGo.transform.SetParent(barRoot.transform, worldPositionStays: false);
                    var rt = (RectTransform)cellGo.transform;
                    rt.anchorMin = new Vector2(i * cellWidth, 0f);
                    rt.anchorMax = new Vector2((i + 1) * cellWidth, 1f);
                    rt.offsetMin = rt.offsetMax = Vector2.zero;
                    var hit = cellGo.AddComponent<Image>();
                    hit.color = new Color(0f, 0f, 0f, 0f);
                    hit.raycastTarget = true;
                    cellGo.AddComponent<ThemedTabCell>();
                }
                else
                {
                    cellGo = existing.gameObject;
                    if (cellGo.GetComponent<Image>() == null)
                    {
                        var hit = cellGo.AddComponent<Image>();
                        hit.color = new Color(0f, 0f, 0f, 0f);
                        hit.raycastTarget = true;
                    }
                    if (cellGo.GetComponent<ThemedTabCell>() == null)
                    {
                        cellGo.AddComponent<ThemedTabCell>();
                    }
                }

                var cell = cellGo.GetComponent<ThemedTabCell>();
                var cellSo = new SerializedObject(cell);
                var idxProp = cellSo.FindProperty("_index");
                var parentProp = cellSo.FindProperty("_parentTabBar");
                if (idxProp != null) idxProp.intValue = i;
                if (parentProp != null) parentProp.objectReferenceValue = tabBar;
                cellSo.ApplyModifiedPropertiesWithoutUndo();

                indicatorGos[i] = cellGo;
            }

            var barSo = new SerializedObject(tabBar);
            var indicatorsProp = barSo.FindProperty("_tabIndicators");
            if (indicatorsProp != null)
            {
                indicatorsProp.arraySize = tabCount;
                for (int i = 0; i < tabCount; i++)
                {
                    indicatorsProp.GetArrayElementAtIndex(i).objectReferenceValue = indicatorGos[i];
                }
                barSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // ── Stage 7 T7.0 — embedded panel child instantiation ───────────────────

        static GameObject InstantiatePanelChild(string kind, Transform panelRoot, ref int duplicateCounter, UiTheme theme, string label = null, string iconSpriteSlug = null)
        {
            if (panelRoot == null || string.IsNullOrEmpty(kind)) return null;

            var name = duplicateCounter == 0 ? kind : $"{kind} ({duplicateCounter})";
            duplicateCounter++;

            var childGo = new GameObject(name, typeof(RectTransform));
            childGo.transform.SetParent(panelRoot, worldPositionStays: false);

            switch (kind)
            {
                case "knob":
                {
                    var knob = childGo.AddComponent<Knob>();
                    WireThemeRef(knob, theme);
                    knob.ApplyDetail(new KnobDetail());
                    EnsureChildLayoutElement(childGo, preferredWidth: 64f, preferredHeight: 64f, flexibleWidth: 0f);
                    break;
                }
                case "fader":
                {
                    var fader = childGo.AddComponent<Fader>();
                    WireThemeRef(fader, theme);
                    fader.ApplyDetail(new FaderDetail());
                    EnsureChildLayoutElement(childGo, preferredWidth: 60f, preferredHeight: 120f, flexibleWidth: 0f);
                    break;
                }
                case "detent-ring":
                {
                    var dr = childGo.AddComponent<DetentRing>();
                    WireThemeRef(dr, theme);
                    dr.ApplyDetail(new DetentRingDetail());
                    EnsureChildLayoutElement(childGo, preferredWidth: 80f, preferredHeight: 80f, flexibleWidth: 0f);
                    break;
                }
                case "vu-meter":
                {
                    var vu = childGo.AddComponent<VUMeter>();
                    WireThemeRef(vu, theme);
                    var vuRend = childGo.GetComponent<VUMeterRenderer>();
                    if (vuRend == null)
                    {
                        vuRend = childGo.AddComponent<VUMeterRenderer>();
                    }
                    WireThemeRef(vuRend, theme);
                    SpawnVUMeterRenderTargets(childGo);
                    vu.ApplyDetail(new VUMeterDetail());
                    EnsureChildLayoutElement(childGo, preferredWidth: 96f, preferredHeight: 40f, flexibleWidth: 1f);
                    break;
                }
                case "oscilloscope":
                {
                    var osc = childGo.AddComponent<Oscilloscope>();
                    WireThemeRef(osc, theme);
                    osc.ApplyDetail(new OscilloscopeDetail());
                    EnsureChildLayoutElement(childGo, preferredWidth: 160f, preferredHeight: 80f, flexibleWidth: 1f);
                    break;
                }
                case "illuminated-button":
                {
                    var btn = childGo.AddComponent<IlluminatedButton>();
                    WireThemeRef(btn, theme);
                    var btnRend = childGo.GetComponent<IlluminatedButtonRenderer>();
                    if (btnRend == null)
                    {
                        btnRend = childGo.AddComponent<IlluminatedButtonRenderer>();
                    }
                    WireThemeRef(btnRend, theme);
                    bool iconSpriteResolved = SpawnIlluminatedButtonRenderTargets(childGo, iconSpriteSlug, out var ibBody, out var ibHalo);
                    WireIlluminatedButtonHoverAndPress(childGo, btnRend, ibBody, ibHalo, theme);
                    btn.ApplyDetail(new IlluminatedButtonDetail { iconSpriteSlug = iconSpriteSlug });
                    bool isPlaceholderSlug = string.IsNullOrEmpty(iconSpriteSlug) || iconSpriteSlug == "empty";
                    if ((!iconSpriteResolved || isPlaceholderSlug) && !string.IsNullOrEmpty(label))
                    {
                        SpawnIlluminatedButtonCaption(childGo, label);
                    }
                    EnsureChildLayoutElement(childGo, preferredWidth: 64f, preferredHeight: 64f, flexibleWidth: 0f);
                    break;
                }
                case "led":
                {
                    var led = childGo.AddComponent<LED>();
                    WireThemeRef(led, theme);
                    led.ApplyDetail(new LEDDetail());
                    EnsureChildLayoutElement(childGo, preferredWidth: 16f, preferredHeight: 16f, flexibleWidth: 0f);
                    break;
                }
                case "segmented-readout":
                {
                    var sr = childGo.AddComponent<SegmentedReadout>();
                    WireThemeRef(sr, theme);
                    var srRend = childGo.GetComponent<SegmentedReadoutRenderer>();
                    if (srRend == null)
                    {
                        srRend = childGo.AddComponent<SegmentedReadoutRenderer>();
                    }
                    WireThemeRef(srRend, theme);
                    var sd = new SegmentedReadoutDetail { digits = 1 };
                    SpawnSegmentedReadoutRenderTargets(childGo, sd);
                    sr.ApplyDetail(sd);
                    EnsureChildLayoutElement(childGo, preferredWidth: 120f, preferredHeight: 32f, flexibleWidth: 1f);
                    break;
                }
                case "themed-overlay-toggle-row":
                {
                    var row = childGo.AddComponent<ThemedOverlayToggleRow>();
                    WireThemeRef(row, theme);
                    var renderer = childGo.AddComponent<ThemedOverlayToggleRowRenderer>();
                    WireThemeRef(renderer, theme);
                    SpawnThemedOverlayToggleRowChildren(childGo, out var labelTmp, out var iconImage, out var unityToggle);
                    var rendererSo = new SerializedObject(renderer);
                    var labelProp = rendererSo.FindProperty("_labelText");
                    if (labelProp != null) labelProp.objectReferenceValue = labelTmp;
                    var iconProp = rendererSo.FindProperty("_iconImage");
                    if (iconProp != null) iconProp.objectReferenceValue = iconImage;
                    rendererSo.ApplyModifiedPropertiesWithoutUndo();
                    _ = unityToggle;
                    break;
                }
                case "themed-button":
                {
                    var btn = childGo.AddComponent<ThemedButton>();
                    WireThemeRef(btn, theme);
                    var btnImg = childGo.AddComponent<Image>();
                    btnImg.color = Color.white;
                    btnImg.raycastTarget = true;
                    var unityBtn = childGo.AddComponent<Button>();
                    unityBtn.targetGraphic = btnImg;
                    unityBtn.transition = Selectable.Transition.ColorTint;
                    var btnSo = new SerializedObject(btn);
                    var btnImgProp = btnSo.FindProperty("_buttonImage");
                    if (btnImgProp != null) btnImgProp.objectReferenceValue = btnImg;
                    var btnPalette = btnSo.FindProperty("_paletteSlug");
                    if (btnPalette != null) btnPalette.stringValue = "chassis-graphite";
                    var btnFrame = btnSo.FindProperty("_frameStyleSlug");
                    if (btnFrame != null) btnFrame.stringValue = "thin";
                    btnSo.ApplyModifiedPropertiesWithoutUndo();
                    SpawnThemedButtonCaption(childGo, label);
                    EnsureChildLayoutElement(childGo, preferredWidth: 280f, preferredHeight: 56f, flexibleWidth: 1f);
                    break;
                }
                case "themed-label":
                {
                    var lbl = childGo.AddComponent<ThemedLabel>();
                    WireThemeRef(lbl, theme);
                    SpawnThemedLabelChild(childGo, out var labelTmp);
                    if (labelTmp != null) labelTmp.text = string.IsNullOrEmpty(label) ? "--" : label;
                    var lblSo = new SerializedObject(lbl);
                    var tmpProp = lblSo.FindProperty("_tmpText");
                    if (tmpProp != null) tmpProp.objectReferenceValue = labelTmp;
                    var lblPalette = lblSo.FindProperty("_paletteSlug");
                    if (lblPalette != null) lblPalette.stringValue = "silkscreen";
                    lblSo.ApplyModifiedPropertiesWithoutUndo();
                    EnsureChildLayoutElement(childGo, preferredWidth: -1f, preferredHeight: 32f, flexibleWidth: 1f);
                    break;
                }
                case "themed-slider":
                {
                    var slider = childGo.AddComponent<ThemedSlider>();
                    WireThemeRef(slider, theme);
                    var sliderSo = new SerializedObject(slider);
                    var sliderPalette = sliderSo.FindProperty("_paletteSlug");
                    if (sliderPalette != null) sliderPalette.stringValue = "chassis-graphite";
                    sliderSo.ApplyModifiedPropertiesWithoutUndo();
                    var rend = childGo.AddComponent<ThemedSliderRenderer>();
                    WireThemeRef(rend, theme);
                    SpawnThemedSliderChildren(childGo, out var trackImg, out var fillImg, out var thumbImg, out var valueText);
                    var so = new SerializedObject(rend);
                    var trackProp = so.FindProperty("_trackImage");
                    if (trackProp != null) trackProp.objectReferenceValue = trackImg;
                    var fillProp = so.FindProperty("_fillImage");
                    if (fillProp != null) fillProp.objectReferenceValue = fillImg;
                    var thumbProp = so.FindProperty("_thumbImage");
                    if (thumbProp != null) thumbProp.objectReferenceValue = thumbImg;
                    var textProp = so.FindProperty("_valueText");
                    if (textProp != null) textProp.objectReferenceValue = valueText;
                    var rendPalette = so.FindProperty("_paletteSlug");
                    if (rendPalette != null) rendPalette.stringValue = "led-cyan";
                    so.ApplyModifiedPropertiesWithoutUndo();
                    break;
                }
                case "themed-toggle":
                {
                    var tg = childGo.AddComponent<ThemedToggle>();
                    WireThemeRef(tg, theme);
                    var tgSo = new SerializedObject(tg);
                    var tgPalette = tgSo.FindProperty("_paletteSlug");
                    if (tgPalette != null) tgPalette.stringValue = "chassis-graphite";
                    tgSo.ApplyModifiedPropertiesWithoutUndo();
                    var rend = childGo.AddComponent<ThemedToggleRenderer>();
                    WireThemeRef(rend, theme);
                    SpawnThemedToggleChildren(childGo, out var checkmarkImg, out var toggleLabelTmp);
                    var so = new SerializedObject(rend);
                    var checkProp = so.FindProperty("_checkmarkImage");
                    if (checkProp != null) checkProp.objectReferenceValue = checkmarkImg;
                    var labelProp2 = so.FindProperty("_labelText");
                    if (labelProp2 != null) labelProp2.objectReferenceValue = toggleLabelTmp;
                    var rendPalette = so.FindProperty("_paletteSlug");
                    if (rendPalette != null) rendPalette.stringValue = "led-grass";
                    so.ApplyModifiedPropertiesWithoutUndo();
                    break;
                }
                case "themed-tab-bar":
                {
                    var tabBar = childGo.AddComponent<ThemedTabBar>();
                    WireThemeRef(tabBar, theme);
                    var rend = childGo.AddComponent<ThemedTabBarRenderer>();
                    WireThemeRef(rend, theme);
                    SpawnThemedTabBarChildren(childGo, out var stripImg, out var indicatorImg, out var tabLabelTmp);
                    var rendSo = new SerializedObject(rend);
                    var indicatorProp = rendSo.FindProperty("_activeTabIndicator");
                    if (indicatorProp != null) indicatorProp.objectReferenceValue = indicatorImg;
                    var tabLabelProp = rendSo.FindProperty("_tabLabel");
                    if (tabLabelProp != null) tabLabelProp.objectReferenceValue = tabLabelTmp;
                    var rendPalette = rendSo.FindProperty("_paletteSlug");
                    if (rendPalette != null) rendPalette.stringValue = "led-amber";
                    rendSo.ApplyModifiedPropertiesWithoutUndo();
                    var tabBarSo = new SerializedObject(tabBar);
                    var stripProp = tabBarSo.FindProperty("_tabStripImage");
                    if (stripProp != null) stripProp.objectReferenceValue = stripImg;
                    var tabBarPalette = tabBarSo.FindProperty("_paletteSlug");
                    if (tabBarPalette != null) tabBarPalette.stringValue = "chassis-graphite";
                    tabBarSo.ApplyModifiedPropertiesWithoutUndo();
                    break;
                }
                case "themed-list":
                {
                    var lst = childGo.AddComponent<ThemedList>();
                    WireThemeRef(lst, theme);
                    var lstSo = new SerializedObject(lst);
                    var lstPalette = lstSo.FindProperty("_paletteSlug");
                    if (lstPalette != null) lstPalette.stringValue = "chassis-graphite";
                    lstSo.ApplyModifiedPropertiesWithoutUndo();
                    var lstRend = childGo.AddComponent<ThemedListRenderer>();
                    WireThemeRef(lstRend, theme);
                    var lstRendSo = new SerializedObject(lstRend);
                    var lstRendPalette = lstRendSo.FindProperty("_paletteSlug");
                    if (lstRendPalette != null) lstRendPalette.stringValue = "led-amber";
                    lstRendSo.ApplyModifiedPropertiesWithoutUndo();
                    break;
                }
                case "themed-tooltip":
                {
                    var tooltip = childGo.AddComponent<ThemedTooltip>();
                    WireThemeRef(tooltip, theme);
                    SpawnThemedTooltipChildren(childGo, out var bgImg, out var arrowImg, out var bodyTmp);
                    var tipSo = new SerializedObject(tooltip);
                    var bgProp = tipSo.FindProperty("_backgroundImage");
                    if (bgProp != null) bgProp.objectReferenceValue = bgImg;
                    var tmpProp = tipSo.FindProperty("_tmpText");
                    if (tmpProp != null) tmpProp.objectReferenceValue = bodyTmp;
                    var palette = tipSo.FindProperty("_paletteSlug");
                    if (palette != null) palette.stringValue = "chassis-graphite";
                    var fontFace = tipSo.FindProperty("_fontFaceSlug");
                    if (fontFace != null) fontFace.stringValue = "silkscreen";
                    tipSo.ApplyModifiedPropertiesWithoutUndo();
                    if (bodyTmp != null && !string.IsNullOrEmpty(label)) bodyTmp.text = label;
                    var tipRend = childGo.AddComponent<ThemedTooltipRenderer>();
                    WireThemeRef(tipRend, theme);
                    var tipRendSo = new SerializedObject(tipRend);
                    var arrowProp = tipRendSo.FindProperty("_arrowImage");
                    if (arrowProp != null) arrowProp.objectReferenceValue = arrowImg;
                    var bodyProp = tipRendSo.FindProperty("_bodyLabel");
                    if (bodyProp != null) bodyProp.objectReferenceValue = bodyTmp;
                    var tipRendPalette = tipRendSo.FindProperty("_paletteSlug");
                    if (tipRendPalette != null) tipRendPalette.stringValue = "led-amber";
                    tipRendSo.ApplyModifiedPropertiesWithoutUndo();
                    break;
                }
                default:
                    UnityEngine.Object.DestroyImmediate(childGo);
                    return null;
            }

            var studio = childGo.GetComponent<StudioControlBase>();
            if (studio != null)
            {
                var studioSo = new SerializedObject(studio);
                var slugProp = studioSo.FindProperty("_slug");
                if (slugProp != null && slugProp.propertyType == SerializedPropertyType.String)
                {
                    slugProp.stringValue = childGo.name;
                    studioSo.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            return childGo;
        }

        // ── Step 16.2 — procedural border strips ────────────────────────────────

        enum BorderEdge { Top, Bottom, Left, Right }

        static Image SpawnBorderStrip(Transform panelRoot, string name, BorderEdge edge, float thickness)
        {
            var stripGo = new GameObject(name, typeof(RectTransform));
            stripGo.transform.SetParent(panelRoot, worldPositionStays: false);
            var ignore = stripGo.AddComponent<LayoutElement>();
            ignore.ignoreLayout = true;
            var rt = stripGo.GetComponent<RectTransform>();
            switch (edge)
            {
                case BorderEdge.Top:
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(0.5f, 1f);
                    rt.sizeDelta = new Vector2(0f, thickness);
                    rt.anchoredPosition = Vector2.zero;
                    break;
                case BorderEdge.Bottom:
                    rt.anchorMin = new Vector2(0f, 0f);
                    rt.anchorMax = new Vector2(1f, 0f);
                    rt.pivot = new Vector2(0.5f, 0f);
                    rt.sizeDelta = new Vector2(0f, thickness);
                    rt.anchoredPosition = Vector2.zero;
                    break;
                case BorderEdge.Left:
                    rt.anchorMin = new Vector2(0f, 0f);
                    rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 0.5f);
                    rt.sizeDelta = new Vector2(thickness, 0f);
                    rt.anchoredPosition = Vector2.zero;
                    break;
                case BorderEdge.Right:
                    rt.anchorMin = new Vector2(1f, 0f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(1f, 0.5f);
                    rt.sizeDelta = new Vector2(thickness, 0f);
                    rt.anchoredPosition = Vector2.zero;
                    break;
            }
            var img = stripGo.AddComponent<Image>();
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            img.raycastTarget = false;
            return img;
        }

        static float ResolveBorderThickness(string frameStyleSlug)
        {
            if (string.IsNullOrEmpty(frameStyleSlug)) return 3f;
            switch (frameStyleSlug)
            {
                case "thin": return 2f;
                case "bezel": return 3f;
                case "rail": return 3f;
                case "chassis": return 4f;
                default: return 3f;
            }
        }

        // ── Stage 13.7 fallout — anti-loss guard helper ────────────────────────

        static bool ExistingPrefabHasNonDefaultRect(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return false;
            if (assetPath.IndexOf("/Generated/", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (existing == null) return false;
            var rt = existing.GetComponent<RectTransform>();
            if (rt == null) return false;

            const float eps = 0.001f;
            bool MatchesPlaceholder600x800()
            {
                return Mathf.Abs(rt.anchorMin.x - 0.5f) < eps
                    && Mathf.Abs(rt.anchorMin.y - 0.5f) < eps
                    && Mathf.Abs(rt.anchorMax.x - 0.5f) < eps
                    && Mathf.Abs(rt.anchorMax.y - 0.5f) < eps
                    && Mathf.Abs(rt.sizeDelta.x - 600f) < eps
                    && Mathf.Abs(rt.sizeDelta.y - 800f) < eps;
            }
            bool MatchesTopLeftSentinel200x80()
            {
                return Mathf.Abs(rt.anchorMin.x - 0f) < eps
                    && Mathf.Abs(rt.anchorMin.y - 1f) < eps
                    && Mathf.Abs(rt.anchorMax.x - 0f) < eps
                    && Mathf.Abs(rt.anchorMax.y - 1f) < eps
                    && Mathf.Abs(rt.sizeDelta.x - 200f) < eps
                    && Mathf.Abs(rt.sizeDelta.y - 80f) < eps;
            }

            return !MatchesPlaceholder600x800() && !MatchesTopLeftSentinel200x80();
        }
    }
}
