using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Territory.UI.ViewModels
{
    /// <summary>
    /// Stage 5.0 (TECH-32925) — POCO ViewModel for load-view fullscreen panel.
    /// Exposes loading label + progress + status text.
    /// Implements INotifyPropertyChanged for UI Toolkit data binding.
    /// </summary>
    public sealed class LoadViewVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        string _loadingLabel = "Loading...";
        public string LoadingLabel
        {
            get => _loadingLabel;
            set { if (_loadingLabel == value) return; _loadingLabel = value; OnPropertyChanged(); }
        }

        float _progress;
        public float Progress
        {
            get => _progress;
            set { if (_progress == value) return; _progress = value; OnPropertyChanged(); }
        }

        string _statusText = "";
        public string StatusText
        {
            get => _statusText;
            set { if (_statusText == value) return; _statusText = value; OnPropertyChanged(); }
        }

        void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
