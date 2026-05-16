namespace Territory.RegionScene.CellRendering
{
    /// <summary>
    /// Lightweight cell descriptor passed to IRegionCellRenderer.Render(). Carries grid (x,y) coordinates
    /// + optional owning-city id resolved from the underlying RegionCellData record.
    /// Sibling plugins consume this to draw region-scale block visuals.
    /// </summary>
    public readonly struct RegionCell
    {
        public readonly int X;
        public readonly int Y;
        public readonly string OwningCityId;

        public RegionCell(int x, int y, string owningCityId)
        {
            X = x;
            Y = y;
            OwningCityId = owningCityId;
        }
    }
}
