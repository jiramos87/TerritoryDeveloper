using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Territory.Audio;

namespace Territory.Tests.EditMode.Audio
{
    /// <summary>
    /// EditMode tests for <see cref="BlipBaker"/> core render path (Stage 2.1 Phase 1).
    ///
    /// Covers:
    ///   1. Shape — <c>clip.samples == lengthSamples</c>, <c>channels == 1</c>, <c>frequency == sampleRate</c>.
    ///   2. Name  — clip name starts with "Blip_" and contains the hex hash + variant token.
    ///   3. Background-thread tripwire — <see cref="BlipBaker.BakeOrGet"/> throws
    ///      <see cref="InvalidOperationException"/> when called off the main thread.
    ///   4. <see cref="BlipBootstrap.MainThreadId"/> captured correctly (non-zero after SetUp).
    /// </summary>
    public class BlipBakerTests
    {
        private BlipPatch _createdSo;
        private AudioClip _createdClip;

        // -----------------------------------------------------------------------
        // SetUp / TearDown
        // -----------------------------------------------------------------------

        /// <summary>
        /// Writes main-thread id into <see cref="BlipBootstrap.MainThreadId"/> via reflection
        /// before each test. The MonoBehaviour's <c>Awake</c> normally does this at scene load;
        /// in EditMode we must do it manually.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            SetMainThreadId(Thread.CurrentThread.ManagedThreadId);
        }

        [TearDown]
        public void TearDown()
        {
            if (_createdSo != null)
            {
                UnityEngine.Object.DestroyImmediate(_createdSo);
                _createdSo = null;
            }
            if (_createdClip != null)
            {
                UnityEngine.Object.DestroyImmediate(_createdClip);
                _createdClip = null;
            }
        }

        // -----------------------------------------------------------------------
        // 1. Shape test
        // -----------------------------------------------------------------------

        /// <summary>
        /// <see cref="BlipBaker.BakeOrGet"/> must return a non-null <see cref="AudioClip"/>
        /// where <c>clip.samples == (int)(patch.durationSeconds * sampleRate)</c>,
        /// <c>clip.channels == 1</c>, and <c>clip.frequency == sampleRate</c>.
        /// </summary>
        [Test]
        public void BakeOrGet_ReturnsMono_WithCorrectShape()
        {
            const int SampleRate = 44100;
            BlipPatchFlat patch  = BuildPatch(durationSeconds: 0.5f);
            int patchHash        = GetPatchHash();

            var baker = new BlipBaker(SampleRate);
            _createdClip = baker.BakeOrGet(in patch, patchHash, variantIndex: 0);

            int expectedSamples = (int)(0.5f * SampleRate);

            Assert.That(_createdClip,            Is.Not.Null,            "BakeOrGet returned null.");
            Assert.That(_createdClip.samples,    Is.EqualTo(expectedSamples), $"samples: expected {expectedSamples}.");
            Assert.That(_createdClip.channels,   Is.EqualTo(1),          "channels must be 1 (mono).");
            Assert.That(_createdClip.frequency,  Is.EqualTo(SampleRate), "frequency must equal sampleRate ctor arg.");
        }

        // -----------------------------------------------------------------------
        // 2. Name test
        // -----------------------------------------------------------------------

        /// <summary>
        /// Clip name must follow <c>"Blip_{patchHash:X8}_v{variantIndex}"</c>.
        /// </summary>
        [Test]
        public void BakeOrGet_ClipName_StartsWithBlip_ContainsHexHash()
        {
            const int SampleRate   = 44100;
            const int VariantIndex = 2;
            BlipPatchFlat patch    = BuildPatch(durationSeconds: 0.1f);
            int patchHash          = GetPatchHash();

            var baker = new BlipBaker(SampleRate);
            _createdClip = baker.BakeOrGet(in patch, patchHash, VariantIndex);

            string name            = _createdClip.name;
            string expectedName    = $"Blip_{patchHash:X8}_v{VariantIndex}";

            Assert.That(name, Does.StartWith("Blip_"),
                $"Clip name '{name}' does not start with 'Blip_'.");
            Assert.That(name, Is.EqualTo(expectedName),
                $"Clip name '{name}' does not match expected '{expectedName}'.");
        }

        // -----------------------------------------------------------------------
        // 3. Background-thread tripwire
        // -----------------------------------------------------------------------

        /// <summary>
        /// Invoking <see cref="BlipBaker.BakeOrGet"/> from a background thread must throw
        /// <see cref="InvalidOperationException"/>. The exception is unwrapped from the
        /// <see cref="AggregateException"/> produced by <c>Task.Run</c>.
        /// </summary>
        [Test]
        public void BakeOrGet_FromBackgroundThread_ThrowsInvalidOperationException()
        {
            const int SampleRate = 44100;
            BlipPatchFlat patch  = BuildPatch(durationSeconds: 0.05f);
            int patchHash        = GetPatchHash();
            var baker            = new BlipBaker(SampleRate);

            AggregateException? agg = null;
            try
            {
                Task.Run(() => baker.BakeOrGet(in patch, patchHash, 0)).Wait();
            }
            catch (AggregateException ex)
            {
                agg = ex;
            }

            Assert.That(agg,                          Is.Not.Null,  "No AggregateException thrown from background-thread bake.");
            Assert.That(agg!.InnerException,          Is.Not.Null,  "AggregateException has no InnerException.");
            Assert.That(agg.InnerException,           Is.InstanceOf<InvalidOperationException>(),
                $"Expected InvalidOperationException; got {agg.InnerException?.GetType().Name}.");
        }

