using System.Collections.Generic;
using UnityEngine;
using Territory.Core;
using Territory.Forests;
using Territory.Terrain;
using Domains.Grid;
using Domains.Water;
using Domains.Economy;
using Domains.Terrain;

namespace Domains.Forests.Services
{
    /// <summary>
    /// POCO service extracted from ForestManager (Stage 5.0 Tier-C NO-PORT).
    /// Stateless logic layer: placement guards, economy checks, statistics, sorting, chunk-gen matrix.
    /// Hub (ForestManager) owns ForestMap and delegates to this service via WireDependencies.
    /// Invariants #3 (no per-frame FindObjectOfType), #4 (no new singletons) observed.
    /// </summary>
    public class ForestService
    {
        // ── Wired dependencies ──────────────────────────────────────────────────────
        private IGrid    _grid;
        private IWater   _water;
        private IEconomy _economy;
        private ITerrain _terrain;
        private int _gridWidth;
        private int _gridHeight;

        // ── Init constants ──────────────────────────────────────────────────────────
        private const int   ForestChunkSize            = 8;
        private const float ForestChunkProbability     = 0.4f;
        private const float ForestCellInChunkProbability = 0.5f;
        private const int   FOREST_SORTING_OFFSET      = 5;
        // Mirror of SEA_LEVEL (Territory.Buildings asmdef cannot be referenced from Domains.Forests).
        private const int   SEA_LEVEL                  = 0;

        // ── Setup ───────────────────────────────────────────────────────────────────

        /// <summary>Wire domain dependencies + grid dimensions. Call from hub Start after FindObjectOfType pass.</summary>
        public void WireDependencies(IGrid grid, IWater water, IEconomy economy, ITerrain terrain, int gridWidth, int gridHeight)
        {
            _grid      = grid;
            _water     = water;
            _economy   = economy;
            _terrain   = terrain;
            _gridWidth  = gridWidth;
            _gridHeight = gridHeight;
        }

        // ── Initial forest generation ───────────────────────────────────────────────

        /// <summary>
        /// Build initial forest int[,] matrix by chunk-based placement. 0=None,1=Sparse,2=Medium,3=Dense.
        /// Hub calls ForestMap.InitializeFromIntMatrix on the returned value.
        /// Uses IWater + ITerrain for dry-land classification; IGrid for cell access.
        /// </summary>
        public int[,] BuildInitialForestCells(int width, int height)
        {
            int[,] cells = new int[height, width];
            HeightMap hm = _terrain?.GetOrCreateHeightMap();

            for (int by = 0; by < height; by += ForestChunkSize)
            {
                for (int bx = 0; bx < width; bx += ForestChunkSize)
                {
                    bool hasLand = false;
                    for (int oy = 0; oy < ForestChunkSize && by + oy < height && !hasLand; oy++)
                        for (int ox = 0; ox < ForestChunkSize && bx + ox < width; ox++)
                            if (IsDryLandSeed(bx + ox, by + oy, hm)) { hasLand = true; break; }

                    if (!hasLand || Random.value >= ForestChunkProbability) continue;

                    for (int oy = 0; oy < ForestChunkSize && by + oy < height; oy++)
                    {
                        for (int ox = 0; ox < ForestChunkSize && bx + ox < width; ox++)
                        {
                            int x = bx + ox, y = by + oy;
                            if (!IsDryLandSeed(x, y, hm)) continue;
                            CityCell cell = _grid?.GetCell(x, y);
                            if (!CanPlaceOnCell(cell)) continue;
                            if (Random.value >= ForestCellInChunkProbability) continue;
                            float t = Random.value;
                            cells[y, x] = t < 0.33f ? 1 : (t < 0.66f ? 2 : 3);
                        }
                    }
                }
            }

            return cells;
        }

        // ── Placement guards ────────────────────────────────────────────────────────

        /// <summary>True if cell is placeable for forest (zone/road/building checks). Does NOT check ForestMap — hub handles that.</summary>
        public bool CanPlaceOnCell(CityCell cell)
        {
            if (cell == null) return false;
            if (cell.zoneType == Territory.Zones.Zone.ZoneType.Road) return false;
            if (cell.isInterstate) return false;
            if (cell.occupiedBuilding != null) return false;
            return !IsZoneTypeBlockingForest(cell.zoneType);
        }

