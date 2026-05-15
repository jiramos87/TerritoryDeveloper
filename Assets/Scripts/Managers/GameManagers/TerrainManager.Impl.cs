// long-file-allowed: legacy hub — scope outside current atomization plan; deferred to future sweep
using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;
using Territory.Core;
using Territory.Zones;
using Territory.Persistence;

namespace Territory.Terrain
{
// Impl partial — private methods + renamed public entry points for hub delegates.

public partial class TerrainManager
{
    /// <summary>Cardinal offsets (S, N, E, W) for neighbor scans — order matches four-direction loops.</summary>
    static readonly int[] CardinalDx = { -1, 1, 0, 0 };
    static readonly int[] CardinalDy = { 0, 0, -1, 1 };

    /// <summary>Cliff sprites must sort strictly below cell's primary terrain/shore sprite.</summary>
    private const int CliffSortingBelowCellTerrain = 1;

    /// <summary>
    /// S/E face toward off-grid void: water-shore primary prefabs already include transition art on that edge → skip duplicate brown cliff stacks.
    /// </summary>
    private bool ShouldSuppressBrownCliffTowardOffGridForWaterShorePrimary(CityCell cell)
    {
        return cell != null && CellUsesWaterShorePrimaryPrefab(cell);
    }

    private bool IsRegisteredOpenWaterAtImpl(int x, int y)
    {
        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();
        return waterManager != null && waterManager.IsWaterAt(x, y);
    }

    private bool ShouldSkipRoadTerraformSurfaceAtImpl(int x, int y, HeightMap heightMap)
    {
        if (heightMap == null || !heightMap.IsValidPosition(x, y))
            return true;
        if (IsWaterSlopeCellImpl(x, y))
            return true;
        return IsRegisteredOpenWaterAtImpl(x, y);
    }

    /// <summary>
    /// Find terrain prefab (slope, water slope, sea level water, bay) by name. Used when restoring saved games.
    /// </summary>
    private GameObject FindTerrainPrefabByNameImpl(string prefabName)
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

    #region Height Map Generation
    /// <summary>
    /// Configure next <see cref="LoadInitialHeightMap"/> to use uniform height across map (QA / method testing).
    /// </summary>
    /// <param name="enabled">True → skip template + procedural terrain for next load only.</param>
    /// <param name="uniformHeight">Height per cell; clamped to <see cref="MIN_HEIGHT"/>–<see cref="MAX_HEIGHT"/>.</param>
    private void SetNewGameFlatTerrainOptionsImpl(bool enabled, int uniformHeight)
    {
        newGameFlatTerrainEnabled = enabled;
        newGameFlatTerrainHeight = uniformHeight;
    }

    private void ClearNewGameFlatTerrainRequest()
    {
        newGameFlatTerrainEnabled = false;
    }
    /// <summary>
    /// Init heightmap + apply to grid → initial terrain elevations.
    /// </summary>
    private void StartTerrainGenerationImpl()
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
    /// Return current heightmap instance, or null if not yet initialized.
    /// </summary>
    /// <returns>Active HeightMap, or null.</returns>
    private HeightMap GetHeightMapImpl()
    {
        return heightMap;
    }

    /// <summary>
    /// Return heightMap, creating or loading if null. Call before RestoreTerrainForCell
    /// + pass result to RestoreTerrainForCell(x, y, map) so same map is used.
    /// </summary>
    private HeightMap GetOrCreateHeightMapImpl()
    {
        EnsureHeightMapLoadedImpl();
        return heightMap;
    }

    /// <summary>
    /// Create fresh heightmap from grid dims, load initial height data, apply to grid.
    /// </summary>
    private void InitializeHeightMapImpl()
    {
        heightMap = new HeightMap(gridManager.width, gridManager.height);
        LoadInitialHeightMap();
        if (!newGameFlatTerrainEnabled)
            EnsureGuaranteedLakeDepressions();
        ApplyHeightMapToGrid();
        ClearNewGameFlatTerrainRequest();
    }

