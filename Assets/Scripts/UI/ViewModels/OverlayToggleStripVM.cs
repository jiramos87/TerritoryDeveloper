using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Territory.UI.ViewModels
{
    /// <summary>
    /// Stage 5.0 (TECH-32923) — POCO ViewModel for overlay-toggle-strip HUD panel.
    /// Exposes per-layer visibility toggles.
    /// Implements INotifyPropertyChanged for UI Toolkit data binding.
    /// </summary>
    public sealed class OverlayToggleStripVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        bool _showTerrain = true;
        public bool ShowTerrain
        {
            get => _showTerrain;
            set { if (_showTerrain == value) return; _showTerrain = value; OnPropertyChanged(); }
        }

        bool _showZones = true;
        public bool ShowZones
        {
            get => _showZones;
            set { if (_showZones == value) return; _showZones = value; OnPropertyChanged(); }
        }

        bool _showRoads = true;
        public bool ShowRoads
        {
            get => _showRoads;
            set { if (_showRoads == value) return; _showRoads = value; OnPropertyChanged(); }
        }

        bool _showWater = true;
        public bool ShowWater
        {
            get => _showWater;
            set { if (_showWater == value) return; _showWater = value; OnPropertyChanged(); }
        }

        bool _showGrid;
        public bool ShowGrid
        {
            get => _showGrid;
            set { if (_showGrid == value) return; _showGrid = value; OnPropertyChanged(); }
        }

        void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
