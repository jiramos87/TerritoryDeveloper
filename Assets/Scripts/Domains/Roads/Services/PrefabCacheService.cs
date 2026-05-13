using System.Collections.Generic;
using UnityEngine;
using Territory.Core;
using Territory.Terrain;
using Territory.Roads;

namespace Domains.Roads.Services
{
/// <summary>
/// Bridge and water deck inference helpers for prefab resolution.
/// Extracted from PrefabResolverService (Stage 7.3 Tier-E split).
/// </summary>
internal class PrefabCacheService
{
    private readonly IGridManager gridManager;
    private readonly ITerrainManager terrainManager;
    private readonly IRoadManager roadManager;
    private readonly PrefabLookupService lookup;

    private static readonly int[] DirX = { 1, -1, 0, 0 };
    private static readonly int[] DirY = { 0, 0, 1, -1 };

    internal PrefabCacheService(IGridManager grid, ITerrainManager terrain, IRoadManager roads, PrefabLookupService lookupSvc)
    {
        gridManager = grid;
        terrainManager = terrain;
        roadManager = roads;
        lookup = lookupSvc;
    }

    internal bool IsBridgeDeckRoadPrefab(GameObject prefab)
    {
        if (prefab == null || roadManager == null) return false;
        return prefab == roadManager.roadTileBridgeHorizontal || prefab == roadManager.roadTileBridgeVertical;
    }

    internal IWaterManager ResolveWaterManagerForBridge()
    {
        return terrainManager?.Water;
    }

    internal bool DryCellTouchesRegisteredWaterMoore(int x, int y, IWaterManager wm)
    {
        if (terrainManager == null) return false;
        if (terrainManager.IsRegisteredOpenWaterAt(x, y) || terrainManager.IsWaterSlopeCell(x, y)) return true;
        if (wm == null) return false;
        int[] mdx = { 1, -1, 0, 0, 1, 1, -1, -1 };
        int[] mdy = { 0, 0, 1, -1, 1, -1, 1, -1 };
        for (int d = 0; d < 8; d++)
        {
            if (wm.IsWaterAt(x + mdx[d], y + mdy[d])) return true;
        }
        return false;
    }

    internal bool LowerCardinalQualifiesAsHighDeckWaterStep(int nx, int ny, int hn, int lipHeight, IWaterManager wm)
    {
        if (hn >= lipHeight) return false;
        if (terrainManager.IsRegisteredOpenWaterAt(nx, ny) || terrainManager.IsWaterSlopeCell(nx, ny)) return true;
        return DryCellTouchesRegisteredWaterMoore(nx, ny, wm);
    }

    internal bool DryLandCardinalLowerTouchesRegisteredWater(int x, int y, int h, HeightMap heightMap)
    {
        if (heightMap == null || terrainManager == null || !heightMap.IsValidPosition(x, y)) return false;
        IWaterManager wm = ResolveWaterManagerForBridge();
        int[] cdx = { 1, -1, 0, 0 };
        int[] cdy = { 0, 0, 1, -1 };
        for (int d = 0; d < 4; d++)
        {
            int nx = x + cdx[d], ny = y + cdy[d];
            if (!heightMap.IsValidPosition(nx, ny)) continue;
            int hn = heightMap.GetHeight(nx, ny);
            if (LowerCardinalQualifiesAsHighDeckWaterStep(nx, ny, hn, h, wm)) return true;
        }
        return false;
    }

    internal Vector2 InferStrongestCardinalTowardQualifyingLowerNeighbor(int x, int y, int h, HeightMap heightMap)
    {
        IWaterManager wm = ResolveWaterManagerForBridge();
        int bestDelta = -1;
        Vector2 best = Vector2.right;
        int[] cdx = { 1, -1, 0, 0 };
        int[] cdy = { 0, 0, 1, -1 };
        for (int d = 0; d < 4; d++)
        {
            int nx = x + cdx[d], ny = y + cdy[d];
            if (!heightMap.IsValidPosition(nx, ny)) continue;
            int hn = heightMap.GetHeight(nx, ny);
            if (!LowerCardinalQualifiesAsHighDeckWaterStep(nx, ny, hn, h, wm)) continue;
            int delta = h - hn;
            if (delta > bestDelta) { bestDelta = delta; best = new Vector2(nx - x, ny - y); }
        }
        return bestDelta >= 0 ? best : Vector2.right;
    }

