using UnityEngine;
using Territory.Zones;
using Territory.Terrain;

namespace Territory.Core
{
    /// <summary>
    /// Calculates and assigns sprite sorting orders for all tile types (terrain, zoning,
    /// buildings, roads, sea-level) using height-aware formulas from TerrainManager.
    /// Extracted from GridManager to reduce its responsibilities.
    /// </summary>
    public class GridSortingOrderService
    {
        private readonly GridManager grid;

        /// <summary>Offset so roads render above adjacent terrain (depth step = 100). Prevents "buried" interstate/road appearance.</summary>
        public const int ROAD_SORTING_OFFSET = 106;

        public GridSortingOrderService(GridManager grid)
        {
            this.grid = grid;
        }

        /// <summary>
        /// Returns the maximum sorting order that any content on the cell at (x,y) would have
        /// (terrain, forest +5, road +ROAD_SORTING_OFFSET, building +10, etc.). Used so the building can place itself
        /// behind "front" adjacent cells and let forest/terrain draw on top.
        /// </summary>
        private int GetCellMaxContentSortingOrder(int x, int y)
        {
            if (x < 0 || x >= grid.width || y < 0 || y >= grid.height) return int.MinValue;
            if (grid.terrainManager == null) return int.MinValue;

            Cell cell = (x >= 0 && x < grid.width && y >= 0 && y < grid.height) ? grid.cellArray[x, y] : null;
            if (cell == null) return int.MinValue;

            int terrainOrder = grid.terrainManager.CalculateTerrainSortingOrder(x, y, cell.height);
            int maxOrder = terrainOrder;

            if (cell.GetComponent<SpriteRenderer>() != null)
                maxOrder = Mathf.Max(maxOrder, terrainOrder);

            for (int i = 0; i < cell.gameObject.transform.childCount; i++)
            {
                GameObject child = cell.gameObject.transform.GetChild(i).gameObject;
                if (child.GetComponent<SpriteRenderer>() == null) continue;

                int order;
                if (grid.terrainManager.IsWaterSlopeObject(child))
                    order = grid.terrainManager.CalculateWaterSlopeSortingOrder(x, y);
                else if (cell.forestObject != null && cell.forestObject == child)
                    order = terrainOrder + 5;
                else
                {
                    Zone zone = child.GetComponent<Zone>();
                    if (zone != null)
                    {
                        if (zone.zoneType == Zone.ZoneType.Road) order = terrainOrder + ROAD_SORTING_OFFSET;
                        else if (zone.zoneCategory == Zone.ZoneCategory.Zoning) order = terrainOrder + 0;
                        else if (zone.zoneCategory == Zone.ZoneCategory.Building) order = terrainOrder + 10;
                        else order = terrainOrder;
                    }
                    else
                        order = terrainOrder;
                }
                maxOrder = Mathf.Max(maxOrder, order);
            }
            return maxOrder;
        }

        /// <summary>
        /// Sets the sorting order of a tile using a legacy formula based on grid position. Prefers TerrainManager-based methods for new code.
        /// </summary>
        public int SetTileSortingOrder(GameObject tile, Zone.ZoneType zoneType = Zone.ZoneType.Grass)
        {
            Vector3 gridPos = grid.GetGridPosition(tile.transform.position);

            int x = (int)gridPos.x;
            int y = (int)gridPos.y;
            if (x < 0 || x >= grid.width || y < 0 || y >= grid.height)
            {
                return -1001;
            }

            Cell cell = grid.cellArray[x, y];
            tile.transform.SetParent(cell.gameObject.transform);
            SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();

            int baseSortingOrder = (y * grid.width + x);

            int sortingOrder;
            switch (zoneType)
            {
                case Zone.ZoneType.Grass:
                    sortingOrder = -(baseSortingOrder + 100000);
                    break;
                default:
                    sortingOrder = -(baseSortingOrder + 50000);
                    break;
            }
            sr.sortingOrder = sortingOrder;
            cell.SetCellInstanceSortingOrder(sortingOrder);
            return sortingOrder;
        }

        /// <summary>
        /// Sets sorting order for a zoning tile (RCI overlay) using TerrainManager so it renders below forest and buildings.
        /// </summary>
        public void SetZoningTileSortingOrder(GameObject tile, int x, int y)
        {
            if (x < 0 || x >= grid.width || y < 0 || y >= grid.height) return;

            Cell cell = grid.cellArray[x, y];
            if (cell == null) return;

            tile.transform.SetParent(cell.gameObject.transform);

            if (grid.terrainManager == null)
            {
                SetTileSortingOrder(tile, Zone.ZoneType.Grass);
                return;
            }

            int cellHeight = cell.height;
            if (grid.terrainManager.GetHeightMap() != null)
                cellHeight = grid.terrainManager.GetHeightMap().GetHeight(x, y);

            int sortingOrder = grid.terrainManager.CalculateTerrainSortingOrder(x, y, cellHeight);

            SpriteRenderer[] renderers = tile.GetComponentsInChildren<SpriteRenderer>();
            foreach (SpriteRenderer sr in renderers)
            {
                if (sr != null)
                    sr.sortingOrder = sortingOrder;
            }
        }

