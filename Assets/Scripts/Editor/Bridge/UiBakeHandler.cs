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
                rootRect.anchorMin = new Vector2(0f, 1f);
                rootRect.anchorMax = new Vector2(0f, 1f);
                rootRect.pivot = new Vector2(0f, 1f);
                rootRect.anchoredPosition = new Vector2(8f, -8f);
                rootRect.sizeDelta = new Vector2(200f, 80f);

                var bgImage = go.AddComponent<Image>();
                bgImage.color = Color.white;
                bgImage.raycastTarget = false;

                var themedPanel = go.AddComponent<ThemedPanel>();
                WireThemeRef(themedPanel, theme);

                // Map layout_template → root LayoutGroup. Hard fail on missing.
                System.Type layoutGroupType;
                string layoutTemplate = item.fields?.layout_template ?? string.Empty;
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

                go.AddComponent(layoutGroupType);

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

                // Spawn a minimal placeholder child for each snapshot child row.
                var childGo = new GameObject($"child_{child.ord}", typeof(RectTransform));
                childGo.transform.SetParent(parent, worldPositionStays: false);
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
            var prop = so.FindProperty("_themeRef");
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
