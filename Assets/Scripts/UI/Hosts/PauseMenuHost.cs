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
    /// Iter-11 (Effort 1 §16.2) — sub-view nav: Save / Load / Settings swap views inside the
    /// cream card; back-arrow returns to root. Esc on sub-view = back; Esc on root = close.
    /// </summary>
    public sealed class PauseMenuHost : MonoBehaviour
    {
        // View slugs.
        const string ViewRoot     = "root";
        const string ViewSave     = "save";
        const string ViewLoad     = "load";
        const string ViewSettings = "settings";

        [SerializeField] UIDocument _doc;

        PauseMenuVM _vm;
        ModalCoordinator _coordinator;

        // Root view buttons.
        Button _btnResume, _btnSave, _btnLoad, _btnSettings, _btnExit;

        // Sub-views + back buttons.
        VisualElement _viewRoot, _viewSave, _viewLoad, _viewSettings;
        Button _btnSaveBack, _btnLoadBack, _btnSettingsBack;
        Button _btnSaveNew;

        // Current visible view (one of: root|save|load|settings).
        string _currentView = ViewRoot;

        public string CurrentView => _currentView;

        void OnEnable()
        {
            _vm = new PauseMenuVM();
            WireCommands();

            if (_doc != null && _doc.rootVisualElement != null)
            {
                var root = _doc.rootVisualElement;
                _doc.rootVisualElement.SetCompatDataSource(_vm);

                _btnResume   = root.Q<Button>("btn-resume");
                _btnSave     = root.Q<Button>("btn-save");
                _btnLoad     = root.Q<Button>("btn-load");
                _btnSettings = root.Q<Button>("btn-settings");
                _btnExit     = root.Q<Button>("btn-exit");

                _viewRoot     = root.Q<VisualElement>("root-view");
                _viewSave     = root.Q<VisualElement>("save-view");
                _viewLoad     = root.Q<VisualElement>("load-view");
                _viewSettings = root.Q<VisualElement>("settings-view");

                _btnSaveBack     = root.Q<Button>("btn-save-back");
                _btnLoadBack     = root.Q<Button>("btn-load-back");
                _btnSettingsBack = root.Q<Button>("btn-settings-back");
                _btnSaveNew      = root.Q<Button>("btn-save-new");

                if (_btnResume       != null) _btnResume.clicked       += OnResume;
                if (_btnSave         != null) _btnSave.clicked         += OnSave;
                if (_btnLoad         != null) _btnLoad.clicked         += OnLoad;
                if (_btnSettings     != null) _btnSettings.clicked     += OnSettings;
                if (_btnExit         != null) _btnExit.clicked         += OnExit;
                if (_btnSaveBack     != null) _btnSaveBack.clicked     += OnBack;
                if (_btnLoadBack     != null) _btnLoadBack.clicked     += OnBack;
                if (_btnSettingsBack != null) _btnSettingsBack.clicked += OnBack;
                if (_btnSaveNew      != null) _btnSaveNew.clicked      += OnSaveNew;

                // Start at root view (defensive — UXML defaults to display:none on sub-views).
                SwitchView(ViewRoot);
            }
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
            if (_btnResume       != null) _btnResume.clicked       -= OnResume;
            if (_btnSave         != null) _btnSave.clicked         -= OnSave;
            if (_btnLoad         != null) _btnLoad.clicked         -= OnLoad;
            if (_btnSettings     != null) _btnSettings.clicked     -= OnSettings;
            if (_btnExit         != null) _btnExit.clicked         -= OnExit;
            if (_btnSaveBack     != null) _btnSaveBack.clicked     -= OnBack;
            if (_btnLoadBack     != null) _btnLoadBack.clicked     -= OnBack;
            if (_btnSettingsBack != null) _btnSettingsBack.clicked -= OnBack;
            if (_btnSaveNew      != null) _btnSaveNew.clicked      -= OnSaveNew;
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(null);
        }

        void WireCommands()
        {
            _vm.ResumeCommand   = OnResume;
            _vm.SaveCommand     = OnSave;
            _vm.SettingsCommand = OnSettings;
            _vm.ExitCommand     = OnExit;
        }

        void OnResume()
        {
            SwitchView(ViewRoot);
            if (_coordinator != null)
                _coordinator.HideMigrated("pause-menu");
            Time.timeScale = 1f;
        }

        void OnSave()     => SwitchView(ViewSave);
        void OnLoad()     => SwitchView(ViewLoad);
        void OnSettings() => SwitchView(ViewSettings);
        void OnBack()     => SwitchView(ViewRoot);

        void OnSaveNew()
        {
            Debug.Log("[PauseMenuHost] New save requested (stub — wire SaveManager.SaveCurrentSlot).");
        }

        void OnExit()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        void SwitchView(string view)
        {
            _currentView = view ?? ViewRoot;
            SetViewDisplay(_viewRoot,     _currentView == ViewRoot);
            SetViewDisplay(_viewSave,     _currentView == ViewSave);
            SetViewDisplay(_viewLoad,     _currentView == ViewLoad);
            SetViewDisplay(_viewSettings, _currentView == ViewSettings);
        }

        static void SetViewDisplay(VisualElement ve, bool show)
        {
            if (ve == null) return;
            ve.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>
        /// Esc routing hook — UIManager.HandleEscapePress calls this before defaulting
        /// to ModalCoordinator.Close("pause-menu"). Returns true when consumed
        /// (sub-view → root); false when root is already visible (= bubble to close).
        /// </summary>
        public bool TryHandleBackButton()
        {
            if (_currentView != ViewRoot)
            {
                SwitchView(ViewRoot);
                return true;
            }
            return false;
        }
    }
}
