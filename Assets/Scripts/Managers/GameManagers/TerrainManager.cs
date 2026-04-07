using UnityEngine;
using UnityEngine.Serialization;
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
/// Water–water surface steps: <see cref="RefreshWaterCascadeCliffs"/> (same segment stack as brown <c>PlaceCliffWallStack</c>).
/// Map-border south/east cliffs toward the off-grid void stack brown segments down to <see cref="MIN_HEIGHT"/>; water-shore primary tiles skip duplicate faces toward that void.
/// QA uniform new-game terrain: <see cref="SetNewGameFlatTerrainOptions"/> (driven from <see cref="Territory.Geography.GeographyManager"/>).
/// </summary>
public class TerrainManager : MonoBehaviour, ITerrainManager
{
    /// <summary>
    /// When true, logs when <see cref="RestoreTerrainForCell"/> exits early (null cell, overlay, invalid position).
    /// Used to diagnose cut-through / BUG-29 voids vs sorting issues.
    /// </summary>
    public static bool LogTerraformRestoreDiagnostics = false;

    /// <summary>
    /// Single-cell shore prefab diagnostics (temporary). Change coordinates to trace another cell.
    /// Also update <see cref="WaterManager.PlaceWater"/> and <c>ShouldForceDiagonalSlopeWaterAtRiverJunctionBrink</c> (WaterManager.Membership) when changing.
    /// </summary>
    private const int ShoreDiagX = 66;
    private const int ShoreDiagY = 62;

    private static bool IsShoreDiagCell(int x, int y) => x == ShoreDiagX && y == ShoreDiagY;

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

    [Header("Water–water cascade cliffs (BUG-42)")]
    /// <summary>Water-facing cliff stack on the south diamond edge (same placement model as <see cref="southCliffWallPrefab"/>).</summary>
    public GameObject cliffWaterSouthPrefab;
    /// <summary>Water-facing cliff stack on the east diamond edge (same placement model as <see cref="eastCliffWallPrefab"/>).</summary>
    public GameObject cliffWaterEastPrefab;

    [Header("Cliff wall placement")]
    [Tooltip("Extra downward shift in world Y after grid math, in units of (tileHeight/2) — same as one logical height step in GridManager.GetWorldPositionVector. Tune if cliffs float or sink vs grass/water.")]
    [SerializeField] private float cliffWallPivotDownHeightSteps = 1.5f;
    [Tooltip("World X offset for South cliff art vs shared-edge midpoint, as a fraction of tileWidth (e.g. -0.25 when the draw was in the opposite corner). Set to 0 when the sprite art sits on the south face.")]
    [SerializeField] private float cliffWallSouthFaceNudgeTileWidthFraction = 1.0f;
    [Tooltip("World Y offset for South cliff art vs shared-edge midpoint, as a fraction of tileHeight. Set to 0 when the sprite art sits on the south face.")]
    [SerializeField] private float cliffWallSouthFaceNudgeTileHeightFraction = 1.0f;
    [Tooltip("World X offset for East cliff art vs shared-edge midpoint, as a fraction of tileWidth. Set to 0 when the sprite art sits on the east face.")]
    [SerializeField] private float cliffWallEastFaceNudgeTileWidthFraction = -1.0f;
    [Tooltip("World Y offset for East cliff art vs shared-edge midpoint, as a fraction of tileHeight. Set to 0 when the sprite art sits on the east face.")]
    [SerializeField] private float cliffWallEastFaceNudgeTileHeightFraction = 1.0f;
    [Tooltip("When the cell uses a water-shore primary prefab and the cliff lower neighbor is on-grid, cliff world Y is decreased by this fraction of tileHeight (0.5 matches legacy sloped-shore alignment). Skipped when the lower neighbor is off-grid. Set to 0 if cliffs align without the extra drop.")]
    [SerializeField] private float cliffWallWaterShoreYOffsetTileHeightFraction = 1.0f;

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
            cliffWaterSouthPrefab, cliffWaterEastPrefab,
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
    /// <summary>Reserved for future sea bodies (surface S = 0). Do not use as a proxy for “is water” — use <see cref="IsRegisteredOpenWaterAt"/> and logical surface S (spec §11).</summary>
    public const int SEA_LEVEL = 0;

    /// <summary>
    /// True when <see cref="WaterMap"/> registers open water. Beds may be above <see cref="SEA_LEVEL"/>; this is authoritative for roads/terraform (geography spec water map).
    /// </summary>
    public bool IsRegisteredOpenWaterAt(int x, int y)
    {
        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();
        return waterManager != null && waterManager.IsWaterAt(x, y);
    }

    /// <summary>
    /// Path/terraform: skip height writes and primary terrain mesh rebuilds for registered open water and water-shore slope cells (matches <see cref="PathTerraformPlan"/> Apply/Revert).
    /// </summary>
    public bool ShouldSkipRoadTerraformSurfaceAt(int x, int y, HeightMap heightMap)
    {
        if (heightMap == null || !heightMap.IsValidPosition(x, y))
            return true;
        if (IsWaterSlopeCell(x, y))
            return true;
        return IsRegisteredOpenWaterAt(x, y);
    }

    /// <summary>
    /// Land may use water-shore prefabs only when its height is at most this many steps above the <b>visual reference
    /// height</b> derived from an adjacent body's logical surface <c>S</c> (same as water tile placement:
    /// <c>max(MIN_HEIGHT, S - 1)</c>). See <see cref="IsLandEligibleForWaterShorePrefabs"/> and isometric spec §2.4.1.
    /// </summary>
    public const int MAX_LAND_HEIGHT_ABOVE_ADJACENT_WATER_SURFACE_FOR_SHORE_PREFABS = 1;

    /// <summary>Cardinal offsets (south, north, east, west) for neighbor scans — order matches four-direction loops.</summary>
    static readonly int[] CardinalDx = { -1, 1, 0, 0 };
    static readonly int[] CardinalDy = { 0, 0, -1, 1 };

    /// <summary>
    /// South/east face toward off-grid void: water-shore primary prefabs already include visible transition art on that edge; skip duplicate brown cliff stacks.
    /// </summary>
    private bool ShouldSuppressBrownCliffTowardOffGridForWaterShorePrimary(Cell cell)
    {
        return cell != null && CellUsesWaterShorePrimaryPrefab(cell);
    }

    // Sorting order constants for different object types
    public const int TERRAIN_BASE_ORDER = 0;
    /// <summary>Offset for land slope sorting. 1 = slightly in front of terrain so slopes (especially east-facing) render correctly.</summary>
    public const int SLOPE_OFFSET = 1;
    /// <summary>Cliff sprites must sort strictly below the cell's primary terrain/shore sprite; subtract this from <see cref="Cell.sortingOrder"/> for the top stack segment.</summary>
    private const int CliffSortingBelowCellTerrain = 1;
    public const int BUILDING_OFFSET = 10; // Buildings should be above terrain
    public const int EFFECT_OFFSET = 30; // Effects should be above terrain
    public const int DEPTH_MULTIPLIER = 100;
    public const int HEIGHT_MULTIPLIER = 10; // Must be < DEPTH_MULTIPLIER/MAX_HEIGHT so depth dominates (hilltops don't draw over foreground forest)

    /// <summary>
    /// When true for the next initial load, <see cref="LoadInitialHeightMap"/> fills the grid uniformly (no 40×40 template, no procedural extension).
    /// Set via <see cref="SetNewGameFlatTerrainOptions"/> from <see cref="Territory.Geography.GeographyManager"/> before <see cref="GridManager.InitializeGrid"/>.
    /// Cleared after <see cref="InitializeHeightMap"/> or <see cref="StartTerrainGeneration"/>.
    /// </summary>
    private bool newGameFlatTerrainEnabled;
    private int newGameFlatTerrainHeight = 1;
    #endregion

    #region Height Map Generation
    /// <summary>
    /// Configures the next <see cref="LoadInitialHeightMap"/> to use a uniform height across the map (QA / method testing).
    /// </summary>
    /// <param name="enabled">When true, skips template and procedural terrain for the next load only.</param>
    /// <param name="uniformHeight">Height for every cell; clamped to <see cref="MIN_HEIGHT"/>–<see cref="MAX_HEIGHT"/>.</param>
    public void SetNewGameFlatTerrainOptions(bool enabled, int uniformHeight)
    {
        newGameFlatTerrainEnabled = enabled;
        newGameFlatTerrainHeight = uniformHeight;
    }

    private void ClearNewGameFlatTerrainRequest()
    {
        newGameFlatTerrainEnabled = false;
    }
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
        ClearNewGameFlatTerrainRequest();
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
        if (!newGameFlatTerrainEnabled)
            EnsureGuaranteedLakeDepressions();
        ApplyHeightMapToGrid();
        ClearNewGameFlatTerrainRequest();
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
                gridManager.SetCellHeight(new Vector2(x, y), h, skipWaterMembershipRefresh: true);
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
            heightMap = new HeightMap(gridManager.width, gridManager.height);
            LoadInitialHeightMap();
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
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 1, 1, 2, 3, 4, 5, 5, 5, 4, 3, 2, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 1, 1, 2, 2, 3, 4, 5, 4, 3, 3, 2, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 1, 1, 1, 1, 1, 1, 2, 2, 3, 4, 5, 4, 3, 3, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 1, 1, 1, 1, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
          {1, 1, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1},
          {1, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1},
          {2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 3, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1},
          {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1},
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

