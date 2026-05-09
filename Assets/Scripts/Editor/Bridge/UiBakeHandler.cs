using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Territory.UI;
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
            /// <summary>DB-sourced RectTransform overlay (panel_detail.rect_json). Empty/missing => bake falls back to PanelKind hard-coded defaults.</summary>
            public string rect_json;
        }

        /// <summary>Typed view of panel_detail.rect_json. Open shape — any subset of keys may be present.
        /// Each axis-pair stored as float[2] (= [x, y]). Missing/null keys fall back to PanelKind defaults.</summary>
        [Serializable]
        public class PanelRectJson
        {
            public float[] anchor_min;
            public float[] anchor_max;
            public float[] pivot;
            public float[] anchored_position;
            public float[] size_delta;
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

        // Imp-2 (bake-fix-2026-05-08) — typed `params_json` / `layout_json` POCOs.
        // Replaces ad-hoc regex extraction (`ExtractZone`, `ExtractParamsJsonIconSlug`, `TryReadFloatPath`,
        // `ReadIntField`, `ApplyChildLayoutJsonSize`, `ParsePaddingJson`) with `JsonUtility.FromJson<>`.
        // Open-shape blobs — every field optional. Missing field => type default (null/0). All callers
        // must guard for empty input string before parsing (JsonUtility throws on empty/whitespace).

        /// <summary>Inner size sub-object for layout_json — `{"size":{"w":N,"h":N}}`. JsonUtility-friendly.</summary>
        [Serializable]
        public class PanelChildLayoutSize
        {
            public float w;
            public float h;
        }

        /// <summary>Typed view of layout_json on PanelSnapshotChild. Open shape — fields optional; type defaults on miss.</summary>
        [Serializable]
        public class PanelChildLayoutJson
        {
            public string zone;
            public int ord;
            public int col;
            public int row;
            public int sub_col;
            public int rowSpan; // camelCase to match panels.json key
            public PanelChildLayoutSize size;
        }

        /// <summary>Typed view of params_json on PanelSnapshotChild. Open shape — fields optional; type defaults on miss.</summary>
        [Serializable]
        public class PanelChildParamsJson
        {
            public string icon;
            public string kind;
            public string label;
            public string action;
            public string bind;
            public string font;
            public string align;
            public string format;
            public string cadence;
            public string label_bind;
            public string bind_state;
            public string alt_icon;
            public string sub_bind;
            public string sub_format;
            public string shape;
            // main-menu fullscreen-stack additions (docs/ui-element-definitions.md lines 1239-1248).
            public string text_static;
            public string size_token;
            public string color_token;
            public string disabled_bind;
            public string visible_bind;
            public string tooltip;
            public string tooltip_override_when_disabled;
            public string action_confirm;
            public string confirm_label;
            public int    confirm_window_ms;
            public string slot_bind;
            public string @default;
        }

        /// <summary>Typed view of panel-level params_json on PanelSnapshotFields. Open shape — fields optional.</summary>
        [Serializable]
        public class PanelFieldsParamsJson
        {
            public string position; // "top" | "bottom" — Hud arm anchor override (Bug A)
        }

        /// <summary>Typed view of padding_json on PanelSnapshotFields — `{"top":N,"right":N,"bottom":N,"left":N}`.</summary>
        [Serializable]
        public class PanelPaddingJson
        {
            public int top;
            public int right;
            public int bottom;
            public int left;
        }

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
                if (innerKind == "label") return "themed-label";
                // main-menu fullscreen-stack aliases (docs/ui-element-definitions.md lines 1239-1248):
                //   destructive-confirm-button → confirm-button (visual = illuminated-button +
                //     ConfirmButton runtime; deferred runtime wiring uses confirm-button kind tag).
                //   icon-button → illuminated-button (visual identical; iconSlug-only render path
                //     already supported by IlluminatedButton renderer).
                if (innerKind == "destructive-confirm-button") return "confirm-button";
                if (innerKind == "icon-button") return "illuminated-button";
                if (innerKind == "view-slot") return "view-slot";
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
            float preferredWidth = 64f, float preferredHeight = 64f)
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
                    AttachUiActionTrigger(childGo, pj?.action);
                    // Caption fallback when icon sprite missing OR slug is the placeholder "empty" — both
                    // need the label to communicate function while real art is pending.
                    bool isPlaceholder = string.IsNullOrEmpty(iconSlug) || iconSlug == "empty";
                    if ((!iconResolved || isPlaceholder) && !string.IsNullOrEmpty(label))
                    {
                        SpawnIlluminatedButtonCaption(childGo, label);
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
                    // text_static (branding strips) wins over generic label; "--" placeholder is
                    // last-resort default when neither static text nor a label was authored.
                    string staticText = pj?.text_static;
                    string resolvedText = !string.IsNullOrEmpty(staticText)
                        ? staticText
                        : (string.IsNullOrEmpty(label) ? "--" : label);
                    if (labelTmp != null)
                    {
                        labelTmp.text = resolvedText;
                        // size_token mapping (size.text.title-display / .caption / .body) — keeps
                        // branding strips visually distinct from inline labels. Autosize disabled
                        // when an explicit size_token is provided; theme palette + color_token
                        // override default white when "color.text.muted".
                        if (!string.IsNullOrEmpty(pj?.size_token))
                        {
                            labelTmp.enableAutoSizing = false;
                            labelTmp.fontSize = pj.size_token switch
                            {
                                "size.text.title-display" => 64f,
                                "size.text.title"         => 32f,
                                "size.text.body"          => 16f,
                                "size.text.caption"       => 12f,
                                _                         => labelTmp.fontSize,
                            };
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
                    AttachUiActionTrigger(childGo, pj?.action);
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
            Debug.LogWarning($"[UiBakeHandler] {error}: {details} @ {path}");
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
            var padJson = fields?.padding_json;
            if (!string.IsNullOrEmpty(padJson))
            {
                var pad = TryParseTypedJson<PanelPaddingJson>(padJson);
                // Apply only fields the JSON actually carried — JsonUtility cannot distinguish "absent" from
                // "0", so we keep defaults when caller passed empty/whitespace; non-empty input fully overrides.
                padTop = pad.top;
                padRight = pad.right;
                padBottom = pad.bottom;
                padLeft = pad.left;
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

                var prefabError = WritePanelSnapshotPrefabs(snapshot, args.out_dir, theme);
                if (prefabError != null) return new BakeResult { root = null, error = prefabError, warnings = warnings };

                AssetDatabase.Refresh();
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

            foreach (var child in item.children)
            {
                if (child == null) continue;
                var layout = TryParseTypedJson<PanelChildLayoutJson>(child.layout_json);
                var pj = TryParseTypedJson<PanelChildParamsJson>(child.params_json);

                Transform parent = panelRoot.transform;
                if (parentByOrd != null && parentByOrd.TryGetValue(child.ord, out var resolved) && resolved != null)
                {
                    parent = resolved;
                }
                if (parentByZone != null && !string.IsNullOrEmpty(layout?.zone)
                    && parentByZone.TryGetValue(layout.zone, out var zoneParent) && zoneParent != null)
                {
                    parent = zoneParent;
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
                BakeChildByKind(childGo, innerKind, pj, theme, prefW, prefH);
                PropagateThemeRefRecursive(childGo, theme);

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

    }
}
