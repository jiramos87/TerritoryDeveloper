using UnityEngine;
using System.Collections.Generic;
using Territory.Core;
using Territory.Zones;

namespace Territory.Terrain
{
public enum TerrainSlopeType
{
    Flat,
    North,
    South,
    East,
    West,
    NorthEast,
    NorthWest,
    SouthEast,
    SouthWest,
    NorthEastUp,
    NorthWestUp,
    SouthEastUp,
    SouthWestUp
}

/// <summary>
/// Generates and manages the terrain heightmap, slope types, and terrain tile prefab selection.
/// Determines slope direction for each cell based on neighbor heights and selects the appropriate
/// slope prefab (flat, N/S/E/W slopes, corner slopes, water slopes). Coordinates with GridManager
/// for cell height assignment and WaterManager for water-slope prefab variants.
/// </summary>
public class TerrainManager : MonoBehaviour, ITerrainManager
{
    #region Dependencies
    public GridManager gridManager;
    private HeightMap heightMap;
    public ZoneManager zoneManager;
    public WaterManager waterManager;
    #endregion

    #region Slope Prefabs
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
    #endregion

    #region Water Slope Prefabs
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
    public GameObject southCliffWallPrefab;
    public GameObject eastCliffWallPrefab;
    public GameObject northCliffWallPrefab;
    public GameObject westCliffWallPrefab;

    public GameObject northEastBayPrefab;
    public GameObject northWestBayPrefab;
    public GameObject southEastBayPrefab;
    public GameObject southWestBayPrefab;
    #endregion

    /// <summary>
    /// Finds a terrain prefab (slope, water slope, sea level water, bay) by name. Used when restoring saved games.
    /// </summary>
    public GameObject FindTerrainPrefabByName(string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName)) return null;
        string trimmed = prefabName.Replace("(Clone)", "");

        GameObject[] terrainPrefabs = new[]
        {
            northSlopePrefab, southSlopePrefab, eastSlopePrefab, westSlopePrefab,
            northEastSlopePrefab, northWestSlopePrefab, southEastSlopePrefab, southWestSlopePrefab,
            northEastUpslopePrefab, northWestUpslopePrefab, southEastUpslopePrefab, southWestUpslopePrefab,
            northSlopeWaterPrefab, southSlopeWaterPrefab, eastSlopeWaterPrefab, westSlopeWaterPrefab,
            northEastSlopeWaterPrefab, northWestSlopeWaterPrefab, southEastSlopeWaterPrefab, southWestSlopeWaterPrefab,
            northEastUpslopeWaterPrefab, northWestUpslopeWaterPrefab, southEastUpslopeWaterPrefab, southWestUpslopeWaterPrefab,
            seaLevelWaterPrefab, southCliffWallPrefab, eastCliffWallPrefab,
            northCliffWallPrefab, westCliffWallPrefab,
            northEastBayPrefab, northWestBayPrefab, southEastBayPrefab, southWestBayPrefab
        };

