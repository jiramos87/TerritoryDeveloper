using System.Collections.Generic;
using Territory.Core;
using Territory.Economy;
using Territory.Persistence;
using Territory.Roads;
using Territory.Terrain;
using Territory.Timing;
using UnityEngine;

namespace Territory.Testing
{
    /// <summary>
    /// Apply <see cref="ScenarioDescriptorV1"/> in <b>Play Mode</b> after base <b>Save data</b> load: terrain + <b>Water map</b>,
    /// optional <b>road stroke</b> commits via <see cref="RoadManager.TryCommitStreetStrokeForScenarioBuild"/> /
    /// <see cref="RoadManager.PlaceInterstateFromPath"/>. Callers export via <see cref="GameSaveManager.TryWriteGameSaveToPath"/>.
    /// </summary>
    public static class ScenarioDescriptorRuntimeApplier
    {
        const string ExpectedArtifact = "scenario_descriptor_v1";
        const int ScenarioGridSize = 32;
        const int ScenarioGridCellCount = ScenarioGridSize * ScenarioGridSize;

        /// <summary>
        /// Parse JSON + mutate loaded scene: terrain, water, roads, optional time/city overlay.
        /// </summary>
        public static bool TryApplyFromJson(string json, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "descriptor rejected: empty JSON";
                return false;
            }

            ScenarioDescriptorV1 d = JsonUtility.FromJson<ScenarioDescriptorV1>(json);
            if (d == null)
            {
                error = "descriptor rejected: parse failed";
                return false;
            }

            if (d.schemaVersion != 1 || d.artifact != ExpectedArtifact)
            {
                error =
                    $"descriptor rejected: expected artifact \"{ExpectedArtifact}\" and schemaVersion 1 (Save data and interchange contracts use distinct artifacts)";
                return false;
            }

            if (d.map == null || d.map.width != ScenarioGridSize || d.map.height != ScenarioGridSize)
            {
                error = "descriptor rejected: map must be 32×32 for v1 scenario builder";
                return false;
            }

            if (d.terrain == null || string.IsNullOrEmpty(d.terrain.mode))
            {
                error = "descriptor rejected: terrain.mode is required";
                return false;
            }

            if (d.layoutKind == "autoAdjacent" && d.roadStrokes != null && d.roadStrokes.Length > 0)
            {
                error =
                    "descriptor rejected: layoutKind autoAdjacent must not include road strokes — use declarative layout or an AUTO simulation export workflow";
                return false;
            }

            GridManager gridManager = Object.FindObjectOfType<GridManager>();
            TerrainManager terrainManager = Object.FindObjectOfType<TerrainManager>();
            WaterManager waterManager = Object.FindObjectOfType<WaterManager>();
            RoadManager roadManager = Object.FindObjectOfType<RoadManager>();
            InterstateManager interstateManager = Object.FindObjectOfType<InterstateManager>();

            if (gridManager == null || terrainManager == null)
            {
                error = "descriptor apply failed: GridManager or TerrainManager not found";
                return false;
            }

            if (gridManager.width != ScenarioGridSize || gridManager.height != ScenarioGridSize)
            {
                error = "descriptor apply failed: loaded grid must be 32×32";
                return false;
            }

            HeightMap heightMap = terrainManager.GetHeightMap();
            if (heightMap == null)
            {
                error = "descriptor apply failed: HeightMap missing";
                return false;
            }

            if (d.terrain.mode == "uniform")
            {
                int h = d.terrain.uniformHeight;
                if (h < TerrainManager.MIN_HEIGHT || h > TerrainManager.MAX_HEIGHT)
                {
                    error = $"descriptor rejected: uniformHeight {h} out of allowed terrain range";
                    return false;
                }

                for (int x = 0; x < ScenarioGridSize; x++)
                {
                    for (int y = 0; y < ScenarioGridSize; y++)
                        heightMap.SetHeight(x, y, h);
                }
            }
            else if (d.terrain.mode == "rowMajor")
            {
                int[] flat = d.terrain.heightsRowMajor;
                if (flat == null || flat.Length != ScenarioGridCellCount)
                {
                    error = "descriptor rejected: terrain.heightsRowMajor must have length 1024 (y * 32 + x order)";
                    return false;
                }

                for (int y = 0; y < ScenarioGridSize; y++)
                {
                    for (int x = 0; x < ScenarioGridSize; x++)
                    {
                        int v = flat[y * ScenarioGridSize + x];
                        heightMap.SetHeight(x, y, v);
                    }
                }
            }
            else
            {
                error = $"descriptor rejected: unknown terrain.mode \"{d.terrain.mode}\"";
                return false;
            }

