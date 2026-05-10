using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using Territory.UI;
using Territory.UI.Juice;
using Territory.UI.Registry;
using Territory.UI.StudioControls;
using Territory.UI.StudioControls.Renderers;
using Territory.UI.Themed;
using Territory.UI.Themed.Renderers;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Territory.Editor.Bridge
{
    public static partial class UiBakeHandler
    {
        // ── StudioControl interactive bake (Stage 4 T4.5) ───────────────────────

        /// <summary>Known StudioControl kind slugs — Stage 4 T4.5 archetype roster.</summary>
        static readonly HashSet<string> _knownKinds = new HashSet<string>
        {
            "knob", "fader", "detent-ring",
            "vu-meter", "oscilloscope",
            "illuminated-button", "led", "segmented-readout",
            "themed-overlay-toggle-row",
            // Stage 8 Themed* modal primitive kinds.
            "themed-button", "themed-label", "themed-slider",
            "themed-toggle", "themed-tab-bar", "themed-list",
            // Stage 9 (game-ui-design-system) Themed* tooltip kind.
            "themed-tooltip",
            // Wave A1 (TECH-27064) archetypes.
            "view-slot", "confirm-button",
            // Wave A2 (TECH-27069) form + settings archetypes.
            "card-picker", "chip-picker", "text-input",
            "toggle-row", "slider-row", "dropdown-row", "section-header",
            // Wave A3 (TECH-27074) save-load-view archetypes.
            "save-controls-strip", "save-list",
            // Wave B1 (TECH-27079) subtype-picker-strip archetype.
            "subtype-picker-strip",
            // Wave B2 (TECH-27083) stats-panel archetypes.
            "tab-strip", "chart", "range-tabs", "stacked-bar-row", "service-row",
            // Wave B3 (TECH-27088) budget-panel archetypes.
            "slider-row-numeric", "expense-row", "readout-block",
            // Wave B5 (TECH-27098) HUD widget archetypes.
            "info-dock", "field-list", "minimap-canvas", "toast-stack", "toast-card",
        };

        static bool IsKnownStudioControlKind(string kind)
        {
            return !string.IsNullOrEmpty(kind) && _knownKinds.Contains(kind);
        }

        /// <summary>Map IR `panel.kind` string to <see cref="PanelKind"/> enum index. Default = Modal (0).</summary>
        static int ResolvePanelKindIndex(string kind)
        {
            if (string.IsNullOrEmpty(kind)) return (int)PanelKind.Modal;
            switch (kind)
            {
                case "modal": return (int)PanelKind.Modal;
                case "screen": return (int)PanelKind.Screen;
                case "hud": return (int)PanelKind.Hud;
                case "toolbar": return (int)PanelKind.Toolbar;
                case "side-rail":
                case "side_rail":
                case "sideRail": return (int)PanelKind.SideRail;
                default:
                    Debug.LogWarning($"[UiBakeHandler] panel.kind '{kind}' unknown — defaulting to modal");
                    return (int)PanelKind.Modal;
            }
        }

        /// <summary>
        /// Bake one IR interactive into a typed StudioControl prefab. Per-kind switch dispatches to typed
        /// <see cref="IDetailRow"/> parse via <see cref="JsonUtility.FromJson{T}(string)"/> against the
        /// per-row detail substring extracted from raw IR JSON (open-shape <c>detail</c> block cannot
        /// round-trip through JsonUtility otherwise). Schema-mismatch (missing required field) returns
        /// <c>interactive_schema_violation</c>.
        /// </summary>
        public static BakeError BakeInteractive(IrInteractive irRow, int interactiveIndex, string assetPath, string rawIrJson, UiTheme theme)
        {
            if (irRow == null)
            {
                return new BakeError
                {
                    error = "missing_arg",
                    details = "irRow_null",
                    path = $"$.interactives[{interactiveIndex}]",
                };
            }

            var detailJson = ExtractInteractiveDetailJson(rawIrJson, interactiveIndex);
            if (detailJson == null)
            {
                return new BakeError
                {
                    error = "interactive_detail_missing",
                    details = $"kind={irRow.kind} slug={irRow.slug}",
                    path = $"$.interactives[{interactiveIndex}].detail",
                };
            }

            // Themed primitive composite branch — diverges from StudioControlBase ceremony (no _slug field,
            // no IDetailRow, sibling components are Themed primitives + renderer). Early-return on success.
            if (irRow.kind == "themed-overlay-toggle-row")
            {
                return BakeThemedOverlayToggleRow(irRow, assetPath, theme);
            }

            // Stage 8 Themed* modal primitive branch (themed-button, themed-label, themed-slider,
            // themed-toggle, themed-tab-bar, themed-list) — same ceremony skip; renderer-sibling
            // injected per §Pending Decisions audit table in BakeStage8ThemedPrimitive.
            if (irRow.kind == "themed-button" || irRow.kind == "themed-label" ||
                irRow.kind == "themed-slider" || irRow.kind == "themed-toggle" ||
                irRow.kind == "themed-tab-bar" || irRow.kind == "themed-list")
            {
                return BakeStage8ThemedPrimitive(irRow, assetPath, theme);
            }

            // Wave A1 (TECH-27064) — view-slot + confirm-button archetypes.
            if (irRow.kind == "view-slot")
                return BakeViewSlot(irRow, assetPath, theme);
            if (irRow.kind == "confirm-button")
                return BakeConfirmButton(irRow, assetPath, theme);

            // Wave A2 (TECH-27069) — form + settings archetypes.
            if (irRow.kind == "card-picker")
                return BakeCardPicker(irRow, assetPath, theme);
            if (irRow.kind == "chip-picker")
                return BakeChipPicker(irRow, assetPath, theme);
            if (irRow.kind == "text-input")
                return BakeTextInput(irRow, assetPath, theme);
            if (irRow.kind == "toggle-row")
                return BakeToggleRow(irRow, assetPath, theme);
            if (irRow.kind == "slider-row")
                return BakeSliderRow(irRow, assetPath, theme);
            if (irRow.kind == "dropdown-row")
                return BakeDropdownRow(irRow, assetPath, theme);
            if (irRow.kind == "section-header")
                return BakeSectionHeader(irRow, assetPath, theme);

            // Wave A3 (TECH-27074) save-load-view archetypes.
            if (irRow.kind == "save-controls-strip")
                return BakeSaveControlsStrip(irRow, assetPath, theme);
            if (irRow.kind == "save-list")
                return BakeSaveList(irRow, assetPath, theme);

            // Wave B1 (TECH-27079) — subtype-picker-strip archetype.
            if (irRow.kind == "subtype-picker-strip")
                return BakeSubtypePickerStrip(irRow, assetPath, theme);

            // Wave B2 (TECH-27083) — stats-panel archetypes (tab-strip / chart / range-tabs / stacked-bar-row / service-row).
            if (irRow.kind == "tab-strip")
                return BakeTabStrip(irRow, assetPath, theme);
            if (irRow.kind == "chart")
                return BakeChart(irRow, assetPath, theme);
            if (irRow.kind == "range-tabs")
                return BakeRangeTabs(irRow, assetPath, theme);
            if (irRow.kind == "stacked-bar-row")
                return BakeStackedBarRow(irRow, assetPath, theme);
            if (irRow.kind == "service-row")
                return BakeServiceRow(irRow, assetPath, theme);

            // Wave B4 (TECH-27093) — modal-card layout container archetype.
            if (irRow.kind == "modal-card")
                return BakeModalCard(irRow, assetPath, theme);

            GameObject go = null;
            try
            {
                go = new GameObject(irRow.slug);
                go.AddComponent<RectTransform>();

                BakeError schemaError = null;
                StudioControlBase variant = null;
                IDetailRow detailRow = null;

                switch (irRow.kind)
                {
                    case "knob":
                    {
                        var kd = JsonUtility.FromJson<KnobDetail>(detailJson) ?? new KnobDetail();
                        schemaError = AssertKnobFaderFields(irRow.kind, irRow.slug, detailJson, interactiveIndex);
                        if (schemaError != null) return schemaError;
                        variant = go.AddComponent<Knob>();
                        detailRow = kd;
                        break;
                    }
                    case "fader":
                    {
                        var fd = JsonUtility.FromJson<FaderDetail>(detailJson) ?? new FaderDetail();
                        schemaError = AssertKnobFaderFields(irRow.kind, irRow.slug, detailJson, interactiveIndex);
                        if (schemaError != null) return schemaError;
                        variant = go.AddComponent<Fader>();
                        detailRow = fd;
                        break;
                    }
                    case "detent-ring":
                    {
                        var dr = JsonUtility.FromJson<DetentRingDetail>(detailJson) ?? new DetentRingDetail();
                        schemaError = AssertField(detailJson, "detents", irRow.kind, irRow.slug, interactiveIndex);
                        if (schemaError != null) return schemaError;
                        variant = go.AddComponent<DetentRing>();
                        detailRow = dr;
                        break;
                    }
                    case "vu-meter":
                    {
                        var vd = JsonUtility.FromJson<VUMeterDetail>(detailJson) ?? new VUMeterDetail();
                        schemaError = AssertVUMeterFields(irRow.slug, detailJson, interactiveIndex);
                        if (schemaError != null) return schemaError;
                        variant = go.AddComponent<VUMeter>();
                        var vuRend = go.GetComponent<VUMeterRenderer>();
                        if (vuRend == null)
                        {
                            vuRend = go.AddComponent<VUMeterRenderer>();
                        }
                        WireThemeRef(vuRend, theme);
                        SpawnVUMeterRenderTargets(go);
                        detailRow = vd;
                        break;
                    }
                    case "oscilloscope":
                    {
                        var od = JsonUtility.FromJson<OscilloscopeDetail>(detailJson) ?? new OscilloscopeDetail();
                        schemaError = AssertOscilloscopeFields(irRow.slug, detailJson, interactiveIndex);
                        if (schemaError != null) return schemaError;
                        variant = go.AddComponent<Oscilloscope>();
                        detailRow = od;
                        break;
                    }
                    case "illuminated-button":
                    {
                        var bd = JsonUtility.FromJson<IlluminatedButtonDetail>(detailJson) ?? new IlluminatedButtonDetail();
                        schemaError = AssertField(detailJson, "illuminationSlug", irRow.kind, irRow.slug, interactiveIndex);
                        if (schemaError != null) return schemaError;
                        variant = go.AddComponent<IlluminatedButton>();
                        var btnRend = go.GetComponent<IlluminatedButtonRenderer>();
                        if (btnRend == null)
                        {
                            btnRend = go.AddComponent<IlluminatedButtonRenderer>();
                        }
                        WireThemeRef(btnRend, theme);
                        SpawnIlluminatedButtonRenderTargets(go, bd.iconSpriteSlug, out var ibBody, out var ibHalo);
                        WireIlluminatedButtonHoverAndPress(go, btnRend, ibBody, ibHalo, theme);
                        detailRow = bd;
                        break;
                    }
                    case "led":
                    {
                        var ld = JsonUtility.FromJson<LEDDetail>(detailJson) ?? new LEDDetail();
                        schemaError = AssertField(detailJson, "illuminationSlug", irRow.kind, irRow.slug, interactiveIndex);
                        if (schemaError != null) return schemaError;
                        variant = go.AddComponent<LED>();
                        detailRow = ld;
                        break;
                    }
                    case "segmented-readout":
                    {
                        var sd = JsonUtility.FromJson<SegmentedReadoutDetail>(detailJson) ?? new SegmentedReadoutDetail();
                        schemaError = AssertField(detailJson, "digits", irRow.kind, irRow.slug, interactiveIndex);
                        if (schemaError != null) return schemaError;
                        variant = go.AddComponent<SegmentedReadout>();
                        var srRend = go.GetComponent<SegmentedReadoutRenderer>();
                        if (srRend == null)
                        {
                            srRend = go.AddComponent<SegmentedReadoutRenderer>();
                        }
                        WireThemeRef(srRend, theme);
                        SpawnSegmentedReadoutRenderTargets(go, sd);
                        detailRow = sd;
                        break;
                    }
                    default:
                        return new BakeError
                        {
                            error = "interactive_unknown_kind",
                            details = $"kind={irRow.kind} slug={irRow.slug}",
                            path = $"$.interactives[{interactiveIndex}].kind",
                        };
                }

                // Apply detail row + write per-usage slug + theme ref via SerializedObject for
                // deterministic re-bake. _themeRef wire-up (Step 8 fix) must happen here so
                // runtime Awake skips the unreliable FindObjectOfType<UiTheme>() fallback.
                variant.ApplyDetail(detailRow);
                var so = new SerializedObject(variant);
                var slugProp = so.FindProperty("_slug");
                if (slugProp != null)
                {
                    slugProp.stringValue = irRow.slug ?? string.Empty;
                }
                var themeRefProp = so.FindProperty("_themeRef");
                if (themeRefProp != null && theme != null)
                {
                    themeRefProp.objectReferenceValue = theme;
                }
                so.ApplyModifiedPropertiesWithoutUndo();

                // Stage 5 T5.5 — bake-time juice attachment per per-kind defaults + IR juice[] override.
                AttachJuiceComponents(go, irRow);

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

        /// <summary>Extract per-row <c>detail</c> JSON substring from raw IR text. Returns null on miss.</summary>
        public static string ExtractInteractiveDetailJson(string rawJson, int interactiveIndex)
        {
            if (string.IsNullOrEmpty(rawJson) || interactiveIndex < 0) return null;

            // Locate `"interactives"` array start.
            var interactivesMatch = Regex.Match(rawJson, "\"interactives\"\\s*:\\s*\\[");
            if (!interactivesMatch.Success) return null;

            int cursor = interactivesMatch.Index + interactivesMatch.Length;
            int depth = 1;
            int objectStart = -1;
            int objectsSeen = -1;

            for (int i = cursor; i < rawJson.Length && depth > 0; i++)
            {
                char c = rawJson[i];
                if (c == '[')
                {
                    depth++;
                }
                else if (c == ']')
                {
                    depth--;
                    if (depth == 0) break;
                }
                else if (c == '{' && depth == 1)
                {
                    objectsSeen++;
                    if (objectsSeen == interactiveIndex)
                    {
                        objectStart = i;
                        // Walk the object to find its end + locate "detail" key.
                        int objDepth = 1;
                        int detailObjStart = -1;
                        int detailObjEnd = -1;
                        for (int j = i + 1; j < rawJson.Length && objDepth > 0; j++)
                        {
                            char cc = rawJson[j];
                            if (cc == '{') objDepth++;
                            else if (cc == '}') { objDepth--; if (objDepth == 0) { i = j; break; } }
                            else if (objDepth == 1 && detailObjStart < 0)
                            {
                                // Look for `"detail"` key at top level of this interactive object.
                                if (cc == '"')
                                {
                                    var keyMatch = Regex.Match(
                                        rawJson.Substring(j),
                                        "^\"detail\"\\s*:\\s*\\{");
                                    if (keyMatch.Success)
                                    {
                                        // Detail object opens at the `{` after the colon.
                                        int braceIdx = j + keyMatch.Length - 1;
                                        detailObjStart = braceIdx;
                                        int innerDepth = 1;
                                        for (int k = braceIdx + 1; k < rawJson.Length; k++)
                                        {
                                            char kc = rawJson[k];
                                            if (kc == '{') innerDepth++;
                                            else if (kc == '}')
                                            {
                                                innerDepth--;
                                                if (innerDepth == 0)
                                                {
                                                    detailObjEnd = k;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (detailObjStart >= 0 && detailObjEnd > detailObjStart)
                        {
                            return rawJson.Substring(detailObjStart, detailObjEnd - detailObjStart + 1);
                        }
                        return null;
                    }
                    else
                    {
                        // Skip this object (not the target index).
                        int objDepth = 1;
                        for (int j = i + 1; j < rawJson.Length && objDepth > 0; j++)
                        {
                            char cc = rawJson[j];
                            if (cc == '{') objDepth++;
                            else if (cc == '}') { objDepth--; if (objDepth == 0) { i = j; break; } }
                        }
                    }
                }
            }

            return null;
        }

        static BakeError AssertField(string detailJson, string fieldName, string kind, string slug, int interactiveIndex)
        {
            if (string.IsNullOrEmpty(detailJson)) return null;
            var pattern = $"\"{Regex.Escape(fieldName)}\"\\s*:";
            if (Regex.IsMatch(detailJson, pattern)) return null;
            return new BakeError
            {
                error = "interactive_schema_violation",
                details = $"kind={kind} slug={slug} missing={fieldName}",
                path = $"$.interactives[{interactiveIndex}].detail",
            };
        }

        static BakeError AssertKnobFaderFields(string kind, string slug, string detailJson, int interactiveIndex)
        {
            var min = AssertField(detailJson, "min", kind, slug, interactiveIndex);
            if (min != null) return min;
            var max = AssertField(detailJson, "max", kind, slug, interactiveIndex);
            if (max != null) return max;
            return AssertField(detailJson, "step", kind, slug, interactiveIndex);
        }

        static BakeError AssertVUMeterFields(string slug, string detailJson, int interactiveIndex)
        {
            var a = AssertField(detailJson, "attackMs", "vu-meter", slug, interactiveIndex);
            if (a != null) return a;
            var r = AssertField(detailJson, "releaseMs", "vu-meter", slug, interactiveIndex);
            if (r != null) return r;
            return AssertField(detailJson, "range", "vu-meter", slug, interactiveIndex);
        }

        static BakeError AssertOscilloscopeFields(string slug, string detailJson, int interactiveIndex)
        {
            var s = AssertField(detailJson, "sampleCount", "oscilloscope", slug, interactiveIndex);
            if (s != null) return s;
            var sw = AssertField(detailJson, "sweepRateHz", "oscilloscope", slug, interactiveIndex);
            if (sw != null) return sw;
            return AssertField(detailJson, "range", "oscilloscope", slug, interactiveIndex);
        }

        // ── Juice attachment (Stage 5 T5.5) ────────────────────────────────────

        /// <summary>Juice slug constants — Stage 5 juice attachment roster (post-MVP additive field).</summary>
        public const string JuiceKindNeedleBallistics = "needle-ballistics";
        public const string JuiceKindOscilloscopeSweep = "oscilloscope-sweep";
        public const string JuiceKindPulseOnEvent = "pulse-on-event";
        public const string JuiceKindTweenCounter = "tween-counter";
        public const string JuiceKindShadowDepth = "shadow-depth";
        public const string JuiceKindSparkleBurst = "sparkle-burst";

        /// <summary>One entry in the per-kind default juice attachment table.</summary>
        struct JuiceDefault
        {
            public string juiceKind;
            public string defaultCurveSlug;
            public bool gatedOptIn; // true → not attached unless IR juice[] explicitly enables.
        }

        /// <summary>Per-interactive-kind default juice attachments. ShadowDepth on themed-panel is gated (default-off).</summary>
        static readonly Dictionary<string, JuiceDefault[]> _kindToJuiceDefaults = new Dictionary<string, JuiceDefault[]>
        {
            ["vu-meter"] = new[]
            {
                new JuiceDefault { juiceKind = JuiceKindNeedleBallistics, defaultCurveSlug = NeedleBallistics.DefaultCurveSlug },
            },
            ["oscilloscope"] = new[]
            {
                new JuiceDefault { juiceKind = JuiceKindOscilloscopeSweep, defaultCurveSlug = string.Empty },
            },
            ["illuminated-button"] = new[]
            {
                new JuiceDefault { juiceKind = JuiceKindPulseOnEvent, defaultCurveSlug = string.Empty },
            },
            ["segmented-readout"] = new[]
            {
                new JuiceDefault { juiceKind = JuiceKindTweenCounter, defaultCurveSlug = string.Empty },
            },
            // themed-panel → ShadowDepth gated (default off; opt-in via IR juice[] entry)
            ["themed-panel"] = new[]
            {
                new JuiceDefault { juiceKind = JuiceKindShadowDepth, defaultCurveSlug = string.Empty, gatedOptIn = true },
            },
        };

        /// <summary>
        /// Attach JuiceLayer components to a baked prefab root per per-kind defaults + IR <c>juice[]</c>
        /// overrides. Idempotent — existing components of the same juice type are skipped (re-bake safe).
        /// </summary>
        static void AttachJuiceComponents(GameObject prefabRoot, IrInteractive irRow)
        {
            if (prefabRoot == null || irRow == null) return;

            // Build map of override declarations keyed by juice_kind for fast lookup.
            var overrides = new Dictionary<string, IrJuiceDecl>();
            if (irRow.juice != null)
            {
                foreach (var decl in irRow.juice)
                {
                    if (decl == null || string.IsNullOrEmpty(decl.juice_kind)) continue;
                    overrides[decl.juice_kind] = decl;
                }
            }

            // Per-kind default attachments — skip when override marks disabled.
            if (_kindToJuiceDefaults.TryGetValue(irRow.kind ?? string.Empty, out var defaults))
            {
                foreach (var def in defaults)
                {
                    bool enabled = !def.gatedOptIn;
                    string curveSlug = def.defaultCurveSlug;

                    if (overrides.TryGetValue(def.juiceKind, out var ov))
                    {
                        if (ov.disabled) continue;
                        enabled = true;
                        if (!string.IsNullOrEmpty(ov.curve_slug)) curveSlug = ov.curve_slug;
                    }

                    if (!enabled) continue;
                    AttachOneJuice(prefabRoot, def.juiceKind, curveSlug);
                }
            }

            // Override-only attachments (juice kinds not in the per-kind default table).
            foreach (var kv in overrides)
            {
                if (kv.Value.disabled) continue;
                bool isDefault = false;
                if (defaults != null)
                {
                    foreach (var def in defaults)
                    {
                        if (def.juiceKind == kv.Key) { isDefault = true; break; }
                    }
                }
                if (isDefault) continue;
                AttachOneJuice(prefabRoot, kv.Key, kv.Value.curve_slug ?? string.Empty);
            }
        }

        /// <summary>Attach a single juice component by slug. No-op when the juice slug is unknown or already attached.</summary>
        static void AttachOneJuice(GameObject prefabRoot, string juiceKind, string curveSlug)
        {
            switch (juiceKind)
            {
                case JuiceKindNeedleBallistics:
                {
                    if (prefabRoot.GetComponent<NeedleBallistics>() != null) return;
                    var c = prefabRoot.AddComponent<NeedleBallistics>();
                    WriteJuiceCurveSlug(c, curveSlug);
                    break;
                }
                case JuiceKindOscilloscopeSweep:
                {
                    if (prefabRoot.GetComponent<OscilloscopeSweep>() != null) return;
                    var c = prefabRoot.AddComponent<OscilloscopeSweep>();
                    WriteJuiceCurveSlug(c, curveSlug);
                    break;
                }
                case JuiceKindPulseOnEvent:
                {
                    if (prefabRoot.GetComponent<PulseOnEvent>() != null) return;
                    var c = prefabRoot.AddComponent<PulseOnEvent>();
                    WriteJuiceCurveSlug(c, curveSlug);
                    break;
                }
                case JuiceKindTweenCounter:
                {
                    if (prefabRoot.GetComponent<TweenCounter>() != null) return;
                    var c = prefabRoot.AddComponent<TweenCounter>();
                    WriteJuiceCurveSlug(c, curveSlug);
                    break;
                }
                case JuiceKindShadowDepth:
                {
                    if (prefabRoot.GetComponent<ShadowDepth>() != null) return;
                    var c = prefabRoot.AddComponent<ShadowDepth>();
                    WriteJuiceCurveSlug(c, curveSlug);
                    break;
                }
                case JuiceKindSparkleBurst:
                {
                    if (prefabRoot.GetComponent<SparkleBurst>() != null) return;
                    var c = prefabRoot.AddComponent<SparkleBurst>();
                    WriteJuiceCurveSlug(c, curveSlug);
                    break;
                }
                default:
                    Debug.LogWarning($"[UiBakeHandler] juice_kind unknown — skipping (juice_kind={juiceKind})");
                    break;
            }
        }

        /// <summary>Write the <c>curveSlug</c> SerializedProperty on an attached JuiceLayer component when non-empty.</summary>
        static void WriteJuiceCurveSlug(JuiceBase juice, string curveSlug)
        {
            if (juice == null || string.IsNullOrEmpty(curveSlug)) return;
            var so = new SerializedObject(juice);
            var prop = so.FindProperty("curveSlug");
            if (prop == null) return;
            prop.stringValue = curveSlug;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── Render-target child spawners (Stage 10 T10.3) ────────────────────────

        /// <summary>Spawn the VU-meter needle child + body child under the prefab root. Idempotent — re-bake returns early if children already exist (matches Awake-time resolver in <see cref="VUMeterRenderer"/>).</summary>
        static void SpawnVUMeterRenderTargets(GameObject prefabRoot)
        {
            if (prefabRoot == null) return;
            var rootRect = prefabRoot.GetComponent<RectTransform>();
            if (rootRect == null) return;

            // body child — full-rect Image (sized to parent).
            if (prefabRoot.transform.Find("body") == null)
            {
                var body = new GameObject("body", typeof(RectTransform));
                body.transform.SetParent(prefabRoot.transform, worldPositionStays: false);
                var br = (RectTransform)body.transform;
                br.anchorMin = new Vector2(0f, 0f);
                br.anchorMax = new Vector2(1f, 1f);
                br.pivot = new Vector2(0.5f, 0.5f);
                br.anchoredPosition = Vector2.zero;
                br.sizeDelta = Vector2.zero;
                body.AddComponent<Image>();
            }

            // needle child — pivot at base; renderer rotates around Z.
            if (prefabRoot.transform.Find("needle") == null)
            {
                var needle = new GameObject("needle", typeof(RectTransform));
                needle.transform.SetParent(prefabRoot.transform, worldPositionStays: false);
                var nr = (RectTransform)needle.transform;
                nr.anchorMin = new Vector2(0.5f, 0f);
                nr.anchorMax = new Vector2(0.5f, 0f);
                nr.pivot = new Vector2(0.5f, 0f);
                nr.anchoredPosition = Vector2.zero;
                nr.sizeDelta = new Vector2(2f, 64f);
            }
        }

        /// <summary>Spawn the illuminated-button main body Image child + optional icon child + halo child
        /// under the prefab root. Step 16 D3.1 — returns refs so the caller can serialize them onto
        /// <see cref="IlluminatedButtonRenderer"/> at bake time, eliminating the runtime
        /// <c>GetComponentsInChildren</c> + name-match scan that caused partial-hover symptoms when child
        /// ordering shifted between scene-instance + prefab-asset paths.
        /// Step 16.D — when <paramref name="iconSpriteSlug"/> is non-empty, resolve the human-art icon
        /// sprite via <see cref="AssetDatabase"/> (Editor-only, bake-time) at
        /// <c>Assets/Sprites/Buttons/{slug}-target.png</c> with fallback <c>Assets/Sprites/{slug}-target.png</c>
        /// and assign to the icon Image. Sprite ref is serialized into the prefab — no runtime
        /// <c>Resources.Load</c> + no PlayMode-only API.</summary>
        // BUG-61 W6+W7 — return value: true when icon sprite resolved to a real asset; false when
        // slug empty OR ResolveButtonIconSprite returned null (missing target sprite). Caller
        // (Step 16.G in Frame.cs) uses this to decide whether to fall back to a TMP caption so
        // the button still signals function while sprite art is pending.
        static bool SpawnIlluminatedButtonRenderTargets(
            GameObject prefabRoot,
            string iconSpriteSlug,
            out Image bodyImage,
            out Image haloImage)
        {
            bodyImage = null;
            haloImage = null;
            bool spriteResolved = false;
            if (prefabRoot == null) return false;
            var rootRect = prefabRoot.GetComponent<RectTransform>();
            if (rootRect == null) return false;

            // main body Image child.
            var bodyT = prefabRoot.transform.Find("body");
            if (bodyT == null)
            {
                var body = new GameObject("body", typeof(RectTransform));
                body.transform.SetParent(prefabRoot.transform, worldPositionStays: false);
                var br = (RectTransform)body.transform;
                br.anchorMin = new Vector2(0f, 0f);
                br.anchorMax = new Vector2(1f, 1f);
                br.pivot = new Vector2(0.5f, 0.5f);
                br.anchoredPosition = Vector2.zero;
                br.sizeDelta = Vector2.zero;
                bodyImage = body.AddComponent<Image>();
            }
            else
            {
                bodyImage = bodyT.GetComponent<Image>();
                if (bodyImage == null) bodyImage = bodyT.gameObject.AddComponent<Image>();
            }

            // Body visual default — rounded 9-slice sprite + cream fill (per design spec line 339:
            // "cream body + tan border + indigo icon"). UISprite.psd ships with Unity as a 9-slice
            // rounded-corner asset; same primitive UiBakeHandler.Frame uses for border strips.
            // Idempotent: only assign when sprite slot is empty so authored prefabs / future custom
            // body sprites are never clobbered. Color drives _mainImage on the renderer.
            if (bodyImage != null && bodyImage.sprite == null)
            {
                bodyImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                bodyImage.type = Image.Type.Sliced;
                bodyImage.color = new Color(0.96f, 0.92f, 0.83f, 1f); // cream
                bodyImage.raycastTarget = true; // body owns hit-testing.
            }

            // icon child — Step 16.D human-art sprite host. Layered between body + halo so the click
            // pulse (halo) draws on top. Only spawned when iconSpriteSlug is provided.
            var iconT = prefabRoot.transform.Find("icon");
            if (!string.IsNullOrEmpty(iconSpriteSlug))
            {
                Image iconImage;
                if (iconT == null)
                {
                    var icon = new GameObject("icon", typeof(RectTransform));
                    icon.transform.SetParent(prefabRoot.transform, worldPositionStays: false);
                    var ir = (RectTransform)icon.transform;
                    ir.anchorMin = Vector2.zero;
                    ir.anchorMax = Vector2.one;
                    ir.pivot = new Vector2(0.5f, 0.5f);
                    ir.anchoredPosition = Vector2.zero;
                    ir.sizeDelta = Vector2.zero;
                    iconImage = icon.AddComponent<Image>();
                    iconImage.raycastTarget = false; // body owns hit-testing.
                    iconImage.preserveAspect = true;
                }
                else
                {
                    iconImage = iconT.GetComponent<Image>();
                    if (iconImage == null) iconImage = iconT.gameObject.AddComponent<Image>();
                }

                var sprite = ResolveButtonIconSprite(iconSpriteSlug);
                if (sprite != null)
                {
                    iconImage.sprite = sprite;
                    spriteResolved = true;
                }
                else
                {
                    Debug.LogWarning(
                        $"[UiBakeHandler] illuminated-button icon sprite not found (slug={iconSpriteSlug}); "
                        + "expected Assets/Sprites/Buttons/{slug}-target.png or Assets/Sprites/{slug}-target.png");
                }
            }

            // halo child — radial pulse target. Drawn on top of body + icon.
            // Stretches to fill parent rect with a small outset so hover/press alpha covers
            // the entire button (not a fixed 64 px square at center). offsetMin/Max negative
            // values bleed the halo a few px beyond the body edge for a soft glow look.
            var haloT = prefabRoot.transform.Find("halo");
            if (haloT == null)
            {
                var halo = new GameObject("halo", typeof(RectTransform));
                halo.transform.SetParent(prefabRoot.transform, worldPositionStays: false);
                var hr = (RectTransform)halo.transform;
                hr.anchorMin = new Vector2(0f, 0f);
                hr.anchorMax = new Vector2(1f, 1f);
                hr.pivot = new Vector2(0.5f, 0.5f);
                hr.anchoredPosition = Vector2.zero;
                hr.offsetMin = new Vector2(-4f, -4f);
                hr.offsetMax = new Vector2(4f, 4f);
                var img = halo.AddComponent<Image>();
                img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                img.type = Image.Type.Sliced;
                var color = new Color(1f, 0.95f, 0.78f, 0f); // warm halo, idle alpha 0.
                img.color = color;
                img.raycastTarget = false;
                haloImage = img;
            }
            else
            {
                haloImage = haloT.GetComponent<Image>();
                if (haloImage != null && haloImage.sprite == null)
                {
                    haloImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                    haloImage.type = Image.Type.Sliced;
                }
                // Re-bake: re-stretch halo to fill parent (prior bakes used fixed 64×64 centered
                // which made hover/press alpha visible only as a small square at button center).
                var hr = (RectTransform)haloT;
                hr.anchorMin = new Vector2(0f, 0f);
                hr.anchorMax = new Vector2(1f, 1f);
                hr.pivot = new Vector2(0.5f, 0.5f);
                hr.anchoredPosition = Vector2.zero;
                hr.offsetMin = new Vector2(-4f, -4f);
                hr.offsetMax = new Vector2(4f, 4f);
            }

            // Ensure render order: body (back) → icon → halo (front). SetAsLastSibling on halo
            // re-asserts the contract regardless of pre-existing child order.
            if (haloImage != null) haloImage.transform.SetAsLastSibling();
            return spriteResolved;
        }

        /// <summary>Resolve a human-art button sprite by slug. Search order:
        /// (1) <c>Assets/Sprites/Buttons/{slug}-target.png</c> (primary canon).
        /// (2) <c>Assets/Sprites/{slug}-target.png</c> (root catch-all).
        /// (3) <see cref="AssetDatabase.FindAssets"/> name-filtered scan across all sibling
        /// subfolders under <c>Assets/Sprites/**</c> (e.g. <c>Assets/Sprites/Commercial/Commercial-button-64-target.png</c>).
        /// First .png hit wins. Editor-only.</summary>
        static Sprite ResolveButtonIconSprite(string slug)
        {
            if (string.IsNullOrEmpty(slug)) return null;
            var primary = $"Assets/Sprites/Buttons/{slug}-target.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(primary);
            if (sprite != null) return sprite;
            var fallback = $"Assets/Sprites/{slug}-target.png";
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(fallback);
            if (sprite != null) return sprite;
            // Step 16.D extension — sibling-folder scan (sprites organized under family folders
            // like Assets/Sprites/Commercial/Commercial-button-64-target.png).
            var guids = AssetDatabase.FindAssets($"{slug}-target t:Sprite", new[] { "Assets/Sprites" });
            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path)) continue;
                if (!path.EndsWith($"{slug}-target.png", System.StringComparison.OrdinalIgnoreCase)) continue;
                var found = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (found != null) return found;
            }
            // F11 (bake-fix-2026-05-07) — variant-suffix shape `{slug}-button-{variant}-{size}-target.png`.
            // Existing icon catalogue under Assets/Sprites/Buttons follows this naming
            // (zoom-in-button-1-64-target.png, pause-button-1-64-target.png, etc).
            var variantGuids = AssetDatabase.FindAssets($"{slug}-button t:Sprite", new[] { "Assets/Sprites" });
            for (int i = 0; i < variantGuids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(variantGuids[i]);
                if (string.IsNullOrEmpty(path)) continue;
                var fname = System.IO.Path.GetFileName(path);
                if (!fname.StartsWith($"{slug}-button-", System.StringComparison.OrdinalIgnoreCase)) continue;
                if (!fname.EndsWith("-target.png", System.StringComparison.OrdinalIgnoreCase)) continue;
                var found = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>Step 16 D3.1+D3.2 — wire IlluminatedButton hover/press at bake time.
        /// Serializes <c>_mainImage</c>+<c>_haloImage</c> refs onto <paramref name="renderer"/> and adds a
        /// <see cref="UnityEngine.UI.Button"/> with <see cref="Selectable.Transition.ColorTint"/> sourced
        /// from the body palette ramp (last=normal, last-1=highlighted/selected, last-2=pressed,
        /// 0=disabled). Reuses <c>button_state_block</c> conformance check + <c>button_states_wired</c>
        /// targeted check; collapses two hover paths (custom pointer handlers + Selectable) into one.</summary>
        static void WireIlluminatedButtonHoverAndPress(
            GameObject hostGo,
            IlluminatedButtonRenderer renderer,
            Image bodyImage,
            Image haloImage,
            UiTheme theme)
        {
            if (renderer != null)
            {
                var rendSo = new SerializedObject(renderer);
                var bodyProp = rendSo.FindProperty("_mainImage");
                if (bodyProp != null) bodyProp.objectReferenceValue = bodyImage;
                var haloProp = rendSo.FindProperty("_haloImage");
                if (haloProp != null) haloProp.objectReferenceValue = haloImage;
                rendSo.ApplyModifiedPropertiesWithoutUndo();
            }

            if (hostGo == null || bodyImage == null) return;
            var sel = hostGo.GetComponent<UnityEngine.UI.Button>();
            if (sel == null) sel = hostGo.AddComponent<UnityEngine.UI.Button>();
            sel.targetGraphic = bodyImage;
            // Stage 13.2 — IlluminatedButtonRenderer owns hover/press visuals (halo alpha + body alpha
            // via IPointerEnter/Exit/Down/Up). Selectable.Transition.ColorTint races the renderer by
            // overwriting bodyImage.color on hover/select; with selectedColor != normalColor the click
            // leaves the button stuck at the highlight tint forever. Disable Selectable transition so
            // the renderer is the single writer of body color; pointer routing still works because the
            // Button component is present (raycast hit + onClick wiring intact via IPointerClick).
            sel.transition = Selectable.Transition.None;

            var cb = sel.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = Color.white;
            cb.pressedColor = Color.white;
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(0.6f, 0.6f, 0.6f, 1f);
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.1f;
            sel.colors = cb;
            _ = theme; // Palette no longer consumed here — renderer reads body alpha from IlluminatedButton.IlluminationAlpha.
        }

        /// <summary>Wave A0 follow-up — bake-time wiring for params_json.action. Attaches a
        /// <see cref="UiActionTrigger"/> to the host so the rendered button dispatches via
        /// <see cref="UiActionRegistry"/> at runtime. SerializedObject persistence keeps the
        /// actionId on the prefab without scene-side hand-edit. No-op when actionId is empty.</summary>
        static void AttachUiActionTrigger(GameObject hostGo, string actionId)
        {
            if (hostGo == null) return;
            if (string.IsNullOrEmpty(actionId)) return;
            var trigger = hostGo.GetComponent<UiActionTrigger>();
            if (trigger == null) trigger = hostGo.AddComponent<UiActionTrigger>();
            var so = new SerializedObject(trigger);
            var prop = so.FindProperty("_actionId");
            if (prop != null)
            {
                prop.stringValue = actionId;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>Step 16.G — caption fallback for illuminated-button slots that carry a label
        /// in IR but no human-art icon sprite (e.g. AUTO, MAP). Spawns a centered, raycast-inert
        /// TMP_Text child stretched to fill the body so the button still signals its function
        /// while art is pending. Idempotent — re-bake skips when "caption" child already exists.
        /// Halo is re-asserted as last sibling so click pulse still draws on top.</summary>
        static void SpawnIlluminatedButtonCaption(GameObject prefabRoot, string label)
        {
            if (prefabRoot == null) return;
            if (string.IsNullOrEmpty(label)) return;
            var rootRect = prefabRoot.GetComponent<RectTransform>();
            if (rootRect == null) return;

            var existing = prefabRoot.transform.Find("caption");
            TextMeshProUGUI tmp;
            if (existing == null)
            {
                var go = new GameObject("caption", typeof(RectTransform));
                go.transform.SetParent(prefabRoot.transform, worldPositionStays: false);
                var tr = (RectTransform)go.transform;
                tr.anchorMin = new Vector2(0f, 0f);
                tr.anchorMax = new Vector2(1f, 1f);
                tr.pivot = new Vector2(0.5f, 0.5f);
                tr.anchoredPosition = Vector2.zero;
                tr.sizeDelta = Vector2.zero;
                tmp = go.AddComponent<TextMeshProUGUI>();
            }
            else
            {
                tmp = existing.GetComponent<TextMeshProUGUI>();
                if (tmp == null) tmp = existing.gameObject.AddComponent<TextMeshProUGUI>();
            }

            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 14f;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 8f;
            tmp.fontSizeMax = 18f;
            // Caption fallback contrasts against default white body Image (no sprite art assigned).
            // Dark slate keeps labels legible on either white-default or themed pale-tone bodies;
            // when human-art sprite art lands the icon child covers body and caption usually drops.
            tmp.color = new Color(0.10f, 0.12f, 0.16f, 1f);
            tmp.raycastTarget = false; // body owns hit-testing.

            // Re-assert render order: caption sits above body, halo stays on top.
            var haloT = prefabRoot.transform.Find("halo");
            if (haloT != null) haloT.SetAsLastSibling();
        }

        /// <summary>Spawn the segmented-readout TMP child under the prefab root. Idempotent — placeholder text is empty so T10.6 visual smoke (asserts non-empty post-render) is not polluted.</summary>
        static void SpawnSegmentedReadoutRenderTargets(GameObject prefabRoot, SegmentedReadoutDetail detail)
        {
            if (prefabRoot == null) return;
            var rootRect = prefabRoot.GetComponent<RectTransform>();
            if (rootRect == null) return;

            if (prefabRoot.transform.Find("text") != null) return;

            var textGo = new GameObject("text", typeof(RectTransform));
            textGo.transform.SetParent(prefabRoot.transform, worldPositionStays: false);
            var tr = (RectTransform)textGo.transform;
            tr.anchorMin = new Vector2(0f, 0f);
            tr.anchorMax = new Vector2(1f, 1f);
            tr.pivot = new Vector2(0.5f, 0.5f);
            tr.anchoredPosition = Vector2.zero;
            tr.sizeDelta = Vector2.zero;

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            // bake-fix-2026-05-08: placeholder digits visible at bake-time so visual smoke
            // can verify readout shape before runtime ApplyDetail binds the live value.
            tmp.text = "0";
            tmp.alignment = TextAlignmentOptions.Center;
            int digits = detail != null && detail.digits > 0 ? detail.digits : 1;
            // Width hint in font size — ~24pt per digit floor; renderer will populate text on ApplyDetail.
            tmp.fontSize = 24f;
            tmp.color = Color.white;
            // Ensure raycast surface stays minimal; readouts are non-interactive.
            tmp.raycastTarget = false;
            // digits unused at spawn time except as future width hint (current impl uses anchor stretch).
            _ = digits;
        }

        /// <summary>
        /// Bake one ThemedOverlayToggleRow IR row into a typed prefab — composite of
        /// <see cref="ThemedOverlayToggleRow"/> state-holder + <see cref="ThemedOverlayToggleRowRenderer"/>
        /// renderer + spawned child Label (TMP_Text), Icon (Image), Toggle (UnityEngine.UI.Toggle)
        /// GameObjects. No StudioControlBase ceremony — Themed primitive composite branch.
        /// Stage 10 lock honored — bake-time-attached only.
        /// </summary>
        static BakeError BakeThemedOverlayToggleRow(IrInteractive irRow, string assetPath, UiTheme theme)
        {
            GameObject go = null;
            try
            {
                go = new GameObject(irRow.slug);
                go.AddComponent<RectTransform>();

                var row = go.AddComponent<ThemedOverlayToggleRow>();
                WireThemeRef(row, theme);
                var renderer = go.AddComponent<ThemedOverlayToggleRowRenderer>();
                WireThemeRef(renderer, theme);

                // Spawn child Label (TMP_Text), Icon (Image), Toggle (Unity Toggle).
                SpawnThemedOverlayToggleRowChildren(go, out var labelTmp, out var iconImage, out var unityToggle);

                // Wire Inspector refs on the row primitive (toggle/label/icon as Themed siblings would
                // require ThemedToggle/Label/Icon — defer to T7.4 art pass). Renderer side caches direct
                // unity refs (TMP_Text + Image) for state writes.
                var so = new SerializedObject(renderer);
                var labelProp = so.FindProperty("_labelText");
                if (labelProp != null) labelProp.objectReferenceValue = labelTmp;
                var iconProp = so.FindProperty("_iconImage");
                if (iconProp != null) iconProp.objectReferenceValue = iconImage;
                so.ApplyModifiedPropertiesWithoutUndo();

                // Suppress unused-variable warnings — refs reserved for T7.4 Themed primitive cascading.
                _ = row;
                _ = unityToggle;

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

        /// <summary>Spawn Label + Icon + Toggle children under a ThemedOverlayToggleRow prefab root. Idempotent.</summary>
        static void SpawnThemedOverlayToggleRowChildren(
            GameObject prefabRoot,
            out TMP_Text labelTmp,
            out Image iconImage,
            out Toggle unityToggle)
        {
            labelTmp = null;
            iconImage = null;
            unityToggle = null;
            if (prefabRoot == null) return;

            // Label child (left third).
            var labelGo = prefabRoot.transform.Find("Label")?.gameObject;
            if (labelGo == null)
            {
                labelGo = new GameObject("Label", typeof(RectTransform));
                labelGo.transform.SetParent(prefabRoot.transform, worldPositionStays: false);
                var lr = (RectTransform)labelGo.transform;
                lr.anchorMin = new Vector2(0f, 0f);
                lr.anchorMax = new Vector2(0.5f, 1f);
                lr.pivot = new Vector2(0.5f, 0.5f);
                lr.anchoredPosition = Vector2.zero;
                lr.sizeDelta = Vector2.zero;
                labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
                labelTmp.text = string.Empty;
                labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
                labelTmp.fontSize = 14f;
                labelTmp.raycastTarget = false;
            }
            else
            {
                labelTmp = labelGo.GetComponent<TMP_Text>();
            }

            // Icon child (middle).
            var iconGo = prefabRoot.transform.Find("Icon")?.gameObject;
            if (iconGo == null)
            {
                iconGo = new GameObject("Icon", typeof(RectTransform));
                iconGo.transform.SetParent(prefabRoot.transform, worldPositionStays: false);
                var ir = (RectTransform)iconGo.transform;
                ir.anchorMin = new Vector2(0.5f, 0f);
                ir.anchorMax = new Vector2(0.75f, 1f);
                ir.pivot = new Vector2(0.5f, 0.5f);
                ir.anchoredPosition = Vector2.zero;
                ir.sizeDelta = Vector2.zero;
                iconImage = iconGo.AddComponent<Image>();
                iconImage.raycastTarget = false;
            }
            else
            {
                iconImage = iconGo.GetComponent<Image>();
            }

            // Toggle child (right third).
            var toggleGo = prefabRoot.transform.Find("Toggle")?.gameObject;
            if (toggleGo == null)
            {
                toggleGo = new GameObject("Toggle", typeof(RectTransform));
                toggleGo.transform.SetParent(prefabRoot.transform, worldPositionStays: false);
                var tr = (RectTransform)toggleGo.transform;
                tr.anchorMin = new Vector2(0.75f, 0f);
                tr.anchorMax = new Vector2(1f, 1f);
                tr.pivot = new Vector2(0.5f, 0.5f);
                tr.anchoredPosition = Vector2.zero;
                tr.sizeDelta = Vector2.zero;
                // Background image required for Toggle interaction.
                var bgImg = toggleGo.AddComponent<Image>();
                bgImg.raycastTarget = true;
                unityToggle = toggleGo.AddComponent<Toggle>();
                unityToggle.targetGraphic = bgImg;
                unityToggle.isOn = false;
            }
            else
            {
                unityToggle = toggleGo.GetComponent<Toggle>();
            }
        }

        // ── Stage 1.4 T1.4.1 — panel spacing application ────────────────────────

        /// <summary>
        /// Apply <paramref name="panel"/>.detail spacing values to <paramref name="root"/>'s
        /// <see cref="VerticalLayoutGroup"/> and optional divider Image. No-op when detail is null.
        /// Stage 1.4 (T1.4.1).
        /// </summary>
        static void ApplySpacing(IrPanel panel, GameObject root)
        {
            if (panel?.detail == null) return;

            var vlg = root.GetComponent<VerticalLayoutGroup>();
            if (vlg == null) vlg = root.AddComponent<VerticalLayoutGroup>();

            int px = Mathf.RoundToInt(panel.detail.paddingX);
            int py = Mathf.RoundToInt(panel.detail.paddingY);
            vlg.padding = new RectOffset(px, px, py, py);
            vlg.spacing = panel.detail.gap;

            // Apply dividerThickness to the first child Image named "Divider" when present.
            if (panel.detail.dividerThickness > 0f)
            {
                var dividerTf = root.transform.Find("Divider");
                if (dividerTf != null)
                {
                    var rt = dividerTf as RectTransform ?? dividerTf.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        var sd = rt.sizeDelta;
                        sd.y = panel.detail.dividerThickness;
                        rt.sizeDelta = sd;
                    }
                }
            }
        }

        // ── Stage 9.10 T4 — PanelSnapshot archetype slot-wrapper dispatch ────────

        /// <summary>
        /// Emit slot-wrapper GameObjects for a <see cref="PanelSnapshotItem"/> based on panel slug.
        /// Returns an ord→Transform map keyed by <c>child.ord</c> so <see cref="BakePanelSnapshotChildren"/>
        /// can flatten its iteration — the archetype owns the full sub-grid structure (zones, cols,
        /// rows, sub_cols). Returns null for non-archetype-mapped panels (flat spawn).
        ///
        /// Arm <c>hud-bar</c> (bake-fix-2026-05-08):
        ///   Left   = HLG of buttons (zone=left)
        ///   Center = VLG of stacked label rows (zone=center, ordered by layout.row)
        ///   Right  = HLG of cols; each col = VLG of rows; row with ≥2 children = HLG of sub_cols.
        /// </summary>
        static Dictionary<int, Transform> BakePanelSnapshotArchetype(PanelSnapshotItem item, GameObject panelRoot, UiTheme theme)
        {
            if (item == null || string.IsNullOrEmpty(item.slug)) return null;

            switch (item.slug)
            {
                case "hud-bar":
                case "hud_bar": // legacy slug alias — kept for backwards compat
                    return BuildHudBarArchetype(item, panelRoot);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Build hud-bar zone wrappers + per-ord parent map. See <see cref="BakePanelSnapshotArchetype"/>.
        /// </summary>
        static Dictionary<int, Transform> BuildHudBarArchetype(PanelSnapshotItem item, GameObject panelRoot)
        {
            var parentByOrd = new Dictionary<int, Transform>(item.children?.Length ?? 0);

            // Pre-parse child layouts once so zone/col/row/sub_col is available without re-parse.
            var parsed = new List<(PanelSnapshotChild child, PanelChildLayoutJson layout)>();
            if (item.children != null)
            {
                foreach (var c in item.children)
                {
                    if (c == null) continue;
                    var layout = TryParseTypedJson<PanelChildLayoutJson>(c.layout_json) ?? new PanelChildLayoutJson();
                    parsed.Add((c, layout));
                }
            }

            // ── Left zone — HLG, flat list ───────────────────────────────────────
            var leftWrapper = CreateZoneWrapper(panelRoot.transform, "Left", TextAnchor.MiddleLeft);
            foreach (var (c, _) in parsed.Where(p => string.Equals(p.layout.zone, "left", StringComparison.OrdinalIgnoreCase))
                                          .OrderBy(p => p.layout.ord)
                                          .ThenBy(p => p.child.ord))
            {
                parentByOrd[c.ord] = leftWrapper;
            }

            // ── Center zone — VLG, stacked rows ──────────────────────────────────
            var centerWrapper = CreateZoneWrapperVertical(panelRoot.transform, "Center", TextAnchor.MiddleCenter);
            foreach (var (c, _) in parsed.Where(p => string.Equals(p.layout.zone, "center", StringComparison.OrdinalIgnoreCase))
                                          .OrderBy(p => p.layout.row)
                                          .ThenBy(p => p.child.ord))
            {
                parentByOrd[c.ord] = centerWrapper;
            }

            // ── Right zone — HLG of cols; each col = VLG of rows; row with ≥2 children = HLG of sub_cols ─
            var rightWrapper = CreateZoneWrapper(panelRoot.transform, "Right", TextAnchor.MiddleRight);

            var rightChildren = parsed.Where(p => string.Equals(p.layout.zone, "right", StringComparison.OrdinalIgnoreCase)).ToList();
            var byCol = rightChildren.GroupBy(p => p.layout.col).OrderBy(g => g.Key);

            foreach (var colGroup in byCol)
            {
                var colGo = new GameObject($"Col{colGroup.Key}", typeof(RectTransform));
                colGo.transform.SetParent(rightWrapper, worldPositionStays: false);
                ConfigureSubWrapperRect(colGo);
                var colVlg = colGo.AddComponent<VerticalLayoutGroup>();
                colVlg.spacing = 4;
                colVlg.padding = new RectOffset(0, 0, 0, 0);
                colVlg.childControlWidth = true;
                colVlg.childControlHeight = true;
                colVlg.childForceExpandWidth = false;
                colVlg.childForceExpandHeight = false;
                colVlg.childAlignment = TextAnchor.MiddleCenter;

                // bake-fix-2026-05-08: always wrap rows so VLG sibling order = ascending row
                // (children created in BakePanelSnapshotChildren flat-iter would otherwise interleave
                // with row wrappers built during archetype phase → Col1 BUDGET fell below Row1).
                var byRow = colGroup.GroupBy(p => p.layout.row).OrderBy(g => g.Key);
                foreach (var rowGroup in byRow)
                {
                    var rowList = rowGroup.OrderBy(p => p.layout.sub_col).ThenBy(p => p.child.ord).ToList();
                    var rowGo = new GameObject($"Row{rowGroup.Key}", typeof(RectTransform));
                    rowGo.transform.SetParent(colGo.transform, worldPositionStays: false);
                    ConfigureSubWrapperRect(rowGo);
                    var rowHlg = rowGo.AddComponent<HorizontalLayoutGroup>();
                    rowHlg.spacing = 4;
                    rowHlg.padding = new RectOffset(0, 0, 0, 0);
                    rowHlg.childControlWidth = true;
                    rowHlg.childControlHeight = true;
                    rowHlg.childForceExpandWidth = false;
                    rowHlg.childForceExpandHeight = false;
                    rowHlg.childAlignment = TextAnchor.MiddleCenter;
                    foreach (var (c, _) in rowList) parentByOrd[c.ord] = rowGo.transform;
                }
            }

            return parentByOrd;
        }

        /// <summary>
        /// Create a zone-level HLG wrapper under <paramref name="parent"/>. Used for Left + Right zones
        /// of hud-bar — flexible width share, transparent, fixed-row alignment.
        /// </summary>
        static Transform CreateZoneWrapper(Transform parent, string name, TextAnchor align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, worldPositionStays: false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, 0f);

            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            le.flexibleHeight = 1f;
            le.minHeight = 0f;

            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.padding = new RectOffset(4, 4, 0, 0);
            // Drive Col / button widths from preferred size so wrappers + buttons size correctly.
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childAlignment = align;

            return go.transform;
        }

        /// <summary>
        /// Create a zone-level VLG wrapper. Center zone of hud-bar stacks city-name / sim-date / population
        /// readouts top-to-bottom — VLG with MiddleCenter alignment.
        /// </summary>
        static Transform CreateZoneWrapperVertical(Transform parent, string name, TextAnchor align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, worldPositionStays: false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, 0f);

            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            le.flexibleHeight = 1f;
            le.minHeight = 0f;

            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 2;
            vlg.padding = new RectOffset(0, 0, 0, 0);
            // Center labels stretch to zone width so the readouts span the full middle column.
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = align;

            return go.transform;
        }

        /// <summary>
        /// Configure a sub-wrapper RectTransform — fit-to-content, no anchor stretch, used inside
        /// Right zone for col/row sub-wrappers. ContentSizeFitter wraps the wrapper to its
        /// LayoutGroup-driven preferred size so Col/Row collapse to button bounds.
        /// </summary>
        static void ConfigureSubWrapperRect(GameObject go)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;

            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        // ── Wave A1 (TECH-27064) — view-slot + confirm-button archetypes ────────

        /// <summary>
        /// Bake a view-slot prefab: subscribes <c>bind_enum</c> param to drive sub-view visibility.
        /// Hosts N declared sub-views in <c>host_slots</c> map keyed by enum value.
        /// Spawns a root RectTransform (fullscreen) with one child placeholder per host_slot.
        /// Round-trip IR DTO: <see cref="ViewSlotDetail"/>.
        /// </summary>
        static BakeError BakeViewSlot(IrInteractive irRow, string assetPath, UiTheme theme)
        {
            GameObject go = null;
            try
            {
                go = new GameObject(irRow.slug);
                go.AddComponent<RectTransform>();

                // Fullscreen anchor stretch.
                var rt = (RectTransform)go.transform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;

                // Placeholder Image so the slot is visible in Scene view.
                var bg = go.AddComponent<UnityEngine.UI.Image>();
                bg.color = new Color(0f, 0f, 0f, 0f); // transparent placeholder
                bg.raycastTarget = false;

                PrefabUtility.SaveAsPrefabAsset(go, assetPath);
                return null;
            }
            catch (Exception ex)
            {
                return new BakeError { error = "prefab_write_failed", details = ex.Message, path = assetPath };
            }
            finally
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// Bake a confirm-button prefab: button variant with N-second inline countdown.
        /// Fires <c>confirm_action</c> on countdown completion.
        /// Spawns Button + countdown TMP_Text child.
        /// Round-trip IR DTO: <see cref="ConfirmButtonDetail"/>.
        /// </summary>
        static BakeError BakeConfirmButton(IrInteractive irRow, string assetPath, UiTheme theme)
        {
            GameObject go = null;
            try
            {
                go = new GameObject(irRow.slug);
                go.AddComponent<RectTransform>();

                var img = go.AddComponent<UnityEngine.UI.Image>();
                img.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                var btn = go.AddComponent<UnityEngine.UI.Button>();
                btn.targetGraphic = img;
                btn.transition = UnityEngine.UI.Selectable.Transition.ColorTint;

                // Countdown label child.
                var labelGo = new GameObject("CountdownLabel", typeof(RectTransform));
                labelGo.transform.SetParent(go.transform, worldPositionStays: false);
                var lr = (RectTransform)labelGo.transform;
                lr.anchorMin = Vector2.zero;
                lr.anchorMax = Vector2.one;
                lr.offsetMin = lr.offsetMax = Vector2.zero;
                var tmp = labelGo.AddComponent<TMPro.TextMeshProUGUI>();
                tmp.text = string.Empty; // runtime populates with countdown
                tmp.alignment = TMPro.TextAlignmentOptions.Center;
                tmp.fontSize = 18f;
                tmp.color = Color.white;
                tmp.raycastTarget = false;

                PrefabUtility.SaveAsPrefabAsset(go, assetPath);
                return null;
            }
            catch (Exception ex)
            {
                return new BakeError { error = "prefab_write_failed", details = ex.Message, path = assetPath };
            }
            finally
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
        }

        // ── Wave A1 IR DTOs (TECH-27064) ─────────────────────────────────────────

        /// <summary>IR DTO for view-slot archetype. JsonUtility round-trip validated in EditMode tests.</summary>
        [System.Serializable]
        public class ViewSlotDetail
        {
            public string bind_enum;
        }

        /// <summary>IR DTO for confirm-button archetype. JsonUtility round-trip validated in EditMode tests.</summary>
        [System.Serializable]
        public class ConfirmButtonDetail
        {
            public string action;
            public string confirm_action;
            public int confirm_seconds = 3;
        }

        // ── Wave A2 (TECH-27069) — form + settings archetype bake methods ────────

        /// <summary>Bake card-picker: 3-N card grid with selected-bind. Root HLG + N card children.</summary>
        static BakeError BakeCardPicker(IrInteractive irRow, string assetPath, UiTheme theme)
        {
            GameObject go = null;
            try
            {
                go = new GameObject(irRow.slug);
                go.AddComponent<RectTransform>();
                var hlg = go.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 8;
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;
                hlg.childAlignment = TextAnchor.MiddleCenter;
                var bg = go.AddComponent<UnityEngine.UI.Image>();
                bg.color = new Color(0f, 0f, 0f, 0f);
                bg.raycastTarget = false;
                PrefabUtility.SaveAsPrefabAsset(go, assetPath);
                return null;
            }
            catch (Exception ex)
            {
                return new BakeError { error = "prefab_write_failed", details = ex.Message, path = assetPath };
            }
            finally
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
        }

        /// <summary>Bake chip-picker: compact horizontal chips. Root HLG + transparent background.</summary>
        static BakeError BakeChipPicker(IrInteractive irRow, string assetPath, UiTheme theme)
        {
            GameObject go = null;
            try
            {
                go = new GameObject(irRow.slug);
                go.AddComponent<RectTransform>();
                var hlg = go.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 4;
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;
                hlg.childAlignment = TextAnchor.MiddleLeft;
                var bg = go.AddComponent<UnityEngine.UI.Image>();
                bg.color = new Color(0f, 0f, 0f, 0f);
                bg.raycastTarget = false;
                PrefabUtility.SaveAsPrefabAsset(go, assetPath);
                return null;
            }
            catch (Exception ex)
            {
                return new BakeError { error = "prefab_write_failed", details = ex.Message, path = assetPath };
            }
            finally
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// Bake text-input: label + TMP_InputField + optional reroll button.
        /// Root VLG; children: Label (TMP_Text), InputField, optional RerollButton.
        /// </summary>
        static BakeError BakeTextInput(IrInteractive irRow, string assetPath, UiTheme theme)
        {
            GameObject go = null;
            try
            {
                go = new GameObject(irRow.slug);
                go.AddComponent<RectTransform>();
                var vlg = go.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = 4;
                vlg.childControlWidth = true;
                vlg.childControlHeight = true;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;

                // Label child.
                var labelGo = new GameObject("Label", typeof(RectTransform));
                labelGo.transform.SetParent(go.transform, worldPositionStays: false);
                var labelTmp = labelGo.AddComponent<TMPro.TextMeshProUGUI>();
                labelTmp.text = "City Name";
                labelTmp.fontSize = 14f;
                labelTmp.color = Color.white;
                labelTmp.raycastTarget = false;

                // InputField child.
                var inputGo = new GameObject("InputField", typeof(RectTransform));
                inputGo.transform.SetParent(go.transform, worldPositionStays: false);
                var inputRect = (RectTransform)inputGo.transform;
                inputRect.sizeDelta = new Vector2(0f, 36f);
                var inputBg = inputGo.AddComponent<UnityEngine.UI.Image>();
                inputBg.color = new Color(0.12f, 0.12f, 0.18f, 1f);
                var inputField = inputGo.AddComponent<TMPro.TMP_InputField>();

                // Placeholder child inside InputField.
                var phGo = new GameObject("Placeholder", typeof(RectTransform));
                phGo.transform.SetParent(inputGo.transform, worldPositionStays: false);
                var phRect = (RectTransform)phGo.transform;
                phRect.anchorMin = Vector2.zero; phRect.anchorMax = Vector2.one;
                phRect.offsetMin = new Vector2(8f, 0f); phRect.offsetMax = new Vector2(-8f, 0f);
                var phTmp = phGo.AddComponent<TMPro.TextMeshProUGUI>();
                phTmp.text = "Enter city name...";
                phTmp.fontSize = 14f;
                phTmp.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                phTmp.raycastTarget = false;

                // Text child inside InputField.
                var textGo = new GameObject("Text", typeof(RectTransform));
                textGo.transform.SetParent(inputGo.transform, worldPositionStays: false);
                var textRect = (RectTransform)textGo.transform;
                textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(8f, 0f); textRect.offsetMax = new Vector2(-8f, 0f);
                var textTmp = textGo.AddComponent<TMPro.TextMeshProUGUI>();
                textTmp.fontSize = 14f;
                textTmp.color = Color.white;

                inputField.textComponent = textTmp;
                inputField.placeholder = phTmp;
                inputField.targetGraphic = inputBg;

                PrefabUtility.SaveAsPrefabAsset(go, assetPath);
                return null;
            }
            catch (Exception ex)
            {
                return new BakeError { error = "prefab_write_failed", details = ex.Message, path = assetPath };
            }
            finally
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// Bake toggle-row: label + Unity Toggle side-by-side in HLG.
        /// Children: Label (TMP_Text), ToggleBox (Image + Toggle).
        /// </summary>
        static BakeError BakeToggleRow(IrInteractive irRow, string assetPath, UiTheme theme)
        {
            GameObject go = null;
            try
            {
                go = new GameObject(irRow.slug);
                go.AddComponent<RectTransform>();
                var hlg = go.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 8;
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;
                hlg.childAlignment = TextAnchor.MiddleLeft;

                var labelGo = new GameObject("Label", typeof(RectTransform));
                labelGo.transform.SetParent(go.transform, worldPositionStays: false);
                var le = labelGo.AddComponent<LayoutElement>();
                le.flexibleWidth = 1f;
                var labelTmp = labelGo.AddComponent<TMPro.TextMeshProUGUI>();
                labelTmp.text = string.Empty;
                labelTmp.fontSize = 14f;
                labelTmp.color = Color.white;
                labelTmp.raycastTarget = false;

                var toggleGo = new GameObject("Toggle", typeof(RectTransform));
                toggleGo.transform.SetParent(go.transform, worldPositionStays: false);
                var toggleRect = (RectTransform)toggleGo.transform;
                toggleRect.sizeDelta = new Vector2(28f, 28f);
                var bgImg = toggleGo.AddComponent<UnityEngine.UI.Image>();
                bgImg.color = new Color(0.22f, 0.22f, 0.32f, 1f);
                var toggle = toggleGo.AddComponent<UnityEngine.UI.Toggle>();
                toggle.targetGraphic = bgImg;
                toggle.isOn = false;

                var checkGo = new GameObject("Checkmark", typeof(RectTransform));
                checkGo.transform.SetParent(toggleGo.transform, worldPositionStays: false);
                var checkRect = (RectTransform)checkGo.transform;
                checkRect.anchorMin = new Vector2(0.15f, 0.15f);
                checkRect.anchorMax = new Vector2(0.85f, 0.85f);
                checkRect.offsetMin = checkRect.offsetMax = Vector2.zero;
                var checkImg = checkGo.AddComponent<UnityEngine.UI.Image>();
                checkImg.color = new Color(0.35f, 0.55f, 0.95f, 1f);
                toggle.graphic = checkImg;

                PrefabUtility.SaveAsPrefabAsset(go, assetPath);
                return null;
            }
            catch (Exception ex)
            {
                return new BakeError { error = "prefab_write_failed", details = ex.Message, path = assetPath };
            }
            finally
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// Bake slider-row: label + Unity Slider in HLG. Carries min/max/step in IR detail;
        /// LinearToDecibel flag for audio sliders.
        /// </summary>
        static BakeError BakeSliderRow(IrInteractive irRow, string assetPath, UiTheme theme)
        {
            GameObject go = null;
            try
            {
                go = new GameObject(irRow.slug);
                go.AddComponent<RectTransform>();
                var hlg = go.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 8;
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;
                hlg.childAlignment = TextAnchor.MiddleLeft;

                var labelGo = new GameObject("Label", typeof(RectTransform));
                labelGo.transform.SetParent(go.transform, worldPositionStays: false);
                var le = labelGo.AddComponent<LayoutElement>();
                le.minWidth = 60f;
                var labelTmp = labelGo.AddComponent<TMPro.TextMeshProUGUI>();
                labelTmp.text = string.Empty;
                labelTmp.fontSize = 14f;
                labelTmp.color = Color.white;
                labelTmp.raycastTarget = false;

                var sliderGo = new GameObject("Slider", typeof(RectTransform));
                sliderGo.transform.SetParent(go.transform, worldPositionStays: false);
                var sle = sliderGo.AddComponent<LayoutElement>();
                sle.flexibleWidth = 1f;
                sle.minHeight = 20f;
                var slider = sliderGo.AddComponent<UnityEngine.UI.Slider>();
                slider.minValue = 0f;
                slider.maxValue = 1f;
                slider.value = 1f;

                // Track background.
                var bgGo = new GameObject("Background", typeof(RectTransform));
                bgGo.transform.SetParent(sliderGo.transform, worldPositionStays: false);
                var bgRect = (RectTransform)bgGo.transform;
                bgRect.anchorMin = new Vector2(0f, 0.25f);
                bgRect.anchorMax = new Vector2(1f, 0.75f);
                bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;
                bgGo.AddComponent<UnityEngine.UI.Image>().color = new Color(0.22f, 0.22f, 0.32f, 1f);

                // Fill area.
                var fillAreaGo = new GameObject("Fill Area", typeof(RectTransform));
                fillAreaGo.transform.SetParent(sliderGo.transform, worldPositionStays: false);
                var faRect = (RectTransform)fillAreaGo.transform;
                faRect.anchorMin = new Vector2(0f, 0.25f);
                faRect.anchorMax = new Vector2(1f, 0.75f);
                faRect.offsetMin = new Vector2(5f, 0f); faRect.offsetMax = new Vector2(-5f, 0f);
                var fillGo = new GameObject("Fill", typeof(RectTransform));
                fillGo.transform.SetParent(fillAreaGo.transform, worldPositionStays: false);
                var fillRect = (RectTransform)fillGo.transform;
                fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = Vector2.one;
                fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
                fillGo.AddComponent<UnityEngine.UI.Image>().color = new Color(0.35f, 0.55f, 0.95f, 1f);
                slider.fillRect = fillRect;

                // Handle.
                var haGo = new GameObject("Handle Slide Area", typeof(RectTransform));
                haGo.transform.SetParent(sliderGo.transform, worldPositionStays: false);
                var haRect = (RectTransform)haGo.transform;
                haRect.anchorMin = Vector2.zero; haRect.anchorMax = Vector2.one;
                haRect.offsetMin = new Vector2(10f, 0f); haRect.offsetMax = new Vector2(-10f, 0f);
                var handleGo = new GameObject("Handle", typeof(RectTransform));
                handleGo.transform.SetParent(haGo.transform, worldPositionStays: false);
                var handleRect = (RectTransform)handleGo.transform;
                handleRect.anchorMin = handleRect.anchorMax = new Vector2(0f, 0.5f);
                handleRect.sizeDelta = new Vector2(20f, 20f);
                var handleImg = handleGo.AddComponent<UnityEngine.UI.Image>();
                handleImg.color = Color.white;
                slider.handleRect = handleRect;
                slider.targetGraphic = handleImg;

                PrefabUtility.SaveAsPrefabAsset(go, assetPath);
                return null;
            }
            catch (Exception ex)
            {
                return new BakeError { error = "prefab_write_failed", details = ex.Message, path = assetPath };
            }
            finally
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// Bake dropdown-row: label + TMP_Dropdown in HLG.
        /// </summary>
        static BakeError BakeDropdownRow(IrInteractive irRow, string assetPath, UiTheme theme)
        {
            GameObject go = null;
            try
            {
                go = new GameObject(irRow.slug);
                go.AddComponent<RectTransform>();
                var hlg = go.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 8;
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;
                hlg.childAlignment = TextAnchor.MiddleLeft;

                var labelGo = new GameObject("Label", typeof(RectTransform));
                labelGo.transform.SetParent(go.transform, worldPositionStays: false);
                var le = labelGo.AddComponent<LayoutElement>();
                le.minWidth = 80f;
                var labelTmp = labelGo.AddComponent<TMPro.TextMeshProUGUI>();
                labelTmp.text = string.Empty;
                labelTmp.fontSize = 14f;
                labelTmp.color = Color.white;
                labelTmp.raycastTarget = false;

                var dropGo = new GameObject("Dropdown", typeof(RectTransform));
                dropGo.transform.SetParent(go.transform, worldPositionStays: false);
                var dRect = (RectTransform)dropGo.transform;
                dRect.sizeDelta = new Vector2(160f, 30f);
                var dropBg = dropGo.AddComponent<UnityEngine.UI.Image>();
                dropBg.color = new Color(0.12f, 0.12f, 0.18f, 1f);
                var dropdown = dropGo.AddComponent<TMPro.TMP_Dropdown>();
                dropdown.targetGraphic = dropBg;

                PrefabUtility.SaveAsPrefabAsset(go, assetPath);
                return null;
            }
            catch (Exception ex)
            {
                return new BakeError { error = "prefab_write_failed", details = ex.Message, path = assetPath };
            }
            finally
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// Bake section-header: single TMP_Text child at section-header size token.
        /// </summary>
        static BakeError BakeSectionHeader(IrInteractive irRow, string assetPath, UiTheme theme)
        {
            GameObject go = null;
            try
            {
                go = new GameObject(irRow.slug);
                go.AddComponent<RectTransform>();
                var bg = go.AddComponent<UnityEngine.UI.Image>();
                bg.color = new Color(0f, 0f, 0f, 0f);
                bg.raycastTarget = false;
                var tmp = go.AddComponent<TMPro.TextMeshProUGUI>();
                tmp.text = string.Empty;
                tmp.fontSize = 16f; // size-text-section-header token value
                tmp.fontStyle = TMPro.FontStyles.Bold;
                tmp.color = Color.white;
                tmp.raycastTarget = false;
                tmp.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
                PrefabUtility.SaveAsPrefabAsset(go, assetPath);
                return null;
            }
            catch (Exception ex)
            {
                return new BakeError { error = "prefab_write_failed", details = ex.Message, path = assetPath };
            }
            finally
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
        }

        // ── Wave A2 IR DTOs (TECH-27069) ─────────────────────────────────────────

        /// <summary>IR DTO for card-picker archetype. JsonUtility round-trip validated in EditMode tests.</summary>
        [System.Serializable]
        public class CardPickerDetail
        {
            public string bind;
            public string value;
            public string label;
            public string description;
        }

        /// <summary>IR DTO for chip-picker archetype. JsonUtility round-trip validated in EditMode tests.</summary>
        [System.Serializable]
        public class ChipPickerDetail
        {
            public string bind;
            public string value;
            public string label;
        }

        /// <summary>IR DTO for text-input archetype. JsonUtility round-trip validated in EditMode tests.</summary>
        [System.Serializable]
        public class TextInputDetail
        {
            public string bind;
            public string placeholder;
            public string reroll_action;
        }

        /// <summary>IR DTO for toggle-row archetype. JsonUtility round-trip validated in EditMode tests.</summary>
        [System.Serializable]
        public class ToggleRowDetail
        {
            public string bind;
            public string label;
        }

        /// <summary>IR DTO for slider-row archetype. JsonUtility round-trip validated in EditMode tests.</summary>
        [System.Serializable]
        public class SliderRowDetail
        {
            public string bind;
            public string label;
            public float min;
            public float max = 1f;
            public float step = 0.01f;
            public bool linearToDecibel;
        }

        /// <summary>IR DTO for dropdown-row archetype. JsonUtility round-trip validated in EditMode tests.</summary>
        [System.Serializable]
        public class DropdownRowDetail
        {
            public string bind;
            public string label;
            public string options_action;
        }

        /// <summary>IR DTO for section-header archetype. JsonUtility round-trip validated in EditMode tests.</summary>
        [System.Serializable]
        public class SectionHeaderDetail
        {
            public string label;
            public string size_token;
        }

        // ── Wave A3 IR DTOs (TECH-27074) ─────────────────────────────────────────

        /// <summary>IR DTO for save-controls-strip archetype. JsonUtility round-trip validated in EditMode tests.</summary>
        [System.Serializable]
        public class SaveControlsStripDetail
        {
            public string bindId;
            public string saveLabel;
            public string loadLabel;
        }

        /// <summary>IR DTO for save-list archetype. JsonUtility round-trip validated in EditMode tests.</summary>
        [System.Serializable]
        public class SaveListDetail
        {
            public string listBindId;
            public string selectedSlotBindId;
            public string trashAction;
            public string selectAction;
            public bool sortNewestFirst;
        }

        // ── Wave A3 bake methods (TECH-27074) ────────────────────────────────────

        /// <summary>
        /// Bake save-controls-strip: HLG root with mode label + mode-switch stub.
        /// Bind driven by <see cref="SaveControlsStripDetail.bindId"/> (e.g. saveload.mode).
        /// </summary>
        static BakeError BakeSaveControlsStrip(IrInteractive irRow, string assetPath, UiTheme theme)
        {
            GameObject go = null;
            try
            {
                go = new GameObject(irRow.slug ?? "save-controls-strip");
                go.AddComponent<RectTransform>();
                var hlg = go.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 8f;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;
                hlg.padding = new RectOffset(4, 4, 4, 4);

                // Mode label stub
                var labelGo = new GameObject("ModeLabel", typeof(RectTransform));
                labelGo.transform.SetParent(go.transform, false);
                var tmp = labelGo.AddComponent<TMPro.TextMeshProUGUI>();
                tmp.text = "Load";
                tmp.fontSize = 14f;

                // Save as prefab
                PrefabUtility.SaveAsPrefabAsset(go, assetPath);
                return null;
            }
            catch (Exception ex)
            {
                return new BakeError { error = "bake_exception", details = ex.Message, path = assetPath };
            }
            finally
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// Bake save-list: VLG root (scrollable list container) with per-row action map stubs for
        /// trash + select. Row rendering driven at runtime by <see cref="SaveLoadScreenDataAdapter"/>.
        /// </summary>
        static BakeError BakeSaveList(IrInteractive irRow, string assetPath, UiTheme theme)
        {
            GameObject go = null;
            try
            {
                go = new GameObject(irRow.slug ?? "save-list");
                go.AddComponent<RectTransform>();
                var vlg = go.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = 4f;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;
                vlg.childControlWidth = true;
                vlg.childControlHeight = true;
                vlg.padding = new RectOffset(4, 4, 4, 4);

                // Row template stub
                var rowGo = new GameObject("RowTemplate", typeof(RectTransform));
                rowGo.transform.SetParent(go.transform, false);
                var rowHlg = rowGo.AddComponent<HorizontalLayoutGroup>();
                rowHlg.spacing = 6f;
                rowHlg.childForceExpandWidth = false;
                rowHlg.childForceExpandHeight = false;
                rowHlg.childControlWidth = true;
                rowHlg.childControlHeight = true;

                var rowLabel = new GameObject("RowLabel", typeof(RectTransform));
                rowLabel.transform.SetParent(rowGo.transform, false);
                var rowTmp = rowLabel.AddComponent<TMPro.TextMeshProUGUI>();
                rowTmp.text = "Save Slot";
                rowTmp.fontSize = 13f;

                var trashBtn = new GameObject("TrashButton", typeof(RectTransform));
                trashBtn.transform.SetParent(rowGo.transform, false);
                trashBtn.AddComponent<UnityEngine.UI.Image>().color = new Color(0.7f, 0.2f, 0.2f, 1f);
                trashBtn.AddComponent<UnityEngine.UI.Button>();

                // Save as prefab
                PrefabUtility.SaveAsPrefabAsset(go, assetPath);
                return null;
            }
            catch (Exception ex)
            {
                return new BakeError { error = "bake_exception", details = ex.Message, path = assetPath };
            }
            finally
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// Wave B1 (TECH-27079) — bake subtype-picker-strip archetype.
        /// HorizontalLayoutGroup root (hidden_default=true) + 3 child slot stubs:
        /// arrow-left, card-strip (HLG), arrow-right. Runtime expands card-strip with N cards
        /// per active picker_variant. JsonUtility round-trip DTO: <see cref="SubtypePickerStripDetail"/>.
        /// </summary>
        static BakeError BakeSubtypePickerStrip(IrInteractive irRow, string assetPath, UiTheme theme)
        {
            GameObject go = null;
            try
            {
                go = new GameObject(irRow.slug ?? "subtype-picker-strip");
                var rt = go.AddComponent<RectTransform>();
                // Anchor bottom-left; strip height = 96 px; width = 0 (driven at runtime).
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 0f);
                rt.pivot = new Vector2(0f, 0f);
                rt.sizeDelta = new Vector2(0f, 96f);
                rt.anchoredPosition = new Vector2(8f, 8f);

                var hlg = go.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 8f;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;
                hlg.padding = new RectOffset(8, 8, 8, 8);

                // Background image stub (runtime tints with theme dark translucent).
                var bg = go.AddComponent<UnityEngine.UI.Image>();
                bg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

                // Hidden by default (open_trigger = action.tool-select from toolbar).
                go.SetActive(false);

                // ── Child: arrow-left (overflow affordance, hidden unless overflow).
                var arrowLeft = new GameObject("arrow-left", typeof(RectTransform));
                arrowLeft.transform.SetParent(go.transform, false);
                var arrowLeftImg = arrowLeft.AddComponent<UnityEngine.UI.Image>();
                arrowLeftImg.color = new Color(0.9f, 0.9f, 0.8f, 1f);
                var arrowLeftBtn = arrowLeft.AddComponent<UnityEngine.UI.Button>();
                arrowLeft.SetActive(false); // hidden unless overflow at runtime.
                var alRt = arrowLeft.GetComponent<RectTransform>();
                alRt.sizeDelta = new Vector2(24f, 80f);

                // ── Child: card-strip (runtime-expanded card container).
                var cardStrip = new GameObject("card-strip", typeof(RectTransform));
                cardStrip.transform.SetParent(go.transform, false);
                var cardHlg = cardStrip.AddComponent<HorizontalLayoutGroup>();
                cardHlg.spacing = 8f;
                cardHlg.childForceExpandWidth = false;
                cardHlg.childForceExpandHeight = false;
                cardHlg.childControlWidth = true;
                cardHlg.childControlHeight = true;
                var csRt = cardStrip.GetComponent<RectTransform>();
                var csLe = cardStrip.AddComponent<UnityEngine.UI.LayoutElement>();
                csLe.flexibleWidth = 1f;

                // ── Child: arrow-right (overflow affordance, hidden unless overflow).
                var arrowRight = new GameObject("arrow-right", typeof(RectTransform));
                arrowRight.transform.SetParent(go.transform, false);
                var arrowRightImg = arrowRight.AddComponent<UnityEngine.UI.Image>();
                arrowRightImg.color = new Color(0.9f, 0.9f, 0.8f, 1f);
                var arrowRightBtn = arrowRight.AddComponent<UnityEngine.UI.Button>();
                arrowRight.SetActive(false); // hidden unless overflow at runtime.
                var arRt = arrowRight.GetComponent<RectTransform>();
                arRt.sizeDelta = new Vector2(24f, 80f);

                PrefabUtility.SaveAsPrefabAsset(go, assetPath);
                return null;
            }
            catch (Exception ex)
            {
                return new BakeError { error = "bake_exception", details = ex.Message, path = assetPath };
            }
            finally
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
        }

        /// <summary>Wave B1 (TECH-27079) — IR DTO for subtype-picker-strip bake params. JsonUtility round-trip.</summary>
        [Serializable]
        public class SubtypePickerStripDetail
        {
            public string picker_variant;
            public string layout;
            public int strip_h_px;
            public int card_w_px;
            public int card_h_px;
            public int gap_px;
            public string anchor;
            public bool hidden_default;
        }

        // ── Stage 1.4 T1.4.2 — panel archetype dispatch ─────────────────────────

        /// <summary>
        /// Dispatch IR <paramref name="panel"/>.archetype to instantiate the matching Themed component
        /// child on <paramref name="root"/>. Known arms: section_header, divider, badge. Unknown archetype
        /// logs a warning and skips (no exception). Stage 1.4 (T1.4.2).
        /// </summary>
        static void BakePanelArchetype(IrPanel panel, GameObject root, UiTheme theme)
        {
            if (panel == null || root == null || string.IsNullOrEmpty(panel.archetype)) return;

            switch (panel.archetype)
            {
                case "section_header":
                {
                    var childGo = new GameObject("SectionHeader", typeof(RectTransform));
                    childGo.transform.SetParent(root.transform, worldPositionStays: false);
                    var header = childGo.AddComponent<ThemedSectionHeader>();
                    WireThemeRef(header, theme);
                    break;
                }
                case "divider":
                {
                    var childGo = new GameObject("Divider", typeof(RectTransform));
                    childGo.transform.SetParent(root.transform, worldPositionStays: false);
                    var divider = childGo.AddComponent<ThemedDivider>();
                    WireThemeRef(divider, theme);
                    break;
                }
                case "badge":
                {
                    var childGo = new GameObject("Badge", typeof(RectTransform));
                    childGo.transform.SetParent(root.transform, worldPositionStays: false);
                    var badge = childGo.AddComponent<ThemedBadge>();
                    WireThemeRef(badge, theme);
                    break;
                }
                default:
                    Debug.LogWarning($"[UiBakeHandler] panel.archetype '{panel.archetype}' unknown — skipped");
                    break;
            }
        }

        // ── Wave B2 (TECH-27083) — stats-panel archetype bake methods ────────────

        /// <summary>Wave B2 — tab-strip: N tabs + active-tab bind. Toggle group children.</summary>
        static BakeError BakeTabStrip(IrInteractive irRow, string assetPath, UiTheme theme)
        {
            GameObject go = null;
            try
            {
                go = new GameObject(irRow.slug ?? "tab-strip");
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.sizeDelta = new Vector2(0f, 48f);

                var hlg = go.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 4f;
                hlg.childForceExpandWidth = true;
                hlg.childForceExpandHeight = true;
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;

                var tg = go.AddComponent<ToggleGroup>();
                tg.allowSwitchOff = false;

                // Placeholder tab children (runtime expands to N tabs from params_json.tabs).
                string[] defaultTabs = { "Tab1", "Tab2", "Tab3" };
                foreach (var tabLabel in defaultTabs)
                {
                    var tabGo = new GameObject(tabLabel, typeof(RectTransform));
                    tabGo.transform.SetParent(go.transform, false);
                    var toggle = tabGo.AddComponent<UnityEngine.UI.Toggle>();
                    toggle.group = tg;
                    var label = new GameObject("Label", typeof(RectTransform));
                    label.transform.SetParent(tabGo.transform, false);
                    label.AddComponent<TMPro.TextMeshProUGUI>().text = tabLabel;
                }

                PrefabUtility.SaveAsPrefabAsset(go, assetPath);
                return null;
            }
            catch (Exception ex)
            {
                return new BakeError { error = "bake_exception", details = ex.Message, path = assetPath };
            }
            finally { if (go != null) UnityEngine.Object.DestroyImmediate(go); }
        }

        /// <summary>Wave B2 — chart: read-only line-series placeholder (RawImage stub; runtime plots via StatsHistoryRecorder).</summary>
        static BakeError BakeChart(IrInteractive irRow, string assetPath, UiTheme theme)
        {
            GameObject go = null;
            try
            {
                go = new GameObject(irRow.slug ?? "chart");
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

                // RawImage as chart canvas stub — runtime replaces with line-render texture.
                var img = go.AddComponent<UnityEngine.UI.RawImage>();
                img.color = new Color(0.08f, 0.12f, 0.18f, 1f);

                // Axis label stubs.
                var xLabel = new GameObject("x-axis-label", typeof(RectTransform));
                xLabel.transform.SetParent(go.transform, false);
                xLabel.AddComponent<TMPro.TextMeshProUGUI>().text = "Time";

                var yLabel = new GameObject("y-axis-label", typeof(RectTransform));
                yLabel.transform.SetParent(go.transform, false);
                yLabel.AddComponent<TMPro.TextMeshProUGUI>().text = "Value";

                PrefabUtility.SaveAsPrefabAsset(go, assetPath);
                return null;
            }
            catch (Exception ex)
            {
                return new BakeError { error = "bake_exception", details = ex.Message, path = assetPath };
            }
            finally { if (go != null) UnityEngine.Object.DestroyImmediate(go); }
        }

        /// <summary>Wave B2 — range-tabs: 3-chip toggle group (3mo / 12mo / all-time).</summary>
        static BakeError BakeRangeTabs(IrInteractive irRow, string assetPath, UiTheme theme)
        {
            GameObject go = null;
            try
            {
                go = new GameObject(irRow.slug ?? "range-tabs");
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.sizeDelta = new Vector2(0f, 36f);

                var hlg = go.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 4f;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = true;
                hlg.childControlWidth = false;
                hlg.childControlHeight = true;

                var tg = go.AddComponent<ToggleGroup>();
                tg.allowSwitchOff = false;

                string[] chips = { "3mo", "12mo", "all-time" };
                foreach (var chip in chips)
                {
                    var chipGo = new GameObject(chip, typeof(RectTransform));
                    chipGo.transform.SetParent(go.transform, false);
                    var chipRt = chipGo.GetComponent<RectTransform>();
                    chipRt.sizeDelta = new Vector2(72f, 32f);
                    var toggle = chipGo.AddComponent<UnityEngine.UI.Toggle>();
                    toggle.group = tg;
                    var lbl = new GameObject("Label", typeof(RectTransform));
                    lbl.transform.SetParent(chipGo.transform, false);
                    lbl.AddComponent<TMPro.TextMeshProUGUI>().text = chip;
                }

                PrefabUtility.SaveAsPrefabAsset(go, assetPath);
                return null;
            }
            catch (Exception ex)
            {
                return new BakeError { error = "bake_exception", details = ex.Message, path = assetPath };
            }
            finally { if (go != null) UnityEngine.Object.DestroyImmediate(go); }
        }

        /// <summary>Wave B2 — stacked-bar-row: label + segmented horizontal Image stack.</summary>
        static BakeError BakeStackedBarRow(IrInteractive irRow, string assetPath, UiTheme theme)
        {
            GameObject go = null;
            try
            {
                go = new GameObject(irRow.slug ?? "stacked-bar-row");
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.sizeDelta = new Vector2(0f, 40f);

                var hlg = go.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 8f;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = true;
                hlg.childControlWidth = false;
                hlg.childControlHeight = true;
                hlg.padding = new RectOffset(8, 8, 4, 4);

                // Row label
                var labelGo = new GameObject("RowLabel", typeof(RectTransform));
                labelGo.transform.SetParent(go.transform, false);
                var lblRt = labelGo.GetComponent<RectTransform>();
                lblRt.sizeDelta = new Vector2(140f, 32f);
                var lblTmp = labelGo.AddComponent<TMPro.TextMeshProUGUI>();
                lblTmp.text = irRow.slug ?? "Row";

                // Bar container
                var barGo = new GameObject("BarContainer", typeof(RectTransform));
                barGo.transform.SetParent(go.transform, false);
                var barLe = barGo.AddComponent<LayoutElement>();
                barLe.flexibleWidth = 1f;
                var barHlg = barGo.AddComponent<HorizontalLayoutGroup>();
                barHlg.spacing = 1f;
                barHlg.childForceExpandWidth = true;
                barHlg.childForceExpandHeight = true;
                barHlg.childControlWidth = true;
                barHlg.childControlHeight = true;

                // 2 segment stubs (runtime expands per data).
                for (int i = 0; i < 2; i++)
                {
                    var seg = new GameObject($"Seg{i}", typeof(RectTransform));
                    seg.transform.SetParent(barGo.transform, false);
                    var segImg = seg.AddComponent<UnityEngine.UI.Image>();
                    segImg.color = i == 0 ? new Color(0.3f, 0.7f, 0.4f) : new Color(0.2f, 0.4f, 0.6f);
                }

                PrefabUtility.SaveAsPrefabAsset(go, assetPath);
                return null;
            }
            catch (Exception ex)
            {
                return new BakeError { error = "bake_exception", details = ex.Message, path = assetPath };
            }
            finally { if (go != null) UnityEngine.Object.DestroyImmediate(go); }
        }

        /// <summary>Wave B2 — service-row: icon Image + label TMP + value-bind TMP.</summary>
        static BakeError BakeServiceRow(IrInteractive irRow, string assetPath, UiTheme theme)
        {
            GameObject go = null;
            try
            {
                go = new GameObject(irRow.slug ?? "service-row");
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.sizeDelta = new Vector2(0f, 36f);

                var hlg = go.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 8f;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = true;
                hlg.childControlWidth = false;
                hlg.childControlHeight = true;
                hlg.padding = new RectOffset(8, 8, 4, 4);

                // Icon stub
                var iconGo = new GameObject("Icon", typeof(RectTransform));
                iconGo.transform.SetParent(go.transform, false);
                var iconRt = iconGo.GetComponent<RectTransform>();
                iconRt.sizeDelta = new Vector2(28f, 28f);
                iconGo.AddComponent<UnityEngine.UI.Image>().color = new Color(0.8f, 0.8f, 0.8f);

                // Label
                var labelGo = new GameObject("ServiceLabel", typeof(RectTransform));
                labelGo.transform.SetParent(go.transform, false);
                var labelLe = labelGo.AddComponent<LayoutElement>();
                labelLe.flexibleWidth = 1f;
                var labelTmp = labelGo.AddComponent<TMPro.TextMeshProUGUI>();
                labelTmp.text = irRow.slug ?? "Service";

                // Value bind
                var valueGo = new GameObject("ValueLabel", typeof(RectTransform));
                valueGo.transform.SetParent(go.transform, false);
                var valueRt = valueGo.GetComponent<RectTransform>();
                valueRt.sizeDelta = new Vector2(64f, 28f);
                var valueTmp = valueGo.AddComponent<TMPro.TextMeshProUGUI>();
                valueTmp.text = "—";

                PrefabUtility.SaveAsPrefabAsset(go, assetPath);
                return null;
            }
            catch (Exception ex)
            {
                return new BakeError { error = "bake_exception", details = ex.Message, path = assetPath };
            }
            finally { if (go != null) UnityEngine.Object.DestroyImmediate(go); }
        }

        /// <summary>Wave B2 (TECH-27083) — IR DTO for tab-strip. JsonUtility round-trip.</summary>
        [Serializable]
        public class TabStripDetail
        {
            public string[] tabs;
            public string activeTabBindId;
        }

        /// <summary>Wave B2 (TECH-27083) — IR DTO for chart. JsonUtility round-trip.</summary>
        [Serializable]
        public class ChartDetail
        {
            public string seriesId;
            public string bindId;
            public string tabGroup;
        }

        /// <summary>Wave B2 (TECH-27083) — IR DTO for range-tabs. JsonUtility round-trip.</summary>
        [Serializable]
        public class RangeTabsDetail
        {
            public string[] options;
            public string bindId;
        }

        /// <summary>Wave B2 (TECH-27083) — IR DTO for stacked-bar-row. JsonUtility round-trip.</summary>
        [Serializable]
        public class StackedBarRowDetail
        {
            public string bindId;
            public string tabGroup;
        }

        /// <summary>Wave B2 (TECH-27083) — IR DTO for service-row. JsonUtility round-trip.</summary>
        [Serializable]
        public class ServiceRowDetail
        {
            public string icon;
            public string bindId;
            public string tabGroup;
        }

        // ── Wave B4 (TECH-27093) — modal-card layout container ───────────────────

        /// <summary>
        /// Bake modal-card root container: full-screen backdrop (Image + raycast-target) +
        /// center-anchored card RectTransform + content-replace slot named "{slug}-content-slot".
        /// Used as root layout template for pause-menu, stats-panel, budget-panel (Wave B4+).
        /// Round-trip IR DTO: <see cref="ModalCardDetail"/>.
        /// </summary>
        static BakeError BakeModalCard(IrInteractive irRow, string assetPath, UiTheme theme)
        {
            GameObject go = null;
            try
            {
                go = new GameObject(irRow.slug);
                var rootRt = go.AddComponent<RectTransform>();
                // Fullscreen stretch.
                rootRt.anchorMin = Vector2.zero;
                rootRt.anchorMax = Vector2.one;
                rootRt.offsetMin = rootRt.offsetMax = Vector2.zero;

                // Backdrop child: full-screen Image blocking raycasts.
                var backdropGo = new GameObject("backdrop", typeof(RectTransform));
                backdropGo.transform.SetParent(go.transform, worldPositionStays: false);
                var backdropRt = (RectTransform)backdropGo.transform;
                backdropRt.anchorMin = Vector2.zero;
                backdropRt.anchorMax = Vector2.one;
                backdropRt.offsetMin = backdropRt.offsetMax = Vector2.zero;
                var backdropImg = backdropGo.AddComponent<UnityEngine.UI.Image>();
                backdropImg.color = new Color(0f, 0f, 0f, 0.55f);
                backdropImg.raycastTarget = true;

                // Card child: center-anchored content area.
                var cardGo = new GameObject("card", typeof(RectTransform));
                cardGo.transform.SetParent(go.transform, worldPositionStays: false);
                var cardRt = (RectTransform)cardGo.transform;
                cardRt.anchorMin = new Vector2(0.5f, 0.5f);
                cardRt.anchorMax = new Vector2(0.5f, 0.5f);
                cardRt.pivot = new Vector2(0.5f, 0.5f);
                cardRt.sizeDelta = new Vector2(480f, 480f);
                cardRt.anchoredPosition = Vector2.zero;
                var cardImg = cardGo.AddComponent<UnityEngine.UI.Image>();
                cardImg.color = new Color(0.08f, 0.08f, 0.12f, 1f);
                cardImg.raycastTarget = true;

                // Content-replace slot child: named "{slug}-content-slot" per SlotAnchorResolver convention.
                var slotName = $"{irRow.slug}-content-slot";
                var slotGo = new GameObject(slotName, typeof(RectTransform));
                slotGo.transform.SetParent(cardGo.transform, worldPositionStays: false);
                var slotRt = (RectTransform)slotGo.transform;
                slotRt.anchorMin = Vector2.zero;
                slotRt.anchorMax = Vector2.one;
                slotRt.offsetMin = slotRt.offsetMax = Vector2.zero;
                var slotImg = slotGo.AddComponent<UnityEngine.UI.Image>();
                slotImg.color = new Color(0f, 0f, 0f, 0f); // transparent placeholder
                slotImg.raycastTarget = false;

                PrefabUtility.SaveAsPrefabAsset(go, assetPath);
                return null;
            }
            catch (Exception ex)
            {
                return new BakeError { error = "prefab_write_failed", details = ex.Message, path = assetPath };
            }
            finally
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
        }

        /// <summary>Wave B4 (TECH-27093) — IR DTO for modal-card archetype. JsonUtility round-trip.</summary>
        [Serializable]
        public class ModalCardDetail
        {
            public float width = 480f;
            public float height = 480f;
            public string modal_kind;
        }

    }
}
