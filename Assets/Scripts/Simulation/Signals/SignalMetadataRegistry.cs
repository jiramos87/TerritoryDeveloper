using System;
using UnityEngine;

namespace Territory.Simulation.Signals
{
    /// <summary>Per-signal rollup taxonomy — see <c>ia/specs/simulation-signals.md</c> Rollup Rule Table.</summary>
    public enum RollupRule
    {
        Mean = 0,
        P90 = 1,
    }

    /// <summary>Inspector-tunable per-signal diffusion + decay + rollup metadata. One entry per <see cref="SimulationSignal"/> ordinal.</summary>
    [CreateAssetMenu(fileName = "SignalMetadataRegistry", menuName = "Territory/Simulation/SignalMetadataRegistry", order = 0)]
    public class SignalMetadataRegistry : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public float diffusionRadius;
            public float decayPerStep;
            public Vector2 anisotropy;
            public RollupRule rollup;
        }

        [SerializeField] private Entry[] entries = new Entry[12];

        /// <summary>Resolve metadata for a signal via enum-ordinal index.</summary>
        public Entry GetMetadata(SimulationSignal signal)
        {
            return entries[(int)signal];
        }
    }
}
