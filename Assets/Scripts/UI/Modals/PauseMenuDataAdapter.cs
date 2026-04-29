using UnityEngine;
using Territory.UI;
using Territory.UI.Themed;

namespace Territory.UI.Modals
{
    /// <summary>
    /// Bridges six ThemedButton click events to <see cref="MainMenuController"/> pause
    /// actions. Inspector producer slot with FindObjectOfType fallback (invariant #4);
    /// UiTheme cached in Awake (invariant #3); OnEnable/OnDisable subscription lifecycle.
    /// </summary>
    public class PauseMenuDataAdapter : MonoBehaviour
    {
        [Header("Producer")]
        [SerializeField] private MainMenuController _mainMenu;

        [Header("Consumers")]
        [SerializeField] private ThemedButton _resumeButton;
        [SerializeField] private ThemedButton _settingsButton;
        [SerializeField] private ThemedButton _saveButton;
        [SerializeField] private ThemedButton _loadButton;
        [SerializeField] private ThemedButton _mainMenuButton;
        [SerializeField] private ThemedButton _quitButton;

        private void Awake()
        {
            if (_mainMenu == null)
                _mainMenu = FindObjectOfType<MainMenuController>();
        }

        private void OnEnable()
        {
            if (_resumeButton != null) _resumeButton.OnClicked += OnResume;
            if (_settingsButton != null) _settingsButton.OnClicked += OnSettings;
            if (_saveButton != null) _saveButton.OnClicked += OnSave;
            if (_loadButton != null) _loadButton.OnClicked += OnLoad;
            if (_mainMenuButton != null) _mainMenuButton.OnClicked += OnMainMenu;
            if (_quitButton != null) _quitButton.OnClicked += OnQuit;
        }

        private void OnDisable()
        {
            if (_resumeButton != null) _resumeButton.OnClicked -= OnResume;
            if (_settingsButton != null) _settingsButton.OnClicked -= OnSettings;
            if (_saveButton != null) _saveButton.OnClicked -= OnSave;
            if (_loadButton != null) _loadButton.OnClicked -= OnLoad;
            if (_mainMenuButton != null) _mainMenuButton.OnClicked -= OnMainMenu;
            if (_quitButton != null) _quitButton.OnClicked -= OnQuit;
        }

        private void OnResume()
        {
            if (_mainMenu != null) _mainMenu.ResumeGame();
            if (UIManager.Instance != null) UIManager.Instance.ClosePopup(PopupType.PauseMenu);
        }
        private void OnSettings() { if (_mainMenu != null) _mainMenu.OpenSettings(); }
        private void OnSave() { if (UIManager.Instance != null) UIManager.Instance.OpenPopup(PopupType.SaveLoadScreen); }
        private void OnLoad() { if (UIManager.Instance != null) UIManager.Instance.OpenPopup(PopupType.SaveLoadScreen); }
        private void OnMainMenu() { if (_mainMenu != null) _mainMenu.ReturnToMainMenu(); }
        private void OnQuit() { if (_mainMenu != null) _mainMenu.QuitGame(); }
    }
}
