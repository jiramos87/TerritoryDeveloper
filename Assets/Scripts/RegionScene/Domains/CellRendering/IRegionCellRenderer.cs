using Territory.Core;

namespace Territory.RegionScene.CellRendering
{
    /// <summary>Pluggable renderer for a single region-scale cell. Default impl: BrownDiamondCellRenderer.
    /// Sibling exploration region-scale-city-blocks injects its own impl via ServiceRegistry-Region slot.
    /// Anchor: CellRenderer_ContractAndEvent.</summary>
    public interface IRegionCellRenderer
    {
        /// <summary>Render cell at (x, y) with optional city state overlay. Called per visible cell during stream-in
        /// and on PlayerCityDataUpdated for the player 2x2 area.</summary>
        void Render(RegionCell cell, PlayerCityState optionalCityState);
    }
}
