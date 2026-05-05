// TECH-11939 / game-ui-catalog-bake Stage 5 §Red-Stage Proof.
//
// red_test_anchor: visibility-delta-test:Assets/Tests/EditMode/UI/GridPickerLayoutTest.cs::GridPickerRendersIconGrid
// target_kind: visibility_delta
//
// Asserts the subtype_picker_rcis baked by CatalogBakeHandler.BakeGrid carries
// exactly N Cell_{ord} Button children matching the fixture's panel_child rows
// AND the GridLayoutGroup constraintCount + cellSize match the panel
// params_json (grid_cols, cell_w_px, cell_h_px). Snapshot is source of truth.
//
// Depends on:
//   - TECH-11937: CatalogBakeHandler.Grid.cs partial (BakeGrid dispatcher entry)
//   - FEAT-57:    panel_detail layout=grid + subtype_picker_rcis seed
//   - Fixture:    Assets/Tests/EditMode/UI/Fixtures/subtype-picker-snapshot.json
//
// Visibility delta gate: "Subtype picker renders 12-icon grid via GridLayoutGroup
// FixedColumnCount(4)" — see game-ui-catalog-bake Stage 5 master plan.

using System.IO;
using NUnit.Framework;
using TerritoryDeveloper.Editor.Bake;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Territory.Tests.EditMode.UI
{
    [TestFixture]
    public class GridPickerLayoutTest
    {
        private const string FixtureSnapshotPath =
            "Assets/Tests/EditMode/UI/Fixtures/subtype-picker-snapshot.json";

        private const string TestOutDir =
            "Assets/Tests/Generated/SubtypePickerRcis";

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
        public void GridPickerRendersIconGrid()
        {
            // ── 1. Fixture preflight ──────────────────────────────────────────
            if (!File.Exists(FixtureSnapshotPath))
                Assert.Fail($"Fixture snapshot missing at {FixtureSnapshotPath}");

            var json = File.ReadAllText(FixtureSnapshotPath);
            var snapshot = JsonUtility.FromJson<CatalogBakeHandler.PanelsSnapshot>(json);
            Assert.IsNotNull(snapshot, "Snapshot did not parse into PanelsSnapshot");
            Assert.AreEqual("panels", snapshot.kind, "Snapshot kind must be 'panels'");
            Assert.IsNotNull(snapshot.items, "Snapshot items array missing");
            Assert.AreEqual(1, snapshot.items.Length,
                "Subtype picker fixture should carry exactly 1 panel item");

            var item = snapshot.items[0];
            Assert.IsNotNull(item.panel, "Snapshot panel missing");
            Assert.AreEqual("subtype_picker_rcis", item.panel.slug,
                "Fixture panel slug must be subtype_picker_rcis");
            Assert.AreEqual("grid", item.panel.layout,
                "Fixture panel layout must be grid (Stage 5 primitive)");
            Assert.IsNotNull(item.children, "Snapshot panel.children missing");

            int expectedButtonCount = 0;
            foreach (var c in item.children)
            {
                if (c == null) continue;
                if (c.kind == "button") expectedButtonCount++;
            }
            Assert.Greater(expectedButtonCount, 0,
                "Subtype picker fixture must carry at least one button child");

            var gridParams = JsonUtility.FromJson<GridParamsFixture>(item.panel.params_json);
            Assert.IsNotNull(gridParams, "panel.params_json missing for grid layout");
            Assert.Greater(gridParams.grid_cols, 0, "grid_cols must be > 0");
            Assert.Greater(gridParams.cell_w_px, 0, "cell_w_px must be > 0");
            Assert.Greater(gridParams.cell_h_px, 0, "cell_h_px must be > 0");

            // ── 2. Bake via the same dispatcher Stage 5 ships ─────────────────
            Directory.CreateDirectory(TestOutDir);
            CatalogBakeHandler.BakeFromSnapshot(FixtureSnapshotPath, TestOutDir);

            var prefabPath = TestOutDir + "/subtype_picker_rcis.prefab";
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(asset,
                $"subtype_picker_rcis prefab not found at {prefabPath} after bake");

            // ── 3. Instantiate under the test Canvas ──────────────────────────
            _instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            _instance.transform.SetParent(_canvasGo.transform, false);

            // ── 4. Assertion 1 — Button count matches fixture ─────────────────
            var buttons = _instance.GetComponentsInChildren<Button>(true);
            Assert.AreEqual(expectedButtonCount, buttons.Length,
                $"Expected {expectedButtonCount} Cell Button children (from fixture); " +
                $"prefab has {buttons.Length}");

            // ── 5. Assertion 2 — child names follow Cell_{ord} pattern ────────
            int cellNamedChildren = 0;
            foreach (Transform t in _instance.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != null && t.name.StartsWith("Cell_")) cellNamedChildren++;
            }
            Assert.AreEqual(expectedButtonCount, cellNamedChildren,
                $"Expected {expectedButtonCount} children named 'Cell_*'; " +
                $"prefab has {cellNamedChildren}");

            // ── 6. Assertion 3 — GridLayoutGroup constraint matches fixture ───
            var grid = _instance.GetComponent<GridLayoutGroup>();
            Assert.IsNotNull(grid, "Root must carry GridLayoutGroup");
            Assert.AreEqual(GridLayoutGroup.Constraint.FixedColumnCount, grid.constraint,
                "GridLayoutGroup must use FixedColumnCount constraint");
            Assert.AreEqual(gridParams.grid_cols, grid.constraintCount,
                $"constraintCount must match params_json.grid_cols={gridParams.grid_cols}");
            Assert.AreEqual(gridParams.cell_w_px, (int)grid.cellSize.x,
                $"cellSize.x must match params_json.cell_w_px={gridParams.cell_w_px}");
            Assert.AreEqual(gridParams.cell_h_px, (int)grid.cellSize.y,
                $"cellSize.y must match params_json.cell_h_px={gridParams.cell_h_px}");
        }

        [System.Serializable]
        private class GridParamsFixture
        {
            public int grid_cols;
            public int cell_w_px;
            public int cell_h_px;
            public int spacing_x_px;
            public int spacing_y_px;
        }
    }
}
