// long-file-allowed: legacy hub — scope outside current atomization plan; deferred to future sweep
using UnityEngine;
using System.Collections.Generic;
using Territory.Core;
using Territory.Zones;
using Domains.Water.Services;

namespace Territory.Terrain
{
    /// <summary>
    /// Water visual placement: PlaceWater, RemoveWater, UpdateWaterVisuals.
    /// Extracted to partial to keep WaterManager.cs hub ≤200 LOC (Stage 4.4 THIN).
    /// Guardrail #6 (RefreshShoreTerrainAfterWaterUpdate ordering) preserved verbatim.
    /// </summary>
    public partial class WaterManager
    {
        public void PlaceWater(int x, int y)
        {
            if (waterMap == null || !waterMap.IsValidPosition(x, y)) return;

            int terrainHeight = seaLevel;
            if (terrainManager != null && terrainManager.GetHeightMap() != null)
                terrainHeight = terrainManager.GetHeightMap().GetHeight(x, y);

            if (!waterMap.IsWater(x, y))
            {
                WaterBodyType provisional = terrainHeight <= seaLevel ? WaterBodyType.Sea : WaterBodyType.Lake;
                waterMap.AddLegacyPaintedWaterCell(x, y, seaLevel, provisional);
            }

            int surfaceHeight = waterMap.GetSurfaceHeightAt(x, y);
            if (surfaceHeight < 0) surfaceHeight = seaLevel;

            int visualSurfaceHeight = Mathf.Max(TerrainManager.MIN_HEIGHT, surfaceHeight - 1);

            GameObject cell = gridManager.gridArray[x, y];
            CityCell cellComponent = gridManager.GetCell(x, y);
            if (cellComponent == null) return;

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
                    if (!string.IsNullOrEmpty(n)) { cellComponent.prefabName = n; cellComponent.buildingType = n; }
                }
                return;
            }

            foreach (Transform child in cell.transform) GameObject.Destroy(child.gameObject);

            cellComponent.zoneType = Zone.ZoneType.Water;
            gridManager.SetCellHeight(new Vector2(x, y), terrainHeight);
            Vector2 cellWorldPos = gridManager.GetWorldPositionVector(x, y, terrainHeight);
            cell.transform.position = cellWorldPos;
            cellComponent.transformPosition = cellWorldPos;

            Vector2 waterSurfaceWorld = gridManager.GetWorldPositionVector(x, y, visualSurfaceHeight);
            float halfCellHeight = gridManager.tileHeight * 0.25f;
            Vector2 waterTileWorldPos = waterSurfaceWorld + new Vector2(0f, halfCellHeight);

            GameObject waterPrefab = GetRandomWaterPrefab();
            if (waterPrefab == null) return;

            GameObject waterTile = GameObject.Instantiate(waterPrefab, waterTileWorldPos, Quaternion.identity);

            Zone zone = waterTile.AddComponent<Zone>();
            zone.zoneType = Zone.ZoneType.Water;
            zone.zoneCategory = Zone.ZoneCategory.Water;

            waterTile.transform.SetParent(cell.transform);
            int sortingOrder = terrainManager != null
                ? terrainManager.CalculateTerrainSortingOrder(x, y, visualSurfaceHeight)
                : -(y * gridManager.width + x + 50000);
            SpriteRenderer sr = waterTile.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = sortingOrder;
            cellComponent.SetCellInstanceSortingOrder(sortingOrder);

            cellComponent.prefabName = waterPrefab.name;
            cellComponent.buildingType = waterPrefab.name;
            cellComponent.buildingSize = 0;
            cellComponent.occupiedBuilding = null;
            cellComponent.secondaryPrefabName = "";

