using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using NUnit.Framework;
using Territory.Audio;
using UnityEngine;
using UnityEngine.TestTools;

namespace Territory.Tests.EditMode.Audio
{
    /// <summary>
    /// EditMode tests for <see cref="BlipBaker"/> memory budget + eviction (Stage 2.1 Phase 2).
    ///
    /// Covers:
    ///   (a) Seed past budget ceiling → <c>DebugTotalBytes ≤ budgetBytes</c> + <c>DebugCount</c> reduced.
    ///   (b) Normal insert within budget keeps <c>DebugTotalBytes ≤ budgetBytes</c>.
    ///   (c) Oversize single entry → <c>LogAssert.Expect</c> warning + returned <see cref="AudioClip"/> non-null.
    ///   (d) Evicted clips destroyed — Unity null-check via <c>UnityEngine.Object == null</c>.
    ///   (e) Invalid ctor — <c>Assert.Throws&lt;ArgumentOutOfRangeException&gt;</c> on <c>budgetBytes &lt;= 0</c>.
    /// </summary>
    public class BlipBakerBudgetTests
    {
        // ---------------------------------------------------------------------------
        // SetUp / TearDown
        // ---------------------------------------------------------------------------

        [SetUp]
        public void SetUp()
        {
            SetMainThreadId(Thread.CurrentThread.ManagedThreadId);
        }

        [TearDown]
        public void TearDown() { }

        // ---------------------------------------------------------------------------
        // (a) Insert past budget → DebugTotalBytes ≤ budgetBytes + count drops
        // ---------------------------------------------------------------------------

        /// <summary>
        /// Seeds a baker with a tiny budget (4096 bytes = 1024 mono float32 samples).
        /// Each patch is 0.1 s at 44100 Hz = 4410 samples = 17 640 bytes → well above budget.
        /// After inserting 3 entries, total bytes must stay ≤ budget and entry count must be 1.
        /// </summary>
        [Test]
        public void Insert_PastBudget_TotalBytesWithinCeiling()
        {
            const int SampleRate  = 44100;
            const long Budget     = 4096L;   // 1024 float32 samples
            var baker = new BlipBaker(SampleRate, Budget);

            BlipPatchFlat patch = BuildPatch(durationSeconds: 0.1f, sampleRate: SampleRate);
            // Each entry: 44100 * 0.1 * 4 = 17 640 bytes (exceeds 4096 individually
            // but the oversize-warning path inserts regardless — we test the budget
            // floor via a smaller duration below).

            // Use very short patch so an entry fits; budget = 4096 ≥ 882*4=3528 bytes.
            BlipPatchFlat smallPatch = BuildPatch(durationSeconds: 0.02f, sampleRate: SampleRate);
            // 44100 * 0.02 = 882 samples = 3528 bytes — fits within 4096.

            // Insert 3 entries; the 2nd will evict the 1st to make room, etc.
            baker.BakeOrGet(in smallPatch, patchHash: 1, variantIndex: 0);
            baker.BakeOrGet(in smallPatch, patchHash: 2, variantIndex: 0);
            baker.BakeOrGet(in smallPatch, patchHash: 3, variantIndex: 0);

            Assert.That(baker.DebugTotalBytes, Is.LessThanOrEqualTo(Budget),
                "DebugTotalBytes must not exceed budget after overflow inserts.");
            Assert.That(baker.DebugCount, Is.LessThan(3),
                "Cache entry count must have been reduced by eviction.");
        }

        // ---------------------------------------------------------------------------
        // (b) Normal insert stays within budget
        // ---------------------------------------------------------------------------

        /// <summary>
        /// Two entries that each fit comfortably within the budget.
        /// <c>DebugTotalBytes</c> must equal their combined byte count and be ≤ budget.
        /// </summary>
        [Test]
        public void Insert_Normal_TotalBytesDoesNotExceedBudget()
        {
            const int SampleRate  = 44100;
            // Budget large enough for 2 × 0.02 s patches (2 × 3528 = 7056 bytes).
            const long Budget     = 16384L;
            var baker = new BlipBaker(SampleRate, Budget);

            BlipPatchFlat patch = BuildPatch(durationSeconds: 0.02f, sampleRate: SampleRate);

            baker.BakeOrGet(in patch, patchHash: 1, variantIndex: 0);
            baker.BakeOrGet(in patch, patchHash: 2, variantIndex: 0);

            Assert.That(baker.DebugTotalBytes, Is.LessThanOrEqualTo(Budget),
                "DebugTotalBytes must not exceed budget for normal inserts.");
            Assert.That(baker.DebugCount, Is.EqualTo(2),
                "Both entries must be present when they fit within the budget.");
        }

        // ---------------------------------------------------------------------------
        // (c) Oversize entry → warning + non-null clip
        // ---------------------------------------------------------------------------

        /// <summary>
        /// Budget = 128 bytes (too small for any rendered clip).
        /// <see cref="BlipBaker.BakeOrGet"/> must emit exactly one <c>LogWarning</c>
        /// matching "exceeds budget" and return a non-null <see cref="AudioClip"/>.
        /// </summary>
        [Test]
        public void Insert_OversizeEntry_WarnsOnceAndReturnsClip()
        {
            const int SampleRate = 44100;
            const long Budget    = 128L;   // smaller than any rendered patch
            var baker = new BlipBaker(SampleRate, Budget);

            BlipPatchFlat patch = BuildPatch(durationSeconds: 0.05f, sampleRate: SampleRate);

            LogAssert.Expect(LogType.Warning, new Regex("exceeds budget"));

            var clip = baker.BakeOrGet(in patch, patchHash: 42, variantIndex: 0);

            Assert.That(clip, Is.Not.Null,
                "BakeOrGet must return a non-null AudioClip even when entry exceeds budget.");
            // Cleanup — clip kept by baker, so use DestroyImmediate.
            if (clip != null) Object.DestroyImmediate(clip);
        }

