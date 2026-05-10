// Stage 5 — Audit fix-up (C1–C4 + H1) — EditMode red→green test file.
//
// Protocol (ia/rules/agent-principles.md §Testing + verification):
//   ONE test file per Stage. First task creates file in failing/red state.
//   Each subsequent task extends same file with new assertions tied to its phase.
//   File stays red until last task of the Stage. Stage close = file green.
//   Master-plan close runs `unity:testmode-batch --filter BakePipeline.*`.
//
// Tasks (anchored by §Red-Stage Proof per task spec):
//   5.2 (TECH-27541) SlotAnchorResolver_ConsumedFromRuntimeAsmdef
//   5.3 (TECH-27542) BakeChildByKind_ProducesRealSliderToggleDropdown

using NUnit.Framework;
using Territory.UI;
using Territory.Editor.UiBake;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

namespace Territory.Tests.EditMode.BakePipeline
{
    [TestFixture]
    public sealed class Stage5FixUp
    {
        // ── 5.2 (TECH-27541): SlotAnchorResolver_ConsumedFromRuntimeAsmdef ──────

        [Test]
        public void SlotAnchorResolver_ConsumedFromRuntimeAsmdef()
        {
            // 1. Verify type lives in Territory.UI namespace.
            var resolverType = typeof(SlotAnchorResolver);
            Assert.AreEqual("Territory.UI", resolverType.Namespace,
                "SlotAnchorResolver must be in Territory.UI namespace (runtime)");

            // 2. Verify SettingsViewController no longer declares its own WalkForSlot method.
            var svcType = typeof(Territory.UI.Modals.SettingsViewController);
            var walkMethod = svcType.GetMethod(
                "WalkForSlot",
                BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNull(walkMethod,
                "SettingsViewController must NOT declare WalkForSlot — delegate to SlotAnchorResolver");

            // 3. Functional: ResolveByPanel still works from runtime namespace.
            var root = new GameObject("root");
            var slotGo = new GameObject("settings-content-slot");
            slotGo.transform.SetParent(root.transform, false);
            try
            {
                var resolved = SlotAnchorResolver.ResolveByPanel("settings", root.transform);
                Assert.IsNotNull(resolved, "ResolveByPanel must still find settings-content-slot after relocation");
                Assert.AreEqual(slotGo.transform, resolved);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // ── 5.3 (TECH-27542): BakeChildByKind_ProducesRealSliderToggleDropdown ──

        [Test]
        public void BakeChildByKind_ProducesRealSliderToggleDropdown()
        {
            // Build minimal parent hierarchy, call KindRendererMatrix.Render per kind,
            // then assert that the hierarchy carries real Slider, Toggle, TMP_Dropdown components.
            var parent = new GameObject("test-parent").transform;
            try
            {
                // slider-row
                var sliderGo = KindRendererMatrix.Render("slider-row", null, parent);
                Assert.IsNotNull(sliderGo, "Render returned null for slider-row");
                var slider = sliderGo.GetComponentInChildren<Slider>(true);
                Assert.IsNotNull(slider,
                    "slider-row must produce a real UnityEngine.UI.Slider component in hierarchy");

                // toggle-row
                var toggleGo = KindRendererMatrix.Render("toggle-row", null, parent);
                Assert.IsNotNull(toggleGo, "Render returned null for toggle-row");
                var toggle = toggleGo.GetComponentInChildren<Toggle>(true);
                Assert.IsNotNull(toggle,
                    "toggle-row must produce a real UnityEngine.UI.Toggle component in hierarchy");

                // dropdown-row
                var dropdownGo = KindRendererMatrix.Render("dropdown-row", null, parent);
                Assert.IsNotNull(dropdownGo, "Render returned null for dropdown-row");
                var dropdown = dropdownGo.GetComponentInChildren<TMPro.TMP_Dropdown>(true);
                Assert.IsNotNull(dropdown,
                    "dropdown-row must produce a real TMPro.TMP_Dropdown component in hierarchy");
            }
            finally
            {
                Object.DestroyImmediate(parent.gameObject);
            }
        }
    }
}