    /// <summary>
    /// After procedural height gen → carve minimal cardinal bowls until
    /// <see cref="LakeFeasibility.CountSpillPassingCells"/> reaches
    /// <c>2 × ProceduralLakeBudgetHardCap + LakeFeasibilityExtraBowls</c> (capped by map area).
    /// Shuffled full interior scans → target met for any map size when physically possible.
    /// Skip if <see cref="waterManager"/> missing or lake fill disabled.
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
    /// Restore heightMap from saved grid data. Call before RestoreGrid so terrain/water systems use correct heights.
    /// </summary>
    private void RestoreHeightMapFromGridDataImpl(List<CellData> gridData)
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
    /// Apply restored heightMap positions to all cell GameObjects. Call after RestoreHeightMapFromGridData
    /// + before RestoreGrid so buildings (e.g. water plant) parent to correctly positioned cells.
    /// </summary>
    private void ApplyRestoredPositionsToGridImpl()
    {
        if (heightMap == null || gridManager == null) return;

        for (int x = 0; x < gridManager.width; x++)
        {
            for (int y = 0; y < gridManager.height; y++)
            {
                CityCell cell = gridManager.GetCell(x, y);
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
    /// Ensure heightMap exists + has initial data (for RestoreTerrainForCell when init order skipped).
    /// Try: 1) create from gridManager, 2) borrow from another TerrainManager in scene.
    /// Does not call ApplyHeightMapToGrid → visible grid not reset.
    /// Public so GridManager can call on same TM reference before RestoreTerrainForCell.
    /// </summary>
    private void EnsureHeightMapLoadedImpl()
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
    /// <summary>Low-freq Perlin (large horizontal plateaus for extended map).</summary>
    private const float ExtendedPerlinScaleCoarse = 58f;
    /// <summary>Medium-freq detail; mixed lightly with coarse.</summary>
    private const float ExtendedPerlinScaleFine = 38f;
    private const float ExtendedPerlinCoarseWeight = 0.72f;
    private const float ExtendedNoiseRemapLow = 0.32f;
    private const float ExtendedNoiseRemapRange = 0.58f;
    private const int BorderBlendWidth = 16;
    private const int ExtendedTerrainSmoothPasses = 2;
    /// <summary>Fine Perlin scale for sparse one-step dips (lake seeds outside 40×40 template).</summary>
    private const float ExtendedMicroLakeNoiseScale = 9f;

    /// <summary>Original 40×40 height map (rows y, cols x). Grid larger → placed <b>centered</b>; procedural fill surrounds.</summary>
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
    /// Fill cells outside centered 40×40 template → low-freq Perlin terrain, layered plateaus, 3×3 smoothing.
    /// Blend at template border. Lakes placed later via <see cref="WaterMap.InitializeLakesFromDepressionFill"/>.
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
    /// Sparse fine-scale height dips outside template → depression-fill finds valid lake seeds on extended terrain.
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

    /// <summary>Blend procedural height toward centered template along 4 sides + 4 corner bands.</summary>
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

    /// <summary>3×3 box blur on procedural cells only (outside centered template); softens terraces + sharp pits.</summary>
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

    /// <summary>Map Perlin [0,1] to land height 1–5: favor wide plains + mid plateaus; fewer peaks (large lakes).</summary>
    private static int PerlinToHeightExtended(float n)
    {
        if (n < 0.28f) return 1;
        if (n < 0.48f) return 2;
        if (n < 0.66f) return 3;
        if (n < 0.84f) return 4;
        return 5;
    }

    /// <summary>
    /// Apply terrain (slopes, water, cliff walls) to all cells based on heightMap.
    /// Same mechanism as New Game. Call after RestoreHeightMapFromGridData + ApplyRestoredPositionsToGrid for Load.
    /// </summary>
    private void ApplyHeightMapToGridImpl()
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
    /// Reapply terrain for inclusive heightmap rect (e.g. after artificial lake carving).
    /// Same diagonal sweep order as <see cref="ApplyHeightMapToGrid"/> → consistent slope resolution.
    /// </summary>
    private void ApplyHeightMapToRegionImpl(int minX, int minY, int maxX, int maxY)
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
    /// After lake/river water visuals exist → refresh land cells adjacent to water so shore/bay slopes +
    /// <see cref="PlaceCliffWalls"/> stay consistent. Default: each shore cell + 1 cardinal land
    /// neighbor outward from water (minimal audit). Add Moore ring of dry cells around <b>high</b> water cell on
    /// <see cref="WaterMap.IsLakeSurfaceStepContactForbidden"/> edges (§12.7 lake rim). Pass <paramref name="expandSecondChebyshevRing"/> true after procedural river
    /// generation → confluence mouths get wider Chebyshev-2 halo refresh.
    /// Then runs junction shore post-pass + <see cref="ApplyUpperBrinkShoreWaterCascadeCliffStacks"/> (§12.8.1).
    /// Call after <see cref="WaterManager.UpdateWaterVisuals"/>.
    /// </summary>
    private void RefreshShoreTerrainAfterWaterUpdateImpl(WaterManager wm, bool expandSecondChebyshevRing = false)
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
    /// After <see cref="ApplyJunctionCascadeShorePostPass"/> → build <see cref="cliffWaterSouthPrefab"/> / <see cref="cliffWaterEastPrefab"/> stacks on
    /// dry land classified as <see cref="RiverJunctionBrinkRole.UpperBrink"/> only, parented to that shore cell. Same cardinal edge +
    /// mirror rules as <see cref="RefreshWaterCascadeCliffs"/> (South when lower pool is S/N of high cell; East when E/W).
    /// Anchor Y follows upper pool water plane (§5.6.2, §12.8.1).
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

            CityCell shoreCell = gridManager.GetCell(p.x, p.y);
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
    /// After main shore refresh → re-apply shore prefabs for dry land on upper/lower river–river junction brinks using extended
    /// neighbor mask + forced diagonal <c>*SlopeWaterPrefab</c> → cascade edges don't stay on cardinal-only tiles (§12.8.1).
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
    /// Add extra dry cells in Moore neighborhood of <b>higher</b> water cell on each cardinal edge where
    /// <see cref="WaterMap.IsLakeSurfaceStepContactForbidden"/> applies (Lake at surface step — §12.7).
    /// <see cref="UpdateTileElevation"/> revisits rim grass/cliffs → lake shore closes before escarpment instead
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
    /// Lower dry cells in Moore shore ring when above <b>logical surface</b> of affiliated adjacent water body →
    /// plateau heights don't dominate <see cref="PlaceWaterShore"/> (§2.4.1). At multi-surface junctions (e.g. waterfall),
    /// use <see cref="CityCell.waterBodyId"/> or <see cref="WaterManager.GetShoreAffiliatedWaterBodyIdForLandCell"/>
    /// → shore-line terrain stays aligned with that body&apos;s <c>S</c>, not min <c>S</c> across all neighboring pools.
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
                CityCell cell = gridManager.GetCell(x, y);
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
        CityCell cell = gridManager.GetCell(x, y);
        if (cell == null) return;
        try
        {
            gridManager.SetCellHeight(new Vector2(x, y), newHeight, skipWaterMembershipRefresh: true);

            Vector2 newWorldPos = gridManager.GetCellWorldPosition(cell);
            cell.gameObject.transform.position = newWorldPos;
            cell.transformPosition = newWorldPos;

            int sortingOrder = CalculateTerrainSortingOrderImpl(x, y, newHeight);
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
    /// True if cell has any child with ZoneCategory.Zoning (residential/commercial/industrial overlay).
    /// Skip terrain refresh on those cells → <see cref="PathTerraformPlan.Apply"/> Phase 2/3 doesn't replace overlay.
    /// Building / footprint protection uses <see cref="GridManager.IsCellOccupiedByBuilding"/>.
    /// </summary>
    private bool CellHasZoningOverlay(CityCell cell)
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
    /// Reapply terrain for single cell from heightMap (e.g. after demolition).
    /// Restore height, world position, sorting order, slope prefab if needed.
    /// Return true if cell restored as water slope (caller should not add grass tile).
    /// Cut-through boundaries: Phase 3 refreshes neighbors of flattened path cells. Those neighbors
    /// have mixed flattened/non-flattened neighbors; RequiresSlope + DetermineSlopePrefab use
    /// updated heightmap → slope selection matches new landscape. forceFlat/forceSlopeType are
    /// used for path + adjacent cells in Phase 2; Phase 3 neighbors use live heightmap.
    /// </summary>
    /// <param name="useHeightMap">Non-null → this map used (+ assigned to heightMap) so restore works even when instance field null.</param>
    /// <param name="forceFlat">True → use flat terrain regardless of neighbor heights. For terraformed path transition cells.</param>
    /// <param name="forceSlopeType">When set → use this orthogonal slope prefab instead of DetermineSlopePrefab. For terraformed path slope cells.</param>
    /// <param name="terraformCutCorridorCells">Non-null (cut-through apply) → place land–land cliff walls for 1-step drops toward these cells.</param>
    private bool RestoreTerrainForCellImpl(int x, int y, HeightMap useHeightMap = null, bool forceFlat = false, TerrainSlopeType? forceSlopeType = null, ISet<Vector2Int> terraformCutCorridorCells = null)
    {
        if (useHeightMap != null)
            heightMap = useHeightMap;
        if (heightMap == null)
        {
            EnsureHeightMapLoadedImpl();
        }
        if (heightMap == null)
            return false;
        if (!heightMap.IsValidPosition(x, y))
            return false;
        int newHeight = heightMap.GetHeight(x, y);
        if (newHeight == SEA_LEVEL)
            return false;
        CityCell cell = gridManager.GetCell(x, y);
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

            int sortingOrder = CalculateTerrainSortingOrderImpl(x, y, newHeight);
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
    /// Return terrain prefab for orthogonal slope type. Used when forcing slope prefab for terraformed path cells.
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
    private void DestroyCellChildren(CityCell cell)
    {
        GameObject cellObject = cell.gameObject;  // Get the GameObject that holds the CityCell component
        var toDestroy = new List<GameObject>();
        foreach (Transform child in cellObject.transform)
            toDestroy.Add(child.gameObject);
        foreach (GameObject go in toDestroy)
            Destroy(go);
    }

    /// <summary>
    /// Destroy only slope + grass children, preserve road, forest, buildings.
    /// Used when replacing slope with flat grass (plateau or flat terrain).
    /// DestroyImmediate avoids deferred Destroy → multiple grass instances when
    /// RestoreTerrainForCell called repeatedly in same frame (e.g. interstate generation).
    /// </summary>
    private void DestroyTerrainChildrenOnly(CityCell cell)
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
            else if (IsWaterSlopeObjectImpl(child.gameObject) || IsLandSlopeObjectImpl(child.gameObject) || IsShoreBayObjectImpl(child.gameObject))
                toDestroy.Add(child.gameObject);
            else if (zone == null && child.GetComponent<SpriteRenderer>() != null
                     && !IsWaterSlopeObjectImpl(child.gameObject)
                     && !IsLandSlopeObjectImpl(child.gameObject)
                     && !IsShoreBayObjectImpl(child.gameObject)
                     && !IsSeaLevelWaterObjectImpl(child.gameObject))
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
    /// Slopes allowed only for 1x1 buildings.
    /// </summary>
    private bool IsTerrainPlaceableForBuilding(int x, int y, int buildingSize = 1)
    {
        TerrainSlopeType slope = GetTerrainSlopeTypeAtImpl(x, y);
        if (slope == TerrainSlopeType.Flat) return true;
        return buildingSize == 1;
    }

    /// <summary>
    /// True if land cell may use water-shore prefabs: among 8 neighbors, some water/sea exists whose logical
    /// surface <c>S</c> yields visual reference <c>V = max(MIN_HEIGHT, S - 1)</c> (aligned with
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
    /// Same height index as water tile world placement: 1 step below logical spill surface.
    /// </summary>
    private static int GetWaterVisualReferenceHeightFromLogicalSurface(int logicalSurface)
    {
        return Mathf.Max(MIN_HEIGHT, logicalSurface - 1);
    }

    /// <summary>
    /// Surface height for water at neighbor, or -1 if cell not water/sea. Uses <see cref="WaterManager"/>
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
    /// True if cell has ≥1 neighbor (incl. diagonals) at sea level (height 0).
    /// Allows water plants on coastal slope tiles. 8 neighbors to match RequiresSlope,
    /// so cells that only touch water diagonally still count as coastal.
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
    /// True if cell is water-shore tile (land within 1 height step of adjacent water body's surface).
    /// Rim cells far above water surface → false, so roads/terraform treat as normal terrain.
    /// </summary>
    private bool IsWaterSlopeCellImpl(int x, int y)
    {
        if (heightMap == null)
            EnsureHeightMapLoadedImpl();
        if (heightMap == null || !heightMap.IsValidPosition(x, y))
            return false;
        int h = heightMap.GetHeight(x, y);
        if (h < 1)
            return false;
        return IsLandEligibleForWaterShorePrefabs(x, y, h);
    }

    /// <summary>
    /// Dry land that may carry <see cref="CityCell.waterBodyId"/> for water-shore art or rim cliffs toward registered water.
    /// </summary>
    private bool IsDryShoreOrRimMembershipEligibleImpl(int x, int y)
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
    private bool IsWaterOrSeaAtNeighborImpl(int nx, int ny)
    {
        return WaterOrSeaAt(nx, ny);
    }

    /// <summary>
    /// True for <b>transition</b> tile 1 step below inland land toward open water (cardinal sea or
    /// registered water) → hosts water-slope prefabs, not rim cliffs. Flat plateau rim next to
    /// rectangular lake has same height as land behind along edge → no cardinal land neighbor strictly
    /// higher; those cells <b>not</b> ramp tiles + can receive cliffs toward water. (Cardinal-only water
    /// alone matched every fallback lake rim + suppressed all rim cliffs.)
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
    /// Replace terrain with flat grass. Destroy slope + grass children, then place grass.
    /// Used when cell flat or plateau (no higher neighbors).
    /// </summary>
    private void PlaceFlatTerrain(int x, int y)
    {
        if (gridManager == null || zoneManager == null) return;

        CityCell cell = gridManager.GetCell(x, y);
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

        int sortingOrder = CalculateTerrainSortingOrderImpl(x, y, cell.height);
        SpriteRenderer sr = zoneTile.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.sortingOrder = sortingOrder;
        cell.SetCellInstanceSortingOrder(sortingOrder);
        cell.prefabName = grassPrefab.name;
    }

    /// <summary>
    /// Place slope tile from given prefab. Used by RestoreGrid for Load when saved prefabName is slope.
    /// </summary>
    private void PlaceSlopeFromPrefabImpl(int x, int y, GameObject slopePrefab, int cellHeight = -1)
    {
        if (slopePrefab == null || gridManager == null) return;

        CityCell cell = gridManager.GetCell(x, y);
        if (cell == null) return;

        int currentHeight = cellHeight >= 0 ? cellHeight : (heightMap != null ? heightMap.GetHeight(x, y) : cell.height);
        DestroyTerrainChildrenOnly(cell);

        Vector2 worldPos = cell.transformPosition;
        GameObject slope = Instantiate(slopePrefab, worldPos, Quaternion.identity);
        slope.transform.SetParent(cell.gameObject.transform);

        cell.prefabName = slopePrefab.name;

        int slopeOrder = CalculateSlopeSortingOrderImpl(x, y, currentHeight);
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
    /// Restore water slopes on land cells adjacent to water. Call after RestoreGrid during Load.
    /// Skip cells with buildings. Does not modify water cells (WaterManager already placed them).
    /// </summary>
    private void RestoreWaterSlopesFromHeightMapImpl()
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
                        CityCell neighborCell = gridManager.GetCell(nx, ny);
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
    /// Restore land slopes + water slopes for terrain-only cells. Call after RestoreGrid during Load.
    /// Skip cells with buildings, roads, or zoning.
    /// </summary>
    private void RestoreTerrainSlopesFromHeightMapImpl()
    {
        if (heightMap == null || gridManager == null) return;

        for (int x = 0; x < gridManager.width; x++)
        {
            for (int y = 0; y < gridManager.height; y++)
            {
                if (heightMap.GetHeight(x, y) == SEA_LEVEL) continue;

                CityCell cell = gridManager.GetCell(x, y);
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

                RestoreTerrainForCellImpl(x, y);
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

        CityCell cell = gridManager.GetCell(x, y);
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
        CityCell updatedCell = gridManager.GetCell(x, y);

        int sortingOrder = CalculateTerrainSortingOrderImpl(x, y, SEA_LEVEL);
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
    /// Instantiate 1+ lake/coast shore prefabs (cardinal, Bay, or upslope+downslope pair) as children of cell.
    /// </summary>
    private void PlaceWaterShore(int x, int y, List<GameObject> waterShorePrefabs)
    {
        if (waterShorePrefabs == null || waterShorePrefabs.Count == 0)
            return;

        CityCell cell = gridManager.GetCell(x, y);
        DestroyTerrainChildrenOnly(cell);

        int landH = heightMap != null ? heightMap.GetHeight(x, y) : 1;
        if (landH <= SEA_LEVEL)
            landH = 1;

        gridManager.SetCellHeight(new Vector2(x, y), landH);
        CityCell updatedCell = gridManager.GetCell(x, y);

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
                ? CalculateShoreBaySortingOrderImpl(x, y)
                : CalculateWaterSlopeSortingOrderImpl(x, y);
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
            ? CalculateShoreBaySortingOrderImpl(x, y)
            : CalculateWaterSlopeSortingOrderImpl(x, y);
        updatedCell.sortingOrder = primarySort;
        updatedCell.SetCellInstanceSortingOrder(primarySort);
    }

    /// <summary>
    /// Restore lake/coast shore visuals from saved prefab names (load path).
    /// Sorting uses same formulas as private PlaceWaterShore path (Bay / water-slope orders). savedPrimarySort arg kept for call-site compatibility with older saves + not applied.
    /// </summary>
    private void RestoreWaterShorePrefabsFromSaveImpl(int x, int y, string primaryName, string secondaryName, int savedPrimarySort)
    {
        var list = new List<GameObject>();
        if (!string.IsNullOrEmpty(primaryName))
        {
            GameObject p = FindTerrainPrefabByNameImpl(primaryName);
            if (p != null) list.Add(p);
        }
        if (!string.IsNullOrEmpty(secondaryName))
        {
            GameObject p = FindTerrainPrefabByNameImpl(secondaryName);
            if (p != null) list.Add(p);
        }
        if (list.Count == 0)
            return;

        PlaceWaterShore(x, y, list);
    }

    /// <param name="terraformCutCorridorCells">Cells lowered by cut-through terraform → enable 1-step land–land cliff faces toward corridor.</param>
    private void PlaceCliffWalls(int x, int y, ISet<Vector2Int> terraformCutCorridorCells = null)
    {
        int currentHeight = heightMap.GetHeight(x, y);
        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();
        if (waterManager != null && waterManager.IsWaterAt(x, y))
            return;

        if (IsCellSurroundedByCardinalWaterOnly(x, y, waterManager))
            return;

        CityCell cell = gridManager.GetCell(x, y);
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
    /// True when all 4 cardinals are sea or registered water (e.g. 1×1 island) → no vertical cliff ring.
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
    /// True for <b>one-step</b> drop toward sea, registered water, or water-shore slope tile → don't instantiate
    /// cliff prefabs; water-slope / water visuals cover that transition. Escarpments (Δh ≥ 2) toward same neighbor
    /// <b>not</b> suppressed → stacked cliff segments can fill vertical face (lake basins, rim voids).
    /// Suppression applies only when <b>high</b> cell in water-shore eligibility band; rim plateaus above
    /// shore strip (not eligible) keep cliff faces toward that lower neighbor.
    /// </summary>
    private bool ShouldSuppressCliffFaceTowardLowerCell(int highX, int highY, int lowerX, int lowerY, int lowerHeight, int currentHeight)
    {
        if (currentHeight - lowerHeight != 1)
            return false;
        if (heightMap == null || !heightMap.IsValidPosition(lowerX, lowerY))
            return false;
        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();
        bool lowerIsWaterOrSlope = (waterManager != null && waterManager.IsWaterAt(lowerX, lowerY)) || IsWaterSlopeCellImpl(lowerX, lowerY);
        if (!lowerIsWaterOrSlope)
            return false;
        if (currentHeight < 1 || !IsLandEligibleForWaterShorePrefabs(highX, highY, currentHeight))
            return false;
        return true;
    }

    /// <summary>
    /// After one-step suppression rules → resolve segment count for dry land, narrow shore, cut-through, rim plateau toward water/slope.
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
                if (IsWaterSlopeCellImpl(lowerX, lowerY))
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
            if (IsWaterSlopeCellImpl(lowerX, lowerY))
                return 1;
        }

        return 0;
    }

    /// <summary>
    /// One-step drop toward open water only: suppress cliff on narrow shore strip (height-1 band with higher land
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

    /// <summary>Stacked cliff segment count on north face (0 = none). Higher cell = (x,y); lower neighbor = north.</summary>
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
                && !IsWaterSlopeCellImpl(x + 1, y))
                return 1;
            return 0;
        }

        return ResolveCliffWallDropAfterSuppression(x, y, currentHeight, x + 1, y, heightAtNorth, diff, terraformCutCorridorCells);
    }

    /// <summary>Stacked cliff segment count on south face (0 = none). Higher cell = (x,y); lower = south.</summary>
    private int GetCliffWallDropSouth(int x, int y, int currentHeight, ISet<Vector2Int> terraformCutCorridorCells = null)
    {
        int lowerX = x - 1;
        int lowerY = y;
        bool lowerValid = heightMap != null && heightMap.IsValidPosition(lowerX, lowerY);
        if (!lowerValid)
        {
            CityCell c = gridManager != null ? gridManager.GetCell(x, y) : null;
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
                && !IsWaterSlopeCellImpl(lowerX, lowerY))
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
            CityCell c = gridManager != null ? gridManager.GetCell(x, y) : null;
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
                && !IsWaterSlopeCellImpl(lowerX, lowerY))
                return 1;
            return 0;
        }

        return ResolveCliffWallDropAfterSuppression(x, y, currentHeight, lowerX, lowerY, heightAtEast, diff, terraformCutCorridorCells);
    }

    /// <summary>Stacked cliff segment count on west face (0 = none). Higher cell = (x,y); lower neighbor = west.</summary>
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
                && !IsWaterSlopeCellImpl(x, y + 1))
                return 1;
            return 0;
        }

        return ResolveCliffWallDropAfterSuppression(x, y, currentHeight, x, y + 1, heightAtWest, diff, terraformCutCorridorCells);
    }

