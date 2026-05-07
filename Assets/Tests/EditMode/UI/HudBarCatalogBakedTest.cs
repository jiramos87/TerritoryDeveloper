// TECH-19061 / game-ui-catalog-bake Stage 9.12
//
// §Red-Stage Proof: HudBar_ScenePrefab_RootSlugMatchesCatalog
//
// Asserts the hud-bar panel entity row exists in panels.json snapshot with slug 'hud-bar'.
// Pre-implementation: no hud-bar item in panels.json (0089 seeded children under a
// never-registered 'hud_bar' entity) → test fails (snapshot missing key).
// Post-implementation: migration 0096 registers catalog_entity + re-export lands hud-bar
// in panels.json → test passes.
//
// Scene-baked swap (CityScene hud-bar GameObject → DB-baked prefab instance) is a
// manual Editor step (bake + scene wire). This test validates the catalog side:
// snapshot contains 'hud-bar' + correct zone distribution.

using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Territory.Tests.EditMode.UI
{
    public class HudBarCatalogBakedTest
    {
        private const string SnapshotPath = "Assets/UI/Snapshots/panels.json";

        /// <summary>
        /// Verifies panels.json contains hud-bar panel item with slug='hud-bar'.
        /// Red: snapshot missing hud-bar. Green: 0096 migration + re-export lands row.
        /// </summary>
        [Test]
        public void HudBar_ScenePrefab_RootSlugMatchesCatalog()
        {
            if (!File.Exists(SnapshotPath))
            {
                Assert.Ignore($"panels.json missing at {SnapshotPath} — run npm run snapshot:export-game-ui first");
                return;
            }

            string raw = File.ReadAllText(SnapshotPath);

            // Check hud-bar slug present in snapshot.
            Assert.IsTrue(raw.Contains("\"slug\": \"hud-bar\""),
                "panels.json must contain item with slug='hud-bar'. " +
                "Run migration 0096 + npm run snapshot:export-game-ui to re-export.");
        }

        /// <summary>
        /// Verifies panels.json hud-bar has exactly 10 children with correct zone distribution:
        /// 3 left, 3 center, 4 right.
        /// </summary>
        [Test]
        public void HudBar_Snapshot_Has10ChildrenWithCorrectZones()
        {
            if (!File.Exists(SnapshotPath))
            {
                Assert.Ignore($"panels.json missing at {SnapshotPath} — run npm run snapshot:export-game-ui first");
                return;
            }

            string raw = File.ReadAllText(SnapshotPath);

            // Locate hud-bar block — find items array entry with slug=hud-bar.
            // Use simple token counting — no JSON lib dependency in EditMode tests.
            int hudBarIdx = raw.IndexOf("\"slug\": \"hud-bar\"", System.StringComparison.Ordinal);
            Assert.IsTrue(hudBarIdx >= 0,
                "panels.json missing hud-bar entry. Run migration 0096 + snapshot:export-game-ui.");

            // Count zone tokens after the hud-bar slug (up to the next top-level slug entry).
            int nextSlug = raw.IndexOf("\"slug\":", hudBarIdx + 10, System.StringComparison.Ordinal);
            string hudBlock = nextSlug > hudBarIdx
                ? raw.Substring(hudBarIdx, nextSlug - hudBarIdx)
                : raw.Substring(hudBarIdx);

            int leftCount   = CountOccurrences(hudBlock, "\"zone\": \"left\"");
            int centerCount = CountOccurrences(hudBlock, "\"zone\": \"center\"");
            int rightCount  = CountOccurrences(hudBlock, "\"zone\": \"right\"");

            Assert.AreEqual(3, leftCount,
                $"Expected 3 left-zone children, got {leftCount}. Re-run migration 0096 + export.");
            Assert.AreEqual(3, centerCount,
                $"Expected 3 center-zone children, got {centerCount}. Re-run migration 0096 + export.");
            Assert.AreEqual(4, rightCount,
                $"Expected 4 right-zone children, got {rightCount}. Re-run migration 0096 + export.");
        }

        private static int CountOccurrences(string src, string token)
        {
            int count = 0;
            int pos = 0;
            while ((pos = src.IndexOf(token, pos, System.StringComparison.Ordinal)) >= 0)
            {
                count++;
                pos += token.Length;
            }
            return count;
        }
    }
}
