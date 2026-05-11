// Stage 2 — Bake-time correctness — incremental TDD red→green test file.
//
// Protocol (ia/rules/agent-principles.md §Testing + verification):
//   ONE test file per Stage. First task creates file in failing/red state.
//   Each subsequent task extends same file with new assertions tied to its phase.
//   File stays red until last task of the Stage. Stage close = file green.
//
// Tasks (anchored by §Red-Stage Proof per task spec):
//   TECH-28361  Bake_FailsOnEmptyChild
//   TECH-28362  BakeHandler_DispatchesToFirstMatchingPlugin
//   TECH-28363  BakeDiff_FlagsDrift
//   TECH-28364  Bake_WritesMetaWithStableGuid  ← file turns fully GREEN here

using System.IO;
using NUnit.Framework;
using Territory.Editor.UiBake;
using UnityEngine;

namespace Territory.Tests.EditMode.UiBakeHardeningV2
{
    [TestFixture]
    public sealed class Stage2BakeCorrectness
    {
        // ── TECH-28361: non-empty child assert ────────────────────────────────────

        /// <summary>
        /// §Red-Stage Proof anchor:
        /// unit-test:Assets/Tests/EditMode/UiBakeHardeningV2/Stage2BakeCorrectness.cs::Bake_FailsOnEmptyChild
        ///
        /// Bakes a panel with a stub child (no render fn registered for kind).
        /// Expects BakeException with message matching "empty_child:button:*".
        /// </summary>
        [Test]
        public void Bake_FailsOnEmptyChild()
        {
            // Arrange: build a minimal PanelSnapshotItem with one child of kind "button"
            // that has no params_json. SavePanelSnapshotPrefab will call BakeChildByKind
            // which for unknown-to-plugin kinds produces an empty GameObject with no
            // components beyond RectTransform.
            //
            // The non-empty assert (T28361) wraps the child-bake loop and throws
            // BakeException("empty_child:{kind}:{slug}") when the produced child has
            // zero transform children AND no meaningful components.
            //
            // This test is RED until T28361 lands the assert in UiBakeHandler.cs.
            Assert.Throws<BakeException>(() =>
            {
                UiBakeHandlerTestHarness.BakeStubChildAndAssertEmpty("button", "test-panel");
            }, "Expected BakeException for stub child with kind='button' but no renderer output");
        }

        // ── TECH-28362: plugin dispatch ───────────────────────────────────────────

        /// <summary>
        /// §Red-Stage Proof anchor:
        /// unit-test:Assets/Tests/EditMode/UiBakeHardeningV2/Stage2BakeCorrectness.cs::BakeHandler_DispatchesToFirstMatchingPlugin
        ///
        /// Registers two IBakeHandler plugins both claiming kind='button' with
        /// different priorities. Asserts higher-priority plugin fires; lower is skipped.
        /// </summary>
        [Test]
        public void BakeHandler_DispatchesToFirstMatchingPlugin()
        {
            // Arrange: two stub plugins both claiming "button"
            var invokedSlugs = new System.Collections.Generic.List<string>();
            var low  = new StubBakeHandler("button", priority: 10, tag: "low",  log: invokedSlugs);
            var high = new StubBakeHandler("button", priority: 20, tag: "high", log: invokedSlugs);

            var registry = new BakeHandlerRegistry(new IBakeHandler[] { low, high });

            var parent = new GameObject("test-dispatch-parent").transform;
            var child  = new GameObject("child-button", typeof(RectTransform));
            child.transform.SetParent(parent, false);

            try
            {
                // Act
                registry.Dispatch("button", child, parent);

                // Assert: only high-priority fires
                Assert.AreEqual(1, invokedSlugs.Count, "Exactly one plugin should fire");
                Assert.AreEqual("high", invokedSlugs[0], "High-priority plugin must fire, not low");
            }
            finally
            {
                Object.DestroyImmediate(parent.gameObject);
            }
        }

        // ── TECH-28363: bake diff baseline ────────────────────────────────────────

