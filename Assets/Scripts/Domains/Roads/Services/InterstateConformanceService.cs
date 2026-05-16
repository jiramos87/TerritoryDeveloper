using UnityEngine;
using System;
using System.Collections.Generic;
using Territory.Core;
using Territory.Terrain;

namespace Domains.Roads.Services
{
/// <summary>
/// Cell-level conformance checks for interstate placement.
/// Validates slopes, border eligibility, endpoint scoring.
/// </summary>
public class InterstateConformanceService
{
    private const int MaxRiverWidthForBridge = 5;

    private readonly ITerrainManager _terrain;

    /// <summary>Construct conformance service with terrain reference.</summary>
    public InterstateConformanceService(ITerrainManager terrain)
    {
        _terrain = terrain;
    }

    // ------------------------------------------------------------------
    // Border helpers
    // ------------------------------------------------------------------

    /// <summary>Borders (0..3) with at least one interstate-valid land cell.</summary>
    public List<int> GetBordersWithLand(int w, int h, HeightMap heightMap)
    {
        var list = new List<int>();
        for (int b = 0; b < 4; b++)
        {
            if (HasAnyValidCellOnBorder(b, w, h, heightMap))
                list.Add(b);
        }
        return list;
    }

    /// <summary>True if border has any valid interstate cell.</summary>
    public bool HasAnyValidCellOnBorder(int border, int w, int h, HeightMap heightMap)
    {
        switch (border)
        {
            case 0:
                for (int x = 0; x < w; x++)
                    if (IsCellAllowedForInterstate(x, 0, w, h, heightMap, checkSlopes: true)) return true;
                break;
            case 1:
                for (int x = 0; x < w; x++)
                    if (IsCellAllowedForInterstate(x, h - 1, w, h, heightMap, checkSlopes: true)) return true;
                break;
            case 2:
                for (int y = 0; y < h; y++)
                    if (IsCellAllowedForInterstate(0, y, w, h, heightMap, checkSlopes: true)) return true;
                break;
            case 3:
                for (int y = 0; y < h; y++)
                    if (IsCellAllowedForInterstate(w - 1, y, w, h, heightMap, checkSlopes: true)) return true;
                break;
        }
        return false;
    }

    /// <summary>True if cell allowed as interstate origin (height>0 + slope allowed).</summary>
    public bool IsCellAllowedForInterstate(int x, int y, int w, int h, HeightMap heightMap, bool checkSlopes)
    {
        if (heightMap == null) throw new ArgumentNullException(nameof(heightMap));

        if (heightMap.GetHeight(x, y) <= 0)
            return false;
        if (!checkSlopes || _terrain == null) return true;
        if (_terrain.IsWaterSlopeCell(x, y))
            return true;
        TerrainSlopeType st = _terrain.GetTerrainSlopeTypeAt(x, y);
        if (!IsLandSlopeAllowedForRoadStroke(st))
            return false;
        return true;
    }

    /// <summary>List valid interstate border cells on given border.</summary>
    public List<Vector2Int> GetValidBorderCells(int border, int w, int h, HeightMap heightMap)
    {
        var candidates = new List<Vector2Int>();
        switch (border)
        {
            case 0:
                for (int x = 0; x < w; x++)
                    if (IsCellAllowedForInterstate(x, 0, w, h, heightMap, checkSlopes: true)) candidates.Add(new Vector2Int(x, 0));
                break;
            case 1:
                for (int x = 0; x < w; x++)
                    if (IsCellAllowedForInterstate(x, h - 1, w, h, heightMap, checkSlopes: true)) candidates.Add(new Vector2Int(x, h - 1));
                break;
            case 2:
                for (int y = 0; y < h; y++)
                    if (IsCellAllowedForInterstate(0, y, w, h, heightMap, checkSlopes: true)) candidates.Add(new Vector2Int(0, y));
                break;
            case 3:
                for (int y = 0; y < h; y++)
                    if (IsCellAllowedForInterstate(w - 1, y, w, h, heightMap, checkSlopes: true)) candidates.Add(new Vector2Int(w - 1, y));
                break;
        }
        return candidates;
    }

    /// <summary>Border cells preferring non-water-slope; sorted by endpoint quality.</summary>
    public List<Vector2Int> GetValidBorderCellsWithPreference(int border, int w, int h, HeightMap heightMap)
    {
        var raw = GetValidBorderCells(border, w, h, heightMap);
        if (raw.Count == 0) return raw;

        var filtered = new List<Vector2Int>();
        foreach (var c in raw)
        {
            if (_terrain != null && _terrain.IsWaterSlopeCell(c.x, c.y))
                continue;
            filtered.Add(c);
        }

        if (filtered.Count == 0)
            return raw;

        SortBorderCellsByInterstateEndpointQuality(filtered, w, h, heightMap);
        return filtered;
    }

