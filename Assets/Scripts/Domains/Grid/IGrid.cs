using System.Collections.Generic;
using UnityEngine;
using Territory.Core;

namespace Domains.Grid
{
    /// <summary>
    /// Public facade interface for the Grid domain. Consumers bind to this interface only
    /// — never to GridManager or concrete service classes directly.
    /// Stage 1 surface: sorting-order methods extracted in tracer slice.
    /// Stage 5 surface: cell-access methods extracted via CellAccessService.
    /// </summary>
    public interface IGrid
    {
        // ── Sorting order (Stage 1) ─────────────────────────────────────────

        /// <summary>Returns the sorting order for a road tile at (x,y) at given height.</summary>
        int GetRoadSortingOrderForCell(int x, int y, int height);

        /// <summary>Sets sorting order for a zoning tile at (x,y). Renders below forest + buildings.</summary>
        void SetZoningTileSortingOrder(GameObject tile, int x, int y);

        /// <summary>Sets sorting order for a zone building at (x,y). Renders above forest + terrain.</summary>
        void SetZoneBuildingSortingOrder(GameObject tile, int x, int y);

        /// <summary>Sets sorting order for a multi-cell building at pivot (x,y) with given size.</summary>
        void SetZoneBuildingSortingOrder(GameObject tile, int pivotX, int pivotY, int buildingSize);

        /// <summary>Sets road tile sorting order at (x,y). Forces grass + terrain below road.</summary>
        void SetRoadSortingOrder(GameObject tile, int x, int y);

        /// <summary>Sets sea-level tile sorting order. Renders behind all land content.</summary>
        int SetResortSeaLevelOrder(GameObject tile, CityCell cell);

        /// <summary>Legacy tile sorting order formula. Prefer TerrainManager-based methods.</summary>
        int SetTileSortingOrder(GameObject tile, Territory.Zones.Zone.ZoneType zoneType);

        // ── Cell access (Stage 5) ───────────────────────────────────────────

        /// <summary>CityCell at grid coords, or null if out of bounds.</summary>
        CityCell GetCell(int x, int y);

        /// <summary>Typed accessor for CellBase subclasses at grid coords.</summary>
        T GetCell<T>(int x, int y) where T : CellBase;

        /// <summary>GameObject for cell at pos, or null if out of bounds.</summary>
        GameObject GetGridCell(Vector2 gridPos);

        /// <summary>Serialize every cell in grid to CellData list for saving.</summary>
        List<CellData> GetGridData();

        /// <summary>True if cell on outer edge of grid (first or last row/column).</summary>
        bool IsBorderCell(int x, int y);

        /// <summary>True if cell occupied by building (any tile of multi-cell footprint).</summary>
        bool IsCellOccupiedByBuilding(int x, int y);

        /// <summary>Footprint offset for building (even size = 0,0; odd size = buildingSize/2).</summary>
        void GetBuildingFootprintOffset(int buildingSize, out int offsetX, out int offsetY);

        /// <summary>Return pivot cell of multi-cell building, or grid cell for single-cell buildings.</summary>
        GameObject GetBuildingPivotCell(CityCell cell);
    }
}
