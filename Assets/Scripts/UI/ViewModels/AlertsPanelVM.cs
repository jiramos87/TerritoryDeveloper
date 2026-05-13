using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Territory.UI.ViewModels
{
    /// <summary>
    /// Stage 5.0 (TECH-32924) — POCO ViewModel for alerts-panel modal.
    /// Exposes persistent alert list + clear command.
    /// Implements INotifyPropertyChanged for UI Toolkit data binding.
    /// </summary>
    public sealed class AlertsPanelVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public enum AlertSeverity { Info, Warn, Error }

        public sealed class Alert
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public string Message { get; set; } = "";
            public AlertSeverity Severity { get; set; } = AlertSeverity.Warn;
        }

        // ── Commands ─────────────────────────────────────────────────────────
        public Action CloseCommand { get; set; } = () => { };
        public Action ClearAllCommand { get; set; } = () => { };

        // ── Header ────────────────────────────────────────────────────────────
        string _title = "Alerts";
        public string Title
        {
            get => _title;
            set { if (_title == value) return; _title = value; OnPropertyChanged(); }
        }

        // ── Alerts ────────────────────────────────────────────────────────────
        List<Alert> _alerts = new List<Alert>();
        public List<Alert> Alerts
        {
            get => _alerts;
            set { _alerts = value ?? new List<Alert>(); OnPropertyChanged(); OnPropertyChanged(nameof(CountLabel)); }
        }

        public string CountLabel => _alerts.Count > 0 ? _alerts.Count.ToString() : "";

        void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
