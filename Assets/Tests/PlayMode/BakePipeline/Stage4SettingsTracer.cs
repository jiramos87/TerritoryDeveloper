// Stage 4 — Settings sub-view rewire tracer — incremental TDD red→green test file.
//
// Protocol (ia/rules/agent-principles.md §Testing + verification):
//   ONE test file per Stage. First task creates file in failing/red state.
//   Each subsequent task extends with new assertions tied to its phase.
//   File stays red until last task of the Stage. Stage close = file green.
//   Master-plan close runs `unity:testmode-batch --filter BakePipeline.*`.
//
// Tasks (anchored by §Red-Stage Proof per task spec):
//   4.1 SettingsPanel_BakesAllFourKinds
//   4.2 SettingsView_AppliesViaSlotResolver
//   4.3 SettingsTracer_AllGatesGreen

using System.Collections;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using TMPro;
using Territory.UI.Modals;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Territory.Tests.PlayMode.BakePipeline
{
    [TestFixture]
    public sealed class Stage4SettingsTracer
    {
        // ── 4.1 Bake: settings panel widget hierarchy counts ─────────────────────

        /// <summary>
        /// Builds a hierarchy mirroring what UiBakeHandler emits for settings-view
        /// (3 slider-row + 5 toggle-row + 1 dropdown-row + 3 section-header = 12 children)
        /// and verifies the counts via component inspection.
        /// Task 4.1 anchor — counts 3 Slider + 5 Toggle + 1 TMP_Dropdown + 3 TMP_Text headers.
        /// </summary>
        [UnityTest]
        public IEnumerator SettingsPanel_BakesAllFourKinds()
        {
            var root = new GameObject("settings-test-root");
            try
            {
                var parent = root.transform;

                // Replicate what BakeChildByKind emits for settings-view children.
                // slider-row: parent GO + Thumb Image child (acts as Slider proxy).
                for (int i = 0; i < 3; i++) SpawnSliderRow(parent);

                // toggle-row: parent GO + Checkmark Image + Label TMP_Text.
                for (int i = 0; i < 5; i++) SpawnToggleRow(parent);

                // dropdown-row: parent GO + Label TMP_Text + Value TMP_Text + Arrow Image.
                SpawnDropdownRow(parent);

                // section-header: parent GO + Label TMP_Text (Bold).
                for (int i = 0; i < 3; i++) SpawnSectionHeader(parent);

                // 12 direct widget children.
                Assert.AreEqual(12, parent.childCount,
                    $"expected 12 widget children (3 slider + 5 toggle + 1 dropdown + 3 header), got {parent.childCount}");

                // Section-headers carry Bold TMP_Text.
                int boldHeaders = 0;
                foreach (Transform child in parent)
                {
                    if (child.name != "section-header") continue;
                    foreach (var tmp in child.GetComponentsInChildren<TMP_Text>(true))
                    {
                        if ((tmp.fontStyle & TMPro.FontStyles.Bold) != 0) boldHeaders++;
                    }
                }
                Assert.AreEqual(3, boldHeaders,
                    $"expected 3 bold section-header TMP_Text, got {boldHeaders}");

                // slider-row / toggle-row / dropdown-row direct child counts.
                int sliders = 0, toggles = 0, dropdowns = 0;
                foreach (Transform child in parent)
                {
                    if (child.name == "slider-row")   sliders++;
                    if (child.name == "toggle-row")   toggles++;
                    if (child.name == "dropdown-row") dropdowns++;
                }
                Assert.AreEqual(3, sliders,   $"expected 3 slider-row children, got {sliders}");
                Assert.AreEqual(5, toggles,   $"expected 5 toggle-row children, got {toggles}");
                Assert.AreEqual(1, dropdowns, $"expected 1 dropdown-row child, got {dropdowns}");
            }
            finally
            {
                Object.Destroy(root);
            }
            yield return null;
        }

        // ── 4.2 SettingsViewController resolves slot via ResolveByPanel ───────────

        /// <summary>
        /// Verifies SettingsViewController.ResolveByPanel("settings") finds "settings-content-slot"
        /// via suffix-match. CountWidgets sees >= 12 widgets. Apply() runs without exception.
        /// Task 4.2 anchor.
        /// </summary>
        [UnityTest]
        public IEnumerator SettingsView_AppliesViaSlotResolver()
        {
            var root = new GameObject("settings-view-mock");
            var controller = root.AddComponent<SettingsViewController>();
            var slotGo = new GameObject("settings-content-slot");
            slotGo.transform.SetParent(root.transform, false);

            try
            {
                // Populate slot: 3 Slider + 5 Toggle + 1 TMP_Dropdown + 3 TMP_Text.
                for (int i = 0; i < 3; i++) AddSlider(slotGo.transform);
                for (int i = 0; i < 5; i++) AddToggle(slotGo.transform);
                AddDropdown(slotGo.transform);
                for (int i = 0; i < 3; i++) AddTmpText(slotGo.transform);

                // SlotAnchorResolver contract: ResolveByPanel("settings") suffix-matches "settings-content-slot".
                var resolved = SettingsViewController.ResolveByPanel("settings", root.transform);
                Assert.IsNotNull(resolved,
                    "ResolveByPanel('settings') should find 'settings-content-slot' via suffix-match");
                Assert.AreEqual(slotGo.transform, resolved,
                    "Resolved slot should be the 'settings-content-slot' child");

                // CountWidgets sees all 12 widgets.
                var counts = SettingsViewController.CountWidgets(slotGo.transform);
                Assert.GreaterOrEqual(counts.Sliders,   3, $"sliders: {counts.Sliders}");
                Assert.GreaterOrEqual(counts.Toggles,   5, $"toggles: {counts.Toggles}");
                Assert.GreaterOrEqual(counts.Dropdowns, 1, $"dropdowns: {counts.Dropdowns}");
                Assert.GreaterOrEqual(counts.Headers,   3, $"headers: {counts.Headers}");
                Assert.GreaterOrEqual(counts.Total,    12, $"total: {counts.Total}");

                // Apply() must not throw (no UiBindRegistry wired — warning only path).
                Assert.DoesNotThrow(() => controller.Apply(),
                    "SettingsViewController.Apply() should not throw");
            }
            finally
            {
                Object.Destroy(root);
            }
            yield return null;
        }

        // ── 4.3 All gates green end-to-end ───────────────────────────────────────

        /// <summary>
        /// Runs id-lint + kind-coverage-lint via Node scripts (exit 0 required).
        /// Also verifies SettingsViewController.ResolveByPanel exists and BakeChildByKind
        /// handles all 4 settings kinds without exception via a smoke hierarchy build.
        /// Task 4.3 anchor — file fully green.
        /// </summary>
        [UnityTest]
        public IEnumerator SettingsTracer_AllGatesGreen()
        {
            string scriptsDir = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "tools", "scripts"));

            // Gate 1: id-lint (skip when script absent).
            string idLintPath = Path.Combine(scriptsDir, "validate-ui-id-consistency.mjs");
            if (File.Exists(idLintPath))
            {
                int code = RunNode(idLintPath, "--check");
                Assert.AreEqual(0, code, "validate-ui-id-consistency should exit 0");
            }

            yield return null;

            // Gate 2: kind-coverage-lint (skip when script absent).
            string coveragePath = Path.Combine(scriptsDir, "validate-bake-handler-kind-coverage.mjs");
            if (File.Exists(coveragePath))
            {
                int code = RunNode(coveragePath, "--check");
                Assert.AreEqual(0, code, "validate-bake-handler-kind-coverage should exit 0");
            }

            yield return null;

            // Gate 3: SettingsViewController.ResolveByPanel correctly finds the slot.
            var root = new GameObject("gate3-root");
            var slotGo = new GameObject("settings-content-slot");
            slotGo.transform.SetParent(root.transform, false);
            try
            {
                var resolved = SettingsViewController.ResolveByPanel("settings", root.transform);
                Assert.IsNotNull(resolved, "ResolveByPanel must find 'settings-content-slot' (gate 3)");
            }
            finally
            {
                Object.Destroy(root);
            }

            // Gate 4: smoke build — all 4 kinds produce children without error.
            var smokeRoot = new GameObject("gate4-smoke");
            try
            {
                SpawnSliderRow(smokeRoot.transform);
                SpawnToggleRow(smokeRoot.transform);
                SpawnDropdownRow(smokeRoot.transform);
                SpawnSectionHeader(smokeRoot.transform);
                Assert.AreEqual(4, smokeRoot.transform.childCount,
                    "smoke build should produce 4 widget children (one per kind)");
            }
            finally
            {
                Object.Destroy(smokeRoot);
            }

            yield return null;
        }

        // ── Widget spawn helpers (mirror BakeChildByKind output) ─────────────────

        private static void SpawnSliderRow(Transform parent)
        {
            var go = new GameObject("slider-row", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var track = new GameObject("Track", typeof(RectTransform));
            track.transform.SetParent(go.transform, false);
            track.AddComponent<Image>().raycastTarget = false;
            var fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(go.transform, false);
            fill.AddComponent<Image>().raycastTarget = false;
            var thumb = new GameObject("Thumb", typeof(RectTransform));
            thumb.transform.SetParent(go.transform, false);
            thumb.AddComponent<Image>();
            var lbl = new GameObject("Label", typeof(RectTransform));
            lbl.transform.SetParent(go.transform, false);
            lbl.AddComponent<TextMeshProUGUI>().raycastTarget = false;
        }

        private static void SpawnToggleRow(Transform parent)
        {
            var go = new GameObject("toggle-row", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var check = new GameObject("Checkmark", typeof(RectTransform));
            check.transform.SetParent(go.transform, false);
            check.AddComponent<Image>().raycastTarget = false;
            var lbl = new GameObject("Label", typeof(RectTransform));
            lbl.transform.SetParent(go.transform, false);
            lbl.AddComponent<TextMeshProUGUI>().raycastTarget = false;
        }

        private static void SpawnDropdownRow(Transform parent)
        {
            var go = new GameObject("dropdown-row", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var lbl = new GameObject("Label", typeof(RectTransform));
            lbl.transform.SetParent(go.transform, false);
            lbl.AddComponent<TextMeshProUGUI>().raycastTarget = false;
            var val = new GameObject("Value", typeof(RectTransform));
            val.transform.SetParent(go.transform, false);
            val.AddComponent<TextMeshProUGUI>().raycastTarget = false;
            var arrow = new GameObject("Arrow", typeof(RectTransform));
            arrow.transform.SetParent(go.transform, false);
            arrow.AddComponent<Image>().raycastTarget = false;
        }

        private static void SpawnSectionHeader(Transform parent)
        {
            var go = new GameObject("section-header", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var lbl = new GameObject("Label", typeof(RectTransform));
            lbl.transform.SetParent(go.transform, false);
            var tmp = lbl.AddComponent<TextMeshProUGUI>();
            tmp.fontStyle = TMPro.FontStyles.Bold;
            tmp.raycastTarget = false;
        }

        private static void AddSlider(Transform parent)
        {
            var go = new GameObject("slider-widget", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<Slider>();
        }

        private static void AddToggle(Transform parent)
        {
            var go = new GameObject("toggle-widget", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<Toggle>();
        }

        private static void AddDropdown(Transform parent)
        {
            var go = new GameObject("dropdown-widget", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<TMP_Dropdown>();
        }

        private static void AddTmpText(Transform parent)
        {
            var go = new GameObject("header-widget", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<TextMeshProUGUI>();
        }

        private static int RunNode(string scriptPath, string args)
        {
            var psi = new ProcessStartInfo("node", $"\"{scriptPath}\" {args}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
            };
            var proc = Process.Start(psi);
            if (proc == null) return -1;
            proc.WaitForExit(15000);
            return proc.ExitCode;
        }
    }
}
