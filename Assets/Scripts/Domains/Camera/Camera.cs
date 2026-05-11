using UnityEngine;
using Domains.Registry;

namespace Domains.Camera
{
    /// <summary>
    /// Facade impl for the Camera domain. Thin orchestrator MonoBehaviour.
    /// Stage 2.0f: scaffold; registers ICamera in Awake; empty Services/ grown per consumer.
    /// Invariants #3 (no per-frame FindObjectOfType), #4 (no new singletons) observed.
    /// Note: class name 'Camera' lives in Domains.Camera namespace; UnityEngine.Camera is distinct.
    /// </summary>
    public class Camera : MonoBehaviour, ICamera
    {
        private ServiceRegistry _registry;

        private void Awake()
        {
            _registry = FindObjectOfType<ServiceRegistry>();
            if (_registry != null)
                _registry.Register<ICamera>(this);
            else
                Debug.LogWarning("[Camera] ServiceRegistry not found — ICamera not registered.");
        }
    }
}
