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
        public static string TitleCaseSlug(string slug)
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
        public static string NormalizeChildKind(string outerKind, string innerKind)
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

        /// <summary>Imp-1 — shared kind dispatcher. Delegates to PanelBaker.BakeChildByKind (TECH-31982).</summary>
        public static void BakeChildByKind(GameObject childGo, string innerKind, PanelChildParamsJson pj, UiTheme theme,
            float preferredWidth = 64f, float preferredHeight = 64f, string panelDisplayName = null)
        {
            Domains.UI.Editor.UiBake.Services.PanelBaker.BakeChildByKind(childGo, innerKind, pj, theme, preferredWidth, preferredHeight, panelDisplayName);
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

        public static void AddBakeWarning(string error, string details, string path)
        {
            UnityEngine.Debug.LogWarning($"[UiBakeHandler] {error}: {details} @ {path}");
            if (_currentBakeWarnings != null)
            {
                _currentBakeWarnings.Add(new BakeError { error = error, details = details, path = path });
            }
        }

        // ── Button delegate (inlined from UiBakeHandler.Button.cs, TECH-31988) ──
        static UnityEngine.GameObject BakeButton(Domains.UI.Data.IrInteractive row, Domains.UI.Editor.UiBake.Services.BakeContext ctx) =>
            new Domains.UI.Editor.UiBake.Services.ButtonBaker(ctx).Bake(row);

        // ── Archetype delegates (inlined from UiBakeHandler.Archetype.cs, TECH-31988) ──
        /// <summary>Delegate — see PanelBaker.IsKnownStudioControlKind.</summary>
        public static bool IsKnownStudioControlKind(string kind)
            => Domains.UI.Editor.UiBake.Services.PanelBaker.IsKnownStudioControlKind(kind);

        /// <summary>Delegate — see PanelBaker.ResolvePanelKindIndex.</summary>
        public static int ResolvePanelKindIndex(string kind)
            => Domains.UI.Editor.UiBake.Services.PanelBaker.ResolvePanelKindIndex(kind);

    }
}
