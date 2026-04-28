using System.Collections;
using Territory.UI.StudioControls;
using UnityEngine;

namespace Territory.UI.Juice
{
    /// <summary>
    /// Int-to-int tween writer for <see cref="SegmentedReadout"/>. Drives intermediate digit values
    /// during a transition over the configured <c>motion_curve</c> duration. Re-entrant
    /// <see cref="SetTarget"/> restarts from the current sampled value (no jump-back).
    /// </summary>
    [RequireComponent(typeof(SegmentedReadout))]
    public class TweenCounter : JuiceBase
    {
        [SerializeField] private int currentTarget;
        [SerializeField] private float fallbackDurationMs = 500f;

        private SegmentedReadout _readout;
        private Coroutine _activeTween;
        private float _sampleStart;

        /// <summary>Read-only current target (final value of active or last tween).</summary>
        public int CurrentTarget => currentTarget;

        protected override void Awake()
        {
            base.Awake();
            _readout = GetComponent<SegmentedReadout>();
        }

        /// <summary>
        /// Start (or restart) a tween toward <paramref name="target"/>. If a tween is in flight,
        /// the new tween starts from the current sampled value to avoid visual snap-back.
        /// </summary>
        public void SetTarget(int target)
        {
            if (_readout == null) return;
            currentTarget = target;
            _sampleStart = _readout.CurrentValue;

            if (_activeTween != null) StopCoroutine(_activeTween);
            _activeTween = StartCoroutine(TweenRoutine(_sampleStart, target));
        }

        private IEnumerator TweenRoutine(float startValue, int target)
        {
            float durationMs = fallbackDurationMs;
            if (TryEvaluateCurve(out var spec) && spec.durationMs > 0f)
            {
                durationMs = spec.durationMs;
            }
            float duration = Mathf.Max(0.001f, durationMs / 1000f);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float sample = Mathf.Lerp(startValue, target, t);
                _readout.CurrentValue = Mathf.FloorToInt(sample);
                yield return null;
            }
            _readout.CurrentValue = target;
            _activeTween = null;
        }
    }
}
