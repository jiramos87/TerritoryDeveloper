using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using Territory.Catalog;
using UnityEngine;

namespace Territory.Tests.EditMode.CatalogLoader
{
    /// <summary>
    /// Edit Mode coverage for <see cref="Territory.Catalog.CatalogLoader"/>.
    /// Static <c>TryBuildEntities</c> + <c>ComputeKindOrderedHashHex</c> are the
    /// pure entry points; the MonoBehaviour reload swap is covered by mutating a
    /// temp directory and calling <c>TryBuildEntities</c> twice (old reference
    /// must remain untouched per Unity invariant 9 immutable-replace contract).
    /// </summary>
    public class CatalogLoaderTests
    {
        private static readonly string[] KindOrder =
        {
            "sprite",
            "asset",
            "button",
            "panel",
            "audio",
            "pool",
            "token",
            "archetype",
        };

        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(
                Path.GetTempPath(),
                "catalog-loader-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }

        [Test]
        public void TryBuildEntities_LoadsAllKinds_StampsKind_AndKeysByEntityId()
        {
            WriteValidSnapshot(
                _tempDir,
                spriteRows: new[] { ("sprite-1", "sprite-one") },
                assetRows: new[] { ("asset-1", "asset-one") });

            string err;
            IReadOnlyDictionary<string, CatalogEntity> dict;
            bool ok = Territory.Catalog.CatalogLoader.TryBuildEntities(_tempDir, out dict, out err);

            Assert.IsTrue(ok, "TryBuildEntities should succeed; err=" + err);
            Assert.IsNotNull(dict);
            Assert.AreEqual(2, dict.Count);
            Assert.IsTrue(dict.ContainsKey("sprite-1"));
            Assert.AreEqual("sprite", dict["sprite-1"].Kind);
            Assert.AreEqual("sprite-one", dict["sprite-1"].slug);
            Assert.AreEqual("asset", dict["asset-1"].Kind);
        }

        [Test]
        public void TryBuildEntities_ParityFails_WhenManifestHashMismatches()
        {
            WriteValidSnapshot(_tempDir, spriteRows: new[] { ("sprite-1", "sprite-one") });

            // Corrupt the manifest hash without touching per-kind files.
            string manifestPath = Path.Combine(_tempDir, "manifest.json");
            string text = File.ReadAllText(manifestPath);
            string corrupted = text.Replace("\"snapshotHash\":\"", "\"snapshotHash\":\"deadbeef");
            File.WriteAllText(manifestPath, corrupted);

            string err;
            IReadOnlyDictionary<string, CatalogEntity> dict;
            bool ok = Territory.Catalog.CatalogLoader.TryBuildEntities(_tempDir, out dict, out err);

            Assert.IsFalse(ok, "Hash parity must fail when manifest is corrupted.");
            Assert.IsNull(dict);
            StringAssert.Contains("Hash parity failed", err);
        }

        [Test]
        public void TryBuildEntities_TwoCalls_ReturnDistinctImmutableSnapshots()
        {
            WriteValidSnapshot(_tempDir, spriteRows: new[] { ("sprite-1", "v1") });

            string err;
            IReadOnlyDictionary<string, CatalogEntity> first;
            Assert.IsTrue(
                Territory.Catalog.CatalogLoader.TryBuildEntities(_tempDir, out first, out err), err);
            int firstCount = first.Count;

            // Mutate snapshot on disk + rebuild — first reference must NOT change.
            WriteValidSnapshot(_tempDir, spriteRows: new[] { ("sprite-1", "v2"), ("sprite-2", "v2b") });

            IReadOnlyDictionary<string, CatalogEntity> second;
            Assert.IsTrue(
                Territory.Catalog.CatalogLoader.TryBuildEntities(_tempDir, out second, out err), err);

            Assert.AreNotSame(first, second, "Each rebuild must yield a fresh dictionary.");
            Assert.AreEqual(firstCount, first.Count, "Old snapshot must remain unchanged after rebuild.");
            Assert.AreEqual("v1", first["sprite-1"].slug, "Old snapshot row must be untouched.");
            Assert.AreEqual("v2", second["sprite-1"].slug);
            Assert.AreEqual(2, second.Count);
        }

        [Test]
        public void TryBuildEntities_DuplicateEntityIdAcrossKinds_Fails()
        {
            WriteValidSnapshot(
                _tempDir,
                spriteRows: new[] { ("dup-id", "sprite") },
                assetRows: new[] { ("dup-id", "asset") });

            string err;
            IReadOnlyDictionary<string, CatalogEntity> dict;
            bool ok = Territory.Catalog.CatalogLoader.TryBuildEntities(_tempDir, out dict, out err);

            Assert.IsFalse(ok);
            StringAssert.Contains("Duplicate entity_id", err);
        }

        [Test]
        public void ComputeKindOrderedHashHex_DeterministicAndOrderSensitive()
        {
            var bytesA = new Dictionary<string, byte[]>();
            var bytesB = new Dictionary<string, byte[]>();
            foreach (var kind in KindOrder)
            {
                bytesA[kind] = Encoding.UTF8.GetBytes("{\"kind\":\"" + kind + "\",\"rows\":[]}");
                bytesB[kind] = Encoding.UTF8.GetBytes("{\"kind\":\"" + kind + "\",\"rows\":[]}");
            }

            string h1 = Territory.Catalog.CatalogLoader.ComputeKindOrderedHashHex(bytesA);
            string h2 = Territory.Catalog.CatalogLoader.ComputeKindOrderedHashHex(bytesB);
            Assert.AreEqual(h1, h2, "Hash must be deterministic over identical bytes.");
            Assert.AreEqual(64, h1.Length, "sha256 hex string must be 64 chars.");

            // Mutate one kind's bytes — hash must change.
            bytesB["sprite"] = Encoding.UTF8.GetBytes("{\"kind\":\"sprite\",\"rows\":[1]}");
            string h3 = Territory.Catalog.CatalogLoader.ComputeKindOrderedHashHex(bytesB);
            Assert.AreNotEqual(h1, h3);
        }

        // -----------------------------------------------------------------
        // Test helpers
        // -----------------------------------------------------------------

        /// <summary>
        /// Build a valid 8-kind snapshot on disk: empty per-kind files for kinds
        /// not supplied, single-row files for the kinds the caller passed.
        /// Hash is computed live so manifest stays in parity with the bytes.
        /// </summary>
        private static void WriteValidSnapshot(
            string dir,
            (string entityId, string slug)[] spriteRows = null,
            (string entityId, string slug)[] assetRows = null,
            (string entityId, string slug)[] buttonRows = null,
            (string entityId, string slug)[] panelRows = null,
            (string entityId, string slug)[] audioRows = null,
            (string entityId, string slug)[] poolRows = null,
            (string entityId, string slug)[] tokenRows = null,
            (string entityId, string slug)[] archetypeRows = null)
        {
            var rowsByKind = new Dictionary<string, (string, string)[]>
            {
                { "sprite", spriteRows ?? Array.Empty<(string, string)>() },
                { "asset", assetRows ?? Array.Empty<(string, string)>() },
                { "button", buttonRows ?? Array.Empty<(string, string)>() },
                { "panel", panelRows ?? Array.Empty<(string, string)>() },
                { "audio", audioRows ?? Array.Empty<(string, string)>() },
                { "pool", poolRows ?? Array.Empty<(string, string)>() },
                { "token", tokenRows ?? Array.Empty<(string, string)>() },
                { "archetype", archetypeRows ?? Array.Empty<(string, string)>() },
            };

            var perKindBytes = new Dictionary<string, byte[]>();
            int[] counts = new int[8];
            int idx = 0;
            foreach (var kind in KindOrder)
            {
                var rows = rowsByKind[kind];
                counts[idx++] = rows.Length;
                string json = BuildPerKindJson(kind, rows);
                byte[] buf = Encoding.UTF8.GetBytes(json);
                perKindBytes[kind] = buf;
                File.WriteAllBytes(Path.Combine(dir, kind + ".json"), buf);
            }

            string hash = Territory.Catalog.CatalogLoader.ComputeKindOrderedHashHex(perKindBytes);
            string manifestJson = BuildManifestJson(hash, counts);
            File.WriteAllText(Path.Combine(dir, "manifest.json"), manifestJson);
        }

        private static string BuildPerKindJson(string kind, (string entityId, string slug)[] rows)
        {
            var sb = new StringBuilder();
            sb.Append("{\"kind\":\"").Append(kind).Append("\",\"generatedAt\":\"2026-01-01T00:00:00Z\",\"rows\":[");
            for (int i = 0; i < rows.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append("{\"entity_id\":\"").Append(rows[i].entityId)
                  .Append("\",\"slug\":\"").Append(rows[i].slug)
                  .Append("\",\"display_name\":\"").Append(rows[i].slug)
                  .Append("\",\"tags\":[],\"version_id\":\"v-1\",\"version_number\":1,\"status\":\"published\"}");
            }
            sb.Append("]}");
            return sb.ToString();
        }

        private static string BuildManifestJson(string hash, int[] counts)
        {
            // counts ordered by KindOrder: sprite,asset,button,panel,audio,pool,token,archetype
            return "{\"schemaVersion\":1,\"generatedAt\":\"2026-01-01T00:00:00Z\","
                + "\"snapshotHash\":\"" + hash + "\","
                + "\"entityCounts\":{"
                + "\"sprite\":" + counts[0]
                + ",\"asset\":" + counts[1]
                + ",\"button\":" + counts[2]
                + ",\"panel\":" + counts[3]
                + ",\"audio\":" + counts[4]
                + ",\"pool\":" + counts[5]
                + ",\"token\":" + counts[6]
                + ",\"archetype\":" + counts[7]
                + "}}";
        }
    }
}
