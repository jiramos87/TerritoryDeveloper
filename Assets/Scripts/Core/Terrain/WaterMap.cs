using System;
using System.Collections.Generic;
using Territory.Core;
using UnityEngine;

namespace Territory.Terrain
{
    /// <summary>
    /// Dry-land role at river–river cardinal surface step (§12.8). Used for shore affiliation + prefab selection.
    /// </summary>
    public enum RiverJunctionBrinkRole
    {
        None,
        /// <summary>Dry cell on upper-pool side of river–river cascade (Moore-adjacent to high-surface water cell).</summary>
        UpperBrink,
        /// <summary>Dry cell cardinally adjacent to low-surface water cell of river–river step.</summary>
        LowerBrink
    }

    /// <summary>
    /// Water overlay on height map. Each cell has water body id (0 = dry).
    /// Bodies carry shared surface height. Terrain depth implicit (surface &gt; terrain floor).
    /// Large logic bodies live in partial files: WaterMap.LakeGen.cs, WaterMap.JunctionMerge.cs.
    /// </summary>
    public sealed partial class WaterMap
    {
        public const int FormatVersionV2 = 2;

        /// <summary>V3: <see cref="WaterBodySerialized.bodyClassification"/> per body. Loads still accept V2 without it.</summary>
        public const int FormatVersionV3 = 3;

        /// <summary>Neighbor outside grid treated higher than any terrain cell (see <see cref="HeightMap"/> range 0–5).</summary>
        internal const int OutsideMapSpillHeight = 6;

        /// <summary>Reserved id for player-painted water. Does not collide with procedural lake ids 1..N.</summary>
        public const int LegacyPaintWaterBodyId = 10001;

        private int[,] waterBodyIds;
        internal readonly Dictionary<int, WaterBody> bodies = new Dictionary<int, WaterBody>();
        internal int nextBodyId = 1;

        internal int width;
        internal int height;

        /// <summary>First centerline cell per procedural river placed in <see cref="ProceduralRiverGenerator"/> (diagnostics).</summary>
        private readonly List<Vector2Int> proceduralRiverEntryAnchors = new List<Vector2Int>();

        /// <summary>After artificial lake fallback, region in heightmap coords to refresh terrain (inclusive). -1 if none.</summary>
        public int ArtificialDirtyMinX { get; internal set; } = -1;
        public int ArtificialDirtyMinY { get; internal set; } = -1;
        public int ArtificialDirtyMaxX { get; internal set; } = -1;
        public int ArtificialDirtyMaxY { get; internal set; } = -1;

        /// <summary>Populated after <see cref="InitializeLakesFromDepressionFill"/> for console diagnostics.</summary>
        public int LastLakeGenerationTargetBodies { get; internal set; }
        public int LastLakeGenerationScaledBudget { get; internal set; }
        public int LastLakeGenerationFinalBodyCount { get; internal set; }
        public int LastLakeGenerationArtificialBodiesPlaced { get; internal set; }
        public int LastLakeGenerationRecoveryPasses { get; internal set; }
        public bool LastLakeGenerationMetTarget { get; internal set; }
        public int LastLakeGenerationProceduralBodiesAfterBounded { get; internal set; }

        public WaterMap(int width, int height)
        {
            this.width = width;
            this.height = height;
            waterBodyIds = new int[width, height];
        }

        public int Width => width;
        public int Height => height;

        public bool IsValidPosition(int x, int y) =>
            x >= 0 && x < width && y >= 0 && y < height;

        public bool IsWater(int x, int y)
        {
            if (!IsValidPosition(x, y)) return false;
            return waterBodyIds[x, y] != 0;
        }

        public int GetWaterBodyId(int x, int y)
        {
            if (!IsValidPosition(x, y)) return 0;
            return waterBodyIds[x, y];
        }

        /// <summary>Return -1 when cell not water.</summary>
        public int GetSurfaceHeightAt(int x, int y)
        {
            int id = GetWaterBodyId(x, y);
            if (id == 0) return -1;
            return bodies[id].SurfaceHeight;
        }

        public WaterBody GetWaterBody(int bodyId) =>
            bodyId != 0 && bodies.TryGetValue(bodyId, out WaterBody b) ? b : null;

        /// <summary>Classification of body at this cell, or <see cref="WaterBodyType.None"/> when dry.</summary>
        public WaterBodyType GetBodyClassificationAt(int x, int y)
        {
            int id = GetWaterBodyId(x, y);
            if (id == 0) return WaterBodyType.None;
            return bodies.TryGetValue(id, out WaterBody b) ? b.Classification : WaterBodyType.None;
        }