        // ---------------------------------------------------------------------------
        // (d) Evicted clips destroyed
        // ---------------------------------------------------------------------------

        /// <summary>
        /// Inserts two entries into a budget that fits only one.
        /// Captures the first clip ref, inserts a second (which evicts the first),
        /// then asserts the evicted clip compares equal to null via Unity's
        /// <c>UnityEngine.Object == null</c> operator (destroyed object).
        /// </summary>
        [Test]
        public void Eviction_DestroysEvictedClip()
        {
            const int SampleRate = 44100;
            // Budget = 4096 bytes; 0.02 s patch = 882 samples × 4 = 3528 bytes (fits one, not two).
            const long Budget    = 4096L;
            var baker = new BlipBaker(SampleRate, Budget);

            BlipPatchFlat patch = BuildPatch(durationSeconds: 0.02f, sampleRate: SampleRate);

            // Insert first entry and hold a reference to its clip.
            AudioClip firstClip = baker.BakeOrGet(in patch, patchHash: 1, variantIndex: 0);
            Assert.That(firstClip, Is.Not.Null, "First insert must return a non-null clip.");

            // Insert second entry — must evict the first to fit.
            baker.BakeOrGet(in patch, patchHash: 2, variantIndex: 0);

            // Unity's Object == null returns true for destroyed objects.
            Assert.That(firstClip == null, Is.True,
                "Evicted AudioClip must have been destroyed (Unity null-check returns true).");
        }

        // ---------------------------------------------------------------------------
        // (e) Invalid ctor
        // ---------------------------------------------------------------------------

        /// <summary>
        /// <c>budgetBytes = 0</c> must throw <see cref="System.ArgumentOutOfRangeException"/>.
        /// </summary>
        [Test]
        public void Ctor_ZeroBudget_ThrowsArgumentOutOfRange()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => _ = new BlipBaker(0, 0),
                "budgetBytes <= 0 must throw ArgumentOutOfRangeException.");
        }

        // ---------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------

        private static BlipPatchFlat BuildPatch(float durationSeconds, int sampleRate)
        {
            var so = ScriptableObject.CreateInstance<BlipPatch>();
            const BindingFlags privInst = BindingFlags.NonPublic | BindingFlags.Instance;

            var osc = new BlipOscillator
            {
                waveform    = BlipWaveform.Sine,
                frequencyHz = 440f,
                detuneCents = 0f,
                pulseDuty   = 0.5f,
                gain        = 1f,
            };
            SetField(so, "oscillators", new BlipOscillator[] { osc }, privInst);

            var env = new BlipEnvelope
            {
                attackMs     = 1f,
                attackShape  = BlipEnvShape.Linear,
                holdMs       = 0f,
                decayMs      = 5f,
                decayShape   = BlipEnvShape.Linear,
                sustainLevel = 0.5f,
                releaseMs    = 1f,
                releaseShape = BlipEnvShape.Linear,
            };
            SetField(so, "envelope", env, privInst);

            var flt = new BlipFilter { kind = BlipFilterKind.None, cutoffHz = 0f };
            SetField(so, "filter", flt, privInst);

            SetField(so, "deterministic",   true,            privInst);
            SetField(so, "variantCount",    2,               privInst);
            SetField(so, "voiceLimit",      1,               privInst);
            SetField(so, "durationSeconds", durationSeconds, privInst);

            InvokeOnValidate(so, privInst);

            var flat = BlipPatchFlat.FromSO(so);
            Object.DestroyImmediate(so);
            return flat;
        }

        private static void SetField(object target, string name, object value, BindingFlags flags)
        {
            var fi = target.GetType().GetField(name, flags);
            if (fi == null)
                throw new System.InvalidOperationException(
                    $"[BlipBakerBudgetTests] Field '{name}' not found on {target.GetType().FullName}.");
            fi.SetValue(target, value);
        }

        private static void InvokeOnValidate(object target, BindingFlags flags)
        {
            var mi = target.GetType().GetMethod("OnValidate", flags);
            if (mi == null)
                throw new System.InvalidOperationException(
                    $"[BlipBakerBudgetTests] OnValidate not found on {target.GetType().FullName}.");
            mi.Invoke(target, null);
        }

        private static void SetMainThreadId(int threadId)
        {
            var prop = typeof(BlipBootstrap).GetProperty(
                nameof(BlipBootstrap.MainThreadId),
                BindingFlags.Public | BindingFlags.Static);
            if (prop == null)
                throw new System.InvalidOperationException(
                    "[BlipBakerBudgetTests] BlipBootstrap.MainThreadId property not found.");
            var setter = prop.GetSetMethod(nonPublic: true);
            if (setter == null)
                throw new System.InvalidOperationException(
                    "[BlipBakerBudgetTests] BlipBootstrap.MainThreadId has no private setter.");
            setter.Invoke(null, new object[] { threadId });
        }
    }
}