        /// <summary>
        /// Sets sorting order for a zone building (RCI) tile using TerrainManager so it renders above forest and terrain.
        /// </summary>
        public void SetZoneBuildingSortingOrder(GameObject tile, int x, int y)
        {
            if (x < 0 || x >= grid.width || y < 0 || y >= grid.height) return;

            Cell cell = grid.cellArray[x, y];
            if (cell == null) return;

            tile.transform.SetParent(cell.gameObject.transform);

            if (grid.terrainManager == null)
            {
                SetTileSortingOrder(tile, Zone.ZoneType.Building);
                return;
            }

            int cellHeight = cell.height;
            if (grid.terrainManager.GetHeightMap() != null)
                cellHeight = grid.terrainManager.GetHeightMap().GetHeight(x, y);

            int sortingOrder = grid.terrainManager.CalculateBuildingSortingOrder(x, y, cellHeight);

            SpriteRenderer[] renderers = tile.GetComponentsInChildren<SpriteRenderer>();
            foreach (SpriteRenderer sr in renderers)
            {
                if (sr != null)
                    sr.sortingOrder = sortingOrder;
            }
            cell.SetCellInstanceSortingOrder(sortingOrder);
        }

        /// <summary>
        /// Sets sorting order for a multi-cell building using the maximum order over its footprint
        /// so the whole building renders in front of all covered terrain.
        /// </summary>
        public void SetZoneBuildingSortingOrder(GameObject tile, int pivotX, int pivotY, int buildingSize)
        {
            if (buildingSize <= 1)
            {
                SetZoneBuildingSortingOrder(tile, pivotX, pivotY);
                return;
            }
            if (pivotX < 0 || pivotX >= grid.width || pivotY < 0 || pivotY >= grid.height) return;
            Cell pivotCell = grid.cellArray[pivotX, pivotY];
            if (pivotCell == null) return;
            tile.transform.SetParent(pivotCell.gameObject.transform);
            if (grid.terrainManager == null)
            {
                SetTileSortingOrder(tile, Zone.ZoneType.Building);
                return;
            }
            grid.GetBuildingFootprintOffset(buildingSize, out int offsetX, out int offsetY);
            int minFx = pivotX - offsetX;
            int minFy = pivotY - offsetY;
            int maxFx = minFx + buildingSize - 1;
            int maxFy = minFy + buildingSize - 1;

            int maxOrder = int.MinValue;

            for (int x = 0; x < buildingSize; x++)
            {
                for (int y = 0; y < buildingSize; y++)
                {
                    int gridX = pivotX + x - offsetX;
                    int gridY = pivotY + y - offsetY;
                    if (gridX < 0 || gridX >= grid.width || gridY < 0 || gridY >= grid.height) continue;
                    Cell cell = grid.cellArray[gridX, gridY];
                    if (cell == null) continue;
                    int cellHeight = cell.height;
                    if (grid.terrainManager.GetHeightMap() != null)
                        cellHeight = grid.terrainManager.GetHeightMap().GetHeight(gridX, gridY);
                    int order = grid.terrainManager.CalculateBuildingSortingOrder(gridX, gridY, cellHeight);
                    if (order > maxOrder) maxOrder = order;
                }
            }
            if (maxOrder == int.MinValue) return;

            // Front = left or top. Back = south-east face only.
            int minFrontAdjacentContentOrder = int.MaxValue;
            int maxBackAdjacentContentOrder = int.MinValue;
            for (int ax = minFx - 1; ax <= maxFx + 1; ax++)
            {
                for (int ay = minFy - 1; ay <= maxFy + 1; ay++)
                {
                    if (ax >= minFx && ax <= maxFx && ay >= minFy && ay <= maxFy) continue;
                    if (ax < 0 || ax >= grid.width || ay < 0 || ay >= grid.height) continue;
                    int contentOrder = GetCellMaxContentSortingOrder(ax, ay);
                    if (contentOrder == int.MinValue) continue;
                    bool isFront = (ax < minFx) || (ay < minFy);
                    bool isBackSouthEast = (ax == maxFx + 1 && ay >= minFy && ay <= maxFy + 1) || (ay == maxFy + 1 && ax >= minFx && ax <= maxFx + 1);
                    if (isFront && contentOrder < minFrontAdjacentContentOrder)
                        minFrontAdjacentContentOrder = contentOrder;
                    if (isBackSouthEast && contentOrder > maxBackAdjacentContentOrder)
                        maxBackAdjacentContentOrder = contentOrder;
                }
            }
            if (maxBackAdjacentContentOrder != int.MinValue)
            {
                int orderInFrontOfBack = maxBackAdjacentContentOrder + 1;
                if (orderInFrontOfBack > maxOrder)
                    maxOrder = orderInFrontOfBack;
            }
            if (minFrontAdjacentContentOrder != int.MaxValue)
            {
                int orderBehindFront = minFrontAdjacentContentOrder - 1;
                bool skipCapForVisibility = orderBehindFront < maxOrder && maxOrder > minFrontAdjacentContentOrder;
                if (orderBehindFront < maxOrder && !skipCapForVisibility)
                    maxOrder = orderBehindFront;
            }

            SpriteRenderer[] renderers = tile.GetComponentsInChildren<SpriteRenderer>();
            foreach (SpriteRenderer sr in renderers)
            {
                if (sr != null)
                    sr.sortingOrder = maxOrder;
            }
            pivotCell.SetCellInstanceSortingOrder(maxOrder);
        }

