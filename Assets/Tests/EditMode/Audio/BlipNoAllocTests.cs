using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Territory.Audio;

namespace Territory.Tests.EditMode.Audio
{
    /// <summary>
    /// EditMode regression test asserting that steady-state <see cref="BlipVoice.Render"/>
    /// calls produce zero managed allocations per call.
    ///
    /// Measurement method:
    ///   1. Warm-up — 3 renders (absorbs JIT compile, first-call lazy init, Editor one-shots).
    ///   2. Measure — <c>GC.GetAllocatedBytesForCurrentThread</c> delta across 10 renders.
    ///   3. Assert delta / 10 ≤ 0 bytes/call (≤ 0 tolerates GC reclaim within window).
    ///
    /// Covers Blip Stage 1.4 Exit bullet 7 (no-alloc invariant — Step 1 scope).
    /// Guards against: boxing, closure capture, LINQ, array re-alloc inside Render loop.
    /// </summary>
    public class BlipNoAllocTests
    {
        private const int SampleRate    = 48000;
        private const int WarmupCount   = 3;
        private const int MeasureCount  = 10;

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
        /// After a 3-render warm-up, 10 renders with a 2-slot FX chain
        /// (BitCrush p0=6 + DcBlocker) must produce ≤ 0 managed bytes per call.
        /// </summary>
        [Test]
        public void Render_WithFxChain_ZeroManagedAlloc()
        {
            BlipPatchFlat patch = BuildPatchWithFx();

            var buf   = new float[SampleRate];
            var state = default(BlipVoiceState);

            // --- Warm-up ---
            for (int i = 0; i < WarmupCount; i++)
            {
                state = default(BlipVoiceState);
                BlipVoice.Render(buf, 0, buf.Length, SampleRate, in patch, 0, ref state);
            }

            // --- Measure ---
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < MeasureCount; i++)
            {
                state = default(BlipVoiceState);
                BlipVoice.Render(buf, 0, buf.Length, SampleRate, in patch, 0, ref state);
            }
            long delta = GC.GetAllocatedBytesForCurrentThread() - before;

            long deltaPerCall = delta / MeasureCount;

            Assert.That(deltaPerCall, Is.LessThanOrEqualTo(0L),
                $"Managed alloc with FX chain: total={delta} bytes over {MeasureCount} renders, " +
                $"{deltaPerCall} bytes/call. " +
                "Likely cause: boxing or alloc inside BlipFxChain.ProcessFx. " +
                $"First rendered sample: {buf[1]:G6}.");
        }

        /// <summary>
        /// After a 3-render warm-up, 10 steady-state renders must produce
        /// ≤ 0 managed bytes per call (delta / 10 ≤ 0).
        /// </summary>
        [Test]
        public void Render_SteadyState_ZeroManagedAlloc()
        {
            BlipPatchFlat patch = BuildPatch();

            // Preallocate output buffer and voice state outside measurement window.
            var buf   = new float[SampleRate]; // 1 s @ 48 kHz — preallocated once
            var state = default(BlipVoiceState);

            // --- Warm-up (absorb JIT, first-call lazy init, Editor instrumentation) ---
            for (int i = 0; i < WarmupCount; i++)
            {
                state = default(BlipVoiceState);
                BlipVoice.Render(buf, 0, buf.Length, SampleRate, in patch, 0, ref state);
            }

            // --- Measure managed alloc across MeasureCount steady-state renders ---
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < MeasureCount; i++)
            {
                state = default(BlipVoiceState);
                BlipVoice.Render(buf, 0, buf.Length, SampleRate, in patch, 0, ref state);
            }
            long delta = GC.GetAllocatedBytesForCurrentThread() - before;

            long deltaPerCall = delta / MeasureCount;

