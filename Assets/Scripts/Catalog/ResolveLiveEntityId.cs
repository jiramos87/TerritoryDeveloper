using System.Collections.Generic;
using UnityEngine;

namespace Territory.Catalog
{
    /// <summary>
    /// TECH-1586 — single-hop "is this entity_id live?" predicate consumed by
    /// the load-time remap (TECH-1585) and the missing-asset placeholder
    /// fallback (TECH-1587). Returns the input id unchanged when present in
    /// the live catalog snapshot, returns <c>null</c> on miss / null / empty.
    ///
    /// <para>
    /// IMPORTANT — multi-hop <c>replaced_by_entity_id</c> chain walking is
    /// NOT performed here. The catalog snapshot exporter
    /// (<c>web/lib/snapshot/export.ts</c>) filters every per-kind row on
    /// <c>e.retired_at is null</c>, so retired rows + the
    /// <c>replaced_by_entity_id</c> pointer never reach Unity. Real chain
    /// walking requires extending the exporter to surface retired rows +
    /// pointer columns; deferred to a follow-up TECH issue under Stage 19.3
    /// / 20.x. For Stage 19.2 the predicate is single-hop only — callers
    /// route the <c>null</c> result to placeholder fallback.
    /// </para>
    ///
    /// <para>
    /// Not a singleton MonoBehaviour (Unity invariant 4). Not added to
    /// <c>GridManager</c> (invariant 6). Construction takes a
    /// <see cref="CatalogLoader"/> reference once; <see cref="Resolve"/>
    /// performs O(1) <c>ContainsKey</c> on the loader's immutable snapshot
    /// per call (no per-frame <c>FindObjectOfType</c> — invariant 3).
    /// </para>
    /// </summary>
    public class ResolveLiveEntityId
    {
        private readonly CatalogLoader _loader;

        /// <summary>
        /// Constructor — capture the <see cref="CatalogLoader"/> reference.
        /// Caller is responsible for resolving the loader (e.g. one-shot
        /// <c>FindObjectOfType&lt;CatalogLoader&gt;()</c> at boot).
        /// </summary>
        public ResolveLiveEntityId(CatalogLoader loader)
        {
            _loader = loader;
        }

        /// <summary>
        /// Static convenience overload — accepts an entity dictionary directly
        /// (test seam + caller-provided snapshot path). Returns <paramref name="entityId"/>
        /// when present, <c>null</c> when null / empty / absent.
        /// </summary>
        public static string Resolve(string entityId, IReadOnlyDictionary<string, CatalogEntity> entities)
        {
            if (string.IsNullOrEmpty(entityId)) return null;
            if (entities == null) return null;
            return entities.ContainsKey(entityId) ? entityId : null;
        }

        /// <summary>
        /// Single-hop predicate. Returns <paramref name="entityId"/> when the id
        /// is present in the live catalog snapshot, <c>null</c> otherwise. Null /
        /// empty input → graceful no-op (returns <c>null</c>, never throws).
        /// Caller routes <c>null</c> to placeholder fallback (TECH-1587).
        /// </summary>
        public string Resolve(string entityId)
        {
            if (string.IsNullOrEmpty(entityId)) return null;
            if (_loader == null) return null;
            IReadOnlyDictionary<string, CatalogEntity> entities = _loader.Entities;
            return Resolve(entityId, entities);
        }
    }
}
