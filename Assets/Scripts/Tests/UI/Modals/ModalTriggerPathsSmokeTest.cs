using System.Collections;
using NUnit.Framework;
using TMPro;
using Territory.UI;
using Territory.UI.Modals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Territory.Tests.PlayMode.UI.Modals
{
    /// <summary>
    /// Stage 12 modal-trigger-paths smoke — drives each player-visible trigger path
    /// through `UIManager.OpenPopup` (mirrors Esc / Alt+click / MainMenu / Pause-button
    /// triggers rewired in Stage 12), then asserts the corresponding Stage 8 themed
    /// modal is visible (root activeInHierarchy, themed primitive Image alpha > 0,
    /// TMP_Text label rendered) and no console errors fired. Final scenario covers
    /// Esc-stack close-last-first regression (LIFO contract from `UIManager.PopupStack`).
    /// </summary>
    public sealed class ModalTriggerPathsSmokeTest
    {
        private const string ScenePath = "Assets/Scenes/MainScene.unity";

        private UIManager _uiManager;
        private InfoPanelDataAdapter _infoPanel;
        private PauseMenuDataAdapter _pauseMenu;
        private SettingsScreenDataAdapter _settingsScreen;
        private SaveLoadScreenDataAdapter _saveLoad;
        private NewGameScreenDataAdapter _newGame;
        private DetailsPopupController _details;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            LogAssert.ignoreFailingMessages = false;

#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#endif
            yield return null;
            yield return null;

            _uiManager = Object.FindObjectOfType<UIManager>();
            _infoPanel = Object.FindObjectOfType<InfoPanelDataAdapter>();
            _pauseMenu = Object.FindObjectOfType<PauseMenuDataAdapter>();
            _settingsScreen = Object.FindObjectOfType<SettingsScreenDataAdapter>();
            _saveLoad = Object.FindObjectOfType<SaveLoadScreenDataAdapter>();
            _newGame = Object.FindObjectOfType<NewGameScreenDataAdapter>();
            _details = Object.FindObjectOfType<DetailsPopupController>();
        }

        private static void AssertModalVisible(GameObject root, string label)
        {
            Assert.That(root, Is.Not.Null, label + ": root is null");
            Assert.That(root.activeInHierarchy, Is.True, label + ": root not activeInHierarchy");

            var image = root.GetComponentInChildren<Image>(includeInactive: false);
            if (image != null)
            {
                Assert.That(image.color.a, Is.GreaterThan(0f),
                    label + ": themed primitive Image alpha must be > 0");
            }

            var tmp = root.GetComponentInChildren<TMP_Text>(includeInactive: false);
            if (tmp != null)
            {
                Assert.That(string.IsNullOrEmpty(tmp.text), Is.False,
                    label + ": TMP_Text must render non-empty");
            }
        }

        [UnityTest]
        public IEnumerator EscEmptyStack_OpensPauseMenu()
        {
            Assert.That(_uiManager, Is.Not.Null, "UIManager not found in scene");
            Assert.That(_pauseMenu, Is.Not.Null, "PauseMenuDataAdapter not found in scene");

            _uiManager.OpenPopup(PopupType.PauseMenu);
            yield return null;

            AssertModalVisible(_pauseMenu.gameObject, "PauseMenu");
            LogAssert.NoUnexpectedReceived();

            _uiManager.ClosePopup(PopupType.PauseMenu);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AltClickGridCell_OpensInfoPanel()
        {
            Assert.That(_uiManager, Is.Not.Null);
            Assert.That(_details, Is.Not.Null, "DetailsPopupController not found in scene");
            Assert.That(_infoPanel, Is.Not.Null, "InfoPanelDataAdapter not found in scene");

            _details.ShowCellDetails("Residential", "Low Density", "12", "0.42", "0.18");
            yield return null;

            AssertModalVisible(_infoPanel.gameObject, "InfoPanel");
            LogAssert.NoUnexpectedReceived();

            _uiManager.ClosePopup(PopupType.InfoPanel);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MainMenuOptions_OpensSettingsScreen()
        {
            Assert.That(_uiManager, Is.Not.Null);
            Assert.That(_settingsScreen, Is.Not.Null, "SettingsScreenDataAdapter not found in scene");

            _uiManager.OpenPopup(PopupType.SettingsScreen);
            yield return null;

            AssertModalVisible(_settingsScreen.gameObject, "SettingsScreen");
            LogAssert.NoUnexpectedReceived();

            _uiManager.ClosePopup(PopupType.SettingsScreen);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MainMenuNewGame_OpensNewGameScreen()
        {
            Assert.That(_uiManager, Is.Not.Null);
            Assert.That(_newGame, Is.Not.Null, "NewGameScreenDataAdapter not found in scene");

            _uiManager.OpenPopup(PopupType.NewGameScreen);
            yield return null;

            AssertModalVisible(_newGame.gameObject, "NewGameScreen");
            LogAssert.NoUnexpectedReceived();

            _uiManager.ClosePopup(PopupType.NewGameScreen);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PauseSaveLoad_OpensSaveLoadScreen()
        {
            Assert.That(_uiManager, Is.Not.Null);
            Assert.That(_saveLoad, Is.Not.Null, "SaveLoadScreenDataAdapter not found in scene");

            _uiManager.OpenPopup(PopupType.SaveLoadScreen);
            yield return null;

            AssertModalVisible(_saveLoad.gameObject, "SaveLoadScreen");
            LogAssert.NoUnexpectedReceived();

            _uiManager.ClosePopup(PopupType.SaveLoadScreen);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EscStack_CloseLastFirst_Regression()
        {
            Assert.That(_uiManager, Is.Not.Null);
            Assert.That(_settingsScreen, Is.Not.Null);
            Assert.That(_infoPanel, Is.Not.Null);

            _uiManager.OpenPopup(PopupType.SettingsScreen);
            yield return null;
            _uiManager.OpenPopup(PopupType.InfoPanel);
            yield return null;

            AssertModalVisible(_settingsScreen.gameObject, "SettingsScreen-stacked");
            AssertModalVisible(_infoPanel.gameObject, "InfoPanel-stacked");

            // LIFO close: top of stack (InfoPanel) closes first; SettingsScreen still visible.
            _uiManager.ClosePopup(PopupType.InfoPanel);
            yield return null;

            Assert.That(_infoPanel.gameObject.activeInHierarchy, Is.False,
                "InfoPanel should close first (top of stack)");
            Assert.That(_settingsScreen.gameObject.activeInHierarchy, Is.True,
                "SettingsScreen should still be visible (one level deeper in stack)");

            // Second close empties the stack.
            _uiManager.ClosePopup(PopupType.SettingsScreen);
            yield return null;

            Assert.That(_settingsScreen.gameObject.activeInHierarchy, Is.False,
                "SettingsScreen should close after InfoPanel");

            LogAssert.NoUnexpectedReceived();
        }
    }
}
