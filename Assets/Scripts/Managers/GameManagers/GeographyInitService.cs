// long-file-allowed: legacy hub — scope outside current atomization plan; deferred to future sweep
using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Profiling;
using Territory.Core;
using Territory.Terrain;
using Territory.Forests;
using Territory.Zones;
using Territory.Roads;
using Territory.Economy;
using Territory.UI;
using Territory.Utilities;
using Territory.Persistence;

namespace Territory.Geography
{
/// <summary>
/// Init, load, reinit, and report pipeline extracted from GeographyManager.
/// Holds interchange loading, grid init sequence, and JSON report generation.
/// Invariants #1 (HeightMap/Cell sync), #7 (shore band) preserved via delegated manager calls.
/// </summary>
public class GeographyInitService
{
    private readonly GeographyManager _hub;

    private static readonly Regex StripEmptyInterchangeSnapshotJsonProperty = new Regex(
        @"\r?\n[ \t]*""interchange_snapshot_json""\s*:\s*"""",?\s*|,\s*""interchange_snapshot_json""\s*:\s*""""",
        RegexOptions.CultureInvariant);

    // Interchange state — reset each pipeline run
    private GeographyInitParamsDto _lastLoadedParams;
    private GeographyInitMapDto _mapAdvisory;
    private bool? _riversEnabledFromInterchange;

    public GeographyInitService(GeographyManager hub)
    {
        _hub = hub;
    }

    /// <summary>Expose last loaded params to hub for report JSON.</summary>
    public GeographyInitParamsDto LastLoadedParams => _lastLoadedParams;

    /// <summary>Override for procedural rivers (null = use Inspector toggle).</summary>
    public bool GetEffectiveProceduralRiversOnInit() =>
        _riversEnabledFromInterchange ?? _hub.generateProceduralRiversOnInit;

    // ---- InitializeGeography ----

    public void InitializeGeography()
    {
        ApplyInterchangeAtPipelineStart();

        if (_hub.regionalMapManager != null)
            _hub.regionalMapManager.InitializeRegionalMap();

        var terrainManager = _hub.terrainManager;
        if (terrainManager == null)
            terrainManager = UnityEngine.Object.FindObjectOfType<TerrainManager>();
        if (terrainManager != null)
            terrainManager.SetNewGameFlatTerrainOptions(_hub.useFlatTerrainOnNewGame, _hub.flatTerrainHeight);

        if (_hub.gridManager != null)
        {
            // Pre-warm tile pool before grid creation to eliminate per-tile Instantiate allocations.
            var pool = _hub.gridManager.TilePool;
            if (pool != null)
            {
                int w = _hub.gridManager.width > 0 ? _hub.gridManager.width : 64;
                int h = _hub.gridManager.height > 0 ? _hub.gridManager.height : 64;
                // Resolve base grass prefab via ZoneManager for pre-warm
                var zoneManager = _hub.gridManager.zoneManager;
                GameObject tilePrefab = zoneManager != null
                    ? (zoneManager.GetRandomZonePrefab(Territory.Zones.Zone.ZoneType.Grass, 1) ??
                       (zoneManager.grassPrefabs != null && zoneManager.grassPrefabs.Count > 0 ? zoneManager.grassPrefabs[0] : null))
                    : null;
                if (tilePrefab != null)
                    pool.PreWarm(w * h, tilePrefab);
                else
                    Debug.LogWarning("[GeographyInitService] TilePool pre-warm skipped: could not resolve grass tile prefab.");
            }
            else
            {
                Debug.LogWarning("[GeographyInitService] TilePool not wired on GridManager — pre-warm skipped.");
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Profiler.BeginSample("GeographyInitService.CreateGrid");
#endif
            try { _hub.gridManager.InitializeGrid(); }
            finally
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Profiler.EndSample();
#endif
            }
        }

        WarnMapMismatch();

        RunWaterPipeline();
        RunInterstatePipeline(false);

        if (_hub.forestManager != null)
        {
            if (_hub.initializeForestsOnStart)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Profiler.BeginSample("GeographyInitService.InitializeForestMap");
#endif
                try { _hub.forestManager.InitializeForestMap(); }
                finally
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Profiler.EndSample();
#endif
                }
            }
            // deferred path: forest init runs post-playable via coroutine after IsInitialized fires
        }

        _hub.InitializeWaterDesirability();

