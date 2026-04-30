using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Territory.UI;
using Territory.UI.Juice;
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
    /// Field names mirror <c>tools/scripts/ir-schema.ts</c> verbatim so JsonUtility round-trips
    /// match the Stage 1 transcribe pipeline output. Polymorphic shapes use Unity-friendly
    /// optional-with-zero-default fields (JsonUtility cannot model discriminated unions natively).
    ///
    /// T2.2 lands DTOs + Parse + ValidateSlotAcceptRules + Bake skeleton; T2.4 fills the bake body.
    /// </summary>
    public static class UiBakeHandler
    {
        // ── DTOs (mirror tools/scripts/ir-schema.ts) ────────────────────────────

        /// <summary>Top-level IR JSON shape — single output of `transcribe:cd-game-ui`.</summary>
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

        [Serializable]
        public class IrPanel
        {
            public string slug;
            public string archetype;
            public IrPanelSlot[] slots;
        }

        [Serializable]
        public class IrPanelSlot
        {
            public string name;
            public string[] accepts;
            public string[] children;
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
            /// <summary>StudioControl archetype slug — see ir-schema.ts `StudioControlKind`.</summary>
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
            public string out_dir;
            public string theme_so;
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
        /// Mirrors `validateSlotAccept` in `tools/scripts/ir-schema.ts` — bridge parity guarantees
        /// transcribe-time and bake-time guards reject identical inputs.
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

            if (string.IsNullOrEmpty(args.ir_path))
            {
                return new BakeResult
                {
                    root = null,
                    error = new BakeError { error = "missing_arg", details = "ir_path", path = "$.ir_path" },
                };
            }

            string jsonText;
            try
            {
                jsonText = System.IO.File.ReadAllText(args.ir_path);
            }
            catch (Exception ex)
            {
                return new BakeResult
                {
                    root = null,
                    error = new BakeError
                    {
                        error = "ir_path_not_readable",
                        details = ex.Message,
                        path = args.ir_path,
                    },
                };
            }

            var parseResult = Parse(jsonText);
            if (parseResult.error != null) return parseResult;

            var slotError = ValidateSlotAcceptRules(parseResult.root);
            if (slotError != null)
            {
                return new BakeResult { root = parseResult.root, error = slotError };
            }

            // T2.4 — populate UiTheme SO + write placeholder prefabs.
            var soPath = string.IsNullOrEmpty(args.theme_so)
                ? "Assets/UI/Theme/DefaultUiTheme.asset"
                : args.theme_so;

            var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(soPath);
            if (theme == null)
            {
                return new BakeResult
                {
                    root = parseResult.root,
                    error = new BakeError
                    {
                        error = "theme_so_not_found",
                        details = soPath,
                        path = "$.theme_so",
                    },
                };
            }

            PopulateThemeFromIr(theme, parseResult.root);
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();

            var prefabError = WritePlaceholderPrefabs(parseResult.root, args.out_dir);
            if (prefabError != null)
            {
                return new BakeResult { root = parseResult.root, error = prefabError };
            }

            AssetDatabase.Refresh();
            return new BakeResult { root = parseResult.root, error = null };
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

        // ── Placeholder prefab writes ───────────────────────────────────────────

        /// <summary>
        /// Write empty-RectTransform placeholder prefabs per panel + interactive in IR. Writes go under
        /// <paramref name="outDir"/> (defaulting to <c>Assets/UI/Prefabs/Generated</c> when empty).
        /// PrefabUtility.SaveAsPrefabAsset overwrites existing files — second bake on same IR is idempotent.
        /// Returns null on success, populated <see cref="BakeError"/> on first IO failure.
        /// </summary>
        static BakeError WritePlaceholderPrefabs(IrRoot ir, string outDir)
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
                        err = BakeInteractive(ic, i, assetPath, _lastRawIrJson);
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
                    var err = SavePanelPrefab(panel, assetPath, dir);
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

        // ── Panel prefab bake (Stage 3 T3.5) ────────────────────────────────────

        /// <summary>
        /// Write a panel prefab with attached <see cref="ThemedPanel"/> + populated <c>_slots</c> +
        /// <c>_children[]</c>. SlotSpec order = IR slot order; children order = IR slot iteration order.
        /// Stage 7 T7.0 — children are instantiated as scene-instance <c>GameObject</c>s under the panel
        /// root (state-holder + renderer pair + render-target descendants per Stage 10 T10.3 convention).
        /// Asset-GUID resolution (loading <c>{dir}/{childSlug}.prefab</c>) is no longer used: adapter
        /// Inspector slots must bind against real children, not asset stand-ins. <paramref name="dir"/>
        /// stays in the signature for future bake-time variant resolution.
        /// SerializedObject path enforces deterministic property write for re-bake idempotency.
        /// </summary>
        static BakeError SavePanelPrefab(IrPanel panel, string assetPath, string dir)
        {
            // dir kept for future variant resolution; not consumed in T7.0 path.
            _ = dir;

            GameObject go = null;
            try
            {
                go = new GameObject(panel.slug);
                go.AddComponent<RectTransform>();
                var themedPanel = go.AddComponent<ThemedPanel>();

                // Populate _slots + _children[] via SerializedObject for deterministic order.
                var so = new SerializedObject(themedPanel);
                var slotsProp = so.FindProperty("_slots");
                var childrenProp = so.FindProperty("_children");

                var childrenList = new List<GameObject>();

                int slotCount = panel.slots != null ? panel.slots.Length : 0;
                slotsProp.arraySize = slotCount;
                // Panel-scoped per-kind counter — prevents Unity sibling-name auto-suffix when
                // multiple slots declare the same child kind (e.g. tool-grid + subtype-row both
                // emit illuminated-button). Keys are the StudioControl kind slug; values are the
                // running 0-based count for that kind across all slots in this panel.
                var perKindCounters = new Dictionary<string, int>();
                for (int s = 0; s < slotCount; s++)
                {
                    var slot = panel.slots[s];
                    var slotProp = slotsProp.GetArrayElementAtIndex(s);
                    var slugProp = slotProp.FindPropertyRelative("slug");
                    var acceptsProp = slotProp.FindPropertyRelative("accepts");

                    slugProp.stringValue = slot?.name ?? string.Empty;

                    var slotAccepts = slot?.accepts ?? Array.Empty<string>();
                    acceptsProp.arraySize = slotAccepts.Length;
                    for (int a = 0; a < slotAccepts.Length; a++)
                    {
                        acceptsProp.GetArrayElementAtIndex(a).stringValue = slotAccepts[a] ?? string.Empty;
                    }

                    // Stage 7 T7.0 — instantiate scene-instance child GameObjects under the panel root
                    // for each declared slot child (state-holder + renderer pair + render-target
                    // descendants). Asset-prefab loading retired: adapter Inspector slots must bind
                    // against real children, not asset GUID stand-ins.
                    var slotChildren = slot?.children ?? Array.Empty<string>();
                    for (int c = 0; c < slotChildren.Length; c++)
                    {
                        var childKind = slotChildren[c];
                        if (string.IsNullOrEmpty(childKind)) continue;
                        if (!IsKnownStudioControlKind(childKind))
                        {
                            Debug.LogWarning(
                                $"[UiBakeHandler] panel={panel.slug} slot={slot?.name} child kind={childKind} unknown — skipped");
                            continue;
                        }
                        // Panel-scoped per-kind counter — read-modify-write so multi-slot panels
                        // (tool-grid + subtype-row) produce contiguous names (illuminated-button,
                        // illuminated-button (1), ..., illuminated-button (17)) instead of
                        // colliding with sibling auto-suffix.
                        if (!perKindCounters.TryGetValue(childKind, out int kindCounter))
                        {
                            kindCounter = 0;
                        }
                        var childGo = InstantiatePanelChild(childKind, go.transform, ref kindCounter);
                        perKindCounters[childKind] = kindCounter;
                        if (childGo == null) continue;
                        childrenList.Add(childGo);
                    }
                }

                childrenProp.arraySize = childrenList.Count;
                for (int i = 0; i < childrenList.Count; i++)
                {
                    childrenProp.GetArrayElementAtIndex(i).objectReferenceValue = childrenList[i];
                }

                so.ApplyModifiedPropertiesWithoutUndo();

                // Stage 5 T5.5 — synthetic interactive row enables ShadowDepth opt-in via IR juice[]
                // entries on the panel level (current MVP: defaults gated off; override-only path).
                var panelRow = new IrInteractive
                {
                    slug = panel.slug,
                    kind = "themed-panel",
                    juice = null,
                };
                AttachJuiceComponents(go, panelRow);

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

        // ── Stage 7 T7.0 — embedded panel child instantiation ───────────────────

        /// <summary>
        /// Instantiate a fresh scene-instance <c>GameObject</c> under the panel root for the given
        /// StudioControl <paramref name="kind"/>. Attaches the matching state-holder + renderer pair
        /// (per Stage 10 T10.3 convention) and spawns the kind's render-target descendants
        /// (<c>body</c> / <c>halo</c> / <c>text</c> / <c>Label</c> / <c>Icon</c> / <c>Toggle</c>).
        /// Detail-row defaults applied so the embedded child is visually consistent with the
        /// top-level baked prefab counterparts. Adapter Inspector slots bind against the returned
        /// <c>GameObject</c> (or one of its components) — no asset-GUID stand-in.
        /// </summary>
        /// <param name="kind">StudioControl kind slug (must satisfy <see cref="IsKnownStudioControlKind"/>).</param>
        /// <param name="panelRoot">Panel root <see cref="Transform"/>; child reparented with <c>worldPositionStays:false</c>.</param>
        /// <param name="duplicateCounter">Per-panel suffix counter — incremented each call so multiple
        /// children of the same kind get unique <c>GameObject.name</c> values (e.g. <c>illuminated-button</c>,
        /// <c>illuminated-button (1)</c>, ...). Suffix-only — does not affect IR slot resolution.</param>
        /// <returns>Instantiated child <c>GameObject</c>; <c>null</c> on unknown kind.</returns>
        static GameObject InstantiatePanelChild(string kind, Transform panelRoot, ref int duplicateCounter)
        {
            if (panelRoot == null || string.IsNullOrEmpty(kind)) return null;

            var name = duplicateCounter == 0 ? kind : $"{kind} ({duplicateCounter})";
            duplicateCounter++;

            var childGo = new GameObject(name, typeof(RectTransform));
            childGo.transform.SetParent(panelRoot, worldPositionStays: false);

            // State-holder + renderer pair — mirrors the per-kind switch in BakeInteractive.
            // Detail rows: panel children carry per-kind defaults at bake time (no IR detail block
            // exists for slot children; future Stage extensions may add one).
            switch (kind)
            {
                case "knob":
                {
                    var knob = childGo.AddComponent<Knob>();
                    knob.ApplyDetail(new KnobDetail());
                    break;
                }
                case "fader":
                {
                    var fader = childGo.AddComponent<Fader>();
                    fader.ApplyDetail(new FaderDetail());
                    break;
                }
                case "detent-ring":
                {
                    var dr = childGo.AddComponent<DetentRing>();
                    dr.ApplyDetail(new DetentRingDetail());
                    break;
                }
                case "vu-meter":
                {
                    var vu = childGo.AddComponent<VUMeter>();
                    if (childGo.GetComponent<VUMeterRenderer>() == null)
                    {
                        childGo.AddComponent<VUMeterRenderer>();
                    }
                    SpawnVUMeterRenderTargets(childGo);
                    vu.ApplyDetail(new VUMeterDetail());
                    break;
                }
                case "oscilloscope":
                {
                    var osc = childGo.AddComponent<Oscilloscope>();
                    osc.ApplyDetail(new OscilloscopeDetail());
                    break;
                }
                case "illuminated-button":
                {
                    var btn = childGo.AddComponent<IlluminatedButton>();
                    if (childGo.GetComponent<IlluminatedButtonRenderer>() == null)
                    {
                        childGo.AddComponent<IlluminatedButtonRenderer>();
                    }
                    SpawnIlluminatedButtonRenderTargets(childGo);
                    btn.ApplyDetail(new IlluminatedButtonDetail());
                    break;
                }
                case "led":
                {
                    var led = childGo.AddComponent<LED>();
                    led.ApplyDetail(new LEDDetail());
                    break;
                }
                case "segmented-readout":
                {
                    var sr = childGo.AddComponent<SegmentedReadout>();
                    if (childGo.GetComponent<SegmentedReadoutRenderer>() == null)
                    {
                        childGo.AddComponent<SegmentedReadoutRenderer>();
                    }
                    var sd = new SegmentedReadoutDetail { digits = 1 };
                    SpawnSegmentedReadoutRenderTargets(childGo, sd);
                    sr.ApplyDetail(sd);
                    break;
                }
                case "themed-overlay-toggle-row":
                {
                    childGo.AddComponent<ThemedOverlayToggleRow>();
                    var renderer = childGo.AddComponent<ThemedOverlayToggleRowRenderer>();
                    SpawnThemedOverlayToggleRowChildren(childGo, out var labelTmp, out var iconImage, out var unityToggle);
                    var rendererSo = new SerializedObject(renderer);
                    var labelProp = rendererSo.FindProperty("_labelText");
                    if (labelProp != null) labelProp.objectReferenceValue = labelTmp;
                    var iconProp = rendererSo.FindProperty("_iconImage");
                    if (iconProp != null) iconProp.objectReferenceValue = iconImage;
                    rendererSo.ApplyModifiedPropertiesWithoutUndo();
                    _ = unityToggle;
                    break;
                }
                // Stage 8 Themed* modal primitive panel-child cases.
                // Renderer-sibling decision per §Pending Decisions:
                //   themed-button  : UGUI-self-renders (no renderer sibling)
                //   themed-label   : UGUI-self-renders (no renderer sibling)
                //   themed-slider  : renderer-sibling needed (ThemedSliderRenderer)
                //   themed-toggle  : renderer-sibling needed (ThemedToggleRenderer)
                //   themed-tab-bar : renderer-sibling needed (ThemedTabBarRenderer)
                //   themed-list    : UGUI-self-renders (no renderer sibling)
                case "themed-button":
                {
                    childGo.AddComponent<ThemedButton>();
                    break;
                }
                case "themed-label":
                {
                    var lbl = childGo.AddComponent<ThemedLabel>();
                    SpawnThemedLabelChild(childGo, out var labelTmp);
                    var lblSo = new SerializedObject(lbl);
                    var tmpProp = lblSo.FindProperty("_tmpText");
                    if (tmpProp != null) tmpProp.objectReferenceValue = labelTmp;
                    lblSo.ApplyModifiedPropertiesWithoutUndo();
                    break;
                }
                case "themed-slider":
                {
                    childGo.AddComponent<ThemedSlider>();
                    var rend = childGo.AddComponent<ThemedSliderRenderer>();
                    SpawnThemedSliderChildren(childGo, out var trackImg, out var fillImg, out var thumbImg, out var valueText);
                    var so = new SerializedObject(rend);
                    var trackProp = so.FindProperty("_trackImage");
                    if (trackProp != null) trackProp.objectReferenceValue = trackImg;
                    var fillProp = so.FindProperty("_fillImage");
                    if (fillProp != null) fillProp.objectReferenceValue = fillImg;
                    var thumbProp = so.FindProperty("_thumbImage");
                    if (thumbProp != null) thumbProp.objectReferenceValue = thumbImg;
                    var textProp = so.FindProperty("_valueText");
                    if (textProp != null) textProp.objectReferenceValue = valueText;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    break;
                }
                case "themed-toggle":
                {
                    childGo.AddComponent<ThemedToggle>();
                    var rend = childGo.AddComponent<ThemedToggleRenderer>();
                    SpawnThemedToggleChildren(childGo, out var checkmarkImg, out var toggleLabelTmp);
                    var so = new SerializedObject(rend);
                    var checkProp = so.FindProperty("_checkmarkImage");
                    if (checkProp != null) checkProp.objectReferenceValue = checkmarkImg;
                    var labelProp2 = so.FindProperty("_labelText");
                    if (labelProp2 != null) labelProp2.objectReferenceValue = toggleLabelTmp;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    break;
                }
                case "themed-tab-bar":
                {
                    var tabBar = childGo.AddComponent<ThemedTabBar>();
                    var rend = childGo.AddComponent<ThemedTabBarRenderer>();
                    SpawnThemedTabBarChildren(childGo, out var stripImg, out var indicatorImg, out var tabLabelTmp);
                    var rendSo = new SerializedObject(rend);
                    var indicatorProp = rendSo.FindProperty("_activeTabIndicator");
                    if (indicatorProp != null) indicatorProp.objectReferenceValue = indicatorImg;
                    var tabLabelProp = rendSo.FindProperty("_tabLabel");
                    if (tabLabelProp != null) tabLabelProp.objectReferenceValue = tabLabelTmp;
                    rendSo.ApplyModifiedPropertiesWithoutUndo();
                    var tabBarSo = new SerializedObject(tabBar);
                    var stripProp = tabBarSo.FindProperty("_tabStripImage");
                    if (stripProp != null) stripProp.objectReferenceValue = stripImg;
                    tabBarSo.ApplyModifiedPropertiesWithoutUndo();
                    break;
                }
                case "themed-list":
                {
                    childGo.AddComponent<ThemedList>();
                    break;
                }
                default:
                    UnityEngine.Object.DestroyImmediate(childGo);
                    return null;
            }

            return childGo;
        }

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
        };

        static bool IsKnownStudioControlKind(string kind)
        {
            return !string.IsNullOrEmpty(kind) && _knownKinds.Contains(kind);
        }

        /// <summary>
        /// Bake one IR interactive into a typed StudioControl prefab. Per-kind switch dispatches to typed
        /// <see cref="IDetailRow"/> parse via <see cref="JsonUtility.FromJson{T}(string)"/> against the
        /// per-row detail substring extracted from raw IR JSON (open-shape <c>detail</c> block cannot
        /// round-trip through JsonUtility otherwise). Schema-mismatch (missing required field) returns
        /// <c>interactive_schema_violation</c>.
        /// </summary>
        public static BakeError BakeInteractive(IrInteractive irRow, int interactiveIndex, string assetPath, string rawIrJson)
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
                return BakeThemedOverlayToggleRow(irRow, assetPath);
            }

            // Stage 8 Themed* modal primitive branch (themed-button, themed-label, themed-slider,
            // themed-toggle, themed-tab-bar, themed-list) — same ceremony skip; renderer-sibling
            // injected per §Pending Decisions audit table in BakeStage8ThemedPrimitive.
            if (irRow.kind == "themed-button" || irRow.kind == "themed-label" ||
                irRow.kind == "themed-slider" || irRow.kind == "themed-toggle" ||
                irRow.kind == "themed-tab-bar" || irRow.kind == "themed-list")
            {
                return BakeStage8ThemedPrimitive(irRow, assetPath);
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
                        if (go.GetComponent<VUMeterRenderer>() == null)
                        {
                            go.AddComponent<VUMeterRenderer>();
                        }
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
                        if (go.GetComponent<IlluminatedButtonRenderer>() == null)
                        {
                            go.AddComponent<IlluminatedButtonRenderer>();
                        }
                        SpawnIlluminatedButtonRenderTargets(go);
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
                        if (go.GetComponent<SegmentedReadoutRenderer>() == null)
                        {
                            go.AddComponent<SegmentedReadoutRenderer>();
                        }
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

                // Apply detail row + write per-usage slug via SerializedObject for deterministic re-bake.
                variant.ApplyDetail(detailRow);
                var so = new SerializedObject(variant);
                var slugProp = so.FindProperty("_slug");
                if (slugProp != null)
                {
                    slugProp.stringValue = irRow.slug ?? string.Empty;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

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

        /// <summary>Spawn the illuminated-button main body Image child + halo child under the prefab root. Idempotent.</summary>
        static void SpawnIlluminatedButtonRenderTargets(GameObject prefabRoot)
        {
            if (prefabRoot == null) return;
            var rootRect = prefabRoot.GetComponent<RectTransform>();
            if (rootRect == null) return;

            // main body Image child — renderer reads first non-halo child Image.
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

            // halo child — radial pulse target.
            if (prefabRoot.transform.Find("halo") == null)
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
            }
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
        static BakeError BakeThemedOverlayToggleRow(IrInteractive irRow, string assetPath)
        {
            GameObject go = null;
            try
            {
                go = new GameObject(irRow.slug);
                go.AddComponent<RectTransform>();

                var row = go.AddComponent<ThemedOverlayToggleRow>();
                var renderer = go.AddComponent<ThemedOverlayToggleRowRenderer>();

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

        // ── Stage 8 Themed* modal primitive bake (T8.2) ─────────────────────────

        /// <summary>
        /// Bake one Stage 8 Themed* IR interactive (themed-button, themed-label, themed-slider,
        /// themed-toggle, themed-tab-bar, themed-list) into a prefab. Renderer-sibling injected
        /// per audit table (see §Pending Decisions). No StudioControlBase ceremony.
        /// Stage 10 lock honored — bake-time-attached only.
        /// </summary>
        static BakeError BakeStage8ThemedPrimitive(IrInteractive irRow, string assetPath)
        {
            GameObject go = null;
            try
            {
                go = new GameObject(irRow.slug);
                go.AddComponent<RectTransform>();

                switch (irRow.kind)
                {
                    case "themed-button":
                        go.AddComponent<ThemedButton>();
                        break;
                    case "themed-label":
                    {
                        var lbl = go.AddComponent<ThemedLabel>();
                        SpawnThemedLabelChild(go, out var labelTmp);
                        var lblSo = new SerializedObject(lbl);
                        var tmpProp = lblSo.FindProperty("_tmpText");
                        if (tmpProp != null) tmpProp.objectReferenceValue = labelTmp;
                        lblSo.ApplyModifiedPropertiesWithoutUndo();
                        break;
                    }
                    case "themed-slider":
                    {
                        go.AddComponent<ThemedSlider>();
                        var rend = go.AddComponent<ThemedSliderRenderer>();
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
                        so.ApplyModifiedPropertiesWithoutUndo();
                        break;
                    }
                    case "themed-toggle":
                    {
                        go.AddComponent<ThemedToggle>();
                        var rend = go.AddComponent<ThemedToggleRenderer>();
                        SpawnThemedToggleChildren(go, out var checkmarkImg, out var labelTmp);
                        var so = new SerializedObject(rend);
                        var checkProp = so.FindProperty("_checkmarkImage");
                        if (checkProp != null) checkProp.objectReferenceValue = checkmarkImg;
                        var labelProp = so.FindProperty("_labelText");
                        if (labelProp != null) labelProp.objectReferenceValue = labelTmp;
                        so.ApplyModifiedPropertiesWithoutUndo();
                        break;
                    }
                    case "themed-tab-bar":
                    {
                        var tabBar = go.AddComponent<ThemedTabBar>();
                        var rend = go.AddComponent<ThemedTabBarRenderer>();
                        SpawnThemedTabBarChildren(go, out var stripImg, out var indicatorImg, out var tabLabelTmp);
                        var rendSo = new SerializedObject(rend);
                        var indicatorProp = rendSo.FindProperty("_activeTabIndicator");
                        if (indicatorProp != null) indicatorProp.objectReferenceValue = indicatorImg;
                        var tabLabelProp = rendSo.FindProperty("_tabLabel");
                        if (tabLabelProp != null) tabLabelProp.objectReferenceValue = tabLabelTmp;
                        rendSo.ApplyModifiedPropertiesWithoutUndo();
                        var tabBarSo = new SerializedObject(tabBar);
                        var stripProp = tabBarSo.FindProperty("_tabStripImage");
                        if (stripProp != null) stripProp.objectReferenceValue = stripImg;
                        tabBarSo.ApplyModifiedPropertiesWithoutUndo();
                        break;
                    }
                    case "themed-list":
                        go.AddComponent<ThemedList>();
                        break;
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

        /// <summary>Spawn Track + Fill + Thumb + ValueText children under a ThemedSlider prefab root. Idempotent.</summary>
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

        /// <summary>Spawn Checkmark + Label children under a ThemedToggle prefab root. Idempotent.</summary>
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

        /// <summary>Spawn ActiveTabIndicator + TabLabel children under a ThemedTabBar prefab root. Idempotent.</summary>
        /// <summary>
        /// Spawn a Label TMP_Text child stretched to fill the prefab root. Idempotent.
        /// Wired into ThemedLabel._tmpText so ApplyTheme can repaint and Detail setter
        /// can push DataAdapter strings into visible text. Without this child the
        /// ThemedLabel component is inert (silent bail in ApplyTheme + no-op Detail).
        /// </summary>
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

            // Backplate Image consumed by ThemedTabBar._tabStripImage (palette repaint target).
            // Sibling-zero so it renders behind ActiveTabIndicator + TabLabel.
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
    }
}
