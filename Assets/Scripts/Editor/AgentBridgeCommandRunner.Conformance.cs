using System;
using System.Collections.Generic;
using System.IO;
using Territory.UI;
using Territory.UI.StudioControls;
using Territory.UI.StudioControls.Renderers;
using Territory.UI.Themed;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Stage 12 Step 14.3 — `claude_design_conformance` bridge kind (read-only).
// Diffs a baked prefab/scene root against IR slug intent + UiTheme-resolved
// values. Returns one row per check. Eight check kinds:
//   palette_ramp        — ThemedButton/Label/Panel `_paletteSlug` resolves
//                         to a UiTheme palette whose ramp stop matches the
//                         actual rendered Color on the bound Image / TMP_Text.
//   font_face           — ThemedLabel `_fontFaceSlug` resolves to a UiTheme
//                         font face (family + weight emitted as expected/actual).
//   frame_style         — ThemedButton/Panel `_frameStyleSlug` resolves to a
//                         UiTheme frame style (edge + innerShadowAlpha).
//   panel_kind          — ThemedPanel `_kind` matches IR panel.kind for the
//                         panel whose slug == GameObject name.
//   caption             — IR panel.slot.labels[i] for child slug C matches the
//                         TMP_Text.m_text of a ThemedLabel under that child.
//   contrast_ratio      — Per ThemedLabel under a ThemedPanel parent, WCAG
//                         contrast between text color + panel fill color ≥ 4.5.
//   frame_sprite_bound  — Step 16.5 procedural border check: ThemedPanel has
//                         all 4 border strip Image children (BorderTop/Bottom/
//                         Left/Right) wired via SerializedObject refs.
//   button_state_block  — Step 16.5: IlluminatedButton has companion renderer
//                         that handles pointer hover/press (lighten halo,
//                         darken body); detected via IlluminatedButtonRenderer
//                         presence on same GameObject.

public static partial class AgentBridgeCommandRunner
{
    const float ConformanceColorEpsilon = 1.5f / 255f;
    const float WcagBodyTextThreshold = 4.5f;

