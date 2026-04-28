using System;
using UnityEngine;

namespace Territory.UI.StudioControls
{
    /// <summary>Linear-translation StudioControl variant; static prefab + cached <see cref="FaderDetail"/> at Stage 4.</summary>
    public class Fader : StudioControlBase
    {
        [SerializeField] private FaderDetail _detail;

        /// <inheritdoc />
        public override string Kind => "fader";

        /// <summary>Bake-time-cached detail row (read-only).</summary>
        public FaderDetail Detail => _detail;

        /// <inheritdoc />
        public override void ApplyDetail(IDetailRow detail)
        {
            if (detail is FaderDetail fd)
            {
                _detail = fd;
                return;
            }
            Debug.LogWarning($"[Fader] ApplyDetail received non-FaderDetail row (slug={Slug})");
        }
    }

    /// <summary>Typed detail row for <see cref="Fader"/>; field names mirror IR fixture verbatim.</summary>
    [Serializable]
    public class FaderDetail : IDetailRow
    {
        public float min;
        public float max;
        public float step;
    }
}
