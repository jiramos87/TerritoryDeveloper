using System;
using UnityEngine;
using UnityEngine.Audio;
using Territory.Audio;
using Territory.UI.Themed;
using Territory.UI.Registry;

namespace Territory.UI.Modals
{
    /// <summary>
    /// Bridges ThemedSlider/ThemedToggle change events to PlayerPrefs writes and audio/quality apply hooks.
    /// Inspector consumer slots; OnEnable/OnDisable subscription lifecycle via UiBindRegistry/UiActionRegistry.
    /// Wave A2 (TECH-27070): adds SFX slider, monthly-budget-notif toggle, auto-save toggle, Reset-to-defaults.
    /// Volume mapping: <c>LinearToDecibel(percent/100f)</c> for master/music/sfx sliders.
    /// </summary>
    public class SettingsScreenDataAdapter : MonoBehaviour
    {
        // ── PlayerPrefs keys ──────────────────────────────────────────────────
        private const string MasterVolumeKey                 = "MasterVolume";
        private const string MusicVolumeKey                  = "MusicVolumeDb";
        private const string ResolutionIndexKey              = "ResolutionIndex";
        private const string FullscreenKey                   = "Fullscreen";
        private const string VSyncKey                        = "VSync";
        private const string ScrollEdgePanKey                = "ScrollEdgePan";
        public  const string MonthlyBudgetNotificationsKey   = "MonthlyBudgetNotifications";
        public  const string AutoSaveKey                     = "AutoSave";

        // Default values for Reset-to-defaults.
        private const float DefaultMasterVolume  = 1f;
        private const float DefaultMusicVolume   = 0.8f;
        private const float DefaultSfxVolume     = 0.8f;
        private const bool  DefaultFullscreen    = false;
        private const bool  DefaultVSync         = false;
        private const bool  DefaultScrollEdgePan = true;
        private const bool  DefaultMonthlyNotif  = true;
        private const bool  DefaultAutoSave      = true;

        [Header("Audio Mixer (optional — maps volume sliders to decibel channels)")]
        [SerializeField] private AudioMixer _audioMixer;

        [Header("Consumers — Sliders (legacy ThemedSlider path; baked-UI uses bind registry)")]
        [SerializeField] private ThemedSlider _masterVolumeSlider;
        [SerializeField] private ThemedSlider _musicVolumeSlider;
        [SerializeField] private ThemedSlider _sfxVolumeSlider;
        [SerializeField] private ThemedSlider _resolutionSlider;

        [Header("Consumers — Toggles (legacy ThemedToggle path)")]
        [SerializeField] private ThemedToggle _fullscreenToggle;
        [SerializeField] private ThemedToggle _vsyncToggle;
        [SerializeField] private ThemedToggle _scrollEdgeToggle;
        [SerializeField] private ThemedToggle _monthlyBudgetNotifToggle;
        [SerializeField] private ThemedToggle _autoSaveToggle;

        [Header("Registries (optional — resolved via FindObjectOfType if null)")]
        [SerializeField] private UiActionRegistry _actionRegistry;
        [SerializeField] private UiBindRegistry   _bindRegistry;

        // Subscription handles.
        private IDisposable _masterVolSub;
        private IDisposable _musicVolSub;
        private IDisposable _sfxVolSub;
        private IDisposable _resolutionSub;
        private IDisposable _fullscreenSub;
        private IDisposable _vsyncSub;
        private IDisposable _scrollEdgeSub;
        private IDisposable _monthlyNotifSub;
        private IDisposable _autoSaveSub;

        private void Awake()
        {
            if (_actionRegistry == null)
                _actionRegistry = FindObjectOfType<UiActionRegistry>();
            if (_bindRegistry == null)
                _bindRegistry = FindObjectOfType<UiBindRegistry>();
        }

