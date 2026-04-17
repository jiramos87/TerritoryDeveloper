using System;
using System.Collections.Generic;
using System.Threading;
using Territory.Audio;
using UnityEngine;

/// <summary>
/// Plain class (not MonoBehaviour) that renders a <see cref="BlipPatchFlat"/> through
/// <see cref="BlipVoice.Render"/> and wraps the result in a non-streaming <see cref="AudioClip"/>.
/// Owned as an instance field by <c>BlipCatalog</c> (Stage 2.2) — no singleton (invariant #4).
/// Main-thread only: <see cref="BakeOrGet"/> throws <see cref="InvalidOperationException"/> when
/// called from a background thread. No <c>FindObjectOfType</c> (invariant #3).
/// </summary>
public sealed class BlipBaker
{
    // -----------------------------------------------------------------------
    // Nested types
    // -----------------------------------------------------------------------

    /// <summary>
    /// LRU node payload — holds the cache key, the baked clip, and byte size.
    /// <c>byteCount</c> = <c>lengthSamples * sizeof(float)</c>, written on miss insert
    /// and subtracted from <c>_totalBytes</c> on eviction.
    /// </summary>
    private sealed class BlipBakeEntry
    {
        public BlipBakeKey key;
        public AudioClip   clip;
        public long        byteCount;
    }

    // -----------------------------------------------------------------------
    // Cache state
    // -----------------------------------------------------------------------

    /// <summary>Lookup index from key → LRU node for O(1) hit detection.</summary>
    private readonly Dictionary<BlipBakeKey, LinkedListNode<BlipBakeEntry>> _index = new();

    /// <summary>
    /// Access-order doubly-linked list — <b>head = oldest, tail = newest</b>.
    /// Only three verbs mutate this list:
    /// <list type="bullet">
    ///   <item><see cref="AddAtTail"/> — miss path; appends new entry at tail.</item>
    ///   <item>Hit promote (in <see cref="BakeOrGet"/>) — removes node then re-adds at tail.</item>
    ///   <item><see cref="TryEvictHead"/> — eviction path; pops head (oldest).</item>
    /// </list>
    /// </summary>
    private readonly LinkedList<BlipBakeEntry> _lru = new();

    // -----------------------------------------------------------------------
    // Internals (test hooks)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns the <see cref="BlipBakeKey"/> of the most-recently-used entry
    /// (LRU tail), or <c>null</c> when the cache is empty.
    /// Exposed for EditMode tests; gated by <c>InternalsVisibleTo("Blip.Tests.EditMode")</c>.
    /// </summary>
    internal BlipBakeKey? DebugTailKey
        => _lru.Last == null ? (BlipBakeKey?)null : _lru.Last.Value.key;

    /// <summary>
    /// Number of entries currently in the cache (LRU list length).
    /// <c>_index.Count</c> must equal this at all times.
    /// Exposed for EditMode post-drain coherence assertions.
    /// Gated by <c>InternalsVisibleTo("Blip.Tests.EditMode")</c>.
    /// </summary>
    internal int DebugCount => _lru.Count;

    /// <summary>
    /// Running byte total of all cached PCM data.  Must equal <c>sum(entry.byteCount)</c>
    /// at all times; exposed so budget-overflow tests can assert exact accounting.
    /// Gated by <c>InternalsVisibleTo("Blip.Tests.EditMode")</c>.
    /// </summary>
    internal long DebugTotalBytes => _totalBytes;

    // -----------------------------------------------------------------------
    // Fields
    // -----------------------------------------------------------------------

    /// <summary>
    /// Maximum delay-line ceiling in milliseconds used when pre-leasing pool buffers.
    /// 50 ms covers Comb (30 ms) + Chorus/Flanger LFO-sweep ranges.
    /// Revisit if per-slot tighter sizing is beneficial.
    /// </summary>
    private const float MaxDelayMs = 50f;

    private readonly int           _sampleRate;
    private readonly long          _budgetBytes;
    private          long          _totalBytes;
    private readonly BlipDelayPool _delayPool;

    /// <summary>
    /// Creates a baker bound to <paramref name="sampleRate"/> and a memory
    /// <paramref name="budgetBytes"/> ceiling for the LRU cache.
    /// When <paramref name="sampleRate"/> is zero or negative the baker resolves
    /// <see cref="AudioSettings.outputSampleRate"/> once at construction time so the
    /// cache key domain stays <c>(patchHash, variantIndex)</c> only — no rate axis.
    /// </summary>
    /// <param name="sampleRate">Sample rate in Hz; pass 0 to use <see cref="AudioSettings.outputSampleRate"/>.</param>
    /// <param name="budgetBytes">
    /// Maximum byte footprint of all cached <see cref="AudioClip"/> PCM data (mono float32).
    /// Defaults to 4 MiB. Must be greater than zero; throws <see cref="ArgumentOutOfRangeException"/> otherwise.
    /// </param>
    /// <remarks>
    /// Test back-compat ctor: creates a private <see cref="BlipDelayPool"/> internally.
    /// Callers in production should prefer <see cref="BlipBaker(BlipDelayPool,int,long)"/>
    /// so <see cref="BlipCatalog"/> owns the pool lifetime.
    /// </remarks>
    public BlipBaker(int sampleRate = 0, long budgetBytes = 4L * 1024 * 1024)
        : this(new BlipDelayPool(), sampleRate, budgetBytes) { }

