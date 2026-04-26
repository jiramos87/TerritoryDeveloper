using Territory.Core;
using Territory.Simulation;
using UnityEngine;

namespace Territory.Simulation.Signals
{
    /// <summary>Owns the per-cell <see cref="DistrictMap"/> instance. Inspector + <c>Awake</c> <c>FindObjectOfType</c> fallback for <see cref="UrbanCentroidService"/> + <see cref="GridManager"/> deps (invariant #4). <see cref="Rebuild"/> driven by <c>SignalTickScheduler</c> Phase 0 each tick.</summary>
    public class DistrictManager : MonoBehaviour
    {
        [SerializeField] private UrbanCentroidService centroid;
        [SerializeField] private GridManager grid;

        public DistrictMap Map { get; private set; }

        private void Awake()
        {
            if (centroid == null)
            {
                centroid = FindObjectOfType<UrbanCentroidService>();
            }
            if (grid == null)
            {
                grid = FindObjectOfType<GridManager>();
            }

            if (centroid == null)
            {
                Debug.LogError("DistrictManager.centroid not assigned — Inspector OR scene fallback required. Map will not be allocated.");
                return;
            }
            if (grid == null)
            {
                Debug.LogError("DistrictManager.grid not assigned — Inspector OR scene fallback required. Map will not be allocated.");
                return;
            }

            Map = new DistrictMap(grid.width, grid.height);
        }

        /// <summary>Repopulate <see cref="DistrictMap"/> from current centroid state. No-op when <see cref="Map"/> is null.</summary>
        public void Rebuild()
        {
            if (Map == null)
            {
                return;
            }
            Map.Rebuild(centroid, grid);
        }
    }
}