        /// <summary>
        /// True when this <b>water</b> cell on lower side of cardinal water-water surface step (<c>S_high &gt; S_low</c>)
        /// + step not <see cref="IsLakeSurfaceStepContactForbidden"/> (§12.7).
        /// </summary>
        public bool IsWaterCellLowerSideOfCardinalSurfaceStep(int x, int y)
        {
            if (!IsValidPosition(x, y) || !IsWater(x, y)) return false;
            int sL = GetSurfaceHeightAt(x, y);
            if (sL < 0) return false;
            int[] d4x = { 1, -1, 0, 0 };
            int[] d4y = { 0, 0, 1, -1 };
            for (int i = 0; i < 4; i++)
            {
                int ux = x + d4x[i], uy = y + d4y[i];
                if (!IsValidPosition(ux, uy) || !IsWater(ux, uy)) continue;
                int sH = GetSurfaceHeightAt(ux, uy);
                if (sH <= sL) continue;
                if (IsLakeSurfaceStepContactForbidden(ux, uy, x, y)) continue;
                return true;
            }
            return false;
        }

        public bool TryGetDryLandRiverJunctionBrink(int x, int y, out RiverJunctionBrinkRole role, out int affiliatedBodyId) =>
            TryGetDryLandRiverJunctionBrinkWithStep(x, y, out role, out affiliatedBodyId, out _, out _, out _, out _);

        // Implementation lives in WaterMap.JunctionMerge.cs
        public bool TryGetDryLandRiverJunctionBrinkWithStep(
            int x, int y,
            out RiverJunctionBrinkRole role, out int affiliatedBodyId,
            out int highX, out int highY, out int lowX, out int lowY)
            => TryGetDryLandRiverJunctionBrinkWithStepImpl(x, y, out role, out affiliatedBodyId, out highX, out highY, out lowX, out lowY);

        public bool IsDryLandRiverJunctionBrinkClosestToCascadeStep(
            int x, int y, RiverJunctionBrinkRole role, int stepHighX, int stepHighY, int stepLowX, int stepLowY)
            => IsDryLandRiverJunctionBrinkClosestToCascadeStepImpl(x, y, role, stepHighX, stepHighY, stepLowX, stepLowY);

        public bool IsLakeSurfaceStepContactForbidden(int highX, int highY, int lowX, int lowY)
        {
            if (!IsWater(highX, highY) || !IsWater(lowX, lowY)) return false;
            int sH = GetSurfaceHeightAt(highX, highY);
            int sL = GetSurfaceHeightAt(lowX, lowY);
            if (sH <= sL) return false;
            WaterBodyType cH = GetBodyClassificationAt(highX, highY);
            WaterBodyType cL = GetBodyClassificationAt(lowX, lowY);
            return cH == WaterBodyType.Lake || cL == WaterBodyType.Lake;
        }

        public bool TryFindRiverRiverSurfaceStepBetweenBodiesNear(int x, int y, int bodyA, int bodyB, int searchRadius)
            => TryFindRiverRiverSurfaceStepBetweenBodiesNearImpl(x, y, bodyA, bodyB, searchRadius);

        // Bed normalization + junction merge: see WaterMap.JunctionMerge.cs
        public void ApplyMultiBodySurfaceBoundaryNormalization(HeightMap heightMap)
            => ApplyMultiBodySurfaceBoundaryNormalizationImpl(heightMap);

        public bool ApplyWaterSurfaceJunctionMerge(HeightMap heightMap, IGridManager gridManager,
            out int dirtyMinX, out int dirtyMinY, out int dirtyMaxX, out int dirtyMaxY)
            => ApplyWaterSurfaceJunctionMergeImpl(heightMap, gridManager, out dirtyMinX, out dirtyMinY, out dirtyMaxX, out dirtyMaxY);

        public bool ApplyLakeHighToRiverLowContactFallback(HeightMap heightMap, IGridManager gridManager,
            out List<(int x, int y, int lakeSurface)> restoredCells)
        {
            restoredCells = new List<(int, int, int)>();
            if (heightMap == null) return false;
            int[] d4x = { 1, -1, 0, 0 };
            int[] d4y = { 0, 0, 1, -1 };
            var candidates = new List<(int x, int y, int sLake)>();
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    if (!IsWater(x, y)) continue;
                    if (GetBodyClassificationAt(x, y) != WaterBodyType.Lake) continue;
                    int sLake = GetSurfaceHeightAt(x, y);
                    if (sLake < 0) continue;
                    for (int i = 0; i < 4; i++)
                    {
                        int nx = x + d4x[i], ny = y + d4y[i];
                        if (!IsValidPosition(nx, ny) || !IsWater(nx, ny)) continue;
                        if (GetBodyClassificationAt(nx, ny) != WaterBodyType.River) continue;
                        int sRiver = GetSurfaceHeightAt(nx, ny);
                        if (sRiver < 0 || sRiver >= sLake) continue;
                        candidates.Add((x, y, sLake));
                        break;
                    }
                }
            if (candidates.Count == 0) return false;
            bool any = false;
            foreach (var (x, y, sLake) in candidates)
            {
                if (!IsWater(x, y) || GetBodyClassificationAt(x, y) != WaterBodyType.Lake) continue;
                ClearWaterAt(x, y);
                int clampedSurface = Mathf.Clamp(sLake, HeightMap.MIN_HEIGHT, HeightMap.MAX_HEIGHT);
                heightMap.SetHeight(x, y, clampedSurface);
                if (gridManager != null)
                    gridManager.SetCellHeight(new Vector2(x, y), heightMap.GetHeight(x, y));
                restoredCells.Add((x, y, sLake));
                any = true;
            }
            return any;
        }

