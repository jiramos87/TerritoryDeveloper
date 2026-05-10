// Stage 2 — Design translation (DB→Unity) — incremental TDD red→green test file.
//
// Protocol (ia/rules/agent-principles.md §Testing + verification):
//   ONE test file per Stage. First task creates file in failing/red state.
//   Each subsequent task extends same file with new assertions tied to its phase.
//   File stays red until last task of the Stage. Stage close = file green.
//   Master-plan close runs `unity:testmode-batch --filter BakePipeline.*`.
//
// Tasks (anchored by §Red-Stage Proof per task spec):
//   2.1  KindRendererMatrix_RendersAllRegisteredKinds
//   2.2a SlotAnchorResolver_ResolvesByPanel
//   2.2b RenderCheck_FailsOnMissingSlot
//   2.3  IdConsistencyLint_FailsOnDrift
//   2.4  KindCoverageLint_FailsOnUnmappedKind

using NUnit.Framework;
using Territory.Editor.UiBake;
using Territory.Editor.UiBake.SlotResolver;
using UnityEngine;
using System.Diagnostics;
using System.IO;

namespace Territory.Tests.EditMode.BakePipeline
{
    [TestFixture]
    public sealed class Stage2DesignTranslation
    {
        // ── 2.1 Kind-renderer matrix ──────────────────────────────────────────────

        [Test]
        public void KindRendererMatrix_RendersAllRegisteredKinds()
        {
            var kinds = new[]
            {
                "slider-row",
                "toggle-row",
                "dropdown-row",
                "section-header",
                "list-row",
            };

            var parent = new GameObject("test-parent").transform;
            try
            {
                foreach (var kind in kinds)
                {
                    Assert.IsTrue(KindRendererMatrix.IsRegistered(kind), $"kind '{kind}' not registered");
                    var go = KindRendererMatrix.Render(kind, null, parent);
                    Assert.IsNotNull(go, $"Render returned null for kind '{kind}'");
                    Assert.Greater(go.transform.childCount, 0,
                        $"kind '{kind}' produced no child render targets");
                }
            }
            finally
            {
                Object.DestroyImmediate(parent.gameObject);
            }
        }

        [Test]
        public void KindRendererMatrix_UnknownKind_ThrowsBakeException()
        {
            var parent = new GameObject("test-parent").transform;
            try
            {
                Assert.Throws<BakeException>(() =>
                    KindRendererMatrix.Render("not-a-real-kind", null, parent));
            }
            finally
            {
                Object.DestroyImmediate(parent.gameObject);
            }
        }

        // ── 2.2 SlotAnchorResolver ────────────────────────────────────────────────

        [Test]
        public void SlotAnchorResolver_ResolvesByPanel()
        {
            // Build a canvas hierarchy: root → main-menu-content-slot child.
            var root = new GameObject("root");
            var slotGo = new GameObject("main-menu-content-slot");
            slotGo.transform.SetParent(root.transform, false);
            try
            {
                // ResolveByPanel("main-menu") should suffix-match "main-menu-content-slot".
                var resolved = SlotAnchorResolver.ResolveByPanel("main-menu", root.transform);
                Assert.IsNotNull(resolved, "suffix-match fallback should find main-menu-content-slot");
                Assert.AreEqual(slotGo.transform, resolved);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RenderCheck_FailsOnMissingSlot()
        {
            var root = new GameObject("empty-root");
            try
            {
                // No slot child — resolver returns null, apply-time check should flag it.
                var resolved = SlotAnchorResolver.ResolveByPanel("missing-panel", root.transform);
                Assert.IsNull(resolved, "missing slot should return null");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // ── 2.3 validate:ui-id-consistency lint ──────────────────────────────────

        [Test]
        public void IdConsistencyLint_FailsOnDrift()
        {
            // Shell out to validate-ui-id-consistency.mjs with a drift fixture.
            // Exit code 1 = lint detected drift (expected).
            string scriptPath = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "tools", "scripts",
                    "validate-ui-id-consistency.mjs"));
            Assert.IsTrue(File.Exists(scriptPath),
                $"validate-ui-id-consistency.mjs not found at {scriptPath}");

            // Inject drift fixture via env var that the script honours.
            var psi = new ProcessStartInfo("node", $"\"{scriptPath}\" --fixture drift")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            psi.EnvironmentVariables["UI_ID_CONSISTENCY_FIXTURE"] = "drift";
            var proc = Process.Start(psi);
            proc.WaitForExit(10000);
            Assert.AreEqual(1, proc.ExitCode,
                "validate-ui-id-consistency should exit 1 on drift fixture");
        }

        // ── 2.4 validate:bake-handler-kind-coverage lint ─────────────────────────

        [Test]
        public void KindCoverageLint_FailsOnUnmappedKind()
        {
            // Shell out to validate-bake-handler-kind-coverage.mjs with an injected unknown kind.
            string scriptPath = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "tools", "scripts",
                    "validate-bake-handler-kind-coverage.mjs"));
            Assert.IsTrue(File.Exists(scriptPath),
                $"validate-bake-handler-kind-coverage.mjs not found at {scriptPath}");

            var psi = new ProcessStartInfo("node", $"\"{scriptPath}\" --fixture unmapped-kind")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            psi.EnvironmentVariables["BAKE_COVERAGE_FIXTURE"] = "unmapped-kind";
            var proc = Process.Start(psi);
            proc.WaitForExit(10000);
            Assert.AreEqual(1, proc.ExitCode,
                "validate-bake-handler-kind-coverage should exit 1 on unmapped-kind fixture");
        }
    }
}
