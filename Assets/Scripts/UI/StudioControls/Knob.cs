using System;
using UnityEngine;

namespace Territory.UI.StudioControls
{
    /// <summary>Rotary StudioControl variant; static prefab + cached <see cref="KnobDetail"/> at Stage 4 (interaction defers to JuiceLayer Stage 5).</summary>
    public class Knob : StudioControlBase
    {
        [SerializeField] private KnobDetail _detail;

        /// <inheritdoc />
        public override string Kind => "knob";

        /// <summary>Bake-time-cached detail row (read-only — runtime accessor used by EditMode smoke).</summary>
        public KnobDetail Detail => _detail;

        /// <inheritdoc />
        public override void ApplyDetail(IDetailRow detail)
        {
            if (detail is KnobDetail kd)
            {
                _detail = kd;
                return;
            }
            Debug.LogWarning($"[Knob] ApplyDetail received non-KnobDetail row (slug={Slug})");
        }
    }

    /// <summary>Typed detail row for <see cref="Knob"/>; field names mirror IR fixture <c>interactives[].detail</c> verbatim.</summary>
    [Serializable]
    public class KnobDetail : IDetailRow
    {
        public float min;
        public float max;
        public float step;
    }
}