        _hub.currentGeographyData = _hub.CreateGeographyData();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Profiler.BeginSample("GeographyInitService.ReCalculateSortingOrderBasedOnHeight");
#endif
        try { _hub.ReCalculateSortingOrderBasedOnHeight(); }
        finally
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Profiler.EndSample();
#endif
        }

        if (_hub.regionalMapManager != null)
            _hub.regionalMapManager.PlaceBorderSigns();

        PostInterstateNotification();
        _hub.NotifyMiniMap();

        _hub.IsInitialized = true;

        // Deferred forest init: start coroutine after playable state is signalled.
        if (_hub.forestManager != null && !_hub.initializeForestsOnStart)
            _hub.StartCoroutine(DeferredForestInit());
    }

    // ---- DeferredForestInit ----

    private IEnumerator DeferredForestInit()
    {
        yield return null;
        yield return null;
        _hub.forestManager.InitializeForestMap();
    }

    // ---- ReinitializeGeographyForNewGame ----

    public void ReinitializeGeographyForNewGame()
    {
        ApplyInterchangeAtPipelineStart();
        WarnMapMismatch();

        if (_hub.regionalMapManager != null)
            _hub.regionalMapManager.InitializeRegionalMap();

        if (_hub.terrainManager != null)
            _hub.terrainManager.InitializeHeightMap();

        RunWaterPipeline();
        RunInterstateWithGuard();

        if (_hub.forestManager != null && _hub.initializeForestsOnStart)
            _hub.forestManager.InitializeForestMap();

        _hub.InitializeWaterDesirability();
        _hub.ReCalculateSortingOrderBasedOnHeight();

        if (_hub.regionalMapManager != null)
            _hub.regionalMapManager.PlaceBorderSigns();

        PostInterstateNotification();
        _hub.NotifyMiniMap();
    }

    // ---- LoadGeography ----

    public void LoadGeography(GeographyData data)
    {
        _hub.currentGeographyData = data;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Profiler.BeginSample("GeographyInitService.RestoreGrid");
#endif
        try
        {
            if (data.hasTerrainData && _hub.terrainManager != null)
                _hub.terrainManager.InitializeHeightMap();

            if (data.hasWaterData && _hub.waterManager != null)
                _hub.waterManager.InitializeWaterMap();

            if (data.hasForestData && _hub.forestManager != null)
                _hub.forestManager.InitializeForestMap();

            _hub.InitializeWaterDesirability();
            _hub.ReCalculateSortingOrderBasedOnHeight();
            _hub.NotifyMiniMap();
        }
        finally
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Profiler.EndSample();
#endif
        }
    }

    // ---- BuildGeographyInitReportJson ----

    public string BuildGeographyInitReportJson()
    {
        MapGenerationSeed.EnsureSessionMasterSeed();
        var root = new GeographyInitReportRootDto
        {
            artifact = "geography_init_report",
            schema_version = 1,
            exported_utc_unix_seconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            master_seed = MapGenerationSeed.MasterSeed,
            interchange_file_was_applied = _lastLoadedParams != null,
            interchange_snapshot_json = _lastLoadedParams != null
                ? UnityEngine.JsonUtility.ToJson(_lastLoadedParams)
                : null,
            map_width = _hub.gridManager != null ? _hub.gridManager.width : 0,
            map_height = _hub.gridManager != null ? _hub.gridManager.height : 0,
            generate_standard_water_bodies = _hub.generateStandardWaterBodies,
            procedural_rivers_enabled_effective = GetEffectiveProceduralRiversOnInit(),
            generate_test_river_on_init = _hub.generateTestRiverOnInit,
            forest_coverage_percentage = GetForestCoverageForReport(),
            notes = "Not Save data. interchange_snapshot_json set when StreamingAssets geography_init_params loaded successfully this pipeline run."
        };
        string json = UnityEngine.JsonUtility.ToJson(root, true);
        if (_lastLoadedParams == null)
            json = StripEmptyInterchangeSnapshotJsonProperty.Replace(json, string.Empty);
        return json;
    }

    // ---- Privates ----

    private void ApplyInterchangeAtPipelineStart()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        UnityEngine.Profiling.Profiler.BeginSample("GeographyInitParams.Load");
