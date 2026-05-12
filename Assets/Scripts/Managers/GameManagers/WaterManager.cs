using UnityEngine;
using System.Collections.Generic;
using Territory.Core;
using Territory.Zones;
using Territory.Buildings;
using Territory.Persistence;
using Domains.Water.Services;

namespace Territory.Terrain
{
/// <summary>
/// THIN hub — delegates water accounting to <see cref="WaterService"/>.
/// Visual placement + shore refresh call ordering preserved (guardrail #6).
/// Serialized fields UNCHANGED (locked #3). Class/namespace UNCHANGED (locked #1, #2).
/// </summary>
public partial class WaterManager : MonoBehaviour, IWaterManager
{
    public GridManager gridManager;
    public TerrainManager terrainManager;
    public ZoneManager zoneManager;

    public List<GameObject> waterTilePrefabs;

    [Tooltip("When true, procedural lakes come from depression-fill on the height map. When false, any cell with terrain height <= seaLevel is water (legacy).")]
    public bool useLakeDepressionFill = true;

    private LakeFillSettings lakeFillSettings;
    public LakeFillSettings LakeFillSettings => lakeFillSettings;

    [Tooltip("Legacy: height at or below which water is placed when useLakeDepressionFill is false; also used for painted water and restore from old saves.")]
    public int seaLevel = 0;

    private WaterMap waterMap;
    private bool generateStandardWaterBodies = true;
    private WaterService _water = new WaterService();

    private List<WaterPlant> waterPlants = new List<WaterPlant>();
    private bool[,] initialWaterCells = new bool[20, 20]; // all false — default empty

    void Awake()
    {
        lakeFillSettings = new LakeFillSettings();
        const int maxLakeBboxPerAxisCap = 10;
        lakeFillSettings.MaxLakeBoundingExtent = Mathf.Min(lakeFillSettings.MaxLakeBoundingExtent, maxLakeBboxPerAxisCap);
        lakeFillSettings.MaxLakeBoundingExtent = Mathf.Max(lakeFillSettings.MaxLakeBoundingExtent, lakeFillSettings.MinLakeBoundingExtent);
    }

    void Start()
    {
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
    }

    public void SetGenerateStandardWater(bool enabled) => generateStandardWaterBodies = enabled;

    public void InitializeWaterMap()
    {
        if (gridManager == null) return;
        waterMap = new WaterMap(gridManager.width, gridManager.height);
        if (!generateStandardWaterBodies) { UpdateWaterVisuals(); return; }
        if (terrainManager == null) terrainManager = FindObjectOfType<TerrainManager>();
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
            waterMap.MergeSeaLevelDryCellsFromHeightMap(terrainManager.GetHeightMap(), seaLevel);
            if (useLakeDepressionFill && WaterService.TryGetLakeTerrainRefreshRegion(waterMap, gridManager.width, gridManager.height,
                    out int rMinX, out int rMinY, out int rMaxX, out int rMaxY))
                terrainManager.ApplyHeightMapToRegion(rMinX, rMinY, rMaxX, rMaxY);
        }
        else
            InitializeWaterBodiesFromMatrix();
        UpdateWaterVisuals();
    }

    public void GenerateTestRiver(int[] segmentBedWidths = null)
    {
        if (waterMap == null || terrainManager == null || gridManager == null || terrainManager.GetHeightMap() == null) return;
        TestRiverGenerator.Generate(this, terrainManager, gridManager, segmentBedWidths);
        waterMap.MergeAdjacentBodiesWithSameSurface();
        UpdateWaterVisuals(expandShoreRefreshSecondRing: true, skipMultiBodySurfacePasses: true);
        gridManager.InvalidateRoadCache();
    }

    public void GenerateProceduralRiversForNewGame()
    {
        if (waterMap == null || terrainManager == null || gridManager == null || terrainManager.GetHeightMap() == null) return;
        MapGenerationSeed.EnsureSessionMasterSeed();
        int seed = MapGenerationSeed.GetLakeFillRandomSeed();
        var rnd = new System.Random(seed ^ unchecked((int)0xBADC0DE1));
        ProceduralRiverGenerator.Generate(this, terrainManager, gridManager, rnd);
        waterMap.MergeAdjacentBodiesWithSameSurface();
        UpdateWaterVisuals(expandShoreRefreshSecondRing: true);
        gridManager.InvalidateRoadCache();
    }

