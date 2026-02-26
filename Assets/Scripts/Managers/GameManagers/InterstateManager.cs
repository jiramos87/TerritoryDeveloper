using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the Interstate Highway: generation at game start, connectivity checks,
/// and validation for player road placement (streets must grow from the interstate).
/// </summary>
public class InterstateManager : MonoBehaviour
{
    public GridManager gridManager;
    public TerrainManager terrainManager;
    public RoadManager roadManager;

    private List<Vector2Int> interstatePositions = new List<Vector2Int>();
    private bool isConnectedToInterstate;

    /// <summary>
    /// Whether the player's road network is connected to the interstate (updated monthly).
    /// </summary>
    public bool IsConnectedToInterstate => isConnectedToInterstate;

    /// <summary>
    /// Read-only list of grid positions that are part of the interstate.
    /// </summary>
    public IReadOnlyList<Vector2Int> InterstatePositions => interstatePositions;

    void Awake()
    {
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (terrainManager == null) terrainManager = FindObjectOfType<TerrainManager>();
        if (roadManager == null) roadManager = FindObjectOfType<RoadManager>();
    }

    /// <summary>
    /// Generate interstate route, place tiles, then refresh prefabs so junctions are correct.
    /// Call after water map, before forest map.
    /// </summary>
    public void GenerateAndPlaceInterstate()
    {
        if (roadManager == null) roadManager = FindObjectOfType<RoadManager>();
        GenerateInterstateRoute();
        if (interstatePositions.Count == 0 || roadManager == null) return;

        Vector2 prev = new Vector2(interstatePositions[0].x, interstatePositions[0].y);
        for (int i = 0; i < interstatePositions.Count; i++)
        {
            Vector2 curr = new Vector2(interstatePositions[i].x, interstatePositions[i].y);
            if (i > 0) prev = new Vector2(interstatePositions[i - 1].x, interstatePositions[i - 1].y);
            roadManager.PlaceInterstateTile(prev, curr, true);
        }

        if (interstatePositions.Count > 1)
        {
            for (int i = 0; i < interstatePositions.Count; i++)
            {
                Vector2 prevGrid = i > 0 ? new Vector2(interstatePositions[i - 1].x, interstatePositions[i - 1].y) : new Vector2(interstatePositions[1].x, interstatePositions[1].y);
                Vector2 currGrid = new Vector2(interstatePositions[i].x, interstatePositions[i].y);
                GameObject prefab = roadManager.GetCorrectRoadPrefabForPath(prevGrid, currGrid);
                roadManager.ReplaceRoadTileAt(interstatePositions[i], prefab, true);
            }
        }
    }

    /// <summary>
    /// Generate interstate route and store positions. Does not place tiles (call PlaceInterstateTiles after).
    /// </summary>
    public List<Vector2Int> GenerateInterstateRoute()
    {
        interstatePositions.Clear();
        if (gridManager == null || terrainManager == null) return interstatePositions;

        HeightMap heightMap = terrainManager.GetHeightMap();
        if (heightMap == null) return interstatePositions;

        int w = gridManager.width;
        int h = gridManager.height;

        // 1. Pick two distinct borders
        List<int> borders = new List<int> { 0, 1, 2, 3 }; // 0=South, 1=North, 2=West, 3=East
        int borderA = borders[Random.Range(0, borders.Count)];
        borders.Remove(borderA);
        int borderB = borders[Random.Range(0, borders.Count)];

        Vector2Int? entry = GetValidBorderCell(borderA, w, h, heightMap);
        Vector2Int? exit = GetValidBorderCell(borderB, w, h, heightMap);
        if (!entry.HasValue || !exit.HasValue)
        {
            Debug.LogWarning("InterstateManager: Could not find valid entry/exit on borders. Using fallback.");
            entry = GetValidBorderCell(0, w, h, heightMap);
            exit = GetValidBorderCell(1, w, h, heightMap);
            if (!entry.HasValue || !exit.HasValue) return interstatePositions;
        }

        List<Vector2Int> path = null;
        for (int tryCount = 0; tryCount < 3; tryCount++)
        {
            path = BiasedWalkPath(entry.Value, exit.Value, w, h, heightMap);
            if (path != null && path.Count > 0 && path[path.Count - 1] == exit.Value)
                break;
        }

        if (path != null && path.Count > 0 && path[path.Count - 1] == exit.Value)
            interstatePositions = path;

        return interstatePositions;
    }

