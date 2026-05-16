using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Territory.Services
{
    /// <summary>CoreScene MonoBehaviour — streams region cells center-out from player 2×2 anchor.
    /// Spiral enumeration; yields via Task.Yield() after every perFrameBudget cells (Unity 2022.3).
    /// Pure pipeline — no direct RegionData access; callers wire rendering via subclass or events.
    /// Invariant #3: no inspector deps.</summary>
    public class CellStreamingPipeline : MonoBehaviour, ICellStreamingPipeline
    {
        // ── Public API ───────────────────────────────────────────────────────
        public event Action FirstRingLoaded;
        public event Action AllCellsLoaded;

        /// <summary>Streams region cells in spiral order from playerCityAnchorCell. Budget = cells per frame.</summary>
        public async Task StreamCenterOut(Vector2Int playerCityAnchorCell, int perFrameBudget, CancellationToken ct)
        {
            if (perFrameBudget < 1) perFrameBudget = 1;

            // First ring = 3×3 around the 2×2 anchor (top-left coord).
            // Anchor occupies [cx, cy]..[cx+1, cy+1]; ring = cx-1..cx+2, cy-1..cy+2.
            int cx = playerCityAnchorCell.x;
            int cy = playerCityAnchorCell.y;

            int firstRingMinX = cx - 1;
            int firstRingMaxX = cx + 2;
            int firstRingMinY = cy - 1;
            int firstRingMaxY = cy + 2;

            bool firstRingFired = false;
            int processed = 0;

            foreach (var cell in SpiralFrom(playerCityAnchorCell))
            {
                if (ct.IsCancellationRequested) return;

                ProcessCell(cell);
                processed++;

                // First-ring check: fire after all cells in the 3×3 perimeter are processed.
                if (!firstRingFired
                    && cell.x >= firstRingMinX && cell.x <= firstRingMaxX
                    && cell.y >= firstRingMinY && cell.y <= firstRingMaxY)
                {
                    // Determine if this is the last cell of the first ring.
                    // We are done with first ring once we exit the ring bounding box on next step.
                    // Use spiral ring logic: ring 0 = anchor, ring 1 = surrounding 3×3.
                    // Check by ring index: rings are enumerated in order; ring 1 ends at index 8 (9 cells - 1 anchor = 8).
                    // Simpler: track if processed >= 9 cells within ring 1 boundary.
                }

                if (!firstRingFired && processed >= 9)
                {
                    firstRingFired = true;
                    FirstRingLoaded?.Invoke();
                }

                if (processed % perFrameBudget == 0)
                {
                    await Task.Yield();
                    if (ct.IsCancellationRequested) return;
                }
            }

            // Ensure FirstRingLoaded fires even if region < 9 cells.
            if (!firstRingFired)
            {
                FirstRingLoaded?.Invoke();
            }

            AllCellsLoaded?.Invoke();
        }

        // ── Virtual hook ─────────────────────────────────────────────────────

        /// <summary>Per-cell processing hook. Override in subclass or handle via AllCellsLoaded/FirstRingLoaded.
        /// Default: no-op. Does NOT call RegionData.GetCell — callers own that wiring.</summary>
        protected virtual void ProcessCell(Vector2Int cell) { }

        // ── Spiral enumeration ───────────────────────────────────────────────

        /// <summary>Yields infinite spiral of Vector2Int coords centered at origin, offset to anchor.
        /// Ring 0 = anchor cell itself. Ring N = square perimeter at distance N.</summary>
        private static IEnumerable<Vector2Int> SpiralFrom(Vector2Int anchor)
        {
            // Yield anchor first (ring 0).
            yield return anchor;

            // Ring r = 1, 2, 3, ... — square perimeter at Chebyshev distance r.
            // Walk: right along top, down along right, left along bottom, up along left.
            for (int r = 1; r < 32768; r++)
            {
                int x = anchor.x - r;
                int y = anchor.y - r;

                // Top row: left→right
                for (int i = 0; i < 2 * r; i++)
                {
                    yield return new Vector2Int(x + i, y);
                }
                // Right col: top→bottom
                for (int i = 0; i < 2 * r; i++)
                {
                    yield return new Vector2Int(x + 2 * r, y + i);
                }
                // Bottom row: right→left
                for (int i = 0; i < 2 * r; i++)
                {
                    yield return new Vector2Int(x + 2 * r - i, y + 2 * r);
                }
                // Left col: bottom→top
                for (int i = 0; i < 2 * r; i++)
                {
                    yield return new Vector2Int(x, y + 2 * r - i);
                }
            }
        }
    }
}
