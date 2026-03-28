using UnityEngine;
using Territory.Core;

namespace Territory.Terrain
{
    /// <summary>
    /// Shore membership: <see cref="Cell.waterBodyId"/> sync with <see cref="WaterMap"/> and dry shoreline affiliation.
    /// </summary>
    public partial class WaterManager
    {
        /// <summary>
        /// Runtime query for open water vs dry shore/rim membership and logical surface height.
        /// </summary>
        public struct CellWaterContext
        {
            public bool IsOpenWater;
            public bool HasWaterBodyMembership;
            public int WaterBodyId;
            /// <summary>Logical surface <c>S</c> from <see cref="WaterBody.SurfaceHeight"/>, or -1 if none.</summary>
            public int SurfaceHeight;
            public WaterBodyType Classification;
        }

        /// <summary>
        /// Returns <see cref="Cell.waterBodyId"/> when set; for open water matches <see cref="WaterMap"/>.
        /// </summary>
        public int GetCellWaterBodyId(int x, int y)
        {
            if (gridManager == null)
                return 0;
            Cell c = gridManager.GetCell(x, y);
            return c != null ? c.waterBodyId : 0;
        }

        /// <summary>
        /// Logical surface height <c>S</c> for the cell&apos;s water body, or -1 if none.
        /// </summary>
        public int TryGetCellWaterSurfaceHeight(int x, int y)
        {
            int id = GetCellWaterBodyId(x, y);
            if (id == 0 || waterMap == null)
                return -1;
            WaterBody b = waterMap.GetWaterBody(id);
            return b != null ? b.SurfaceHeight : -1;
        }

        /// <summary>
        /// Full water/shore context for gameplay and debug.
        /// </summary>
        public CellWaterContext GetCellWaterContext(int x, int y)
        {
            var ctx = new CellWaterContext
            {
                IsOpenWater = false,
                HasWaterBodyMembership = false,
                WaterBodyId = 0,
                SurfaceHeight = -1,
                Classification = WaterBodyType.None
            };
            if (waterMap == null || gridManager == null || !waterMap.IsValidPosition(x, y))
                return ctx;
            Cell cell = gridManager.GetCell(x, y);
            if (cell == null)
                return ctx;
            int id = cell.waterBodyId;
            ctx.WaterBodyId = id;
            if (waterMap.IsWater(x, y))
            {
                ctx.IsOpenWater = true;
                ctx.HasWaterBodyMembership = id != 0;
                ctx.SurfaceHeight = waterMap.GetSurfaceHeightAt(x, y);
                ctx.Classification = waterMap.GetBodyClassificationAt(x, y);
                return ctx;
            }
            if (id == 0)
                return ctx;
            ctx.HasWaterBodyMembership = true;
            WaterBody b = waterMap.GetWaterBody(id);
            if (b != null)
            {
                ctx.SurfaceHeight = b.SurfaceHeight;
                ctx.Classification = b.Classification;
            }
            return ctx;
        }