        public IReadOnlyDictionary<int, WaterBody> GetBodies() => bodies;

        public IReadOnlyList<Vector2Int> GetProceduralRiverEntryAnchors() => proceduralRiverEntryAnchors;

        public void RecordProceduralRiverEntryAnchor(int x, int y)
        {
            if (IsValidPosition(x, y))
                proceduralRiverEntryAnchors.Add(new Vector2Int(x, y));
        }

        public bool TryGetAllWaterBoundingBox(out int minX, out int minY, out int maxX, out int maxY)
        {
            minX = minY = maxX = maxY = 0;
            bool found = false;
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    if (waterBodyIds[x, y] == 0) continue;
                    if (!found) { minX = maxX = x; minY = maxY = y; found = true; }
                    else
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            return found;
        }

        public void RestoreFromLegacyCellData(List<CellData> gridData, int seaLevel)
        {
            ClearAllWater();
            if (gridData == null) return;
            const int legacyId = 1;
            var body = new WaterBody(legacyId, seaLevel, WaterBodyType.Lake);
            bodies[legacyId] = body;
            nextBodyId = 2;
            foreach (CellData cd in gridData)
            {
                if (!IsValidPosition(cd.x, cd.y)) continue;
                bool isWater = cd.height <= seaLevel
                    || (cd.zoneType != null && cd.zoneType.Equals("Water", StringComparison.OrdinalIgnoreCase));
                if (!isWater) continue;
                waterBodyIds[cd.x, cd.y] = legacyId;
                body.AddCellIndex(ToFlat(cd.x, cd.y));
            }
        }

        public void AddLegacyPaintedWaterCell(int x, int y, int surfaceHeight, WaterBodyType classification = WaterBodyType.Lake)
        {
            if (!IsValidPosition(x, y)) return;
            int legacyBodyId = LegacyPaintWaterBodyId;
            EnsureBody(legacyBodyId, surfaceHeight, classification);
            int oldId = waterBodyIds[x, y];
            if (oldId != 0 && oldId != legacyBodyId)
                RemoveCellFromBody(x, y, oldId);
            int flat = ToFlat(x, y);
            waterBodyIds[x, y] = legacyBodyId;
            bodies[legacyBodyId].SurfaceHeight = surfaceHeight;
            bodies[legacyBodyId].AddCellIndex(flat);
            nextBodyId = Math.Max(nextBodyId, legacyBodyId + 1);
        }

        public void ClearWaterAt(int x, int y)
        {
            if (!IsValidPosition(x, y)) return;
            int id = waterBodyIds[x, y];
            if (id == 0) return;
            RemoveCellFromBody(x, y, id);
        }

        private void RemoveCellFromBody(int x, int y, int bodyId)
        {
            int flat = ToFlat(x, y);
            waterBodyIds[x, y] = 0;
            if (bodies.TryGetValue(bodyId, out WaterBody body))
            {
                body.RemoveCellIndex(flat);
                if (body.CellCount == 0) bodies.Remove(bodyId);
            }
        }

        internal void EnsureBody(int id, int surfaceHeight, WaterBodyType classification = WaterBodyType.Lake)
        {
            if (!bodies.TryGetValue(id, out WaterBody body))
            {
                body = new WaterBody(id, surfaceHeight, classification);
                bodies[id] = body;
                nextBodyId = Math.Max(nextBodyId, id + 1);
            }
            else
                body.SurfaceHeight = surfaceHeight;
        }

