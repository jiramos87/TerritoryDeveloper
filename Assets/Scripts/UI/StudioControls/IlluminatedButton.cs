using System;
using UnityEngine;

namespace Territory.UI.StudioControls
{
    /// <summary>Illuminated push-button StudioControl variant; ApplyTheme resolves <see cref="UiTheme"/> illumination spec via cached slug.</summary>
    public class IlluminatedButton : StudioControlBase
    {
        [SerializeField] private IlluminatedButtonDetail _detail;

        /// <inheritdoc />
        public override string Kind => "illuminated-button";

        /// <summary>Bake-time-cached detail row (read-only).</summary>
        public IlluminatedButtonDetail Detail => _detail;

        /// <inheritdoc />
        public override void ApplyDetail(IDetailRow detail)
        {
            if (detail is IlluminatedButtonDetail bd)
            {
                _detail = bd;
                return;
            }
            Debug.LogWarning($"[IlluminatedButton] ApplyDetail received non-IlluminatedButtonDetail row (slug={Slug})");
        }

        /// <inheritdoc />
        public override void ApplyTheme(UiTheme theme)
        {
            if (theme == null || _detail == null) return;
            if (string.IsNullOrEmpty(_detail.illuminationSlug)) return;
            if (!theme.TryGetIllumination(_detail.illuminationSlug, out _))
            {
                Debug.LogWarning($"[IlluminatedButton] illumination slug not found (slug={Slug}, illuminationSlug={_detail.illuminationSlug})");
            }
            // IlluminationSpec carries color + halo radius; runtime sprite/halo binding deferred to Stage 5.
        }
    }

    /// <summary>Typed detail row for <see cref="IlluminatedButton"/>.</summary>
    [Serializable]
    public class IlluminatedButtonDetail : IDetailRow
    {
        public string illuminationSlug;
        public string pulseOnEvent;
    }
}