        /// <summary>
        /// Among 8-neighbors with registered water: highest logical surface <c>S</c>, then lowest body id on tie.
        /// For open water cells, returns that cell&apos;s body id.
        /// </summary>
        public int ResolveWinningWaterBodyIdForLandCell(int x, int y)
        {
            if (waterMap == null || !waterMap.IsValidPosition(x, y))
                return 0;
            if (waterMap.IsWater(x, y))
                return waterMap.GetWaterBodyId(x, y);
            int maxS = int.MinValue;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                        continue;
                    int nx = x + dx;
                    int ny = y + dy;
                    if (!waterMap.IsValidPosition(nx, ny) || !waterMap.IsWater(nx, ny))
                        continue;
                    int s = waterMap.GetSurfaceHeightAt(nx, ny);
                    if (s > maxS)
                        maxS = s;
                }
            }
            if (maxS == int.MinValue)
                return 0;
            int bestId = int.MaxValue;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                        continue;
                    int nx = x + dx;
                    int ny = y + dy;
                    if (!waterMap.IsValidPosition(nx, ny) || !waterMap.IsWater(nx, ny))
                        continue;
                    if (waterMap.GetSurfaceHeightAt(nx, ny) != maxS)
                        continue;
                    int bid = waterMap.GetWaterBodyId(nx, ny);
                    if (bid < bestId)
                        bestId = bid;
                }
            }
            return bestId == int.MaxValue ? 0 : bestId;
        }

        /// <summary>
        /// Sets <see cref="Cell.waterBodyId"/> from <see cref="WaterMap"/> for registered water.
        /// </summary>
        public void SyncOpenWaterCellBodyIdAt(int x, int y)
        {
            if (waterMap == null || gridManager == null || !waterMap.IsValidPosition(x, y))
                return;
            Cell c = gridManager.GetCell(x, y);
            if (c == null)
                return;
            if (waterMap.IsWater(x, y))
                c.waterBodyId = waterMap.GetWaterBodyId(x, y);
        }

        /// <summary>
        /// Ensures every open-water cell has <see cref="Cell.waterBodyId"/> matching the map.
        /// </summary>
        public void SyncAllOpenWaterCellsBodyIdsFromMap()
        {
            if (waterMap == null || gridManager == null)
                return;
            for (int x = 0; x < gridManager.width; x++)
            {
                for (int y = 0; y < gridManager.height; y++)
                {
                    if (waterMap.IsWater(x, y))
                        SyncOpenWaterCellBodyIdAt(x, y);
                }
            }
        }

        /// <summary>
        /// Updates dry shore/rim membership or syncs open water; clears id on island (terrain above body <c>S</c>).
        /// </summary>
        public void ApplyShoreMembershipForLandCell(int x, int y)
        {
            if (waterMap == null || gridManager == null || !waterMap.IsValidPosition(x, y))
                return;
            if (terrainManager == null)
                terrainManager = FindObjectOfType<TerrainManager>();
            Cell cell = gridManager.GetCell(x, y);
            if (cell == null)
                return;
            if (waterMap.IsWater(x, y))
            {
                SyncOpenWaterCellBodyIdAt(x, y);
                return;
            }
            int win = ResolveWinningWaterBodyIdForLandCell(x, y);
            int h = terrainManager != null && terrainManager.GetHeightMap() != null
                ? terrainManager.GetHeightMap().GetHeight(x, y)
                : cell.GetCellInstanceHeight();
            if (win != 0)
            {
                WaterBody wb = waterMap.GetWaterBody(win);
                if (wb != null && h > wb.SurfaceHeight)
                {
                    cell.waterBodyId = 0;
                    return;
                }
            }
            if (win == 0)
            {
                cell.waterBodyId = 0;
                return;
            }
            if (terrainManager == null || !terrainManager.IsDryShoreOrRimMembershipEligible(x, y))
            {
                cell.waterBodyId = 0;
                return;
            }
            cell.waterBodyId = win;
        }

        /// <summary>
        /// After height changes: refresh membership for this cell and Moore neighbors.
        /// </summary>
        public void OnLandCellHeightCommitted(int x, int y)
        {
            if (waterMap == null || gridManager == null)
                return;
            ApplyShoreMembershipForLandCell(x, y);
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                        continue;
                    int nx = x + dx;
                    int ny = y + dy;
                    if (!waterMap.IsValidPosition(nx, ny))
                        continue;
                    ApplyShoreMembershipForLandCell(nx, ny);
                }
            }
        }

        /// <summary>
        /// Neighbor counts for <see cref="TerrainManager.DetermineWaterShorePrefabs"/> when filtering by winning owner.
        /// Registered water must match <paramref name="ownerBodyId"/>; sea-level terrain without a map entry matches a sea body at <see cref="seaLevel"/>.
        /// </summary>
        public bool NeighborMatchesShoreOwnerForPattern(int nx, int ny, int ownerBodyId)
        {
            if (waterMap == null || ownerBodyId == 0 || !waterMap.IsValidPosition(nx, ny))
                return false;
            if (terrainManager == null)
                terrainManager = FindObjectOfType<TerrainManager>();
            if (terrainManager == null || terrainManager.GetHeightMap() == null)
                return false;
            if (waterMap.IsWater(nx, ny))
                return waterMap.GetWaterBodyId(nx, ny) == ownerBodyId;
            int nh = terrainManager.GetHeightMap().GetHeight(nx, ny);
            if (nh != TerrainManager.SEA_LEVEL)
                return false;
            WaterBody b = waterMap.GetWaterBody(ownerBodyId);
            if (b == null)
                return false;
            if (b.Classification == WaterBodyType.Sea && b.SurfaceHeight == seaLevel)
                return true;
            return false;
        }

        /// <summary>
        /// After load: sync open water ids; recompute dry shore membership for legacy saves.
        /// </summary>
        public void MigrateWaterBodyIdsAfterGridRestore()
        {
            if (waterMap == null || gridManager == null)
                return;
            if (terrainManager == null)
                terrainManager = FindObjectOfType<TerrainManager>();
            SyncAllOpenWaterCellsBodyIdsFromMap();
            for (int x = 0; x < gridManager.width; x++)
            {
                for (int y = 0; y < gridManager.height; y++)
                {
                    if (!waterMap.IsWater(x, y))
                        ApplyShoreMembershipForLandCell(x, y);
                }
            }
        }
    }
}
