using System;
using System.Collections.Generic;
using UnityEngine;

namespace Territory.UI.Registry
{
    /// <summary>
    /// Wave A0 (TECH-27059) — shell-only reactive bind table.
    /// Typed Set/Get/Subscribe per bind id.
    /// MonoBehaviour; mount under UI host GameObject per scene (MainMenu.unity, CityScene.unity — T2.0.5+).
    /// </summary>
    /// <remarks>TECH-32927 Stage 6.0 — Quarantined. Use native UI Toolkit binding (INotifyValueChanged / ChangeEvent) instead.</remarks>
    [Obsolete("UiBindRegistry quarantined (TECH-32927). Migrate to native UI Toolkit INotifyValueChanged / ChangeEvent bindings. Deletion deferred to uGUI purge plan.")]
    public class UiBindRegistry : MonoBehaviour
    {
        private readonly Dictionary<string, object> _values = new Dictionary<string, object>();
        private readonly Dictionary<string, List<Action<object>>> _subscribers =
            new Dictionary<string, List<Action<object>>>();

        /// <summary>Set bind value; notifies subscribers.</summary>
        [Obsolete("UiBindRegistry.Set quarantined (TECH-32927). Use native UI Toolkit binding.")]
        public void Set<T>(string bindId, T value)
        {
            if (string.IsNullOrEmpty(bindId)) throw new ArgumentNullException(nameof(bindId));
            _values[bindId] = value;
            if (_subscribers.TryGetValue(bindId, out var list))
            {
                foreach (var cb in list) cb(value);
            }
        }

        /// <summary>Get current bind value. Throws KeyNotFoundException when bindId not set.</summary>
        [Obsolete("UiBindRegistry.Get quarantined (TECH-32927). Use native UI Toolkit binding.")]
        public T Get<T>(string bindId)
        {
            if (!_values.TryGetValue(bindId, out var raw))
                throw new KeyNotFoundException($"UiBindRegistry: bindId '{bindId}' not set.");
            return (T)raw;
        }

        /// <summary>Subscribe to bind changes. Returns IDisposable to unsubscribe.</summary>
        [Obsolete("UiBindRegistry.Subscribe quarantined (TECH-32927). Use native UI Toolkit binding.")]
        public IDisposable Subscribe<T>(string bindId, Action<T> onChange)
        {
            if (string.IsNullOrEmpty(bindId)) throw new ArgumentNullException(nameof(bindId));
            if (onChange == null) throw new ArgumentNullException(nameof(onChange));

            if (!_subscribers.TryGetValue(bindId, out var list))
            {
                list = new List<Action<object>>();
                _subscribers[bindId] = list;
            }

            Action<object> wrapped = obj => onChange((T)obj);
            list.Add(wrapped);

            return new Subscription(() => list.Remove(wrapped));
        }

        /// <summary>Returns true when at least one subscriber is registered for <paramref name="bindId"/>.</summary>
        [Obsolete("UiBindRegistry.HasSubscribers quarantined (TECH-32927). Use native UI Toolkit binding.")]
        public bool HasSubscribers(string bindId)
        {
            return !string.IsNullOrEmpty(bindId)
                && _subscribers.TryGetValue(bindId, out var list)
                && list.Count > 0;
        }

        /// <summary>Returns snapshot of all bind ids with current values.</summary>
        [Obsolete("UiBindRegistry.ListRegistered quarantined (TECH-32927). Use native UI Toolkit binding.")]
        public IReadOnlyList<string> ListRegistered()
        {
            return new List<string>(_values.Keys);
        }

        private sealed class Subscription : IDisposable
        {
            private readonly Action _dispose;
            public Subscription(Action dispose) => _dispose = dispose;
            public void Dispose() => _dispose();
        }
    }
}
