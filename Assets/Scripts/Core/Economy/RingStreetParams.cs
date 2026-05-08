namespace Territory.Simulation
{
    /// <summary>Street extension params per urban ring for density gradient. Lifted from UrbanMetrics.cs to Core leaf for IUrbanCentroidService surface.</summary>
    [System.Serializable]
    public struct RingStreetParams
    {
        public int minLength;
        public int maxLength;
        public int parallelSpacing;
        public int parallelSpacingMin;
        public int parallelSpacingMax;
    }
}
