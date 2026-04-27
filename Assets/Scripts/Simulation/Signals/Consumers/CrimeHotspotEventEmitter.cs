using System;
using Territory.Simulation.Signals.Events;
using UnityEngine;

namespace Territory.Simulation.Signals.Consumers
{
    /// <summary>
    /// Stage 8 consumer iterating <c>0..DistrictMap.DistrictCount-1</c>; reads
    /// <see cref="DistrictSignalCache"/>'s P90 rollup of <see cref="SimulationSignal.Crime"/>
    /// and invokes <see cref="Hotspot"/> when <c>!float.IsNaN(level) &amp;&amp; level &gt; CrimeHotspotThreshold</c>.
    /// Strict <c>&gt;</c> per <c>simulation-signals.md</c> step-1 spec phrasing.
    /// Bucket 5 protest-animation will subscribe later; Stage 8 ships emitter contract
    /// only with no in-tree listener (deferred — emitter-without-listener intent).
    /// Subscribers MUST cache the <c>+=</c> reference in <c>Awake</c> per invariant #3
    /// (no per-frame <c>FindObjectOfType</c>).
    /// </summary>
    public class CrimeHotspotEventEmitter : MonoBehaviour, ISignalConsumer
    {
        [SerializeField] private SignalTuningWeightsAsset tuningWeights;

        /// <summary>Fired once per qualifying district per <see cref="ConsumeSignals"/> call.</summary>
        public event Action<CrimeHotspotEvent> Hotspot;

        private void Awake()
        {
            if (tuningWeights == null)
            {
                tuningWeights = Resources.Load<SignalTuningWeightsAsset>("SignalTuningWeights");
                if (tuningWeights == null)
                {
                    Debug.LogError("CrimeHotspotEventEmitter.tuningWeights unresolved — SignalTuningWeightsAsset missing under Resources/ and no Inspector wiring. ConsumeSignals will no-op.");
                }
            }
        }

        /// <summary>Iterate districts; emit <see cref="CrimeHotspotEvent"/> per district whose P90 Crime rollup exceeds threshold.</summary>
        public void ConsumeSignals(SignalFieldRegistry registry, DistrictSignalCache cache)
        {
            if (cache == null || tuningWeights == null)
            {
                return;
            }
            float threshold = tuningWeights.CrimeHotspotThreshold;
            for (int districtId = 0; districtId < DistrictMap.DistrictCount; districtId++)
            {
                float level = cache.Get(districtId, SimulationSignal.Crime);
                if (float.IsNaN(level))
                {
                    continue;
                }
                if (level > threshold)
                {
                    Hotspot?.Invoke(new CrimeHotspotEvent { districtId = districtId, level = level });
                }
            }
        }
    }
}
