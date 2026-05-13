using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Territory.UI.ViewModels
{
    /// <summary>
    /// Stage 4.0 (TECH-32917) — POCO ViewModel for budget-panel UI Toolkit panel.
    /// Exposes budget categories + Apply/Cancel commands.
    /// Implements INotifyPropertyChanged for UI Toolkit data binding.
    /// </summary>
    public sealed class BudgetPanelVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // ── Commands ─────────────────────────────────────────────────────────
        public Action ApplyCommand { get; set; } = () => { };
        public Action CancelCommand { get; set; } = () => { };
        public Action CloseCommand { get; set; } = () => { };

        // ── Header ────────────────────────────────────────────────────────────
        string _title = "City Budget";
        public string Title
        {
            get => _title;
            set { if (_title == value) return; _title = value; OnPropertyChanged(); }
        }

        // ── Treasury ──────────────────────────────────────────────────────────
        string _treasury = "$0";
        public string Treasury
        {
            get => _treasury;
            set { if (_treasury == value) return; _treasury = value; OnPropertyChanged(); }
        }

        // ── Tax sliders ───────────────────────────────────────────────────────
        float _taxResidential;
        public float TaxResidential
        {
            get => _taxResidential;
            set { if (_taxResidential == value) return; _taxResidential = value; OnPropertyChanged(); OnPropertyChanged(nameof(TaxResidentialDisplay)); }
        }

        public string TaxResidentialDisplay => $"{(int)_taxResidential}%";

        float _taxCommercial;
        public float TaxCommercial
        {
            get => _taxCommercial;
            set { if (_taxCommercial == value) return; _taxCommercial = value; OnPropertyChanged(); OnPropertyChanged(nameof(TaxCommercialDisplay)); }
        }

        public string TaxCommercialDisplay => $"{(int)_taxCommercial}%";

        float _taxIndustrial;
        public float TaxIndustrial
        {
            get => _taxIndustrial;
            set { if (_taxIndustrial == value) return; _taxIndustrial = value; OnPropertyChanged(); OnPropertyChanged(nameof(TaxIndustrialDisplay)); }
        }

        public string TaxIndustrialDisplay => $"{(int)_taxIndustrial}%";

        // ── Forecast ──────────────────────────────────────────────────────────
        string _forecastBalance = "$0";
        public string ForecastBalance
        {
            get => _forecastBalance;
            set { if (_forecastBalance == value) return; _forecastBalance = value; OnPropertyChanged(); }
        }

        void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
