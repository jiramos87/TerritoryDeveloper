using Territory.Core;
using Territory.Zones;
using UnityEngine;

namespace Territory.Simulation.Signals.Producers
{
    /// <summary>
    /// Stage 9.A producer emitting <see cref="SignalTuningWeightsAsset.ServiceFireCoverage"/>
    /// into <see cref="SimulationSignal.ServiceFire"/> at fire-equipped state-service cells
    /// only. Predicate: <c>cell.zoneType ∈ {StateServiceLight,Medium,Heavy}Building</c> AND
    /// <c>cell.occupiedBuilding != null</c> AND <c>Zone.SubTypeId == 1</c> (Fire = id 1 per
    /// <c>Assets/Resources/Economy/zone-sub-types.json</c>). Predicate ordered cheap → expensive
    /// (zone enum compare → null check → GetComponent) to short-circuit non-state-service cells.
    /// Producer reads cell-local predicates only — never other signals.
    /// </summary>
    public class ServiceFireProducer : MonoBehaviour, ISignalProducer
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
                    Debug.LogError("ServiceFireProducer.tuningWeights unresolved — SignalTuningWeightsAsset missing under Resources/ and no Inspector wiring. EmitSignals will no-op.");
                }
            }
        }

        /// <summary>Iterate grid; write <c>ServiceFireCoverage</c> at fire-equipped state-service cells only.</summary>
        public void EmitSignals(SignalFieldRegistry registry)
        {
            if (registry == null || gridManager == null || tuningWeights == null)
            {
                return;
            }
            SignalField field = registry.GetField(SimulationSignal.ServiceFire);
            if (field == null)
            {
                return;
            }

            float coverage = tuningWeights.ServiceFireCoverage;

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
                    if (zone == null || zone.SubTypeId != 1)
                    {
                        continue;
                    }
                    field.Set(x, y, coverage);
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
