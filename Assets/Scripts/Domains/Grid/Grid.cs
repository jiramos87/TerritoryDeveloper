using UnityEngine;
using Territory.Core;
using Domains.Grid.Services;

namespace Domains.Grid
{
    /// <summary>
    /// Facade impl for the Grid domain. Thin orchestrator — MonoBehaviour; holds GridManager ref.
    /// Composes GridSortingOrderService for sorting-order concerns.
    /// Stage 1 tracer: exposes IGrid surface backed by GridSortingOrderService.
    /// </summary>
    public class Grid : MonoBehaviour, IGrid
    {
        [SerializeField] private GridManager gridManager;

        private GridSortingOrderService _sortingOrderService;

        private void Awake()
        {
            if (gridManager == null)
                gridManager = FindObjectOfType<GridManager>();

            if (gridManager != null)
                _sortingOrderService = new GridSortingOrderService(gridManager);
        }

        /// <inheritdoc/>
        public int GetRoadSortingOrderForCell(int x, int y, int height)
        {
            EnsureService();
            return _sortingOrderService.GetRoadSortingOrderForCell(x, y, height);
        }

        /// <inheritdoc/>
        public void SetZoningTileSortingOrder(GameObject tile, int x, int y)
        {
            EnsureService();
            _sortingOrderService.SetZoningTileSortingOrder(tile, x, y);
        }

        /// <inheritdoc/>
        public void SetZoneBuildingSortingOrder(GameObject tile, int x, int y)
        {
            EnsureService();
            _sortingOrderService.SetZoneBuildingSortingOrder(tile, x, y);
        }

        /// <inheritdoc/>
        public void SetZoneBuildingSortingOrder(GameObject tile, int pivotX, int pivotY, int buildingSize)
        {
            EnsureService();
            _sortingOrderService.SetZoneBuildingSortingOrder(tile, pivotX, pivotY, buildingSize);
        }

        /// <inheritdoc/>
        public void SetRoadSortingOrder(GameObject tile, int x, int y)
        {
            EnsureService();
            _sortingOrderService.SetRoadSortingOrder(tile, x, y);
        }

        /// <inheritdoc/>
        public int SetResortSeaLevelOrder(GameObject tile, CityCell cell)
        {
            EnsureService();
            return _sortingOrderService.SetResortSeaLevelOrder(tile, cell);
        }

        /// <inheritdoc/>
        public int SetTileSortingOrder(GameObject tile, Territory.Zones.Zone.ZoneType zoneType = Territory.Zones.Zone.ZoneType.Grass)
        {
            EnsureService();
            return _sortingOrderService.SetTileSortingOrder(tile, zoneType);
        }

        private void EnsureService()
        {
            if (_sortingOrderService == null && gridManager != null)
                _sortingOrderService = new GridSortingOrderService(gridManager);
        }
    }
}
