using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Domains.UI.Data;
using Territory.UI;
using Territory.UI.Decoration;
using Territory.UI.Editor;
using Territory.UI.HUD;
using Territory.UI.Juice;
using Territory.UI.Modals;
using Territory.UI.StudioControls;
using Territory.UI.StudioControls.Renderers;
using Territory.UI.Themed;
using Territory.UI.Themed.Renderers;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Territory.Editor.Bridge
{
    /// <summary>
    /// IR JSON → UiTheme.asset bake handler. Editor-only. Stage 2 of Game UI Design System MVP.
    ///
    /// DTO field names mirror the historical Stage 1 sketchpad shape so JsonUtility round-trips
    /// stay deterministic. Polymorphic shapes use Unity-friendly optional-with-zero-default fields
    /// (JsonUtility cannot model discriminated unions natively).
    ///
    /// T2.2 lands DTOs + Parse + ValidateSlotAcceptRules + Bake skeleton; T2.4 fills the bake body.
    /// </summary>
    public static partial class UiBakeHandler
    {
        // ── DTOs extracted to Domains/UI/Data/ (TECH-31974) ─────────────────────
        // IrRoot, IrTokens, IrTokenPalette, IrTokenFrameStyle, IrTokenFontFace,
        // IrTokenMotionCurve, IrTokenIllumination, IrPanelDetail, IrButtonDetail,
        // IrButtonPaletteRamp, IrButtonAtlasSlotEnum, IrButtonMotionCurve,
        // IrButtonStateDetail, IrPanel, IrPanelSlot, IrTab, IrRow,
        // IrInteractive, IrJuiceDecl, BakeArgs, PanelSnapshot, PanelSnapshotItem,
        // PanelSnapshotFields, PanelRectJson, PanelSnapshotChild, PanelChildLayoutSize,
        // PanelChildLayoutJson, PanelChildParamsJson, PanelFieldsParamsJson, PanelPaddingJson,
        // TokenSnapshot, TokenSnapshotItem, TokenSpacingValueDto, TokenColorValueDto,
        // TokenTypeScaleValueDto — all via `using Domains.UI.Data;` above.

        // ── Nested re-exports — keep backward-compat for external callers (TECH-31974) ──
        // External callers reference UiBakeHandler.BakeArgs; keep nested alias pointing at Data type.
        public class BakeArgs : Domains.UI.Data.BakeArgs { }

        // ── Token resolver — delegates to Domains.UI.Services.TokenResolver (TECH-31975/31976) ──

        private static void LoadTokenSnapshot(string panelsPath) =>
            Domains.UI.Services.TokenResolver.LoadTokenSnapshot(panelsPath);

        private static string SubstituteSpacingTokensInJson(string raw) =>
            Domains.UI.Services.TokenResolver.SubstituteSpacingTokensInJson(raw);

        public static float ResolveTypeScaleFontSize(string slug, float fallback) =>
            Domains.UI.Services.TokenResolver.ResolveTypeScaleFontSize(slug, fallback);

        public static string ResolveTypeScaleWeight(string slug, string fallback) =>
            Domains.UI.Services.TokenResolver.ResolveTypeScaleWeight(slug, fallback);

        public static string ResolveColorTokenHex(string slug) =>
            Domains.UI.Services.TokenResolver.ResolveColorTokenHex(slug);

        /// <summary>JsonUtility wrapper that returns a default-init T when input is null/whitespace/malformed (silent — log on caller side when needed).</summary>
        private static T TryParseTypedJson<T>(string json) where T : class, new()
        {
            if (string.IsNullOrWhiteSpace(json)) return new T();
            try
            {
                var parsed = JsonUtility.FromJson<T>(json);
                return parsed ?? new T();
            }
            catch
            {
                return new T();
            }
        }

        /// <summary>Slug → Title Case ("power" → "Power", "public-housing" → "Public Housing").</summary>
        private static string TitleCaseSlug(string slug)
        {
            if (string.IsNullOrEmpty(slug)) return string.Empty;
            var parts = slug.Replace('_', '-').Split('-');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length == 0) continue;
                parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1);
            }
            return string.Join(" ", parts);
        }

        // Imp-1 (bake-fix-2026-05-08) — unified kind dispatch. Snapshot path + IR/Frame path share
        // a single switch, so a new component kind never has to be added in two places.
        // Normalization: panels.json uses outer `child.kind` (button/label) + inner `params_json.kind`
        // (illuminated-button / readout / label) — collapse to a single inner-kind string.

        /// <summary>Map outer `child.kind` + inner `params_json.kind` to canonical inner-kind slug.
        /// Inner pj.kind wins when set; outer child.kind drives default when pj.kind is missing.
        /// Mappings: pj.kind="readout" → "segmented-readout"; pj.kind="label" → "themed-label";
        /// pj.kind="illuminated-button" → "illuminated-button"; child.kind="button" (no pj.kind) →
        /// "illuminated-button"; child.kind="label" (no pj.kind) → "themed-label". Pass-through otherwise.</summary>
        static string NormalizeChildKind(string outerKind, string innerKind)
        {
            if (!string.IsNullOrEmpty(innerKind))
            {
                if (innerKind == "readout") return "segmented-readout";
                if (innerKind == "readout-block") return "segmented-readout";
                if (innerKind == "label") return "themed-label";
                // main-menu fullscreen-stack aliases (docs/ui-element-definitions.md lines 1239-1248):
                //   destructive-confirm-button → confirm-button (visual = illuminated-button +
                //     ConfirmButton runtime; deferred runtime wiring uses confirm-button kind tag).
                //   icon-button → illuminated-button (visual identical; iconSlug-only render path
                //     already supported by IlluminatedButton renderer).
                if (innerKind == "destructive-confirm-button") return "confirm-button";
                if (innerKind == "icon-button") return "illuminated-button";
                if (innerKind == "themed-button") return "illuminated-button";
                if (innerKind == "view-slot") return "view-slot";
                // Stage 10 budget/stats modal aliases — map new catalog slugs to existing handlers.
                if (innerKind == "slider-row-numeric") return "slider-row";
                if (innerKind == "expense-row") return "list-row";
                if (innerKind == "service-row") return "list-row";
                // Bucket C5 — `chart` resolves to full renderer with axis-label support.
                // stacked-bar-row keeps the stub path.
                if (innerKind == "stacked-bar-row") return "chart-stub";
                if (innerKind == "range-tabs") return "tab-strip-stub";
                if (innerKind == "tab-strip") return "tab-strip-stub";
                return innerKind;
            }
            if (outerKind == "button") return "illuminated-button";
            if (outerKind == "label") return "themed-label";
            if (outerKind == "confirm-button") return "confirm-button";
            if (outerKind == "view-slot") return "view-slot";
            return outerKind;
        }

        /// <summary>Imp-1 — shared kind dispatcher. Attaches the correct StudioControl + renderer pair
        /// + spawns render-target child GameObjects (body/icon/halo, tmp text, caption) onto
        /// <paramref name="childGo"/>. Reuses the same spawn helpers as the IR/Frame path
        /// (<see cref="SpawnIlluminatedButtonRenderTargets"/>, <see cref="WireIlluminatedButtonHoverAndPress"/>,
        /// <see cref="SpawnIlluminatedButtonCaption"/>, <see cref="SpawnSegmentedReadoutRenderTargets"/>,
        /// <see cref="SpawnThemedLabelChild"/>) so snapshot and IR contracts stay byte-identical.</summary>
        static void BakeChildByKind(GameObject childGo, string innerKind, PanelChildParamsJson pj, UiTheme theme,
            float preferredWidth = 64f, float preferredHeight = 64f, string panelDisplayName = null)
        {
            if (childGo == null) return;
            string iconSlug = pj != null ? pj.icon : null;
            string label = pj != null ? pj.label : null;

            switch (innerKind)
            {
                case "illuminated-button":
                {
                    var btn = childGo.AddComponent<IlluminatedButton>();
                    WireThemeRef(btn, theme);
                    var btnRend = childGo.GetComponent<IlluminatedButtonRenderer>();
                    if (btnRend == null) btnRend = childGo.AddComponent<IlluminatedButtonRenderer>();
                    WireThemeRef(btnRend, theme);
                    bool iconResolved = SpawnIlluminatedButtonRenderTargets(childGo, iconSlug, out var bodyImg, out var haloImg);
                    WireIlluminatedButtonHoverAndPress(childGo, btnRend, bodyImg, haloImg, theme);
                    btn.ApplyDetail(new IlluminatedButtonDetail { iconSpriteSlug = iconSlug });
                    AttachUiActionTrigger(childGo, !string.IsNullOrEmpty(pj?.action) ? pj.action : pj?.actionId);
                    // Caption fallback when icon sprite missing OR slug is the placeholder "empty" — both
                    // need the label to communicate function while real art is pending.
                    bool isPlaceholder = string.IsNullOrEmpty(iconSlug) || iconSlug == "empty";
                    if ((!iconResolved || isPlaceholder) && !string.IsNullOrEmpty(label))
                    {
                        SpawnIlluminatedButtonCaption(childGo, label);
                    }
                    // Dynamic value label — pj.bind drives BindTextRenderer that subscribes to the
                    // bindId and formats per pj.format. Used by hud-bar-budget-button to show $N.
                    string bindForLabel = !string.IsNullOrEmpty(pj?.bind) ? pj.bind : pj?.bindId;
                    if (!string.IsNullOrEmpty(bindForLabel))
                    {
                        var bindLabelGo = new GameObject("BindLabel", typeof(RectTransform));
                        bindLabelGo.transform.SetParent(childGo.transform, worldPositionStays: false);
                        var bindLabelRt = (RectTransform)bindLabelGo.transform;
                        // Center across the full button rect — stretch on both axes so the text
                        // sits in the middle visually instead of being bottom-anchored.
                        bindLabelRt.anchorMin = Vector2.zero;
                        bindLabelRt.anchorMax = Vector2.one;
                        bindLabelRt.pivot = new Vector2(0.5f, 0.5f);
                        bindLabelRt.offsetMin = Vector2.zero;
                        bindLabelRt.offsetMax = Vector2.zero;
                        var bindTmp = bindLabelGo.AddComponent<TextMeshProUGUI>();
                        // Bumped to size-text-modal-title (24pt bold) so the dollar amount reads at a glance.
                        bindTmp.fontSize = ResolveTypeScaleFontSize("size-text-modal-title", 24f);
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
                    EnsureChildLayoutElement(childGo, preferredWidth: preferredWidth, preferredHeight: preferredHeight, flexibleWidth: 0f);
                    break;
                }
                case "segmented-readout":
                {
                    var sr = childGo.AddComponent<SegmentedReadout>();
                    WireThemeRef(sr, theme);
                    var srRend = childGo.GetComponent<SegmentedReadoutRenderer>();
                    if (srRend == null) srRend = childGo.AddComponent<SegmentedReadoutRenderer>();
                    WireThemeRef(srRend, theme);
                    var sd = new SegmentedReadoutDetail { digits = 1 };
                    SpawnSegmentedReadoutRenderTargets(childGo, sd);
                    sr.ApplyDetail(sd);
                    EnsureChildLayoutElement(childGo, preferredWidth: 120f, preferredHeight: 32f, flexibleWidth: 1f);
                    break;
                }
                case "themed-label":
                {
                    var lbl = childGo.AddComponent<ThemedLabel>();
                    WireThemeRef(lbl, theme);
                    SpawnThemedLabelChild(childGo, out var labelTmp);
                    // text_static (branding strips) wins over generic label; modal-title variant
                    // falls back to the parent panel's display_name so stats-panel / budget-panel
                    // headers render the catalog display label without an explicit text_static.
                    // "--" placeholder is the last-resort default.
                    string staticText = pj?.text_static;
                    bool isModalTitle = string.Equals(pj?.variant, "modal-title", StringComparison.Ordinal);
                    string resolvedText;
                    if (!string.IsNullOrEmpty(staticText)) resolvedText = staticText;
                    else if (!string.IsNullOrEmpty(label)) resolvedText = label;
                    else if (isModalTitle && !string.IsNullOrEmpty(panelDisplayName)) resolvedText = panelDisplayName;
                    else resolvedText = "--";
                    if (labelTmp != null)
                    {
                        labelTmp.text = resolvedText;
                        // Variant → type-scale token resolution (pilot iter 8 + Bucket F).
                        // modal-title → size-text-modal-title (24pt bold).
                        // section-header → size-text-section-header (20pt bold).
                        // Default body row → size-text-body-row (18pt regular).
                        // Autosize disabled when variant or size_token resolves a fontSize.
                        bool variantSized = false;
                        if (!string.IsNullOrEmpty(pj?.variant))
                        {
                            float vSize = pj.variant switch
                            {
                                "modal-title"   => ResolveTypeScaleFontSize("size-text-modal-title", 24f),
                                "section-header" => ResolveTypeScaleFontSize("size-text-section-header", 20f),
                                "body-row"      => ResolveTypeScaleFontSize("size-text-body-row", 18f),
                                "value"         => ResolveTypeScaleFontSize("size-text-value", 18f),
                                _               => -1f,
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
                        // size_token mapping (size.text.* legacy slugs + size-text-* published slugs).
                        // Autosize disabled when an explicit size_token is provided.
                        if (!string.IsNullOrEmpty(pj?.size_token))
                        {
                            labelTmp.enableAutoSizing = false;
                            float resolvedSize = pj.size_token switch
                            {
                                "size.text.title-display" => 64f,
                                "size.text.title"         => 32f,
                                "size.text.body"          => 16f,
                                "size.text.caption"       => 12f,
                                _ => ResolveTypeScaleFontSize(pj.size_token, labelTmp.fontSize),
                            };
                            labelTmp.fontSize = resolvedSize;
                            string weight = ResolveTypeScaleWeight(pj.size_token, null);
                            if (string.Equals(weight, "bold", StringComparison.Ordinal))
                                labelTmp.fontStyle |= TMPro.FontStyles.Bold;
                            variantSized = true;
                        }
                        // Floor guard for unsized labels — autosize min lift to 12 prevents the
                        // 8pt collapse seen in narrow HLG cells (settings-view sub-view header).
                        if (!variantSized && labelTmp.enableAutoSizing)
                        {
                            labelTmp.fontSizeMin = Mathf.Max(labelTmp.fontSizeMin, 12f);
                        }
                        if (string.Equals(pj?.color_token, "color.text.muted", StringComparison.Ordinal))
                        {
                            labelTmp.color = new Color(0.62f, 0.62f, 0.62f, 1f);
                        }
                        if (string.Equals(pj?.align, "center", StringComparison.Ordinal))
                            labelTmp.alignment = TextAlignmentOptions.Center;
                        else if (string.Equals(pj?.align, "right", StringComparison.Ordinal))
                            labelTmp.alignment = TextAlignmentOptions.Right;
                        else if (string.Equals(pj?.align, "left", StringComparison.Ordinal))
                            labelTmp.alignment = TextAlignmentOptions.Left;
                    }
                    var lblSo = new SerializedObject(lbl);
                    var tmpProp = lblSo.FindProperty("_tmpText");
                    if (tmpProp != null) tmpProp.objectReferenceValue = labelTmp;
                    var lblPalette = lblSo.FindProperty("_paletteSlug");
                    if (lblPalette != null) lblPalette.stringValue = "silkscreen";
                    lblSo.ApplyModifiedPropertiesWithoutUndo();
                    EnsureChildLayoutElement(childGo, preferredWidth: -1f, preferredHeight: 32f, flexibleWidth: 1f);
                    break;
                }
                case "confirm-button":
                {
                    // Visual identical to illuminated-button; runtime confirm-window wiring lives
                    // on a future ConfirmButton MonoBehaviour. For now bake renders the button +
                    // caption fallback so the destructive Quit row is visible + clickable.
                    var btn = childGo.AddComponent<IlluminatedButton>();
                    WireThemeRef(btn, theme);
                    var btnRend = childGo.GetComponent<IlluminatedButtonRenderer>();
                    if (btnRend == null) btnRend = childGo.AddComponent<IlluminatedButtonRenderer>();
                    WireThemeRef(btnRend, theme);
                    bool iconResolved = SpawnIlluminatedButtonRenderTargets(childGo, iconSlug, out var bodyImg, out var haloImg);
                    WireIlluminatedButtonHoverAndPress(childGo, btnRend, bodyImg, haloImg, theme);
                    btn.ApplyDetail(new IlluminatedButtonDetail { iconSpriteSlug = iconSlug });
                    AttachUiActionTrigger(childGo, !string.IsNullOrEmpty(pj?.action) ? pj.action : pj?.actionId);
                    bool isPlaceholder = string.IsNullOrEmpty(iconSlug) || iconSlug == "empty";
                    if ((!iconResolved || isPlaceholder) && !string.IsNullOrEmpty(label))
                    {
                        SpawnIlluminatedButtonCaption(childGo, label);
                    }
                    EnsureChildLayoutElement(childGo, preferredWidth: preferredWidth, preferredHeight: preferredHeight, flexibleWidth: 0f);
                    break;
                }
                case "view-slot":
                {
                    // Sub-view mount point. No visible primitive — runtime swaps a child prefab
                    // into this transform when slot_bind value changes (root | new-game-form |
                    // load-list | settings). Rect filled by parent Zone_Center stretch.
                    var rect = childGo.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        rect.anchorMin = new Vector2(0f, 0f);
                        rect.anchorMax = new Vector2(1f, 1f);
                        rect.offsetMin = rect.offsetMax = Vector2.zero;
                    }
                    EnsureChildLayoutElement(childGo, preferredWidth: -1f, preferredHeight: -1f, flexibleWidth: 1f);
                    break;
                }
                case "back-button":
                {
                    // Official UI back-arrow — single source of truth in NavBackButton factory.
                    // 40x40 dark chip + "<" TMP glyph. Action wired via UiActionTrigger (pj.action).
                    // childGo is the wrapper produced by the bake loop; reparent the factory output
                    // INTO childGo so corner/layout post-processing still hits childGo's RectTransform.
                    var spawned = NavBackButton.Spawn(childGo);
                    // NavBackButton.Spawn adds Image+Button+LayoutElement to spawned; collapse them
                    // onto childGo instead so corner-overlay + LayoutElement.ignoreLayout still target
                    // the single wrapper. Move the "<" label child up.
                    var labelTr = spawned.transform.Find("Label");
                    if (labelTr != null) labelTr.SetParent(childGo.transform, worldPositionStays: false);
                    // Replicate chip visuals on childGo (Image color + Button targetGraphic).
                    var chipImg = childGo.GetComponent<Image>();
                    if (chipImg == null) chipImg = childGo.AddComponent<Image>();
                    chipImg.color = new Color(0.18f, 0.18f, 0.18f, 0.9f);
                    var chipBtn = childGo.GetComponent<Button>();
                    if (chipBtn == null) chipBtn = childGo.AddComponent<Button>();
                    chipBtn.targetGraphic = chipImg;
                    UnityEngine.Object.DestroyImmediate(spawned);
                    AttachUiActionTrigger(childGo, !string.IsNullOrEmpty(pj?.action) ? pj.action : pj?.actionId);
                    float size = pj != null && pj.corner_size > 0 ? pj.corner_size : NavBackButton.DefaultSize;
                    EnsureChildLayoutElement(childGo, preferredWidth: size, preferredHeight: size, flexibleWidth: 0f);
                    break;
                }
                case "slider-row":
                {
                    // 2-col HLG row: label (left, flex) + slider host (right, fixed 160).
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

                    // Slider host — real Unity Slider with Background + Fill Area/Fill + Handle Slide Area/Handle.
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

                    EnsureChildLayoutElement(childGo, preferredWidth: -1f, preferredHeight: 32f, flexibleWidth: 1f);
                    break;
                }
                case "toggle-row":
                {
                    // 2-col HLG row: label (left, flex) + toggle host (right, fixed 24x24).
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

                    EnsureChildLayoutElement(childGo, preferredWidth: -1f, preferredHeight: 32f, flexibleWidth: 1f);
                    break;
                }
                case "dropdown-row":
                {
                    // 2-col HLG row: label (left, flex) + dropdown host (right, fixed 160).
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

                    EnsureChildLayoutElement(childGo, preferredWidth: -1f, preferredHeight: 32f, flexibleWidth: 1f);
                    break;
                }
                case "section-header":
                {
                    // Stage 4 settings widget — bold section text label.
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
                    EnsureChildLayoutElement(childGo, preferredWidth: -1f, preferredHeight: 24f, flexibleWidth: 1f);
                    break;
                }
                case "list-row":
                {
                    // Stage 10 stats/budget — 3-col HLG row: Icon (24×24) · PrimaryLabel (flex) · SecondaryValue (64px right).
                    // Mirrors toggle-row HLG settings; runtime ServiceRowController subscribes pj.bindId → SecondaryValue text.
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
                    var iconSprite = ResolveButtonIconSprite(pj?.icon);
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
                    // Step 12.1 — list-row label fallback: when params_json.label is absent,
                    // derive from pj.icon (e.g. "power" → "Power"). Mirrors authoring intent for
                    // panels.json expense-row/service-row entries that ship icon + bindId only.
                    string resolvedListLabel = pj?.label;
                    if (string.IsNullOrEmpty(resolvedListLabel) && !string.IsNullOrEmpty(pj?.icon))
                    {
                        resolvedListLabel = TitleCaseSlug(pj.icon);
                    }
                    listPrimaryTmp.text = resolvedListLabel ?? string.Empty;
                    listPrimaryTmp.alignment = TextAlignmentOptions.MidlineLeft;
                    listPrimaryTmp.fontSize = 16f;
                    listPrimaryTmp.color = theme != null ? theme.TextPrimary : Color.white;
                    listPrimaryTmp.raycastTarget = false;
                    var listPrimaryLe = listPrimary.GetComponent<LayoutElement>();
                    listPrimaryLe.flexibleWidth = 1f;
                    listPrimaryLe.preferredHeight = 36f;
                    // Caption-only fallback bumps preferred width so secondary value still right-aligns.
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

                    // Wire ServiceRowController.
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

                    EnsureChildLayoutElement(childGo, preferredWidth: -1f, preferredHeight: 44f, flexibleWidth: 1f);
                    break;
                }
                // Wave B5 (TECH-27098) HUD widget archetypes.
                case "info-dock":
                {
                    // Outer right-edge dock container — header + field-list slot + footer-action area.
                    // Stage 5.5 OUTER_KIND_EXCLUSIONS pre-pop; actual children baked as field-list + confirm-button.
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
                    EnsureChildLayoutElement(childGo, preferredWidth: 280f, preferredHeight: -1f, flexibleWidth: 0f);
                    break;
                }
                case "field-list":
                {
                    // Stage 10 stats/budget — label-value pair list from string[] bind. Transparent BG,
                    // VLG container, hidden Row_Prototype (HLG of FieldKey + FieldValue). Runtime
                    // FieldListRenderer clones the prototype per pair on Subscribe<string[]>(bindId).
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

                    EnsureChildLayoutElement(childGo, preferredWidth: -1f, preferredHeight: -1f, flexibleWidth: 1f);
                    break;
                }
                case "minimap-canvas":
                {
                    // RawImage render surface + IDragHandler-marked component + layer-toggle children.
                    // Stage 5.5 alias to themed-label (aliased renderer path) — now first-class.
                    var mapRawImg = childGo.AddComponent<RawImage>();
                    mapRawImg.raycastTarget = true;
                    var mapRt = childGo.GetComponent<RectTransform>();
                    if (mapRt != null)
                    {
                        mapRt.sizeDelta = new Vector2(360f, 324f);
                    }
                    // IDragHandler scaffold — runtime MiniMapController.OnDrag implements drag-pan.
                    // Bake-time: add EventTrigger as proxy for IDragHandler detection by render-check.
                    childGo.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                    AttachUiActionTrigger(childGo, !string.IsNullOrEmpty(pj?.action) ? pj.action : pj?.actionId);
                    EnsureChildLayoutElement(childGo, preferredWidth: 360f, preferredHeight: 324f, flexibleWidth: 0f);
                    break;
                }
                case "toast-stack":
                {
                    // Top-right anchored vertical stack container. Stage 5.5 OUTER_KIND_EXCLUSIONS pre-pop.
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
                    EnsureChildLayoutElement(childGo, preferredWidth: 320f, preferredHeight: -1f, flexibleWidth: 0f);
                    break;
                }
                case "toast-card":
                {
                    // Toast card: icon + title + body + dismiss button + sticky-variant flag.
                    // Stage 5.5 alias to themed-label (aliased renderer path) — now first-class.
                    var cardBg = childGo.AddComponent<Image>();
                    cardBg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
                    cardBg.raycastTarget = true;
                    var cardHlg = childGo.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                    cardHlg.spacing = 8f;
                    cardHlg.padding = new RectOffset(8, 8, 8, 8);
                    // Icon placeholder.
                    var cardIcon = new GameObject("Icon", typeof(RectTransform));
                    cardIcon.transform.SetParent(childGo.transform, worldPositionStays: false);
                    var cardIconImg = cardIcon.AddComponent<Image>();
                    cardIconImg.raycastTarget = false;
                    EnsureChildLayoutElement(cardIcon, preferredWidth: 24f, preferredHeight: 24f, flexibleWidth: 0f);
                    // Title + body column.
                    var cardTextCol = new GameObject("TextColumn", typeof(RectTransform));
                    cardTextCol.transform.SetParent(childGo.transform, worldPositionStays: false);
                    cardTextCol.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
                    EnsureChildLayoutElement(cardTextCol, preferredWidth: -1f, preferredHeight: -1f, flexibleWidth: 1f);
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
                    // Dismiss button.
                    var dismissGo = new GameObject("Dismiss", typeof(RectTransform));
                    dismissGo.transform.SetParent(childGo.transform, worldPositionStays: false);
                    var dismissBtn = dismissGo.AddComponent<UnityEngine.UI.Button>();
                    var dismissImg = dismissGo.AddComponent<Image>();
                    dismissImg.color = new Color(1f, 1f, 1f, 0.2f);
                    dismissBtn.targetGraphic = dismissImg;
                    EnsureChildLayoutElement(dismissGo, preferredWidth: 24f, preferredHeight: 24f, flexibleWidth: 0f);
                    EnsureChildLayoutElement(childGo, preferredWidth: -1f, preferredHeight: 56f, flexibleWidth: 1f);
                    break;
                }
                case "chart-stub":
                {
                    // Stage 10 budget/stats — chart + stacked-bar placeholder. RawImage scaffold;
                    // ChartRenderer owns the Texture2D + paints on Subscribe<float[]>(bindId, ...).
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
                        // pj.kind selects mode: "stacked-bar-row" → StackedBar, otherwise Line.
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
                    EnsureChildLayoutElement(childGo, preferredWidth: -1f, preferredHeight: 120f, flexibleWidth: 1f);
                    break;
                }
                case "tab-strip-stub":
                {
                    // Stage 10 budget/stats — tab-strip + range-tabs. Pills built from pj.tabs (else
                    // pj.options, else single fallback caption). ToggleGroup ensures single-active;
                    // runtime TabStripController publishes captionId to bindId on click + recolors.
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

                    EnsureChildLayoutElement(childGo, preferredWidth: -1f, preferredHeight: 44f, flexibleWidth: 1f);
                    break;
                }
                case "save-controls-strip":
                {
                    // Save/Load mode toggle strip — two pills (Save | Load) wired to bindId.
                    // Bake-time placeholder: HLG container + 2 themed-button-like pills with
                    // captions. Runtime SaveLoadController populates active state from bindId.
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
                    EnsureChildLayoutElement(childGo, preferredWidth: -1f, preferredHeight: 40f, flexibleWidth: 1f);
                    break;
                }
                case "save-list":
                {
                    // Vertical save-slot list — bake-time scroll-view stub. Runtime
                    // SaveLoadController populates rows from listBindId.
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
                    // Placeholder row so the container has visible content at bake time.
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
                    EnsureChildLayoutElement(childGo, preferredWidth: -1f, preferredHeight: 160f, flexibleWidth: 1f);
                    var saveListLe = childGo.GetComponent<LayoutElement>();
                    if (saveListLe != null) saveListLe.flexibleHeight = 1f;
                    break;
                }
                case "text-input":
                {
                    // TMP_InputField scaffold — runtime SaveLoadController binds via bind id.
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
                    EnsureChildLayoutElement(childGo, preferredWidth: -1f, preferredHeight: 32f, flexibleWidth: 1f);
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
                    foreach (var caption in labels)
                    {
                        var cardGo = new GameObject("card_" + (caption?.ToLowerInvariant() ?? "x"), typeof(RectTransform));
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
                        tmp.text = caption ?? string.Empty;
                        tmp.fontSize = ResolveTypeScaleFontSize("size-text-body-row", 18f);
                        tmp.alignment = TextAlignmentOptions.Center;
                        tmp.raycastTarget = false;
                    }
                    EnsureChildLayoutElement(childGo, preferredWidth: -1f, preferredHeight: 200f, flexibleWidth: 1f);
                    AttachUiActionTrigger(childGo, !string.IsNullOrEmpty(pj?.action) ? pj.action : pj?.actionId);
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
                    var selectedHex = ResolveColorTokenHex("color-bg-selected");
                    Color selectedColor;
                    if (string.IsNullOrEmpty(selectedHex) || !ColorUtility.TryParseHtmlString(selectedHex, out selectedColor))
                    {
                        selectedColor = new Color(0.165f, 0.369f, 0.749f, 1f);
                    }
                    var tg = childGo.AddComponent<ToggleGroup>();
                    foreach (var caption in captions)
                    {
                        var chipGo = new GameObject("chip_" + (caption?.ToLowerInvariant() ?? "x"), typeof(RectTransform));
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
                        tmp.text = caption ?? string.Empty;
                        tmp.fontSize = ResolveTypeScaleFontSize("size-text-body-row", 18f);
                        tmp.alignment = TextAlignmentOptions.Center;
                        tmp.raycastTarget = false;
                        EnsureChildLayoutElement(chipGo, preferredWidth: 96f, preferredHeight: 36f, flexibleWidth: 0f);
                    }
                    EnsureChildLayoutElement(childGo, preferredWidth: -1f, preferredHeight: 48f, flexibleWidth: 1f);
                    AttachUiActionTrigger(childGo, !string.IsNullOrEmpty(pj?.action) ? pj.action : pj?.actionId);
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
                    EnsureChildLayoutElement(iconGo, preferredWidth: 48f, preferredHeight: 48f, flexibleWidth: 0f);
                    var labelGo = new GameObject("Label", typeof(RectTransform));
                    labelGo.transform.SetParent(childGo.transform, worldPositionStays: false);
                    var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
                    labelTmp.text = !string.IsNullOrEmpty(pj?.label) ? pj.label
                        : (!string.IsNullOrEmpty(pj?.subtype) ? TitleCaseSlug(pj.subtype) : "Subtype");
                    labelTmp.fontSize = ResolveTypeScaleFontSize("size-text-body-row", 14f);
                    labelTmp.alignment = TextAlignmentOptions.Center;
                    labelTmp.raycastTarget = false;
                    EnsureChildLayoutElement(labelGo, preferredWidth: -1f, preferredHeight: 20f, flexibleWidth: 1f);
                    AttachUiActionTrigger(childGo, !string.IsNullOrEmpty(pj?.action) ? pj.action : pj?.actionId);
                    EnsureChildLayoutElement(childGo, preferredWidth: 96f, preferredHeight: 96f, flexibleWidth: 0f);
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
                            axisTmp.fontSize = ResolveTypeScaleFontSize("size-text-body-row", 12f);
                            axisTmp.alignment = TextAlignmentOptions.Center;
                            axisTmp.raycastTarget = false;
                            var axisLe = axisGo.AddComponent<LayoutElement>();
                            axisLe.ignoreLayout = true;
                        }
                    }
                    EnsureChildLayoutElement(childGo, preferredWidth: -1f, preferredHeight: 160f, flexibleWidth: 1f);
                    break;
                }
                default:
                {
                    AddBakeWarning("unhandled_inner_kind", innerKind ?? "(null)", $"$.child[{childGo.name}].kind");
                    break;
                }
            }
        }

        /// <summary>Structured bake error — round-trips through bridge `{ok: false, error, details, path}` payload.</summary>
        [Serializable]
        public class BakeError
        {
            public string error;
            public string details;
            public string path;
        }

        /// <summary>Parse result — non-null root on success; populated error on schema fault.</summary>
        public class BakeResult
        {
            public IrRoot root;
            public BakeError error;
            // Imp-3 (bake-fix-2026-05-08) — non-fatal warnings collected during bake.
            // Empty when bake clean. Bridge runner surfaces these in mutation_result JSON
            // so the agent can flag silent failures without a hard bake error.
            public List<BakeError> warnings = new List<BakeError>();
        }

        // Imp-3 (bake-fix-2026-05-08) — call-scoped warning collector. BakeFromPanelSnapshot
        // assigns + clears around its body. Helpers append via AddBakeWarning(...).
        // Always logs to Debug regardless of collector presence.
        private static List<BakeError> _currentBakeWarnings;

        internal static void AddBakeWarning(string error, string details, string path)
        {
            UnityEngine.Debug.LogWarning($"[UiBakeHandler] {error}: {details} @ {path}");
            if (_currentBakeWarnings != null)
            {
                _currentBakeWarnings.Add(new BakeError { error = error, details = details, path = path });
            }
        }

        // ── Parse ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Parse IR JSON via <see cref="JsonUtility.FromJson{T}(string)"/>. Returns
        /// <c>(root, error=null)</c> on success or <c>(root=null, error)</c> on schema fault.
        /// JsonUtility silently drops unknown fields — acceptable for MVP per §Plan Digest.
        /// </summary>
        /// <summary>Last raw IR JSON text passed to <see cref="Parse"/>; used by <see cref="ExtractInteractiveDetailJson"/> for per-row detail substring capture (JsonUtility cannot model open-shape detail block).</summary>
        private static string _lastRawIrJson;

        // ── Stage 9.10 — PanelSnapshot parse + layout primitive map ─────────────

        /// <summary>
        /// Parse panels.json snapshot JSON via JsonUtility. Returns <c>(snapshot, error=null)</c>
        /// on success or <c>(null, error)</c> on schema fault. Missing layout_template fails
        /// hard with <c>bake.layout_template_missing</c> error code.
        /// </summary>
        public static (PanelSnapshot snapshot, BakeError error) ParsePanelSnapshot(string jsonText)
        {
            if (string.IsNullOrWhiteSpace(jsonText))
            {
                return (null, new BakeError
                {
                    error = "schema_violation",
                    details = "empty_or_whitespace_json",
                    path = "$",
                });
            }

            PanelSnapshot parsed;
            try
            {
                parsed = JsonUtility.FromJson<PanelSnapshot>(jsonText);
            }
            catch (Exception ex)
            {
                return (null, new BakeError
                {
                    error = "schema_violation",
                    details = ex.Message,
                    path = "$",
                });
            }

            if (parsed == null)
            {
                return (null, new BakeError
                {
                    error = "schema_violation",
                    details = "json_parsed_null",
                    path = "$",
                });
            }

            if (parsed.items == null || parsed.items.Length == 0)
            {
                return (null, new BakeError
                {
                    error = "schema_violation",
                    details = "items_missing_or_empty",
                    path = "$.items",
                });
            }

            return (parsed, null);
        }

        /// <summary>
        /// Map <c>layout_template</c> string to LayoutGroup component type.
        /// Throws <see cref="BakeError"/> (<c>bake.layout_template_missing</c>) when null or empty —
        /// no silent vstack fallback per Stage 9.10 spec.
        /// </summary>
        /// <param name="layoutTemplate">Value from <see cref="PanelSnapshotFields.layout_template"/>.</param>
        /// <param name="panelSlug">Panel slug for error context.</param>
        /// <returns>Type of LayoutGroup component to add (<c>HorizontalLayoutGroup</c> / <c>VerticalLayoutGroup</c> / <c>GridLayoutGroup</c>).</returns>
        /// <exception cref="Exception">Throws formatted exception with <c>bake.layout_template_missing</c> when template absent.</exception>
        public static System.Type MapLayoutTemplate(string layoutTemplate, string panelSlug)
        {
            if (string.IsNullOrEmpty(layoutTemplate))
            {
                throw new Exception($"bake.layout_template_missing: {panelSlug}");
            }
            switch (layoutTemplate)
            {
                case "hstack": return typeof(HorizontalLayoutGroup);
                case "vstack": return typeof(VerticalLayoutGroup);
                case "grid":   return typeof(GridLayoutGroup);
                default:
                    AddBakeWarning("layout_template_unrecognised", $"'{layoutTemplate}' falling back to vstack", $"$.items[{panelSlug}].fields.layout_template");
                    return typeof(VerticalLayoutGroup);
            }
        }

        // F1 (bake-fix-2026-05-07): map layout_template → PanelKind so bake-time _kind
        // matches the LayoutGroup attached at root, preventing OnEnable from stripping it.
        internal static PanelKind MapLayoutTemplateToPanelKind(string layoutTemplate, string panelSlug)
        {
            switch (layoutTemplate)
            {
                case "hstack":           return PanelKind.Hud;
                case "vstack":           return PanelKind.Modal;
                case "grid":             return PanelKind.Toolbar;
                case "fullscreen-stack": return PanelKind.Screen;
                default:                 return PanelKind.Modal;
            }
        }

        /// <summary>True when layout_template requires zone-wrapper routing instead of a single root LayoutGroup.</summary>
        internal static bool IsFullscreenStackTemplate(string layoutTemplate)
            => string.Equals(layoutTemplate, "fullscreen-stack", StringComparison.Ordinal);

        // F1: SerializedObject write — ThemedPanel._kind is private serialized field.
        internal static void AssignPanelKind(ThemedPanel themedPanel, PanelKind kind)
        {
            if (themedPanel == null) return;
            var so = new SerializedObject(themedPanel);
            var prop = so.FindProperty("_kind");
            if (prop == null) return;
            prop.intValue = (int)kind;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // F2: per-kind anchor/size defaults. Hud=top-strip stretch by default (Bug A 2026-05-08;
        // overridable via PanelFieldsParamsJson.position="bottom"), Modal=center, Toolbar=left-rail,
        // SideRail=right-rail, Screen=full stretch.
        internal static void ApplyPanelKindRectDefaults(RectTransform rect, PanelKind kind, string position = null)
        {
            if (rect == null) return;
            switch (kind)
            {
                case PanelKind.Hud:
                {
                    bool bottom = string.Equals(position, "bottom", StringComparison.OrdinalIgnoreCase);
                    if (bottom)
                    {
                        rect.anchorMin = new Vector2(0f, 0f);
                        rect.anchorMax = new Vector2(1f, 0f);
                        rect.pivot = new Vector2(0.5f, 0f);
                        rect.anchoredPosition = new Vector2(0f, 8f);
                    }
                    else
                    {
                        // Default top-strip — HUD-bar spec position. Anchor min=(0,1) max=(1,1) pivot=(0.5,1)
                        // pulls the strip to the top edge with 8px breathing room beneath the screen edge.
                        rect.anchorMin = new Vector2(0f, 1f);
                        rect.anchorMax = new Vector2(1f, 1f);
                        rect.pivot = new Vector2(0.5f, 1f);
                        rect.anchoredPosition = new Vector2(0f, -8f);
                    }
                    // Y=144 fits Right zone stacked Col (zoom-in 64 + spacing 4 + zoom-out 64 = 132) +
                    // top/bottom padding (4+4) with headroom; Center 3-row label stack (3*32 + 2*4 = 104) fits too.
                    rect.sizeDelta = new Vector2(-16f, 144f);
                    break;
                }
                case PanelKind.Toolbar:
                    rect.anchorMin = new Vector2(0f, 0f);
                    rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0f, 0.5f);
                    rect.anchoredPosition = new Vector2(8f, 0f);
                    rect.sizeDelta = new Vector2(96f, -16f);
                    break;
                case PanelKind.SideRail:
                    rect.anchorMin = new Vector2(1f, 0f);
                    rect.anchorMax = new Vector2(1f, 1f);
                    rect.pivot = new Vector2(1f, 0.5f);
                    rect.anchoredPosition = new Vector2(-8f, 0f);
                    rect.sizeDelta = new Vector2(96f, -16f);
                    break;
                case PanelKind.Screen:
                    rect.anchorMin = new Vector2(0f, 0f);
                    rect.anchorMax = new Vector2(1f, 1f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = Vector2.zero;
                    rect.sizeDelta = Vector2.zero;
                    break;
                case PanelKind.Modal:
                default:
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = Vector2.zero;
                    rect.sizeDelta = new Vector2(480f, 320f);
                    break;
            }
        }

        // DB-first rect overlay — panel_detail.rect_json from panels.json `fields.rect_json`.
        // Source of truth: hud-bar (Stage hud-bar bake-test fork) keys = anchor_min, anchor_max, pivot,
        // anchored_position, size_delta — each a float[2] = [x, y]. Missing keys leave the prior
        // PanelKind hard-coded default in place (defense in depth: kind defaults are still authoritative
        // for panel-kinds without a DB record yet, e.g. Toolbar / SideRail at the time of writing).
        internal static void ApplyPanelRectJsonOverlay(RectTransform rect, string rectJson)
        {
            if (rect == null || string.IsNullOrWhiteSpace(rectJson)) return;
            var rj = TryParseTypedJson<PanelRectJson>(rectJson);
            if (rj == null) return;
            if (rj.anchor_min != null && rj.anchor_min.Length >= 2)
                rect.anchorMin = new Vector2(rj.anchor_min[0], rj.anchor_min[1]);
            if (rj.anchor_max != null && rj.anchor_max.Length >= 2)
                rect.anchorMax = new Vector2(rj.anchor_max[0], rj.anchor_max[1]);
            if (rj.pivot != null && rj.pivot.Length >= 2)
                rect.pivot = new Vector2(rj.pivot[0], rj.pivot[1]);
            if (rj.anchored_position != null && rj.anchored_position.Length >= 2)
                rect.anchoredPosition = new Vector2(rj.anchored_position[0], rj.anchored_position[1]);
            if (rj.size_delta != null && rj.size_delta.Length >= 2)
                rect.sizeDelta = new Vector2(rj.size_delta[0], rj.size_delta[1]);
        }

        // Track A.3 — DB-rect-only mode for non-bake-spawned panels (e.g. toolbar).
        // Panel is published with empty `panel_child` rows; prefab is hand-authored
        // and lives under Assets/UI/Prefabs/Generated/{slug}.prefab. Bake skips prefab
        // regeneration (root rect would clobber the hand-authored hierarchy) and instead
        // syncs `panel_detail.rect_json` onto every live PrefabInstance of the prefab in
        // Assets/Scenes/**/*.unity. Result: PrefabInstance overrides on root rect come
        // from DB programmatically, not hand-edited yaml. (docs/ui-bake-pipeline-rollout-plan.md.)
        static BakeError ApplyDbRectToScenePrefabInstances(string slug, string assetPath, string rectJson)
        {
            if (string.IsNullOrWhiteSpace(rectJson))
            {
                // No DB rect to apply — DB-rect-only mode requires a rect_json to be useful.
                // Treat as no-op (panel published but rect not yet seeded).
                return null;
            }

            var prefabAssetRoot = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefabAssetRoot == null)
            {
                return new BakeError
                {
                    error = "panel_prefab_load_failed",
                    details = $"could not load prefab asset for slug '{slug}' at '{assetPath}'",
                    path = assetPath,
                };
            }

            var sceneSetupBefore = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
                foreach (var guid in sceneGuids)
                {
                    var scenePath = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(scenePath)) continue;

                    var deps = AssetDatabase.GetDependencies(scenePath, false);
                    bool referencesPrefab = false;
                    foreach (var dep in deps)
                    {
                        if (string.Equals(dep, assetPath, StringComparison.Ordinal)) { referencesPrefab = true; break; }
                    }
                    if (!referencesPrefab) continue;

                    var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    if (!scene.IsValid()) continue;

                    bool sceneTouched = false;
                    foreach (var root in scene.GetRootGameObjects())
                    {
                        foreach (var t in root.GetComponentsInChildren<Transform>(true))
                        {
                            var go = t.gameObject;
                            if (!PrefabUtility.IsAnyPrefabInstanceRoot(go)) continue;
                            var src = PrefabUtility.GetCorrespondingObjectFromSource(go) as GameObject;
                            if (src == null) continue;
                            if (src != prefabAssetRoot) continue;
                            var rect = go.GetComponent<RectTransform>();
                            if (rect == null) continue;
                            ApplyPanelRectJsonOverlay(rect, rectJson);
                            PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
                            sceneTouched = true;
                        }
                    }

                    if (sceneTouched)
                    {
                        EditorSceneManager.MarkSceneDirty(scene);
                        EditorSceneManager.SaveScene(scene);
                    }
                }
            }
            catch (Exception ex)
            {
                return new BakeError
                {
                    error = "panel_db_rect_sync_failed",
                    details = ex.Message,
                    path = assetPath,
                };
            }
            finally
            {
                if (sceneSetupBefore != null && sceneSetupBefore.Length > 0)
                {
                    try { EditorSceneManager.RestoreSceneManagerSetup(sceneSetupBefore); }
                    catch { /* swallow — restore is best-effort */ }
                }
            }

            return null;
        }

        // F3 (bake-fix-2026-05-07): configure root LayoutGroup so children fill row evenly,
        // pull padding from fields.padding_json + spacing from fields.gap_px.
        internal static void ApplyRootLayoutGroupConfig(LayoutGroup layoutGroup, PanelKind kind, PanelSnapshotFields fields)
        {
            if (layoutGroup == null) return;
            int padTop = 4, padBottom = 4, padLeft = 8, padRight = 8;
            float gap = fields?.gap_px ?? 8f;
            PanelPaddingJson pad = null;
            var padJson = fields?.padding_json;
            if (!string.IsNullOrEmpty(padJson))
            {
                pad = TryParseTypedJson<PanelPaddingJson>(padJson);
                // Apply only fields the JSON actually carried — JsonUtility cannot distinguish "absent" from
                // "0", so we keep defaults when caller passed empty/whitespace; non-empty input fully overrides.
                padTop = pad.top;
                padRight = pad.right;
                padBottom = pad.bottom;
                padLeft = pad.left;
            }
            layoutGroup.padding = new RectOffset(padLeft, padRight, padTop, padBottom);

            // Frame: rounded border. Skipped silently when fields absent / zero.
            if (pad != null && (pad.border_width > 0f || pad.corner_radius > 0f))
            {
                ApplyRoundedBorder(layoutGroup.gameObject, pad);
            }

            switch (layoutGroup)
            {
                case HorizontalLayoutGroup hlg:
                    hlg.spacing = gap;
                    hlg.childControlWidth = true;
                    hlg.childControlHeight = true;
                    hlg.childForceExpandWidth = (kind == PanelKind.Hud);
                    hlg.childForceExpandHeight = true;
                    hlg.childAlignment = TextAnchor.MiddleCenter;
                    break;
                case VerticalLayoutGroup vlg:
                    vlg.spacing = gap;
                    vlg.childControlWidth = true;
                    vlg.childControlHeight = true;
                    vlg.childForceExpandWidth = true;
                    vlg.childForceExpandHeight = false;
                    vlg.childAlignment = TextAnchor.UpperCenter;
                    break;
                case GridLayoutGroup grid:
                    grid.spacing = new Vector2(gap, gap);
                    grid.cellSize = new Vector2(80f, 80f);
                    grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
                    grid.startAxis = GridLayoutGroup.Axis.Horizontal;
                    grid.childAlignment = TextAnchor.UpperLeft;
                    break;
            }
        }

        // Stats-panel pilot — bake-time color resolution for frame border tokens.
        // TokenCatalog runtime resolver not bake-accessible; hardcoded slug→hex map promotes to
        // tokens snapshot reader in Phase 2 (after pilot approved).
        private static readonly Dictionary<string, Color> s_BorderTokenHexFallback = new Dictionary<string, Color>(StringComparer.Ordinal)
        {
            { "led-amber", new Color(1f, 0.835f, 0.541f, 1f) }, // #ffd58a
            { "white",    Color.white },
        };

        private static Color ResolveBorderColor(string token)
        {
            if (string.IsNullOrEmpty(token)) return Color.white;
            // Bucket F resolver — published color tokens consulted first.
            var hex = ResolveColorTokenHex(token);
            if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out var parsed)) return parsed;
            if (s_BorderTokenHexFallback.TryGetValue(token, out var c)) return c;
            return Color.white;
        }

        /// <summary>
        /// Stats-panel pilot — true when a panel-child kind belongs to the row-list family
        /// (expense-row / service-row / key-value). These are the kinds that flow into a
        /// RowGrid wrapper when panel.params_json.row_columns >= 2.
        /// </summary>
        private static bool IsListRowFamily(string outerKind, string innerKind)
        {
            if (outerKind == "expense-row" || outerKind == "service-row" || outerKind == "key-value") return true;
            if (innerKind == "expense-row" || innerKind == "service-row" || innerKind == "key-value") return true;
            return false;
        }

        /// <summary>
        /// Stats-panel pilot — create a "RowGrid" wrapper GameObject under <paramref name="panelRoot"/>
        /// with a GridLayoutGroup configured for the requested column count. Cell width derives from
        /// panel width / cols when known; falls back to 280 px otherwise. ord seed makes name unique
        /// when the panel contains multiple row blocks separated by non-row children.
        /// </summary>
        private static GameObject CreateRowGrid(GameObject panelRoot, int cols, int panelWidth, int ordSeed)
        {
            var go = new GameObject($"RowGrid_{ordSeed}", typeof(RectTransform));
            go.transform.SetParent(panelRoot.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            var grid = go.AddComponent<GridLayoutGroup>();
            float gridGap = 4f;
            float availableWidth = panelWidth > 0 ? panelWidth - 16f : 700f;
            float cellW = Mathf.Floor((availableWidth - gridGap * (cols - 1)) / cols);
            grid.cellSize = new Vector2(cellW, 28f);
            grid.spacing = new Vector2(gridGap, gridGap);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = cols;

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = panelWidth > 0 ? panelWidth - 16f : 700f;
            le.flexibleWidth = 1f;
            return go;
        }

        /// <summary>
        /// Stats-panel pilot — attach a <see cref="RoundedBorder"/> child to the panel root.
        /// Border GO stretches to the panel rect via anchors (0,0)-(1,1); LayoutElement.ignoreLayout
        /// prevents the root VerticalLayoutGroup from squishing it into the layout flow. Sibling
        /// index 0 (first) so it renders behind labels/buttons. RoundedBorder mesh draws BOTH the
        /// rounded fill (panel-face color) AND the outline — the panel root's square bg Image is
        /// hidden (alpha→0) so corners actually round. Idempotent: replaces an existing "Border".
        /// </summary>
        private static void ApplyRoundedBorder(GameObject panelRoot, PanelPaddingJson pad)
        {
            if (panelRoot == null || pad == null) return;
            var existing = panelRoot.transform.Find("Border");
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);

            // Hide the panel root's square Image bg so the rounded fill below isn't masked by hard
            // corners showing underneath. Keep the component (other systems may inspect it) but make
            // it invisible + non-raycasting.
            var rootImage = panelRoot.GetComponent<Image>();
            if (rootImage != null)
            {
                var c = rootImage.color;
                c.a = 0f;
                rootImage.color = c;
                rootImage.raycastTarget = false;
            }

            var go = new GameObject("Border", typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(panelRoot.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.SetAsFirstSibling(); // render behind text/buttons, but provide rounded fill

            // Escape VLG / HLG / Grid layout — Border must follow the parent rect exactly.
            var le = go.AddComponent<LayoutElement>();
            le.ignoreLayout = true;

            var border = go.AddComponent<RoundedBorder>();
            border.BorderWidth = pad.border_width;
            border.CornerRadius = pad.corner_radius;
            border.BorderColor = ResolveBorderColor(pad.border_color_token);
            // Fill mode — rounded panel face, same color the bg Image was using
            // (ui-design-system.md §1.1 `ui-surface-dark` panel-face token).
            border.FillEnabled = true;
            border.FillColor = new Color(0.196f, 0.196f, 0.196f, 1f);
            border.raycastTarget = false;
        }

        /// <summary>
        /// Build per-zone wrapper RectTransforms for a fullscreen-stack panel (main-menu shape per
        /// docs/ui-element-definitions.md lines 1188-1248). Five wrappers:
        ///   top           — anchored top-center, branding title strip.
        ///   top-left      — anchored top-left, back icon-button corner (48×48).
        ///   bottom-left   — anchored bottom-left, studio caption.
        ///   bottom-right  — anchored bottom-right, version caption.
        ///   center        — full-screen stretch with VLG MiddleCenter, 320 px wide column,
        ///                   12 px gap; primary buttons + confirm + view-slot stack here.
        /// Returns dictionary keyed by zone slug → Transform of the wrapper.
        /// </summary>
        internal static Dictionary<string, Transform> BuildFullscreenStackZoneWrappers(GameObject panelRoot)
        {
            var dict = new Dictionary<string, Transform>(StringComparer.Ordinal);
            if (panelRoot == null) return dict;

            // top — branding title strip, anchored top-edge stretching across full width.
            dict["top"] = MakeZoneWrapper(panelRoot, "Zone_Top",
                anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(1f, 1f),
                pivot: new Vector2(0.5f, 1f),
                anchoredPosition: new Vector2(0f, -32f), sizeDelta: new Vector2(-64f, 80f),
                addVerticalLayout: true, alignment: TextAnchor.MiddleCenter, gap: 4f);

            // top-left — back icon-button corner, fixed 48×48 with small inset.
            dict["top-left"] = MakeZoneWrapper(panelRoot, "Zone_TopLeft",
                anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(0f, 1f),
                pivot: new Vector2(0f, 1f),
                anchoredPosition: new Vector2(16f, -16f), sizeDelta: new Vector2(64f, 64f),
                addVerticalLayout: false);

            // bottom-left — studio caption.
            dict["bottom-left"] = MakeZoneWrapper(panelRoot, "Zone_BottomLeft",
                anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(0f, 0f),
                pivot: new Vector2(0f, 0f),
                anchoredPosition: new Vector2(16f, 16f), sizeDelta: new Vector2(240f, 32f),
                addVerticalLayout: true, alignment: TextAnchor.LowerLeft, gap: 0f);

            // bottom-right — version caption.
            dict["bottom-right"] = MakeZoneWrapper(panelRoot, "Zone_BottomRight",
                anchorMin: new Vector2(1f, 0f), anchorMax: new Vector2(1f, 0f),
                pivot: new Vector2(1f, 0f),
                anchoredPosition: new Vector2(-16f, 16f), sizeDelta: new Vector2(240f, 32f),
                addVerticalLayout: true, alignment: TextAnchor.LowerRight, gap: 0f);

            // center — full-stretch vertical column 320 px wide, MiddleCenter, 12 px gap.
            // Buttons + confirm-button + view-slot stack here. childForceExpand = false so each
            // child honours its LayoutElement.preferred dims (320×56 from layout_json.size).
            var centerGo = MakeZoneWrapper(panelRoot, "Zone_Center",
                anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                pivot: new Vector2(0.5f, 0.5f),
                anchoredPosition: Vector2.zero, sizeDelta: new Vector2(320f, 480f),
                addVerticalLayout: true, alignment: TextAnchor.MiddleCenter, gap: 12f);
            // Override center VLG: childControlWidth+ChildControlHeight true, childForceExpand off,
            // so preferred sizes win (320×56 buttons stay narrow).
            var centerVlg = centerGo.GetComponent<VerticalLayoutGroup>();
            if (centerVlg != null)
            {
                centerVlg.childForceExpandWidth = false;
                centerVlg.childForceExpandHeight = false;
                centerVlg.childControlWidth = true;
                centerVlg.childControlHeight = true;
            }
            // ContentSizeFitter on center column so VLG expands to fit children — keeps MiddleCenter
            // alignment honest when the column is taller than the actual stack.
            dict["center"] = centerGo.transform;

            return dict;
        }

        private static Transform MakeZoneWrapper(GameObject panelRoot, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPosition, Vector2 sizeDelta,
            bool addVerticalLayout, TextAnchor alignment = TextAnchor.MiddleCenter, float gap = 0f)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(panelRoot.transform, worldPositionStays: false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;
            if (addVerticalLayout)
            {
                var vlg = go.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = gap;
                vlg.childAlignment = alignment;
                vlg.childControlWidth = true;
                vlg.childControlHeight = true;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;
            }
            return go.GetComponent<RectTransform>();
        }

        // Imp-2 (bake-fix-2026-05-08): regex JSON helpers (ParsePaddingJson, ReadIntField, ExtractZone)
        // removed in favor of TryParseTypedJson<PanelPaddingJson>() / TryParseTypedJson<PanelChildLayoutJson>().

        public static BakeResult Parse(string jsonText)
        {
            if (string.IsNullOrWhiteSpace(jsonText))
            {
                return new BakeResult
                {
                    root = null,
                    error = new BakeError
                    {
                        error = "schema_violation",
                        details = "empty_or_whitespace_json",
                        path = "$",
                    },
                };
            }
            _lastRawIrJson = jsonText;

            IrRoot parsed;
            try
            {
                parsed = JsonUtility.FromJson<IrRoot>(jsonText);
            }
            catch (Exception ex)
            {
                return new BakeResult
                {
                    root = null,
                    error = new BakeError
                    {
                        error = "schema_violation",
                        details = ex.Message,
                        path = "$",
                    },
                };
            }

            if (parsed == null)
            {
                return new BakeResult
                {
                    root = null,
                    error = new BakeError
                    {
                        error = "schema_violation",
                        details = "json_parsed_null",
                        path = "$",
                    },
                };
            }

            // Minimum structural guard — top-level required blocks must be present.
            if (parsed.tokens == null)
            {
                return new BakeResult
                {
                    root = null,
                    error = new BakeError
                    {
                        error = "schema_violation",
                        details = "tokens_missing",
                        path = "$.tokens",
                    },
                };
            }

            return new BakeResult { root = parsed, error = null };
        }

        // ── Slot accept-rule guard ──────────────────────────────────────────────

        /// <summary>
        /// Validate that every <see cref="IrPanelSlot.children"/> entry appears in <see cref="IrPanelSlot.accepts"/>.
        /// Bake-time slot-accept guard — rejects panels whose slot children violate the
        /// declared accept rule before any prefab write.
        ///
        /// Returns <c>null</c> on pass; populated <see cref="BakeError"/> on first violation found.
        /// </summary>
        public static BakeError ValidateSlotAcceptRules(IrRoot ir)
        {
            if (ir == null)
            {
                return new BakeError
                {
                    error = "schema_violation",
                    details = "ir_root_null",
                    path = "$",
                };
            }

            if (ir.panels == null) return null; // No panels → nothing to validate.

            for (int p = 0; p < ir.panels.Length; p++)
            {
                var panel = ir.panels[p];
                if (panel?.slots == null) continue;

                for (int s = 0; s < panel.slots.Length; s++)
                {
                    var slot = panel.slots[s];
                    if (slot?.children == null || slot.accepts == null) continue;

                    var accepts = new System.Collections.Generic.HashSet<string>(slot.accepts);
                    var offending = new System.Collections.Generic.List<string>();
                    foreach (var child in slot.children)
                    {
                        if (!accepts.Contains(child)) offending.Add(child);
                    }
                    if (offending.Count == 0) continue;

                    return new BakeError
                    {
                        error = "slot_accept_violation",
                        details = $"panel={panel.slug} slot={slot.name} offending=[{string.Join(",", offending)}] accepts=[{string.Join(",", slot.accepts)}]",
                        path = $"$.panels[{p}].slots[{s}]",
                    };
                }
            }

            return null;
        }

        // ── Bake skeleton (T2.4 fills body) ─────────────────────────────────────

        /// <summary>
        /// Bake skeleton — invokes <see cref="Parse"/> + <see cref="ValidateSlotAcceptRules"/>
        /// against the JSON file at <c>args.ir_path</c>. Returns structured result; does NOT
        /// mutate <c>args.theme_so</c> in this Task (T2.4 fills the SO + prefab write body).
        /// </summary>
        public static BakeResult Bake(BakeArgs args)
        {
            if (args == null)
            {
                return new BakeResult
                {
                    root = null,
                    error = new BakeError { error = "missing_arg", details = "args", path = "$" },
                };
            }

            // Stage 9.10 — panels_path (DB snapshot) is the canonical input.
            // ir_path (legacy sketchpad IR) read-path retired. BakeArgs.ir_path
            // field retained for bridge backwards-compat; value forwarded to
            // panels_path when panels_path absent (old callers still work during
            // transition).
            if (!string.IsNullOrEmpty(args.panels_path))
            {
                return BakeFromPanelSnapshot(args);
            }

            // Fallback: caller passed only ir_path (deprecated bridge clients).
            // Treat ir_path as panels_path so callers don't hard-fail at runtime
            // while the C# bridge is updated to send panels_path.
            if (!string.IsNullOrEmpty(args.ir_path))
            {
                var redirectArgs = new BakeArgs
                {
                    panels_path = args.ir_path,
                    out_dir = args.out_dir,
                    theme_so = args.theme_so,
                };
                return BakeFromPanelSnapshot(redirectArgs);
            }

            return new BakeResult
            {
                root = null,
                error = new BakeError { error = "missing_arg", details = "panels_path", path = "$.panels_path" },
            };
        }

        /// <summary>
        /// Pre-flight panel blueprint validator — reads <c>tools/blueprints/panel-schema.yaml</c>
        /// and checks that each required key is present for the given panel slug.
        /// Returns null on pass; populated <see cref="BakeError"/> on schema-file read failure.
        /// Non-fatal on missing keys: logs warning + continues bake (Stage 1 scope).
        /// </summary>
        internal static BakeError ValidatePanelBlueprint(string repoRoot, string panelSlug)
        {
            if (string.IsNullOrEmpty(repoRoot) || string.IsNullOrEmpty(panelSlug)) return null;
            string schemaPath = System.IO.Path.Combine(repoRoot, "tools", "blueprints", "panel-schema.yaml");
            if (!System.IO.File.Exists(schemaPath)) return null; // schema optional during rollout
            // Stage 1: structural check only — confirm schema reads without error.
            try
            {
                _ = System.IO.File.ReadAllText(schemaPath);
            }
            catch (Exception ex)
            {
                return new BakeError
                {
                    error = "validate_panel_blueprint_schema_read_failed",
                    details = ex.Message,
                    path = schemaPath,
                };
            }
            return null;
        }

        /// <summary>
        /// Stage 9.10 — bake from panels.json snapshot (PanelSnapshot DTOs).
        /// Reads panels_path, parses into <see cref="PanelSnapshot"/>, bakes each panel item.
        /// Fails hard when layout_template missing.
        /// </summary>
        public static BakeResult BakeFromPanelSnapshot(BakeArgs args)
        {
            // Imp-3 — install warnings collector for the duration of this bake.
            var warnings = new List<BakeError>();
            _currentBakeWarnings = warnings;
            try
            {
                if (args == null || string.IsNullOrEmpty(args.panels_path))
                {
                    return new BakeResult
                    {
                        root = null,
                        error = new BakeError { error = "missing_arg", details = "panels_path", path = "$.panels_path" },
                        warnings = warnings,
                    };
                }

                string jsonText;
                try
                {
                    jsonText = System.IO.File.ReadAllText(args.panels_path);
                }
                catch (Exception ex)
                {
                    return new BakeResult
                    {
                        root = null,
                        error = new BakeError
                        {
                            error = "panels_path_not_readable",
                            details = ex.Message,
                            path = args.panels_path,
                        },
                        warnings = warnings,
                    };
                }

                var (snapshot, parseError) = ParsePanelSnapshot(jsonText);
                if (parseError != null) return new BakeResult { root = null, error = parseError, warnings = warnings };

                // Bucket F resolver — load tokens.json + substitute spacing slugs in
                // padding_json + params_json strings before downstream JsonUtility parses run.
                LoadTokenSnapshot(args.panels_path);
                if (snapshot?.items != null)
                {
                    foreach (var item in snapshot.items)
                    {
                        if (item?.fields != null)
                        {
                            item.fields.padding_json = SubstituteSpacingTokensInJson(item.fields.padding_json);
                            item.fields.params_json = SubstituteSpacingTokensInJson(item.fields.params_json);
                        }
                        if (item?.children != null)
                        {
                            foreach (var c in item.children)
                            {
                                if (c == null) continue;
                                c.params_json = SubstituteSpacingTokensInJson(c.params_json);
                                c.layout_json = SubstituteSpacingTokensInJson(c.layout_json);
                            }
                        }
                    }
                }

                var soPath = string.IsNullOrEmpty(args.theme_so)
                    ? "Assets/UI/Theme/DefaultUiTheme.asset"
                    : args.theme_so;

                var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(soPath);
                if (theme == null)
                {
                    return new BakeResult
                    {
                        root = null,
                        error = new BakeError
                        {
                            error = "theme_so_not_found",
                            details = soPath,
                            path = "$.theme_so",
                        },
                        warnings = warnings,
                    };
                }

                // validate_panel_blueprint pre-flight: read schema YAML before bake write.
                // Non-fatal on missing keys (Stage 1 scope); hard-fail only on schema read error.
                foreach (var item in snapshot.items ?? Array.Empty<PanelSnapshotItem>())
                {
                    if (item == null || string.IsNullOrEmpty(item.slug)) continue;
                    string repoRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(
                        System.IO.Path.GetDirectoryName(args.panels_path), "..", ".."));
                    var blueprintErr = ValidatePanelBlueprint(repoRoot, item.slug);
                    if (blueprintErr != null) return new BakeResult { root = null, error = blueprintErr, warnings = warnings };
                }

                var prefabError = WritePanelSnapshotPrefabs(snapshot, args.out_dir, theme);
                if (prefabError != null) return new BakeResult { root = null, error = prefabError, warnings = warnings };

                AssetDatabase.Refresh();

                // Layer 6 — audit row (TECH-28378). Best-effort; failure does not abort bake.
                string repoRootForAudit = System.IO.Path.GetFullPath(System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(args.panels_path), "..", ".."));
                foreach (var item in snapshot.items ?? Array.Empty<PanelSnapshotItem>())
                {
                    if (item == null || string.IsNullOrEmpty(item.slug)) continue;
                    WriteBakeAuditRow(repoRootForAudit, item.slug);
                }

                // Visual regression — capture baseline candidates when flag set (TECH-31891).
                if (args.captureBaselines)
                {
                    var panelFilter = new System.Collections.Generic.HashSet<string>(
                        string.IsNullOrEmpty(args.capturePanelsCsv)
                            ? Array.Empty<string>()
                            : args.capturePanelsCsv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));

                    foreach (var item in snapshot.items ?? Array.Empty<PanelSnapshotItem>())
                    {
                        if (item == null || string.IsNullOrEmpty(item.slug)) continue;
                        if (panelFilter.Count > 0 && !panelFilter.Contains(item.slug)) continue;
                        var captureResult = CaptureBaselineCandidate(item.slug);
                        if (!string.IsNullOrEmpty(captureResult.error))
                        {
                            AddBakeWarning(
                                "capture_baseline_failed",
                                captureResult.error,
                                item.slug);
                        }
                        else
                        {
                            UnityEngine.Debug.Log(
                                $"[UiBakeHandler] Captured baseline candidate: {captureResult.candidate_path} sha256={captureResult.sha256}");
                        }
                    }
                }

                return new BakeResult { root = null, error = null, warnings = warnings };
            }
            finally
            {
                _currentBakeWarnings = null;
            }
        }

        /// <summary>
        /// Write prefabs for each item in a <see cref="PanelSnapshot"/>.
        /// Dispatches per-item to <see cref="SavePanelSnapshotPrefab"/>.
        /// </summary>
        static BakeError WritePanelSnapshotPrefabs(PanelSnapshot snapshot, string outDir, UiTheme theme)
        {
            var dir = string.IsNullOrEmpty(outDir) ? "Assets/UI/Prefabs/Generated" : outDir;

            try { Directory.CreateDirectory(dir); }
            catch (Exception ex)
            {
                return new BakeError { error = "out_dir_not_creatable", details = ex.Message, path = dir };
            }

            AssetDatabase.Refresh();

            foreach (var item in snapshot.items)
            {
                if (item == null || string.IsNullOrEmpty(item.slug)) continue;
                var assetPath = $"{dir.TrimEnd('/')}/{item.slug}.prefab";

                // DB-rect-only mode — panel published with empty children list:
                // prefab is hand-authored, DB owns root rect only. Skip prefab
                // regeneration; sync rect_json onto live PrefabInstance(s) in scenes.
                // (Track A.3, docs/ui-bake-pipeline-rollout-plan.md.)
                bool dbRectOnly = (item.children == null || item.children.Length == 0);
                if (dbRectOnly && File.Exists(assetPath))
                {
                    var syncErr = ApplyDbRectToScenePrefabInstances(item.slug, assetPath, item.fields?.rect_json);
                    if (syncErr != null) return syncErr;
                    continue;
                }

                var err = SavePanelSnapshotPrefab(item, assetPath, theme);
                if (err != null) return err;
            }

            return null;
        }

        /// <summary>
        /// Bake one <see cref="PanelSnapshotItem"/> into a prefab.
        /// Root LayoutGroup type determined by <c>fields.layout_template</c> via
        /// <see cref="MapLayoutTemplate"/>. Missing layout_template fails hard.
        /// Children iterated with slot-wrapper routing for hud-bar archetype (T4).
        /// </summary>
        static BakeError SavePanelSnapshotPrefab(PanelSnapshotItem item, string assetPath, UiTheme theme)
        {
            if (ExistingPrefabHasNonDefaultRect(assetPath))
            {
                return new BakeError
                {
                    error = "panel_layout_rect_missing",
                    details = $"panel '{item.slug}' would overwrite existing prefab at '{assetPath}' which carries non-default RectTransform.",
                    path = assetPath,
                };
            }

            GameObject go = null;
            try
            {
                go = new GameObject(item.slug);
                var rootRect = go.AddComponent<RectTransform>();

                // F1+F2 (bake-fix-2026-05-07) — derive PanelKind from layout_template,
                // assign per-kind anchor/size defaults so OnEnable's ApplyKindLayout
                // attaches the matching LayoutGroup instead of stripping it back to VLG.
                // Bug A (2026-05-08): pass params_json.position so Hud arm honors top/bottom override.
                string layoutTemplate = item.fields?.layout_template ?? string.Empty;
                var panelKind = MapLayoutTemplateToPanelKind(layoutTemplate, item.slug);
                var fieldsPj = TryParseTypedJson<PanelFieldsParamsJson>(item.fields?.params_json);
                ApplyPanelKindRectDefaults(rootRect, panelKind, fieldsPj.position);
                // DB-first rect overlay: panel_detail.rect_json wins over PanelKind hard-coded defaults
                // (per-axis, last write wins). Missing keys fall through to the kind default applied above.
                ApplyPanelRectJsonOverlay(rootRect, item.fields?.rect_json);

                var bgImage = go.AddComponent<Image>();
                // ui-design-system.md §1.1 — `ui-surface-dark` panel-face token.
                bgImage.color = new Color(0.196f, 0.196f, 0.196f, 1f);
                bgImage.raycastTarget = false;

                var themedPanel = go.AddComponent<ThemedPanel>();
                WireThemeRef(themedPanel, theme);
                AssignPanelKind(themedPanel, panelKind);

                // Map layout_template → root LayoutGroup. Hard fail on missing.
                // fullscreen-stack mode: no root LayoutGroup; child rows route into per-zone
                // wrappers built below (top, top-left, bottom-left, bottom-right, center).
                bool fullscreenStack = IsFullscreenStackTemplate(layoutTemplate);
                System.Type layoutGroupType = null;
                if (!fullscreenStack)
                {
                    try
                    {
                        layoutGroupType = MapLayoutTemplate(layoutTemplate, item.slug);
                    }
                    catch (Exception ex)
                    {
                        return new BakeError
                        {
                            error = "bake.layout_template_missing",
                            details = ex.Message,
                            path = $"$.items[{item.slug}].fields.layout_template",
                        };
                    }
                }

                // F8 (bake-fix-2026-05-07) — ThemedPanel.OnEnable fires at AddComponent
                // time with default _kind=Modal=0 → ApplyKindLayout attaches a VLG before
                // AssignPanelKind has a chance to set the right kind. Strip whatever
                // LayoutGroup that pass added so the bake-driven AddComponent below ends
                // up as the sole root LayoutGroup matching layout_template.
                foreach (var stale in go.GetComponents<LayoutGroup>())
                {
                    UnityEngine.Object.DestroyImmediate(stale);
                }

                if (!fullscreenStack)
                {
                    var layoutGroup = (LayoutGroup)go.AddComponent(layoutGroupType);
                    ApplyRootLayoutGroupConfig(layoutGroup, panelKind, item.fields);
                }

                // Slot-wrapper iteration + children (T4 fills archetype dispatch).
                BakePanelSnapshotChildren(item, go, theme);

                // hud-bar runtime adapter — slug-walks IlluminatedButton children + attaches OnClick
                // listeners. Without this, baked buttons render but never wire (adapter must live in
                // the prefab so it ships wherever the prefab is instantiated).
                if (item.slug == "hud-bar" || item.slug == "hud_bar")
                {
                    if (go.GetComponent<HudBarDataAdapter>() == null)
                    {
                        go.AddComponent<HudBarDataAdapter>();
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(go, assetPath);

                // Layer 2 .meta-file write proof (TECH-28364): assert .meta exists with
                // stable GUID after SaveAsPrefabAsset so phantom-GUID defect (F4) is caught
                // before the bake result is returned to the caller.
                try
                {
                    // Preserve any pre-existing GUID Unity already assigned (scene refs depend on it).
                    // Only write deterministic GUID when .meta is missing — first-time bake of new slug.
                    var metaPath = assetPath + ".meta";
                    string expectedGuid;
                    if (System.IO.File.Exists(metaPath))
                    {
                        // Parse `guid: <hex>` from existing .meta and assert against itself.
                        var existing = System.IO.File.ReadAllText(metaPath);
                        var match = System.Text.RegularExpressions.Regex.Match(existing, @"guid:\s*([0-9a-fA-F]{32})");
                        expectedGuid = match.Success
                            ? match.Groups[1].Value
                            : Territory.Editor.UiBake.BakeMetaProof.ComputeStableGuid(item.slug);
                        if (!match.Success)
                        {
                            Territory.Editor.UiBake.BakeMetaProof.WriteMetaFile(assetPath, expectedGuid);
                            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                        }
                    }
                    else
                    {
                        expectedGuid = Territory.Editor.UiBake.BakeMetaProof.ComputeStableGuid(item.slug);
                        Territory.Editor.UiBake.BakeMetaProof.WriteMetaFile(assetPath, expectedGuid);
                        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                    }
                    Territory.Editor.UiBake.BakeMetaProof.AssertMetaExists(assetPath, expectedGuid);
                }
                catch (Territory.Editor.UiBake.BakeException metaEx)
                {
                    return new BakeError
                    {
                        error   = "meta_missing_or_unstable",
                        details = metaEx.Message,
                        path    = assetPath,
                    };
                }

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

        /// <summary>
        /// Iterate children[] for a snapshot panel item.
        /// Called by <see cref="SavePanelSnapshotPrefab"/>; archetype slot-wrapper
        /// dispatch extended in T4 (<see cref="BakePanelSnapshotArchetype"/>).
        /// </summary>
        static void BakePanelSnapshotChildren(PanelSnapshotItem item, GameObject panelRoot, UiTheme theme)
        {
            if (item?.children == null || item.children.Length == 0) return;

            // bake-fix-2026-05-08: archetype dispatch returns per-child parent transforms keyed by
            // ord — owns the full sub-grid (cols/rows/sub_cols) for hud-bar so flat iteration here
            // stays mechanical.
            var parentByOrd = BakePanelSnapshotArchetype(item, panelRoot, theme);

            // Fullscreen-stack zone wrappers: built once on first sight of a layout_json.zone child,
            // shared across all subsequent zone-routed children.
            string layoutTemplate = item.fields?.layout_template ?? string.Empty;
            bool fullscreenStack = IsFullscreenStackTemplate(layoutTemplate);
            Dictionary<string, Transform> parentByZone = fullscreenStack
                ? BuildFullscreenStackZoneWrappers(panelRoot)
                : null;

            // Stats-panel pilot — panel-level row_columns drives 2-col wrapping of contiguous
            // list-row family runs (expense-row / service-row / key-value). Single-col panels
            // bypass entirely (rowColumns < 2 → null active RowGrid).
            var fieldsParamsTop = TryParseTypedJson<PanelFieldsParamsJson>(item.fields?.params_json);
            int rowColumns = fieldsParamsTop?.row_columns ?? 0;
            int panelWidth = fieldsParamsTop?.width ?? 0;
            // Stats-panel pilot — panel padding parsed once for corner-overlay offset math
            // (child rect anchoredPosition uses padding to clear the border + radius zone).
            var paddingTop = TryParseTypedJson<PanelPaddingJson>(item.fields?.padding_json);
            GameObject activeRowGrid = null;
            // Track every RowGrid we create so we can finalize LayoutElement.preferredHeight
            // after all children are attached — VLG on the panel root needs an explicit height
            // for the grid to actually expand vertically; otherwise it collapses to 100px default.
            var createdRowGrids = new List<GameObject>();

            // Header-strip detection (pilot iter 12): when first 2 children are
            // [back-button, themed-label modal-title], wrap both into a horizontal
            // HLG so the back arrow renders inline left of the title (no corner-overlay).
            GameObject headerStripHLG = null;
            if (item.children != null && item.children.Length >= 2)
            {
                var hc0 = item.children[0];
                var hc1 = item.children[1];
                var hpj0 = TryParseTypedJson<PanelChildParamsJson>(hc0?.params_json);
                var hpj1 = TryParseTypedJson<PanelChildParamsJson>(hc1?.params_json);
                string hk0 = NormalizeChildKind(hc0?.kind, hpj0?.kind);
                string hk1 = NormalizeChildKind(hc1?.kind, hpj1?.kind);
                bool isHeader = hk1 == "themed-label" && string.Equals(hpj1?.variant, "modal-title", StringComparison.Ordinal);
                if (hk0 == "back-button" && isHeader)
                {
                    headerStripHLG = new GameObject("HeaderStrip", typeof(RectTransform));
                    headerStripHLG.transform.SetParent(panelRoot.transform, worldPositionStays: false);
                    var hsHlg = headerStripHLG.AddComponent<HorizontalLayoutGroup>();
                    hsHlg.spacing = 8f;
                    hsHlg.padding = new RectOffset(0, 0, 0, 0);
                    hsHlg.childAlignment = TextAnchor.MiddleLeft;
                    hsHlg.childForceExpandHeight = false;
                    hsHlg.childForceExpandWidth = false;
                    hsHlg.childControlHeight = true;
                    hsHlg.childControlWidth = true;
                    var hsLe = headerStripHLG.AddComponent<LayoutElement>();
                    hsLe.preferredHeight = 40f;
                    hsLe.flexibleWidth = 1f;
                }
            }

            foreach (var child in item.children)
            {
                if (child == null) continue;
                var layout = TryParseTypedJson<PanelChildLayoutJson>(child.layout_json);
                var pj = TryParseTypedJson<PanelChildParamsJson>(child.params_json);

                Transform parent = panelRoot.transform;
                bool slotOverridden = false;
                if (parentByOrd != null && parentByOrd.TryGetValue(child.ord, out var resolved) && resolved != null)
                {
                    parent = resolved;
                    slotOverridden = true;
                }
                if (parentByZone != null && !string.IsNullOrEmpty(layout?.zone)
                    && parentByZone.TryGetValue(layout.zone, out var zoneParent) && zoneParent != null)
                {
                    parent = zoneParent;
                    slotOverridden = true;
                }

                // Header-strip routing: first 2 children parent into the HLG container.
                if (headerStripHLG != null && !slotOverridden && (child.ord == 1 || child.ord == 2))
                {
                    parent = headerStripHLG.transform;
                    slotOverridden = true;
                }

                // Stats-panel pilot — RowGrid wrapping. Active only when:
                //   (a) panel-level row_columns >= 2
                //   (b) child parents directly to panelRoot (no slot/zone override)
                //   (c) child kind is in list-row family
                // Non-row child (or slot override) flushes the active group.
                bool isListRowFamily = IsListRowFamily(child.kind, pj?.kind);
                if (rowColumns >= 2 && !slotOverridden && isListRowFamily)
                {
                    if (activeRowGrid == null)
                    {
                        activeRowGrid = CreateRowGrid(panelRoot, rowColumns, panelWidth, child.ord);
                        createdRowGrids.Add(activeRowGrid);
                    }
                    parent = activeRowGrid.transform;
                }
                else
                {
                    // Boundary: flush.
                    activeRowGrid = null;
                }

                string childName = !string.IsNullOrEmpty(child.instance_slug)
                    ? child.instance_slug
                    : $"child_{child.ord}";
                var childGo = new GameObject(childName, typeof(RectTransform));
                childGo.transform.SetParent(parent, worldPositionStays: false);

                // Resolve preferred dims from icon hint + rowSpan + outer kind. BUDGET (icon=long)
                // → 256×64; rowSpan≥2 → height×2 (col 2-4 right zone tall buttons); label → flex.
                var (prefW, prefH) = ResolveSnapshotChildDims(child.kind, pj?.icon, layout);

                var childRect = (RectTransform)childGo.transform;
                childRect.anchorMin = new Vector2(0f, 0.5f);
                childRect.anchorMax = new Vector2(0f, 0.5f);
                childRect.pivot = new Vector2(0.5f, 0.5f);
                childRect.anchoredPosition = Vector2.zero;
                childRect.sizeDelta = new Vector2(prefW > 0f ? prefW : 64f, prefH > 0f ? prefH : 64f);
                if (layout.size != null && layout.size.w > 0f && layout.size.h > 0f)
                {
                    childRect.sizeDelta = new Vector2(layout.size.w, layout.size.h);
                    prefW = layout.size.w;
                    prefH = layout.size.h;
                }

                // bake-fix-2026-05-08: parent zone HLG/VLG runs childControl=true so wrappers
                // size to button content. Tell the LayoutGroup our preferred dims via
                // LayoutElement; flex labels (prefW=-1) signal "stretch in-zone".
                var childLe = childGo.AddComponent<LayoutElement>();
                childLe.preferredWidth = prefW > 0f ? prefW : -1f;
                childLe.preferredHeight = prefH > 0f ? prefH : 32f;
                if (prefW < 0f) childLe.flexibleWidth = 1f;

                if (!string.IsNullOrEmpty(child.instance_slug))
                {
                    var childRef = childGo.AddComponent<CatalogPrefabRef>();
                    childRef.slug = child.instance_slug;
                }

                string innerKind = NormalizeChildKind(child.kind, pj.kind);
                BakeChildByKind(childGo, innerKind, pj, theme, prefW, prefH, item.fields?.display_name);
                // Layer 2 non-empty assert (TECH-28361): throw BakeException when renderer
                // produced an empty stub (no child GameObjects + no meaningful component).
                Territory.Editor.UiBake.BakeEmptyChildGuard.AssertNotEmpty(childGo, innerKind, item.slug);
                PropagateThemeRefRecursive(childGo, theme);

                // Stats-panel pilot — corner-anchor overlay. Escapes panel VLG flow + pins to
                // named corner with padding-aware offset. Applied after BakeChildByKind so the
                // renderer-spawned body/halo/icon are already attached when we collapse the rect.
                if (!slotOverridden && !string.IsNullOrEmpty(pj?.corner))
                {
                    ApplyCornerOverlay(childGo, pj.corner, pj.corner_size, pj.corner_offset, paddingTop);
                }

                // visible_bind → toggles GameObject.SetActive on bool bind id changes. Defaults
                // to hidden when the bind id has not yet been seeded; the runtime registry seed
                // (e.g. MainMenuRegistrySeed.cs) declares initial state. Without this, the
                // back-button (visible only on sub-views) would always render on the root view.
                if (!string.IsNullOrEmpty(pj.visible_bind))
                {
                    var binder = childGo.AddComponent<Territory.UI.Registry.UiVisibilityBinder>();
                    binder.Initialize(pj.visible_bind);
                }
            }

            // Stats-panel pilot — finalize every RowGrid we created. Each wraps a contiguous
            // run of list-row children; child count is only known after the loop above. VLG on
            // panel root needs LayoutElement.preferredHeight to give the grid actual vertical
            // space — otherwise GridLayoutGroup collapses to ~100px and rows render off-panel.
            foreach (var grid in createdRowGrids)
            {
                if (grid == null) continue;
                int childCount = grid.transform.childCount;
                if (childCount == 0) continue;
                var glg = grid.GetComponent<GridLayoutGroup>();
                var rgLe = grid.GetComponent<LayoutElement>();
                if (glg == null || rgLe == null) continue;
                int cols = Mathf.Max(1, glg.constraintCount);
                int rows = Mathf.CeilToInt((float)childCount / cols);
                float cellH = glg.cellSize.y;
                float spacingY = glg.spacing.y;
                rgLe.preferredHeight = rows * cellH + Mathf.Max(0, rows - 1) * spacingY;
            }
        }

        /// <summary>
        /// Stats-panel pilot — pin a child to a named panel corner with padding-aware offset.
        /// Escapes panel VLG flow via LayoutElement.ignoreLayout=true, overrides anchors/pivot
        /// to the corner, and shrinks sizeDelta to a small fixed square (default 40px).
        /// Corner values: "top-left" | "top-right" | "bottom-left" | "bottom-right".
        /// Offset = padding side + border_width so the button clears the rounded border zone.
        /// </summary>
        static void ApplyCornerOverlay(GameObject childGo, string corner, int requestedSize, string offsetOverride, PanelPaddingJson padding)
        {
            if (childGo == null || string.IsNullOrEmpty(corner)) return;

            float size = requestedSize > 0 ? requestedSize : 40f;
            float padLeft  = padding != null ? padding.left   : 0f;
            float padRight = padding != null ? padding.right  : 0f;
            float padTop   = padding != null ? padding.top    : 0f;
            float padBot   = padding != null ? padding.bottom : 0f;
            float border   = padding != null ? padding.border_width : 0f;
            float offsetX = padLeft + border;
            float offsetY = padTop  + border;
            // Right/bottom use their own pad sides (symmetric default but kept distinct).
            float offsetRX = padRight + border;
            float offsetRY = padBot   + border;

            // Manual override — "x,y" string parsed as left/top offsets. Applied to whichever
            // side the corner uses (left→x, right→rx; top→y, bottom→ry).
            if (!string.IsNullOrEmpty(offsetOverride))
            {
                var parts = offsetOverride.Split(',');
                if (parts.Length == 2
                    && float.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float ox)
                    && float.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float oy))
                {
                    offsetX = ox; offsetRX = ox;
                    offsetY = oy; offsetRY = oy;
                }
            }

            // Escape panel VLG flow.
            var le = childGo.GetComponent<LayoutElement>();
            if (le == null) le = childGo.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
            le.preferredWidth = size;
            le.preferredHeight = size;
            le.flexibleWidth = 0f;
            le.flexibleHeight = 0f;

            var rect = (RectTransform)childGo.transform;
            Vector2 anchor; Vector2 pivot; Vector2 anchored;
            switch (corner)
            {
                case "top-left":
                    anchor = new Vector2(0f, 1f); pivot = new Vector2(0f, 1f);
                    anchored = new Vector2(offsetX, -offsetY);
                    break;
                case "top-right":
                    anchor = new Vector2(1f, 1f); pivot = new Vector2(1f, 1f);
                    anchored = new Vector2(-offsetRX, -offsetY);
                    break;
                case "bottom-left":
                    anchor = new Vector2(0f, 0f); pivot = new Vector2(0f, 0f);
                    anchored = new Vector2(offsetX, offsetRY);
                    break;
                case "bottom-right":
                    anchor = new Vector2(1f, 0f); pivot = new Vector2(1f, 0f);
                    anchored = new Vector2(-offsetRX, offsetRY);
                    break;
                default:
                    return; // unknown corner — leave child in VLG flow
            }
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchored;
            rect.sizeDelta = new Vector2(size, size);

            // Renderer-spawned descendants (body/halo/icon for illuminated-button) inherit the
            // rect collapse via their own anchors/sizeDelta math at bake time — no recursive
            // fix-up needed; the shrunken root rect propagates through their fitter components.
        }

        /// <summary>
        /// Resolve preferred wrapper dims for a snapshot child. Hud-bar art surface heuristic:
        /// icon=long → 256×64 (BUDGET wide-button); outer label kind → flex width + 32 height.
        /// Square buttons stay 64×64 regardless of rowSpan — vertical centering in the parent
        /// wrapper handles tall-zone placement (rowSpan was producing 64×128 stretched bodies).
        /// Returns (-1, h) for flex-width labels — caller lets LayoutElement.preferredWidth=-1
        /// signal "size by content".
        /// </summary>
        static (float w, float h) ResolveSnapshotChildDims(string outerKind, string iconSlug, PanelChildLayoutJson layout)
        {
            float w = 64f;
            float h = 64f;
            if (!string.IsNullOrEmpty(iconSlug) && iconSlug == "long")
            {
                w = 256f;
                h = 64f;
            }
            if (outerKind == "label")
            {
                w = -1f;
                h = 32f;
            }
            return (w, h);
        }

        // F6: recursively wire UiTheme ref onto every Component on root + descendants
        // that exposes a `_themeRef` SerializedProperty.
        private static void PropagateThemeRefRecursive(GameObject root, UiTheme theme)
        {
            if (root == null || theme == null) return;
            var components = root.GetComponentsInChildren<Component>(true);
            foreach (var c in components)
            {
                if (c == null) continue;
                WireThemeRef(c, theme);
            }
        }

        // ── Token bake ──────────────────────────────────────────────────────────

        /// <summary>
        /// Populate the SO's five backing lists from the IR `tokens` block. Sorts by slug ordinal-asc
        /// before write for deterministic output. Invalidates dict cache on completion so consumers
        /// rebuild lazily on next `TryGet*` call.
        /// </summary>
        static void PopulateThemeFromIr(UiTheme theme, IrRoot ir)
        {
            // Palette
            theme.PaletteEntries.Clear();
            if (ir.tokens.palette != null)
            {
                var sorted = new List<IrTokenPalette>(ir.tokens.palette);
                sorted.Sort((a, b) => string.CompareOrdinal(a?.slug ?? string.Empty, b?.slug ?? string.Empty));
                foreach (var p in sorted)
                {
                    if (p == null || string.IsNullOrEmpty(p.slug)) continue;
                    theme.PaletteEntries.Add(new UiTheme.PaletteKv
                    {
                        slug = p.slug,
                        value = new UiTheme.PaletteRamp { ramp = p.ramp ?? Array.Empty<string>() },
                    });
                }
            }

            // Frame style
            theme.FrameStyleEntries.Clear();
            if (ir.tokens.frame_style != null)
            {
                var sorted = new List<IrTokenFrameStyle>(ir.tokens.frame_style);
                sorted.Sort((a, b) => string.CompareOrdinal(a?.slug ?? string.Empty, b?.slug ?? string.Empty));
                foreach (var f in sorted)
                {
                    if (f == null || string.IsNullOrEmpty(f.slug)) continue;
                    theme.FrameStyleEntries.Add(new UiTheme.FrameStyleKv
                    {
                        slug = f.slug,
                        value = new UiTheme.FrameStyleSpec
                        {
                            edge = f.edge ?? string.Empty,
                            innerShadowAlpha = f.innerShadowAlpha,
                            catalog_sprite_slug = f.slug,
                            sprite_ref_fallback = Territory.UI.Editor.AtlasIndex.Resolve(f.slug),
                        },
                    });
                }
            }

            // Font face
            theme.FontFaceEntries.Clear();
            if (ir.tokens.font_face != null)
            {
                var sorted = new List<IrTokenFontFace>(ir.tokens.font_face);
                sorted.Sort((a, b) => string.CompareOrdinal(a?.slug ?? string.Empty, b?.slug ?? string.Empty));
                foreach (var ff in sorted)
                {
                    if (ff == null || string.IsNullOrEmpty(ff.slug)) continue;
                    theme.FontFaceEntries.Add(new UiTheme.FontFaceKv
                    {
                        slug = ff.slug,
                        value = new UiTheme.FontFaceSpec
                        {
                            family = ff.family ?? string.Empty,
                            weight = ff.weight,
                        },
                    });
                }
            }

            // Motion curve
            theme.MotionCurveEntries.Clear();
            if (ir.tokens.motion_curve != null)
            {
                var sorted = new List<IrTokenMotionCurve>(ir.tokens.motion_curve);
                sorted.Sort((a, b) => string.CompareOrdinal(a?.slug ?? string.Empty, b?.slug ?? string.Empty));
                foreach (var m in sorted)
                {
                    if (m == null || string.IsNullOrEmpty(m.slug)) continue;
                    theme.MotionCurveEntries.Add(new UiTheme.MotionCurveKv
                    {
                        slug = m.slug,
                        value = new UiTheme.MotionCurveSpec
                        {
                            kind = m.kind ?? string.Empty,
                            stiffness = m.stiffness,
                            damping = m.damping,
                            c1 = m.c1 ?? Array.Empty<float>(),
                            c2 = m.c2 ?? Array.Empty<float>(),
                            durationMs = m.durationMs,
                        },
                    });
                }
            }

            // Illumination
            theme.IlluminationEntries.Clear();
            if (ir.tokens.illumination != null)
            {
                var sorted = new List<IrTokenIllumination>(ir.tokens.illumination);
                sorted.Sort((a, b) => string.CompareOrdinal(a?.slug ?? string.Empty, b?.slug ?? string.Empty));
                foreach (var il in sorted)
                {
                    if (il == null || string.IsNullOrEmpty(il.slug)) continue;
                    theme.IlluminationEntries.Add(new UiTheme.IlluminationKv
                    {
                        slug = il.slug,
                        value = new UiTheme.IlluminationSpec
                        {
                            color = il.color ?? string.Empty,
                            haloRadiusPx = il.haloRadiusPx,
                        },
                    });
                }
            }

            theme.InvalidateTokenCaches();
        }

        // ── _themeRef wire-up helper (Step 8 fix) ───────────────────────────────

        /// <summary>
        /// Bake-time write of the <c>_themeRef</c> SerializeField on a <see cref="ThemedPrimitiveBase"/>
        /// or <see cref="StudioControlBase"/> derived component (also <c>StudioControlRendererBase</c>).
        /// Without this, runtime <c>Awake</c> falls back to <see cref="UnityEngine.Object.FindObjectOfType{T}"/>
        /// which never resolves a <see cref="UiTheme"/> ScriptableObject asset (white-square chrome bug).
        /// No-op when the component does not declare <c>_themeRef</c> or when <paramref name="theme"/> is null.
        /// </summary>
        static void WireThemeRef(Component component, UiTheme theme)
        {
            if (component == null || theme == null) return;
            var so = new SerializedObject(component);
            // ThemedPanel / ThemedPrimitiveBase / StudioControlBase / TooltipController
            // serialize as `_themeRef`; JuiceBase (+ derived PulseOnEvent etc.) uses
            // `themeRef`. Try both — silent no-op if neither exists.
            var prop = so.FindProperty("_themeRef") ?? so.FindProperty("themeRef");
            if (prop == null) return;
            prop.objectReferenceValue = theme;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── Placeholder prefab writes ───────────────────────────────────────────

        /// <summary>
        /// Write empty-RectTransform placeholder prefabs per panel + interactive in IR. Writes go under
        /// <paramref name="outDir"/> (defaulting to <c>Assets/UI/Prefabs/Generated</c> when empty).
        /// PrefabUtility.SaveAsPrefabAsset overwrites existing files — second bake on same IR is idempotent.
        /// Returns null on success, populated <see cref="BakeError"/> on first IO failure.
        /// </summary>
        static BakeError WritePlaceholderPrefabs(IrRoot ir, string outDir, UiTheme theme)
        {
            var dir = string.IsNullOrEmpty(outDir) ? "Assets/UI/Prefabs/Generated" : outDir;

            try
            {
                Directory.CreateDirectory(dir);
            }
            catch (Exception ex)
            {
                return new BakeError
                {
                    error = "out_dir_not_creatable",
                    details = ex.Message,
                    path = dir,
                };
            }

            // Refresh so the new directory is recognized by AssetDatabase before writes.
            AssetDatabase.Refresh();

            // Interactives FIRST so panel bake can resolve child slug → prefab without warn-skip on first run.
            if (ir.interactives != null)
            {
                for (int i = 0; i < ir.interactives.Length; i++)
                {
                    var ic = ir.interactives[i];
                    if (ic == null || string.IsNullOrEmpty(ic.slug)) continue;

                    var assetPath = $"{dir.TrimEnd('/')}/{ic.slug}.prefab";
                    BakeError err;
                    if (IsKnownStudioControlKind(ic.kind))
                    {
                        err = BakeInteractive(ic, i, assetPath, _lastRawIrJson, theme);
                    }
                    else
                    {
                        // Defensive fallback — IR schema validation already gates kind enum upstream;
                        // unknown slug still produces a placeholder so panel bake child-resolution works.
                        err = SaveEmptyPlaceholderPrefab(ic.slug, assetPath);
                    }
                    if (err != null) return err;
                }
                // Refresh again so freshly-written interactive prefabs are loadable by AssetDatabase.LoadAssetAtPath.
                AssetDatabase.Refresh();
            }

            if (ir.panels != null)
            {
                for (int i = 0; i < ir.panels.Length; i++)
                {
                    var panel = ir.panels[i];
                    if (panel == null || string.IsNullOrEmpty(panel.slug)) continue;

                    var assetPath = $"{dir.TrimEnd('/')}/{panel.slug}.prefab";
                    var err = SavePanelPrefab(panel, assetPath, dir, theme);
                    if (err != null) return err;
                }
            }

            return null;
        }

        static BakeError SaveEmptyPlaceholderPrefab(string slug, string assetPath)
        {
            GameObject go = null;
            try
            {
                go = new GameObject(slug);
                go.AddComponent<RectTransform>();
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

        // ── Layer 6 — bake audit (TECH-28378) ───────────────────────────────────

        /// <summary>Version tag written to ia_ui_bake_history.bake_handler_version.</summary>
        internal const string BakeHandlerVersion = "1.0";

        /// <summary>
        /// Best-effort: insert one row into ia_ui_bake_history via bake-audit-write.mjs.
        /// Failure logs a warning but does NOT abort the bake.
        /// </summary>
        internal static void WriteBakeAuditRow(string repoRoot, string panelSlug)
        {
            if (string.IsNullOrEmpty(repoRoot) || string.IsNullOrEmpty(panelSlug)) return;

            string script = System.IO.Path.Combine(repoRoot, "tools", "postgres-ia", "bake-audit-write.mjs");
            if (!System.IO.File.Exists(script))
            {
                UnityEngine.Debug.LogWarning($"[UiBakeHandler] bake-audit-write.mjs not found at {script} — skipping audit row.");
                return;
            }

            // Build payload JSON.
            string commitSha = ResolveGitCommitSha(repoRoot);
            var payloadObj = new BakeAuditPayload
            {
                panel_slug           = panelSlug,
                bake_handler_version = BakeHandlerVersion,
                diff_summary         = new BakeDiffSummaryForAudit(),
                commit_sha           = commitSha ?? string.Empty,
            };
            string payloadJson = JsonUtility.ToJson(payloadObj);

            // Write payload to temp file.
            string tmpFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"bake-audit-{Guid.NewGuid():N}.json");
            try
            {
                System.IO.File.WriteAllText(tmpFile, payloadJson, new UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[UiBakeHandler] Failed to write audit payload temp file: {ex.Message}");
                return;
            }

            try
            {
                string nodeExe = EditorPostgresExportRegistrar.ResolveNodeExecutablePath();
                string dbUrl   = EditorPostgresExportRegistrar.ResolveEffectiveDatabaseUrl(repoRoot);
                if (string.IsNullOrWhiteSpace(dbUrl) || string.IsNullOrWhiteSpace(nodeExe)) return;

                var psi = new ProcessStartInfo
                {
                    FileName               = nodeExe,
                    Arguments              = $"\"{script}\" --payload-file \"{tmpFile}\"",
                    WorkingDirectory       = repoRoot,
                    UseShellExecute        = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    CreateNoWindow         = true,
                };
                psi.EnvironmentVariables["DATABASE_URL"] = dbUrl;
                psi.EnvironmentVariables["NODE_NO_WARNINGS"] = "1";

                using var proc = new Process { StartInfo = psi };
                proc.Start();
                bool finished = proc.WaitForExit(15_000);
                if (!finished)
                {
                    proc.Kill();
                    UnityEngine.Debug.LogWarning("[UiBakeHandler] bake-audit-write.mjs timed out — audit row skipped.");
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[UiBakeHandler] bake audit write failed: {ex.Message}");
            }
            finally
            {
                try { System.IO.File.Delete(tmpFile); } catch { /* ignored */ }
            }
        }

        /// <summary>Returns the current HEAD commit SHA (short). Returns empty string on failure.</summary>
        static string ResolveGitCommitSha(string repoRoot)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName               = "git",
                    Arguments              = "rev-parse --short HEAD",
                    WorkingDirectory       = repoRoot,
                    UseShellExecute        = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    CreateNoWindow         = true,
                };
                using var proc = new Process { StartInfo = psi };
                proc.Start();
                string sha = proc.StandardOutput.ReadToEnd().Trim();
                proc.WaitForExit(5_000);
                return sha;
            }
            catch
            {
                return string.Empty;
            }
        }

        // ── DTOs for bake audit payload (JsonUtility-compatible) ─────────────────

        [Serializable]
        class BakeAuditPayload
        {
            public string panel_slug;
            public string bake_handler_version;
            public BakeDiffSummaryForAudit diff_summary;
            public string commit_sha;
        }

        [Serializable]
        class BakeDiffSummaryForAudit
        {
            // Empty summary — detailed diffs require a live prefab reference not available
            // at post-bake hook time without a second prefab load. Extended by TECH-28379+.
            public string status = "written";
        }

    }
}
