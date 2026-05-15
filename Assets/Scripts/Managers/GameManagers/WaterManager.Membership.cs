// long-file-allowed: legacy hub — scope outside current atomization plan; deferred to future sweep
using UnityEngine;
using Territory.Core;

namespace Territory.Terrain
{
    /// <summary>
    /// Shore membership: <see cref="CityCell.waterBodyId"/> sync with <see cref="WaterMap"/> + dry shoreline affiliation.
    /// </summary>
    public partial class WaterManager
    {
        /// <summary>
        /// Runtime query → open water vs dry shore/rim membership + logical surface height.
        /// </summary>
        public struct CellWaterContext
        {
            public bool IsOpenWater;
            public bool HasWaterBodyMembership;
            public int WaterBodyId;
            /// <summary>Logical surface <c>S</c> from <see cref="WaterBody.SurfaceHeight"/>; -1 if none.</summary>
            public int SurfaceHeight;
            public WaterBodyType Classification;
        }

        /// <summary>
        /// Return <see cref="CityCell.waterBodyId"/> when set; open water matches <see cref="WaterMap"/>.
        /// </summary>
        public int GetCellWaterBodyId(int x, int y)
        {
            if (gridManager == null)
                return 0;
            CityCell c = gridManager.GetCell(x, y);
            return c != null ? c.waterBodyId : 0;
        }

        /// <summary>
        /// Logical surface height <c>S</c> for cell&apos;s water body; -1 if none.
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
        /// Full water/shore context for gameplay + debug.
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
            CityCell cell = gridManager.GetCell(x, y);
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
        /// Among Moore neighbors with registered water, pick body with <b>lowest</b> logical surface <c>S</c>
        /// (beach of that pool when multiple surfaces meet). Tie → lowest body id. Open water returns this cell&apos;s id.
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
        /// Resolve dry-land <see cref="CityCell.waterBodyId"/> via river–river junction brinks (§12.8) when applicable,
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
        /// True → dry <paramref name="x"/>,<paramref name="y"/> classified as upper-pool brink at river–river cascade (§12.8).
        /// </summary>
        public bool IsDryLandUpperRiverJunctionBrink(int x, int y)
        {
            if (waterMap == null || !waterMap.IsValidPosition(x, y))
                return false;
            return waterMap.TryGetDryLandRiverJunctionBrink(x, y, out RiverJunctionBrinkRole role, out _)
                && role == RiverJunctionBrinkRole.UpperBrink;
        }

        /// <summary>
        /// True → dry land is <see cref="RiverJunctionBrinkRole.LowerBrink"/> at river–river cascade (§12.8).
        /// </summary>
        public bool IsDryLandLowerRiverJunctionBrink(int x, int y)
        {
            if (waterMap == null || !waterMap.IsValidPosition(x, y))
                return false;
            return waterMap.TryGetDryLandRiverJunctionBrink(x, y, out RiverJunctionBrinkRole role, out _)
                && role == RiverJunctionBrinkRole.LowerBrink;
        }

        /// <summary>
        /// True → <see cref="WaterMap.TryGetDryLandRiverJunctionBrink"/> returns upper/lower brink <b>and</b> this cell sole
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
        /// Shore affiliation: <see cref="CityCell.waterBodyId"/> when set on dry land; otherwise
        /// <see cref="ComputeShoreAffiliationForDryLandCell"/>. Open water → map body id.
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
        /// Set <see cref="CityCell.waterBodyId"/> from <see cref="WaterMap"/> for registered water.
        /// </summary>
        public void SyncOpenWaterCellBodyIdAt(int x, int y)
        {
            if (waterMap == null || gridManager == null || !waterMap.IsValidPosition(x, y))
                return;
            CityCell c = gridManager.GetCell(x, y);
            if (c == null)
                return;
            if (waterMap.IsWater(x, y))
                c.waterBodyId = waterMap.GetWaterBodyId(x, y);
        }

        /// <summary>
        /// Ensure every open-water cell has <see cref="CityCell.waterBodyId"/> matching map.
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
        /// Update dry shore/rim membership or sync open water; clear id on island (terrain above body <c>S</c>).
        /// </summary>
        public void ApplyShoreMembershipForLandCell(int x, int y)
        {
            if (waterMap == null || gridManager == null || !waterMap.IsValidPosition(x, y))
                return;
            if (terrainManager == null)
                terrainManager = FindObjectOfType<TerrainManager>();
            CityCell cell = gridManager.GetCell(x, y);
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
        /// After height changes → refresh membership for this cell + Moore neighbors.
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
        /// Topological &quot;water&quot; for <see cref="TerrainManager.DetermineWaterShorePrefabs"/> Moore masks when shore cell has affiliated body.
        /// Registered water must match <paramref name="ownerBodyId"/>; sea-level terrain without map entry matches sea body at <see cref="seaLevel"/>.
        /// Dry junction-brink land <b>not</b> treated as water here — diagonal <c>*SlopeWaterPrefab</c> at river–river cascades driven by
        /// <see cref="ShouldForceDiagonalSlopeWaterAtRiverJunctionBrink"/> on shore cell (isometric spec §12.8.1).
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
            {
                int neighborId = waterMap.GetWaterBodyId(nx, ny);
                if (neighborId == ownerBodyId)
                    return true;
                // Same horizontal water plane: a Lake/River/Sea body adjacent to the shore
                // cell's affiliated body at the IDENTICAL SurfaceHeight renders as one
                // continuous water surface. Shore-prefab decisions must see them as one
                // mass to pick corner-slope variants at body junctions (e.g. lake meets
                // sea at sea level — without this, the single-body mask drops the off-body
                // cardinal and the algorithm falls into the wrong single-cardinal branch).
                // Different-surface cases (cascade / waterfall §12.7-§12.8) excluded
                // — those are handled by IsMultiSurfacePerpendicularWaterCorner upstream.
                WaterBody owner = waterMap.GetWaterBody(ownerBodyId);
                WaterBody neighbor = waterMap.GetWaterBody(neighborId);
                if (owner != null && neighbor != null && owner.SurfaceHeight == neighbor.SurfaceHeight)
                    return true;
                return false;
            }
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
        /// Extended neighbor &quot;wet&quot; mask for junction cascade shore post-pass only: same as <see cref="IsOpenWaterForShoreTopology"/>
        /// + dry <see cref="WaterMap.TryGetDryLandRiverJunctionBrink"/> cells whose affiliation or river–river step matches shore owner (§12.8.1).
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
