// TECH-11939 / game-ui-catalog-bake Stage 5 §Red-Stage Proof.
//
// red_test_anchor: visibility-delta-test:Assets/Tests/EditMode/UI/FreePanelLayoutTest.cs::FreePanelHonorsAbsolutePositions
// target_kind: visibility_delta
//
// Asserts the resource_info_popup baked by CatalogBakeHandler.BakeFree places
// each child at literal RectTransform.anchoredPosition = (x_px, -y_px) and
// sizeDelta = (w_px, h_px) per panel_child.params_json. Y-axis convention:
// catalog y_px is "px from top"; Unity top-anchor needs negative Y.
//
// Depends on:
//   - TECH-11938: CatalogBakeHandler.Free.cs partial (BakeFree dispatcher entry)
//   - FEAT-57:    panel_detail layout=free + resource_info_popup seed
//   - Fixture:    Assets/Tests/EditMode/UI/Fixtures/free-panel-snapshot.json
//
// Visibility delta gate: "Free panel honors absolute (x_px, y_px, w_px, h_px)
// per child" — see game-ui-catalog-bake Stage 5 master plan.

using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TerritoryDeveloper.Editor.Bake;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Territory.Tests.EditMode.UI
{
    [TestFixture]
    public class FreePanelLayoutTest
    {
        private const string FixtureSnapshotPath =
            "Assets/Tests/EditMode/UI/Fixtures/free-panel-snapshot.json";

        private const string TestOutDir =
            "Assets/Tests/Generated/FreePanel";

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
        public void FreePanelHonorsAbsolutePositions()
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
                "Free panel fixture should carry exactly 1 panel item");

            var item = snapshot.items[0];
            Assert.IsNotNull(item.panel, "Snapshot panel missing");
            Assert.AreEqual("free", item.panel.layout,
                "Fixture panel layout must be free (Stage 5 primitive)");
            Assert.IsNotNull(item.children, "Snapshot panel.children missing");
            Assert.Greater(item.children.Length, 0,
                "Free panel fixture must carry at least one child");

            // Build expected coords map by ord → (x, -y, w, h).
            var expected = new Dictionary<int, FreeChildExpect>();
            foreach (var c in item.children)
            {
                if (c == null) continue;
                var dims = JsonUtility.FromJson<FreeChildFixture>(c.params_json);
                Assert.IsNotNull(dims, $"params_json missing for child ord={c.ord}");
                expected[c.ord] = new FreeChildExpect
                {
                    kind  = c.kind,
                    pos   = new Vector2(dims.x_px, -dims.y_px),
                    size  = new Vector2(dims.w_px, dims.h_px),
                };
            }

            // ── 2. Bake via the same dispatcher Stage 5 ships ─────────────────
            Directory.CreateDirectory(TestOutDir);
            CatalogBakeHandler.BakeFromSnapshot(FixtureSnapshotPath, TestOutDir);

            var prefabPath = TestOutDir + "/" + item.panel.slug + ".prefab";
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(asset,
                $"Free panel prefab not found at {prefabPath} after bake");

            // ── 3. Instantiate under the test Canvas ──────────────────────────
            _instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            _instance.transform.SetParent(_canvasGo.transform, false);

            // ── 4. Assertion — per-child anchoredPosition + sizeDelta match ───
            int matched = 0;
            foreach (Transform t in _instance.GetComponentsInChildren<Transform>(true))
            {
                if (t == null || t.name == null) continue;
                if (t == _instance.transform) continue;

                int ord;
                if (!TryParseChildOrd(t.name, out ord)) continue;
                if (!expected.TryGetValue(ord, out var want)) continue;

                var rt = t as RectTransform;
                Assert.IsNotNull(rt, $"Child '{t.name}' must carry RectTransform");

                Assert.AreEqual(want.pos.x, rt.anchoredPosition.x, 0.01f,
                    $"Child ord={ord} anchoredPosition.x mismatch");
                Assert.AreEqual(want.pos.y, rt.anchoredPosition.y, 0.01f,
                    $"Child ord={ord} anchoredPosition.y mismatch (Y-flip from catalog y_px)");
                Assert.AreEqual(want.size.x, rt.sizeDelta.x, 0.01f,
                    $"Child ord={ord} sizeDelta.x mismatch");
                Assert.AreEqual(want.size.y, rt.sizeDelta.y, 0.01f,
                    $"Child ord={ord} sizeDelta.y mismatch");

                matched++;
            }

            Assert.AreEqual(expected.Count, matched,
                $"Expected {expected.Count} matched free children; got {matched}");
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private static bool TryParseChildOrd(string name, out int ord)
        {
            ord = 0;
            if (string.IsNullOrEmpty(name)) return false;
            int underscore = name.LastIndexOf('_');
            if (underscore < 0 || underscore == name.Length - 1) return false;
            return int.TryParse(name.Substring(underscore + 1), out ord);
        }

        [System.Serializable]
        private class FreeChildFixture
        {
            public int x_px;
            public int y_px;
            public int w_px;
            public int h_px;
            public string text;
        }

        private struct FreeChildExpect
        {
            public string kind;
            public Vector2 pos;
            public Vector2 size;
        }
    }
}
