using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using Territory.UI;
using Territory.UI.Juice;
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

        /// <summary>Known StudioControl kind slugs — kept in sync with `tools/scripts/ir-schema.ts` <c>StudioControlKind</c>.</summary>
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

        /// <summary>Juice slug constants — kept in sync with `tools/scripts/ir-schema.ts` Stage 5 IR juice DTO (post-MVP additive field).</summary>
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
        static void SpawnIlluminatedButtonRenderTargets(
            GameObject prefabRoot,
            string iconSpriteSlug,
            out Image bodyImage,
            out Image haloImage)
        {
            bodyImage = null;
            haloImage = null;
            if (prefabRoot == null) return;
            var rootRect = prefabRoot.GetComponent<RectTransform>();
            if (rootRect == null) return;

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
                    ir.anchorMin = new Vector2(0.5f, 0.5f);
                    ir.anchorMax = new Vector2(0.5f, 0.5f);
                    ir.pivot = new Vector2(0.5f, 0.5f);
                    ir.anchoredPosition = Vector2.zero;
                    ir.sizeDelta = new Vector2(64f, 64f);
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
                }
                else
                {
                    Debug.LogWarning(
                        $"[UiBakeHandler] illuminated-button icon sprite not found (slug={iconSpriteSlug}); "
                        + "expected Assets/Sprites/Buttons/{slug}-target.png or Assets/Sprites/{slug}-target.png");
                }
            }

            // halo child — radial pulse target. Drawn on top of body + icon.
            var haloT = prefabRoot.transform.Find("halo");
            if (haloT == null)
            {
                var halo = new GameObject("halo", typeof(RectTransform));
                halo.transform.SetParent(prefabRoot.transform, worldPositionStays: false);
                var hr = (RectTransform)halo.transform;
                hr.anchorMin = new Vector2(0.5f, 0.5f);
                hr.anchorMax = new Vector2(0.5f, 0.5f);
                hr.pivot = new Vector2(0.5f, 0.5f);
                hr.anchoredPosition = Vector2.zero;
                hr.sizeDelta = new Vector2(64f, 64f);
                var img = halo.AddComponent<Image>();
                var color = img.color;
                color.a = 0f; // Halo idle alpha; renderer animates 1→0 on click.
                img.color = color;
                img.raycastTarget = false;
                haloImage = img;
            }
            else
            {
                haloImage = haloT.GetComponent<Image>();
            }

            // Ensure render order: body (back) → icon → halo (front). SetAsLastSibling on halo
            // re-asserts the contract regardless of pre-existing child order.
            if (haloImage != null) haloImage.transform.SetAsLastSibling();
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
            sel.transition = Selectable.Transition.ColorTint;

            var cb = sel.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = Color.white;
            cb.pressedColor = Color.white;
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(0.6f, 0.6f, 0.6f, 1f);
            if (theme != null && theme.TryGetPalette("led-amber", out var ramp) && ramp.ramp != null && ramp.ramp.Length >= 3)
            {
                int last = ramp.ramp.Length - 1;
                if (ColorUtility.TryParseHtmlString(ramp.ramp[last], out var normal)) cb.normalColor = normal;
                if (ColorUtility.TryParseHtmlString(ramp.ramp[Mathf.Max(0, last - 1)], out var hl))
                {
                    cb.highlightedColor = hl;
                    cb.selectedColor = hl;
                }
                if (ColorUtility.TryParseHtmlString(ramp.ramp[Mathf.Max(0, last - 2)], out var pressed)) cb.pressedColor = pressed;
                if (ColorUtility.TryParseHtmlString(ramp.ramp[0], out var disabled)) cb.disabledColor = disabled;
            }
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.1f;
            sel.colors = cb;
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
            tmp.color = Color.white;
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
            tmp.text = string.Empty;
            tmp.alignment = TextAlignmentOptions.Center;
            int digits = detail != null && detail.digits > 0 ? detail.digits : 1;
            // Width hint in font size — ~24pt per digit floor; renderer will populate text on ApplyDetail.
            tmp.fontSize = 24f;
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

    }
}