    /// <summary>
    /// Cut-through only: 1-step land drop into lowered corridor gets cliff wall (avoids black voids at rim).
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
    /// Logical water surface height at cell across cliff foot (low side), or -1 if cell not registered water.
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
    /// True when entire height band of segment strictly below water surface (no sprite — underwater).
    /// </summary>
    private static bool ShouldSkipCliffSegmentFullyUnderwater(int topH, int bottomH, int waterSurfaceHeight)
    {
        if (waterSurfaceHeight < 0)
            return false;
        int segmentHigh = Mathf.Max(topH, bottomH);
        return segmentHigh < waterSurfaceHeight;
    }

    /// <summary>
    /// Fixed isometric camera: S + E diamond edges face player (↙ ↘). N + W hidden behind terrain art.
    /// </summary>
    private static bool IsCliffCardinalFaceVisibleToCamera(CliffCardinalFace face)
    {
        return face == CliffCardinalFace.South || face == CliffCardinalFace.East;
    }

    /// <summary>Inspector cliff prefab for geometric cardinal face (not delegated by name).</summary>
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
    /// Cliff world pos: same x as cell; y = anchor y minus tile height × segment index.
    /// Land cliffs use cell transform (terrain floor). Water–water cascades pass <paramref name="overrideAnchorWorldY"/> at water visual surface (see <see cref="WaterManager.PlaceWater"/>).
    /// </summary>
    private Vector2 GetCliffWallSegmentWorldPositionOnSharedEdge(CityCell cell, int topH, int segmentIndex, float? overrideAnchorWorldY = null)
    {
        float anchorY = overrideAnchorWorldY ?? cell.gameObject.transform.position.y;
        return new Vector2(cell.gameObject.transform.position.x, anchorY - gridManager.tileHeight * (segmentIndex * 0.5f));
    }