        private void OnEnable()
        {
            // Legacy ThemedSlider/Toggle path.
            if (_masterVolumeSlider != null) _masterVolumeSlider.OnValueChanged += OnMasterVolume;
            if (_musicVolumeSlider  != null) _musicVolumeSlider.OnValueChanged  += OnMusicVolume;
            if (_sfxVolumeSlider    != null) _sfxVolumeSlider.OnValueChanged    += OnSfxVolume;
            if (_resolutionSlider   != null) _resolutionSlider.OnValueChanged   += OnResolution;
            if (_fullscreenToggle   != null) _fullscreenToggle.OnToggled        += OnFullscreen;
            if (_vsyncToggle        != null) _vsyncToggle.OnToggled             += OnVSync;
            if (_scrollEdgeToggle   != null) _scrollEdgeToggle.OnToggled        += OnScrollEdge;
            if (_monthlyBudgetNotifToggle != null) _monthlyBudgetNotifToggle.OnToggled += OnMonthlyNotif;
            if (_autoSaveToggle     != null) _autoSaveToggle.OnToggled          += OnAutoSave;

            // Baked-UI bind registry path.
            if (_bindRegistry != null)
            {
                _masterVolSub   = _bindRegistry.Subscribe<float>("settings.masterVolume",              OnMasterVolume);
                _musicVolSub    = _bindRegistry.Subscribe<float>("settings.musicVolume",               OnMusicVolume);
                _sfxVolSub      = _bindRegistry.Subscribe<float>("settings.sfxVolume",                 OnSfxVolume);
                _resolutionSub  = _bindRegistry.Subscribe<float>("settings.resolution",                OnResolution);
                _fullscreenSub  = _bindRegistry.Subscribe<bool> ("settings.fullscreen",                OnFullscreen);
                _vsyncSub       = _bindRegistry.Subscribe<bool> ("settings.vsync",                     OnVSync);
                _scrollEdgeSub  = _bindRegistry.Subscribe<bool> ("settings.scrollEdgePan",             OnScrollEdge);
                _monthlyNotifSub= _bindRegistry.Subscribe<bool> ("settings.monthlyBudgetNotifications",OnMonthlyNotif);
                _autoSaveSub    = _bindRegistry.Subscribe<bool> ("settings.autoSave",                  OnAutoSave);
            }

            if (_actionRegistry != null)
            {
                _actionRegistry.Register("settings.reset",           _ => OnReset());
                _actionRegistry.Register("settings.reset.confirmed", _ => ApplyReset());
            }

            // Push current PlayerPrefs values into binds so UI reflects persisted state on open.
            PublishPersistedState();
        }

        private void OnDisable()
        {
            if (_masterVolumeSlider != null) _masterVolumeSlider.OnValueChanged -= OnMasterVolume;
            if (_musicVolumeSlider  != null) _musicVolumeSlider.OnValueChanged  -= OnMusicVolume;
            if (_sfxVolumeSlider    != null) _sfxVolumeSlider.OnValueChanged    -= OnSfxVolume;
            if (_resolutionSlider   != null) _resolutionSlider.OnValueChanged   -= OnResolution;
            if (_fullscreenToggle   != null) _fullscreenToggle.OnToggled        -= OnFullscreen;
            if (_vsyncToggle        != null) _vsyncToggle.OnToggled             -= OnVSync;
            if (_scrollEdgeToggle   != null) _scrollEdgeToggle.OnToggled        -= OnScrollEdge;
            if (_monthlyBudgetNotifToggle != null) _monthlyBudgetNotifToggle.OnToggled -= OnMonthlyNotif;
            if (_autoSaveToggle     != null) _autoSaveToggle.OnToggled          -= OnAutoSave;

            _masterVolSub?.Dispose();   _masterVolSub   = null;
            _musicVolSub?.Dispose();    _musicVolSub    = null;
            _sfxVolSub?.Dispose();      _sfxVolSub      = null;
            _resolutionSub?.Dispose();  _resolutionSub  = null;
            _fullscreenSub?.Dispose();  _fullscreenSub  = null;
            _vsyncSub?.Dispose();       _vsyncSub       = null;
            _scrollEdgeSub?.Dispose();  _scrollEdgeSub  = null;
            _monthlyNotifSub?.Dispose();_monthlyNotifSub= null;
            _autoSaveSub?.Dispose();    _autoSaveSub    = null;
        }

        // ── Volume callbacks ──────────────────────────────────────────────────

        private void OnMasterVolume(float value)
        {
            PlayerPrefs.SetFloat(MasterVolumeKey, value);
            AudioListener.volume = value;
            SetMixerChannel("MasterVolume", value);
        }

        private void OnMusicVolume(float value)
        {
            PlayerPrefs.SetFloat(MusicVolumeKey, value);
            SetMixerChannel("MusicVolume", value);
        }

        private void OnSfxVolume(float value)
        {
            PlayerPrefs.SetFloat(BlipBootstrap.SfxVolumeDbKey, value);
            SetMixerChannel("SFXVolume", value);
        }

        private void OnResolution(float value)
        {
            int index = Mathf.RoundToInt(value);
            PlayerPrefs.SetInt(ResolutionIndexKey, index);
            var resolutions = Screen.resolutions;
            if (index >= 0 && index < resolutions.Length)
                Screen.SetResolution(resolutions[index].width, resolutions[index].height, Screen.fullScreen);
        }

