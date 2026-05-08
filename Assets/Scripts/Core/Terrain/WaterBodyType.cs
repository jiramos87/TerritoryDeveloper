namespace Territory.Terrain
{
    /// <summary>
    /// Classification of water mass per cell for serialization + gameplay (rivers, sea, lakes).
    /// </summary>
    public enum WaterBodyType
    {
        None,
        Lake,
        River,
        Sea
    }
}