    /// <summary>
    /// Place stack of brown cliff walls on <b>land</b> cell (see <see cref="PlaceCliffWalls"/>).
    /// </summary>
    private void PlaceCliffWallStack(CityCell cell, CliffCardinalFace cardinalFace, int highX, int highY, int lowX, int lowY, int highH, int lowH, int segmentCount)
    {
        GameObject prefab = GetCliffPrefabForCardinalFace(cardinalFace);
        PlaceCliffWallStackCore(cell, prefab, cardinalFace, highX, highY, lowX, lowY, highH, lowH, segmentCount);
    }

    /// <summary>
    /// Shared stack placement for brown cliffs + water cascade cliffs — same segment loop + sorting.
    /// Land cliffs skip segments fully below lower-side water surface; water–water cascades don't (logical surface vs bed heights would hide whole stack).
    /// </summary>
    /// <returns>Cliff segment sprite count instantiated.</returns>
    /// <param name="cliffStackAnchorWorldY">When set (water–water cascades) → Y anchor at water visual surface; else cell transform Y (terrain floor).</param>
    /// <param name="sortingReferenceGridX">When set with <paramref name="sortingReferenceGridY"/> → used for sorting depth instead of <paramref name="highX"/>,<paramref name="highY"/> (e.g. upper-brink shore cell).</param>
    private int PlaceCliffWallStackCore(
        CityCell cell,
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
                int computedSort = CalculateTerrainSortingOrderImpl(sortGX, sortGY, topH) + SLOPE_OFFSET + visualIndex;
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
    /// After water tiles placed, build water–water surface-step stacks per cardinal step
    /// (<c>S_high &gt; S_low</c>) using south/east cliff-water prefabs only (§5.6.2).
    /// Standard edges → parent to <b>upper</b> cell when lower pool is south/east (visible faces there).
    /// Lower pool <b>north</b>/<b>west</b> → same prefabs attach to <b>lower</b> cell (south face toward upper south;
    /// east face toward upper east) so cascades not missing on those contacts.
    /// <see cref="GetEffectiveHeightsForWaterWaterCliff"/> uses <c>segmentCount = S_high − S_low</c>. Skip edges where
    /// <see cref="WaterMap.IsLakeSurfaceStepContactForbidden"/> true (§12.7).
    /// </summary>
    private void RefreshWaterCascadeCliffs_Impl(WaterManager wm)
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
                CityCell cell = gridManager.GetCell(x, y);
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
    /// Compute terrain heights + segment count for water–water cascade. Segment count follows
    /// <b>logical</b> surface step <c>S_high − S_low</c>; bed <see cref="HeightMap"/> does not define surface height.
    /// High bed not above low bed → adjust <paramref name="lowH"/> so <c>highH &gt; lowH</c> for stack geometry
    /// (may drop below <see cref="MIN_HEIGHT"/> only for depth calc at equal beds — §5.6.2, §12.7).
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
    /// Instantiate water–water cascade prefabs for one cardinal step. Optional <paramref name="parentCellX"/> / <paramref name="parentCellY"/>
    /// parent to lower pool when visible south/east face lies on that cell (north/west lower neighbor — mirror placement).
    /// </summary>
    /// <param name="parentCellX">With <paramref name="parentCellY"/>: cliff children parent to this cell; else to upper pool.</param>
    /// <param name="waterSurfaceAnchorGridX">With <paramref name="waterSurfaceAnchorGridY"/>: <see cref="GridManager.GetWorldPositionVector"/> cascade anchor Y uses this grid cell (e.g. upper-brink shore) so isometric water-plane Y matches parent shore tile (§12.8.1).</param>
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
        CityCell cell = gridManager.GetCell(px, py);
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

    private void RemoveExistingWaterCascadeCliffs(CityCell cell)
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

    private void RemoveExistingCliffWalls(CityCell cell)
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
    /// don't render above registered water in 8-neighbor ring when that water strictly in front in isometric depth
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
                CityCell neighborCell = gridManager.GetCell(nx, ny);
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
    /// True if neighbor cell is sea-level terrain or registered water (lake/sea in <see cref="WaterManager"/>).
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
    /// Visual height index for water sprites at <paramref name="nx"/>, <paramref name="ny"/> (logical surface − 1).
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
    /// World height index for water visuals adjacent to shore cell: logical surface − 1 (see WaterManager.PlaceWater).
    /// Min among cardinal water/sea neighbors; none → fall back to diagonals (external lake corners).
    /// </summary>
    private int GetNeighborWaterVisualHeightForShore(int x, int y)
    {
        return GetNeighborWaterVisualHeightForShore(x, y, 0);
    }

    /// <summary>
    /// Same as <see cref="GetNeighborWaterVisualHeightForShore(int,int)"/>, but when <paramref name="affiliatedBodyId"/> non-zero,
    /// only registered water neighbors w/ matching body id contribute to min (§12.8 upper-brink alignment).
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
    /// True when shore cell sits on terrain slope toward non-water neighbor (cardinal land higher than this cell).
    /// Used to pick upslope+downslope water pair vs Bay on diagonal-only water patterns.
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
    /// True when <paramref name="prefab"/> is a shore corner (Bay) prefab — inner 90° (two cardinals water) or external rectangular-lake corner.
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
    /// Reference equality — same prefabs as <see cref="IsWaterSlopeObject"/> + Bay.
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
    private bool CellUsesWaterShorePrimaryPrefab(CityCell cell)
    {
        if (cell == null || string.IsNullOrEmpty(cell.prefabName))
            return false;
        GameObject p = FindTerrainPrefabByNameImpl(cell.prefabName);
        return IsWaterShoreTerrainPrefabAsset(p);
    }

    /// <summary>
    /// Agent / Editor diagnostics (<c>debug_context_bundle</c>): same predicate as <see cref="CellUsesWaterShorePrimaryPrefab"/> w/o exposing private helpers.
    /// </summary>
    private bool DoesCellUseWaterShorePrimaryPrefabImpl(CityCell cell) => CellUsesWaterShorePrimaryPrefab(cell);

    /// <summary>
    /// Diagonal shore tiles (NE/NW/SE/SW Upslope or SlopeWater), not Bay — used for Y offset + diagonal downslope selection.
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
    /// True for NE/NW/SE/SW <c>*SlopeWaterPrefab</c> only (not Upslope). Used for flat-lake corner placement vs upslope+downslope pair.
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
    /// Standalone corner SlopeWater: 0 (same water plane as neighbors). Diagonal Upslope or upslope+downslope pair:
    /// <c>(landH − waterVisualH) × tileHeight × 0.25</c>. Cardinal slopes: 0.
    /// </summary>
    /// <param name="shorePrefabCount">Shore prefab count placed together (2 = upslope+downslope pair).</param>
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
    /// Sorting order of water surface sprite on <paramref name="nx"/>, <paramref name="ny"/>,
    /// matching lake <c>PlaceWater</c> + sea-level water tiles (for Bay vs neighbor overlap).
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
            return CalculateTerrainSortingOrderImpl(nx, ny, visualSurfaceHeight);
        }

