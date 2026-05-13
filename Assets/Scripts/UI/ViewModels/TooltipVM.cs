using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Territory.UI.ViewModels
{
    /// <summary>
    /// Stage 5.0 (TECH-32924) — POCO ViewModel for tooltip popover panel.
    /// Exposes Label (title) + Text (body) + position.
    /// Implements INotifyPropertyChanged for UI Toolkit data binding.
    /// </summary>
    public sealed class TooltipVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        string _label = "";
        public string Label
        {
            get => _label;
            set { if (_label == value) return; _label = value; OnPropertyChanged(); }
        }

        string _text = "";
        public string Text
        {
            get => _text;
            set { if (_text == value) return; _text = value; OnPropertyChanged(); }
        }

        float _posX;
        public float PositionX
        {
            get => _posX;
            set { if (_posX == value) return; _posX = value; OnPropertyChanged(); }
        }

        float _posY;
        public float PositionY
        {
            get => _posY;
            set { if (_posY == value) return; _posY = value; OnPropertyChanged(); }
        }

        void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
