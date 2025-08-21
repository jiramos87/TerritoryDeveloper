using UnityEngine;
using System.Collections.Generic;

public class TerrainManager : MonoBehaviour
{
    public GridManager gridManager;
    private HeightMap heightMap;
    public ZoneManager zoneManager;
    public WaterManager waterManager;

    // References to slope prefabs
    public GameObject northSlopePrefab;
    public GameObject southSlopePrefab;
    public GameObject eastSlopePrefab;
    public GameObject westSlopePrefab;
    public GameObject northEastSlopePrefab;
    public GameObject northWestSlopePrefab;
    public GameObject southEastSlopePrefab;
    public GameObject southWestSlopePrefab;
    public GameObject northEastUpslopePrefab;
    public GameObject northWestUpslopePrefab;
    public GameObject southEastUpslopePrefab;
    public GameObject southWestUpslopePrefab;

    // Water-slope prefabs
    public GameObject northSlopeWaterPrefab;
    public GameObject southSlopeWaterPrefab;
    public GameObject eastSlopeWaterPrefab;
    public GameObject westSlopeWaterPrefab;
    public GameObject northEastSlopeWaterPrefab;
    public GameObject northWestSlopeWaterPrefab;
    public GameObject southEastSlopeWaterPrefab;
    public GameObject southWestSlopeWaterPrefab;
    public GameObject northEastUpslopeWaterPrefab;
    public GameObject northWestUpslopeWaterPrefab;
    public GameObject southEastUpslopeWaterPrefab;
    public GameObject southWestUpslopeWaterPrefab;

    public GameObject seaLevelWaterPrefab;

    public const int MIN_HEIGHT = 0;
    public const int MAX_HEIGHT = 5;
    public const int SEA_LEVEL = 0;

    // Sorting order constants for different object types
    public const int TERRAIN_BASE_ORDER = 0;
    public const int SLOPE_OFFSET = -1;
    public const int BUILDING_OFFSET = 10; // Buildings should be above terrain
    public const int DEPTH_MULTIPLIER = 100;
    public const int HEIGHT_MULTIPLIER = 1000;

    public void StartTerrainGeneration()
    {
        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridManager>();
        }

