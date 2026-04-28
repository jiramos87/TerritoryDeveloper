using System.Collections;
using Territory.UI.StudioControls;
using UnityEngine;
using UnityEngine.Events;

namespace Territory.UI.Juice
{
    /// <summary>
    /// UnityEvent-driven illumination pulse for <see cref="IlluminatedButton"/>. Listener attaches
    /// to <see cref="IlluminatedButton.OnClick"/> by default; tween fires alpha 0→1→0 across the
    /// configured <c>motion_curve</c> duration (first half ramps up, second half ramps down).
    /// </summary>
    [RequireComponent(typeof(IlluminatedButton))]
    public class PulseOnEvent : JuiceBase
    {
        [SerializeField] private UnityEvent triggerEvent;
        [SerializeField] private string illuminationSlug;
        [SerializeField] private float fallbackDurationMs = 250f;

        private IlluminatedButton _button;
        private Coroutine _activePulse;
        private bool _illuminationCached;

        /// <summary>Manual external trigger (used when no <see cref="UnityEvent"/> wiring is desired).</summary>
        public void Pulse()
        {
            if (_button == null) return;
            if (_activePulse != null) StopCoroutine(_activePulse);
            _activePulse = StartCoroutine(PulseRoutine());
        }

        /// <summary>External hook used by smoke fixtures + game-side wiring.</summary>
        public UnityEvent TriggerEvent => triggerEvent;

        protected override void Awake()
        {
            base.Awake();
            _button = GetComponent<IlluminatedButton>();
            if (string.IsNullOrEmpty(illuminationSlug) && _button?.Detail != null)
            {
                illuminationSlug = _button.Detail.illuminationSlug;
            }
        }

        private void OnEnable()
        {
            if (triggerEvent == null) triggerEvent = new UnityEvent();
            triggerEvent.AddListener(OnTriggerFired);

            // Default wiring: also fire on IlluminatedButton.OnClick when no explicit trigger is set up
            // by the bake handler (T5.5 default). Listener removed in OnDisable.
            if (_button != null) _button.OnClick.AddListener(OnTriggerFired);
        }

        private void OnDisable()
        {
            triggerEvent?.RemoveListener(OnTriggerFired);
            if (_button != null) _button.OnClick.RemoveListener(OnTriggerFired);
            if (_activePulse != null)
            {
                StopCoroutine(_activePulse);
                _activePulse = null;
            }
        }

        private void OnTriggerFired()
        {
            // Resolve illumination once on entry (slug → spec memo). Warn-once when missing.
            if (!_illuminationCached && theme != null && !string.IsNullOrEmpty(illuminationSlug))
            {
                if (!theme.TryGetIllumination(illuminationSlug, out _))
                {
                    Debug.LogWarning($"[PulseOnEvent] illumination slug not found (slug={illuminationSlug})");
                }
                _illuminationCached = true;
            }
            Pulse();
        }

        private IEnumerator PulseRoutine()
        {
            float durationMs = fallbackDurationMs;
            if (TryEvaluateCurve(out var spec) && spec.durationMs > 0f)
            {
                durationMs = spec.durationMs;
            }
            float duration = Mathf.Max(0.001f, durationMs / 1000f);
            float halfDuration = duration * 0.5f;
            float baseAlpha = _button.IlluminationAlpha;

            // Ramp 0 → 1 (relative to baseAlpha, capped at 1).
            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                _button.IlluminationAlpha = Mathf.Lerp(baseAlpha, 1f, t);
                yield return null;
            }
            _button.IlluminationAlpha = 1f;

            // Ramp 1 → baseAlpha.
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                _button.IlluminationAlpha = Mathf.Lerp(1f, baseAlpha, t);
                yield return null;
            }
            _button.IlluminationAlpha = baseAlpha;
            _activePulse = null;
        }
    }
}
