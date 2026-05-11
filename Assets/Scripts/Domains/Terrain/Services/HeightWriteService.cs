using System.Collections.Generic;
using Territory.Core;
using Territory.Terrain;

namespace Domains.Terrain.Services
{
    /// <summary>
    /// Height mutation paths extracted from TerrainManager.
    /// Owns every heightMap.SetHeight / SetHeights call path that is not part of generation.
    /// Invariant #1: HeightMap.SetHeight ordering preserved verbatim; call sites identical to hub.
    /// Invariant #5 (cellArray carve-out): does not touch cell array.
    /// </summary>
    public class HeightWriteService
    {
        private readonly System.Func<HeightMap> _getHeightMap;
        private readonly System.Action<HeightMap> _setHeightMap;
        private readonly System.Func<int> _getWidth;
        private readonly System.Func<int> _getHeight;

        public HeightWriteService(
            System.Func<HeightMap> getHeightMap,
            System.Action<HeightMap> setHeightMap,
            System.Func<int> getWidth,
            System.Func<int> getHeight)
        {
            _getHeightMap = getHeightMap;
            _setHeightMap = setHeightMap;
            _getWidth = getWidth;
            _getHeight = getHeight;
        }

        /// <summary>
        /// Restore heightMap from saved grid data.
        /// Verbatim call-site ordering from TerrainManager.RestoreHeightMapFromGridData.
        /// Invariant #1: SetHeight order = gridData iteration order (unchanged).
        /// </summary>
        public void RestoreHeightMapFromGridData(List<CellData> gridData)
        {
            if (gridData == null) return;
            int w = _getWidth();
            int h = _getHeight();
            HeightMap heightMap = _getHeightMap();
            if (heightMap == null || heightMap.Width != w || heightMap.Height != h)
            {
                heightMap = new HeightMap(w, h);
                _setHeightMap(heightMap);
            }
            // Invariant #1: write order verbatim from saved gridData iteration — no reordering.
            foreach (CellData cellData in gridData)
            {
                if (heightMap.IsValidPosition(cellData.x, cellData.y))
                    heightMap.SetHeight(cellData.x, cellData.y, cellData.height);
            }
        }

        /// <summary>
        /// Modify terrain height at grid position.
        /// Stub body preserved from TerrainManager.ModifyTerrain (stub).
        /// </summary>
        public void ModifyTerrain(int x, int y, int newHeight)
        {
            // Implementation for terrain modification — stub body preserved from hub.
        }
    }
}
