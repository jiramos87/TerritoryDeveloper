using System.Reflection;
using System.Threading;
using NUnit.Framework;
using Territory.Audio;
using UnityEngine;

namespace Territory.Tests.EditMode.Audio
{
    /// <summary>
    /// EditMode tests for <see cref="BlipBaker"/> LRU cache behaviour (Stage 2.1 Phase 1 + Phase 2).
    ///
    /// Covers:
    ///   1. <see cref="BlipBakeKey"/> equality + hash stability — Dictionary round-trip.
    ///   2. Repeated <c>BakeOrGet</c> with the same key returns ref-equal <see cref="AudioClip"/>.
    ///   3. Different variant → distinct clip (guards hash-collision regressions).
    ///   4. Cache hit promotes node to LRU tail — verified via <c>BlipBaker.DebugTailKey</c>.
    ///   5. <c>TryEvictHead</c> drains head-first (3 seeds → true,true,true,false).
    ///   6. <c>_index</c> + <c>_lru</c> coherence post-drain (both 0, <c>DebugTailKey</c> null).
    ///
    /// Note: <c>AddAtTail</c> tail=newest ordering is covered transitively by test 4 above
    /// (<c>BakeOrGet_Hit_PromotesToLruTail</c>) — no separate test needed.
    /// </summary>
    public class BlipBakerCacheTests
    {
        // Tracks ScriptableObjects and AudioClips created during a test for cleanup.
        private BlipPatch  _createdSo;
        private AudioClip  _clip0;
        private AudioClip  _clip1;

        // -----------------------------------------------------------------------
        // SetUp / TearDown
        // -----------------------------------------------------------------------

        /// <summary>
        /// Injects the main-thread id before each test so <c>AssertMainThread()</c> passes.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            SetMainThreadId(Thread.CurrentThread.ManagedThreadId);
        }

        [TearDown]
        public void TearDown()
        {
            if (_createdSo != null) { Object.DestroyImmediate(_createdSo); _createdSo = null; }
            if (_clip0     != null) { Object.DestroyImmediate(_clip0);     _clip0     = null; }
            if (_clip1     != null) { Object.DestroyImmediate(_clip1);     _clip1     = null; }
        }

        // -----------------------------------------------------------------------
        // 1. BlipBakeKey equality + hash
        // -----------------------------------------------------------------------