    /// <summary>
    /// Creates a baker bound to <paramref name="sampleRate"/>, a memory
    /// <paramref name="budgetBytes"/> ceiling, and an externally-owned
    /// <paramref name="delayPool"/> for pre-leasing delay-line buffers.
    /// </summary>
    /// <param name="delayPool">
    /// Pool to lease delay-line buffers from. Must not be null; if null a private instance is created.
    /// Owned by the caller (e.g. <see cref="Territory.Audio.BlipCatalog"/>).
    /// </param>
    /// <param name="sampleRate">Sample rate in Hz; pass 0 to use <see cref="AudioSettings.outputSampleRate"/>.</param>
    /// <param name="budgetBytes">
    /// Maximum byte footprint of all cached <see cref="AudioClip"/> PCM data (mono float32).
    /// Defaults to 4 MiB. Must be greater than zero; throws <see cref="ArgumentOutOfRangeException"/> otherwise.
    /// </param>
    internal BlipBaker(BlipDelayPool delayPool, int sampleRate = 0, long budgetBytes = 4L * 1024 * 1024)
    {
        if (budgetBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(budgetBytes),
                "budgetBytes must be greater than zero.");
        _delayPool   = delayPool ?? new BlipDelayPool();
        _sampleRate  = sampleRate > 0 ? sampleRate : AudioSettings.outputSampleRate;
        _budgetBytes = budgetBytes;
    }

    /// <summary>
    /// Returns a non-streaming <see cref="AudioClip"/> for <paramref name="patch"/>,
    /// dispatching through the in-memory LRU cache.
    ///
    /// <para><b>Cache hit</b> — when <c>(_patchHash, variantIndex)</c> is already cached,
    /// the existing node is promoted to the LRU tail (most-recently-used) and its
    /// <see cref="AudioClip"/> is returned by reference — no re-render.</para>
    ///
    /// <para><b>Cache miss</b> — renders via <see cref="BlipVoice.Render"/>, wraps the result
    /// in a <see cref="BlipBakeEntry"/>, appends to LRU tail, and registers in the index.
    /// Eviction (budget-driven head pop) is added in a follow-up task.</para>
    /// </summary>
    /// <param name="patch">Immutable blittable patch parameters (Stage 1.2).</param>
    /// <param name="patchHash">
    /// Pre-computed <b>patch hash</b> from <c>BlipPatch.PatchHash</c> (Stage 1.2 source line 162
    /// explicitly defers the hash from <see cref="BlipPatchFlat"/>; caller — <c>BlipCatalog</c> —
    /// reads it from the SO and passes it here).
    /// </param>
    /// <param name="variantIndex">Round-robin variant selector (0-based); forwarded to <see cref="BlipVoice.Render"/> on miss.</param>
    /// <returns>Non-null mono <see cref="AudioClip"/> with <c>samples == lengthSamples</c>, <c>channels == 1</c>, <c>frequency == sampleRate</c>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when called from a non-main thread.</exception>
    public AudioClip BakeOrGet(in BlipPatchFlat patch, int patchHash, int variantIndex)
    {
        AssertMainThread();

        var key = new BlipBakeKey(patchHash, variantIndex);

        // Cache hit — promote to LRU tail and return cached clip (ref-equal on each call).
        if (_index.TryGetValue(key, out var existing))
        {
            _lru.Remove(existing);
            _lru.AddLast(existing);
            return existing.Value.clip;
        }

        // Cache miss — render, wrap, insert at LRU tail, register in index.
        int lengthSamples = (int)(patch.durationSeconds * _sampleRate);
        float[] buffer = new float[lengthSamples];
        BlipVoiceState state = default;

        // Pre-lease 4 delay-line buffers outside the Render call so no managed alloc
        // occurs inside Render (Stage 5.2 no-alloc contract).
        // All four slots get the same 50 ms ceiling; delay-line kernels live in BlipFxChain.
        float[]? d0 = null, d1 = null, d2 = null, d3 = null;
        int wp0 = 0, wp1 = 0, wp2 = 0, wp3 = 0;
        int len = (int)Math.Ceiling(MaxDelayMs / 1000f * _sampleRate) + 1;
        try
        {
            d0 = _delayPool.Lease(_sampleRate, MaxDelayMs);
            d1 = _delayPool.Lease(_sampleRate, MaxDelayMs);
            d2 = _delayPool.Lease(_sampleRate, MaxDelayMs);
            d3 = _delayPool.Lease(_sampleRate, MaxDelayMs);
            BlipVoice.Render(buffer, 0, lengthSamples, _sampleRate, in patch, variantIndex, ref state,
                d0, d1, d2, d3, len, len, len, len,
                ref wp0, ref wp1, ref wp2, ref wp3);
        }
        finally
        {
            // Return non-null leases unconditionally; BlipDelayPool.Return clears (clearArray: true).
            if (d0 != null) _delayPool.Return(d0);
            if (d1 != null) _delayPool.Return(d1);
            if (d2 != null) _delayPool.Return(d2);
            if (d3 != null) _delayPool.Return(d3);
        }

        string clipName = $"Blip_{patchHash:X8}_v{variantIndex}";
        var clip = AudioClip.Create(clipName, lengthSamples, channels: 1, _sampleRate, stream: false);
        clip.SetData(buffer, 0);

        long newByteCount = (long)lengthSamples * sizeof(float);

        // Drain head entries until the new entry fits; stops early when cache is empty.
        while (_totalBytes + newByteCount > _budgetBytes && TryEvictHead()) { }

        // Post-loop guard: single-entry-over-budget case (cache now empty, still too big).
        if (_totalBytes + newByteCount > _budgetBytes)
        {
            Debug.LogWarning(
                $"BlipBaker: entry {key} ({newByteCount} bytes) exceeds budget " +
                $"{_budgetBytes}; inserting anyway.");
        }

        var entry = new BlipBakeEntry { key = key, clip = clip, byteCount = newByteCount };
        AddAtTail(entry);
        _totalBytes += newByteCount;
        return clip;
    }

