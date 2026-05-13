using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Territory.UI.ViewModels
{
    /// <summary>
    /// Stage 5.0 (TECH-32923) — POCO ViewModel for time-controls HUD panel.
    /// Exposes TimeSpeed + Paused state + speed-set commands.
    /// Implements INotifyPropertyChanged for UI Toolkit data binding.
    /// </summary>
    public sealed class TimeControlsVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // ── Commands ─────────────────────────────────────────────────────────
        public Action PauseCommand { get; set; } = () => { };
        public Action SetSpeed1Command { get; set; } = () => { };
        public Action SetSpeed2Command { get; set; } = () => { };
        public Action SetSpeed3Command { get; set; } = () => { };

        // ── State ─────────────────────────────────────────────────────────────
        bool _paused;
        public bool Paused
        {
            get => _paused;
            set { if (_paused == value) return; _paused = value; OnPropertyChanged(); OnPropertyChanged(nameof(SpeedLabel)); }
        }

        int _timeSpeed = 1;
        public int TimeSpeed
        {
            get => _timeSpeed;
            set { if (_timeSpeed == value) return; _timeSpeed = value; OnPropertyChanged(); OnPropertyChanged(nameof(SpeedLabel)); }
        }

        public string SpeedLabel => _paused ? "PAUSED" : $"{_timeSpeed}x";

        void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
