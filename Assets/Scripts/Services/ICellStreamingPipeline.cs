using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Territory.Services
{
    /// <summary>Contract for center-out cell streaming pipeline.</summary>
    public interface ICellStreamingPipeline
    {
        /// <summary>Fires once when first ring (3×3 around 2×2 anchor) is loaded.</summary>
        event Action FirstRingLoaded;

        /// <summary>Fires once when all cells have been processed.</summary>
        event Action AllCellsLoaded;

        /// <summary>Streams region cells in spiral order from playerCityAnchorCell, N cells per frame.</summary>
        Task StreamCenterOut(Vector2Int playerCityAnchorCell, int perFrameBudget, CancellationToken ct);
    }
}