        if (newGameFlatTerrainEnabled)
        {
            int uniform = Mathf.Clamp(newGameFlatTerrainHeight, MIN_HEIGHT, MAX_HEIGHT);
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                    heightMap.SetHeight(x, y, uniform);
            }
            return;
        }

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
    /// After lake/river water visuals exist, refreshes land cells adjacent to water so shore/bay slopes and
    /// <see cref="PlaceCliffWalls"/> stay consistent (BUG-42). Default: each shore cell plus one cardinal land
    /// neighbor outward from water (minimal audit). Adds a Moore ring of dry cells around the <b>high</b> water cell on
    /// <see cref="WaterMap.IsLakeSurfaceStepContactForbidden"/> edges (§12.7 lake rim). Pass <paramref name="expandSecondChebyshevRing"/> true after procedural river
    /// generation so confluence mouths get a wider Chebyshev-2 halo refresh.
    /// Then runs junction shore post-pass and <see cref="ApplyUpperBrinkShoreWaterCascadeCliffStacks"/> (§12.8.1).
    /// Call after <see cref="WaterManager.UpdateWaterVisuals"/>.
    /// </summary>
    public void RefreshShoreTerrainAfterWaterUpdate(WaterManager wm, bool expandSecondChebyshevRing = false)
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

        AddDryMooreRingAroundLakeRimForbiddenSurfaceSteps(wmMap, shore);

        var toRefresh = new HashSet<Vector2Int>(shore);
        bool useSecondRing = expandSecondChebyshevRing;
        if (useSecondRing)
        {
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
        }
        else
        {
            int[] d4x = { 1, -1, 0, 0 };
            int[] d4y = { 0, 0, 1, -1 };
            foreach (Vector2Int p in shore)
            {
                for (int i = 0; i < 4; i++)
                {
                    int wx = p.x + d4x[i];
                    int wy = p.y + d4y[i];
                    if (!heightMap.IsValidPosition(wx, wy) || !wmMap.IsWater(wx, wy))
                        continue;
                    int ox = p.x - d4x[i];
                    int oy = p.y - d4y[i];
                    if (!heightMap.IsValidPosition(ox, oy) || wmMap.IsWater(ox, oy))
                        continue;
                    toRefresh.Add(new Vector2Int(ox, oy));
                }
            }
        }

        ClampShoreLandHeightsToAdjacentWaterSurface(wm, wmMap, shore);

        var ordered = new List<Vector2Int>(toRefresh);
        ordered.Sort((a, b) =>
        {
            int sa = a.x + a.y;
            int sb = b.x + b.y;
            if (sa != sb)
                return sa.CompareTo(sb);
            return a.x.CompareTo(b.x);
        });

        foreach (Vector2Int p in ordered)
            UpdateTileElevation(p.x, p.y);

        ApplyJunctionCascadeShorePostPass(wm);
        ApplyUpperBrinkShoreWaterCascadeCliffStacks(wm);
    }

    /// <summary>
    /// After <see cref="ApplyJunctionCascadeShorePostPass"/>, builds <see cref="cliffWaterSouthPrefab"/> / <see cref="cliffWaterEastPrefab"/> stacks on
    /// dry land classified as <see cref="RiverJunctionBrinkRole.UpperBrink"/> only, parented to that shore cell. Uses the same cardinal edge and
    /// mirror rules as <see cref="RefreshWaterCascadeCliffs"/> (South when the lower pool is south or north of the high cell; East when east or west).
    /// Anchor Y follows the upper pool water plane (§5.6.2, §12.8.1).
    /// </summary>
    private void ApplyUpperBrinkShoreWaterCascadeCliffStacks(WaterManager wm)
    {
        if (heightMap == null || gridManager == null || wm == null)
            return;
        WaterMap wmMap = wm.GetWaterMap();
        if (wmMap == null)
            return;
        if (cliffWaterSouthPrefab == null && cliffWaterEastPrefab == null)
            return;
        if (waterManager == null)
            waterManager = wm;

        int gw = gridManager.width;
        int gh = gridManager.height;
        var cells = new List<Vector2Int>();
        for (int x = 0; x < gw; x++)
        {
            for (int y = 0; y < gh; y++)
            {
                if (wmMap.IsWater(x, y))
                    continue;
                if (!wmMap.TryGetDryLandRiverJunctionBrinkWithStep(x, y, out RiverJunctionBrinkRole role, out _, out _, out _, out _, out _))
                    continue;
                if (role != RiverJunctionBrinkRole.UpperBrink)
                    continue;
                cells.Add(new Vector2Int(x, y));
            }
        }

        cells.Sort((a, b) =>
        {
            int sa = a.x + a.y;
            int sb = b.x + b.y;
            if (sa != sb)
                return sa.CompareTo(sb);
            return a.x.CompareTo(b.x);
        });

        foreach (Vector2Int p in cells)
        {
            if (!wmMap.TryGetDryLandRiverJunctionBrinkWithStep(p.x, p.y, out RiverJunctionBrinkRole role, out _, out int highX, out int highY, out int lowX, out int lowY))
                continue;
            if (role != RiverJunctionBrinkRole.UpperBrink)
                continue;

            int sHigh = wm.GetWaterSurfaceHeight(highX, highY);
            int sLow = wm.GetWaterSurfaceHeight(lowX, lowY);
            if (sHigh < 0 || sLow < 0 || sHigh <= sLow)
                continue;
            if (wmMap.IsLakeSurfaceStepContactForbidden(highX, highY, lowX, lowY))
                continue;

            Cell shoreCell = gridManager.GetCell(p.x, p.y);
            if (shoreCell != null)
                RemoveExistingWaterCascadeCliffs(shoreCell);

            int sx = p.x;
            int sy = p.y;

            if (cliffWaterSouthPrefab != null && lowX == highX - 1 && lowY == highY)
            {
                TryPlaceWaterCascadeCliffStack(highX, highY, lowX, lowY, sHigh, sLow, CliffCardinalFace.South, cliffWaterSouthPrefab, sx, sy, sx, sy);
                continue;
            }
            if (cliffWaterEastPrefab != null && lowX == highX && lowY == highY - 1)
            {
                TryPlaceWaterCascadeCliffStack(highX, highY, lowX, lowY, sHigh, sLow, CliffCardinalFace.East, cliffWaterEastPrefab, sx, sy, sx, sy);
                continue;
            }
            if (cliffWaterSouthPrefab != null && lowX == highX + 1 && lowY == highY)
            {
                TryPlaceWaterCascadeCliffStack(highX, highY, lowX, lowY, sHigh, sLow, CliffCardinalFace.South, cliffWaterSouthPrefab, sx, sy, sx, sy);
                continue;
            }
            if (cliffWaterEastPrefab != null && lowX == highX && lowY == highY + 1)
            {
                TryPlaceWaterCascadeCliffStack(highX, highY, lowX, lowY, sHigh, sLow, CliffCardinalFace.East, cliffWaterEastPrefab, sx, sy, sx, sy);
            }
        }
    }

    /// <summary>
    /// After the main shore refresh, re-applies shore prefabs for dry land on upper/lower river–river junction brinks using an extended
    /// neighbor mask and forced diagonal <c>*SlopeWaterPrefab</c> so cascade edges do not stay on cardinal-only tiles (§12.8.1).
    /// </summary>
    private void ApplyJunctionCascadeShorePostPass(WaterManager wm)
    {
        if (heightMap == null || gridManager == null || wm == null)
            return;
        WaterMap wmMap = wm.GetWaterMap();
        if (wmMap == null)
            return;
        if (waterManager == null)
            waterManager = wm;

        var cells = new List<Vector2Int>();
        for (int x = 0; x < gridManager.width; x++)
        {
            for (int y = 0; y < gridManager.height; y++)
            {
                if (wmMap.IsWater(x, y))
                    continue;
                if (!wmMap.TryGetDryLandRiverJunctionBrinkWithStep(x, y, out RiverJunctionBrinkRole role, out _, out _, out _, out _, out _))
                    continue;
                if (role != RiverJunctionBrinkRole.UpperBrink && role != RiverJunctionBrinkRole.LowerBrink)
                    continue;
                cells.Add(new Vector2Int(x, y));
            }
        }

        cells.Sort((a, b) =>
        {
            int sa = a.x + a.y;
            int sb = b.x + b.y;
            if (sa != sb)
                return sa.CompareTo(sb);
            return a.x.CompareTo(b.x);
        });

        foreach (Vector2Int p in cells)
        {
            List<GameObject> prefabs = DetermineWaterShorePrefabs(p.x, p.y, useJunctionTopologyForShorePattern: true, forceJunctionDiagonalSlopeForCascade: true);
            if (prefabs == null || prefabs.Count == 0)
                continue;
            PlaceWaterShore(p.x, p.y, prefabs);
        }
    }

    /// <summary>
    /// Adds extra dry cells in the Moore neighborhood of the <b>higher</b> water cell on each cardinal edge where
    /// <see cref="WaterMap.IsLakeSurfaceStepContactForbidden"/> applies (Lake at a surface step — §12.7). Ensures
    /// <see cref="UpdateTileElevation"/> revisits rim grass/cliffs so lake shore can close before the escarpment instead
    /// of leaving gaps next to lower-pool water.
    /// </summary>
    private void AddDryMooreRingAroundLakeRimForbiddenSurfaceSteps(WaterMap wmMap, HashSet<Vector2Int> shore)
    {
        if (heightMap == null || gridManager == null || wmMap == null || shore == null)
            return;

        int[] d4x = { 1, -1, 0, 0 };
        int[] d4y = { 0, 0, 1, -1 };

        for (int x = 0; x < gridManager.width; x++)
        {
            for (int y = 0; y < gridManager.height; y++)
            {
                if (!wmMap.IsWater(x, y))
                    continue;
                int sHere = wmMap.GetSurfaceHeightAt(x, y);
                if (sHere < 0)
                    continue;
                for (int i = 0; i < 4; i++)
                {
                    int nx = x + d4x[i];
                    int ny = y + d4y[i];
                    if (!heightMap.IsValidPosition(nx, ny) || !wmMap.IsWater(nx, ny))
                        continue;
                    int sN = wmMap.GetSurfaceHeightAt(nx, ny);
                    if (sN < 0 || sHere == sN)
                        continue;
                    int hX, hY, lX, lY;
                    if (sHere > sN) { hX = x; hY = y; lX = nx; lY = ny; }
                    else { hX = nx; hY = ny; lX = x; lY = y; }
                    if (!wmMap.IsLakeSurfaceStepContactForbidden(hX, hY, lX, lY))
                        continue;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0)
                                continue;
                            int ax = hX + dx;
                            int ay = hY + dy;
                            if (!heightMap.IsValidPosition(ax, ay))
                                continue;
                            if (!wmMap.IsWater(ax, ay))
                                shore.Add(new Vector2Int(ax, ay));
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Lowers dry cells in the Moore shore ring when they sit above the <b>logical surface</b> of their affiliated
    /// adjacent water body, so plateau heights do not dominate <see cref="PlaceWaterShore"/> (§2.4.1). At multi-surface
    /// junctions (e.g. waterfall), uses <see cref="Cell.waterBodyId"/> or <see cref="WaterManager.GetShoreAffiliatedWaterBodyIdForLandCell"/>
    /// so shore-line terrain stays aligned with that body&apos;s <c>S</c>, not the minimum <c>S</c> across all neighboring pools.
    /// Only decreases <see cref="HeightMap"/>; never raises terrain.
    /// </summary>
    private void ClampShoreLandHeightsToAdjacentWaterSurface(WaterManager wm, WaterMap wmMap, HashSet<Vector2Int> shoreCells)
    {
        if (heightMap == null || wm == null || wmMap == null || shoreCells == null || shoreCells.Count == 0)
            return;

        foreach (Vector2Int p in shoreCells)
        {
            int x = p.x;
            int y = p.y;
            if (!heightMap.IsValidPosition(x, y))
                continue;
            if (wmMap.IsWater(x, y))
                continue;

            int minSurfaceAll = int.MaxValue;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                        continue;
                    int nx = x + dx;
                    int ny = y + dy;
                    if (!heightMap.IsValidPosition(nx, ny) || !wmMap.IsWater(nx, ny))
                        continue;
                    int s = wm.GetWaterSurfaceHeight(nx, ny);
                    if (s >= 0)
                        minSurfaceAll = Mathf.Min(minSurfaceAll, s);
                }
            }

            if (minSurfaceAll == int.MaxValue)
                continue;

            int minSurface = minSurfaceAll;
            if (gridManager != null)
            {
                Cell cell = gridManager.GetCell(x, y);
                int affId = cell != null ? cell.waterBodyId : 0;
                if (affId == 0)
                    affId = wm.GetShoreAffiliatedWaterBodyIdForLandCell(x, y);

                if (affId != 0)
                {
                    int minAff = int.MaxValue;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0)
                                continue;
                            int nx = x + dx;
                            int ny = y + dy;
                            if (!heightMap.IsValidPosition(nx, ny) || !wmMap.IsWater(nx, ny))
                                continue;
                            if (wmMap.GetWaterBodyId(nx, ny) != affId)
                                continue;
                            int s = wm.GetWaterSurfaceHeight(nx, ny);
                            if (s >= 0)
                                minAff = Mathf.Min(minAff, s);
                        }
                    }
                    if (minAff != int.MaxValue)
                        minSurface = minAff;
                }
            }

            int h = heightMap.GetHeight(x, y);
            if (h > minSurface)
                heightMap.SetHeight(x, y, Mathf.Clamp(minSurface, MIN_HEIGHT, MAX_HEIGHT));
        }
    }

    private void UpdateTileElevation(int x, int y)
    {
        int newHeight = heightMap.GetHeight(x, y);
        Cell cell = gridManager.GetCell(x, y);
        if (cell == null) return;
        try
        {
            gridManager.SetCellHeight(new Vector2(x, y), newHeight, skipWaterMembershipRefresh: true);

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

            if (waterManager == null)
                waterManager = FindObjectOfType<WaterManager>();
            // River/lake/sea cells: terrain refresh must not place grass or slopes on top of water (avoids holes / double stacks).
            if (waterManager != null && waterManager.IsWaterAt(x, y))
            {
                waterManager.PlaceWater(x, y);
                return;
            }

            // Land cell adjacent to water: use water-shore prefabs only when close enough to that body's surface (rim
            // cliffs go on higher cells). Water cells already returned above.
            bool canUseWaterShorePrefabsHere = newHeight >= 1
                && IsLandEligibleForWaterShorePrefabs(x, y, newHeight);
            if (canUseWaterShorePrefabsHere)
            {
                List<GameObject> waterShorePrefabs = DetermineWaterShorePrefabs(x, y);
                if (waterShorePrefabs != null && waterShorePrefabs.Count > 0)
                {
                    PlaceWaterShore(x, y, waterShorePrefabs);
                    PlaceCliffWalls(x, y);
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
        finally
        {
            if (waterManager == null)
                waterManager = FindObjectOfType<WaterManager>();
            if (waterManager != null)
                waterManager.OnLandCellHeightCommitted(x, y);
        }
    }

    /// <summary>
    /// True if the cell has any child with ZoneCategory.Zoning (residential/commercial/industrial overlay).
    /// Used to skip terrain refresh on those cells so <see cref="PathTerraformPlan.Apply"/> Phase 2/3 does not replace the overlay.
    /// Building / footprint protection uses <see cref="GridManager.IsCellOccupiedByBuilding"/> (BUG-37).
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

        try
        {
            gridManager.SetCellHeight(new Vector2(x, y), newHeight, skipWaterMembershipRefresh: true);

            Vector2 newWorldPos = gridManager.GetCellWorldPosition(cell);
            cell.gameObject.transform.position = newWorldPos;
            cell.transformPosition = newWorldPos;

            int sortingOrder = CalculateTerrainSortingOrder(x, y, newHeight);
            cell.sortingOrder = sortingOrder;
            SpriteRenderer sr = cell.gameObject.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.sortingOrder = sortingOrder;

            // BUG-37: Path terraform Phase 2/3 calls this on Moore neighbors of the stroke. RCI buildings (and
            // multi-cell footprint cells with no local tile) must not get PlaceFlatTerrain / slope rebuild — that
            // stacks grass under or beside the building and reads as cleared development. Preview avoids Apply, so
            // only commit/AUTO hit this path. Still sync height / cell transform to the heightmap (Phase 1 may
            // have written this cell when it is in the terraform footprint).
            if (gridManager != null && gridManager.IsCellOccupiedByBuilding(x, y))
                return false;

            bool requiresSlope = forceFlat ? false : (forceSlopeType.HasValue || RequiresSlope(x, y, newHeight));

            if (waterManager == null)
                waterManager = FindObjectOfType<WaterManager>();
            // Same rule as <see cref="UpdateTileElevation"/>: water-shore only when within surface-height cap (not rim cliffs).
            bool landShoreRestore = newHeight >= 1 && !forceFlat && !forceSlopeType.HasValue
                && !(waterManager != null && waterManager.IsWaterAt(x, y))
                && IsLandEligibleForWaterShorePrefabs(x, y, newHeight);
            if (landShoreRestore)
            {
                List<GameObject> waterShorePrefabs = DetermineWaterShorePrefabs(x, y);
                if (waterShorePrefabs != null && waterShorePrefabs.Count > 0)
                {
                    PlaceWaterShore(x, y, waterShorePrefabs);
                    PlaceCliffWalls(x, y, terraformCutCorridorCells);
                    return true;
                }
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
        finally
        {
            if (waterManager == null)
                waterManager = FindObjectOfType<WaterManager>();
            if (waterManager != null)
                waterManager.OnLandCellHeightCommitted(x, y);
        }
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
            else if (IsWaterSlopeObject(child.gameObject) || IsLandSlopeObject(child.gameObject) || IsShoreBayObject(child.gameObject))
                toDestroy.Add(child.gameObject);
            else if (zone == null && child.GetComponent<SpriteRenderer>() != null
                     && !IsWaterSlopeObject(child.gameObject)
                     && !IsLandSlopeObject(child.gameObject)
                     && !IsShoreBayObject(child.gameObject)
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
    /// True if this land cell may use water-shore prefabs: among 8 neighbors, some water/sea exists whose logical
    /// surface <c>S</c> yields a visual reference <c>V = max(MIN_HEIGHT, S - 1)</c> (aligned with
    /// <see cref="WaterManager.PlaceWater"/>) such that
    /// <c>landHeight &lt;= V + <see cref="MAX_LAND_HEIGHT_ABOVE_ADJACENT_WATER_SURFACE_FOR_SHORE_PREFABS"/></c>.
    /// Higher rim cells fall through to land slopes + cliff stacks.
    /// </summary>
    private bool IsLandEligibleForWaterShorePrefabs(int x, int y, int landHeight)
    {
        if (heightMap == null || landHeight < 1)
            return false;
        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue;
                int nx = x + dx;
                int ny = y + dy;
                int surface = TryGetSurfaceHeightForWaterNeighbor(nx, ny);
                if (surface < 0)
                    continue;
                int visualRef = GetWaterVisualReferenceHeightFromLogicalSurface(surface);
                if (landHeight <= visualRef + MAX_LAND_HEIGHT_ABOVE_ADJACENT_WATER_SURFACE_FOR_SHORE_PREFABS)
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Same height index used for water tile world placement (FEAT-37 Option A): one step below logical spill surface.
    /// </summary>
    private static int GetWaterVisualReferenceHeightFromLogicalSurface(int logicalSurface)
    {
        return Mathf.Max(MIN_HEIGHT, logicalSurface - 1);
    }

    /// <summary>
    /// Surface height for water at the neighbor, or -1 if the cell is not water/sea. Uses <see cref="WaterManager"/>
    /// / <see cref="WaterMap"/> when registered; sea-level terrain before registration uses <see cref="WaterManager.seaLevel"/>.
    /// </summary>
    private int TryGetSurfaceHeightForWaterNeighbor(int nx, int ny)
    {
        if (heightMap == null || !heightMap.IsValidPosition(nx, ny))
            return -1;
        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();

        int nh = heightMap.GetHeight(nx, ny);
        if (waterManager != null && waterManager.IsWaterAt(nx, ny))
        {
            int s = waterManager.GetWaterSurfaceHeight(nx, ny);
            return s >= 0 ? s : waterManager.seaLevel;
        }
        if (nh == SEA_LEVEL)
        {
            if (waterManager != null)
            {
                int s = waterManager.GetWaterSurfaceHeight(nx, ny);
                if (s >= 0)
                    return s;
                return waterManager.seaLevel;
            }
            return SEA_LEVEL;
        }
        return -1;
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
    /// True if this cell is a water-shore tile (land within one height step of an adjacent water body's surface).
    /// Rim cells far above the water surface are false so roads/terraform treat them as normal terrain.
    /// </summary>
    public bool IsWaterSlopeCell(int x, int y)
    {
        if (heightMap == null)
            EnsureHeightMapLoaded();
        if (heightMap == null || !heightMap.IsValidPosition(x, y))
            return false;
        int h = heightMap.GetHeight(x, y);
        if (h < 1)
            return false;
        return IsLandEligibleForWaterShorePrefabs(x, y, h);
    }

    /// <summary>
    /// Dry land that may carry <see cref="Cell.waterBodyId"/> for water-shore art or rim cliffs toward registered water.
    /// </summary>
    public bool IsDryShoreOrRimMembershipEligible(int x, int y)
    {
        if (heightMap == null || !heightMap.IsValidPosition(x, y))
            return false;
        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();
        if (waterManager != null && waterManager.IsWaterAt(x, y))
            return false;
        int h = heightMap.GetHeight(x, y);
        if (h < 1)
            return false;
        if (IsLandEligibleForWaterShorePrefabs(x, y, h))
            return true;
        int[] d4x = { 1, -1, 0, 0 };
        int[] d4y = { 0, 0, 1, -1 };
        for (int i = 0; i < 4; i++)
        {
            int nx = x + d4x[i];
            int ny = y + d4y[i];
            if (!heightMap.IsValidPosition(nx, ny))
                continue;
            if (waterManager == null || !waterManager.IsWaterAt(nx, ny))
                continue;
            int nh = heightMap.GetHeight(nx, ny);
            if (h > nh)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Public alias for shore prefab neighbor tests (same as private <see cref="WaterOrSeaAt"/>).
    /// </summary>
    public bool IsWaterOrSeaAtNeighbor(int nx, int ny)
    {
        return WaterOrSeaAt(nx, ny);
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

        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();
        int affBody = waterManager != null ? waterManager.GetShoreAffiliatedWaterBodyIdForLandCell(x, y) : 0;
        int waterVisualH = GetNeighborWaterVisualHeightForShore(x, y, affBody);

        GameObject primaryPrefab = waterShorePrefabs[0];
        bool primaryIsBay = primaryPrefab != null && IsShoreBayPrefab(primaryPrefab);

        for (int i = 0; i < waterShorePrefabs.Count; i++)
        {
            GameObject prefab = waterShorePrefabs[i];
            if (prefab == null)
                continue;

            Vector2 slopeWorldPos = gridManager.GetWorldPositionVector(x, y, waterVisualH);
            float extraWorldY = GetShoreExtraWorldYOffset(prefab, landH, waterVisualH, waterShorePrefabs.Count);
            if (extraWorldY != 0f)
                slopeWorldPos += new Vector2(0f, extraWorldY);

            GameObject slope = Instantiate(prefab, slopeWorldPos, Quaternion.identity);
            slope.SetActive(true);
            slope.transform.SetParent(cell.gameObject.transform, true);

            bool isBay = IsShoreBayPrefab(prefab);
            int sortingOrder = isBay
                ? CalculateShoreBaySortingOrder(x, y)
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
            ? CalculateShoreBaySortingOrder(x, y)
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
        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();
        if (waterManager != null && waterManager.IsWaterAt(x, y))
            return;

        if (IsCellSurroundedByCardinalWaterOnly(x, y, waterManager))
            return;

        Cell cell = gridManager.GetCell(x, y);
        RemoveExistingCliffWalls(cell);
        if (cell != null)
            cell.cliffFaces = CliffFaceFlags.None;

        // Cardinal drops from this (high) cell toward lower neighbors. Prefab is chosen by geometric face (CliffCardinalFace);
        // north/west faces skip instantiation (hidden from fixed camera) but cliffFaces still records the risco.
        int dNorth = GetCliffWallDropNorth(x, y, currentHeight, terraformCutCorridorCells);
        if (dNorth > 0)
        {
            if (cell != null)
                cell.cliffFaces |= CliffFaceFlags.North;
            PlaceCliffWallStack(cell, CliffCardinalFace.North, x, y, x + 1, y, currentHeight, heightMap.GetHeight(x + 1, y), dNorth);
        }

        int dSouth = GetCliffWallDropSouth(x, y, currentHeight, terraformCutCorridorCells);
        if (dSouth > 0)
        {
            if (cell != null)
                cell.cliffFaces |= CliffFaceFlags.South;
            int southLowH = heightMap.IsValidPosition(x - 1, y)
                ? heightMap.GetHeight(x - 1, y)
                : MIN_HEIGHT;
            PlaceCliffWallStack(cell, CliffCardinalFace.South, x, y, x - 1, y, currentHeight, southLowH, dSouth);
        }

        int dEast = GetCliffWallDropEast(x, y, currentHeight, terraformCutCorridorCells);
        if (dEast > 0)
        {
            if (cell != null)
                cell.cliffFaces |= CliffFaceFlags.East;
            int eastLowH = heightMap.IsValidPosition(x, y - 1)
                ? heightMap.GetHeight(x, y - 1)
                : MIN_HEIGHT;
            PlaceCliffWallStack(cell, CliffCardinalFace.East, x, y, x, y - 1, currentHeight, eastLowH, dEast);
        }

        int dWest = GetCliffWallDropWest(x, y, currentHeight, terraformCutCorridorCells);
        if (dWest > 0)
        {
            if (cell != null)
                cell.cliffFaces |= CliffFaceFlags.West;
            PlaceCliffWallStack(cell, CliffCardinalFace.West, x, y, x, y + 1, currentHeight, heightMap.GetHeight(x, y + 1), dWest);
        }

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
            if (wm == null || !wm.IsWaterAt(nx, ny))
                return false;
        }
        return true;
    }

    /// <summary>
    /// True for a <b>one-step</b> drop toward sea, registered water, or a water-shore slope tile — do not instantiate
    /// cliff prefabs; water-slope / water visuals cover that transition. Escarpments (Δh ≥ 2) toward the same neighbor
    /// are <b>not</b> suppressed here so stacked cliff segments can fill the vertical face (lake basins, rim voids).
    /// Suppression applies only when the <b>high</b> cell is in the water-shore eligibility band; rim plateaus above the
    /// shore strip (not eligible) keep cliff faces toward that lower neighbor (BUG-42 voids).
    /// </summary>
    private bool ShouldSuppressCliffFaceTowardLowerCell(int highX, int highY, int lowerX, int lowerY, int lowerHeight, int currentHeight)
    {
        if (currentHeight - lowerHeight != 1)
            return false;
        if (heightMap == null || !heightMap.IsValidPosition(lowerX, lowerY))
            return false;
        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();
        bool lowerIsWaterOrSlope = (waterManager != null && waterManager.IsWaterAt(lowerX, lowerY)) || IsWaterSlopeCell(lowerX, lowerY);
        if (!lowerIsWaterOrSlope)
            return false;
        if (currentHeight < 1 || !IsLandEligibleForWaterShorePrefabs(highX, highY, currentHeight))
            return false;
        return true;
    }

    /// <summary>
    /// After one-step suppression rules, resolves segment count for dry land, narrow shore, cut-through, and rim plateau toward water/slope.
    /// </summary>
    private int ResolveCliffWallDropAfterSuppression(
        int highX,
        int highY,
        int currentHeight,
        int lowerX,
        int lowerY,
        int lowerHeight,
        int diff,
        ISet<Vector2Int> terraformCutCorridorCells)
    {
        // Map edge (south/east): lower cell is outside the grid — caller passes diff and lowerHeight (typically to MIN_HEIGHT).
        if (heightMap == null || !heightMap.IsValidPosition(lowerX, lowerY))
        {
            if (diff <= 0)
                return 0;
            if (ShouldSuppressCliffFaceTowardLowerCell(highX, highY, lowerX, lowerY, lowerHeight, currentHeight))
                return 0;
            if (diff > 1)
                return diff;
            if (ShouldSuppressCliffTowardCardinalLower(highX, highY, lowerX, lowerY, currentHeight, lowerHeight))
                return 0;
            if (NeedsCutThroughOneStepCliffToCorridor(terraformCutCorridorCells, currentHeight, lowerHeight, lowerX, lowerY))
                return 1;
            if (diff == 1 && currentHeight >= 1 && !IsLandEligibleForWaterShorePrefabs(highX, highY, currentHeight))
            {
                if (waterManager == null)
                    waterManager = FindObjectOfType<WaterManager>();
                if (waterManager != null && waterManager.IsWaterAt(lowerX, lowerY))
                    return 1;
                if (IsWaterSlopeCell(lowerX, lowerY))
                    return 1;
            }
            return 1;
        }

        if (diff > 1)
            return diff;
        if (ShouldSuppressCliffTowardCardinalLower(highX, highY, lowerX, lowerY, currentHeight, lowerHeight))
            return 0;
        if (NeedsCutThroughOneStepCliffToCorridor(terraformCutCorridorCells, currentHeight, lowerHeight, lowerX, lowerY))
            return 1;
        if (diff == 1 && currentHeight >= 1 && !IsLandEligibleForWaterShorePrefabs(highX, highY, currentHeight))
        {
            if (waterManager == null)
                waterManager = FindObjectOfType<WaterManager>();
            if (waterManager != null && waterManager.IsWaterAt(lowerX, lowerY))
                return 1;
            if (IsWaterSlopeCell(lowerX, lowerY))
                return 1;
        }

        return 0;
    }

    /// <summary>
    /// One-step drop toward open water only: suppress a cliff on the narrow shore strip (height-1 band with higher land
    /// behind). One-step drops toward water / water-shore also use <see cref="ShouldSuppressCliffFaceTowardLowerCell"/>.
    /// </summary>
    private bool ShouldSuppressCliffTowardCardinalLower(int x, int y, int lowerX, int lowerY, int currentHeight, int lowerHeight)
    {
        if (currentHeight - lowerHeight != 1)
            return false;
        if (heightMap == null || !heightMap.IsValidPosition(x, y) || !heightMap.IsValidPosition(lowerX, lowerY))
            return false;
        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();
        bool lowerIsWaterSurface = waterManager != null && waterManager.IsWaterAt(lowerX, lowerY);
        if (!lowerIsWaterSurface)
            return false;
        int inlandX = x + (x - lowerX);
        int inlandY = y + (y - lowerY);
        if (!heightMap.IsValidPosition(inlandX, inlandY))
            return false;
        int inlandH = heightMap.GetHeight(inlandX, inlandY);
        return inlandH > currentHeight;
    }

    /// <summary>Number of stacked cliff segments on the north face (0 = none). Higher cell is (x,y); lower neighbor is north.</summary>
    private int GetCliffWallDropNorth(int x, int y, int currentHeight, ISet<Vector2Int> terraformCutCorridorCells = null)
    {
        if (!heightMap.IsValidPosition(x + 1, y))
            return 0;
        int heightAtNorth = heightMap.GetHeight(x + 1, y);
        if (heightAtNorth >= currentHeight)
            return 0;
        int diff = currentHeight - heightAtNorth;

        if (ShouldSuppressCliffFaceTowardLowerCell(x, y, x + 1, y, heightAtNorth, currentHeight))
        {
            if (diff == 1 && NeedsCutThroughOneStepCliffToCorridor(terraformCutCorridorCells, currentHeight, heightAtNorth, x + 1, y)
                && !IsWaterSlopeCell(x + 1, y))
                return 1;
            return 0;
        }

        return ResolveCliffWallDropAfterSuppression(x, y, currentHeight, x + 1, y, heightAtNorth, diff, terraformCutCorridorCells);
    }

    /// <summary>Number of stacked cliff segments on the south face (0 = none). Higher cell is (x,y); lower is south.</summary>
    private int GetCliffWallDropSouth(int x, int y, int currentHeight, ISet<Vector2Int> terraformCutCorridorCells = null)
    {
        int lowerX = x - 1;
        int lowerY = y;
        bool lowerValid = heightMap != null && heightMap.IsValidPosition(lowerX, lowerY);
        if (!lowerValid)
        {
            Cell c = gridManager != null ? gridManager.GetCell(x, y) : null;
            if (ShouldSuppressBrownCliffTowardOffGridForWaterShorePrimary(c))
                return 0;
            int diffVoid = currentHeight - MIN_HEIGHT;
            if (diffVoid <= 0)
                return 0;
            return ResolveCliffWallDropAfterSuppression(x, y, currentHeight, lowerX, lowerY, MIN_HEIGHT, diffVoid, terraformCutCorridorCells);
        }

        int heightAtSouth = heightMap.GetHeight(lowerX, lowerY);
        if (heightAtSouth >= currentHeight)
            return 0;
        int diff = currentHeight - heightAtSouth;

        if (ShouldSuppressCliffFaceTowardLowerCell(x, y, lowerX, lowerY, heightAtSouth, currentHeight))
        {
            if (diff == 1 && NeedsCutThroughOneStepCliffToCorridor(terraformCutCorridorCells, currentHeight, heightAtSouth, lowerX, lowerY)
                && !IsWaterSlopeCell(lowerX, lowerY))
                return 1;
            return 0;
        }

        return ResolveCliffWallDropAfterSuppression(x, y, currentHeight, lowerX, lowerY, heightAtSouth, diff, terraformCutCorridorCells);
    }

    private int GetCliffWallDropEast(int x, int y, int currentHeight, ISet<Vector2Int> terraformCutCorridorCells = null)
    {
        int lowerX = x;
        int lowerY = y - 1;
        bool lowerValid = heightMap != null && heightMap.IsValidPosition(lowerX, lowerY);
        if (!lowerValid)
        {
            Cell c = gridManager != null ? gridManager.GetCell(x, y) : null;
            if (ShouldSuppressBrownCliffTowardOffGridForWaterShorePrimary(c))
                return 0;
            int diffVoid = currentHeight - MIN_HEIGHT;
            if (diffVoid <= 0)
                return 0;
            return ResolveCliffWallDropAfterSuppression(x, y, currentHeight, lowerX, lowerY, MIN_HEIGHT, diffVoid, terraformCutCorridorCells);
        }

        int heightAtEast = heightMap.GetHeight(lowerX, lowerY);
        if (heightAtEast >= currentHeight)
            return 0;
        int diff = currentHeight - heightAtEast;

        if (ShouldSuppressCliffFaceTowardLowerCell(x, y, lowerX, lowerY, heightAtEast, currentHeight))
        {
            if (diff == 1 && NeedsCutThroughOneStepCliffToCorridor(terraformCutCorridorCells, currentHeight, heightAtEast, lowerX, lowerY)
                && !IsWaterSlopeCell(lowerX, lowerY))
                return 1;
            return 0;
        }

        return ResolveCliffWallDropAfterSuppression(x, y, currentHeight, lowerX, lowerY, heightAtEast, diff, terraformCutCorridorCells);
    }

    /// <summary>Number of stacked cliff segments on the west face (0 = none). Higher cell is (x,y); lower neighbor is west.</summary>
    private int GetCliffWallDropWest(int x, int y, int currentHeight, ISet<Vector2Int> terraformCutCorridorCells = null)
    {
        if (!heightMap.IsValidPosition(x, y + 1))
            return 0;
        int heightAtWest = heightMap.GetHeight(x, y + 1);
        if (heightAtWest >= currentHeight)
            return 0;
        int diff = currentHeight - heightAtWest;

        if (ShouldSuppressCliffFaceTowardLowerCell(x, y, x, y + 1, heightAtWest, currentHeight))
        {
            if (diff == 1 && NeedsCutThroughOneStepCliffToCorridor(terraformCutCorridorCells, currentHeight, heightAtWest, x, y + 1)
                && !IsWaterSlopeCell(x, y + 1))
                return 1;
            return 0;
        }

        return ResolveCliffWallDropAfterSuppression(x, y, currentHeight, x, y + 1, heightAtWest, diff, terraformCutCorridorCells);
    }

    /// <summary>
    /// Cut-through only: 1-step land drop into the lowered corridor gets a cliff wall (avoids black voids at rim).
    /// </summary>
    private bool NeedsCutThroughOneStepCliffToCorridor(ISet<Vector2Int> terraformCutCorridorCells, int currentHeight, int neighborHeight, int nx, int ny)
    {
        if (terraformCutCorridorCells == null || !terraformCutCorridorCells.Contains(new Vector2Int(nx, ny)))
            return false;
        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();
        if (waterManager != null && waterManager.IsWaterAt(nx, ny))
            return false;
        return currentHeight - neighborHeight == 1 && neighborHeight < currentHeight;
    }

    /// <summary>
    /// Logical water surface height at the cell across the cliff foot (low side), or -1 if that cell is not registered water.
    /// </summary>
    private int GetWaterSurfaceHeightForCliffProbe(int probeX, int probeY)
    {
        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();
        if (waterManager == null || heightMap == null || !heightMap.IsValidPosition(probeX, probeY))
            return -1;
        if (!waterManager.IsWaterAt(probeX, probeY))
            return -1;
        int surf = waterManager.GetWaterSurfaceHeight(probeX, probeY);
        if (surf < 0)
            surf = waterManager.seaLevel;
        return surf;
    }

    /// <summary>
    /// True when the entire height band of this segment lies strictly below the water surface (no sprite — underwater).
    /// </summary>
    private static bool ShouldSkipCliffSegmentFullyUnderwater(int topH, int bottomH, int waterSurfaceHeight)
    {
        if (waterSurfaceHeight < 0)
            return false;
        int segmentHigh = Mathf.Max(topH, bottomH);
        return segmentHigh < waterSurfaceHeight;
    }

    /// <summary>
    /// Fixed isometric camera: south and east diamond edges face the player (↙ ↘). North and west are hidden behind terrain art.
    /// </summary>
    private static bool IsCliffCardinalFaceVisibleToCamera(CliffCardinalFace face)
    {
        return face == CliffCardinalFace.South || face == CliffCardinalFace.East;
    }

    /// <summary>Inspector cliff prefab for the geometric cardinal face (not delegated by name).</summary>
    private GameObject GetCliffPrefabForCardinalFace(CliffCardinalFace face)
    {
        switch (face)
        {
            case CliffCardinalFace.North:
                return northCliffWallPrefab;
            case CliffCardinalFace.South:
                return southCliffWallPrefab;
            case CliffCardinalFace.East:
                return eastCliffWallPrefab;
            case CliffCardinalFace.West:
                return westCliffWallPrefab;
            default:
                return null;
        }
    }

    /// <summary>
    /// World position for a cliff is the same x position as the cell, and the y position is the anchor y minus the tile height times the segment index.
    /// Land cliffs use the cell transform (terrain floor). Water–water cascades pass <paramref name="overrideAnchorWorldY"/> at the water visual surface (FEAT-37 / <see cref="WaterManager.PlaceWater"/>).
    /// </summary>
    private Vector2 GetCliffWallSegmentWorldPositionOnSharedEdge(Cell cell, int topH, int segmentIndex, float? overrideAnchorWorldY = null)
    {
        float anchorY = overrideAnchorWorldY ?? cell.gameObject.transform.position.y;
        return new Vector2(cell.gameObject.transform.position.x, anchorY - gridManager.tileHeight * (segmentIndex * 0.5f));
    }

    /// <summary>
    /// Places a stack of brown cliff walls on a <b>land</b> cell (see <see cref="PlaceCliffWalls"/>).
    /// </summary>
    private void PlaceCliffWallStack(Cell cell, CliffCardinalFace cardinalFace, int highX, int highY, int lowX, int lowY, int highH, int lowH, int segmentCount)
    {
        GameObject prefab = GetCliffPrefabForCardinalFace(cardinalFace);
        PlaceCliffWallStackCore(cell, prefab, cardinalFace, highX, highY, lowX, lowY, highH, lowH, segmentCount);
    }

    /// <summary>
    /// Shared stack placement for brown cliffs and water cascade cliffs — same segment loop and sorting.
    /// Land cliffs skip segments fully below the lower-side water surface; water–water cascades do not (logical surface vs bed heights would hide the whole stack).
    /// </summary>
    /// <returns>Number of cliff segment sprites instantiated.</returns>
    /// <param name="cliffStackAnchorWorldY">When set (water–water cascades), Y anchor at water visual surface; otherwise cell transform Y (terrain floor).</param>
    /// <param name="sortingReferenceGridX">When set with <paramref name="sortingReferenceGridY"/>, used for sorting depth instead of <paramref name="highX"/>,<paramref name="highY"/> (e.g. upper-brink shore cell).</param>
    private int PlaceCliffWallStackCore(
        Cell cell,
        GameObject cliffPrefab,
        CliffCardinalFace cardinalFace,
        int highX,
        int highY,
        int lowX,
        int lowY,
        int highH,
        int lowH,
        int segmentCount,
        float? cliffStackAnchorWorldY = null,
        int? sortingReferenceGridX = null,
        int? sortingReferenceGridY = null)
    {
        if (gridManager == null || heightMap == null || segmentCount <= 0)
            return 0;
        if (cell == null || cliffPrefab == null)
            return 0;
        if (highH <= lowH)
            return 0;

        if (!IsCliffCardinalFaceVisibleToCamera(cardinalFace))
            return 0;

        int waterSurfaceH = cliffStackAnchorWorldY == null
            ? GetWaterSurfaceHeightForCliffProbe(lowX, lowY)
            : -1;
        int cellTerrainSort = cell.sortingOrder;
        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();
        int sortGX = sortingReferenceGridX ?? highX;
        int sortGY = sortingReferenceGridY ?? highY;
        int maxSortFromForegroundWater = GetMaxCliffSortingOrderFromForegroundWaterNeighbors(sortGX, sortGY);

        float z = cell.gameObject.transform.position.z;
        Quaternion rot = Quaternion.identity;
        int d = highH - lowH;
        int count = Mathf.Min(segmentCount, d);
        int visualIndex = 0;
        for (int s = 0; s < count; s++)
        {
            int topH = highH - s;
            int bottomH = (s < count - 1) ? (highH - s - 1) : lowH;
            if (cliffStackAnchorWorldY == null && ShouldSkipCliffSegmentFullyUnderwater(topH, bottomH, waterSurfaceH))
                continue;

            Vector2 world = GetCliffWallSegmentWorldPositionOnSharedEdge(cell, topH, s, cliffStackAnchorWorldY);

            // Water-shore Y nudge aligns brown cliffs to adjacent open water on a real lower cell; skip when lower is off-grid.
            if (cliffStackAnchorWorldY == null && CellUsesWaterShorePrimaryPrefab(cell)
                && heightMap.IsValidPosition(lowX, lowY))
                world.y -= gridManager.tileHeight * cliffWallWaterShoreYOffsetTileHeightFraction;

            GameObject cliffWall = Instantiate(cliffPrefab, new Vector3(world.x, world.y, z), rot);
            cliffWall.transform.SetParent(cell.gameObject.transform, true);

            SpriteRenderer sr = cliffWall.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                int computedSort = CalculateTerrainSortingOrder(sortGX, sortGY, topH) + SLOPE_OFFSET + visualIndex;
                int maxCliffSort = cellTerrainSort - CliffSortingBelowCellTerrain - visualIndex;
                int finalSort = Mathf.Min(computedSort, maxCliffSort);
                if (maxSortFromForegroundWater != int.MaxValue)
                    finalSort = Mathf.Min(finalSort, maxSortFromForegroundWater - visualIndex);
                sr.sortingOrder = finalSort;
            }

            visualIndex++;
        }

        return visualIndex;
    }

    /// <summary>
    /// After all water tiles are placed, builds water–water surface-step stacks for every cardinal surface step
    /// (<c>S_high &gt; S_low</c>) using existing south/east cliff-water prefabs only (§5.6.2).
    /// Standard edges parent stacks to the <b>upper</b> cell when the lower pool is south or east (visible faces on that cell).
    /// When the lower pool is <b>north</b> or <b>west</b>, the same prefabs attach to the <b>lower</b> cell (south face toward
    /// the upper pool to the south; east face toward the upper pool to the east) so cascades are not missing on those contacts.
    /// <see cref="GetEffectiveHeightsForWaterWaterCliff"/> uses <c>segmentCount = S_high − S_low</c>. Skips edges where
    /// <see cref="WaterMap.IsLakeSurfaceStepContactForbidden"/> is true (§12.7).
    /// </summary>
    public void RefreshWaterCascadeCliffs(WaterManager wm)
    {
        if (heightMap == null || gridManager == null || wm == null)
            return;
        WaterMap wmMap = wm.GetWaterMap();
        if (wmMap == null)
            return;
        if (cliffWaterSouthPrefab == null && cliffWaterEastPrefab == null)
            return;
        if (waterManager == null)
            waterManager = wm;

        int gw = gridManager.width;
        int gh = gridManager.height;
        for (int x = 0; x < gw; x++)
        {
            for (int y = 0; y < gh; y++)
            {
                if (!wmMap.IsWater(x, y))
                    continue;
                Cell cell = gridManager.GetCell(x, y);
                if (cell != null)
                    RemoveExistingWaterCascadeCliffs(cell);
            }
        }

        for (int x = 0; x < gw; x++)
        {
            for (int y = 0; y < gh; y++)
            {
                if (!wmMap.IsWater(x, y))
                    continue;
                int sHere = wm.GetWaterSurfaceHeight(x, y);
                if (sHere < 0)
                    continue;

                if (cliffWaterSouthPrefab != null && heightMap.IsValidPosition(x - 1, y) && wmMap.IsWater(x - 1, y))
                {
                    int sLow = wm.GetWaterSurfaceHeight(x - 1, y);
                    if (sLow >= 0 && sHere > sLow && !wmMap.IsLakeSurfaceStepContactForbidden(x, y, x - 1, y))
                        TryPlaceWaterCascadeCliffStack(x, y, x - 1, y, sHere, sLow, CliffCardinalFace.South, cliffWaterSouthPrefab);
                }

                if (cliffWaterEastPrefab != null && heightMap.IsValidPosition(x, y - 1) && wmMap.IsWater(x, y - 1))
                {
                    int sLow = wm.GetWaterSurfaceHeight(x, y - 1);
                    if (sLow >= 0 && sHere > sLow && !wmMap.IsLakeSurfaceStepContactForbidden(x, y, x, y - 1))
                        TryPlaceWaterCascadeCliffStack(x, y, x, y - 1, sHere, sLow, CliffCardinalFace.East, cliffWaterEastPrefab);
                }

                // Lower pool to the north: shared edge is the south face of the northern (lower-S) cell — parent there (mirror).
                if (cliffWaterSouthPrefab != null && heightMap.IsValidPosition(x + 1, y) && wmMap.IsWater(x + 1, y))
                {
                    int sLowNorth = wm.GetWaterSurfaceHeight(x + 1, y);
                    if (sLowNorth >= 0 && sHere > sLowNorth && !wmMap.IsLakeSurfaceStepContactForbidden(x, y, x + 1, y))
                        TryPlaceWaterCascadeCliffStack(x, y, x + 1, y, sHere, sLowNorth, CliffCardinalFace.South, cliffWaterSouthPrefab, x + 1, y);
                }

                // Lower pool to the west: shared edge is the east face of the western (lower-S) cell — parent there (mirror).
                if (cliffWaterEastPrefab != null && heightMap.IsValidPosition(x, y + 1) && wmMap.IsWater(x, y + 1))
                {
                    int sLowWest = wm.GetWaterSurfaceHeight(x, y + 1);
                    if (sLowWest >= 0 && sHere > sLowWest && !wmMap.IsLakeSurfaceStepContactForbidden(x, y, x, y + 1))
                        TryPlaceWaterCascadeCliffStack(x, y, x, y + 1, sHere, sLowWest, CliffCardinalFace.East, cliffWaterEastPrefab, x, y + 1);
                }
            }
        }
    }

    /// <summary>
    /// Computes terrain heights and segment count for a water–water cascade. Segment count follows the
    /// <b>logical</b> surface step <c>S_high − S_low</c>; bed <see cref="HeightMap"/> does not define surface height.
    /// When high bed is not above low bed, adjusts <paramref name="lowH"/> so <c>highH &gt; lowH</c> for stack geometry
    /// (may be below <see cref="MIN_HEIGHT"/> only for the depth calculation at equal beds — §5.6.2, §12.7).
    /// </summary>
    private void GetEffectiveHeightsForWaterWaterCliff(
        int highX,
        int highY,
        int lowX,
        int lowY,
        int sHigh,
        int sLow,
        out int highH,
        out int lowH,
        out int segmentCount)
    {
        highH = heightMap.GetHeight(highX, highY);
        lowH = heightMap.GetHeight(lowX, lowY);
        int dS = sHigh - sLow;
        if (dS <= 0)
        {
            segmentCount = 0;
            return;
        }

        segmentCount = dS;
        if (highH <= lowH)
            lowH = Mathf.Max(MIN_HEIGHT, highH - dS);
        // Stack uses d = highH − lowH for segment count; beds may still coincide at MIN_HEIGHT after clamping.
        if (segmentCount > 0 && highH <= lowH)
            lowH = highH - segmentCount;
    }

    /// <summary>
    /// Instantiates water–water cascade prefabs for one cardinal step. Optional <paramref name="parentCellX"/> / <paramref name="parentCellY"/>
    /// parent to the lower pool when the visible south/east face lies on that cell (north/west lower neighbor — mirror placement).
    /// </summary>
    /// <param name="parentCellX">When set with <paramref name="parentCellY"/>, cliff children parent to this cell; otherwise to the upper pool.</param>
    /// <param name="waterSurfaceAnchorGridX">When set with <paramref name="waterSurfaceAnchorGridY"/>, <see cref="GridManager.GetWorldPositionVector"/> for the cascade anchor Y uses this grid cell (e.g. upper-brink shore) so isometric water-plane Y matches the parent shore tile (§12.8.1).</param>
    private void TryPlaceWaterCascadeCliffStack(
        int highX,
        int highY,
        int lowX,
        int lowY,
        int sHigh,
        int sLow,
        CliffCardinalFace face,
        GameObject waterCliffPrefab,
        int? parentCellX = null,
        int? parentCellY = null,
        int? waterSurfaceAnchorGridX = null,
        int? waterSurfaceAnchorGridY = null)
    {
        if (waterCliffPrefab == null || heightMap == null || gridManager == null)
            return;

        GetEffectiveHeightsForWaterWaterCliff(highX, highY, lowX, lowY, sHigh, sLow, out int highH, out int lowH, out int segmentCount);
        if (segmentCount <= 0 || highH <= lowH)
            return;

        int px = parentCellX ?? highX;
        int py = parentCellY ?? highY;
        Cell cell = gridManager.GetCell(px, py);
        if (cell == null)
            return;

        // Same Y band as the animated water tile child (WaterManager.PlaceWater): waterSurfaceWorld + (0, tileHeight*0.25).
        // Anchor uses upper pool visual surface; for shore parents use waterSurfaceAnchorGrid so isometric Y matches that tile (not only high cell center).
        int visualSurfaceHeight = Mathf.Max(MIN_HEIGHT, sHigh - 1);
        int anchorGx = waterSurfaceAnchorGridX ?? highX;
        int anchorGy = waterSurfaceAnchorGridY ?? highY;
        Vector2 waterSurfaceWorld = gridManager.GetWorldPositionVector(anchorGx, anchorGy, visualSurfaceHeight);
        float halfCellHeight = gridManager.tileHeight * 0.25f;
        float anchorY = waterSurfaceWorld.y + halfCellHeight;

        int? sortRx = waterSurfaceAnchorGridX;
        int? sortRy = waterSurfaceAnchorGridY;
        PlaceCliffWallStackCore(cell, waterCliffPrefab, face, highX, highY, lowX, lowY, highH, lowH, segmentCount, anchorY, sortRx, sortRy);
    }

    private void RemoveExistingWaterCascadeCliffs(Cell cell)
    {
        if (cell == null)
            return;
        for (int i = cell.transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = cell.transform.GetChild(i).gameObject;
            if (IsPrefabInstance(child, cliffWaterSouthPrefab) || IsPrefabInstance(child, cliffWaterEastPrefab))
                Destroy(child);
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

    /// <summary>
    /// Upper bound for cliff <see cref="SpriteRenderer.sortingOrder"/> so vertical cliff faces on <paramref name="highX"/>,<paramref name="highY"/>
    /// do not render above registered water in the 8-neighbor ring when that water is strictly in front in isometric depth
    /// (<c>nx+ny &lt; highX+highY</c>, same rule as <see cref="CalculateTerrainSortingOrder"/>). Returns <see cref="int.MaxValue"/> if none.
    /// </summary>
    private int GetMaxCliffSortingOrderFromForegroundWaterNeighbors(int highX, int highY)
    {
        if (gridManager == null || heightMap == null)
            return int.MaxValue;
        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();
        if (waterManager == null)
            return int.MaxValue;

        int highDepth = highX + highY;
        int maxAllowed = int.MaxValue;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue;
                int nx = highX + dx;
                int ny = highY + dy;
                if (!heightMap.IsValidPosition(nx, ny))
                    continue;
                if (nx + ny >= highDepth)
                    continue;
                if (!waterManager.IsWaterAt(nx, ny))
                    continue;
                Cell neighborCell = gridManager.GetCell(nx, ny);
                if (neighborCell == null)
                    continue;
                maxAllowed = Mathf.Min(maxAllowed, neighborCell.sortingOrder - 1);
            }
        }

        return maxAllowed;
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
        return GetNeighborWaterVisualHeightForShore(x, y, 0);
    }

    /// <summary>
    /// Same as <see cref="GetNeighborWaterVisualHeightForShore(int,int)"/>, but when <paramref name="affiliatedBodyId"/> is non-zero,
    /// only registered water neighbors whose body id matches contribute to the minimum (§12.8 upper-brink alignment).
    /// </summary>
    private int GetNeighborWaterVisualHeightForShore(int x, int y, int affiliatedBodyId)
    {
        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();
        WaterMap wm = waterManager != null ? waterManager.GetWaterMap() : null;

        bool IncludeNeighbor(int nx, int ny)
        {
            if (!WaterOrSeaAt(nx, ny))
                return false;
            if (affiliatedBodyId == 0 || wm == null)
                return true;
            if (waterManager.IsWaterAt(nx, ny))
                return wm.GetWaterBodyId(nx, ny) == affiliatedBodyId;
            return true;
        }

        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };
        int best = int.MaxValue;
        for (int i = 0; i < 4; i++)
        {
            int nx = x + dx[i];
            int ny = y + dy[i];
            if (!IncludeNeighbor(nx, ny))
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
                if (!IncludeNeighbor(nx, ny))
                    continue;
                int vis = GetWaterVisualHeightForNeighborCell(nx, ny);
                if (vis < best)
                    best = vis;
            }
        }
        if (best == int.MaxValue && affiliatedBodyId != 0)
            return GetNeighborWaterVisualHeightForShore(x, y, 0);
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
    private bool IsShoreBayPrefab(GameObject prefab)
    {
        if (prefab == null)
            return false;
        return prefab == northEastBayPrefab
            || prefab == northWestBayPrefab
            || prefab == southEastBayPrefab
            || prefab == southWestBayPrefab;
    }

    /// <summary>
    /// True when <paramref name="prefab"/> is any lake/coast shore asset from <see cref="PlaceWaterShore"/> (cardinal/corner slope water, upslope, Bay).
    /// Uses reference equality — same prefabs as <see cref="IsWaterSlopeObject"/> plus Bay.
    /// </summary>
    private bool IsWaterShoreTerrainPrefabAsset(GameObject prefab)
    {
        if (prefab == null)
            return false;
        return prefab == northSlopeWaterPrefab
            || prefab == southSlopeWaterPrefab
            || prefab == eastSlopeWaterPrefab
            || prefab == westSlopeWaterPrefab
            || prefab == northEastSlopeWaterPrefab
            || prefab == northWestSlopeWaterPrefab
            || prefab == southEastSlopeWaterPrefab
            || prefab == southWestSlopeWaterPrefab
            || prefab == northEastUpslopeWaterPrefab
            || prefab == northWestUpslopeWaterPrefab
            || prefab == southEastUpslopeWaterPrefab
            || prefab == southWestUpslopeWaterPrefab
            || IsShoreBayPrefab(prefab);
    }

    /// <summary>True when the cell's primary terrain prefab is a water-shore tile (see <see cref="PlaceWaterShore"/>).</summary>
    private bool CellUsesWaterShorePrimaryPrefab(Cell cell)
    {
        if (cell == null || string.IsNullOrEmpty(cell.prefabName))
            return false;
        GameObject p = FindTerrainPrefabByName(cell.prefabName);
        return IsWaterShoreTerrainPrefabAsset(p);
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
    private float GetShoreExtraWorldYOffset(GameObject prefab, int landH, int waterVisualH, int shorePrefabCount)
    {
        if (prefab == null || gridManager == null)
            return 0f;

        if (IsShoreBayPrefab(prefab))
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
    /// When <paramref name="forceCascadeJunctionSlopeWater"/> is true (river–river upper or lower brink, §12.8), uses the diagonal <c>*SlopeWaterPrefab</c> only.
    /// </summary>
    private List<GameObject> BuildDiagonalOnlyShorePrefabs(int x, int y, GameObject bayPrefab, GameObject upslopePrefab, GameObject downslopePrefab, bool isAxisAlignedRectangleCornerWater, bool forceCascadeJunctionSlopeWater = false)
    {
        if (forceCascadeJunctionSlopeWater && downslopePrefab != null)
            return new List<GameObject> { downslopePrefab };
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
    private bool IsAxisAlignedRectangleCornerWaterNorthEast(int x, int y, System.Func<int, int, bool> waterAt)
    {
        int wx = x + 1, wy = y - 1;
        if (!waterAt(wx, wy))
            return false;
        return !waterAt(wx + 1, wy) && !waterAt(wx, wy - 1);
    }

    private bool IsAxisAlignedRectangleCornerWaterNorthEast(int x, int y) =>
        IsAxisAlignedRectangleCornerWaterNorthEast(x, y, WaterOrSeaAt);

    /// <summary>
    /// True when diagonal water at NW of shore is the outer corner of an axis-aligned rectangle: no water further North or West of W.
    /// </summary>
    private bool IsAxisAlignedRectangleCornerWaterNorthWest(int x, int y, System.Func<int, int, bool> waterAt)
    {
        int wx = x + 1, wy = y + 1;
        if (!waterAt(wx, wy))
            return false;
        return !waterAt(wx + 1, wy) && !waterAt(wx, wy + 1);
    }

    private bool IsAxisAlignedRectangleCornerWaterNorthWest(int x, int y) =>
        IsAxisAlignedRectangleCornerWaterNorthWest(x, y, WaterOrSeaAt);

    /// <summary>
    /// True when diagonal water at SE of shore is the outer corner of an axis-aligned rectangle: no water further South or East of W.
    /// </summary>
    private bool IsAxisAlignedRectangleCornerWaterSouthEast(int x, int y, System.Func<int, int, bool> waterAt)
    {
        int wx = x - 1, wy = y - 1;
        if (!waterAt(wx, wy))
            return false;
        return !waterAt(wx - 1, wy) && !waterAt(wx, wy - 1);
    }

    private bool IsAxisAlignedRectangleCornerWaterSouthEast(int x, int y) =>
        IsAxisAlignedRectangleCornerWaterSouthEast(x, y, WaterOrSeaAt);

    /// <summary>
    /// True when diagonal water at SW of shore is the outer corner of an axis-aligned rectangle: no water further South or West of W.
    /// </summary>
    private bool IsAxisAlignedRectangleCornerWaterSouthWest(int x, int y, System.Func<int, int, bool> waterAt)
    {
        int wx = x - 1, wy = y + 1;
        if (!waterAt(wx, wy))
            return false;
        return !waterAt(wx - 1, wy) && !waterAt(wx, wy + 1);
    }

    private bool IsAxisAlignedRectangleCornerWaterSouthWest(int x, int y) =>
        IsAxisAlignedRectangleCornerWaterSouthWest(x, y, WaterOrSeaAt);

    private enum ShoreCornerQuadrant
    {
        SouthEast,
        SouthWest,
        NorthEast,
        NorthWest
    }

    /// <summary>
    /// True when both perpendicular cardinal neighbors are <b>registered</b> water with different logical surface heights
    /// (BUG-45 junction), <b>except</b> when that step is a <see cref="WaterMap.IsLakeSurfaceStepContactForbidden"/> edge — then
    /// the land cell should use normal lake-rim Bay / axis-aligned shore logic instead of multi-surface diagonal slopes (§12.7).
    /// </summary>
    private bool IsMultiSurfacePerpendicularWaterCorner(int x, int y, ShoreCornerQuadrant quadrant)
    {
        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();
        if (waterManager == null)
            return false;

        bool Reg(int cx, int cy, out int s)
        {
            s = -1;
            if (!waterManager.IsWaterAt(cx, cy))
                return false;
            s = waterManager.GetWaterSurfaceHeight(cx, cy);
            return s >= 0;
        }

        WaterMap wm = waterManager.GetWaterMap();

        switch (quadrant)
        {
            case ShoreCornerQuadrant.SouthEast:
            {
                if (!Reg(x - 1, y, out int sSouth) || !Reg(x, y - 1, out int sEast) || sSouth == sEast)
                    return false;
                if (wm != null)
                {
                    int hX, hY, lX, lY;
                    if (sSouth > sEast) { hX = x - 1; hY = y; lX = x; lY = y - 1; }
                    else { hX = x; hY = y - 1; lX = x - 1; lY = y; }
                    if (wm.IsLakeSurfaceStepContactForbidden(hX, hY, lX, lY))
                        return false;
                }
                return true;
            }
            case ShoreCornerQuadrant.SouthWest:
            {
                if (!Reg(x - 1, y, out int sSouth2) || !Reg(x, y + 1, out int sWest) || sSouth2 == sWest)
                    return false;
                if (wm != null)
                {
                    int hX, hY, lX, lY;
                    if (sSouth2 > sWest) { hX = x - 1; hY = y; lX = x; lY = y + 1; }
                    else { hX = x; hY = y + 1; lX = x - 1; lY = y; }
                    if (wm.IsLakeSurfaceStepContactForbidden(hX, hY, lX, lY))
                        return false;
                }
                return true;
            }
            case ShoreCornerQuadrant.NorthEast:
            {
                if (!Reg(x + 1, y, out int sNorth) || !Reg(x, y - 1, out int sEast2) || sNorth == sEast2)
                    return false;
                if (wm != null)
                {
                    int hX, hY, lX, lY;
                    if (sNorth > sEast2) { hX = x + 1; hY = y; lX = x; lY = y - 1; }
                    else { hX = x; hY = y - 1; lX = x + 1; lY = y; }
                    if (wm.IsLakeSurfaceStepContactForbidden(hX, hY, lX, lY))
                        return false;
                }
                return true;
            }
            case ShoreCornerQuadrant.NorthWest:
            {
                if (!Reg(x + 1, y, out int sNorth2) || !Reg(x, y + 1, out int sWest2) || sNorth2 == sWest2)
                    return false;
                if (wm != null)
                {
                    int hX, hY, lX, lY;
                    if (sNorth2 > sWest2) { hX = x + 1; hY = y; lX = x; lY = y + 1; }
                    else { hX = x; hY = y + 1; lX = x + 1; lY = y; }
                    if (wm.IsLakeSurfaceStepContactForbidden(hX, hY, lX, lY))
                        return false;
                }
                return true;
            }
            default:
                return false;
        }
    }

    /// <summary>
    /// True when the perpendicular-corner triangle (two cardinal water neighbors + the diagonal cell meeting at the land
    /// corner) is fully registered water and the three cells do not share one logical surface <c>S</c>. Covers cascade
    /// junctions where both cardinals belong to the lower pool (same <c>S</c>) but the diagonal is upper pool — then
    /// <see cref="IsMultiSurfacePerpendicularWaterCorner"/> is false and <see cref="IsAxisAlignedRectangleCornerWaterSouthWest"/>
    /// (and siblings) would wrongly choose Bay (concave) instead of <c>*SlopeWaterPrefab</c> (convex land tip).
    /// </summary>
    private bool IsMixedSurfaceThreeCellPerpendicularCorner(int x, int y, ShoreCornerQuadrant quadrant)
    {
        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();
        if (waterManager == null || heightMap == null)
            return false;

        int ax, ay, bx, by, cx, cy;
        switch (quadrant)
        {
            case ShoreCornerQuadrant.SouthEast:
                ax = x - 1; ay = y;
                bx = x; by = y - 1;
                cx = x - 1; cy = y - 1;
                break;
            case ShoreCornerQuadrant.SouthWest:
                ax = x - 1; ay = y;
                bx = x; by = y + 1;
                cx = x - 1; cy = y + 1;
                break;
            case ShoreCornerQuadrant.NorthEast:
                ax = x + 1; ay = y;
                bx = x; by = y - 1;
                cx = x + 1; cy = y - 1;
                break;
            case ShoreCornerQuadrant.NorthWest:
                ax = x + 1; ay = y;
                bx = x; by = y + 1;
                cx = x + 1; cy = y + 1;
                break;
            default:
                return false;
        }

        bool TryS(int px, int py, out int s)
        {
            s = -1;
            if (!heightMap.IsValidPosition(px, py) || !waterManager.IsWaterAt(px, py))
                return false;
            s = waterManager.GetWaterSurfaceHeight(px, py);
            return s >= 0;
        }

        if (!TryS(ax, ay, out int sa) || !TryS(bx, by, out int sb) || !TryS(cx, cy, out int sc))
            return false;
        return sa != sb || sb != sc || sa != sc;
    }

    /// <summary>
    /// Bay vs corner slope for one perpendicular pair (two cardinals wet). Returns null if both Bay and SlopeWater prefabs are missing.
    /// When <paramref name="forceCascadeJunctionSlopeWater"/> (river–river junction brink, §12.8), returns the diagonal <c>*SlopeWaterPrefab</c> only, matching <see cref="BuildDiagonalOnlyShorePrefabs"/>.
    /// </summary>
    private List<GameObject> SelectPerpendicularWaterCornerPrefabs(int x, int y, ShoreCornerQuadrant quadrant, bool forceCascadeJunctionSlopeWater = false)
    {
        GameObject bayPrefab;
        GameObject slopePrefab;
        switch (quadrant)
        {
            case ShoreCornerQuadrant.SouthEast:
                bayPrefab = southEastBayPrefab;
                slopePrefab = southEastSlopeWaterPrefab;
                if (forceCascadeJunctionSlopeWater && slopePrefab != null)
                    return ShoreList(slopePrefab);
                if (IsMultiSurfacePerpendicularWaterCorner(x, y, ShoreCornerQuadrant.SouthEast))
                {
                    if (slopePrefab != null) return ShoreList(slopePrefab);
                    if (bayPrefab != null) return ShoreList(bayPrefab);
                    return null;
                }
                if (IsMixedSurfaceThreeCellPerpendicularCorner(x, y, ShoreCornerQuadrant.SouthEast))
                {
                    if (slopePrefab != null) return ShoreList(slopePrefab);
                    if (bayPrefab != null) return ShoreList(bayPrefab);
                    return null;
                }
                if (IsAxisAlignedRectangleCornerWaterSouthEast(x, y))
                {
                    if (bayPrefab != null) return ShoreList(bayPrefab);
                    if (slopePrefab != null) return ShoreList(slopePrefab);
                    return null;
                }
                if (HasLandSlopeIgnoringWater(x, y))
                {
                    if (bayPrefab != null) return ShoreList(bayPrefab);
                    if (slopePrefab != null) return ShoreList(slopePrefab);
                    return null;
                }
                if (slopePrefab != null) return ShoreList(slopePrefab);
                if (bayPrefab != null) return ShoreList(bayPrefab);
                return null;
            case ShoreCornerQuadrant.SouthWest:
                bayPrefab = southWestBayPrefab;
                slopePrefab = southWestSlopeWaterPrefab;
                if (forceCascadeJunctionSlopeWater && slopePrefab != null)
                    return ShoreList(slopePrefab);
                if (IsMultiSurfacePerpendicularWaterCorner(x, y, ShoreCornerQuadrant.SouthWest))
                {
                    if (slopePrefab != null) return ShoreList(slopePrefab);
                    if (bayPrefab != null) return ShoreList(bayPrefab);
                    return null;
                }
                if (IsMixedSurfaceThreeCellPerpendicularCorner(x, y, ShoreCornerQuadrant.SouthWest))
                {
                    if (slopePrefab != null) return ShoreList(slopePrefab);
                    if (bayPrefab != null) return ShoreList(bayPrefab);
                    return null;
                }
                if (IsAxisAlignedRectangleCornerWaterSouthWest(x, y))
                {
                    if (bayPrefab != null) return ShoreList(bayPrefab);
                    if (slopePrefab != null) return ShoreList(slopePrefab);
                    return null;
                }
                if (HasLandSlopeIgnoringWater(x, y))
                {
                    if (bayPrefab != null) return ShoreList(bayPrefab);
                    if (slopePrefab != null) return ShoreList(slopePrefab);
                    return null;
                }
                if (slopePrefab != null) return ShoreList(slopePrefab);
                if (bayPrefab != null) return ShoreList(bayPrefab);
                return null;
            case ShoreCornerQuadrant.NorthEast:
                bayPrefab = northEastBayPrefab;
                slopePrefab = northEastSlopeWaterPrefab;
                if (forceCascadeJunctionSlopeWater && slopePrefab != null)
                    return ShoreList(slopePrefab);
                if (IsMultiSurfacePerpendicularWaterCorner(x, y, ShoreCornerQuadrant.NorthEast))
                {
                    if (slopePrefab != null) return ShoreList(slopePrefab);
                    if (bayPrefab != null) return ShoreList(bayPrefab);
                    return null;
                }
                if (IsMixedSurfaceThreeCellPerpendicularCorner(x, y, ShoreCornerQuadrant.NorthEast))
                {
                    if (slopePrefab != null) return ShoreList(slopePrefab);
                    if (bayPrefab != null) return ShoreList(bayPrefab);
                    return null;
                }
                if (IsAxisAlignedRectangleCornerWaterNorthEast(x, y))
                {
                    if (bayPrefab != null) return ShoreList(bayPrefab);
                    if (slopePrefab != null) return ShoreList(slopePrefab);
                    return null;
                }
                if (HasLandSlopeIgnoringWater(x, y))
                {
                    if (bayPrefab != null) return ShoreList(bayPrefab);
                    if (slopePrefab != null) return ShoreList(slopePrefab);
                    return null;
                }
                if (slopePrefab != null) return ShoreList(slopePrefab);
                if (bayPrefab != null) return ShoreList(bayPrefab);
                return null;
            case ShoreCornerQuadrant.NorthWest:
                bayPrefab = northWestBayPrefab;
                slopePrefab = northWestSlopeWaterPrefab;
                if (forceCascadeJunctionSlopeWater && slopePrefab != null)
                    return ShoreList(slopePrefab);
                if (IsMultiSurfacePerpendicularWaterCorner(x, y, ShoreCornerQuadrant.NorthWest))
                {
                    if (slopePrefab != null) return ShoreList(slopePrefab);
                    if (bayPrefab != null) return ShoreList(bayPrefab);
                    return null;
                }
                if (IsMixedSurfaceThreeCellPerpendicularCorner(x, y, ShoreCornerQuadrant.NorthWest))
                {
                    if (slopePrefab != null) return ShoreList(slopePrefab);
                    if (bayPrefab != null) return ShoreList(bayPrefab);
                    return null;
                }
                if (IsAxisAlignedRectangleCornerWaterNorthWest(x, y))
                {
                    if (bayPrefab != null) return ShoreList(bayPrefab);
                    if (slopePrefab != null) return ShoreList(slopePrefab);
                    return null;
                }
                if (HasLandSlopeIgnoringWater(x, y))
                {
                    if (bayPrefab != null) return ShoreList(bayPrefab);
                    if (slopePrefab != null) return ShoreList(slopePrefab);
                    return null;
                }
                if (slopePrefab != null) return ShoreList(slopePrefab);
                if (bayPrefab != null) return ShoreList(bayPrefab);
                return null;
        }

        return null;
    }

    /// <summary>
    /// When exactly three cardinal neighbors are water, two perpendicular pairs are both true; pick the inner corner using diagonal water
    /// (same tie-break order as legacy SE/SW/NE/NW when diagonals agree or are both dry).
    /// </summary>
    private List<GameObject> TrySelectShoreForExactlyThreeCardinalWaters(
        int x, int y,
        bool hasWaterAtNorth, bool hasWaterAtSouth, bool hasWaterAtEast, bool hasWaterAtWest,
        bool hasWaterAtNorthEast, bool hasWaterAtNorthWest, bool hasWaterAtSouthEast, bool hasWaterAtSouthWest,
        bool forceCascadeJunctionSlopeWater = false)
    {
        int count = (hasWaterAtNorth ? 1 : 0) + (hasWaterAtSouth ? 1 : 0) + (hasWaterAtEast ? 1 : 0) + (hasWaterAtWest ? 1 : 0);
        if (count != 3)
            return null;

        if (!hasWaterAtNorth)
        {
            if (hasWaterAtSouthEast && !hasWaterAtSouthWest)
                return SelectPerpendicularWaterCornerPrefabs(x, y, ShoreCornerQuadrant.SouthEast, forceCascadeJunctionSlopeWater);
            if (hasWaterAtSouthWest && !hasWaterAtSouthEast)
                return SelectPerpendicularWaterCornerPrefabs(x, y, ShoreCornerQuadrant.SouthWest, forceCascadeJunctionSlopeWater);
            List<GameObject> southEastFirst = SelectPerpendicularWaterCornerPrefabs(x, y, ShoreCornerQuadrant.SouthEast, forceCascadeJunctionSlopeWater);
            if (southEastFirst != null)
                return southEastFirst;
            return SelectPerpendicularWaterCornerPrefabs(x, y, ShoreCornerQuadrant.SouthWest, forceCascadeJunctionSlopeWater);
        }
        if (!hasWaterAtSouth)
        {
            if (hasWaterAtNorthEast && !hasWaterAtNorthWest)
                return SelectPerpendicularWaterCornerPrefabs(x, y, ShoreCornerQuadrant.NorthEast, forceCascadeJunctionSlopeWater);
            if (hasWaterAtNorthWest && !hasWaterAtNorthEast)
                return SelectPerpendicularWaterCornerPrefabs(x, y, ShoreCornerQuadrant.NorthWest, forceCascadeJunctionSlopeWater);
            List<GameObject> northEastFirst = SelectPerpendicularWaterCornerPrefabs(x, y, ShoreCornerQuadrant.NorthEast, forceCascadeJunctionSlopeWater);
            if (northEastFirst != null)
                return northEastFirst;
            return SelectPerpendicularWaterCornerPrefabs(x, y, ShoreCornerQuadrant.NorthWest, forceCascadeJunctionSlopeWater);
        }
        if (!hasWaterAtEast)
        {
            if (hasWaterAtNorthWest && !hasWaterAtSouthWest)
                return SelectPerpendicularWaterCornerPrefabs(x, y, ShoreCornerQuadrant.NorthWest, forceCascadeJunctionSlopeWater);
            if (hasWaterAtSouthWest && !hasWaterAtNorthWest)
                return SelectPerpendicularWaterCornerPrefabs(x, y, ShoreCornerQuadrant.SouthWest, forceCascadeJunctionSlopeWater);
            List<GameObject> southWestFirst = SelectPerpendicularWaterCornerPrefabs(x, y, ShoreCornerQuadrant.SouthWest, forceCascadeJunctionSlopeWater);
            if (southWestFirst != null)
                return southWestFirst;
            return SelectPerpendicularWaterCornerPrefabs(x, y, ShoreCornerQuadrant.NorthWest, forceCascadeJunctionSlopeWater);
        }
        if (!hasWaterAtWest)
        {
            if (hasWaterAtNorthEast && !hasWaterAtSouthEast)
                return SelectPerpendicularWaterCornerPrefabs(x, y, ShoreCornerQuadrant.NorthEast, forceCascadeJunctionSlopeWater);
            if (hasWaterAtSouthEast && !hasWaterAtNorthEast)
                return SelectPerpendicularWaterCornerPrefabs(x, y, ShoreCornerQuadrant.SouthEast, forceCascadeJunctionSlopeWater);
            List<GameObject> southEastLead = SelectPerpendicularWaterCornerPrefabs(x, y, ShoreCornerQuadrant.SouthEast, forceCascadeJunctionSlopeWater);
            if (southEastLead != null)
                return southEastLead;
            return SelectPerpendicularWaterCornerPrefabs(x, y, ShoreCornerQuadrant.NorthEast, forceCascadeJunctionSlopeWater);
        }
        return null;
    }

    /// <summary>
    /// Two cardinal offsets from a shore cell to probe for cascade: only along the shore-line axis (both sides), e.g. E+W for an
    /// E–W shoreline, N+S for an N–S shoreline. Uses the same cardinal water pattern as <see cref="DetermineWaterShorePrefabs"/>.
    /// </summary>
    private static bool TryGetShoreLineSearchOffsets(
        bool hasWaterAtNorth, bool hasWaterAtSouth, bool hasWaterAtEast, bool hasWaterAtWest,
        out int dx0, out int dy0, out int dx1, out int dy1)
    {
        dx0 = dy0 = dx1 = dy1 = 0;
        bool n = hasWaterAtNorth, s = hasWaterAtSouth, e = hasWaterAtEast, w = hasWaterAtWest;

        // N–S water strip (column): shoreline runs E–W → search east and west only.
        if (n && s)
        {
            dx0 = 0; dy0 = -1; // East (x, y-1)
            dx1 = 0; dy1 = 1;  // West (x, y+1)
            return true;
        }

        // E–W water strip (row): shoreline runs N–S → search north and south only.
        if (e && w)
        {
            dx0 = 1; dy0 = 0;  // North (x+1, y)
            dx1 = -1; dy1 = 0; // South (x-1, y)
            return true;
        }

        // Single cardinal toward water: shoreline is perpendicular to that → search the two neighbors along the parallel axis.
        if (n && !s && !e && !w) { dx0 = 0; dy0 = -1; dx1 = 0; dy1 = 1; return true; }
        if (s && !n && !e && !w) { dx0 = 0; dy0 = -1; dx1 = 0; dy1 = 1; return true; }
        if (e && !w && !n && !s) { dx0 = 1; dy0 = 0; dx1 = -1; dy1 = 0; return true; }
        if (w && !e && !n && !s) { dx0 = 1; dy0 = 0; dx1 = -1; dy1 = 0; return true; }

        // Outer corners (two perpendicular cardinals).
        if (n && e && !s && !w) { dx0 = 0; dy0 = -1; dx1 = 0; dy1 = 1; return true; }
        if (n && w && !s && !e) { dx0 = 0; dy0 = -1; dx1 = 0; dy1 = 1; return true; }
        if (s && e && !n && !w) { dx0 = 0; dy0 = -1; dx1 = 0; dy1 = 1; return true; }
        if (s && w && !n && !e) { dx0 = 0; dy0 = -1; dx1 = 0; dy1 = 1; return true; }
        if (e && s && !n && !w) { dx0 = 1; dy0 = 0; dx1 = -1; dy1 = 0; return true; }

        return false;
    }

    /// <summary>
    /// True when <paramref name="cx"/>,<paramref name="cy"/> lies on the lower-pool cascade / junction strip: registered water
    /// on the low side of a cardinal surface step (same <paramref name="ownerBodyId"/>), or dry land with the same shore
    /// affiliation that connects along the shore-line axis only to such water (Pass B strip may include dry cells).
    /// <paramref name="axisDx0"/>/<paramref name="axisDy0"/> and <paramref name="axisDx1"/>/<paramref name="axisDy1"/> are the two
    /// cardinal directions along the shore line (e.g. east and west); dry traversal does not step off that axis.
    /// </summary>
    private bool IsOnLowerCascadeOrJunctionStrip(
        int cx, int cy, int ownerBodyId, WaterMap wm, HashSet<Vector2Int> excludeFromTraversal, int depthRemaining,
        int axisDx0, int axisDy0, int axisDx1, int axisDy1)
    {
        if (depthRemaining <= 0 || wm == null || !wm.IsValidPosition(cx, cy))
            return false;
        var key = new Vector2Int(cx, cy);
        if (excludeFromTraversal.Contains(key))
            return false;
        excludeFromTraversal.Add(key);

        if (wm.IsWater(cx, cy))
        {
            if (wm.GetWaterBodyId(cx, cy) != ownerBodyId)
                return false;
            return wm.IsWaterCellLowerSideOfCardinalSurfaceStep(cx, cy);
        }

        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();
        if (waterManager == null || waterManager.GetShoreAffiliatedWaterBodyIdForLandCell(cx, cy) != ownerBodyId)
            return false;

        int nx0 = cx + axisDx0;
        int ny0 = cy + axisDy0;
        if (wm.IsValidPosition(nx0, ny0) &&
            IsOnLowerCascadeOrJunctionStrip(nx0, ny0, ownerBodyId, wm, excludeFromTraversal, depthRemaining - 1, axisDx0, axisDy0, axisDx1, axisDy1))
            return true;

        int nx1 = cx + axisDx1;
        int ny1 = cy + axisDy1;
        if (wm.IsValidPosition(nx1, ny1) &&
            IsOnLowerCascadeOrJunctionStrip(nx1, ny1, ownerBodyId, wm, excludeFromTraversal, depthRemaining - 1, axisDx0, axisDy0, axisDx1, axisDy1))
            return true;

        return false;
    }

    /// <summary>
    /// When true, this dry land shore cell should use a <c>*SlopeWaterPrefab</c> corner (not <c>*UpslopeWaterPrefab</c>) to close
    /// the shore line before the terrain/water surface step at a cascade or multi-surface junction: the cell is affiliated with
    /// the lower pool (<paramref name="ownerBodyId"/>), touches the cascade/junction strip along the shore-line axis only, and
    /// <paramref name="quadrant"/> is the perpendicular corner orientation (multi-surface or mixed three-cell pattern).
    /// </summary>
    private bool ShouldPlaceShoreEnd(
        int x, int y, int ownerBodyId,
        bool hasWaterAtNorth, bool hasWaterAtSouth, bool hasWaterAtEast, bool hasWaterAtWest,
        out ShoreCornerQuadrant quadrant)
    {
        quadrant = default;
        if (ownerBodyId == 0 || heightMap == null || !heightMap.IsValidPosition(x, y))
            return false;
        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();
        if (waterManager == null || waterManager.IsWaterAt(x, y))
            return false;

        WaterMap wm = waterManager.GetWaterMap();
        if (wm != null && wm.TryGetDryLandRiverJunctionBrink(x, y, out RiverJunctionBrinkRole brinkRole, out _)
            && brinkRole == RiverJunctionBrinkRole.UpperBrink)
            return false;

        if (!TryGetShoreLineSearchOffsets(hasWaterAtNorth, hasWaterAtSouth, hasWaterAtEast, hasWaterAtWest, out int ax0, out int ay0, out int ax1, out int ay1))
            return false;

        if (wm == null)
            return false;

        bool touchesCascadeStrip = false;
        int lx0 = x + ax0;
        int ly0 = y + ay0;
        if (wm.IsValidPosition(lx0, ly0))
        {
            var visited0 = new HashSet<Vector2Int>();
            visited0.Add(new Vector2Int(x, y));
            if (IsOnLowerCascadeOrJunctionStrip(lx0, ly0, ownerBodyId, wm, visited0, 32, ax0, ay0, ax1, ay1))
                touchesCascadeStrip = true;
        }
        if (!touchesCascadeStrip)
        {
            int lx1 = x + ax1;
            int ly1 = y + ay1;
            if (wm.IsValidPosition(lx1, ly1))
            {
                var visited1 = new HashSet<Vector2Int>();
                visited1.Add(new Vector2Int(x, y));
                if (IsOnLowerCascadeOrJunctionStrip(lx1, ly1, ownerBodyId, wm, visited1, 32, ax0, ay0, ax1, ay1))
                    touchesCascadeStrip = true;
            }
        }
        if (!touchesCascadeStrip)
            return false;

        ShoreCornerQuadrant[] order =
        {
            ShoreCornerQuadrant.SouthEast,
            ShoreCornerQuadrant.SouthWest,
            ShoreCornerQuadrant.NorthEast,
            ShoreCornerQuadrant.NorthWest
        };
        foreach (ShoreCornerQuadrant q in order)
        {
            if (IsMultiSurfacePerpendicularWaterCorner(x, y, q) || IsMixedSurfaceThreeCellPerpendicularCorner(x, y, q))
            {
                quadrant = q;
                return true;
            }
        }
        return false;
    }

    private GameObject GetSlopeWaterPrefabForQuadrant(ShoreCornerQuadrant quadrant)
    {
        switch (quadrant)
        {
            case ShoreCornerQuadrant.SouthEast: return southEastSlopeWaterPrefab;
            case ShoreCornerQuadrant.SouthWest: return southWestSlopeWaterPrefab;
            case ShoreCornerQuadrant.NorthEast: return northEastSlopeWaterPrefab;
            case ShoreCornerQuadrant.NorthWest: return northWestSlopeWaterPrefab;
            default: return null;
        }
    }

    /// <summary>
    /// Selects lake/coast shore prefab(s) for a land cell adjacent to water. Returns one prefab or an upslope+downslope pair for diagonal slopes.
    /// Moore neighbor wet/dry uses <see cref="WaterManager.IsOpenWaterForShoreTopology"/> (affiliated body) or <see cref="WaterOrSeaAt"/> (no affiliation), unless
    /// <paramref name="useJunctionTopologyForShorePattern"/> uses <see cref="WaterManager.NeighborMatchesShoreOwnerForJunctionTopology"/> for the junction post-pass (§12.8.1).
    /// Perpendicular two-cardinal corners: when both cardinals are <b>registered</b> water with different logical surfaces (BUG-45), prefer
    /// diagonal <c>*SlopeWaterPrefab</c> over Bay <b>unless</b> that edge is <see cref="WaterMap.IsLakeSurfaceStepContactForbidden"/> (§12.7 — lake rim vs lower pool, no junction).
    /// Else Bay when the diagonal water cell is an axis-aligned rectangle outer corner; when not, prefer Bay if
    /// <see cref="HasLandSlopeIgnoringWater"/> (cliff rim), else SlopeWater then Bay (convex land tip / large-lake shore).
    /// Pure cardinal north or south (no east/west water on those branches) uses north/south slope only — not <see cref="BuildDiagonalOnlyShorePrefabs"/>.
    /// River confluences and non-rectangular water patterns: isometric spec §5.9; refresh land after river stamps via <see cref="RefreshShoreTerrainAfterWaterUpdate"/>.
    /// </summary>
    /// <param name="useJunctionTopologyForShorePattern">When true, junction-brink dry neighbors count as wet (post-pass only).</param>
    /// <param name="forceJunctionDiagonalSlopeForCascade">When true, forces diagonal <c>*SlopeWaterPrefab</c> over Bay for this cell (post-pass along the cascade strip).</param>
    private List<GameObject> DetermineWaterShorePrefabs(int x, int y, bool useJunctionTopologyForShorePattern = false, bool forceJunctionDiagonalSlopeForCascade = false)
    {
        if (heightMap == null)
            return null;
        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();

        int ownerBodyId = waterManager != null ? waterManager.GetShoreAffiliatedWaterBodyIdForLandCell(x, y) : 0;
        bool PatternWater(int nx, int ny)
        {
            if (ownerBodyId == 0)
                return WaterOrSeaAt(nx, ny);
            if (useJunctionTopologyForShorePattern)
                return waterManager != null && waterManager.NeighborMatchesShoreOwnerForJunctionTopology(nx, ny, ownerBodyId);
            return waterManager != null && waterManager.IsOpenWaterForShoreTopology(nx, ny, ownerBodyId);
        }

        bool hasWaterAtNorth = PatternWater(x + 1, y);
        bool hasWaterAtSouth = PatternWater(x - 1, y);
        bool hasWaterAtWest = PatternWater(x, y + 1);
        bool hasWaterAtEast = PatternWater(x, y - 1);

        bool hasWaterAtNorthEast = PatternWater(x + 1, y - 1);
        bool hasWaterAtNorthWest = PatternWater(x + 1, y + 1);
        bool hasWaterAtSouthEast = PatternWater(x - 1, y - 1);
        bool hasWaterAtSouthWest = PatternWater(x - 1, y + 1);

        bool forceCascadeJunctionSlopeWater = forceJunctionDiagonalSlopeForCascade
            || (waterManager != null && waterManager.ShouldForceDiagonalSlopeWaterAtRiverJunctionBrink(x, y));

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

        // Cascade / junction lower-pool shore cap: force corner SlopeWater (not Bay) when closing the shore line toward a surface step.
        if (ShouldPlaceShoreEnd(x, y, ownerBodyId, hasWaterAtNorth, hasWaterAtSouth, hasWaterAtEast, hasWaterAtWest, out ShoreCornerQuadrant shoreEndQuadrant))
        {
            GameObject shoreEndPrefab = GetSlopeWaterPrefabForQuadrant(shoreEndQuadrant);
            if (shoreEndPrefab != null)
                return ShoreList(shoreEndPrefab);
        }

        // Multi-surface perpendicular corners (BUG-45 / §12.7): two cardinals are water at different logical S.
        // Run before TrySelectShoreForExactlyThreeCardinalWaters so cascade/junction cells prefer *SlopeWaterPrefab over Bay
        // when three cardinals match PatternWater but the perpendicular pair is a surface step.
        if (IsMultiSurfacePerpendicularWaterCorner(x, y, ShoreCornerQuadrant.SouthEast))
        {
            List<GameObject> cornerMs = SelectPerpendicularWaterCornerPrefabs(x, y, ShoreCornerQuadrant.SouthEast, forceCascadeJunctionSlopeWater);
            if (cornerMs != null)
                return cornerMs;
        }
        if (IsMultiSurfacePerpendicularWaterCorner(x, y, ShoreCornerQuadrant.SouthWest))
        {
            List<GameObject> cornerMs = SelectPerpendicularWaterCornerPrefabs(x, y, ShoreCornerQuadrant.SouthWest, forceCascadeJunctionSlopeWater);
            if (cornerMs != null)
                return cornerMs;
        }
        if (IsMultiSurfacePerpendicularWaterCorner(x, y, ShoreCornerQuadrant.NorthEast))
        {
            List<GameObject> cornerMs = SelectPerpendicularWaterCornerPrefabs(x, y, ShoreCornerQuadrant.NorthEast, forceCascadeJunctionSlopeWater);
            if (cornerMs != null)
                return cornerMs;
        }
        if (IsMultiSurfacePerpendicularWaterCorner(x, y, ShoreCornerQuadrant.NorthWest))
        {
            List<GameObject> cornerMs = SelectPerpendicularWaterCornerPrefabs(x, y, ShoreCornerQuadrant.NorthWest, forceCascadeJunctionSlopeWater);
            if (cornerMs != null)
                return cornerMs;
        }

        // Perpendicular shore corners: both cardinals of a quadrant have water.
        // Bay = concave water corner (outer axis-aligned corner of the water patch: see IsAxisAlignedRectangleCornerWater*).
        // SlopeWater = convex land corner when water continues past the diagonal (peninsula tip, island corners, large lakes).
        // When not a rectangle outer corner but a higher land neighbor exists (cliff rim), prefer Bay like flat lakes.
        // Exactly three cardinal waters: two pairs match — resolve using diagonal water before pairwise order (legacy: SE, SW, NE, NW).
        List<GameObject> threeCardinalShore = TrySelectShoreForExactlyThreeCardinalWaters(
            x, y,
            hasWaterAtNorth, hasWaterAtSouth, hasWaterAtEast, hasWaterAtWest,
            hasWaterAtNorthEast, hasWaterAtNorthWest, hasWaterAtSouthEast, hasWaterAtSouthWest,
            forceCascadeJunctionSlopeWater);
        if (threeCardinalShore != null)
            return threeCardinalShore;

        if (hasWaterAtSouth && hasWaterAtEast)
        {
            List<GameObject> corner = SelectPerpendicularWaterCornerPrefabs(x, y, ShoreCornerQuadrant.SouthEast, forceCascadeJunctionSlopeWater);
            if (corner != null)
                return corner;
        }
        if (hasWaterAtSouth && hasWaterAtWest)
        {
            List<GameObject> corner = SelectPerpendicularWaterCornerPrefabs(x, y, ShoreCornerQuadrant.SouthWest, forceCascadeJunctionSlopeWater);
            if (corner != null)
                return corner;
        }
        if (hasWaterAtNorth && hasWaterAtEast)
        {
            List<GameObject> corner = SelectPerpendicularWaterCornerPrefabs(x, y, ShoreCornerQuadrant.NorthEast, forceCascadeJunctionSlopeWater);
            if (corner != null)
                return corner;
        }
        if (hasWaterAtNorth && hasWaterAtWest)
        {
            List<GameObject> corner = SelectPerpendicularWaterCornerPrefabs(x, y, ShoreCornerQuadrant.NorthWest, forceCascadeJunctionSlopeWater);
            if (corner != null)
                return corner;
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
                // Cardinal north only (no east/west water): always straight north ramp — including beside lake corners,
                // where only one of NE/NW is wet; BuildDiagonalOnly/Bay is for tail diagonal-only patterns without this cardinal.
                return ShoreList(northSlopeWaterPrefab);
            }
            else
            {
                return ShoreList(southEastUpslopeWaterPrefab);
            }
        }

        // Water to the south only (water north is handled above): mirror East — pure south or SE/SW upslope when E/W cardinal water.
        if (hasWaterAtSouth)
        {
            if (!hasWaterAtNorth)
            {
                if (!hasWaterAtWest && !hasWaterAtEast)
                {
                    // Cardinal south only: always straight south ramp (same rationale as north branch).
                    return ShoreList(southSlopeWaterPrefab);
                }
                if (!hasWaterAtWest && hasWaterAtEast)
                    return ShoreList(southEastUpslopeWaterPrefab);
                if (hasWaterAtWest && !hasWaterAtEast)
                    return ShoreList(southWestUpslopeWaterPrefab);
                return ShoreList(southSlopeWaterPrefab);
            }
        }

        if (hasWaterAtNorthEast && !hasWaterAtSouth)
            return BuildDiagonalOnlyShorePrefabs(x, y, northEastBayPrefab, northEastUpslopeWaterPrefab, northEastSlopeWaterPrefab, IsAxisAlignedRectangleCornerWaterNorthEast(x, y, PatternWater), forceCascadeJunctionSlopeWater);

        if (hasWaterAtNorthWest && !hasWaterAtSouth)
            return BuildDiagonalOnlyShorePrefabs(x, y, northWestBayPrefab, northWestUpslopeWaterPrefab, northWestSlopeWaterPrefab, IsAxisAlignedRectangleCornerWaterNorthWest(x, y, PatternWater), forceCascadeJunctionSlopeWater);

        if (hasWaterAtSouthEast && !hasWaterAtNorth)
            return BuildDiagonalOnlyShorePrefabs(x, y, southEastBayPrefab, southEastUpslopeWaterPrefab, southEastSlopeWaterPrefab, IsAxisAlignedRectangleCornerWaterSouthEast(x, y, PatternWater), forceCascadeJunctionSlopeWater);

        if (hasWaterAtSouthWest && !hasWaterAtNorth)
            return BuildDiagonalOnlyShorePrefabs(x, y, southWestBayPrefab, southWestUpslopeWaterPrefab, southWestSlopeWaterPrefab, IsAxisAlignedRectangleCornerWaterSouthWest(x, y, PatternWater), forceCascadeJunctionSlopeWater);

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
        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();
        int aff = waterManager != null ? waterManager.GetShoreAffiliatedWaterBodyIdForLandCell(x, y) : 0;
        int waterVisualH = GetNeighborWaterVisualHeightForShore(x, y, aff);
        return CalculateTerrainSortingOrder(x, y, waterVisualH) + WATER_SLOPE_OFFSET;
    }

    /// <summary>
    /// Sorting for ShoreBay (inner 90°) shore tiles only: same base as <see cref="CalculateWaterSlopeSortingOrder"/>,
    /// then at least one step above adjacent water tiles so isometric neighbors do not cover the corner.
    /// </summary>
    public int CalculateShoreBaySortingOrder(int x, int y)
    {
        const int BAY_SHORE_MIN_OFFSET = 1;
        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();
        int aff = waterManager != null ? waterManager.GetShoreAffiliatedWaterBodyIdForLandCell(x, y) : 0;
        int waterVisualH = GetNeighborWaterVisualHeightForShore(x, y, aff);
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
            || IsShoreBayObject(obj);
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
    public bool IsShoreBayObject(GameObject obj)
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
    /// Implements <see cref="ITerrainManager.CanPlaceRoad(int, int)"/>; does not allow shore trace (use overload for FEAT-44).
    /// </summary>
    public bool CanPlaceRoad(int x, int y)
    {
        return CanPlaceRoad(x, y, allowWaterSlopeForWaterBridgeTrace: false);
    }

    /// <summary>
    /// Same as <see cref="CanPlaceRoad(int, int)"/> with optional shore allowance for pathfinding and manual bridge strokes.
    /// </summary>
    /// <param name="allowWaterSlopeForWaterBridgeTrace">
    /// When true (pathfinding / manual road stroke only), water-slope shore cells may pass so a shared
    /// <see cref="Territory.Roads.RoadManager.TryPrepareRoadPlacementPlan"/> pass can validate water bridges (FEAT-44).
    /// Single-tile placement and zoning must keep this false.
    /// </param>
    public bool CanPlaceRoad(int x, int y, bool allowWaterSlopeForWaterBridgeTrace)
    {
        if (gridManager != null && gridManager.IsCellOccupiedByBuilding(x, y))
            return false;
        if (gridManager != null)
        {
            Cell c = gridManager.GetCell(x, y);
            if (c != null && c.GetCellInstanceHeight() == 0)
                return true;
        }
        if (IsWaterSlopeCell(x, y) && !allowWaterSlopeForWaterBridgeTrace)
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
