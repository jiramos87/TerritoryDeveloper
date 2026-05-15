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

        public const int CurrentSchemaVersion = 1;

        /// <summary>Migrate legacy data: default region_unlocked=false for older schemas.</summary>
        public static CityData MigrateLoaded(CityData data)
        {
            if (data == null) return null;
            // Version 1 = current; no further migrations needed.
            data.schemaVersion = CurrentSchemaVersion;
            return data;
        }
    }
}
