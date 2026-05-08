using UnityEngine;
using System.Collections.Generic;

namespace Domains.Roads
{
    /// <summary>
    /// Public facade interface for interstate highway operations.
    /// Consumers bind to IInterstate — never to InterstateManager directly.
    /// Extracted from Territory.Roads.InterstateManager per atomization Stage 16 (TECH-23789).
    /// </summary>
    public interface IInterstate
    {
        /// <summary>Whether player road network is connected to the interstate (updated monthly).</summary>
        bool IsConnectedToInterstate { get; }

        /// <summary>Read-only list of grid positions that are part of the interstate.</summary>
        IReadOnlyList<Vector2Int> InterstatePositions { get; }

        /// <summary>Border entry point of the interstate (set during generation or rebuild).</summary>
        Vector2Int? EntryPoint { get; }

        /// <summary>Border exit point of the interstate.</summary>
        Vector2Int? ExitPoint { get; }

        /// <summary>Border index of the entry point (0=South, 1=North, 2=West, 3=East; -1 unset).</summary>
        int EntryBorder { get; }

        /// <summary>Border index of the exit point.</summary>
        int ExitBorder { get; }

        /// <summary>True if cell at (x, y) is an interstate tile.</summary>
        bool IsInterstateAt(int x, int y);

        /// <summary>True if player can start placing a street from (x, y).</summary>
        bool CanPlaceStreetFrom(int x, int y);

        /// <summary>BFS connectivity check: set IsConnectedToInterstate flag.</summary>
        void CheckInterstateConnectivity();

        /// <summary>Rebuild interstate positions list from grid (call after RestoreGrid on load).</summary>
        void RebuildFromGrid();

        /// <summary>Force-set connectivity flag (used during save/load restore).</summary>
        void SetConnectedToInterstate(bool connected);
    }
}