    /// <summary>
    /// True if cell is valid for interstate: not water, and either flat or cardinal (N-S / E-W) slope only. No diagonal slopes.
    /// Uses same cardinal convention as TerrainManager: North = (x+1,y), South = (x-1,y), West = (x,y+1), East = (x,y-1).
    /// </summary>
    private static bool IsCellAllowedForInterstate(int x, int y, int w, int h, HeightMap heightMap)
    {
        if (heightMap.GetHeight(x, y) <= 0) return false;

        int currentHeight = heightMap.GetHeight(x, y);
        bool hasNorthSlope = heightMap.IsValidPosition(x + 1, y) && heightMap.GetHeight(x + 1, y) > currentHeight;
        bool hasSouthSlope = heightMap.IsValidPosition(x - 1, y) && heightMap.GetHeight(x - 1, y) > currentHeight;
        bool hasWestSlope = heightMap.IsValidPosition(x, y + 1) && heightMap.GetHeight(x, y + 1) > currentHeight;
        bool hasEastSlope = heightMap.IsValidPosition(x, y - 1) && heightMap.GetHeight(x, y - 1) > currentHeight;
        bool hasNorthEastSlope = heightMap.IsValidPosition(x + 1, y - 1) && heightMap.GetHeight(x + 1, y - 1) > currentHeight;
        bool hasNorthWestSlope = heightMap.IsValidPosition(x + 1, y + 1) && heightMap.GetHeight(x + 1, y + 1) > currentHeight;
        bool hasSouthEastSlope = heightMap.IsValidPosition(x - 1, y - 1) && heightMap.GetHeight(x - 1, y - 1) > currentHeight;
        bool hasSouthWestSlope = heightMap.IsValidPosition(x - 1, y + 1) && heightMap.GetHeight(x - 1, y + 1) > currentHeight;

        if (hasWestSlope && hasNorthSlope) return false;
        if (hasWestSlope && hasSouthSlope) return false;
        if (hasEastSlope && hasNorthSlope) return false;
        if (hasEastSlope && hasSouthSlope) return false;
        if (hasNorthEastSlope || hasNorthWestSlope || hasSouthEastSlope || hasSouthWestSlope) return false;

        return true;
    }

    /// <summary>
    /// Returns the only allowed first step from a border cell so the road enters in a cardinal (N-S or E-W) direction.
    /// </summary>
    private static Vector2Int? GetFirstStepFromBorder(Vector2Int start, int w, int h)
    {
        if (start.y == 0) return new Vector2Int(start.x, 1);
        if (start.y == h - 1) return new Vector2Int(start.x, h - 2);
        if (start.x == 0) return new Vector2Int(1, start.y);
        if (start.x == w - 1) return new Vector2Int(w - 2, start.y);
        return null;
    }

    const int MaxRiverWidthForBridge = 5;

