using System.Collections.Generic;

namespace Territory.Simulation.Signals
{
    /// <summary>Per-district signal rollup cache populated each tick by <c>DistrictAggregator</c>. Tuple-key dict (sparse population — empty districts cost zero bytes). NaN-on-miss semantics. See <c>ia/specs/simulation-signals.md</c> District layer.</summary>
    public class DistrictSignalCache
    {
        private readonly Dictionary<(int districtId, SimulationSignal signal), float> values = new Dictionary<(int, SimulationSignal), float>();

        /// <summary>Read per-district per-signal rollup value. Returns <c>float.NaN</c> when no <see cref="Set"/> recorded for that (districtId, signal) pair.</summary>
        public float Get(int districtId, SimulationSignal signal)
        {
            if (values.TryGetValue((districtId, signal), out float v))
            {
                return v;
            }
            return float.NaN;
        }

        /// <summary>Return all stored signals for one district. Unset signals omitted; empty district returns empty dict.</summary>
        public Dictionary<SimulationSignal, float> GetAll(int districtId)
        {
            var result = new Dictionary<SimulationSignal, float>();
            foreach (var kv in values)
            {
                if (kv.Key.districtId == districtId)
                {
                    result[kv.Key.signal] = kv.Value;
                }
            }
            return result;
        }

        /// <summary>Write per-district per-signal rollup value. Last-write-wins for repeat keys.</summary>
        public void Set(int districtId, SimulationSignal signal, float value)
        {
            values[(districtId, signal)] = value;
        }

        /// <summary>Empty internal storage; subsequent <see cref="Get"/> returns NaN for all keys.</summary>
        public void Clear()
        {
            values.Clear();
        }
    }
}
