namespace Territory.Core
{
    /// <summary>
    /// Enum representing the four cardinal sides of the city map border
    /// where an interstate exit can cross into a neighbor city.
    /// </summary>
    public enum BorderSide { North, South, East, West }

    /// <summary>
    /// Minimum conceptual representation of a neighbor city at an interstate
    /// map border. Schema-only value type; no behavior, no MonoBehaviour.
    /// Serializable so Unity can hold it in lists (GameSaveData, TECH-103).
    /// Fields follow the id-as-GUID-string convention from GameSaveData
    /// (regionId / countryId — TECH-87 / TECH-88).
    /// </summary>
    [System.Serializable]
    public struct NeighborCityStub
    {
        public string id;            // GUID string, matches GameSaveData.{region,country}Id convention
        public string displayName;
        public BorderSide borderSide;
    }

    /// <summary>
    /// Records a single interstate exit crossing at the map border, linking a
    /// <see cref="NeighborCityStub"/> to the grid cell where the crossing occurs.
    /// Schema-only value type; no behavior, no MonoBehaviour.
    /// Added in schema 3. Dedupe key: (stubId, exitCellX, exitCellY).
    /// </summary>
    [System.Serializable]
    public struct NeighborCityBinding
    {
        /// <summary>Id of the matched <see cref="NeighborCityStub"/> (GUID string).</summary>
        public string stubId;
        /// <summary>Grid X coordinate of the border exit cell.</summary>
        public int exitCellX;
        /// <summary>Grid Y coordinate of the border exit cell.</summary>
        public int exitCellY;
    }
}
