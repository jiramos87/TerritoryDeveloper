using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Territory.UI.ViewModels
{
    /// <summary>
    /// Stage 4.0 (TECH-32918) — POCO ViewModel for info-panel UI Toolkit panel.
    /// Exposes selection context properties (cell coord, entity type, field list).
    /// Implements INotifyPropertyChanged for UI Toolkit data binding.
    /// </summary>
    public sealed class InfoPanelVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // ── Commands ─────────────────────────────────────────────────────────
        public Action CloseCommand { get; set; } = () => { };
        public Action DemolishCommand { get; set; } = () => { };

        // ── Header ────────────────────────────────────────────────────────────
        string _title = "Selection Info";
        public string Title
        {
            get => _title;
            set { if (_title == value) return; _title = value; OnPropertyChanged(); }
        }

        // ── Selection context ─────────────────────────────────────────────────
        string _cellCoord = "";
        public string CellCoord
        {
            get => _cellCoord;
            set { if (_cellCoord == value) return; _cellCoord = value; OnPropertyChanged(); }
        }

        string _entityType = "";
        public string EntityType
        {
            get => _entityType;
            set { if (_entityType == value) return; _entityType = value; OnPropertyChanged(); }
        }

        // ── Detail fields (plain text summary for binding placeholder) ────────
        string _fields = "";
        public string Fields
        {
            get => _fields;
            set { if (_fields == value) return; _fields = value; OnPropertyChanged(); }
        }

        /// <summary>Push selection context into VM properties.</summary>
        public void SetSelection(int x, int y, string entityType, string fieldsSummary = "")
        {
            CellCoord = $"({x}, {y})";
            EntityType = entityType ?? "";
            Fields = fieldsSummary ?? "";
            Title = string.IsNullOrEmpty(entityType) ? "Selection Info" : entityType;
        }

        void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
