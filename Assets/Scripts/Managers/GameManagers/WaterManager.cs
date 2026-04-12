using UnityEngine;
using System.Collections.Generic;
using Territory.Core;
using Territory.Zones;
using Territory.Buildings;
using Territory.Persistence;

namespace Territory.Terrain
{
/// <summary>
/// Generate + manage water bodies on grid. Lakes → depression-fill on height map;
/// sea-level terrain cells merged into <see cref="WaterMap"/> after fill → match <c>PlaceSeaLevelWater</c>.
/// Save → <see cref="WaterMap.GetSerializableData"/>; load → <see cref="RestoreWaterMapFromSaveData"/>.
/// Procedural rivers: <see cref="GenerateProceduralRiversForNewGame"/> after lake init, before interstate.
/// QA: <see cref="SetGenerateStandardWater"/> + <see cref="GenerateTestRiver"/> driven from GeographyManager (Inspector toggles on <c>Territory.Geography.GeographyManager</c>).
/// Legacy sea-level threshold kept for paint tool / old save restore. Coordinates with GridManager,
/// TerrainManager, ZoneManager. <see cref="LakeFillSettings"/> built in code (not Inspector) until terrain UI exists.
/// </summary>
public partial class WaterManager : MonoBehaviour
{
    public GridManager gridManager;
    public TerrainManager terrainManager;
    public ZoneManager zoneManager;

    public List<GameObject> waterTilePrefabs; // Water tile prefabs with animation

    [Tooltip("When true, procedural lakes come from depression-fill on the height map (multi-level surfaces). When false, any cell with terrain height <= seaLevel is water (legacy).")]
    public bool useLakeDepressionFill = true;

    /// <summary>Lake depression-fill params — not serialized; tune defaults in <see cref="LakeFillSettings"/> (terrain generator UI will expose later).</summary>
    private LakeFillSettings lakeFillSettings;

    /// <summary>Read-only access → terrain feasibility + diagnostics.</summary>
    public LakeFillSettings LakeFillSettings => lakeFillSettings;

    [Tooltip("Legacy: height at or below which water is placed when useLakeDepressionFill is false; also used for painted water and restore from old saves.")]
    public int seaLevel = 0;

    private WaterMap waterMap;

    /// <summary>False → <see cref="InitializeWaterMap"/> only allocates empty <see cref="WaterMap"/> (no lakes/sea from terrain).</summary>
    private bool generateStandardWaterBodies = true;

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

    /// <summary>
    /// <paramref name="enabled"/> false → next <see cref="InitializeWaterMap"/> creates empty <see cref="WaterMap"/> without lake/sea placement from terrain.
    /// Call from <see cref="Territory.Geography.GeographyManager"/> before <see cref="InitializeWaterMap"/> when toggling QA options.
    /// </summary>
    public void SetGenerateStandardWater(bool enabled)
    {
        generateStandardWaterBodies = enabled;
    }

    public void InitializeWaterMap()
    {
        if (gridManager != null)
        {
            waterMap = new WaterMap(gridManager.width, gridManager.height);

            if (!generateStandardWaterBodies)
            {
                UpdateWaterVisuals();
                return;
            }

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
        }
    }

    /// <summary>
    /// QA straight grid West→East test river (four segments S=4..1; see <see cref="TestRiverGenerator"/>). Run after <see cref="InitializeWaterMap"/> + optional <see cref="GenerateProceduralRiversForNewGame"/>.
    /// </summary>
    /// <param name="segmentBedWidths">Four entries (bed width 1–3 per segment); null → default 1,2,3,2.</param>
    public void GenerateTestRiver(int[] segmentBedWidths = null)
    {
        if (waterMap == null || terrainManager == null || gridManager == null || terrainManager.GetHeightMap() == null)
            return;

        TestRiverGenerator.Generate(this, terrainManager, gridManager, segmentBedWidths);
        waterMap.MergeAdjacentBodiesWithSameSurface();
        UpdateWaterVisuals(expandShoreRefreshSecondRing: true, skipMultiBodySurfacePasses: true);
        gridManager.InvalidateRoadCache();
    }

    /// <summary>
    /// Procedural static rivers: run after <see cref="InitializeWaterMap"/> (lakes/sea), before interstate.
    /// </summary>
    public void GenerateProceduralRiversForNewGame()
    {
        if (waterMap == null || terrainManager == null || gridManager == null || terrainManager.GetHeightMap() == null)
            return;

        MapGenerationSeed.EnsureSessionMasterSeed();
        int seed = MapGenerationSeed.GetLakeFillRandomSeed();
        var rnd = new System.Random(seed ^ unchecked((int)0xBADC0DE1));
        ProceduralRiverGenerator.Generate(this, terrainManager, gridManager, rnd);
        waterMap.MergeAdjacentBodiesWithSameSurface();
        UpdateWaterVisuals(expandShoreRefreshSecondRing: true);
        gridManager.InvalidateRoadCache();
    }

