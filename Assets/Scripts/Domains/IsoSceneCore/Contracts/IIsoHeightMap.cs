namespace Territory.IsoSceneCore.Contracts
{
    /// <summary>Abstract height/water/cliff query interface. RegionHeightMap + CityHeightMap implement. Consumer: RegionCellRenderer, RegionCellInspectorPanel.</summary>
    public interface IIsoHeightMap
    {
        /// <summary>Grid dimension (width = height for square grids).</summary>
        int GridSize { get; }

        /// <summary>Return elevation at cell. Out-of-range → 0.</summary>
        int HeightAt(int x, int y);

        /// <summary>Seed procedural height generation with deterministic value.</summary>
        void Seed(int deterministicSeed);
    }
}
