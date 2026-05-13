using Territory.UI.Modals;
using Territory.UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// Stage 5.0 (TECH-32922) — MonoBehaviour Host for notifications-toast transient panel.
    /// Manages toast queue, TTL expiry, and z-order via VM.
    /// </summary>
    public sealed class NotificationsToastHost : MonoBehaviour
    {
        [SerializeField] UIDocument _doc;

        NotificationsToastVM _vm;
        ModalCoordinator _coordinator;

        void OnEnable()
        {
            _vm = new NotificationsToastVM();
            _vm.DismissCommand = id => _vm.Dismiss(id);

            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(_vm);
            else
                Debug.LogWarning("[NotificationsToastHost] UIDocument or rootVisualElement null on enable.");
        }

        void OnDisable()
        {
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(null);
        }

        void Update()
        {
            if (_vm == null) return;
            // TTL expiry — remove toasts past their ExpiresAt
            bool anyExpired = false;
            foreach (var t in _vm.Toasts)
            {
                if (t.ExpiresAt > 0f && UnityEngine.Time.time >= t.ExpiresAt)
                    anyExpired = true;
            }
            if (anyExpired)
            {
                var alive = new System.Collections.Generic.List<NotificationsToastVM.Toast>();
                foreach (var t in _vm.Toasts)
                    if (t.ExpiresAt <= 0f || UnityEngine.Time.time < t.ExpiresAt)
                        alive.Add(t);
                _vm.Toasts = alive;
            }
        }

        /// <summary>Push a toast from external callers (UIManager, NotificationService).</summary>
        public void Push(string message, NotificationsToastVM.ToastKind kind = NotificationsToastVM.ToastKind.Info, float ttlSeconds = 4f)
        {
            if (_vm == null) return;
            _vm.Push(message, kind, ttlSeconds);
            // Ensure panel is visible
            if (_coordinator != null)
                _coordinator.Show("notifications-toast");
        }
    }
}