            WaterBodyType cls = waterMap.GetBodyClassificationAt(x, y);
            cellComponent.waterBodyType = cls != WaterBodyType.None ? cls
                : (terrainHeight <= seaLevel ? WaterBodyType.Sea : WaterBodyType.Lake);
            cellComponent.waterBodyId = waterMap.GetWaterBodyId(x, y);
        }

        public void RemoveWater(int x, int y)
        {
            if (waterMap == null || !waterMap.IsValidPosition(x, y) || !IsWaterAt(x, y)) return;

            waterMap.ClearWaterAt(x, y);

            GameObject cell = gridManager.gridArray[x, y];
            CityCell cellComponent = gridManager.GetCell(x, y);

            foreach (Transform child in cell.transform) GameObject.Destroy(child.gameObject);

            cellComponent.zoneType = Zone.ZoneType.Grass;
            cellComponent.waterBodyType = WaterBodyType.None;
            cellComponent.waterBodyId = 0;
            cellComponent.secondaryPrefabName = "";

            GameObject grassPrefab = zoneManager.GetRandomZonePrefab(Zone.ZoneType.Grass);
            Vector2 worldPos = gridManager.GetWorldPosition(x, y);
            GameObject grassTile = GameObject.Instantiate(grassPrefab, worldPos, Quaternion.identity);

            Zone zone = grassTile.AddComponent<Zone>();
            zone.zoneType = Zone.ZoneType.Grass;
            zone.zoneCategory = Zone.ZoneCategory.Grass;

            gridManager.SetTileSortingOrder(grassTile, Zone.ZoneType.Grass);
            OnLandCellHeightCommitted(x, y);
        }

        /// <summary>
        /// Refresh every water cell prefab + water-water cascade cliffs.
        /// Guardrail #6: RefreshShoreTerrainAfterWaterUpdate ordering preserved verbatim.
        /// </summary>
        public void UpdateWaterVisuals(bool expandShoreRefreshSecondRing = false, bool skipMultiBodySurfacePasses = false)
        {
            if (waterMap == null || gridManager == null) return;

            if (terrainManager == null) terrainManager = FindObjectOfType<TerrainManager>();
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
                for (int y = 0; y < gridManager.height; y++)
                    if (waterMap.IsWater(x, y)) PlaceWater(x, y);

            SyncAllOpenWaterCellsBodyIdsFromMap();

            if (terrainManager == null) terrainManager = FindObjectOfType<TerrainManager>();
            if (terrainManager != null)
                terrainManager.RefreshWaterCascadeCliffs(this);

            // Guardrail #6: RefreshShoreTerrainAfterWaterUpdate
            if (terrainManager != null && (useLakeDepressionFill || junctionMerged || lakeRiverFallback || expandShoreRefreshSecondRing))
            {
                terrainManager.RefreshShoreTerrainAfterWaterUpdate(this, expandSecondChebyshevRing: expandShoreRefreshSecondRing || junctionMerged || lakeRiverFallback);
                if (lakeRiverFallback && lakeRiverRimCells != null && lakeRiverRimCells.Count > 0)
                    ReapplyLakeRiverFallbackRimTerrain(lakeRiverRimCells, hm);
            }
        }

        // ─── Lake-river fallback rim terrain reapplication (private — refs TerrainManager) ─────

        private void ReapplyLakeRiverFallbackRimTerrain(List<(int x, int y, int lakeSurface)> lakeRiverRimCells, HeightMap hm)
        {
            if (lakeRiverRimCells == null || lakeRiverRimCells.Count == 0 || hm == null || terrainManager == null || gridManager == null)
                return;
            foreach (var (x, y, sLake) in lakeRiverRimCells)
            {
                if (!hm.IsValidPosition(x, y)) continue;
                int clamped = Mathf.Clamp(sLake, TerrainManager.MIN_HEIGHT, TerrainManager.MAX_HEIGHT);
                hm.SetHeight(x, y, clamped);
                gridManager.SetCellHeight(new Vector2(x, y), hm.GetHeight(x, y));
                terrainManager.RestoreTerrainForCell(x, y, hm);
                CityCell cell = gridManager.GetCell(x, y);
                if (cell != null)
                {
                    cell.zoneType = Zone.ZoneType.Grass;
                    cell.waterBodyType = WaterBodyType.None;
                    cell.waterBodyId = 0;
                }
            }
        }
    }
}
