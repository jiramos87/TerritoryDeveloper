using UnityEngine;
using System.Collections.Generic;

namespace Domains.Roads.Services
{
    /// <summary>
    /// Pure road stroke validation logic extracted from RoadManager.
    /// No MonoBehaviour dependency — stateless helper for stroke path checks.
    /// Extracted from Territory.Roads.RoadManager per atomization Stage 3 (TECH-23776).
    /// Invariant #2: InvalidateRoadCache remains in GridManager (called by RoadManager after commit).
    /// Invariant #10: PathTerraformPlan.Apply validation stays in RoadManager.TryPrepareRoadPlacementPlan.
    /// </summary>
    public class StrokeService
    {
        /// <summary>Minimum cells in a valid road stroke (start + end).</summary>
        public const int MinStrokeCellCount = 2;

        /// <summary>
        /// Validate raw stroke path meets minimum requirements.
        /// Returns false + error if path null, empty, or below minimum cell count.
        /// </summary>
        public bool ValidateStrokePath(List<Vector2> pathRaw, out string error)
        {
            if (pathRaw == null || pathRaw.Count < MinStrokeCellCount)
            {
                error = $"road stroke must list at least {MinStrokeCellCount} cells";
                return false;
            }
            error = null;
            return true;
        }

        /// <summary>
        /// True if all consecutive cell pairs in the stroke are cardinal-adjacent (no diagonals, no gaps).
        /// Diagonal connections are handled by RoadPrefabResolver elbow logic — raw strokes must be cardinal.
        /// </summary>
        public bool IsCardinalAdjacent(List<Vector2> path)
        {
            if (path == null || path.Count < 2) return false;
            for (int i = 1; i < path.Count; i++)
            {
                float dx = Mathf.Abs(path[i].x - path[i - 1].x);
                float dy = Mathf.Abs(path[i].y - path[i - 1].y);
                // Cardinal only: exactly one axis changes by 1
                if (!((dx == 1f && dy == 0f) || (dx == 0f && dy == 1f)))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// True if stroke contains duplicate cell positions (loop or self-intersect).
        /// Used to early-reject degenerate strokes before terraform plan computation.
        /// </summary>
        public bool HasDuplicateCells(List<Vector2> path)
        {
            if (path == null || path.Count < 2) return false;
            var seen = new HashSet<Vector2>();
            foreach (var cell in path)
            {
                if (!seen.Add(cell))
                    return true;
            }
            return false;
        }
    }
}
