// unit-test:Assets/Scripts/RegionScene/Domains/Evolution/RegionEvolutionService.cs::EvolvesPopAndUrbanAreaPerTick
using UnityEngine;
using Domains.Registry;
using Territory.IsoSceneCore;

namespace Territory.RegionScene.Evolution
{
    /// <summary>Subscribes to IsoSceneTickBus.GlobalTick; evolves pop + urban_area per region cell. Subscribe in Start (invariant #12).</summary>
    public sealed class RegionEvolutionService : MonoBehaviour, IIsoSceneTickHandler
    {
        private const float PopGrowthRate    = 0.01f;  // 1% per tick (prototype-grade)
        private const float UrbanGrowthRate  = 0.05f;  // +0.05 per tick
        private const int   MinPopToEvolve   = 100;    // cells below this skip urban growth

        private IsoSceneTickBus _tickBus;
        private RegionData _regionData;
        private bool _ready;

        private void Start()
        {
            var registry = FindObjectOfType<ServiceRegistry>();
            if (registry == null)
            {
                Debug.LogWarning("[RegionEvolutionService] ServiceRegistry not found — evolution disabled.");
                return;
            }

            _tickBus    = registry.Resolve<IsoSceneTickBus>();
            _regionData = registry.Resolve<RegionData>();

            if (_tickBus == null)
            {
                Debug.LogWarning("[RegionEvolutionService] IsoSceneTickBus not registered — evolution disabled.");
                return;
            }

            _tickBus.Subscribe(this, IsoTickKind.GlobalTick);
            _ready = true;
        }

        private void OnDestroy()
        {
            if (_tickBus != null)
                _tickBus.Unsubscribe(this, IsoTickKind.GlobalTick);
        }

        /// <summary>Called by IsoSceneTickBus on every GlobalTick.</summary>
        public void OnIsoTick(IsoTickKind kind)
        {
            if (kind != IsoTickKind.GlobalTick) return;
            if (_regionData == null) return;   // null guard — scene mid-load tick dropped harmlessly

            foreach (var cell in _regionData.AllCells)
            {
                if (cell == null) continue;

                // Prototype-grade multiplicative pop growth (flat terrain only; water/mountain skip)
                if (cell.terrainKind == RegionTerrainKind.Flat)
                {
                    int seed = Mathf.Max(cell.pop, 1);
                    cell.pop = seed + Mathf.Max(1, Mathf.RoundToInt(seed * PopGrowthRate));

                    if (cell.pop >= MinPopToEvolve)
                        cell.urbanArea += UrbanGrowthRate;
                }
            }
        }
    }
}
