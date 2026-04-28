using Territory.UI.StudioControls;
using UnityEngine;

namespace Territory.UI.Juice
{
    /// <summary>
    /// Spring-driven needle ballistics for <see cref="VUMeter"/>. Per-frame critical-damped step
    /// in <c>LateUpdate</c>; reads <c>attackMs</c> / <c>releaseMs</c> from sibling
    /// <see cref="VUMeterDetail"/> + spring stiffness from <see cref="UiTheme"/> motion-curve slug.
    /// </summary>
    /// <remarks>
    /// Per-phase damping: rising (toward target above current) uses <c>attackMs</c>, falling uses
    /// <c>releaseMs</c>. Sibling <see cref="VUMeter"/> + theme cached in <c>Awake</c> per
    /// invariant #3; no <c>FindObjectOfType</c> / <c>GetComponent</c> in <c>LateUpdate</c>.
    /// </remarks>
    [RequireComponent(typeof(VUMeter))]
    public class NeedleBallistics : JuiceBase
    {
        /// <summary>Default curve slug used by Stage 5 §Plan Digest.</summary>
        public const string DefaultCurveSlug = "needle-ballistic";

        [SerializeField] private float targetValue;
        [SerializeField] private float currentValue;
        [SerializeField] private float velocity;

        private VUMeter _meter;

        /// <summary>Spring sim driver — set by external signal source (T5.6 fixture / runtime hook).</summary>
        public float TargetValue
        {
            get => targetValue;
            set => targetValue = value;
        }

        /// <summary>Read-only current needle position (post-step).</summary>
        public float CurrentValue => currentValue;

        protected override void Awake()
        {
            base.Awake();
            _meter = GetComponent<VUMeter>();
            if (string.IsNullOrEmpty(curveSlug)) curveSlug = DefaultCurveSlug;
        }

        private void LateUpdate()
        {
            if (_meter == null) return;

            float attackMs = _meter.AttackMs;
            float releaseMs = _meter.ReleaseMs;

            // Spring stiffness pulled from cached motion-curve spec when present; else fallback.
            float stiffness = 120f;
            float baseDamping = 14f;
            if (TryEvaluateCurve(out var spec))
            {
                if (spec.stiffness > 0f) stiffness = spec.stiffness;
                if (spec.damping > 0f) baseDamping = spec.damping;
            }

            // Per-phase time constant — attack vs release (ms → seconds).
            bool rising = targetValue >= currentValue;
            float timeConstantMs = rising ? attackMs : releaseMs;
            float timeConstant = timeConstantMs > 0f ? timeConstantMs / 1000f : 0.1f;

            // Critically-damped semi-implicit Euler step.
            float dt = Time.deltaTime;
            float dx = targetValue - currentValue;
            float damping = baseDamping / Mathf.Max(0.01f, timeConstant);
            velocity += (stiffness * dx - damping * velocity) * dt;
            currentValue += velocity * dt;
        }

        /// <summary>Test hook — reset internal state (used by PlayMode smoke setup).</summary>
        public void ResetState(float initial)
        {
            currentValue = initial;
            targetValue = initial;
            velocity = 0f;
        }
    }
}
