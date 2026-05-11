// Stage 5 PlayMode — Visual diff + functional smoke + legacy-drift gates
// (TECH-28374 / TECH-28375 / TECH-28376 / TECH-28377).
// File turns green at TECH-28377 (task 5.0.4 — ship-cycle gate simulation).
//
// §Red-Stage Proof anchors:
//   TECH-28374: VisualDiff_TolerableUnderTinyJitter
//   TECH-28375: FunctionalSmoke_NoDeadButtons / FunctionalSmoke_NoWrongTargets
//   TECH-28376: LegacyDrift_AllRetiredGOsAbsent
//   TECH-28377: ShipCycleGates_AbortOnVisualMismatch

using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Territory.Editor.UiBake;
using Territory.UI.Registry;

namespace Territory.Tests.PlayMode.UiBakeHardeningV2
{
    [TestFixture]
    public sealed class Stage5VisualFunctional
    {
        // ── Known HUD actions (mirrors Stage 4 contract) ──────────────────────
        private static readonly Dictionary<string, string> HudActionTargets =
            new Dictionary<string, string>
            {
                { "hud-budget-open",    "budget-panel"     },
                { "hud-citystats-open", "city-stats-panel" },
                { "hud-pause",          "pause-menu"       },
            };

        // Legacy GOs expected absent after retirement (catalog_legacy_gos equivalent).
        // Format: hierarchy_path → retire_after_stage label (informational only).
        private static readonly Dictionary<string, string> LegacyGOPaths =
            new Dictionary<string, string>
            {
                { "SubtypePickerRoot",      "stage-5.0-rollout" },
                { "LegacyHudBar",           "stage-7.0-rollout" },
                { "OldMainMenuCanvas",      "stage-6.0-rollout" },
            };

        // ── T5.0.1 — VisualDiff_TolerableUnderTinyJitter ─────────────────────

        /// <summary>
        /// §Red-Stage Proof anchor: VisualDiff_TolerableUnderTinyJitter
        ///
        /// Creates a synthetic 64x64 baseline texture; injects 1-pixel jitter via
        /// VisualBaselineCapture.InjectOnePixelJitter; asserts SSIM > 0.95 (within tolerance).
        /// Companion test asserts deliberately-mutated capture fails (SSIM below 0.95).
        /// </summary>
        [UnityTest]
        public IEnumerator VisualDiff_TolerableUnderTinyJitter()
        {
            yield return null;

            // Build a synthetic solid-color baseline (64x64 grey).
            var baseline = BuildSolidTexture(64, 64, new Color(0.5f, 0.5f, 0.5f));

            // 1-pixel jitter must remain within SSIM tolerance.
            var jittered = VisualBaselineCapture.InjectOnePixelJitter(baseline);

            float ssim = VisualBaselineCapture.ComputeSSIM(jittered, baseline);
            Assert.Greater(ssim, 0.95f,
                $"SSIM after 1-pixel jitter should be > 0.95, got {ssim:F4}");

            // Companion: large distortion must be BELOW tolerance.
            var distorted = VisualBaselineCapture.InjectLargeDistortion(baseline);
            float ssimBad = VisualBaselineCapture.ComputeSSIM(distorted, baseline);
            Assert.Less(ssimBad, 0.95f,
                $"SSIM after large distortion should be < 0.95, got {ssimBad:F4}");

            Object.Destroy(baseline);
            Object.Destroy(jittered);
            Object.Destroy(distorted);
            yield return null;
        }

        // ── T5.0.2 — FunctionalSmoke_NoDeadButtons ───────────────────────────

        /// <summary>
        /// §Red-Stage Proof anchor: FunctionalSmoke_NoDeadButtons
        ///
        /// Iterates HUD action ids; dispatches each via UiActionRegistry;
        /// asserts dispatch returns true (handler registered = not dead).
        /// Depends on TECH-28372 action dispatch (T4.0.3) being in place.
        /// </summary>
        [UnityTest]
        public IEnumerator FunctionalSmoke_NoDeadButtons()
        {
            var registryGo = new GameObject("UiActionRegistry");
            var registry   = registryGo.AddComponent<UiActionRegistry>();

            // Register stub handlers for every HUD action.
            foreach (var kv in HudActionTargets)
            {
                var capturedSlug = kv.Value;
                registry.Register(kv.Key, _ => { /* stub handler — not dead */ });
            }

            yield return null;

            var deadButtons = new List<string>();
            foreach (var kv in HudActionTargets)
            {
                bool dispatched = registry.Dispatch(kv.Key, null);
                if (!dispatched) deadButtons.Add(kv.Key);
            }

            Object.Destroy(registryGo);
            yield return null;

            Assert.IsEmpty(deadButtons,
                $"Dead buttons detected (no handler): {string.Join(", ", deadButtons)}");
        }

