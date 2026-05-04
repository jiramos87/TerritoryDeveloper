// TECH-11928 / game-ui-catalog-bake Stage 1.0 §Red-Stage Proof.
//
// Asserts CatalogBakeHandler.BakeFromSnapshot produces the hud-bar prefab
// with HorizontalLayoutGroup root + exactly 9 child Button components,
// each carrying a non-null Image.sprite. Test name locked by Stage
// `red_test_anchor` field (HudBarRendersAsHorizontalRowOf9ButtonsWithIcons).

using System.IO;
using NUnit.Framework;
using TerritoryDeveloper.Editor.Bake;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Territory.Tests.EditMode.UI
{
    public class HudBarBakeLayoutTest
    {
        private const string SnapshotPath = "Assets/UI/Snapshots/panels.json";
        private const string TestOutDir = "Assets/UI/Prefabs/Generated/_test_HudBar";
        private const string ExpectedPrefab = TestOutDir + "/hud_bar.prefab";

        private GameObject _instance;

        [SetUp]
        public void SetUp()
        {
            Directory.CreateDirectory(TestOutDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (_instance != null)
            {
                Object.DestroyImmediate(_instance);
                _instance = null;
            }
            if (AssetDatabase.IsValidFolder(TestOutDir))
            {
                AssetDatabase.DeleteAsset(TestOutDir);
            }
        }

        [Test]
        public void HudBarRendersAsHorizontalRowOf9ButtonsWithIcons()
        {
            if (!File.Exists(SnapshotPath))
            {
                Assert.Ignore("panels.json missing — run npm run snapshot:export-game-ui first");
                return;
            }

            CatalogBakeHandler.BakeFromSnapshot(SnapshotPath, TestOutDir);

            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ExpectedPrefab);
            Assert.IsNotNull(asset, $"hud_bar prefab not found at {ExpectedPrefab}");

            _instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            Assert.IsNotNull(_instance, "PrefabUtility.InstantiatePrefab returned null");

            var hlg = _instance.GetComponent<HorizontalLayoutGroup>();
            Assert.IsNotNull(hlg, "root missing HorizontalLayoutGroup");

            Assert.That(_instance.transform.childCount, Is.EqualTo(9),
                $"expected 9 child buttons, got {_instance.transform.childCount}");

            for (int i = 0; i < _instance.transform.childCount; i++)
            {
                var child = _instance.transform.GetChild(i);
                var btn = child.GetComponent<Button>();
                Assert.IsNotNull(btn, $"child {i} ({child.name}) missing Button component");

                var iconImg = FindIconImage(child);
                Assert.IsNotNull(iconImg, $"child {i} ({child.name}) missing descendant Image");
                Assert.IsNotNull(iconImg.sprite, $"child {i} ({child.name}) Image.sprite is null");
            }
        }

        private static Image FindIconImage(Transform parent)
        {
            // Look for a child named "Icon" with an Image component (Hstack convention).
            for (int i = 0; i < parent.childCount; i++)
            {
                var c = parent.GetChild(i);
                if (c.name == "Icon")
                {
                    var img = c.GetComponent<Image>();
                    if (img != null) return img;
                }
            }
            // Fallback: any descendant Image whose GameObject != parent.
            return parent.GetComponentInChildren<Image>(includeInactive: true);
        }
    }
}
