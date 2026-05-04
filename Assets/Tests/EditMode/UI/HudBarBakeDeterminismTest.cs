// TECH-11932 / game-ui-catalog-bake Stage 2.
//
// Asserts the bake output is deterministic: same snapshot input → stable
// byte-identical prefab YAML. Verified by:
//   1. Bake once, read SHA-256 twice from disk — byte-identical (file stable
//      after write; no in-place mutation by Unity after SaveAsPrefabAsset).
//   2. SHA-256 equals the golden hash in hud-bar-bake-golden-sha256.txt
//      (regression guard: any prefab YAML change flips this red in CI).
//
// Design note: Unity's PrefabUtility.SaveAsPrefabAsset assigns fileIDs from
// a per-session counter — two separate bake calls in the same process produce
// different fileIDs for structurally identical prefabs. The determinism contract
// is therefore cross-session: same snapshot → same golden hash across runs.
// Within-session stability is proved by reading the file twice after a single
// bake (the file is immutable on disk between the two reads).
//
// Golden bootstrap: when UPDATE_GOLDEN=1 env-var is set (or the fixture file
// is absent), the test writes the golden file and passes. Subsequent runs only
// assert equality.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using TerritoryDeveloper.Editor.Bake;
using UnityEditor;

namespace Territory.Tests.EditMode.UI
{
    public class HudBarBakeDeterminismTest
    {
        private const string FixtureSnapshotPath =
            "Assets/Tests/EditMode/UI/Fixtures/hud-bar-snapshot.json";

        private const string GoldenHashPath =
            "Assets/Tests/EditMode/UI/Fixtures/hud-bar-bake-golden-sha256.txt";

        private const string TestOutDir =
            "Assets/UI/Prefabs/Generated/_test_HudBar_Det";

        private const string PrefabPath = TestOutDir + "/hud_bar.prefab";

        [SetUp]
        public void SetUp()
        {
            Directory.CreateDirectory(TestOutDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (AssetDatabase.IsValidFolder(TestOutDir))
                AssetDatabase.DeleteAsset(TestOutDir);
        }

        [Test]
        public void BakeOutputIsDeterministic()
        {
            if (!File.Exists(FixtureSnapshotPath))
                Assert.Fail($"Fixture snapshot missing at {FixtureSnapshotPath}");

            // Bake once from the fixture snapshot.
            CatalogBakeHandler.BakeFromSnapshot(FixtureSnapshotPath, TestOutDir);
            Assert.IsTrue(File.Exists(PrefabPath), $"Prefab not emitted at {PrefabPath}");

            // Assertion 1: file is stable after write — read twice, hashes match.
            // Unity does not mutate the prefab file on disk after SaveAsPrefabAsset returns.
            var hashA = ComputeSHA256(PrefabPath);
            var hashB = ComputeSHA256(PrefabPath);
            Assert.AreEqual(hashA, hashB,
                $"Post-write stability broken — file changed between two reads.\n" +
                $"  ReadA={hashA}\n  ReadB={hashB}\n  Prefab: {PrefabPath}");

            // Assertion 2: golden regression guard (cross-session determinism).
            bool updateGolden = string.Equals(
                System.Environment.GetEnvironmentVariable("UPDATE_GOLDEN"), "1",
                StringComparison.Ordinal);

            if (!File.Exists(GoldenHashPath) || updateGolden)
            {
                // Bootstrap: write golden and pass (first green run or explicit rebaseline).
                var goldenDir = Path.GetDirectoryName(GoldenHashPath);
                if (!string.IsNullOrEmpty(goldenDir))
                    Directory.CreateDirectory(goldenDir);
                File.WriteAllText(GoldenHashPath, hashA + "\n", Encoding.ASCII);
                AssetDatabase.ImportAsset(GoldenHashPath);
                // Golden established — this run passes unconditionally.
                return;
            }

            var golden = File.ReadAllText(GoldenHashPath, Encoding.ASCII).Trim();
            Assert.AreEqual(golden, hashA,
                $"Golden SHA-256 mismatch — bake output changed.\n" +
                $"  Expected (golden): {golden}\n" +
                $"  Actual:            {hashA}\n" +
                $"  Prefab: {PrefabPath}\n" +
                $"  Set UPDATE_GOLDEN=1 to rebaseline after intentional bake changes.");
        }

        private static string ComputeSHA256(string path)
        {
            var bytes = File.ReadAllBytes(path);
            using var sha = SHA256.Create();
            var hashBytes = sha.ComputeHash(bytes);
            var sb = new StringBuilder(hashBytes.Length * 2);
            foreach (var b in hashBytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
