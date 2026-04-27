using System.Collections.Generic;
using UnityEngine;

namespace Territory.Simulation.Signals.Consumers
{
    /// <summary>
    /// Stage 9.B consumer reading <see cref="SimulationSignal.TrafficLevel"/> per-district P90
    /// rollup from <see cref="DistrictSignalCache"/> and storing into
    /// <see cref="LastTickP90ByDistrict"/> as a per-tick read-only snapshot. Bucket 3 demand-model
    /// will wire downstream — this Stage ships emitter + consumer contract + EditMode test as
    /// consumer-of-record. Per <c>simulation-signals.md</c> §Interface contract step 4.
    /// Iterates <c>0..DistrictMap.DistrictCount-1</c> mirroring <see cref="ServiceFireConsumer"/>;
    /// NaN entries (no rollup recorded for that district) excluded.
    /// Consumer SO field <c>trafficLevelConsumerScale</c> defined by TECH-2136 is reserved for
    /// Bucket 3 demand-model wiring; not read in this Stage (consumer-of-record stores raw P90).
    /// </summary>
    public class TrafficLevelConsumer : MonoBehaviour, ISignalConsumer
    {
        [SerializeField] private SignalTuningWeightsAsset tuningWeights;

        private readonly Dictionary<int, float> _lastTickP90ByDistrict = new Dictionary<int, float>(DistrictMap.DistrictCount);

        /// <summary>Read-only view of last <see cref="ConsumeSignals"/> per-district P90 values; districts whose <see cref="DistrictSignalCache.Get"/> returned NaN are omitted.</summary>
        public IReadOnlyDictionary<int, float> LastTickP90ByDistrict => _lastTickP90ByDistrict;

        private void Awake()
        {
            if (tuningWeights == null)
            {
                tuningWeights = Resources.Load<SignalTuningWeightsAsset>("SignalTuningWeights");
                if (tuningWeights == null)
                {
                    Debug.LogError("TrafficLevelConsumer.tuningWeights unresolved — SignalTuningWeightsAsset missing under Resources/ and no Inspector wiring. ConsumeSignals will no-op.");
                }
            }
        }

        /// <summary>Clear + refill <see cref="LastTickP90ByDistrict"/> from <paramref name="cache"/> for <see cref="SimulationSignal.TrafficLevel"/>; iterates <c>0..DistrictMap.DistrictCount-1</c>.</summary>
        public void ConsumeSignals(SignalFieldRegistry registry, DistrictSignalCache cache)
        {
            _lastTickP90ByDistrict.Clear();

            if (cache == null)
            {
                return;
            }

            for (int districtId = 0; districtId < DistrictMap.DistrictCount; districtId++)
            {
                float v = cache.Get(districtId, SimulationSignal.TrafficLevel);
                if (float.IsNaN(v))
                {
                    continue;
                }
                _lastTickP90ByDistrict[districtId] = v;
            }
        }
    }
}