    /// <summary>
    /// Build inclusive rect for <see cref="TerrainManager.ApplyHeightMapToRegion"/> after lake fill: union of
    /// all water cells (margin for shore/cliffs) + artificial carve dirty rect when present.
    /// Match fallback path → procedural lakes get same terrain + cliff refresh as carved rectangles.
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
    /// Restore WaterMap from serialized save data, or best-effort from legacy CellData when <paramref name="data"/> missing.
    /// Call after RestoreHeightMapFromGridData + before RestoreGrid.
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
    /// Restore WaterMap from saved grid data only (legacy saves). Prefer <see cref="RestoreWaterMapFromSaveData"/>.
    /// </summary>
    public void RestoreWaterMapFromGridData(List<CellData> gridData)
    {
        if (gridManager == null || gridData == null) return;

        if (waterMap == null || waterMap.IsValidPosition(0, 0) == false)
            waterMap = new WaterMap(gridManager.width, gridManager.height);

        waterMap.RestoreFromLegacyCellData(gridData, seaLevel);
    }

    /// <summary>
    /// Resolve water tile prefab by saved prefab name for load restore. Fallback → random when not found.
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
    /// Register sea-level cell in <see cref="WaterMap"/> when terrain placed water without lake fill (e.g. <c>PlaceSeaLevelWater</c> at runtime).
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
        waterMap.AddLegacyPaintedWaterCell(x, y, seaLevel, WaterBodyType.Sea);
    }

    /// <summary>-1 if cell not water.</summary>
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

        int terrainHeight = seaLevel;
        if (terrainManager != null && terrainManager.GetHeightMap() != null)
            terrainHeight = terrainManager.GetHeightMap().GetHeight(x, y);

        if (!waterMap.IsWater(x, y))
        {
            WaterBodyType provisional = terrainHeight <= seaLevel ? WaterBodyType.Sea : WaterBodyType.Lake;
            waterMap.AddLegacyPaintedWaterCell(x, y, seaLevel, provisional);
        }

        int surfaceHeight = waterMap.GetSurfaceHeightAt(x, y);
        if (surfaceHeight < 0)
            surfaceHeight = seaLevel;

        // Logical surface height in WaterMap is spill (fill level). World placement uses one step lower (Option A / FEAT-37).
        int visualSurfaceHeight = Mathf.Max(TerrainManager.MIN_HEIGHT, surfaceHeight - 1);

        // Update the grid cell to display water
        GameObject cell = gridManager.gridArray[x, y];
        Cell cellComponent = gridManager.GetCell(x, y);
        if (cellComponent == null)
            return;

        // Terrain already placed sea-level water; do not replace with animated lake prefabs. Sync inspector fields from the existing child.
        // Procedural rivers (FEAT-38) must always use the path below so all bed cells share the same visual surface placement;
        // the legacy branch would leave border/sea-level lecho cells misaligned with the rest of the same river body.
        WaterBodyType classificationForLegacyPath = waterMap.GetBodyClassificationAt(x, y);
        if (terrainHeight <= seaLevel
            && cellComponent.zoneType == Zone.ZoneType.Water
            && cell.transform.childCount > 0
            && classificationForLegacyPath != WaterBodyType.River)
        {
            cellComponent.waterBodyType = WaterBodyType.Sea;
            cellComponent.waterBodyId = waterMap.GetWaterBodyId(x, y);
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

        WaterBodyType cls = waterMap.GetBodyClassificationAt(x, y);
        if (cls != WaterBodyType.None)
            cellComponent.waterBodyType = cls;
        else
            cellComponent.waterBodyType = terrainHeight <= seaLevel ? WaterBodyType.Sea : WaterBodyType.Lake;
        cellComponent.waterBodyId = waterMap.GetWaterBodyId(x, y);
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
        cellComponent.waterBodyId = 0;
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

        OnLandCellHeightCommitted(x, y);
    }

