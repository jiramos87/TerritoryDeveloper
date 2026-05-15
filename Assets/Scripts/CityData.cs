using UnityEngine;

namespace Territory.Persistence
{
    /// <summary>Lightweight city record carried by GameSaveData. Stores city-level flags including region unlock state.</summary>
    [System.Serializable]
    public class CityData
    {
        public int schemaVersion;
        public string cityId;
        public string cityName;
        public int pop;
        /// <summary>True once city pop crosses RegionUnlockGate.RegionUnlockPopThreshold OR cheat flag set.</summary>
        public bool regionUnlocked;

        // Stage 5.0: owning region cell coords (null for cities created pre-Stage-5.0).
        public bool hasOwningRegionCell;
        public int owningRegionCellX;
        public int owningRegionCellY;

        public const int CurrentSchemaVersion = 2;

        /// <summary>Migrate legacy data: default fields for older schemas.</summary>
        public static CityData MigrateLoaded(CityData data)
        {
            if (data == null) return null;
            // Version 1 → 2: owning_region_cell defaults to absent (hasOwningRegionCell=false).
            data.schemaVersion = CurrentSchemaVersion;
            return data;
        }
    }

    /// <summary>Factory for lazy-created CityData records (no full city scene needed at creation time).</summary>
    public static class CityDataFactory
    {
        /// <summary>
        /// Create a minimal CityData linked to an owning region cell. Uses runtime GUID as city id.
        /// Pop = 0, name = placeholder, region_unlocked = false.
        /// </summary>
        public static CityData CreateLazy(Vector2Int owningRegionCell)
        {
            return new CityData
            {
                schemaVersion       = CityData.CurrentSchemaVersion,
                cityId              = System.Guid.NewGuid().ToString("N"),
                cityName            = $"New City ({owningRegionCell.x},{owningRegionCell.y})",
                pop                 = 0,
                regionUnlocked      = false,
                hasOwningRegionCell = true,
                owningRegionCellX   = owningRegionCell.x,
                owningRegionCellY   = owningRegionCell.y,
            };
        }
    }
}