            terrainManager.ApplyHeightMapToGrid();

            if (d.waterMapData != null && d.waterMapData.waterBodyIds != null
                                       && d.waterMapData.waterBodyIds.Length == ScenarioGridCellCount)
            {
                if (waterManager == null)
                {
                    error = "descriptor apply failed: waterMapData set but WaterManager missing";
                    return false;
                }

                waterManager.RestoreWaterMapFromSaveData(d.waterMapData, ScenarioGridSize, ScenarioGridSize, gridManager.GetGridData());
                waterManager.MigrateWaterBodyIdsAfterGridRestore();
                terrainManager.RefreshShoreTerrainAfterWaterUpdate(waterManager);
            }

            if (d.roadStrokes != null && d.roadStrokes.Length > 0)
            {
                if (roadManager == null)
                {
                    error = "descriptor apply failed: road strokes require RoadManager";
                    return false;
                }

                bool anyInterstate = false;
                for (int s = 0; s < d.roadStrokes.Length; s++)
                {
                    RoadStrokeV1 stroke = d.roadStrokes[s];
                    if (stroke == null || stroke.cells == null || stroke.cells.Length < 2)
                    {
                        error = $"descriptor rejected: road stroke {s} must have at least two cells";
                        return false;
                    }

                    string kind = stroke.kind ?? string.Empty;
                    if (kind == "street")
                    {
                        var path = new List<Vector2>(stroke.cells.Length);
                        for (int i = 0; i < stroke.cells.Length; i++)
                            path.Add(new Vector2(stroke.cells[i].x, stroke.cells[i].y));

                        if (!roadManager.TryCommitStreetStrokeForScenarioBuild(path, out string roadErr))
                        {
                            error = roadErr ?? $"road stroke {s} failed";
                            return false;
                        }
                    }
                    else if (kind == "interstate")
                    {
                        anyInterstate = true;
                        var pathI = new List<Vector2Int>(stroke.cells.Length);
                        for (int i = 0; i < stroke.cells.Length; i++)
                            pathI.Add(new Vector2Int(stroke.cells[i].x, stroke.cells[i].y));

                        if (!roadManager.PlaceInterstateFromPath(pathI))
                        {
                            error =
                                $"interstate road stroke {s} rejected by road preparation or PathTerraformPlan.Apply (see wet run and cut-through rules)";
                            return false;
                        }
                    }
                    else
                    {
                        error = $"descriptor rejected: road stroke kind \"{kind}\" must be street or interstate";
                        return false;
                    }
                }

                gridManager.InvalidateRoadCache();
                if (anyInterstate && interstateManager != null)
                {
                    interstateManager.RebuildFromGrid();
                    interstateManager.CheckInterstateConnectivity();
                }
            }

            if (d.saveOverlay != null)
            {
                TimeManager timeManager = Object.FindObjectOfType<TimeManager>();
                CityStats cityStats = Object.FindObjectOfType<CityStats>();
                if (!string.IsNullOrEmpty(d.saveOverlay.cityName) && cityStats != null)
                    cityStats.cityName = d.saveOverlay.cityName;
                if (d.saveOverlay.inGameTime.year > 0 && timeManager != null)
                    timeManager.RestoreInGameTime(d.saveOverlay.inGameTime);
            }

            return true;
        }

        /// <summary>Save name for export: overlay → scenario id → fallback.</summary>
        public static string ResolveSaveNameForExport(ScenarioDescriptorV1 d)
        {
            if (d?.saveOverlay != null && !string.IsNullOrEmpty(d.saveOverlay.saveName))
                return d.saveOverlay.saveName;
            if (!string.IsNullOrEmpty(d?.scenarioId))
                return d.scenarioId;
            return "scenario-export";
        }
    }
}
