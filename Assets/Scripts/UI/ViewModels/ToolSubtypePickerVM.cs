using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Territory.UI.ViewModels
{
    /// <summary>
    /// Stage 4.0 (TECH-32921) — POCO ViewModel for tool-subtype-picker UI Toolkit panel.
    /// Exposes ObservableCollection-style list of subtypes + Select command.
    /// Implements INotifyPropertyChanged for UI Toolkit data binding.
    /// </summary>
    public sealed class ToolSubtypePickerVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>Individual tool subtype entry shown in the picker grid.</summary>
        public sealed class ToolSubtype
        {
            public int Id { get; set; }
            public string Label { get; set; } = "";
            public string IconSlug { get; set; } = "";
        }

        // ── Commands ─────────────────────────────────────────────────────────
        public Action CloseCommand { get; set; } = () => { };
        /// <summary>Called by Host with selected ToolSubtype.Id when tile clicked.</summary>
        public Action<int> SelectCommand { get; set; } = _ => { };

        // ── Header ────────────────────────────────────────────────────────────
        string _title = "Select Tool";
        public string Title
        {
            get => _title;
            set { if (_title == value) return; _title = value; OnPropertyChanged(); }
        }

        // ── Subtypes list ─────────────────────────────────────────────────────
        List<ToolSubtype> _subtypes = new List<ToolSubtype>();
        public List<ToolSubtype> Subtypes
        {
            get => _subtypes;
            set { _subtypes = value ?? new List<ToolSubtype>(); OnPropertyChanged(); }
        }

        // ── Selection ─────────────────────────────────────────────────────────
        int _selectedId = -1;
        public int SelectedId
        {
            get => _selectedId;
            set { if (_selectedId == value) return; _selectedId = value; OnPropertyChanged(); }
        }

        void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
