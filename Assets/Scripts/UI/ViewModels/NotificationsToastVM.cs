using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Territory.UI.ViewModels
{
    /// <summary>
    /// Stage 5.0 (TECH-32922) — POCO ViewModel for notifications-toast transient panel.
    /// Exposes list of Toast entries + Dismiss command.
    /// Implements INotifyPropertyChanged for UI Toolkit data binding.
    /// </summary>
    public sealed class NotificationsToastVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>Toast severity level.</summary>
        public enum ToastKind { Info, Warn, Error }

        /// <summary>Individual toast entry.</summary>
        public sealed class Toast
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public string Message { get; set; } = "";
            public ToastKind Kind { get; set; } = ToastKind.Info;
            public float ExpiresAt { get; set; } = -1f; // -1 = no auto-expire
        }

        // ── Commands ─────────────────────────────────────────────────────────
        /// <summary>Called by Host with Toast.Id when dismiss clicked.</summary>
        public Action<string> DismissCommand { get; set; } = _ => { };

        // ── Toast list ────────────────────────────────────────────────────────
        List<Toast> _toasts = new List<Toast>();
        public List<Toast> Toasts
        {
            get => _toasts;
            set { _toasts = value ?? new List<Toast>(); OnPropertyChanged(); }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        /// <summary>Add a toast; notifies binding.</summary>
        public void Push(string message, ToastKind kind = ToastKind.Info, float ttlSeconds = -1f)
        {
            var t = new Toast { Message = message, Kind = kind };
            if (ttlSeconds > 0f) t.ExpiresAt = UnityEngine.Time.time + ttlSeconds;
            var list = new List<Toast>(_toasts) { t };
            Toasts = list;
        }

        /// <summary>Remove toast by id; notifies binding.</summary>
        public void Dismiss(string id)
        {
            var list = new List<Toast>(_toasts);
            list.RemoveAll(t => t.Id == id);
            Toasts = list;
        }

        void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
