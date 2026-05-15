using System.Collections.Generic;
using Territory.Persistence;
using Territory.RegionScene.Evolution;

namespace Territory.RegionScene.Persistence
{
    /// <summary>Serializable DTO written as &lt;saveName&gt;.region.json. JsonUtility requires public fields.</summary>
    [System.Serializable]
    public class RegionSaveFile
    {
        public int schemaVersion;
        public int gridSize;
        /// <summary>Flat row-major array: index = y * gridSize + x.</summary>
        public RegionCellData[] cells;
        /// <summary>City-ownership map: cityId → serializable list of owned cell indices.</summary>
        public List<CityOwnershipEntry> cityOwnership;
        /// <summary>Stage 5.0: lazy-created CityData records. Mirrors owningCityId links in cells.</summary>
        public List<CityData> lazyCities;

        public const int CurrentSchemaVersion = 2;
    }

    /// <summary>JsonUtility-compatible key-value pair for city ownership (Dictionary not serializable by JsonUtility).</summary>
    [System.Serializable]
    public class CityOwnershipEntry
    {
        public string cityId;
        public int cellIndex;  // y * gridSize + x
    }
}
