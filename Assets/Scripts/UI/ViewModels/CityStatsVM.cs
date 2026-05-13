using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Territory.UI.ViewModels
{
    /// <summary>
    /// Stage 5.0 (TECH-32925) — POCO ViewModel for city-stats HUD panel.
    /// Exposes population, happiness, funds, day.
    /// Implements INotifyPropertyChanged for UI Toolkit data binding.
    /// </summary>
    public sealed class CityStatsVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        int _population;
        public int Population
        {
            get => _population;
            set { if (_population == value) return; _population = value; OnPropertyChanged(); }
        }

        int _happiness;
        public int Happiness
        {
            get => _happiness;
            set { if (_happiness == value) return; _happiness = value; OnPropertyChanged(); }
        }

        int _funds;
        public int Funds
        {
            get => _funds;
            set { if (_funds == value) return; _funds = value; OnPropertyChanged(); }
        }

        int _day = 1;
        public int Day
        {
            get => _day;
            set { if (_day == value) return; _day = value; OnPropertyChanged(); }
        }

        void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