        heightMap = new HeightMap(gridManager.width, gridManager.height);
        LoadInitialHeightMap();
        ApplyHeightMapToGrid();
    }

    public HeightMap GetHeightMap()
    {
        return heightMap;
    }

    public void InitializeHeightMap()
    {
        heightMap = new HeightMap(gridManager.width, gridManager.height);
        LoadInitialHeightMap();
        ApplyHeightMapToGrid();
    }

    private void LoadInitialHeightMap()
    {
        int[,] initialHeights = new int[,] {
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 2, 2, 2, 2, 2, 1, 1, 1, 1, 0, 1, 1, 2, 1, 1, 1, 1, 1},
          {1, 1, 2, 3, 3, 3, 2, 1, 1, 1, 1, 0, 1, 1, 2, 1, 1, 1, 1, 1},
          {1, 1, 2, 3, 3, 3, 2, 1, 1, 1, 1, 0, 1, 2, 2, 1, 1, 1, 1, 1},
          {1, 1, 2, 3, 3, 3, 2, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 2, 2, 2, 2, 2, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 2, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 2, 2, 2, 2},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 2, 1, 2},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 2},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 2},
          {1, 1, 2, 2, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 1, 1, 1, 1},
          {1, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1},
          {2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 1, 0, 1, 1, 1, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 3, 2, 1, 0, 1, 1, 1, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 1, 0, 1, 1, 1, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1},
        };
        heightMap.SetHeights(initialHeights);
    }

    private void ApplyHeightMapToGrid()
    {
        for (int sum = 0; sum < gridManager.width + gridManager.height - 1; sum++)
        {
            for (int x = 0; x < gridManager.width; x++)
            {
                int y = sum - x;
                if (y >= 0 && y < gridManager.height)
                {
                    UpdateTileElevation(x, y);
                }
            }
        }
    }

    private void UpdateTileElevation(int x, int y)
    {
        int newHeight = heightMap.GetHeight(x, y);
        Cell cell = gridManager.GetCell(x, y);
        if (cell == null) return;
        gridManager.SetCellHeight(new Vector2(x, y), newHeight);

        Vector2 newWorldPos = gridManager.GetCellWorldPosition(cell);
        cell.gameObject.transform.position = newWorldPos;
        cell.transformPosition = newWorldPos;

        int sortingOrder = CalculateTerrainSortingOrder(x, y, newHeight);

        cell.sortingOrder = sortingOrder;
        SpriteRenderer sr = cell.gameObject.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = sortingOrder;
        }

        if (RequiresSlope(x, y, newHeight))
        {
            PlaceSlope(x, y);
        }

        if (newHeight == SEA_LEVEL)
        {
            ModifyWaterSlopeInAdjacentNeighbors(x, y);
            return;
        }
    }

    private void DestroyCellChildren(Cell cell)
    {
        GameObject cellObject = cell.gameObject;  // Get the GameObject that holds the Cell component
        foreach (Transform child in cellObject.transform)
        {
            DestroyImmediate(child.gameObject);
        }
    }

    private bool RequiresSlope(int x, int y, int currentHeight)
    {
        if (currentHeight == 0)
        {
            return false;
        }
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                int nx = x + dx;
                int ny = y + dy;

                if (heightMap.IsValidPosition(nx, ny))
                {
                    int neighborHeight = heightMap.GetHeight(nx, ny);
                    if (Mathf.Abs(neighborHeight - currentHeight) > 0)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private void PlaceSlope(int x, int y)
    {
        int currentHeight = heightMap.GetHeight(x, y);

        var slopeResult = DetermineSlopePrefab(x, y);
        if (!slopeResult.HasValue)
        {
            return;
        }

        var (key, slopePrefab) = slopeResult.Value;

        if (slopePrefab != null)
        {
            Cell cell = gridManager.GetCell(x, y);
            DestroyCellChildren(cell);

            Vector2 worldPos = cell.transformPosition;
            GameObject slope = Instantiate(
                slopePrefab,
                worldPos,
                Quaternion.identity
            );
            slope.transform.SetParent(cell.gameObject.transform);
            cell.terrainSlope = key;

            SpriteRenderer sr = slope.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = CalculateSlopeSortingOrder(x, y, currentHeight);
            }
        }
    }

    private void ModifyWaterSlopeInAdjacentNeighbors(int x, int y)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                int nx = x + dx;
                int ny = y + dy;

                if (heightMap.IsValidPosition(nx, ny))
                {

                    int neighborHeight = heightMap.GetHeight(nx, ny);

                    if (neighborHeight == SEA_LEVEL)
                    {
                        continue;
                    }
                    GameObject waterSlopePrefab = DetermineWaterSlopePrefab(nx, ny);
                    if (waterSlopePrefab == null)
                    {
                        continue;
                    }

                    PlaceWaterSlope(nx, ny, waterSlopePrefab);
                }
            }
        }

        PlaceSeaLevelWater(x, y);
    }

    private void PlaceSeaLevelWater(int x, int y)
    {
        GameObject seaLevelWater = seaLevelWaterPrefab;
        if (seaLevelWater == null)
        {
            Debug.Log("seaLevelWaterPrefab is null for position " + x + ", " + y);
            return;
        }

        Cell cell = gridManager.GetCell(x, y);
        if (cell == null)
        {
            Debug.Log("cell is null for position " + x + ", " + y);
            return;
        }
        if (cell.gameObject == null || cell.gameObject.transform == null)
        {
            Debug.Log("cell.gameObject or cell.gameObject.transform is null for position " + x + ", " + y);
            return;
        }
        bool isDebugCell = (x == 0 && y == 11);
        if (isDebugCell)
        {
            Debug.Log($"[DEBUG] Placing sea level water for position ({x}, {y})");
            Debug.Log($"[DEBUG] Cell: {cell.name}, Pre children count: {cell.gameObject.transform.childCount}");
            Debug.Log($"[DEBUG] Cell active: {cell.gameObject.activeInHierarchy}");

            if (cell.gameObject.transform.childCount > 0)
            {
                Debug.Log($"[DEBUG] First child: {cell.gameObject.transform.GetChild(0).name}");
            }
        }
        DestroyCellChildren(cell);
        if (isDebugCell)
        {
            Debug.Log($"[DEBUG] After destroying children, count: {cell.gameObject.transform.childCount}");
        }

        Vector2 worldPos = cell.transformPosition;
        GameObject seaLevelWaterObject = Instantiate(
            seaLevelWater,
            new Vector3(worldPos.x, worldPos.y, 0f),
            Quaternion.identity
        );

        if (seaLevelWaterObject == null)
        {
            Debug.Log("seaLevelWaterObject is null for position " + x + ", " + y);
            return;
        }

        if (isDebugCell)
        {
            Debug.Log($"[DEBUG] Instantiated object: {seaLevelWaterObject.name}");
            Debug.Log($"[DEBUG] Object active: {seaLevelWaterObject.activeInHierarchy}");
            Debug.Log($"[DEBUG] Object position: {seaLevelWaterObject.transform.position}");
        }

        // Set parent with explicit world position staying
        Transform parentTransform = cell.gameObject.transform;
        Transform childTransform = seaLevelWaterObject.transform;

        // Store world position before parenting
        Vector3 worldPosition = childTransform.position;

        // Set parent
        childTransform.SetParent(parentTransform, false); // worldPositionStays = false for local positioning

        // Restore position if needed (adjust based on your coordinate system needs)
        childTransform.position = worldPosition;

        if (isDebugCell)
        {
            Debug.Log($"[DEBUG] After SetParent - Cell children count: {parentTransform.childCount}");
            Debug.Log($"[DEBUG] Child transform parent: {(childTransform.parent != null ? childTransform.parent.name : "NULL")}");
            Debug.Log($"[DEBUG] Child active after parenting: {seaLevelWaterObject.activeInHierarchy}");

            // Verify the child is actually there
            for (int i = 0; i < parentTransform.childCount; i++)
            {
                Debug.Log($"[DEBUG] Child {i}: {parentTransform.GetChild(i).name}");
            }
        }

        // Update cell properties
        cell.zoneType = Zone.ZoneType.Water;
        gridManager.SetCellHeight(new Vector2(x, y), 0);

        // Set sorting order
        gridManager.SetTileSortingOrder(seaLevelWaterObject);

        if (isDebugCell)
        {
            Debug.Log($"[DEBUG] Final cell children count: {cell.gameObject.transform.childCount}");
            Debug.Log($"[DEBUG] Water object still exists: {seaLevelWaterObject != null}");
        }
    }

    private void PlaceWaterSlope(int x, int y, GameObject waterSlopePrefab)
    {
        Cell cell = gridManager.GetCell(x, y);

        DestroyCellChildren(cell);

        // modify the height of the cell to be 0
        gridManager.SetCellHeight(new Vector2(x, y), 0);
        Cell updatedCell = gridManager.GetCell(x, y);

        Vector2 worldPos = gridManager.GetWorldPosition(x, y);
        GameObject slope = Instantiate(
            waterSlopePrefab,
            worldPos,
            Quaternion.identity
        );

        int sortingOrder = gridManager.SetTileSortingOrder(slope.gameObject);
        if (x == 0 && y == 10)
        {
            Debug.Log("placing water slope for position " + x + ", " + y + " with new sorting order " + sortingOrder);
        }
    }

    private GameObject DetermineWaterSlopePrefab(int x, int y)
    {
        int heightAtNorth = heightMap.getHeightWithBorder(x + 1, y);
        int heightAtSouth = heightMap.getHeightWithBorder(x - 1, y);
        int heightAtWest = heightMap.getHeightWithBorder(x, y + 1);
        int heightAtEast = heightMap.getHeightWithBorder(x, y - 1);

        int heightAtNorthEast = heightMap.getHeightWithBorder(x + 1, y - 1);
        int heightAtNorthWest = heightMap.getHeightWithBorder(x + 1, y + 1);
        int heightAtSouthEast = heightMap.getHeightWithBorder(x - 1, y - 1);
        int heightAtSouthWest = heightMap.getHeightWithBorder(x - 1, y + 1);

        bool hasSeaLevelAtNorth = heightAtNorth == SEA_LEVEL;
        bool hasSeaLevelAtSouth = heightAtSouth == SEA_LEVEL;
        bool hasSeaLevelAtWest = heightAtWest == SEA_LEVEL;
        bool hasSeaLevelAtEast = heightAtEast == SEA_LEVEL;

        bool hasSeaLevelAtNorthEast = heightAtNorthEast == SEA_LEVEL;
        bool hasSeaLevelAtNorthWest = heightAtNorthWest == SEA_LEVEL;
        bool hasSeaLevelAtSouthEast = heightAtSouthEast == SEA_LEVEL;
        bool hasSeaLevelAtSouthWest = heightAtSouthWest == SEA_LEVEL;

        if (hasSeaLevelAtWest && heightAtSouth == -1) return westSlopeWaterPrefab;
        if (hasSeaLevelAtEast && heightAtSouth == -1) return eastSlopeWaterPrefab;
        if (hasSeaLevelAtNorth && heightAtSouth == -1) return northSlopeWaterPrefab;
        if (hasSeaLevelAtSouth && heightAtNorth == -1) return southSlopeWaterPrefab;
        if (hasSeaLevelAtWest && heightAtNorth == -1) return westSlopeWaterPrefab;
        if (hasSeaLevelAtEast && heightAtNorth == -1) return eastSlopeWaterPrefab;
        if (hasSeaLevelAtNorth && heightAtWest == -1) return northSlopeWaterPrefab;
        if (hasSeaLevelAtSouth && heightAtWest == -1) return southSlopeWaterPrefab;
        if (hasSeaLevelAtEast && heightAtWest == -1) return eastSlopeWaterPrefab;
        if (hasSeaLevelAtWest && heightAtEast == -1) return westSlopeWaterPrefab;


        if (hasSeaLevelAtEast)
        {
            if (!hasSeaLevelAtSouth)
            {
                if (!hasSeaLevelAtNorth)
                {
                    return eastSlopeWaterPrefab;
                }
                else
                {
                    return northEastSlopeWaterPrefab;
                }
            }
            else
            {
                if (!hasSeaLevelAtWest)
                {
                    return southEastSlopeWaterPrefab;
                }
                else
                {
                    return southEastUpslopeWaterPrefab;
                }
            }
        }

        if (hasSeaLevelAtWest)
        {
            if (!hasSeaLevelAtSouth)
            {
                if (!hasSeaLevelAtNorth)
                {
                    return westSlopeWaterPrefab;
                }
                else
                {
                    return northWestSlopeWaterPrefab;
                }
            }
            else
            {
                if (!hasSeaLevelAtNorth)
                {
                    return southWestSlopeWaterPrefab;
                }
                else
                {
                    return southWestUpslopeWaterPrefab;
                }
            }
        }

        if (hasSeaLevelAtNorth)
        {
            if (!hasSeaLevelAtSouth)
            {
                if (!hasSeaLevelAtNorth)
                {
                    return northSlopeWaterPrefab;
                }
                else
                {
                    return northSlopeWaterPrefab;
                }
            }
            else
            {
                if (!hasSeaLevelAtWest)
                {
                    return southSlopeWaterPrefab;
                }
                else
                {
                    return southSlopeWaterPrefab;
                }
            }
        }

        if (hasSeaLevelAtSouth)
        {
            if (!hasSeaLevelAtNorth)
            {
                if (!hasSeaLevelAtWest)
                {
                    return southSlopeWaterPrefab;
                }
                else
                {
                    return northSlopeWaterPrefab;
                }
            }
            else
            {
                if (!hasSeaLevelAtWest)
                {
                    return southSlopeWaterPrefab;
                }
                else
                {
                    return southSlopeWaterPrefab;
                }
            }
        }

        if (hasSeaLevelAtNorthEast)
        {
            if (!hasSeaLevelAtSouth)
            {
                return northEastUpslopeWaterPrefab;
            }
        }

        if (hasSeaLevelAtNorthWest)
        {
            if (!hasSeaLevelAtSouth)
            {
                return northWestUpslopeWaterPrefab;
            }
        }

        if (hasSeaLevelAtSouthEast)
        {
            if (!hasSeaLevelAtNorth)
            {
                return southEastUpslopeWaterPrefab;
            }
        }

        if (hasSeaLevelAtSouthWest)
        {
            if (!hasSeaLevelAtNorth)
            {
                return southWestUpslopeWaterPrefab;
            }
        }

        return null;
    }

    /// <summary>
    /// Calculate sorting order for terrain tiles
    /// </summary>
    /// <param name="x">Grid X coordinate</param>
    /// <param name="y">Grid Y coordinate</param>
    /// <param name="height">Terrain height</param>
    /// <returns>Sorting order value</returns>
    public int CalculateTerrainSortingOrder(int x, int y, int height)
    {
        // In isometric view, objects further back (higher x+y) should render first (lower sorting order)
        int isometricDepth = x + y;
        int depthOrder = -isometricDepth * DEPTH_MULTIPLIER;

        // Higher terrain should render on top of lower terrain at same depth
        // int heightOrder = height * HEIGHT_MULTIPLIER;

        return TERRAIN_BASE_ORDER + depthOrder;
    }

    /// <summary>
    /// Calculate sorting order for slope tiles (slightly behind terrain)
    /// </summary>
    /// <param name="x">Grid X coordinate</param>
    /// <param name="y">Grid Y coordinate</param>
    /// <param name="height">Terrain height</param>
    /// <returns>Sorting order value</returns>
    public int CalculateSlopeSortingOrder(int x, int y, int height)
    {
        return CalculateTerrainSortingOrder(x, y, height) + SLOPE_OFFSET;
    }

    /// <summary>
    /// Calculate sorting order for buildings (should be above terrain)
    /// Call this method from your building placement code
    /// </summary>
    /// <param name="x">Grid X coordinate</param>
    /// <param name="y">Grid Y coordinate</param>
    /// <param name="height">Terrain height at building location</param>
    /// <returns>Sorting order value</returns>
    public int CalculateBuildingSortingOrder(int x, int y, int height)
    {
        return CalculateTerrainSortingOrder(x, y, height) + BUILDING_OFFSET;
    }

    /// <summary>
    /// Calculate sorting order for any object type at given position
    /// </summary>
    /// <param name="x">Grid X coordinate</param>
    /// <param name="y">Grid Y coordinate</param>
    /// <param name="objectType">Type of object (terrain, building, etc.)</param>
    /// <returns>Sorting order value</returns>
    public int CalculateSortingOrder(int x, int y, ObjectType objectType)
    {
        int height = heightMap.GetHeight(x, y);

        switch (objectType)
        {
            case ObjectType.Terrain:
                return CalculateTerrainSortingOrder(x, y, height);
            case ObjectType.Slope:
                return CalculateSlopeSortingOrder(x, y, height);
            case ObjectType.Building:
                return CalculateBuildingSortingOrder(x, y, height);
            case ObjectType.Road:
                return CalculateTerrainSortingOrder(x, y, height) + 5; // Roads slightly above terrain
            case ObjectType.Utility:
                return CalculateTerrainSortingOrder(x, y, height) + 8; // Utilities above roads
            default:
                return CalculateTerrainSortingOrder(x, y, height);
        }
    }

    // Enum for different object types (add this to help with sorting)
    public enum ObjectType
    {
        Terrain,
        Slope,
        Road,
        Utility,
        Building,
        Effect
    }

    private (string key, GameObject prefab)? DetermineSlopePrefab(int x, int y)
    {
        int currentHeight = heightMap.GetHeight(x, y);

        int northHeight = heightMap.GetHeight(x + 1, y);
        int southHeight = heightMap.GetHeight(x - 1, y);
        int westHeight = heightMap.GetHeight(x, y + 1);
        int eastHeight = heightMap.GetHeight(x, y - 1);

        int neHeight = heightMap.GetHeight(x + 1, y - 1);
        int nwHeight = heightMap.GetHeight(x + 1, y + 1);
        int swHeight = heightMap.GetHeight(x - 1, y + 1);
        int seHeight = heightMap.GetHeight(x - 1, y - 1);

        bool hasNorthSlope = northHeight > currentHeight;
        bool hasSouthSlope = southHeight > currentHeight;
        bool hasEastSlope = eastHeight > currentHeight;
        bool hasWestSlope = westHeight > currentHeight;

        bool hasNorthEastSlope = neHeight > currentHeight;
        bool hasNorthWestSlope = nwHeight > currentHeight;
        bool hasSouthEastSlope = seHeight > currentHeight;
        bool hasSouthWestSlope = swHeight > currentHeight;

        if (hasWestSlope && hasNorthSlope) return ("southEast", southEastUpslopePrefab);
        if (hasWestSlope && hasSouthSlope) return ("northEast", northEastUpslopePrefab);
        if (hasEastSlope && hasNorthSlope) return ("southWest", southWestUpslopePrefab);
        if (hasEastSlope && hasSouthSlope) return ("northWest", northWestUpslopePrefab);

        if (hasNorthSlope) return ("south", southSlopePrefab);
        if (hasSouthSlope) return ("north", northSlopePrefab);
        if (hasEastSlope) return ("west", westSlopePrefab);
        if (hasWestSlope) return ("east", eastSlopePrefab);

        if (hasNorthWestSlope) return ("southEast", southEastSlopePrefab);
        if (hasNorthEastSlope) return ("southWest", southWestSlopePrefab);
        if (hasSouthWestSlope) return ("northEast", northEastSlopePrefab);
        if (hasSouthEastSlope) return ("northWest", northWestSlopePrefab);

        return null;
    }

    // These methods will be implemented later
    public void ModifyTerrain(int x, int y, int newHeight)
    {
        // Implementation for terrain modification
    }

    public bool CanPlaceBuildingInTerrain(Vector2 gridPosition, int size)
    {
        // Check if all tiles in the building footprint are at the same height
        int baseHeight = heightMap.GetHeight((int)gridPosition.x, (int)gridPosition.y);

        for (int dx = 0; dx < size; dx++)
        {
            for (int dy = 0; dy < size; dy++)
            {
                int checkX = (int)gridPosition.x + dx - size / 2;
                int checkY = (int)gridPosition.y + dy - size / 2;

                if (!heightMap.IsValidPosition(checkX, checkY))
                    return false;

                if (heightMap.GetHeight(checkX, checkY) != baseHeight)
                    return false;

                if (RequiresSlope(checkX, checkY, baseHeight))
                    return false;

                // Check that no water tiles exist on the building footprint
                if (waterManager != null && waterManager.IsWaterAt(checkX, checkY))
                    return false;
            }
        }

        return true;
    }

    public bool CanPlaceRoad(int x, int y)
    {
        // Implementation for road placement validation
        return true; // Temporary - roads can be placed anywhere for now
    }

    void OnDrawGizmos()
    {
        if (heightMap == null) return;

        for (int x = 0; x < gridManager.width; x++)
        {
            for (int y = 0; y < gridManager.height; y++)
            {
                Vector3 pos = gridManager.GetWorldPosition(x, y);
                float height = heightMap.GetHeight(x, y);
                Gizmos.color = Color.Lerp(Color.blue, Color.red, height / 20f);
                Gizmos.DrawWireCube(pos, new Vector3(0.5f, 0.5f, 0.1f));
            }
        }
    }
}
