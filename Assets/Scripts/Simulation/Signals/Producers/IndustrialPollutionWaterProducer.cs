using Territory.Core;
using Territory.Utilities.Compute;
using Territory.Zones;
using UnityEngine;

namespace Territory.Simulation.Signals.Producers
{
    /// <summary>
    /// Stage 7 producer emitting per-cell <see cref="SimulationSignal.PollutionWater"/> contributions
    /// from industrial-zone buildings ONLY when the cell is Moore-adjacent to registered open water
    /// (per <see cref="WaterAdjacency.IsMooreAdjacentToOpenWater"/>). Producer-side gating keeps
    /// <c>DiffusionKernel</c> signal-agnostic — water spillover physics enforced at emit, not diffusion.
    /// Tier weights externalized to <see cref="SignalTuningWeightsAsset"/> (TECH-1890); heavier than
    /// land tier (Heavy 3.0 vs 2.5) per spillover-impact contract.
    /// </summary>
    public class IndustrialPollutionWaterProducer : MonoBehaviour, ISignalProducer
    {
        [SerializeField] private GridManager gridManager;
        [SerializeField] private Territory.Terrain.TerrainManager terrainManager;
        [SerializeField] private SignalTuningWeightsAsset tuningWeights;

        // Test seam — set via reflection in EditMode to bypass real TerrainManager wiring.
        private IOpenWaterMapView _waterViewOverride;

        /// <summary>EditMode test seam — inject a fake <see cref="IOpenWaterMapView"/> bypassing the real <c>TerrainManager</c>.</summary>
        public void SetWaterViewOverride(IOpenWaterMapView view)
        {
            _waterViewOverride = view;
        }

        private void Awake()
        {
            if (gridManager == null)
            {
                gridManager = FindObjectOfType<GridManager>();
            }
            if (terrainManager == null)
            {
                terrainManager = FindObjectOfType<Territory.Terrain.TerrainManager>();
            }
            if (tuningWeights == null)
            {
                tuningWeights = Resources.Load<SignalTuningWeightsAsset>("SignalTuningWeights");
                if (tuningWeights == null)
                {
                    Debug.LogError("IndustrialPollutionWaterProducer.tuningWeights unresolved — SignalTuningWeightsAsset missing under Resources/ and no Inspector wiring. EmitSignals will no-op.");
                }
            }
        }

        /// <summary>Iterate grid; emit per-cell water pollution into <see cref="SimulationSignal.PollutionWater"/> only when Moore-adjacent to open water.</summary>
        public void EmitSignals(SignalFieldRegistry registry)
        {
            if (registry == null || gridManager == null || tuningWeights == null)
            {
                return;
            }
            SignalField field = registry.GetField(SimulationSignal.PollutionWater);
            if (field == null)
            {
                return;
            }

            float heavy = tuningWeights.PollutionWaterHeavy;
            float medium = tuningWeights.PollutionWaterMedium;
            float light = tuningWeights.PollutionWaterLight;

            IOpenWaterMapView waterView = _waterViewOverride ?? (IOpenWaterMapView)new TerrainOpenWaterMapView(terrainManager);

            int width = gridManager.width;
            int height = gridManager.height;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    CityCell cell = gridManager.GetCell(x, y);
                    if (cell == null)
                    {
                        continue;
                    }
                    float weight = WeightForZone(cell.zoneType, heavy, medium, light);
                    if (weight <= 0f)
                    {
                        continue;
                    }
                    if (!WaterAdjacency.IsMooreAdjacentToOpenWater(x, y, waterView))
                    {
                        continue;
                    }
                    field.Add(x, y, weight);
                }
            }
        }

        private static float WeightForZone(Zone.ZoneType zoneType, float heavy, float medium, float light)
        {
            switch (zoneType)
            {
                case Zone.ZoneType.IndustrialHeavyBuilding:
                    return heavy;
                case Zone.ZoneType.IndustrialMediumBuilding:
                    return medium;
                case Zone.ZoneType.IndustrialLightBuilding:
                    return light;
                default:
                    return 0f;
            }
        }
    }
}
