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
        private Dictionary<BlipId, int> _indexById;
        private BlipMixerRouter _mixerRouter;
        private BlipCooldownRegistry _cooldownRegistry;
        private bool _isReady;

        /// <summary>
        /// Returns <c>true</c> after <c>Awake</c> completes flatten + bind.
        /// Callers must check this before invoking <see cref="Resolve"/>
        /// (scene-load suppression contract — Stage 1.1 T1.1.4).
        /// </summary>
        public bool IsReady => _isReady;

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
            _flat      = new BlipPatchFlat[entries.Length];
            _indexById = new Dictionary<BlipId, int>(entries.Length);

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

                _flat[i]        = BlipPatchFlat.FromSO(patch);
                _indexById[id]  = i;
            }

            _mixerRouter       = new BlipMixerRouter(entries);
            _cooldownRegistry  = new BlipCooldownRegistry();

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