        if (heightMap.GetHeight(nx, ny) == SEA_LEVEL)
            return CalculateTerrainSortingOrderImpl(nx, ny, SEA_LEVEL);

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
    /// (2) <see cref="HasLandSlopeIgnoringWater"/> → single Bay (cliff / higher land rim matches flat-lake concave corner); else downslope if no Bay;
    /// (3) else single Bay (flat terrain, diagonal lake edge); (4) fallback upslope+downslope pair.
    /// Rectangle corners must win over land-slope so higher land neighbor doesn't force companion pair on straight corners.
    /// <paramref name="forceCascadeJunctionSlopeWater"/> true (river–river upper/lower brink, §12.8) → diagonal <c>*SlopeWaterPrefab</c> only.
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
    /// True when diagonal water at NE of shore is outer corner of axis-aligned rectangle: no water further North/East of W.
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
    /// True when diagonal water at NW of shore is outer corner of axis-aligned rectangle: no water further North/West of W.
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
    /// True when diagonal water at SE of shore is outer corner of axis-aligned rectangle: no water further South/East of W.
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
    /// True when diagonal water at SW of shore is outer corner of axis-aligned rectangle: no water further South/West of W.
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
    /// (junction), <b>except</b> when that step is a <see cref="WaterMap.IsLakeSurfaceStepContactForbidden"/> edge — then
    /// land cell uses normal lake-rim Bay / axis-aligned shore logic instead of multi-surface diagonal slopes (§12.7).
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
    /// True when perpendicular-corner triangle (two cardinal water neighbors + diagonal cell at land corner)
    /// fully registered water + three cells don't share one logical surface <c>S</c>. Covers cascade
    /// junctions where both cardinals belong to lower pool (same <c>S</c>) but diagonal is upper pool — then
    /// <see cref="IsMultiSurfacePerpendicularWaterCorner"/> false + <see cref="IsAxisAlignedRectangleCornerWaterSouthWest"/>
    /// (+ siblings) would wrongly pick Bay (concave) instead of <c>*SlopeWaterPrefab</c> (convex land tip).
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
    /// Bay vs corner slope for one perpendicular pair (two cardinals wet). Return null if both Bay + SlopeWater prefabs missing.
    /// <paramref name="forceCascadeJunctionSlopeWater"/> (river–river junction brink, §12.8) → diagonal <c>*SlopeWaterPrefab</c> only, matching <see cref="BuildDiagonalOnlyShorePrefabs"/>.
    /// </summary>
    private List<GameObject> SelectPerpendicularWaterCornerPrefabs(int x, int y, ShoreCornerQuadrant quadrant, bool forceCascadeJunctionSlopeWater = false)
    {
        // User-stated semantic (Bug B/C, 2026-05-15):
        //   slope-water    → isolated convex tip (peninsula end, single shore cell on the diagonal)
        //   bay-slope      → concave corner of the water body (rectangle outer-corner)
        //   up-slope-water → cell on a diagonal land edge composed of MULTIPLE adjacent cells
        //                    (each with the same perpendicular-water pattern along the NW-SE / NE-SW axis)
        // Previous routing forced Bay whenever HasLandSlopeIgnoringWater was true, which
        // mis-classified every peninsula tip backed by a higher inland cell as a Bay.
        GameObject bayPrefab;
        GameObject slopePrefab;
        GameObject upslopePrefab;
        System.Func<int, int, bool> isAxisAligned;
        switch (quadrant)
        {
            case ShoreCornerQuadrant.SouthEast:
                bayPrefab = southEastBayPrefab;
                slopePrefab = southEastSlopeWaterPrefab;
                upslopePrefab = southEastUpslopeWaterPrefab;
                isAxisAligned = IsAxisAlignedRectangleCornerWaterSouthEast;
                break;
            case ShoreCornerQuadrant.SouthWest:
                bayPrefab = southWestBayPrefab;
                slopePrefab = southWestSlopeWaterPrefab;
                upslopePrefab = southWestUpslopeWaterPrefab;
                isAxisAligned = IsAxisAlignedRectangleCornerWaterSouthWest;
                break;
            case ShoreCornerQuadrant.NorthEast:
                bayPrefab = northEastBayPrefab;
                slopePrefab = northEastSlopeWaterPrefab;
                upslopePrefab = northEastUpslopeWaterPrefab;
                isAxisAligned = IsAxisAlignedRectangleCornerWaterNorthEast;
                break;
            case ShoreCornerQuadrant.NorthWest:
                bayPrefab = northWestBayPrefab;
                slopePrefab = northWestSlopeWaterPrefab;
                upslopePrefab = northWestUpslopeWaterPrefab;
                isAxisAligned = IsAxisAlignedRectangleCornerWaterNorthWest;
                break;
            default:
                return null;
        }

        if (forceCascadeJunctionSlopeWater && slopePrefab != null)
            return ShoreList(slopePrefab);
        if (IsMultiSurfacePerpendicularWaterCorner(x, y, quadrant))
        {
            if (slopePrefab != null) return ShoreList(slopePrefab);
            if (bayPrefab != null) return ShoreList(bayPrefab);
            return null;
        }
        if (IsMixedSurfaceThreeCellPerpendicularCorner(x, y, quadrant))
        {
            if (slopePrefab != null) return ShoreList(slopePrefab);
            if (bayPrefab != null) return ShoreList(bayPrefab);
            return null;
        }
        if (isAxisAligned(x, y))
        {
            if (bayPrefab != null) return ShoreList(bayPrefab);
            if (slopePrefab != null) return ShoreList(slopePrefab);
            return null;
        }
        if (IsMultiCellDiagonalLandEdge(x, y, quadrant))
        {
            if (upslopePrefab != null) return ShoreList(upslopePrefab);
            if (slopePrefab != null) return ShoreList(slopePrefab);
            if (bayPrefab != null) return ShoreList(bayPrefab);
            return null;
        }
        if (slopePrefab != null) return ShoreList(slopePrefab);
        if (bayPrefab != null) return ShoreList(bayPrefab);
        return null;
    }

