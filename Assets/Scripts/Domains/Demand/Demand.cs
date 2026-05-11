using UnityEngine;
using Domains.Registry;

namespace Domains.Demand
{
    /// <summary>
    /// Facade impl for the Demand domain. Thin orchestrator MonoBehaviour.
    /// Stage 2.0g: scaffold; registers IDemand in Awake; empty Services/ grown per consumer.
    /// Invariants #3 (no per-frame FindObjectOfType), #4 (no new singletons) observed.
    /// UrbanizationProposal pre-scan deferred to Stage 5.4 (invariant #11 — flag, never delete).
    /// </summary>
    public class Demand : MonoBehaviour, IDemand
    {
        private ServiceRegistry _registry;

        private void Awake()
        {
            _registry = FindObjectOfType<ServiceRegistry>();
            if (_registry != null)
                _registry.Register<IDemand>(this);
            else
                Debug.LogWarning("[Demand] ServiceRegistry not found — IDemand not registered.");
        }
    }
}
