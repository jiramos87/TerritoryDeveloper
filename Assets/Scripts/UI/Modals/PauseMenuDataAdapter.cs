using UnityEngine;
using Territory.UI;
using Territory.UI.Themed;

namespace Territory.UI.Modals
{
    /// <summary>
    /// Host-adapter for pause-menu modal (Wave B4 / TECH-27094).
    /// Subscribes pause action buttons; routes Settings/Save/Load to sub-view slot
    /// mounts via <see cref="SlotAnchorResolver"/>. Main-menu + Quit route through
    /// confirm-button (3-second countdown — wired by bake handler Wave A1).
    /// Resume / ESC / backdrop → <see cref="UIManager.ClosePopup"/> + <see cref="ModalCoordinator.Close"/>.
    /// Inv #4: Inspector producer slot + FindObjectOfType fallback.
    /// Inv #3: UiTheme cached in Awake.
    /// </summary>
    public class PauseMenuDataAdapter : MonoBehaviour
    {
        [Header("Producer")]
        [SerializeField] private MainMenuController _mainMenu;
        [SerializeField] private ModalCoordinator _modalCoordinator;

        [Header("Buttons")]
        [SerializeField] private ThemedButton _resumeButton;
        [SerializeField] private ThemedButton _settingsButton;
        [SerializeField] private ThemedButton _saveButton;
        [SerializeField] private ThemedButton _loadButton;

        [Header("Sub-view prefabs (host-adapter slot mounts)")]
        [SerializeField] private GameObject _settingsViewPrefab;
        [SerializeField] private GameObject _saveLoadViewPrefab;

        private Transform _settingsSlot;
        private Transform _saveSlot;
        private Transform _loadSlot;

        private GameObject _currentSubView;
        private string _currentContentScreen = "root";

        private void Awake()
        {
            if (_mainMenu == null)
                _mainMenu = FindObjectOfType<MainMenuController>();
            if (_modalCoordinator == null)
                _modalCoordinator = FindObjectOfType<ModalCoordinator>();

            // Resolve slot anchors under this panel root.
            _settingsSlot = SlotAnchorResolver.ResolveByPanel("settings", transform);
            _saveSlot     = SlotAnchorResolver.ResolveByPanel("save",     transform);
            _loadSlot     = SlotAnchorResolver.ResolveByPanel("load",     transform);
        }

        private void OnEnable()
        {
            if (_resumeButton  != null) _resumeButton.OnClicked  += OnResume;
            if (_settingsButton != null) _settingsButton.OnClicked += OnSettings;
            if (_saveButton    != null) _saveButton.OnClicked    += OnSave;
            if (_loadButton    != null) _loadButton.OnClicked    += OnLoad;

            // Register with ModalCoordinator exclusive group.
            if (_modalCoordinator != null)
                _modalCoordinator.TryOpen("pause-menu");
        }

        private void OnDisable()
        {
            if (_resumeButton  != null) _resumeButton.OnClicked  -= OnResume;
            if (_settingsButton != null) _settingsButton.OnClicked -= OnSettings;
            if (_saveButton    != null) _saveButton.OnClicked    -= OnSave;
            if (_loadButton    != null) _loadButton.OnClicked    -= OnLoad;

            UnmountSubView();

            // Deregister from ModalCoordinator.
            if (_modalCoordinator != null)
                _modalCoordinator.Close("pause-menu");
        }

        // ── Button handlers ───────────────────────────────────────────────────

        private void OnResume()
        {
            if (_mainMenu != null) _mainMenu.ResumeGame();
            if (UIManager.Instance != null) UIManager.Instance.ClosePopup(PopupType.PauseMenu);
        }

        private void OnSettings()
        {
            MountSubView("settings", _settingsSlot, _settingsViewPrefab);
        }

        private void OnSave()
        {
            MountSubView("save", _saveSlot, _saveLoadViewPrefab);
        }

        private void OnLoad()
        {
            MountSubView("load", _loadSlot, _saveLoadViewPrefab);
        }

        // ── Sub-view slot mount ───────────────────────────────────────────────

        private void MountSubView(string screen, Transform slot, GameObject prefab)
        {
            if (_currentContentScreen == screen) return;
            UnmountSubView();

            if (slot == null || prefab == null)
            {
                _currentContentScreen = screen;
                return;
            }

            _currentSubView = Instantiate(prefab, slot, worldPositionStays: false);
            var rt = _currentSubView.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
            }
            _currentContentScreen = screen;
        }

        private void UnmountSubView()
        {
            if (_currentSubView != null)
            {
                Destroy(_currentSubView);
                _currentSubView = null;
            }
            _currentContentScreen = "root";
        }
    }
}
