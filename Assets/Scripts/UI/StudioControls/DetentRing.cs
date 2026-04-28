using System;
using UnityEngine;

namespace Territory.UI.StudioControls
{
    /// <summary>Detented rotary ring StudioControl variant; static prefab + cached <see cref="DetentRingDetail"/> at Stage 4.</summary>
    public class DetentRing : StudioControlBase
    {
        [SerializeField] private DetentRingDetail _detail;

        /// <inheritdoc />
        public override string Kind => "detent-ring";

        /// <summary>Bake-time-cached detail row (read-only).</summary>
        public DetentRingDetail Detail => _detail;

        /// <inheritdoc />
        public override void ApplyDetail(IDetailRow detail)
        {
            if (detail is DetentRingDetail dr)
            {
                _detail = dr;
                return;
            }
            Debug.LogWarning($"[DetentRing] ApplyDetail received non-DetentRingDetail row (slug={Slug})");
        }
    }

    /// <summary>Typed detail row for <see cref="DetentRing"/>; field names mirror IR fixture verbatim.</summary>
    [Serializable]
    public class DetentRingDetail : IDetailRow
    {
        public int detents;
        public float snapAngle;
    }
}
