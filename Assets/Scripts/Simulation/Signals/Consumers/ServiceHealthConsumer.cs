using System.Collections.Generic;
using UnityEngine;

namespace Territory.Simulation.Signals.Consumers
{
    /// <summary>
    /// Stage 9.A consumer reading <see cref="SimulationSignal.ServiceHealth"/> per-district mean
    /// rollup from <see cref="DistrictSignalCache"/> and storing into
    /// <see cref="LastTickMeanByDistrict"/> as a per-tick read-only snapshot. Bucket 3 demand-model
    /// will wire downstream — this Stage ships emitter + consumer contract + EditMode test as
    /// consumer-of-record. Per <c>simulation-signals.md</c> §Interface contract step 4.
    /// Iterates <c>0..DistrictMap.DistrictCount-1</c> mirroring <see cref="CrimeHotspotEventEmitter"/>;
    /// NaN entries (no rollup recorded for that district) excluded.
    /// </summary>
    public class ServiceHealthConsumer : MonoBehaviour, ISignalConsumer
    {
        [SerializeField] private SignalTuningWeightsAsset tuningWeights;

        private readonly Dictionary<int, float> _lastTickMeanByDistrict = new Dictionary<int, float>(DistrictMap.DistrictCount);

        /// <summary>Read-only view of last <see cref="ConsumeSignals"/> per-district means; districts whose <see cref="DistrictSignalCache.Get"/> returned NaN are omitted.</summary>
        public IReadOnlyDictionary<int, float> LastTickMeanByDistrict => _lastTickMeanByDistrict;

        private void Awake()
        {
            if (tuningWeights == null)
            {
                tuningWeights = Resources.Load<SignalTuningWeightsAsset>("SignalTuningWeights");
                if (tuningWeights == null)
                {
                    Debug.LogError("ServiceHealthConsumer.tuningWeights unresolved — SignalTuningWeightsAsset missing under Resources/ and no Inspector wiring. ConsumeSignals will no-op.");
                }
            }
        }

        /// <summary>Clear + refill <see cref="LastTickMeanByDistrict"/> from <paramref name="cache"/> for <see cref="SimulationSignal.ServiceHealth"/>; iterates <c>0..DistrictMap.DistrictCount-1</c>.</summary>
        public void ConsumeSignals(SignalFieldRegistry registry, DistrictSignalCache cache)
        {
            _lastTickMeanByDistrict.Clear();

            if (cache == null)
            {
                return;
            }

            for (int districtId = 0; districtId < DistrictMap.DistrictCount; districtId++)
            {
                float v = cache.Get(districtId, SimulationSignal.ServiceHealth);
                if (float.IsNaN(v))
                {
                    continue;
                }
                _lastTickMeanByDistrict[districtId] = v;
            }
        }
    }
}
