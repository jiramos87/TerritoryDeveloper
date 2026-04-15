using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Territory.Audio;

namespace Territory.Tests.EditMode.Audio
{
    /// <summary>
    /// EditMode tests asserting that <see cref="BlipVoice.Render"/> produces identical
    /// output on two consecutive calls with the same patch, sample rate, and variantIndex
    /// when <c>deterministic = true</c>.
    ///
    /// Covers Blip Stage 1.4 Exit bullet 6 (determinism).
    /// Guards against:
    ///   - Stale filter z1 state leaking between invocations.
    ///   - RNG not re-seeded per call (xorshift32 rngState reset via variantIndex + 1).
    ///   - Phase accumulators not zeroed on fresh BlipVoiceState.
    /// </summary>
    public class BlipDeterminismTests
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
        /// Two renders of the same patch + variantIndex using fresh BlipVoiceState
        /// each time must produce identical buffers:
        ///   1. SumAbsHash delta &lt; 1e-6 (L1-norm fingerprint; catches deep drift).
        ///   2. First 256 samples exactly equal (bit-for-bit; catches early state leak).
        /// Non-zero jitter params are set but must be bypassed by deterministic=true path.
        /// </summary>
        [Test]
        public void RenderPatch_SameSeedVariant_ProducesDeterministicBuffer()
        {
            const int SampleRate   = 48000;
            const int Seconds      = 1;
            const int VariantIndex = 0;
            const int PrefixLen    = 256;

            BlipPatchFlat patch = BuildPatch();

            float[] bufA = BlipTestFixtures.RenderPatch(in patch, SampleRate, Seconds, variantIndex: VariantIndex);
            float[] bufB = BlipTestFixtures.RenderPatch(in patch, SampleRate, Seconds, variantIndex: VariantIndex);

            // --- Gate 1: L1-norm hash delta < 1e-6 ---
            double hashA = BlipTestFixtures.SumAbsHash(bufA);
            double hashB = BlipTestFixtures.SumAbsHash(bufB);
            Assert.That(Math.Abs(hashA - hashB), Is.LessThan(1e-6),
                $"SumAbsHash mismatch: |{hashA:G10} - {hashB:G10}| >= 1e-6. " +
                "Likely cause: stale filter z1 or RNG rngState not reset between calls.");

            // --- Gate 2: first 256 samples exactly equal (no Linq) ---
            for (int i = 0; i < PrefixLen; i++)
            {
                Assert.That(bufA[i], Is.EqualTo(bufB[i]),
                    $"Sample mismatch at index {i}: bufA={bufA[i]:G9} bufB={bufB[i]:G9}. " +
                    "Likely cause: phase accumulator or rngState not zero on fresh BlipVoiceState.");
            }
        }

        // -----------------------------------------------------------------------
        // BuildPatch — create + flatten BlipPatch SO via reflection
        // -----------------------------------------------------------------------

        /// <summary>
        /// Creates a <see cref="BlipPatch"/> ScriptableObject with:
        ///   - Sine oscillator, 440 Hz, gain 1.
        ///   - AHDSR A=50/H=0/D=100/S=0.5/R=50 ms (Linear).
        ///   - Filter disabled.
        ///   - deterministic = true (deterministic RNG path: rngState = variantIndex + 1).
        ///   - Non-zero pitchJitterCents / gainJitterDb / panJitter — exercised only to
        ///     prove the deterministic path bypasses jitter XOR-seed logic entirely.
        /// </summary>
        private BlipPatchFlat BuildPatch()
        {
            var so = ScriptableObject.CreateInstance<BlipPatch>();
            _createdSo = so;

            const BindingFlags privInst = BindingFlags.NonPublic | BindingFlags.Instance;

            // --- Oscillator (sine, 440 Hz) ---
            var osc = new BlipOscillator
            {
                waveform    = BlipWaveform.Sine,
                frequencyHz = 440f,
                detuneCents = 0f,
                pulseDuty   = 0.5f,
                gain        = 1f,
            };
            SetField(so, "oscillators", new BlipOscillator[] { osc }, privInst);

            // --- Envelope: A=50/H=0/D=100/S=0.5/R=50 ms ---
            var env = new BlipEnvelope
            {
                attackMs     = 50f,
                attackShape  = BlipEnvShape.Linear,
                holdMs       = 0f,
                decayMs      = 100f,
                decayShape   = BlipEnvShape.Linear,
                sustainLevel = 0.5f,
                releaseMs    = 50f,
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

            // --- Determinism + jitter (non-zero to exercise zero-guard branches) ---
            SetField(so, "deterministic",     true,  privInst);
            SetField(so, "variantCount",      1,     privInst);
            SetField(so, "voiceLimit",        1,     privInst);
            SetField(so, "durationSeconds",   1f,    privInst);
            SetField(so, "pitchJitterCents",  10f,   privInst); // bypassed by deterministic path
            SetField(so, "gainJitterDb",       2f,   privInst); // bypassed by deterministic path
            SetField(so, "panJitter",          0.2f, privInst); // bypassed by deterministic path

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
                    $"[BlipDeterminismTests] Field '{fieldName}' not found on {target.GetType().FullName}. " +
                    "Check that BlipPatch private field names match.");
            fi.SetValue(target, value);
        }

        private static void InvokeOnValidate(object target, BindingFlags flags)
        {
            var mi = target.GetType().GetMethod("OnValidate", flags);
            if (mi == null)
                throw new InvalidOperationException(
                    $"[BlipDeterminismTests] OnValidate not found on {target.GetType().FullName}.");
            mi.Invoke(target, null);
        }
    }
}
