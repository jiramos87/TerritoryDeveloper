namespace Territory.Roads
{
    /// <summary>
    /// Compile-time constants for road placement. Lives in Territory.Core leaf (zero Game dep).
    /// RoadManager.RoadCostPerTile mirrors this value — single source of truth.
    /// </summary>
    public static class RoadConstants
    {
        public const int RoadCostPerTile = 50;
    }
}