    /// <summary>
    /// Refresh every water cell prefab + water–water cascade cliffs. Unless <paramref name="skipMultiBodySurfacePasses"/> true: Pass A + B (§12.7) bed normalization → junction merge →
    /// lake–river high/low fallback (dry rim at <c>S</c> where Pass A/B skipped for lakes) → <see cref="PlaceWater"/>,
    /// <see cref="TerrainManager.RefreshWaterCascadeCliffs"/>, <see cref="TerrainManager.RefreshShoreTerrainAfterWaterUpdate"/> when
    /// depression-fill enabled, Pass B merged junction cells, or fallback ran.
    /// </summary>
    /// <param name="expandShoreRefreshSecondRing">True → expand land shore refresh halo (procedural river confluences).</param>
    /// <param name="skipMultiBodySurfacePasses">True → skip Pass A/B (§12.7 bed normalization + junction merge). Use after QA test river → intentional multi-surface segments not merged into lowest pool.</param>
    public void UpdateWaterVisuals(bool expandShoreRefreshSecondRing = false, bool skipMultiBodySurfacePasses = false)
    {
        if (waterMap == null || gridManager == null) return;

        if (terrainManager == null)
            terrainManager = FindObjectOfType<TerrainManager>();
        HeightMap hm = terrainManager != null ? terrainManager.GetHeightMap() : null;
        bool junctionMerged = false;
        if (hm != null && !skipMultiBodySurfacePasses)
        {
            waterMap.ApplyMultiBodySurfaceBoundaryNormalization(hm);
            junctionMerged = waterMap.ApplyWaterSurfaceJunctionMerge(hm, gridManager, out int jMinX, out int jMinY, out int jMaxX, out int jMaxY);
            if (junctionMerged && terrainManager != null)
                terrainManager.ApplyHeightMapToRegion(jMinX, jMinY, jMaxX, jMaxY);
        }

        bool lakeRiverFallback = false;
        List<(int x, int y, int lakeSurface)> lakeRiverRimCells = null;
        if (hm != null)
        {
            lakeRiverFallback = waterMap.ApplyLakeHighToRiverLowContactFallback(hm, gridManager, out lakeRiverRimCells);
            if (lakeRiverFallback && terrainManager != null && lakeRiverRimCells != null && lakeRiverRimCells.Count > 0)
            {
                int rMinX = int.MaxValue, rMinY = int.MaxValue, rMaxX = int.MinValue, rMaxY = int.MinValue;
                foreach (var (rx, ry, _) in lakeRiverRimCells)
                {
                    if (rx < rMinX) rMinX = rx;
                    if (ry < rMinY) rMinY = ry;
                    if (rx > rMaxX) rMaxX = rx;
                    if (ry > rMaxY) rMaxY = ry;
                }
                terrainManager.ApplyHeightMapToRegion(rMinX, rMinY, rMaxX, rMaxY);
                ReapplyLakeRiverFallbackRimTerrain(lakeRiverRimCells, hm);
            }
        }

        for (int x = 0; x < gridManager.width; x++)
        {
            for (int y = 0; y < gridManager.height; y++)
            {
                if (waterMap.IsWater(x, y))
                    PlaceWater(x, y);
            }
        }

        SyncAllOpenWaterCellsBodyIdsFromMap();

        if (terrainManager == null)
            terrainManager = FindObjectOfType<TerrainManager>();
        if (terrainManager != null)
            terrainManager.RefreshWaterCascadeCliffs(this);

        if (terrainManager != null && (useLakeDepressionFill || junctionMerged || lakeRiverFallback || expandShoreRefreshSecondRing))
        {
            terrainManager.RefreshShoreTerrainAfterWaterUpdate(this, expandSecondChebyshevRing: expandShoreRefreshSecondRing || junctionMerged || lakeRiverFallback);
            if (lakeRiverFallback && lakeRiverRimCells != null && lakeRiverRimCells.Count > 0)
                ReapplyLakeRiverFallbackRimTerrain(lakeRiverRimCells, hm);
        }
    }

    /// <summary>
    /// Re-apply logical surface height + terrain after <see cref="TerrainManager.RefreshShoreTerrainAfterWaterUpdate"/> —
    /// <see cref="TerrainManager.ClampShoreLandHeightsToAdjacentWaterSurface"/> can pull rim cells toward lower pool&apos;s <c>S</c>.
    /// </summary>
    private void ReapplyLakeRiverFallbackRimTerrain(List<(int x, int y, int lakeSurface)> lakeRiverRimCells, HeightMap hm)
    {
        if (lakeRiverRimCells == null || lakeRiverRimCells.Count == 0 || hm == null || terrainManager == null || gridManager == null)
            return;

        foreach (var (x, y, sLake) in lakeRiverRimCells)
        {
            if (!hm.IsValidPosition(x, y))
                continue;
            int clamped = Mathf.Clamp(sLake, TerrainManager.MIN_HEIGHT, TerrainManager.MAX_HEIGHT);
            hm.SetHeight(x, y, clamped);
            gridManager.SetCellHeight(new Vector2(x, y), hm.GetHeight(x, y));
            terrainManager.RestoreTerrainForCell(x, y, hm);
            Cell cell = gridManager.GetCell(x, y);
            if (cell != null)
            {
                cell.zoneType = Zone.ZoneType.Grass;
                cell.waterBodyType = WaterBodyType.None;
                cell.waterBodyId = 0;
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