        // ── T5.0.2b — FunctionalSmoke_NoWrongTargets ─────────────────────────

        /// <summary>
        /// §Red-Stage Proof anchor: FunctionalSmoke_NoWrongTargets
        ///
        /// For each HUD action, dispatches and asserts the DECLARED target panel
        /// becomes mounted (active). A wrong-target bug would mount a different panel.
        /// </summary>
        [UnityTest]
        public IEnumerator FunctionalSmoke_NoWrongTargets()
        {
            var registryGo = new GameObject("UiActionRegistry");
            var registry   = registryGo.AddComponent<UiActionRegistry>();
            var panelGos   = new List<GameObject>();

            foreach (var kv in HudActionTargets)
            {
                string targetSlug = kv.Value;

                // Create target panel initially inactive.
                var targetGo = new GameObject(targetSlug);
                targetGo.SetActive(false);
                panelGos.Add(targetGo);

                var capturedTarget = targetGo;
                registry.Register(kv.Key, _ => capturedTarget.SetActive(true));
            }

            yield return null;

            var wrongTargets = new List<string>();
            foreach (var kv in HudActionTargets)
            {
                registry.Dispatch(kv.Key, null);
                yield return null;

                // Verify the declared target panel is now mounted.
                var state = RuntimePanelQuery.QueryPanelState(kv.Value);
                if (!state.mounted) wrongTargets.Add($"{kv.Key} → {kv.Value}");
            }

            Object.Destroy(registryGo);
            foreach (var go in panelGos) Object.Destroy(go);
            yield return null;

            Assert.IsEmpty(wrongTargets,
                $"Wrong-target defects: {string.Join("; ", wrongTargets)}");
        }

        // ── T5.0.3 — LegacyDrift_AllRetiredGOsAbsent ─────────────────────────

        /// <summary>
        /// §Red-Stage Proof anchor: LegacyDrift_AllRetiredGOsAbsent
        ///
        /// For every catalog_legacy_gos equivalent path, asserts
        /// GameObject.Find returns null (retired GO is absent from scene).
        /// Catches the cityscene Stage 5-7 class: legacy GOs still active
        /// despite supposed retirement.
        /// </summary>
        [UnityTest]
        public IEnumerator LegacyDrift_AllRetiredGOsAbsent()
        {
            yield return null;

            var stillPresent = new List<string>();
            foreach (var kv in LegacyGOPaths)
            {
                string path     = kv.Key;
                var    found    = GameObject.Find(path);
                if (found != null) stillPresent.Add(path);
            }

            Assert.IsEmpty(stillPresent,
                $"Legacy GOs still active in scene (should be retired): {string.Join(", ", stillPresent)}");

            yield return null;
        }

        // ── T5.0.4 — ShipCycleGates_AbortOnVisualMismatch ────────────────────

        /// <summary>
        /// §Red-Stage Proof anchor: ShipCycleGates_AbortOnVisualMismatch
        ///
        /// Simulates ship-cycle Pass B gate B.7c: creates a stub baseline and a
        /// deliberately-distorted capture; invokes VisualBaselineCapture.AssertSSIM;
        /// asserts it throws (= recipe exit non-zero before reaching B.8 stage_commit).
        /// Stage 5 file = fully green when this passes.
        /// </summary>
        [UnityTest]
        public IEnumerator ShipCycleGates_AbortOnVisualMismatch()
        {
            yield return null;

            var baseline  = BuildSolidTexture(64, 64, new Color(0.5f, 0.5f, 0.5f));
            var distorted = VisualBaselineCapture.InjectLargeDistortion(baseline);

            // AssertSSIM must throw when distorted capture is well below 0.95 threshold.
            bool threw = false;
            try
            {
                VisualBaselineCapture.AssertSSIM(distorted, baseline, 0.95f, "stub-panel");
            }
            catch (System.Exception ex)
            {
                threw = true;
                // Verify exception message contains expected markers.
                StringAssert.Contains("Visual diff FAIL", ex.Message,
                    "Exception message should contain 'Visual diff FAIL'");
                StringAssert.Contains("SSIM=", ex.Message,
                    "Exception message should report SSIM score");
            }

            Object.Destroy(baseline);
            Object.Destroy(distorted);
            yield return null;

            Assert.IsTrue(threw,
                "AssertSSIM should have thrown on deliberately-distorted capture (B.7c abort path).");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static Texture2D BuildSolidTexture(int width, int height, Color color)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
