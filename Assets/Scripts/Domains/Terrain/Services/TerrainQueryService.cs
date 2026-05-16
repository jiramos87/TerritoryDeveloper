using System.Collections.Generic;
using UnityEngine;
using Territory.Core;
using Territory.Terrain;
using Territory.Zones;

namespace Domains.Terrain.Services
{
    /// <summary>
    /// Read-only terrain queries extracted from TerrainManager.
    /// No heightMap writes. Delegates read access via constructor funcs.
    /// Invariant #1: no Cell.height assignment in this service.
    /// </summary>
    public class TerrainQueryService
    {
        private readonly System.Func<HeightMap> _getHeightMap;
        private readonly System.Func<int, int, bool> _isWaterAt;
        private readonly System.Func<int, int, bool> _isWaterSlopeCellInternal;
        private readonly System.Func<int, int, bool> _isDryShoreOrRimMembershipEligibleInternal;
        private readonly System.Func<int, int, bool> _isAdjacentToWaterHeight;
        private readonly System.Func<int, int, bool> _isTerrainPlaceableForBuilding;
        private readonly System.Func<int, int, bool> _isCellOccupiedByBuilding;
        private readonly System.Func<int, int, CityCell> _getCell;
        private readonly System.Func<int, int, Territory.Terrain.TerrainSlopeType> _getTerrainSlopeTypeAt;
        private readonly System.Func<Vector2, int, int, int, bool> _getBuildingFootprintOffsetCallback;
        private readonly System.Func<int, int, int, bool> _isBuildingFootprintOffset;

        // Prefab set — injected so this POCO needs no MonoBehaviour refs.
        private readonly IList<GameObject> _allTerrainPrefabs;
        private readonly IList<GameObject> _waterSlopePrefabs;
        private readonly IList<GameObject> _landSlopePrefabs;
        private readonly IList<GameObject> _seaLevelWaterPrefabs;
        private readonly IList<GameObject> _shoreBayPrefabs;
        private readonly IList<GameObject> _cliffStackPrefabs;

        /// <summary>Construct terrain query service with dependencies.</summary>
        public TerrainQueryService(
            System.Func<HeightMap> getHeightMap,
            System.Func<int, int, bool> isWaterAt,
            System.Func<int, int, bool> isWaterSlopeCellInternal,
            System.Func<int, int, bool> isDryShoreOrRimMembershipEligibleInternal,
            System.Func<int, int, bool> isAdjacentToWaterHeight,
            System.Func<int, int, bool> isTerrainPlaceableForBuilding,
            System.Func<int, int, bool> isCellOccupiedByBuilding,
            System.Func<int, int, CityCell> getCell,
            System.Func<int, int, Territory.Terrain.TerrainSlopeType> getTerrainSlopeTypeAt,
            IList<GameObject> allTerrainPrefabs = null,
            IList<GameObject> waterSlopePrefabs = null,
            IList<GameObject> landSlopePrefabs = null,
            IList<GameObject> seaLevelWaterPrefabs = null,
            IList<GameObject> shoreBayPrefabs = null,
            IList<GameObject> cliffStackPrefabs = null)
        {
            _getHeightMap = getHeightMap;
            _isWaterAt = isWaterAt;
            _isWaterSlopeCellInternal = isWaterSlopeCellInternal;
            _isDryShoreOrRimMembershipEligibleInternal = isDryShoreOrRimMembershipEligibleInternal;
            _isAdjacentToWaterHeight = isAdjacentToWaterHeight;
            _isTerrainPlaceableForBuilding = isTerrainPlaceableForBuilding;
            _isCellOccupiedByBuilding = isCellOccupiedByBuilding;
            _getCell = getCell;
            _getTerrainSlopeTypeAt = getTerrainSlopeTypeAt;
            _allTerrainPrefabs = allTerrainPrefabs ?? new List<GameObject>();
            _waterSlopePrefabs = waterSlopePrefabs ?? new List<GameObject>();
            _landSlopePrefabs = landSlopePrefabs ?? new List<GameObject>();
            _seaLevelWaterPrefabs = seaLevelWaterPrefabs ?? new List<GameObject>();
            _shoreBayPrefabs = shoreBayPrefabs ?? new List<GameObject>();
            _cliffStackPrefabs = cliffStackPrefabs ?? new List<GameObject>();
        }

