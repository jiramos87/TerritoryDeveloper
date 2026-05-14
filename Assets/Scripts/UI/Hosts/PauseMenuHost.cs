using System.Collections.Generic;
using Territory.Audio;
using Territory.Economy;
using Territory.Persistence;
using Territory.UI.Modals;
using Territory.UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// MonoBehaviour Host — resolves PauseMenuVM and sets UIDocument.rootVisualElement.dataSource.
    /// Iter-11 (Effort 1 §16.2) — sub-view nav: Save / Load / Settings swap views inside the
    /// cream card; back-arrow returns to root. Esc on sub-view = back; Esc on root = close.
    /// Iter-16..18 (Effort 1 §16.2 wiring) — Save sub-view writes via GameSaveManager;
    /// Load sub-view renders save file list + restores via SceneManager+GameStartInfo;
    /// Settings sub-view exposes SFX volume slider.
    /// </summary>
    public sealed class PauseMenuHost : MonoBehaviour
    {
        const string ViewRoot     = "root";
        const string ViewSave     = "save";
        const string ViewLoad     = "load";
        const string ViewSettings = "settings";

        const string SfxVolumePrefKey = "SfxVolume";

        [SerializeField] UIDocument _doc;
        [SerializeField] GameSaveManager _saveManager;
        [SerializeField] CityStats _cityStats;

        PauseMenuVM _vm;
        ModalCoordinator _coordinator;

        Button _btnResume, _btnSave, _btnLoad, _btnSettings, _btnExit;
        VisualElement _viewRoot, _viewSave, _viewLoad, _viewSettings;
        Button _btnSaveBack, _btnLoadBack, _btnSettingsBack;

        // Save sub-view.
        TextField _saveNameInput;
        Button _btnSaveConfirm;
        Label _saveStatus;

        // Load sub-view.
        ScrollView _loadList;
        Label _loadStatus;
        Button _btnLoadConfirm;
        readonly List<SaveFileMeta> _slotMetas = new List<SaveFileMeta>();
        int _selectedLoadIndex = -1;

        // Settings sub-view.
        Slider _sfxVolumeSlider;
        Label _sfxVolumeValue;

        string _currentView = ViewRoot;
        public string CurrentView => _currentView;

        void Awake()
        {
            if (_saveManager == null) _saveManager = FindObjectOfType<GameSaveManager>();
            if (_cityStats   == null) _cityStats   = FindObjectOfType<CityStats>();
        }

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

                _saveNameInput   = root.Q<TextField>("save-name-input");
                _btnSaveConfirm  = root.Q<Button>("btn-save-confirm");
                _saveStatus      = root.Q<Label>("save-status");

                _loadList        = root.Q<ScrollView>("load-list");
                _loadStatus      = root.Q<Label>("load-status");
                _btnLoadConfirm  = root.Q<Button>("btn-load-confirm");

                _sfxVolumeSlider = root.Q<Slider>("sfx-volume-slider");
                _sfxVolumeValue  = root.Q<Label>("sfx-volume-value");

                if (_btnResume       != null) _btnResume.clicked       += OnResume;
                if (_btnSave         != null) _btnSave.clicked         += OnSave;
                if (_btnLoad         != null) _btnLoad.clicked         += OnLoad;
                if (_btnSettings     != null) _btnSettings.clicked     += OnSettings;
                if (_btnExit         != null) _btnExit.clicked         += OnExit;
                if (_btnSaveBack     != null) _btnSaveBack.clicked     += OnBack;
                if (_btnLoadBack     != null) _btnLoadBack.clicked     += OnBack;
                if (_btnSettingsBack != null) _btnSettingsBack.clicked += OnBack;
                if (_btnSaveConfirm  != null) _btnSaveConfirm.clicked  += OnSaveConfirm;
                if (_btnLoadConfirm  != null) _btnLoadConfirm.clicked  += OnLoadConfirm;

                if (_sfxVolumeSlider != null)
                    _sfxVolumeSlider.RegisterValueChangedCallback(OnSfxVolumeChanged);

                // iter-23 (Effort 2) — hover + click blips on every pause-menu button.
                BindPauseMenuBlips();

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
            if (_coordinator == null)
            {
                _coordinator = FindObjectOfType<ModalCoordinator>();
                if (_coordinator != null && _doc != null && _doc.rootVisualElement != null)
                    _coordinator.RegisterMigratedPanel("pause-menu", _doc.rootVisualElement);
            }
            if (_saveManager == null) _saveManager = FindObjectOfType<GameSaveManager>();
            if (_cityStats   == null) _cityStats   = FindObjectOfType<CityStats>();
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
            if (_btnSaveConfirm  != null) _btnSaveConfirm.clicked  -= OnSaveConfirm;
            if (_btnLoadConfirm  != null) _btnLoadConfirm.clicked  -= OnLoadConfirm;
            if (_sfxVolumeSlider != null) _sfxVolumeSlider.UnregisterValueChangedCallback(OnSfxVolumeChanged);
            UnbindPauseMenuBlips();
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(null);
        }

        void BindPauseMenuBlips()
        {
            var btns = new Button[]
            {
                _btnResume, _btnSave, _btnLoad, _btnSettings, _btnExit,
                _btnSaveBack, _btnLoadBack, _btnSettingsBack,
                _btnSaveConfirm, _btnLoadConfirm,
            };
            foreach (var b in btns)
                ToolkitBlipBinder.BindClickAndHover(b, BlipId.UiButtonClick, BlipId.UiButtonHover);
        }

        void UnbindPauseMenuBlips()
        {
            var btns = new Button[]
            {
                _btnResume, _btnSave, _btnLoad, _btnSettings, _btnExit,
                _btnSaveBack, _btnLoadBack, _btnSettingsBack,
                _btnSaveConfirm, _btnLoadConfirm,
            };
            foreach (var b in btns) ToolkitBlipBinder.UnbindAll(b);
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

        void OnSave()
        {
            PrepareSaveView();
            SwitchView(ViewSave);
        }

        void OnLoad()
        {
            RefreshLoadList();
            SwitchView(ViewLoad);
        }

        void OnSettings()
        {
            PrepareSettingsView();
            SwitchView(ViewSettings);
        }

        void OnBack() => SwitchView(ViewRoot);

        void OnExit()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        // ── Save sub-view ─────────────────────────────────────────────────────
        void PrepareSaveView()
        {
            if (_saveNameInput != null)
                _saveNameInput.value = BuildDefaultSaveName();
            if (_saveStatus != null) _saveStatus.text = "";
        }

        string BuildDefaultSaveName()
        {
            string city = _cityStats != null && !string.IsNullOrEmpty(_cityStats.cityName) ? _cityStats.cityName : "city";
            // Strip whitespace so the resulting filename is safe across platforms.
            city = city.Replace(' ', '_');
            return $"{city}_{System.DateTime.Now:yyyyMMdd_HHmmss}";
        }

        void OnSaveConfirm()
        {
            if (_saveManager == null)
            {
                if (_saveStatus != null) _saveStatus.text = "Save manager not found in scene.";
                return;
            }
            string name = _saveNameInput != null ? _saveNameInput.value : null;
            if (string.IsNullOrWhiteSpace(name)) name = BuildDefaultSaveName();
            try
            {
                _saveManager.SaveGame(name);
                if (_saveStatus != null) _saveStatus.text = $"Saved as “{name}”.";
            }
            catch (System.Exception ex)
            {
                if (_saveStatus != null) _saveStatus.text = $"Save failed: {ex.Message}";
                Debug.LogError("[PauseMenuHost] SaveGame threw: " + ex);
            }
        }

        // ── Load sub-view ─────────────────────────────────────────────────────
        void RefreshLoadList()
        {
            _slotMetas.Clear();
            _selectedLoadIndex = -1;
            if (_loadList == null) return;
            _loadList.Clear();
            string dir = Application.persistentDataPath;
            var metas = GameSaveManager.GetSaveFiles(dir);
            if (metas == null || metas.Length == 0)
            {
                if (_loadStatus != null) _loadStatus.text = "No saves found.";
                return;
            }
            for (int i = 0; i < metas.Length; i++)
            {
                int idx = i;
                _slotMetas.Add(metas[i]);
                var row = BuildLoadRow(metas[i]);
                row.RegisterCallback<ClickEvent>(_ => OnLoadRowClicked(idx));
                _loadList.Add(row);
            }
            if (_loadStatus != null) _loadStatus.text = "Pick a save then press Load.";
        }

        static VisualElement BuildLoadRow(SaveFileMeta meta)
        {
            var row = new VisualElement();
            row.AddToClassList("pause-menu__load-row");
            var name = new Label(meta.DisplayName);
            name.AddToClassList("pause-menu__load-row-name");
            var date = new Label(meta.SortDate.ToString("yyyy-MM-dd HH:mm"));
            date.AddToClassList("pause-menu__load-row-date");
            row.Add(name);
            row.Add(date);
            return row;
        }

        void OnLoadRowClicked(int idx)
        {
            _selectedLoadIndex = idx;
            for (int i = 0; i < _loadList.contentContainer.childCount; i++)
            {
                var row = _loadList.contentContainer[i];
                row.EnableInClassList("pause-menu__load-row--selected", i == idx);
            }
            if (_loadStatus != null && idx >= 0 && idx < _slotMetas.Count)
                _loadStatus.text = $"Selected: {_slotMetas[idx].DisplayName}";
        }

        void OnLoadConfirm()
        {
            if (_selectedLoadIndex < 0 || _selectedLoadIndex >= _slotMetas.Count)
            {
                if (_loadStatus != null) _loadStatus.text = "Pick a save first.";
                return;
            }
            var meta = _slotMetas[_selectedLoadIndex];
            GameStartInfo.SetPendingLoadPath(meta.FilePath);
            Time.timeScale = 1f;
            // Reload CityScene (build index 1) — boot path consumes pending load.
            UnityEngine.SceneManagement.SceneManager.LoadScene(1);
        }

        // ── Settings sub-view ─────────────────────────────────────────────────
        void PrepareSettingsView()
        {
            float v = PlayerPrefs.GetFloat(SfxVolumePrefKey, 1f);
            if (_sfxVolumeSlider != null) _sfxVolumeSlider.SetValueWithoutNotify(v);
            ApplySfxVolume(v);
            UpdateSfxLabel(v);
        }

        void OnSfxVolumeChanged(ChangeEvent<float> evt)
        {
            float v = Mathf.Clamp01(evt.newValue);
            PlayerPrefs.SetFloat(SfxVolumePrefKey, v);
            ApplySfxVolume(v);
            UpdateSfxLabel(v);
        }

        void UpdateSfxLabel(float v)
        {
            if (_sfxVolumeValue != null) _sfxVolumeValue.text = $"{Mathf.RoundToInt(v * 100f)}%";
        }

        static void ApplySfxVolume(float v)
        {
            // Fallback to AudioListener while AudioMixer / Blip routing isn't auto-resolved here.
            AudioListener.volume = v;
            // Also publish to the Blip pref key so the engine picks it up on next play.
            PlayerPrefs.SetFloat(BlipBootstrap.SfxVolumeDbKey, v);
            PlayerPrefs.Save();
        }

        // ── View switching ────────────────────────────────────────────────────
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
