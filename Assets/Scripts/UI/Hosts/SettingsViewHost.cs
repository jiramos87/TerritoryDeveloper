using Territory.UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// MonoBehaviour Host — resolves SettingsViewVM and sets UIDocument.rootVisualElement.dataSource.
    /// Lives on the UIDocument GameObject in MainMenu scene (sidecar coexistence per Q2).
    /// Seeds VM from PlayerPrefs on enable; persists on Apply.
    /// Legacy SettingsScreenDataAdapter + SettingsViewController remain alive until Stage 6.0.
    /// </summary>
    public sealed class SettingsViewHost : MonoBehaviour
    {
        // PlayerPrefs keys (mirror SettingsScreenDataAdapter constants).
        const string MasterVolumeKey              = "MasterVolume";
        const string MusicVolumeKey               = "MusicVolumeDb";
        const string SfxVolumeKey                 = "SfxVolumeDb";
        const string FullscreenKey                = "Fullscreen";
        const string VSyncKey                     = "VSync";
        const string ScrollEdgePanKey             = "ScrollEdgePan";
        const string MonthlyBudgetNotifKey        = "MonthlyBudgetNotifications";
        const string AutoSaveKey                  = "AutoSave";

        [SerializeField] UIDocument _doc;

        SettingsViewVM _vm;

        void OnEnable()
        {
            _vm = new SettingsViewVM();
            SeedFromPlayerPrefs();
            WireCommands();

            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(_vm);
            else
                Debug.LogWarning("[SettingsViewHost] UIDocument or rootVisualElement null on enable — check PanelSettings wiring.");
        }

        void OnDisable()
        {
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(null);
        }

        void SeedFromPlayerPrefs()
        {
            _vm.MasterVolume              = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
            _vm.MusicVolume               = PlayerPrefs.GetFloat(MusicVolumeKey,  0.8f);
            _vm.SfxVolume                 = PlayerPrefs.GetFloat(SfxVolumeKey,    0.8f);
            _vm.Fullscreen                = PlayerPrefs.GetInt(FullscreenKey,      0) != 0;
            _vm.VSync                     = PlayerPrefs.GetInt(VSyncKey,           0) != 0;
            _vm.ScrollEdgePan             = PlayerPrefs.GetInt(ScrollEdgePanKey,   1) != 0;
            _vm.MonthlyBudgetNotifications = PlayerPrefs.GetInt(MonthlyBudgetNotifKey, 1) != 0;
            _vm.AutoSave                  = PlayerPrefs.GetInt(AutoSaveKey,        1) != 0;
        }

        void WireCommands()
        {
            _vm.ApplyCommand = OnApply;
            _vm.ResetCommand = OnReset;
            _vm.CloseCommand = OnClose;
        }

        void OnApply()
        {
            PlayerPrefs.SetFloat(MasterVolumeKey,           _vm.MasterVolume);
            PlayerPrefs.SetFloat(MusicVolumeKey,            _vm.MusicVolume);
            PlayerPrefs.SetFloat(SfxVolumeKey,              _vm.SfxVolume);
            PlayerPrefs.SetInt(FullscreenKey,               _vm.Fullscreen ? 1 : 0);
            PlayerPrefs.SetInt(VSyncKey,                    _vm.VSync ? 1 : 0);
            PlayerPrefs.SetInt(ScrollEdgePanKey,            _vm.ScrollEdgePan ? 1 : 0);
            PlayerPrefs.SetInt(MonthlyBudgetNotifKey,       _vm.MonthlyBudgetNotifications ? 1 : 0);
            PlayerPrefs.SetInt(AutoSaveKey,                 _vm.AutoSave ? 1 : 0);
            PlayerPrefs.Save();

            AudioListener.volume = _vm.MasterVolume;
            Screen.fullScreen = _vm.Fullscreen;
            QualitySettings.vSyncCount = _vm.VSync ? 1 : 0;

            Debug.Log("[SettingsViewHost] Settings applied.");
        }

        void OnReset()
        {
            _vm.MasterVolume               = 1f;
            _vm.MusicVolume                = 0.8f;
            _vm.SfxVolume                  = 0.8f;
            _vm.Fullscreen                 = false;
            _vm.VSync                      = false;
            _vm.ScrollEdgePan              = true;
            _vm.MonthlyBudgetNotifications = true;
            _vm.AutoSave                   = true;
        }

        void OnClose()
        {
            gameObject.SetActive(false);
        }
    }
}
