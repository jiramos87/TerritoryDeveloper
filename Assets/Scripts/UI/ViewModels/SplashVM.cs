using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Territory.UI.ViewModels
{
    /// <summary>
    /// Stage 5.0 (TECH-32925) — POCO ViewModel for splash fullscreen panel.
    /// Exposes studio label, game title, version.
    /// Implements INotifyPropertyChanged for UI Toolkit data binding.
    /// </summary>
    public sealed class SplashVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        string _studioLabel = "BACAYO STUDIO";
        public string StudioLabel
        {
            get => _studioLabel;
            set { if (_studioLabel == value) return; _studioLabel = value; OnPropertyChanged(); }
        }

        string _gameTitle = "Territory";
        public string GameTitle
        {
            get => _gameTitle;
            set { if (_gameTitle == value) return; _gameTitle = value; OnPropertyChanged(); }
        }

        string _versionLabel = "";
        public string VersionLabel
        {
            get => _versionLabel;
            set { if (_versionLabel == value) return; _versionLabel = value; OnPropertyChanged(); }
        }

        void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