        public void MergeSeaLevelDryCellsFromHeightMap(HeightMap heightMap, int seaLevel)
        {
            if (heightMap == null) return;
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    if (!heightMap.IsValidPosition(x, y)) continue;
                    if (heightMap.GetHeight(x, y) > seaLevel) continue;
                    if (IsWater(x, y)) continue;
                    AddLegacyPaintedWaterCell(x, y, seaLevel, WaterBodyType.Sea);
                }
        }

        public void InitializeWaterBodiesBasedOnHeight(HeightMap heightMap, int seaLevel)
        {
            ClearAllWater();
            if (heightMap == null) return;
            const int legacyId = 1;
            var body = new WaterBody(legacyId, seaLevel, WaterBodyType.Sea);
            bodies[legacyId] = body;
            nextBodyId = 2;
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (heightMap.GetHeight(x, y) <= seaLevel)
                    {
                        waterBodyIds[x, y] = legacyId;
                        body.AddCellIndex(ToFlat(x, y));
                    }
        }

        // Serialization: see WaterMap.Serialization.cs

        internal void ClearAllWater()
        {
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    waterBodyIds[x, y] = 0;
            bodies.Clear();
            nextBodyId = 1;
            proceduralRiverEntryAnchors.Clear();
        }

        internal int ToFlat(int x, int y) => x + y * width;

        internal void SetWaterBodyId(int x, int y, int bodyId) => waterBodyIds[x, y] = bodyId;

        // River ops
        public int CreateRiverWaterBody(int surfaceHeight)
        {
            int bodyId = nextBodyId++;
            bodies[bodyId] = new WaterBody(bodyId, surfaceHeight, WaterBodyType.River);
            return bodyId;
        }

        public bool TryAssignCellToRiverBody(int x, int y, int bodyId)
        {
            if (!IsValidPosition(x, y)) return false;
            if (waterBodyIds[x, y] != 0) return false;
            if (!bodies.TryGetValue(bodyId, out WaterBody wb) || wb.Classification != WaterBodyType.River) return false;
            waterBodyIds[x, y] = bodyId;
            wb.AddCellIndex(ToFlat(x, y));
            return true;
        }

        public bool TryReassignCellFromAnyWaterToRiverBody(int x, int y, int riverBodyId)
        {
            if (!IsValidPosition(x, y)) return false;
            if (!bodies.TryGetValue(riverBodyId, out WaterBody river) || river.Classification != WaterBodyType.River) return false;
            int oldId = waterBodyIds[x, y];
            if (oldId == 0) return TryAssignCellToRiverBody(x, y, riverBodyId);
            if (oldId == riverBodyId) return true;
            if (bodies.TryGetValue(oldId, out WaterBody oldBody)
                && oldBody.Classification == WaterBodyType.Lake
                && oldBody.SurfaceHeight != river.SurfaceHeight)
                return false;
            RemoveCellFromBody(x, y, oldId);
            waterBodyIds[x, y] = riverBodyId;
            river.AddCellIndex(ToFlat(x, y));
            return true;
        }

        public void MergeAdjacentBodiesWithSameSurface() => RunMergeAdjacentBodiesWithSameSurface();

        internal void RunMergeAdjacentBodiesWithSameSurface()
        {
            bool changed = true;
            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };
            while (changed)
            {
                changed = false;
                for (int x = 0; x < width; x++)
                    for (int y = 0; y < height; y++)
                    {
                        int idA = waterBodyIds[x, y];
                        if (idA == 0) continue;
                        for (int i = 0; i < 4; i++)
                        {
                            int nx = x + dx[i], ny = y + dy[i];
                            if (!IsValidPosition(nx, ny)) continue;
                            int idB = waterBodyIds[nx, ny];
                            if (idB == 0 || idA == idB) continue;
                            if (bodies[idA].SurfaceHeight != bodies[idB].SurfaceHeight) continue;
                            if (!CanMergeWaterBodies(bodies[idA], bodies[idB])) continue;
                            int keep = Math.Min(idA, idB);
                            int merge = Math.Max(idA, idB);
                            MergeBodyInto(merge, keep);
                            changed = true;
                            idA = waterBodyIds[x, y];
                        }
                    }
            }
        }

        private static bool CanMergeWaterBodies(WaterBody a, WaterBody b)
        {
            if (a.Classification == b.Classification) return true;
            if (a.Classification == WaterBodyType.River || b.Classification == WaterBodyType.River) return false;
            return true;
        }

        internal void MergeBodyInto(int fromId, int toId)
        {
            if (fromId == toId || !bodies.TryGetValue(fromId, out WaterBody from) || !bodies.TryGetValue(toId, out WaterBody to))
                return;
            foreach (int flat in from.CellIndices)
            {
                int x = flat % width, y = flat / width;
                waterBodyIds[x, y] = toId;
                to.AddCellIndex(flat);
            }
            bodies.Remove(fromId);
        }

        // Lake generation: see WaterMap.LakeGen.cs
        public void InitializeLakesFromDepressionFill(HeightMap heightMap, LakeFillSettings settings, int seaLevelForArtificialFallback = 0)
            => InitializeLakesFromDepressionFillImpl(heightMap, settings, seaLevelForArtificialFallback);
    }

    // WaterMapData, WaterBodySerialized: see WaterMap.Serialization.cs
    // LakeFillSettings: see WaterMap.LakeGen.cs
}