        /// <summary>
        /// Two <see cref="BlipBakeKey"/> instances with the same fields must be equal,
        /// produce equal hash codes, and survive a <see cref="System.Collections.Generic.Dictionary{TKey,TValue}"/> round-trip.
        /// </summary>
        [Test]
        public void BlipBakeKey_Equality_AndHash()
        {
            var a = new BlipBakeKey(patchHash: unchecked((int)0xDEADBEEF), variantIndex: 0);
            var b = new BlipBakeKey(patchHash: unchecked((int)0xDEADBEEF), variantIndex: 0);
            var c = new BlipBakeKey(patchHash: unchecked((int)0xDEADBEEF), variantIndex: 1);

            // Structural equality
            Assert.That(a.Equals(b),   Is.True,  "Same-field keys must be equal.");
            Assert.That(a.Equals(c),   Is.False, "Different variantIndex must not be equal.");
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()), "Equal keys must have equal hash codes.");

            // Dictionary round-trip
            var dict = new System.Collections.Generic.Dictionary<BlipBakeKey, string>
            {
                [a] = "value"
            };
            Assert.That(dict.ContainsKey(b), Is.True,  "Dictionary must find 'b' when 'a' was inserted (same key).");
            Assert.That(dict.ContainsKey(c), Is.False, "Dictionary must NOT find 'c' (different variantIndex).");
        }

        // -----------------------------------------------------------------------
        // 2. Repeat BakeOrGet → ref-equal clip
        // -----------------------------------------------------------------------

        /// <summary>
        /// Calling <see cref="BlipBaker.BakeOrGet"/> twice with identical arguments
        /// must return the same <see cref="AudioClip"/> reference (cache hit path).
        /// </summary>
        [Test]
        public void BakeOrGet_SameKey_ReturnsRefEqualClip()
        {
            const int SampleRate = 44100;
            BlipPatchFlat patch = BuildPatch(durationSeconds: 0.05f);
            int hash            = GetPatchHash();
            var baker           = new BlipBaker(SampleRate);

            _clip0 = baker.BakeOrGet(in patch, hash, variantIndex: 0);
            var second = baker.BakeOrGet(in patch, hash, variantIndex: 0);

            Assert.AreSame(_clip0, second,
                "Second BakeOrGet with the same key must return the cached AudioClip (ref-equal).");
        }

        // -----------------------------------------------------------------------
        // 3. Different variant → distinct clip
        // -----------------------------------------------------------------------

        /// <summary>
        /// A different <c>variantIndex</c> must produce a different <see cref="AudioClip"/> ref,
        /// guarding against <see cref="BlipBakeKey.GetHashCode"/> collisions.
        /// </summary>
        [Test]
        public void BakeOrGet_DifferentVariant_ReturnsDistinctClip()
        {
            const int SampleRate = 44100;
            BlipPatchFlat patch = BuildPatch(durationSeconds: 0.05f);
            int hash            = GetPatchHash();
            var baker           = new BlipBaker(SampleRate);

            _clip0 = baker.BakeOrGet(in patch, hash, variantIndex: 0);
            _clip1 = baker.BakeOrGet(in patch, hash, variantIndex: 1);

            Assert.AreNotSame(_clip0, _clip1,
                "Different variantIndex must produce distinct AudioClip refs.");
        }

        // -----------------------------------------------------------------------
        // 4. Hit promotes node to LRU tail
        // -----------------------------------------------------------------------

        /// <summary>
        /// Pre-populate keys A (hash 1, variant 0) and B (hash 2, variant 0).
        /// After B is inserted, tail is B. Then hit A — tail must become A.
        /// Verified via <c>BlipBaker.DebugTailKey</c> (internal test hook).
        /// </summary>
        [Test]
        public void BakeOrGet_Hit_PromotesToLruTail()
        {
            const int SampleRate = 44100;
            BlipPatchFlat patch = BuildPatch(durationSeconds: 0.05f);
            var baker = new BlipBaker(SampleRate);

            // Insert A (hash=1, variant=0) then B (hash=2, variant=0).
            var clipA = baker.BakeOrGet(in patch, patchHash: 1, variantIndex: 0);
            var clipB = baker.BakeOrGet(in patch, patchHash: 2, variantIndex: 0);

            // Tail is B before the hit.
            BlipBakeKey? tailBeforeHit = baker.DebugTailKey;
            Assert.That(tailBeforeHit.HasValue, Is.True, "Cache must have a tail after two inserts.");
            Assert.That(tailBeforeHit!.Value.patchHash, Is.EqualTo(2),
                "After inserting A then B, tail must be B (patchHash=2).");

            // Hit A — tail must become A.
            _ = baker.BakeOrGet(in patch, patchHash: 1, variantIndex: 0);

            BlipBakeKey? tailAfterHit = baker.DebugTailKey;
            Assert.That(tailAfterHit.HasValue, Is.True, "Cache must still have a tail after hit.");
            Assert.That(tailAfterHit!.Value.patchHash, Is.EqualTo(1),
                "After hitting A, tail must be A (patchHash=1).");

            // Cleanup
            Object.DestroyImmediate(clipA);
            Object.DestroyImmediate(clipB);
        }

        // -----------------------------------------------------------------------
        // 5. TryEvictHead — head-first drain
        // -----------------------------------------------------------------------

        /// <summary>
        /// Seed 3 distinct entries, then drain via <c>TryEvictHead</c>.
        /// Calls 1–3 must return <c>true</c>; call 4 (empty) must return <c>false</c>.
        /// Post-drain: <c>DebugCount == 0</c> and <c>DebugTailKey == null</c>.
        /// </summary>
        [Test]
        public void TryEvictHead_DrainThreeEntries_ReturnsTrueTrueTrueFalse()
        {
            const int SampleRate = 44100;
            BlipPatchFlat patch = BuildPatch(durationSeconds: 0.02f);
            var baker = new BlipBaker(SampleRate);

            // Seed 3 distinct entries (different patchHash values).
            baker.BakeOrGet(in patch, patchHash: 10, variantIndex: 0);
            baker.BakeOrGet(in patch, patchHash: 20, variantIndex: 0);
            baker.BakeOrGet(in patch, patchHash: 30, variantIndex: 0);

            Assert.That(baker.DebugCount, Is.EqualTo(3), "Cache must hold 3 entries before drain.");

            // Drain.
            Assert.That(baker.TryEvictHead(), Is.True,  "1st evict must return true.");
            Assert.That(baker.TryEvictHead(), Is.True,  "2nd evict must return true.");
            Assert.That(baker.TryEvictHead(), Is.True,  "3rd evict must return true.");
            Assert.That(baker.TryEvictHead(), Is.False, "4th evict on empty cache must return false.");

            // Post-drain coherence.
            Assert.That(baker.DebugCount,    Is.EqualTo(0), "_lru must be empty after full drain.");
            Assert.That(baker.DebugTailKey,  Is.Null,       "DebugTailKey must be null after full drain.");
        }

        /// <summary>
        /// Verifies head-first (oldest-first) eviction order.
        /// Insert A (hash 1) then B (hash 2); first eviction must remove A (head/oldest).
        /// </summary>
        [Test]
        public void TryEvictHead_RemovesOldestFirst()
        {
            const int SampleRate = 44100;
            BlipPatchFlat patch = BuildPatch(durationSeconds: 0.02f);
            var baker = new BlipBaker(SampleRate);

            baker.BakeOrGet(in patch, patchHash: 1, variantIndex: 0); // A — oldest
            baker.BakeOrGet(in patch, patchHash: 2, variantIndex: 0); // B — newest

            // Tail is B before eviction.
            Assert.That(baker.DebugTailKey!.Value.patchHash, Is.EqualTo(2),
                "Tail must be B (patchHash=2) before eviction.");

            baker.TryEvictHead(); // Removes A (head).

            // After removing A, only B remains; tail is still B.
            Assert.That(baker.DebugCount, Is.EqualTo(1), "One entry must remain after one eviction.");
            Assert.That(baker.DebugTailKey!.Value.patchHash, Is.EqualTo(2),
                "Tail must still be B (patchHash=2) after evicting A.");

            // B must still be accessible via BakeOrGet (cache hit).
            var clipB2 = baker.BakeOrGet(in patch, patchHash: 2, variantIndex: 0);
            Assert.That(clipB2, Is.Not.Null, "B must still be accessible after A was evicted.");

            Object.DestroyImmediate(clipB2);
        }

        // -----------------------------------------------------------------------
        // Helpers — patch construction (mirrors BlipBakerTests pattern)
        // -----------------------------------------------------------------------

        private BlipPatchFlat BuildPatch(float durationSeconds = 0.1f)
        {
            var so = ScriptableObject.CreateInstance<BlipPatch>();
            _createdSo = so;

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

            var flt = new BlipFilter { kind = BlipFilterKind.None, cutoffHz = 0f };
            SetField(so, "filter", flt, privInst);

            SetField(so, "deterministic",   true,            privInst);
            SetField(so, "variantCount",    2,               privInst);   // allow variant 0 + 1
            SetField(so, "voiceLimit",      1,               privInst);
            SetField(so, "durationSeconds", durationSeconds, privInst);

            InvokeOnValidate(so, privInst);

            return BlipPatchFlat.FromSO(so);
        }

        private int GetPatchHash()
        {
            if (_createdSo == null)
                throw new System.InvalidOperationException(
                    "[BlipBakerCacheTests] BuildPatch must be called before GetPatchHash.");
            const BindingFlags privInst = BindingFlags.NonPublic | BindingFlags.Instance;
            var fi = _createdSo.GetType().GetField("patchHash", privInst);
            if (fi == null)
                throw new System.InvalidOperationException(
                    "[BlipBakerCacheTests] 'patchHash' field not found on BlipPatch.");
            return (int)fi.GetValue(_createdSo);
        }

        private static void SetField(object target, string fieldName, object value, BindingFlags flags)
        {
            var fi = target.GetType().GetField(fieldName, flags);
            if (fi == null)
                throw new System.InvalidOperationException(
                    $"[BlipBakerCacheTests] Field '{fieldName}' not found on {target.GetType().FullName}.");
            fi.SetValue(target, value);
        }

        private static void InvokeOnValidate(object target, BindingFlags flags)
        {
            var mi = target.GetType().GetMethod("OnValidate", flags);
            if (mi == null)
                throw new System.InvalidOperationException(
                    $"[BlipBakerCacheTests] OnValidate not found on {target.GetType().FullName}.");
            mi.Invoke(target, null);
        }

        private static void SetMainThreadId(int threadId)
        {
            var prop = typeof(BlipBootstrap).GetProperty(
                nameof(BlipBootstrap.MainThreadId),
                BindingFlags.Public | BindingFlags.Static);
            if (prop == null)
                throw new System.InvalidOperationException(
                    "[BlipBakerCacheTests] BlipBootstrap.MainThreadId property not found.");
            var setter = prop.GetSetMethod(nonPublic: true);
            if (setter == null)
                throw new System.InvalidOperationException(
                    "[BlipBakerCacheTests] BlipBootstrap.MainThreadId has no private setter.");
            setter.Invoke(null, new object[] { threadId });
        }
    }
}
