using System;
using System.Collections.Generic;
using UnityEngine;

namespace Territory.UI.Registry
{
    /// <summary>
    /// Wave A0 (TECH-27059) — shell-only action dispatch table.
    /// Register named action handlers; Dispatch routes payload to first matching handler.
    /// MonoBehaviour; mount under UI host GameObject per scene (MainMenu.unity, CityScene.unity — T2.0.5+).
    /// </summary>
    public class UiActionRegistry : MonoBehaviour
    {
        private readonly Dictionary<string, Action<object>> _handlers =
            new Dictionary<string, Action<object>>();

        /// <summary>Register handler for actionId. Replaces existing handler.</summary>
        public void Register(string actionId, Action<object> handler)
        {
            if (string.IsNullOrEmpty(actionId)) throw new ArgumentNullException(nameof(actionId));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            _handlers[actionId] = handler;
        }

        /// <summary>Dispatch payload to registered handler. Returns false when actionId not found.</summary>
        public bool Dispatch(string actionId, object payload)
        {
            if (_handlers.TryGetValue(actionId, out var handler))
            {
                handler(payload);
                return true;
            }
            return false;
        }

        /// <summary>Returns snapshot of all registered action ids.</summary>
        public IReadOnlyList<string> ListRegistered()
        {
            return new List<string>(_handlers.Keys);
        }
    }
}
