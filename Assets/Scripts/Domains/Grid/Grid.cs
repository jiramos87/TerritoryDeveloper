using System.Collections.Generic;
using UnityEngine;
using Territory.Core;
using Domains.Grid.Services;

namespace Domains.Grid
{
    /// <summary>
    /// Facade impl for the Grid domain. Thin orchestrator — MonoBehaviour; holds IGridManager ref.
    /// Stage 1: GridSortingOrderService (sorting-order concerns).
    /// Stage 5: CellAccessService (cell-query concerns).
    /// Domain-leaf: refs Core only — IGridManager resolved via runtime FindObjectsOfType filter.
    /// </summary>
    public class Grid : MonoBehaviour, IGrid
    {
        private IGridManager gridManager;

        private GridSortingOrderService _sortingOrderService;
        private CellAccessService _cellAccessService;

        private void Awake()
        {
            if (gridManager == null)
            {
                foreach (MonoBehaviour mb in Object.FindObjectsOfType<MonoBehaviour>())
                {
                    if (mb is IGridManager gm) { gridManager = gm; break; }
                }
            }

            if (gridManager != null)
            {
                _sortingOrderService = new GridSortingOrderService(gridManager);
                _cellAccessService = new CellAccessService(gridManager);
            }
        }

        // ── Sorting order (Stage 1) ─────────────────────────────────────────

        /// <inheritdoc/>
        public int GetRoadSortingOrderForCell(int x, int y, int height)
        {
            EnsureServices();
            return _sortingOrderService.GetRoadSortingOrderForCell(x, y, height);
        }

        /// <inheritdoc/>
        public void SetZoningTileSortingOrder(GameObject tile, int x, int y)
        {
            EnsureServices();
            _sortingOrderService.SetZoningTileSortingOrder(tile, x, y);
        }

        /// <inheritdoc/>
        public void SetZoneBuildingSortingOrder(GameObject tile, int x, int y)
        {
            EnsureServices();
            _sortingOrderService.SetZoneBuildingSortingOrder(tile, x, y);
        }

        /// <inheritdoc/>
        public void SetZoneBuildingSortingOrder(GameObject tile, int pivotX, int pivotY, int buildingSize)
        {
            EnsureServices();
            _sortingOrderService.SetZoneBuildingSortingOrder(tile, pivotX, pivotY, buildingSize);
        }

        /// <inheritdoc/>
        public void SetRoadSortingOrder(GameObject tile, int x, int y)
        {
            EnsureServices();
            _sortingOrderService.SetRoadSortingOrder(tile, x, y);
        }

        /// <inheritdoc/>
        public int SetResortSeaLevelOrder(GameObject tile, CityCell cell)
        {
            EnsureServices();
            return _sortingOrderService.SetResortSeaLevelOrder(tile, cell);
        }

        /// <inheritdoc/>
        public int SetTileSortingOrder(GameObject tile, Territory.Zones.Zone.ZoneType zoneType = Territory.Zones.Zone.ZoneType.Grass)
        {
            EnsureServices();
            return _sortingOrderService.SetTileSortingOrder(tile, zoneType);
        }

        // ── Cell access (Stage 5) ───────────────────────────────────────────

        /// <inheritdoc/>
        public CityCell GetCell(int x, int y)
        {
            EnsureServices();
            return _cellAccessService.GetCell(x, y);
        }

        /// <inheritdoc/>
        public T GetCell<T>(int x, int y) where T : CellBase
        {
            EnsureServices();
            return _cellAccessService.GetCell<T>(x, y);
        }

        /// <inheritdoc/>
        public GameObject GetGridCell(Vector2 gridPos)
        {
            EnsureServices();
            return _cellAccessService.GetGridCell(gridPos);
        }

        /// <inheritdoc/>
        public List<CellData> GetGridData()
        {
            EnsureServices();
            return _cellAccessService.GetGridData();
        }

        /// <inheritdoc/>
        public bool IsBorderCell(int x, int y)
        {
            EnsureServices();
            return _cellAccessService.IsBorderCell(x, y);
        }

        /// <inheritdoc/>
        public bool IsCellOccupiedByBuilding(int x, int y)
        {
            EnsureServices();
            return _cellAccessService.IsCellOccupiedByBuilding(x, y);
        }

        /// <inheritdoc/>
        public void GetBuildingFootprintOffset(int buildingSize, out int offsetX, out int offsetY)
        {
            if (_cellAccessService == null && gridManager != null)
                _cellAccessService = new CellAccessService(gridManager);
            if (_cellAccessService != null)
            {
                _cellAccessService.GetBuildingFootprintOffset(buildingSize, out offsetX, out offsetY);
                return;
            }
            // Stateless fallback — no GridManager ref needed for this pure calc
            offsetX = buildingSize % 2 == 0 ? 0 : buildingSize / 2;
            offsetY = buildingSize % 2 == 0 ? 0 : buildingSize / 2;
        }

        /// <inheritdoc/>
        public GameObject GetBuildingPivotCell(CityCell cell)
        {
            EnsureServices();
            return _cellAccessService.GetBuildingPivotCell(cell);
        }

        private void EnsureServices()
        {
            if (gridManager == null)
            {
                foreach (MonoBehaviour mb in Object.FindObjectsOfType<MonoBehaviour>())
                {
                    if (mb is IGridManager gm) { gridManager = gm; break; }
                }
                if (gridManager == null) return;
            }
            if (_sortingOrderService == null)
                _sortingOrderService = new GridSortingOrderService(gridManager);
            if (_cellAccessService == null)
                _cellAccessService = new CellAccessService(gridManager);
        }
    }
}
