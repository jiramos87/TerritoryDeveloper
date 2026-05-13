using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Territory.UI.ViewModels
{
    /// <summary>
    /// Stage 5.0 (TECH-32923) — POCO ViewModel for mini-map HUD panel.
    /// Exposes map label and cursor coordinates.
    /// Implements INotifyPropertyChanged for UI Toolkit data binding.
    /// </summary>
    public sealed class MiniMapVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        string _mapLabel = "Map";
        public string MapLabel
        {
            get => _mapLabel;
            set { if (_mapLabel == value) return; _mapLabel = value; OnPropertyChanged(); }
        }

        int _cursorX;
        int _cursorY;

        public int CursorX
        {
            get => _cursorX;
            set { if (_cursorX == value) return; _cursorX = value; OnPropertyChanged(); OnPropertyChanged(nameof(CoordsLabel)); }
        }

        public int CursorY
        {
            get => _cursorY;
            set { if (_cursorY == value) return; _cursorY = value; OnPropertyChanged(); OnPropertyChanged(nameof(CoordsLabel)); }
        }

        public string CoordsLabel => $"{_cursorX}, {_cursorY}";

        void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
