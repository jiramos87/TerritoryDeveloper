using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Territory.UI.ViewModels
{
    /// <summary>
    /// POCO ViewModel for the settings-view UI Toolkit panel.
    /// Implements INotifyPropertyChanged for UI Toolkit data binding.
    /// Exposes per-setting properties seeded from PlayerPrefs + Apply/Reset/Close commands.
    /// SettingsViewHost integrates with SettingsScreenDataAdapter persistence until Stage 6.0.
    /// </summary>
    public sealed class SettingsViewVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // ── Commands ─────────────────────────────────────────────────────────
        public Action ApplyCommand { get; set; } = () => { };
        public Action ResetCommand { get; set; } = () => { };
        public Action CloseCommand { get; set; } = () => { };

        // ── Audio ─────────────────────────────────────────────────────────────
        float _masterVolume = 1f;
        public float MasterVolume
        {
            get => _masterVolume;
            set { if (_masterVolume == value) return; _masterVolume = value; OnPropertyChanged(); }
        }

        float _musicVolume = 0.8f;
        public float MusicVolume
        {
            get => _musicVolume;
            set { if (_musicVolume == value) return; _musicVolume = value; OnPropertyChanged(); }
        }

        float _sfxVolume = 0.8f;
        public float SfxVolume
        {
            get => _sfxVolume;
            set { if (_sfxVolume == value) return; _sfxVolume = value; OnPropertyChanged(); }
        }

        // ── Video ─────────────────────────────────────────────────────────────
        bool _fullscreen;
        public bool Fullscreen
        {
            get => _fullscreen;
            set { if (_fullscreen == value) return; _fullscreen = value; OnPropertyChanged(); }
        }

        bool _vSync;
        public bool VSync
        {
            get => _vSync;
            set { if (_vSync == value) return; _vSync = value; OnPropertyChanged(); }
        }

        // ── Controls ──────────────────────────────────────────────────────────
        bool _scrollEdgePan = true;
        public bool ScrollEdgePan
        {
            get => _scrollEdgePan;
            set { if (_scrollEdgePan == value) return; _scrollEdgePan = value; OnPropertyChanged(); }
        }

        // ── Gameplay ──────────────────────────────────────────────────────────
        bool _monthlyBudgetNotifications = true;
        public bool MonthlyBudgetNotifications
        {
            get => _monthlyBudgetNotifications;
            set { if (_monthlyBudgetNotifications == value) return; _monthlyBudgetNotifications = value; OnPropertyChanged(); }
        }

        bool _autoSave = true;
        public bool AutoSave
        {
            get => _autoSave;
            set { if (_autoSave == value) return; _autoSave = value; OnPropertyChanged(); }
        }

        void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