    /// <summary>Random top-third sample from sorted border candidates; null if empty.</summary>
    public Vector2Int? GetValidBorderCell(int border, int w, int h, HeightMap heightMap)
    {
        var candidates = GetValidBorderCellsWithPreference(border, w, h, heightMap);
        if (candidates.Count == 0) return null;
        int poolSize = Mathf.Min(candidates.Count, Mathf.Max(1, (candidates.Count + 2) / 3));
        int idx = UnityEngine.Random.Range(0, poolSize);
        return candidates[idx];
    }

    /// <summary>Sort cells by descending interstate endpoint score; tiebreak by xy.</summary>
    public void SortBorderCellsByInterstateEndpointQuality(List<Vector2Int> cells, int w, int h, HeightMap heightMap)
    {
        if (cells == null || cells.Count < 2) return;
        cells.Sort((a, b) =>
        {
            int sa = ComputeInterstateBorderEndpointScore(a, w, h, heightMap);
            int sb = ComputeInterstateBorderEndpointScore(b, w, h, heightMap);
            int cmp = sb.CompareTo(sa);
            if (cmp != 0) return cmp;
            if (a.x != b.x) return a.x.CompareTo(b.x);
            return a.y.CompareTo(b.y);
        });
    }

    /// <summary>Score border cell — prefers low height + flat surroundings + small first step.</summary>
    public int ComputeInterstateBorderEndpointScore(Vector2Int c, int w, int h, HeightMap heightMap)
    {
        if (heightMap == null || !heightMap.IsValidPosition(c.x, c.y))
            return int.MinValue;
        if (_terrain != null && _terrain.IsWaterSlopeCell(c.x, c.y))
            return int.MinValue;

        int h0 = heightMap.GetHeight(c.x, c.y);
        if (_terrain != null && _terrain.IsRegisteredOpenWaterAt(c.x, c.y))
            return int.MinValue;

        int score = 0;
        if (h0 == 1)
            score += 10_000;
        else if (h0 == 2)
            score += 3_000;
        else
            score += 1_000;

        int flatAroundBorder = 0;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = c.x + dx;
                int ny = c.y + dy;
                if (!heightMap.IsValidPosition(nx, ny)) continue;
                int nh = heightMap.GetHeight(nx, ny);
                if (_terrain != null && _terrain.IsRegisteredOpenWaterAt(nx, ny)) continue;
                if (nh == 1) flatAroundBorder++;
            }
        }
        score += flatAroundBorder * 500;

        Vector2Int? firstStep = GetFirstStepFromBorder(c, w, h);
        if (!firstStep.HasValue)
            return score;

        Vector2Int fs = firstStep.Value;
        if (!heightMap.IsValidPosition(fs.x, fs.y))
            return score;

