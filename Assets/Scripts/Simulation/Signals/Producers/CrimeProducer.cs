using Territory.Core;
using Territory.Zones;
using UnityEngine;

namespace Territory.Simulation.Signals.Producers
{
    /// <summary>
    /// Stage 8 cell-local producer emitting per-cell <see cref="SimulationSignal.Crime"/> =
    /// <c>crimeBase + crimeDensityWeight * residentialDensityTier(cell.zoneType)</c>. Reads
    /// cell-local predicates only — never reads <see cref="SimulationSignal.ServicePolice"/>
    /// mid-producer (police reduction is consumer-side per <c>simulation-signals.md</c>
    /// §Interface contract step 4). Density tier mirrors <see cref="LandValueProducer"/>:
    /// ResidentialLight=1, ResidentialMedium=2, ResidentialHeavy=3, else 0.
    /// Uses <see cref="SignalField.Set"/> (overwrite) so producer is idempotent across
    /// repeat ticks without diffusion (Stage 1 pre-diffusion semantics).
    /// </summary>
    public class CrimeProducer : MonoBehaviour, ISignalProducer
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
                    Debug.LogError("CrimeProducer.tuningWeights unresolved — SignalTuningWeightsAsset missing under Resources/ and no Inspector wiring. EmitSignals will no-op.");
                }
            }
        }

        /// <summary>Iterate grid; write per-cell <c>crimeBase + crimeDensityWeight * tier</c> into <see cref="SimulationSignal.Crime"/>.</summary>
        public void EmitSignals(SignalFieldRegistry registry)
        {
            if (registry == null || gridManager == null || tuningWeights == null)
            {
                return;
            }
            SignalField field = registry.GetField(SimulationSignal.Crime);
            if (field == null)
            {
                return;
            }

            float baseValue = tuningWeights.CrimeBase;
            float densityWeight = tuningWeights.CrimeDensityWeight;

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
                    int densityTier = ResidentialDensityTier(cell.zoneType);
                    float emit = baseValue + densityWeight * densityTier;
                    field.Set(x, y, emit);
                }
            }
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
