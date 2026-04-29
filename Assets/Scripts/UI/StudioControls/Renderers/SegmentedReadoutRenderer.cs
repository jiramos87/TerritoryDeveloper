using TMPro;
using UnityEngine;

namespace Territory.UI.StudioControls.Renderers
{
    /// <summary>Render-layer companion for <see cref="SegmentedReadout"/>; writes zero-padded numeric value into child <see cref="TMP_Text"/>.</summary>
    [RequireComponent(typeof(SegmentedReadout))]
    public class SegmentedReadoutRenderer : StudioControlRendererBase
    {
        private SegmentedReadout _readout;
        private TMP_Text _text;

        protected override void Awake()
        {
            base.Awake();
            _readout = GetComponent<SegmentedReadout>();
            _text = GetComponentInChildren<TMP_Text>(true);
        }

        protected override void OnStateApplied()
        {
            if (_readout == null || _text == null) return;
            var detail = _readout.Detail;
            if (detail == null)
            {
                // Bake-time order may leave one frame with null detail — defensive early-return.
                return;
            }
            int digits = detail.digits > 0 ? detail.digits : 1;
            _text.text = _readout.CurrentValue.ToString($"D{digits}");
        }
    }
}
