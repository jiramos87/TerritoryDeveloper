namespace Territory.Terrain
{
    /// <summary>
    /// Slope orientation for a terrain cell. Extracted to Territory.Core leaf (Stage 20).
    /// Matches original definition in TerrainManager.cs — values are order-sensitive for save-file compat.
    /// </summary>
    public enum TerrainSlopeType
    {
        Flat,
        North,
        South,
        East,
        West,
        NorthEast,
        NorthWest,
        SouthEast,
        SouthWest,
        NorthEastUp,
        NorthWestUp,
        SouthEastUp,
        SouthWestUp
    }
}