    /// <summary>
    /// True if stepping onto this water cell is valid as a bridge: toward end there is a narrow strip of water then solid ground.
    /// </summary>
    private static bool IsValidBridgeSegment(List<Vector2Int> path, Vector2Int waterCell, Vector2Int end, int w, int h, HeightMap heightMap)
    {
        if (path.Count == 0) return false;
        int dx = end.x - waterCell.x;
        int dy = end.y - waterCell.y;
        int stepX = dx != 0 ? (dx > 0 ? 1 : -1) : 0;
        int stepY = dy != 0 ? (dy > 0 ? 1 : -1) : 0;
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

    private static Vector2Int? GetValidBorderCell(int border, int w, int h, HeightMap heightMap)
    {
        List<Vector2Int> candidates = new List<Vector2Int>();
        switch (border)
        {
            case 0: // South (y = 0)
                for (int x = 0; x < w; x++)
                    if (IsCellAllowedForInterstate(x, 0, w, h, heightMap)) candidates.Add(new Vector2Int(x, 0));
                break;
            case 1: // North (y = h-1)
                for (int x = 0; x < w; x++)
                    if (IsCellAllowedForInterstate(x, h - 1, w, h, heightMap)) candidates.Add(new Vector2Int(x, h - 1));
                break;
            case 2: // West (x = 0)
                for (int y = 0; y < h; y++)
                    if (IsCellAllowedForInterstate(0, y, w, h, heightMap)) candidates.Add(new Vector2Int(0, y));
                break;
            case 3: // East (x = w-1)
                for (int y = 0; y < h; y++)
                    if (IsCellAllowedForInterstate(w - 1, y, w, h, heightMap)) candidates.Add(new Vector2Int(w - 1, y));
                break;
        }

        if (candidates.Count == 0) return null;
        int center = candidates.Count / 2;
        int idx = Mathf.Clamp(center + Random.Range(-candidates.Count / 3, candidates.Count / 3 + 1), 0, candidates.Count - 1);
        return candidates[idx];
    }

    private List<Vector2Int> BiasedWalkPath(Vector2Int start, Vector2Int end, int w, int h, HeightMap heightMap)
    {
        List<Vector2Int> path = new List<Vector2Int> { start };
        HashSet<Vector2Int> visited = new HashSet<Vector2Int> { start };
        Vector2Int current = start;
        int maxSteps = w * h;
        int steps = 0;

        while (current != end && steps < maxSteps)
        {
            List<Vector2Int> candidates = new List<Vector2Int>();

            if (path.Count >= 2 && heightMap.GetHeight(current.x, current.y) == 0)
            {
                Vector2Int bridgeDir = new Vector2Int(
                    path[path.Count - 1].x - path[path.Count - 2].x,
                    path[path.Count - 1].y - path[path.Count - 2].y);
                candidates.Add(new Vector2Int(current.x + bridgeDir.x, current.y + bridgeDir.y));
            }
            else if (path.Count == 1)
            {
                Vector2Int? firstStep = GetFirstStepFromBorder(start, w, h);
                if (firstStep.HasValue) candidates.Add(firstStep.Value);
            }
            else
            {
                int dx = end.x - current.x;
                int dy = end.y - current.y;
                int ax = dx != 0 ? (dx > 0 ? 1 : -1) : 0;
                int ay = dy != 0 ? (dy > 0 ? 1 : -1) : 0;

                if (ax != 0) candidates.Add(new Vector2Int(current.x + ax, current.y));
                if (ay != 0) candidates.Add(new Vector2Int(current.x, current.y + ay));
                if (Random.value < 0.25f)
                {
                    if (ax != 0) { candidates.Add(new Vector2Int(current.x, current.y + 1)); candidates.Add(new Vector2Int(current.x, current.y - 1)); }
                    if (ay != 0) { candidates.Add(new Vector2Int(current.x + 1, current.y)); candidates.Add(new Vector2Int(current.x - 1, current.y)); }
                }
            }

            Vector2Int? next = null;
            int bestDist = int.MaxValue;
            foreach (Vector2Int c in candidates)
            {
                if (c.x < 0 || c.x >= w || c.y < 0 || c.y >= h) continue;
                if (visited.Contains(c)) continue;

                int cellHeight = heightMap.GetHeight(c.x, c.y);
                bool allowed = false;
                if (cellHeight > 0)
                    allowed = IsCellAllowedForInterstate(c.x, c.y, w, h, heightMap);
                else if (cellHeight == 0)
                    allowed = IsValidBridgeSegment(path, c, end, w, h, heightMap);

                if (!allowed) continue;

                int dist = Mathf.Abs(c.x - end.x) + Mathf.Abs(c.y - end.y);
                if (dist < bestDist || (dist == bestDist && Random.value > 0.5f))
                {
                    bestDist = dist;
                    next = c;
                }
            }

            if (!next.HasValue)
            {
                if (path.Count <= 1) break;
                path.RemoveAt(path.Count - 1);
                current = path[path.Count - 1];
                steps++;
                continue;
            }

            path.Add(next.Value);
            visited.Add(next.Value);
            current = next.Value;
            steps++;
        }

        return path;
    }

    /// <summary>
    /// Rebuild interstate positions list from grid (e.g. after load). Call after RestoreGrid.
    /// </summary>
    public void RebuildFromGrid()
    {
        interstatePositions.Clear();
        if (gridManager == null) return;
        for (int x = 0; x < gridManager.width; x++)
        {
            for (int y = 0; y < gridManager.height; y++)
            {
                Cell cell = gridManager.GetCell(x, y);
                if (cell != null && cell.isInterstate)
                    interstatePositions.Add(new Vector2Int(x, y));
            }
        }
    }

    /// <summary>
    /// Check if the given grid position is an interstate cell.
    /// </summary>
    public bool IsInterstateAt(int x, int y)
    {
        if (gridManager == null) return false;
        Cell cell = gridManager.GetCell(x, y);
        return cell != null && cell.isInterstate;
    }

    /// <summary>
    /// Check if the given grid position is an interstate cell.
    /// </summary>
    public bool IsInterstateAt(Vector2 gridPos)
    {
        return IsInterstateAt(Mathf.RoundToInt(gridPos.x), Mathf.RoundToInt(gridPos.y));
    }

    /// <summary>
    /// BFS from all interstate cells through road cells. Sets isConnectedToInterstate if any player road is reached.
    /// </summary>
    public void CheckInterstateConnectivity()
    {
        isConnectedToInterstate = false;
        if (gridManager == null || interstatePositions.Count == 0) return;

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        foreach (Vector2Int p in interstatePositions)
        {
            queue.Enqueue(p);
            visited.Add(p);
        }

        int w = gridManager.width;
        int h = gridManager.height;
        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };

        while (queue.Count > 0)
        {
            Vector2Int curr = queue.Dequeue();
            if (!IsInterstateAt(curr.x, curr.y))
            {
                isConnectedToInterstate = true;
                return;
            }

            for (int i = 0; i < 4; i++)
            {
                int nx = curr.x + dx[i];
                int ny = curr.y + dy[i];
                if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                var n = new Vector2Int(nx, ny);
                if (visited.Contains(n)) continue;
                if (!IsRoadAt(nx, ny)) continue;
                visited.Add(n);
                queue.Enqueue(n);
            }
        }
    }

