namespace Territory.RegionScene.CellRendering
{
    /// <summary>Snapshot of the player city's visible state at the time of region render.
    /// Passed to IRegionCellRenderer.Render so sibling impls can show city icons, pop rings, etc.
    /// Null-safe: callers pass null when city data is unavailable.</summary>
    public sealed class PlayerCityState
    {
        /// <summary>Grid coords of the player 2x2 anchor cell (top-left of the 2x2 footprint).</summary>
        public int AnchorX { get; }
        public int AnchorY { get; }

        /// <summary>Latest population count. 0 until first growth tick lands.</summary>
        public int Population { get; }

        /// <summary>City-side tick at the moment of this snapshot.</summary>
        public long Tick { get; }

        public PlayerCityState(int anchorX, int anchorY, int population, long tick)
        {
            AnchorX    = anchorX;
            AnchorY    = anchorY;
            Population = population;
            Tick       = tick;
        }
    }
}