    internal void CollectHighDeckBridgeNormals(int x, int y, int h, HeightMap heightMap, List<Vector2Int> outNormals)
    {
        outNormals.Clear();
        if (heightMap == null || terrainManager == null || !heightMap.IsValidPosition(x, y)) return;
        IWaterManager wm = ResolveWaterManagerForBridge();
        int[] cdx = { 1, -1, 0, 0 };
        int[] cdy = { 0, 0, 1, -1 };
        for (int d = 0; d < 4; d++)
        {
            int nx = x + cdx[d], ny = y + cdy[d];
            if (!heightMap.IsValidPosition(nx, ny)) continue;
            int hn = heightMap.GetHeight(nx, ny);
            if (!LowerCardinalQualifiesAsHighDeckWaterStep(nx, ny, hn, h, wm)) continue;
            var step = new Vector2Int(cdx[d], cdy[d]);
            if (!outNormals.Contains(step)) outNormals.Add(step);
        }
    }

    internal bool CardinalApproachParallelToHighDeckNormal(Vector2 dirIn, List<Vector2Int> bridgeNormals)
    {
        if (bridgeNormals == null || bridgeNormals.Count == 0) return false;
        if (Mathf.Approximately(dirIn.x, 0f) && Mathf.Approximately(dirIn.y, 0f)) return true;
        int rdx = Mathf.RoundToInt(dirIn.x);
        int rdy = Mathf.RoundToInt(dirIn.y);
        if (Mathf.Abs(rdx) + Mathf.Abs(rdy) != 1) return false;
        for (int i = 0; i < bridgeNormals.Count; i++)
        {
            Vector2Int n = bridgeNormals[i];
            if (rdx == n.x && rdy == n.y) return true;
            if (rdx == -n.x && rdy == -n.y) return true;
        }
        return false;
    }

    internal bool TryGetHighCliffLipBridgePrefab(PathTerraformPlan plan, int cx, int cy, int height, HeightMap heightMap, Vector2 dirIn, List<Vector2Int> scratchNormals, out GameObject prefab)
    {
        prefab = null;
        if (plan == null || !plan.waterBridgeTerraformRelaxation || plan.waterBridgeDeckDisplayHeight <= 0) return false;
        if (height != plan.waterBridgeDeckDisplayHeight || heightMap == null || terrainManager == null) return false;
        if (terrainManager.IsRegisteredOpenWaterAt(cx, cy) || terrainManager.IsWaterSlopeCell(cx, cy)) return false;
        if (!DryLandCardinalLowerTouchesRegisteredWater(cx, cy, height, heightMap)) return false;

        CollectHighDeckBridgeNormals(cx, cy, height, heightMap, scratchNormals);
        if (scratchNormals.Count == 0) return false;
        if (!CardinalApproachParallelToHighDeckNormal(dirIn, scratchNormals)) return false;

        bool isHorizontal;
        if (Mathf.Approximately(dirIn.x, 0f) && Mathf.Approximately(dirIn.y, 0f))
        {
            Vector2 toward = InferStrongestCardinalTowardQualifyingLowerNeighbor(cx, cy, height, heightMap);
            isHorizontal = Mathf.Abs(toward.x) >= Mathf.Abs(toward.y);
        }
        else
            isHorizontal = Mathf.Abs(dirIn.x) >= Mathf.Abs(dirIn.y);
        prefab = isHorizontal ? roadManager.roadTileBridgeHorizontal : roadManager.roadTileBridgeVertical;
        return true;
    }

    internal bool TryInferWaterBridgeDeckDisplayHeight(int x, int y, out int deckH)
    {
        deckH = 0;
        if (terrainManager == null || gridManager == null) return false;
        int best = 0;
        for (int d = 0; d < 4; d++)
        {
            int nx = x + DirX[d], ny = y + DirY[d];
            if (!lookup.IsValidGrid(nx, ny) || !lookup.IsRoadAt(new Vector2(nx, ny))) continue;
            if (terrainManager.IsRegisteredOpenWaterAt(nx, ny)) continue;
            CityCell cn = gridManager.GetCell(nx, ny);
            if (cn != null) best = Mathf.Max(best, cn.GetCellInstanceHeight());
        }
        if (best > 0) { deckH = best; return true; }

        for (int d = 0; d < 4; d++)
        {
            int cx = x + DirX[d], cy = y + DirY[d];
            if (!lookup.IsValidGrid(cx, cy) || !lookup.IsRoadAt(new Vector2(cx, cy))) continue;
            while (terrainManager.IsRegisteredOpenWaterAt(cx, cy))
            {
                int ax = cx + DirX[d], ay = cy + DirY[d];
                if (!lookup.IsValidGrid(ax, ay) || !lookup.IsRoadAt(new Vector2(ax, ay))) break;
                cx = ax; cy = ay;
            }
            if (lookup.IsValidGrid(cx, cy) && lookup.IsRoadAt(new Vector2(cx, cy)) && !terrainManager.IsRegisteredOpenWaterAt(cx, cy))
            {
                CityCell c = gridManager.GetCell(cx, cy);
                if (c != null) best = Mathf.Max(best, c.GetCellInstanceHeight());
            }
        }
        if (best > 0) { deckH = best; return true; }
        return false;
    }
}
}
