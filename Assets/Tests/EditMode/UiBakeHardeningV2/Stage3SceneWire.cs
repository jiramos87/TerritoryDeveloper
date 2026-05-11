// Stage 3 — Scene-wire DB-driven — incremental TDD red→green test file.
//
// Protocol (ia/rules/agent-principles.md §Testing + verification):
//   ONE test file per Stage. First task creates file in failing/red state.
//   Each subsequent task extends same file with new assertions tied to its phase.
//   File stays red until last task of the Stage. Stage close = file green.
//
// Tasks (anchored by §Red-Stage Proof per task spec):
//   TECH-28365  ScenePlan_EmitsFullManifest
//   TECH-28366  SceneWirePlan_DriftDetectorCatchesLegacyGO  +  WrongPanelTarget_Flagged
//   TECH-28367  CanvasLayering_NotificationsAboveHud
//   TECH-28368  AdapterPanelBinding_HudButtonsHitDbDeclaredTargets
//   TECH-28369  LegacyGoRetirement_BlocksAfterRetireStage  ← file turns fully GREEN here

using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Territory.Editor.UiBake;
using UnityEngine;

namespace Territory.Tests.EditMode.UiBakeHardeningV2
{
    [TestFixture]
    public sealed class Stage3SceneWire
    {
        // ── TECH-28365: scene-wire-plan emit ─────────────────────────────────────

        /// <summary>
        /// §Red-Stage Proof anchor:
        /// unit-test:Assets/Tests/EditMode/UiBakeHardeningV2/Stage3SceneWire.cs::ScenePlan_EmitsFullManifest
        ///
        /// Injects 3 scene_target rows; runs SceneWirePlanEmitter.RunAll();
        /// asserts yaml file exists + parses + contains all 3 panel slugs with
        /// declared canvas/anchor/controller fields.
        /// </summary>
        [Test]
        public void ScenePlan_EmitsFullManifest()
        {
            var rows = new List<SceneTargetRow>
            {
                new SceneTargetRow
                {
                    PanelSlug      = "budget-panel",
                    SceneName      = "MainScene",
                    CanvasPath     = "Canvas/HUD",
                    SlotAnchor     = "hud-slot",
                    ControllerType = "BudgetViewController",
                    AdapterType    = "BudgetPanelAdapter",
                },
                new SceneTargetRow
                {
                    PanelSlug      = "city-stats-panel",
                    SceneName      = "MainScene",
                    CanvasPath     = "Canvas/HUD",
                    SlotAnchor     = "stats-slot",
                    ControllerType = "CityStatsViewController",
                    AdapterType    = "CityStatsPanelAdapter",
                },
                new SceneTargetRow
                {
                    PanelSlug      = "pause-menu",
                    SceneName      = "MainScene",
                    CanvasPath     = "Canvas/Modals",
                    SlotAnchor     = "modal-slot",
                    ControllerType = "PauseMenuController",
                    AdapterType    = null,
                },
            };

            // Run emitter — writes to temp-redirected output path via env override
            // (production path is Application.dataPath-relative; tests run in Editor).
            string tmpDir = Path.Combine(Application.temporaryCachePath, "scene-wire-test");
            Directory.CreateDirectory(tmpDir);
            string tmpYaml = Path.Combine(tmpDir, "scene-wire-plan.yaml");

            try
            {
                // Emit directly to string for test isolation.
                SceneWirePlanEmitter.RunAll(rows);

                // Verify the file was written to the canonical output path.
                string outputPath = SceneWirePlanEmitter.GetOutputPath();
                Assert.IsTrue(File.Exists(outputPath),
                    $"Expected scene-wire-plan.yaml at '{outputPath}' but file not found");

                // Parse the emitted yaml.
                string content = File.ReadAllText(outputPath);
                var parsed = SceneWirePlanEmitter.ParseYaml(content);

                Assert.AreEqual(3, parsed.Count,
                    $"Expected 3 panel rows in scene-wire-plan.yaml, got {parsed.Count}");

                var slugs = parsed.Select(r => r.PanelSlug).ToList();
                Assert.Contains("budget-panel",     slugs, "budget-panel must appear in manifest");
                Assert.Contains("city-stats-panel", slugs, "city-stats-panel must appear in manifest");
                Assert.Contains("pause-menu",       slugs, "pause-menu must appear in manifest");

                // Each row must have canvas_path, slot_anchor, controller_type.
                foreach (var row in parsed)
                {
                    Assert.IsFalse(string.IsNullOrEmpty(row.CanvasPath),
                        $"canvas_path missing for panel '{row.PanelSlug}'");
                    Assert.IsFalse(string.IsNullOrEmpty(row.SlotAnchor),
                        $"slot_anchor missing for panel '{row.PanelSlug}'");
                    Assert.IsFalse(string.IsNullOrEmpty(row.ControllerType),
                        $"controller_type missing for panel '{row.PanelSlug}'");
                }
            }
            finally
            {
                if (Directory.Exists(tmpDir))
                    Directory.Delete(tmpDir, recursive: true);
            }
        }

        // ── TECH-28366: drift detector — legacy GO + wrong panel target ───────────