    public void RestoreWaterMapFromSaveData(WaterMapData data, int gridWidth, int gridHeight, List<CellData> gridData)
    {
        if (gridManager == null) return;
        waterMap = new WaterMap(gridWidth, gridHeight);
        if (data != null && data.waterBodyIds != null && data.waterBodyIds.Length == gridWidth * gridHeight)
            waterMap.LoadFromSerializableData(data);
        else if (gridData != null)
            waterMap.RestoreFromLegacyCellData(gridData, seaLevel);
    }

    public void RestoreWaterMapFromGridData(List<CellData> gridData)
    {
        if (gridManager == null || gridData == null) return;
        if (waterMap == null || waterMap.IsValidPosition(0, 0) == false)
            waterMap = new WaterMap(gridManager.width, gridManager.height);
        waterMap.RestoreFromLegacyCellData(gridData, seaLevel);
    }

    public GameObject FindWaterPrefabByName(string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName) || waterTilePrefabs == null) return GetRandomWaterPrefab();
        string trimmed = prefabName.Replace("(Clone)", "").Trim();
        foreach (GameObject p in waterTilePrefabs)
            if (p != null && p.name == trimmed) return p;
        return GetRandomWaterPrefab();
    }

    public bool IsWaterAt(int x, int y) => waterMap != null && waterMap.IsWater(x, y);

    public void TryRegisterSeaLevelWaterCell(int x, int y)
    {
        if (waterMap == null || terrainManager == null || terrainManager.GetHeightMap() == null) return;
        if (!waterMap.IsValidPosition(x, y) || terrainManager.GetHeightMap().GetHeight(x, y) > seaLevel || waterMap.IsWater(x, y)) return;
        waterMap.AddLegacyPaintedWaterCell(x, y, seaLevel, WaterBodyType.Sea);
    }

    public int GetWaterSurfaceHeight(int x, int y) => waterMap == null ? -1 : waterMap.GetSurfaceHeightAt(x, y);

    // ─── Water plant registry (hub owns WaterPlant; accounting delegated to WaterService) ─────

    public void RegisterWaterPlant(WaterPlant waterPlant)
    {
        waterPlants.Add(waterPlant);
        _water.RegisterWaterProduction(waterPlant.WaterOutput);
    }

    public void UnregisterWaterPlant(WaterPlant waterPlant)
    {
        waterPlants.Remove(waterPlant);
        _water.UnregisterWaterProduction(waterPlant.WaterOutput);
    }

    public void ResetWaterPlants()
    {
        waterPlants.Clear();
        _water.ResetWaterOutput();
    }

    public int GetTotalWaterOutput() => _water.GetTotalWaterOutput();
    public void AddWaterConsumption(int value) => _water.AddWaterConsumption(value);
    public void RemoveWaterConsumption(int value) => _water.RemoveWaterConsumption(value);
    public int GetTotalWaterConsumption() => _water.GetTotalWaterConsumption();
    public bool GetCityWaterAvailability() => _water.GetCityWaterAvailability();
    public bool IsAdjacentToWater(int x, int y) => WaterService.IsAdjacentToWater(waterMap, x, y);
    public WaterMap GetWaterMap() => waterMap;
    public GameObject GetRandomWaterPrefab()
    {
        if (waterTilePrefabs == null || waterTilePrefabs.Count == 0) return null;
        return waterTilePrefabs[Random.Range(0, waterTilePrefabs.Count)];
    }

    private void InitializeWaterBodiesFromMatrix()
    {
        if (waterMap == null || initialWaterCells == null) return;
        int width = Mathf.Min(gridManager.width, initialWaterCells.GetLength(0));
        int height = Mathf.Min(gridManager.height, initialWaterCells.GetLength(1));
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (initialWaterCells[x, y])
                    waterMap.AddLegacyPaintedWaterCell(x, y, seaLevel);
    }
}
}
