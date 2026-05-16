using System;
using System.Collections.Generic;
using Territory.UI;
using Territory.UI.StudioControls;
using Territory.UI.StudioControls.Renderers;
using Territory.UI.Themed;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Domains.Bridge.Services
{
    /// <summary>
    /// POCO service owning all conformance check logic extracted from
    /// AgentBridgeCommandRunner.Conformance.cs (Stage 6.1).
    /// Implements: ApplyThemePrePass, WalkConformanceNode, per-kind check methods,
    /// IR index builders, color/contrast helpers.
    /// Lives in implicit editor assembly — reaches Territory.UI.Themed.* freely.
    /// </summary>
    public static class BridgeConformanceService
    {
        public const float ConformanceColorEpsilon = 1.5f / 255f;
        public const float WcagBodyTextThreshold = 4.5f;

        /// <summary>Apply theme to every ThemedPanel/Label/Button under root before walk.</summary>
        public static void ApplyThemePrePass(Transform root, UiTheme theme)
        {
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

        /// <summary>Recursively walk node tree; emit conformance rows per themed component.</summary>
        public static void WalkConformanceNode(
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
                CheckButtonStateBlock(nodePath, node, illuminatedButton, rows);
            for (int i = 0; i < node.childCount; i++)
                WalkConformanceNode(node.GetChild(i), root, theme, ir, irPanels, irLabelIndex, rows);
        }

        // ── IR index builders ──────────────────────────────────────────────────

        /// <summary>Build slug→panel lookup from IR root.</summary>
        public static Dictionary<string, ConformanceIrPanelDto> BuildIrPanelIndex(ConformanceIrRootDto ir)
        {
            var dict = new Dictionary<string, ConformanceIrPanelDto>(StringComparer.Ordinal);
            if (ir.panels != null)
                foreach (var p in ir.panels)
                    if (p != null && !string.IsNullOrEmpty(p.slug)) dict[p.slug] = p;
            return dict;
        }

        /// <summary>Build 'panelSlug/childName'→label lookup from IR slots.</summary>
        public static Dictionary<string, string> BuildIrLabelIndex(ConformanceIrRootDto ir)
        {
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
                        string c = slot.children[i]; string l = slot.labels[i];
                        if (!string.IsNullOrEmpty(c) && !string.IsNullOrEmpty(l)) dict[$"{p.slug}/{c}"] = l;
                    }
                }
            }
            return dict;
        }

        // ── Per-kind check methods ─────────────────────────────────────────────

        static void CheckThemedButton(string nodePath, ThemedButton btn, UiTheme theme, List<AgentBridgeConformanceRowDto> rows)
        {
            var so = new SerializedObject(btn);
            string paletteSlug = TryReadStringField(so, "_paletteSlug");
            string frameStyleSlug = TryReadStringField(so, "_frameStyleSlug");
            var buttonImageRef = TryReadObjectField(so, "_buttonImage");
            if (!string.IsNullOrEmpty(paletteSlug))
                CheckPaletteRamp(nodePath, "ThemedButton", paletteSlug, -1, buttonImageRef as Component, "m_Color", theme, rows);
            if (!string.IsNullOrEmpty(frameStyleSlug))
                CheckFrameStyle(nodePath, "ThemedButton", frameStyleSlug, theme, rows);
        }

        static void CheckThemedLabel(string nodePath, ThemedLabel lbl, UiTheme theme, List<AgentBridgeConformanceRowDto> rows)
        {
            var so = new SerializedObject(lbl);
            string paletteSlug = TryReadStringField(so, "_paletteSlug");
            string fontFaceSlug = TryReadStringField(so, "_fontFaceSlug");
            var tmpRef = TryReadObjectField(so, "_tmpText");
            if (!string.IsNullOrEmpty(paletteSlug))
                CheckPaletteRamp(nodePath, "ThemedLabel", paletteSlug, -1, tmpRef as Component, "m_fontColor", theme, rows);
            if (!string.IsNullOrEmpty(fontFaceSlug))
                CheckFontFace(nodePath, "ThemedLabel", fontFaceSlug, theme, rows);
        }

        static void CheckThemedPanel(string nodePath, Transform node, ThemedPanel pnl, UiTheme theme, Dictionary<string, ConformanceIrPanelDto> irPanels, List<AgentBridgeConformanceRowDto> rows)
        {
            var so = new SerializedObject(pnl);
            string paletteSlug = TryReadStringField(so, "_paletteSlug");
            var bgImageRef = TryReadObjectField(so, "_backgroundImage");
            int kindEnumIdx = TryReadEnumIndex(so, "_kind");
            if (!string.IsNullOrEmpty(paletteSlug))
                CheckPaletteRamp(nodePath, "ThemedPanel", paletteSlug, 1, bgImageRef as Component, "m_Color", theme, rows);
            if (irPanels.TryGetValue(node.gameObject.name, out var irPanel))
            {
                string irKind = string.IsNullOrEmpty(irPanel.kind) ? "modal" : irPanel.kind;
                string actualKind = PanelKindEnumIdxToIrName(kindEnumIdx);
                bool pass = string.Equals(irKind, actualKind, StringComparison.Ordinal);
                rows.Add(new AgentBridgeConformanceRowDto { node_path = nodePath, component = "ThemedPanel", check_kind = "panel_kind", slug = irPanel.slug, expected = irKind, resolved = irKind, actual = actualKind, severity = pass ? "info" : "error", pass = pass, message = pass ? "panel kind matches IR" : $"panel kind mismatch — IR='{irKind}' actual='{actualKind}'" });
            }
        }

        static void CheckPaletteRamp(string nodePath, string componentName, string slug, int expectedStopIdx, Component bound, string colorPropName, UiTheme theme, List<AgentBridgeConformanceRowDto> rows)
        {
            if (!theme.TryGetPalette(slug, out var ramp) || ramp.ramp == null || ramp.ramp.Length == 0) { rows.Add(new AgentBridgeConformanceRowDto { node_path = nodePath, component = componentName, check_kind = "palette_ramp", slug = slug, expected = "(palette in theme)", resolved = "(missing)", actual = string.Empty, severity = "error", pass = false, message = $"palette slug '{slug}' not in UiTheme" }); return; }
            int idx = expectedStopIdx < 0 ? ramp.ramp.Length + expectedStopIdx : expectedStopIdx;
            if (idx < 0 || idx >= ramp.ramp.Length) idx = ramp.ramp.Length - 1;
            string expectedHex = ramp.ramp[idx];
            if (!ColorUtility.TryParseHtmlString(expectedHex, out var expectedColor)) { rows.Add(new AgentBridgeConformanceRowDto { node_path = nodePath, component = componentName, check_kind = "palette_ramp", slug = slug, expected = expectedHex, resolved = "(unparsable hex)", actual = string.Empty, severity = "error", pass = false, message = $"theme ramp[{idx}] '{expectedHex}' is not parsable hex" }); return; }
            if (bound == null) { rows.Add(new AgentBridgeConformanceRowDto { node_path = nodePath, component = componentName, check_kind = "palette_ramp", slug = slug, expected = expectedHex, resolved = FormatColor(expectedColor), actual = "(unbound)", severity = "warn", pass = false, message = $"{componentName} has no bound target component (Image / TMP_Text) — cannot verify rendered color" }); return; }
            Color? actualColor = TryReadComponentColor(bound, colorPropName);
            if (!actualColor.HasValue) { rows.Add(new AgentBridgeConformanceRowDto { node_path = nodePath, component = componentName, check_kind = "palette_ramp", slug = slug, expected = expectedHex, resolved = FormatColor(expectedColor), actual = "(unreadable)", severity = "warn", pass = false, message = $"could not read color property '{colorPropName}' on {bound.GetType().Name}" }); return; }
            bool pass = ColorsApproxEqual(expectedColor, actualColor.Value);
            rows.Add(new AgentBridgeConformanceRowDto { node_path = nodePath, component = componentName, check_kind = "palette_ramp", slug = slug, expected = expectedHex, resolved = FormatColor(expectedColor), actual = FormatColor(actualColor.Value), severity = pass ? "info" : "error", pass = pass, message = pass ? $"ramp[{idx}] resolves and matches" : $"ramp[{idx}]={expectedHex} but rendered color differs" });
        }

        static void CheckFontFace(string nodePath, string componentName, string slug, UiTheme theme, List<AgentBridgeConformanceRowDto> rows)
        {
            if (!theme.TryGetFontFace(slug, out var face)) { rows.Add(new AgentBridgeConformanceRowDto { node_path = nodePath, component = componentName, check_kind = "font_face", slug = slug, expected = "(font_face in theme)", resolved = "(missing)", actual = string.Empty, severity = "error", pass = false, message = $"font_face slug '{slug}' not in UiTheme" }); return; }
            rows.Add(new AgentBridgeConformanceRowDto { node_path = nodePath, component = componentName, check_kind = "font_face", slug = slug, expected = $"family='{face.family}' weight={face.weight}", resolved = $"family='{face.family}' weight={face.weight}", actual = $"family='{face.family}' weight={face.weight}", severity = "info", pass = true, message = "font_face slug resolves (runtime fontAsset binding deferred to Stage 6 catalog)" });
        }

        static void CheckFrameStyle(string nodePath, string componentName, string slug, UiTheme theme, List<AgentBridgeConformanceRowDto> rows)
        {
            if (!theme.TryGetFrameStyle(slug, out var fs)) { rows.Add(new AgentBridgeConformanceRowDto { node_path = nodePath, component = componentName, check_kind = "frame_style", slug = slug, expected = "(frame_style in theme)", resolved = "(missing)", actual = string.Empty, severity = "error", pass = false, message = $"frame_style slug '{slug}' not in UiTheme" }); return; }
            rows.Add(new AgentBridgeConformanceRowDto { node_path = nodePath, component = componentName, check_kind = "frame_style", slug = slug, expected = $"edge='{fs.edge}' innerShadowAlpha={fs.innerShadowAlpha:F3}", resolved = $"edge='{fs.edge}' innerShadowAlpha={fs.innerShadowAlpha:F3}", actual = $"edge='{fs.edge}' innerShadowAlpha={fs.innerShadowAlpha:F3}", severity = "info", pass = true, message = "frame_style slug resolves (sprite swap deferred)" });
        }

        static void CheckFrameSpriteBound(string nodePath, ThemedPanel pnl, List<AgentBridgeConformanceRowDto> rows)
        {
            var so = new SerializedObject(pnl);
            var top = TryReadObjectField(so, "_borderTop") as Image;
            var bot = TryReadObjectField(so, "_borderBottom") as Image;
            var lft = TryReadObjectField(so, "_borderLeft") as Image;
            var rgt = TryReadObjectField(so, "_borderRight") as Image;
            var strips = new[] { ("Top", top), ("Bottom", bot), ("Left", lft), ("Right", rgt) };
            int present = 0, visibleAlpha = 0, nonZeroSize = 0, ignoreLayout = 0;
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
            rows.Add(new AgentBridgeConformanceRowDto { node_path = nodePath, component = "ThemedPanel", check_kind = "frame_sprite_bound", slug = TryReadStringField(so, "_frameStyleSlug") ?? string.Empty, expected = "4 strips wired + visible alpha + non-zero size + LayoutElement.ignoreLayout=true", resolved = "4 strips wired + visible alpha + non-zero size + LayoutElement.ignoreLayout=true", actual = $"wired={present}/4 alpha={visibleAlpha}/4 size={nonZeroSize}/4 ignoreLayout={ignoreLayout}/4", severity = pass ? "info" : "error", pass = pass, message = pass ? "all 4 border strips wired + visible + sized + layout-exempt" : $"strip defects: {string.Join(",", failures)} — re-bake panel" });
        }

        static void CheckButtonStateBlock(string nodePath, Transform node, IlluminatedButton btn, List<AgentBridgeConformanceRowDto> rows)
        {
            var renderer = node.GetComponent<IlluminatedButtonRenderer>();
            bool pass = renderer != null;
            rows.Add(new AgentBridgeConformanceRowDto { node_path = nodePath, component = "IlluminatedButton", check_kind = "button_state_block", slug = btn.Slug ?? string.Empty, expected = "IlluminatedButtonRenderer present (handles hover/press)", resolved = "IlluminatedButtonRenderer present (handles hover/press)", actual = pass ? "renderer present" : "renderer missing", severity = pass ? "info" : "error", pass = pass, message = pass ? "hover/press state block wired via IlluminatedButtonRenderer" : "no IlluminatedButtonRenderer — pointer hover/press will not animate" });
        }

        static void CheckCaptionMatch(string nodePath, Transform node, ThemedLabel lbl, Dictionary<string, string> irLabelIndex, List<AgentBridgeConformanceRowDto> rows)
        {
            Transform cursor = node.parent; Transform childCursor = node;
            string panelSlug = null, childSlug = null;
            while (cursor != null)
            {
                if (cursor.GetComponent<ThemedPanel>() != null) { panelSlug = cursor.gameObject.name; childSlug = childCursor.gameObject.name; break; }
                childCursor = cursor; cursor = cursor.parent;
            }
            if (panelSlug == null || childSlug == null) return;
            string indexKey = $"{panelSlug}/{childSlug}";
            if (!irLabelIndex.TryGetValue(indexKey, out string expectedLabel)) return;
            var so = new SerializedObject(lbl);
            var tmpRef = TryReadObjectField(so, "_tmpText");
            string actual = string.Empty;
            if (tmpRef != null) { var tmpSo = new SerializedObject(tmpRef); var textProp = tmpSo.FindProperty("m_text"); if (textProp != null && textProp.propertyType == SerializedPropertyType.String) actual = textProp.stringValue ?? string.Empty; }
            bool pass = string.Equals(expectedLabel, actual, StringComparison.Ordinal);
            rows.Add(new AgentBridgeConformanceRowDto { node_path = nodePath, component = "ThemedLabel", check_kind = "caption", slug = indexKey, expected = expectedLabel, resolved = expectedLabel, actual = actual, severity = pass ? "info" : "error", pass = pass, message = pass ? "caption matches IR slot label" : $"caption differs from IR slot label — expected '{expectedLabel}' actual '{actual}'" });
        }

        static void CheckContrastUnderPanel(string nodePath, Transform node, ThemedLabel lbl, UiTheme theme, List<AgentBridgeConformanceRowDto> rows)
        {
            ThemedPanel ancestorPanel = null; Transform cursor = node.parent;
            while (cursor != null && ancestorPanel == null) { ancestorPanel = cursor.GetComponent<ThemedPanel>(); cursor = cursor.parent; }
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
            rows.Add(new AgentBridgeConformanceRowDto { node_path = nodePath, component = "ThemedLabel", check_kind = "contrast_ratio", slug = $"{labelSlug}@{panelSlug}", expected = $">= {WcagBodyTextThreshold:F2}", resolved = $"text={textHex} fill={fillHex}", actual = ratio.ToString("F2", System.Globalization.CultureInfo.InvariantCulture), severity = pass ? "info" : "warn", pass = pass, message = pass ? "WCAG body-text contrast met" : $"WCAG body-text contrast {ratio:F2} below 4.5" });
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        /// <summary>Build slash-joined relative path from node to root.</summary>
        public static string ComputeRelativePath(Transform node, Transform root)
        {
            if (node == root) return node.gameObject.name;
            var parts = new System.Collections.Generic.List<string>();
            Transform c = node;
            while (c != null && c != root) { parts.Insert(0, c.gameObject.name); c = c.parent; }
            if (c == root) parts.Insert(0, root.gameObject.name);
            return string.Join("/", parts);
        }

        /// <summary>Read string serialized property; null if absent/wrong type.</summary>
        public static string TryReadStringField(SerializedObject so, string propName)
        {
            var p = so.FindProperty(propName);
            return (p == null || p.propertyType != SerializedPropertyType.String) ? null : p.stringValue;
        }

        /// <summary>Read object reference serialized property; null if absent/wrong type.</summary>
        public static UnityEngine.Object TryReadObjectField(SerializedObject so, string propName)
        {
            var p = so.FindProperty(propName);
            return (p == null || p.propertyType != SerializedPropertyType.ObjectReference) ? null : p.objectReferenceValue;
        }

        /// <summary>Read enum serialized property index; -1 if absent/wrong type.</summary>
        public static int TryReadEnumIndex(SerializedObject so, string propName)
        {
            var p = so.FindProperty(propName);
            return (p == null || p.propertyType != SerializedPropertyType.Enum) ? -1 : p.enumValueIndex;
        }

        static Color? TryReadComponentColor(Component comp, string propName)
        {
            if (comp == null) return null;
            try { switch (comp) { case TMPro.TMP_Text tmp: return tmp.color; case Graphic gfx: return gfx.color; } }
            catch { }
            try { var so = new SerializedObject(comp); var p = so.FindProperty(propName); if (p == null || p.propertyType != SerializedPropertyType.Color) return null; return p.colorValue; }
            catch { return null; }
        }

        /// <summary>True if all channels within epsilon.</summary>
        public static bool ColorsApproxEqual(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) < ConformanceColorEpsilon && Mathf.Abs(a.g - b.g) < ConformanceColorEpsilon && Mathf.Abs(a.b - b.b) < ConformanceColorEpsilon && Mathf.Abs(a.a - b.a) < ConformanceColorEpsilon;

        /// <summary>Format color as rgba(…) string with 3-decimal precision.</summary>
        public static string FormatColor(Color c) =>
            string.Format(System.Globalization.CultureInfo.InvariantCulture, "rgba({0:F3},{1:F3},{2:F3},{3:F3})", c.r, c.g, c.b, c.a);

        /// <summary>Map ThemedPanel.kind enum index → IR kind name.</summary>
        public static string PanelKindEnumIdxToIrName(int idx)
        {
            switch (idx) { case 0: return "modal"; case 1: return "screen"; case 2: return "hud"; case 3: return "toolbar"; default: return "(unknown)"; }
        }

        static float ContrastRatio(Color a, Color b) { float la = RelativeLuminance(a) + 0.05f; float lb = RelativeLuminance(b) + 0.05f; return la > lb ? la / lb : lb / la; }
        static float RelativeLuminance(Color c) => 0.2126f * LinearChannel(c.r) + 0.7152f * LinearChannel(c.g) + 0.0722f * LinearChannel(c.b);
        static float LinearChannel(float v) => v <= 0.03928f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);
    }
}