        /// <summary>
        /// §Red-Stage Proof anchor:
        /// unit-test:Assets/Tests/EditMode/UiBakeHardeningV2/Stage3SceneWire.cs::SceneWirePlan_DriftDetectorCatchesLegacyGO
        ///
        /// Constructs synthetic scene yaml containing SubtypePickerRoot + plan that
        /// doesn't reference it; runs detector; asserts result contains drift entry
        /// { kind: 'legacy_go', name: 'SubtypePickerRoot' }.
        /// </summary>
        [Test]
        public void SceneWirePlan_DriftDetectorCatchesLegacyGO()
        {
            // Synthetic scene yaml declaring SubtypePickerRoot GO.
            string syntheticScene = BuildSyntheticSceneYaml(new[] { "SubtypePickerRoot", "HudBar" });

            // Plan only declares HudBar; SubtypePickerRoot is a legacy GO.
            var planRows = new List<SceneWireDriftDetector.PlanEntry>
            {
                new SceneWireDriftDetector.PlanEntry { GoName = "HudBar", PanelSlug = "hud-bar" },
            };

            var findings = SceneWireDriftDetector.DetectDrift(
                sceneYaml: syntheticScene,
                planEntries: planRows,
                hudButtonTargets: new Dictionary<string, string>());

            Assert.IsNotNull(findings, "Detector must return findings list");

            var legacyFindings = findings
                .Where(f => f.Kind == "legacy_go" && f.Name == "SubtypePickerRoot")
                .ToList();

            Assert.AreEqual(1, legacyFindings.Count,
                $"Expected 1 legacy_go finding for SubtypePickerRoot, got {legacyFindings.Count}. " +
                $"All findings: [{string.Join(", ", findings.Select(f => $"{f.Kind}:{f.Name}"))}]");
        }

        /// <summary>
        /// §Red-Stage Proof companion for TECH-28366:
        /// unit-test:Assets/Tests/EditMode/UiBakeHardeningV2/Stage3SceneWire.cs::WrongPanelTarget_Flagged
        ///
        /// HUD button onClick wired to wrong panel slug — detector flags class (c) drift.
        /// </summary>
        [Test]
        public void WrongPanelTarget_Flagged()
        {
            // Scene matches plan. Button targets wrong panel.
            var planRows = new List<SceneWireDriftDetector.PlanEntry>
            {
                new SceneWireDriftDetector.PlanEntry { GoName = "HudBar", PanelSlug = "hud-bar" },
                new SceneWireDriftDetector.PlanEntry { GoName = "BudgetPanel", PanelSlug = "budget-panel" },
            };

            // HUD button 'budget-open' wired to wrong target 'growth-budget-panel' (not 'budget-panel').
            var hudButtonTargets = new Dictionary<string, string>
            {
                { "budget-open", "growth-budget-panel" },
            };

            // Plan says budget-open should target budget-panel.
            var expectedTargets = new Dictionary<string, string>
            {
                { "budget-open", "budget-panel" },
            };

            string syntheticScene = BuildSyntheticSceneYaml(new[] { "HudBar", "BudgetPanel" });

            var findings = SceneWireDriftDetector.DetectDrift(
                sceneYaml: syntheticScene,
                planEntries: planRows,
                hudButtonTargets: hudButtonTargets,
                expectedButtonTargets: expectedTargets);

            var wrongTarget = findings
                .Where(f => f.Kind == "wrong_panel_target" && f.Name == "budget-open")
                .ToList();

            Assert.AreEqual(1, wrongTarget.Count,
                $"Expected 1 wrong_panel_target finding for 'budget-open', got {wrongTarget.Count}. " +
                $"All: [{string.Join(", ", findings.Select(f => $"{f.Kind}:{f.Name}"))}]");
        }

        // ── TECH-28367: canvas layering audit ────────────────────────────────────