        /// <summary>
        /// Find terrain prefab by name. Read-only lookup from injected prefab list.
        /// Verbatim body from TerrainManager.FindTerrainPrefabByName.
        /// </summary>
        public GameObject FindTerrainPrefabByName(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName)) return null;
            string trimmed = prefabName.Replace("(Clone)", "");
            foreach (GameObject prefab in _allTerrainPrefabs)
            {
                if (prefab != null && prefab.name == trimmed)
                    return prefab;
            }
            return null;
        }

        /// <summary>
        /// True when WaterManager registers open water at (x,y).
        /// Verbatim logic from TerrainManager.IsRegisteredOpenWaterAt.
        /// </summary>
        public bool IsRegisteredOpenWaterAt(int x, int y) => _isWaterAt != null && _isWaterAt(x, y);

        /// <summary>
        /// Skip height writes + primary terrain mesh rebuilds for registered open water + water-shore cells.
        /// Verbatim logic from TerrainManager.ShouldSkipRoadTerraformSurfaceAt.
        /// </summary>
        public bool ShouldSkipRoadTerraformSurfaceAt(int x, int y, HeightMap heightMap)
        {
            if (heightMap == null || !heightMap.IsValidPosition(x, y))
                return true;
            if (IsWaterSlopeCell(x, y))
                return true;
            return IsRegisteredOpenWaterAt(x, y);
        }

        /// <summary>True if cell uses water-shore slope prefab.</summary>
        public bool IsWaterSlopeCell(int x, int y) => _isWaterSlopeCellInternal != null && _isWaterSlopeCellInternal(x, y);

        /// <summary>True if cell is eligible for dry-shore or rim membership.</summary>
        public bool IsDryShoreOrRimMembershipEligible(int x, int y) => _isDryShoreOrRimMembershipEligibleInternal != null && _isDryShoreOrRimMembershipEligibleInternal(x, y);

        /// <summary>True if water or sea at neighbor position. Delegates to water query.</summary>
        public bool IsWaterOrSeaAtNeighbor(int nx, int ny) => _isWaterAt != null && _isWaterAt(nx, ny);

        /// <summary>True if GameObject uses a water-slope prefab.</summary>
        public bool IsWaterSlopeObject(GameObject obj)
        {
            if (obj == null) return false;
            string trimmed = obj.name.Replace("(Clone)", "");
            foreach (var prefab in _waterSlopePrefabs)
            {
                if (prefab != null && prefab.name == trimmed)
                    return true;
            }
            return false;
        }

        /// <summary>True if GameObject uses a land-slope prefab.</summary>
        public bool IsLandSlopeObject(GameObject obj)
        {
            if (obj == null) return false;
            string trimmed = obj.name.Replace("(Clone)", "");
            foreach (var prefab in _landSlopePrefabs)
            {
                if (prefab != null && prefab.name == trimmed)
                    return true;
            }
            return false;
        }

        /// <summary>True if GameObject is the sea-level water prefab.</summary>
        public bool IsSeaLevelWaterObject(GameObject obj)
        {
            if (obj == null) return false;
            string trimmed = obj.name.Replace("(Clone)", "");
            foreach (var prefab in _seaLevelWaterPrefabs)
            {
                if (prefab != null && prefab.name == trimmed)
                    return true;
            }
            return false;
        }

        /// <summary>True if GameObject is a shore-bay prefab.</summary>
        public bool IsShoreBayObject(GameObject obj)
        {
            if (obj == null) return false;
            string trimmed = obj.name.Replace("(Clone)", "");
            foreach (var prefab in _shoreBayPrefabs)
            {
                if (prefab != null && prefab.name == trimmed)
                    return true;
            }
            return false;
        }

        /// <summary>True if GameObject is a cliff-stack terrain object.</summary>
        public bool IsCliffStackTerrainObject(GameObject obj)
        {
            if (obj == null) return false;
            string trimmed = obj.name.Replace("(Clone)", "");
            foreach (var prefab in _cliffStackPrefabs)
            {
                if (prefab != null && prefab.name == trimmed)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Check whether building can be placed at grid position per terrain constraints.
        /// Verbatim logic from TerrainManager.CanPlaceBuildingInTerrain.
        /// Read-only: no heightMap writes.
        /// </summary>
        public bool CanPlaceBuildingInTerrain(HeightMap heightMap, int x, int y, int size, out string failReason,
            bool allowCoastalSlope = false, bool allowWaterInFootprint = false)
        {
            failReason = null;
            int offsetX = size % 2 == 0 ? 0 : size / 2;
            int offsetY = size % 2 == 0 ? 0 : size / 2;

            int? landBaseHeight = null;
            if (!allowWaterInFootprint)
                landBaseHeight = heightMap.GetHeight(x, y);

            for (int dx = 0; dx < size; dx++)
            {
                for (int dy = 0; dy < size; dy++)
                {
                    int checkX = x + dx - offsetX;
                    int checkY = y + dy - offsetY;

                    if (!heightMap.IsValidPosition(checkX, checkY))
                    {
                        failReason = "Out of bounds.";
                        return false;
                    }

                    bool isWater = _isWaterAt != null && _isWaterAt(checkX, checkY);
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

                    if (_isTerrainPlaceableForBuilding != null && !_isTerrainPlaceableForBuilding(checkX, checkY))
                    {
                        if (!allowCoastalSlope || (_isAdjacentToWaterHeight != null && !_isAdjacentToWaterHeight(checkX, checkY)))
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
        /// True if road can be placed (no water slope, valid slope type).
        /// Verbatim logic from TerrainManager.CanPlaceRoad.
        /// </summary>
        public bool CanPlaceRoad(int x, int y) => CanPlaceRoad(x, y, allowWaterSlopeForWaterBridgeTrace: false);

        /// <summary>
        /// True if road can be placed with optional shore allowance.
        /// Verbatim logic from TerrainManager.CanPlaceRoad(int,int,bool).
        /// </summary>
        public bool CanPlaceRoad(int x, int y, bool allowWaterSlopeForWaterBridgeTrace)
        {
            if (_isCellOccupiedByBuilding != null && _isCellOccupiedByBuilding(x, y))
                return false;
            CityCell c = _getCell?.Invoke(x, y);
            if (c != null && c.GetCellInstanceHeight() == 0)
                return true;
            if (IsWaterSlopeCell(x, y) && !allowWaterSlopeForWaterBridgeTrace)
                return false;
            if (_getTerrainSlopeTypeAt == null) return true;
            Territory.Terrain.TerrainSlopeType slope = _getTerrainSlopeTypeAt(x, y);
            switch (slope)
            {
                case Territory.Terrain.TerrainSlopeType.Flat:
                case Territory.Terrain.TerrainSlopeType.North:
                case Territory.Terrain.TerrainSlopeType.South:
                case Territory.Terrain.TerrainSlopeType.East:
                case Territory.Terrain.TerrainSlopeType.West:
                case Territory.Terrain.TerrainSlopeType.NorthEast:
                case Territory.Terrain.TerrainSlopeType.NorthWest:
                case Territory.Terrain.TerrainSlopeType.SouthEast:
                case Territory.Terrain.TerrainSlopeType.SouthWest:
                case Territory.Terrain.TerrainSlopeType.NorthEastUp:
                case Territory.Terrain.TerrainSlopeType.NorthWestUp:
                case Territory.Terrain.TerrainSlopeType.SouthEastUp:
                case Territory.Terrain.TerrainSlopeType.SouthWestUp:
                    return true;
                default:
                    return false;
            }
        }
    }
}