        // ── Toggle callbacks ──────────────────────────────────────────────────

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

        private void OnMonthlyNotif(bool value)
        {
            PlayerPrefs.SetInt(MonthlyBudgetNotificationsKey, value ? 1 : 0);
        }

        private void OnAutoSave(bool value)
        {
            PlayerPrefs.SetInt(AutoSaveKey, value ? 1 : 0);
        }

        // ── Reset-to-defaults ─────────────────────────────────────────────────

        /// <summary>Trigger confirm-button countdown via confirm_action; actual reset in ApplyReset.</summary>
        private void OnReset()
        {
            // Baked confirm-button handles the countdown; fires settings.reset.confirmed on completion.
            Debug.Log("[SettingsScreenDataAdapter] Reset requested — awaiting confirmation.");
        }

        /// <summary>Apply default values to all settings + persist + push binds.</summary>
        private void ApplyReset()
        {
            OnMasterVolume(DefaultMasterVolume);
            OnMusicVolume(DefaultMusicVolume);
            OnSfxVolume(DefaultSfxVolume);
            OnFullscreen(DefaultFullscreen);
            OnVSync(DefaultVSync);
            OnScrollEdge(DefaultScrollEdgePan);
            OnMonthlyNotif(DefaultMonthlyNotif);
            OnAutoSave(DefaultAutoSave);
            PlayerPrefs.Save();

            // Push reset values into bind registry so UI sliders/toggles reflect defaults.
            if (_bindRegistry != null)
            {
                _bindRegistry.Set("settings.masterVolume",               DefaultMasterVolume);
                _bindRegistry.Set("settings.musicVolume",                DefaultMusicVolume);
                _bindRegistry.Set("settings.sfxVolume",                  DefaultSfxVolume);
                _bindRegistry.Set("settings.fullscreen",                 DefaultFullscreen);
                _bindRegistry.Set("settings.vsync",                      DefaultVSync);
                _bindRegistry.Set("settings.scrollEdgePan",              DefaultScrollEdgePan);
                _bindRegistry.Set("settings.monthlyBudgetNotifications", DefaultMonthlyNotif);
                _bindRegistry.Set("settings.autoSave",                   DefaultAutoSave);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Map 0..1 linear percent to decibel and set AudioMixer parameter.
        /// LinearToDecibel: dB = 20 * log10(max(percent, 0.0001)).
        /// No-op when <see cref="_audioMixer"/> is null or param not found.
        /// </summary>
        private void SetMixerChannel(string paramName, float linearPercent)
        {
            if (_audioMixer == null) return;
            float db = LinearToDecibel(linearPercent);
            _audioMixer.SetFloat(paramName, db);
        }

        private static float LinearToDecibel(float linear)
        {
            return 20f * Mathf.Log10(Mathf.Max(linear, 0.0001f));
        }

        /// <summary>Push persisted PlayerPrefs state into bind registry on panel open.</summary>
        private void PublishPersistedState()
        {
            if (_bindRegistry == null) return;
            _bindRegistry.Set("settings.masterVolume",               PlayerPrefs.GetFloat(MasterVolumeKey, DefaultMasterVolume));
            _bindRegistry.Set("settings.musicVolume",                PlayerPrefs.GetFloat(MusicVolumeKey,  DefaultMusicVolume));
            _bindRegistry.Set("settings.sfxVolume",                  PlayerPrefs.GetFloat(BlipBootstrap.SfxVolumeDbKey, DefaultSfxVolume));
            _bindRegistry.Set("settings.fullscreen",                 PlayerPrefs.GetInt(FullscreenKey,           DefaultFullscreen    ? 1 : 0) != 0);
            _bindRegistry.Set("settings.vsync",                      PlayerPrefs.GetInt(VSyncKey,                DefaultVSync         ? 1 : 0) != 0);
            _bindRegistry.Set("settings.scrollEdgePan",              PlayerPrefs.GetInt(ScrollEdgePanKey,        DefaultScrollEdgePan ? 1 : 0) != 0);
            _bindRegistry.Set("settings.monthlyBudgetNotifications", PlayerPrefs.GetInt(MonthlyBudgetNotificationsKey, DefaultMonthlyNotif ? 1 : 0) != 0);
            _bindRegistry.Set("settings.autoSave",                   PlayerPrefs.GetInt(AutoSaveKey,             DefaultAutoSave      ? 1 : 0) != 0);
        }
    }
}
