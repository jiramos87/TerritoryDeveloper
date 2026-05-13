using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Territory.UI.ViewModels
{
    /// <summary>
    /// Stage 5.0 (TECH-32925) — POCO ViewModel for growth-budget-panel modal.
    /// Exposes infrastructure + services budget sliders.
    /// Implements INotifyPropertyChanged for UI Toolkit data binding.
    /// </summary>
    public sealed class GrowthBudgetPanelVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // ── Commands ─────────────────────────────────────────────────────────
        public Action CloseCommand { get; set; } = () => { };
        public Action ApplyCommand { get; set; } = () => { };
        public Action CancelCommand { get; set; } = () => { };

        // ── Header ────────────────────────────────────────────────────────────
        string _title = "Growth Budget";
        public string Title
        {
            get => _title;
            set { if (_title == value) return; _title = value; OnPropertyChanged(); }
        }

        string _budgetSummary = "";
        public string BudgetSummary
        {
            get => _budgetSummary;
            set { if (_budgetSummary == value) return; _budgetSummary = value; OnPropertyChanged(); }
        }

        // ── Infrastructure ────────────────────────────────────────────────────
        float _infrastructureBudget = 50f;
        public float InfrastructureBudget
        {
            get => _infrastructureBudget;
            set { if (_infrastructureBudget == value) return; _infrastructureBudget = value; OnPropertyChanged(); OnPropertyChanged(nameof(InfrastructureDisplay)); }
        }

        public string InfrastructureDisplay => $"{(int)_infrastructureBudget}%";

        // ── Services ──────────────────────────────────────────────────────────
        float _servicesBudget = 50f;
        public float ServicesBudget
        {
            get => _servicesBudget;
            set { if (_servicesBudget == value) return; _servicesBudget = value; OnPropertyChanged(); OnPropertyChanged(nameof(ServicesDisplay)); }
        }

        public string ServicesDisplay => $"{(int)_servicesBudget}%";

        void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