        foreach (GameObject prefab in terrainPrefabs)
        {
            if (prefab != null && prefab.name == trimmed)
                return prefab;
        }
        return null;
    }

    #region Configuration
    public const int MIN_HEIGHT = 0;
    public const int MAX_HEIGHT = 5;
    public const int SEA_LEVEL = 0;

    // Sorting order constants for different object types
    public const int TERRAIN_BASE_ORDER = 0;
    /// <summary>Offset for land slope sorting. 1 = slightly in front of terrain so slopes (especially east-facing) render correctly.</summary>
    public const int SLOPE_OFFSET = 1;
    public const int BUILDING_OFFSET = 10; // Buildings should be above terrain
    public const int EFFECT_OFFSET = 30; // Effects should be above terrain
    public const int DEPTH_MULTIPLIER = 100;
    public const int HEIGHT_MULTIPLIER = 10; // Must be < DEPTH_MULTIPLIER/MAX_HEIGHT so depth dominates (hilltops don't draw over foreground forest)
    #endregion

    #region Height Map Generation
    /// <summary>
    /// Initializes the heightmap and applies it to the grid, creating initial terrain elevations.
    /// </summary>
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

    /// <summary>
    /// Returns the current heightmap instance, or null if not yet initialized.
    /// </summary>
    /// <returns>The active HeightMap, or null.</returns>
    public HeightMap GetHeightMap()
    {
        return heightMap;
    }

    /// <summary>
    /// Returns the heightMap, creating or loading it if null. Call this before RestoreTerrainForCell
    /// and pass the result to RestoreTerrainForCell(x, y, map) so the same map is used.
    /// </summary>
    public HeightMap GetOrCreateHeightMap()
    {
        EnsureHeightMapLoaded();
        return heightMap;
    }

    /// <summary>
    /// Creates a fresh heightmap from the grid dimensions, loads initial height data, and applies it to the grid.
    /// </summary>
    public void InitializeHeightMap()
    {
        heightMap = new HeightMap(gridManager.width, gridManager.height);
        LoadInitialHeightMap();
        ApplyHeightMapToGrid();
    }

    /// <summary>
    /// Restores heightMap from saved grid data. Call before RestoreGrid so terrain/water systems use correct heights.
    /// </summary>
    public void RestoreHeightMapFromGridData(List<CellData> gridData)
    {
        if (gridData == null || gridManager == null) return;

        if (heightMap == null || heightMap.Width != gridManager.width || heightMap.Height != gridManager.height)
            heightMap = new HeightMap(gridManager.width, gridManager.height);

        foreach (CellData cellData in gridData)
        {
            if (heightMap.IsValidPosition(cellData.x, cellData.y))
                heightMap.SetHeight(cellData.x, cellData.y, cellData.height);
        }
    }

    /// <summary>
    /// Applies restored heightMap positions to all cell GameObjects. Call after RestoreHeightMapFromGridData
    /// and before RestoreGrid so buildings (e.g. water plant) are parented to correctly positioned cells.
    /// </summary>
    public void ApplyRestoredPositionsToGrid()
    {
        if (heightMap == null || gridManager == null) return;

        for (int x = 0; x < gridManager.width; x++)
        {
            for (int y = 0; y < gridManager.height; y++)
            {
                Cell cell = gridManager.GetCell(x, y);
                if (cell == null) continue;
                if (!heightMap.IsValidPosition(x, y)) continue;

                int h = heightMap.GetHeight(x, y);
                gridManager.SetCellHeight(new Vector2(x, y), h);
                Vector2 pos = gridManager.GetWorldPositionVector(x, y, h);
                cell.gameObject.transform.position = pos;
                cell.transformPosition = pos;
            }
        }
    }

    /// <summary>
    /// Ensures heightMap exists and has initial data (for RestoreTerrainForCell when init order skipped).
    /// Tries: 1) create from gridManager, 2) borrow from another TerrainManager in scene.
    /// Does not call ApplyHeightMapToGrid so the visible grid is not reset.
    /// Public so GridManager can call it on the same TM reference before RestoreTerrainForCell.
    /// </summary>
    public void EnsureHeightMapLoaded()
    {
        if (heightMap != null) return;

        if (gridManager == null)
            gridManager = FindObjectOfType<GridManager>();

        if (gridManager != null && gridManager.width > 0 && gridManager.height > 0)
        {
            Debug.Log($"[EnsureHeightMapLoaded] Creating HeightMap from grid (width={gridManager.width}, height={gridManager.height}).");
            heightMap = new HeightMap(gridManager.width, gridManager.height);
            LoadInitialHeightMap();
        }
        else
        {
            Debug.Log($"[EnsureHeightMapLoaded] Skipping HeightMap creation: gridManager null? {gridManager == null}, width={gridManager?.width ?? -1}, height={gridManager?.height ?? -1}.");
        }

        if (heightMap == null)
        {
            TerrainManager[] all = FindObjectsOfType<TerrainManager>();
            foreach (TerrainManager tm in all)
            {
                if (tm != this && tm.GetHeightMap() != null)
                {
                    heightMap = tm.GetHeightMap();
                    break;
                }
            }
        }

        if (heightMap == null)
            Debug.LogWarning($"[EnsureHeightMapLoaded] heightMap still null after load attempt. gridManager null? {gridManager == null}, width={gridManager?.width ?? -1}, height={gridManager?.height ?? -1}. Check init order (grid may not be ready when forest runs).");
    }

    private const int OriginalMapSize = 40;
    private const int TerrainGenSeed = 12345;
    private const float PerlinNoiseScale = 6f;
    private const int BorderBlendWidth = 10;

    /// <summary>Original 40x40 height map (rows y, cols x). Used as template for [0..39,0..39] when grid is larger.</summary>
    private static int[,] GetOriginal40x40Heights()
    {
        return new int[,] {
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 3, 4, 5, 5, 5, 4, 3, 2, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 3, 4, 5, 5, 5, 4, 3, 2, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2},
          {1, 1, 2, 2, 2, 2, 2, 1, 1, 1, 1, 0, 1, 1, 2, 1, 1, 1, 1, 1, 1, 2, 3, 4, 5, 5, 5, 4, 3, 2, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2},
          {1, 1, 2, 3, 3, 3, 2, 1, 1, 1, 1, 0, 1, 1, 2, 1, 1, 1, 1, 1, 1, 2, 2, 3, 4, 5, 4, 3, 3, 2, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2},
          {1, 1, 2, 3, 3, 3, 2, 1, 1, 1, 1, 0, 1, 2, 2, 1, 1, 1, 1, 1, 1, 2, 2, 3, 4, 5, 4, 3, 3, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2},
          {1, 1, 2, 3, 3, 3, 2, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2},
          {1, 1, 2, 2, 2, 2, 2, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 2, 2, 2, 2, 1, 1, 1, 1, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 2, 1, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 2, 2, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 3, 2, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
          {2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
          {3, 3, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
          {4, 4, 3, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
          {4, 4, 3, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
          {3, 2, 1, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
          {2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
        };
    }

    private void LoadInitialHeightMap()
    {
        int w = gridManager.width;
        int h = gridManager.height;

        if (w == OriginalMapSize && h == OriginalMapSize)
        {
            heightMap.SetHeights(GetOriginal40x40Heights());
            return;
        }

        int[,] extended = new int[w, h];
        int[,] template = GetOriginal40x40Heights();
        for (int x = 0; x < OriginalMapSize && x < w; x++)
        {
            for (int y = 0; y < OriginalMapSize && y < h; y++)
            {
                extended[x, y] = template[y, x];
            }
        }

        FillExtendedTerrainProcedural(extended, w, h);
        heightMap.SetHeights(extended);
    }

    /// <summary>Fills cells outside [0..39,0..39] with coherent Perlin-based terrain and smooth blend at 40x40 border. Water only from lakes and rivers.</summary>
    private void FillExtendedTerrainProcedural(int[,] heights, int w, int h)
    {
        float offsetX = TerrainGenSeed * 0.1f;
        float offsetY = TerrainGenSeed * 0.27f;
        int[,] template = GetOriginal40x40Heights();

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                if (x < OriginalMapSize && y < OriginalMapSize)
                    continue;

                float n1 = Mathf.PerlinNoise((x + offsetX) / 14f, (y + offsetY) / 14f);
                float n2 = Mathf.PerlinNoise((x + offsetX + 100f) / PerlinNoiseScale, (y + offsetY + 100f) / PerlinNoiseScale);
                float n = 0.5f * n1 + 0.5f * n2;
                n = 0.2f + 0.8f * n;
                int perlinHeight = PerlinToHeight(n);

                float blend = 1f;
                int edgeHeight = perlinHeight;
                if (x >= OriginalMapSize && x < OriginalMapSize + BorderBlendWidth && y < OriginalMapSize)
                {
                    edgeHeight = template[y, OriginalMapSize - 1];
                    blend = (float)(x - OriginalMapSize) / BorderBlendWidth;
                }
                else if (y >= OriginalMapSize && y < OriginalMapSize + BorderBlendWidth && x < OriginalMapSize)
                {
                    edgeHeight = template[OriginalMapSize - 1, x];
                    blend = (float)(y - OriginalMapSize) / BorderBlendWidth;
                }

                int finalHeight = blend >= 1f ? perlinHeight : Mathf.RoundToInt(edgeHeight * (1f - blend) + perlinHeight * blend);
                heights[x, y] = Mathf.Clamp(finalHeight, 1, MAX_HEIGHT);
            }
        }

        AddProceduralLakes(heights, w, h);
        AddProceduralRivers(heights, w, h);
    }

    /// <summary>Maps Perlin value [0,1] to land height 1-5: heavily mountainous, few plains. For slope/road testing.</summary>
    private static int PerlinToHeight(float n)
    {
        if (n < 0.1f) return 1;
        if (n < 0.3f) return 2;
        if (n < 0.5f) return 3;
        if (n < 0.75f) return 4;
        return 5;
    }

    private void AddProceduralLakes(int[,] heights, int w, int h)
    {
        Random.InitState(TerrainGenSeed + 1);
        int numLakes = 4 + (int)(Random.value * 3);
        for (int i = 0; i < numLakes; i++)
        {
            int cx = OriginalMapSize + (int)(Random.value * (w - OriginalMapSize - 2));
            int cy = OriginalMapSize + (int)(Random.value * (h - OriginalMapSize - 2));
            if (cx < OriginalMapSize || cy < OriginalMapSize) continue;
            int radius = 2 + (int)(Random.value * 2);
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int nx = cx + dx, ny = cy + dy;
                    if (nx >= OriginalMapSize && nx < w && ny >= OriginalMapSize && ny < h && dx * dx + dy * dy <= radius * radius)
                        heights[nx, ny] = SEA_LEVEL;
                }
            }
        }
    }

    private void AddProceduralRivers(int[,] heights, int w, int h)
    {
        Random.InitState(TerrainGenSeed + 2);
        int numRivers = 4 + (int)(Random.value * 4);
        int[][] dirs = { new[] { 1, 0 }, new[] { -1, 0 }, new[] { 0, 1 }, new[] { 0, -1 } };
        for (int i = 0; i < numRivers; i++)
        {
            int sx = OriginalMapSize + (int)(Random.value * (w - OriginalMapSize - 4));
            int sy = OriginalMapSize + (int)(Random.value * (h - OriginalMapSize - 4));
            if (sx < OriginalMapSize || sy < OriginalMapSize) continue;
            if (heights[sx, sy] < 2) continue;
            int len = 4 + (int)(Random.value * 8);
            int x = sx, y = sy;
            for (int step = 0; step < len; step++)
            {
                if (x >= 0 && x < w && y >= 0 && y < h && (x >= OriginalMapSize || y >= OriginalMapSize))
                    heights[x, y] = SEA_LEVEL;
                int bestDx = 0, bestDy = 0, bestH = 6;
                for (int d = 0; d < 4; d++)
                {
                    int nx = x + dirs[d][0], ny = y + dirs[d][1];
                    if (nx >= 0 && nx < w && ny >= 0 && ny < h)
                    {
                        int nh = heights[nx, ny];
                        if (nh < bestH) { bestH = nh; bestDx = dirs[d][0]; bestDy = dirs[d][1]; }
                    }
                }
                x += bestDx;
                y += bestDy;
                if (bestH == 0) break;
            }
        }
    }

    /// <summary>
    /// Applies terrain (slopes, water, cliff walls) to all cells based on heightMap.
    /// Same mechanism used by New Game. Call after RestoreHeightMapFromGridData + ApplyRestoredPositionsToGrid for Load.
    /// </summary>
    public void ApplyHeightMapToGrid()
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

        // Land cell adjacent to water: use water slope, not land slope or grass (avoids black voids at coast)
        if (newHeight >= 1 && IsAdjacentToWaterHeight(x, y))
        {
            GameObject waterSlopePrefab = DetermineWaterSlopePrefab(x, y);
            if (waterSlopePrefab != null)
                PlaceWaterSlope(x, y, waterSlopePrefab);
            else
                PlaceFlatTerrain(x, y);  // fallback if pattern not recognized
            PlaceCliffWalls(x, y);
            return;
        }

        if (RequiresSlope(x, y, newHeight))
        {
            GameObject slopePrefab = DetermineSlopePrefab(x, y);
            if (slopePrefab != null)
                PlaceSlopeFromPrefab(x, y, slopePrefab, newHeight);
            else
                PlaceFlatTerrain(x, y);  // plateau: no higher neighbors
        }
        else
        {
            PlaceFlatTerrain(x, y);  // flat: all neighbors same height
        }

        if (newHeight == SEA_LEVEL)
        {
            ModifyWaterSlopeInAdjacentNeighbors(x, y);
            return;
        }

        PlaceCliffWalls(x, y);
    }

    /// <summary>
    /// True if the cell has any child with ZoneCategory.Zoning (residential/commercial/industrial overlay).
    /// Used to skip terrain refresh on zoned cells during road preview neighbor refresh.
    /// </summary>
    private bool CellHasZoningOverlay(Cell cell)
    {
        if (cell == null) return false;
        foreach (Transform child in cell.gameObject.transform)
        {
            Zone zone = child.GetComponent<Zone>();
            if (zone != null && zone.zoneCategory == Zone.ZoneCategory.Zoning)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Reapplies terrain for a single cell from the heightMap (e.g. after demolition).
    /// Restores height, world position, sorting order, and slope prefab if needed.
    /// Returns true if this cell was restored as a water slope (caller should not add grass tile).
    /// </summary>
    /// <param name="useHeightMap">If non-null, this map is used (and assigned to heightMap) so restore works even when instance field was null.</param>
    /// <param name="forceFlat">When true, use flat terrain regardless of neighbor heights. Used for terraformed path transition cells.</param>
    /// <param name="forceSlopeType">When set, use this orthogonal slope prefab instead of DetermineSlopePrefab. Used for terraformed path slope cells.</param>
    public bool RestoreTerrainForCell(int x, int y, HeightMap useHeightMap = null, bool forceFlat = false, TerrainSlopeType? forceSlopeType = null)
    {
        if (useHeightMap != null)
            heightMap = useHeightMap;
        if (heightMap == null)
        {
            EnsureHeightMapLoaded();
        }
        if (heightMap == null)
            return false;
        if (!heightMap.IsValidPosition(x, y))
            return false;
        int newHeight = heightMap.GetHeight(x, y);
        if (newHeight == SEA_LEVEL)
            return false;
        Cell cell = gridManager.GetCell(x, y);
        if (cell == null)
            return false;

        if (CellHasZoningOverlay(cell))
            return false;

        gridManager.SetCellHeight(new Vector2(x, y), newHeight);

        Vector2 newWorldPos = gridManager.GetCellWorldPosition(cell);
        cell.gameObject.transform.position = newWorldPos;
        cell.transformPosition = newWorldPos;

        int sortingOrder = CalculateTerrainSortingOrder(x, y, newHeight);
        cell.sortingOrder = sortingOrder;
        SpriteRenderer sr = cell.gameObject.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.sortingOrder = sortingOrder;

        bool adjacentWater = IsAdjacentToWaterHeight(x, y);
        bool requiresSlope = forceFlat ? false : (forceSlopeType.HasValue || RequiresSlope(x, y, newHeight));

        // Land cell adjacent to water (height 0) must restore water slope, not land slope.
        if (newHeight >= 1 && adjacentWater && !forceFlat && !forceSlopeType.HasValue)
        {
            GameObject waterSlopePrefab = DetermineWaterSlopePrefab(x, y);
            if (waterSlopePrefab != null)
                PlaceWaterSlope(x, y, waterSlopePrefab);
            return true;
        }

        if (requiresSlope)
        {
            GameObject slopePrefab = forceSlopeType.HasValue ? GetOrthogonalSlopePrefab(forceSlopeType.Value) : DetermineSlopePrefab(x, y);
            if (slopePrefab != null)
                PlaceSlopeFromPrefab(x, y, slopePrefab, newHeight);
            else
                PlaceFlatTerrain(x, y);  // plateau: no higher neighbors
        }
        else
        {
            PlaceFlatTerrain(x, y);  // flat: all neighbors same height
        }

        PlaceCliffWalls(x, y);
        return false;
    }

    /// <summary>
    /// Returns the terrain prefab for an orthogonal slope type. Used when forcing slope prefab for terraformed path cells.
    /// </summary>
    GameObject GetOrthogonalSlopePrefab(TerrainSlopeType slopeType)
    {
        switch (slopeType)
        {
            case TerrainSlopeType.North: return northSlopePrefab;
            case TerrainSlopeType.South: return southSlopePrefab;
            case TerrainSlopeType.East: return eastSlopePrefab;
            case TerrainSlopeType.West: return westSlopePrefab;
            default: return null;
        }
    }
    #endregion

    #region Terrain Tile Placement
    private void DestroyCellChildren(Cell cell)
    {
        GameObject cellObject = cell.gameObject;  // Get the GameObject that holds the Cell component
        var toDestroy = new List<GameObject>();
        foreach (Transform child in cellObject.transform)
            toDestroy.Add(child.gameObject);
        foreach (GameObject go in toDestroy)
            Destroy(go);
    }

    /// <summary>
    /// Destroys only slope and grass children, preserving road, forest, and buildings.
    /// Used when replacing slope with flat grass (plateau or flat terrain).
    /// Uses DestroyImmediate to avoid deferred Destroy causing multiple grass instances when
    /// RestoreTerrainForCell is called repeatedly in the same frame (e.g. during interstate generation).
    /// </summary>
    private void DestroyTerrainChildrenOnly(Cell cell)
    {
        var toDestroy = new List<GameObject>();
        foreach (Transform child in cell.gameObject.transform)
        {
            Zone zone = child.GetComponent<Zone>();
            if (zone != null && zone.zoneType == Zone.ZoneType.Grass)
                toDestroy.Add(child.gameObject);
            else if (IsWaterSlopeObject(child.gameObject) || IsLandSlopeObject(child.gameObject) || IsBayObject(child.gameObject))
                toDestroy.Add(child.gameObject);
        }
        foreach (GameObject go in toDestroy)
            Object.DestroyImmediate(go);
    }

    private bool RequiresSlope(int x, int y, int currentHeight)
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
                    if (Mathf.Abs(neighborHeight - currentHeight) > 0)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// True if terrain at (x,y) allows building placement. Flat terrain always allowed.
    /// Slopes allowed only for 1x1 buildings (FEAT-34).
    /// </summary>
    private bool IsTerrainPlaceableForBuilding(int x, int y, int buildingSize = 1)
    {
        TerrainSlopeType slope = GetTerrainSlopeTypeAt(x, y);
        if (slope == TerrainSlopeType.Flat) return true;
        return buildingSize == 1;
    }

    /// <summary>
    /// Returns true if this cell has at least one neighbor (including diagonals) at sea level (height 0).
    /// Used to allow water plants on coastal slope tiles. Uses 8 neighbors to match RequiresSlope,
    /// so cells that only touch water diagonally are still considered coastal.
    /// </summary>
    private bool IsAdjacentToWaterHeight(int x, int y)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                int nx = x + dx;
                int ny = y + dy;
                if (heightMap.IsValidPosition(nx, ny) && heightMap.GetHeight(nx, ny) == SEA_LEVEL)
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// True if cell is land (height >= 1) with at least one water neighbor.
    /// Used to enforce road buffer from coastlines: normal roads should stay 1 cell from coast.
    /// </summary>
    public bool IsWaterSlopeCell(int x, int y)
    {
        if (heightMap == null)
            EnsureHeightMapLoaded();
        if (heightMap == null || !heightMap.IsValidPosition(x, y))
            return false;
        int h = heightMap.GetHeight(x, y);
        return h >= 1 && IsAdjacentToWaterHeight(x, y);
    }

    /// <summary>
    /// Replaces terrain with flat grass. Destroys slope and grass children, then places grass.
    /// Used when cell is flat or a plateau (no higher neighbors).
    /// </summary>
    private void PlaceFlatTerrain(int x, int y)
    {
        if (gridManager == null || zoneManager == null) return;

        Cell cell = gridManager.GetCell(x, y);
        if (cell == null) return;

        DestroyTerrainChildrenOnly(cell);

        GameObject grassPrefab = zoneManager.GetRandomZonePrefab(Zone.ZoneType.Grass);
        if (grassPrefab == null && zoneManager.grassPrefabs != null && zoneManager.grassPrefabs.Count > 0)
            grassPrefab = zoneManager.grassPrefabs[0];
        if (grassPrefab == null) return;

        GameObject zoneTile = Instantiate(grassPrefab, cell.transformPosition, Quaternion.identity);
        zoneTile.transform.SetParent(cell.gameObject.transform);

        int sortingOrder = CalculateTerrainSortingOrder(x, y, cell.height);
        SpriteRenderer sr = zoneTile.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.sortingOrder = sortingOrder;
        cell.SetCellInstanceSortingOrder(sortingOrder);
        cell.prefabName = grassPrefab.name;
    }

    /// <summary>
    /// Places a slope tile from the given prefab. Used by RestoreGrid for Load when saved prefabName is a slope.
    /// </summary>
    public void PlaceSlopeFromPrefab(int x, int y, GameObject slopePrefab, int cellHeight = -1)
    {
        if (slopePrefab == null || gridManager == null) return;

        Cell cell = gridManager.GetCell(x, y);
        if (cell == null) return;

        int currentHeight = cellHeight >= 0 ? cellHeight : (heightMap != null ? heightMap.GetHeight(x, y) : cell.height);
        DestroyTerrainChildrenOnly(cell);

        Vector2 worldPos = cell.transformPosition;
        GameObject slope = Instantiate(slopePrefab, worldPos, Quaternion.identity);
        slope.transform.SetParent(cell.gameObject.transform);

        cell.prefabName = slopePrefab.name;

        SpriteRenderer sr = slope.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.sortingOrder = CalculateSlopeSortingOrder(x, y, currentHeight);
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
                        continue;

                    PlaceWaterSlope(nx, ny, waterSlopePrefab);
                }
            }
        }

        PlaceSeaLevelWater(x, y);
    }

    /// <summary>
    /// Restores water slopes on land cells adjacent to water. Call after RestoreGrid during Load.
    /// Skips cells with buildings. Does not modify water cells (WaterManager already placed them).
    /// </summary>
    public void RestoreWaterSlopesFromHeightMap()
    {
        if (heightMap == null || gridManager == null) return;

        for (int x = 0; x < gridManager.width; x++)
        {
            for (int y = 0; y < gridManager.height; y++)
            {
                if (heightMap.GetHeight(x, y) != SEA_LEVEL) continue;

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;

                        int nx = x + dx, ny = y + dy;
                        if (!heightMap.IsValidPosition(nx, ny) || heightMap.GetHeight(nx, ny) == SEA_LEVEL)
                            continue;

                        if (gridManager.IsCellOccupiedByBuilding(nx, ny)) continue;
                        Cell neighborCell = gridManager.GetCell(nx, ny);
                        if (neighborCell != null && (neighborCell.zoneType != Zone.ZoneType.Grass || neighborCell.HasForest())) continue;

                        GameObject waterSlopePrefab = DetermineWaterSlopePrefab(nx, ny);
                        if (waterSlopePrefab != null)
                            PlaceWaterSlope(nx, ny, waterSlopePrefab);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Restores land slopes and water slopes for terrain-only cells. Call after RestoreGrid during Load.
    /// Skips cells with buildings, roads, or zoning.
    /// </summary>
    public void RestoreTerrainSlopesFromHeightMap()
    {
        if (heightMap == null || gridManager == null) return;

        for (int x = 0; x < gridManager.width; x++)
        {
            for (int y = 0; y < gridManager.height; y++)
            {
                if (heightMap.GetHeight(x, y) == SEA_LEVEL) continue;

                Cell cell = gridManager.GetCell(x, y);
                if (cell == null) continue;
                if (gridManager.IsCellOccupiedByBuilding(x, y)) continue;
                if (cell.zoneType != Zone.ZoneType.Grass) continue;
                if (cell.HasForest()) continue;

                bool adjacentWater = IsAdjacentToWaterHeight(x, y);
                bool requiresSlope = RequiresSlope(x, y, heightMap.GetHeight(x, y));
                if (!adjacentWater && !requiresSlope) continue;

                // Only process cells that are the LOW side of a height transition (where we place the slope).
                // High-side cells keep their grass from RestoreGrid and must not be touched.
                bool isLowSideOfHeightTransition = requiresSlope && DetermineSlopePrefab(x, y) != null;
                if (!adjacentWater && !isLowSideOfHeightTransition)
                    continue;

                RestoreTerrainForCell(x, y);
            }
        }
    }

    private void PlaceSeaLevelWater(int x, int y)
    {
        GameObject seaLevelWater = seaLevelWaterPrefab;
        if (seaLevelWater == null)
        {
            return;
        }

        Cell cell = gridManager.GetCell(x, y);
        DestroyCellChildren(cell);

        Vector2 worldPos = cell.transformPosition;
        GameObject seaLevelWaterObject = Instantiate(
            seaLevelWater,
            worldPos,
            Quaternion.identity
        );

        seaLevelWaterObject.transform.SetParent(cell.gameObject.transform);
        cell.zoneType = Zone.ZoneType.Water;
        gridManager.SetCellHeight(new Vector2(x, y), 0);
        Cell updatedCell = gridManager.GetCell(x, y);

        int sortingOrder = CalculateTerrainSortingOrder(x, y, SEA_LEVEL);
        SpriteRenderer sr = seaLevelWaterObject.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = sortingOrder;
        }
        updatedCell.sortingOrder = sortingOrder;
    }

    private void PlaceWaterSlope(int x, int y, GameObject waterSlopePrefab)
    {
        Cell cell = gridManager.GetCell(x, y);
        DestroyTerrainChildrenOnly(cell);

        // Cell height = 1 (land) so logic (forest, roads, etc.) treats this as land; heightMap already has 1 here.
        gridManager.SetCellHeight(new Vector2(x, y), 1);
        Cell updatedCell = gridManager.GetCell(x, y);

        // Cell at land elevation; slope prefab at water elevation so it appears lower (transition to water).
        Vector2 cellWorldPos = gridManager.GetWorldPositionVector(x, y, 1);
        cell.gameObject.transform.position = cellWorldPos;
        updatedCell.transformPosition = cellWorldPos;

        Vector2 slopeWorldPos = gridManager.GetWorldPositionVector(x, y, SEA_LEVEL);
        GameObject slope = Instantiate(
            waterSlopePrefab,
            slopeWorldPos,
            Quaternion.identity
        );
        slope.transform.SetParent(cell.gameObject.transform, true);

        updatedCell.prefabName = waterSlopePrefab.name;

        int sortingOrder = CalculateWaterSlopeSortingOrder(x, y);
        SpriteRenderer sr = slope.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = sortingOrder;
        }
        updatedCell.sortingOrder = sortingOrder;
    }

    private void PlaceCliffWalls(int x, int y)
    {
        int currentHeight = heightMap.GetHeight(x, y);
        if (currentHeight <= SEA_LEVEL)
        {
            return;
        }

        Cell cell = gridManager.GetCell(x, y);
        RemoveExistingCliffWalls(cell);

        if (NeedsCliffWallSouth(x, y, currentHeight))
            PlaceCliffWallPrefab(cell, southCliffWallPrefab, x, y, currentHeight);
        if (NeedsCliffWallEast(x, y, currentHeight))
            PlaceCliffWallPrefab(cell, eastCliffWallPrefab, x, y, currentHeight);
        if (NeedsCliffWallNorth(x, y, currentHeight))
            PlaceCliffWallPrefab(cell, northCliffWallPrefab, x, y, currentHeight);
        if (NeedsCliffWallWest(x, y, currentHeight))
            PlaceCliffWallPrefab(cell, westCliffWallPrefab, x, y, currentHeight);
    }

    private bool NeedsCliffWallSouth(int x, int y, int currentHeight)
    {
        if (!heightMap.IsValidPosition(x - 1, y) || !heightMap.IsValidPosition(x + 1, y))
        {
            return false;
        }

        int heightAtSouth = heightMap.GetHeight(x - 1, y);
        if (currentHeight - heightAtSouth > 1)
        {
            return true;
        }

        int heightAtNorth = heightMap.GetHeight(x + 1, y);
        return heightAtSouth == SEA_LEVEL && currentHeight == 1 && heightAtNorth == 2;
    }

    private bool NeedsCliffWallEast(int x, int y, int currentHeight)
    {
        if (!heightMap.IsValidPosition(x, y - 1) || !heightMap.IsValidPosition(x, y + 1))
        {
            return false;
        }

        int heightAtEast = heightMap.GetHeight(x, y - 1);
        if (currentHeight - heightAtEast > 1)
        {
            return true;
        }

        int heightAtWest = heightMap.GetHeight(x, y + 1);
        return heightAtEast == SEA_LEVEL && currentHeight == 1 && heightAtWest == 2;
    }

    private bool NeedsCliffWallNorth(int x, int y, int currentHeight)
    {
        if (!heightMap.IsValidPosition(x + 1, y) || !heightMap.IsValidPosition(x - 1, y))
            return false;
        int heightAtNorth = heightMap.GetHeight(x + 1, y);
        if (currentHeight - heightAtNorth > 1)
            return true;
        int heightAtSouth = heightMap.GetHeight(x - 1, y);
        return heightAtNorth == SEA_LEVEL && currentHeight == 1 && heightAtSouth == 2;
    }

    private bool NeedsCliffWallWest(int x, int y, int currentHeight)
    {
        if (!heightMap.IsValidPosition(x, y + 1) || !heightMap.IsValidPosition(x, y - 1))
            return false;
        int heightAtWest = heightMap.GetHeight(x, y + 1);
        if (currentHeight - heightAtWest > 1)
            return true;
        int heightAtEast = heightMap.GetHeight(x, y - 1);
        return heightAtWest == SEA_LEVEL && currentHeight == 1 && heightAtEast == 2;
    }

    private void PlaceCliffWallPrefab(Cell cell, GameObject prefab, int x, int y, int currentHeight)
    {
        if (prefab == null)
        {
            return;
        }

        GameObject cliffWall = Instantiate(prefab, cell.transformPosition, Quaternion.identity);
        cliffWall.transform.SetParent(cell.gameObject.transform);

        SpriteRenderer sr = cliffWall.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = CalculateTerrainSortingOrder(x, y, currentHeight) + SLOPE_OFFSET;
        }
    }

    private void RemoveExistingCliffWalls(Cell cell)
    {
        if (cell == null)
        {
            return;
        }

        for (int i = cell.transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = cell.transform.GetChild(i).gameObject;
            if (IsPrefabInstance(child, southCliffWallPrefab)
                || IsPrefabInstance(child, eastCliffWallPrefab)
                || IsPrefabInstance(child, northCliffWallPrefab)
                || IsPrefabInstance(child, westCliffWallPrefab))
            {
                Destroy(child);
            }
        }
    }
    #endregion

    #region Slope Calculation
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

        bool isAtNorthBorder = !heightMap.IsValidPosition(x + 1, y);
        bool isAtSouthBorder = !heightMap.IsValidPosition(x - 1, y);
        bool isAtWestBorder = !heightMap.IsValidPosition(x, y + 1);
        bool isAtEastBorder = !heightMap.IsValidPosition(x, y - 1);

        if (isAtSouthBorder)
        {
            if (hasSeaLevelAtWest) return westSlopeWaterPrefab;
            if (hasSeaLevelAtEast) return eastSlopeWaterPrefab;
            if (hasSeaLevelAtNorth) return northSlopeWaterPrefab;
        }

        if (isAtNorthBorder)
        {
            if (hasSeaLevelAtWest) return westSlopeWaterPrefab;
            if (hasSeaLevelAtEast) return eastSlopeWaterPrefab;
            if (hasSeaLevelAtSouth) return southSlopeWaterPrefab;
        }

        if (isAtWestBorder)
        {
            if (hasSeaLevelAtNorth) return northSlopeWaterPrefab;
            if (hasSeaLevelAtSouth) return southSlopeWaterPrefab;
            if (hasSeaLevelAtEast) return eastSlopeWaterPrefab;
        }

        if (isAtEastBorder)
        {
            if (hasSeaLevelAtNorth) return northSlopeWaterPrefab;
            if (hasSeaLevelAtSouth) return southSlopeWaterPrefab;
            if (hasSeaLevelAtWest) return westSlopeWaterPrefab;
        }


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
        int heightOrder = height * HEIGHT_MULTIPLIER;

        return TERRAIN_BASE_ORDER + depthOrder + heightOrder;
    }

    /// <summary>
    /// Calculates sorting order for water-slope tiles, positioned slightly above sea-level terrain.
    /// </summary>
    /// <param name="x">Grid X coordinate.</param>
    /// <param name="y">Grid Y coordinate.</param>
    /// <returns>Sorting order value for the water slope.</returns>
    public int CalculateWaterSlopeSortingOrder(int x, int y)
    {
        const int WATER_SLOPE_OFFSET = 1;
        return CalculateTerrainSortingOrder(x, y, SEA_LEVEL) + WATER_SLOPE_OFFSET;
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

    /// <summary>
    /// Returns true if the given GameObject is an instance of any water-slope prefab.
    /// </summary>
    /// <param name="obj">The GameObject to check.</param>
    /// <returns>True if the object matches a water-slope prefab.</returns>
    public bool IsWaterSlopeObject(GameObject obj)
    {
        return IsPrefabInstance(obj, northSlopeWaterPrefab)
            || IsPrefabInstance(obj, southSlopeWaterPrefab)
            || IsPrefabInstance(obj, eastSlopeWaterPrefab)
            || IsPrefabInstance(obj, westSlopeWaterPrefab)
            || IsPrefabInstance(obj, northEastSlopeWaterPrefab)
            || IsPrefabInstance(obj, northWestSlopeWaterPrefab)
            || IsPrefabInstance(obj, southEastSlopeWaterPrefab)
            || IsPrefabInstance(obj, southWestSlopeWaterPrefab)
            || IsPrefabInstance(obj, northEastUpslopeWaterPrefab)
            || IsPrefabInstance(obj, northWestUpslopeWaterPrefab)
            || IsPrefabInstance(obj, southEastUpslopeWaterPrefab)
            || IsPrefabInstance(obj, southWestUpslopeWaterPrefab);
    }

    /// <summary>
    /// Returns true if the object is a land slope tile (not water slope).
    /// </summary>
    public bool IsLandSlopeObject(GameObject obj)
    {
        return IsPrefabInstance(obj, northSlopePrefab)
            || IsPrefabInstance(obj, southSlopePrefab)
            || IsPrefabInstance(obj, eastSlopePrefab)
            || IsPrefabInstance(obj, westSlopePrefab)
            || IsPrefabInstance(obj, northEastSlopePrefab)
            || IsPrefabInstance(obj, northWestSlopePrefab)
            || IsPrefabInstance(obj, southEastSlopePrefab)
            || IsPrefabInstance(obj, southWestSlopePrefab)
            || IsPrefabInstance(obj, northEastUpslopePrefab)
            || IsPrefabInstance(obj, northWestUpslopePrefab)
            || IsPrefabInstance(obj, southEastUpslopePrefab)
            || IsPrefabInstance(obj, southWestUpslopePrefab);
    }

    /// <summary>
    /// Returns true if the given GameObject is an instance of the sea-level water prefab.
    /// </summary>
    /// <param name="obj">The GameObject to check.</param>
    /// <returns>True if the object matches the sea-level water prefab.</returns>
    public bool IsSeaLevelWaterObject(GameObject obj)
    {
        return IsPrefabInstance(obj, seaLevelWaterPrefab);
    }

    /// <summary>
    /// Returns true if the given GameObject is an instance of any bay prefab (coastal water terrain).
    /// </summary>
    public bool IsBayObject(GameObject obj)
    {
        return IsPrefabInstance(obj, northEastBayPrefab)
            || IsPrefabInstance(obj, northWestBayPrefab)
            || IsPrefabInstance(obj, southEastBayPrefab)
            || IsPrefabInstance(obj, southWestBayPrefab);
    }

    private bool IsPrefabInstance(GameObject obj, GameObject prefab)
    {
        if (obj == null || prefab == null)
        {
            return false;
        }

        return obj.name.StartsWith(prefab.name);
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

        GameObject result = null;
        if (hasWestSlope && hasNorthSlope) result = southEastUpslopePrefab;
        else if (hasWestSlope && hasSouthSlope) result = northEastUpslopePrefab;
        else if (hasEastSlope && hasNorthSlope) result = southWestUpslopePrefab;
        else if (hasEastSlope && hasSouthSlope) result = northWestUpslopePrefab;
        else if (hasNorthSlope) result = southSlopePrefab;
        else if (hasSouthSlope) result = northSlopePrefab;
        else if (hasEastSlope) result = westSlopePrefab;
        else if (hasWestSlope) result = eastSlopePrefab;
        else if (hasNorthWestSlope) result = southEastSlopePrefab;
        else if (hasNorthEastSlope) result = southWestSlopePrefab;
        else if (hasSouthWestSlope) result = northEastSlopePrefab;
        else if (hasSouthEastSlope) result = northWestSlopePrefab;
        return result;
    }

    /// <summary>
    /// Returns the slope type at (x,y) for use by ForestManager etc. Uses same logic as DetermineSlopePrefab.
    /// Returns Flat if heightMap is null or position invalid. Calls EnsureHeightMapLoaded() when heightMap is null so ForestManager can get slope type even if init order skipped.
    /// </summary>
    public TerrainSlopeType GetTerrainSlopeTypeAt(int x, int y)
    {
        if (heightMap == null)
            EnsureHeightMapLoaded();

        if (heightMap == null || !heightMap.IsValidPosition(x, y))
            return TerrainSlopeType.Flat;

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

        // Return slope *face* direction (same convention as DetermineSlopePrefab): prefab name = direction slope faces (downhill).
        TerrainSlopeType result;
        if (hasWestSlope && hasNorthSlope) result = TerrainSlopeType.SouthEastUp;
        else if (hasWestSlope && hasSouthSlope) result = TerrainSlopeType.NorthEastUp;
        else if (hasEastSlope && hasNorthSlope) result = TerrainSlopeType.SouthWestUp;
        else if (hasEastSlope && hasSouthSlope) result = TerrainSlopeType.NorthWestUp;
        else if (hasNorthSlope) result = TerrainSlopeType.South;   // north higher => slope faces south
        else if (hasSouthSlope) result = TerrainSlopeType.North;   // south higher => slope faces north
        else if (hasEastSlope) result = TerrainSlopeType.West;      // east higher => slope faces west
        else if (hasWestSlope) result = TerrainSlopeType.East;     // west higher => slope faces east
        else if (hasNorthWestSlope) result = TerrainSlopeType.SouthEast;
        else if (hasNorthEastSlope) result = TerrainSlopeType.SouthWest;
        else if (hasSouthWestSlope) result = TerrainSlopeType.NorthEast;
        else if (hasSouthEastSlope) result = TerrainSlopeType.NorthWest;
        else result = TerrainSlopeType.Flat;

        return result;
    }
    #endregion

    #region Utility Methods
    /// <summary>
    /// Modifies the terrain height at the given grid position.
    /// </summary>
    /// <param name="x">Grid X coordinate.</param>
    /// <param name="y">Grid Y coordinate.</param>
    /// <param name="newHeight">The new height value to set.</param>
    public void ModifyTerrain(int x, int y, int newHeight)
    {
        // Implementation for terrain modification
    }

    /// <summary>
    /// Checks whether a building of the given size can be placed at the grid position based on terrain constraints (height uniformity, slopes, water).
    /// Allows Flat terrain only; rejects all slope types until slope building support is implemented.
    /// </summary>
    /// <param name="gridPosition">The grid position for placement.</param>
    /// <param name="size">The footprint size of the building.</param>
    /// <param name="failReason">When the method returns false, contains the specific reason for failure.</param>
    /// <param name="allowCoastalSlope">When true, allows placement on tiles that have slope only due to being adjacent to water (e.g. for water plants).</param>
    /// <param name="allowWaterInFootprint">When true, water tiles in the footprint are allowed (e.g. for water plants); they are skipped for height/slope checks.</param>
    /// <returns>True if the terrain allows placement; false otherwise.</returns>
    public bool CanPlaceBuildingInTerrain(Vector2 gridPosition, int size, out string failReason, bool allowCoastalSlope = false, bool allowWaterInFootprint = false)
    {
        failReason = null;
        int offsetX, offsetY;
        if (gridManager != null)
            gridManager.GetBuildingFootprintOffset(size, out offsetX, out offsetY);
        else
        {
            offsetX = size % 2 == 0 ? 0 : size / 2;
            offsetY = size % 2 == 0 ? 0 : size / 2;
        }

        int? landBaseHeight = null;
        if (!allowWaterInFootprint)
            landBaseHeight = heightMap.GetHeight((int)gridPosition.x, (int)gridPosition.y);

        for (int dx = 0; dx < size; dx++)
        {
            for (int dy = 0; dy < size; dy++)
            {
                int checkX = (int)gridPosition.x + dx - offsetX;
                int checkY = (int)gridPosition.y + dy - offsetY;

                if (!heightMap.IsValidPosition(checkX, checkY))
                {
                    failReason = "Out of bounds.";
                    return false;
                }

                bool isWater = waterManager != null && waterManager.IsWaterAt(checkX, checkY);
                if (allowWaterInFootprint && isWater)
                    continue;

                int cellHeight = heightMap.GetHeight(checkX, checkY);
                if (allowWaterInFootprint)
                {
                    if (!landBaseHeight.HasValue)
                        landBaseHeight = cellHeight;
                    if (cellHeight != landBaseHeight.Value)
                    {
                        failReason = "Height mismatch in footprint.";
                        return false;
                    }
                }
                else
                {
                    if (cellHeight != landBaseHeight.Value)
                    {
                        failReason = "Height mismatch in footprint.";
                        return false;
                    }
                }

                if (!IsTerrainPlaceableForBuilding(checkX, checkY, size))
                {
                    if (!allowCoastalSlope || !IsAdjacentToWaterHeight(checkX, checkY))
                    {
                        failReason = "Slope not allowed here (diagonal or corner slope).";
                        return false;
                    }
                }

                if (!allowWaterInFootprint && isWater)
                {
                    failReason = "Water in footprint.";
                    return false;
                }
            }
        }

        if (allowWaterInFootprint && !landBaseHeight.HasValue)
        {
            failReason = "Water plant must have at least one land tile in footprint.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// True if a road can be placed at (x,y): not occupied, and terrain is flat, cardinal, or diagonal slope.
    /// Water cells (height 0) are allowed for bridge placement. Water slope cells (land adjacent to water)
    /// are rejected for normal roads to keep a 1-cell buffer from coastlines. Diagonal slopes use
    /// orthogonal road prefabs (FEAT-05). Corner slopes (NEUp, NWUp, SEUp, SWUp) have no prefabs yet and are rejected.
    /// </summary>
    public bool CanPlaceRoad(int x, int y)
    {
        if (gridManager != null && gridManager.IsCellOccupiedByBuilding(x, y))
            return false;
        if (gridManager != null)
        {
            Cell c = gridManager.GetCell(x, y);
            if (c != null && c.GetCellInstanceHeight() == 0)
                return true;
        }
        if (IsWaterSlopeCell(x, y))
            return false;
        TerrainSlopeType slope = GetTerrainSlopeTypeAt(x, y);
        switch (slope)
        {
            case TerrainSlopeType.Flat:
            case TerrainSlopeType.North:
            case TerrainSlopeType.South:
            case TerrainSlopeType.East:
            case TerrainSlopeType.West:
            case TerrainSlopeType.NorthEast:
            case TerrainSlopeType.NorthWest:
            case TerrainSlopeType.SouthEast:
            case TerrainSlopeType.SouthWest:
            case TerrainSlopeType.NorthEastUp:
            case TerrainSlopeType.NorthWestUp:
            case TerrainSlopeType.SouthEastUp:
            case TerrainSlopeType.SouthWestUp:
                return true;
            default:
                return false;
        }
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
    #endregion
}
}
