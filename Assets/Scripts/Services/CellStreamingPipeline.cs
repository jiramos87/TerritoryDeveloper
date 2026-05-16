using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Territory.RegionScene;
using Territory.RegionScene.Evolution;

namespace Territory.Services
{
    /// <summary>Streams region cells center-out from player 2×2 anchor via spiral enumeration, yielding Awaitable.NextFrameAsync every perFrameBudget cells.</summary>
    public class CellStreamingPipeline : MonoBehaviour, ICellStreamingPipeline
    {
        public event Action FirstRingLoaded;
        public event Action AllCellsLoaded;

        [SerializeField] private RegionManager regionManager;

        /// <summary>Stream cells in spiral order from anchor. Yields per frame after processing perFrameBudget cells. Fires FirstRingLoaded (first ring complete) and AllCellsLoaded (all done).</summary>
        public async Awaitable StreamCenterOut(Vector2Int playerCityAnchorCell, int perFrameBudget, CancellationToken ct)
        {
            if (perFrameBudget <= 0) perFrameBudget = 8;

            // Anchor center = playerCityAnchorCell (top-left of 2×2 anchor block)
            Vector2Int center = playerCityAnchorCell;

            // First ring = 3×3 around anchor center: Chebyshev distance ≤ 1 → 9 cells (clamped to grid bounds)
            const int FirstRingRadius = 1;

            // Build spiral cell list center-out using Chebyshev ring expansion
            var cells = BuildSpiralCells(center);

            // Pre-compute first-ring cell count (cells with Chebyshev dist ≤ 1 inside grid bounds)
            const int GridSize = 64;
            int firstRingCount = 0;
            for (int dy = -FirstRingRadius; dy <= FirstRingRadius; dy++)
            for (int dx = -FirstRingRadius; dx <= FirstRingRadius; dx++)
            {
                int cx = center.x + dx;
                int cy = center.y + dy;
                if (cx >= 0 && cx < GridSize && cy >= 0 && cy < GridSize)
                    firstRingCount++;
            }

            bool firstRingFired = false;
            int processed = 0;

            foreach (var cell in cells)
            {
                ct.ThrowIfCancellationRequested();

                // Read cell through RegionManager (invariant #5 — no direct gridArray access)
                if (regionManager != null)
                    regionManager.GetCell(cell.x, cell.y);

                processed++;

                // Fire FirstRingLoaded when all first-ring cells have been processed
                if (!firstRingFired && processed >= firstRingCount)
                {
                    firstRingFired = true;
                    FirstRingLoaded?.Invoke();
                }

                // Yield every perFrameBudget cells
                if (processed % perFrameBudget == 0)
                    await Awaitable.NextFrameAsync(ct);
            }

            // Edge case: region smaller than first ring
            if (!firstRingFired)
                FirstRingLoaded?.Invoke();

            AllCellsLoaded?.Invoke();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>Build spiral cell list from center outward using Chebyshev ring expansion.</summary>
        private static List<Vector2Int> BuildSpiralCells(Vector2Int center)
        {
            // RegionHeightMap.RegionGridSize = 64×64
            const int GridSize = 64;
            var result = new List<Vector2Int>(GridSize * GridSize);

            // Emit ring 0 first (the center cell itself)
            result.Add(center);

            // Then expand rings outward
            int maxRadius = Mathf.Max(GridSize, GridSize); // cover full grid
            for (int r = 1; r <= maxRadius; r++)
            {
                // Walk perimeter of Chebyshev ring r: top edge, right edge, bottom edge, left edge
                // Top row: y = center.y + r, x from center.x - r to center.x + r
                for (int x = center.x - r; x <= center.x + r; x++)
                    TryAdd(result, x, center.y + r, GridSize);
                // Right col: x = center.x + r, y from center.y + r - 1 down to center.y - r
                for (int y = center.y + r - 1; y >= center.y - r; y--)
                    TryAdd(result, center.x + r, y, GridSize);
                // Bottom row: y = center.y - r, x from center.x + r - 1 down to center.x - r
                for (int x = center.x + r - 1; x >= center.x - r; x--)
                    TryAdd(result, x, center.y - r, GridSize);
                // Left col: x = center.x - r, y from center.y - r + 1 up to center.y + r - 1
                for (int y = center.y - r + 1; y <= center.y + r - 1; y++)
                    TryAdd(result, center.x - r, y, GridSize);

                // Stop expanding once ring boundary is fully outside grid
                if (RingFullyOutsideGrid(center, r, GridSize)) break;
            }

            return result;
        }

        private static void TryAdd(List<Vector2Int> list, int x, int y, int gridSize)
        {
            if (x >= 0 && x < gridSize && y >= 0 && y < gridSize)
                list.Add(new Vector2Int(x, y));
        }

        private static bool RingFullyOutsideGrid(Vector2Int center, int r, int gridSize)
        {
            return center.x - r < 0 && center.x + r >= gridSize &&
                   center.y - r < 0 && center.y + r >= gridSize;
        }
    }
}
