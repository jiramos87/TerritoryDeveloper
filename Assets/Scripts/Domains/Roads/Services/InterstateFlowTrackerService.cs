using UnityEngine;
using System.Collections.Generic;
using Territory.Core;
using Territory.Zones;

namespace Domains.Roads.Services
{
/// <summary>
/// Tracks interstate connectivity and position state.
/// Owns RebuildFromGrid, CheckInterstateConnectivity, CanPlaceStreetFrom.
/// </summary>
public class InterstateFlowTrackerService
{
    private readonly IGridManager _grid;

    public InterstateFlowTrackerService(IGridManager grid)
    {
        _grid = grid;
    }

    // ------------------------------------------------------------------
    // Grid query helpers
    // ------------------------------------------------------------------

    public bool IsInterstateAt(int x, int y)
    {
        if (_grid == null) return false;
        CityCell cell = _grid.GetCell(x, y);
        return cell != null && cell.isInterstate;
    }

    public bool IsInterstateAt(Vector2 gridPos)
    {
        return IsInterstateAt(Mathf.RoundToInt(gridPos.x), Mathf.RoundToInt(gridPos.y));
    }

    public bool IsRoadAt(int gridX, int gridY)
    {
        if (_grid == null) return false;
        CityCell cell = _grid.GetCell(gridX, gridY);
        return cell != null && cell.zoneType == Zone.ZoneType.Road;
    }

    // ------------------------------------------------------------------
    // Rebuild from saved grid state
    // ------------------------------------------------------------------

    public void RebuildFromGrid(List<Vector2Int> positions, ref Vector2Int? entryPoint, ref Vector2Int? exitPoint, ref int entryBorder, ref int exitBorder)
    {
        positions.Clear();
        entryPoint = null;
        exitPoint = null;
        entryBorder = -1;
        exitBorder = -1;
        if (_grid == null) return;
        int w = _grid.width;
        int h = _grid.height;
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                CityCell cell = _grid.GetCell(x, y);
                if (cell != null && cell.isInterstate)
                    positions.Add(new Vector2Int(x, y));
            }
        }
        Vector2Int? firstBorder = null;
        int firstBorderIdx = -1;
        Vector2Int? secondBorder = null;
        int secondBorderIdx = -1;
        foreach (Vector2Int pos in positions)
        {
            int borderIdx = -1;
            if (pos.y == 0) borderIdx = 0;
            else if (pos.y == h - 1) borderIdx = 1;
            else if (pos.x == 0) borderIdx = 2;
            else if (pos.x == w - 1) borderIdx = 3;
            if (borderIdx < 0) continue;
            if (!firstBorder.HasValue)
            {
                firstBorder = pos;
                firstBorderIdx = borderIdx;
            }
            else if (!secondBorder.HasValue)
            {
                secondBorder = pos;
                secondBorderIdx = borderIdx;
                break;
            }
        }
        if (firstBorder.HasValue)
        {
            entryPoint = firstBorder;
            entryBorder = firstBorderIdx;
        }
        if (secondBorder.HasValue)
        {
            exitPoint = secondBorder;
            exitBorder = secondBorderIdx;
        }
    }

    // ------------------------------------------------------------------
    // Connectivity check
    // ------------------------------------------------------------------

    public bool CheckInterstateConnectivity(List<Vector2Int> interstatePositions)
    {
        if (_grid == null || interstatePositions.Count == 0) return false;

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        foreach (Vector2Int p in interstatePositions)
        {
            queue.Enqueue(p);
            visited.Add(p);
        }

        int w = _grid.width;
        int h = _grid.height;
        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };

        while (queue.Count > 0)
        {
            Vector2Int curr = queue.Dequeue();
            if (!IsInterstateAt(curr.x, curr.y))
                return true;

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

    // ------------------------------------------------------------------
    // Street placement eligibility
    // ------------------------------------------------------------------

    public bool CanPlaceStreetFrom(Vector2 gridPosition)
    {
        int x = Mathf.RoundToInt(gridPosition.x);
        int y = Mathf.RoundToInt(gridPosition.y);
        return CanPlaceStreetFrom(x, y);
    }

    public bool CanPlaceStreetFrom(int x, int y)
    {
        if (_grid == null) return false;
        int w = _grid.width;
        int h = _grid.height;
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

    private bool IsStreetConnectedToInterstate(int startX, int startY)
    {
        if (_grid == null) return false;
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        queue.Enqueue(new Vector2Int(startX, startY));
        visited.Add(new Vector2Int(startX, startY));

        int w = _grid.width;
        int h = _grid.height;
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
}
}
