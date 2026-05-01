using System;

namespace Territory.Economy
{
    /// <summary>
    /// Persisted placement record for one state-service zone cell.
    /// Placement / restore lands in Step 2; field carried forward this task as empty list on fresh games.
    /// Added schema 4.
    /// </summary>
    [Serializable]
    public class StateServiceZoneData
    {
        /// <summary>Grid X coordinate of the zone cell.</summary>
        public int cellX;

        /// <summary>Grid Y coordinate of the zone cell.</summary>
        public int cellY;

        /// <summary>State-service sub-type id (value domain resolved in Step 2 placement task).</summary>
        public int subTypeId;

        /// <summary>Density tier (raw int; Light / Medium / Heavy resolved in Step 2).</summary>
        public int densityTier;

        /// <summary>
        /// TECH-1585 — catalog `entity_id` (string UUID). Empty `""` on legacy saves;
        /// resolved on load via <c>CatalogLoader.TryResolveByLegacyAssetId(subTypeId)</c>
        /// when empty AND <c>subTypeId &gt;= 0</c>. Re-saves persist the resolved id.
        /// Existing <c>subTypeId</c> stays as the legacy carrier (D5 — no field removal,
        /// no save-schema bump). Idempotent: second-pass load short-circuits the
        /// lookup branch when this field is non-empty.
        /// </summary>
        public string entityId = string.Empty;
    }
}
