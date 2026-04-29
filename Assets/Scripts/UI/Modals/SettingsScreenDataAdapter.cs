using UnityEngine;
using Territory.UI.Themed;

namespace Territory.UI.Modals
{
    /// <summary>
    /// Bridges ThemedSlider/ThemedToggle change events to PlayerPrefs writes and legacy
    /// audio/quality apply hooks. Inspector consumer slots with UiTheme cached in Awake
    /// (invariant #3); OnEnable/OnDisable subscription lifecycle.
    /// </summary>
    public class SettingsScreenDataAdapter : MonoBehaviour
    {
        private const string MasterVolumeKey = "MasterVolume";
        private const string MusicVolumeKey = "MusicVolumeDb";
        private const string ResolutionIndexKey = "ResolutionIndex";
        private const string FullscreenKey = "Fullscreen";
        private const string VSyncKey = "VSync";
        private const string ScrollEdgePanKey = "ScrollEdgePan";

        [Header("Consumers — Tabs")]
        [SerializeField] private ThemedTabBar _tabBar;

        [Header("Consumers — Sliders")]
        [SerializeField] private ThemedSlider _masterVolumeSlider;
        [SerializeField] private ThemedSlider _musicVolumeSlider;
        [SerializeField] private ThemedSlider _sfxVolumeSlider;
        [SerializeField] private ThemedSlider _resolutionSlider;

        [Header("Consumers — Toggles")]
        [SerializeField] private ThemedToggle _fullscreenToggle;
        [SerializeField] private ThemedToggle _vsyncToggle;
        [SerializeField] private ThemedToggle _scrollEdgeToggle;

        private void Awake()
        {
        }

        private void OnEnable()
        {
            if (_masterVolumeSlider != null) _masterVolumeSlider.OnValueChanged += OnMasterVolume;
            if (_musicVolumeSlider != null) _musicVolumeSlider.OnValueChanged += OnMusicVolume;
            if (_sfxVolumeSlider != null) _sfxVolumeSlider.OnValueChanged += OnSfxVolume;
            if (_resolutionSlider != null) _resolutionSlider.OnValueChanged += OnResolution;
            if (_fullscreenToggle != null) _fullscreenToggle.OnToggled += OnFullscreen;
            if (_vsyncToggle != null) _vsyncToggle.OnToggled += OnVSync;
            if (_scrollEdgeToggle != null) _scrollEdgeToggle.OnToggled += OnScrollEdge;
        }

        private void OnDisable()
        {
            if (_masterVolumeSlider != null) _masterVolumeSlider.OnValueChanged -= OnMasterVolume;
            if (_musicVolumeSlider != null) _musicVolumeSlider.OnValueChanged -= OnMusicVolume;
            if (_sfxVolumeSlider != null) _sfxVolumeSlider.OnValueChanged -= OnSfxVolume;
            if (_resolutionSlider != null) _resolutionSlider.OnValueChanged -= OnResolution;
            if (_fullscreenToggle != null) _fullscreenToggle.OnToggled -= OnFullscreen;
            if (_vsyncToggle != null) _vsyncToggle.OnToggled -= OnVSync;
            if (_scrollEdgeToggle != null) _scrollEdgeToggle.OnToggled -= OnScrollEdge;
        }

        private void OnMasterVolume(float value)
        {
            PlayerPrefs.SetFloat(MasterVolumeKey, value);
            AudioListener.volume = value;
        }

        private void OnMusicVolume(float value)
        {
            PlayerPrefs.SetFloat(MusicVolumeKey, value);
        }

        private void OnSfxVolume(float value)
        {
            PlayerPrefs.SetFloat(BlipBootstrap.SfxVolumeDbKey, value);
        }

        private void OnResolution(float value)
        {
            int index = Mathf.RoundToInt(value);
            PlayerPrefs.SetInt(ResolutionIndexKey, index);
            var resolutions = Screen.resolutions;
            if (index >= 0 && index < resolutions.Length)
                Screen.SetResolution(resolutions[index].width, resolutions[index].height, Screen.fullScreen);
        }

        private void OnFullscreen(bool value)
        {
            PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
            Screen.fullScreen = value;
        }

        private void OnVSync(bool value)
        {
            PlayerPrefs.SetInt(VSyncKey, value ? 1 : 0);
            QualitySettings.vSyncCount = value ? 1 : 0;
        }

        private void OnScrollEdge(bool value)
        {
            PlayerPrefs.SetInt(ScrollEdgePanKey, value ? 1 : 0);
        }
    }
}