    /// <summary>
    /// True when this shore cell sits on a diagonal land edge of TWO+ cells along the
    /// quadrant's NW-SE (or NE-SW) axis, where the next inland cell also matches the
    /// same perpendicular-water cardinal pair. Used to pick <c>*UpslopeWaterPrefab</c>
    /// instead of <c>*SlopeWaterPrefab</c> (isolated tip).
    /// </summary>
    private bool IsMultiCellDiagonalLandEdge(int x, int y, ShoreCornerQuadrant quadrant)
    {
        int cardA_X, cardA_Y, cardB_X, cardB_Y, landNbrX, landNbrY;
        switch (quadrant)
        {
            case ShoreCornerQuadrant.SouthEast:
                // S+E water pattern; multi-cell edge continues NW. NW-neighbor (x+1, y+1)
                // has S-of-NW = (x, y+1), E-of-NW = (x+1, y).
                cardA_X = x;     cardA_Y = y + 1;
                cardB_X = x + 1; cardB_Y = y;
                landNbrX = x + 1; landNbrY = y + 1;
                break;
            case ShoreCornerQuadrant.SouthWest:
                // S+W water pattern; multi-cell edge continues NE. NE-neighbor (x+1, y-1)
                // has S-of-NE = (x, y-1), W-of-NE = (x+1, y).
                cardA_X = x;     cardA_Y = y - 1;
                cardB_X = x + 1; cardB_Y = y;
                landNbrX = x + 1; landNbrY = y - 1;
                break;
            case ShoreCornerQuadrant.NorthEast:
                // N+E water pattern; multi-cell edge continues SW. SW-neighbor (x-1, y+1)
                // has N-of-SW = (x-1, y), E-of-SW = (x, y+1).
                cardA_X = x - 1; cardA_Y = y;
                cardB_X = x;     cardB_Y = y + 1;
                landNbrX = x - 1; landNbrY = y + 1;
                break;
            case ShoreCornerQuadrant.NorthWest:
                // N+W water pattern; multi-cell edge continues SE. SE-neighbor (x-1, y-1)
                // has N-of-SE = (x-1, y), W-of-SE = (x, y-1).
                cardA_X = x - 1; cardA_Y = y;
                cardB_X = x;     cardB_Y = y - 1;
                landNbrX = x - 1; landNbrY = y - 1;
                break;
            default:
                return false;
        }
        return WaterOrSeaAt(cardA_X, cardA_Y)
            && WaterOrSeaAt(cardB_X, cardB_Y)
            && !WaterOrSeaAt(landNbrX, landNbrY);
    }

