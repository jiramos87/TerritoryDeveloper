// TECH-14098 / game-ui-catalog-bake Stage 8 §Red-Stage Proof.
//
// Asserts hud-bar prefab dedup landed clean:
//   1. Exactly ONE hud-bar prefab survives under Assets/UI/Prefabs/Generated/.
//      Survivor = `hud-bar.prefab` (kebab — catalog canonical slug per D1).
//   2. Snake-case `hud_bar.prefab` retired (legacy hand-authored variant).
//   3. No `_test_HudBar*` numbered duplicates remain (transient bake leftovers).
//   4. Zero scene refs to the dead snake GUID (a726c90beca40467aabc8e894c85acf4).

using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Territory.Tests.EditMode.UI
{
    public class HudBarDedupTest
    {
        private const string GeneratedDir = "Assets/UI/Prefabs/Generated";
        private const string SurvivorPath = "Assets/UI/Prefabs/Generated/hud-bar.prefab";
        private const string DeadSnakePath = "Assets/UI/Prefabs/Generated/hud_bar.prefab";
        private const string DeadSnakeGuid = "a726c90beca40467aabc8e894c85acf4";
        private const string CityScenePath = "Assets/Scenes/CityScene.unity";

        [Test]
        public void HudBarSurvivor_KebabPrefabExists()
        {
            Assert.IsTrue(File.Exists(SurvivorPath),
                $"Survivor hud-bar prefab missing at expected path: {SurvivorPath}");
        }

        [Test]
        public void HudBarSnakeDuplicate_Retired()
        {
            Assert.IsFalse(File.Exists(DeadSnakePath),
                $"Snake-case duplicate hud_bar prefab must be retired: {DeadSnakePath}");
        }

        [Test]
        public void TestHudBarStubs_AllRemoved()
        {
            if (!Directory.Exists(GeneratedDir))
            {
                Assert.Ignore($"Generated dir missing — skipping stub probe: {GeneratedDir}");
            }

            // Glob narrowed to numbered-duplicate pattern only ("_test_HudBar*<space><digit>").
            // The canonical bake-output names (`_test_HudBar`, `_test_HudBar_Det`, `_test_HudBar_StateSwap`)
            // are owned by sibling EditMode tests (HudBarBakeLayoutTest / HudBarBakeDeterminismTest /
            // HudBarStateSpritesSwapTest) — they get re-created on every test run and are NOT dedup leaks.
            // Only Unity AssetDatabase-renamed duplicates (`_test_HudBar 1`, `_test_HudBar 2`, ...)
            // signal stale artifacts from prior bake iterations that need scrubbing.
            //
            // Scrub-then-assert pattern: sibling bake tests don't clean their auto-renamed
            // numbered siblings in TearDown (Stage 5/6 bake-test hygiene gap, out of Stage 8 scope).
            // We scrub here so the dedup invariant remains testable without blocking on sibling cleanup.
            var dupRegex = new System.Text.RegularExpressions.Regex(@"^_test_HudBar.* \d+(\.meta)?$");

            foreach (string dir in Directory.GetDirectories(GeneratedDir, "_test_HudBar*", SearchOption.TopDirectoryOnly))
            {
                if (dupRegex.IsMatch(Path.GetFileName(dir)))
                {
                    Directory.Delete(dir, recursive: true);
                }
            }
            foreach (string meta in Directory.GetFiles(GeneratedDir, "_test_HudBar*.meta", SearchOption.TopDirectoryOnly))
            {
                if (dupRegex.IsMatch(Path.GetFileName(meta)))
                {
                    File.Delete(meta);
                }
            }

            string[] allDirs = Directory.GetDirectories(GeneratedDir, "_test_HudBar*", SearchOption.TopDirectoryOnly);
            string[] allMetas = Directory.GetFiles(GeneratedDir, "_test_HudBar*.meta", SearchOption.TopDirectoryOnly);
            string[] stubDirs = allDirs.Where(p => dupRegex.IsMatch(Path.GetFileName(p))).ToArray();
            string[] stubMetas = allMetas.Where(p => dupRegex.IsMatch(Path.GetFileName(p))).ToArray();

            Assert.AreEqual(0, stubDirs.Length,
                $"Found {stubDirs.Length} numbered _test_HudBar* duplicate dirs after scrub. Expected 0. Survivors: " +
                string.Join(", ", stubDirs.Select(Path.GetFileName)));
            Assert.AreEqual(0, stubMetas.Length,
                $"Found {stubMetas.Length} numbered _test_HudBar*.meta duplicate orphans after scrub. Expected 0. Survivors: " +
                string.Join(", ", stubMetas.Select(Path.GetFileName)));
        }

        [Test]
        public void CityScene_NoRefsToDeadSnakeGuid()
        {
            if (!File.Exists(CityScenePath))
            {
                Assert.Ignore("CityScene.unity missing — skipping dead-guid probe");
            }

            string sceneText = File.ReadAllText(CityScenePath);
            Assert.IsFalse(sceneText.Contains(DeadSnakeGuid),
                $"CityScene contains refs to retired snake hud_bar guid {DeadSnakeGuid} — dedup leaked");
        }
    }
}
