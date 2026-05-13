using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Territory.UI.ViewModels
{
    /// <summary>
    /// POCO ViewModel for the hud-bar UI Toolkit panel.
    /// Implements INotifyPropertyChanged for UI Toolkit native dataBindingPath wiring.
    /// Pumped directly by HudBarHost from CityStats / EconomyManager / TimeManager producers.
    /// </summary>
    public sealed class HudBarVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // ── Money ─────────────────────────────────────────────────────────────
        string _money = "$0";
        public string Money
        {
            get => _money;
            set { if (_money == value) return; _money = value; OnPropertyChanged(); }
        }

        // ── Budget delta (monthly income delta) ───────────────────────────────
        string _budgetDelta = "+0";
        public string BudgetDelta
        {
            get => _budgetDelta;
            set { if (_budgetDelta == value) return; _budgetDelta = value; OnPropertyChanged(); }
        }

        // ── City name ─────────────────────────────────────────────────────────
        string _cityName = "—";
        public string CityName
        {
            get => _cityName;
            set { if (_cityName == value) return; _cityName = value; OnPropertyChanged(); }
        }

        // ── Date ──────────────────────────────────────────────────────────────
        string _date = "";
        public string Date
        {
            get => _date;
            set { if (_date == value) return; _date = value; OnPropertyChanged(); }
        }

        // ── Weather ───────────────────────────────────────────────────────────
        string _weather = "";
        public string Weather
        {
            get => _weather;
            set { if (_weather == value) return; _weather = value; OnPropertyChanged(); }
        }

        // ── Happiness (0–100) ─────────────────────────────────────────────────
        string _happiness = "50";
        public string Happiness
        {
            get => _happiness;
            set { if (_happiness == value) return; _happiness = value; OnPropertyChanged(); }
        }

        // ── Population (city-stats) ───────────────────────────────────────────
        string _population = "Pop 0";
        public string Population
        {
            get => _population;
            set { if (_population == value) return; _population = value; OnPropertyChanged(); }
        }

        // ── Surplus caption ("Est. monthly surplus: Δ +$0") ───────────────────
        string _surplus = "Est. monthly surplus: Δ +$0";
        public string Surplus
        {
            get => _surplus;
            set { if (_surplus == value) return; _surplus = value; OnPropertyChanged(); }
        }

        // ── AUTO toggle visual state ──────────────────────────────────────────
        bool _autoMode;
        public bool AutoMode
        {
            get => _autoMode;
            set { if (_autoMode == value) return; _autoMode = value; OnPropertyChanged(); }
        }

        void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
