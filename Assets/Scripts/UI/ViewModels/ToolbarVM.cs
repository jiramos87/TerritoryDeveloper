using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Territory.UI.ViewModels
{
    /// <summary>
    /// Stage 5.0 (TECH-32923) — POCO ViewModel for toolbar HUD panel.
    /// Exposes tool list + ActiveTool selection.
    /// Implements INotifyPropertyChanged for UI Toolkit data binding.
    /// </summary>
    public sealed class ToolbarVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>Individual tool entry shown in the toolbar strip.</summary>
        public sealed class ToolEntry
        {
            public string Id { get; set; } = "";
            public string Label { get; set; } = "";
            public string IconSlug { get; set; } = "";
        }

        // ── Commands ─────────────────────────────────────────────────────────
        public Action<string> SelectToolCommand { get; set; } = _ => { };

        // ── Tool list ─────────────────────────────────────────────────────────
        List<ToolEntry> _tools = new List<ToolEntry>();
        public List<ToolEntry> Tools
        {
            get => _tools;
            set { _tools = value ?? new List<ToolEntry>(); OnPropertyChanged(); }
        }

        // ── Selection ─────────────────────────────────────────────────────────
        string _activeTool = "";
        public string ActiveTool
        {
            get => _activeTool;
            set { if (_activeTool == value) return; _activeTool = value; OnPropertyChanged(); }
        }

        // ── Alias for binding SelectedTool ─────────────────────────────────────
        public string SelectedTool => _activeTool;

        void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