        /// <summary>
        /// Returns the sorting order to use for a road tile at (x, y) at the given height level.
        /// </summary>
        public int GetRoadSortingOrderForCell(int x, int y, int height)
        {
            if (x < 0 || x >= grid.width || y < 0 || y >= grid.height) return 0;
            if (grid.terrainManager == null) return 0;
            return grid.terrainManager.CalculateTerrainSortingOrder(x, y, height) + ROAD_SORTING_OFFSET;
        }

        /// <summary>
        /// Sets sorting order for a road tile using TerrainManager so it renders above grass and below forest/buildings.
        /// Also ensures grass and other terrain in the same cell render below the road.
        /// </summary>
        public void SetRoadSortingOrder(GameObject tile, int x, int y)
        {
            if (x < 0 || x >= grid.width || y < 0 || y >= grid.height) return;

            Cell cell = grid.cellArray[x, y];
            if (cell == null) return;

            tile.transform.SetParent(cell.gameObject.transform);
            tile.transform.SetAsLastSibling();

            if (grid.terrainManager == null)
            {
                SetTileSortingOrder(tile, Zone.ZoneType.Road);
                return;
            }

            int cellHeight = cell.height;
            if (grid.terrainManager.GetHeightMap() != null)
                cellHeight = grid.terrainManager.GetHeightMap().GetHeight(x, y);

            int terrainOrder = grid.terrainManager.CalculateTerrainSortingOrder(x, y, cellHeight);
            int roadSortingOrder = terrainOrder + ROAD_SORTING_OFFSET;

            if (cellHeight == 0)
            {
                int bridgeOrder = grid.terrainManager.CalculateTerrainSortingOrder(x, y, 1) + ROAD_SORTING_OFFSET;
                roadSortingOrder = Mathf.Max(roadSortingOrder, bridgeOrder);
            }

            SpriteRenderer[] renderers = tile.GetComponentsInChildren<SpriteRenderer>();
            foreach (SpriteRenderer sr in renderers)
            {
                if (sr != null)
                    sr.sortingOrder = roadSortingOrder;
            }

            for (int i = 0; i < cell.gameObject.transform.childCount; i++)
            {
                Transform child = cell.gameObject.transform.GetChild(i);
                if (child.gameObject == tile) continue;
                Zone zone = child.GetComponent<Zone>();
                if (zone != null && zone.zoneType == Zone.ZoneType.Road) continue;
                SpriteRenderer childSr = child.GetComponent<SpriteRenderer>();
                if (childSr != null && childSr.sortingOrder >= roadSortingOrder)
                    childSr.sortingOrder = terrainOrder;
            }
        }

        /// <summary>
        /// Sets the sorting order for a sea-level tile so it renders behind all land content.
        /// </summary>
        public int SetResortSeaLevelOrder(GameObject tile, Cell cell)
        {
            int x = (int)cell.x;
            int y = (int)cell.y;

            tile.transform.SetParent(cell.gameObject.transform);

            int baseSortingOrder = (y * grid.width + x);
            int sortingOrder = -(baseSortingOrder + 110000);

            SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();

            if (sr != null)
            {
                sr.sortingOrder = sortingOrder;
            }
            cell.sortingOrder = sortingOrder;
            return sortingOrder;
        }
    }
}
