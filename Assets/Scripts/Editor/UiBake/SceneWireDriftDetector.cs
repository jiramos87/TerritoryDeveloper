// SceneWireDriftDetector.cs
// Layer 3 scene-wire (TECH-28366/28367/28369) — drift detection across 3 classes:
//   (a) Legacy GOs in scene NOT declared in scene-wire-plan (legacy_go)
//   (b) Plan-declared controller missing from scene (unwired_controller)
//   (c) HUD button onClick wired to wrong panel slug (wrong_panel_target)
// Plus canvas-layering audit (TECH-28367) and legacy-GO retirement severity (TECH-28369).

using System;
using System.Collections.Generic;
using System.Linq;

namespace Territory.Editor.UiBake
{
    /// <summary>
    /// Detects three drift classes between a scene's GO hierarchy and the scene-wire-plan.
    /// Used by validate-scene-wire-drift.mjs (Node) and Stage3SceneWire C# tests.
    /// </summary>
    public static class SceneWireDriftDetector
    {
        // ── Drift detection ───────────────────────────────────────────────────────

        /// <summary>
        /// Detects drift between scene yaml GO names and plan entries.
        /// </summary>
        /// <param name="sceneYaml">Raw unity scene yaml text (or synthetic subset for tests).</param>
        /// <param name="planEntries">Entries from scene-wire-plan.yaml.</param>
        /// <param name="hudButtonTargets">Map of button-id → actual wired panel slug (from scene).</param>
        /// <param name="expectedButtonTargets">Map of button-id → DB-declared target (from plan). Optional.</param>
        public static List<DriftFinding> DetectDrift(
            string sceneYaml,
            IReadOnlyList<PlanEntry> planEntries,
            IDictionary<string, string> hudButtonTargets,
            IDictionary<string, string> expectedButtonTargets = null)
        {
            var findings = new List<DriftFinding>();
            var planGoNames = new HashSet<string>(
                planEntries.Select(e => e.GoName), StringComparer.Ordinal);

            // Parse GO names from scene yaml (synthetic or real).
            var sceneGoNames = ParseGoNamesFromYaml(sceneYaml);

            // Class (a): legacy GOs in scene NOT declared in plan.
            foreach (var goName in sceneGoNames)
            {
                if (!planGoNames.Contains(goName))
                {
                    findings.Add(new DriftFinding
                    {
                        Kind = "legacy_go",
                        Name = goName,
                        Detail = $"GO '{goName}' present in scene but not declared in scene-wire-plan",
                    });
                }
            }

            // Class (b): plan-declared controller missing from scene.
            foreach (var entry in planEntries)
            {
                if (!sceneGoNames.Contains(entry.GoName))
                {
                    findings.Add(new DriftFinding
                    {
                        Kind = "unwired_controller",
                        Name = entry.GoName,
                        Detail = $"Plan declares GO '{entry.GoName}' (panel='{entry.PanelSlug}') " +
                                 "but it is missing from scene",
                    });
                }
            }

            // Class (c): HUD button onClick wired to wrong panel slug.
            if (expectedButtonTargets != null)
            {
                foreach (var kv in hudButtonTargets)
                {
                    if (expectedButtonTargets.TryGetValue(kv.Key, out var expected) &&
                        kv.Value != expected)
                    {
                        findings.Add(new DriftFinding
                        {
                            Kind     = "wrong_panel_target",
                            Name     = kv.Key,
                            Detail   = $"Button '{kv.Key}' wired to '{kv.Value}' but plan declares '{expected}'",
                            Expected = expected,
                            Actual   = kv.Value,
                        });
                    }
                }
            }

            return findings;
        }

        // ── Canvas-layering audit (TECH-28367) ────────────────────────────────────

