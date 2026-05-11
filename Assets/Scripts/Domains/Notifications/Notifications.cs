using UnityEngine;
using Domains.Registry;

namespace Domains.Notifications
{
    /// <summary>
    /// Facade impl for the Notifications domain. Thin orchestrator MonoBehaviour.
    /// Stage 2.0d: scaffold; registers INotifications in Awake; empty Services/ grown per consumer.
    /// Invariants #3 (no per-frame FindObjectOfType), #4 (no new singletons) observed.
    /// </summary>
    public class Notifications : MonoBehaviour, INotifications
    {
        private ServiceRegistry _registry;

        private void Awake()
        {
            _registry = FindObjectOfType<ServiceRegistry>();
            if (_registry != null)
                _registry.Register<INotifications>(this);
            else
                Debug.LogWarning("[Notifications] ServiceRegistry not found — INotifications not registered.");
        }
    }
}