    static void RunClaudeDesignConformance(string repoRoot, string commandId, string requestJson)
    {
        if (!TryParseConformanceParams(requestJson, out var dto, out string parseErr))
        {
            TryFinalizeFailed(repoRoot, commandId, parseErr);
            return;
        }

        if (string.IsNullOrWhiteSpace(dto.ir_path))
        {
            TryFinalizeFailed(repoRoot, commandId, "params_invalid:ir_path");
            return;
        }
        if (string.IsNullOrWhiteSpace(dto.theme_so))
        {
            TryFinalizeFailed(repoRoot, commandId, "params_invalid:theme_so");
            return;
        }
        bool hasPrefab = !string.IsNullOrWhiteSpace(dto.prefab_path);
        bool hasScene = !string.IsNullOrWhiteSpace(dto.scene_root_path);
        if (hasPrefab == hasScene)
        {
            TryFinalizeFailed(repoRoot, commandId, "params_invalid:exactly_one_of_prefab_path_or_scene_root_path");
            return;
        }

        // Load IR JSON.
        ConformanceIrRootDto ir;
        try
        {
            string absIr = Path.IsPathRooted(dto.ir_path)
                ? dto.ir_path
                : Path.Combine(repoRoot, dto.ir_path);
            string irText = File.ReadAllText(absIr);
            ir = JsonUtility.FromJson<ConformanceIrRootDto>(irText);
        }
        catch (Exception ex)
        {
            TryFinalizeFailed(repoRoot, commandId, $"ir_read_failed:{ex.GetType().Name}:{ex.Message}");
            return;
        }
        if (ir == null)
        {
            TryFinalizeFailed(repoRoot, commandId, "ir_parse_returned_null");
            return;
        }

        // Load UiTheme.
        var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(dto.theme_so);
        if (theme == null)
        {
            TryFinalizeFailed(repoRoot, commandId, $"theme_so_not_found:{dto.theme_so}");
            return;
        }

        // Resolve target root.
        Transform root;
        GameObject loadedPrefabContents = null;
        string targetKind;
        string targetPath;
        try
        {
            if (hasPrefab)
            {
                loadedPrefabContents = PrefabUtility.LoadPrefabContents(dto.prefab_path);
                if (loadedPrefabContents == null)
                {
                    TryFinalizeFailed(repoRoot, commandId, $"prefab_load_failed:{dto.prefab_path}");
                    return;
                }
                root = loadedPrefabContents.transform;
                targetKind = "prefab";
                targetPath = dto.prefab_path;
            }
            else
            {
                var sceneRoot = GameObject.Find(dto.scene_root_path);
                if (sceneRoot == null)
                {
                    TryFinalizeFailed(repoRoot, commandId, $"scene_root_not_found:{dto.scene_root_path}");
                    return;
                }
                root = sceneRoot.transform;
                targetKind = "scene";
                targetPath = dto.scene_root_path;
            }

            // IR side index.
            var irPanels = BuildIrPanelIndex(ir);
            var irLabelIndex = BuildIrLabelIndex(ir);

            // Pre-pass — Themed* components paint their bound Image / TMP_Text from
            // the resolved theme inside Awake at runtime, but PrefabUtility.LoadPrefabContents
            // does NOT invoke Awake, so serialized colors stay at the default
            // (white) state authored at bake time. We invoke ApplyTheme on every
            // Themed* descendant so the second-pass conformance checks measure
            // the runtime-equivalent surface instead of the pre-paint serialized
            // state. Order: panels first (paint background + slot graph compose),
            // then labels + buttons (depend on final tmpText / image refs).
            ApplyThemePrePass(root, theme);

            var rows = new List<AgentBridgeConformanceRowDto>();
            WalkConformanceNode(root, root, theme, ir, irPanels, irLabelIndex, rows);

            int failCount = 0;
            for (int i = 0; i < rows.Count; i++) if (!rows[i].pass) failCount++;

            var resultDto = new AgentBridgeConformanceResultDto
            {
                ir_path = dto.ir_path,
                theme_so = dto.theme_so,
                target_kind = targetKind,
                target_path = targetPath,
                row_count = rows.Count,
                fail_count = failCount,
                rows = rows.ToArray(),
            };

            var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "claude_design_conformance");
            resp.claude_design_conformance_result = resultDto;
            CompleteOrFail(repoRoot, commandId, JsonUtility.ToJson(resp, true));
        }
        catch (Exception ex)
        {
            TryFinalizeFailed(repoRoot, commandId, $"conformance_threw:{ex.GetType().Name}:{ex.Message}");
        }
        finally
        {
            if (loadedPrefabContents != null)
            {
                PrefabUtility.UnloadPrefabContents(loadedPrefabContents);
            }
        }
    }

    static void ApplyThemePrePass(Transform root, UiTheme theme)
    {
        // Two passes — panels first (slot reparenting + bg paint), then labels +
        // buttons (palette paint on bound TMP / Image, which may have been
        // reparented by the panel pass).
        var panels = root.GetComponentsInChildren<ThemedPanel>(includeInactive: true);
        for (int i = 0; i < panels.Length; i++)
        {
            try { panels[i].ApplyTheme(theme); }
            catch (Exception ex) { Debug.LogWarning($"[Conformance] ThemedPanel.ApplyTheme threw on '{panels[i].gameObject.name}': {ex.Message}"); }
        }
        var labels = root.GetComponentsInChildren<ThemedLabel>(includeInactive: true);
        for (int i = 0; i < labels.Length; i++)
        {
            try { labels[i].ApplyTheme(theme); }
            catch (Exception ex) { Debug.LogWarning($"[Conformance] ThemedLabel.ApplyTheme threw on '{labels[i].gameObject.name}': {ex.Message}"); }
        }
        var buttons = root.GetComponentsInChildren<ThemedButton>(includeInactive: true);
        for (int i = 0; i < buttons.Length; i++)
        {
            try { buttons[i].ApplyTheme(theme); }
            catch (Exception ex) { Debug.LogWarning($"[Conformance] ThemedButton.ApplyTheme threw on '{buttons[i].gameObject.name}': {ex.Message}"); }
        }
    }

    static void WalkConformanceNode(
        Transform node,
        Transform root,
        UiTheme theme,
        ConformanceIrRootDto ir,
        Dictionary<string, ConformanceIrPanelDto> irPanels,
        Dictionary<string, string> irLabelIndex,
        List<AgentBridgeConformanceRowDto> rows)
    {
        string nodePath = ComputeRelativePath(node, root);

        var components = node.gameObject.GetComponents<Component>();
        ThemedPanel themedPanel = null;
        Image backgroundImage = null;
        IlluminatedButton illuminatedButton = null;
        foreach (var comp in components)
        {
            if (comp == null) continue;
            switch (comp)
            {
                case ThemedButton btn:
                    CheckThemedButton(nodePath, btn, theme, rows);
                    break;
                case ThemedLabel lbl:
                    CheckThemedLabel(nodePath, lbl, theme, rows);
                    CheckCaptionMatch(nodePath, node, lbl, irLabelIndex, rows);
                    CheckContrastUnderPanel(nodePath, node, lbl, theme, rows);
                    break;
                case ThemedPanel pnl:
                    themedPanel = pnl;
                    break;
                case IlluminatedButton ibtn:
                    illuminatedButton = ibtn;
                    break;
            }
        }
        if (themedPanel != null)
        {
            CheckThemedPanel(nodePath, node, themedPanel, theme, irPanels, rows);
            CheckFrameSpriteBound(nodePath, themedPanel, rows);
        }
        if (illuminatedButton != null)
        {
            CheckButtonStateBlock(nodePath, node, illuminatedButton, rows);
        }

        for (int i = 0; i < node.childCount; i++)
        {
            WalkConformanceNode(node.GetChild(i), root, theme, ir, irPanels, irLabelIndex, rows);
        }
    }

    // -- ThemedButton checks ----------------------------------------------------

    static void CheckThemedButton(
        string nodePath,
        ThemedButton btn,
        UiTheme theme,
        List<AgentBridgeConformanceRowDto> rows)
    {
        var so = new SerializedObject(btn);
        string paletteSlug = TryReadStringField(so, "_paletteSlug");
        string frameStyleSlug = TryReadStringField(so, "_frameStyleSlug");
        var buttonImageRef = TryReadObjectField(so, "_buttonImage");

        if (!string.IsNullOrEmpty(paletteSlug))
        {
            CheckPaletteRamp(
                nodePath: nodePath,
                componentName: "ThemedButton",
                slug: paletteSlug,
                expectedStopIdx: -1, // ramp[length-1] (lightest)
                bound: buttonImageRef as Component,
                colorPropName: "m_Color",
                theme: theme,
                rows: rows);
        }
        if (!string.IsNullOrEmpty(frameStyleSlug))
        {
            CheckFrameStyle(nodePath, "ThemedButton", frameStyleSlug, theme, rows);
        }
    }

    // -- ThemedLabel checks -----------------------------------------------------

    static void CheckThemedLabel(
        string nodePath,
        ThemedLabel lbl,
        UiTheme theme,
        List<AgentBridgeConformanceRowDto> rows)
    {
        var so = new SerializedObject(lbl);
        string paletteSlug = TryReadStringField(so, "_paletteSlug");
        string fontFaceSlug = TryReadStringField(so, "_fontFaceSlug");
        var tmpRef = TryReadObjectField(so, "_tmpText");

        if (!string.IsNullOrEmpty(paletteSlug))
        {
            CheckPaletteRamp(
                nodePath: nodePath,
                componentName: "ThemedLabel",
                slug: paletteSlug,
                expectedStopIdx: -1,
                bound: tmpRef as Component,
                colorPropName: "m_fontColor",
                theme: theme,
                rows: rows);
        }
        if (!string.IsNullOrEmpty(fontFaceSlug))
        {
            CheckFontFace(nodePath, "ThemedLabel", fontFaceSlug, theme, rows);
        }
    }

    // -- ThemedPanel checks -----------------------------------------------------

    static void CheckThemedPanel(
        string nodePath,
        Transform node,
        ThemedPanel pnl,
        UiTheme theme,
        Dictionary<string, ConformanceIrPanelDto> irPanels,
        List<AgentBridgeConformanceRowDto> rows)
    {
        var so = new SerializedObject(pnl);
        string paletteSlug = TryReadStringField(so, "_paletteSlug");
        var bgImageRef = TryReadObjectField(so, "_backgroundImage");
        int kindEnumIdx = TryReadEnumIndex(so, "_kind");

        if (!string.IsNullOrEmpty(paletteSlug))
        {
            CheckPaletteRamp(
                nodePath: nodePath,
                componentName: "ThemedPanel",
                slug: paletteSlug,
                expectedStopIdx: 1, // ramp[1] = panel-fill shade
                bound: bgImageRef as Component,
                colorPropName: "m_Color",
                theme: theme,
                rows: rows);
        }

        // panel_kind check — only meaningful when GameObject name resolves an IR panel.
        if (irPanels.TryGetValue(node.gameObject.name, out var irPanel))
        {
            string irKind = string.IsNullOrEmpty(irPanel.kind) ? "modal" : irPanel.kind;
            string actualKind = PanelKindEnumIdxToIrName(kindEnumIdx);
            bool pass = string.Equals(irKind, actualKind, StringComparison.Ordinal);
            rows.Add(new AgentBridgeConformanceRowDto
            {
                node_path = nodePath,
                component = "ThemedPanel",
                check_kind = "panel_kind",
                slug = irPanel.slug,
                expected = irKind,
                resolved = irKind,
                actual = actualKind,
                severity = pass ? "info" : "error",
                pass = pass,
                message = pass
                    ? "panel kind matches IR"
                    : $"panel kind mismatch — IR='{irKind}' actual='{actualKind}'",
            });
        }
    }

    // -- Palette ramp diff ------------------------------------------------------

    static void CheckPaletteRamp(
        string nodePath,
        string componentName,
        string slug,
        int expectedStopIdx, // -1 → ramp[length-1]
        Component bound,
        string colorPropName,
        UiTheme theme,
        List<AgentBridgeConformanceRowDto> rows)
    {
        if (!theme.TryGetPalette(slug, out var ramp) || ramp.ramp == null || ramp.ramp.Length == 0)
        {
            rows.Add(new AgentBridgeConformanceRowDto
            {
                node_path = nodePath,
                component = componentName,
                check_kind = "palette_ramp",
                slug = slug,
                expected = "(palette in theme)",
                resolved = "(missing)",
                actual = string.Empty,
                severity = "error",
                pass = false,
                message = $"palette slug '{slug}' not in UiTheme",
            });
            return;
        }

        int idx = expectedStopIdx < 0
            ? ramp.ramp.Length + expectedStopIdx
            : expectedStopIdx;
        if (idx < 0 || idx >= ramp.ramp.Length) idx = ramp.ramp.Length - 1;
        string expectedHex = ramp.ramp[idx];

        if (!ColorUtility.TryParseHtmlString(expectedHex, out var expectedColor))
        {
            rows.Add(new AgentBridgeConformanceRowDto
            {
                node_path = nodePath,
                component = componentName,
                check_kind = "palette_ramp",
                slug = slug,
                expected = expectedHex,
                resolved = "(unparsable hex)",
                actual = string.Empty,
                severity = "error",
                pass = false,
                message = $"theme ramp[{idx}] '{expectedHex}' is not parsable hex",
            });
            return;
        }

        if (bound == null)
        {
            rows.Add(new AgentBridgeConformanceRowDto
            {
                node_path = nodePath,
                component = componentName,
                check_kind = "palette_ramp",
                slug = slug,
                expected = expectedHex,
                resolved = FormatColor(expectedColor),
                actual = "(unbound)",
                severity = "warn",
                pass = false,
                message = $"{componentName} has no bound target component (Image / TMP_Text) — cannot verify rendered color",
            });
            return;
        }

        Color? actualColor = TryReadComponentColor(bound, colorPropName);
        if (!actualColor.HasValue)
        {
            rows.Add(new AgentBridgeConformanceRowDto
            {
                node_path = nodePath,
                component = componentName,
                check_kind = "palette_ramp",
                slug = slug,
                expected = expectedHex,
                resolved = FormatColor(expectedColor),
                actual = "(unreadable)",
                severity = "warn",
                pass = false,
                message = $"could not read color property '{colorPropName}' on {bound.GetType().Name}",
            });
            return;
        }

        bool pass = ColorsApproxEqual(expectedColor, actualColor.Value);
        rows.Add(new AgentBridgeConformanceRowDto
        {
            node_path = nodePath,
            component = componentName,
            check_kind = "palette_ramp",
            slug = slug,
            expected = expectedHex,
            resolved = FormatColor(expectedColor),
            actual = FormatColor(actualColor.Value),
            severity = pass ? "info" : "error",
            pass = pass,
            message = pass
                ? $"ramp[{idx}] resolves and matches"
                : $"ramp[{idx}]={expectedHex} but rendered color differs",
        });
    }

    // -- Font face diff ---------------------------------------------------------

    static void CheckFontFace(
        string nodePath,
        string componentName,
        string slug,
        UiTheme theme,
        List<AgentBridgeConformanceRowDto> rows)
    {
        if (!theme.TryGetFontFace(slug, out var face))
        {
            rows.Add(new AgentBridgeConformanceRowDto
            {
                node_path = nodePath,
                component = componentName,
                check_kind = "font_face",
                slug = slug,
                expected = "(font_face in theme)",
                resolved = "(missing)",
                actual = string.Empty,
                severity = "error",
                pass = false,
                message = $"font_face slug '{slug}' not in UiTheme",
            });
            return;
        }
        rows.Add(new AgentBridgeConformanceRowDto
        {
            node_path = nodePath,
            component = componentName,
            check_kind = "font_face",
            slug = slug,
            expected = $"family='{face.family}' weight={face.weight}",
            resolved = $"family='{face.family}' weight={face.weight}",
            actual = $"family='{face.family}' weight={face.weight}",
            severity = "info",
            pass = true,
            message = "font_face slug resolves (runtime fontAsset binding deferred to Stage 6 catalog)",
        });
    }

    // -- Frame style diff -------------------------------------------------------

    static void CheckFrameStyle(
        string nodePath,
        string componentName,
        string slug,
        UiTheme theme,
        List<AgentBridgeConformanceRowDto> rows)
    {
        if (!theme.TryGetFrameStyle(slug, out var fs))
        {
            rows.Add(new AgentBridgeConformanceRowDto
            {
                node_path = nodePath,
                component = componentName,
                check_kind = "frame_style",
                slug = slug,
                expected = "(frame_style in theme)",
                resolved = "(missing)",
                actual = string.Empty,
                severity = "error",
                pass = false,
                message = $"frame_style slug '{slug}' not in UiTheme",
            });
            return;
        }
        rows.Add(new AgentBridgeConformanceRowDto
        {
            node_path = nodePath,
            component = componentName,
            check_kind = "frame_style",
            slug = slug,
            expected = $"edge='{fs.edge}' innerShadowAlpha={fs.innerShadowAlpha:F3}",
            resolved = $"edge='{fs.edge}' innerShadowAlpha={fs.innerShadowAlpha:F3}",
            actual = $"edge='{fs.edge}' innerShadowAlpha={fs.innerShadowAlpha:F3}",
            severity = "info",
            pass = true,
            message = "frame_style slug resolves (sprite swap deferred)",
        });
    }

    // -- Step 16.5 — frame_sprite_bound (procedural 4-strip border) -------------

    static void CheckFrameSpriteBound(
        string nodePath,
        ThemedPanel pnl,
        List<AgentBridgeConformanceRowDto> rows)
    {
        var so = new SerializedObject(pnl);
        var top = TryReadObjectField(so, "_borderTop") as Image;
        var bot = TryReadObjectField(so, "_borderBottom") as Image;
        var lft = TryReadObjectField(so, "_borderLeft") as Image;
        var rgt = TryReadObjectField(so, "_borderRight") as Image;

        var strips = new[] {
            ("Top", top), ("Bottom", bot), ("Left", lft), ("Right", rgt),
        };

        int present = 0;
        int visibleAlpha = 0;
        int nonZeroSize = 0;
        int ignoreLayout = 0;
        var failures = new List<string>();

        foreach (var (name, img) in strips)
        {
            if (img == null) { failures.Add($"{name}:missing"); continue; }
            present++;
            if (img.color.a > 0f) visibleAlpha++; else failures.Add($"{name}:alpha=0");
            var rt = img.rectTransform;
            var size = rt != null ? rt.rect.size : Vector2.zero;
            if (size.x > 0f || size.y > 0f) nonZeroSize++; else failures.Add($"{name}:size=0");
            var le = img.GetComponent<LayoutElement>();
            if (le != null && le.ignoreLayout) ignoreLayout++; else failures.Add($"{name}:layout-not-ignored");
        }

        bool pass = present == 4 && visibleAlpha == 4 && nonZeroSize == 4 && ignoreLayout == 4;
        rows.Add(new AgentBridgeConformanceRowDto
        {
            node_path = nodePath,
            component = "ThemedPanel",
            check_kind = "frame_sprite_bound",
            slug = TryReadStringField(so, "_frameStyleSlug") ?? string.Empty,
            expected = "4 strips wired + visible alpha + non-zero size + LayoutElement.ignoreLayout=true",
            resolved = "4 strips wired + visible alpha + non-zero size + LayoutElement.ignoreLayout=true",
            actual = $"wired={present}/4 alpha={visibleAlpha}/4 size={nonZeroSize}/4 ignoreLayout={ignoreLayout}/4",
            severity = pass ? "info" : "error",
            pass = pass,
            message = pass
                ? "all 4 border strips wired + visible + sized + layout-exempt"
                : $"strip defects: {string.Join(",", failures)} — re-bake panel",
        });
    }

    // -- Step 16.5 — button_state_block (hover/press wiring) --------------------

    static void CheckButtonStateBlock(
        string nodePath,
        Transform node,
        IlluminatedButton btn,
        List<AgentBridgeConformanceRowDto> rows)
    {
        var renderer = node.GetComponent<IlluminatedButtonRenderer>();
        bool pass = renderer != null;
        rows.Add(new AgentBridgeConformanceRowDto
        {
            node_path = nodePath,
            component = "IlluminatedButton",
            check_kind = "button_state_block",
            slug = btn.Slug ?? string.Empty,
            expected = "IlluminatedButtonRenderer present (handles hover/press)",
            resolved = "IlluminatedButtonRenderer present (handles hover/press)",
            actual = pass ? "renderer present" : "renderer missing",
            severity = pass ? "info" : "error",
            pass = pass,
            message = pass
                ? "hover/press state block wired via IlluminatedButtonRenderer"
                : "no IlluminatedButtonRenderer — pointer hover/press will not animate",
        });
    }

    // -- Caption label diff -----------------------------------------------------

    static void CheckCaptionMatch(
        string nodePath,
        Transform node,
        ThemedLabel lbl,
        Dictionary<string, string> irLabelIndex,
        List<AgentBridgeConformanceRowDto> rows)
    {
        // Walk up the parent chain looking for a panel with name in IR.
        // Key shape: "{panelSlug}/{childSlug}/{labelIndex}" — collapsed here to
        // "{panelSlug}/{childSlug}". Best-effort: emit info row when childSlug
        // == labeled child name within IR panel.
        Transform cursor = node.parent;
        Transform childCursor = node;
        string panelSlug = null;
        string childSlug = null;
        while (cursor != null)
        {
            if (cursor.GetComponent<ThemedPanel>() != null)
            {
                panelSlug = cursor.gameObject.name;
                childSlug = childCursor.gameObject.name;
                break;
            }
            childCursor = cursor;
            cursor = cursor.parent;
        }
        if (panelSlug == null || childSlug == null) return;

        string indexKey = $"{panelSlug}/{childSlug}";
        if (!irLabelIndex.TryGetValue(indexKey, out string expectedLabel)) return;

        var so = new SerializedObject(lbl);
        var tmpRef = TryReadObjectField(so, "_tmpText");
        string actual = string.Empty;
        if (tmpRef != null)
        {
            var tmpSo = new SerializedObject(tmpRef);
            var textProp = tmpSo.FindProperty("m_text");
            if (textProp != null && textProp.propertyType == SerializedPropertyType.String)
            {
                actual = textProp.stringValue ?? string.Empty;
            }
        }
        bool pass = string.Equals(expectedLabel, actual, StringComparison.Ordinal);
        rows.Add(new AgentBridgeConformanceRowDto
        {
            node_path = nodePath,
            component = "ThemedLabel",
            check_kind = "caption",
            slug = indexKey,
            expected = expectedLabel,
            resolved = expectedLabel,
            actual = actual,
            severity = pass ? "info" : "error",
            pass = pass,
            message = pass
                ? "caption matches IR slot label"
                : $"caption differs from IR slot label — expected '{expectedLabel}' actual '{actual}'",
        });
    }

    // -- Contrast ratio diff ----------------------------------------------------

    static void CheckContrastUnderPanel(
        string nodePath,
        Transform node,
        ThemedLabel lbl,
        UiTheme theme,
        List<AgentBridgeConformanceRowDto> rows)
    {
        ThemedPanel ancestorPanel = null;
        Transform cursor = node.parent;
        while (cursor != null && ancestorPanel == null)
        {
            ancestorPanel = cursor.GetComponent<ThemedPanel>();
            cursor = cursor.parent;
        }
        if (ancestorPanel == null) return;

        string labelSlug = TryReadStringField(new SerializedObject(lbl), "_paletteSlug");
        string panelSlug = TryReadStringField(new SerializedObject(ancestorPanel), "_paletteSlug");
        if (string.IsNullOrEmpty(labelSlug) || string.IsNullOrEmpty(panelSlug)) return;
        if (!theme.TryGetPalette(labelSlug, out var labelRamp) || labelRamp.ramp == null || labelRamp.ramp.Length == 0) return;
        if (!theme.TryGetPalette(panelSlug, out var panelRamp) || panelRamp.ramp == null || panelRamp.ramp.Length == 0) return;

        string textHex = labelRamp.ramp[labelRamp.ramp.Length - 1];
        int panelIdx = panelRamp.ramp.Length >= 2 ? 1 : 0;
        string fillHex = panelRamp.ramp[panelIdx];
        if (!ColorUtility.TryParseHtmlString(textHex, out var textColor)) return;
        if (!ColorUtility.TryParseHtmlString(fillHex, out var fillColor)) return;

        float ratio = ContrastRatio(textColor, fillColor);
        bool pass = ratio >= WcagBodyTextThreshold;
        rows.Add(new AgentBridgeConformanceRowDto
        {
            node_path = nodePath,
            component = "ThemedLabel",
            check_kind = "contrast_ratio",
            slug = $"{labelSlug}@{panelSlug}",
            expected = $">= {WcagBodyTextThreshold:F2}",
            resolved = $"text={textHex} fill={fillHex}",
            actual = ratio.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
            severity = pass ? "info" : "warn",
            pass = pass,
            message = pass
                ? "WCAG body-text contrast met"
                : $"WCAG body-text contrast {ratio:F2} below 4.5",
        });
    }

    // -- IR side index builders -------------------------------------------------

    static Dictionary<string, ConformanceIrPanelDto> BuildIrPanelIndex(ConformanceIrRootDto ir)
    {
        var dict = new Dictionary<string, ConformanceIrPanelDto>(StringComparer.Ordinal);
        if (ir.panels != null)
        {
            foreach (var p in ir.panels)
            {
                if (p == null || string.IsNullOrEmpty(p.slug)) continue;
                dict[p.slug] = p;
            }
        }
        return dict;
    }

    static Dictionary<string, string> BuildIrLabelIndex(ConformanceIrRootDto ir)
    {
        // Key "{panelSlug}/{childSlug}" → label string. Skips slots without
        // labels, and entries whose label is empty.
        var dict = new Dictionary<string, string>(StringComparer.Ordinal);
        if (ir.panels == null) return dict;
        foreach (var p in ir.panels)
        {
            if (p == null || p.slots == null) continue;
            foreach (var slot in p.slots)
            {
                if (slot == null || slot.children == null || slot.labels == null) continue;
                int n = Math.Min(slot.children.Length, slot.labels.Length);
                for (int i = 0; i < n; i++)
                {
                    string c = slot.children[i];
                    string l = slot.labels[i];
                    if (string.IsNullOrEmpty(c) || string.IsNullOrEmpty(l)) continue;
                    dict[$"{p.slug}/{c}"] = l;
                }
            }
        }
        return dict;
    }

    // -- Helpers ----------------------------------------------------------------

    static string TryReadStringField(SerializedObject so, string propName)
    {
        var p = so.FindProperty(propName);
        if (p == null || p.propertyType != SerializedPropertyType.String) return null;
        return p.stringValue;
    }

    static UnityEngine.Object TryReadObjectField(SerializedObject so, string propName)
    {
        var p = so.FindProperty(propName);
        if (p == null || p.propertyType != SerializedPropertyType.ObjectReference) return null;
        return p.objectReferenceValue;
    }

    static int TryReadEnumIndex(SerializedObject so, string propName)
    {
        var p = so.FindProperty(propName);
        if (p == null || p.propertyType != SerializedPropertyType.Enum) return -1;
        return p.enumValueIndex;
    }

    static Color? TryReadComponentColor(Component comp, string propName)
    {
        if (comp == null) return null;
        // Live accessor first — Themed* ApplyTheme writes via property setters,
        // which may not flush to the SerializedObject snapshot until next
        // serialization. Read the runtime color directly when the component
        // exposes a `.color` property (Graphic / TMP_Text / etc).
        try
        {
            switch (comp)
            {
                case TMPro.TMP_Text tmp:
                    return tmp.color;
                case UnityEngine.UI.Graphic gfx:
                    return gfx.color;
            }
        }
        catch
        {
            // fall through to SerializedObject path
        }
        try
        {
            var so = new SerializedObject(comp);
            var p = so.FindProperty(propName);
            if (p == null || p.propertyType != SerializedPropertyType.Color) return null;
            return p.colorValue;
        }
        catch
        {
            return null;
        }
    }

    static bool ColorsApproxEqual(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < ConformanceColorEpsilon
            && Mathf.Abs(a.g - b.g) < ConformanceColorEpsilon
            && Mathf.Abs(a.b - b.b) < ConformanceColorEpsilon
            && Mathf.Abs(a.a - b.a) < ConformanceColorEpsilon;
    }

    static string FormatColor(Color c) =>
        string.Format(System.Globalization.CultureInfo.InvariantCulture,
            "rgba({0:F3},{1:F3},{2:F3},{3:F3})", c.r, c.g, c.b, c.a);

    static string PanelKindEnumIdxToIrName(int idx)
    {
        // Mirrors PanelKind enum order in `ThemedPanel.cs` + IR token in `ir-schema.ts`.
        switch (idx)
        {
            case 0: return "modal";
            case 1: return "screen";
            case 2: return "hud";
            case 3: return "toolbar";
            default: return "(unknown)";
        }
    }

    static float ContrastRatio(Color a, Color b)
    {
        float la = RelativeLuminance(a) + 0.05f;
        float lb = RelativeLuminance(b) + 0.05f;
        return la > lb ? la / lb : lb / la;
    }

    static float RelativeLuminance(Color c)
    {
        float lr = LinearChannel(c.r);
        float lg = LinearChannel(c.g);
        float lb = LinearChannel(c.b);
        return 0.2126f * lr + 0.7152f * lg + 0.0722f * lb;
    }

    static float LinearChannel(float v)
    {
        return v <= 0.03928f
            ? v / 12.92f
            : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);
    }

    // ── Stage 1.4 T1.4.5 — targeted check dispatch ──────────────────────────────

    /// <summary>
    /// <c>claude_design_check</c> bridge command. Dispatches a single targeted conformance check
    /// by <c>check_kind</c>, returns <c>{"check_kind":"...","pass":true/false,"detail":"..."}</c>.
    /// Stage 1.4 (T1.4.5).
    /// </summary>
    static void RunClaudeDesignCheck(string repoRoot, string commandId, string requestJson)
    {
        if (!TryParseDesignCheckParams(requestJson, out var dto, out string parseErr))
        {
            TryFinalizeFailed(repoRoot, commandId, parseErr);
            return;
        }

        if (string.IsNullOrEmpty(dto.check_kind))
        {
            TryFinalizeFailed(repoRoot, commandId, "params_invalid:check_kind");
            return;
        }

        GameObject loadedPrefabContents = null;
        try
        {
            Transform root = null;
            if (!string.IsNullOrEmpty(dto.prefab_path))
            {
                loadedPrefabContents = PrefabUtility.LoadPrefabContents(dto.prefab_path);
                if (loadedPrefabContents == null)
                {
                    TryFinalizeFailed(repoRoot, commandId, $"prefab_load_failed:{dto.prefab_path}");
                    return;
                }
                root = loadedPrefabContents.transform;
            }
            else if (!string.IsNullOrEmpty(dto.scene_root_path))
            {
                var sceneRoot = GameObject.Find(dto.scene_root_path);
                if (sceneRoot == null)
                {
                    TryFinalizeFailed(repoRoot, commandId, $"scene_root_not_found:{dto.scene_root_path}");
                    return;
                }
                root = sceneRoot.transform;
            }
            else
            {
                TryFinalizeFailed(repoRoot, commandId, "params_invalid:require_prefab_path_or_scene_root_path");
                return;
            }

            bool pass;
            string detail;

            switch (dto.check_kind)
            {
                case "frame_visual_present":
                {
                    // Pass when root has a non-null Image sprite (indicating a frame sprite was resolved at bake time).
                    var img = root.GetComponent<Image>();
                    pass = img != null && img.sprite != null;
                    detail = pass ? "Image.sprite is non-null" : "Image.sprite is null or Image component missing";
                    break;
                }
                case "font_asset_bound":
                {
                    // Pass when all TMP_Text components in scope have a non-null font asset.
                    var texts = root.GetComponentsInChildren<TMP_Text>(includeInactive: true);
                    if (texts == null || texts.Length == 0)
                    {
                        pass = false;
                        detail = "no TMP_Text components found in scope";
                        break;
                    }
                    int unbound = 0;
                    for (int i = 0; i < texts.Length; i++)
                    {
                        if (texts[i].font == null) unbound++;
                    }
                    pass = unbound == 0;
                    detail = pass
                        ? $"all {texts.Length} TMP_Text(s) have a bound font asset"
                        : $"{unbound}/{texts.Length} TMP_Text(s) have unbound font asset";
                    break;
                }
                case "spacing_match":
                {
                    // Pass when LayoutGroup.spacing matches dto.expected_spacing within ±1 px tolerance.
                    const float SpacingTolerance = 1f; // ±1 px per Stage 1.4 exit criteria
                    var lg = root.GetComponent<LayoutGroup>();
                    if (lg == null)
                    {
                        pass = false;
                        detail = "no LayoutGroup on root";
                        break;
                    }
                    float actualSpacing = 0f;
                    if (lg is HorizontalOrVerticalLayoutGroup hvlg) actualSpacing = hvlg.spacing;
                    float delta = Mathf.Abs(actualSpacing - dto.expected_spacing);
                    pass = delta <= SpacingTolerance;
                    detail = pass
                        ? $"spacing={actualSpacing:F1} matches expected={dto.expected_spacing:F1} within ±{SpacingTolerance} px"
                        : $"spacing={actualSpacing:F1} differs from expected={dto.expected_spacing:F1} (delta={delta:F1}, tolerance=±{SpacingTolerance} px)";
                    break;
                }
                case "button_states_wired":
                {
                    // Pass when Selectable.transition != None OR colors.normalColor != white (indicates explicit wiring).
                    var sel = root.GetComponent<Selectable>();
                    if (sel == null)
                    {
                        pass = false;
                        detail = "no Selectable component on root";
                        break;
                    }
                    pass = sel.transition != Selectable.Transition.None
                        || sel.colors.normalColor != Color.white;
                    detail = pass
                        ? $"transition={sel.transition} normalColor={sel.colors.normalColor}"
                        : "Selectable has default transition=None and normalColor=white — not wired";
                    break;
                }
                case "illumination_layer_present":
                {
                    // Pass when a ThemedIlluminationLayer component exists as a direct child of root.
                    bool found = false;
                    for (int i = 0; i < root.childCount; i++)
                    {
                        if (root.GetChild(i).GetComponent<ThemedIlluminationLayer>() != null)
                        {
                            found = true;
                            break;
                        }
                    }
                    pass = found;
                    detail = pass
                        ? "ThemedIlluminationLayer found as direct child"
                        : "no ThemedIlluminationLayer direct child found";
                    break;
                }
                default:
                    TryFinalizeFailed(repoRoot, commandId, $"check_kind_unknown:{dto.check_kind}");
                    return;
            }

            string resultJson = $"{{\"check_kind\":\"{dto.check_kind}\",\"pass\":{(pass ? "true" : "false")},\"detail\":\"{EscapeJsonString(detail)}\"}}";
            var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "claude_design_check");
            resp.result_json = resultJson;
            CompleteOrFail(repoRoot, commandId, JsonUtility.ToJson(resp, true));
        }
        catch (Exception ex)
        {
            TryFinalizeFailed(repoRoot, commandId, $"design_check_threw:{ex.GetType().Name}:{ex.Message}");
        }
        finally
        {
            if (loadedPrefabContents != null) PrefabUtility.UnloadPrefabContents(loadedPrefabContents);
        }
    }

    static bool TryParseDesignCheckParams(string requestJson, out DesignCheckParamsDto dto, out string error)
    {
        dto = null;
        error = null;
        if (string.IsNullOrWhiteSpace(requestJson))
        {
            error = "params_invalid: empty request_json";
            return false;
        }
        string paramsJson = ExtractParamsJsonBlockInspect(requestJson);
        if (string.IsNullOrWhiteSpace(paramsJson))
        {
            dto = new DesignCheckParamsDto();
            return true;
        }
        try
        {
            dto = JsonUtility.FromJson<DesignCheckParamsDto>(paramsJson);
        }
        catch (Exception ex)
        {
            error = $"params_invalid: {ex.Message}";
            return false;
        }
        if (dto == null) dto = new DesignCheckParamsDto();
        return true;
    }

    // -- Param parse ------------------------------------------------------------

    static bool TryParseConformanceParams(string requestJson, out ConformanceParamsDto dto, out string error)
    {
        dto = null;
        error = null;
        if (string.IsNullOrWhiteSpace(requestJson))
        {
            error = "params_invalid: empty request_json";
            return false;
        }
        string paramsJson = ExtractParamsJsonBlockInspect(requestJson);
        if (string.IsNullOrWhiteSpace(paramsJson))
        {
            dto = new ConformanceParamsDto();
            return true;
        }
        try
        {
            dto = JsonUtility.FromJson<ConformanceParamsDto>(paramsJson);
        }
        catch (Exception ex)
        {
            error = $"params_invalid: {ex.Message}";
            return false;
        }
        if (dto == null) dto = new ConformanceParamsDto();
        return true;
    }
}

