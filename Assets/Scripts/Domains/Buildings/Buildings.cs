using UnityEngine;
using Domains.Registry;

namespace Domains.Buildings
{
    /// <summary>
    /// Facade impl for the Buildings domain. Thin orchestrator MonoBehaviour.
    /// Stage 2.0b: scaffold; registers IBuildings in Awake; empty Services/ grown per consumer.
    /// Invariants #3 (no per-frame FindObjectOfType), #4 (no new singletons) observed.
    /// </summary>
    public class Buildings : MonoBehaviour, IBuildings
    {
        private ServiceRegistry _registry;

        private void Awake()
        {
            _registry = FindObjectOfType<ServiceRegistry>();
            if (_registry != null)
                _registry.Register<IBuildings>(this);
            else
                Debug.LogWarning("[Buildings] ServiceRegistry not found — IBuildings not registered.");
        }
    }
}