    /// <summary>
    /// Exactly three cardinal neighbors wet → two perpendicular pairs both true; pick inner corner via diagonal water
    /// (same tie-break order as legacy SE/SW/NE/NW when diagonals agree or both dry).
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
    /// Two cardinal offsets from shore cell to probe for cascade: along shore-line axis only (both sides), e.g. E+W for
    /// E–W shoreline, N+S for N–S shoreline. Same cardinal water pattern as <see cref="DetermineWaterShorePrefabs"/>.
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
    /// True when <paramref name="cx"/>,<paramref name="cy"/> on lower-pool cascade / junction strip: registered water
    /// on low side of cardinal surface step (same <paramref name="ownerBodyId"/>), or dry land w/ same shore
    /// affiliation connecting along shore-line axis only to such water (Pass B strip may include dry cells).
    /// <paramref name="axisDx0"/>/<paramref name="axisDy0"/> + <paramref name="axisDx1"/>/<paramref name="axisDy1"/> are
    /// two cardinal directions along shore line (e.g. east + west); dry traversal doesn't step off that axis.
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
    /// True → dry land shore cell should use <c>*SlopeWaterPrefab</c> corner (not <c>*UpslopeWaterPrefab</c>) to close
    /// shore line before terrain/water surface step at cascade / multi-surface junction: cell affiliated with
    /// lower pool (<paramref name="ownerBodyId"/>), touches cascade/junction strip along shore-line axis only,
    /// <paramref name="quadrant"/> is perpendicular corner orientation (multi-surface or mixed three-cell pattern).
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
    /// Select lake/coast shore prefab(s) for land cell adjacent to water. Return single prefab or upslope+downslope pair for diagonal slopes.
    /// Moore neighbor wet/dry uses <see cref="WaterManager.IsOpenWaterForShoreTopology"/> (affiliated body) or <see cref="WaterOrSeaAt"/> (no affiliation), unless
    /// <paramref name="useJunctionTopologyForShorePattern"/> → <see cref="WaterManager.NeighborMatchesShoreOwnerForJunctionTopology"/> (junction post-pass, §12.8.1).
    /// Perpendicular two-cardinal corners: both cardinals <b>registered</b> water w/ different logical surfaces → prefer
    /// diagonal <c>*SlopeWaterPrefab</c> over Bay <b>unless</b> edge is <see cref="WaterMap.IsLakeSurfaceStepContactForbidden"/> (§12.7 — lake rim vs lower pool, no junction).
    /// Else Bay when diagonal water cell is axis-aligned rectangle outer corner; if not, prefer Bay if
    /// <see cref="HasLandSlopeIgnoringWater"/> (cliff rim), else SlopeWater then Bay (convex land tip / large-lake shore).
    /// Pure cardinal north/south (no east/west water on those branches) → north/south slope only — not <see cref="BuildDiagonalOnlyShorePrefabs"/>.
    /// River confluences + non-rectangular water patterns: isometric spec §5.9; refresh land after river stamps via <see cref="RefreshShoreTerrainAfterWaterUpdate"/>.
    /// </summary>
    /// <param name="useJunctionTopologyForShorePattern">True → junction-brink dry neighbors count as wet (post-pass only).</param>
    /// <param name="forceJunctionDiagonalSlopeForCascade">True → force diagonal <c>*SlopeWaterPrefab</c> over Bay for this cell (post-pass along cascade strip).</param>
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
    /// Sorting order for terrain tiles.
    /// </summary>
    /// <param name="x">Grid X.</param>
    /// <param name="y">Grid Y.</param>
    /// <param name="height">Terrain height.</param>
    /// <returns>Sorting order value.</returns>
    private int CalculateTerrainSortingOrderImpl(int x, int y, int height)
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
    /// <param name="x">Grid X.</param>
    /// <param name="y">Grid Y.</param>
    /// <returns>Sorting order value for water slope.</returns>
    private int CalculateWaterSlopeSortingOrderImpl(int x, int y)
    {
        const int WATER_SLOPE_OFFSET = 1;
        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();
        int aff = waterManager != null ? waterManager.GetShoreAffiliatedWaterBodyIdForLandCell(x, y) : 0;
        int waterVisualH = GetNeighborWaterVisualHeightForShore(x, y, aff);
        return CalculateTerrainSortingOrderImpl(x, y, waterVisualH) + WATER_SLOPE_OFFSET;
    }

    /// <summary>
    /// Sorting for ShoreBay (inner 90°) shore tiles only: same base as <see cref="CalculateWaterSlopeSortingOrder"/>,
    /// then ≥1 step above adjacent water tiles so isometric neighbors don't cover corner.
    /// </summary>
    private int CalculateShoreBaySortingOrderImpl(int x, int y)
    {
        const int BAY_SHORE_MIN_OFFSET = 1;
        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();
        int aff = waterManager != null ? waterManager.GetShoreAffiliatedWaterBodyIdForLandCell(x, y) : 0;
        int waterVisualH = GetNeighborWaterVisualHeightForShore(x, y, aff);
        int baseOrder = CalculateTerrainSortingOrderImpl(x, y, waterVisualH) + BAY_SHORE_MIN_OFFSET;

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
    /// Sorting order for slope tiles (slightly behind terrain).
    /// </summary>
    /// <param name="x">Grid X.</param>
    /// <param name="y">Grid Y.</param>
    /// <param name="height">Terrain height.</param>
    /// <returns>Sorting order value.</returns>
    private int CalculateSlopeSortingOrderImpl(int x, int y, int height)
    {
        return CalculateTerrainSortingOrderImpl(x, y, height) + SLOPE_OFFSET;
    }

    /// <summary>
    /// Sorting order for buildings (above terrain). Call from building placement code.
    /// </summary>
    /// <param name="x">Grid X.</param>
    /// <param name="y">Grid Y.</param>
    /// <param name="height">Terrain height at building location.</param>
    /// <returns>Sorting order value.</returns>
    private int CalculateBuildingSortingOrderImpl(int x, int y, int height)
    {
        return CalculateTerrainSortingOrderImpl(x, y, height) + BUILDING_OFFSET;
    }

    /// <summary>
    /// Sorting order for any object type at given position.
    /// </summary>
    /// <param name="x">Grid X.</param>
    /// <param name="y">Grid Y.</param>
    /// <param name="objectType">Object type (terrain, building, etc).</param>
    /// <returns>Sorting order value.</returns>
    private int CalculateSortingOrderImpl(int x, int y, ObjectType objectType)
    {
        int height = heightMap.GetHeight(x, y);

        switch (objectType)
        {
            case ObjectType.Terrain:
                return CalculateTerrainSortingOrderImpl(x, y, height);
            case ObjectType.Slope:
                return CalculateSlopeSortingOrderImpl(x, y, height);
            case ObjectType.Building:
                return CalculateBuildingSortingOrderImpl(x, y, height);
            case ObjectType.Road:
                return CalculateTerrainSortingOrderImpl(x, y, height) + 5; // Roads slightly above terrain
            case ObjectType.Utility:
                return CalculateTerrainSortingOrderImpl(x, y, height) + 8; // Utilities above roads
            default:
                return CalculateTerrainSortingOrderImpl(x, y, height);
        }
    }

    /// <summary>
    /// True if GameObject is instance of any water-slope or shore Bay prefab.
    /// </summary>
    /// <param name="obj">GameObject to check.</param>
    /// <returns>True if matches water-slope or Bay prefab.</returns>
    private bool IsWaterSlopeObjectImpl(GameObject obj)
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
            || IsShoreBayObjectImpl(obj);
    }