    // -----------------------------------------------------------------------
    // LRU helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Appends <paramref name="entry"/> to the LRU tail and registers it in <c>_index</c>.
    /// All miss-path inserts route through here (DRY wrapper).
    /// </summary>
    /// <returns>The new <see cref="LinkedListNode{T}"/> for the appended entry.</returns>
    private LinkedListNode<BlipBakeEntry> AddAtTail(BlipBakeEntry entry)
    {
        var node = _lru.AddLast(entry);
        _index[entry.key] = node;
        return node;
    }

    /// <summary>
    /// Enumerates all cached <see cref="AudioClip"/> instances whose cache key has a
    /// <c>patchHash</c> matching <paramref name="patchHash"/>.
    /// <para>
    /// Used by <see cref="Territory.Audio.BlipEngine.StopAll"/> (Stage 2.3 Phase 2) to
    /// collect the set of clips that must be stopped across the voice pool.
    /// </para>
    /// <para>
    /// <b>Non-mutating</b> — does not touch <c>_lru</c> order or <c>_totalBytes</c>.
    /// Gated by <c>InternalsVisibleTo</c>; <c>BlipEngine</c> is in the same
    /// <c>Territory.Audio</c> namespace so <c>internal</c> access is sufficient.
    /// </para>
    /// </summary>
    /// <param name="patchHash">The patch hash axis of the key to match.</param>
    /// <returns>
    /// Each <see cref="AudioClip"/> ref-equal to a cached entry whose
    /// <c>key.patchHash == patchHash</c>. Order is undefined.
    /// </returns>
    internal System.Collections.Generic.IEnumerable<AudioClip> EnumerateClipsForPatchHash(int patchHash)
    {
        foreach (var node in _index)
        {
            if (node.Key.patchHash == patchHash)
                yield return node.Value.Value.clip;
        }
    }

    /// <summary>
    /// Evicts the oldest entry (LRU head): subtracts its <c>byteCount</c> from
    /// <c>_totalBytes</c>, removes from <c>_lru</c> and <c>_index</c>, then calls
    /// <see cref="UnityEngine.Object.Destroy"/> on the baked clip.
    /// </summary>
    /// <returns>
    /// <c>true</c> if an entry was evicted; <c>false</c> when the cache is empty.
    /// </returns>
    internal bool TryEvictHead()
    {
        var head = _lru.First;
        if (head == null) return false;
        // Accounting folded here (single mutation site, _totalBytes invariant stays local).
        _totalBytes -= head.Value.byteCount;
        _lru.RemoveFirst();
        _index.Remove(head.Value.key);
#if UNITY_EDITOR
        if (UnityEngine.Application.isPlaying)
            UnityEngine.Object.Destroy(head.Value.clip);
        else
            UnityEngine.Object.DestroyImmediate(head.Value.clip);
#else
        UnityEngine.Object.Destroy(head.Value.clip);
#endif
        return true;
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Throws <see cref="InvalidOperationException"/> when the calling thread is not the Unity
    /// main thread. Compares against <see cref="BlipBootstrap.MainThreadId"/>, which is captured
    /// in <c>BlipBootstrap.Awake</c> (Phase 1 Step A).
    /// </summary>
    private static void AssertMainThread()
    {
        int current  = Thread.CurrentThread.ManagedThreadId;
        int expected = BlipBootstrap.MainThreadId;
        if (current != expected)
            throw new InvalidOperationException(
                $"BlipBaker.BakeOrGet must run on main thread " +
                $"(expected thread id {expected}, got {current}).");
    }
}
