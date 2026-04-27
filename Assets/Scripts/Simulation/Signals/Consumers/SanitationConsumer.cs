using Territory.Core;
using Territory.Zones;
using UnityEngine;

namespace Territory.Simulation.Signals.Consumers
{
    /// <summary>
    /// Stage 9.C consumer subtracting waste pressure in the 8-cell Moore neighborhood of every
    /// sanitation-equipped state-service cell (predicate: <c>cell.zoneType ∈
    /// {StateServiceLight,Medium,Heavy}Building</c> AND <c>cell.occupiedBuilding != null</c> AND
    /// <c>Zone.SubTypeId ∈ {5, 6}</c> — Public Housing / Public Offices proxies for sanitation per
    /// <c>Assets/Resources/Economy/zone-sub-types.json</c>; placeholder until a dedicated sanitation
    /// sub-type lands in Bucket 6). Applies <c>wasteField.Add(nx, ny, -SanitationConsumerScale ×
    /// 1.0f)</c> per neighbor with bounds check; <see cref="SignalField.Add"/> floor-clamps at 0
    /// (clamp-floor invariant — never bypass). Runs in step 4 (post-rollup) per
    /// <c>simulation-signals.md</c> §Interface contract.
    /// </summary>
    public class SanitationConsumer : MonoBehaviour, ISignalConsumer
    {
        [SerializeField] private GridManager gridManager;
        [SerializeField] private SignalTuningWeightsAsset tuningWeights;

        private static readonly int[] MooreDX = { -1, 0, 1, -1, 1, -1, 0, 1 };
        private static readonly int[] MooreDY = { -1, -1, -1, 0, 0, 1, 1, 1 };

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
                    Debug.LogError("SanitationConsumer.tuningWeights unresolved — SignalTuningWeightsAsset missing under Resources/ and no Inspector wiring. ConsumeSignals will no-op.");
                }
            }
        }

        /// <summary>Subtract <c>scale × 1.0f</c> from <see cref="SimulationSignal.WastePressure"/> in the 8-cell Moore neighborhood of every sanitation cell; <see cref="SignalField.Add"/> clamps floor-0.</summary>
        public void ConsumeSignals(SignalFieldRegistry registry, DistrictSignalCache cache)
        {
            if (registry == null || gridManager == null || tuningWeights == null)
            {
                return;
            }
            SignalField wasteField = registry.GetField(SimulationSignal.WastePressure);
            if (wasteField == null)
            {
                return;
            }

            float scale = tuningWeights.SanitationConsumerScale;
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
                    if (!IsStateServiceZone(cell.zoneType))
                    {
                        continue;
                    }
                    if (cell.occupiedBuilding == null)
                    {
                        continue;
                    }
                    Zone zone = cell.occupiedBuilding.GetComponent<Zone>();
                    if (zone == null)
                    {
                        continue;
                    }
                    int subTypeId = zone.SubTypeId;
                    if (subTypeId != 5 && subTypeId != 6)
                    {
                        continue;
                    }

                    for (int i = 0; i < 8; i++)
                    {
                        int nx = x + MooreDX[i];
                        int ny = y + MooreDY[i];
                        if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                        {
                            continue;
                        }
                        wasteField.Add(nx, ny, -scale * 1.0f);
                    }
                }
            }
        }

        private static bool IsStateServiceZone(Zone.ZoneType z)
        {
            return z == Zone.ZoneType.StateServiceLightBuilding
                || z == Zone.ZoneType.StateServiceMediumBuilding
                || z == Zone.ZoneType.StateServiceHeavyBuilding;
        }
    }
}
