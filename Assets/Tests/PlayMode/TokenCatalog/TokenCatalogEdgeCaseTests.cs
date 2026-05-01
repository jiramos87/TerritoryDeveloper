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
    /// Edge-case PlayMode tests for <see cref="Territory.UI.TokenCatalog"/>
    /// (TECH-8610 / asset-pipeline Stage 19.1).
    /// <para>
    /// Sibling cluster of <see cref="TokenCatalogRoundtripTests"/> covering
    /// gap areas: spacing-token edges (negative / zero / fractional px),
    /// motion <c>cubic_bezier</c> array path, type-scale <c>line_height</c>
    /// resolution against <c>size_px</c>, and retired-token absence after
    /// <c>RebuildIndexes</c> with the row removed.
    /// </para>
    /// </summary>
    public sealed class TokenCatalogEdgeCaseTests
    {
        private const string FixtureRelativePath =
            "Assets/Tests/PlayMode/TokenCatalog/Fixtures/token-catalog-edge-fixture.json";

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
                $"Edge fixture parse failed: {err}");

            _host = new GameObject("TokenCatalogEdgeTestHost");
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
        public void SpacingTokens_NegativeZeroFractional_RoundTrip()
        {
            Assert.IsTrue(_catalog.TryGetSpacing("spacing-neg", out var neg), "spacing-neg not indexed");
            Assert.AreEqual(-4.0f, neg.px, "negative spacing px round-trip mismatch");

            Assert.IsTrue(_catalog.TryGetSpacing("spacing-zero", out var zero), "spacing-zero not indexed");
            Assert.AreEqual(0.0f, zero.px, "zero spacing px round-trip mismatch");

            Assert.IsTrue(_catalog.TryGetSpacing("spacing-frac", out var frac), "spacing-frac not indexed");
            Assert.AreEqual(1.5f, frac.px, "fractional spacing px round-trip mismatch");
        }

        [Test]
        public void MotionToken_CubicBezierArray_PreservesAllFourControlPoints()
        {
            Assert.IsTrue(_catalog.TryGetMotion("motion-bezier", out var motion), "motion-bezier not indexed");
            Assert.AreEqual("cubic-bezier", motion.curve);
            Assert.AreEqual(250.0f, motion.duration_ms);
            Assert.IsNotNull(motion.cubic_bezier, "cubic_bezier array should not be null");
            Assert.AreEqual(4, motion.cubic_bezier.Length, "cubic_bezier must carry 4 control points");
            Assert.AreEqual(0.25f, motion.cubic_bezier[0]);
            Assert.AreEqual(0.1f, motion.cubic_bezier[1]);
            Assert.AreEqual(0.25f, motion.cubic_bezier[2]);
            Assert.AreEqual(1.0f, motion.cubic_bezier[3]);
        }

        [Test]
        public void TypeScale_LineHeightResolves_AgainstSizePx()
        {
            Assert.IsTrue(_catalog.TryGetTypeScale("type-display", out var ts), "type-display not indexed");
            Assert.AreEqual(32.0f, ts.size_px, "size_px round-trip mismatch");
            Assert.AreEqual(1.2f, ts.line_height, "line_height round-trip mismatch");
            float resolvedPx = ts.size_px * ts.line_height;
            Assert.AreEqual(38.4f, resolvedPx, 0.001f, "line_height × size_px resolution drift");
        }

        [Test]
        public void RetiredToken_AbsentAfterRebuild_TryGetReturnsFalse()
        {
            Assert.IsTrue(_catalog.TryGetSpacing("spacing-retired", out _), "pre-condition: spacing-retired indexed");

            // Rebuild with the retired row dropped — simulates a publish that retired the token.
            var trimmed = new TokenCatalogSnapshotDto
            {
                schemaVersion = _snapshot.schemaVersion,
                generatedAt = _snapshot.generatedAt,
                tokens = System.Array.FindAll(_snapshot.tokens, r => r != null && r.slug != "spacing-retired")
            };
            _catalog.RebuildIndexes(trimmed);

            Assert.IsFalse(_catalog.TryGetSpacing("spacing-retired", out _),
                "retired spacing slug should be absent after rebuild");
            Assert.IsTrue(_catalog.TryGetSpacing("spacing-neg", out _),
                "non-retired spacing-neg should remain indexed after rebuild");
        }
    }
}
