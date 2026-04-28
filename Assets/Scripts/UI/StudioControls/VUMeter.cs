using System;
using UnityEngine;

namespace Territory.UI.StudioControls
{
    /// <summary>Analog-needle readout StudioControl variant; static prefab + cached <see cref="VUMeterDetail"/>. Render motion defers to JuiceLayer Stage 5.</summary>
    public class VUMeter : StudioControlBase
    {
        [SerializeField] private VUMeterDetail _detail;

        /// <inheritdoc />
        public override string Kind => "vu-meter";

        /// <summary>Bake-time-cached detail row (read-only).</summary>
        public VUMeterDetail Detail => _detail;

        /// <summary>Attack envelope time (ms) — needle rise constant.</summary>
        public float AttackMs => _detail != null ? _detail.attackMs : 0f;

        /// <summary>Release envelope time (ms) — needle fall constant.</summary>
        public float ReleaseMs => _detail != null ? _detail.releaseMs : 0f;

        /// <summary>Signal magnitude range (full-scale).</summary>
        public float Range => _detail != null ? _detail.range : 0f;

        /// <inheritdoc />
        public override void ApplyDetail(IDetailRow detail)
        {
            if (detail is VUMeterDetail vd)
            {
                _detail = vd;
                return;
            }
            Debug.LogWarning($"[VUMeter] ApplyDetail received non-VUMeterDetail row (slug={Slug})");
        }
    }

    /// <summary>Typed detail row for <see cref="VUMeter"/>; field names mirror IR fixture verbatim.</summary>
    [Serializable]
    public class VUMeterDetail : IDetailRow
    {
        public float attackMs;
        public float releaseMs;
        public float range;
    }
}
