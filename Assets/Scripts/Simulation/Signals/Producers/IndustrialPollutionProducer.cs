using Territory.Core;
using Territory.Zones;
using UnityEngine;

namespace Territory.Simulation.Signals.Producers
{
    /// <summary>
    /// Stage 4 producer emitting per-cell <see cref="SimulationSignal.PollutionAir"/> contributions
    /// from industrial-zone buildings via <see cref="GridManager.GetCell(int, int)"/> (invariant #5
    /// — no direct <c>gridArray</c>). Weights ported from <c>CityStats.cs</c> lines 881–883.
    /// </summary>
    public class IndustrialPollutionProducer : MonoBehaviour, ISignalProducer
    {
        [SerializeField] private GridManager gridManager;

        // Per-tier weights — match CityStats.cs constants verbatim.
        private const float POLLUTION_INDUSTRIAL_HEAVY = 3.0f;
        private const float POLLUTION_INDUSTRIAL_MEDIUM = 2.0f;
        private const float POLLUTION_INDUSTRIAL_LIGHT = 1.0f;

        private void Awake()
        {
            if (gridManager == null)
            {
                gridManager = FindObjectOfType<GridManager>();
            }
        }

        /// <summary>Iterate grid; emit per-cell pollution into <see cref="SimulationSignal.PollutionAir"/>.</summary>
        public void EmitSignals(SignalFieldRegistry registry)
        {
            if (registry == null || gridManager == null)
            {
                return;
            }
            SignalField field = registry.GetField(SimulationSignal.PollutionAir);
            if (field == null)
            {
                return;
            }

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
                    float weight = WeightForZone(cell.zoneType);
                    if (weight > 0f)
                    {
                        field.Add(x, y, weight);
                    }
                }
            }
        }

        private static float WeightForZone(Zone.ZoneType zoneType)
        {
            switch (zoneType)
            {
                case Zone.ZoneType.IndustrialHeavyBuilding:
                    return POLLUTION_INDUSTRIAL_HEAVY;
                case Zone.ZoneType.IndustrialMediumBuilding:
                    return POLLUTION_INDUSTRIAL_MEDIUM;
                case Zone.ZoneType.IndustrialLightBuilding:
                    return POLLUTION_INDUSTRIAL_LIGHT;
                default:
                    return 0f;
            }
        }
    }
}
