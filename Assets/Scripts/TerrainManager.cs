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

    public const int MIN_HEIGHT = 0;
    public const int MAX_HEIGHT = 20;
    public const int SEA_LEVEL = 0;

    public void StartTerrainGeneration()
    {
        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridManager>();
        }

        heightMap = new HeightMap(gridManager.width, gridManager.height);
        LoadInitialHeightMap();
        ApplyHeightMapToGrid();
        // Initialize water map after height map
        if (waterManager != null)
        {
            waterManager.InitializeWaterMap();
        }
    }

    public HeightMap GetHeightMap()
    {
        return heightMap;
    }

    private void LoadInitialHeightMap()
    {
        int[,] initialHeights = new int[,] {
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 1},
          {1, 1, 2, 3, 3, 3, 2, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 1},
          {1, 1, 2, 3, 3, 3, 2, 1, 1, 1, 1, 1, 1, 2, 2, 1, 1, 1, 1, 1},
          {1, 1, 2, 3, 3, 3, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 2},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2},
          {1, 1, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 3, 2, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
        };
        heightMap.SetHeights(initialHeights);
    }

    private void ApplyHeightMapToGrid()
    {
        // Process from top-right to bottom-left for proper isometric layering
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
        int currentHeight = heightMap.GetHeight(x, y);
        Cell cell = gridManager.GetCell(x, y);
        cell.height = currentHeight;

        if (RequiresSlope(x, y))
        {
            PlaceSlope(x, y);
        }

        if (currentHeight > 1) // If it's flat but elevated
        {
            UpdateElevatedTilePosition(x, y, currentHeight);
        }
    }

    private void UpdateElevatedTilePosition(int x, int y, int height)
    {
        Cell cell = gridManager.GetCell(x, y);
        cell.height = height;
        Vector2 worldPos = gridManager.GetWorldPosition(x, y);

        Transform existingTile = cell.gameObject.transform.GetChild(0);
        existingTile.position = new Vector3(worldPos.x, worldPos.y, 0);
        
        SpriteRenderer sr = existingTile.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // Give more weight to height in sorting order
            int baseOrder = -(y * 10 + x) - 100;
            int heightOrder = height * 1000; // Increased multiplier for height
            sr.sortingOrder = baseOrder + heightOrder; // Changed to addition to bring higher tiles to front
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

    private bool RequiresSlope(int x, int y)
    {
        int currentHeight = heightMap.GetHeight(x, y);
        
        // Check all 8 surrounding tiles
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
        GameObject slopePrefab = DetermineSlopePrefab(x, y);
        
        if (slopePrefab != null)
        {
            Cell cell = gridManager.GetCell(x, y);
            DestroyCellChildren(cell);
            
            Vector2 worldPos = gridManager.GetWorldPosition(x, y);
            GameObject slope = Instantiate(
                slopePrefab,
                worldPos,
                Quaternion.identity
            );
            slope.transform.SetParent(cell.gameObject.transform);
            
            SpriteRenderer sr = slope.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                int baseOrder = -(y * 10 + x) - 100;
                int heightOrder = currentHeight * 1000;
                sr.sortingOrder = baseOrder + heightOrder - 1; // Slopes should be slightly behind tiles of same height
            }
        }
    }

    private int CalculateIsometricSortingOrder(int x, int y, int height)
    {
        // Higher (x + y) means further back in isometric view
        int isometricDepth = x + y;
        // Base sorting order determined by isometric depth
        int baseOrder = -isometricDepth * 100;
        // Height adjustment (higher tiles should appear on top)
        int heightAdjustment = height * 1000;  // Multiply by larger number to ensure height takes precedence
        
        return baseOrder - heightAdjustment;
    }

    private GameObject DetermineSlopePrefab(int x, int y)
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

        if (hasWestSlope && hasNorthSlope) return southEastUpslopePrefab;
        if (hasWestSlope && hasSouthSlope) return northEastUpslopePrefab;
        if (hasEastSlope && hasNorthSlope) return southWestUpslopePrefab;
        if (hasEastSlope && hasSouthSlope) return northWestUpslopePrefab;

        if (hasNorthSlope) return southSlopePrefab;
        if (hasSouthSlope) return northSlopePrefab;
        if (hasEastSlope) return westSlopePrefab;
        if (hasWestSlope) return eastSlopePrefab;

        if (hasNorthWestSlope) return southEastSlopePrefab;
        if (hasNorthEastSlope) return southWestSlopePrefab;
        if (hasSouthWestSlope) return northEastSlopePrefab;
        if (hasSouthEastSlope) return northWestSlopePrefab;

        return null;
    }

    // These methods will be implemented later
    public void ModifyTerrain(int x, int y, int newHeight)
    {
        // Implementation for terrain modification
    }

    public bool CanPlaceBuilding(int x, int y, int size)
    {
        // Check if all tiles in the building footprint are at the same height
        int baseHeight = heightMap.GetHeight(x, y);
        
        for (int dx = 0; dx < size; dx++)
        {
            for (int dy = 0; dy < size; dy++)
            {
                int checkX = x + dx - size/2;
                int checkY = y + dy - size/2;
                
                if (!heightMap.IsValidPosition(checkX, checkY))
                    return false;
                    
                if (heightMap.GetHeight(checkX, checkY) != baseHeight)
                    return false;
                    
                if (RequiresSlope(checkX, checkY))
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
