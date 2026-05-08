using UnityEngine;
using Territory.Core;

namespace Domains.Grid
{
    /// <summary>
    /// Public facade interface for the Grid domain. Consumers bind to this interface only
    /// — never to GridManager or concrete service classes directly.
    /// Stage 1 surface: sorting-order methods extracted in tracer slice.
    /// </summary>
    public interface IGrid
    {
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
    }
}
