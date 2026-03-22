namespace Territory.Terrain
{
    /// <summary>
    /// Classification of water mass per cell for serialization and future gameplay (rivers, sea, lakes).
    /// </summary>
    public enum WaterBodyType
    {
        None,
        Lake,
        River,
        Sea
    }
}
