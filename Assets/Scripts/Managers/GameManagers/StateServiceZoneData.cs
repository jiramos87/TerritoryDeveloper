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
    }
}
