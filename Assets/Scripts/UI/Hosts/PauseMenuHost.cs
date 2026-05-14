using Territory.UI.Modals;
using Territory.UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// MonoBehaviour Host — resolves PauseMenuVM and sets UIDocument.rootVisualElement.dataSource.
    /// Lives on the UIDocument GameObject added in CityScene (sidecar coexistence per Q2).
    /// Iter-5 — registers with ModalCoordinator so Esc routing via UIManager.HandleEscapePress
    /// flips display state. Panel content starts hidden (RegisterMigratedPanel default).
    /// </summary>
    public sealed class PauseMenuHost : MonoBehaviour
    {
        [SerializeField] UIDocument _doc;

        PauseMenuVM _vm;
        ModalCoordinator _coordinator;

        void OnEnable()
        {
            _vm = new PauseMenuVM();
            WireCommands();

            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(_vm);
            else
                Debug.LogWarning("[PauseMenuHost] UIDocument or rootVisualElement null on enable — check PanelSettings wiring.");

            _coordinator = FindObjectOfType<ModalCoordinator>();
            if (_coordinator != null && _doc != null && _doc.rootVisualElement != null)
                _coordinator.RegisterMigratedPanel("pause-menu", _doc.rootVisualElement);
        }
        void Start()
        {
            // Iter-7: retry registration when ModalCoordinator is created
            // by UIManager.Start (runs after Host.OnEnable).
            if (_coordinator == null)
            {
                _coordinator = FindObjectOfType<ModalCoordinator>();
                if (_coordinator != null && _doc != null && _doc.rootVisualElement != null)
                    _coordinator.RegisterMigratedPanel("pause-menu", _doc.rootVisualElement);
            }
        }


        void OnDisable()
        {
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(null);
        }

        void WireCommands()
        {
            _vm.ResumeCommand = OnResume;
            _vm.SaveCommand = OnSave;
            _vm.SettingsCommand = OnSettings;
            _vm.ExitCommand = OnExit;
        }

        void OnResume()
        {
            if (_coordinator != null)
                _coordinator.HideMigrated("pause-menu");
            Time.timeScale = 1f;
        }

        void OnSave()
        {
            Debug.Log("[PauseMenuHost] Save requested (stub — wire SaveManager).");
        }

        void OnSettings()
        {
            Debug.Log("[PauseMenuHost] Settings requested (stub — wire SettingsController).");
        }

        void OnExit()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }
}
