using UnityEngine;
using Territory.Zones;
using Territory.Terrain;

namespace Territory.Core
{
    /// <summary>
    /// Sprite sorting orders for all tile types (terrain, zoning, buildings, roads, sea-level)
    /// via height-aware formulas from <see cref="TerrainManager"/>. Extracted from <see cref="GridManager"/>
    /// to reduce responsibilities.
    /// </summary>
    public class GridSortingOrderService
    {
        private readonly GridManager grid;

        /// <summary>Offset so roads render above adjacent terrain (depth step = 100). Prevents "buried" interstate/road look.</summary>
        public const int ROAD_SORTING_OFFSET = 106;

        public GridSortingOrderService(GridManager grid)
        {
            this.grid = grid;
        }

        /// <summary>
        /// After building sorting applied → re-apply pure terrain sorting to grass/slope zone children
        /// so they stay strictly below building (only when flat grass preserved). Multi-cell footprints
        /// call this per footprint cell.
        /// </summary>
        void SyncCellTerrainLayersBelowBuilding(int cellX, int cellY)
        {
            if (grid.terrainManager == null || grid.cellArray == null || grid.gridArray == null) return;
            if (cellX < 0 || cellX >= grid.width || cellY < 0 || cellY >= grid.height) return;

            Cell cell = grid.cellArray[cellX, cellY];
            GameObject cellGo = grid.gridArray[cellX, cellY];
            if (cell == null || cellGo == null) return;

            int h = cell.height;
            if (grid.terrainManager.GetHeightMap() != null)
                h = grid.terrainManager.GetHeightMap().GetHeight(cellX, cellY);

            foreach (Transform child in cellGo.transform)
            {
                GameObject go = child.gameObject;
                if (grid.terrainManager.IsWaterSlopeObject(go) || grid.terrainManager.IsShoreBayObject(go))
                    continue;

                Zone zone = go.GetComponent<Zone>();
                if (zone == null || zone.zoneType != Zone.ZoneType.Grass)
                    continue;

                int terrainOrder = grid.terrainManager.IsLandSlopeObject(go)
                    ? grid.terrainManager.CalculateSlopeSortingOrder(cellX, cellY, h)
                    : grid.terrainManager.CalculateTerrainSortingOrder(cellX, cellY, h);

                SpriteRenderer[] srs = go.GetComponentsInChildren<SpriteRenderer>(true);
                foreach (SpriteRenderer sr in srs)
                {
                    if (sr != null)
                        sr.sortingOrder = terrainOrder;
                }
            }
        }

        /// <summary>
        /// Max sorting order any content on cell (x,y) would have (terrain, forest +5, road
        /// +<see cref="ROAD_SORTING_OFFSET"/>, building +10, etc). Lets building sit behind "front"
        /// adjacent cells → forest/terrain draw on top.
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
                else if (grid.terrainManager.IsShoreBayObject(child))
                    order = grid.terrainManager.CalculateShoreBaySortingOrder(x, y);
                else if (cell.forestObject != null && cell.forestObject == child)
                    order = terrainOrder + 5;
                else
                {
                    Zone zone = child.GetComponent<Zone>();
                    if (zone != null)
                    {
                        if (zone.zoneType == Zone.ZoneType.Road)
                        {
                            int effectiveHeight = (cell.height == 0) ? 1 : cell.height;  // Bridge over water
                            order = grid.terrainManager.CalculateTerrainSortingOrder(x, y, effectiveHeight) + ROAD_SORTING_OFFSET;
                        }
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
        /// Legacy formula based on grid position. New code → prefer <see cref="TerrainManager"/>-based methods.
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
        /// Zoning tile (RCI overlay) sorting via <see cref="TerrainManager"/> → renders below forest + buildings.
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
        /// Zone building (RCI) sorting via <see cref="TerrainManager"/> → renders above forest + terrain.
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
            SyncCellTerrainLayersBelowBuilding(x, y);
        }

        /// <summary>
        /// Multi-cell building sorting via max order over footprint → whole building renders
        /// in front of all covered terrain.
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

            for (int fx = 0; fx < buildingSize; fx++)
            {
                for (int fy = 0; fy < buildingSize; fy++)
                {
                    int gridX = pivotX + fx - offsetX;
                    int gridY = pivotY + fy - offsetY;
                    if (gridX < 0 || gridX >= grid.width || gridY < 0 || gridY >= grid.height) continue;
                    SyncCellTerrainLayersBelowBuilding(gridX, gridY);
                }
            }
        }

        /// <summary>
        /// Sorting order for road tile at (x,y) at given height. Caps order when adjacent higher terrain
        /// sits "in front" (cut-through scenario).
        /// </summary>
        public int GetRoadSortingOrderForCell(int x, int y, int height)
        {
            if (x < 0 || x >= grid.width || y < 0 || y >= grid.height) return 0;
            if (grid.terrainManager == null) return 0;
            int roadSortingOrder = grid.terrainManager.CalculateTerrainSortingOrder(x, y, height) + ROAD_SORTING_OFFSET;

            var heightMap = grid.terrainManager.GetHeightMap();
            if (heightMap != null)
            {
                int[] adx = { 1, -1, 0, 0, 1, 1, -1, -1 };
                int[] ady = { 0, 0, 1, -1, 1, -1, 1, -1 };
                int roadDepth = x + y;
                int minFrontHigherOrder = int.MaxValue;
                for (int d = 0; d < 8; d++)
                {
                    int nx = x + adx[d];
                    int ny = y + ady[d];
                    if (nx < 0 || nx >= grid.width || ny < 0 || ny >= grid.height) continue;
                    int nh = heightMap.GetHeight(nx, ny);
                    if (nh < height) continue;
                    if ((nx + ny) >= roadDepth) continue;
                    int adjOrder = GetCellMaxContentSortingOrder(nx, ny);
                    if (adjOrder != int.MinValue && adjOrder < minFrontHigherOrder)
                        minFrontHigherOrder = adjOrder;
                }
                if (minFrontHigherOrder != int.MaxValue)
                    roadSortingOrder = Mathf.Min(roadSortingOrder, minFrontHigherOrder - 1);
            }

            return roadSortingOrder;
        }

        /// <summary>
        /// Road tile sorting via <see cref="TerrainManager"/> → renders above grass, below forest/buildings.
        /// Forces grass + other terrain in same cell below road.
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

            // Cut-through: road at lower height must render behind adjacent higher terrain that is "in front"
            var heightMap = grid.terrainManager.GetHeightMap();
            if (heightMap != null)
            {
                int[] adx = { 1, -1, 0, 0, 1, 1, -1, -1 };
                int[] ady = { 0, 0, 1, -1, 1, -1, 1, -1 };
                int roadDepth = x + y;
                int minFrontHigherOrder = int.MaxValue;
                for (int d = 0; d < 8; d++)
                {
                    int nx = x + adx[d];
                    int ny = y + ady[d];
                    if (nx < 0 || nx >= grid.width || ny < 0 || ny >= grid.height) continue;
                    int nh = heightMap.GetHeight(nx, ny);
                    if (nh < cellHeight) continue;
                    if ((nx + ny) >= roadDepth) continue;
                    int adjOrder = GetCellMaxContentSortingOrder(nx, ny);
                    if (adjOrder != int.MinValue && adjOrder < minFrontHigherOrder)
                        minFrontHigherOrder = adjOrder;
                }
                if (minFrontHigherOrder != int.MaxValue)
                    roadSortingOrder = Mathf.Min(roadSortingOrder, minFrontHigherOrder - 1);
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
        /// Sea-level tile sorting → renders behind all land content.
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
