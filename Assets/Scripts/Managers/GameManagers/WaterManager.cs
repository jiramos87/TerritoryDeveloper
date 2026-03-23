using UnityEngine;
using System.Collections.Generic;
using Territory.Core;
using Territory.Zones;
using Territory.Buildings;
using Territory.Persistence;

namespace Territory.Terrain
{
/// <summary>
/// Generates and manages water bodies on the grid. Lakes use depression-fill on the height map (FEAT-37a);
/// sea-level terrain cells are merged into <see cref="WaterMap"/> after fill so they match <c>PlaceSeaLevelWater</c>.
/// Save serializes <see cref="WaterMap.GetSerializableData"/> (FEAT-37c); load uses <see cref="RestoreWaterMapFromSaveData"/>.
/// Legacy sea-level threshold remains for paint tool / old save restore. Coordinates with GridManager,
/// TerrainManager, and ZoneManager. <see cref="LakeFillSettings"/> are created in code (not Inspector) until terrain UI exists.
/// </summary>
public class WaterManager : MonoBehaviour
{
    public GridManager gridManager;
    public TerrainManager terrainManager;
    public ZoneManager zoneManager;

    public List<GameObject> waterTilePrefabs; // Water tile prefabs with animation

    [Tooltip("When true, procedural lakes come from depression-fill on the height map (multi-level surfaces). When false, any cell with terrain height <= seaLevel is water (legacy).")]
    public bool useLakeDepressionFill = true;

    /// <summary>Lake depression-fill parameters — not serialized; tune defaults in <see cref="LakeFillSettings"/> (terrain generator UI will expose these later).</summary>
    private LakeFillSettings lakeFillSettings;

    /// <summary>Read-only access for terrain feasibility and diagnostics.</summary>
    public LakeFillSettings LakeFillSettings => lakeFillSettings;

