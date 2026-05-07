// TECH-14101 / game-ui-catalog-bake Stage 8 §Red-Stage Proof.
//
// Asserts CityScene's UIManager.gridCoordinatesText field is bound to a
// live Text component whose GameObject is active in scene yaml. Catches
// regressions where catalog-bake (or any future authoring) leaves the
// CellDataPanel inner Text inactive — root cause of the empty-panel bug
// found at Stage 8 entry.

using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Territory.UI;

namespace Territory.Tests.EditMode.UI
{
    public class CellDataPanelBindingTest
    {
        private const string CityScenePath = "Assets/Scenes/CityScene.unity";

        [Test]
        public void GridCoordinatesText_BoundAndActiveInScene()
        {
            if (!File.Exists(CityScenePath))
            {
                Assert.Ignore("CityScene.unity missing — skipping binding probe");
            }

            Scene scene = EditorSceneManager.OpenScene(CityScenePath, OpenSceneMode.Single);
            try
            {
                UIManager ui = Object.FindObjectOfType<UIManager>(includeInactive: true);
                Assert.IsNotNull(ui, "UIManager not found in CityScene");
                Assert.IsNotNull(ui.gridCoordinatesText, "UIManager.gridCoordinatesText is null — scene field reference broken");
                Assert.IsTrue(ui.gridCoordinatesText.gameObject.activeSelf,
                    "GridCoordinatesText GameObject is inactive — CellDataPanel renders empty");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, removeScene: true);
            }
        }
    }
}
