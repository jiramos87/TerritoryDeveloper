using System.IO;
using Territory.Persistence;
using Territory.UI.ViewModels;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// MonoBehaviour Host for main-menu UIToolkit panel. Wires nav buttons to actual scene
    /// transitions (parity with legacy <c>MainMenuController</c>). Lives on the UIDocument GameObject
    /// in MainMenu scene.
    /// </summary>
    public sealed class MainMenuHost : MonoBehaviour
    {
        const int CitySceneBuildIndex = 1;
        const string LastSavePathKey = "LastSavePath";

        [SerializeField] UIDocument _doc;

        MainMenuVM _vm;
        Button _btnNewGame;
        Button _btnLoad;
        Button _btnSettings;
        Button _btnQuit;

        void OnEnable()
        {
            _vm = new MainMenuVM();
            _vm.NewGameCommand = OnNewGame;
            _vm.LoadCommand = OnLoad;
            _vm.SettingsCommand = OnSettings;
            _vm.QuitCommand = OnQuit;

            if (_doc == null || _doc.rootVisualElement == null)
            {
                Debug.LogWarning("[MainMenuHost] UIDocument or rootVisualElement null on enable — check PanelSettings wiring.");
                return;
            }

            var root = _doc.rootVisualElement;
            root.SetCompatDataSource(_vm);

            _btnNewGame = root.Q<Button>("btn-new-game");
            _btnLoad = root.Q<Button>("btn-load");
            _btnSettings = root.Q<Button>("btn-settings");
            _btnQuit = root.Q<Button>("btn-quit");

            if (_btnNewGame != null) _btnNewGame.clicked += OnNewGame;
            if (_btnLoad != null) _btnLoad.clicked += OnLoad;
            if (_btnSettings != null) _btnSettings.clicked += OnSettings;
            if (_btnQuit != null) _btnQuit.clicked += OnQuit;
        }

        void OnDisable()
        {
            if (_btnNewGame != null) _btnNewGame.clicked -= OnNewGame;
            if (_btnLoad != null) _btnLoad.clicked -= OnLoad;
            if (_btnSettings != null) _btnSettings.clicked -= OnSettings;
            if (_btnQuit != null) _btnQuit.clicked -= OnQuit;
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(null);
        }

        void OnNewGame()
        {
            GameStartInfo.SetStartModeNewGame();
            SceneManager.LoadScene(CitySceneBuildIndex);
        }

        void OnLoad()
        {
            string path = ResolveMostRecentSavePath();
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("[MainMenuHost] Load Game requested but no save found.");
                return;
            }
            GameStartInfo.SetPendingLoadPath(path);
            SceneManager.LoadScene(CitySceneBuildIndex);
        }

        void OnSettings()
        {
            Debug.Log("[MainMenuHost] Settings requested — settings-view modal not yet wired.");
        }

        void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        static string ResolveMostRecentSavePath()
        {
            string lastPath = PlayerPrefs.GetString(LastSavePathKey, "");
            if (!string.IsNullOrEmpty(lastPath) && File.Exists(lastPath))
                return lastPath;
            var files = GameSaveManager.GetSaveFiles(Application.persistentDataPath);
            return files.Length > 0 ? files[0].FilePath : null;
        }
    }
}
