using Territory.Core;
using Territory.Zones;
using UnityEngine;

namespace Territory.Simulation.Signals.Producers
{
    /// <summary>
    /// Stage 9.B cell-local producer emitting per-cell <see cref="SimulationSignal.TrafficLevel"/> =
    /// <c>trafficBase + trafficRoadwayDensityWeight × MooreNeighborRciCount(cell)</c> for every
    /// road cell (<see cref="Zone.ZoneType.Road"/>). Off-road cells emit nothing — field stays 0.
    /// Reads cell-local + Moore-neighborhood predicates only — never reads diffused signal state
    /// (per <c>simulation-signals.md</c> §Interface contract step 1, pre-diffusion semantics).
    /// Uses <see cref="SignalField.Set"/> (overwrite) so producer is idempotent across repeat ticks
    /// without diffusion. Density-tier scope: ALL R / C / I density tiers (any building variant).
    /// </summary>
    public class TrafficProducer : MonoBehaviour, ISignalProducer
    {
        [SerializeField] private GridManager gridManager;
        [SerializeField] private SignalTuningWeightsAsset tuningWeights;

        private static readonly int[] MooreDx = { 1, -1, 0, 0, 1, 1, -1, -1 };
        private static readonly int[] MooreDy = { 0, 0, 1, -1, 1, -1, 1, -1 };

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
                    Debug.LogError("TrafficProducer.tuningWeights unresolved — SignalTuningWeightsAsset missing under Resources/ and no Inspector wiring. EmitSignals will no-op.");
                }
            }
        }

        /// <summary>Iterate grid; for each road cell write <c>trafficBase + trafficRoadwayDensityWeight × CountRciNeighbors(cell)</c> into <see cref="SimulationSignal.TrafficLevel"/>; off-road cells left at 0.</summary>
        public void EmitSignals(SignalFieldRegistry registry)
        {
            if (registry == null || gridManager == null || tuningWeights == null)
            {
                return;
            }
            SignalField field = registry.GetField(SimulationSignal.TrafficLevel);
            if (field == null)
            {
                return;
            }

            float baseValue = tuningWeights.TrafficBase;
            float densityWeight = tuningWeights.TrafficRoadwayDensityWeight;

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
                    if (cell.zoneType != Zone.ZoneType.Road)
                    {
                        continue;
                    }
                    int rciNeighbors = CountRciNeighbors(x, y, width, height);
                    float emit = baseValue + densityWeight * rciNeighbors;
                    field.Set(x, y, emit);
                }
            }
        }

        private int CountRciNeighbors(int x, int y, int width, int height)
        {
            int count = 0;
            for (int i = 0; i < MooreDx.Length; i++)
            {
                int nx = x + MooreDx[i];
                int ny = y + MooreDy[i];
                if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                {
                    continue;
                }
                CityCell neighbor = gridManager.GetCell(nx, ny);
                if (neighbor == null)
                {
                    continue;
                }
                if (IsRci(neighbor.zoneType))
                {
                    count++;
                }
            }
            return count;
        }

        private static bool IsRci(Zone.ZoneType z)
        {
            switch (z)
            {
                case Zone.ZoneType.ResidentialLightBuilding:
                case Zone.ZoneType.ResidentialMediumBuilding:
                case Zone.ZoneType.ResidentialHeavyBuilding:
                case Zone.ZoneType.CommercialLightBuilding:
                case Zone.ZoneType.CommercialMediumBuilding:
                case Zone.ZoneType.CommercialHeavyBuilding:
                case Zone.ZoneType.IndustrialLightBuilding:
                case Zone.ZoneType.IndustrialMediumBuilding:
                case Zone.ZoneType.IndustrialHeavyBuilding:
                    return true;
                default:
                    return false;
            }
        }
    }
}
