using UnityEngine;
using Domains.Registry;

namespace Domains.Save
{
    /// <summary>
    /// Facade impl for the Save domain. Thin orchestrator MonoBehaviour.
    /// Stage 2.0e: scaffold; registers ISave in Awake; empty Services/ grown per consumer.
    /// Invariants #3 (no per-frame FindObjectOfType), #4 (no new singletons) observed.
    /// </summary>
    public class Save : MonoBehaviour, ISave
    {
        private ServiceRegistry _registry;

        private void Awake()
        {
            _registry = FindObjectOfType<ServiceRegistry>();
            if (_registry != null)
                _registry.Register<ISave>(this);
            else
                Debug.LogWarning("[Save] ServiceRegistry not found — ISave not registered.");
        }
    }
}
