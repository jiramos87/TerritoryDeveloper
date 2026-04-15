using System;
using System.Collections.Generic;
using UnityEngine.Audio;

namespace Territory.Audio
{
    /// <summary>
    /// Maps <see cref="BlipId"/> values to <see cref="AudioMixerGroup"/> routing targets
    /// at catalog bootstrap.
    /// <para>
    /// Reads authoring-only <c>BlipPatch.mixerGroup</c> refs during construction.
    /// The field is intentionally absent from <see cref="BlipPatchFlat"/> to preserve
    /// blittable layout — see Stage 1.2 T1.2.4 Decision Log.
    /// </para>
    /// <para>
    /// Lifetime: owned by <see cref="BlipCatalog"/> (invariant #4 — no singleton).
    /// </para>
    /// </summary>
    public sealed class BlipMixerRouter
    {
        private readonly Dictionary<BlipId, AudioMixerGroup> _map;

        /// <summary>
        /// Build the <see cref="BlipId"/> → <see cref="AudioMixerGroup"/> map from the
        /// catalog's authoring entries.
        /// <para>
        /// Null <c>entries[i].patch</c> stores <c>null</c> for that id — caller fallback
        /// (mixer master) handled by Stage 2.3 T2.3.3 consumer.
        /// </para>
        /// </summary>
        /// <param name="entries">Catalog entries array; must not be null.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown on duplicate <see cref="BlipId"/> — mirrors <see cref="BlipCatalog"/>
        /// duplicate-id contract (defense-in-depth; Stage 1.2 T1.2.4 Decision Log).
        /// </exception>
        public BlipMixerRouter(BlipPatchEntry[] entries)
        {
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));

            _map = new Dictionary<BlipId, AudioMixerGroup>(entries.Length);
            for (int i = 0; i < entries.Length; i++)
            {
                BlipId id = entries[i].id;
                if (_map.ContainsKey(id))
                    throw new InvalidOperationException(
                        $"BlipMixerRouter entries[{i}] duplicate id '{id}'.");
                _map[id] = entries[i].patch != null ? entries[i].patch.MixerGroup : null;
            }
        }

        /// <summary>
        /// Returns the <see cref="AudioMixerGroup"/> for <paramref name="id"/>.
        /// May return <c>null</c> when the authored patch left <c>mixerGroup</c> unset —
        /// Stage 2.3 T2.3.3 falls back to mixer master in that case.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="id"/> is not registered in this router.
        /// </exception>
        public AudioMixerGroup Get(BlipId id)
        {
            if (!_map.TryGetValue(id, out AudioMixerGroup group))
                throw new ArgumentOutOfRangeException(nameof(id),
                    $"BlipMixerRouter: unknown id '{id}'.");
            return group; // may be null — caller fallback in Stage 2.3
        }
    }
}
