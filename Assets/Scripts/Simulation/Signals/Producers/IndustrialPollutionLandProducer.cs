using Territory.Core;
using Territory.Zones;
using UnityEngine;

namespace Territory.Simulation.Signals.Producers
{
    /// <summary>
    /// Stage 7 producer emitting per-cell <see cref="SimulationSignal.PollutionLand"/> contributions
    /// from industrial-zone buildings via <see cref="GridManager.GetCell(int, int)"/> (invariant #5
    /// — no direct <c>gridArray</c>). Tier weights externalized to <see cref="SignalTuningWeightsAsset"/>
    /// per Stage 6 pattern (designer hot-edit + save-load round-trip). Mirrors
    /// <see cref="IndustrialPollutionProducer"/> cadence (PollutionAir) but writes into
    /// the land-pollution channel for the 3-type pollution split (TECH-1889).
    /// </summary>
    public class IndustrialPollutionLandProducer : MonoBehaviour, ISignalProducer
    {
        [SerializeField] private GridManager gridManager;
        [SerializeField] private SignalTuningWeightsAsset tuningWeights;

        private void Awake()
        {
            if (gridManager == null)
            {
                gridManager = FindObjectOfType<GridManager>();
            }
            if (tuningWeights == null)
            {
                tuningWeights = Resources.Load<SignalTuningWeightsAsset>("SignalTuningWeights");
                if (tuningWeights == null)
                {
                    Debug.LogError("IndustrialPollutionLandProducer.tuningWeights unresolved — SignalTuningWeightsAsset missing under Resources/ and no Inspector wiring. EmitSignals will no-op.");
                }
            }
        }

        /// <summary>Iterate grid; emit per-cell land pollution into <see cref="SimulationSignal.PollutionLand"/>.</summary>
        public void EmitSignals(SignalFieldRegistry registry)
        {
            if (registry == null || gridManager == null || tuningWeights == null)
            {
                return;
            }
            SignalField field = registry.GetField(SimulationSignal.PollutionLand);
            if (field == null)
            {
                return;
            }

            float heavy = tuningWeights.PollutionLandHeavy;
            float medium = tuningWeights.PollutionLandMedium;
            float light = tuningWeights.PollutionLandLight;

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
                    if (weight > 0f)
                    {
                        field.Add(x, y, weight);
                    }
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
