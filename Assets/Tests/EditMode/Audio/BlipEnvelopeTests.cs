using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Territory.Audio;

namespace Territory.Tests.EditMode.Audio
{
    /// <summary>
    /// EditMode tests asserting AHDSR envelope shape (Linear + Exponential) monotonicity
    /// and silence guard (all-zero buffer when oscillator gain = 0).
    /// Covers Blip Stage 1.4 EditMode Exit bullets 4 + 5 (envelope shape + silence).
    /// </summary>
    public class BlipEnvelopeTests
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

        /// <summary>
        /// Linear A=50/H=0/D=50/S=0.5/R=50 ms patch rendered at 48 kHz.
        /// Assert Attack monotone non-decreasing, Decay monotone non-increasing toward
        /// sustainLevel, Release monotone non-increasing toward 0.
        /// </summary>
        [Test]
        public void Envelope_Linear_MonotonicPerSegment()
        {
            const int SampleRate = 48000;
            const int StageMs    = 50;
            const int Stride     = 16;

            var patch = MakePatch(
                BlipEnvShape.Linear, BlipEnvShape.Linear, BlipEnvShape.Linear,
                sustainLevel: 0.5f, oscGain: 1f);

            float[] buf = RenderEnvelope(patch, SampleRate);
            float[] seg = BlipTestFixtures.SampleEnvelopeLevels(buf, Stride);

            int attackEnd  = SampleRate * StageMs / 1000 / Stride; // ~150
            int decayEnd   = attackEnd + SampleRate * StageMs / 1000 / Stride; // ~300
            int releaseEnd = decayEnd  + SampleRate * StageMs / 1000 / Stride; // ~450

            // Attack: non-decreasing
            AssertMonotonicallyNonDecreasing(seg, 0, attackEnd,
                "Linear Attack");

            // Decay: non-increasing
            AssertMonotonicallyNonIncreasing(seg, attackEnd, decayEnd,
                "Linear Decay");

            // Release: non-increasing toward 0
            AssertMonotonicallyNonIncreasing(seg, decayEnd, releaseEnd,
                "Linear Release");
        }

        /// <summary>
        /// Exponential A=50/H=0/D=50/S=0.5/R=50 ms patch rendered at 48 kHz.
        /// Same monotonicity assertions as Linear test.
        /// </summary>
        [Test]
        public void Envelope_Exponential_MonotonicPerSegment()
        {
            const int SampleRate = 48000;
            const int StageMs    = 50;
            const int Stride     = 16;

            var patch = MakePatch(
                BlipEnvShape.Exponential, BlipEnvShape.Exponential, BlipEnvShape.Exponential,
                sustainLevel: 0.5f, oscGain: 1f);

            float[] buf = RenderEnvelope(patch, SampleRate);
            float[] seg = BlipTestFixtures.SampleEnvelopeLevels(buf, Stride);

            int attackEnd  = SampleRate * StageMs / 1000 / Stride;
            int decayEnd   = attackEnd + SampleRate * StageMs / 1000 / Stride;
            int releaseEnd = decayEnd  + SampleRate * StageMs / 1000 / Stride;

            AssertMonotonicallyNonDecreasing(seg, 0, attackEnd,
                "Exponential Attack");

            AssertMonotonicallyNonIncreasing(seg, attackEnd, decayEnd,
                "Exponential Decay");

            AssertMonotonicallyNonIncreasing(seg, decayEnd, releaseEnd,
                "Exponential Release");
        }

        /// <summary>
        /// Exponential attack front-loading: slope in first quarter > slope in last quarter.
        /// Signature of τ=stageDur/4 exponential: level rises fast near t=0, slow near t=end.
        /// </summary>
        [Test]
        public void Envelope_Exponential_AttackFrontLoaded()
        {
            const int SampleRate = 48000;
            const int StageMs    = 50;
            const int Stride     = 16;

            var patch = MakePatch(
                BlipEnvShape.Exponential, BlipEnvShape.Exponential, BlipEnvShape.Exponential,
                sustainLevel: 0.5f, oscGain: 1f);

            float[] buf = RenderEnvelope(patch, SampleRate);
            float[] seg = BlipTestFixtures.SampleEnvelopeLevels(buf, Stride);

            int attackSegLen = SampleRate * StageMs / 1000 / Stride; // ~150
            int q1           = attackSegLen / 4;
            int q3           = (attackSegLen * 3) / 4;
            int end          = attackSegLen - 1;

            float slopeFirst = seg[q1]  - seg[0];
            float slopeLast  = seg[end] - seg[q3];

            Assert.That(slopeFirst, Is.GreaterThan(slopeLast),
                $"Exponential attack not front-loaded: first-quarter slope {slopeFirst:F6} vs last-quarter slope {slopeLast:F6}");
        }

        /// <summary>
        /// Patch with oscillator gain = 0 must produce an all-zero buffer.
        /// IIR filter has zero input so filterZ1 stays 0; no tolerance needed.
        /// </summary>
        [Test]
        public void Envelope_OscGainZero_BufferIsAllZero()
        {
            const int SampleRate = 48000;

            var patch = MakePatch(
                BlipEnvShape.Linear, BlipEnvShape.Linear, BlipEnvShape.Linear,
                sustainLevel: 0.5f, oscGain: 0f);

            float[] buf = RenderEnvelope(patch, SampleRate);

            Assert.That(buf, Is.All.EqualTo(0f),
                "Buffer must be all-zero when oscillator gain = 0");
        }

