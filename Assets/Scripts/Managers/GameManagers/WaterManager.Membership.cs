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
        /// Among Moore neighbors with registered water, picks the body with the <b>lowest</b> logical surface <c>S</c>
        /// (beach of that pool when multiple surfaces meet). Tie: lowest body id. Open water returns this cell&apos;s id.
        /// </summary>
        public int ComputeShoreAffiliationFromLowestLogicalSurfaceAmongMooreWater(int x, int y)
        {
            if (waterMap == null || !waterMap.IsValidPosition(x, y))
                return 0;
            if (waterMap.IsWater(x, y))
                return waterMap.GetWaterBodyId(x, y);
            int minS = int.MaxValue;
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
                    if (s >= 0 && s < minS)
                        minS = s;
                }
            }
            if (minS == int.MaxValue)
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
                    if (waterMap.GetSurfaceHeightAt(nx, ny) != minS)
                        continue;
                    int bid = waterMap.GetWaterBodyId(nx, ny);
                    if (bid < bestId)
                        bestId = bid;
                }
            }
            return bestId == int.MaxValue ? 0 : bestId;
        }

        /// <summary>
        /// Resolves dry-land <see cref="Cell.waterBodyId"/> using river–river junction brinks (§12.8) when applicable,
        /// else <see cref="ComputeShoreAffiliationFromLowestLogicalSurfaceAmongMooreWater"/>.
        /// </summary>
        public int ComputeShoreAffiliationForDryLandCell(int x, int y)
        {
            if (waterMap == null || !waterMap.IsValidPosition(x, y))
                return 0;
            if (waterMap.IsWater(x, y))
                return waterMap.GetWaterBodyId(x, y);

            if (waterMap.TryGetDryLandRiverJunctionBrink(x, y, out RiverJunctionBrinkRole role, out int affId))
            {
                if (role == RiverJunctionBrinkRole.UpperBrink && affId != 0)
                    return affId;
                if (role == RiverJunctionBrinkRole.LowerBrink && affId != 0)
                    return affId;
            }
            return ComputeShoreAffiliationFromLowestLogicalSurfaceAmongMooreWater(x, y);
        }

        /// <summary>
        /// True when dry <paramref name="x"/>,<paramref name="y"/> is classified as an upper-pool brink at a river–river cascade (§12.8).
        /// </summary>
        public bool IsDryLandUpperRiverJunctionBrink(int x, int y)
        {
            if (waterMap == null || !waterMap.IsValidPosition(x, y))
                return false;
            return waterMap.TryGetDryLandRiverJunctionBrink(x, y, out RiverJunctionBrinkRole role, out _)
                && role == RiverJunctionBrinkRole.UpperBrink;
        }

        /// <summary>
        /// True when dry land is a <see cref="RiverJunctionBrinkRole.LowerBrink"/> at a river–river cascade (§12.8).
        /// </summary>
        public bool IsDryLandLowerRiverJunctionBrink(int x, int y)
        {
            if (waterMap == null || !waterMap.IsValidPosition(x, y))
                return false;
            return waterMap.TryGetDryLandRiverJunctionBrink(x, y, out RiverJunctionBrinkRole role, out _)
                && role == RiverJunctionBrinkRole.LowerBrink;
        }

        /// <summary>
        /// True when <see cref="WaterMap.TryGetDryLandRiverJunctionBrink"/> returns upper or lower brink <b>and</b> this cell is the sole
        /// closest-to-junction shore in its cardinal land component for that river–river step (<see cref="WaterMap.IsDryLandRiverJunctionBrinkClosestToCascadeStep"/>).
        /// Diagonal <c>*SlopeWaterPrefab</c> over Bay applies only on that one tile per shore strip (§12.8).
        /// </summary>
        public bool ShouldForceDiagonalSlopeWaterAtRiverJunctionBrink(int x, int y)
        {
            if (waterMap == null || !waterMap.IsValidPosition(x, y))
                return false;
            if (!waterMap.TryGetDryLandRiverJunctionBrinkWithStep(x, y, out RiverJunctionBrinkRole role, out _, out int hx, out int hy, out int lx, out int ly))
                return false;
            if (role != RiverJunctionBrinkRole.UpperBrink && role != RiverJunctionBrinkRole.LowerBrink)
                return false;
            return waterMap.IsDryLandRiverJunctionBrinkClosestToCascadeStep(x, y, role, hx, hy, lx, ly);
        }

        /// <summary>
        /// Shore affiliation: returns <see cref="Cell.waterBodyId"/> when set on dry land; otherwise
        /// <see cref="ComputeShoreAffiliationForDryLandCell"/>. Open water returns map body id.
        /// </summary>
        public int GetShoreAffiliatedWaterBodyIdForLandCell(int x, int y)
        {
            if (waterMap == null || !waterMap.IsValidPosition(x, y))
                return 0;
            if (waterMap.IsWater(x, y))
                return waterMap.GetWaterBodyId(x, y);
            int id = GetCellWaterBodyId(x, y);
            if (id != 0)
                return id;
            return ComputeShoreAffiliationForDryLandCell(x, y);
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
            int win = ComputeShoreAffiliationForDryLandCell(x, y);
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
        /// Topological &quot;water&quot; for <see cref="TerrainManager.DetermineWaterShorePrefabs"/> Moore masks when the shore cell has an affiliated body.
        /// Registered water must match <paramref name="ownerBodyId"/>; sea-level terrain without a map entry matches a sea body at <see cref="seaLevel"/>.
        /// Dry junction-brink land is <b>not</b> treated as water here — diagonal <c>*SlopeWaterPrefab</c> at river–river cascades is driven by
        /// <see cref="ShouldForceDiagonalSlopeWaterAtRiverJunctionBrink"/> on the shore cell (isometric spec §12.8.1).
        /// </summary>
        public bool IsOpenWaterForShoreTopology(int nx, int ny, int ownerBodyId)
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
        /// Extended neighbor &quot;wet&quot; mask for junction cascade shore post-pass only: same as <see cref="IsOpenWaterForShoreTopology"/>,
        /// plus dry <see cref="WaterMap.TryGetDryLandRiverJunctionBrink"/> cells whose affiliation or river–river step matches the shore owner (§12.8.1).
        /// </summary>
        public bool NeighborMatchesShoreOwnerForJunctionTopology(int nx, int ny, int ownerBodyId)
        {
            if (IsOpenWaterForShoreTopology(nx, ny, ownerBodyId))
                return true;
            if (waterMap == null || ownerBodyId == 0 || !waterMap.IsValidPosition(nx, ny))
                return false;
            if (waterMap.IsWater(nx, ny))
                return false;
            if (waterMap.TryGetDryLandRiverJunctionBrink(nx, ny, out _, out int aff))
            {
                if (aff == ownerBodyId)
                    return true;
                if (waterMap.TryFindRiverRiverSurfaceStepBetweenBodiesNear(nx, ny, ownerBodyId, aff, 2))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// After grid restore: sync open water body ids; recompute dry shore membership for all land cells.
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
