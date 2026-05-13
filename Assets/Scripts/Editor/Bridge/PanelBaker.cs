using System.Collections.Generic;
using Domains.UI.Data;
using Territory.UI;
using Territory.UI.Decoration;
using Territory.UI.Juice;
using Territory.UI.Registry;
using Territory.UI.StudioControls;
using Territory.UI.StudioControls.Renderers;
using Territory.UI.Themed;
using Territory.UI.Themed.Renderers;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Domains.UI.Editor.UiBake.Services
{
    /// <summary>POCO baker for panel snapshot items. Extracted from UiBakeHandler (TECH-31982).
    /// Constructor takes BakeContext; Bake(IrPanel row) is the entry point for IR-panel bake.
    /// BakeChildByKind body migrated here; hub delegates to this class.</summary>
    public class PanelBaker
    {
        readonly BakeContext _ctx;

        // ── Known StudioControl kind slugs (migrated from UiBakeHandler.Archetype.cs) ──

        static readonly HashSet<string> _knownKinds = new HashSet<string>
        {
            "knob", "fader", "detent-ring",
            "vu-meter", "oscilloscope",
            "illuminated-button", "led", "segmented-readout",
            "themed-overlay-toggle-row",
            "themed-button", "themed-label", "themed-slider",
            "themed-toggle", "themed-tab-bar", "themed-list",
            "themed-tooltip",
            "view-slot", "confirm-button",
            "card-picker", "chip-picker", "text-input",
            "toggle-row", "slider-row", "dropdown-row", "section-header",
            "save-controls-strip", "save-list",
            "subtype-picker-strip",
            "tab-strip", "chart", "range-tabs", "stacked-bar-row", "service-row",
            "slider-row-numeric", "expense-row", "readout-block",
            "info-dock", "field-list", "minimap-canvas", "toast-stack", "toast-card",
        };

        /// <summary>True when kind is a known StudioControl archetype slug.</summary>
        public static bool IsKnownStudioControlKind(string kind)
            => !string.IsNullOrEmpty(kind) && _knownKinds.Contains(kind);

        /// <summary>Map IR panel.kind string to PanelKind enum index. Default = Modal (0).</summary>
        public static int ResolvePanelKindIndex(string kind)
        {
            if (string.IsNullOrEmpty(kind)) return (int)PanelKind.Modal;
            switch (kind)
            {
                case "modal":    return (int)PanelKind.Modal;
                case "screen":   return (int)PanelKind.Screen;
                case "hud":      return (int)PanelKind.Hud;
                case "toolbar":  return (int)PanelKind.Toolbar;
                case "side-rail":
                case "side_rail":
                case "sideRail": return (int)PanelKind.SideRail;
                default:
                    Debug.LogWarning($"[UiBakeHandler] panel.kind '{kind}' unknown — defaulting to modal");
                    return (int)PanelKind.Modal;
            }
        }

        public PanelBaker(BakeContext ctx)
        {
            _ctx = ctx;
        }

        /// <summary>Bake an IrPanel row into a prefab root GameObject.
        /// Establishes type boundary — callers bind to PanelBaker.Bake(IrPanel).
        /// Panel bake path goes through BakeOrchestrator → UiBakeHandler.BakeFromPanelSnapshot.</summary>
        public Territory.Editor.Bridge.UiBakeHandler.BakeError Bake(IrPanel row)
        {
            if (row == null)
            {
                return new Territory.Editor.Bridge.UiBakeHandler.BakeError
                {
                    error = "missing_arg",
                    details = "irRow_null",
                    path = "$.panels[?]",
                };
            }
            return null;
        }

        /// <summary>Normalize outer+inner child kind to canonical inner-kind slug.
        /// Delegates to UiBakeHandler.NormalizeChildKind.</summary>
        public static string NormalizeChildKind(string outerKind, string innerKind)
            => Territory.Editor.Bridge.UiBakeHandler.NormalizeChildKind(outerKind, innerKind);

        /// <summary>Dispatch child bake by kind. Full body migrated from UiBakeHandler (TECH-31982).</summary>
        public static void BakeChildByKind(GameObject childGo, string innerKind, PanelChildParamsJson pj,
            UiTheme theme, float preferredWidth = 64f, float preferredHeight = 64f, string panelDisplayName = null)
        {
            if (childGo == null) return;
            string iconSlug = pj != null ? pj.icon : null;
            string label = pj != null ? pj.label : null;

            switch (innerKind)
            {
                case "illuminated-button":
                {
                    var btn = childGo.AddComponent<IlluminatedButton>();
                    Territory.Editor.Bridge.UiBakeHandler.WireThemeRef(btn, theme);
                    var btnRend = childGo.GetComponent<IlluminatedButtonRenderer>();
                    if (btnRend == null) btnRend = childGo.AddComponent<IlluminatedButtonRenderer>();
                    Territory.Editor.Bridge.UiBakeHandler.WireThemeRef(btnRend, theme);
                    bool iconResolved = Territory.Editor.Bridge.UiBakeHandler.SpawnIlluminatedButtonRenderTargets(childGo, iconSlug, out var bodyImg, out var haloImg);
                    Territory.Editor.Bridge.UiBakeHandler.WireIlluminatedButtonHoverAndPress(childGo, btnRend, bodyImg, haloImg, theme);
                    btn.ApplyDetail(new IlluminatedButtonDetail { iconSpriteSlug = iconSlug });
                    Territory.Editor.Bridge.UiBakeHandler.AttachUiActionTrigger(childGo, !string.IsNullOrEmpty(pj?.action) ? pj.action : pj?.actionId);
                    bool isPlaceholder = string.IsNullOrEmpty(iconSlug) || iconSlug == "empty";
                    if ((!iconResolved || isPlaceholder) && !string.IsNullOrEmpty(label))
                    {
                        Territory.Editor.Bridge.UiBakeHandler.SpawnIlluminatedButtonCaption(childGo, label);
                    }
                    string bindForLabel = !string.IsNullOrEmpty(pj?.bind) ? pj.bind : pj?.bindId;
                    if (!string.IsNullOrEmpty(bindForLabel))
                    {
                        var bindLabelGo = new GameObject("BindLabel", typeof(RectTransform));
                        bindLabelGo.transform.SetParent(childGo.transform, worldPositionStays: false);
                        var bindLabelRt = (RectTransform)bindLabelGo.transform;
                        bindLabelRt.anchorMin = Vector2.zero;
                        bindLabelRt.anchorMax = Vector2.one;
                        bindLabelRt.pivot = new Vector2(0.5f, 0.5f);
                        bindLabelRt.offsetMin = Vector2.zero;
                        bindLabelRt.offsetMax = Vector2.zero;
                        var bindTmp = bindLabelGo.AddComponent<TextMeshProUGUI>();
                        bindTmp.fontSize = Territory.Editor.Bridge.UiBakeHandler.ResolveTypeScaleFontSize("size-text-modal-title", 24f);
                        bindTmp.fontStyle = FontStyles.Bold;
                        bindTmp.alignment = TextAlignmentOptions.Center;
                        bindTmp.color = Color.white;
                        bindTmp.raycastTarget = false;
                        bindTmp.text = string.Empty;
                        var bindLabelLe = bindLabelGo.AddComponent<LayoutElement>();
                        bindLabelLe.ignoreLayout = true;
                        var bindRenderer = bindLabelGo.AddComponent<Territory.UI.Renderers.BindTextRenderer>();
                        var bindSo = new SerializedObject(bindRenderer);
                        var bindIdProp = bindSo.FindProperty("_bindId");
                        if (bindIdProp != null) bindIdProp.stringValue = bindForLabel;
                        var formatProp = bindSo.FindProperty("_format");
                        if (formatProp != null) formatProp.stringValue = pj?.format ?? string.Empty;
                        var targetProp = bindSo.FindProperty("_target");
                        if (targetProp != null) targetProp.objectReferenceValue = bindTmp;
                        bindSo.ApplyModifiedPropertiesWithoutUndo();
                    }
                    Territory.Editor.Bridge.UiBakeHandler.EnsureChildLayoutElement(childGo, preferredWidth: preferredWidth, preferredHeight: preferredHeight, flexibleWidth: 0f);
                    break;
                }
                case "segmented-readout":
                {
                    var sr = childGo.AddComponent<SegmentedReadout>();
                    Territory.Editor.Bridge.UiBakeHandler.WireThemeRef(sr, theme);
                    var srRend = childGo.GetComponent<SegmentedReadoutRenderer>();
                    if (srRend == null) srRend = childGo.AddComponent<SegmentedReadoutRenderer>();
                    Territory.Editor.Bridge.UiBakeHandler.WireThemeRef(srRend, theme);
                    var sd = new SegmentedReadoutDetail { digits = 1 };
                    Territory.Editor.Bridge.UiBakeHandler.SpawnSegmentedReadoutRenderTargets(childGo, sd);
                    sr.ApplyDetail(sd);
                    Territory.Editor.Bridge.UiBakeHandler.EnsureChildLayoutElement(childGo, preferredWidth: 120f, preferredHeight: 32f, flexibleWidth: 1f);
                    break;
                }
                case "themed-label":
                {
                    var lbl = childGo.AddComponent<ThemedLabel>();
                    Territory.Editor.Bridge.UiBakeHandler.WireThemeRef(lbl, theme);
                    Territory.Editor.Bridge.UiBakeHandler.SpawnThemedLabelChild(childGo, out var labelTmp);
                    string staticText = pj?.text_static;
                    bool isModalTitle = string.Equals(pj?.variant, "modal-title", System.StringComparison.Ordinal);
                    string resolvedText;
                    if (!string.IsNullOrEmpty(staticText)) resolvedText = staticText;
                    else if (!string.IsNullOrEmpty(label)) resolvedText = label;
                    else if (isModalTitle && !string.IsNullOrEmpty(panelDisplayName)) resolvedText = panelDisplayName;
                    else resolvedText = "--";
                    if (labelTmp != null)
                    {
                        labelTmp.text = resolvedText;
                        bool variantSized = false;
                        if (!string.IsNullOrEmpty(pj?.variant))
                        {
                            float vSize = pj.variant switch
                            {
                                "modal-title"    => Territory.Editor.Bridge.UiBakeHandler.ResolveTypeScaleFontSize("size-text-modal-title", 24f),
                                "section-header" => Territory.Editor.Bridge.UiBakeHandler.ResolveTypeScaleFontSize("size-text-section-header", 20f),
                                "body-row"       => Territory.Editor.Bridge.UiBakeHandler.ResolveTypeScaleFontSize("size-text-body-row", 18f),
                                "value"          => Territory.Editor.Bridge.UiBakeHandler.ResolveTypeScaleFontSize("size-text-value", 18f),
                                _                => -1f,
                            };
                            if (vSize > 0f)
                            {
                                labelTmp.enableAutoSizing = false;
                                labelTmp.fontSize = vSize;
                                if (pj.variant == "modal-title" || pj.variant == "section-header" || pj.variant == "value")
                                    labelTmp.fontStyle |= TMPro.FontStyles.Bold;
                                variantSized = true;
                            }
                        }
                        if (!string.IsNullOrEmpty(pj?.size_token))
                        {
                            labelTmp.enableAutoSizing = false;
                            float resolvedSize = pj.size_token switch
                            {
                                "size.text.title-display" => 64f,
                                "size.text.title"         => 32f,
                                "size.text.body"          => 16f,
                                "size.text.caption"       => 12f,
                                _ => Territory.Editor.Bridge.UiBakeHandler.ResolveTypeScaleFontSize(pj.size_token, labelTmp.fontSize),
                            };
                            labelTmp.fontSize = resolvedSize;
                            string weight = Territory.Editor.Bridge.UiBakeHandler.ResolveTypeScaleWeight(pj.size_token, null);
                            if (string.Equals(weight, "bold", System.StringComparison.Ordinal))
                                labelTmp.fontStyle |= TMPro.FontStyles.Bold;
                            variantSized = true;
                        }
                        if (!variantSized && labelTmp.enableAutoSizing)
                        {
                            labelTmp.fontSizeMin = Mathf.Max(labelTmp.fontSizeMin, 12f);
                        }
                        if (string.Equals(pj?.color_token, "color.text.muted", System.StringComparison.Ordinal))
                        {
                            labelTmp.color = new Color(0.62f, 0.62f, 0.62f, 1f);
                        }
                        if (string.Equals(pj?.align, "center", System.StringComparison.Ordinal))
                            labelTmp.alignment = TextAlignmentOptions.Center;
                        else if (string.Equals(pj?.align, "right", System.StringComparison.Ordinal))
                            labelTmp.alignment = TextAlignmentOptions.Right;
                        else if (string.Equals(pj?.align, "left", System.StringComparison.Ordinal))
                            labelTmp.alignment = TextAlignmentOptions.Left;
                    }
                    var lblSo = new SerializedObject(lbl);
                    var tmpProp = lblSo.FindProperty("_tmpText");
                    if (tmpProp != null) tmpProp.objectReferenceValue = labelTmp;
                    var lblPalette = lblSo.FindProperty("_paletteSlug");
                    if (lblPalette != null) lblPalette.stringValue = "silkscreen";
                    lblSo.ApplyModifiedPropertiesWithoutUndo();
                    Territory.Editor.Bridge.UiBakeHandler.EnsureChildLayoutElement(childGo, preferredWidth: -1f, preferredHeight: 32f, flexibleWidth: 1f);
                    break;
                }
                case "confirm-button":
                {
                    var btn = childGo.AddComponent<IlluminatedButton>();
                    Territory.Editor.Bridge.UiBakeHandler.WireThemeRef(btn, theme);
                    var btnRend = childGo.GetComponent<IlluminatedButtonRenderer>();
                    if (btnRend == null) btnRend = childGo.AddComponent<IlluminatedButtonRenderer>();
                    Territory.Editor.Bridge.UiBakeHandler.WireThemeRef(btnRend, theme);
                    bool iconResolved = Territory.Editor.Bridge.UiBakeHandler.SpawnIlluminatedButtonRenderTargets(childGo, iconSlug, out var bodyImg, out var haloImg);
                    Territory.Editor.Bridge.UiBakeHandler.WireIlluminatedButtonHoverAndPress(childGo, btnRend, bodyImg, haloImg, theme);
                    btn.ApplyDetail(new IlluminatedButtonDetail { iconSpriteSlug = iconSlug });
                    Territory.Editor.Bridge.UiBakeHandler.AttachUiActionTrigger(childGo, !string.IsNullOrEmpty(pj?.action) ? pj.action : pj?.actionId);
                    bool isPlaceholder = string.IsNullOrEmpty(iconSlug) || iconSlug == "empty";
                    if ((!iconResolved || isPlaceholder) && !string.IsNullOrEmpty(label))
                    {
                        Territory.Editor.Bridge.UiBakeHandler.SpawnIlluminatedButtonCaption(childGo, label);
                    }
                    Territory.Editor.Bridge.UiBakeHandler.EnsureChildLayoutElement(childGo, preferredWidth: preferredWidth, preferredHeight: preferredHeight, flexibleWidth: 0f);
                    break;
                }
                case "view-slot":
                {
                    var rect = childGo.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        rect.anchorMin = new Vector2(0f, 0f);
                        rect.anchorMax = new Vector2(1f, 1f);
                        rect.offsetMin = rect.offsetMax = Vector2.zero;
                    }
                    Territory.Editor.Bridge.UiBakeHandler.EnsureChildLayoutElement(childGo, preferredWidth: -1f, preferredHeight: -1f, flexibleWidth: 1f);
                    break;
                }
                case "back-button":
                {
                    var spawned = NavBackButton.Spawn(childGo);
                    var labelTr = spawned.transform.Find("Label");
                    if (labelTr != null) labelTr.SetParent(childGo.transform, worldPositionStays: false);
                    var chipImg = childGo.GetComponent<Image>();
                    if (chipImg == null) chipImg = childGo.AddComponent<Image>();
                    chipImg.color = new Color(0.18f, 0.18f, 0.18f, 0.9f);
                    var chipBtn = childGo.GetComponent<Button>();
                    if (chipBtn == null) chipBtn = childGo.AddComponent<Button>();
                    chipBtn.targetGraphic = chipImg;
                    UnityEngine.Object.DestroyImmediate(spawned);
                    Territory.Editor.Bridge.UiBakeHandler.AttachUiActionTrigger(childGo, !string.IsNullOrEmpty(pj?.action) ? pj.action : pj?.actionId);
                    float size = pj != null && pj.corner_size > 0 ? pj.corner_size : NavBackButton.DefaultSize;
                    Territory.Editor.Bridge.UiBakeHandler.EnsureChildLayoutElement(childGo, preferredWidth: size, preferredHeight: size, flexibleWidth: 0f);
                    break;
                }
                case "slider-row":
                {
                    var rowHlg = childGo.AddComponent<HorizontalLayoutGroup>();
                    rowHlg.spacing = 8f;
                    rowHlg.padding = new RectOffset(0, 0, 0, 0);
                    rowHlg.childAlignment = TextAnchor.MiddleLeft;
                    rowHlg.childForceExpandHeight = false;
                    rowHlg.childForceExpandWidth = false;
                    rowHlg.childControlHeight = true;
                    rowHlg.childControlWidth = true;
                    var sliderLabel = new GameObject("Label", typeof(RectTransform), typeof(LayoutElement));
                    sliderLabel.transform.SetParent(childGo.transform, worldPositionStays: false);
                    var sliderTmp = sliderLabel.AddComponent<TextMeshProUGUI>();
                    sliderTmp.text = pj?.label ?? string.Empty;
                    sliderTmp.alignment = TextAlignmentOptions.MidlineLeft;
                    sliderTmp.fontSize = 14f;
                    sliderTmp.color = Color.white;
                    sliderTmp.raycastTarget = false;
                    var sliderLabelLe = sliderLabel.GetComponent<LayoutElement>();
                    sliderLabelLe.flexibleWidth = 1f;
                    sliderLabelLe.preferredHeight = 28f;
                    var sliderHost = new GameObject("SliderHost", typeof(RectTransform), typeof(LayoutElement));
                    sliderHost.transform.SetParent(childGo.transform, worldPositionStays: false);
                    var sliderHostLe = sliderHost.GetComponent<LayoutElement>();
                    sliderHostLe.preferredWidth = 160f;
                    sliderHostLe.minWidth = 160f;
                    sliderHostLe.preferredHeight = 20f;
                    sliderHostLe.minHeight = 20f;
                    var sliderBg = new GameObject("Background", typeof(RectTransform));
                    sliderBg.transform.SetParent(sliderHost.transform, worldPositionStays: false);
                    var sliderBgRt = sliderBg.GetComponent<RectTransform>();
                    sliderBgRt.anchorMin = new Vector2(0f, 0.4f);
                    sliderBgRt.anchorMax = new Vector2(1f, 0.6f);
                    sliderBgRt.offsetMin = sliderBgRt.offsetMax = Vector2.zero;
                    var sliderBgImg = sliderBg.AddComponent<Image>();
                    sliderBgImg.color = new Color(0.25f, 0.25f, 0.25f, 1f);
                    sliderBgImg.raycastTarget = false;
                    var fillArea = new GameObject("Fill Area", typeof(RectTransform));
                    fillArea.transform.SetParent(sliderHost.transform, worldPositionStays: false);
                    var fillAreaRt = fillArea.GetComponent<RectTransform>();
                    fillAreaRt.anchorMin = new Vector2(0f, 0.4f);
                    fillAreaRt.anchorMax = new Vector2(1f, 0.6f);
                    fillAreaRt.offsetMin = new Vector2(5f, 0f);
                    fillAreaRt.offsetMax = new Vector2(-5f, 0f);
                    var fill = new GameObject("Fill", typeof(RectTransform));
                    fill.transform.SetParent(fillArea.transform, worldPositionStays: false);
                    var fillRt = fill.GetComponent<RectTransform>();
                    fillRt.anchorMin = new Vector2(0f, 0f);
                    fillRt.anchorMax = new Vector2(1f, 1f);
                    fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
                    var fillImg = fill.AddComponent<Image>();
                    fillImg.color = new Color(0.4f, 0.6f, 0.9f, 1f);
                    fillImg.raycastTarget = false;
                    var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
                    handleArea.transform.SetParent(sliderHost.transform, worldPositionStays: false);
                    var handleAreaRt = handleArea.GetComponent<RectTransform>();
                    handleAreaRt.anchorMin = new Vector2(0f, 0f);
                    handleAreaRt.anchorMax = new Vector2(1f, 1f);
                    handleAreaRt.offsetMin = new Vector2(5f, 0f);
                    handleAreaRt.offsetMax = new Vector2(-5f, 0f);
                    var handle = new GameObject("Handle", typeof(RectTransform));
                    handle.transform.SetParent(handleArea.transform, worldPositionStays: false);
                    var handleRt = handle.GetComponent<RectTransform>();
                    handleRt.anchorMin = new Vector2(0f, 0f);
                    handleRt.anchorMax = new Vector2(0f, 1f);
                    handleRt.sizeDelta = new Vector2(14f, 0f);
                    var handleImg = handle.AddComponent<Image>();
                    handleImg.color = Color.white;
                    var slider = sliderHost.AddComponent<UnityEngine.UI.Slider>();
                    slider.targetGraphic = handleImg;
                    slider.fillRect = fillRt;
                    slider.handleRect = handleRt;
                    slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
                    Territory.Editor.Bridge.UiBakeHandler.EnsureChildLayoutElement(childGo, preferredWidth: -1f, preferredHeight: 32f, flexibleWidth: 1f);
                    break;
                }
                case "toggle-row":
                {
                    var rowHlg = childGo.AddComponent<HorizontalLayoutGroup>();
                    rowHlg.spacing = 8f;
                    rowHlg.padding = new RectOffset(0, 0, 0, 0);
                    rowHlg.childAlignment = TextAnchor.MiddleLeft;
                    rowHlg.childForceExpandHeight = false;
                    rowHlg.childForceExpandWidth = false;
                    rowHlg.childControlHeight = true;
                    rowHlg.childControlWidth = true;
                    var toggleLabel = new GameObject("Label", typeof(RectTransform), typeof(LayoutElement));
                    toggleLabel.transform.SetParent(childGo.transform, worldPositionStays: false);
                    var toggleTmp = toggleLabel.AddComponent<TextMeshProUGUI>();
                    toggleTmp.text = pj?.label ?? string.Empty;
                    toggleTmp.alignment = TextAlignmentOptions.MidlineLeft;
                    toggleTmp.fontSize = 14f;
                    toggleTmp.color = Color.white;
                    toggleTmp.raycastTarget = false;
                    var toggleLabelLe = toggleLabel.GetComponent<LayoutElement>();
                    toggleLabelLe.flexibleWidth = 1f;
                    toggleLabelLe.preferredHeight = 24f;
                    var toggleHost = new GameObject("ToggleHost", typeof(RectTransform), typeof(LayoutElement));
                    toggleHost.transform.SetParent(childGo.transform, worldPositionStays: false);
                    var toggleHostLe = toggleHost.GetComponent<LayoutElement>();
                    toggleHostLe.preferredWidth = 24f;
                    toggleHostLe.minWidth = 24f;
                    toggleHostLe.preferredHeight = 24f;
                    toggleHostLe.minHeight = 24f;
                    var toggleBgImg = toggleHost.AddComponent<Image>();
                    toggleBgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                    var checkmark = new GameObject("Checkmark", typeof(RectTransform));
                    checkmark.transform.SetParent(toggleHost.transform, worldPositionStays: false);
                    var checkmarkRt = checkmark.GetComponent<RectTransform>();
                    checkmarkRt.anchorMin = new Vector2(0f, 0f);
                    checkmarkRt.anchorMax = new Vector2(1f, 1f);
                    checkmarkRt.offsetMin = new Vector2(4f, 4f);
                    checkmarkRt.offsetMax = new Vector2(-4f, -4f);
                    var checkImg = checkmark.AddComponent<Image>();
                    checkImg.color = new Color(0.4f, 0.8f, 0.4f, 1f);
                    checkImg.raycastTarget = false;
                    var toggle = toggleHost.AddComponent<UnityEngine.UI.Toggle>();
                    toggle.graphic = checkImg;
                    toggle.targetGraphic = toggleBgImg;
                    Territory.Editor.Bridge.UiBakeHandler.EnsureChildLayoutElement(childGo, preferredWidth: -1f, preferredHeight: 32f, flexibleWidth: 1f);
                    break;
                }
                case "dropdown-row":
                {
                    var rowHlg = childGo.AddComponent<HorizontalLayoutGroup>();
                    rowHlg.spacing = 8f;
                    rowHlg.padding = new RectOffset(0, 0, 0, 0);
                    rowHlg.childAlignment = TextAnchor.MiddleLeft;
                    rowHlg.childForceExpandHeight = false;
                    rowHlg.childForceExpandWidth = false;
                    rowHlg.childControlHeight = true;
                    rowHlg.childControlWidth = true;
                    var dropLabel = new GameObject("Label", typeof(RectTransform), typeof(LayoutElement));
                    dropLabel.transform.SetParent(childGo.transform, worldPositionStays: false);
                    var dropLabelTmp = dropLabel.AddComponent<TextMeshProUGUI>();
                    dropLabelTmp.text = pj?.label ?? string.Empty;
                    dropLabelTmp.alignment = TextAlignmentOptions.MidlineLeft;
                    dropLabelTmp.fontSize = 14f;
                    dropLabelTmp.color = Color.white;
                    dropLabelTmp.raycastTarget = false;
                    var dropLabelLe = dropLabel.GetComponent<LayoutElement>();
                    dropLabelLe.flexibleWidth = 1f;
                    dropLabelLe.preferredHeight = 28f;
                    var dropHost = new GameObject("DropdownHost", typeof(RectTransform), typeof(LayoutElement));
                    dropHost.transform.SetParent(childGo.transform, worldPositionStays: false);
                    var dropHostLe = dropHost.GetComponent<LayoutElement>();
                    dropHostLe.preferredWidth = 160f;
                    dropHostLe.minWidth = 160f;
                    dropHostLe.preferredHeight = 28f;
                    dropHostLe.minHeight = 28f;
                    var dropBgImg = dropHost.AddComponent<Image>();
                    dropBgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                    var dropCaption = new GameObject("Label", typeof(RectTransform));
                    dropCaption.transform.SetParent(dropHost.transform, worldPositionStays: false);
                    var dropCaptionRt = dropCaption.GetComponent<RectTransform>();
                    dropCaptionRt.anchorMin = new Vector2(0f, 0f);
                    dropCaptionRt.anchorMax = new Vector2(1f, 1f);
                    dropCaptionRt.offsetMin = new Vector2(8f, 2f);
                    dropCaptionRt.offsetMax = new Vector2(-24f, -2f);
                    var dropCaptionTmp = dropCaption.AddComponent<TMPro.TextMeshProUGUI>();
                    dropCaptionTmp.text = string.Empty;
                    dropCaptionTmp.alignment = TextAlignmentOptions.MidlineLeft;
                    dropCaptionTmp.fontSize = 12f;
                    dropCaptionTmp.color = Color.white;
                    dropCaptionTmp.raycastTarget = false;
                    var arrow = new GameObject("Arrow", typeof(RectTransform));
                    arrow.transform.SetParent(dropHost.transform, worldPositionStays: false);
                    var arrowRt = arrow.GetComponent<RectTransform>();
                    arrowRt.anchorMin = new Vector2(1f, 0.5f);
                    arrowRt.anchorMax = new Vector2(1f, 0.5f);
                    arrowRt.pivot = new Vector2(1f, 0.5f);
                    arrowRt.sizeDelta = new Vector2(16f, 16f);
                    arrowRt.anchoredPosition = new Vector2(-6f, 0f);
                    var arrowTmp = arrow.AddComponent<TMPro.TextMeshProUGUI>();
                    arrowTmp.text = "v";
                    arrowTmp.alignment = TextAlignmentOptions.Center;
                    arrowTmp.fontSize = 12f;
                    arrowTmp.fontStyle = FontStyles.Bold;
                    arrowTmp.color = Color.white;
                    arrowTmp.raycastTarget = false;
                    var dropdown = dropHost.AddComponent<TMPro.TMP_Dropdown>();
                    dropdown.captionText = dropCaptionTmp;
                    dropdown.targetGraphic = dropBgImg;
                    Territory.Editor.Bridge.UiBakeHandler.EnsureChildLayoutElement(childGo, preferredWidth: -1f, preferredHeight: 32f, flexibleWidth: 1f);
                    break;
                }
                case "section-header":
                {
                    var hdrLabel = new GameObject("Label", typeof(RectTransform));
                    hdrLabel.transform.SetParent(childGo.transform, worldPositionStays: false);
                    var hdrLabelRt = hdrLabel.GetComponent<RectTransform>();
                    hdrLabelRt.anchorMin = Vector2.zero;
                    hdrLabelRt.anchorMax = Vector2.one;
                    hdrLabelRt.offsetMin = hdrLabelRt.offsetMax = Vector2.zero;
                    var hdrTmp = hdrLabel.AddComponent<TextMeshProUGUI>();
                    hdrTmp.text = pj?.label ?? string.Empty;
                    hdrTmp.alignment = TextAlignmentOptions.MidlineLeft;
                    hdrTmp.fontSize = 16f;
                    hdrTmp.color = Color.white;
                    hdrTmp.fontStyle = TMPro.FontStyles.Bold;
                    hdrTmp.raycastTarget = false;
                    Territory.Editor.Bridge.UiBakeHandler.EnsureChildLayoutElement(childGo, preferredWidth: -1f, preferredHeight: 24f, flexibleWidth: 1f);
                    break;
                }
                case "list-row":
                {
                    var listHlg = childGo.AddComponent<HorizontalLayoutGroup>();
                    listHlg.spacing = 8f;
                    listHlg.padding = new RectOffset(8, 8, 4, 4);
                    listHlg.childAlignment = TextAnchor.MiddleLeft;
                    listHlg.childForceExpandHeight = false;
                    listHlg.childForceExpandWidth = false;
                    listHlg.childControlHeight = true;
                    listHlg.childControlWidth = true;
                    var listIcon = new GameObject("Icon", typeof(RectTransform), typeof(LayoutElement));
                    listIcon.transform.SetParent(childGo.transform, worldPositionStays: false);
                    var listIconImg = listIcon.AddComponent<Image>();
                    listIconImg.raycastTarget = false;
                    var listIconLe = listIcon.GetComponent<LayoutElement>();
                    listIconLe.preferredWidth = 24f;
                    listIconLe.minWidth = 24f;
                    listIconLe.preferredHeight = 24f;
                    listIconLe.minHeight = 24f;
                    var iconSprite = Territory.Editor.Bridge.UiBakeHandler.ResolveButtonIconSprite(pj?.icon);
                    if (iconSprite != null)
                    {
                        listIconImg.sprite = iconSprite;
                    }
                    else
                    {
                        listIcon.SetActive(false);
                        UnityEngine.Debug.LogWarning(
                            $"[UiBakeHandler] list-row missing icon for slug={pj?.icon ?? "<null>"} — caption-only fallback");
                    }
                    var listPrimary = new GameObject("PrimaryLabel", typeof(RectTransform), typeof(LayoutElement));
                    listPrimary.transform.SetParent(childGo.transform, worldPositionStays: false);
                    var listPrimaryTmp = listPrimary.AddComponent<TextMeshProUGUI>();
                    string resolvedListLabel = pj?.label;
                    if (string.IsNullOrEmpty(resolvedListLabel) && !string.IsNullOrEmpty(pj?.icon))
                    {
                        resolvedListLabel = Territory.Editor.Bridge.UiBakeHandler.TitleCaseSlug(pj.icon);
                    }
                    listPrimaryTmp.text = resolvedListLabel ?? string.Empty;
                    listPrimaryTmp.alignment = TextAlignmentOptions.MidlineLeft;
                    listPrimaryTmp.fontSize = 16f;
                    listPrimaryTmp.color = theme != null ? theme.TextPrimary : Color.white;
                    listPrimaryTmp.raycastTarget = false;
                    var listPrimaryLe = listPrimary.GetComponent<LayoutElement>();
                    listPrimaryLe.flexibleWidth = 1f;
                    listPrimaryLe.preferredHeight = 36f;
                    if (iconSprite == null) listPrimaryLe.preferredWidth = 160f;
                    var listSecondary = new GameObject("SecondaryValue", typeof(RectTransform), typeof(LayoutElement));
                    listSecondary.transform.SetParent(childGo.transform, worldPositionStays: false);
                    var listSecondaryTmp = listSecondary.AddComponent<TextMeshProUGUI>();
                    listSecondaryTmp.text = string.Empty;
                    listSecondaryTmp.alignment = TextAlignmentOptions.MidlineRight;
                    listSecondaryTmp.fontSize = 16f;
                    listSecondaryTmp.color = theme != null ? theme.TextSecondary : new Color(0.8f, 0.8f, 0.8f, 1f);
                    listSecondaryTmp.raycastTarget = false;
                    var listSecondaryLe = listSecondary.GetComponent<LayoutElement>();
                    listSecondaryLe.preferredWidth = 80f;
                    listSecondaryLe.minWidth = 80f;
                    listSecondaryLe.preferredHeight = 36f;
                    var rowCtrl = childGo.AddComponent<Territory.UI.Renderers.ServiceRowController>();
                    var rowSo = new SerializedObject(rowCtrl);
                    var bindIdProp = rowSo.FindProperty("_bindId");
                    if (bindIdProp != null) bindIdProp.stringValue = pj?.bindId ?? string.Empty;
                    var formatProp = rowSo.FindProperty("_format");
                    if (formatProp != null && !string.IsNullOrEmpty(pj?.format)) formatProp.stringValue = pj.format;
                    var secondaryProp = rowSo.FindProperty("_secondaryValueText");
                    if (secondaryProp != null) secondaryProp.objectReferenceValue = listSecondaryTmp;
                    var iconProp = rowSo.FindProperty("_iconImage");
                    if (iconProp != null) iconProp.objectReferenceValue = listIconImg;
                    rowSo.ApplyModifiedPropertiesWithoutUndo();
                    Territory.Editor.Bridge.UiBakeHandler.EnsureChildLayoutElement(childGo, preferredWidth: -1f, preferredHeight: 44f, flexibleWidth: 1f);
                    break;
                }
                case "info-dock":
                {
                    var infoBg = childGo.AddComponent<Image>();
                    infoBg.color = new Color(0.05f, 0.05f, 0.05f, 0.85f);
                    infoBg.raycastTarget = true;
                    var infoRt = childGo.GetComponent<RectTransform>();
                    if (infoRt != null)
                    {
                        infoRt.anchorMin = new Vector2(1f, 0f);
                        infoRt.anchorMax = new Vector2(1f, 1f);
                        infoRt.pivot = new Vector2(1f, 0.5f);
                        infoRt.sizeDelta = new Vector2(280f, 0f);
                        infoRt.anchoredPosition = new Vector2(-8f, 0f);
                    }
                    childGo.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
                    Territory.Editor.Bridge.UiBakeHandler.EnsureChildLayoutElement(childGo, preferredWidth: 280f, preferredHeight: -1f, flexibleWidth: 0f);
                    break;
                }
                case "field-list":
                {
                    var fieldBg = childGo.AddComponent<Image>();
                    fieldBg.color = new Color(0f, 0f, 0f, 0f);
                    fieldBg.raycastTarget = false;
                    var fieldVlg = childGo.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
                    fieldVlg.spacing = 2f;
                    fieldVlg.padding = new RectOffset(4, 4, 4, 4);
                    fieldVlg.childForceExpandHeight = false;
                    fieldVlg.childForceExpandWidth = true;
                    fieldVlg.childControlHeight = true;
                    fieldVlg.childControlWidth = true;
                    var prototypeGo = new GameObject("Row_Prototype", typeof(RectTransform), typeof(LayoutElement));
                    prototypeGo.transform.SetParent(childGo.transform, worldPositionStays: false);
                    var prototypeHlg = prototypeGo.AddComponent<HorizontalLayoutGroup>();
                    prototypeHlg.spacing = 8f;
                    prototypeHlg.childAlignment = TextAnchor.MiddleLeft;
                    prototypeHlg.childForceExpandHeight = false;
                    prototypeHlg.childForceExpandWidth = false;
                    prototypeHlg.childControlHeight = true;
                    prototypeHlg.childControlWidth = true;
                    var prototypeLe = prototypeGo.GetComponent<LayoutElement>();
                    prototypeLe.preferredHeight = 18f;
                    prototypeLe.flexibleWidth = 1f;
                    var keyGo = new GameObject("FieldKey", typeof(RectTransform), typeof(LayoutElement));
                    keyGo.transform.SetParent(prototypeGo.transform, worldPositionStays: false);
                    var keyTmp = keyGo.AddComponent<TextMeshProUGUI>();
                    keyTmp.text = "Key";
                    keyTmp.alignment = TextAlignmentOptions.MidlineLeft;
                    keyTmp.fontSize = 12f;
                    keyTmp.color = theme != null ? theme.TextSecondary : new Color(0.75f, 0.75f, 0.75f, 1f);
                    keyTmp.raycastTarget = false;
                    var keyLe = keyGo.GetComponent<LayoutElement>();
                    keyLe.flexibleWidth = 1f;
                    keyLe.preferredHeight = 18f;
                    var valGo = new GameObject("FieldValue", typeof(RectTransform), typeof(LayoutElement));
                    valGo.transform.SetParent(prototypeGo.transform, worldPositionStays: false);
                    var valTmp = valGo.AddComponent<TextMeshProUGUI>();
                    valTmp.text = "--";
                    valTmp.alignment = TextAlignmentOptions.MidlineRight;
                    valTmp.fontSize = 12f;
                    valTmp.color = theme != null ? theme.TextPrimary : Color.white;
                    valTmp.raycastTarget = false;
                    var valLe = valGo.GetComponent<LayoutElement>();
                    valLe.preferredWidth = 80f;
                    valLe.preferredHeight = 18f;
                    prototypeGo.SetActive(false);
                    var fieldCtrl = childGo.AddComponent<Territory.UI.Renderers.FieldListRenderer>();
                    var fieldSo = new SerializedObject(fieldCtrl);
                    var fieldBindIdProp = fieldSo.FindProperty("_bindId");
                    if (fieldBindIdProp != null) fieldBindIdProp.stringValue = pj?.bindId ?? string.Empty;
                    var containerProp = fieldSo.FindProperty("_container");
                    if (containerProp != null) containerProp.objectReferenceValue = childGo.GetComponent<RectTransform>();
                    var prototypeProp = fieldSo.FindProperty("_prototype");
                    if (prototypeProp != null) prototypeProp.objectReferenceValue = prototypeGo;
                    fieldSo.ApplyModifiedPropertiesWithoutUndo();
                    Territory.Editor.Bridge.UiBakeHandler.EnsureChildLayoutElement(childGo, preferredWidth: -1f, preferredHeight: -1f, flexibleWidth: 1f);
                    break;
                }
                case "minimap-canvas":
                {
                    var mapRawImg = childGo.AddComponent<RawImage>();
                    mapRawImg.raycastTarget = true;
                    var mapRt = childGo.GetComponent<RectTransform>();
                    if (mapRt != null)
                    {
                        mapRt.sizeDelta = new Vector2(360f, 324f);
                    }
                    childGo.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                    Territory.Editor.Bridge.UiBakeHandler.AttachUiActionTrigger(childGo, !string.IsNullOrEmpty(pj?.action) ? pj.action : pj?.actionId);
                    Territory.Editor.Bridge.UiBakeHandler.EnsureChildLayoutElement(childGo, preferredWidth: 360f, preferredHeight: 324f, flexibleWidth: 0f);
                    break;
                }
                case "toast-stack":
                {
                    var stackBg = childGo.AddComponent<Image>();
                    stackBg.color = new Color(0f, 0f, 0f, 0f);
                    stackBg.raycastTarget = false;
                    var vlg = childGo.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
                    vlg.spacing = 4f;
                    vlg.childAlignment = TextAnchor.UpperRight;
                    var stackRt = childGo.GetComponent<RectTransform>();
                    if (stackRt != null)
                    {
                        stackRt.anchorMin = new Vector2(1f, 1f);
                        stackRt.anchorMax = new Vector2(1f, 1f);
                        stackRt.pivot = new Vector2(1f, 1f);
                        stackRt.anchoredPosition = new Vector2(-8f, -8f);
                        stackRt.sizeDelta = new Vector2(320f, 0f);
                    }
                    Territory.Editor.Bridge.UiBakeHandler.EnsureChildLayoutElement(childGo, preferredWidth: 320f, preferredHeight: -1f, flexibleWidth: 0f);
                    break;
                }
                case "toast-card":
                {
                    var cardBg = childGo.AddComponent<Image>();
                    cardBg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
                    cardBg.raycastTarget = true;
                    var cardHlg = childGo.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                    cardHlg.spacing = 8f;
                    cardHlg.padding = new RectOffset(8, 8, 8, 8);
                    var cardIcon = new GameObject("Icon", typeof(RectTransform));
                    cardIcon.transform.SetParent(childGo.transform, worldPositionStays: false);
                    var cardIconImg = cardIcon.AddComponent<Image>();
                    cardIconImg.raycastTarget = false;
                    Territory.Editor.Bridge.UiBakeHandler.EnsureChildLayoutElement(cardIcon, preferredWidth: 24f, preferredHeight: 24f, flexibleWidth: 0f);
                    var cardTextCol = new GameObject("TextColumn", typeof(RectTransform));
                    cardTextCol.transform.SetParent(childGo.transform, worldPositionStays: false);
                    cardTextCol.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
                    Territory.Editor.Bridge.UiBakeHandler.EnsureChildLayoutElement(cardTextCol, preferredWidth: -1f, preferredHeight: -1f, flexibleWidth: 1f);
                    var cardTitle = new GameObject("Title", typeof(RectTransform));
                    cardTitle.transform.SetParent(cardTextCol.transform, worldPositionStays: false);
                    var cardTitleTmp = cardTitle.AddComponent<TextMeshProUGUI>();
                    cardTitleTmp.text = pj?.label ?? "Notification";
                    cardTitleTmp.fontSize = 14f;
                    cardTitleTmp.fontStyle = TMPro.FontStyles.Bold;
                    cardTitleTmp.raycastTarget = false;
                    var cardBody = new GameObject("Body", typeof(RectTransform));
                    cardBody.transform.SetParent(cardTextCol.transform, worldPositionStays: false);
                    var cardBodyTmp = cardBody.AddComponent<TextMeshProUGUI>();
                    cardBodyTmp.text = string.Empty;
                    cardBodyTmp.fontSize = 12f;
                    cardBodyTmp.raycastTarget = false;
                    var dismissGo = new GameObject("Dismiss", typeof(RectTransform));
                    dismissGo.transform.SetParent(childGo.transform, worldPositionStays: false);
                    var dismissBtn = dismissGo.AddComponent<UnityEngine.UI.Button>();
                    var dismissImg = dismissGo.AddComponent<Image>();
                    dismissImg.color = new Color(1f, 1f, 1f, 0.2f);
                    dismissBtn.targetGraphic = dismissImg;
                    Territory.Editor.Bridge.UiBakeHandler.EnsureChildLayoutElement(dismissGo, preferredWidth: 24f, preferredHeight: 24f, flexibleWidth: 0f);
                    Territory.Editor.Bridge.UiBakeHandler.EnsureChildLayoutElement(childGo, preferredWidth: -1f, preferredHeight: 56f, flexibleWidth: 1f);
                    break;
                }
                case "chart-stub":
                {
                    var chartImg = childGo.AddComponent<RawImage>();
                    chartImg.color = Color.white;
                    chartImg.raycastTarget = false;
                    var chartRenderer = childGo.AddComponent<Territory.UI.Renderers.ChartRenderer>();
                    var chartSo = new SerializedObject(chartRenderer);
                    var chartBindIdProp = chartSo.FindProperty("_bindId");
                    if (chartBindIdProp != null) chartBindIdProp.stringValue = pj?.bindId ?? string.Empty;
                    var chartModeProp = chartSo.FindProperty("_mode");
                    if (chartModeProp != null)
                    {
                        chartModeProp.enumValueIndex = (pj?.kind == "stacked-bar-row") ? 1 : 0;
                    }
                    if (theme != null)
                    {
                        var lineColorProp = chartSo.FindProperty("_lineColor");
                        if (lineColorProp != null) lineColorProp.colorValue = theme.AccentPrimary;
                        var axisColorProp = chartSo.FindProperty("_axisColor");
                        if (axisColorProp != null) axisColorProp.colorValue = theme.BorderSubtle;
                    }
                    chartSo.ApplyModifiedPropertiesWithoutUndo();
                    Territory.Editor.Bridge.UiBakeHandler.EnsureChildLayoutElement(childGo, preferredWidth: -1f, preferredHeight: 120f, flexibleWidth: 1f);
                    break;
                }
                case "tab-strip-stub":
                {
                    bool isRangeTabs = pj?.kind == "range-tabs";
                    float pillWidth  = isRangeTabs ? 72f  : 120f;
                    float pillHeight = isRangeTabs ? 28f  : 36f;
                    float pillFont   = isRangeTabs ? 14f  : 18f;
                    var stripBg = childGo.AddComponent<Image>();
                    stripBg.color = new Color(0f, 0f, 0f, 0f);
                    stripBg.raycastTarget = false;
                    var stripHlg = childGo.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                    stripHlg.spacing = 4f;
                    stripHlg.childAlignment = isRangeTabs ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
                    stripHlg.childForceExpandWidth  = false;
                    stripHlg.childForceExpandHeight = false;
                    stripHlg.childControlWidth  = false;
                    stripHlg.childControlHeight = false;
                    string[] captions = (pj?.tabs != null && pj.tabs.Length > 0) ? pj.tabs
                                       : (pj?.options != null && pj.options.Length > 0) ? pj.options
                                       : new[] { "(no tabs)" };
                    var toggleGroup = childGo.AddComponent<ToggleGroup>();
                    toggleGroup.allowSwitchOff = false;
                    Color idleColor = theme != null ? theme.SurfaceCardHud : new Color(0.18f, 0.18f, 0.22f, 1f);
                    Color activeColor = theme != null ? theme.AccentPrimary : new Color(0.29f, 0.62f, 1f, 1f);
                    var pillsArr = new System.Collections.Generic.List<(Toggle toggle, Image bg, string caption)>(captions.Length);
                    for (int i = 0; i < captions.Length; i++)
                    {
                        var caption = captions[i];
                        var pill = new GameObject($"Pill_{caption}", typeof(RectTransform), typeof(LayoutElement));
                        pill.transform.SetParent(childGo.transform, worldPositionStays: false);
                        var pillRt = pill.GetComponent<RectTransform>();
                        pillRt.sizeDelta = new Vector2(pillWidth, pillHeight);
                        var pillLe = pill.GetComponent<LayoutElement>();
                        pillLe.preferredWidth  = pillWidth;
                        pillLe.minWidth        = pillWidth;
                        pillLe.flexibleWidth   = 0f;
                        pillLe.preferredHeight = pillHeight;
                        pillLe.minHeight       = pillHeight;
                        pillLe.flexibleHeight  = 0f;
                        var pillImg = pill.AddComponent<Image>();
                        pillImg.color = idleColor;
                        pillImg.raycastTarget = true;
                        var labelGo = new GameObject("Label", typeof(RectTransform));
                        labelGo.transform.SetParent(pill.transform, worldPositionStays: false);
                        var labelRt = labelGo.GetComponent<RectTransform>();
                        labelRt.anchorMin = Vector2.zero;
                        labelRt.anchorMax = Vector2.one;
                        labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;
                        var labelTmp = labelGo.AddComponent<TMPro.TextMeshProUGUI>();
                        labelTmp.text = caption;
                        labelTmp.alignment = TextAlignmentOptions.Center;
                        labelTmp.fontSize = pillFont;
                        labelTmp.color = theme != null ? theme.TextPrimary : Color.white;
                        labelTmp.raycastTarget = false;
                        var pillToggle = pill.AddComponent<Toggle>();
                        pillToggle.targetGraphic = pillImg;
                        pillToggle.group = toggleGroup;
                        pillToggle.isOn = (i == 0);
                        if (i == 0) pillImg.color = activeColor;
                        pillsArr.Add((pillToggle, pillImg, caption));
                    }
                    var tabCtrl = childGo.AddComponent<Territory.UI.Renderers.TabStripController>();
                    var tabSo = new SerializedObject(tabCtrl);
                    var tabBindIdProp = tabSo.FindProperty("_bindId");
                    if (tabBindIdProp != null) tabBindIdProp.stringValue = pj?.bindId ?? string.Empty;
                    var tabActiveColorProp = tabSo.FindProperty("_activeColor");
                    if (tabActiveColorProp != null) tabActiveColorProp.colorValue = activeColor;
                    var tabIdleColorProp = tabSo.FindProperty("_idleColor");
                    if (tabIdleColorProp != null) tabIdleColorProp.colorValue = idleColor;
                    var pillsProp = tabSo.FindProperty("_pills");
                    if (pillsProp != null)
                    {
                        pillsProp.arraySize = pillsArr.Count;
                        for (int i = 0; i < pillsArr.Count; i++)
                        {
                            var elem = pillsProp.GetArrayElementAtIndex(i);
                            elem.FindPropertyRelative("toggle").objectReferenceValue = pillsArr[i].toggle;
                            elem.FindPropertyRelative("background").objectReferenceValue = pillsArr[i].bg;
                            elem.FindPropertyRelative("captionId").stringValue = pillsArr[i].caption;
                        }
                    }
                    tabSo.ApplyModifiedPropertiesWithoutUndo();
                    Territory.Editor.Bridge.UiBakeHandler.EnsureChildLayoutElement(childGo, preferredWidth: -1f, preferredHeight: 44f, flexibleWidth: 1f);
                    break;
                }
                case "save-controls-strip":
                {
                    var stripBg = childGo.AddComponent<Image>();
                    stripBg.color = new Color(0f, 0f, 0f, 0f);
                    stripBg.raycastTarget = false;
                    var stripHlg = childGo.AddComponent<HorizontalLayoutGroup>();
                    stripHlg.spacing = 8f;
                    stripHlg.childAlignment = TextAnchor.MiddleCenter;
                    stripHlg.childForceExpandWidth = true;
                    stripHlg.childForceExpandHeight = true;
                    stripHlg.childControlWidth = true;
                    stripHlg.childControlHeight = true;
                    string[] pillLabels = { "Save", "Load" };
                    for (int i = 0; i < pillLabels.Length; i++)
                    {
                        var pill = new GameObject($"Pill_{pillLabels[i]}",
                            typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                        pill.transform.SetParent(childGo.transform, worldPositionStays: false);
                        var pillImg = pill.GetComponent<Image>();
                        pillImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                        var pillBtn = pill.GetComponent<Button>();
                        pillBtn.targetGraphic = pillImg;
                        var pillLe = pill.GetComponent<LayoutElement>();
                        pillLe.preferredHeight = 36f;
                        pillLe.flexibleWidth = 1f;
                        var pillLabelGo = new GameObject("Label", typeof(RectTransform));
                        pillLabelGo.transform.SetParent(pill.transform, worldPositionStays: false);
                        var pillTmp = pillLabelGo.AddComponent<TextMeshProUGUI>();
                        pillTmp.text = pillLabels[i];
                        pillTmp.alignment = TextAlignmentOptions.Center;
                        pillTmp.fontSize = 14f;
                        pillTmp.color = Color.white;
                        pillTmp.raycastTarget = false;
                        var pillLabelRt = pillLabelGo.GetComponent<RectTransform>();
                        pillLabelRt.anchorMin = Vector2.zero;
                        pillLabelRt.anchorMax = Vector2.one;
                        pillLabelRt.offsetMin = pillLabelRt.offsetMax = Vector2.zero;
                    }
                    Territory.Editor.Bridge.UiBakeHandler.EnsureChildLayoutElement(childGo, preferredWidth: -1f, preferredHeight: 40f, flexibleWidth: 1f);
                    break;
                }
                case "save-list":
                {
                    var listBg = childGo.AddComponent<Image>();
                    listBg.color = new Color(0.08f, 0.08f, 0.08f, 1f);
                    listBg.raycastTarget = false;
                    var listVlg = childGo.AddComponent<VerticalLayoutGroup>();
                    listVlg.spacing = 4f;
                    listVlg.padding = new RectOffset(4, 4, 4, 4);
                    listVlg.childForceExpandWidth = true;
                    listVlg.childForceExpandHeight = false;
                    listVlg.childControlWidth = true;
                    listVlg.childControlHeight = true;
                    var placeholderRow = new GameObject("PlaceholderRow", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                    placeholderRow.transform.SetParent(childGo.transform, worldPositionStays: false);
                    var rowImg = placeholderRow.GetComponent<Image>();
                    rowImg.color = new Color(0.15f, 0.15f, 0.15f, 1f);
                    rowImg.raycastTarget = false;
                    var rowLe = placeholderRow.GetComponent<LayoutElement>();
                    rowLe.preferredHeight = 32f;
                    var rowLabelGo = new GameObject("Label", typeof(RectTransform));
                    rowLabelGo.transform.SetParent(placeholderRow.transform, worldPositionStays: false);
                    var rowTmp = rowLabelGo.AddComponent<TextMeshProUGUI>();
                    rowTmp.text = "(no saves)";
                    rowTmp.alignment = TextAlignmentOptions.MidlineLeft;
                    rowTmp.fontSize = 12f;
                    rowTmp.color = new Color(0.62f, 0.62f, 0.62f, 1f);
                    rowTmp.raycastTarget = false;
                    var rowLabelRt = rowLabelGo.GetComponent<RectTransform>();
                    rowLabelRt.anchorMin = new Vector2(0f, 0f);
                    rowLabelRt.anchorMax = new Vector2(1f, 1f);
                    rowLabelRt.offsetMin = new Vector2(8f, 0f);
                    rowLabelRt.offsetMax = new Vector2(-8f, 0f);
                    Territory.Editor.Bridge.UiBakeHandler.EnsureChildLayoutElement(childGo, preferredWidth: -1f, preferredHeight: 160f, flexibleWidth: 1f);
                    var saveListLe = childGo.GetComponent<LayoutElement>();
                    if (saveListLe != null) saveListLe.flexibleHeight = 1f;
                    break;
                }
                case "text-input":
                {
                    var inputBg = childGo.AddComponent<Image>();
                    inputBg.color = new Color(0.12f, 0.12f, 0.12f, 1f);
                    inputBg.raycastTarget = true;
                    var textArea = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
                    textArea.transform.SetParent(childGo.transform, worldPositionStays: false);
                    var textAreaRt = textArea.GetComponent<RectTransform>();
                    textAreaRt.anchorMin = Vector2.zero;
                    textAreaRt.anchorMax = Vector2.one;
                    textAreaRt.offsetMin = new Vector2(8f, 4f);
                    textAreaRt.offsetMax = new Vector2(-8f, -4f);
                    var placeholderGo = new GameObject("Placeholder", typeof(RectTransform));
                    placeholderGo.transform.SetParent(textArea.transform, worldPositionStays: false);
                    var placeholderTmp = placeholderGo.AddComponent<TextMeshProUGUI>();
                    placeholderTmp.text = pj?.placeholder ?? string.Empty;
                    placeholderTmp.alignment = TextAlignmentOptions.MidlineLeft;
                    placeholderTmp.fontSize = 12f;
                    placeholderTmp.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                    placeholderTmp.raycastTarget = false;
                    var placeholderRt = placeholderGo.GetComponent<RectTransform>();
                    placeholderRt.anchorMin = Vector2.zero;
                    placeholderRt.anchorMax = Vector2.one;
                    placeholderRt.offsetMin = placeholderRt.offsetMax = Vector2.zero;
                    var inputTextGo = new GameObject("Text", typeof(RectTransform));
                    inputTextGo.transform.SetParent(textArea.transform, worldPositionStays: false);
                    var inputTmp = inputTextGo.AddComponent<TextMeshProUGUI>();
                    inputTmp.text = string.Empty;
                    inputTmp.alignment = TextAlignmentOptions.MidlineLeft;
                    inputTmp.fontSize = 12f;
                    inputTmp.color = Color.white;
                    inputTmp.raycastTarget = false;
                    var inputTextRt = inputTextGo.GetComponent<RectTransform>();
                    inputTextRt.anchorMin = Vector2.zero;
                    inputTextRt.anchorMax = Vector2.one;
                    inputTextRt.offsetMin = inputTextRt.offsetMax = Vector2.zero;
                    var inputField = childGo.AddComponent<TMP_InputField>();
                    inputField.targetGraphic = inputBg;
                    inputField.textViewport = textAreaRt;
                    inputField.textComponent = inputTmp;
                    inputField.placeholder = placeholderTmp;
                    Territory.Editor.Bridge.UiBakeHandler.EnsureChildLayoutElement(childGo, preferredWidth: -1f, preferredHeight: 32f, flexibleWidth: 1f);
                    break;
                }
                case "card-picker":
                {
                    var pickerBg = childGo.AddComponent<Image>();
                    pickerBg.color = new Color(0f, 0f, 0f, 0f);
                    pickerBg.raycastTarget = false;
                    var grid = childGo.AddComponent<GridLayoutGroup>();
                    grid.cellSize = new Vector2(160f, 96f);
                    grid.spacing = new Vector2(8f, 8f);
                    grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    grid.constraintCount = 3;
                    var labels = pj?.cards ?? new[] { "Small", "Medium", "Large" };
                    var toggleGroup = childGo.AddComponent<ToggleGroup>();
                    foreach (var cap in labels)
                    {
                        var cardGo = new GameObject("card_" + (cap?.ToLowerInvariant() ?? "x"), typeof(RectTransform));
                        cardGo.transform.SetParent(childGo.transform, worldPositionStays: false);
                        var cardBg = cardGo.AddComponent<Image>();
                        cardBg.color = new Color(0.18f, 0.18f, 0.22f, 1f);
                        var toggle = cardGo.AddComponent<Toggle>();
                        toggle.targetGraphic = cardBg;
                        toggle.group = toggleGroup;
                        var cardLabelGo = new GameObject("Label", typeof(RectTransform));
                        cardLabelGo.transform.SetParent(cardGo.transform, worldPositionStays: false);
                        var labelRt = (RectTransform)cardLabelGo.transform;
                        labelRt.anchorMin = Vector2.zero;
                        labelRt.anchorMax = Vector2.one;
                        labelRt.offsetMin = Vector2.zero;
                        labelRt.offsetMax = Vector2.zero;
                        var tmp = cardLabelGo.AddComponent<TextMeshProUGUI>();
                        tmp.text = cap ?? string.Empty;
                        tmp.fontSize = Territory.Editor.Bridge.UiBakeHandler.ResolveTypeScaleFontSize("size-text-body-row", 18f);
                        tmp.alignment = TextAlignmentOptions.Center;
                        tmp.raycastTarget = false;
                    }
                    Territory.Editor.Bridge.UiBakeHandler.EnsureChildLayoutElement(childGo, preferredWidth: -1f, preferredHeight: 200f, flexibleWidth: 1f);
                    Territory.Editor.Bridge.UiBakeHandler.AttachUiActionTrigger(childGo, !string.IsNullOrEmpty(pj?.action) ? pj.action : pj?.actionId);
                    break;
                }
                case "chip-picker":
                {
                    var chipBg = childGo.AddComponent<Image>();
                    chipBg.color = new Color(0f, 0f, 0f, 0f);
                    chipBg.raycastTarget = false;
                    var hlg = childGo.AddComponent<HorizontalLayoutGroup>();
                    hlg.spacing = 8f;
                    hlg.padding = new RectOffset(0, 0, 0, 0);
                    hlg.childAlignment = TextAnchor.MiddleLeft;
                    hlg.childForceExpandHeight = false;
                    hlg.childForceExpandWidth = false;
                    var captions = pj?.chips ?? new[] { "Low", "Mid", "High" };
                    var selectedHex = Territory.Editor.Bridge.UiBakeHandler.ResolveColorTokenHex("color-bg-selected");
                    Color selectedColor;
                    if (string.IsNullOrEmpty(selectedHex) || !ColorUtility.TryParseHtmlString(selectedHex, out selectedColor))
                    {
                        selectedColor = new Color(0.165f, 0.369f, 0.749f, 1f);
                    }
                    var tg = childGo.AddComponent<ToggleGroup>();
                    foreach (var cap in captions)
                    {
                        var chipGo = new GameObject("chip_" + (cap?.ToLowerInvariant() ?? "x"), typeof(RectTransform));
                        chipGo.transform.SetParent(childGo.transform, worldPositionStays: false);
                        var bg = chipGo.AddComponent<Image>();
                        bg.color = new Color(0.16f, 0.16f, 0.20f, 1f);
                        var toggle = chipGo.AddComponent<Toggle>();
                        toggle.targetGraphic = bg;
                        toggle.group = tg;
                        var colors = toggle.colors;
                        colors.normalColor = new Color(0.16f, 0.16f, 0.20f, 1f);
                        colors.selectedColor = selectedColor;
                        colors.highlightedColor = selectedColor;
                        toggle.colors = colors;
                        var chipLabelGo = new GameObject("Label", typeof(RectTransform));
                        chipLabelGo.transform.SetParent(chipGo.transform, worldPositionStays: false);
                        var labelRt = (RectTransform)chipLabelGo.transform;
                        labelRt.anchorMin = Vector2.zero;
                        labelRt.anchorMax = Vector2.one;
                        labelRt.offsetMin = new Vector2(12f, 4f);
                        labelRt.offsetMax = new Vector2(-12f, -4f);
                        var tmp = chipLabelGo.AddComponent<TextMeshProUGUI>();
                        tmp.text = cap ?? string.Empty;
                        tmp.fontSize = Territory.Editor.Bridge.UiBakeHandler.ResolveTypeScaleFontSize("size-text-body-row", 18f);
                        tmp.alignment = TextAlignmentOptions.Center;
                        tmp.raycastTarget = false;
                        Territory.Editor.Bridge.UiBakeHandler.EnsureChildLayoutElement(chipGo, preferredWidth: 96f, preferredHeight: 36f, flexibleWidth: 0f);
                    }
                    Territory.Editor.Bridge.UiBakeHandler.EnsureChildLayoutElement(childGo, preferredWidth: -1f, preferredHeight: 48f, flexibleWidth: 1f);
                    Territory.Editor.Bridge.UiBakeHandler.AttachUiActionTrigger(childGo, !string.IsNullOrEmpty(pj?.action) ? pj.action : pj?.actionId);
                    break;
                }
                case "subtype-card":
                {
                    var subBg = childGo.AddComponent<Image>();
                    subBg.color = new Color(0.16f, 0.16f, 0.20f, 1f);
                    var btn = childGo.AddComponent<Button>();
                    btn.targetGraphic = subBg;
                    var subVlg = childGo.AddComponent<VerticalLayoutGroup>();
                    subVlg.padding = new RectOffset(6, 6, 6, 6);
                    subVlg.spacing = 4f;
                    subVlg.childAlignment = TextAnchor.MiddleCenter;
                    var iconGo = new GameObject("Icon", typeof(RectTransform));
                    iconGo.transform.SetParent(childGo.transform, worldPositionStays: false);
                    var iconImg = iconGo.AddComponent<Image>();
                    iconImg.color = new Color(1f, 1f, 1f, 0.6f);
                    iconImg.raycastTarget = false;
                    Territory.Editor.Bridge.UiBakeHandler.EnsureChildLayoutElement(iconGo, preferredWidth: 48f, preferredHeight: 48f, flexibleWidth: 0f);
                    var labelGo = new GameObject("Label", typeof(RectTransform));
                    labelGo.transform.SetParent(childGo.transform, worldPositionStays: false);
                    var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
                    labelTmp.text = !string.IsNullOrEmpty(pj?.label) ? pj.label
                        : (!string.IsNullOrEmpty(pj?.subtype) ? Territory.Editor.Bridge.UiBakeHandler.TitleCaseSlug(pj.subtype) : "Subtype");
                    labelTmp.fontSize = Territory.Editor.Bridge.UiBakeHandler.ResolveTypeScaleFontSize("size-text-body-row", 14f);
                    labelTmp.alignment = TextAlignmentOptions.Center;
                    labelTmp.raycastTarget = false;
                    Territory.Editor.Bridge.UiBakeHandler.EnsureChildLayoutElement(labelGo, preferredWidth: -1f, preferredHeight: 20f, flexibleWidth: 1f);
                    Territory.Editor.Bridge.UiBakeHandler.AttachUiActionTrigger(childGo, !string.IsNullOrEmpty(pj?.action) ? pj.action : pj?.actionId);
                    Territory.Editor.Bridge.UiBakeHandler.EnsureChildLayoutElement(childGo, preferredWidth: 96f, preferredHeight: 96f, flexibleWidth: 0f);
                    break;
                }
                case "chart":
                {
                    var chartImg = childGo.AddComponent<RawImage>();
                    chartImg.color = Color.white;
                    chartImg.raycastTarget = false;
                    var chartRenderer = childGo.AddComponent<Territory.UI.Renderers.ChartRenderer>();
                    var chartSo = new SerializedObject(chartRenderer);
                    var chartBindIdProp = chartSo.FindProperty("_bindId");
                    if (chartBindIdProp != null) chartBindIdProp.stringValue = pj?.bindId ?? pj?.bind ?? string.Empty;
                    var chartModeProp = chartSo.FindProperty("_mode");
                    if (chartModeProp != null) chartModeProp.enumValueIndex = (pj?.kind == "stacked-bar-row") ? 1 : 0;
                    if (theme != null)
                    {
                        var lineColorProp = chartSo.FindProperty("_lineColor");
                        if (lineColorProp != null) lineColorProp.colorValue = theme.AccentPrimary;
                        var axisColorProp = chartSo.FindProperty("_axisColor");
                        if (axisColorProp != null) axisColorProp.colorValue = theme.BorderSubtle;
                    }
                    chartSo.ApplyModifiedPropertiesWithoutUndo();
                    if (pj?.axisLabels != null && pj.axisLabels.Length > 0)
                    {
                        for (int i = 0; i < pj.axisLabels.Length; i++)
                        {
                            var axisGo = new GameObject("axis_label_" + i, typeof(RectTransform));
                            axisGo.transform.SetParent(childGo.transform, worldPositionStays: false);
                            var axisRt = (RectTransform)axisGo.transform;
                            float t = pj.axisLabels.Length > 1 ? (float)i / (pj.axisLabels.Length - 1) : 0.5f;
                            axisRt.anchorMin = new Vector2(t, 0f);
                            axisRt.anchorMax = new Vector2(t, 0f);
                            axisRt.pivot = new Vector2(0.5f, 1f);
                            axisRt.anchoredPosition = new Vector2(0f, -2f);
                            axisRt.sizeDelta = new Vector2(64f, 16f);
                            var axisTmp = axisGo.AddComponent<TextMeshProUGUI>();
                            axisTmp.text = pj.axisLabels[i] ?? string.Empty;
                            axisTmp.fontSize = Territory.Editor.Bridge.UiBakeHandler.ResolveTypeScaleFontSize("size-text-body-row", 12f);
                            axisTmp.alignment = TextAlignmentOptions.Center;
                            axisTmp.raycastTarget = false;
                            var axisLe = axisGo.AddComponent<LayoutElement>();
                            axisLe.ignoreLayout = true;
                        }
                    }
                    Territory.Editor.Bridge.UiBakeHandler.EnsureChildLayoutElement(childGo, preferredWidth: -1f, preferredHeight: 160f, flexibleWidth: 1f);
                    break;
                }
                default:
                {
                    Territory.Editor.Bridge.UiBakeHandler.AddBakeWarning("unhandled_inner_kind", innerKind ?? "(null)", $"$.child[{childGo.name}].kind");
                    break;
                }
            }
        }
    }
}
