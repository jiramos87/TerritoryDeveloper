using UnityEngine;
using Domains.Registry;

namespace Domains.Audio
{
    /// <summary>
    /// Facade impl for the Audio domain. Thin orchestrator MonoBehaviour.
    /// Stage 2.0c: scaffold; registers IAudio in Awake; empty Services/ grown per consumer.
    /// Invariants #3 (no per-frame FindObjectOfType), #4 (no new singletons) observed.
    /// </summary>
    public class Audio : MonoBehaviour, IAudio
    {
        private ServiceRegistry _registry;

        private void Awake()
        {
            _registry = FindObjectOfType<ServiceRegistry>();
            if (_registry != null)
                _registry.Register<IAudio>(this);
            else
                Debug.LogWarning("[Audio] ServiceRegistry not found — IAudio not registered.");
        }
    }
}
