using Territory.Core;
using UnityEngine;

namespace Territory.Simulation.Signals.Consumers
{
    /// <summary>
    /// Stage 8 consumer subtracting <c>ServicePoliceConsumerScale * policeField.Get(x,y)</c>
    /// from <see cref="SimulationSignal.Crime"/> cell-by-cell, floor-clamped at 0 via
    /// <see cref="SignalField.Add"/>. Runs in step 4 (post-rollup) per
    /// <c>simulation-signals.md</c> §Interface contract.
    /// First file under <c>Consumers/</c> directory.
    /// </summary>
    public class ServicePoliceConsumer : MonoBehaviour, ISignalConsumer
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
                    Debug.LogError("ServicePoliceConsumer.tuningWeights unresolved — SignalTuningWeightsAsset missing under Resources/ and no Inspector wiring. ConsumeSignals will no-op.");
                }
            }
        }

        /// <summary>Subtract <c>scale * policeField.Get(x,y)</c> from <see cref="SimulationSignal.Crime"/>; <see cref="SignalField.Add"/> clamps floor-0.</summary>
        public void ConsumeSignals(SignalFieldRegistry registry, DistrictSignalCache cache)
        {
            if (registry == null || gridManager == null || tuningWeights == null)
            {
                return;
            }
            SignalField crimeField = registry.GetField(SimulationSignal.Crime);
            SignalField policeField = registry.GetField(SimulationSignal.ServicePolice);
            if (crimeField == null || policeField == null)
            {
                return;
            }

            float scale = tuningWeights.ServicePoliceConsumerScale;

            int width = gridManager.width;
            int height = gridManager.height;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float police = policeField.Get(x, y);
                    if (police <= 0f)
                    {
                        continue;
                    }
                    crimeField.Add(x, y, -scale * police);
                }
            }
        }
    }
}
