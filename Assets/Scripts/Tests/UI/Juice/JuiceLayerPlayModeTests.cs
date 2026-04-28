using System.Collections;
using NUnit.Framework;
using Territory.UI;
using Territory.UI.Juice;
using Territory.UI.StudioControls;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Territory.Tests.PlayMode.UI.Juice
{
    /// <summary>
    /// PlayMode smoke tests for Stage 5 JuiceLayer behaviors. Scripted scaffolds (no baked prefab
    /// dependency) exercise <see cref="NeedleBallistics"/> spring step, <see cref="PulseOnEvent"/>
    /// alpha tween, and <see cref="TweenCounter"/> intermediate-digit emission. Fallback durations
    /// (motion_curve unavailable in fixture) keep tests deterministic without a UiTheme asset.
    /// </summary>
    public sealed class JuiceLayerPlayModeTests
    {
        private GameObject _root;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Runtime init (BlipBootstrap, ZoneSubTypeRegistry, etc.) emits Debug.LogError on
            // PlayMode test entry; LogAssert.ignoreFailingMessages prevents those baseline
            // log leaks from auto-failing JuiceLayer-scoped assertions.
            LogAssert.ignoreFailingMessages = true;
            _root = new GameObject("JuiceLayerTestRoot");
            yield return null; // settle Awake cascade
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_root != null) Object.Destroy(_root);
            _root = null;
            yield return null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // T5.6.A — NeedleBallistics rises toward target via spring step.
        // Mid-tween: needle strictly between start (0) and target (1).
        // Settle: needle within tolerance of target.
        // ─────────────────────────────────────────────────────────────────────
        [UnityTest]
        public IEnumerator NeedleBallistics_RampsToTarget()
        {
            var go = new GameObject("VUMeter_test", typeof(RectTransform));
            go.transform.SetParent(_root.transform, false);
            var meter = go.AddComponent<VUMeter>();
            meter.ApplyDetail(new VUMeterDetail { attackMs = 200f, releaseMs = 200f, range = 1f });

            var needle = go.AddComponent<NeedleBallistics>();
            needle.ResetState(0f);
            needle.TargetValue = 1f;

            // Mid-tween sample (~half attack window) — value must have moved off zero.
            yield return new WaitForSeconds(0.1f);
            float midSample = needle.CurrentValue;
            Assert.Greater(midSample, 0f, "NeedleBallistics should leave start within attack window");
            Assert.Less(midSample, 1.5f, "NeedleBallistics mid-tween should not overshoot wildly");

            // Settle window — let spring relax to target.
            yield return new WaitForSeconds(1.0f);
            float settled = needle.CurrentValue;
            Assert.Less(Mathf.Abs(settled - 1f), 0.05f,
                $"NeedleBallistics did not settle near target=1.0 (settled={settled})");
        }

        // ─────────────────────────────────────────────────────────────────────
        // T5.6.B — PulseOnEvent ramps alpha up then back to base on Pulse().
        // Direct Pulse() call (avoid IlluminatedButton.OnClick wiring nuance);
        // half-duration sample shows alpha > base; settle returns to base.
        // ─────────────────────────────────────────────────────────────────────
        [UnityTest]
        public IEnumerator PulseOnEvent_FiresAndDecays()
        {
            var go = new GameObject("IlluminatedButton_test", typeof(RectTransform));
            go.transform.SetParent(_root.transform, false);
            var button = go.AddComponent<IlluminatedButton>();
            button.IlluminationAlpha = 0.4f;
            float baseAlpha = button.IlluminationAlpha;

            var pulse = go.AddComponent<PulseOnEvent>();
            // Manual entry — avoids OnEnable wiring race + missing-theme warn path.
            pulse.Pulse();

            // ~Half fallback duration (250 ms) — alpha must exceed baseline.
            yield return new WaitForSeconds(0.12f);
            Assert.Greater(button.IlluminationAlpha, baseAlpha + 0.05f,
                $"PulseOnEvent did not raise alpha above baseline (alpha={button.IlluminationAlpha}, base={baseAlpha})");

            // Settle window — alpha returns to base.
            yield return new WaitForSeconds(0.5f);
            Assert.Less(Mathf.Abs(button.IlluminationAlpha - baseAlpha), 0.05f,
                $"PulseOnEvent did not return alpha to baseline (alpha={button.IlluminationAlpha}, base={baseAlpha})");
        }

        // ─────────────────────────────────────────────────────────────────────
        // T5.6.C — TweenCounter writes intermediate digits to SegmentedReadout
        // during a tween from 0 → 100. Mid-tween sample strictly inside (0, 100).
        // ─────────────────────────────────────────────────────────────────────
        [UnityTest]
        public IEnumerator TweenCounter_ProducesIntermediates()
        {
            var go = new GameObject("SegmentedReadout_test", typeof(RectTransform));
            go.transform.SetParent(_root.transform, false);
            var readout = go.AddComponent<SegmentedReadout>();
            readout.CurrentValue = 0;

            var tween = go.AddComponent<TweenCounter>();
            tween.SetTarget(100);

            // Mid-tween sample at ~half fallback duration (500 ms).
            yield return new WaitForSeconds(0.25f);
            int midDigit = readout.CurrentValue;
            Assert.Greater(midDigit, 0, "TweenCounter mid-tween digit should exceed start (0)");
            Assert.Less(midDigit, 100, "TweenCounter mid-tween digit should be below target (100)");

            // Settle window — digit lands on target exactly.
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(100, readout.CurrentValue,
                "TweenCounter did not land on target after settle window");
        }
    }
}
