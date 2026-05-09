using System;
using UnityEngine;

namespace Territory.UI.Registry
{
    /// <summary>
    /// Toggles GameObject.SetActive based on a bool bind id in <see cref="UiBindRegistry"/>.
    /// Wired by the bake handler when params_json.visible_bind is set on a child element.
    /// Subscribes on Awake (not OnEnable) so SetActive(false) does not unsubscribe the binder.
    /// Defaults to hidden when the bind id is not yet seeded so destructive surfaces stay hidden
    /// until intentionally shown.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UiVisibilityBinder : MonoBehaviour
    {
        [SerializeField] private string _bindId;

        private UiBindRegistry _registry;
        private IDisposable _subscription;

        public string BindId => _bindId;

        public void Initialize(string bindId)
        {
            _bindId = bindId;
        }

        private void Awake()
        {
            if (string.IsNullOrEmpty(_bindId)) return;

            _registry = FindObjectOfType<UiBindRegistry>(includeInactive: true);
            if (_registry == null) return;

            _subscription = _registry.Subscribe<bool>(_bindId, Apply);

            bool initial = false;
            try { initial = _registry.Get<bool>(_bindId); }
            catch { /* bindId not yet seeded — stay hidden until first Set. */ }
            Apply(initial);
        }

        private void OnDestroy()
        {
            _subscription?.Dispose();
            _subscription = null;
        }

        private void Apply(bool visible)
        {
            if (gameObject.activeSelf != visible) gameObject.SetActive(visible);
        }
    }
}
