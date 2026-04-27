using Territory.Core;
using Territory.Zones;
using UnityEngine;

namespace Territory.Simulation.Signals.Producers
{
    /// <summary>
    /// Stage 9.C cell-local producer emitting per-cell <see cref="SimulationSignal.WastePressure"/>
    /// at every RCI cell via <c>wasteBase + (residentialDensityTier × wasteResidentialDensityWeight)
    /// + (commercialDensityTier × wasteCommercialDensityWeight) + (industrialDensityTier ×
    /// wasteIndustrialDensityWeight)</c>. Density tiers map XLight=1 / XMedium=2 / XHeavy=3 per
    /// RCI family; non-RCI / empty cells emit value 0 (do NOT emit <c>wasteBase</c> at empty cells).
    /// Reads cell-local predicates only — never reads diffused signal state (per
    /// <c>simulation-signals.md</c> §Interface contract step 1, pre-diffusion semantics).
    /// </summary>
    public class WasteProducer : MonoBehaviour, ISignalProducer
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
                    Debug.LogError("WasteProducer.tuningWeights unresolved — SignalTuningWeightsAsset missing under Resources/ and no Inspector wiring. EmitSignals will no-op.");
                }
            }
        }

        /// <summary>Iterate grid; for each RCI cell write per-cell waste composite into <see cref="SimulationSignal.WastePressure"/>; non-RCI cells left at 0.</summary>
        public void EmitSignals(SignalFieldRegistry registry)
        {
            if (registry == null || gridManager == null || tuningWeights == null)
            {
                return;
            }
            SignalField field = registry.GetField(SimulationSignal.WastePressure);
            if (field == null)
            {
                return;
            }

            float baseValue = tuningWeights.WasteBase;
            float resWeight = tuningWeights.WasteResidentialDensityWeight;
            float comWeight = tuningWeights.WasteCommercialDensityWeight;
            float indWeight = tuningWeights.WasteIndustrialDensityWeight;

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
                    int rTier = ResidentialDensityTier(cell.zoneType);
                    int cTier = CommercialDensityTier(cell.zoneType);
                    int iTier = IndustrialDensityTier(cell.zoneType);
                    if (rTier == 0 && cTier == 0 && iTier == 0)
                    {
                        // Non-RCI / empty cell — do NOT emit wasteBase.
                        continue;
                    }
                    float emit = baseValue
                        + resWeight * rTier
                        + comWeight * cTier
                        + indWeight * iTier;
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

        private static int CommercialDensityTier(Zone.ZoneType z)
        {
            switch (z)
            {
                case Zone.ZoneType.CommercialLightBuilding:
                    return 1;
                case Zone.ZoneType.CommercialMediumBuilding:
                    return 2;
                case Zone.ZoneType.CommercialHeavyBuilding:
                    return 3;
                default:
                    return 0;
            }
        }

        private static int IndustrialDensityTier(Zone.ZoneType z)
        {
            switch (z)
            {
                case Zone.ZoneType.IndustrialLightBuilding:
                    return 1;
                case Zone.ZoneType.IndustrialMediumBuilding:
                    return 2;
                case Zone.ZoneType.IndustrialHeavyBuilding:
                    return 3;
                default:
                    return 0;
            }
        }
    }
}
