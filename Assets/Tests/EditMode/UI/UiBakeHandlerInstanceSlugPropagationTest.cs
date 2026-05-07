// TECH-23572 / game-ui-catalog-bake Stage 9.15
//
// §Red-Stage Proof:
// Assets/Tests/EditMode/UI/UiBakeHandlerInstanceSlugPropagationTest.cs::Bake_ChildName_Equals_InstanceSlug
//
// Red: child GameObjects keep "child_{ord}" placeholder names.
// Green: child name == instance_slug AND CatalogPrefabRef.slug == instance_slug
//        AND no "^illuminated-button( \(\d+\))?$" matches.

using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Territory.Editor.Bridge;
using Territory.UI;
using UnityEditor;
using UnityEngine;

namespace Territory.Tests.EditMode.UI
{
    public class UiBakeHandlerInstanceSlugPropagationTest
    {
        private const string TmpOutDir = "Assets/Tests/EditMode/UI/__TmpBakeOut__";

        [OneTimeTearDown]
        public void Cleanup()
        {
            if (AssetDatabase.IsValidFolder(TmpOutDir))
            {
                AssetDatabase.DeleteAsset(TmpOutDir);
            }
        }

        /// <summary>
        /// Bakes a minimal panels.json with instance_slug-carrying children.
        /// Asserts every child GameObject name == instance_slug AND CatalogPrefabRef.slug == instance_slug.
        /// §Red-Stage Proof surface keywords: PanelSnapshotChild, instance_slug, CatalogPrefabRef,
        /// Green (semantic name on every child, no illuminated-button (N) pattern).
        /// </summary>
        [Test]
        public void Bake_ChildName_Equals_InstanceSlug()
        {
            // Build a minimal PanelSnapshotItem with two children having instance_slug.
            var item = new UiBakeHandler.PanelSnapshotItem
            {
                slug = "test-panel-slug-prop",
                fields = new UiBakeHandler.PanelSnapshotFields
                {
                    layout_template = "hstack",
                    layout = "hstack",
                    gap_px = 0,
                    padding_json = "{}",
                    params_json = "{}",
                },
                children = new[]
                {
                    new UiBakeHandler.PanelSnapshotChild
                    {
                        ord = 1,
                        kind = "illuminated-button",
                        params_json = "{}",
                        sprite_ref = "",
                        layout_json = "{\"zone\":\"left\"}",
                        instance_slug = "hud-bar-zoom-in-button",
                    },
                    new UiBakeHandler.PanelSnapshotChild
                    {
                        ord = 2,
                        kind = "illuminated-button",
                        params_json = "{}",
                        sprite_ref = "",
                        layout_json = "{\"zone\":\"right\"}",
                        instance_slug = "hud-bar-pause-button",
                    },
                },
            };

            // Call BakePanelSnapshotChildren against a temporary root GO.
            var root = new GameObject("TestRoot-InstanceSlugProp");
            root.AddComponent<RectTransform>();
            root.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();

            try
            {
                // Invoke via reflection to access internal static method.
                var method = typeof(UiBakeHandler).GetMethod(
                    "BakePanelSnapshotChildren",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

                if (method == null)
                {
                    Assert.Ignore("BakePanelSnapshotChildren not accessible via reflection — test requires internal visibility. Mark internal or make public.");
                    return;
                }

                var themePath = "Assets/UI/Theme/DefaultUiTheme.asset";
                var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(themePath);
                method.Invoke(null, new object[] { item, root, theme });

                // Assert: children in subtree named by instance_slug, not "child_N" or "illuminated-button (N)".
                var illRx = new Regex(@"^illuminated-button( \(\d+\))?$", RegexOptions.Compiled);
                var allDescendants = root.GetComponentsInChildren<Transform>(true);
                foreach (var t in allDescendants)
                {
                    if (t.gameObject == root) continue;
                    Assert.IsFalse(illRx.IsMatch(t.name),
                        $"Found anonymous name '{t.name}' — instance_slug propagation not applied.");
                }

                // Assert CatalogPrefabRef present with correct slug on named children.
                var slugs = new[] { "hud-bar-zoom-in-button", "hud-bar-pause-button" };
                foreach (var slug in slugs)
                {
                    var found = root.transform.Find(slug);
                    // Note: may be nested under slot wrapper; do deep search.
                    Transform deep = FindDeep(root.transform, slug);
                    Assert.IsNotNull(deep,
                        $"Expected child named '{slug}' not found in baked hierarchy.");

                    var catalogRef = deep.GetComponent<CatalogPrefabRef>();
                    Assert.IsNotNull(catalogRef,
                        $"Child '{slug}' missing CatalogPrefabRef component.");
                    Assert.AreEqual(slug, catalogRef.slug,
                        $"CatalogPrefabRef.slug mismatch on '{slug}' child.");
                }
            }
            finally
            {
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Child_WithoutInstanceSlug_FallsBack_To_OrdName()
        {
            var item = new UiBakeHandler.PanelSnapshotItem
            {
                slug = "test-panel-fallback",
                fields = new UiBakeHandler.PanelSnapshotFields
                {
                    layout_template = "hstack",
                    layout = "hstack",
                    gap_px = 0,
                    padding_json = "{}",
                    params_json = "{}",
                },
                children = new[]
                {
                    new UiBakeHandler.PanelSnapshotChild
                    {
                        ord = 5,
                        kind = "illuminated-button",
                        params_json = "{}",
                        sprite_ref = "",
                        layout_json = null,
                        instance_slug = null,
                    },
                },
            };

            var root = new GameObject("TestRoot-FallbackOrd");
            root.AddComponent<RectTransform>();

            try
            {
                var method = typeof(UiBakeHandler).GetMethod(
                    "BakePanelSnapshotChildren",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

                if (method == null)
                {
                    Assert.Ignore("BakePanelSnapshotChildren not accessible.");
                    return;
                }

                var theme = AssetDatabase.LoadAssetAtPath<UiTheme>("Assets/UI/Theme/DefaultUiTheme.asset");
                method.Invoke(null, new object[] { item, root, theme });

                // Should have a child named "child_5" (no CatalogPrefabRef since no instance_slug).
                var childFive = FindDeep(root.transform, "child_5");
                Assert.IsNotNull(childFive, "child without instance_slug should be named 'child_{ord}'.");
            }
            finally
            {
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Transform FindDeep(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                var found = FindDeep(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
