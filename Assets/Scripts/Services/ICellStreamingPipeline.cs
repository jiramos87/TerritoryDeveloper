using System;
using System.Threading;
using UnityEngine;

namespace Territory.Services
{
    /// <summary>Contract for CellStreamingPipeline — center-out region cell streaming.</summary>
    public interface ICellStreamingPipeline
    {
        /// <summary>Fired once when the first ring (3×3 around 2×2 anchor) is fully loaded.</summary>
        event Action FirstRingLoaded;

        /// <summary>Fired once when all region cells are loaded.</summary>
        event Action AllCellsLoaded;

        /// <summary>Stream region cells center-out from player 2×2 anchor, processing perFrameBudget cells per frame.</summary>
        Awaitable StreamCenterOut(Vector2Int playerCityAnchorCell, int perFrameBudget, CancellationToken ct);
    }
}
