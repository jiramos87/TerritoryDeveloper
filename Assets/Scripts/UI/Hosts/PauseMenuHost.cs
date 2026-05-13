using Territory.UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// MonoBehaviour Host — resolves PauseMenuVM and sets UIDocument.rootVisualElement.dataSource.
    /// Lives on the UIDocument GameObject added in CityScene (sidecar coexistence per Q2).
    /// Legacy Canvas + PauseMenuDataAdapter remain alive until Stage 6.0 quarantine plan.
    /// </summary>
    public sealed class PauseMenuHost : MonoBehaviour
    {
        [SerializeField] UIDocument _doc;

        PauseMenuVM _vm;

        void OnEnable()
        {
            _vm = new PauseMenuVM();
            WireCommands();

            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.dataSource = _vm;
            else
                Debug.LogWarning("[PauseMenuHost] UIDocument or rootVisualElement null on enable — check PanelSettings wiring.");
        }

        void OnDisable()
        {
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.dataSource = null;
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
            gameObject.SetActive(false);
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