    private bool IsRoadAt(int gridX, int gridY)
    {
        if (gridManager == null) return false;
        Cell cell = gridManager.GetCell(gridX, gridY);
        return cell != null && cell.zoneType == Zone.ZoneType.Road;
    }

    /// <summary>
    /// Whether the player can start placing a street from this position (must touch interstate or connected road chain).
    /// </summary>
    public bool CanPlaceStreetFrom(Vector2 gridPosition)
    {
        int x = Mathf.RoundToInt(gridPosition.x);
        int y = Mathf.RoundToInt(gridPosition.y);
        return CanPlaceStreetFrom(x, y);
    }

    /// <summary>
    /// Whether the player can start placing a street from (x,y). Valid if any cardinal neighbor is interstate or road connected to interstate.
    /// </summary>
    public bool CanPlaceStreetFrom(int x, int y)
    {
        if (gridManager == null) return false;
        int w = gridManager.width;
        int h = gridManager.height;
        if (x < 0 || x >= w || y < 0 || y >= h) return false;

        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };
        for (int i = 0; i < 4; i++)
        {
            int nx = x + dx[i];
            int ny = y + dy[i];
            if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
            if (IsInterstateAt(nx, ny)) return true;
            if (IsRoadAt(nx, ny) && IsStreetConnectedToInterstate(nx, ny)) return true;
        }
        return false;
    }

    /// <summary>
    /// BFS from (startX, startY) through road cells; returns true if an interstate cell is reached.
    /// </summary>
    private bool IsStreetConnectedToInterstate(int startX, int startY)
    {
        if (gridManager == null) return false;
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        queue.Enqueue(new Vector2Int(startX, startY));
        visited.Add(new Vector2Int(startX, startY));

        int w = gridManager.width;
        int h = gridManager.height;
        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };

        while (queue.Count > 0)
        {
            Vector2Int curr = queue.Dequeue();
            if (IsInterstateAt(curr.x, curr.y)) return true;

            for (int i = 0; i < 4; i++)
            {
                int nx = curr.x + dx[i];
                int ny = curr.y + dy[i];
                if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                var n = new Vector2Int(nx, ny);
                if (visited.Contains(n)) continue;
                if (!IsRoadAt(nx, ny)) continue;
                visited.Add(n);
                queue.Enqueue(n);
            }
        }
        return false;
    }

    /// <summary>
    /// Set connectivity flag (e.g. when loading from save).
    /// </summary>
    public void SetConnectedToInterstate(bool connected)
    {
        isConnectedToInterstate = connected;
    }
}