[Serializable]
class ConformanceParamsDto
{
    public string ir_path;
    public string theme_so;
    public string prefab_path;
    public string scene_root_path;
}

/// <summary>Stage 1.4 (T1.4.5) — params bag for <c>claude_design_check</c> targeted check command.</summary>
[Serializable]
class DesignCheckParamsDto
{
    public string check_kind;
    public string prefab_path;
    public string scene_root_path;
    /// <summary>Expected spacing value in px; used by <c>spacing_match</c> check kind.</summary>
    public float expected_spacing;
}

// IR JSON subset needed for conformance scope. JsonUtility cannot deserialize
// the full IR (interactives[].detail is open-ended) — these DTOs intentionally
// drop fields the bridge never reads (motion_curve, illumination, detail).
[Serializable]
class ConformanceIrRootDto
{
    public ConformanceIrTokensDto tokens;
    public ConformanceIrPanelDto[] panels;
}

[Serializable]
class ConformanceIrTokensDto
{
    public ConformanceIrPaletteDto[] palette;
    public ConformanceIrFrameStyleDto[] frame_style;
    public ConformanceIrFontFaceDto[] font_face;
}

[Serializable]
class ConformanceIrPaletteDto
{
    public string slug;
    public string[] ramp;
}

[Serializable]
class ConformanceIrFrameStyleDto
{
    public string slug;
    public string edge;
    public float innerShadowAlpha;
}

[Serializable]
class ConformanceIrFontFaceDto
{
    public string slug;
    public string family;
    public int weight;
}

[Serializable]
class ConformanceIrPanelDto
{
    public string slug;
    public string archetype;
    public string kind;
    public ConformanceIrPanelSlotDto[] slots;
}

[Serializable]
class ConformanceIrPanelSlotDto
{
    public string name;
    public string[] accepts;
    public string[] children;
    public string[] labels;
}

[Serializable]
public class AgentBridgeConformanceResultDto
{
    public string ir_path;
    public string theme_so;
    public string target_kind; // "prefab" | "scene"
    public string target_path;
    public int row_count;
    public int fail_count;
    public AgentBridgeConformanceRowDto[] rows;
}

[Serializable]
public class AgentBridgeConformanceRowDto
{
    public string node_path;
    public string component;
    public string check_kind; // palette_ramp | font_face | frame_style | panel_kind | caption | contrast_ratio | frame_sprite_bound | button_state_block
    public string slug;
    public string expected;
    public string resolved;
    public string actual;
    public string severity; // info | warn | error
    public bool pass;
    public string message;
}