        // -----------------------------------------------------------------------
        // Render helper — 0.2 s buffer (9600 samples @ 48 kHz)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Renders the patch into a 9600-sample (200 ms @ 48 kHz) buffer.
        /// Drives <see cref="BlipVoice.Render"/> directly to allow sub-second durations.
        /// </summary>
        private static float[] RenderEnvelope(in BlipPatchFlat patch, int sampleRate)
        {
            // 200 ms covers A(50)+D(50)+S+R(50) plus post-release idle tail.
            int length = sampleRate * 200 / 1000; // 9600 @ 48 kHz
            var buf    = new float[length];
            var state  = default(BlipVoiceState);
            BlipVoice.Render(buf, 0, length, sampleRate, in patch, 0, ref state);
            return buf;
        }

        // -----------------------------------------------------------------------
        // MakePatch — build + flatten a BlipPatch SO via reflection
        // -----------------------------------------------------------------------

        /// <summary>
        /// Creates a <see cref="BlipPatch"/> ScriptableObject, sets serialized fields via
        /// reflection, forces <c>OnValidate</c>, then flattens via <see cref="BlipPatchFlat.FromSO"/>.
        /// Default: A=50 / H=0 / D=50 / S=sustainLevel / R=50 ms; sine 440 Hz; filter None;
        /// deterministic=true; durationSeconds=0.1f (Release fires at 100 ms).
        /// </summary>
        private BlipPatchFlat MakePatch(
            BlipEnvShape attackShape,
            BlipEnvShape decayShape,
            BlipEnvShape releaseShape,
            float        sustainLevel,
            float        oscGain)
        {
            var so = ScriptableObject.CreateInstance<BlipPatch>();
            _createdSo = so;

            const BindingFlags privInst = BindingFlags.NonPublic | BindingFlags.Instance;

            // --- Oscillator array (1 slot) ---
            // frequencyHz = 3000 Hz chosen so period = 48000/3000 = 16 samples = stride.
            // Each SampleEnvelopeLevels(buf, 16) sample lands at the same oscillator phase,
            // so consecutive abs values differ only by envelope evolution (not oscillator phase).
            var osc = new BlipOscillator
            {
                waveform    = BlipWaveform.Sine,
                frequencyHz = 3000f,
                detuneCents = 0f,
                pulseDuty   = 0.5f,
                gain        = oscGain,
            };
            SetField(so, "oscillators", new BlipOscillator[] { osc }, privInst);

            // --- Envelope: A=50 / H=0 / D=50 / S=sustainLevel / R=50 ms ---
            var env = new BlipEnvelope
            {
                attackMs     = 50f,
                attackShape  = attackShape,
                holdMs       = 0f,
                decayMs      = 50f,
                decayShape   = decayShape,
                sustainLevel = sustainLevel,
                releaseMs    = 50f,
                releaseShape = releaseShape,
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
            SetField(so, "deterministic",   true,  privInst);
            SetField(so, "variantCount",    1,     privInst);
            SetField(so, "voiceLimit",      1,     privInst);
            SetField(so, "durationSeconds", 0.1f,  privInst);
            // oscillatorCount is derived from oscillators.Length in FromSO — no private field needed.

            // Force OnValidate (clamps + recomputes patchHash).
            InvokeOnValidate(so, privInst);

            return BlipPatchFlat.FromSO(so);
        }

        // -----------------------------------------------------------------------
        // Monotonicity assertion helpers
        // -----------------------------------------------------------------------

        private static void AssertMonotonicallyNonDecreasing(
            float[] seg, int startIdx, int endIdx, string label)
        {
            for (int i = startIdx + 1; i < endIdx && i < seg.Length; i++)
            {
                Assert.That(seg[i], Is.GreaterThanOrEqualTo(seg[i - 1] - 1e-5f),
                    $"{label}: non-decreasing violated at index {i} " +
                    $"(seg[{i}]={seg[i]:F6} < seg[{i - 1}]={seg[i - 1]:F6})");
            }
        }

        private static void AssertMonotonicallyNonIncreasing(
            float[] seg, int startIdx, int endIdx, string label)
        {
            for (int i = startIdx + 1; i < endIdx && i < seg.Length; i++)
            {
                Assert.That(seg[i], Is.LessThanOrEqualTo(seg[i - 1] + 1e-5f),
                    $"{label}: non-increasing violated at index {i} " +
                    $"(seg[{i}]={seg[i]:F6} > seg[{i - 1}]={seg[i - 1]:F6})");
            }
        }

        // -----------------------------------------------------------------------
        // Reflection helpers
        // -----------------------------------------------------------------------

        private static void SetField(object target, string fieldName, object value, BindingFlags flags)
        {
            var fi = target.GetType().GetField(fieldName, flags);
            if (fi == null)
                throw new InvalidOperationException(
                    $"[BlipEnvelopeTests] Field '{fieldName}' not found on {target.GetType().FullName}. " +
                    "Check that BlipPatch private field names match.");
            fi.SetValue(target, value);
        }

        private static void InvokeOnValidate(object target, BindingFlags flags)
        {
            var mi = target.GetType().GetMethod("OnValidate", flags);
            if (mi == null)
                throw new InvalidOperationException(
                    $"[BlipEnvelopeTests] OnValidate not found on {target.GetType().FullName}.");
            mi.Invoke(target, null);
        }
    }
}
