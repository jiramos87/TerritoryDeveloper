using System;
using Territory.UI;
using Territory.UI.Editor;
using Territory.UI.Themed;
using Territory.UI.Themed.Renderers;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Territory.Editor.Bridge
{
    // Overflow implementation extracted from UiBakeHandler.Button.cs by Stage 6.0 Tier-D.
    // Thin shell stays in UiBakeHandler.Button.cs (≤200 LOC); all logic lives here.
    public static partial class UiBakeHandler
    {
        // ── Stage 8 Themed* modal primitive bake (T8.2) ─────────────────────────

        static BakeError BakeStage8ThemedPrimitive(IrInteractive irRow, string assetPath, UiTheme theme)
        {
            GameObject go = null;
            try
            {
                go = new GameObject(irRow.slug);
                go.AddComponent<RectTransform>();

                switch (irRow.kind)
                {
                    case "themed-button":
                    {
                        var btn = go.AddComponent<ThemedButton>();
                        WireThemeRef(btn, theme);
                        var btnImg = go.AddComponent<Image>();
                        btnImg.color = Color.white;
                        btnImg.raycastTarget = true;
                        var btnSo = new SerializedObject(btn);
                        var btnImgProp = btnSo.FindProperty("_buttonImage");
                        if (btnImgProp != null) btnImgProp.objectReferenceValue = btnImg;
                        var btnPalette = btnSo.FindProperty("_paletteSlug");
                        if (btnPalette != null) btnPalette.stringValue = "chassis-graphite";
                        var btnFrame = btnSo.FindProperty("_frameStyleSlug");
                        if (btnFrame != null) btnFrame.stringValue = "thin";
                        btnSo.ApplyModifiedPropertiesWithoutUndo();
                        break;
                    }
                    case "themed-label":
                    {
                        var lbl = go.AddComponent<ThemedLabel>();
                        WireThemeRef(lbl, theme);
                        SpawnThemedLabelChild(go, out var labelTmp);
                        var lblSo = new SerializedObject(lbl);
                        var tmpProp = lblSo.FindProperty("_tmpText");
                        if (tmpProp != null) tmpProp.objectReferenceValue = labelTmp;
                        var lblPalette = lblSo.FindProperty("_paletteSlug");
                        if (lblPalette != null) lblPalette.stringValue = "silkscreen";
                        lblSo.ApplyModifiedPropertiesWithoutUndo();
                        break;
                    }
                    case "themed-slider":
                    {
                        var slider = go.AddComponent<ThemedSlider>();
                        WireThemeRef(slider, theme);
                        var sliderSo = new SerializedObject(slider);
                        var sliderPalette = sliderSo.FindProperty("_paletteSlug");
                        if (sliderPalette != null) sliderPalette.stringValue = "chassis-graphite";
                        sliderSo.ApplyModifiedPropertiesWithoutUndo();
                        var rend = go.AddComponent<ThemedSliderRenderer>();
                        WireThemeRef(rend, theme);
                        SpawnThemedSliderChildren(go, out var trackImg, out var fillImg, out var thumbImg, out var valueText);
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
                        var tg = go.AddComponent<ThemedToggle>();
                        WireThemeRef(tg, theme);
                        var tgSo = new SerializedObject(tg);
                        var tgPalette = tgSo.FindProperty("_paletteSlug");
                        if (tgPalette != null) tgPalette.stringValue = "chassis-graphite";
                        tgSo.ApplyModifiedPropertiesWithoutUndo();
                        var rend = go.AddComponent<ThemedToggleRenderer>();
                        WireThemeRef(rend, theme);
                        SpawnThemedToggleChildren(go, out var checkmarkImg, out var labelTmp);
                        var so = new SerializedObject(rend);
                        var checkProp = so.FindProperty("_checkmarkImage");
                        if (checkProp != null) checkProp.objectReferenceValue = checkmarkImg;
                        var labelProp = so.FindProperty("_labelText");
                        if (labelProp != null) labelProp.objectReferenceValue = labelTmp;
                        var rendPalette = so.FindProperty("_paletteSlug");
                        if (rendPalette != null) rendPalette.stringValue = "led-grass";
                        so.ApplyModifiedPropertiesWithoutUndo();
                        break;
                    }
                    case "themed-tab-bar":
                    {
                        var tabBar = go.AddComponent<ThemedTabBar>();
                        WireThemeRef(tabBar, theme);
                        var rend = go.AddComponent<ThemedTabBarRenderer>();
                        WireThemeRef(rend, theme);
                        SpawnThemedTabBarChildren(go, out var stripImg, out var indicatorImg, out var tabLabelTmp);
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
                        var lst = go.AddComponent<ThemedList>();
                        WireThemeRef(lst, theme);
                        var lstSo = new SerializedObject(lst);
                        var lstPalette = lstSo.FindProperty("_paletteSlug");
                        if (lstPalette != null) lstPalette.stringValue = "chassis-graphite";
                        lstSo.ApplyModifiedPropertiesWithoutUndo();
                        break;
                    }
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

        static void SpawnThemedSliderChildren(
            GameObject prefabRoot,
            out Image trackImage,
            out Image fillImage,
            out Image thumbImage,
            out TMP_Text valueText)
        {
            trackImage = null; fillImage = null; thumbImage = null; valueText = null;
            if (prefabRoot == null) return;

            var trackGo = prefabRoot.transform.Find("Track")?.gameObject;
            if (trackGo == null)
            {
                trackGo = new GameObject("Track", typeof(RectTransform));
                trackGo.transform.SetParent(prefabRoot.transform, worldPositionStays: false);
                var rt = (RectTransform)trackGo.transform;
                rt.anchorMin = new Vector2(0f, 0.25f);
                rt.anchorMax = new Vector2(1f, 0.75f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                trackImage = trackGo.AddComponent<Image>();
                trackImage.raycastTarget = false;
            }
            else { trackImage = trackGo.GetComponent<Image>(); }

            var fillGo = prefabRoot.transform.Find("Fill")?.gameObject;
            if (fillGo == null)
            {
                fillGo = new GameObject("Fill", typeof(RectTransform));
                fillGo.transform.SetParent(prefabRoot.transform, worldPositionStays: false);
                var rt = (RectTransform)fillGo.transform;
                rt.anchorMin = new Vector2(0f, 0.25f);
                rt.anchorMax = new Vector2(0.5f, 0.75f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                fillImage = fillGo.AddComponent<Image>();
                fillImage.raycastTarget = false;
            }
            else { fillImage = fillGo.GetComponent<Image>(); }

            var thumbGo = prefabRoot.transform.Find("Thumb")?.gameObject;
            if (thumbGo == null)
            {
                thumbGo = new GameObject("Thumb", typeof(RectTransform));
                thumbGo.transform.SetParent(prefabRoot.transform, worldPositionStays: false);
                var rt = (RectTransform)thumbGo.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(20f, 20f);
                rt.anchoredPosition = Vector2.zero;
                thumbImage = thumbGo.AddComponent<Image>();
                thumbImage.raycastTarget = true;
            }
            else { thumbImage = thumbGo.GetComponent<Image>(); }

            var textGo = prefabRoot.transform.Find("ValueText")?.gameObject;
            if (textGo == null)
            {
                textGo = new GameObject("ValueText", typeof(RectTransform));
                textGo.transform.SetParent(prefabRoot.transform, worldPositionStays: false);
                var rt = (RectTransform)textGo.transform;
                rt.anchorMin = new Vector2(0.8f, 0f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                var tmp = textGo.AddComponent<TextMeshProUGUI>();
                tmp.text = "0";
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 12f;
                tmp.raycastTarget = false;
                valueText = tmp;
            }
            else { valueText = textGo.GetComponent<TMP_Text>(); }
        }

        static void SpawnThemedToggleChildren(
            GameObject prefabRoot,
            out Image checkmarkImage,
            out TMP_Text labelText)
        {
            checkmarkImage = null; labelText = null;
            if (prefabRoot == null) return;

            var checkGo = prefabRoot.transform.Find("Checkmark")?.gameObject;
            if (checkGo == null)
            {
                checkGo = new GameObject("Checkmark", typeof(RectTransform));
                checkGo.transform.SetParent(prefabRoot.transform, worldPositionStays: false);
                var rt = (RectTransform)checkGo.transform;
                rt.anchorMin = new Vector2(0f, 0.1f);
                rt.anchorMax = new Vector2(0.2f, 0.9f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                checkmarkImage = checkGo.AddComponent<Image>();
                checkmarkImage.raycastTarget = false;
            }
            else { checkmarkImage = checkGo.GetComponent<Image>(); }

            var labelGo = prefabRoot.transform.Find("Label")?.gameObject;
            if (labelGo == null)
            {
                labelGo = new GameObject("Label", typeof(RectTransform));
                labelGo.transform.SetParent(prefabRoot.transform, worldPositionStays: false);
                var rt = (RectTransform)labelGo.transform;
                rt.anchorMin = new Vector2(0.25f, 0f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                var tmp = labelGo.AddComponent<TextMeshProUGUI>();
                tmp.text = string.Empty;
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.fontSize = 14f;
                tmp.raycastTarget = false;
                labelText = tmp;
            }
            else { labelText = labelGo.GetComponent<TMP_Text>(); }
        }

        static void SpawnThemedButtonCaption(GameObject buttonRoot, string captionText)
        {
            if (buttonRoot == null) return;
            var existing = buttonRoot.transform.Find("Caption")?.gameObject;
            TMP_Text tmp;
            if (existing == null)
            {
                var go = new GameObject("Caption", typeof(RectTransform));
                go.transform.SetParent(buttonRoot.transform, worldPositionStays: false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 18f;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = Color.white;
                tmp.raycastTarget = false;
            }
            else
            {
                tmp = existing.GetComponent<TMP_Text>();
            }
            if (tmp != null) tmp.text = captionText ?? string.Empty;
        }

        static void EnsureChildLayoutElement(GameObject child, float preferredWidth, float preferredHeight, float flexibleWidth = 0f)
        {
            if (child == null) return;
            var le = child.GetComponent<LayoutElement>();
            if (le == null) le = child.AddComponent<LayoutElement>();
            le.preferredWidth = preferredWidth;
            le.preferredHeight = preferredHeight;
            le.flexibleWidth = flexibleWidth;
        }

        static void SpawnThemedLabelChild(GameObject prefabRoot, out TMP_Text tmp)
        {
            tmp = null;
            if (prefabRoot == null) return;

            var labelGo = prefabRoot.transform.Find("Label")?.gameObject;
            if (labelGo == null)
            {
                labelGo = new GameObject("Label", typeof(RectTransform));
                labelGo.transform.SetParent(prefabRoot.transform, worldPositionStays: false);
                var rt = (RectTransform)labelGo.transform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                var t = labelGo.AddComponent<TextMeshProUGUI>();
                t.text = string.Empty;
                t.alignment = TextAlignmentOptions.Center;
                t.fontSize = 14f;
                t.enableAutoSizing = true;
                t.fontSizeMin = 8f;
                t.fontSizeMax = 18f;
                t.color = Color.white;
                t.raycastTarget = false;
                tmp = t;
            }
            else { tmp = labelGo.GetComponent<TMP_Text>(); }
        }

        static void SpawnThemedTabBarChildren(
            GameObject prefabRoot,
            out Image tabStripImage,
            out Image activeTabIndicator,
            out TMP_Text tabLabel)
        {
            tabStripImage = null; activeTabIndicator = null; tabLabel = null;
            if (prefabRoot == null) return;

            var stripGo = prefabRoot.transform.Find("TabStrip")?.gameObject;
            if (stripGo == null)
            {
                stripGo = new GameObject("TabStrip", typeof(RectTransform));
                stripGo.transform.SetParent(prefabRoot.transform, worldPositionStays: false);
                stripGo.transform.SetSiblingIndex(0);
                var rt = (RectTransform)stripGo.transform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                tabStripImage = stripGo.AddComponent<Image>();
                tabStripImage.raycastTarget = false;
            }
            else { tabStripImage = stripGo.GetComponent<Image>(); }

            var indicatorGo = prefabRoot.transform.Find("ActiveTabIndicator")?.gameObject;
            if (indicatorGo == null)
            {
                indicatorGo = new GameObject("ActiveTabIndicator", typeof(RectTransform));
                indicatorGo.transform.SetParent(prefabRoot.transform, worldPositionStays: false);
                var rt = (RectTransform)indicatorGo.transform;
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0.25f, 0.1f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                activeTabIndicator = indicatorGo.AddComponent<Image>();
                activeTabIndicator.raycastTarget = false;
            }
            else { activeTabIndicator = indicatorGo.GetComponent<Image>(); }

            var labelGo = prefabRoot.transform.Find("TabLabel")?.gameObject;
            if (labelGo == null)
            {
                labelGo = new GameObject("TabLabel", typeof(RectTransform));
                labelGo.transform.SetParent(prefabRoot.transform, worldPositionStays: false);
                var rt = (RectTransform)labelGo.transform;
                rt.anchorMin = new Vector2(0f, 0.1f);
                rt.anchorMax = new Vector2(0.25f, 1f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                var tmp = labelGo.AddComponent<TextMeshProUGUI>();
                tmp.text = string.Empty;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 14f;
                tmp.raycastTarget = false;
                tabLabel = tmp;
            }
            else { tabLabel = labelGo.GetComponent<TMP_Text>(); }
        }

        static void SpawnThemedTooltipChildren(
            GameObject prefabRoot,
            out Image backgroundImage,
            out Image arrowImage,
            out TMP_Text bodyText)
        {
            backgroundImage = null; arrowImage = null; bodyText = null;
            if (prefabRoot == null) return;

            var bgGo = prefabRoot.transform.Find("Background")?.gameObject;
            if (bgGo == null)
            {
                bgGo = new GameObject("Background", typeof(RectTransform));
                bgGo.transform.SetParent(prefabRoot.transform, worldPositionStays: false);
                bgGo.transform.SetSiblingIndex(0);
                var rt = (RectTransform)bgGo.transform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                backgroundImage = bgGo.AddComponent<Image>();
                backgroundImage.raycastTarget = false;
            }
            else { backgroundImage = bgGo.GetComponent<Image>(); }

            var arrowGo = prefabRoot.transform.Find("Arrow")?.gameObject;
            if (arrowGo == null)
            {
                arrowGo = new GameObject("Arrow", typeof(RectTransform));
                arrowGo.transform.SetParent(prefabRoot.transform, worldPositionStays: false);
                var rt = (RectTransform)arrowGo.transform;
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(12f, 8f);
                arrowImage = arrowGo.AddComponent<Image>();
                arrowImage.raycastTarget = false;
            }
            else { arrowImage = arrowGo.GetComponent<Image>(); }

            var bodyGo = prefabRoot.transform.Find("Body")?.gameObject;
            if (bodyGo == null)
            {
                bodyGo = new GameObject("Body", typeof(RectTransform));
                bodyGo.transform.SetParent(prefabRoot.transform, worldPositionStays: false);
                var rt = (RectTransform)bodyGo.transform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(8f, 6f);
                rt.offsetMax = new Vector2(-8f, -6f);
                var tmp = bodyGo.AddComponent<TextMeshProUGUI>();
                tmp.text = string.Empty;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 14f;
                tmp.raycastTarget = false;
                bodyText = tmp;
            }
            else { bodyText = bodyGo.GetComponent<TMP_Text>(); }
        }

        // ── Stage 1.4 T1.4.4 — button state wiring ──────────────────────────────

        static void ApplyButtonStates(IrButtonStateDetail detail, Selectable sel)
        {
            if (detail == null || sel == null) return;

            bool hasSpriteSlots = detail.atlas_slot_enum != null
                && !string.IsNullOrEmpty(detail.atlas_slot_enum.highlighted);
            bool hasPaletteRamp = detail.palette_ramp != null
                && !string.IsNullOrEmpty(detail.palette_ramp.normal);

            if (hasSpriteSlots)
            {
                sel.transition = Selectable.Transition.SpriteSwap;
                var ss = sel.spriteState;
                var highlightSprite = AtlasIndex.Resolve(detail.atlas_slot_enum.highlighted);
                if (highlightSprite != null) ss.highlightedSprite = highlightSprite;
                if (!string.IsNullOrEmpty(detail.atlas_slot_enum.pressed))
                {
                    var pressedSprite = AtlasIndex.Resolve(detail.atlas_slot_enum.pressed);
                    if (pressedSprite != null) ss.pressedSprite = pressedSprite;
                }
                if (!string.IsNullOrEmpty(detail.atlas_slot_enum.normal))
                {
                    var normalSprite = AtlasIndex.Resolve(detail.atlas_slot_enum.normal);
                    if (normalSprite != null) ss.selectedSprite = normalSprite;
                }
                sel.spriteState = ss;
            }
            else if (hasPaletteRamp)
            {
                sel.transition = Selectable.Transition.ColorTint;
            }

            if (hasPaletteRamp)
            {
                var cb = sel.colors;
                const float DefaultColorMultiplier = 1f;
                float fadeDuration = detail.motion_curve != null ? detail.motion_curve.fadeDuration : 0.1f;
                cb.fadeDuration = fadeDuration;
                cb.colorMultiplier = DefaultColorMultiplier;
                if (ColorUtility.TryParseHtmlString(detail.palette_ramp.normal, out var normalColor))
                    cb.normalColor = normalColor;
                if (!string.IsNullOrEmpty(detail.palette_ramp.highlighted)
                    && ColorUtility.TryParseHtmlString(detail.palette_ramp.highlighted, out var highlightColor))
                    cb.highlightedColor = highlightColor;
                if (!string.IsNullOrEmpty(detail.palette_ramp.pressed)
                    && ColorUtility.TryParseHtmlString(detail.palette_ramp.pressed, out var pressedColor))
                    cb.pressedColor = pressedColor;
                if (!string.IsNullOrEmpty(detail.palette_ramp.disabled)
                    && ColorUtility.TryParseHtmlString(detail.palette_ramp.disabled, out var disabledColor))
                    cb.disabledColor = disabledColor;
                sel.colors = cb;
            }
            else if (detail.motion_curve != null)
            {
                var cb = sel.colors;
                cb.fadeDuration = detail.motion_curve.fadeDuration;
                sel.colors = cb;
            }
        }
    }
}
