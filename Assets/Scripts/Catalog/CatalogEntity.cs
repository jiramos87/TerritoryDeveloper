using System;

namespace Territory.Catalog
{
    /// <summary>
    /// Per-row POCO for the per-kind catalog JSON files written by
    /// <c>web/lib/snapshot/export.ts</c> (TECH-2673). Field names match the
    /// snake_case keys produced by <c>canonicalStringify</c> + per-kind
    /// fetcher SQL aliases — see <c>ExportedEntityRow</c>.
    ///
    /// `paramsJson` + `detail` are intentionally omitted from the POCO surface
    /// because Unity's <see cref="UnityEngine.JsonUtility"/> cannot round-trip
    /// arbitrary nested objects. Consumers that need either field can re-parse
    /// the original bytes via a domain-specific DTO. The hot-reload contract
    /// for TECH-2675 only requires the indexed lookup keys + slug + kind.
    /// </summary>
    [Serializable]
    public class CatalogEntity
    {
        public string entity_id;
        public string slug;
        public string display_name;
        public string[] tags;
        public string version_id;
        public int version_number;
        public string status;
        // TECH-1585 — legacy `subTypeId` (0..6 from `ZoneSubTypeRegistry`) carrier on
        // `asset` kind rows. Snapshot exporter (`web/lib/snapshot/export.ts:287`) emits
        // this column as `bigint::text`, so the field stays `string` to mirror the JSON
        // shape; CatalogLoader builds a side `legacy_asset_id (int) → entity_id`
        // dictionary at hot-reload time. Null / empty on non-`asset` rows.
        public string legacy_asset_id;
        // Per-kind tag set after dispatch — `kind` is not on the per-row JSON,
        // but the CatalogLoader stamps it post-parse from the file envelope.
        [NonSerialized] public string Kind;
    }

    /// <summary>Top-level shape of each per-kind file: <c>{kind, generatedAt, rows}</c>.</summary>
    [Serializable]
    public class CatalogPerKindFile
    {
        public string kind;
        public string generatedAt;
        public CatalogEntity[] rows;
    }
}