            Assert.That(deltaPerCall, Is.LessThanOrEqualTo(0L),
                $"Managed alloc in steady state: total={delta} bytes over {MeasureCount} renders, " +
                $"{deltaPerCall} bytes/call. " +
                "Likely cause: boxing, closure capture, or array re-alloc inside Render loop. " +
                $"First rendered sample (non-zero check): {buf[1]:G6}.");
        }

        // -----------------------------------------------------------------------
        // Render_WithChorus_ZeroManagedAlloc
        // -----------------------------------------------------------------------

        /// <summary>
        /// Pre-leases one chorus delay buffer outside the measurement window.
        /// After 3 warm-up renders, 10 measured renders with the delay-aware
        /// <see cref="BlipVoice.Render"/> overload must produce ≤ 0 managed
        /// bytes per call (delta / 10 ≤ 0).
        ///
        /// Covers Stage 5.2 no-alloc contract for the Chorus (heaviest delay path:
        /// Sin + 2 taps + modulo write-head).
        /// </summary>
        [Test]
        public void Render_WithChorus_ZeroManagedAlloc()
        {
            BlipPatchFlat patch = BuildChorusPatch();

            var buf   = new float[SampleRate];
            var state = default(BlipVoiceState);
            var pool  = new BlipDelayPool();

            // Pre-lease outside measurement window: ArrayPool.Rent allocates on first
            // call per size class; leasing inside the window would corrupt the assertion.
            float[] d0  = pool.Lease(SampleRate, maxDelayMs: 50f);
            int     len0 = d0.Length;
            int     wp0  = 0;

            // Scratch slots for ref writePos1..3 — C# forbids `ref _` discards on ref params.
            int     wp1  = 0, wp2 = 0, wp3 = 0;

            try
            {
                // --- Warm-up (absorb JIT, Editor instrumentation, cold ArrayPool) ---
                for (int i = 0; i < WarmupCount; i++)
                {
                    state = default; wp0 = 0; wp1 = 0; wp2 = 0; wp3 = 0;
                    BlipVoice.Render(buf, 0, buf.Length, SampleRate, in patch, 0, ref state,
                        d0, null, null, null, len0, 0, 0, 0,
                        ref wp0, ref wp1, ref wp2, ref wp3);
                }

                // --- Measure ---
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < MeasureCount; i++)
                {
                    state = default; wp0 = 0; wp1 = 0; wp2 = 0; wp3 = 0;
                    BlipVoice.Render(buf, 0, buf.Length, SampleRate, in patch, 0, ref state,
                        d0, null, null, null, len0, 0, 0, 0,
                        ref wp0, ref wp1, ref wp2, ref wp3);
                }
                long delta = GC.GetAllocatedBytesForCurrentThread() - before;

                long deltaPerCall = delta / MeasureCount;

                Assert.That(deltaPerCall, Is.LessThanOrEqualTo(0L),
                    $"Managed alloc with Chorus delay FX: total={delta} bytes over {MeasureCount} renders, " +
                    $"{deltaPerCall} bytes/call. " +
                    "Likely cause: boxing or alloc inside BlipFxChain.ProcessFx Chorus branch. " +
                    $"First rendered sample: {buf[1]:G6}.");
            }
            finally
            {
                pool.Return(d0);
            }
        }

        // -----------------------------------------------------------------------
        // BuildChorusPatch — slot 0 = Chorus (p0=1 Hz rate, p1=5 ms depth, p2=0.4 mix)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Creates a <see cref="BlipPatch"/> with the same base settings as
        /// <see cref="BuildPatch"/> but with a single FX slot:
        ///   slot 0 — Chorus, param0 = 1 (rate Hz), param1 = 5 (depth ms), param2 = 0.4 (mix).
        /// Used by <see cref="Render_WithChorus_ZeroManagedAlloc"/>.
        /// </summary>
        private BlipPatchFlat BuildChorusPatch()
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

            // --- Determinism + jitter ---
            SetField(so, "deterministic",    true,  privInst);
            SetField(so, "variantCount",     1,     privInst);
            SetField(so, "voiceLimit",       1,     privInst);
            SetField(so, "durationSeconds",  1f,    privInst);
            SetField(so, "pitchJitterCents", 10f,   privInst);
            SetField(so, "gainJitterDb",      2f,   privInst);
            SetField(so, "panJitter",         0.2f, privInst);

            // --- FX chain: slot 0 Chorus (rate=1Hz, depth=5ms, mix=0.4) ---
            var fxSlots = new BlipFxSlot[]
            {
                new BlipFxSlot { kind = BlipFxKind.Chorus, param0 = 1f, param1 = 5f, param2 = 0.4f },
            };
            SetField(so, "fxChain", fxSlots, privInst);

            // Force OnValidate (clamps + recomputes patchHash).
            InvokeOnValidate(so, privInst);

            return BlipPatchFlat.FromSO(so);
        }

        // -----------------------------------------------------------------------
        // BuildPatch — create + flatten BlipPatch SO via reflection
        // -----------------------------------------------------------------------

        /// <summary>
        /// Creates a <see cref="BlipPatch"/> ScriptableObject with:
        ///   - Sine oscillator, 440 Hz, gain 1.
        ///   - AHDSR A=50/H=0/D=100/S=0.5/R=50 ms (Linear).
        ///   - Filter disabled.
        ///   - deterministic = true.
        ///   - Non-zero jitter params (to exercise zero-guard branches without triggering jitter).
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
            SetField(so, "deterministic",    true,  privInst);
            SetField(so, "variantCount",     1,     privInst);
            SetField(so, "voiceLimit",       1,     privInst);
            SetField(so, "durationSeconds",  1f,    privInst);
            SetField(so, "pitchJitterCents", 10f,   privInst); // bypassed by deterministic path
            SetField(so, "gainJitterDb",      2f,   privInst); // bypassed by deterministic path
            SetField(so, "panJitter",         0.2f, privInst); // bypassed by deterministic path

            // Force OnValidate (clamps + recomputes patchHash).
            InvokeOnValidate(so, privInst);

            return BlipPatchFlat.FromSO(so);
        }

        // -----------------------------------------------------------------------
        // BuildPatchWithFx — 2-slot FX chain: BitCrush(p0=6) + DcBlocker
        // -----------------------------------------------------------------------

        /// <summary>
        /// Creates a <see cref="BlipPatch"/> with the same base settings as
        /// <see cref="BuildPatch"/> but with a 2-slot FX chain:
        ///   slot 0 — BitCrush, param0 = 6 (64 levels), param1 = 0.
        ///   slot 1 — DcBlocker, param0/param1 = 0.
        /// Used by <see cref="Render_WithFxChain_ZeroManagedAlloc"/>.
        /// </summary>
        private BlipPatchFlat BuildPatchWithFx()
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

            // --- Determinism + jitter ---
            SetField(so, "deterministic",    true,  privInst);
            SetField(so, "variantCount",     1,     privInst);
            SetField(so, "voiceLimit",       1,     privInst);
            SetField(so, "durationSeconds",  1f,    privInst);
            SetField(so, "pitchJitterCents", 10f,   privInst);
            SetField(so, "gainJitterDb",      2f,   privInst);
            SetField(so, "panJitter",         0.2f, privInst);

            // --- FX chain: slot 0 BitCrush(p0=6), slot 1 DcBlocker ---
            var fxSlots = new BlipFxSlot[]
            {
                new BlipFxSlot { kind = BlipFxKind.BitCrush,  param0 = 6f, param1 = 0f, param2 = 0f },
                new BlipFxSlot { kind = BlipFxKind.DcBlocker, param0 = 0f, param1 = 0f, param2 = 0f },
            };
            SetField(so, "fxChain", fxSlots, privInst);

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
                    $"[BlipNoAllocTests] Field '{fieldName}' not found on {target.GetType().FullName}. " +
                    "Check that BlipPatch private field names match.");
            fi.SetValue(target, value);
        }

        private static void InvokeOnValidate(object target, BindingFlags flags)
        {
            var mi = target.GetType().GetMethod("OnValidate", flags);
            if (mi == null)
                throw new InvalidOperationException(
                    $"[BlipNoAllocTests] OnValidate not found on {target.GetType().FullName}.");
            mi.Invoke(target, null);
        }
    }
}
