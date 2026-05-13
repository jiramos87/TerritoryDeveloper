using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Territory.UI.ViewModels
{
    /// <summary>
    /// Stage 5.0 (TECH-32924) — POCO ViewModel for building-info modal.
    /// Exposes building name, type, level, residents, condition + demolish command.
    /// Implements INotifyPropertyChanged for UI Toolkit data binding.
    /// </summary>
    public sealed class BuildingInfoVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // ── Commands ─────────────────────────────────────────────────────────
        public Action CloseCommand { get; set; } = () => { };
        public Action DemolishCommand { get; set; } = () => { };

        // ── Building identity ─────────────────────────────────────────────────
        string _buildingName = "Building";
        public string BuildingName
        {
            get => _buildingName;
            set { if (_buildingName == value) return; _buildingName = value; OnPropertyChanged(); }
        }

        string _buildingType = "";
        public string BuildingType
        {
            get => _buildingType;
            set { if (_buildingType == value) return; _buildingType = value; OnPropertyChanged(); }
        }

        // ── Stats ─────────────────────────────────────────────────────────────
        int _level = 1;
        public int Level
        {
            get => _level;
            set { if (_level == value) return; _level = value; OnPropertyChanged(); }
        }

        int _residents;
        public int Residents
        {
            get => _residents;
            set { if (_residents == value) return; _residents = value; OnPropertyChanged(); }
        }

        string _condition = "Good";
        public string Condition
        {
            get => _condition;
            set { if (_condition == value) return; _condition = value; OnPropertyChanged(); }
        }

        void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
