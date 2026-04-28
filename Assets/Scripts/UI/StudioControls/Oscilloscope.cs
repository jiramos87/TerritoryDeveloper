using System;
using UnityEngine;

namespace Territory.UI.StudioControls
{
    /// <summary>Sweep-trace readout StudioControl variant; static prefab + cached <see cref="OscilloscopeDetail"/>. Sample buffer + sweep render defer to JuiceLayer Stage 5.</summary>
    public class Oscilloscope : StudioControlBase
    {
        [SerializeField] private OscilloscopeDetail _detail;

        /// <inheritdoc />
        public override string Kind => "oscilloscope";

        /// <summary>Bake-time-cached detail row (read-only).</summary>
        public OscilloscopeDetail Detail => _detail;

        /// <summary>Sample buffer length (discrete sample count).</summary>
        public int SampleCount => _detail != null ? _detail.sampleCount : 0;

        /// <summary>Sweep rate (Hz) — horizontal traversal frequency.</summary>
        public float SweepRateHz => _detail != null ? _detail.sweepRateHz : 0f;

        /// <summary>Signal magnitude range (full-scale).</summary>
        public float Range => _detail != null ? _detail.range : 0f;

        /// <inheritdoc />
        public override void ApplyDetail(IDetailRow detail)
        {
            if (detail is OscilloscopeDetail od)
            {
                _detail = od;
                return;
            }
            Debug.LogWarning($"[Oscilloscope] ApplyDetail received non-OscilloscopeDetail row (slug={Slug})");
        }
    }

    /// <summary>Typed detail row for <see cref="Oscilloscope"/>; field names mirror IR fixture verbatim.</summary>
    [Serializable]
    public class OscilloscopeDetail : IDetailRow
    {
        public int sampleCount;
        public float sweepRateHz;
        public float range;
    }
}
