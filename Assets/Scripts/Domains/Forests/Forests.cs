using UnityEngine;
using Domains.Registry;

namespace Domains.Forests
{
    /// <summary>
    /// Facade impl for the Forests domain. Thin orchestrator MonoBehaviour.
    /// Stage 2.0a: scaffold; registers IForests in Awake; empty Services/ grown per consumer.
    /// Invariants #3 (no per-frame FindObjectOfType), #4 (no new singletons) observed.
    /// </summary>
    public class Forests : MonoBehaviour, IForests
    {
        private ServiceRegistry _registry;

        private void Awake()
        {
            _registry = FindObjectOfType<ServiceRegistry>();
            if (_registry != null)
                _registry.Register<IForests>(this);
            else
                Debug.LogWarning("[Forests] ServiceRegistry not found — IForests not registered.");
        }
    }
}
