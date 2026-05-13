using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Territory.UI.ViewModels
{
    /// <summary>
    /// POCO ViewModel for the main-menu UI Toolkit panel.
    /// Implements INotifyPropertyChanged for UI Toolkit data binding.
    /// Exposes nav command callbacks resolved by MainMenuHost at runtime.
    /// </summary>
    public sealed class MainMenuVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // ── Commands ─────────────────────────────────────────────────────────
        public Action NewGameCommand { get; set; } = () => { };
        public Action LoadCommand { get; set; } = () => { };
        public Action SettingsCommand { get; set; } = () => { };
        public Action QuitCommand { get; set; } = () => { };

        // ── Bindable properties ──────────────────────────────────────────────
        string _titleText = "TERRITORY";
        public string TitleText
        {
            get => _titleText;
            set { _titleText = value; OnPropertyChanged(); }
        }

        string _subtitleText = "City Builder";
        public string SubtitleText
        {
            get => _subtitleText;
            set { _subtitleText = value; OnPropertyChanged(); }
        }

        void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