    [Tooltip("Legacy: height at or below which water is placed when useLakeDepressionFill is false; also used for painted water and restore from old saves.")]
    public int seaLevel = 0;

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
        {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
        {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
        {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
        {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
        {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
        {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
        {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
        {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
        {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
        {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
        {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
        {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false},
        {false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false}
    };

    void Awake()
    {
        lakeFillSettings = new LakeFillSettings();
        const int maxLakeBboxPerAxisCap = 10;
        lakeFillSettings.MaxLakeBoundingExtent = Mathf.Min(lakeFillSettings.MaxLakeBoundingExtent, maxLakeBboxPerAxisCap);
        lakeFillSettings.MaxLakeBoundingExtent = Mathf.Max(lakeFillSettings.MaxLakeBoundingExtent, lakeFillSettings.MinLakeBoundingExtent);
    }

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

            if (terrainManager == null)
                terrainManager = FindObjectOfType<TerrainManager>();
            if (terrainManager != null && terrainManager.GetHeightMap() != null)
            {
                if (useLakeDepressionFill)
                {
                    MapGenerationSeed.EnsureSessionMasterSeed();
                    lakeFillSettings.RandomSeed = MapGenerationSeed.GetLakeFillRandomSeed();
                    waterMap.InitializeLakesFromDepressionFill(terrainManager.GetHeightMap(), lakeFillSettings, seaLevel);
                }
                else
                    waterMap.InitializeWaterBodiesBasedOnHeight(terrainManager.GetHeightMap(), seaLevel);
                // Sea-level terrain may already use PlaceSeaLevelWater without WaterMap entries when using lake fill.
                waterMap.MergeSeaLevelDryCellsFromHeightMap(terrainManager.GetHeightMap(), seaLevel);

                if (useLakeDepressionFill && TryGetLakeTerrainRefreshRegion(waterMap, gridManager.width, gridManager.height,
                        out int rMinX, out int rMinY, out int rMaxX, out int rMaxY))
                {
                    terrainManager.ApplyHeightMapToRegion(rMinX, rMinY, rMaxX, rMaxY);
                }
            }
            else
            {
                InitializeWaterBodiesFromMatrix();
            }

            UpdateWaterVisuals();

            if (terrainManager != null && useLakeDepressionFill)
                terrainManager.RefreshLakeShoreAfterLakePlacement(this);
        }
    }

    /// <summary>
    /// Builds the inclusive rect for <see cref="TerrainManager.ApplyHeightMapToRegion"/> after lake fill: union of
    /// all water cells (with margin for shore/cliffs) and the artificial carve dirty rect when present.
    /// Matches the fallback path so procedural lakes get the same terrain + cliff refresh as carved rectangles.
    /// </summary>
    static bool TryGetLakeTerrainRefreshRegion(WaterMap wm, int gridWidth, int gridHeight, out int minX, out int minY, out int maxX, out int maxY)
    {
        const int waterMargin = 3;
        minX = minY = maxX = maxY = 0;
        bool has = false;
        if (wm.TryGetAllWaterBoundingBox(out int wx0, out int wy0, out int wx1, out int wy1))
        {
            minX = wx0 - waterMargin;
            minY = wy0 - waterMargin;
            maxX = wx1 + waterMargin;
            maxY = wy1 + waterMargin;
            has = true;
        }
        if (wm.ArtificialDirtyMinX >= 0)
        {
            int ax0 = wm.ArtificialDirtyMinX;
            int ay0 = wm.ArtificialDirtyMinY;
            int ax1 = wm.ArtificialDirtyMaxX;
            int ay1 = wm.ArtificialDirtyMaxY;
            if (!has)
            {
                minX = ax0;
                minY = ay0;
                maxX = ax1;
                maxY = ay1;
                has = true;
            }
            else
            {
                minX = Mathf.Min(minX, ax0);
                minY = Mathf.Min(minY, ay0);
                maxX = Mathf.Max(maxX, ax1);
                maxY = Mathf.Max(maxY, ay1);
            }
        }
        if (!has)
            return false;
        minX = Mathf.Clamp(minX, 0, gridWidth - 1);
        maxX = Mathf.Clamp(maxX, 0, gridWidth - 1);
        minY = Mathf.Clamp(minY, 0, gridHeight - 1);
        maxY = Mathf.Clamp(maxY, 0, gridHeight - 1);
        return true;
    }

    /// <summary>
    /// Restores WaterMap from serialized save data, or best-effort from legacy CellData when <paramref name="data"/> is missing.
    /// Call after RestoreHeightMapFromGridData and before RestoreGrid.
    /// </summary>
    public void RestoreWaterMapFromSaveData(WaterMapData data, int gridWidth, int gridHeight, List<CellData> gridData)
    {
        if (gridManager == null) return;

        waterMap = new WaterMap(gridWidth, gridHeight);

        if (data != null && data.waterBodyIds != null && data.waterBodyIds.Length == gridWidth * gridHeight)
        {
            waterMap.LoadFromSerializableData(data);
        }
        else if (gridData != null)
        {
            waterMap.RestoreFromLegacyCellData(gridData, seaLevel);
        }
    }

    /// <summary>
    /// Restores WaterMap from saved grid data only (legacy saves). Prefer <see cref="RestoreWaterMapFromSaveData"/>.
    /// </summary>
    public void RestoreWaterMapFromGridData(List<CellData> gridData)
    {
        if (gridManager == null || gridData == null) return;

        if (waterMap == null || waterMap.IsValidPosition(0, 0) == false)
            waterMap = new WaterMap(gridManager.width, gridManager.height);

        waterMap.RestoreFromLegacyCellData(gridData, seaLevel);
    }

    /// <summary>
    /// Resolves a water tile prefab by saved prefab name for load restore. Falls back to random when not found.
    /// </summary>
    public GameObject FindWaterPrefabByName(string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName) || waterTilePrefabs == null)
            return GetRandomWaterPrefab();
        string trimmed = prefabName.Replace("(Clone)", "").Trim();
        foreach (GameObject p in waterTilePrefabs)
        {
            if (p != null && p.name == trimmed)
                return p;
        }
        return GetRandomWaterPrefab();
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
                    waterMap.AddLegacyPaintedWaterCell(x, y, seaLevel);
                }
            }
        }
    }

    public bool IsWaterAt(int x, int y)
    {
        if (waterMap == null) return false;
        return waterMap.IsWater(x, y);
    }

    /// <summary>
    /// Registers a sea-level cell in <see cref="WaterMap"/> when terrain placed water without going through lake fill (e.g. <c>PlaceSeaLevelWater</c> at runtime).
    /// </summary>
    public void TryRegisterSeaLevelWaterCell(int x, int y)
    {
        if (waterMap == null || terrainManager == null || terrainManager.GetHeightMap() == null)
            return;
        if (!waterMap.IsValidPosition(x, y))
            return;
        if (terrainManager.GetHeightMap().GetHeight(x, y) > seaLevel)
            return;
        if (waterMap.IsWater(x, y))
            return;
        waterMap.AddLegacyPaintedWaterCell(x, y, seaLevel);
    }

    /// <summary>Returns -1 if the cell is not water.</summary>
    public int GetWaterSurfaceHeight(int x, int y)
    {
        if (waterMap == null) return -1;
        return waterMap.GetSurfaceHeightAt(x, y);
    }

    public void PlaceWater(int x, int y)
    {
        if (waterMap == null)
        {
            return;
        }

        if (!waterMap.IsValidPosition(x, y))
        {
            return;
        }

        if (!waterMap.IsWater(x, y))
            waterMap.AddLegacyPaintedWaterCell(x, y, seaLevel);

        int surfaceHeight = waterMap.GetSurfaceHeightAt(x, y);
        if (surfaceHeight < 0)
            surfaceHeight = seaLevel;

        // Logical surface height in WaterMap is spill (fill level). World placement uses one step lower (Option A / FEAT-37).
        int visualSurfaceHeight = Mathf.Max(TerrainManager.MIN_HEIGHT, surfaceHeight - 1);

        int terrainHeight = seaLevel;
        if (terrainManager != null && terrainManager.GetHeightMap() != null)
            terrainHeight = terrainManager.GetHeightMap().GetHeight(x, y);

        // Update the grid cell to display water
        GameObject cell = gridManager.gridArray[x, y];
        Cell cellComponent = gridManager.GetCell(x, y);
        if (cellComponent == null)
            return;

        // Terrain already placed sea-level water; do not replace with animated lake prefabs. Sync inspector fields from the existing child.
        if (terrainHeight <= seaLevel && cellComponent.zoneType == Zone.ZoneType.Water && cell.transform.childCount > 0)
        {
            cellComponent.waterBodyType = WaterBodyType.Sea;
            Transform first = cell.transform.GetChild(0);
            if (first != null)
            {
                string n = first.name.Replace("(Clone)", "").Trim();
                if (!string.IsNullOrEmpty(n))
                {
                    cellComponent.prefabName = n;
                    cellComponent.buildingType = n;
                }
            }
            return;
        }

        // Destroy existing children
        foreach (Transform child in cell.transform)
        {
            GameObject.Destroy(child.gameObject);
        }

        // Cell height follows terrain floor (HeightMap); water surface is used for the water tile and sorting.
        cellComponent.zoneType = Zone.ZoneType.Water;
        gridManager.SetCellHeight(new Vector2(x, y), terrainHeight);
        Vector2 cellWorldPos = gridManager.GetWorldPositionVector(x, y, terrainHeight);
        cell.transform.position = cellWorldPos;
        cellComponent.transformPosition = cellWorldPos;

        Vector2 waterSurfaceWorld = gridManager.GetWorldPositionVector(x, y, visualSurfaceHeight);
        float halfCellHeight = gridManager.tileHeight * 0.25f;
        Vector2 waterTileWorldPos = waterSurfaceWorld + new Vector2(0f, halfCellHeight);

        // Place water tile
        GameObject waterPrefab = GetRandomWaterPrefab();

        if (waterPrefab == null)
            return;

        GameObject waterTile = GameObject.Instantiate(
            waterPrefab,
            waterTileWorldPos,
            Quaternion.identity
        );
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

        waterTile.transform.SetParent(cell.transform);
        // Sorting matches visual water height (one step below logical surface).
        int sortingOrder = terrainManager != null
            ? terrainManager.CalculateTerrainSortingOrder(x, y, visualSurfaceHeight)
            : -(y * gridManager.width + x + 50000);
        SpriteRenderer sr = waterTile.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.sortingOrder = sortingOrder;
        cellComponent.SetCellInstanceSortingOrder(sortingOrder);

        cellComponent.prefabName = waterPrefab.name;
        cellComponent.buildingType = waterPrefab.name;
        cellComponent.buildingSize = 0;
        cellComponent.occupiedBuilding = null;
        cellComponent.secondaryPrefabName = "";

        cellComponent.waterBodyType = terrainHeight <= seaLevel ? WaterBodyType.Sea : WaterBodyType.Lake;
    }

    // Rest of the existing WaterManager methods remain the same
    public void RemoveWater(int x, int y)
    {
        if (waterMap == null) return;

        if (!waterMap.IsValidPosition(x, y))
            return;

        if (!IsWaterAt(x, y))
            return;

        waterMap.ClearWaterAt(x, y);

        // Update the grid cell to display grass
        GameObject cell = gridManager.gridArray[x, y];
        Cell cellComponent = gridManager.GetCell(x, y);

        // Destroy existing children
        foreach (Transform child in cell.transform)
        {
            GameObject.Destroy(child.gameObject);
        }

        // Update the cell's zone type to grass
        cellComponent.zoneType = Zone.ZoneType.Grass;
        cellComponent.waterBodyType = WaterBodyType.None;
        cellComponent.secondaryPrefabName = "";

        // Place grass tile
        GameObject grassPrefab = zoneManager.GetRandomZonePrefab(Zone.ZoneType.Grass);
        Vector2 worldPos = gridManager.GetWorldPosition(x, y);

        GameObject grassTile = GameObject.Instantiate(
            grassPrefab,
            worldPos,
            Quaternion.identity
        );

        // Configure zone properties
        Zone zone = grassTile.AddComponent<Zone>();
        zone.zoneType = Zone.ZoneType.Grass;
        zone.zoneCategory = Zone.ZoneCategory.Grass;

        // Set sorting order
        gridManager.SetTileSortingOrder(grassTile, Zone.ZoneType.Grass);
    }

    public void UpdateWaterVisuals()
    {
        if (waterMap == null || gridManager == null) return;

        for (int x = 0; x < gridManager.width; x++)
        {
            for (int y = 0; y < gridManager.height; y++)
            {
                if (waterMap.IsWater(x, y))
                    PlaceWater(x, y);
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
}
