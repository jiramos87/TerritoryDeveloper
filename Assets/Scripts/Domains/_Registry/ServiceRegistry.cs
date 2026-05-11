using System;
using System.Collections.Generic;
using UnityEngine;

namespace Domains.Registry
{
    /// <summary>Scene-hosted service locator MonoBehaviour. Register in Awake; Resolve in Start or method body — NEVER in Awake.</summary>
    public class ServiceRegistry : MonoBehaviour, IServiceRegistry
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public void Register<T>(T impl)
        {
            _services[typeof(T)] = impl;
        }

        public T Resolve<T>()
        {
            if (_services.TryGetValue(typeof(T), out var svc))
                return (T)svc;
            Debug.LogWarning($"[ServiceRegistry] No service registered for {typeof(T).Name}");
            return default;
        }
    }
}