#endif
        _mapAdvisory = null;
        _riversEnabledFromInterchange = null;
        _lastLoadedParams = null;
        try
        {
            if (!_hub.loadGeographyInitParamsFromStreamingAssets)
            {
                MapGenerationSeed.EnsureSessionMasterSeed();
                return;
            }

            string path = Path.Combine(Application.streamingAssetsPath, _hub.geographyInitParamsRelativePath);
            if (!GeographyInitParamsLoader.TryLoadFromPath(path, out GeographyInitParamsDto dto, out string err))
            {
                if (!string.IsNullOrEmpty(err))
                    DebugHelper.LogWarning($"GeographyManager: Geography init interchange — {err}");
                MapGenerationSeed.EnsureSessionMasterSeed();
                return;
            }

            _lastLoadedParams = dto;
            MapGenerationSeed.SetSessionMasterSeed(dto.seed);
            if (dto.rivers != null)
                _riversEnabledFromInterchange = dto.rivers.enabled;
            if (dto.map != null)
                _mapAdvisory = dto.map;

            if (dto.water != null)
                DebugHelper.Log($"GeographyManager: Interchange water.seaBias={dto.water.seaBias} (reserved for FEAT-46; not applied).");
            if (dto.forest != null)
                DebugHelper.Log($"GeographyManager: Interchange forest.coverageTarget={dto.forest.coverageTarget} (reserved for FEAT-46; not applied).");

            DebugHelper.Log($"GeographyManager: Loaded geography interchange StreamingAssets/{_hub.geographyInitParamsRelativePath} (seed applied; procedural rivers={(_riversEnabledFromInterchange.HasValue ? _riversEnabledFromInterchange.Value.ToString() : "Inspector")}).");
        }
        finally
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UnityEngine.Profiling.Profiler.EndSample();
#endif
        }
    }

    private void WarnMapMismatch()
    {
        if (_mapAdvisory == null || _hub.gridManager == null) return;
        if (_hub.gridManager.width != _mapAdvisory.width || _hub.gridManager.height != _mapAdvisory.height)
        {
            DebugHelper.LogWarning(
                $"GeographyManager: Interchange map ({_mapAdvisory.width}x{_mapAdvisory.height}) does not match grid ({_hub.gridManager.width}x{_hub.gridManager.height}). Interchange map is advisory only.");
        }
    }

    private void RunWaterPipeline()
    {
        if (_hub.waterManager == null) return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Profiler.BeginSample("GeographyInitService.RunWaterPipeline");
#endif
        try
        {
            _hub.waterManager.SetGenerateStandardWater(_hub.generateStandardWaterBodies);
            _hub.waterManager.InitializeWaterMap();
            if (_hub.generateStandardWaterBodies && GetEffectiveProceduralRiversOnInit())
                _hub.waterManager.GenerateProceduralRiversForNewGame();
            if (_hub.generateTestRiverOnInit)
                _hub.waterManager.GenerateTestRiver(_hub.testRiverSegmentBedWidths);
        }
        finally
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Profiler.EndSample();
#endif
        }
    }

    private void RunInterstatePipeline(bool withTerrainGuard)
    {
        if (_hub.interstateManager == null) return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Profiler.BeginSample("GeographyInitService.RunInterstatePipeline");
#endif
        try
        {
            const int maxAttempts = 3;
            bool placed = false;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                placed = _hub.interstateManager.GenerateAndPlaceInterstate(attempt);
                if (placed) break;
            }
            if (!placed)
                placed = _hub.interstateManager.TryGenerateInterstateDeterministic();
        }
        finally
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Profiler.EndSample();
#endif
        }
    }

    private void RunInterstateWithGuard()
    {
        if (_hub.interstateManager == null) return;
        if (_hub.terrainManager == null || _hub.terrainManager.GetHeightMap() == null || _hub.gridManager == null)
        {
            DebugHelper.LogWarning("GeographyManager: Skipping interstate - terrainManager, heightMap, or gridManager missing.");
            return;
        }
        RunInterstatePipeline(true);
    }

    private void PostInterstateNotification()
    {
        if (_hub.interstateManager == null || GameNotificationManager.Instance == null) return;
        if (_hub.interstateManager.InterstatePositions != null && _hub.interstateManager.InterstatePositions.Count >= 2)
        {
            CityStats cityStats = UnityEngine.Object.FindObjectOfType<CityStats>();
            string cityName = (cityStats != null && !string.IsNullOrEmpty(cityStats.cityName)) ? cityStats.cityName : "your city";
            GameNotificationManager.Instance.PostNotification(
                "Welcome to " + cityName + "! An Interstate Highway crosses your territory. Build a road connecting to it to start developing your city.",
                GameNotificationManager.NotificationType.Info,
                8f
            );
        }
        else
        {
            DebugHelper.LogWarning("GeographyManager: Interstate could not be placed (no valid path). Game continues without interstate.");
        }
    }

    private float GetForestCoverageForReport()
    {
        if (_hub.forestManager == null) return 0f;
        var map = _hub.forestManager.GetForestMap();
        return map != null ? map.GetForestCoveragePercentage() : 0f;
    }
}
}
