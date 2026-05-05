// TECH-14097 / game-ui-catalog-bake Stage 8 §Red-Stage Proof.
// TECH-14448 / game-ui-catalog-bake Stage 9.1 — tightened to scene-wide single-Canvas (descendant ban).
//
// Asserts MainScene carries exactly ONE Canvas component scene-wide (T9.1 D9 tightening):
//   1. Root-level Canvas count == 1, named "UI Canvas".
//   2. Scene-wide Canvas count == 1 (no descendant Canvases under root).
//   3. Every Canvas in the scene has transform.parent == null.
//
// Rationale: Stage 8 D9 only checked root-level Canvas count. Post-Stage-8 ui_tree_walk
// reported canvas_count: 4 — wrapper Canvases nested under "UI Canvas/Canvas" +
// "UI Canvas/Canvas (Game UI)". Stage 9.1 flattened those wrappers + re-parented
// 4 legacy children (ControlPanel, DebugPanel, ProposalUI, MiniMapPanel) to root.
// This test now closes the descendant-Canvas loophole.

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

        [Test]
        public void MainScene_HasExactlyOneCanvas_SceneWide()
        {
            if (!File.Exists(MainScenePath))
            {
                Assert.Ignore("MainScene.unity missing — skipping scene-wide canvas probe");
            }

            Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            try
            {
                Canvas[] all = Object.FindObjectsOfType<Canvas>(includeInactive: true);

                Assert.AreEqual(1, all.Length,
                    $"MainScene must carry exactly 1 Canvas component scene-wide (T9.1 D9 tightening). Found {all.Length}: " +
                    string.Join(", ", all.Select(c =>
                    {
                        string path = c.name;
                        Transform t = c.transform.parent;
                        while (t != null) { path = t.name + "/" + path; t = t.parent; }
                        return path;
                    })));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, removeScene: true);
            }
        }

        [Test]
        public void MainScene_NoDescendantCanvases_UnderRoot()
        {
            if (!File.Exists(MainScenePath))
            {
                Assert.Ignore("MainScene.unity missing — skipping descendant-canvas probe");
            }

            Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            try
            {
                Canvas[] all = Object.FindObjectsOfType<Canvas>(includeInactive: true);
                Canvas[] descendants = all
                    .Where(c => c.transform.parent != null)
                    .ToArray();

                Assert.AreEqual(0, descendants.Length,
                    $"No Canvas component may sit under another GameObject (T9.1 D9 tightening). Found {descendants.Length} descendant Canvas(es): " +
                    string.Join(", ", descendants.Select(c =>
                    {
                        string path = c.name;
                        Transform t = c.transform.parent;
                        while (t != null) { path = t.name + "/" + path; t = t.parent; }
                        return path;
                    })));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, removeScene: true);
            }
        }
    }
}