    /// <summary>
    /// True if object is land slope tile (not water slope).
    /// </summary>
    private bool IsLandSlopeObjectImpl(GameObject obj)
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
    /// True if GameObject is instance of sea-level water prefab.
    /// </summary>
    /// <param name="obj">GameObject to check.</param>
    /// <returns>True if matches sea-level water prefab.</returns>
    private bool IsSeaLevelWaterObjectImpl(GameObject obj)
    {
        return IsPrefabInstance(obj, seaLevelWaterPrefab);
    }

    /// <summary>
    /// True if GameObject is instance of any bay prefab (coastal water terrain).
    /// </summary>
    private bool IsShoreBayObjectImpl(GameObject obj)
    {
        return IsPrefabInstance(obj, northEastBayPrefab)
            || IsPrefabInstance(obj, northWestBayPrefab)
            || IsPrefabInstance(obj, southEastBayPrefab)
            || IsPrefabInstance(obj, southWestBayPrefab);
    }

    /// <summary>
    /// True if <paramref name="obj"/> is brown cliff wall stack segment or water–water cascade cliff
    /// instance (same prefab matching as <see cref="RemoveExistingCliffWalls"/> / cascade cleanup).
    /// Used by <see cref="GridManager.DestroyCellChildren"/> so building placement doesn't strip map-border cliffs.
    /// </summary>
    private bool IsCliffStackTerrainObjectImpl(GameObject obj)
    {
        if (obj == null) return false;
        return IsPrefabInstance(obj, southCliffWallPrefab)
            || IsPrefabInstance(obj, eastCliffWallPrefab)
            || IsPrefabInstance(obj, northCliffWallPrefab)
            || IsPrefabInstance(obj, westCliffWallPrefab)
            || IsPrefabInstance(obj, cliffWaterSouthPrefab)
            || IsPrefabInstance(obj, cliffWaterEastPrefab);
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
    /// Height for land slope / prefab selection. Out-of-map neighbors use <paramref name="currentHeight"/> so MIN_HEIGHT outside grid doesn't fake slopes toward void.
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
    /// Return slope type at (x,y) for ForestManager etc. Same logic as DetermineSlopePrefab.
    /// Flat if heightMap null or position invalid. Calls EnsureHeightMapLoaded() when heightMap null so ForestManager gets slope type even if init order skipped.
    /// </summary>
    private TerrainSlopeType GetTerrainSlopeTypeAtImpl(int x, int y)
    {
        if (heightMap == null)
            EnsureHeightMapLoadedImpl();

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
    /// Modify terrain height at grid position.
    /// </summary>
    /// <param name="x">Grid X.</param>
    /// <param name="y">Grid Y.</param>
    /// <param name="newHeight">New height value.</param>
    private void ModifyTerrainImpl(int x, int y, int newHeight)
    {
        // Implementation for terrain modification
    }

    /// <summary>
    /// Check whether building of given size can be placed at grid position per terrain constraints (height uniformity, slopes, water).
    /// Flat terrain only; reject all slope types until slope building support implemented.
    /// </summary>
    /// <param name="gridPosition">Grid position for placement.</param>
    /// <param name="size">Building footprint size.</param>
    /// <param name="failReason">On false return, specific reason for failure.</param>
    /// <param name="allowCoastalSlope">True → allow placement on tiles w/ slope only due to water adjacency (e.g. water plants).</param>
    /// <param name="allowWaterInFootprint">True → water tiles in footprint allowed (e.g. water plants); skipped for height/slope checks.</param>
    /// <returns>True if terrain allows placement; false otherwise.</returns>
    private bool CanPlaceBuildingInTerrainImpl(Vector2 gridPosition, int size, out string failReason, bool allowCoastalSlope = false, bool allowWaterInFootprint = false)
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
    /// True if road can be placed at (x,y): not occupied, terrain flat/cardinal/diagonal slope.
    /// Water cells (height 0) allowed for bridge placement. Water slope cells (land adjacent to water)
    /// rejected for normal roads to keep 1-cell buffer from coastlines. Diagonal slopes use
    /// orthogonal road prefabs. Corner slopes (NEUp, NWUp, SEUp, SWUp) have no prefabs yet → rejected.
    /// Implements <see cref="ITerrainManager.CanPlaceRoad(int, int)"/>; doesn't allow shore trace (use overload).
    /// </summary>
    private bool CanPlaceRoadImpl(int x, int y)
    {
        return CanPlaceRoadImpl(x, y, allowWaterSlopeForWaterBridgeTrace: false);
    }

    /// <summary>
    /// Same as <see cref="CanPlaceRoad(int, int)"/> with optional shore allowance for pathfinding + manual bridge strokes.
    /// </summary>
    /// <param name="allowWaterSlopeForWaterBridgeTrace">
    /// True (pathfinding / manual road stroke only) → water-slope shore cells may pass so shared
    /// <see cref="Territory.Roads.RoadManager.TryPrepareRoadPlacementPlan"/> pass can validate water bridges.
    /// Single-tile placement + zoning must keep this false.
    /// </param>
    private bool CanPlaceRoadImpl(int x, int y, bool allowWaterSlopeForWaterBridgeTrace)
    {
        if (gridManager != null && gridManager.IsCellOccupiedByBuilding(x, y))
            return false;
        if (gridManager != null)
        {
            CityCell c = gridManager.GetCell(x, y);
            if (c != null && c.GetCellInstanceHeight() == 0)
                return true;
        }
        if (IsWaterSlopeCellImpl(x, y) && !allowWaterSlopeForWaterBridgeTrace)
            return false;
        TerrainSlopeType slope = GetTerrainSlopeTypeAtImpl(x, y);
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
