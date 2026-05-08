namespace Territory.Simulation
{
    /// <summary>Urban ring classification for density gradient + sector-based zoning.
    /// Inner=urban center (dense, no industrial), Mid=residential, Outer=transition to rural, Rural=sparse.
    /// Lifted from UrbanMetrics.cs to Core leaf so IUrbanCentroidService surface can reference without Game.asmdef ref.</summary>
    public enum UrbanRing
    {
        Inner,
        Mid,
        Outer,
        Rural
    }
}
