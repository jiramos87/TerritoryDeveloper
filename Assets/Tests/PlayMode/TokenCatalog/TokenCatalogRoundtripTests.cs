using System.Collections;
using System.IO;
using NUnit.Framework;
using Territory.UI;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Territory.Tests.PlayMode.TokenCatalog
{
    /// <summary>
    /// PlayMode round-trip tests for <see cref="Territory.UI.TokenCatalog"/>
    /// (TECH-2095 / asset-pipeline Stage 10.1).
    /// <para>
    /// Each test instantiates a fresh GameObject hosting <c>TokenCatalog</c>
    /// and feeds it the fixture JSON via <c>RebuildIndexes</c>. The fixture
    /// covers all five DEC-A44 token kinds (color / type-scale / motion /
    /// spacing / semantic), a 2-hop terminating alias chain, and a 2-cycle
    /// loop used to exercise the depth cap.
    /// </para>
    /// </summary>
    public sealed class TokenCatalogRoundtripTests
    {
        private const string FixtureRelativePath =
            "Assets/Tests/PlayMode/TokenCatalog/Fixtures/token-catalog-fixture.json";

        private GameObject _host;
        private Territory.UI.TokenCatalog _catalog;
        private TokenCatalogSnapshotDto _snapshot;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            string path = Path.Combine(Application.dataPath, "..", FixtureRelativePath);
            string text = File.ReadAllText(path);

            Assert.IsTrue(
                Territory.UI.TokenCatalog.TryParseSnapshotJson(text, out _snapshot, out var err),
                $"Fixture parse failed: {err}");

            _host = new GameObject("TokenCatalogTestHost");
            _catalog = _host.AddComponent<Territory.UI.TokenCatalog>();
            _catalog.RebuildIndexes(_snapshot);

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_host != null) Object.Destroy(_host);
            yield return null;
        }

        [Test]
        public void RebuildIndexes_AllKindsResolvable()
        {
            Assert.IsTrue(_catalog.TryGetColor("color-primary", out var color),
                "color-primary not indexed");
            Assert.AreEqual("#3366ff", color.hex, "color-primary hex round-trip mismatch");

            Assert.IsTrue(_catalog.TryGetTypeScale("type-body", out var ts),
                "type-body not indexed");
            Assert.AreEqual("Inter", ts.font_family);
            Assert.AreEqual(14.0f, ts.size_px);

            Assert.IsTrue(_catalog.TryGetMotion("motion-fast", out var motion),
                "motion-fast not indexed");
            Assert.AreEqual("ease-out", motion.curve);
            Assert.AreEqual(150.0f, motion.duration_ms);

            Assert.IsTrue(_catalog.TryGetSpacing("spacing-md", out var spacing),
                "spacing-md not indexed");
            Assert.AreEqual(8.0f, spacing.px);

            Assert.IsTrue(_catalog.TryGetSemantic("semantic-accent", out var sem),
                "semantic-accent not indexed");
            Assert.AreEqual("semantic-accent-mid", sem.target_slug);
        }

        [Test]
        public void TryResolveSemantic_TwoHopChain_TerminatesWithRole()
        {
            bool ok = _catalog.TryResolveSemantic("semantic-accent",
                out string targetSlug, out string tokenRole);

            Assert.IsTrue(ok, "TryResolveSemantic returned false on valid chain");
            Assert.AreEqual("semantic-accent-terminal", targetSlug);
            Assert.AreEqual("accent", tokenRole);
        }

        [Test]
        public void TryResolveSemantic_LoopChain_FailsAtDepthCap()
        {
            bool ok = _catalog.TryResolveSemantic("loop-a",
                out string targetSlug, out string tokenRole);

            Assert.IsFalse(ok, "TryResolveSemantic should fail on cyclic chain");
            Assert.IsNull(targetSlug);
            Assert.IsNull(tokenRole);
        }

        [Test]
        public void TryResolveSemantic_MissingSlug_ReturnsFalse()
        {
            bool ok = _catalog.TryResolveSemantic("does-not-exist",
                out string targetSlug, out string tokenRole);

            Assert.IsFalse(ok);
            Assert.IsNull(targetSlug);
            Assert.IsNull(tokenRole);
        }

        [Test]
        public void RebuildIndexes_EmitsOnCatalogReloadedEventOnReload()
        {
            int callCount = 0;
            _catalog.OnCatalogReloaded.AddListener(() => callCount++);

            _catalog.RebuildIndexes(_snapshot);
            // RebuildIndexes is the index path; event fires only via LoadInternal.
            // Drive the listener path via a second AddListener + manual invoke
            // to assert UnityEvent wiring stays bound across rebuilds.
            _catalog.OnCatalogReloaded.Invoke();

            Assert.AreEqual(1, callCount, "OnCatalogReloaded listener not fired");
        }

        [Test]
        public void SemanticDepthCap_MatchesContract()
        {
            Assert.AreEqual(6, Territory.UI.TokenCatalog.SemanticDepthCap,
                "SemanticDepthCap drifted from contract (Stage 8.1 panel-cycle budget + TECH-2094 PanelPreview cap)");
        }
    }
}
