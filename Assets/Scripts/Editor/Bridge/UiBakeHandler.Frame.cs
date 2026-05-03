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
    public static partial class UiBakeHandler
    {
        // ── Panel prefab bake (Stage 3 T3.5) ────────────────────────────────────

        /// <summary>
        /// Write a panel prefab with attached <see cref="ThemedPanel"/> + populated <c>_slots</c> +
        /// <c>_children[]</c>. SlotSpec order = IR slot order; children order = IR slot iteration order.
        /// Stage 7 T7.0 — children are instantiated as scene-instance <c>GameObject</c>s under the panel
        /// root (state-holder + renderer pair + render-target descendants per Stage 10 T10.3 convention).
        /// Asset-GUID resolution (loading <c>{dir}/{childSlug}.prefab</c>) is no longer used: adapter
        /// Inspector slots must bind against real children, not asset stand-ins. <paramref name="dir"/>
        /// stays in the signature for future bake-time variant resolution.
        /// SerializedObject path enforces deterministic property write for re-bake idempotency.
        /// </summary>
        static BakeError SavePanelPrefab(IrPanel panel, string assetPath, string dir, UiTheme theme)
        {
            // dir kept for future variant resolution; not consumed in T7.0 path.
            _ = dir;

            GameObject go = null;
            try
            {
                go = new GameObject(panel.slug);
                var rootRect = go.AddComponent<RectTransform>();
                // Step 11.3 — bake-time RectTransform values are placeholder; runtime ApplyKindLayout
                // overrides anchor/sizeDelta/pivot per _kind on every OnEnable (defeats scene PrefabInstance
                // override pin-down). Slug-suffix heuristic + unconditional VLG attach removed.
                rootRect.anchorMin = new Vector2(0.5f, 0.5f);
                rootRect.anchorMax = new Vector2(0.5f, 0.5f);
                rootRect.pivot = new Vector2(0.5f, 0.5f);
                rootRect.anchoredPosition = Vector2.zero;
                rootRect.sizeDelta = new Vector2(600f, 800f);
                var bgImage = go.AddComponent<Image>();
                bgImage.color = Color.white;
                // Stage 13.2 — chrome raycast policy: only modal kinds block input behind the panel.
                // Hud / Toolbar / SideRail / Screen are non-modal overlays — chrome stays input-transparent
                // so legacy DebugPanel (and any background world clicks) keep receiving pointer events.
                bgImage.raycastTarget = string.Equals(panel.kind, "modal", StringComparison.Ordinal);
                var themedPanel = go.AddComponent<ThemedPanel>();
                WireThemeRef(themedPanel, theme);

                // Populate _kind + _slots + _children[] via SerializedObject for deterministic order.
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
                // Panel-scoped per-kind counter — prevents Unity sibling-name auto-suffix when
                // multiple slots declare the same child kind (e.g. tool-grid + subtype-row both
                // emit illuminated-button). Keys are the StudioControl kind slug; values are the
                // running 0-based count for that kind across all slots in this panel.
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

                    // Stage 7 T7.0 — instantiate scene-instance child GameObjects under the panel root
                    // for each declared slot child (state-holder + renderer pair + render-target
                    // descendants). Asset-prefab loading retired: adapter Inspector slots must bind
                    // against real children, not asset GUID stand-ins.
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
                        // Panel-scoped per-kind counter — read-modify-write so multi-slot panels
                        // (tool-grid + subtype-row) produce contiguous names (illuminated-button,
                        // illuminated-button (1), ..., illuminated-button (17)) instead of
                        // colliding with sibling auto-suffix.
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

                // Stage 5 T5.5 — synthetic interactive row enables ShadowDepth opt-in via IR juice[]
                // entries on the panel level (current MVP: defaults gated off; override-only path).
                var panelRow = new IrInteractive
                {
                    slug = panel.slug,
                    kind = "themed-panel",
                    juice = null,
                };
                AttachJuiceComponents(go, panelRow);

                // Step 13 — pause-menu adapter wiring. Bake-time: attach PauseMenuDataAdapter
                // and serialize the six ThemedButton refs in IR-canonical order
                // [Resume, Save, Load, Settings, MainMenu, Quit] so OnEnable subscriptions fire
                // without manual Inspector wiring per scene placement.
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
                    aSo.ApplyModifiedPropertiesWithoutUndo();
                }

                // Step 16 D2.1 — info-panel adapter wiring. Bake-time: attach InfoPanelAdapter
                // and serialize the ordered StudioControlBase refs (slot order, child order within slot)
                // captured during BakeSlots above. Stage 13 cell-data wiring will subscribe via this array
                // instead of runtime GetComponentsInChildren scans (invariant #3).
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

                // Stage 13.2 (TECH-9854) — IR v2 row hierarchy. Emit one fresh GameObject per
                // `panel.rows[]` entry under the panel root. Caption + Value primitive leaves
                // (TMP_Text) populated from row.label / row.value; runtime adapters (Stage 13.4)
                // bind data sources against these leaves. Optional VU/Delta/IconRef leaves
                // deferred until row schema carries those fields. Invariant #6 honored: every
                // row container is a fresh GameObject — no AddComponent on existing nodes.
                EmitRowChildren(panel, go.transform, theme);

                // Stage 13.2 (TECH-9854) — wire ThemedTabBar.pages[] from `panel.tabs[]` when both
                // a tab bar slot child and tab descriptors are present. Tabless panels skip wholly.
                if (panel.tabs != null && panel.tabs.Length > 0)
                {
                    WireTabBarPages(panel, childrenList);
                }

                // Stage 1.4 T1.4.2 — archetype dispatch: instantiate section_header / divider / badge child.
                BakePanelArchetype(panel, go, theme);

                // Stage 1.4 T1.4.1 — apply spacing overrides from panel.detail to LayoutGroup + divider.
                ApplySpacing(panel, go);

                // Stage 1.4 T1.4.3 — frame sprite resolution via AtlasIndex; fallback slug on miss.
                // Step 16.2 — additionally bake procedural 4-strip border (top/bottom/left/right) so
                // panel chrome renders even when no PNG asset is present under Assets/UI/Sprites/Frames/.
                Image borderTop = null, borderBottom = null, borderLeft = null, borderRight = null;
                {
                    const string FallbackFrameSlug = "ui/frame/default";
                    string frameSlug = !string.IsNullOrEmpty(panel.frame_style_slug)
                        ? panel.frame_style_slug
                        : FallbackFrameSlug;
                    var frameSprite = AtlasIndex.Resolve(frameSlug);
                    if (frameSprite == null && panel.frame_style_slug != null)
                    {
                        // AtlasIndex.Resolve already logged a warning; try fallback.
                        frameSprite = AtlasIndex.Resolve(FallbackFrameSlug);
                    }
                    if (frameSprite != null) bgImage.sprite = frameSprite;

                    // Procedural border — thickness from frame_style edge keyword; default 2px.
                    float thickness = ResolveBorderThickness(panel.frame_style_slug);
                    borderTop = SpawnBorderStrip(go.transform, "BorderTop", BorderEdge.Top, thickness);
                    borderBottom = SpawnBorderStrip(go.transform, "BorderBottom", BorderEdge.Bottom, thickness);
                    borderLeft = SpawnBorderStrip(go.transform, "BorderLeft", BorderEdge.Left, thickness);
                    borderRight = SpawnBorderStrip(go.transform, "BorderRight", BorderEdge.Right, thickness);

                    // Persist border refs onto ThemedPanel so runtime ApplyTheme can tint them.
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

                // Stage 1.4 T1.4.3 — conditional ThemedIlluminationLayer sibling when illumination_slug set.
                if (!string.IsNullOrEmpty(panel.illumination_slug))
                {
                    var illumGo = new GameObject("IlluminationLayer", typeof(RectTransform));
                    illumGo.transform.SetParent(go.transform, worldPositionStays: false);
                    var illumImg = illumGo.AddComponent<Image>();
                    illumImg.raycastTarget = false;
                    illumImg.color = new Color(1f, 1f, 1f, 0f); // transparent until ApplyTheme
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

        /// <summary>
        /// Emit one fresh <c>GameObject</c> per <c>panel.rows[]</c> entry under <paramref name="panelRoot"/>.
        /// Each row container holds a <c>Caption</c> TMP child (label) and a <c>Value</c> TMP child
        /// (formatted value). <c>kind=header</c> rows skip the value leaf. Runtime adapters
        /// (Stage 13.4) re-bind these leaves against live data sources; bake handler only
        /// captures the IR-declared placeholder text so designers can review structure.
        /// </summary>
        static void EmitRowChildren(IrPanel panel, Transform panelRoot, UiTheme theme)
        {
            _ = theme; // Theme wiring deferred — row leaves stay neutral until Stage 13.4 adapter pass.
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
                // Stage 13.2 — row container must size cleanly under panel VLG (SideRail / Modal).
                // LayoutElement supplies preferredHeight; HorizontalLayoutGroup splits caption + value
                // 50/50 with childForceExpandWidth so both leaves render at known size. Without this
                // patch the row + its leaves stayed at the default 0×0 RectTransform → invisible.
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

        /// <summary>
        /// Locate the first <see cref="ThemedTabBar"/> child in <paramref name="panelChildren"/>
        /// and write its <c>_pages[]</c> serialized array from <c>panel.tabs[]</c>. No-op when
        /// no tab bar child exists (panel declared tabs but no `themed-tab-bar` slot child).
        /// </summary>
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
                if (idProp != null) idProp.stringValue = tab?.id ?? string.Empty;
                if (labelProp != null) labelProp.stringValue = tab?.label ?? string.Empty;
                if (activeProp != null) activeProp.boolValue = tab?.active ?? false;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── Stage 7 T7.0 — embedded panel child instantiation ───────────────────

        /// <summary>
        /// Instantiate a fresh scene-instance <c>GameObject</c> under the panel root for the given
        /// StudioControl <paramref name="kind"/>. Attaches the matching state-holder + renderer pair
        /// (per Stage 10 T10.3 convention) and spawns the kind's render-target descendants
        /// (<c>body</c> / <c>halo</c> / <c>text</c> / <c>Label</c> / <c>Icon</c> / <c>Toggle</c>).
        /// Detail-row defaults applied so the embedded child is visually consistent with the
        /// top-level baked prefab counterparts. Adapter Inspector slots bind against the returned
        /// <c>GameObject</c> (or one of its components) — no asset-GUID stand-in.
        /// </summary>
        /// <param name="kind">StudioControl kind slug (must satisfy <see cref="IsKnownStudioControlKind"/>).</param>
        /// <param name="panelRoot">Panel root <see cref="Transform"/>; child reparented with <c>worldPositionStays:false</c>.</param>
        /// <param name="duplicateCounter">Per-panel suffix counter — incremented each call so multiple
        /// children of the same kind get unique <c>GameObject.name</c> values (e.g. <c>illuminated-button</c>,
        /// <c>illuminated-button (1)</c>, ...). Suffix-only — does not affect IR slot resolution.</param>
        /// <returns>Instantiated child <c>GameObject</c>; <c>null</c> on unknown kind.</returns>
        static GameObject InstantiatePanelChild(string kind, Transform panelRoot, ref int duplicateCounter, UiTheme theme, string label = null, string iconSpriteSlug = null)
        {
            if (panelRoot == null || string.IsNullOrEmpty(kind)) return null;

            var name = duplicateCounter == 0 ? kind : $"{kind} ({duplicateCounter})";
            duplicateCounter++;

            var childGo = new GameObject(name, typeof(RectTransform));
            childGo.transform.SetParent(panelRoot, worldPositionStays: false);

            // State-holder + renderer pair — mirrors the per-kind switch in BakeInteractive.
            // Detail rows: panel children carry per-kind defaults at bake time (no IR detail block
            // exists for slot children; future Stage extensions may add one).
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
                    SpawnIlluminatedButtonRenderTargets(childGo, iconSpriteSlug, out var ibBody, out var ibHalo);
                    // Step 16 D3.1+D3.2 — bake-time hover/press wiring (refs onto renderer + Selectable).
                    WireIlluminatedButtonHoverAndPress(childGo, btnRend, ibBody, ibHalo, theme);
                    // Step 16.D — IR-side icon sprite slug + per-button identity injected via parallel-array
                    // slot.iconSpriteSlugs[c]; persists onto detail row so renderer/render reads back.
                    btn.ApplyDetail(new IlluminatedButtonDetail { iconSpriteSlug = iconSpriteSlug });
                    // Step 16.G — caption fallback. When IR provides slot.labels[c] but iconSpriteSlugs[c]
                    // is empty (no human-art available yet), spawn a TMP caption child so the button
                    // still signals its function. Stage 13 follow-up: promote to first-class detail field.
                    if (string.IsNullOrEmpty(iconSpriteSlug) && !string.IsNullOrEmpty(label))
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
                // Stage 8 Themed* modal primitive panel-child cases.
                // Renderer-sibling decision per §Pending Decisions:
                //   themed-button  : UGUI-self-renders (no renderer sibling)
                //   themed-label   : UGUI-self-renders (no renderer sibling)
                //   themed-slider  : renderer-sibling needed (ThemedSliderRenderer)
                //   themed-toggle  : renderer-sibling needed (ThemedToggleRenderer)
                //   themed-tab-bar : renderer-sibling needed (ThemedTabBarRenderer)
                //   themed-list    : UGUI-self-renders (no renderer sibling)
                case "themed-button":
                {
                    var btn = childGo.AddComponent<ThemedButton>();
                    WireThemeRef(btn, theme);
                    var btnImg = childGo.AddComponent<Image>();
                    btnImg.color = Color.white;
                    btnImg.raycastTarget = true;
                    // Step 13 — UnityEngine.UI.Button so ThemedButton.Awake can wire onClick → OnClicked event.
                    // Without this, PauseMenuDataAdapter.OnClicked subscriptions never fire.
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
                    // Step 12 — caption child + LayoutElement so VLG/HLG can size button.
                    SpawnThemedButtonCaption(childGo, label);
                    EnsureChildLayoutElement(childGo, preferredWidth: 280f, preferredHeight: 56f, flexibleWidth: 1f);
                    break;
                }
                case "themed-label":
                {
                    var lbl = childGo.AddComponent<ThemedLabel>();
                    WireThemeRef(lbl, theme);
                    SpawnThemedLabelChild(childGo, out var labelTmp);
                    // Step 16 D2.3 — fall through to placeholder "--" so the slot composes a non-empty
                    // visible chrome even before live cell-data wiring lands in Stage 13.
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
                    // Stage 9 (TECH-8541) — renderer sibling for theme-driven row tinting.
                    var lstRend = childGo.AddComponent<ThemedListRenderer>();
                    WireThemeRef(lstRend, theme);
                    var lstRendSo = new SerializedObject(lstRend);
                    var lstRendPalette = lstRendSo.FindProperty("_paletteSlug");
                    if (lstRendPalette != null) lstRendPalette.stringValue = "led-amber";
                    lstRendSo.ApplyModifiedPropertiesWithoutUndo();
                    break;
                }
                // Stage 9 (game-ui-design-system) — themed-tooltip primitive panel-child.
                // Composition: ThemedTooltip primitive + background Image + body TMP_Text + arrow Image.
                // Renderer-sibling (ThemedTooltipRenderer) wired in TECH-8541; this case stays minimal.
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
                    // Stage 9 (TECH-8541) — renderer sibling consumes arrow + body refs.
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

            // Step 16 D2.2 — write canonical _slug onto every StudioControlBase-derived widget so adapter
            // resolution + conformance walks can key on a stable IR-rooted slug instead of GameObject name.
            // Themed primitives (button/label/slider/toggle/tab-bar/list/tooltip) intentionally skipped —
            // their identity is captured by IR slot.labels + child kind in the conformance label index.
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

        /// <summary>
        /// Spawn a thin Image strip stretched along one panel edge. Tint applied at runtime by
        /// <c>ThemedPanel.ApplyTheme</c>; bake-time color is white so theme-driven colors win.
        /// Step 16.8 — strip carries <see cref="LayoutElement"/> with <c>ignoreLayout=true</c> so
        /// the panel's <see cref="VerticalLayoutGroup"/> / <see cref="HorizontalLayoutGroup"/> does
        /// not rewrite its anchored RectTransform into the flow layout (which would zero its size
        /// + lose the edge anchor + render the strip invisible).
        /// </summary>
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

        /// <summary>Map IR frame_style edge keyword → border strip thickness in px.
        /// Step 16.8 — bumped baseline thicknesses so hairlines stay visible against the panel
        /// fill at standard reference resolution (1920×1080) without HiDPI scale-up.</summary>
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

    }
}
