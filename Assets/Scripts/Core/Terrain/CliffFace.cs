namespace Territory.Terrain
{
    /// <summary>
    /// Cardinal dir of vertical cliff face on <b>higher</b> cell toward lower neighbor (see isometric spec §1.2).
    /// Prefab selection uses this geometry, not variable names.
    /// </summary>
    public enum CliffCardinalFace
    {
        North,
        South,
        East,
        West
    }

    /// <summary>
    /// Bitmask of logical cliff faces on land cell (hydrology / flow blocking). Set for any cardinal risco even when
    /// north/west faces skip prefab instantiation (hidden from fixed camera).
    /// </summary>
    [System.Flags]
    public enum CliffFaceFlags
    {
        None = 0,
        North = 1,
        South = 2,
        East = 4,
        West = 8
    }
}
