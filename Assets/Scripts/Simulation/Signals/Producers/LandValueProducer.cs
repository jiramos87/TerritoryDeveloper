using Territory.Core;
using Territory.Zones;
using UnityEngine;

namespace Territory.Simulation.Signals.Producers
{
    /// <summary>
    /// Stage 7 composite producer emitting per-cell <see cref="SimulationSignal.LandValue"/> = baseline
    /// + park-adjacency bonus − industrial-adjacency penalty + residential-density-tier bonus.
    /// Parks proxied by <see cref="Zone.ZoneType.Forest"/> at this stage (no dedicated ParkBuilding
    /// enum; revisit if ParkBuilding lands). Constants externalized to
    /// <see cref="SignalTuningWeightsAsset"/> per Stage 6 pattern (TECH-1891).
    /// Adjacency uses 8-neighbor Moore window. Density tier maps:
    /// ResidentialLightBuilding=1, ResidentialMediumBuilding=2, ResidentialHeavyBuilding=3.
    /// </summary>
    public class LandValueProducer : MonoBehaviour, ISignalProducer
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
                    Debug.LogError("LandValueProducer.tuningWeights unresolved — SignalTuningWeightsAsset missing under Resources/ and no Inspector wiring. EmitSignals will no-op.");
                }
            }
        }

        /// <summary>Iterate grid; emit per-cell composite land value into <see cref="SimulationSignal.LandValue"/>.</summary>
        public void EmitSignals(SignalFieldRegistry registry)
        {
            if (registry == null || gridManager == null || tuningWeights == null)
            {
                return;
            }
            SignalField field = registry.GetField(SimulationSignal.LandValue);
            if (field == null)
            {
                return;
            }

            float baseValue = tuningWeights.LandValueBase;
            float parkBonus = tuningWeights.LandValueParkBonus;
            float industrialPenalty = tuningWeights.LandValueIndustrialPenalty;
            float densityBonus = tuningWeights.LandValueDensityBonus;

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

                    int parkNeighbors = 0;
                    int industrialNeighbors = 0;
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
                        if (IsParkProxy(neighbor.zoneType))
                        {
                            parkNeighbors++;
                        }
                        if (IsIndustrial(neighbor.zoneType))
                        {
                            industrialNeighbors++;
                        }
                    }

                    int densityTier = ResidentialDensityTier(cell.zoneType);
                    float emit = baseValue
                        + parkBonus * parkNeighbors
                        - industrialPenalty * industrialNeighbors
                        + densityBonus * densityTier;
                    if (emit < 0f)
                    {
                        emit = 0f;
                    }
                    field.Add(x, y, emit);
                }
            }
        }

        private static bool IsParkProxy(Zone.ZoneType z)
        {
            // Forest proxies park-adjacency until a dedicated ParkBuilding enum lands.
            return z == Zone.ZoneType.Forest;
        }

        private static bool IsIndustrial(Zone.ZoneType z)
        {
            return z == Zone.ZoneType.IndustrialLightBuilding
                || z == Zone.ZoneType.IndustrialMediumBuilding
                || z == Zone.ZoneType.IndustrialHeavyBuilding;
        }

        private static int ResidentialDensityTier(Zone.ZoneType z)
        {
            switch (z)
            {
                case Zone.ZoneType.ResidentialLightBuilding:
                    return 1;
                case Zone.ZoneType.ResidentialMediumBuilding:
                    return 2;
                case Zone.ZoneType.ResidentialHeavyBuilding:
                    return 3;
                default:
                    return 0;
            }
        }
    }
}
