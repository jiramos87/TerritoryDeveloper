// TECH-11931 / game-ui-catalog-bake Stage 2 §Red-Stage Proof.
//
// Asserts CatalogBakeHandler.BakeFromSnapshot populates Button.spriteState
// (highlightedSprite / pressedSprite / disabledSprite) on every baked hud-bar
// child, and that Button.transition is SpriteSwap.
//
// Test name locked by Stage red_test_anchor: HudBarStateSpritesSwap.

using System.IO;
using NUnit.Framework;
using TerritoryDeveloper.Editor.Bake;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Territory.Tests.EditMode.UI
{
    public class HudBarStateSpritesSwapTest
    {
        private const string FixtureSnapshotPath =
            "Assets/Tests/EditMode/UI/Fixtures/hud-bar-snapshot.json";

        private const string TestOutDir =
            "Assets/UI/Prefabs/Generated/_test_HudBar_StateSwap";

        private const string ExpectedPrefab = TestOutDir + "/hud_bar.prefab";

        // Per-child expected hover/pressed/disabled refs (fixture uses idle icons for all states).
        private static readonly string[] ExpectedHoverRefs =
        {
            "Assets/UI/Sprites/hud_bar_icon_1.png",
            "Assets/UI/Sprites/hud_bar_icon_2.png",
            "Assets/UI/Sprites/hud_bar_icon_3.png",
            "Assets/UI/Sprites/hud_bar_icon_4.png",
            "Assets/UI/Sprites/hud_bar_icon_5.png",
            "Assets/UI/Sprites/hud_bar_icon_6.png",
            "Assets/UI/Sprites/hud_bar_icon_7.png",
            "Assets/UI/Sprites/hud_bar_icon_8.png",
            "Assets/UI/Sprites/hud_bar_icon_9.png",
        };

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
        public void HudBarStateSpritesSwap()
        {
            if (!File.Exists(FixtureSnapshotPath))
                Assert.Fail($"Fixture snapshot missing at {FixtureSnapshotPath}");

            CatalogBakeHandler.BakeFromSnapshot(FixtureSnapshotPath, TestOutDir);

            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ExpectedPrefab);
            Assert.IsNotNull(asset, $"hud_bar prefab not found at {ExpectedPrefab}");

            _instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            Assert.IsNotNull(_instance, "PrefabUtility.InstantiatePrefab returned null");

            Assert.That(_instance.transform.childCount, Is.EqualTo(9),
                $"expected 9 child buttons, got {_instance.transform.childCount}");

            for (int i = 0; i < _instance.transform.childCount; i++)
            {
                var child = _instance.transform.GetChild(i);
                var btn = child.GetComponent<Button>();
                Assert.IsNotNull(btn, $"child {i} ({child.name}) missing Button");

                // Transition must be SpriteSwap so Unity renders state sprites.
                Assert.AreEqual(
                    Selectable.Transition.SpriteSwap,
                    btn.transition,
                    $"child {i} ({child.name}) transition != SpriteSwap");

                var ss = btn.spriteState;

                Assert.IsNotNull(ss.highlightedSprite,
                    $"child {i} ({child.name}) spriteState.highlightedSprite is null");
                Assert.IsNotNull(ss.pressedSprite,
                    $"child {i} ({child.name}) spriteState.pressedSprite is null");
                Assert.IsNotNull(ss.disabledSprite,
                    $"child {i} ({child.name}) spriteState.disabledSprite is null");

                // Asset-path equality: deterministic and survives re-import.
                var expectedPath = ExpectedHoverRefs[i];
                Assert.AreEqual(expectedPath,
                    AssetDatabase.GetAssetPath(ss.highlightedSprite),
                    $"child {i} highlightedSprite path mismatch");
                Assert.AreEqual(expectedPath,
                    AssetDatabase.GetAssetPath(ss.pressedSprite),
                    $"child {i} pressedSprite path mismatch");
                Assert.AreEqual(expectedPath,
                    AssetDatabase.GetAssetPath(ss.disabledSprite),
                    $"child {i} disabledSprite path mismatch");
            }
        }
    }
}
