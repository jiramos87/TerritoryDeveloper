using Territory.RegionScene.Terrain;

namespace Territory.RegionScene.Evolution
{
    /// <summary>Scene-level region state: grid of RegionCellData. Registered into ServiceRegistry from RegionManager.Awake.</summary>
    public sealed class RegionData
    {
        private readonly RegionCellData[] _cells;
        private readonly int _gridSize;

        public int GridSize => _gridSize;

        public RegionData(int gridSize)
        {
            _gridSize = gridSize;
            _cells = new RegionCellData[gridSize * gridSize];
            for (int i = 0; i < _cells.Length; i++)
                _cells[i] = new RegionCellData { terrainKind = RegionTerrainKind.Flat, pop = 0, urbanArea = 0f };
        }

        public RegionCellData GetCell(int x, int y)
        {
            if (x < 0 || x >= _gridSize || y < 0 || y >= _gridSize) return null;
            return _cells[y * _gridSize + x];
        }

        public void SetCell(int x, int y, RegionCellData data)
        {
            if (x < 0 || x >= _gridSize || y < 0 || y >= _gridSize) return;
            _cells[y * _gridSize + x] = data;
        }

        /// <summary>All cells in row-major order (read-only snapshot suitable for iteration).</summary>
        public RegionCellData[] AllCells => _cells;
    }
}
