// TECH-11936 / game-ui-catalog-bake Stage 4 §Red-Stage Proof.
//
// red_test_anchor: visibility-delta-test:Assets/Tests/EditMode/UI/StatsPanelRendersFourTabsAndRowsTest.cs::StatsPanelRendersFourTabsAndRows
// target_kind: visibility_delta
//
// Asserts the stats_panel_city baked by CatalogBakeHandler.BakeVstack carries
// exactly 4 tab Button children (Money/People/Land/Infrastructure) and the
// per-kind row count matches the snapshot's panel_child rows. Snapshot is the
// source of truth — counts are read from the fixture, not hardcoded.
//
// Depends on:
//   - TECH-11935: CatalogBakeHandler.Vstack.cs partial (BakeVstack dispatcher entry)
//   - FEAT-56:    panel_detail.params_json column + stats_panel_city seed
//   - Fixture:    Assets/Tests/EditMode/UI/Fixtures/stats-panel-snapshot.json
//                 (mirrors FEAT-56 seed shape: 1 panel + 4 tabs + 16 rows)
//
// Visibility delta gate: "Stats panel renders city Money/People/Land/Infrastructure
// tabs with row data" — see game-ui-catalog-bake Stage 4 master plan.

using System.IO;
using NUnit.Framework;
using TerritoryDeveloper.Editor.Bake;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Territory.Tests.EditMode.UI
{
    [TestFixture]
    public class StatsPanelRendersFourTabsAndRowsTest
    {
        private const string FixtureSnapshotPath =
            "Assets/Tests/EditMode/UI/Fixtures/stats-panel-snapshot.json";

        private const string TestOutDir =
            "Assets/Tests/Generated/StatsPanelCity";

        private GameObject _canvasGo;
        private GameObject _instance;

        [SetUp]
        public void SetUp()
        {
            _canvasGo = new GameObject("TestCanvas");
            _canvasGo.AddComponent<Canvas>();
            _canvasGo.AddComponent<CanvasScaler>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_instance != null)
            {
                Object.DestroyImmediate(_instance);
                _instance = null;
            }

            if (_canvasGo != null)
            {
                Object.DestroyImmediate(_canvasGo);
                _canvasGo = null;
            }

            if (AssetDatabase.IsValidFolder(TestOutDir))
                AssetDatabase.DeleteAsset(TestOutDir);
        }

        [Test]
        public void StatsPanelRendersFourTabsAndRows()
        {
            // ── 1. Fixture preflight ──────────────────────────────────────────
            if (!File.Exists(FixtureSnapshotPath))
                Assert.Fail($"Fixture snapshot missing at {FixtureSnapshotPath}");

            // Snapshot is source of truth — derive expected counts from the fixture.
            var json = File.ReadAllText(FixtureSnapshotPath);
            var snapshot = JsonUtility.FromJson<CatalogBakeHandler.PanelsSnapshot>(json);
            Assert.IsNotNull(snapshot, "Snapshot did not parse into PanelsSnapshot");
            Assert.AreEqual("panels", snapshot.kind, "Snapshot kind must be 'panels'");
            Assert.IsNotNull(snapshot.items, "Snapshot items array missing");
            Assert.AreEqual(1, snapshot.items.Length,
                "Stats fixture should carry exactly 1 panel item (stats_panel_city)");

            var item = snapshot.items[0];
            Assert.IsNotNull(item.panel, "Snapshot panel missing");
            Assert.AreEqual("stats_panel_city", item.panel.slug,
                "Fixture panel slug must be stats_panel_city");
            Assert.AreEqual("vstack", item.panel.layout,
                "Fixture panel layout must be vstack (Stage 4 primitive)");
            Assert.IsNotNull(item.children, "Snapshot panel.children missing");

            int expectedTabCount = 0;
            int expectedRowCount = 0;
            foreach (var c in item.children)
            {
                if (c == null) continue;
                if (c.kind == "button") expectedTabCount++;
                else if (c.kind == "row") expectedRowCount++;
            }
            Assert.AreEqual(4, expectedTabCount,
                "Stats fixture must carry 4 tab buttons (Money/People/Land/Infrastructure)");
            Assert.Greater(expectedRowCount, 0,
                "Stats fixture must carry at least one stat row");

            // ── 2. Bake via the same dispatcher Stage 4 ships ─────────────────
            Directory.CreateDirectory(TestOutDir);
            CatalogBakeHandler.BakeFromSnapshot(FixtureSnapshotPath, TestOutDir);

            var prefabPath = TestOutDir + "/stats_panel_city.prefab";
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(asset,
                $"stats_panel_city prefab not found at {prefabPath} after bake");

            // ── 3. Instantiate under the test Canvas ──────────────────────────
            _instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            _instance.transform.SetParent(_canvasGo.transform, false);

            // ── 4. Assertion 1 — tab Button count matches snapshot ────────────
            var buttons = _instance.GetComponentsInChildren<Button>(true);
            Assert.AreEqual(expectedTabCount, buttons.Length,
                $"Expected {expectedTabCount} tab Button children (from snapshot); " +
                $"prefab has {buttons.Length}");

            // ── 5. Assertion 2 — Row child count matches snapshot ─────────────
            // Vstack BuildChildRow names rows "Row_{ord}" with a HorizontalLayoutGroup.
            // HLG count is the unambiguous prefab projection of kind='row' children.
            var hlgs = _instance.GetComponentsInChildren<HorizontalLayoutGroup>(true);
            Assert.AreEqual(expectedRowCount, hlgs.Length,
                $"Expected {expectedRowCount} row children (from snapshot panel_child kind='row'); " +
                $"prefab has {hlgs.Length} HorizontalLayoutGroup nodes");

            // Belt-and-braces: assert by child name prefix as well, since the
            // §Acceptance binds on "row count" not "HLG count" — guards against
            // a future refactor that swaps HLG for a different layout primitive.
            int rowNamedChildren = 0;
            foreach (Transform t in _instance.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != null && t.name.StartsWith("Row_")) rowNamedChildren++;
            }
            Assert.AreEqual(expectedRowCount, rowNamedChildren,
                $"Expected {expectedRowCount} children named 'Row_*'; prefab has {rowNamedChildren}");
        }
    }
}