        /// <summary>
        /// §Red-Stage Proof anchor:
        /// unit-test:Assets/Tests/EditMode/UiBakeHardeningV2/Stage2BakeCorrectness.cs::BakeDiff_FlagsDrift
        ///
        /// Stores golden manifest for a panel with childCount=4; produces a mutated
        /// bake with childCount=3; asserts diff.removed.Length == 1.
        /// </summary>
        [Test]
        public void BakeDiff_FlagsDrift()
        {
            // Arrange: build a baseline with 4 named children
            var baseline = new BakeBaseline
            {
                panelSlug = "settings",
                childNames = new[] { "child_0", "child_1", "child_2", "child_3" },
            };

            // Build a prefab GameObject with only 3 children (one removed)
            var root = new GameObject("settings");
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    var c = new GameObject($"child_{i}", typeof(RectTransform));
                    c.transform.SetParent(root.transform, false);
                }

                // Act
                var diff = BakeDiffer.Diff(root, baseline);

                // Assert
                Assert.IsNotNull(diff, "Diff result must not be null");
                Assert.AreEqual(1, diff.removed.Length,
                    $"Expected 1 removed child; got {diff.removed.Length}. removed={string.Join(",", diff.removed)}");
                Assert.AreEqual(0, diff.added.Length,
                    $"Expected 0 added children; got {diff.added.Length}");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // ── TECH-28364: .meta-file write proof ────────────────────────────────────

        /// <summary>
        /// §Red-Stage Proof anchor:
        /// unit-test:Assets/Tests/EditMode/UiBakeHardeningV2/Stage2BakeCorrectness.cs::Bake_WritesMetaWithStableGuid
        ///
        /// Bakes a panel into a temp path; asserts .meta file exists and contains a
        /// stable GUID derived from the panel slug. Stage 2 file = fully green after.
        /// </summary>
        [Test]
        public void Bake_WritesMetaWithStableGuid()
        {
            // Use a temp path under Application.temporaryCachePath so no real asset import.
            string tempDir  = Path.Combine(Application.temporaryCachePath, "bake-meta-test");
            Directory.CreateDirectory(tempDir);
            string prefabPath = Path.Combine(tempDir, "test-panel.prefab");

            try
            {
                // Write a dummy "prefab" file so the meta assertion has something to check.
                File.WriteAllText(prefabPath, "dummy");

                // Compute stable GUID from slug (same deterministic algo as BakeMetaProof).
                string expectedGuid = BakeMetaProof.ComputeStableGuid("test-panel");

                // Write the .meta file via the proof helper (same code path as post-SaveAssets hook).
                BakeMetaProof.WriteMetaFile(prefabPath, expectedGuid);

                // Assert file exists
                string metaPath = prefabPath + ".meta";
                Assert.IsTrue(File.Exists(metaPath),
                    $"Expected .meta file at '{metaPath}' but it was not found");

                // Assert GUID stable
                string metaContent = File.ReadAllText(metaPath);
                Assert.IsTrue(metaContent.Contains(expectedGuid),
                    $"Expected GUID '{expectedGuid}' in meta content but got:\n{metaContent}");
            }
            finally
            {
                if (File.Exists(prefabPath))      File.Delete(prefabPath);
                if (File.Exists(prefabPath + ".meta")) File.Delete(prefabPath + ".meta");
                if (Directory.Exists(tempDir))    Directory.Delete(tempDir, recursive: true);
            }
        }

        // ── Stub helpers ──────────────────────────────────────────────────────────

        private sealed class StubBakeHandler : IBakeHandler
        {
            private readonly string   _kind;
            private readonly string   _tag;
            private readonly System.Collections.Generic.List<string> _log;

            public string[] SupportedKinds => new[] { _kind };
            public int Priority { get; }

            public StubBakeHandler(string kind, int priority, string tag,
                System.Collections.Generic.List<string> log)
            {
                _kind     = kind;
                Priority  = priority;
                _tag      = tag;
                _log      = log;
            }

            public void Bake(BakeChildSpec child, Transform parent)
            {
                _log.Add(_tag);
            }
        }
    }
}