        /// <summary>True → land cell orthogonally adjacent to logical water. Uses IWater.</summary>
        public bool IsRiverOrCoastEdge(int x, int y)
        {
            if (_water == null) return false;
            if (_water.IsWaterAt(x, y)) return true;
            int[] dx = { -1, 1, 0, 0 }, dy = { 0, 0, -1, 1 };
            for (int i = 0; i < 4; i++)
            {
                int nx = x + dx[i], ny = y + dy[i];
                if (nx >= 0 && nx < _gridWidth && ny >= 0 && ny < _gridHeight && _water.IsWaterAt(nx, ny))
                    return true;
            }
            return false;
        }

        /// <summary>True if zone type blocks forest placement.</summary>
        public bool IsZoneTypeBlockingForest(Territory.Zones.Zone.ZoneType zoneType)
        {
            switch (zoneType)
            {
                case Territory.Zones.Zone.ZoneType.Road:
                case Territory.Zones.Zone.ZoneType.Water:
                case Territory.Zones.Zone.ZoneType.Building:
                case Territory.Zones.Zone.ZoneType.ResidentialLightBuilding:
                case Territory.Zones.Zone.ZoneType.ResidentialMediumBuilding:
                case Territory.Zones.Zone.ZoneType.ResidentialHeavyBuilding:
                case Territory.Zones.Zone.ZoneType.CommercialLightBuilding:
                case Territory.Zones.Zone.ZoneType.CommercialMediumBuilding:
                case Territory.Zones.Zone.ZoneType.CommercialHeavyBuilding:
                case Territory.Zones.Zone.ZoneType.IndustrialLightBuilding:
                case Territory.Zones.Zone.ZoneType.IndustrialMediumBuilding:
                case Territory.Zones.Zone.ZoneType.IndustrialHeavyBuilding:
                    return true;
                default:
                    return false;
            }
        }

        // ── Economy / resource checks ───────────────────────────────────────────────

        /// <summary>True if city can afford forest placement cost.</summary>
        public bool CanAffordForest(int cost)
        {
            if (_economy == null || cost <= 0) return true;
            return _economy.CanAfford(cost);
        }

        /// <summary>True if city has enough water capacity for the given consumption.</summary>
        public bool HasSufficientWaterForForest(int waterConsumption)
        {
            if (_economy == null || waterConsumption <= 0) return true;
            return (_economy.GetTotalWaterConsumption() + waterConsumption) <= _economy.GetTotalWaterOutput();
        }

        // ── Forest type data ────────────────────────────────────────────────────────

        /// <summary>Water consumption for forest density type.</summary>
        public int GetWaterConsumptionForForestType(Forest.ForestType ft)
        { switch (ft) { case Forest.ForestType.Sparse: return 2; case Forest.ForestType.Medium: return 3; case Forest.ForestType.Dense: return 5; default: return 0; } }

        /// <summary>Construction cost for forest type (currently 0).</summary>
        public int GetConstructionCostForForestType(Forest.ForestType ft) => 0;

        // ── Sorting order ───────────────────────────────────────────────────────────

        /// <summary>Forest sprite sorting order at (x,y) with given cell height.</summary>
        public int GetForestSortingOrder(int x, int y, int cellHeight)
        {
            if (_terrain != null) return _terrain.CalculateTerrainSortingOrder(x, y, cellHeight) + FOREST_SORTING_OFFSET;
            return -(y * 10 + x) - (cellHeight * 100) - 50;
        }

        // ── Terrain helpers ─────────────────────────────────────────────────────────

        /// <summary>Prefer ITerrain.GetOrCreateHeightMap; returns null if no terrain wired.</summary>
        public HeightMap GetTerrainHeightMap() => _terrain?.GetOrCreateHeightMap();

        // ── Dry-land seed check ─────────────────────────────────────────────────────

        private bool IsDryLandSeed(int x, int y, HeightMap hm)
        {
            if (_water != null && _water.IsWaterAt(x, y)) return false;
            if (hm != null && hm.IsValidPosition(x, y)) return hm.GetHeight(x, y) > SEA_LEVEL;
            CityCell c = _grid?.GetCell(x, y);
            return c != null && c.height > SEA_LEVEL;
        }
    }
}
