using UnityEngine;
using System.Collections.Generic;

public class WaterManager : MonoBehaviour
{
    public GridManager gridManager;
    public TerrainManager terrainManager;
    public ZoneManager zoneManager;
    
    public List<GameObject> waterTilePrefabs; // Water tile prefabs with animation
    
    public int seaLevel = 0; // Height at or below which water will be placed
    private WaterMap waterMap;
    
    private List<WaterPlant> waterPlants = new List<WaterPlant>();
    private int cityWaterConsumption;
    private int cityWaterOutput;
    
    // Define the initial water cells matrix (true = water, false = no water)
    private bool[,] initialWaterCells = new bool[,] {
        {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
        {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
        {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
        {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
        {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
        {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
        {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
        {false, false, false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, false, false},
        {false, false, false, false, false, true, true, true, true, true, false, false, false, false, false, false, false, false, false, false},
        {false, false, false, false, true, true, true, true, true, true, true, false, false, false, false, false, false, false, false, false},
        {false, false, false, false, false, true, true, true, true, true, false, false, false, false, false, false, false, false, false, false},
        {false, false, false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false, false, false},
        {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
        {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
        {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
        {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
        {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
        {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
        {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
        {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false}
    };
    
    void Start()
    {
        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridManager>();
        }
        
        cityWaterConsumption = 0;
        cityWaterOutput = 0;
    }
    
    public void InitializeWaterMap()
    {
        if (gridManager != null)
        {
            waterMap = new WaterMap(gridManager.width, gridManager.height);
            // First apply predefined water cells from the matrix
            InitializeWaterBodiesFromMatrix();
            
            // Then optionally apply water based on terrain height
            // You can comment this out if you only want to use the predefined matrix
            if (terrainManager != null)
            {
                waterMap.InitializeWaterBodiesBasedOnHeight(terrainManager.GetHeightMap(), seaLevel);
            }
            
            // Apply the water visuals to the grid
            UpdateWaterVisuals();
        }
    }
    
    private void InitializeWaterBodiesFromMatrix()
    {
        if (waterMap == null || initialWaterCells == null)
        {
            return;
        }
            
        int width = Mathf.Min(gridManager.width, initialWaterCells.GetLength(0));
        int height = Mathf.Min(gridManager.height, initialWaterCells.GetLength(1));

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (initialWaterCells[x, y])
                {
                    waterMap.SetWater(x, y, true);
                }
            }
        }
    }
    
    public bool IsWaterAt(int x, int y)
    {
        if (waterMap == null) return false;
        return waterMap.IsWater(x, y);
    }
    
    public void PlaceWater(int x, int y)
    {
        if (waterMap == null) {
          return;
        }
        
        if (!waterMap.IsValidPosition(x, y))
        {
            return;
        }
        
        // Update the grid cell to display water
        GameObject cell = gridManager.gridArray[x, y];

        Cell cellComponent = cell.GetComponent<Cell>();
        // Destroy existing children
        foreach (Transform child in cell.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
        
        // Update the cell's zone type
        cellComponent.zoneType = Zone.ZoneType.Water;
        
        // Place water tile
        GameObject waterPrefab = GetRandomWaterPrefab();

        if (waterPrefab == null) return;
        
        Vector2 worldPos = gridManager.GetWorldPosition(x, y);
        
        GameObject waterTile = GameObject.Instantiate(
            waterPrefab,
            worldPos,
            Quaternion.identity
        );
        
        // Set parent
        waterTile.transform.SetParent(cell.transform);
        
        // Set up animation
        // Animator animator = waterTile.GetComponent<Animator>();
        // if (animator != null)
        // {
        //     AnimatorManager animatorManager = FindObjectOfType<AnimatorManager>();
        //     if (animatorManager != null)
        //     {
        //         animatorManager.RegisterAnimator(animator);
        //     }
        // }
        
        // Configure zone properties
        Zone zone = waterTile.AddComponent<Zone>();
        zone.zoneType = Zone.ZoneType.Water;
        zone.zoneCategory = Zone.ZoneCategory.Water;
        
        // Set sorting order
        int sortingOrder = gridManager.SetTileSortingOrder(waterTile, Zone.ZoneType.Water);
        cellComponent.sortingOrder = sortingOrder;
    }
    
    // Rest of the existing WaterManager methods remain the same
    public void RemoveWater(int x, int y)
    {
        if (waterMap == null) return;
        
        if (!waterMap.IsValidPosition(x, y))
            return;
            
        if (!IsWaterAt(x, y))
            return;
            
        // Set the water map cell to no water
        waterMap.SetWater(x, y, false);
        
        // Update the grid cell to display grass
        GameObject cell = gridManager.gridArray[x, y];
        Cell cellComponent = cell.GetComponent<Cell>();
        
        // Destroy existing children
        foreach (Transform child in cell.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
        
        // Update the cell's zone type to grass
        cellComponent.zoneType = Zone.ZoneType.Grass;
        
        // Place grass tile
        GameObject grassPrefab = zoneManager.GetRandomZonePrefab(Zone.ZoneType.Grass);
        Vector2 worldPos = gridManager.GetWorldPosition(x, y);
        
        GameObject grassTile = GameObject.Instantiate(
            grassPrefab,
            worldPos,
            Quaternion.identity
        );
        
        // Set parent
        grassTile.transform.SetParent(cell.transform);
        
        // Configure zone properties
        Zone zone = grassTile.AddComponent<Zone>();
        zone.zoneType = Zone.ZoneType.Grass;
        zone.zoneCategory = Zone.ZoneCategory.Grass;
        
        // Set sorting order
        int sortingOrder = gridManager.SetTileSortingOrder(grassTile, Zone.ZoneType.Grass);
        cellComponent.sortingOrder = sortingOrder;
    }
    
    public void UpdateWaterVisuals()
    {
        if (waterMap == null || gridManager == null) return;
        
        // Update all water cells in the grid
        for (int x = 0; x < gridManager.width; x++)
        {
            for (int y = 0; y < gridManager.height; y++)
            {
               if (waterMap.IsWater(x, y))
                {
                    PlaceWater(x, y);
                }
            }
        }
    }
    
    public GameObject GetRandomWaterPrefab()
    {
        if (waterTilePrefabs == null || waterTilePrefabs.Count == 0)
            return null;
            
        return waterTilePrefabs[Random.Range(0, waterTilePrefabs.Count)];
    }
    
    public void RegisterWaterPlant(WaterPlant waterPlant)
    {
        waterPlants.Add(waterPlant);
        
        int totalWaterOutput = 0;
        foreach (var plant in waterPlants)
        {
            totalWaterOutput += plant.WaterOutput;
        }
        
        cityWaterOutput = totalWaterOutput;
    }
    
    public void UnregisterWaterPlant(WaterPlant waterPlant)
    {
        waterPlants.Remove(waterPlant);
        
        int totalWaterOutput = 0;
        foreach (var plant in waterPlants)
        {
            totalWaterOutput += plant.WaterOutput;
        }
        
        cityWaterOutput = totalWaterOutput;
    }
    
    public void ResetWaterPlants()
    {
        waterPlants.Clear();
        cityWaterOutput = 0;
    }
    
    public int GetTotalWaterOutput()
    {
        return cityWaterOutput;
    }
    
    public void AddWaterConsumption(int value)
    {
        cityWaterConsumption += value;
    }
    
    public void RemoveWaterConsumption(int value)
    {
        cityWaterConsumption -= value;
    }
    
    public int GetTotalWaterConsumption()
    {
        return cityWaterConsumption;
    }
    
    public bool GetCityWaterAvailability()
    {
        return cityWaterOutput > cityWaterConsumption;
    }
    
    public bool IsAdjacentToWater(int x, int y)
    {
        if (waterMap == null) return false;
        
        // Check if any adjacent cell has water
        int[] dx = { -1, 0, 1, 0 };
        int[] dy = { 0, 1, 0, -1 };
        
        for (int i = 0; i < 4; i++)
        {
            int nx = x + dx[i];
            int ny = y + dy[i];
            
            if (waterMap.IsValidPosition(nx, ny) && waterMap.IsWater(nx, ny))
            {
                return true;
            }
        }
        
        return false;
    }
    
    public WaterMap GetWaterMap()
    {
        return waterMap;
    }
}
