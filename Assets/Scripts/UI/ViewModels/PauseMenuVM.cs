using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Territory.UI.ViewModels
{
    /// <summary>
    /// POCO ViewModel for the pause menu UI Toolkit panel.
    /// Implements INotifyPropertyChanged for UI Toolkit data binding.
    /// Exposes command callbacks resolved by PauseMenuHost at runtime.
    /// </summary>
    public sealed class PauseMenuVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // ── Commands ─────────────────────────────────────────────────────────
        public Action ResumeCommand { get; set; } = () => { };
        public Action SaveCommand { get; set; } = () => { };
        public Action SettingsCommand { get; set; } = () => { };
        public Action ExitCommand { get; set; } = () => { };

        // ── Bindable properties ──────────────────────────────────────────────
        string _titleText = "PAUSED";
        public string TitleText
        {
            get => _titleText;
            set { _titleText = value; OnPropertyChanged(); }
        }

        bool _isVisible;
        public bool IsVisible
        {
            get => _isVisible;
            set { _isVisible = value; OnPropertyChanged(); }
        }

        void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
