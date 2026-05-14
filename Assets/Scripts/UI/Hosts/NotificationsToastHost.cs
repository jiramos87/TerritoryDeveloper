using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// Effort 3 (post iter-25) — top-right toast surface for GameNotificationManager.
    /// Programmatic VisualElement stack; click-to-dismiss; auto-expire via schedule.Execute.
    /// Static Instance accessor lets GameNotificationManager route Post* calls.
    /// </summary>
    public sealed class NotificationsToastHost : MonoBehaviour
    {
        public enum ToastKind { Info, Success, Warning, Error }

        const int MaxVisible = 5;
        const float DefaultDurationSeconds = 3f;

        public static NotificationsToastHost Instance { get; private set; }

        [SerializeField] UIDocument _doc;

        VisualElement _stack;
        readonly List<VisualElement> _live = new List<VisualElement>();

        void OnEnable()
        {
            if (Instance == null) Instance = this;

            if (_doc == null) _doc = GetComponent<UIDocument>();
            var root = _doc != null ? _doc.rootVisualElement : null;
            if (root == null)
            {
                Debug.LogWarning("[NotificationsToastHost] UIDocument or rootVisualElement null on enable.");
                return;
            }
            _stack = root.Q<VisualElement>("toast-stack");
            if (_stack == null)
            {
                Debug.LogWarning("[NotificationsToastHost] toast-stack VisualElement not found in UXML.");
            }
        }

        void OnDisable()
        {
            if (Instance == this) Instance = null;
            if (_stack != null) _stack.Clear();
            _live.Clear();
        }

        /// <summary>Public API — push one toast onto the stack.</summary>
        public void Post(string message, ToastKind kind, float durationSeconds)
        {
            if (string.IsNullOrEmpty(message) || _stack == null) return;
            if (durationSeconds <= 0f) durationSeconds = DefaultDurationSeconds;

            // Evict oldest when over cap.
            while (_live.Count >= MaxVisible)
            {
                var oldest = _live[0];
                _live.RemoveAt(0);
                if (oldest.parent != null) oldest.RemoveFromHierarchy();
            }

            var card = BuildCard(message, kind);
            _stack.Add(card);
            _live.Add(card);

            // Auto-remove after duration.
            long delayMs = (long)(durationSeconds * 1000f);
            card.schedule.Execute(() => RemoveCard(card)).StartingIn(delayMs);

            // Click-to-dismiss (anywhere on the card).
            card.RegisterCallback<ClickEvent>(_ => RemoveCard(card));
        }

        void RemoveCard(VisualElement card)
        {
            if (card == null) return;
            if (card.parent != null) card.RemoveFromHierarchy();
            _live.Remove(card);
        }

        VisualElement BuildCard(string message, ToastKind kind)
        {
            var card = new VisualElement();
            card.AddToClassList("notifications-toast__card");
            card.AddToClassList("notifications-toast__card--" + KindClass(kind));

            var icon = new Label(KindIcon(kind));
            icon.AddToClassList("notifications-toast__icon");
            icon.AddToClassList("notifications-toast__icon--" + KindClass(kind));
            card.Add(icon);

            var text = new Label(message);
            text.AddToClassList("notifications-toast__text");
            card.Add(text);

            return card;
        }

        static string KindClass(ToastKind k)
        {
            switch (k)
            {
                case ToastKind.Success: return "success";
                case ToastKind.Warning: return "warning";
                case ToastKind.Error:   return "error";
                default:                return "info";
            }
        }

        static string KindIcon(ToastKind k)
        {
            // Glyphs render via -unity-font (built-in default font supports these).
            switch (k)
            {
                case ToastKind.Success: return "✓";
                case ToastKind.Warning: return "!";
                case ToastKind.Error:   return "✕";
                default:                return "i";
            }
        }
    }
}
