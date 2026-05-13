using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Territory.UI.ViewModels
{
    /// <summary>
    /// POCO ViewModel for the save-load-view UI Toolkit panel.
    /// Implements INotifyPropertyChanged for UI Toolkit data binding.
    /// Exposes ObservableCollection of save slot display strings + Load/Save/Delete/Cancel commands.
    /// Thumbnail loading handled lazily by SaveLoadViewHost off main thread.
    /// </summary>
    public sealed class SaveLoadViewVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // ── Commands ─────────────────────────────────────────────────────────
        public Action LoadCommand { get; set; } = () => { };
        public Action SaveCommand { get; set; } = () => { };
        public Action DeleteCommand { get; set; } = () => { };
        public Action CancelCommand { get; set; } = () => { };
        public Action SelectLoadTabCommand { get; set; } = () => { };
        public Action SelectSaveTabCommand { get; set; } = () => { };

        // ── Bindable properties ──────────────────────────────────────────────
        string _titleText = "Save / Load";
        public string TitleText
        {
            get => _titleText;
            set { if (_titleText == value) return; _titleText = value; OnPropertyChanged(); }
        }

        string _mode = "load";
        public string Mode
        {
            get => _mode;
            set { if (_mode == value) return; _mode = value; OnPropertyChanged(); }
        }

        int _selectedIndex = -1;
        public int SelectedIndex
        {
            get => _selectedIndex;
            set { if (_selectedIndex == value) return; _selectedIndex = value; OnPropertyChanged(); }
        }

        // ── Save slot list ────────────────────────────────────────────────────
        readonly ObservableCollection<string> _slots = new ObservableCollection<string>();
        public ObservableCollection<string> Slots => _slots;

        void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
