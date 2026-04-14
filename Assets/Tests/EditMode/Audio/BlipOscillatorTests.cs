using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Territory.Audio;

namespace Territory.Tests.EditMode.Audio
{
    /// <summary>
    /// EditMode tests asserting oscillator fundamental-frequency accuracy via zero-crossing proxy.
    /// Renders 1 s @ 48 kHz for each waveform kind at 440 Hz; asserts crossing count ≈ 880 ± 2.
    /// Tolerance absorbs 1-ms attack ramp + period-boundary rounding.
    /// </summary>
    public class BlipOscillatorTests
    {
        // Collected for TearDown cleanup.
        private BlipPatch _createdSo;

        // -----------------------------------------------------------------------
        // TearDown — destroy SO to keep Editor leak-tracker quiet
        // -----------------------------------------------------------------------

        [TearDown]
        public void TearDown()
        {
            if (_createdSo != null)
            {
                UnityEngine.Object.DestroyImmediate(_createdSo);
                _createdSo = null;
            }
        }

        // -----------------------------------------------------------------------
        // Tests
        // -----------------------------------------------------------------------

        [Test]
        public void OscSine_440Hz_OneSecond_Has880Crossings()
        {
            var patch = MakePatch(BlipWaveform.Sine);
            var buf   = BlipTestFixtures.RenderPatch(in patch, sampleRate: 48000, seconds: 1, variantIndex: 0);
            int count = BlipTestFixtures.CountZeroCrossings(buf);
            Assert.That(count, Is.EqualTo(880).Within(2),
                $"Sine 440 Hz crossing count {count} outside 880 ± 2");
        }

        [Test]
        public void OscTriangle_440Hz_OneSecond_Has880Crossings()
        {
            var patch = MakePatch(BlipWaveform.Triangle);
            var buf   = BlipTestFixtures.RenderPatch(in patch, sampleRate: 48000, seconds: 1, variantIndex: 0);
            int count = BlipTestFixtures.CountZeroCrossings(buf);
            Assert.That(count, Is.EqualTo(880).Within(2),
                $"Triangle 440 Hz crossing count {count} outside 880 ± 2");
        }

        [Test]
        public void OscSquare_440Hz_OneSecond_Has880Crossings()
        {
            var patch = MakePatch(BlipWaveform.Square);
            var buf   = BlipTestFixtures.RenderPatch(in patch, sampleRate: 48000, seconds: 1, variantIndex: 0);
            int count = BlipTestFixtures.CountZeroCrossings(buf);
            Assert.That(count, Is.EqualTo(880).Within(2),
                $"Square 440 Hz crossing count {count} outside 880 ± 2");
        }

        [Test]
        public void OscPulseDuty50_440Hz_OneSecond_Has880Crossings()
        {
            var patch = MakePatch(BlipWaveform.Pulse, pulseDuty: 0.5f);
            var buf   = BlipTestFixtures.RenderPatch(in patch, sampleRate: 48000, seconds: 1, variantIndex: 0);
            int count = BlipTestFixtures.CountZeroCrossings(buf);
            Assert.That(count, Is.EqualTo(880).Within(2),
                $"Pulse duty=0.5 440 Hz crossing count {count} outside 880 ± 2");
        }

        // -----------------------------------------------------------------------
        // MakePatch — build + flatten a BlipPatch SO via reflection
        // -----------------------------------------------------------------------

        /// <summary>
        /// Creates a <see cref="BlipPatch"/> ScriptableObject, sets serialized fields via
        /// reflection (no public setters exist on the SO), forces <c>OnValidate</c>, then
        /// flattens via <see cref="BlipPatchFlat.FromSO"/>.
        /// </summary>
        /// <param name="waveform">Oscillator waveform shape.</param>
        /// <param name="pulseDuty">Pulse duty cycle [0..1]; only meaningful for <c>Pulse</c>.</param>
        /// <returns>Immutable blittable patch ready for <see cref="BlipTestFixtures.RenderPatch"/>.</returns>
        private BlipPatchFlat MakePatch(BlipWaveform waveform, float pulseDuty = 0.5f)
        {
            var so = ScriptableObject.CreateInstance<BlipPatch>();
            _createdSo = so;

            const BindingFlags privInst = BindingFlags.NonPublic | BindingFlags.Instance;

            // --- Oscillator array (1 slot) ---
            var osc = new BlipOscillator
            {
                waveform    = waveform,
                frequencyHz = 440f,
                detuneCents = 0f,
                pulseDuty   = pulseDuty,
                gain        = 1f,
            };
            SetField(so, "oscillators", new BlipOscillator[] { osc }, privInst);

            // --- Envelope (attack/release clamped ≥ 1 ms by OnValidate; hold >> 1 s) ---
            var env = new BlipEnvelope
            {
                attackMs     = 1f,
                attackShape  = BlipEnvShape.Linear,
                holdMs       = 2000f,
                decayMs      = 0f,
                decayShape   = BlipEnvShape.Linear,
                sustainLevel = 1f,
                releaseMs    = 1f,
                releaseShape = BlipEnvShape.Linear,
            };
            SetField(so, "envelope", env, privInst);

            // --- Filter disabled ---
            var flt = new BlipFilter
            {
                kind     = BlipFilterKind.None,
                cutoffHz = 0f,
            };
            SetField(so, "filter", flt, privInst);

            // --- Determinism + voice management ---
            SetField(so, "deterministic",  true, privInst);
            SetField(so, "variantCount",   1,    privInst);
            SetField(so, "voiceLimit",     1,    privInst);

            // Force OnValidate (clamps + recomputes patchHash).
            InvokeOnValidate(so, privInst);

            return BlipPatchFlat.FromSO(so);
        }

        // -----------------------------------------------------------------------
        // Reflection helpers
        // -----------------------------------------------------------------------

        private static void SetField(object target, string fieldName, object value, BindingFlags flags)
        {
            var fi = target.GetType().GetField(fieldName, flags);
            if (fi == null)
                throw new InvalidOperationException(
                    $"[BlipOscillatorTests] Field '{fieldName}' not found on {target.GetType().FullName}. " +
                    "Check that BlipPatch private field names match.");
            fi.SetValue(target, value);
        }

        private static void InvokeOnValidate(object target, BindingFlags flags)
        {
            var mi = target.GetType().GetMethod("OnValidate", flags);
            if (mi == null)
                throw new InvalidOperationException(
                    $"[BlipOscillatorTests] OnValidate not found on {target.GetType().FullName}.");
            mi.Invoke(target, null);
        }
    }
}
