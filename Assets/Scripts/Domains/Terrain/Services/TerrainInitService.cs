using System.Collections.Generic;
using UnityEngine;
using Territory.Core;
using Territory.Terrain;

namespace Domains.Terrain.Services
{
    /// <summary>
    /// One-time init paths extracted from TerrainManager.
    /// Owns HeightMap allocation + initial load + scene-load positioning.
    /// Invariant #1: HeightMap/Cell sync preserved — SetCellHeight + transform write order unchanged.
    /// Invariant #5: no cellArray mutation.
    /// </summary>
    public class TerrainInitService
    {
        private readonly System.Func<HeightMap> _getHeightMap;
        private readonly System.Action<HeightMap> _setHeightMap;
        private readonly System.Func<int> _getWidth;
        private readonly System.Func<int> _getHeight;
        private readonly System.Func<bool> _getNewGameFlatEnabled;
        private readonly System.Func<int> _getNewGameFlatHeight;
        private readonly System.Action<bool> _setNewGameFlatEnabled;
        private readonly System.Action<int> _setNewGameFlatHeight;
        private readonly System.Action _loadInitialHeightMap;
        private readonly System.Action _applyHeightMapToGrid;
        private readonly System.Action _ensureGuaranteedLakeDepressions;
        private readonly System.Func<int, int, CityCell> _getCell;
        private readonly System.Action<Vector2, int, bool> _setCellHeight;
        private readonly System.Func<int, int, int, Vector2> _getWorldPositionVector;

        /// <summary>Construct terrain init service with dependencies.</summary>
        public TerrainInitService(
            System.Func<HeightMap> getHeightMap,
            System.Action<HeightMap> setHeightMap,
            System.Func<int> getWidth,
            System.Func<int> getHeight,
            System.Func<bool> getNewGameFlatEnabled,
            System.Func<int> getNewGameFlatHeight,
            System.Action<bool> setNewGameFlatEnabled,
            System.Action<int> setNewGameFlatHeight,
            System.Action loadInitialHeightMap,
            System.Action applyHeightMapToGrid,
            System.Action ensureGuaranteedLakeDepressions,
            System.Func<int, int, CityCell> getCell,
            System.Action<Vector2, int, bool> setCellHeight,
            System.Func<int, int, int, Vector2> getWorldPositionVector)
        {
            _getHeightMap = getHeightMap;
            _setHeightMap = setHeightMap;
            _getWidth = getWidth;
            _getHeight = getHeight;
            _getNewGameFlatEnabled = getNewGameFlatEnabled;
            _getNewGameFlatHeight = getNewGameFlatHeight;
            _setNewGameFlatEnabled = setNewGameFlatEnabled;
            _setNewGameFlatHeight = setNewGameFlatHeight;
            _loadInitialHeightMap = loadInitialHeightMap;
            _applyHeightMapToGrid = applyHeightMapToGrid;
            _ensureGuaranteedLakeDepressions = ensureGuaranteedLakeDepressions;
            _getCell = getCell;
            _setCellHeight = setCellHeight;
            _getWorldPositionVector = getWorldPositionVector;
        }

        /// <summary>
        /// Configure next LoadInitialHeightMap to use uniform height (QA / method testing).
        /// Verbatim from TerrainManager.SetNewGameFlatTerrainOptions.
        /// </summary>
        public void SetNewGameFlatTerrainOptions(bool enabled, int uniformHeight)
        {
            _setNewGameFlatEnabled?.Invoke(enabled);
            _setNewGameFlatHeight?.Invoke(uniformHeight);
        }

        /// <summary>
        /// Return current heightmap instance, or null if not yet initialized.
        /// Verbatim from TerrainManager.GetHeightMap.
        /// </summary>
        public HeightMap GetHeightMap() => _getHeightMap?.Invoke();

        /// <summary>
        /// Return heightMap, creating or loading if null.
        /// Verbatim from TerrainManager.GetOrCreateHeightMap.
        /// </summary>
        public HeightMap GetOrCreateHeightMap()
        {
            EnsureHeightMapLoaded();
            return _getHeightMap?.Invoke();
        }

        /// <summary>
        /// Init heightmap + apply to grid → initial terrain elevations.
        /// Verbatim from TerrainManager.StartTerrainGeneration (minus gridManager null-check which hub retains).
        /// </summary>
        public void StartTerrainGeneration()
        {
            int w = _getWidth();
            int h = _getHeight();
            _setHeightMap?.Invoke(new HeightMap(w, h));
            _loadInitialHeightMap?.Invoke();
            _applyHeightMapToGrid?.Invoke();
            // Clear flat request — hub calls ClearNewGameFlatTerrainRequest after this.
            _setNewGameFlatEnabled?.Invoke(false);
        }

        /// <summary>
        /// Create fresh heightmap, load initial data, apply to grid.
        /// Verbatim from TerrainManager.InitializeHeightMap.
        /// </summary>
        public void InitializeHeightMap()
        {
            int w = _getWidth();
            int h = _getHeight();
            _setHeightMap?.Invoke(new HeightMap(w, h));
            _loadInitialHeightMap?.Invoke();
            if (!(_getNewGameFlatEnabled?.Invoke() ?? false))
                _ensureGuaranteedLakeDepressions?.Invoke();
            _applyHeightMapToGrid?.Invoke();
            _setNewGameFlatEnabled?.Invoke(false);
        }

        /// <summary>
        /// Ensure heightMap exists + has initial data (for RestoreTerrainForCell when init order skipped).
        /// Verbatim logic from TerrainManager.EnsureHeightMapLoaded (minus FindObjectOfType which hub retains).
        /// </summary>
        public void EnsureHeightMapLoaded()
        {
            HeightMap heightMap = _getHeightMap?.Invoke();
            if (heightMap != null) return;

            int w = _getWidth();
            int h = _getHeight();
            if (w > 0 && h > 0)
            {
                _setHeightMap?.Invoke(new HeightMap(w, h));
                _loadInitialHeightMap?.Invoke();
            }
        }

        /// <summary>
        /// Apply restored heightMap positions to all cell GameObjects.
        /// Verbatim call-site ordering from TerrainManager.ApplyRestoredPositionsToGrid.
        /// Invariant #1: SetCellHeight → transform.position write order unchanged.
        /// </summary>
        public void ApplyRestoredPositionsToGrid()
        {
            HeightMap heightMap = _getHeightMap?.Invoke();
            if (heightMap == null) return;
            int w = _getWidth();
            int h = _getHeight();
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    CityCell cell = _getCell?.Invoke(x, y);
                    if (cell == null) continue;
                    if (!heightMap.IsValidPosition(x, y)) continue;
                    int cellH = heightMap.GetHeight(x, y);
                    // Invariant #1: SetCellHeight before transform write — identical to hub ordering.
                    _setCellHeight?.Invoke(new Vector2(x, y), cellH, true);
                    Vector2 pos = _getWorldPositionVector?.Invoke(x, y, cellH) ?? Vector2.zero;
                    cell.gameObject.transform.position = pos;
                    cell.transformPosition = pos;
                }
            }
        }
    }
}
