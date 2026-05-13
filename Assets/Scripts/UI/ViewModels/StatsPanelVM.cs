using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Territory.UI.ViewModels
{
    /// <summary>
    /// Stage 4.0 (TECH-32919) — POCO ViewModel for stats-panel UI Toolkit panel.
    /// Exposes stats categories + tab selection + chart data summary.
    /// Implements INotifyPropertyChanged for UI Toolkit data binding.
    /// </summary>
    public sealed class StatsPanelVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // ── Tab enum ──────────────────────────────────────────────────────────
        public enum StatsTab { Population, Services, Economy }

        // ── Commands ─────────────────────────────────────────────────────────
        public Action CloseCommand { get; set; } = () => { };
        public Action SelectPopulationTab { get; set; } = () => { };
        public Action SelectServicesTab { get; set; } = () => { };
        public Action SelectEconomyTab { get; set; } = () => { };

        // ── Header ────────────────────────────────────────────────────────────
        string _title = "City Stats";
        public string Title
        {
            get => _title;
            set { if (_title == value) return; _title = value; OnPropertyChanged(); }
        }

        // ── Active tab ────────────────────────────────────────────────────────
        StatsTab _activeTab = StatsTab.Population;
        public StatsTab ActiveTab
        {
            get => _activeTab;
            set { if (_activeTab == value) return; _activeTab = value; OnPropertyChanged(); OnPropertyChanged(nameof(ActiveTabLabel)); }
        }

        public string ActiveTabLabel => _activeTab switch
        {
            StatsTab.Population => "Population trends",
            StatsTab.Services => "Service levels",
            StatsTab.Economy => "Economic overview",
            _ => "",
        };

        // ── Chart summary (text description for binding placeholder) ──────────
        string _chartSummary = "No data";
        public string ChartSummary
        {
            get => _chartSummary;
            set { if (_chartSummary == value) return; _chartSummary = value; OnPropertyChanged(); }
        }

        // ── Stacked-bar widths (0–1 normalized) ──────────────────────────────
        float _barPopulationWidth;
        public float BarPopulationWidth
        {
            get => _barPopulationWidth;
            set { if (_barPopulationWidth == value) return; _barPopulationWidth = value; OnPropertyChanged(); }
        }

        float _barServicesWidth;
        public float BarServicesWidth
        {
            get => _barServicesWidth;
            set { if (_barServicesWidth == value) return; _barServicesWidth = value; OnPropertyChanged(); }
        }

        float _barEconomyWidth;
        public float BarEconomyWidth
        {
            get => _barEconomyWidth;
            set { if (_barEconomyWidth == value) return; _barEconomyWidth = value; OnPropertyChanged(); }
        }

        // ── Service rows summary (placeholder) ───────────────────────────────
        string _serviceRows = "";
        public string ServiceRows
        {
            get => _serviceRows;
            set { if (_serviceRows == value) return; _serviceRows = value; OnPropertyChanged(); }
        }

        void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
