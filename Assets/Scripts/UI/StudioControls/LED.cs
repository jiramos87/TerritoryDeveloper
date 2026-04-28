using System;
using UnityEngine;

namespace Territory.UI.StudioControls
{
    /// <summary>Single LED indicator StudioControl variant; ApplyTheme resolves <see cref="UiTheme"/> illumination spec via cached slug.</summary>
    public class LED : StudioControlBase
    {
        [SerializeField] private LEDDetail _detail;

        /// <inheritdoc />
        public override string Kind => "led";

        /// <summary>Bake-time-cached detail row (read-only).</summary>
        public LEDDetail Detail => _detail;

        /// <inheritdoc />
        public override void ApplyDetail(IDetailRow detail)
        {
            if (detail is LEDDetail ld)
            {
                _detail = ld;
                return;
            }
            Debug.LogWarning($"[LED] ApplyDetail received non-LEDDetail row (slug={Slug})");
        }

        /// <inheritdoc />
        public override void ApplyTheme(UiTheme theme)
        {
            if (theme == null || _detail == null) return;
            if (string.IsNullOrEmpty(_detail.illuminationSlug)) return;
            if (!theme.TryGetIllumination(_detail.illuminationSlug, out _))
            {
                Debug.LogWarning($"[LED] illumination slug not found (slug={Slug}, illuminationSlug={_detail.illuminationSlug})");
            }
        }
    }

    /// <summary>Typed detail row for <see cref="LED"/>.</summary>
    [Serializable]
    public class LEDDetail : IDetailRow
    {
        public string illuminationSlug;
        public bool defaultState;
    }
}
