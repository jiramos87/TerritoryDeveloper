using Territory.UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// MonoBehaviour Host — resolves MainMenuVM and sets UIDocument.rootVisualElement.dataSource.
    /// Lives on the UIDocument GameObject in MainMenu scene (sidecar coexistence per Q2).
    /// Legacy Canvas + uGUI panels remain alive until Stage 3.0.5 (Canvas removal).
    /// </summary>
    public sealed class MainMenuHost : MonoBehaviour
    {
        [SerializeField] UIDocument _doc;

        MainMenuVM _vm;

        void OnEnable()
        {
            _vm = new MainMenuVM();
            WireCommands();

            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.dataSource = _vm;
            else
                Debug.LogWarning("[MainMenuHost] UIDocument or rootVisualElement null on enable — check PanelSettings wiring.");
        }

        void OnDisable()
        {
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.dataSource = null;
        }

        void WireCommands()
        {
            _vm.NewGameCommand = OnNewGame;
            _vm.LoadCommand = OnLoad;
            _vm.SettingsCommand = OnSettings;
            _vm.QuitCommand = OnQuit;
        }

        void OnNewGame()
        {
            Debug.Log("[MainMenuHost] New Game requested (stub — wire NewGameFormHost).");
        }

        void OnLoad()
        {
            Debug.Log("[MainMenuHost] Load Game requested (stub — wire SaveLoadViewHost).");
        }

        void OnSettings()
        {
            Debug.Log("[MainMenuHost] Settings requested (stub — wire SettingsViewHost).");
        }

        void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
