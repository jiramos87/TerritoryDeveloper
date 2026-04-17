using System;
using System.Collections.Generic;
using UnityEngine;

namespace Territory.Audio
{
    /// <summary>
    /// Scene MonoBehaviour that maps <see cref="BlipId"/> values to
    /// <see cref="BlipPatchFlat"/> structs at scene-load time.
    /// <para>
    /// Inspector authors populate <c>entries</c> by binding one
    /// <see cref="BlipPatch"/> ScriptableObject per <see cref="BlipId"/>.
    /// <c>Awake</c> validates the array (null-patch + duplicate-id checks),
    /// then flattens each entry into a parallel <c>_flat</c> array and builds
    /// <c>_indexById</c> for O(1) id → slot lookup.
    /// </para>
    /// <para>
    /// No singleton / static state — invariant #4 compliant.
    /// </para>
    /// </summary>
    public sealed class BlipCatalog : MonoBehaviour
    {
        [SerializeField]
        private BlipPatchEntry[] entries = Array.Empty<BlipPatchEntry>();

        private BlipPatchFlat[] _flat;
        private int[] _patchHashes;
        private Dictionary<BlipId, int> _indexById;
        private Dictionary<BlipId, uint> _rngState;
        private BlipMixerRouter _mixerRouter;
        private BlipCooldownRegistry _cooldownRegistry;
        private BlipBaker _baker;
        private readonly BlipDelayPool _delayPool = new BlipDelayPool();
        private bool _isReady;

        /// <summary>
        /// Returns <c>true</c> after <c>Awake</c> completes flatten + bind.
        /// Callers must check this before invoking <see cref="Resolve"/>
        /// (scene-load suppression contract — Stage 1.1 T1.1.4).
        /// </summary>
        public bool IsReady => _isReady;

        /// <summary>LRU baker owned by this catalog (invariant #4 — no singleton).</summary>
        internal BlipBaker Baker => _baker;

        /// <summary>Mixer-group router for this catalog's id space.</summary>
        internal BlipMixerRouter MixerRouter => _mixerRouter;

        /// <summary>Cooldown registry for this catalog's id space.</summary>
        internal BlipCooldownRegistry CooldownRegistry => _cooldownRegistry;

        /// <summary>
        /// Returns the SO-side patch hash for <paramref name="id"/> from the parallel
        /// <c>_patchHashes</c> array populated in <c>Awake</c>.
        /// <para>
        /// <c>BlipPatchFlat</c> intentionally omits <c>patchHash</c> (blittable-layout contract).
        /// <see cref="BlipBaker.BakeOrGet"/> receives it as a separate arg (Stage 2.3 Decision Log).
        /// </para>
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="id"/> is not registered in this catalog.
        /// </exception>
        internal int PatchHash(BlipId id)
        {
            if (!_indexById.TryGetValue(id, out int idx))
                throw new ArgumentOutOfRangeException(nameof(id), $"BlipCatalog.PatchHash: unknown id '{id}'.");
            return _patchHashes[idx];
        }

        /// <summary>
        /// Returns the next variant index for <paramref name="id"/> using a per-id
        /// xorshift32 RNG, seeded lazily on first call.
        /// <para>
        /// Seed formula: <c>(uint)id * 2654435761u | 1u</c> (Knuth hash, forced odd so
        /// xorshift never collapses to 0).
        /// Step: <c>x ^= x &lt;&lt; 13; x ^= x &gt;&gt; 17; x ^= x &lt;&lt; 5;</c>
        /// Result: <c>(int)(x % (uint)variantCount)</c>.
        /// </para>
        /// </summary>
        /// <param name="id">Blip id whose per-id RNG state is advanced.</param>
        /// <param name="variantCount">Total variant slots; returns 0 when &lt;= 1.</param>
        internal int NextVariant(BlipId id, int variantCount)
        {
            if (variantCount <= 1) return 0;

            _rngState ??= new Dictionary<BlipId, uint>();
            if (!_rngState.TryGetValue(id, out uint x))
                x = (uint)id * 2654435761u | 1u;

            // xorshift32
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _rngState[id] = x;

            return (int)(x % (uint)variantCount);
        }

        /// <summary>
        /// Returns a <c>ref readonly</c> reference into the internal flat array
        /// for the given <paramref name="id"/>.
        /// <para>
        /// <b>Consume immediately.</b> The reference is valid only for the
        /// lifetime of the catalog (no catalog rebuild during scene lifetime),
        /// but callers must not cache it across frames.
        /// </para>
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="id"/> is not registered in this catalog.
        /// </exception>
        public ref readonly BlipPatchFlat Resolve(BlipId id)
        {
            if (!_indexById.TryGetValue(id, out int idx))
                throw new ArgumentOutOfRangeException(nameof(id), $"BlipCatalog: unknown id '{id}'.");
            return ref _flat[idx];
        }

        private void Awake()
        {
            _flat         = new BlipPatchFlat[entries.Length];
            _patchHashes  = new int[entries.Length];
            _indexById    = new Dictionary<BlipId, int>(entries.Length);

            for (int i = 0; i < entries.Length; i++)
            {
                BlipId    id    = entries[i].id;
                BlipPatch patch = entries[i].patch;

                if (patch == null)
                    throw new InvalidOperationException(
                        $"BlipCatalog entries[{i}] has null patch ref.");

                if (_indexById.TryGetValue(id, out int prev))
                    throw new InvalidOperationException(
                        $"BlipCatalog entries[{i}] duplicate id '{id}' (first seen at index {prev}).");

                _flat[i]         = BlipPatchFlat.FromSO(patch);
                _patchHashes[i]  = patch.PatchHash;
                _indexById[id]   = i;
            }

            _mixerRouter       = new BlipMixerRouter(entries);
            _cooldownRegistry  = new BlipCooldownRegistry();
            // Inject _delayPool so baker pre-leases buffers from catalog-owned pool.
            _baker             = new BlipBaker(_delayPool);

            // scene-load suppression boundary — Stage 1.1 T1.1.4
            BlipEngine.Bind(this);
            _isReady = true;
        }

        private void OnDestroy()
        {
            BlipEngine.Unbind(this);
        }
    }
}
