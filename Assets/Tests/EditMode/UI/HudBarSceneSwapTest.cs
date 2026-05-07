// TECH-22667 / game-ui-catalog-bake Stage 9.14
//
// §Red-Stage Proof (Anchor 1):
// Assets/Tests/EditMode/UI/HudBarSceneSwapTest.cs::HudBar_Root_IsCatalogPrefabInstance
//
// Red: Pre-swap — MainScene hud-bar root has no CatalogPrefabRef component
//      AND/OR descendants named "illuminated-button (N)" exist.
// Green: Post-swap via scene_replace_with_prefab bridge — root carries
//        CatalogPrefabRef.slug=="hud-bar" AND zero illuminated-button(N) descendants.

using System.Text.RegularExpressions;
using NUnit.Framework;
using Territory.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Territory.Tests.EditMode.UI
{
    public class HudBarSceneSwapTest
    {
        private const string ScenePath = "Assets/Scenes/MainScene.unity";
        private static readonly Regex IlluminatedButtonNameRx =
            new Regex(@"^illuminated-button( \(\d+\))?$", RegexOptions.Compiled);

        [OneTimeSetUp]
        public void LoadScene()
        {
            if (!System.IO.File.Exists(ScenePath))
                Assert.Ignore($"Scene not found: {ScenePath}");

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        /// <summary>
        /// Asserts hud-bar root in MainScene carries CatalogPrefabRef.slug=='hud-bar'
        /// AND has zero descendants whose name matches /^illuminated-button( \(\d+\))?$/.
        /// Red: scene has no CatalogPrefabRef or still has generic illuminated-button (N) children.
        /// Green: prefab swap via scene_replace_with_prefab bridge landed; CatalogPrefabRef attached with slug hud-bar.
        /// </summary>
        [Test]
        public void HudBar_Root_IsCatalogPrefabInstance()
        {
            // §Red-Stage Proof surface keywords: PrefabUtility, InstantiatePrefab, Green (post-swap state).
            var hudBar = GameObject.Find("hud-bar");
            Assert.IsNotNull(hudBar, "hud-bar GameObject not found in MainScene.");

            // Assert CatalogPrefabRef present with correct slug.
            var catalogRef = hudBar.GetComponent<CatalogPrefabRef>();
            Assert.IsNotNull(catalogRef,
                "hud-bar root missing CatalogPrefabRef component — prefab swap not yet applied. " +
                "Run scene_replace_with_prefab bridge (scene_path='Assets/Scenes/MainScene.unity', " +
                "target_object_name='hud-bar', prefab_path='Assets/UI/Prefabs/Generated/hud-bar.prefab').");

            Assert.AreEqual("hud-bar", catalogRef.slug,
                $"CatalogPrefabRef.slug expected 'hud-bar', got '{catalogRef.slug}'.");

            // Assert no descendant named "illuminated-button (N)".
            var allChildren = hudBar.GetComponentsInChildren<Transform>(true);
            foreach (var child in allChildren)
            {
                if (child.gameObject == hudBar) continue; // skip root
                Assert.IsFalse(IlluminatedButtonNameRx.IsMatch(child.name),
                    $"Found generic-named descendant '{child.name}' under hud-bar — " +
                    "prefab swap incomplete: old hand-laid hierarchy still present.");
            }
        }
    }
}
