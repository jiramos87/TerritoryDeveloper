using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Territory.UI;
using Territory.UI.Editor;
using Territory.UI.Juice;
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
        // ── DTOs (Stage 1 sketchpad shape — JsonUtility-friendly) ───────────────

        /// <summary>Top-level IR JSON shape — single bake-input root.</summary>
        [Serializable]
        public class IrRoot
        {
            public IrTokens tokens;
            public IrPanel[] panels;
            public IrInteractive[] interactives;
        }

        /// <summary>Token block — five subblocks, one per kind.</summary>
        [Serializable]
        public class IrTokens
        {
            public IrTokenPalette[] palette;
            public IrTokenFrameStyle[] frame_style;
            public IrTokenFontFace[] font_face;
            public IrTokenMotionCurve[] motion_curve;
            public IrTokenIllumination[] illumination;
        }

        [Serializable]
        public class IrTokenPalette
        {
            public string slug;
            /// <summary>Ordered hex stops (low → high).</summary>
            public string[] ramp;
        }

        [Serializable]
        public class IrTokenFrameStyle
        {
            public string slug;
            /// <summary>`single` | `double` (CD partner extends).</summary>
            public string edge;
            public float innerShadowAlpha;
        }

        [Serializable]
        public class IrTokenFontFace
        {
            public string slug;
            public string family;
            public int weight;
        }

        [Serializable]
        public class IrTokenMotionCurve
        {
            public string slug;
            /// <summary>`spring` | `cubic-bezier` | other.</summary>
            public string kind;
            public float stiffness;
            public float damping;
            public float[] c1;
            public float[] c2;
            public float durationMs;
        }

        [Serializable]
        public class IrTokenIllumination
        {
            public string slug;
            public string color;
            public float haloRadiusPx;
        }

        /// <summary>Stage 1.4 (T1.4.1) — panel spacing overrides from IR `panel.detail`.</summary>
        [Serializable]
        public class IrPanelDetail
        {
            public float paddingX;
            public float paddingY;
            public float gap;
            public float dividerThickness;
        }

        /// <summary>Stage 1.4 (T1.4.4) — button state override block from IR `button.detail`.</summary>
        [Serializable]
        public class IrButtonDetail
        {
            public float paddingX;
            public float paddingY;
        }

        /// <summary>Stage 1.4 (T1.4.4) — per-state color table from IR `button.palette_ramp`.</summary>
        [Serializable]
        public class IrButtonPaletteRamp
        {
            public string normal;
            public string highlighted;
            public string pressed;
            public string disabled;
        }

        /// <summary>Stage 1.4 (T1.4.4) — atlas slot enums for sprite-swap states.</summary>
        [Serializable]
        public class IrButtonAtlasSlotEnum
        {
            public string normal;
            public string highlighted;
            public string pressed;
        }

        /// <summary>Stage 1.4 (T1.4.4) — motion-curve override for button fade duration.</summary>
        [Serializable]
        public class IrButtonMotionCurve
        {
            public float fadeDuration;
        }

        /// <summary>Stage 1.4 (T1.4.4) — full button state IR detail block; parsed from interactive.detail JSON.</summary>
        [Serializable]
        public class IrButtonStateDetail
        {
            public IrButtonPaletteRamp palette_ramp;
            public IrButtonAtlasSlotEnum atlas_slot_enum;
            public IrButtonMotionCurve motion_curve;
        }

        [Serializable]
        public class IrPanel
        {
            public string slug;
            public string archetype;
            public string kind;
            public IrPanelSlot[] slots;
            /// <summary>Stage 1.4 (T1.4.1) — optional spacing overrides; null when absent from IR.</summary>
            public IrPanelDetail detail;
            /// <summary>Stage 1.4 (T1.4.3) — atlas frame sprite slug; resolved via <see cref="Territory.UI.Editor.AtlasIndex"/>.</summary>
            public string frame_style_slug;
            /// <summary>Stage 1.4 (T1.4.3) — illumination token slug; drives <see cref="ThemedIlluminationLayer"/> child when non-empty.</summary>
            public string illumination_slug;
            /// <summary>Stage 13.1+ — IR v2 tab descriptors. Null/empty on tabless panels; bake skips ThemedTabBar.pages[] wiring when absent.</summary>
            public IrTab[] tabs;
            /// <summary>Stage 13.1+ — IR v2 flat row list. Null/empty on rowless panels; bake emits no row children.</summary>
            public IrRow[] rows;
            /// <summary>Stage 13.4 (TECH-9867) — IR v2 default tab index (0 = first tab; absent in IR also defaults to 0 since JsonUtility cannot model nullable int). Bake clamps to [0, tabs.Length).</summary>
            public int defaultTabIndex;
        }

        /// <summary>Stage 13.1+ — IR v2 tab descriptor. Drives ThemedTabBar.pages[] wiring at bake time.</summary>
        [Serializable]
        public class IrTab
        {
            public string id;
            public string label;
            public bool active;
            /// <summary>Stage 13.3 — optional icon slug; when non-empty, bake adds <see cref="Territory.UI.Themed.ThemedIcon"/> child resolving via <see cref="Territory.UI.Themed.UiTheme.TryGetIcon"/>.</summary>
            public string iconSlug;
        }

        /// <summary>Stage 13.1+ — IR v2 row descriptor. Flat list per panel; `kind` discriminates render shape.</summary>
        [Serializable]
        public class IrRow
        {
            /// <summary>Render shape — `stat`, `detail`, or `header`.</summary>
            public string kind;
            public string label;
            public string value;
            public int segments;
            public string fontSlug;
            /// <summary>Stage 13.3 — optional icon slug; when non-empty, bake adds <see cref="Territory.UI.Themed.ThemedIcon"/> child resolving via <see cref="Territory.UI.Themed.UiTheme.TryGetIcon"/>.</summary>
            public string iconSlug;
        }

        [Serializable]
        public class IrPanelSlot
        {
            public string name;
            public string[] accepts;
            public string[] children;
            // Step 12 — optional per-child label content; parallel to children[] when present.
            public string[] labels;
            // Step 16.D — optional per-child icon sprite slug; parallel to children[]. Empty/null skips icon.
            // Bake handler resolves to Assets/Sprites/Buttons/{slug}-target.png (fallback Assets/Sprites/{slug}-target.png).
            public string[] iconSpriteSlugs;
        }

        /// <summary>Polymorphic token entry — guardrail #11 `value_kind` + flat `value` shape.
        /// Reserved for future bridge-side use; not currently produced by Stage 1 transcribe.</summary>
        [Serializable]
        public class IrTokenEntry
        {
            public string slug;
            public string value_kind;
            public string value;
        }

        [Serializable]
        public class IrInteractive
        {
            public string slug;
            /// <summary>StudioControl archetype slug — see <see cref="UiBakeHandler"/>._knownKinds for the roster.</summary>
            public string kind;
            // `detail` is open-ended (Record&lt;string,unknown&gt; in TS); not modeled in C# DTO since
            // bridge-side bake at this stage does not consume it. T3+ will introduce typed details.

            /// <summary>
            /// Optional Stage 5 (T5.5) juice declarations. Additive on the IR root — defaults to null
            /// when absent so prior baked artifacts stay valid. Each entry overrides or disables a
            /// per-kind default juice attachment.
            /// </summary>
            public IrJuiceDecl[] juice;
        }

        /// <summary>
        /// Stage 5 juice override entry. <c>juice_kind</c> matches a known JuiceLayer behavior slug
        /// (see <see cref="UiBakeHandler.JuiceKindNeedleBallistics"/> etc.). When <c>disabled</c> is true,
        /// the per-kind default attachment for this interactive is skipped.
        /// </summary>
        [Serializable]
        public class IrJuiceDecl
        {
            /// <summary>Optional usage slug for filtering; mirrors interactive slug when empty.</summary>
            public string usage_slug;
            /// <summary>Juice slug — see <see cref="UiBakeHandler.JuiceKindNeedleBallistics"/> et al.</summary>
            public string juice_kind;
            /// <summary>Optional motion-curve slug override for the juice component.</summary>
            public string curve_slug;
            /// <summary>When true, suppresses the per-kind default attachment for the matching <see cref="juice_kind"/>.</summary>
            public bool disabled;
        }

        /// <summary>Bridge-mutation argument bag for `bake_ui_from_ir`.</summary>
        [Serializable]
        public class BakeArgs
        {
            public string ir_path;
            /// <summary>Stage 9.10 — canonical panels snapshot path (panels.json). Takes precedence over ir_path when set.</summary>
            public string panels_path;
            public string out_dir;
            public string theme_so;
        }

        // ── Stage 9.10 — PanelSnapshot DTOs (JsonUtility-friendly, no Newtonsoft) ─

        /// <summary>Top-level panels.json snapshot shape (schema_version 3).</summary>
        [Serializable]
        public class PanelSnapshot
        {
            public string snapshot_id;
            public string kind;
            public int schema_version;
            public PanelSnapshotItem[] items;
        }

        /// <summary>One panel item in panels.json items[].</summary>
        [Serializable]
        public class PanelSnapshotItem
        {
            public string slug;
            public PanelSnapshotFields fields;
            public PanelSnapshotChild[] children;
        }

        /// <summary>Panel fields block — mirrors panel_detail columns projected by exporter.</summary>
        [Serializable]
        public class PanelSnapshotFields
        {
            public string layout_template;
            public string layout;
            public float gap_px;
            public string padding_json;
            public string params_json;
        }

        /// <summary>Per-child row in panels.json children[]. layout_json is a JSON string (JsonUtility cannot model nested objects).</summary>
        [Serializable]
        public class PanelSnapshotChild
        {
            public int ord;
            public string kind;
            public string params_json;
            public string sprite_ref;
            /// <summary>JSON string of layout routing metadata, e.g. {\"zone\":\"left\"}. Null/empty = no zone routing.</summary>
            public string layout_json;
            /// <summary>Stage 9.15 — per-child semantic slug (e.g. "hud-bar-zoom-in-button"). Null/empty falls back to "child_{ord}" naming.</summary>
            public string instance_slug;
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
                    Debug.LogWarning($"[UiBakeHandler] layout_template '{layoutTemplate}' unrecognised — falling back to vstack for panel '{panelSlug}'");
                    return typeof(VerticalLayoutGroup);
            }
        }

        // F1 (bake-fix-2026-05-07): map layout_template → PanelKind so bake-time _kind
        // matches the LayoutGroup attached at root, preventing OnEnable from stripping it.
        internal static PanelKind MapLayoutTemplateToPanelKind(string layoutTemplate, string panelSlug)
        {
            switch (layoutTemplate)
            {
                case "hstack": return PanelKind.Hud;
                case "vstack": return PanelKind.Modal;
                case "grid":   return PanelKind.Toolbar;
                default:       return PanelKind.Modal;
            }
        }

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

        // F2: per-kind anchor/size defaults. Hud=bottom-strip stretch, Modal=center,
        // Toolbar=left-rail, SideRail=right-rail, Screen=full stretch.
        internal static void ApplyPanelKindRectDefaults(RectTransform rect, PanelKind kind)
        {
            if (rect == null) return;
            switch (kind)
            {
                case PanelKind.Hud:
                    rect.anchorMin = new Vector2(0f, 0f);
                    rect.anchorMax = new Vector2(1f, 0f);
                    rect.pivot = new Vector2(0.5f, 0f);
                    rect.anchoredPosition = new Vector2(0f, 8f);
                    rect.sizeDelta = new Vector2(-16f, 80f); // stretch full width minus 8px padding each side
                    break;
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

        // F3 (bake-fix-2026-05-07): configure root LayoutGroup so children fill row evenly,
        // pull padding from fields.padding_json + spacing from fields.gap_px.
        internal static void ApplyRootLayoutGroupConfig(LayoutGroup layoutGroup, PanelKind kind, PanelSnapshotFields fields)
        {
            if (layoutGroup == null) return;
            int padTop = 4, padBottom = 4, padLeft = 8, padRight = 8;
            float gap = fields?.gap_px ?? 8f;
            var padJson = fields?.padding_json;
            if (!string.IsNullOrEmpty(padJson))
            {
                ParsePaddingJson(padJson, ref padTop, ref padRight, ref padBottom, ref padLeft);
            }
            layoutGroup.padding = new RectOffset(padLeft, padRight, padTop, padBottom);

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

        // Lightweight padding_json reader — accepts {"top":N,"right":N,"bottom":N,"left":N}.
        private static void ParsePaddingJson(string padJson, ref int top, ref int right, ref int bottom, ref int left)
        {
            top    = ReadIntField(padJson, "top",    top);
            right  = ReadIntField(padJson, "right",  right);
            bottom = ReadIntField(padJson, "bottom", bottom);
            left   = ReadIntField(padJson, "left",   left);
        }

        private static int ReadIntField(string json, string key, int fallback)
        {
            var m = System.Text.RegularExpressions.Regex.Match(json, "\"" + key + "\"\\s*:\\s*(-?\\d+)");
            if (!m.Success) return fallback;
            return int.TryParse(m.Groups[1].Value, out var v) ? v : fallback;
        }

        /// <summary>
        /// Extract layout_json.zone from a <see cref="PanelSnapshotChild"/> layout_json string.
        /// Returns null when layout_json absent or zone key missing.
        /// </summary>
        public static string ExtractZone(string layoutJsonStr)
        {
            if (string.IsNullOrEmpty(layoutJsonStr)) return null;
            var match = System.Text.RegularExpressions.Regex.Match(layoutJsonStr, "\"zone\"\\s*:\\s*\"([^\"]+)\"");
            if (match.Success) return match.Groups[1].Value;
            return null;
        }

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
        /// Stage 9.10 — bake from panels.json snapshot (PanelSnapshot DTOs).
        /// Reads panels_path, parses into <see cref="PanelSnapshot"/>, bakes each panel item.
        /// Fails hard when layout_template missing.
        /// </summary>
        public static BakeResult BakeFromPanelSnapshot(BakeArgs args)
        {
            if (args == null || string.IsNullOrEmpty(args.panels_path))
            {
                return new BakeResult
                {
                    root = null,
                    error = new BakeError { error = "missing_arg", details = "panels_path", path = "$.panels_path" },
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
                };
            }

            var (snapshot, parseError) = ParsePanelSnapshot(jsonText);
            if (parseError != null) return new BakeResult { root = null, error = parseError };

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
                };
            }

            var prefabError = WritePanelSnapshotPrefabs(snapshot, args.out_dir, theme);
            if (prefabError != null) return new BakeResult { root = null, error = prefabError };

            AssetDatabase.Refresh();
            return new BakeResult { root = null, error = null };
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
                string layoutTemplate = item.fields?.layout_template ?? string.Empty;
                var panelKind = MapLayoutTemplateToPanelKind(layoutTemplate, item.slug);
                ApplyPanelKindRectDefaults(rootRect, panelKind);

                var bgImage = go.AddComponent<Image>();
                bgImage.color = Color.white;
                bgImage.raycastTarget = false;

                var themedPanel = go.AddComponent<ThemedPanel>();
                WireThemeRef(themedPanel, theme);
                AssignPanelKind(themedPanel, panelKind);

                // Map layout_template → root LayoutGroup. Hard fail on missing.
                System.Type layoutGroupType;
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

                // F8 (bake-fix-2026-05-07) — ThemedPanel.OnEnable fires at AddComponent
                // time with default _kind=Modal=0 → ApplyKindLayout attaches a VLG before
                // AssignPanelKind has a chance to set the right kind. Strip whatever
                // LayoutGroup that pass added so the bake-driven AddComponent below ends
                // up as the sole root LayoutGroup matching layout_template.
                foreach (var stale in go.GetComponents<LayoutGroup>())
                {
                    UnityEngine.Object.DestroyImmediate(stale);
                }

                var layoutGroup = (LayoutGroup)go.AddComponent(layoutGroupType);
                ApplyRootLayoutGroupConfig(layoutGroup, panelKind, item.fields);

                // Slot-wrapper iteration + children (T4 fills archetype dispatch).
                BakePanelSnapshotChildren(item, go, theme);

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

        /// <summary>
        /// Iterate children[] for a snapshot panel item.
        /// Called by <see cref="SavePanelSnapshotPrefab"/>; archetype slot-wrapper
        /// dispatch extended in T4 (<see cref="BakePanelSnapshotArchetype"/>).
        /// </summary>
        static void BakePanelSnapshotChildren(PanelSnapshotItem item, GameObject panelRoot, UiTheme theme)
        {
            if (item?.children == null || item.children.Length == 0) return;

            // Build slot-wrapper map via archetype dispatch (T4 extension).
            var slotWrappers = BakePanelSnapshotArchetype(item, panelRoot, theme);

            foreach (var child in item.children)
            {
                if (child == null) continue;
                string zone = ExtractZone(child.layout_json);
                Transform parent = panelRoot.transform;
                if (slotWrappers != null && zone != null)
                {
                    if (!slotWrappers.TryGetValue(zone, out var wrapper) || wrapper == null)
                    {
                        Debug.LogWarning(
                            $"[UiBakeHandler] panel={item.slug} child ord={child.ord} zone='{zone}' has no matching slot wrapper — defaulting to Center");
                        slotWrappers.TryGetValue("center", out wrapper);
                        if (wrapper != null) parent = wrapper;
                    }
                    else
                    {
                        parent = wrapper;
                    }
                }

                // Spawn child — semantic name from instance_slug when present; fallback child_{ord}.
                string childName = !string.IsNullOrEmpty(child.instance_slug)
                    ? child.instance_slug
                    : $"child_{child.ord}";
                var childGo = new GameObject(childName, typeof(RectTransform));
                childGo.transform.SetParent(parent, worldPositionStays: false);

                // F4 (bake-fix-2026-05-07): wrapper RectTransform default size so HLG can space.
                // Default 64×64; overwritten below when a button prefab is nested + its size is known.
                var childRect = (RectTransform)childGo.transform;
                childRect.anchorMin = new Vector2(0f, 0.5f);
                childRect.anchorMax = new Vector2(0f, 0.5f);
                childRect.pivot = new Vector2(0.5f, 0.5f);
                childRect.anchoredPosition = Vector2.zero;
                childRect.sizeDelta = new Vector2(64f, 64f);
                ApplyChildLayoutJsonSize(childRect, child.layout_json);

                // Attach CatalogPrefabRef when instance_slug carries semantic identity.
                if (!string.IsNullOrEmpty(child.instance_slug))
                {
                    var childRef = childGo.AddComponent<CatalogPrefabRef>();
                    childRef.slug = child.instance_slug;
                }

                // Instantiate illuminated-button prefab for `button` kind children.
                if (child.kind == "button")
                {
                    var btnPrefabPath = "Assets/UI/Prefabs/Generated/illuminated-button.prefab";
                    var btnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(btnPrefabPath);
                    if (btnPrefab != null)
                    {
                        var instance = (GameObject)PrefabUtility.InstantiatePrefab(btnPrefab, childGo.transform);
                        if (instance != null)
                        {
                            // F12 (bake-fix-2026-05-07) — unpack nested prefab connection so added
                            // children (icon GameObject from F10) survive SaveAsPrefabAsset on the
                            // outer panel root. Without this, added objects sit as nested-prefab
                            // overrides that get filtered out when the panel is saved as a fresh
                            // prefab asset.
                            PrefabUtility.UnpackPrefabInstance(
                                instance,
                                PrefabUnpackMode.Completely,
                                InteractionMode.AutomatedAction);

                            // F4: copy prefab's natural size onto wrapper so zone HLG spaces correctly,
                            // then stretch instance to fill wrapper.
                            var instRect = instance.GetComponent<RectTransform>();
                            if (instRect != null)
                            {
                                if (instRect.sizeDelta.x > 0f && instRect.sizeDelta.y > 0f)
                                {
                                    childRect.sizeDelta = instRect.sizeDelta;
                                }
                                instRect.anchorMin = Vector2.zero;
                                instRect.anchorMax = Vector2.one;
                                instRect.pivot = new Vector2(0.5f, 0.5f);
                                instRect.anchoredPosition = Vector2.zero;
                                instRect.sizeDelta = Vector2.zero;
                            }

                            if (!string.IsNullOrEmpty(child.instance_slug))
                            {
                                instance.name = child.instance_slug;
                                var instRef = instance.GetComponent<CatalogPrefabRef>();
                                if (instRef == null) instRef = instance.AddComponent<CatalogPrefabRef>();
                                instRef.slug = child.instance_slug;
                            }

                            // F6 (bake-fix-2026-05-07): propagate UiTheme ref onto every nested
                            // theme-aware component (JuiceBase / PulseOnEvent / ThemedPrimitiveBase
                            // / StudioControlBase) on the instantiated prefab tree.
                            PropagateThemeRefRecursive(instance, theme);

                            // F10 (bake-fix-2026-05-07) — wire icon sprite. panels.json carries
                            // sprite_ref="" (button_detail.sprite_icon_entity_id null in DB) so the
                            // icon slug rides in params_json.icon. Extract slug → Resolve via
                            // existing Buttons/* search → spawn/find icon child + assign Image.sprite,
                            // then ApplyDetail so the renderer reads the slug back at runtime.
                            var iconSlug = ExtractParamsJsonIconSlug(child.params_json);
                            if (!string.IsNullOrEmpty(iconSlug))
                            {
                                WireIlluminatedButtonIcon(instance, iconSlug);
                                var btnComp = instance.GetComponent<IlluminatedButton>();
                                if (btnComp != null)
                                {
                                    btnComp.ApplyDetail(new IlluminatedButtonDetail { iconSpriteSlug = iconSlug });
                                }
                            }
                        }
                    }
                }
            }
        }

        // F10 (bake-fix-2026-05-07) — pull "icon" string out of a params_json blob.
        // Naive scan, no JsonUtility dep — params_json shape is open and varies per child kind.
        private static string ExtractParamsJsonIconSlug(string paramsJson)
        {
            if (string.IsNullOrEmpty(paramsJson)) return null;
            var m = System.Text.RegularExpressions.Regex.Match(
                paramsJson, "\"icon\"\\s*:\\s*\"([^\"]+)\"");
            return m.Success ? m.Groups[1].Value : null;
        }

        // F10 — find/create "icon" child under instantiated illuminated-button prefab + assign sprite.
        // Mirrors SpawnIlluminatedButtonRenderTargets icon block (Archetype.cs ~line 644) so the snapshot
        // bake path produces the same render contract (body → icon → halo siblings) as the IR-flow bake.
        private static void WireIlluminatedButtonIcon(GameObject instance, string iconSlug)
        {
            if (instance == null || string.IsNullOrEmpty(iconSlug)) return;
            var sprite = ResolveButtonIconSprite(iconSlug);
            if (sprite == null)
            {
                Debug.LogWarning(
                    $"[UiBakeHandler] illuminated-button icon sprite not found (slug={iconSlug}); "
                    + "expected Assets/Sprites/Buttons/{slug}-target.png or sibling-folder fallback.");
                return;
            }
            var iconT = instance.transform.Find("icon");
            UnityEngine.UI.Image iconImage;
            if (iconT == null)
            {
                var icon = new GameObject("icon", typeof(RectTransform));
                icon.transform.SetParent(instance.transform, worldPositionStays: false);
                var ir = (RectTransform)icon.transform;
                ir.anchorMin = new Vector2(0.5f, 0.5f);
                ir.anchorMax = new Vector2(0.5f, 0.5f);
                ir.pivot = new Vector2(0.5f, 0.5f);
                ir.anchoredPosition = Vector2.zero;
                ir.sizeDelta = new Vector2(64f, 64f);
                iconImage = icon.AddComponent<UnityEngine.UI.Image>();
                iconImage.raycastTarget = false; // body owns hit-testing
                iconImage.preserveAspect = true;
            }
            else
            {
                iconImage = iconT.GetComponent<UnityEngine.UI.Image>();
                if (iconImage == null) iconImage = iconT.gameObject.AddComponent<UnityEngine.UI.Image>();
            }
            iconImage.sprite = sprite;
            // Re-assert render order body → icon → halo so halo always draws on top.
            var haloT = instance.transform.Find("halo");
            if (haloT != null) haloT.SetAsLastSibling();
        }

        // F4: read layout_json {"size":{"w":N,"h":N}} into RectTransform.sizeDelta when present.
        private static void ApplyChildLayoutJsonSize(RectTransform rect, string layoutJson)
        {
            if (rect == null || string.IsNullOrEmpty(layoutJson)) return;
            var w = TryReadFloatPath(layoutJson, "size", "w");
            var h = TryReadFloatPath(layoutJson, "size", "h");
            if (w.HasValue && h.HasValue)
            {
                rect.sizeDelta = new Vector2(w.Value, h.Value);
            }
        }

        private static float? TryReadFloatPath(string json, string outerKey, string innerKey)
        {
            // Naive nested-object scan — `"outer":{"inner":N}`. Ignores whitespace variation gracefully.
            var pat = "\"" + outerKey + "\"\\s*:\\s*\\{[^}]*\"" + innerKey + "\"\\s*:\\s*(-?\\d+(?:\\.\\d+)?)";
            var m = System.Text.RegularExpressions.Regex.Match(json, pat);
            if (!m.Success) return null;
            return float.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : (float?)null;
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

    }
}