        int h1 = heightMap.GetHeight(fs.x, fs.y);
        if (_terrain == null || !_terrain.IsRegisteredOpenWaterAt(fs.x, fs.y))
        {
            int stepDiff = Mathf.Abs(h1 - h0);
            score -= stepDiff * 2_000;
            if (h1 == 1)
                score += 4_000;

            int flatAroundFirst = 0;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = fs.x + dx;
                    int ny = fs.y + dy;
                    if (!heightMap.IsValidPosition(nx, ny)) continue;
                    int nh = heightMap.GetHeight(nx, ny);
                    if (_terrain != null && _terrain.IsRegisteredOpenWaterAt(nx, ny)) continue;
                    if (nh == 1) flatAroundFirst++;
                }
            }
            score += flatAroundFirst * 200;
        }

        return score;
    }

    // ------------------------------------------------------------------
    // Bridge validation
    // ------------------------------------------------------------------

    /// <summary>True if path crosses water then lands within max bridge width.</summary>
    public bool IsValidBridgeSegment(List<Vector2Int> path, Vector2Int waterCell, Vector2Int end, int w, int h, HeightMap heightMap)
    {
        if (path == null) throw new ArgumentNullException(nameof(path));
        if (heightMap == null) throw new ArgumentNullException(nameof(heightMap));

        if (path.Count == 0) return false;
        int dx = end.x - waterCell.x;
        int dy = end.y - waterCell.y;
        int stepX, stepY;
        if (Mathf.Abs(dx) >= Mathf.Abs(dy))
        {
            stepX = dx != 0 ? (dx > 0 ? 1 : -1) : 0;
            stepY = 0;
        }
        else
        {
            stepX = 0;
            stepY = dy != 0 ? (dy > 0 ? 1 : -1) : 0;
        }
        if (stepX == 0 && stepY == 0) return false;

        int px = waterCell.x;
        int py = waterCell.y;
        int waterCount = 0;
        while (px >= 0 && px < w && py >= 0 && py < h && waterCount <= MaxRiverWidthForBridge)
        {
            int height = heightMap.GetHeight(px, py);
            if (height > 0) return waterCount <= MaxRiverWidthForBridge;
            waterCount++;
            px += stepX;
            py += stepY;
        }
        return false;
    }

    /// <summary>True if bridge segment from start cell crosses ≤ max water tiles to land.</summary>
    public bool IsValidBridgeSegmentFrom(Vector2Int from, Vector2Int waterCell, Vector2Int end, int w, int h, HeightMap heightMap)
    {
        if (heightMap == null) throw new ArgumentNullException(nameof(heightMap));

        if (heightMap.GetHeight(waterCell.x, waterCell.y) != 0) return false;
        int dx = end.x - waterCell.x;
        int dy = end.y - waterCell.y;
        int stepX, stepY;
        if (Mathf.Abs(dx) >= Mathf.Abs(dy))
        {
            stepX = dx != 0 ? (dx > 0 ? 1 : -1) : 0;
            stepY = 0;
        }
        else
        {
            stepX = 0;
            stepY = dy != 0 ? (dy > 0 ? 1 : -1) : 0;
        }
        if (stepX == 0 && stepY == 0) return false;

        int px = waterCell.x, py = waterCell.y;
        int waterCount = 0;
        while (px >= 0 && px < w && py >= 0 && py < h && waterCount <= MaxRiverWidthForBridge)
        {
            if (heightMap.GetHeight(px, py) > 0) return waterCount <= MaxRiverWidthForBridge;
            waterCount++;
            px += stepX;
            py += stepY;
        }
        return false;
    }

    /// <summary>True if single step from→to valid (slope/height/bridge).</summary>
    public bool IsDirectStepValid(Vector2Int from, Vector2Int to, int w, int h, HeightMap heightMap, Vector2Int pathEnd)
    {
        if (heightMap == null) throw new ArgumentNullException(nameof(heightMap));

        if (to.x < 0 || to.x >= w || to.y < 0 || to.y >= h) return false;
        int hFrom = heightMap.GetHeight(from.x, from.y);
        int hTo = heightMap.GetHeight(to.x, to.y);
        if (hTo == 0)
            return IsValidBridgeSegmentFrom(from, to, pathEnd, w, h, heightMap);
        if (hFrom > 0 && Mathf.Abs(hTo - hFrom) > 1) return false;
        return IsCellAllowedForInterstate(to.x, to.y, w, h, heightMap, checkSlopes: true);
    }

    // ------------------------------------------------------------------
    // Static direction helpers
    // ------------------------------------------------------------------

    /// <summary>First inward step from border cell; null if not on border.</summary>
    public static Vector2Int? GetFirstStepFromBorder(Vector2Int start, int w, int h)
    {
        if (start.y == 0) return new Vector2Int(start.x, 1);
        if (start.y == h - 1) return new Vector2Int(start.x, h - 2);
        if (start.x == 0) return new Vector2Int(1, start.y);
        if (start.x == w - 1) return new Vector2Int(w - 2, start.y);
        return null;
    }

    /// <summary>
    /// Inline of RoadStrokeTerrainRules.IsLandSlopeAllowedForRoadStroke — inlined here to avoid cross-asmdef dep.
    /// Allowed: Flat + cardinal ramps + corner-up concave. Outer-corner diagonal slopes disallowed.
    /// </summary>
    public static bool IsLandSlopeAllowedForRoadStroke(TerrainSlopeType slopeType)
    {
        return slopeType == TerrainSlopeType.Flat
            || slopeType == TerrainSlopeType.North
            || slopeType == TerrainSlopeType.South
            || slopeType == TerrainSlopeType.East
            || slopeType == TerrainSlopeType.West
            || slopeType == TerrainSlopeType.NorthEastUp
            || slopeType == TerrainSlopeType.NorthWestUp
            || slopeType == TerrainSlopeType.SouthEastUp
            || slopeType == TerrainSlopeType.SouthWestUp;
    }
}
}
