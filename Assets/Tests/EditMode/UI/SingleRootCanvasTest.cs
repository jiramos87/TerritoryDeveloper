// TECH-14097 / game-ui-catalog-bake Stage 8 §Red-Stage Proof.
//
// Asserts MainScene contains exactly one root-level Canvas (transform.parent == null)
// after Stage 8 D9 consolidation. Nested sub-canvases (children of root) remain Unity-legal
// optimization for sub-tree dirtying. Catches regressions where future authoring spawns
// a second sibling Canvas at scene root.

using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Territory.Tests.EditMode.UI
{
    public class SingleRootCanvasTest
    {
        private const string MainScenePath = "Assets/Scenes/MainScene.unity";
        private const string ExpectedRootCanvasName = "UI Canvas";

        [Test]
        public void MainScene_HasSingleRootCanvas_NamedUiCanvas()
        {
            if (!File.Exists(MainScenePath))
            {
                Assert.Ignore("MainScene.unity missing — skipping single-root-canvas probe");
            }

            Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            try
            {
                Canvas[] all = Object.FindObjectsOfType<Canvas>(includeInactive: true);
                Canvas[] roots = all
                    .Where(c => c.transform.parent == null)
                    .ToArray();

                Assert.AreEqual(1, roots.Length,
                    $"MainScene must carry exactly 1 root-level Canvas (D9 invariant). Found {roots.Length}: " +
                    string.Join(", ", roots.Select(c => c.name)));

                Assert.AreEqual(ExpectedRootCanvasName, roots[0].name,
                    $"Root canvas must be named '{ExpectedRootCanvasName}' (HudBarVisualSmokeTest + scene yaml depend on this name)");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, removeScene: true);
            }
        }
    }
}
