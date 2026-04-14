using System.Collections.Generic;
using UnityEngine;
using Territory.Roads;

namespace Territory.Core
{
    /// <summary>
    /// Static helper — post-interstate-build recorder.
    /// Scans each cell on <paramref name="stroke"/>, resolves <see cref="BorderSide"/> for
    /// border cells, matches a <see cref="NeighborCityStub"/> in <paramref name="stubs"/>,
    /// and appends a <see cref="NeighborCityBinding"/> to <paramref name="bindings"/>.
    /// Dedupes on (stubId, exitCellX, exitCellY). Warns + skips on missing stub match.
    /// Called from <see cref="RoadManager"/> interstate commit post-<c>InvalidateRoadCache</c>.
    /// Does not modify road state; no MonoBehaviour.
    /// </summary>
    public static class NeighborCityBindingRecorder
    {
        /// <summary>
        /// Scan <paramref name="stroke"/> for border exit cells and record bindings.
        /// </summary>
        /// <param name="bindings">Mutable bindings list to append to (GameSaveManager.neighborCityBindings). Must be non-null.</param>
        /// <param name="stubs">Neighbor stubs to match against (GameSaveManager.NeighborStubs). Must be non-null.</param>
        /// <param name="interstate">Active <see cref="InterstateManager"/>; provides EntryBorder/ExitBorder for corner tie-break
        /// and grid dimensions via <c>gridManager</c>.</param>
        /// <param name="stroke">Ordered list of grid positions on the placed interstate stroke.</param>
        public static void RecordExits(
            List<NeighborCityBinding> bindings,
            IReadOnlyList<NeighborCityStub> stubs,
            InterstateManager interstate,
            IReadOnlyList<Vector2Int> stroke)
        {
            if (bindings == null)
            {
                Debug.LogError("[NeighborCityBindingRecorder] bindings list is null — cannot record exits.");
                return;
            }
            if (stubs == null)
            {
                Debug.LogError("[NeighborCityBindingRecorder] stubs list is null — cannot record exits.");
                return;
            }
            if (stroke == null || stroke.Count == 0)
                return;

            int width  = interstate != null && interstate.gridManager != null ? interstate.gridManager.width  : 0;
            int height = interstate != null && interstate.gridManager != null ? interstate.gridManager.height : 0;

            if (width <= 0 || height <= 0)
            {
                Debug.LogWarning("[NeighborCityBindingRecorder] Grid dimensions unknown — cannot resolve border sides.");
                return;
            }

            // Build a set for fast dedupe lookup against existing entries.
            var existing = new HashSet<(string, int, int)>();
            foreach (var b in bindings)
                existing.Add((b.stubId, b.exitCellX, b.exitCellY));

            bool warnedMiss = false; // Log once per stroke to avoid log spam.
            foreach (var cell in stroke)
            {
                if (!IsBorderCell(cell.x, cell.y, width, height))
                    continue;

                BorderSide side = ResolveBorderSide(cell.x, cell.y, width, height, interstate);
                NeighborCityStub? match = FindStubForSide(stubs, side);
                if (match == null)
                {
                    if (!warnedMiss)
                    {
                        Debug.LogWarning($"[NeighborCityBindingRecorder] No stub found for side {side} at ({cell.x},{cell.y}) — skipping.");
                        warnedMiss = true;
                    }
                    continue;
                }

                string stubId = match.Value.id;
                var key = (stubId, cell.x, cell.y);
                if (existing.Contains(key))
                    continue; // Dedupe — same stroke re-applied.

                existing.Add(key);
                bindings.Add(new NeighborCityBinding
                {
                    stubId    = stubId,
                    exitCellX = cell.x,
                    exitCellY = cell.y,
                });
            }
        }

        // ── private helpers ──────────────────────────────────────────────────────

        /// <summary>True when (x,y) lies on any of the four map edges.</summary>
        private static bool IsBorderCell(int x, int y, int width, int height)
        {
            return x == 0 || x == width - 1 || y == 0 || y == height - 1;
        }

        /// <summary>
        /// Resolve <see cref="BorderSide"/> for a border cell.
        /// Non-corner: determined by which edge the cell touches.
        /// Corner (touches two edges): tie-break via interstate EntryBorder / ExitBorder — the border
        /// index whose matching edge the corner touches is preferred. Falls back to x-axis (West/East).
        /// Convention (matches NeighborStubSeeder.BorderIndexToSide): 0=South, 1=North, 2=West, 3=East.
        /// </summary>
        private static BorderSide ResolveBorderSide(int x, int y, int width, int height, InterstateManager interstate)
        {
            bool onSouth = y == 0;
            bool onNorth = y == height - 1;
            bool onWest  = x == 0;
            bool onEast  = x == width - 1;

            // Non-corner: exactly one edge.
            int edgeCount = (onSouth ? 1 : 0) + (onNorth ? 1 : 0) + (onWest ? 1 : 0) + (onEast ? 1 : 0);
            if (edgeCount == 1)
            {
                if (onSouth) return BorderSide.South;
                if (onNorth) return BorderSide.North;
                if (onWest)  return BorderSide.West;
                return BorderSide.East;
            }

            // Corner: tie-break by interstate border indices.
            if (interstate != null)
            {
                int entry = interstate.EntryBorder;
                int exit  = interstate.ExitBorder;
                foreach (int idx in new[] { entry, exit })
                {
                    switch (idx)
                    {
                        case 0 when onSouth: return BorderSide.South;
                        case 1 when onNorth: return BorderSide.North;
                        case 2 when onWest:  return BorderSide.West;
                        case 3 when onEast:  return BorderSide.East;
                    }
                }
            }

            // Default tie-break: West/East by x-axis.
            return onWest ? BorderSide.West : (onEast ? BorderSide.East : (onSouth ? BorderSide.South : BorderSide.North));
        }

        /// <summary>
        /// Find first stub in <paramref name="stubs"/> whose <see cref="BorderSide"/> matches.
        /// Returns null when not found.
        /// </summary>
        private static NeighborCityStub? FindStubForSide(IReadOnlyList<NeighborCityStub> stubs, BorderSide side)
        {
            for (int i = 0; i < stubs.Count; i++)
            {
                if (stubs[i].borderSide == side)
                    return stubs[i];
            }
            return null;
        }
    }
}
