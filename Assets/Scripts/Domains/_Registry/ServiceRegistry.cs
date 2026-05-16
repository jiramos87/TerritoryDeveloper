using System;
using System.Collections.Generic;
using UnityEngine;

namespace Domains.Registry
{
    /// <summary>Thrown when Resolve is called outside a legal phase (Start / method body) — e.g. from Update or LateUpdate.</summary>
    public class CrossRegistryResolveOutsideStartException : Exception
    {
        public CrossRegistryResolveOutsideStartException(string typeName)
            : base($"[ServiceRegistry] Cross-registry Resolve<{typeName}> called outside Start. Resolve is only permitted during Start or explicit method calls — not Update/LateUpdate.") { }
    }

    /// <summary>Scene-hosted service locator MonoBehaviour. Register in Awake; Resolve in Start or method body — NEVER in Awake or Update/LateUpdate.</summary>
    public class ServiceRegistry : MonoBehaviour, IServiceRegistry
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        /// <summary>True while MonoBehaviour.Start is executing for this frame. Callers set this flag to enforce resolve-phase guard.</summary>
        public bool IsStarting { get; set; } = false;

        /// <summary>When true, Resolve throws CrossRegistryResolveOutsideStartException if called outside Start. Default false (opt-in per registry instance).</summary>
        public bool EnforceStartPhase { get; set; } = false;

        public void Register<T>(T impl)
        {
            _services[typeof(T)] = impl;
        }

        public T Resolve<T>()
        {
            if (EnforceStartPhase && !IsStarting)
                throw new CrossRegistryResolveOutsideStartException(typeof(T).Name);

            if (_services.TryGetValue(typeof(T), out var svc))
                return (T)svc;
            Debug.LogWarning($"[ServiceRegistry] No service registered for {typeof(T).Name}");
            return default;
        }
    }
}