        // -----------------------------------------------------------------------
        // 4. MainThreadId captured (non-zero)
        // -----------------------------------------------------------------------

        /// <summary>
        /// After <see cref="SetUp"/> injects the main-thread id, it must equal the
        /// current thread's id and be non-zero.
        /// </summary>
        [Test]
        public void BlipBootstrap_MainThreadId_IsNonZeroAndMatchesCurrentThread()
        {
            int captured = BlipBootstrap.MainThreadId;
            int expected = Thread.CurrentThread.ManagedThreadId;

            Assert.That(captured, Is.Not.EqualTo(0),   "MainThreadId must not be zero.");
            Assert.That(captured, Is.EqualTo(expected), "MainThreadId must match current (main) thread id.");
        }

        // -----------------------------------------------------------------------
        // Helpers — patch construction
        // -----------------------------------------------------------------------

        /// <summary>
        /// Creates and flattens a minimal deterministic <see cref="BlipPatch"/> SO.
        /// </summary>
        private BlipPatchFlat BuildPatch(float durationSeconds = 0.1f)
        {
            var so = ScriptableObject.CreateInstance<BlipPatch>();
            _createdSo = so;

            const BindingFlags privInst = BindingFlags.NonPublic | BindingFlags.Instance;

            // Sine oscillator, 440 Hz
            var osc = new BlipOscillator
            {
                waveform    = BlipWaveform.Sine,
                frequencyHz = 440f,
                detuneCents = 0f,
                pulseDuty   = 0.5f,
                gain        = 1f,
            };
            SetField(so, "oscillators", new BlipOscillator[] { osc }, privInst);

            // Minimal flat envelope
            var env = new BlipEnvelope
            {
                attackMs     = 5f,
                attackShape  = BlipEnvShape.Linear,
                holdMs       = 0f,
                decayMs      = 10f,
                decayShape   = BlipEnvShape.Linear,
                sustainLevel = 0.5f,
                releaseMs    = 5f,
                releaseShape = BlipEnvShape.Linear,
            };
            SetField(so, "envelope", env, privInst);

            // Filter off
            var flt = new BlipFilter { kind = BlipFilterKind.None, cutoffHz = 0f };
            SetField(so, "filter", flt, privInst);

            SetField(so, "deterministic",   true,            privInst);
            SetField(so, "variantCount",    1,               privInst);
            SetField(so, "voiceLimit",      1,               privInst);
            SetField(so, "durationSeconds", durationSeconds, privInst);

            InvokeOnValidate(so, privInst);

            return BlipPatchFlat.FromSO(so);
        }

        /// <summary>Returns the hash from the last SO created by <see cref="BuildPatch"/>.</summary>
        private int GetPatchHash()
        {
            if (_createdSo == null)
                throw new InvalidOperationException("[BlipBakerTests] BuildPatch must be called before GetPatchHash.");
            const BindingFlags privInst = BindingFlags.NonPublic | BindingFlags.Instance;
            var fi = _createdSo.GetType().GetField("patchHash", privInst);
            if (fi == null)
                throw new InvalidOperationException("[BlipBakerTests] 'patchHash' field not found on BlipPatch.");
            return (int)fi.GetValue(_createdSo);
        }

        // -----------------------------------------------------------------------
        // Reflection helpers
        // -----------------------------------------------------------------------

        private static void SetField(object target, string fieldName, object value, BindingFlags flags)
        {
            var fi = target.GetType().GetField(fieldName, flags);
            if (fi == null)
                throw new InvalidOperationException(
                    $"[BlipBakerTests] Field '{fieldName}' not found on {target.GetType().FullName}.");
            fi.SetValue(target, value);
        }

        private static void InvokeOnValidate(object target, BindingFlags flags)
        {
            var mi = target.GetType().GetMethod("OnValidate", flags);
            if (mi == null)
                throw new InvalidOperationException(
                    $"[BlipBakerTests] OnValidate not found on {target.GetType().FullName}.");
            mi.Invoke(target, null);
        }

        /// <summary>
        /// Injects <paramref name="threadId"/> into <see cref="BlipBootstrap.MainThreadId"/>
        /// via the property's private setter.
        /// </summary>
        private static void SetMainThreadId(int threadId)
        {
            var prop = typeof(BlipBootstrap).GetProperty(
                nameof(BlipBootstrap.MainThreadId),
                BindingFlags.Public | BindingFlags.Static);
            if (prop == null)
                throw new InvalidOperationException(
                    "[BlipBakerTests] BlipBootstrap.MainThreadId property not found.");
            var setter = prop.GetSetMethod(nonPublic: true);
            if (setter == null)
                throw new InvalidOperationException(
                    "[BlipBakerTests] BlipBootstrap.MainThreadId has no private setter.");
            setter.Invoke(null, new object[] { threadId });
        }
    }
}
