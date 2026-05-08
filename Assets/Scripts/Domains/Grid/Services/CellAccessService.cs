using System.Collections.Generic;
using UnityEngine;
using Territory.Core;
using Territory.Zones;

namespace Domains.Grid.Services
{
    /// <summary>
    /// Pure cell-access queries extracted from <see cref="IGridManager"/>.
    /// Holds <see cref="IGridManager"/> ref via composition; no MonoBehaviour lifecycle.
    /// Stage 5 carve-out: GetCell / GetGridData / footprint helpers / building-occupancy queries.
    /// Domain-leaf: refs Core only (no Game asmdef dep).
    /// </summary>
    public class CellAccessService
    {
        private readonly IGridManager grid;

        public CellAccessService(IGridManager grid)
        {
            this.grid = grid;
        }

        /// <summary>CityCell at grid coords, or null if out of bounds.</summary>
        public CityCell GetCell(int x, int y)
        {
            if (x >= 0 && x < grid.width && y >= 0 && y < grid.height)
                return grid.GetCell(x, y);
            return null;
        }

        /// <summary>Typed accessor for CellBase subclasses. MVP: only CityCell stored; non-CityCell T returns null.</summary>
        public T GetCell<T>(int x, int y) where T : CellBase
        {
            if (x < 0 || x >= grid.width || y < 0 || y >= grid.height) return null;
            return grid.GetCell(x, y) as T;
        }

        /// <summary>GameObject for cell at pos, or null if out of bounds.</summary>
        public GameObject GetGridCell(Vector2 gridPos)
        {
            if (gridPos.x < 0 || gridPos.x >= grid.width ||
                gridPos.y < 0 || gridPos.y >= grid.height)
                return null;
            return grid.GetGridCell(gridPos);
        }

        /// <summary>Serialize every cell in grid to CellData list for saving.</summary>
        public List<CellData> GetGridData()
        {
            var gridData = new List<CellData>();
            for (int x = 0; x < grid.width; x++)
            {
                for (int y = 0; y < grid.height; y++)
                {
                    CityCell cellComponent = grid.GetCell(x, y);
                    gridData.Add(cellComponent.GetCellData());
                }
            }
            return gridData;
        }

        /// <summary>True if cell on outer edge of grid (first or last row/column).</summary>
        public bool IsBorderCell(int x, int y)
        {
            return x == 0 || x == grid.width - 1 || y == 0 || y == grid.height - 1;
        }

        /// <summary>True if cell occupied by building (any tile of multi-cell footprint).</summary>
        public bool IsCellOccupiedByBuilding(int x, int y)
        {
            if (x < 0 || x >= grid.width || y < 0 || y >= grid.height) return false;
            CityCell cell = grid.GetCell(x, y);
            if (cell == null) return false;
            return cell.occupiedBuilding != null || IsZoneTypeBuilding(cell.zoneType);
        }

        /// <summary>True if zone type is a building variant (residential / commercial / industrial / generic).</summary>
        public bool IsZoneTypeBuilding(Zone.ZoneType zoneType)
        {
            return zoneType == Zone.ZoneType.Building ||
                   zoneType == Zone.ZoneType.ResidentialLightBuilding ||
                   zoneType == Zone.ZoneType.ResidentialMediumBuilding ||
                   zoneType == Zone.ZoneType.ResidentialHeavyBuilding ||
                   zoneType == Zone.ZoneType.CommercialLightBuilding ||
                   zoneType == Zone.ZoneType.CommercialMediumBuilding ||
                   zoneType == Zone.ZoneType.CommercialHeavyBuilding ||
                   zoneType == Zone.ZoneType.IndustrialLightBuilding ||
                   zoneType == Zone.ZoneType.IndustrialMediumBuilding ||
                   zoneType == Zone.ZoneType.IndustrialHeavyBuilding;
        }

        /// <summary>Footprint offset for building (even size = 0,0; odd size = buildingSize/2).</summary>
        public void GetBuildingFootprintOffset(int buildingSize, out int offsetX, out int offsetY)
        {
            if (buildingSize % 2 == 0)
            {
                offsetX = 0;
                offsetY = 0;
            }
            else
            {
                offsetX = buildingSize / 2;
                offsetY = buildingSize / 2;
            }
        }

        /// <summary>Return pivot cell of multi-cell building. If given cell inside footprint, find + return pivot.</summary>
        public GameObject GetBuildingPivotCell(CityCell cell)
        {
            if (cell == null) return null;
            if (cell.occupiedBuilding == null || cell.buildingSize <= 1)
                return grid.GetGridCell(new Vector2(cell.x, cell.y));

            int size = cell.buildingSize;
            int cx = (int)cell.x;
            int cy = (int)cell.y;

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    int px = cx - i;
                    int py = cy - j;
                    if (px >= 0 && px < grid.width && py >= 0 && py < grid.height)
                    {
                        CityCell pivotCandidate = grid.GetCell(px, py);
                        if (pivotCandidate != null && pivotCandidate.isPivot)
                            return grid.GetGridCell(new Vector2(px, py));
                    }
                }
            }
            return grid.GetGridCell(new Vector2(cx, cy));
        }
    }
}
