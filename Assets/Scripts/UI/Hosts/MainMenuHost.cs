using System.Collections.Generic;
using System.IO;
using Territory.Audio;
using Territory.Persistence;
using Territory.UI.ViewModels;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// MonoBehaviour Host for the main-menu UI Toolkit panel. Wires nav buttons to
    /// scene transitions (parity with legacy MainMenuController) and exposes Load +
    /// Settings sub-views inside the same screen (iter-25, mirror of pause-menu).
    /// Lives on the UIDocument GameObject in MainMenu scene.
    /// </summary>
    public sealed class MainMenuHost : MonoBehaviour
    {
        const int CitySceneBuildIndex = 1;
        const string LastSavePathKey = "LastSavePath";
        const string SfxVolumePrefKey = "SfxVolume";

        // View slugs.
        const string ViewRoot     = "root";
        const string ViewLoad     = "load";
        const string ViewSettings = "settings";

        [SerializeField] UIDocument _doc;

        MainMenuVM _vm;

        // Root view buttons.
        Button _btnContinue, _btnNewGame, _btnLoad, _btnSettings, _btnQuit;
        Label _footerRight;

        // Sub-views + back buttons.
        VisualElement _viewRoot, _viewLoad, _viewSettings;
        Button _btnLoadBack, _btnSettingsBack;

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

        void OnEnable()
        {
            _vm = new MainMenuVM();
            _vm.NewGameCommand  = OnNewGame;
            _vm.LoadCommand     = OnLoad;
            _vm.SettingsCommand = OnSettings;
            _vm.QuitCommand     = OnQuit;

            if (_doc == null || _doc.rootVisualElement == null)
            {
                Debug.LogWarning("[MainMenuHost] UIDocument or rootVisualElement null on enable — check PanelSettings wiring.");
                return;
            }

            var root = _doc.rootVisualElement;
            root.style.position = Position.Absolute;
            root.style.top = 0;
            root.style.left = 0;
            root.style.right = 0;
            root.style.bottom = 0;
            root.pickingMode = PickingMode.Ignore;
            root.SetCompatDataSource(_vm);

            _btnContinue = root.Q<Button>("btn-continue");
            _btnNewGame  = root.Q<Button>("btn-new-game");
            _btnLoad     = root.Q<Button>("btn-load");
            _btnSettings = root.Q<Button>("btn-settings");
            _btnQuit     = root.Q<Button>("btn-quit");
            _footerRight = root.Q<Label>("main-menu-footer-right");

            _viewRoot     = root.Q<VisualElement>("root-view");
            _viewLoad     = root.Q<VisualElement>("load-view");
            _viewSettings = root.Q<VisualElement>("settings-view");

            _btnLoadBack     = root.Q<Button>("btn-load-back");
            _btnSettingsBack = root.Q<Button>("btn-settings-back");

            _loadList       = root.Q<ScrollView>("load-list");
            _loadStatus     = root.Q<Label>("load-status");
            _btnLoadConfirm = root.Q<Button>("btn-load-confirm");

            _sfxVolumeSlider = root.Q<Slider>("sfx-volume-slider");
            _sfxVolumeValue  = root.Q<Label>("sfx-volume-value");

            if (_btnContinue != null) _btnContinue.clicked += OnContinue;
            if (_btnNewGame  != null) _btnNewGame.clicked  += OnNewGame;
            if (_btnLoad     != null) _btnLoad.clicked     += OnLoad;
            if (_btnSettings != null) _btnSettings.clicked += OnSettings;
            if (_btnQuit     != null) _btnQuit.clicked     += OnQuit;

            if (_btnLoadBack     != null) _btnLoadBack.clicked     += OnBack;
            if (_btnSettingsBack != null) _btnSettingsBack.clicked += OnBack;
            if (_btnLoadConfirm  != null) _btnLoadConfirm.clicked  += OnLoadConfirm;

            if (_sfxVolumeSlider != null)
                _sfxVolumeSlider.RegisterValueChangedCallback(OnSfxVolumeChanged);

            BindMainMenuBlips();
            SwitchView(ViewRoot);

            // Continue is only meaningful when a recent save exists.
            if (_btnContinue != null && string.IsNullOrEmpty(ResolveMostRecentSavePath()))
                _btnContinue.AddToClassList("hidden");

            if (_footerRight != null) _footerRight.text = Application.version;
        }

        void OnDisable()
        {
            if (_btnContinue != null) _btnContinue.clicked -= OnContinue;
            if (_btnNewGame  != null) _btnNewGame.clicked  -= OnNewGame;
            if (_btnLoad     != null) _btnLoad.clicked     -= OnLoad;
            if (_btnSettings != null) _btnSettings.clicked -= OnSettings;
            if (_btnQuit     != null) _btnQuit.clicked     -= OnQuit;
            if (_btnLoadBack     != null) _btnLoadBack.clicked     -= OnBack;
            if (_btnSettingsBack != null) _btnSettingsBack.clicked -= OnBack;
            if (_btnLoadConfirm  != null) _btnLoadConfirm.clicked  -= OnLoadConfirm;
            if (_sfxVolumeSlider != null) _sfxVolumeSlider.UnregisterValueChangedCallback(OnSfxVolumeChanged);
            UnbindMainMenuBlips();
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(null);
        }

        // ── Root-view actions ─────────────────────────────────────────────────
        void OnContinue()
        {
            string path = ResolveMostRecentSavePath();
            if (string.IsNullOrEmpty(path)) return;
            GameStartInfo.SetPendingLoadPath(path);
            SceneManager.LoadScene(CitySceneBuildIndex);
        }

        void OnNewGame()
        {
            GameStartInfo.SetStartModeNewGame();
            SceneManager.LoadScene(CitySceneBuildIndex);
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

        void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void OnBack() => SwitchView(ViewRoot);

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
            row.AddToClassList("main-menu__load-row");
            var name = new Label(meta.DisplayName);
            name.AddToClassList("main-menu__load-row-name");
            var date = new Label(meta.SortDate.ToString("yyyy-MM-dd HH:mm"));
            date.AddToClassList("main-menu__load-row-date");
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
                row.EnableInClassList("main-menu__load-row--selected", i == idx);
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
            SceneManager.LoadScene(CitySceneBuildIndex);
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
            AudioListener.volume = v;
            PlayerPrefs.SetFloat(BlipBootstrap.SfxVolumeDbKey, v);
            PlayerPrefs.Save();
        }

        // ── View switching ────────────────────────────────────────────────────
        void SwitchView(string view)
        {
            _currentView = view ?? ViewRoot;
            SetViewDisplay(_viewRoot,     _currentView == ViewRoot);
            SetViewDisplay(_viewLoad,     _currentView == ViewLoad);
            SetViewDisplay(_viewSettings, _currentView == ViewSettings);
        }

        static void SetViewDisplay(VisualElement ve, bool show)
        {
            if (ve == null) return;
            ve.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // ── Blip wiring ───────────────────────────────────────────────────────
        void BindMainMenuBlips()
        {
            var btns = new[]
            {
                _btnContinue, _btnNewGame, _btnLoad, _btnSettings, _btnQuit,
                _btnLoadBack, _btnSettingsBack, _btnLoadConfirm,
            };
            foreach (var b in btns)
                ToolkitBlipBinder.BindClickAndHover(b, BlipId.UiButtonClick, BlipId.UiButtonHover);
        }

        void UnbindMainMenuBlips()
        {
            var btns = new[]
            {
                _btnContinue, _btnNewGame, _btnLoad, _btnSettings, _btnQuit,
                _btnLoadBack, _btnSettingsBack, _btnLoadConfirm,
            };
            foreach (var b in btns) ToolkitBlipBinder.UnbindAll(b);
        }

        // ── Save resolution ───────────────────────────────────────────────────
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
