using UnityEngine;
using System.Collections.Generic;

namespace Domains.Roads.Services
{
/// <summary>
/// Pure stateless service extracted from Territory.Roads.InterstateManager (Stage 16 atomization).
/// Provides border/direction helpers used by interstate generation + connectivity checks.
/// No MonoBehaviour, no SerializeField. InterstateManager composes this in-place.
/// Invariant #10 (PathTerraformPlan) preserved in RoadManager.
/// </summary>
public class InterstateService
{
    /// <summary>Border index for south edge (y == 0).</summary>
    public const int BorderSouth = 0;
    /// <summary>Border index for north edge (y == h-1).</summary>
    public const int BorderNorth = 1;
    /// <summary>Border index for west edge (x == 0).</summary>
    public const int BorderWest = 2;
    /// <summary>Border index for east edge (x == w-1).</summary>
    public const int BorderEast = 3;

    /// <summary>
    /// Return border index (0=South,1=North,2=West,3=East) for a cell on the map edge.
    /// Returns -1 if cell is not on any border.
    /// </summary>
    public int GetBorderIndex(Vector2Int pos, int w, int h)
    {
        if (pos.y == 0) return BorderSouth;
        if (pos.y == h - 1) return BorderNorth;
        if (pos.x == 0) return BorderWest;
        if (pos.x == w - 1) return BorderEast;
        return -1;
    }

    /// <summary>
    /// True if the cell sits on any map border (edge row or column).
    /// </summary>
    public bool IsOnMapBorder(Vector2Int pos, int w, int h)
    {
        return pos.x == 0 || pos.x == w - 1 || pos.y == 0 || pos.y == h - 1;
    }

    /// <summary>
    /// True if step from <paramref name="a"/> to <paramref name="b"/> is strictly cardinal (no diagonals).
    /// </summary>
    public bool IsCardinalStep(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(b.x - a.x);
        int dy = Mathf.Abs(b.y - a.y);
        return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
    }

    /// <summary>
    /// True if every consecutive step in <paramref name="path"/> is strictly cardinal.
    /// </summary>
    public bool IsCardinalPath(List<Vector2Int> path)
    {
        if (path == null || path.Count < 2) return false;
        for (int i = 1; i < path.Count; i++)
        {
            if (!IsCardinalStep(path[i - 1], path[i]))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Compute Manhattan distance between two grid cells.
    /// </summary>
    public int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    /// <summary>
    /// Return the first mandatory step direction when entering map from a border cell.
    /// South border → step north; North border → step south; West border → step east; East border → step west.
    /// Returns null if cell is not on a border.
    /// </summary>
    public Vector2Int? GetFirstStepDirectionFromBorder(Vector2Int borderCell, int w, int h)
    {
        if (borderCell.y == 0) return new Vector2Int(0, 1);
        if (borderCell.y == h - 1) return new Vector2Int(0, -1);
        if (borderCell.x == 0) return new Vector2Int(1, 0);
        if (borderCell.x == w - 1) return new Vector2Int(-1, 0);
        return null;
    }
}
}
