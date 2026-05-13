using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Territory.UI.ViewModels
{
    /// <summary>
    /// Stage 4.0 (TECH-32920) — POCO ViewModel for map-panel UI Toolkit panel.
    /// Exposes pan/zoom + layer toggles.
    /// Implements INotifyPropertyChanged for UI Toolkit data binding.
    /// </summary>
    public sealed class MapPanelVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // ── Commands ─────────────────────────────────────────────────────────
        public Action CloseCommand { get; set; } = () => { };
        public Action ZoomInCommand { get; set; } = () => { };
        public Action ZoomOutCommand { get; set; } = () => { };

        // ── Header ────────────────────────────────────────────────────────────
        string _title = "Map";
        public string Title
        {
            get => _title;
            set { if (_title == value) return; _title = value; OnPropertyChanged(); }
        }

        // ── Minimap label (debug/placeholder) ────────────────────────────────
        string _minimapLabel = "Minimap";
        public string MinimapLabel
        {
            get => _minimapLabel;
            set { if (_minimapLabel == value) return; _minimapLabel = value; OnPropertyChanged(); }
        }

        // ── Layer toggles ─────────────────────────────────────────────────────
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

        // ── Zoom ──────────────────────────────────────────────────────────────
        float _zoom = 1f;
        public float Zoom
        {
            get => _zoom;
            set { if (_zoom == value) return; _zoom = value; OnPropertyChanged(); OnPropertyChanged(nameof(ZoomLabel)); }
        }

        public string ZoomLabel => $"{(int)(_zoom * 100)}%";

        void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
