using NUnit.Framework;
using UnityEngine;

namespace Territory.Tests.EditMode.GridAsset
{
    /// <summary>TECH-669 — <see cref="GridAssetCatalog"/> snapshot JSON parse (JsonUtility, TECH-663 envelope).</summary>
    public class GridAssetCatalogParseTests
    {
        private const string MinFixture =
            @"{
  ""assets"": [],
  ""bindings"": [],
  ""economy"": [],
  ""generatedAt"": ""2026-04-22T00:00:00.000Z"",
  ""importHygiene"": [],
  ""includeDrafts"": false,
  ""schemaVersion"": 1,
  ""sprites"": []
}";

        [Test]
        public void TryParseSnapshotJson_MinimalFixture_ReturnsTrue()
        {
            bool ok = GridAssetCatalog.TryParseSnapshotJson(MinFixture, out GridAssetSnapshotRoot root, out string err);
            Assert.IsTrue(ok, err);
            Assert.IsNotNull(root);
            Assert.AreEqual(1, root.schemaVersion);
            Assert.IsNotNull(root.assets);
            Assert.AreEqual(0, root.assets.Length);
        }

        [Test]
        public void TryParseSnapshotJson_InvalidBrace_ReturnsFalse()
        {
            bool ok = GridAssetCatalog.TryParseSnapshotJson("{", out _, out string err);
            Assert.IsFalse(ok);
            Assert.IsFalse(string.IsNullOrEmpty(err));
        }

        [Test]
        public void TryParseSnapshotJson_EmptyString_ReturnsFalse()
        {
            bool ok = GridAssetCatalog.TryParseSnapshotJson("", out _, out _);
            Assert.IsFalse(ok);
        }
    }
}