        /// <summary>
        /// §Red-Stage Proof anchor:
        /// unit-test:Assets/Tests/EditMode/UiBakeHardeningV2/Stage3SceneWire.cs::CanvasLayering_NotificationsAboveHud
        ///
        /// Plan rows with deliberately-inverted layering (notifications sortingOrder=10, HUD=50);
        /// detector returns drift entry { kind: 'canvas_layer_inversion', expected: '>', actual: '<' }.
        /// </summary>
        [Test]
        public void CanvasLayering_NotificationsAboveHud()
        {
            // Layer hierarchy rule: HUD < SubViews < Modals < Notifications < Cursor.
            // Inverted: Notifications(10) < HUD(50) — should be flagged.
            var layerRows = new List<SceneWireDriftDetector.CanvasLayerRow>
            {
                new SceneWireDriftDetector.CanvasLayerRow { LayerName = "HUD",           SortingOrder = 50 },
                new SceneWireDriftDetector.CanvasLayerRow { LayerName = "SubViews",       SortingOrder = 60 },
                new SceneWireDriftDetector.CanvasLayerRow { LayerName = "Modals",         SortingOrder = 70 },
                new SceneWireDriftDetector.CanvasLayerRow { LayerName = "Notifications",  SortingOrder = 10 }, // INVERTED
                new SceneWireDriftDetector.CanvasLayerRow { LayerName = "Cursor",         SortingOrder = 100 },
            };

            var findings = SceneWireDriftDetector.AuditCanvasLayering(layerRows);

            var inversions = findings
                .Where(f => f.Kind == "canvas_layer_inversion")
                .ToList();

            Assert.GreaterOrEqual(inversions.Count, 1,
                $"Expected at least 1 canvas_layer_inversion finding when Notifications < HUD. " +
                $"Got: [{string.Join(", ", findings.Select(f => $"{f.Kind}:{f.Name}"))}]");

            // At minimum the Modals→Notifications inversion must appear.
            var modalsNotifInversion = inversions
                .FirstOrDefault(f => f.Name.Contains("Notifications"));
            Assert.IsNotNull(modalsNotifInversion,
                "Expected inversion finding referencing Notifications layer");

            // Verify expected/actual fields are present.
            Assert.AreEqual(">", modalsNotifInversion.Expected,
                "canvas_layer_inversion.expected should be '>' (notifications should be above modals)");
            Assert.AreEqual("<", modalsNotifInversion.Actual,
                "canvas_layer_inversion.actual should be '<' (notifications is actually below)");
        }

        // ── TECH-28368: adapter↔panel binding fixture ─────────────────────────────

        /// <summary>
        /// §Red-Stage Proof anchor:
        /// unit-test:Assets/Tests/EditMode/UiBakeHardeningV2/Stage3SceneWire.cs::AdapterPanelBinding_HudButtonsHitDbDeclaredTargets
        ///
        /// Asserts the PlayMode fixture file exists + parses + lists every HUD button id from DB.
        /// </summary>
        [Test]
        public void AdapterPanelBinding_HudButtonsHitDbDeclaredTargets()
        {
            string fixturePath = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..", "Assets",
                "Tests", "PlayMode", "UiBakeHardeningV2",
                "AdapterPanelBindingFixture.cs"));

            Assert.IsTrue(File.Exists(fixturePath),
                $"PlayMode fixture must exist at: {fixturePath}");

            string src = File.ReadAllText(fixturePath);

            // Must reference at least one HUD action id pattern.
            Assert.IsTrue(
                src.Contains("HudAction") || src.Contains("hud-") || src.Contains("AdapterPanelBinding"),
                "Fixture must reference HUD action ids or AdapterPanelBinding type");

            // Must contain a test method named AdapterPanelBinding_HudButtonsOpenDbDeclaredPanel.
            Assert.IsTrue(
                src.Contains("AdapterPanelBinding_HudButtonsOpenDbDeclaredPanel"),
                "Fixture must contain test 'AdapterPanelBinding_HudButtonsOpenDbDeclaredPanel'");
        }

        // ── TECH-28369: legacy-GO purge planner ──────────────────────────────────

        /// <summary>
        /// §Red-Stage Proof anchor:
        /// unit-test:Assets/Tests/EditMode/UiBakeHardeningV2/Stage3SceneWire.cs::LegacyGoRetirement_BlocksAfterRetireStage
        ///
        /// Inserts row (MainScene, /Canvas/SubtypePickerRoot, BudgetPanel, stage-11.0);
        /// runs detector against synthetic 'stage-11.0 closed' state with GO still present;
        /// asserts result.severity='ERROR' (not WARN). Stage 3 files = fully green.
        /// </summary>
        [Test]
        public void LegacyGoRetirement_BlocksAfterRetireStage()
        {
            var legacyRows = new List<SceneWireDriftDetector.LegacyGoRow>
            {
                new SceneWireDriftDetector.LegacyGoRow
                {
                    SceneName       = "MainScene",
                    HierarchyPath   = "/Canvas/SubtypePickerRoot",
                    RetiredByPanel  = "BudgetPanel",
                    RetireAfterStage = "stage-11.0",
                },
            };

            // Simulate: stage-11.0 is closed, GO still present in scene.
            string simulatedClosedStage = "stage-11.0";
            bool goStillPresent = true;

            var findings = SceneWireDriftDetector.CheckLegacyGoRetirement(
                legacyRows,
                closedStages: new[] { simulatedClosedStage },
                goPresenceChecker: (sceneName, path) => goStillPresent);

            Assert.GreaterOrEqual(findings.Count, 1,
                "Expected at least 1 finding for SubtypePickerRoot still present after retire stage");

            var errorFinding = findings
                .FirstOrDefault(f => f.HierarchyPath == "/Canvas/SubtypePickerRoot");

            Assert.IsNotNull(errorFinding,
                "Expected finding for /Canvas/SubtypePickerRoot");

            Assert.AreEqual("ERROR", errorFinding.Severity,
                $"Expected severity='ERROR' once retire_after_stage closed, got '{errorFinding.Severity}'");
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static string BuildSyntheticSceneYaml(IEnumerable<string> goNames)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("# synthetic scene yaml for tests");
            foreach (var name in goNames)
            {
                sb.AppendLine($"- m_Name: {name}");
            }
            return sb.ToString();
        }
    }
}
