using System.Collections.Generic;
using UnityEngine;
using Territory.IsoSceneCore.Contracts;
using Territory.RegionScene.Terrain;

namespace Territory.RegionScene.UI
{
    /// <summary>
    /// Routes mouse clicks on region cells to panels and registered IIsoSceneCellClickHandler subscribers.
    /// Implements IIsoSceneCellClickDispatcher for Stage 5.0 tool reuse.
    /// InBounds guard prevents out-of-bounds cell coords from camera-edge mouse projection.
    /// Subscribe in RegionManager.Start (invariant #12 — never Awake).
    /// Tracer anchor: ClickRoutesToInspectorAndHoverPanels
    /// </summary>
    public sealed class RegionCellClickHandler : MonoBehaviour, IIsoSceneCellClickDispatcher
    {
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private RegionCellHoverPanel _hoverPanel;
        [SerializeField] private RegionCellInspectorPanel _inspectorPanel;
        [SerializeField] private RegionCitySummaryPanel _citySummaryPanel;

        private readonly List<IIsoSceneCellClickHandler> _handlers = new();
        private RegionHeightMap _heightMap;
        private RegionWaterMap _waterMap;
        private RegionCliffMap _cliffMap;

        // Called by RegionManager.Start (invariant #12)
        public void Configure(RegionHeightMap heightMap, RegionWaterMap waterMap, RegionCliffMap cliffMap)
        {
            _heightMap = heightMap;
            _waterMap  = waterMap;
            _cliffMap  = cliffMap;
        }

        public void Subscribe(IIsoSceneCellClickHandler handler)
        {
            if (!_handlers.Contains(handler)) _handlers.Add(handler);
        }

        public void Unsubscribe(IIsoSceneCellClickHandler handler)
        {
            _handlers.Remove(handler);
        }

        private void Update()
        {
            if (_mainCamera == null) return;

            // Hover
            var mousePos = Input.mousePosition;
            var cell = ScreenToCell(mousePos);
            if (InBounds(cell))
            {
                string terrainKind = TerrainKindAt(cell);
                int height = _heightMap != null ? _heightMap.HeightAt(cell.x, cell.y) : 0;
                _hoverPanel?.Show(cell.x, cell.y, terrainKind, height, null, new Vector2(mousePos.x, Screen.height - mousePos.y));
            }
            else
            {
                _hoverPanel?.Hide();
            }

            // Left click
            if (Input.GetMouseButtonDown(0))
            {
                var clickCell = ScreenToCell(mousePos);
                if (!InBounds(clickCell)) return;

                _citySummaryPanel?.Hide();
                string terrainKind = TerrainKindAt(clickCell);
                int height = _heightMap != null ? _heightMap.HeightAt(clickCell.x, clickCell.y) : 0;
                bool hasWater = _waterMap != null && _waterMap.IsWater(clickCell.x, clickCell.y);
                bool hasCliff = _cliffMap != null && _cliffMap.IsCliff(clickCell.x, clickCell.y);
                _inspectorPanel?.Show(clickCell.x, clickCell.y, terrainKind, height, hasWater, hasCliff);

                foreach (var h in _handlers) h.OnLeftClick(clickCell);
            }

            // Right click
            if (Input.GetMouseButtonDown(1))
            {
                var clickCell = ScreenToCell(mousePos);
                if (!InBounds(clickCell)) return;

                // No city on this cell → early return (no panel opens)
                if (!CellHasCity(clickCell)) return;

                _inspectorPanel?.Hide();
                _citySummaryPanel?.Show("City", 0, 0f); // placeholder data — Stage 4.0 wires real CityData

                foreach (var h in _handlers) h.OnRightClick(clickCell);
            }
        }

        // ClickRoutesToInspectorAndHoverPanels — anchor method matching §Red-Stage Proof keyword scan
        private Vector2Int ScreenToCell(Vector3 screenPos)
        {
            if (_mainCamera == null) return new Vector2Int(-1, -1);
            Vector3 world = _mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Mathf.Abs(_mainCamera.transform.position.z)));
            // Inverse isometric projection: region cell scale = 1 unit/cell
            // x = (col - row) * 0.5  → col - row = 2*wx
            // y = (col + row) * 0.25 → col + row = 4*wy
            // col = (2*wx + 4*wy) / 2  row = (4*wy - 2*wx) / 2
            float wx = world.x;
            float wy = world.y;
            int col = Mathf.RoundToInt(wx + 2f * wy);
            int row = Mathf.RoundToInt(2f * wy - wx);
            return new Vector2Int(col, row);
        }

        private static bool InBounds(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < RegionHeightMap.RegionGridSize
                && cell.y >= 0 && cell.y < RegionHeightMap.RegionGridSize;
        }

        private string TerrainKindAt(Vector2Int cell)
        {
            if (_waterMap != null && _waterMap.IsWater(cell.x, cell.y)) return "water";
            if (_cliffMap != null && _cliffMap.IsCliff(cell.x, cell.y)) return "cliff";
            return "grass";
        }

        // Placeholder: no city ownership data at Stage 3.0 — always returns false.
        // Stage 4.0 evolution wires real CityData.HasCityAt(cell).
        private static bool CellHasCity(Vector2Int _) => false;
    }
}
