using System;
using System.Collections.Generic;
using System.IO;
using Territory.UI;
using UnityEditor;
using UnityEngine;

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

            if (ir.panels != null)
            {
                for (int i = 0; i < ir.panels.Length; i++)
                {
                    var panel = ir.panels[i];
                    if (panel == null || string.IsNullOrEmpty(panel.slug)) continue;

                    var assetPath = $"{dir.TrimEnd('/')}/{panel.slug}.prefab";
                    var err = SaveEmptyPlaceholderPrefab(panel.slug, assetPath);
                    if (err != null) return err;
                }
            }

            if (ir.interactives != null)
            {
                for (int i = 0; i < ir.interactives.Length; i++)
                {
                    var ic = ir.interactives[i];
                    if (ic == null || string.IsNullOrEmpty(ic.slug)) continue;

                    var assetPath = $"{dir.TrimEnd('/')}/{ic.slug}.prefab";
                    var err = SaveEmptyPlaceholderPrefab(ic.slug, assetPath);
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
