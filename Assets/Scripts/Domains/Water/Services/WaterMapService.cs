using System;
using System.Collections.Generic;
using Territory.Terrain;
using UnityEngine;

namespace Domains.Water.Services
{
    /// <summary>
    /// Pure water-body management logic extracted from WaterMap for testability.
    /// No MonoBehaviour dependency. Manages water body id assignment, lake depression-fill params,
    /// serialization helpers, and river body ops.
    /// Extracted from Territory.Terrain.WaterMap per Strategy γ atomization (TECH-23777).
    /// Invariant #8: river bed monotonic constraint enforced in WaterManager; this service provides query surface only.
    /// </summary>
    public class WaterMapService
    {
        /// <summary>Reserved id for player-painted water. Matches WaterMap.LegacyPaintWaterBodyId.</summary>
        public const int LegacyPaintWaterBodyId = 10001;

        /// <summary>Format version V2: waterBodyIds per cell.</summary>
        public const int FormatVersionV2 = 2;

        /// <summary>Format version V3: bodyClassification per body.</summary>
        public const int FormatVersionV3 = 3;

        private readonly int _width;
        private readonly int _height;

        /// <summary>Construct water map service with grid dimensions.</summary>
        public WaterMapService(int width, int height)
        {
            _width = width;
            _height = height;
        }

        /// <summary>
        /// Validate that a WaterMapData payload is compatible with this map dimensions.
        /// Returns true when data can be loaded; false with reason on mismatch.
        /// </summary>
        public bool ValidateSerializedData(WaterMapData data, out string reason)
        {
            if (data == null)
            {
                reason = "data is null";
                return false;
            }
            if (data.formatVersion >= FormatVersionV2)
            {
                if (data.waterBodyIds == null || data.waterBodyIds.Length != _width * _height)
                {
                    reason = $"waterBodyIds length {data.waterBodyIds?.Length ?? 0} != expected {_width * _height}";
                    return false;
                }
            }
            else
            {
                if (data.waterCells == null || data.waterCells.Length != _width * _height)
                {
                    reason = $"legacy waterCells length {data.waterCells?.Length ?? 0} != expected {_width * _height}";
                    return false;
                }
            }
            reason = null;
            return true;
        }

        /// <summary>
        /// Deserialize body classification from serialized record.
        /// None maps to Lake (backward compat).
        /// </summary>
        public static WaterBodyType DeserializeBodyClassification(WaterBodySerialized ser)
        {
            if (ser == null)
                return WaterBodyType.Lake;
            WaterBodyType cls = (WaterBodyType)ser.bodyClassification;
            if (cls == WaterBodyType.None)
                return WaterBodyType.Lake;
            return cls;
        }

        /// <summary>
        /// True when two water bodies can merge: same classification, or neither is River.
        /// Rivers merge only with rivers; lakes/seas merge with each other.
        /// </summary>
        public static bool CanMergeWaterBodies(WaterBody a, WaterBody b)
        {
            if (a.Classification == b.Classification)
                return true;
            if (a.Classification == WaterBodyType.River || b.Classification == WaterBodyType.River)
                return false;
            return true;
        }

        /// <summary>
        /// Compute artificial lake edge margin from map dimensions.
        /// Smaller margins on tiny grids allow 1×1 lakes to be placed.
        /// </summary>
        public int ComputeArtificialEdgeMargin()
        {
            int m = Mathf.Min(_width, _height);
            if (m <= 4) return 0;
            if (m <= 9) return 1;
            return 2;
        }

        /// <summary>
        /// Area-scaled extra seed attempts for lake depression fill.
        /// Scales up for maps larger than reference area.
        /// </summary>
        public static int GetScaledRandomExtraSeedAttempts(LakeFillSettings settings, int mapWidth, int mapHeight)
        {
            int refArea = Mathf.Max(1, settings.ReferenceMapSide * settings.ReferenceMapSide);
            int area = mapWidth * mapHeight;
            int scaled = Mathf.RoundToInt(settings.RandomExtraSeedAttempts * (area / (float)refArea));
            return Mathf.Max(settings.RandomExtraSeedAttempts, scaled);
        }

        /// <summary>
        /// Target procedural lake body count from settings + map dimensions.
        /// </summary>
        public static int GetEffectiveMaxLakeBodies(LakeFillSettings settings, int mapWidth, int mapHeight)
        {
            return settings.GetEffectiveMaxLakeBodies(mapWidth, mapHeight);
        }

        /// <summary>
        /// True if axis-aligned bounding box of lake (grid cells) fits configured min/max extent on both axes.
        /// </summary>
        public static bool LakeBoundingBoxFits(LakeFillSettings settings, List<Vector2Int> basinCells)
        {
            if (basinCells == null || basinCells.Count == 0)
                return false;
            int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
            foreach (var c in basinCells)
            {
                if (c.x < minX) minX = c.x;
                if (c.x > maxX) maxX = c.x;
                if (c.y < minY) minY = c.y;
                if (c.y > maxY) maxY = c.y;
            }
            int bw = maxX - minX + 1;
            int bh = maxY - minY + 1;
            return bw >= settings.MinLakeBoundingExtent && bw <= settings.MaxLakeBoundingExtent
                && bh >= settings.MinLakeBoundingExtent && bh <= settings.MaxLakeBoundingExtent;
        }

        /// <summary>
        /// True if artificial rectangle width/height match procedural bbox axis limits.
        /// </summary>
        public static bool ArtificialRectangleBboxFits(LakeFillSettings settings, int rw, int rh)
        {
            return rw >= settings.MinLakeBoundingExtent && rw <= settings.MaxLakeBoundingExtent
                && rh >= settings.MinLakeBoundingExtent && rh <= settings.MaxLakeBoundingExtent;
        }

        /// <summary>
        /// Sort seed candidates by spill headroom descending, then terrain height, then random tie-break.
        /// </summary>
        public static void SortSeedCandidatesBySpillHeadroom(List<Vector2Int> cells, Func<int, int, int> getSpillHeight, Func<int, int, int> getHeight, System.Random rnd)
        {
            int n = cells.Count;
            if (n <= 1)
                return;

            var tmp = new List<(Vector2Int p, int tie)>(n);
            for (int i = 0; i < n; i++)
                tmp.Add((cells[i], rnd.Next()));

            tmp.Sort((A, B) =>
            {
                Vector2Int a = A.p;
                Vector2Int b = B.p;
                int spillA = getSpillHeight(a.x, a.y);
                int spillB = getSpillHeight(b.x, b.y);
                int hA = getHeight(a.x, a.y);
                int hB = getHeight(b.x, b.y);
                int scoreA = spillA - hA;
                int scoreB = spillB - hB;
                int c = scoreB.CompareTo(scoreA);
                if (c != 0) return c;
                c = hA.CompareTo(hB);
                if (c != 0) return c;
                return A.tie.CompareTo(B.tie);
            });

            cells.Clear();
            for (int i = 0; i < n; i++)
                cells.Add(tmp[i].p);
        }
    }
}
