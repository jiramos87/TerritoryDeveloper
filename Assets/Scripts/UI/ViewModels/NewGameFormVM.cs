using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Territory.UI.ViewModels
{
    /// <summary>
    /// POCO ViewModel for the new-game-form UI Toolkit panel.
    /// Implements INotifyPropertyChanged for UI Toolkit data binding.
    /// Exposes city name, map size, budget, seed + Submit/Cancel commands.
    /// </summary>
    public sealed class NewGameFormVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // ── Commands ─────────────────────────────────────────────────────────
        public Action SubmitCommand { get; set; } = () => { };
        public Action CancelCommand { get; set; } = () => { };

        // ── Bindable form fields ─────────────────────────────────────────────
        string _cityName = string.Empty;
        public string CityName
        {
            get => _cityName;
            set { if (_cityName == value) return; _cityName = value; OnPropertyChanged(); }
        }

        string _mapSize = "medium";
        public string MapSize
        {
            get => _mapSize;
            set { if (_mapSize == value) return; _mapSize = value; OnPropertyChanged(); }
        }

        string _budget = "medium";
        public string Budget
        {
            get => _budget;
            set { if (_budget == value) return; _budget = value; OnPropertyChanged(); }
        }

        int _seed;
        public int Seed
        {
            get => _seed;
            set { if (_seed == value) return; _seed = value; OnPropertyChanged(); }
        }

        void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
