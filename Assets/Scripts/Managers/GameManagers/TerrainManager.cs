using UnityEngine;
using System.Collections.Generic;
using Territory.Core;
using Territory.Zones;
using Territory.Persistence;

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
    /// <summary>
    /// When true, logs when <see cref="RestoreTerrainForCell"/> exits early (null cell, overlay, invalid position).
    /// Used to diagnose cut-through / BUG-29 voids vs sorting issues.
    /// </summary>
    public static bool LogTerraformRestoreDiagnostics = false;

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

    /// <summary>Cardinal offsets (south, north, east, west) for neighbor scans — order matches four-direction loops.</summary>
    static readonly int[] CardinalDx = { -1, 1, 0, 0 };
    static readonly int[] CardinalDy = { 0, 0, -1, 1 };

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
        EnsureGuaranteedLakeDepressions();
        ApplyHeightMapToGrid();
    }

    /// <summary>
    /// After procedural height generation, carves minimal cardinal bowls until
    /// <see cref="LakeFeasibility.CountSpillPassingCells"/> reaches
    /// <c>2 × ProceduralLakeBudgetHardCap + LakeFeasibilityExtraBowls</c> (capped by map area).
    /// Uses shuffled full interior scans so the target is met for any map size when physically possible.
    /// Skips if <see cref="waterManager"/> is missing or lake fill is disabled.
    /// </summary>
    private void EnsureGuaranteedLakeDepressions()
    {
        if (heightMap == null || gridManager == null)
            return;

        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();
        if (waterManager == null || !waterManager.useLakeDepressionFill)
            return;

        LakeFillSettings settings = waterManager.LakeFillSettings;
        int w = gridManager.width;
        int h = gridManager.height;

        int minRequired = 2 * settings.ProceduralLakeBudgetHardCap + settings.LakeFeasibilityExtraBowls;
        minRequired = Mathf.Max(1, minRequired);
        int maxPassingPossible = w * h;
        minRequired = Mathf.Min(minRequired, maxPassingPossible);

        int passing = LakeFeasibility.CountSpillPassingCells(heightMap);

        if (w < 3 || h < 3)
        {
            return;
        }

        if (passing >= minRequired)
        {
            return;
        }

        int rngSeed = unchecked(settings.RandomSeed ^ (w * 73856093) ^ (h * 19349663) ^ 0x4C414B45);
        var rnd = new System.Random(rngSeed);

        var interior = new List<Vector2Int>((w - 2) * (h - 2));
        for (int x = 1; x < w - 1; x++)
        {
            for (int y = 1; y < h - 1; y++)
                interior.Add(new Vector2Int(x, y));
        }

        int carves = 0;
        int round = 0;
        const int maxRounds = 500;

        while (passing < minRequired && round < maxRounds)
        {
            round++;
            ShuffleCoordsList(interior, rnd);
            bool anyCarveThisRound = false;
            foreach (Vector2Int c in interior)
            {
                if (passing >= minRequired)
                    break;
                if (!LakeFeasibility.PassesSpillTest(c.x, c.y, heightMap))
                {
                    LakeFeasibility.CarveMinimalCardinalBowl(heightMap, c.x, c.y);
                    carves++;
                    passing = LakeFeasibility.CountSpillPassingCells(heightMap);
                    anyCarveThisRound = true;
                    if (passing >= minRequired)
                        break;
                }
            }

            if (!anyCarveThisRound)
                break;
        }

    }

    private static void ShuffleCoordsList(List<Vector2Int> list, System.Random rnd)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rnd.Next(i + 1);
            Vector2Int tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
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
    /// <summary>Low-frequency Perlin (large horizontal plateaus for extended map).</summary>
    private const float ExtendedPerlinScaleCoarse = 58f;
    /// <summary>Medium-frequency detail; mixed lightly with coarse.</summary>
    private const float ExtendedPerlinScaleFine = 38f;
    private const float ExtendedPerlinCoarseWeight = 0.72f;
    private const float ExtendedNoiseRemapLow = 0.32f;
    private const float ExtendedNoiseRemapRange = 0.58f;
    private const int BorderBlendWidth = 16;
    private const int ExtendedTerrainSmoothPasses = 2;
    /// <summary>Fine Perlin scale for sparse one-step dips (FEAT-37a lake seeds outside the 40×40 template).</summary>
    private const float ExtendedMicroLakeNoiseScale = 9f;

    /// <summary>Original 40×40 height map (rows y, cols x). When the grid is larger, placed <b>centered</b>; procedural fill surrounds it.</summary>
    private static int[,] GetOriginal40x40Heights()
    {
        return new int[,] {
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 3, 4, 5, 5, 5, 4, 3, 2, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 3, 4, 5, 5, 5, 4, 3, 2, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2},
          {1, 1, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 1, 1, 2, 3, 4, 5, 5, 5, 4, 3, 2, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2},
          {1, 1, 2, 3, 3, 3, 3, 3, 2, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 1, 1, 2, 2, 3, 4, 5, 4, 3, 3, 2, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2},
          {1, 1, 2, 3, 3, 3, 3, 3, 2, 1, 1, 1, 1, 2, 2, 1, 1, 1, 1, 1, 1, 2, 2, 3, 4, 5, 4, 3, 3, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2},
          {1, 1, 2, 3, 3, 2, 2, 3, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2},
          {1, 1, 2, 3, 3, 2, 2, 3, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 2, 3, 3, 3, 3, 3, 2, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 2, 3, 3, 3, 3, 3, 2, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 1, 1, 1, 1, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1},
          {1, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 3, 3, 3, 3, 3, 3, 3, 2, 1},
          {2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 3, 2, 2, 2, 2, 2, 3, 2, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 3, 2, 2, 2, 2, 2, 3, 2, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 3, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 3, 2, 2, 3, 3, 2, 3, 2, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 3, 2, 2, 3, 3, 2, 3, 2, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 3, 2, 2, 2, 2, 2, 3, 2, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 3, 2, 2, 2, 2, 2, 3, 2, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 3, 3, 3, 3, 3, 3, 3, 2, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {3, 3, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {4, 4, 3, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {4, 4, 3, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2},
          {3, 2, 1, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2},
          {2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 3, 3, 4, 4, 4, 4, 4, 4, 4},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 3, 3, 4, 4, 4, 4, 4, 4, 4},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 3, 3, 4, 4, 3, 3, 3, 3, 4},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 3, 3, 4, 4, 3, 2, 2, 3, 4},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 3, 3, 4, 4, 3, 2, 2, 3, 4},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 3, 3, 4, 4, 3, 3, 3, 3, 4},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 3, 3, 4, 4, 4, 4, 4, 4, 4}
        };
    }

    private void LoadInitialHeightMap()
    {
        MapGenerationSeed.EnsureSessionMasterSeed();

        int w = gridManager.width;
        int h = gridManager.height;

        if (w == OriginalMapSize && h == OriginalMapSize)
        {
            heightMap.SetHeights(GetOriginal40x40Heights());
            return;
        }

        int[,] extended = new int[w, h];
        int[,] template = GetOriginal40x40Heights();
        int ox = Mathf.Max(0, (w - OriginalMapSize) / 2);
        int oy = Mathf.Max(0, (h - OriginalMapSize) / 2);

        for (int tx = 0; tx < OriginalMapSize && ox + tx < w; tx++)
        {
            for (int ty = 0; ty < OriginalMapSize && oy + ty < h; ty++)
            {
                extended[ox + tx, oy + ty] = template[ty, tx];
            }
        }

        FillExtendedTerrainProcedural(extended, w, h, ox, oy);
        heightMap.SetHeights(extended);
    }

    /// <summary>
    /// Fills cells outside the centered 40×40 template with low-frequency Perlin terrain, layered plateaus, and 3×3 smoothing.
    /// Blends at the template border. Lakes are placed later via <see cref="WaterMap.InitializeLakesFromDepressionFill"/> (FEAT-37a).
    /// </summary>
    private void FillExtendedTerrainProcedural(int[,] heights, int w, int h, int templateOriginX, int templateOriginY)
    {
        int terrainSeed = MapGenerationSeed.GetTerrainProceduralOffsetSeed();
        float offsetX = terrainSeed * 0.1f;
        float offsetY = terrainSeed * 0.27f;
        int[,] template = GetOriginal40x40Heights();
        float fineWeight = 1f - ExtendedPerlinCoarseWeight;
        int ox = templateOriginX;
        int oy = templateOriginY;
        int txMax = ox + OriginalMapSize;
        int tyMax = oy + OriginalMapSize;

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                if (x >= ox && x < txMax && y >= oy && y < tyMax)
                    continue;

                float n1 = Mathf.PerlinNoise((x + offsetX) / ExtendedPerlinScaleCoarse, (y + offsetY) / ExtendedPerlinScaleCoarse);
                float n2 = Mathf.PerlinNoise((x + offsetX + 100f) / ExtendedPerlinScaleFine, (y + offsetY + 100f) / ExtendedPerlinScaleFine);
                float n = ExtendedPerlinCoarseWeight * n1 + fineWeight * n2;
                n = Mathf.Clamp01(ExtendedNoiseRemapLow + ExtendedNoiseRemapRange * n);
                int perlinHeight = PerlinToHeightExtended(n);

                float blend = 1f;
                int edgeHeight = perlinHeight;
                TryGetTemplateBorderBlend(x, y, ox, oy, template, perlinHeight, out edgeHeight, out blend);

                int finalHeight = blend >= 1f ? perlinHeight : Mathf.RoundToInt(edgeHeight * (1f - blend) + perlinHeight * blend);
                heights[x, y] = Mathf.Clamp(finalHeight, 1, MAX_HEIGHT);
            }
        }

        SmoothExtendedTerrainHeights(heights, w, h, ox, oy, ExtendedTerrainSmoothPasses);
        ApplyExtendedMicroLakeRoughness(heights, w, h, ox, oy);
    }

    /// <summary>
    /// Sparse fine-scale height dips outside the template so depression-fill can find valid lake seeds on extended terrain.
    /// </summary>
    private static void ApplyExtendedMicroLakeRoughness(int[,] heights, int w, int h, int ox, int oy)
    {
        int microSalt = MapGenerationSeed.GetTerrainMicroLakeNoiseSalt();
        float offX = microSalt * 0.031f;
        float offY = microSalt * 0.019f;
        float carveThreshold = MapGenerationSeed.GetMicroLakeCarveThreshold();
        int txMax = ox + OriginalMapSize;
        int tyMax = oy + OriginalMapSize;
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                if (x >= ox && x < txMax && y >= oy && y < tyMax)
                    continue;

                float n = Mathf.PerlinNoise((x + offX) / ExtendedMicroLakeNoiseScale, (y + offY) / ExtendedMicroLakeNoiseScale);
                if (n < carveThreshold && heights[x, y] > MIN_HEIGHT + 1)
                    heights[x, y] = Mathf.Max(MIN_HEIGHT + 1, heights[x, y] - 1);
            }
        }
    }

    /// <summary>Blends procedural height toward the centered template along the four sides and four corner bands.</summary>
    private static void TryGetTemplateBorderBlend(int x, int y, int ox, int oy, int[,] template, int perlinHeight, out int edgeHeight, out float blend)
    {
        edgeHeight = perlinHeight;
        blend = 1f;
        int bw = BorderBlendWidth;
        int tx0 = ox;
        int tx1 = ox + OriginalMapSize - 1;
        int ty0 = oy;
        int ty1 = oy + OriginalMapSize - 1;

        float bestBlend = 1f;
        int bestEdge = perlinHeight;

        void Consider(float b, int eh)
        {
            if (b < bestBlend)
            {
                bestBlend = b;
                bestEdge = eh;
            }
        }

        // Corner bands (two-axis blend toward template corners)
        if (x >= tx1 + 1 && x < tx1 + 1 + bw && y >= ty1 + 1 && y < ty1 + 1 + bw)
        {
            float bx = (x - (tx1 + 1)) / (float)bw;
            float by = (y - (ty1 + 1)) / (float)bw;
            Consider(Mathf.Max(bx, by), template[OriginalMapSize - 1, OriginalMapSize - 1]);
        }
        if (x >= tx1 + 1 && x < tx1 + 1 + bw && y >= ty0 - bw && y < ty0)
        {
            float bx = (x - (tx1 + 1)) / (float)bw;
            float by = (ty0 - 1 - y) / (float)bw;
            Consider(Mathf.Max(bx, by), template[0, OriginalMapSize - 1]);
        }
        if (x >= tx0 - bw && x < tx0 && y >= ty1 + 1 && y < ty1 + 1 + bw)
        {
            float bx = (tx0 - 1 - x) / (float)bw;
            float by = (y - (ty1 + 1)) / (float)bw;
            Consider(Mathf.Max(bx, by), template[OriginalMapSize - 1, 0]);
        }
        if (x >= tx0 - bw && x < tx0 && y >= ty0 - bw && y < ty0)
        {
            float bx = (tx0 - 1 - x) / (float)bw;
            float by = (ty0 - 1 - y) / (float)bw;
            Consider(Mathf.Max(bx, by), template[0, 0]);
        }

        // East / west strips
        if (y >= ty0 && y <= ty1)
        {
            if (x >= tx1 + 1 && x < tx1 + 1 + bw)
                Consider((x - (tx1 + 1)) / (float)bw, template[y - ty0, OriginalMapSize - 1]);
            if (x >= tx0 - bw && x < tx0)
                Consider((tx0 - 1 - x) / (float)bw, template[y - ty0, 0]);
        }

        // North / south strips (template row 0 = low y edge, row 39 = high y edge in array indexing used by GetOriginal40x40Heights)
        if (x >= tx0 && x <= tx1)
        {
            if (y >= ty1 + 1 && y < ty1 + 1 + bw)
                Consider((y - (ty1 + 1)) / (float)bw, template[OriginalMapSize - 1, x - tx0]);
            if (y >= ty0 - bw && y < ty0)
                Consider((ty0 - 1 - y) / (float)bw, template[0, x - tx0]);
        }

        if (bestBlend < 1f)
        {
            blend = bestBlend;
            edgeHeight = bestEdge;
        }
    }

    /// <summary>3×3 box blur on procedural cells only (outside centered template); softens terraces and sharp pits.</summary>
    private static void SmoothExtendedTerrainHeights(int[,] heights, int w, int h, int ox, int oy, int passes)
    {
        int txMax = ox + OriginalMapSize;
        int tyMax = oy + OriginalMapSize;
        for (int p = 0; p < passes; p++)
        {
            int[,] src = (int[,])heights.Clone();
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    if (x >= ox && x < txMax && y >= oy && y < tyMax)
                        continue;

                    int sum = 0;
                    int count = 0;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int nx = x + dx;
                            int ny = y + dy;
                            if (nx < 0 || nx >= w || ny < 0 || ny >= h)
                                continue;
                            sum += src[nx, ny];
                            count++;
                        }
                    }

                    if (count > 0)
                        heights[x, y] = Mathf.Clamp(Mathf.RoundToInt((float)sum / count), 1, MAX_HEIGHT);
                }
            }
        }
    }

    /// <summary>Maps Perlin [0,1] to land height 1–5: favors wide plains and mid plateaus; fewer peaks (FEAT-37a / large lakes).</summary>
    private static int PerlinToHeightExtended(float n)
    {
        if (n < 0.28f) return 1;
        if (n < 0.48f) return 2;
        if (n < 0.66f) return 3;
        if (n < 0.84f) return 4;
        return 5;
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

    /// <summary>
    /// Reapplies terrain for an inclusive heightmap rectangle (e.g. after artificial lake carving).
    /// Uses the same diagonal sweep order as <see cref="ApplyHeightMapToGrid"/> for consistent slope resolution.
    /// </summary>
    public void ApplyHeightMapToRegion(int minX, int minY, int maxX, int maxY)
    {
        if (heightMap == null || gridManager == null)
            return;

        minX = Mathf.Clamp(minX, 0, gridManager.width - 1);
        maxX = Mathf.Clamp(maxX, 0, gridManager.width - 1);
        minY = Mathf.Clamp(minY, 0, gridManager.height - 1);
        maxY = Mathf.Clamp(maxY, 0, gridManager.height - 1);

        for (int sum = minX + minY; sum <= maxX + maxY; sum++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                int y = sum - x;
                if (y >= minY && y <= maxY && y >= 0 && y < gridManager.height)
                    UpdateTileElevation(x, y);
            }
        }
    }

    /// <summary>
    /// After lake water visuals exist, refreshes land cells near lake water so shore/bay slopes match and cliff walls
    /// on higher terrain above the shore are placed (multi-segment stacks when drop &gt; 1).
    /// Includes (1) all non-water cells in the Moore neighborhood of water and (2) one extra ring of land neighbors
    /// of those cells so rim tiles (Chebyshev distance 2 from water) still run <see cref="UpdateTileElevation"/>.
    /// Call after <see cref="WaterManager.UpdateWaterVisuals"/>.
    /// </summary>
    public void RefreshLakeShoreAfterLakePlacement(WaterManager wm)
    {
        if (heightMap == null || gridManager == null || wm == null)
            return;

        WaterMap wmMap = wm.GetWaterMap();
        if (wmMap == null)
            return;

        var shore = new HashSet<Vector2Int>();
        for (int x = 0; x < gridManager.width; x++)
        {
            for (int y = 0; y < gridManager.height; y++)
            {
                if (!wmMap.IsWater(x, y))
                    continue;
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0)
                            continue;
                        int nx = x + dx;
                        int ny = y + dy;
                        if (!heightMap.IsValidPosition(nx, ny))
                            continue;
                        if (!wmMap.IsWater(nx, ny))
                            shore.Add(new Vector2Int(nx, ny));
                    }
                }
            }
        }

        var toRefresh = new HashSet<Vector2Int>(shore);
        foreach (Vector2Int p in shore)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                        continue;
                    int nx = p.x + dx;
                    int ny = p.y + dy;
                    if (!heightMap.IsValidPosition(nx, ny))
                        continue;
                    if (wmMap.IsWater(nx, ny))
                        continue;
                    toRefresh.Add(new Vector2Int(nx, ny));
                }
            }
        }

        foreach (Vector2Int p in toRefresh)
            UpdateTileElevation(p.x, p.y);
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

        // Land cell adjacent to water: use water slope, not land slope or grass (avoids black voids at coast).
        // Skip for registered water cells (lake/sea interior): those are not shore tiles; water visuals come from WaterManager.
        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();
        bool isLandShoreAdjacentToWater = newHeight >= 1 && IsAdjacentToWaterHeight(x, y)
            && !(waterManager != null && waterManager.IsWaterAt(x, y));
        if (isLandShoreAdjacentToWater)
        {
            List<GameObject> waterShorePrefabs = DetermineWaterShorePrefabs(x, y);
            LogShorePrefabSelectionDebug(x, y, waterShorePrefabs);
            if (waterShorePrefabs != null && waterShorePrefabs.Count > 0)
            {
                PlaceWaterShore(x, y, waterShorePrefabs);
                // Cliff prefabs belong on the higher land cell facing the shore, not on the water-slope tile.
                return;
            }
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
    /// Cut-through boundaries: Phase 3 refreshes neighbors of flattened path cells. Those neighbors
    /// have mixed flattened/non-flattened neighbors; RequiresSlope and DetermineSlopePrefab use the
    /// updated heightmap so slope selection matches the new landscape. forceFlat/forceSlopeType are
    /// used for path and adjacent cells in Phase 2; Phase 3 neighbors use live heightmap.
    /// </summary>
    /// <param name="useHeightMap">If non-null, this map is used (and assigned to heightMap) so restore works even when instance field was null.</param>
    /// <param name="forceFlat">When true, use flat terrain regardless of neighbor heights. Used for terraformed path transition cells.</param>
    /// <param name="forceSlopeType">When set, use this orthogonal slope prefab instead of DetermineSlopePrefab. Used for terraformed path slope cells.</param>
    /// <param name="terraformCutCorridorCells">When non-null (cut-through apply), places land–land cliff walls for 1-step drops toward these cells (BUG-29).</param>
    public bool RestoreTerrainForCell(int x, int y, HeightMap useHeightMap = null, bool forceFlat = false, TerrainSlopeType? forceSlopeType = null, ISet<Vector2Int> terraformCutCorridorCells = null)
    {
        if (useHeightMap != null)
            heightMap = useHeightMap;
        if (heightMap == null)
        {
            EnsureHeightMapLoaded();
        }
        if (heightMap == null)
        {
            if (LogTerraformRestoreDiagnostics)
                Debug.LogWarning($"[Terrain] RestoreTerrainForCell({x},{y}): heightMap null");
            return false;
        }
        if (!heightMap.IsValidPosition(x, y))
        {
            if (LogTerraformRestoreDiagnostics)
                Debug.LogWarning($"[Terrain] RestoreTerrainForCell({x},{y}): invalid position");
            return false;
        }
        int newHeight = heightMap.GetHeight(x, y);
        if (newHeight == SEA_LEVEL)
            return false;
        Cell cell = gridManager.GetCell(x, y);
        if (cell == null)
        {
            if (LogTerraformRestoreDiagnostics)
                Debug.LogWarning($"[Terrain] RestoreTerrainForCell({x},{y}): Cell null");
            return false;
        }

        if (CellHasZoningOverlay(cell))
        {
            if (LogTerraformRestoreDiagnostics)
                Debug.LogWarning($"[Terrain] RestoreTerrainForCell({x},{y}): skipped — zoning overlay");
            return false;
        }

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

        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();
        // Land cell adjacent to water must restore water slope; skip open-water cells (same rule as UpdateTileElevation).
        bool landShoreRestore = newHeight >= 1 && adjacentWater && !forceFlat && !forceSlopeType.HasValue
            && !(waterManager != null && waterManager.IsWaterAt(x, y));
        if (landShoreRestore)
        {
            List<GameObject> waterShorePrefabs = DetermineWaterShorePrefabs(x, y);
            LogShorePrefabSelectionDebug(x, y, waterShorePrefabs);
            if (waterShorePrefabs != null && waterShorePrefabs.Count > 0)
                PlaceWaterShore(x, y, waterShorePrefabs);
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

        PlaceCliffWalls(x, y, terraformCutCorridorCells);
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
            if (cell.forestObject != null && child.gameObject == cell.forestObject)
                continue;

            Zone zone = child.GetComponent<Zone>();
            if (zone != null && zone.zoneType == Zone.ZoneType.Grass)
                toDestroy.Add(child.gameObject);
            else if (zone != null && zone.zoneCategory == Zone.ZoneCategory.Grass)
                toDestroy.Add(child.gameObject);
            else if (IsWaterSlopeObject(child.gameObject) || IsLandSlopeObject(child.gameObject) || IsBayObject(child.gameObject))
                toDestroy.Add(child.gameObject);
            else if (zone == null && child.GetComponent<SpriteRenderer>() != null
                     && !IsWaterSlopeObject(child.gameObject)
                     && !IsLandSlopeObject(child.gameObject)
                     && !IsBayObject(child.gameObject)
                     && !IsSeaLevelWaterObject(child.gameObject))
            {
                // Legacy grass / terrain tiles without Zone on root (PlaceFlatTerrain now adds Zone; old instances must still be cleared for shores).
                toDestroy.Add(child.gameObject);
            }
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
        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                int nx = x + dx;
                int ny = y + dy;
                if (!heightMap.IsValidPosition(nx, ny))
                    continue;
                if (heightMap.GetHeight(nx, ny) == SEA_LEVEL)
                    return true;
                if (waterManager != null && waterManager.IsWaterAt(nx, ny))
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
    /// True for the <b>transition</b> tile that sits one step below inland land toward open water (cardinal sea or
    /// registered water) — this cell should host water-slope prefabs, not rim cliffs. A flat plateau rim next to a
    /// rectangular lake has the same height as land behind along the edge, so no cardinal land neighbor is strictly
    /// higher; those cells are <b>not</b> ramp tiles and can receive cliffs toward the water. (Cardinal-only water
    /// alone matched every fallback lake rim and suppressed all rim cliffs.)
    /// </summary>
    private bool IsWaterShoreRampTerrainCell(int x, int y, WaterManager wm = null)
    {
        if (heightMap == null || !heightMap.IsValidPosition(x, y))
            return false;
        int h = heightMap.GetHeight(x, y);
        if (h <= SEA_LEVEL)
            return false;
        if (wm == null)
            wm = waterManager != null ? waterManager : FindObjectOfType<WaterManager>();
        if (wm != null && wm.IsWaterAt(x, y))
            return false;

        bool hasCardinalWater = false;
        for (int i = 0; i < 4; i++)
        {
            int nx = x + CardinalDx[i];
            int ny = y + CardinalDy[i];
            if (!heightMap.IsValidPosition(nx, ny))
                continue;
            if (heightMap.GetHeight(nx, ny) == SEA_LEVEL)
            {
                hasCardinalWater = true;
                break;
            }
            if (wm != null && wm.IsWaterAt(nx, ny))
            {
                hasCardinalWater = true;
                break;
            }
        }
        if (!hasCardinalWater)
            return false;

        for (int i = 0; i < 4; i++)
        {
            int nx = x + CardinalDx[i];
            int ny = y + CardinalDy[i];
            if (!heightMap.IsValidPosition(nx, ny))
                continue;
            int nh = heightMap.GetHeight(nx, ny);
            if (nh <= SEA_LEVEL)
                continue;
            if (wm != null && wm.IsWaterAt(nx, ny))
                continue;
            if (nh > h)
                return true;
        }
        return false;
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

        Zone zoneComponent = zoneTile.GetComponent<Zone>();
        if (zoneComponent == null)
        {
            zoneComponent = zoneTile.AddComponent<Zone>();
            zoneComponent.zoneType = Zone.ZoneType.Grass;
            zoneComponent.zoneCategory = Zone.ZoneCategory.Grass;
        }

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

        int slopeOrder = CalculateSlopeSortingOrder(x, y, currentHeight);
        SpriteRenderer sr = slope.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.sortingOrder = slopeOrder;
        cell.SetCellInstanceSortingOrder(slopeOrder);
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
                    List<GameObject> waterShorePrefabs = DetermineWaterShorePrefabs(nx, ny);
                    LogShorePrefabSelectionDebug(nx, ny, waterShorePrefabs);
                    if (waterShorePrefabs == null || waterShorePrefabs.Count == 0)
                        continue;
                    PlaceWaterShore(nx, ny, waterShorePrefabs);
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

                        List<GameObject> waterShorePrefabs = DetermineWaterShorePrefabs(nx, ny);
                        LogShorePrefabSelectionDebug(nx, ny, waterShorePrefabs);
                        if (waterShorePrefabs != null && waterShorePrefabs.Count > 0)
                            PlaceWaterShore(nx, ny, waterShorePrefabs);
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
        updatedCell.SetCellInstanceSortingOrder(sortingOrder);
        updatedCell.prefabName = seaLevelWater.name;
        updatedCell.buildingType = seaLevelWater.name;
        updatedCell.waterBodyType = WaterBodyType.Sea;

        if (waterManager != null)
            waterManager.TryRegisterSeaLevelWaterCell(x, y);
    }

    /// <summary>
    /// Instantiates one or more lake/coast shore prefabs (cardinal, Bay, or upslope+downslope pair) as children of the cell.
    /// </summary>
    private void PlaceWaterShore(int x, int y, List<GameObject> waterShorePrefabs)
    {
        if (waterShorePrefabs == null || waterShorePrefabs.Count == 0)
            return;

        Cell cell = gridManager.GetCell(x, y);
        DestroyTerrainChildrenOnly(cell);

        int landH = heightMap != null ? heightMap.GetHeight(x, y) : 1;
        if (landH <= SEA_LEVEL)
            landH = 1;

        gridManager.SetCellHeight(new Vector2(x, y), landH);
        Cell updatedCell = gridManager.GetCell(x, y);

        Vector2 cellWorldPos = gridManager.GetWorldPositionVector(x, y, landH);
        cell.gameObject.transform.position = cellWorldPos;
        updatedCell.transformPosition = cellWorldPos;

        int waterVisualH = GetNeighborWaterVisualHeightForShore(x, y);

        GameObject primaryPrefab = waterShorePrefabs[0];
        bool primaryIsBay = primaryPrefab != null && IsBayShorePrefab(primaryPrefab);

        for (int i = 0; i < waterShorePrefabs.Count; i++)
        {
            GameObject prefab = waterShorePrefabs[i];
            if (prefab == null)
                continue;

            Vector2 slopeWorldPos = gridManager.GetWorldPositionVector(x, y, waterVisualH);
            float extraWorldY = GetLakeShoreExtraWorldYOffset(prefab, landH, waterVisualH, waterShorePrefabs.Count);
            if (extraWorldY != 0f)
                slopeWorldPos += new Vector2(0f, extraWorldY);

            GameObject slope = Instantiate(prefab, slopeWorldPos, Quaternion.identity);
            slope.SetActive(true);
            slope.transform.SetParent(cell.gameObject.transform, true);

            bool isBay = IsBayShorePrefab(prefab);
            int sortingOrder = isBay
                ? CalculateBayShoreSortingOrder(x, y)
                : CalculateWaterSlopeSortingOrder(x, y);
            if (i > 0)
                sortingOrder -= 1;

            SpriteRenderer sr = slope.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.sortingOrder = sortingOrder;
        }

        if (primaryPrefab != null)
            updatedCell.prefabName = primaryPrefab.name;

        updatedCell.secondaryPrefabName = "";
        if (waterShorePrefabs.Count > 1 && waterShorePrefabs[1] != null)
            updatedCell.secondaryPrefabName = waterShorePrefabs[1].name;

        int primarySort = primaryIsBay
            ? CalculateBayShoreSortingOrder(x, y)
            : CalculateWaterSlopeSortingOrder(x, y);
        updatedCell.sortingOrder = primarySort;
        updatedCell.SetCellInstanceSortingOrder(primarySort);
    }

    /// <summary>
    /// Restores lake/coast shore visuals from saved prefab names (load path).
    /// Sorting uses the same formulas as the private PlaceWaterShore path (Bay / water-slope orders). The savedPrimarySort argument is kept for call-site compatibility with older saves and is not applied.
    /// </summary>
    public void RestoreWaterShorePrefabsFromSave(int x, int y, string primaryName, string secondaryName, int savedPrimarySort)
    {
        var list = new List<GameObject>();
        if (!string.IsNullOrEmpty(primaryName))
        {
            GameObject p = FindTerrainPrefabByName(primaryName);
            if (p != null) list.Add(p);
        }
        if (!string.IsNullOrEmpty(secondaryName))
        {
            GameObject p = FindTerrainPrefabByName(secondaryName);
            if (p != null) list.Add(p);
        }
        if (list.Count == 0)
            return;

        PlaceWaterShore(x, y, list);
    }

    /// <param name="terraformCutCorridorCells">Cells lowered by cut-through terraform; enables 1-step land–land cliff faces toward the corridor (BUG-29).</param>
    private void PlaceCliffWalls(int x, int y, ISet<Vector2Int> terraformCutCorridorCells = null)
    {
        int currentHeight = heightMap.GetHeight(x, y);
        if (currentHeight <= SEA_LEVEL)
        {
            return;
        }

        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();
        if (waterManager != null && waterManager.IsWaterAt(x, y))
            return;

        if (IsCellSurroundedByCardinalWaterOnly(x, y, waterManager))
            return;

        Cell cell = gridManager.GetCell(x, y);
        RemoveExistingCliffWalls(cell);

        int dSouth = GetCliffWallDropSouth(x, y, currentHeight, terraformCutCorridorCells);
        if (dSouth > 0)
            PlaceCliffWallStack(cell, southCliffWallPrefab, x, y, x - 1, y, currentHeight, heightMap.GetHeight(x - 1, y), dSouth);

        int dEast = GetCliffWallDropEast(x, y, currentHeight, terraformCutCorridorCells);
        if (dEast > 0)
            PlaceCliffWallStack(cell, eastCliffWallPrefab, x, y, x, y - 1, currentHeight, heightMap.GetHeight(x, y - 1), dEast);

        int dNorth = GetCliffWallDropNorth(x, y, currentHeight, terraformCutCorridorCells);
        if (dNorth > 0)
            PlaceCliffWallStack(cell, northCliffWallPrefab, x, y, x + 1, y, currentHeight, heightMap.GetHeight(x + 1, y), dNorth);

        int dWest = GetCliffWallDropWest(x, y, currentHeight, terraformCutCorridorCells);
        if (dWest > 0)
            PlaceCliffWallStack(cell, westCliffWallPrefab, x, y, x, y + 1, currentHeight, heightMap.GetHeight(x, y + 1), dWest);
    }

    /// <summary>
    /// True when all four cardinals are sea or registered water (e.g. a 1×1 island) — no vertical cliff ring.
    /// </summary>
    private bool IsCellSurroundedByCardinalWaterOnly(int x, int y, WaterManager wm)
    {
        if (heightMap == null || !heightMap.IsValidPosition(x, y))
            return false;
        if (wm == null)
            wm = waterManager != null ? waterManager : FindObjectOfType<WaterManager>();
        for (int i = 0; i < 4; i++)
        {
            int nx = x + CardinalDx[i];
            int ny = y + CardinalDy[i];
            if (!heightMap.IsValidPosition(nx, ny))
                return false;
            bool water = heightMap.GetHeight(nx, ny) == SEA_LEVEL || (wm != null && wm.IsWaterAt(nx, ny));
            if (!water)
                return false;
        }
        return true;
    }

    /// <summary>
    /// One-step drop toward open water only: suppress a cliff on the narrow shore strip (height-1 band with higher land
    /// behind). Multi-step drops toward water are not suppressed — those need stacked cliff segments (fallback rims).
    /// </summary>
    private bool ShouldSuppressCliffTowardCardinalLower(int x, int y, int lowerX, int lowerY, int currentHeight, int lowerHeight)
    {
        if (currentHeight - lowerHeight != 1)
            return false;
        if (heightMap == null || !heightMap.IsValidPosition(x, y) || !heightMap.IsValidPosition(lowerX, lowerY))
            return false;
        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();
        bool lowerIsWaterSurface = lowerHeight <= SEA_LEVEL
            || (waterManager != null && waterManager.IsWaterAt(lowerX, lowerY));
        if (!lowerIsWaterSurface)
            return false;
        int inlandX = x + (x - lowerX);
        int inlandY = y + (y - lowerY);
        if (!heightMap.IsValidPosition(inlandX, inlandY))
            return false;
        int inlandH = heightMap.GetHeight(inlandX, inlandY);
        return inlandH > currentHeight;
    }

    /// <summary>
    /// One-step drop from higher land down to a <see cref="IsWaterSlopeCell"/> neighbor (shore / water-border land).
    /// Cliffs belong on the <b>upper</b> cell facing the shore, not on the shore tile facing water (which already has water-slope prefabs).
    /// </summary>
    private bool NeedsCliffWallOneStepAboveWaterSlopeNeighbor(int cellX, int cellY, int neighborX, int neighborY, int currentHeight, int neighborHeight)
    {
        if (currentHeight - neighborHeight != 1)
            return false;
        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();
        // Exclude only the one-step-down transition tile (water-slope), not flat plateau rim (same height as land behind).
        if (IsWaterShoreRampTerrainCell(cellX, cellY, waterManager))
            return false;
        if (!IsWaterSlopeCell(neighborX, neighborY))
            return false;
        return true;
    }

    /// <summary>Number of stacked cliff segments on the south face (0 = none). Higher cell is (x,y); lower is south.</summary>
    private int GetCliffWallDropSouth(int x, int y, int currentHeight, ISet<Vector2Int> terraformCutCorridorCells = null)
    {
        if (!heightMap.IsValidPosition(x - 1, y))
            return 0;
        int heightAtSouth = heightMap.GetHeight(x - 1, y);
        if (heightAtSouth >= currentHeight)
            return 0;
        int diff = currentHeight - heightAtSouth;
        if (diff > 1)
            return diff;
        if (ShouldSuppressCliffTowardCardinalLower(x, y, x - 1, y, currentHeight, heightAtSouth))
            return 0;
        if (NeedsCutThroughOneStepCliffToCorridor(terraformCutCorridorCells, currentHeight, heightAtSouth, x - 1, y))
            return 1;
        if (NeedsCliffWallOneStepAboveWaterSlopeNeighbor(x, y, x - 1, y, currentHeight, heightAtSouth))
            return 1;
        return 0;
    }

    private int GetCliffWallDropEast(int x, int y, int currentHeight, ISet<Vector2Int> terraformCutCorridorCells = null)
    {
        if (!heightMap.IsValidPosition(x, y - 1))
            return 0;
        int heightAtEast = heightMap.GetHeight(x, y - 1);
        if (heightAtEast >= currentHeight)
            return 0;
        int diff = currentHeight - heightAtEast;
        if (diff > 1)
            return diff;
        if (ShouldSuppressCliffTowardCardinalLower(x, y, x, y - 1, currentHeight, heightAtEast))
            return 0;
        if (NeedsCutThroughOneStepCliffToCorridor(terraformCutCorridorCells, currentHeight, heightAtEast, x, y - 1))
            return 1;
        if (NeedsCliffWallOneStepAboveWaterSlopeNeighbor(x, y, x, y - 1, currentHeight, heightAtEast))
            return 1;
        return 0;
    }

    private int GetCliffWallDropNorth(int x, int y, int currentHeight, ISet<Vector2Int> terraformCutCorridorCells = null)
    {
        if (!heightMap.IsValidPosition(x + 1, y))
            return 0;
        int heightAtNorth = heightMap.GetHeight(x + 1, y);
        if (heightAtNorth >= currentHeight)
            return 0;
        int diff = currentHeight - heightAtNorth;
        if (diff > 1)
            return diff;
        if (ShouldSuppressCliffTowardCardinalLower(x, y, x + 1, y, currentHeight, heightAtNorth))
            return 0;
        if (NeedsCutThroughOneStepCliffToCorridor(terraformCutCorridorCells, currentHeight, heightAtNorth, x + 1, y))
            return 1;
        if (NeedsCliffWallOneStepAboveWaterSlopeNeighbor(x, y, x + 1, y, currentHeight, heightAtNorth))
            return 1;
        return 0;
    }

    private int GetCliffWallDropWest(int x, int y, int currentHeight, ISet<Vector2Int> terraformCutCorridorCells = null)
    {
        if (!heightMap.IsValidPosition(x, y + 1))
            return 0;
        int heightAtWest = heightMap.GetHeight(x, y + 1);
        if (heightAtWest >= currentHeight)
            return 0;
        int diff = currentHeight - heightAtWest;
        if (diff > 1)
            return diff;
        if (ShouldSuppressCliffTowardCardinalLower(x, y, x, y + 1, currentHeight, heightAtWest))
            return 0;
        if (NeedsCutThroughOneStepCliffToCorridor(terraformCutCorridorCells, currentHeight, heightAtWest, x, y + 1))
            return 1;
        if (NeedsCliffWallOneStepAboveWaterSlopeNeighbor(x, y, x, y + 1, currentHeight, heightAtWest))
            return 1;
        return 0;
    }

    /// <summary>
    /// Cut-through only: 1-step land drop into the lowered corridor gets a cliff wall (avoids black voids at rim).
    /// </summary>
    static bool NeedsCutThroughOneStepCliffToCorridor(ISet<Vector2Int> terraformCutCorridorCells, int currentHeight, int neighborHeight, int nx, int ny)
    {
        if (terraformCutCorridorCells == null || !terraformCutCorridorCells.Contains(new Vector2Int(nx, ny)))
            return false;
        if (neighborHeight <= SEA_LEVEL)
            return false;
        return currentHeight - neighborHeight == 1 && neighborHeight < currentHeight;
    }

    /// <summary>
    /// Stacks <paramref name="segmentCount"/> cliff sprites along the vertical face from <paramref name="highH"/> down to
    /// <paramref name="lowH"/> on the lower neighbor cell (one prefab per height unit of drop).
    /// </summary>
    private void PlaceCliffWallStack(Cell cell, GameObject prefab, int highX, int highY, int lowX, int lowY, int highH, int lowH, int segmentCount)
    {
        if (prefab == null || gridManager == null || heightMap == null || segmentCount <= 0)
            return;
        if (highH <= lowH)
            return;

        float z = cell.gameObject.transform.position.z;
        const float edgeBlend = 1.0f;
        int d = highH - lowH;
        int count = Mathf.Min(segmentCount, d);
        for (int s = 0; s < count; s++)
        {
            int topH = highH - s;
            int bottomH = (s < count - 1) ? (highH - s - 1) : lowH;
            Vector2 topCenter = gridManager.GetWorldPositionVector(highX, highY, topH);
            Vector2 bottomCenter = (s < count - 1)
                ? gridManager.GetWorldPositionVector(highX, highY, bottomH)
                : gridManager.GetWorldPositionVector(lowX, lowY, lowH);
            Vector2 world = Vector2.Lerp(topCenter, bottomCenter, edgeBlend);

            GameObject cliffWall = Instantiate(prefab, new Vector3(world.x, world.y, z), Quaternion.identity);
            cliffWall.transform.SetParent(cell.gameObject.transform, true);

            SpriteRenderer sr = cliffWall.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.sortingOrder = CalculateTerrainSortingOrder(highX, highY, topH) + SLOPE_OFFSET + s;
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

    /// <summary>
    /// True if the neighbor cell is sea-level terrain or registered water (lake/sea in <see cref="WaterManager"/>).
    /// Used for shore prefab selection at any terrain height, not only height 0.
    /// </summary>
    private bool WaterOrSeaAt(int nx, int ny)
    {
        if (heightMap == null || !heightMap.IsValidPosition(nx, ny))
            return false;
        if (heightMap.GetHeight(nx, ny) == SEA_LEVEL)
            return true;
        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();
        return waterManager != null && waterManager.IsWaterAt(nx, ny);
    }

    /// <summary>
    /// Visual height index used for water sprites at <paramref name="nx"/>, <paramref name="ny"/> (logical surface minus one).
    /// </summary>
    private int GetWaterVisualHeightForNeighborCell(int nx, int ny)
    {
        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();
        int surf;
        if (waterManager != null && waterManager.IsWaterAt(nx, ny))
        {
            surf = waterManager.GetWaterSurfaceHeight(nx, ny);
            if (surf < 0)
                surf = waterManager.seaLevel;
        }
        else
            surf = SEA_LEVEL;
        return Mathf.Max(SEA_LEVEL, surf - 1);
    }

    /// <summary>
    /// World height index for water visuals adjacent to this shore cell: logical surface minus one (see WaterManager.PlaceWater).
    /// Uses the minimum among cardinal water/sea neighbors; if none, falls back to diagonal neighbors (external lake corners).
    /// </summary>
    private int GetNeighborWaterVisualHeightForShore(int x, int y)
    {
        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();
        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };
        int best = int.MaxValue;
        for (int i = 0; i < 4; i++)
        {
            int nx = x + dx[i];
            int ny = y + dy[i];
            if (!WaterOrSeaAt(nx, ny))
                continue;
            int vis = GetWaterVisualHeightForNeighborCell(nx, ny);
            if (vis < best)
                best = vis;
        }
        if (best == int.MaxValue)
        {
            int[] ddx = { 1, 1, -1, -1 };
            int[] ddy = { -1, 1, -1, 1 };
            for (int i = 0; i < 4; i++)
            {
                int nx = x + ddx[i];
                int ny = y + ddy[i];
                if (!WaterOrSeaAt(nx, ny))
                    continue;
                int vis = GetWaterVisualHeightForNeighborCell(nx, ny);
                if (vis < best)
                    best = vis;
            }
        }
        return best == int.MaxValue ? SEA_LEVEL : best;
    }

    /// <summary>
    /// True when this shore cell sits on a terrain slope toward a non-water neighbor (cardinal land higher than this cell).
    /// Used to choose upslope+downslope water pair vs Bay on diagonal-only water patterns.
    /// </summary>
    private bool HasLandSlopeIgnoringWater(int x, int y)
    {
        if (heightMap == null || !heightMap.IsValidPosition(x, y))
            return false;
        int h = heightMap.GetHeight(x, y);
        int[] ddx = { 1, -1, 0, 0 };
        int[] ddy = { 0, 0, 1, -1 };
        for (int i = 0; i < 4; i++)
        {
            int nx = x + ddx[i];
            int ny = y + ddy[i];
            if (!heightMap.IsValidPosition(nx, ny))
                continue;
            if (WaterOrSeaAt(nx, ny))
                continue;
            if (heightMap.GetHeight(nx, ny) > h)
                return true;
        }
        return false;
    }

    /// <summary>
    /// True when <paramref name="prefab"/> is one of the shore corner (Bay) prefabs — inner 90° (two cardinals water) or external rectangular-lake corners.
    /// </summary>
    private bool IsBayShorePrefab(GameObject prefab)
    {
        if (prefab == null)
            return false;
        return prefab == northEastBayPrefab
            || prefab == northWestBayPrefab
            || prefab == southEastBayPrefab
            || prefab == southWestBayPrefab;
    }

    /// <summary>
    /// Diagonal shore tiles (NE/NW/SE/SW Upslope or SlopeWater), not Bay — used for Y offset and diagonal downslope selection.
    /// </summary>
    private bool IsDiagonalShoreWaterPrefab(GameObject prefab)
    {
        if (prefab == null)
            return false;
        return prefab == northEastUpslopeWaterPrefab
            || prefab == northWestUpslopeWaterPrefab
            || prefab == southEastUpslopeWaterPrefab
            || prefab == southWestUpslopeWaterPrefab
            || prefab == northEastSlopeWaterPrefab
            || prefab == northWestSlopeWaterPrefab
            || prefab == southEastSlopeWaterPrefab
            || prefab == southWestSlopeWaterPrefab;
    }

    /// <summary>
    /// True for NE/NW/SE/SW <c>*SlopeWaterPrefab</c> only (not Upslope). Used for flat-lake corner placement vs upslope+downslope pairs.
    /// </summary>
    private bool IsCornerSlopeWaterPrefab(GameObject prefab)
    {
        if (prefab == null)
            return false;
        return prefab == northEastSlopeWaterPrefab
            || prefab == northWestSlopeWaterPrefab
            || prefab == southEastSlopeWaterPrefab
            || prefab == southWestSlopeWaterPrefab;
    }

    /// <summary>
    /// Extra world Y for lake shore child sprites vs <see cref="GridManager.GetWorldPositionVector"/> at water visual height.
    /// Bay: 0 when <c>landH − waterVisualH ≤ 1</c> (flat rim); same terrain-step nudge as diagonals when delta &gt; 1 (deep bowls / cliffs).
    /// Standalone corner SlopeWater: 0 (same water plane as neighbors). Diagonal Upslope or upslope+downslope pairs:
    /// <c>(landH − waterVisualH) × tileHeight × 0.25</c>. Cardinal slopes: 0.
    /// </summary>
    /// <param name="shorePrefabCount">Number of shore prefabs placed together (2 = upslope+downslope pair).</param>
    private float GetLakeShoreExtraWorldYOffset(GameObject prefab, int landH, int waterVisualH, int shorePrefabCount)
    {
        if (prefab == null || gridManager == null)
            return 0f;

        if (IsBayShorePrefab(prefab))
        {
            int delta = Mathf.Max(0, landH - waterVisualH);
            if (delta <= 1)
                return 0f;
            return delta * gridManager.tileHeight * 0.25f;
        }

        if (IsDiagonalShoreWaterPrefab(prefab))
        {
            // Single corner SlopeWater fills the same role as Bay on a flat lake surface; skip terrain-step nudge.
            if (shorePrefabCount == 1 && IsCornerSlopeWaterPrefab(prefab))
                return 0f;

            int delta = Mathf.Max(0, landH - waterVisualH);
            return delta * gridManager.tileHeight * 0.25f;
        }

        return 0f;
    }

    /// <summary>
    /// Sorting order of the water surface sprite on <paramref name="nx"/>, <paramref name="ny"/>,
    /// matching lake <c>PlaceWater</c> and sea-level water tiles (for Bay vs neighbor overlap).
    /// </summary>
    private int GetWaterNeighborTileSortingOrder(int nx, int ny)
    {
        if (heightMap == null || !heightMap.IsValidPosition(nx, ny))
            return int.MinValue;
        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();

        if (waterManager != null && waterManager.IsWaterAt(nx, ny))
        {
            int surf = waterManager.GetWaterSurfaceHeight(nx, ny);
            if (surf < 0)
                surf = waterManager.seaLevel;
            int visualSurfaceHeight = Mathf.Max(MIN_HEIGHT, surf - 1);
            return CalculateTerrainSortingOrder(nx, ny, visualSurfaceHeight);
        }

        if (heightMap.GetHeight(nx, ny) == SEA_LEVEL)
            return CalculateTerrainSortingOrder(nx, ny, SEA_LEVEL);

        return int.MinValue;
    }

    /// <summary>
    /// Debug-only: logs chosen lake shore prefab(s) for cells under investigation.
    /// </summary>
    private void LogShorePrefabSelectionDebug(int x, int y, IList<GameObject> terrainPrefabs)
    {
        if ((x != 10 || y != 19) && (x != 11 || y != 20) && (x != 12 || y != 22) && (x != 8 || y != 19))
            return;

        int h = -1;
        if (heightMap != null && heightMap.IsValidPosition(x, y))
            h = heightMap.GetHeight(x, y);
        else if (gridManager != null)
        {
            Cell c = gridManager.GetCell(x, y);
            if (c != null)
                h = c.height;
        }

        string prefabStr = "null";
        if (terrainPrefabs != null && terrainPrefabs.Count > 0)
        {
            var names = new List<string>();
            foreach (GameObject p in terrainPrefabs)
                names.Add(p != null ? p.name : "null");
            prefabStr = string.Join(", ", names);
        }
        Debug.Log($"cell ({x},{y}) has terrainPrefab: {prefabStr}, height: {h}");
    }

    private static List<GameObject> ShoreList(GameObject prefab)
    {
        if (prefab == null)
            return null;
        return new List<GameObject> { prefab };
    }

    /// <summary>
    /// Diagonal-only water at shore. <b>Priority:</b> (1) axis-aligned rectangle outer corner → single Bay;
    /// (2) <see cref="HasLandSlopeIgnoringWater"/> → single Bay (cliff / higher land rim matches flat-lake concave corners); else downslope if no Bay;
    /// (3) otherwise single Bay (flat terrain, diagonal lake edge); (4) fallback upslope+downslope pair.
    /// Rectangle corners must win over land-slope so a higher land neighbor does not force the companion pair on straight corners.
    /// </summary>
    private List<GameObject> BuildDiagonalOnlyShorePrefabs(int x, int y, GameObject bayPrefab, GameObject upslopePrefab, GameObject downslopePrefab, bool isAxisAlignedRectangleCornerWater)
    {
        if (isAxisAlignedRectangleCornerWater && bayPrefab != null)
            return new List<GameObject> { bayPrefab };
        if (HasLandSlopeIgnoringWater(x, y))
        {
            if (bayPrefab != null)
                return ShoreList(bayPrefab);
            if (downslopePrefab != null)
                return ShoreList(downslopePrefab);
            // No Bay/downslope: fall through to pair — do not use upslope alone at cliff rim.
        }
        if (bayPrefab != null)
            return new List<GameObject> { bayPrefab };
        var pair = new List<GameObject>();
        if (upslopePrefab != null)
            pair.Add(upslopePrefab);
        if (downslopePrefab != null)
            pair.Add(downslopePrefab);
        return pair.Count > 0 ? pair : null;
    }

    /// <summary>
    /// True when diagonal water at NE of shore is the outer corner of an axis-aligned rectangle: no water further North or East of W.
    /// </summary>
    private bool IsAxisAlignedRectangleCornerWaterNorthEast(int x, int y)
    {
        int wx = x + 1, wy = y - 1;
        if (!WaterOrSeaAt(wx, wy))
            return false;
        return !WaterOrSeaAt(wx + 1, wy) && !WaterOrSeaAt(wx, wy - 1);
    }

    /// <summary>
    /// True when diagonal water at NW of shore is the outer corner of an axis-aligned rectangle: no water further North or West of W.
    /// </summary>
    private bool IsAxisAlignedRectangleCornerWaterNorthWest(int x, int y)
    {
        int wx = x + 1, wy = y + 1;
        if (!WaterOrSeaAt(wx, wy))
            return false;
        return !WaterOrSeaAt(wx + 1, wy) && !WaterOrSeaAt(wx, wy + 1);
    }

    /// <summary>
    /// True when diagonal water at SE of shore is the outer corner of an axis-aligned rectangle: no water further South or East of W.
    /// </summary>
    private bool IsAxisAlignedRectangleCornerWaterSouthEast(int x, int y)
    {
        int wx = x - 1, wy = y - 1;
        if (!WaterOrSeaAt(wx, wy))
            return false;
        return !WaterOrSeaAt(wx - 1, wy) && !WaterOrSeaAt(wx, wy - 1);
    }

    /// <summary>
    /// True when diagonal water at SW of shore is the outer corner of an axis-aligned rectangle: no water further South or West of W.
    /// </summary>
    private bool IsAxisAlignedRectangleCornerWaterSouthWest(int x, int y)
    {
        int wx = x - 1, wy = y + 1;
        if (!WaterOrSeaAt(wx, wy))
            return false;
        return !WaterOrSeaAt(wx - 1, wy) && !WaterOrSeaAt(wx, wy + 1);
    }

    /// <summary>
    /// Selects lake/coast shore prefab(s) for a land cell adjacent to water. Returns one prefab or an upslope+downslope pair for diagonal slopes.
    /// Perpendicular two-cardinal corners: Bay when the diagonal water cell is an axis-aligned rectangle outer corner; when not, prefer Bay if
    /// <see cref="HasLandSlopeIgnoringWater"/> (cliff rim), else SlopeWater then Bay (convex land tip / large-lake shore).
    /// </summary>
    private List<GameObject> DetermineWaterShorePrefabs(int x, int y)
    {
        if (heightMap == null)
            return null;
        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();

        bool hasWaterAtNorth = WaterOrSeaAt(x + 1, y);
        bool hasWaterAtSouth = WaterOrSeaAt(x - 1, y);
        bool hasWaterAtWest = WaterOrSeaAt(x, y + 1);
        bool hasWaterAtEast = WaterOrSeaAt(x, y - 1);

        bool hasWaterAtNorthEast = WaterOrSeaAt(x + 1, y - 1);
        bool hasWaterAtNorthWest = WaterOrSeaAt(x + 1, y + 1);
        bool hasWaterAtSouthEast = WaterOrSeaAt(x - 1, y - 1);
        bool hasWaterAtSouthWest = WaterOrSeaAt(x - 1, y + 1);

        bool isAtNorthBorder = !heightMap.IsValidPosition(x + 1, y);
        bool isAtSouthBorder = !heightMap.IsValidPosition(x - 1, y);
        bool isAtWestBorder = !heightMap.IsValidPosition(x, y + 1);
        bool isAtEastBorder = !heightMap.IsValidPosition(x, y - 1);

        if (isAtSouthBorder)
        {
            if (hasWaterAtWest) return ShoreList(westSlopeWaterPrefab);
            if (hasWaterAtEast) return ShoreList(eastSlopeWaterPrefab);
            if (hasWaterAtNorth) return ShoreList(northSlopeWaterPrefab);
        }

        if (isAtNorthBorder)
        {
            if (hasWaterAtWest) return ShoreList(westSlopeWaterPrefab);
            if (hasWaterAtEast) return ShoreList(eastSlopeWaterPrefab);
            if (hasWaterAtSouth) return ShoreList(southSlopeWaterPrefab);
        }

        if (isAtWestBorder)
        {
            if (hasWaterAtNorth) return ShoreList(northSlopeWaterPrefab);
            if (hasWaterAtSouth) return ShoreList(southSlopeWaterPrefab);
            if (hasWaterAtEast) return ShoreList(eastSlopeWaterPrefab);
        }

        if (isAtEastBorder)
        {
            if (hasWaterAtNorth) return ShoreList(northSlopeWaterPrefab);
            if (hasWaterAtSouth) return ShoreList(southSlopeWaterPrefab);
            if (hasWaterAtWest) return ShoreList(westSlopeWaterPrefab);
        }

        // Perpendicular shore corners: both cardinals of a quadrant have water.
        // Bay = concave water corner (outer axis-aligned corner of the water patch: see IsAxisAlignedRectangleCornerWater*).
        // SlopeWater = convex land corner when water continues past the diagonal (peninsula tip, island corners, large lakes).
        // When not a rectangle outer corner but a higher land neighbor exists (cliff rim), prefer Bay like flat lakes.
        // Order SE, SW, NE, NW so when three cardinals are water (e.g. N+E+S), one unambiguous tile wins (SE before NE).
        if (hasWaterAtSouth && hasWaterAtEast)
        {
            if (IsAxisAlignedRectangleCornerWaterSouthEast(x, y))
            {
                if (southEastBayPrefab != null) return ShoreList(southEastBayPrefab);
                if (southEastSlopeWaterPrefab != null) return ShoreList(southEastSlopeWaterPrefab);
            }
            else if (HasLandSlopeIgnoringWater(x, y))
            {
                if (southEastBayPrefab != null) return ShoreList(southEastBayPrefab);
                if (southEastSlopeWaterPrefab != null) return ShoreList(southEastSlopeWaterPrefab);
            }
            else
            {
                if (southEastSlopeWaterPrefab != null) return ShoreList(southEastSlopeWaterPrefab);
                if (southEastBayPrefab != null) return ShoreList(southEastBayPrefab);
            }
        }
        if (hasWaterAtSouth && hasWaterAtWest)
        {
            if (IsAxisAlignedRectangleCornerWaterSouthWest(x, y))
            {
                if (southWestBayPrefab != null) return ShoreList(southWestBayPrefab);
                if (southWestSlopeWaterPrefab != null) return ShoreList(southWestSlopeWaterPrefab);
            }
            else if (HasLandSlopeIgnoringWater(x, y))
            {
                if (southWestBayPrefab != null) return ShoreList(southWestBayPrefab);
                if (southWestSlopeWaterPrefab != null) return ShoreList(southWestSlopeWaterPrefab);
            }
            else
            {
                if (southWestSlopeWaterPrefab != null) return ShoreList(southWestSlopeWaterPrefab);
                if (southWestBayPrefab != null) return ShoreList(southWestBayPrefab);
            }
        }
        if (hasWaterAtNorth && hasWaterAtEast)
        {
            if (IsAxisAlignedRectangleCornerWaterNorthEast(x, y))
            {
                if (northEastBayPrefab != null) return ShoreList(northEastBayPrefab);
                if (northEastSlopeWaterPrefab != null) return ShoreList(northEastSlopeWaterPrefab);
            }
            else if (HasLandSlopeIgnoringWater(x, y))
            {
                if (northEastBayPrefab != null) return ShoreList(northEastBayPrefab);
                if (northEastSlopeWaterPrefab != null) return ShoreList(northEastSlopeWaterPrefab);
            }
            else
            {
                if (northEastSlopeWaterPrefab != null) return ShoreList(northEastSlopeWaterPrefab);
                if (northEastBayPrefab != null) return ShoreList(northEastBayPrefab);
            }
        }
        if (hasWaterAtNorth && hasWaterAtWest)
        {
            if (IsAxisAlignedRectangleCornerWaterNorthWest(x, y))
            {
                if (northWestBayPrefab != null) return ShoreList(northWestBayPrefab);
                if (northWestSlopeWaterPrefab != null) return ShoreList(northWestSlopeWaterPrefab);
            }
            else if (HasLandSlopeIgnoringWater(x, y))
            {
                if (northWestBayPrefab != null) return ShoreList(northWestBayPrefab);
                if (northWestSlopeWaterPrefab != null) return ShoreList(northWestSlopeWaterPrefab);
            }
            else
            {
                if (northWestSlopeWaterPrefab != null) return ShoreList(northWestSlopeWaterPrefab);
                if (northWestBayPrefab != null) return ShoreList(northWestBayPrefab);
            }
        }

        if (hasWaterAtEast)
        {
            if (!hasWaterAtSouth)
            {
                if (!hasWaterAtNorth)
                {
                    return ShoreList(eastSlopeWaterPrefab);
                }
                else
                {
                    return ShoreList(northEastUpslopeWaterPrefab);
                }
            }
            else
            {
                return ShoreList(southEastUpslopeWaterPrefab);
            }
        }

        if (hasWaterAtWest)
        {
            if (!hasWaterAtSouth)
            {
                if (!hasWaterAtNorth)
                {
                    return ShoreList(westSlopeWaterPrefab);
                }
                else
                {
                    return ShoreList(northWestUpslopeWaterPrefab);
                }
            }
            else
            {
                return ShoreList(southWestUpslopeWaterPrefab);
            }
        }

        // Water to the north: pure north face vs NE/NW corners; water north+south (E–W strip) uses upslope corner tiles like the east branch.
        if (hasWaterAtNorth)
        {
            if (!hasWaterAtSouth)
            {
                if (hasWaterAtEast && hasWaterAtWest)
                    return ShoreList(northSlopeWaterPrefab);
                if (hasWaterAtEast)
                    return ShoreList(northEastUpslopeWaterPrefab);
                if (hasWaterAtWest)
                    return ShoreList(northWestUpslopeWaterPrefab);
                return ShoreList(northSlopeWaterPrefab);
            }
            else
            {
                return ShoreList(southEastUpslopeWaterPrefab);
            }
        }

        // Water to the south only (water north is handled above): mirror East — pure south vs SW corner.
        if (hasWaterAtSouth)
        {
            if (!hasWaterAtNorth)
            {
                if (!hasWaterAtWest)
                    return ShoreList(southSlopeWaterPrefab);
                else
                    return ShoreList(southWestUpslopeWaterPrefab);
            }
        }

        if (hasWaterAtNorthEast && !hasWaterAtSouth)
            return BuildDiagonalOnlyShorePrefabs(x, y, northEastBayPrefab, northEastUpslopeWaterPrefab, northEastSlopeWaterPrefab, IsAxisAlignedRectangleCornerWaterNorthEast(x, y));

        if (hasWaterAtNorthWest && !hasWaterAtSouth)
            return BuildDiagonalOnlyShorePrefabs(x, y, northWestBayPrefab, northWestUpslopeWaterPrefab, northWestSlopeWaterPrefab, IsAxisAlignedRectangleCornerWaterNorthWest(x, y));

        if (hasWaterAtSouthEast && !hasWaterAtNorth)
            return BuildDiagonalOnlyShorePrefabs(x, y, southEastBayPrefab, southEastUpslopeWaterPrefab, southEastSlopeWaterPrefab, IsAxisAlignedRectangleCornerWaterSouthEast(x, y));

        if (hasWaterAtSouthWest && !hasWaterAtNorth)
            return BuildDiagonalOnlyShorePrefabs(x, y, southWestBayPrefab, southWestUpslopeWaterPrefab, southWestSlopeWaterPrefab, IsAxisAlignedRectangleCornerWaterSouthWest(x, y));

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
    /// Sorting for water-slope tiles at (x,y): uses neighbor water visual height (surface − 1), same as <see cref="PlaceWaterShore"/>.
    /// </summary>
    /// <param name="x">Grid X coordinate.</param>
    /// <param name="y">Grid Y coordinate.</param>
    /// <returns>Sorting order value for the water slope.</returns>
    public int CalculateWaterSlopeSortingOrder(int x, int y)
    {
        const int WATER_SLOPE_OFFSET = 1;
        int waterVisualH = GetNeighborWaterVisualHeightForShore(x, y);
        return CalculateTerrainSortingOrder(x, y, waterVisualH) + WATER_SLOPE_OFFSET;
    }

    /// <summary>
    /// Sorting for Bay (inner 90°) shore tiles only: same base as <see cref="CalculateWaterSlopeSortingOrder"/>,
    /// then at least one step above adjacent water tiles so isometric neighbors do not cover the corner.
    /// </summary>
    public int CalculateBayShoreSortingOrder(int x, int y)
    {
        const int BAY_SHORE_MIN_OFFSET = 1;
        int waterVisualH = GetNeighborWaterVisualHeightForShore(x, y);
        int baseOrder = CalculateTerrainSortingOrder(x, y, waterVisualH) + BAY_SHORE_MIN_OFFSET;

        int maxNeighborWater = int.MinValue;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue;
                int nx = x + dx;
                int ny = y + dy;
                if (!WaterOrSeaAt(nx, ny))
                    continue;
                int order = GetWaterNeighborTileSortingOrder(nx, ny);
                if (order > maxNeighborWater)
                    maxNeighborWater = order;
            }
        }

        if (maxNeighborWater == int.MinValue)
            return baseOrder;
        return Mathf.Max(baseOrder, maxNeighborWater + 1);
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
    /// Returns true if the given GameObject is an instance of any water-slope or shore Bay prefab.
    /// </summary>
    /// <param name="obj">The GameObject to check.</param>
    /// <returns>True if the object matches a water-slope or Bay prefab.</returns>
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
            || IsPrefabInstance(obj, southWestUpslopeWaterPrefab)
            || IsBayObject(obj);
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

    /// <summary>
    /// Height used for land slope / prefab selection. Out-of-map neighbors use <paramref name="currentHeight"/> so MIN_HEIGHT outside the grid does not fake slopes toward the void.
    /// </summary>
    int GetNeighborHeightForLandSlope(int nx, int ny, int currentHeight)
    {
        if (heightMap == null || !heightMap.IsValidPosition(nx, ny))
            return currentHeight;
        return heightMap.GetHeight(nx, ny);
    }

    private GameObject DetermineSlopePrefab(int x, int y)
    {
        int currentHeight = heightMap.GetHeight(x, y);

        int northHeight = GetNeighborHeightForLandSlope(x + 1, y, currentHeight);
        int southHeight = GetNeighborHeightForLandSlope(x - 1, y, currentHeight);
        int westHeight = GetNeighborHeightForLandSlope(x, y + 1, currentHeight);
        int eastHeight = GetNeighborHeightForLandSlope(x, y - 1, currentHeight);

        int neHeight = GetNeighborHeightForLandSlope(x + 1, y - 1, currentHeight);
        int nwHeight = GetNeighborHeightForLandSlope(x + 1, y + 1, currentHeight);
        int swHeight = GetNeighborHeightForLandSlope(x - 1, y + 1, currentHeight);
        int seHeight = GetNeighborHeightForLandSlope(x - 1, y - 1, currentHeight);

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
        int northHeight = GetNeighborHeightForLandSlope(x + 1, y, currentHeight);
        int southHeight = GetNeighborHeightForLandSlope(x - 1, y, currentHeight);
        int westHeight = GetNeighborHeightForLandSlope(x, y + 1, currentHeight);
        int eastHeight = GetNeighborHeightForLandSlope(x, y - 1, currentHeight);
        int neHeight = GetNeighborHeightForLandSlope(x + 1, y - 1, currentHeight);
        int nwHeight = GetNeighborHeightForLandSlope(x + 1, y + 1, currentHeight);
        int swHeight = GetNeighborHeightForLandSlope(x - 1, y + 1, currentHeight);
        int seHeight = GetNeighborHeightForLandSlope(x - 1, y - 1, currentHeight);

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
