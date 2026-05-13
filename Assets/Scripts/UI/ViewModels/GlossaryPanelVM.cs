using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Territory.UI.ViewModels
{
    /// <summary>
    /// Stage 5.0 (TECH-32924) — POCO ViewModel for glossary-panel modal.
    /// Exposes searchable term list + selected term detail.
    /// Implements INotifyPropertyChanged for UI Toolkit data binding.
    /// </summary>
    public sealed class GlossaryPanelVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public sealed class GlossaryTerm
        {
            public string Name { get; set; } = "";
            public string Definition { get; set; } = "";
        }

        // ── Commands ─────────────────────────────────────────────────────────
        public Action CloseCommand { get; set; } = () => { };

        // ── Header ────────────────────────────────────────────────────────────
        string _title = "Glossary";
        public string Title
        {
            get => _title;
            set { if (_title == value) return; _title = value; OnPropertyChanged(); }
        }

        // ── Search ────────────────────────────────────────────────────────────
        string _searchQuery = "";
        public string SearchQuery
        {
            get => _searchQuery;
            set { if (_searchQuery == value) return; _searchQuery = value; OnPropertyChanged(); RefreshFilter(); }
        }

        // ── Terms ─────────────────────────────────────────────────────────────
        List<GlossaryTerm> _allTerms = new List<GlossaryTerm>();
        List<GlossaryTerm> _filteredTerms = new List<GlossaryTerm>();

        public List<GlossaryTerm> FilteredTerms
        {
            get => _filteredTerms;
            private set { _filteredTerms = value; OnPropertyChanged(); }
        }

        public void SetTerms(List<GlossaryTerm> terms)
        {
            _allTerms = terms ?? new List<GlossaryTerm>();
            RefreshFilter();
        }

        void RefreshFilter()
        {
            if (string.IsNullOrWhiteSpace(_searchQuery))
            {
                FilteredTerms = new List<GlossaryTerm>(_allTerms);
                return;
            }
            var q = _searchQuery.ToLowerInvariant();
            var filtered = new List<GlossaryTerm>();
            foreach (var t in _allTerms)
                if (t.Name.ToLowerInvariant().Contains(q) || t.Definition.ToLowerInvariant().Contains(q))
                    filtered.Add(t);
            FilteredTerms = filtered;
        }

        // ── Selection ─────────────────────────────────────────────────────────
        string _selectedTerm = "";
        public string SelectedTerm
        {
            get => _selectedTerm;
            set { if (_selectedTerm == value) return; _selectedTerm = value; OnPropertyChanged(); }
        }

        string _selectedDefinition = "";
        public string SelectedDefinition
        {
            get => _selectedDefinition;
            set { if (_selectedDefinition == value) return; _selectedDefinition = value; OnPropertyChanged(); }
        }

        void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
