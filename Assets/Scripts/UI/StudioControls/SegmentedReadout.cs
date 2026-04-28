using System;
using UnityEngine;

namespace Territory.UI.StudioControls
{
    /// <summary>Multi-digit numeric readout StudioControl variant; ApplyTheme resolves illumination + font via cached slugs.</summary>
    public class SegmentedReadout : StudioControlBase
    {
        [SerializeField] private SegmentedReadoutDetail _detail;

        /// <inheritdoc />
        public override string Kind => "segmented-readout";

        /// <summary>Bake-time-cached detail row (read-only).</summary>
        public SegmentedReadoutDetail Detail => _detail;

        /// <summary>Digit count (display width); 0 when detail unset.</summary>
        public int Digits => _detail != null ? _detail.digits : 0;

        /// <inheritdoc />
        public override void ApplyDetail(IDetailRow detail)
        {
            if (detail is SegmentedReadoutDetail sd)
            {
                _detail = sd;
                return;
            }
            Debug.LogWarning($"[SegmentedReadout] ApplyDetail received non-SegmentedReadoutDetail row (slug={Slug})");
        }

        /// <inheritdoc />
        public override void ApplyTheme(UiTheme theme)
        {
            if (theme == null || _detail == null) return;
            if (!string.IsNullOrEmpty(_detail.fontSlug)
                && !theme.TryGetFontFace(_detail.fontSlug, out _))
            {
                Debug.LogWarning($"[SegmentedReadout] font slug not found (slug={Slug}, fontSlug={_detail.fontSlug})");
            }
            if (!string.IsNullOrEmpty(_detail.segmentColor)
                && !theme.TryGetIllumination(_detail.segmentColor, out _))
            {
                // segmentColor reserved for future palette / illumination resolution; warn only when set + missing.
                Debug.LogWarning($"[SegmentedReadout] segment-color illumination slug not found (slug={Slug}, segmentColor={_detail.segmentColor})");
            }
        }
    }

    /// <summary>Typed detail row for <see cref="SegmentedReadout"/>.</summary>
    [Serializable]
    public class SegmentedReadoutDetail : IDetailRow
    {
        public int digits;
        public string fontSlug;
        public string segmentColor;
    }
}