        /// <summary>
        /// Layer hierarchy rule: HUD &lt; SubViews &lt; Modals &lt; Notifications &lt; Cursor.
        /// Flags inversions as canvas_layer_inversion findings.
        /// </summary>
        public static List<DriftFinding> AuditCanvasLayering(IReadOnlyList<CanvasLayerRow> rows)
        {
            var findings = new List<DriftFinding>();

            // Required ascending order.
            var canonicalOrder = new[] { "HUD", "SubViews", "Modals", "Notifications", "Cursor" };

            // Build lookup by layer name.
            var byName = rows.ToDictionary(r => r.LayerName, r => r.SortingOrder,
                StringComparer.OrdinalIgnoreCase);

            // Check each adjacent canonical pair.
            for (int i = 0; i < canonicalOrder.Length - 1; i++)
            {
                string lower = canonicalOrder[i];
                string higher = canonicalOrder[i + 1];

                if (!byName.TryGetValue(lower, out int lowerOrder) ||
                    !byName.TryGetValue(higher, out int higherOrder))
                    continue; // skip if layer not present in data

                if (higherOrder <= lowerOrder)
                {
                    findings.Add(new DriftFinding
                    {
                        Kind     = "canvas_layer_inversion",
                        Name     = $"{lower}→{higher} (Notifications inversion)",
                        Detail   = $"Layer '{higher}' sortingOrder={higherOrder} is not above " +
                                   $"'{lower}' sortingOrder={lowerOrder}",
                        Expected = ">",
                        Actual   = "<",
                    });
                }
            }

            return findings;
        }

        // ── Legacy-GO retirement severity (TECH-28369) ────────────────────────────

        /// <summary>
        /// Checks legacy-GO retirement table. Returns ERROR severity when
        /// retire_after_stage has closed but GO still present in scene.
        /// </summary>
        public static List<LegacyGoFinding> CheckLegacyGoRetirement(
            IReadOnlyList<LegacyGoRow> rows,
            IEnumerable<string> closedStages,
            Func<string, string, bool> goPresenceChecker)
        {
            var closed = new HashSet<string>(closedStages, StringComparer.OrdinalIgnoreCase);
            var findings = new List<LegacyGoFinding>();

            foreach (var row in rows)
            {
                bool retireStageClosed = closed.Contains(row.RetireAfterStage);
                bool goPresent = goPresenceChecker(row.SceneName, row.HierarchyPath);

                if (goPresent)
                {
                    findings.Add(new LegacyGoFinding
                    {
                        SceneName      = row.SceneName,
                        HierarchyPath  = row.HierarchyPath,
                        RetiredByPanel = row.RetiredByPanel,
                        Severity       = retireStageClosed ? "ERROR" : "WARN",
                        Detail         = retireStageClosed
                            ? $"Legacy GO '{row.HierarchyPath}' must be deleted — " +
                              $"retire_after_stage '{row.RetireAfterStage}' is closed"
                            : $"Legacy GO '{row.HierarchyPath}' still present " +
                              $"(retire_after_stage '{row.RetireAfterStage}' not yet closed)",
                    });
                }
            }

            return findings;
        }

        // ── YAML GO-name parser ───────────────────────────────────────────────────

        private static HashSet<string> ParseGoNamesFromYaml(string yaml)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var line in yaml.Split('\n'))
            {
                var trimmed = line.Trim();
                // Match "- m_Name: GoName" (real unity yaml) or "- m_Name: GoName" lines.
                if (trimmed.StartsWith("- m_Name:"))
                {
                    var name = trimmed.Substring("- m_Name:".Length).Trim();
                    if (!string.IsNullOrEmpty(name))
                        names.Add(name);
                }
            }
            return names;
        }

        // ── Data types ────────────────────────────────────────────────────────────

        public sealed class DriftFinding
        {
            public string Kind     { get; set; }
            public string Name     { get; set; }
            public string Detail   { get; set; }
            public string Expected { get; set; }
            public string Actual   { get; set; }
        }

        public sealed class LegacyGoFinding
        {
            public string SceneName      { get; set; }
            public string HierarchyPath  { get; set; }
            public string RetiredByPanel { get; set; }
            public string Severity       { get; set; } // "WARN" | "ERROR"
            public string Detail         { get; set; }
        }

        public sealed class PlanEntry
        {
            public string GoName     { get; set; }
            public string PanelSlug  { get; set; }
        }

        public sealed class CanvasLayerRow
        {
            public string LayerName     { get; set; }
            public int    SortingOrder  { get; set; }
        }

        public sealed class LegacyGoRow
        {
            public string SceneName        { get; set; }
            public string HierarchyPath    { get; set; }
            public string RetiredByPanel   { get; set; }
            public string RetireAfterStage { get; set; }
        }
    }
}
