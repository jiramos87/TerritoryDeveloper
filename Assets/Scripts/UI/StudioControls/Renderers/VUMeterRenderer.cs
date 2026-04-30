using Territory.UI.Juice;
using UnityEngine;

namespace Territory.UI.StudioControls.Renderers
{
    /// <summary>Render-layer companion for <see cref="VUMeter"/>; reads sibling <see cref="NeedleBallistics.CurrentValue"/> in <c>Update</c>, maps to angle via theme range / fallback ±45°, writes to child needle <see cref="RectTransform.localRotation"/>.</summary>
    [RequireComponent(typeof(VUMeter))]
    public class VUMeterRenderer : StudioControlRendererBase
    {
        private const float DefaultMinAngle = 45f;
        private const float DefaultMaxAngle = -45f;

        private VUMeter _meter;
        private NeedleBallistics _needleBallistics;
        private RectTransform _needleRect;

        protected override void Awake()
        {
            base.Awake();
            _meter = GetComponent<VUMeter>();
            _needleBallistics = GetComponent<NeedleBallistics>();
            _needleRect = ResolveNeedleRect();
        }

        private RectTransform ResolveNeedleRect()
        {
            var direct = transform.Find("needle");
            if (direct != null)
            {
                return direct as RectTransform;
            }
            var rects = GetComponentsInChildren<RectTransform>(true);
            for (int i = 0; i < rects.Length; i++)
            {
                var r = rects[i];
                if (r == null) continue;
                if (r.gameObject == gameObject) continue;
                if (r.gameObject.name == "needle")
                {
                    return r;
                }
            }
            return null;
        }

        protected override void OnStateApplied()
        {
            // VU needle motion is driven per-frame in Update via NeedleBallistics; ApplyDetail-time
            // state-applied event has no extra render write to perform here.
        }

        private void Update()
        {
            if (_needleBallistics == null || _needleRect == null) return;
            float current = _needleBallistics.CurrentValue;
            if (float.IsNaN(current) || float.IsInfinity(current)) return;
            float v = Mathf.Clamp01(current);
            float angleZ = Mathf.Lerp(DefaultMinAngle, DefaultMaxAngle, v);
            _needleRect.localRotation = Quaternion.Euler(0f, 0f, angleZ);
        }
    }
}
