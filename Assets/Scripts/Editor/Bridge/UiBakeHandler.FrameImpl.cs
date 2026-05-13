using System;
using System.Collections.Generic;
using Domains.UI.Data;
using Territory.UI;
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
    // Panel prefab bake — delegate stub (TECH-31979).
    // Logic migrated to Domains/UI/Editor/UiBake/Services/FrameBaker.cs.
    // This file retains only the forwarding entry + helpers called from elsewhere in Bridge.
    public static partial class UiBakeHandler
    {
        // ── Panel prefab bake (Stage 3 T3.5) ────────────────────────────────────

        static BakeError SavePanelPrefab(IrPanel panel, string assetPath, string dir, UiTheme theme)
        {
            return new Domains.UI.Editor.UiBake.Services.FrameBaker(null).Bake(panel, assetPath, dir, theme);
        }

        // ── Kept: called from UiBakeHandler.cs (SavePanelSnapshotPrefab) ─────────

        // ExistingPrefabHasNonDefaultRect kept below because SavePanelSnapshotPrefab
        // in UiBakeHandler.cs calls it. FrameBaker has its own private copy.

        // ── Stage 13.7 fallout — anti-loss guard helper ────────────────────────

        public static bool ExistingPrefabHasNonDefaultRect(string assetPath)
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

        // ── Stage 7 T7.0 — embedded panel child instantiation ───────────────────
        // Public: called from FrameBaker + external tests.

        public static GameObject InstantiatePanelChild(string kind, Transform panelRoot, ref int duplicateCounter, UiTheme theme, string label = null, string iconSpriteSlug = null)
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
    }
}
