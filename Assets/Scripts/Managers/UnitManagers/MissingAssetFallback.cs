using UnityEngine;

namespace Territory.Core
{
    /// <summary>
    /// TECH-1587 — missing-asset fallback log emitter for the cell render path.
    /// Consumed by callers that received <c>null</c> from <see cref="Territory.Catalog.ResolveLiveEntityId"/>
    /// (TECH-1586) when looking up an <c>entity_id</c>.
    ///
    /// <para>
    /// Symbol-split behavior:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// <c>UNITY_EDITOR || DEVELOPMENT_BUILD</c> — emits <see cref="Debug.LogWarning"/>
    /// with the structured payload + caller renders the magenta placeholder sprite
    /// (<c>Assets/Sprites/Placeholders/missing_asset.png</c>).
    /// </description></item>
    /// <item><description>
    /// Ship build — emits <see cref="Debug.Log"/> at info level (no warning, but the
    /// payload still lands in the player log so post-mortem analysis can lift it
    /// verbatim) + caller hides the cell GameObject.
    /// </description></item>
    /// </list>
    ///
    /// <para>
    /// Log format: <c>[asset-miss] entity_id={ID} cell=({x},{y}) fallback=placeholder chain_followed=false</c>.
    /// Single-line, key=value pairs so <c>grep [asset-miss]</c> works in editor logs.
    /// <c>chain_followed</c> is hard-coded <c>false</c> because TECH-1586 ships a
    /// single-hop predicate, not a multi-hop chain walker (chain walk requires
    /// snapshot exporter changes; deferred to Stage 19.3 / 20.x).
    /// </para>
    ///
    /// <para>
    /// Audit_log row emit is RECLASSIFIED as <c>Debug.Log{Warning}</c> here for
    /// Stage 19.2: <c>web/lib/audit/emitter.ts</c> is server-side TypeScript invoked
    /// through Next.js request middleware; Unity has no DB connection + no current
    /// audit-bridge surface. The structured payload shape matches the future
    /// <c>audit_log</c> row body (action='asset_missing', target_kind='catalog_entity',
    /// payload jsonb={entity_id, cell_x, cell_y, fallback, chain_followed}) so a
    /// future Unity → web audit bridge can lift it verbatim.
    /// </para>
    ///
    /// <para>
    /// Not a singleton MonoBehaviour (Unity invariant 4). Static-only API. Not
    /// added to <c>GridManager</c> (invariant 6). No per-frame allocations beyond
    /// the formatted log string (invariant 3 — caller decides emission cadence).
    /// </para>
    /// </summary>
    public static class MissingAssetFallback
    {
        /// <summary>Locked path to the magenta placeholder sprite (D6 stage decision).</summary>
        public const string PlaceholderSpritePath = "Assets/Sprites/Placeholders/missing_asset.png";

        /// <summary>
        /// Emit the structured asset-miss log. Caller is responsible for the
        /// visual fallback (render placeholder sprite in dev / hide cell in ship).
        /// </summary>
        /// <param name="entityId">Catalog <c>entity_id</c> that resolved to <c>null</c>; may be empty / null on legacy unmapped saves.</param>
        /// <param name="cellX">Grid X coordinate of the missing-asset cell.</param>
        /// <param name="cellY">Grid Y coordinate of the missing-asset cell.</param>
        public static void Emit(string entityId, int cellX, int cellY)
        {
            string payload = "[asset-miss] entity_id="
                + (string.IsNullOrEmpty(entityId) ? "(empty)" : entityId)
                + " cell=(" + cellX + "," + cellY + ")"
                + " fallback=placeholder"
                + " chain_followed=false";

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Dev: warning-level so the editor console flags the miss + the
            // magenta sprite is visually obvious in the scene.
            Debug.LogWarning(payload);
#else
            // Ship: info-level — the player log retains the payload for
            // post-mortem analysis but no warning fires (cell is hidden).
            Debug.Log(payload);
#endif
        }

        /// <summary>
        /// True when the running build is dev (editor or <c>DEVELOPMENT_BUILD</c>).
        /// Caller branches on this to choose between rendering the magenta sprite
        /// (dev) and hiding the cell GameObject (ship).
        /// </summary>
        public static bool IsDevBuild
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return true;
#else
                return false;
#endif
            }
        }
    }
}
